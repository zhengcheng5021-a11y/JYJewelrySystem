using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;
using ZR.Model.System;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyGoodsService : BaseService<JyGoodsItem>
    {
        private readonly JyLookupService _lookup;

        public JyGoodsService(JyLookupService lookup)
        {
            _lookup = lookup;
        }

        public PagedInfo<JyGoodsItemDto> List(JyActor actor, JyGoodsItemQueryDto query)
        {
            query ??= new JyGoodsItemQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var period = query.Period?.Trim();
            if (!string.IsNullOrEmpty(period) && (period.Length != 6 || !period.All(char.IsDigit)))
                throw new CustomException("年月格式应为 yyyyMM");
            if (!string.IsNullOrEmpty(period)) period += "%";
            var begin = query.BeginTime?.Date;
            var endExclusive = query.EndTime?.Date.AddDays(1);
            if (begin.HasValue && endExclusive.HasValue && begin.Value >= endExclusive.Value)
                throw new CustomException("开始日期不能晚于结束日期");

            var q = Context.Queryable<JyGoodsItem>()
                .LeftJoin<JyInbound>((it, inbound) => it.InboundId == inbound.Id
                    && inbound.TenantId == actor.TenantId
                    && inbound.DelFlag == 0)
                .Where((it, inbound) => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is >= 1 and <= 6, (it, inbound) => it.Status == query.Status)
                .WhereIF(query.InboundId > 0, (it, inbound) => it.InboundId == query.InboundId)
                .WhereIF(!string.IsNullOrEmpty(period), (it, inbound) => it.GoodsNo.StartsWith(period.Substring(0, 6)))
                .WhereIF(begin.HasValue, (it, inbound) => inbound.InboundTime >= begin.Value)
                .WhereIF(endExclusive.HasValue, (it, inbound) => inbound.InboundTime < endExclusive.Value)
                .WhereIF(!string.IsNullOrEmpty(keyword), (it, inbound) =>
                    it.GoodsNo.Contains(keyword) || it.Barcode.Contains(keyword) || it.StyleName.Contains(keyword))
                .Select((it, inbound) => it);
            var total = 0;
            var rows = q.OrderBy(it => it.Status)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JyGoodsItemDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDtos(actor.TenantId, rows)
            };
        }

        public JyGoodsItemDto Detail(JyActor actor, long id)
        {
            return ToDtos(actor.TenantId, new List<JyGoodsItem> { Must(actor.TenantId, id) }).First();
        }

        /// <summary>保留分类选项供库存页筛选，商品本身只能通过完成入库自动生成。</summary>
        public JyGoodsOptionsDto Options(JyActor actor)
        {
            _lookup.EnsureFoundation(actor);
            return new JyGoodsOptionsDto
            {
                Categories = _lookup.Options(actor, "style", true),
                Materials = _lookup.Options(actor, "material", true),
                Metals = _lookup.Options(actor, "metal", true),
                CertTypes = _lookup.Options(actor, "cert_type", true),
                Suppliers = new List<JyOptionDto>()
            };
        }

        /// <summary>商品档案只允许编辑销售相关信息，不允许改动货品编码和来源。</summary>
        public long Save(JyActor actor, JyGoodsItemEditDto dto)
        {
            if (dto == null || dto.Id <= 0) throw new CustomException("商品档案不存在");
            var current = Must(actor.TenantId, dto.Id);
            var goodsName = JyBillNo.NormalizeName(dto.GoodsName, "货品名称", 100);
            if (dto.CostPrice < 0 || dto.SalePrice < 0) throw new CustomException("价格不能为负数");
            if (dto.PricingMode is < 1 or > 2) throw new CustomException("计价方式不正确");
            if (dto.LaborFee < 0) throw new CustomException("工费不能为负数");
            if (dto.PackagingFee < 0) throw new CustomException("包装费不能为负数");
            if (dto.LaborFeeMode is < 1 or > 2) throw new CustomException("工费方式不正确");
            if (dto.PricingMode == 2 && (!dto.Weight.HasValue || dto.Weight.Value <= 0))
                throw new CustomException("按克计价商品必须填写重量");
            if (dto.Status < 1 || dto.Status > 6) throw new CustomException("商品状态不正确");
            var remark = NormalizeText(dto.Remark, 500, "备注");
            var unit = NormalizeUnit(dto.Unit);
            if (dto.Weight.HasValue && (dto.Weight < 0 || dto.Weight > 100000m))
                throw new CustomException("银重不正确");
            var imageFileId = dto.ImageFileId > 0 ? dto.ImageFileId : null;
            EnsureImage(actor.TenantId, imageFileId);
            ApplyAttributes(actor.TenantId, dto);

            current.StyleName = goodsName;
            current.StyleItemId = dto.StyleItemId;
            current.StyleItemName = dto.StyleItemName;
            current.MaterialItemId = dto.MaterialItemId;
            current.MaterialGroup = dto.MaterialGroup is >= 1 and <= 3 ? dto.MaterialGroup : 3;
            current.MaterialName = dto.MaterialName;
            current.MetalItemId = dto.MetalItemId;
            current.MetalName = dto.MetalName;
            current.CertTypeId = dto.CertTypeId;
            current.CertTypeName = dto.CertTypeName;
            current.Weight = dto.Weight.HasValue
                ? decimal.Round(dto.Weight.Value, 2, MidpointRounding.AwayFromZero)
                : null;
            current.Unit = unit;
            current.CostPrice = dto.CostPrice;
            current.SalePrice = dto.SalePrice;
            current.PricingMode = dto.PricingMode;
            current.LaborFee = decimal.Round(dto.LaborFee, 2, MidpointRounding.AwayFromZero);
            current.PackagingFee = decimal.Round(dto.PackagingFee, 2, MidpointRounding.AwayFromZero);
            current.LaborFeeMode = dto.PricingMode == 2 ? 2 : dto.LaborFeeMode;
            current.ImageFileId = imageFileId;
            current.Status = dto.Status;
            current.Remark = remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current)
                .UpdateColumns(it => new { it.StyleName, it.StyleItemId, it.StyleItemName, it.MaterialItemId, it.MaterialGroup, it.MaterialName, it.MetalItemId, it.MetalName, it.CertTypeId, it.CertTypeName, it.Weight, it.Unit, it.CostPrice, it.SalePrice, it.PricingMode, it.LaborFee, it.PackagingFee, it.LaborFeeMode, it.ImageFileId, it.Status, it.Remark, it.Update_by, it.Update_time })
                .Where(it => it.Id == current.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
            return current.Id;
        }

        private List<JyGoodsItemDto> ToDtos(string tenantId, List<JyGoodsItem> rows)
        {
            if (rows.Count == 0) return new List<JyGoodsItemDto>();
            var supplierIds = rows.Where(it => it.SupplierId.HasValue).Select(it => it.SupplierId.Value).Distinct().ToList();
            var suppliers = supplierIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JySupplier>()
                .Where(it => it.TenantId == tenantId && supplierIds.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id, it => it.SupplierName);
            var inboundIds = rows.Where(it => it.InboundId.HasValue).Select(it => it.InboundId.Value).Distinct().ToList();
            var inbounds = inboundIds.Count == 0 ? new Dictionary<long, JyInbound>() : Context.Queryable<JyInbound>()
                .Where(it => it.TenantId == tenantId && inboundIds.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id);
            var images = ImageUrls(tenantId, rows.Select(it => it.ImageFileId));
            return rows.Select(it =>
            {
                inbounds.TryGetValue(it.InboundId ?? 0, out var inbound);
                return new JyGoodsItemDto
                {
                    Id = it.Id,
                    GoodsNo = it.GoodsNo,
                    Barcode = it.Barcode,
                    GoodsName = it.StyleName,
                    StyleId = it.StyleId,
                    StyleItemId = it.StyleItemId,
                    StyleItemName = it.StyleItemName,
                    MaterialItemId = it.MaterialItemId,
                    MaterialGroup = it.MaterialGroup,
                    MaterialName = it.MaterialName,
                    MetalItemId = it.MetalItemId,
                    MetalName = it.MetalName,
                    CertTypeId = it.CertTypeId,
                    CertTypeName = it.CertTypeName,
                    Qty = it.Qty,
                    Weight = it.Weight,
                    Unit = it.Unit,
                    CostPrice = it.CostPrice,
                    SalePrice = it.SalePrice,
                    PricingMode = it.PricingMode,
                    LaborFee = it.LaborFee,
                    PackagingFee = it.PackagingFee,
                    LaborFeeMode = it.LaborFeeMode,
                    ImageFileId = it.ImageFileId,
                    ImageUrl = ImageUrl(images, it.ImageFileId),
                    Status = it.Status,
                    LockedQty = it.LockedQty,
                    LockBizType = it.LockBizType,
                    LockBizId = it.LockBizId,
                    LockText = it.Status == 2 ? (it.LockBizType == "custom" ? "定制定金锁定" : "已锁定") : null,
                    SupplierName = it.SupplierId.HasValue && suppliers.TryGetValue(it.SupplierId.Value, out var supplier) ? supplier : null,
                    InboundNo = inbound?.InboundNo,
                    InboundTime = inbound?.InboundTime,
                    CreateTime = it.Create_time,
                    Remark = it.Remark
                };
            }).ToList();
        }

        /// <summary>校验商品属性字典，并把服务端名称快照写入 DTO。</summary>
        private void ApplyAttributes(string tenantId, JyGoodsItemEditDto dto)
        {
            if (dto.StyleItemId <= 0) dto.StyleItemId = null;
            if (dto.MaterialItemId <= 0) dto.MaterialItemId = null;
            if (dto.MetalItemId <= 0) dto.MetalItemId = null;
            if (dto.CertTypeId <= 0) dto.CertTypeId = null;

            var typeCodes = new[] { "style", "material", "metal", "cert_type" };
            var types = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && typeCodes.Contains(it.TypeCode))
                .ToList();
            var typeIds = types.ToDictionary(it => it.TypeCode, it => it.Id);
            var ids = new[] { dto.StyleItemId, dto.MaterialItemId, dto.MetalItemId, dto.CertTypeId }
                .Where(it => it.HasValue).Select(it => it.Value).Distinct().ToList();
            var items = ids.Count == 0
                ? new List<JyLookupItem>()
                : Context.Queryable<JyLookupItem>()
                    .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && ids.Contains(it.Id))
                    .ToList();
            var map = items.ToDictionary(it => it.Id);

            dto.StyleItemName = ResolveAttribute(map, typeIds, dto.StyleItemId, "style", "款式分类", true);
            dto.MaterialName = ResolveAttribute(map, typeIds, dto.MaterialItemId, "material", "材质", false);
            dto.MetalName = ResolveAttribute(map, typeIds, dto.MetalItemId, "metal", "金属成色", false);
            dto.CertTypeName = ResolveAttribute(map, typeIds, dto.CertTypeId, "cert_type", "证书类型", false);
        }

        private static string ResolveAttribute(Dictionary<long, JyLookupItem> items, Dictionary<string, long> typeIds,
            long? id, string typeCode, string label, bool requireSecondLevel)
        {
            if (!id.HasValue || id.Value <= 0) return null;
            if (!typeIds.TryGetValue(typeCode, out var typeId)
                || !items.TryGetValue(id.Value, out var item)
                || item.TypeId != typeId)
                throw new CustomException(label + "不存在或不属于当前字典");
            if (item.Status != 0) throw new CustomException(label + "已停用");
            if (requireSecondLevel && !item.ParentId.HasValue) throw new CustomException(label + "请选择二级分类");
            return item.ItemName;
        }

        private Dictionary<long, string> ImageUrls(string tenantId, IEnumerable<long?> fileIds)
        {
            var values = fileIds.Where(it => it.HasValue && it.Value > 0).Select(it => it.Value).Distinct().ToList();
            return values.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<SysFile>()
                .Where(it => values.Contains(it.Id) && (it.TenantId == tenantId || it.TenantId == null))
                .ToList().ToDictionary(it => it.Id, it => it.AccessUrl);
        }

        private void EnsureImage(string tenantId, long? fileId)
        {
            if (!fileId.HasValue) return;
            if (!Context.Queryable<SysFile>().Any(it => it.Id == fileId.Value && (it.TenantId == tenantId || it.TenantId == null)))
                throw new CustomException("货品图片不存在或无权使用，请重新上传");
        }

        private JyGoodsItem Must(string tenantId, long id)
        {
            return Context.Queryable<JyGoodsItem>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("商品档案不存在");
        }

        private static string ImageUrl(Dictionary<long, string> images, long? id)
            => id.HasValue && images.TryGetValue(id.Value, out var url) ? url : null;

        private static readonly HashSet<string> AllowedUnits = new(StringComparer.Ordinal)
        {
            "条", "颗", "对", "件", "套"
        };

        private static string NormalizeUnit(string value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return "件";
            if (!AllowedUnits.Contains(value)) throw new CustomException("计量单位只能选择条、颗、对、件或套");
            return value;
        }

        private static string NormalizeText(string value, int max, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > max) throw new CustomException($"{label}不能超过 {max} 个字");
            return value;
        }
    }
}
