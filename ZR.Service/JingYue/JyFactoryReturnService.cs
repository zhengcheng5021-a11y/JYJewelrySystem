using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>
    /// 商品返厂：草稿保存不动库存，确认返厂后商品变为状态 8（返厂出库）并写库存流水。
    /// 返厂不生成标签，标签只在入库时生成。
    /// </summary>
    [AppService]
    public class JyFactoryReturnService : BaseService<JyFactoryReturn>
    {
        private readonly JyBillRuleService _billRule;

        public JyFactoryReturnService(JyBillRuleService billRule)
        {
            _billRule = billRule;
        }

        public PagedInfo<JyFactoryReturnDto> List(JyActor actor, JyFactoryReturnQueryDto query)
        {
            query ??= new JyFactoryReturnQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            if (query.BeginTime.HasValue && query.EndTime.HasValue && query.BeginTime.Value.Date > query.EndTime.Value.Date)
                throw new CustomException("开始日期不能晚于结束日期");
            var q = Context.Queryable<JyFactoryReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is >= 0 and <= 9, it => it.Status == query.Status)
                .WhereIF(query.SupplierId > 0, it => it.SupplierId == query.SupplierId)
                .WhereIF(query.BeginTime.HasValue, it => it.ReturnDate >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.ReturnDate < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.ReturnNo.Contains(keyword)
                    || it.SupplierName.Contains(keyword) || it.Remark.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.Status)
                .OrderBy(it => it.ReturnDate, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JyFactoryReturnDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(ToDto).ToList()
            };
        }

        public JyFactoryReturnDto Detail(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            var dto = ToDto(row);
            dto.Items = LoadItems(actor.TenantId, row.Id);
            return dto;
        }

        /// <summary>可返厂商品：仅状态 1（在库）、未锁定、数量充足。</summary>
        public List<JyStockItemDto> FindGoods(JyActor actor, string keyword)
        {
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword)) return new List<JyStockItemDto>();
            if (keyword.Length > 64) throw new CustomException("商品查询内容不能超过 64 个字");
            var q = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1 && it.Qty > 0);
            var exact = q.Where(it => it.Barcode == keyword || it.GoodsNo == keyword).ToList();
            var rows = exact.Count > 0
                ? exact
                : q.Where(it => it.GoodsNo.Contains(keyword) || it.Barcode.Contains(keyword)
                        || it.StyleName.Contains(keyword) || it.StyleItemName.Contains(keyword))
                    .OrderBy(it => it.Id, OrderByType.Desc).Take(20).ToList();
            return rows.Select(it => new JyStockItemDto
            {
                Id = it.Id,
                GoodsNo = it.GoodsNo,
                Barcode = it.Barcode,
                StyleId = it.StyleId,
                StyleName = it.StyleName,
                StyleItemName = it.StyleItemName,
                MaterialName = it.MaterialName,
                MetalName = it.MetalName,
                ItemMode = it.ItemMode,
                Qty = it.Qty,
                LockedQty = it.LockedQty,
                Weight = it.Weight,
                CostPrice = it.CostPrice,
                SalePrice = it.SalePrice,
                Status = it.Status,
                CreateTime = it.Create_time
            }).ToList();
        }

        public long Save(JyActor actor, JyFactoryReturnEditDto dto)
        {
            if (dto == null) throw new CustomException("返厂单不能为空");
            if (dto.Id > 0)
            {
                var exist = Must(actor.TenantId, dto.Id);
                if (exist.Status != 0) throw new CustomException("已确认或作废的返厂单不能修改");
            }
            if (dto.Items == null || dto.Items.Count == 0) throw new CustomException("请至少选择一件返厂商品");
            if (dto.Items.Count > 100) throw new CustomException("单张返厂单最多支持 100 条明细");
            var dup = dto.Items.GroupBy(it => it.GoodsItemId).FirstOrDefault(it => it.Key <= 0 || it.Count() > 1);
            if (dup != null) throw new CustomException("返厂明细中存在重复或无效的商品");
            var remark = NormalizeText(dto.Remark, 500, "备注");
            var returnDate = dto.ReturnDate?.Date ?? DateTime.Today;

            var goodsIds = dto.Items.Select(it => it.GoodsItemId).Distinct().ToList();
            var goodsRows = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && goodsIds.Contains(it.Id))
                .ToList();
            if (goodsRows.Count != goodsIds.Count) throw new CustomException("部分返厂商品不存在或已删除");
            var goodsMap = goodsRows.ToDictionary(it => it.Id);
            foreach (var line in dto.Items)
            {
                var goods = goodsMap[line.GoodsItemId];
                if (goods.Status != 1) throw new CustomException($"商品 {goods.GoodsNo} 不在库，不能返厂");
                if (line.Qty <= 0) throw new CustomException($"商品 {goods.GoodsNo} 的返厂数量必须大于 0");
                if (goods.ItemMode == 1 && line.Qty != 1m) throw new CustomException($"单件商品 {goods.GoodsNo} 的数量只能为 1");
                if (line.Qty > goods.Qty) throw new CustomException($"商品 {goods.GoodsNo} 库存不足，当前在库 {goods.Qty}");
            }

            string supplierName = null;
            if (dto.SupplierId > 0)
            {
                var supplier = Context.Queryable<JySupplier>()
                    .First(it => it.Id == dto.SupplierId.Value && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (supplier == null) throw new CustomException("供应商不存在");
                supplierName = supplier.SupplierName;
            }

            long resultId = 0;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                JyFactoryReturn header;
                if (dto.Id > 0)
                {
                    header = Must(actor.TenantId, dto.Id);
                    if (header.Status != 0) throw new CustomException("返厂单状态已变化，请刷新后重试");
                    header.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
                    header.ReturnDate = returnDate;
                    header.SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null;
                    header.SupplierName = supplierName;
                    header.Remark = remark;
                    header.Update_by = actor.UserName;
                    header.Update_time = now;
                    var affected = Context.Updateable(header).UpdateColumns(it => new
                    {
                        it.StoreId, it.ReturnDate, it.SupplierId, it.SupplierName, it.Remark, it.Update_by, it.Update_time
                    }).Where(it => it.Id == header.Id && it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                        .ExecuteCommand();
                    if (affected != 1) throw new CustomException("返厂单状态已变化，请刷新后重试");
                    Context.Deleteable<JyFactoryReturnItem>()
                        .Where(it => it.TenantId == actor.TenantId && it.ReturnId == header.Id)
                        .ExecuteCommand();
                }
                else
                {
                    var issued = _billRule.IssueInTransaction(actor, "factory_return", "jy_factory_return", null, returnDate);
                    header = new JyFactoryReturn
                    {
                        TenantId = actor.TenantId,
                        StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                        ReturnNo = issued.BillNo,
                        ReturnDate = returnDate,
                        SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null,
                        SupplierName = supplierName,
                        Status = 0,
                        TotalQty = 0,
                        TotalCost = 0,
                        DelFlag = 0,
                        Create_by = actor.UserName,
                        Create_time = now,
                        Remark = remark
                    };
                    header.Id = Context.Insertable(header).ExecuteReturnBigIdentity();
                }
                resultId = header.Id;

                var lines = dto.Items.Select((it, index) =>
                {
                    var goods = goodsMap[it.GoodsItemId];
                    return new JyFactoryReturnItem
                    {
                        TenantId = actor.TenantId,
                        ReturnId = header.Id,
                        GoodsItemId = goods.Id,
                        GoodsNo = goods.GoodsNo,
                        Barcode = goods.Barcode,
                        GoodsName = goods.StyleName,
                        Qty = it.Qty,
                        CostPrice = goods.CostPrice,
                        Reason = NormalizeText(it.Reason, 200, "返厂原因"),
                        SortNo = index + 1,
                        Create_time = now
                    };
                }).ToList();
                Context.Insertable(lines).ExecuteCommand();
                var totalQty = lines.Sum(it => it.Qty);
                var totalCost = decimal.Round(lines.Sum(it => it.Qty * it.CostPrice), 2, MidpointRounding.AwayFromZero);
                Context.Updateable<JyFactoryReturn>()
                    .SetColumns(it => it.TotalQty == totalQty)
                    .SetColumns(it => it.TotalCost == totalCost)
                    .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "返厂单保存失败");
            return resultId;
        }

        /// <summary>确认返厂：商品状态改为 8，清空锁定，写库存流水。</summary>
        public int Confirm(JyActor actor, long id)
        {
            var tran = UseTran(() =>
            {
                var header = Context.Queryable<JyFactoryReturn>().With(SqlWith.UpdLock)
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException("返厂单不存在");
                if (header.Status != 0) throw new CustomException("返厂单已确认或已作废，不能重复确认");
                var items = Context.Queryable<JyFactoryReturnItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.ReturnId == id)
                    .OrderBy(it => it.SortNo).ToList();
                if (items.Count == 0) throw new CustomException("返厂单没有商品，不能确认");
                var now = DateTime.Now;

                foreach (var line in items)
                {
                    var goods = Context.Queryable<JyGoodsItem>().With(SqlWith.UpdLock)
                        .First(it => it.Id == line.GoodsItemId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                        ?? throw new CustomException($"商品 {line.GoodsNo} 不存在");
                    if (goods.Status != 1) throw new CustomException($"商品 {goods.GoodsNo} 不在库，不能返厂");
                    if (goods.Qty < line.Qty) throw new CustomException($"商品 {goods.GoodsNo} 库存不足");
                    var before = goods.Qty;
                    var after = before - line.Qty;
                    var newStatus = after <= 0 ? 8 : 1;
                    var affected = Context.Updateable<JyGoodsItem>()
                        .SetColumns(it => it.Qty == after)
                        .SetColumns(it => it.Status == newStatus)
                        .SetColumns(it => it.LockedQty == 0)
                        .SetColumns(it => it.LockBizType == null)
                        .SetColumns(it => it.LockBizId == null)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == goods.Id && it.TenantId == actor.TenantId && it.DelFlag == 0
                            && it.Status == 1 && it.Qty == before)
                        .ExecuteCommand();
                    if (affected != 1) throw new CustomException($"商品 {goods.GoodsNo} 状态已变化，请刷新后重试");
                    Context.Insertable(new JyStockLedger
                    {
                        TenantId = actor.TenantId,
                        StoreId = header.StoreId,
                        LedgerNo = LedgerNo(goods.GoodsNo, "FC"),
                        GoodsItemId = goods.Id,
                        GoodsNo = goods.GoodsNo,
                        ChangeType = 11,
                        ChangeQty = -line.Qty,
                        BeforeQty = before,
                        AfterQty = after,
                        BizType = "factory_return",
                        BizId = header.Id,
                        BizTime = now,
                        Create_by = actor.UserName,
                        Create_time = now,
                        Remark = header.ReturnNo
                    }).ExecuteCommand();
                }

                var updated = Context.Updateable<JyFactoryReturn>()
                    .SetColumns(it => it.Status == 1)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
                if (updated != 1) throw new CustomException("返厂单状态已变化，请刷新后重试");
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "确认返厂失败");
            return 1;
        }

        public int Delete(JyActor actor, long id)
        {
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var header = Context.Queryable<JyFactoryReturn>().With(SqlWith.UpdLock)
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException("返厂单不存在");
                if (header.Status != 0) throw new CustomException("只有草稿状态的返厂单可以删除");
                Context.Deleteable<JyFactoryReturnItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.ReturnId == header.Id).ExecuteCommand();
                var affected = Context.Updateable<JyFactoryReturn>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("返厂单状态已变化，请刷新后重试");
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "返厂单删除失败");
            return 1;
        }

        private JyFactoryReturn Must(string tenantId, long id)
        {
            return Context.Queryable<JyFactoryReturn>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("返厂单不存在");
        }

        private JyFactoryReturnDto ToDto(JyFactoryReturn it)
        {
            return new JyFactoryReturnDto
            {
                Id = it.Id,
                ReturnNo = it.ReturnNo,
                ReturnDate = it.ReturnDate,
                SupplierId = it.SupplierId,
                SupplierName = it.SupplierName,
                Status = it.Status,
                TotalQty = it.TotalQty,
                TotalCost = it.TotalCost,
                Remark = it.Remark,
                CreateBy = it.Create_by,
                CreateTime = it.Create_time
            };
        }

        private List<JyFactoryReturnItemDto> LoadItems(string tenantId, long returnId)
        {
            return Context.Queryable<JyFactoryReturnItem>()
                .Where(it => it.TenantId == tenantId && it.ReturnId == returnId)
                .OrderBy(it => it.SortNo)
                .ToList()
                .Select(it => new JyFactoryReturnItemDto
                {
                    Id = it.Id,
                    GoodsItemId = it.GoodsItemId,
                    GoodsNo = it.GoodsNo,
                    Barcode = it.Barcode,
                    GoodsName = it.GoodsName,
                    Qty = it.Qty,
                    CostPrice = it.CostPrice,
                    Reason = it.Reason,
                    SortNo = it.SortNo
                }).ToList();
        }

        private static string LedgerNo(string goodsNo, string suffix)
        {
            var value = goodsNo + "-" + suffix + "-" + DateTime.Now.ToString("HHmmssfff");
            return value.Length <= 32 ? value : value.Substring(0, 32);
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