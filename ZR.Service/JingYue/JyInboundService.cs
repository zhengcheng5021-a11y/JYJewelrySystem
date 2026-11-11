using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;
using ZR.Model.System;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyInboundService : BaseService<JyInbound>
    {
        private readonly JyBillRuleService _billRule;
        private readonly JyLookupService _lookup;

        public JyInboundService(JyBillRuleService billRule, JyLookupService lookup)
        {
            _billRule = billRule;
            _lookup = lookup;
        }

        public PagedInfo<JyInboundDto> List(JyActor actor, JyInboundQueryDto query)
        {
            query ??= new JyInboundQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            if (query.BeginTime.HasValue && query.EndTime.HasValue && query.BeginTime.Value.Date > query.EndTime.Value.Date)
                throw new CustomException("开始日期不能晚于结束日期");
            var q = Context.Queryable<JyInbound>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is 0 or 1, it => it.Status == query.Status)
                .WhereIF(query.SupplierId > 0, it => it.SupplierId == query.SupplierId)
                .WhereIF(query.BeginTime.HasValue, it => it.InboundTime >= query.BeginTime.Value)
                .WhereIF(query.EndTime.HasValue, it => it.InboundTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.InboundNo.Contains(keyword) || it.Remark.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.Status)
                .OrderBy(it => it.InboundTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            var suppliers = SupplierNames(actor.TenantId, rows.Select(it => it.SupplierId));
            return new PagedInfo<JyInboundDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(it => ToDto(it, suppliers)).ToList()
            };
        }

        public JyInboundDto Detail(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            var suppliers = SupplierNames(actor.TenantId, new[] { row.SupplierId });
            var dto = ToDto(row, suppliers);
            dto.Lines = LoadLines(actor.TenantId, new[] { row.Id });
            return dto;
        }

        public JyInboundDashboardDto Dashboard(JyActor actor)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var all = Context.Queryable<JyInbound>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ToList();
            var pendingRows = all.Where(it => it.Status == 0)
                .OrderBy(it => it.InboundTime).ThenByDescending(it => it.Id).Take(6).ToList();
            var suppliers = SupplierNames(actor.TenantId, pendingRows.Select(it => it.SupplierId));
            var trend = new List<JyInboundTrendDto>();
            for (var i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var next = day.AddDays(1);
                var items = all.Where(it => it.Status == 1 && it.InboundTime >= day && it.InboundTime < next);
                trend.Add(new JyInboundTrendDto
                {
                    Date = day.ToString("MM-dd"),
                    Qty = items.Sum(it => it.TotalQty),
                    Cost = items.Sum(it => it.TotalCost)
                });
            }
            return new JyInboundDashboardDto
            {
                DraftCount = all.Count(it => it.Status == 0),
                ConfirmedToday = all.Count(it => it.Status == 1 && it.InboundTime >= today && it.InboundTime < tomorrow),
                TodayQty = all.Where(it => it.Status == 1 && it.InboundTime >= today && it.InboundTime < tomorrow).Sum(it => it.TotalQty),
                TodayCost = all.Where(it => it.Status == 1 && it.InboundTime >= today && it.InboundTime < tomorrow).Sum(it => it.TotalCost),
                PendingSupplierCount = all.Where(it => it.Status == 0 && it.SupplierId.HasValue).Select(it => it.SupplierId).Distinct().Count(),
                Pending = pendingRows.Select(it => ToDto(it, suppliers)).ToList(),
                Trend = trend
            };
        }

        public JyInboundOptionsDto Options(JyActor actor)
        {
            _lookup.EnsureFoundation(actor);
            var suppliers = Context.Queryable<JySupplier>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.SupplierName).ToList();
            var styles = Context.Queryable<JyGoodsStyle>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.StyleCode).ToList();
            var lookupIds = styles.SelectMany(it => new[] { it.StyleItemId, it.MaterialItemId, it.MetalItemId })
                .Where(it => it.HasValue).Select(it => it.Value).Distinct().ToList();
            var lookupItems = lookupIds.Count == 0
                ? new List<JyLookupItem>()
                : Context.Queryable<JyLookupItem>()
                    .Where(it => it.TenantId == actor.TenantId && lookupIds.Contains(it.Id))
                    .ToList();
            var lookupNames = lookupItems.ToDictionary(it => it.Id, it => it.ItemName);
            var categories = _lookup.Options(actor, "style", true);
            var materials = _lookup.Options(actor, "material", true);
            var metals = _lookup.Options(actor, "metal", true);
            var certTypes = _lookup.Options(actor, "cert_type", true);
            return new JyInboundOptionsDto
            {
                Suppliers = suppliers.Select(it => new JyOptionDto
                {
                    Id = it.Id,
                    Code = it.SupplierCode,
                    Name = it.SupplierName
                }).ToList(),
                Categories = categories,
                Materials = materials,
                Metals = metals,
                CertTypes = certTypes,
                Styles = styles.Select(it => new JyOptionDto
                {
                    Id = it.Id,
                    Code = it.StyleCode,
                    Name = it.StyleName,
                    StyleItemId = it.StyleItemId,
                    MaterialItemId = it.MaterialItemId,
                    MaterialGroup = it.MaterialGroup,
                    MetalItemId = it.MetalItemId,
                    ItemMode = it.ItemModeDefault,
                    CostPrice = it.CostPrice,
                    SalePrice = it.SalePrice,
                    PackagingFee = it.PackagingFee,
                    MaterialName = Display(lookupNames, it.MaterialItemId),
                    MetalName = Display(lookupNames, it.MetalItemId)
                }).ToList()
            };
        }

        /// <summary>
        /// 保存入库单。明细可以暂时为空，便于先建档再陆续录入；编码只在新增明细时生成。
        /// </summary>
        public long Save(JyActor actor, JyInboundEditDto dto)
        {
            if (dto == null) throw new CustomException("入库单不能为空");
            dto.Lines ??= new List<JyInboundLineEditDto>();
            if (dto.Lines.Count > 100) throw new CustomException("单张入库单不能超过 100 件货品");
            if (dto.SourceType != 1) throw new CustomException("当前版本只支持采购入库");
            if (dto.InboundTime == default) dto.InboundTime = DateTime.Now;
            if (dto.InboundTime.Year < 2000 || dto.InboundTime.Year > 2100)
                throw new CustomException("入库日期不正确");
            dto.Remark = TrimRemark(dto.Remark);

            if (dto.SupplierId > 0)
            {
                var supplier = Context.Queryable<JySupplier>().First(it =>
                    it.Id == dto.SupplierId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (supplier == null) throw new CustomException("供应商不存在或已删除");
                if (supplier.Status != 0) throw new CustomException("供应商已停用，不能新增入库");
            }

            ValidateLines(actor.TenantId, dto.Lines);
            EnsureImages(actor.TenantId, dto.Lines.Select(it => it.ImageFileId));

            var duplicateIds = dto.Lines.Where(it => it.Id > 0).GroupBy(it => it.Id)
                .Where(it => it.Count() > 1).Select(it => it.Key).ToList();
            if (duplicateIds.Count > 0) throw new CustomException("同一件货品不能在一张入库单中重复提交");

            var totalQty = dto.Lines.Count;
            var totalCost = dto.Lines.Sum(it => it.CostPrice * it.Qty);
            long result = 0;

            UseTran(() =>
            {
                var now = DateTime.Now;
                JyInbound header;
                if (dto.Id > 0)
                {
                    header = Context.Queryable<JyInbound>()
                        .With(SqlWith.UpdLock)
                        .First(it => it.Id == dto.Id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                    if (header == null) throw new CustomException("入库单不存在");
                    if (header.Status != 0) throw new CustomException("已完成的入库单不能修改");
                }
                else
                {
                    var issued = _billRule.IssueInTransaction(actor, "inbound", "jy_inbound", null, dto.InboundTime);
                    header = new JyInbound
                    {
                        TenantId = actor.TenantId,
                        StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                        InboundNo = issued.BillNo,
                        SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null,
                        InboundTime = dto.InboundTime,
                        SourceType = 1,
                        Status = 0,
                        TotalQty = totalQty,
                        TotalCost = totalCost,
                        DelFlag = 0,
                        Create_by = actor.UserName,
                        Create_time = now,
                        Remark = dto.Remark
                    };
                    header.Id = Context.Insertable(header).ExecuteReturnBigIdentity();
                    Context.Updateable<JyBillIssue>()
                        .SetColumns(it => it.BizId == header.Id)
                        .Where(it => it.Id == issued.Id && it.TenantId == actor.TenantId)
                        .ExecuteCommand();
                }

                var existing = dto.Id > 0
                    ? Context.Queryable<JyInboundLine>()
                        .Where(it => it.TenantId == actor.TenantId && it.InboundId == header.Id)
                        .ToList()
                    : new List<JyInboundLine>();
                var existingById = existing.ToDictionary(it => it.Id);
                foreach (var line in dto.Lines.Where(it => it.Id > 0))
                {
                    if (!existingById.TryGetValue(line.Id, out var old) || old.DelFlag != 0)
                        throw new CustomException("入库明细已失效，请刷新后重试");
                }

                // 货品编码包含入库日期，已有编码后改日期会造成编码与摘要日期不一致。
                if (existing.Any(it => it.DelFlag == 0 && !string.IsNullOrWhiteSpace(it.GoodsNo))
                    && header.InboundTime.Date != dto.InboundTime.Date)
                {
                    throw new CustomException("本单已有货品编码，不能再修改入库日期；如需更正请新建入库单");
                }

                // 先把旧排序挪到负数区，避免唯一索引 (InboundId, SortNo) 在重排时冲突。
                if (existing.Count > 0)
                {
                    Context.Ado.ExecuteCommand(
                        "UPDATE dbo.jy_inbound_line SET SortNo = -Id WHERE TenantId = @TenantId AND InboundId = @InboundId",
                        new SugarParameter("@TenantId", actor.TenantId),
                        new SugarParameter("@InboundId", header.Id));
                }

                var keepIds = dto.Lines.Where(it => it.Id > 0).Select(it => it.Id).ToHashSet();
                var removedIds = existing.Where(it => it.DelFlag == 0 && !keepIds.Contains(it.Id))
                    .Select(it => it.Id).ToList();
                if (removedIds.Count > 0)
                {
                    Context.Updateable<JyInboundLine>()
                        .SetColumns(it => it.DelFlag == 2)
                        .Where(it => it.TenantId == actor.TenantId && it.InboundId == header.Id && removedIds.Contains(it.Id))
                        .ExecuteCommand();
                    // 草稿商品与入库明细同步失效，避免删除明细后库存中残留“入库中”货品。
                    Context.Updateable<JyGoodsItem>()
                        .SetColumns(it => it.DelFlag == 2)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                            && it.InboundId == header.Id && it.Status == 7
                            && it.InboundLineId.HasValue && removedIds.Contains(it.InboundLineId.Value))
                        .ExecuteCommand();
                }
                var insertedIdsByIndex = new Dictionary<int, long>();

                var insertedIndex = 0;
                for (var lineIndex = 0; lineIndex < dto.Lines.Count; lineIndex++)
                {
                    var line = dto.Lines[lineIndex];
                    var imageFileId = line.ImageFileId > 0 ? line.ImageFileId : null;
                    if (line.Id > 0)
                    {
                        Context.Updateable<JyInboundLine>()
                            .SetColumns(it => it.StyleId == 0)
                            .SetColumns(it => it.ItemMode == 1)
                            .SetColumns(it => it.Qty == line.Qty)
                            .SetColumns(it => it.Weight == line.Weight)
                            .SetColumns(it => it.Unit == line.Unit)
                            .SetColumns(it => it.StyleName == line.GoodsName)
                            .SetColumns(it => it.StyleItemId == line.StyleItemId)
                            .SetColumns(it => it.StyleItemName == line.StyleItemName)
                            .SetColumns(it => it.MaterialItemId == line.MaterialItemId)
                            .SetColumns(it => it.MaterialGroup == line.MaterialGroup)
                            .SetColumns(it => it.MaterialName == line.MaterialName)
                            .SetColumns(it => it.MetalItemId == line.MetalItemId)
                            .SetColumns(it => it.MetalName == line.MetalName)
                            .SetColumns(it => it.CertTypeId == line.CertTypeId)
                            .SetColumns(it => it.CertTypeName == line.CertTypeName)
                            .SetColumns(it => it.CostPrice == line.CostPrice)
                            .SetColumns(it => it.SalePrice == line.SalePrice)
                            .SetColumns(it => it.PricingMode == line.PricingMode)
                            .SetColumns(it => it.LaborFee == line.LaborFee)
                            .SetColumns(it => it.PackagingFee == line.PackagingFee)
                            .SetColumns(it => it.LaborFeeMode == line.LaborFeeMode)
                            .SetColumns(it => it.ImageFileId == imageFileId)
                            .SetColumns(it => it.Remark == line.Remark)
                            .Where(it => it.Id == line.Id && it.TenantId == actor.TenantId && it.InboundId == header.Id)
                            .ExecuteCommand();
                        SyncDraftGoods(actor, header, line.Id, imageFileId, now);
                    }
                    else
                    {
                        var row = new JyInboundLine
                        {
                            TenantId = actor.TenantId,
                            InboundId = header.Id,
                            SortNo = 1000000 + insertedIndex,
                            StyleId = 0,
                            ItemMode = 1,
                            Qty = line.Qty,
                            Weight = line.Weight,
                            Unit = line.Unit,
                            CostPrice = line.CostPrice,
                            SalePrice = line.SalePrice,
                            PricingMode = line.PricingMode,
                            LaborFee = line.LaborFee,
                            PackagingFee = line.PackagingFee,
                            LaborFeeMode = line.LaborFeeMode,
                            StyleName = line.GoodsName,
                            StyleItemId = line.StyleItemId,
                            StyleItemName = line.StyleItemName,
                            MaterialItemId = line.MaterialItemId,
                            MaterialGroup = line.MaterialGroup,
                            MaterialName = line.MaterialName,
                            MetalItemId = line.MetalItemId,
                            MetalName = line.MetalName,
                            CertTypeId = line.CertTypeId,
                            CertTypeName = line.CertTypeName,
                            ImageFileId = imageFileId,
                            Remark = line.Remark,
                            DelFlag = 0,
                            Create_time = now
                        };
                        row.Id = Context.Insertable(row).ExecuteReturnBigIdentity();
                        insertedIndex++;
                        insertedIdsByIndex[lineIndex] = row.Id;
                        var issued = _billRule.IssueInTransaction(actor, "goods", "jy_inbound_line", row.Id, dto.InboundTime);
                        row.GoodsNo = issued.BillNo;
                        Context.Updateable<JyInboundLine>()
                            .SetColumns(it => it.GoodsNo == issued.BillNo)
                            .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId)
                            .ExecuteCommand();
                        CreateDraftGoods(actor, header, row, imageFileId, now);
                    }
                }

                // 按提交时的原顺序统一排序，兼容已有明细和新录入明细交错的情况。
                var orderedIds = dto.Lines.Select((line, index) =>
                    line.Id > 0 ? line.Id : insertedIdsByIndex[index]).ToList();
                var sortNo = 1;
                foreach (var lineId in orderedIds)
                {
                    Context.Updateable<JyInboundLine>()
                        .SetColumns(it => it.SortNo == sortNo)
                        .Where(it => it.Id == lineId && it.TenantId == actor.TenantId && it.InboundId == header.Id)
                        .ExecuteCommand();
                    sortNo++;
                }

                header.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
                header.SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null;
                header.InboundTime = dto.InboundTime;
                header.TotalQty = totalQty;
                header.TotalCost = totalCost;
                header.Remark = dto.Remark;
                header.Update_by = actor.UserName;
                header.Update_time = now;
                Context.Updateable(header)
                    .UpdateColumns(it => new { it.StoreId, it.SupplierId, it.InboundTime, it.TotalQty, it.TotalCost, it.Remark, it.Update_by, it.Update_time })
                    .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
                result = header.Id;
            });
            return result;
        }

        public int Delete(JyActor actor, long id)
        {
            var current = Must(actor.TenantId, id);
            if (current.Status != 0) throw new CustomException("只有草稿入库单可以删除");
            var affected = 0;
            UseTran(() =>
            {
                var now = DateTime.Now;
                // 明细不做物理删除，保留已经分配的货品编码和追溯线索。
                Context.Updateable<JyInboundLine>()
                    .SetColumns(it => it.DelFlag == 2)
                    .Where(it => it.TenantId == actor.TenantId && it.InboundId == id && it.DelFlag == 0)
                    .ExecuteCommand();
                Context.Updateable<JyGoodsItem>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.TenantId == actor.TenantId && it.InboundId == id && it.Status == 7 && it.DelFlag == 0)
                    .ExecuteCommand();
                affected = Context.Updateable<JyInbound>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
            });
            return affected;
        }

        /// <summary>
        /// 完成入库：只复制本单有效明细，不重新生成货品编码。
        /// </summary>
        public int Confirm(JyActor actor, long id)
        {
            UseTran(() =>
            {
                var now = DateTime.Now;
                var header = Context.Queryable<JyInbound>()
                    .With(SqlWith.UpdLock)
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (header == null) throw new CustomException("入库单不存在");
                if (header.Status != 0) throw new CustomException("入库单已完成，不能重复操作");

                var lines = Context.Queryable<JyInboundLine>()
                    .Where(it => it.TenantId == actor.TenantId && it.InboundId == id && it.DelFlag == 0)
                    .OrderBy(it => it.SortNo).ToList();
                if (lines.Count == 0) throw new CustomException("请至少录入一件货品后再完成入库");
                if (lines.Any(it => string.IsNullOrWhiteSpace(it.GoodsNo)))
                    throw new CustomException("存在未生成货品编码的明细，请先保存草稿");

                var lineIds = lines.Select(it => it.Id).ToList();
                var goodsNos = lines.Select(it => it.GoodsNo).ToList();
                var existingItems = Context.Queryable<JyGoodsItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && it.InboundLineId.HasValue && lineIds.Contains(it.InboundLineId.Value))
                    .ToList();
                if (existingItems.Any(it => it.Status != 7))
                    throw new CustomException("本单已有货品完成入库，不能重复操作");
                if (Context.Queryable<JyGoodsItem>().Any(it =>
                    it.TenantId == actor.TenantId && it.DelFlag == 0 && goodsNos.Contains(it.GoodsNo)
                    // SqlSugar 无法正确翻译 !Nullable.HasValue，会生成 NOT(...) NOT (...) 的非法 SQL，改用 == null 判断。
                    && (it.InboundLineId == null || !lineIds.Contains(it.InboundLineId.Value))))
                    throw new CustomException("货品编码已被商品档案占用，请联系管理员检查");

                var existingByLine = existingItems
                    .Where(it => it.InboundLineId.HasValue)
                    .GroupBy(it => it.InboundLineId.Value)
                    .ToDictionary(it => it.Key, it => it.OrderByDescending(x => x.Id).First());
                foreach (var line in lines)
                {
                    JyGoodsItem item;
                    if (existingByLine.TryGetValue(line.Id, out var draft))
                    {
                        // 草稿商品在保存入库单时已生成，确认时原位转为在库，保证条码与商品身份唯一。
                        Context.Updateable<JyGoodsItem>()
                            .SetColumns(it => it.StoreId == header.StoreId)
                            .SetColumns(it => it.SupplierId == header.SupplierId)
                            .SetColumns(it => it.Qty == line.Qty)
                            .SetColumns(it => it.LockedQty == 0)
                            .SetColumns(it => it.Status == 1)
                            .SetColumns(it => it.Weight == line.Weight)
                            .SetColumns(it => it.Unit == line.Unit)
                            .SetColumns(it => it.CostPrice == line.CostPrice)
                            .SetColumns(it => it.SalePrice == line.SalePrice)
                            .SetColumns(it => it.PricingMode == line.PricingMode)
                            .SetColumns(it => it.LaborFee == line.LaborFee)
                            .SetColumns(it => it.PackagingFee == line.PackagingFee)
                            .SetColumns(it => it.LaborFeeMode == line.LaborFeeMode)
                            .SetColumns(it => it.StyleName == line.StyleName)
                            .SetColumns(it => it.StyleItemId == line.StyleItemId)
                            .SetColumns(it => it.StyleItemName == line.StyleItemName)
                            .SetColumns(it => it.MaterialItemId == line.MaterialItemId)
                            .SetColumns(it => it.MaterialGroup == line.MaterialGroup)
                            .SetColumns(it => it.MaterialName == line.MaterialName)
                            .SetColumns(it => it.MetalItemId == line.MetalItemId)
                            .SetColumns(it => it.MetalName == line.MetalName)
                            .SetColumns(it => it.CertTypeId == line.CertTypeId)
                            .SetColumns(it => it.CertTypeName == line.CertTypeName)
                            .SetColumns(it => it.ImageFileId == line.ImageFileId)
                            .SetColumns(it => it.Remark == line.Remark)
                            .SetColumns(it => it.Update_by == actor.UserName)
                            .SetColumns(it => it.Update_time == now)
                            .Where(it => it.Id == draft.Id && it.TenantId == actor.TenantId && it.Status == 7 && it.DelFlag == 0)
                            .ExecuteCommand();
                        item = draft;
                        item.GoodsNo = line.GoodsNo;
                    }
                    else
                    {
                        // 兼容历史上未生成草稿商品的入库单。
                        item = new JyGoodsItem
                        {
                            TenantId = actor.TenantId,
                            StoreId = header.StoreId,
                            GoodsNo = line.GoodsNo,
                            Barcode = line.GoodsNo,
                            StyleId = 0,
                            ItemMode = 1,
                            Qty = line.Qty,
                            LockedQty = 0,
                            Weight = line.Weight,
                            Unit = line.Unit,
                            CostPrice = line.CostPrice,
                            SalePrice = line.SalePrice,
                            PricingMode = line.PricingMode,
                            LaborFee = line.LaborFee,
                            PackagingFee = line.PackagingFee,
                            LaborFeeMode = line.LaborFeeMode,
                            SupplierId = header.SupplierId,
                            StyleName = line.StyleName,
                            StyleItemId = line.StyleItemId,
                            StyleItemName = line.StyleItemName,
                            MaterialItemId = line.MaterialItemId,
                            MaterialGroup = line.MaterialGroup,
                            MaterialName = line.MaterialName,
                            MetalItemId = line.MetalItemId,
                            MetalName = line.MetalName,
                            CertTypeId = line.CertTypeId,
                            CertTypeName = line.CertTypeName,
                            Status = 1,
                            InboundId = header.Id,
                            InboundLineId = line.Id,
                            ImageFileId = line.ImageFileId,
                            DelFlag = 0,
                            Create_by = actor.UserName,
                            Create_time = now,
                            Remark = line.Remark
                        };
                        item.Id = Context.Insertable(item).ExecuteReturnBigIdentity();
                    }

                    Context.Insertable(new JyStockLedger
                    {
                        TenantId = actor.TenantId,
                        StoreId = header.StoreId,
                        LedgerNo = LedgerNo(item.GoodsNo, "IN"),
                        GoodsItemId = item.Id,
                        GoodsNo = item.GoodsNo,
                        ChangeType = 1,
                        ChangeQty = line.Qty,
                        BeforeQty = 0,
                        AfterQty = line.Qty,
                        BizType = "inbound",
                        BizId = header.Id,
                        BizTime = header.InboundTime,
                        Create_by = actor.UserName,
                        Create_time = now,
                        Remark = header.InboundNo
                    }).ExecuteCommand();
                }
                var affected = Context.Updateable<JyInbound>()
                    .SetColumns(it => it.Status == 1)
                    .SetColumns(it => it.TotalQty == lines.Count)
                    .SetColumns(it => it.TotalCost == lines.Sum(x => x.CostPrice * x.Qty))
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("入库单状态已变化，请刷新后重试");
            });
            return 1;
        }

        /// <summary>入库单保存草稿时生成商品身份证：状态 7（入库中），数量 0。</summary>
        private void CreateDraftGoods(JyActor actor, JyInbound header, JyInboundLine line, long? imageFileId, DateTime now)
        {
            var existing = Context.Queryable<JyGoodsItem>()
                .First(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.InboundLineId == line.Id);
            if (existing != null)
            {
                UpdateDraftGoods(actor, existing, header, line, imageFileId, now);
                return;
            }
            var item = new JyGoodsItem
            {
                TenantId = actor.TenantId,
                StoreId = header.StoreId,
                GoodsNo = line.GoodsNo,
                Barcode = line.GoodsNo,
                StyleId = 0,
                ItemMode = 1,
                Qty = 0,
                LockedQty = 0,
                Weight = line.Weight,
                Unit = line.Unit,
                CostPrice = line.CostPrice,
                SalePrice = line.SalePrice,
                PricingMode = line.PricingMode,
                LaborFee = line.LaborFee,
                PackagingFee = line.PackagingFee,
                LaborFeeMode = line.LaborFeeMode,
                SupplierId = header.SupplierId,
                StyleName = line.StyleName,
                StyleItemId = line.StyleItemId,
                StyleItemName = line.StyleItemName,
                MaterialItemId = line.MaterialItemId,
                MaterialGroup = line.MaterialGroup,
                MaterialName = line.MaterialName,
                MetalItemId = line.MetalItemId,
                MetalName = line.MetalName,
                CertTypeId = line.CertTypeId,
                CertTypeName = line.CertTypeName,
                Status = 7,
                InboundId = header.Id,
                InboundLineId = line.Id,
                ImageFileId = imageFileId,
                DelFlag = 0,
                Create_by = actor.UserName,
                Create_time = now,
                Remark = line.Remark
            };
            Context.Insertable(item).ExecuteCommand();
        }

        private void SyncDraftGoods(JyActor actor, JyInbound header, long lineId, long? imageFileId, DateTime now)
        {
            var line = Context.Queryable<JyInboundLine>()
                .First(it => it.Id == lineId && it.TenantId == actor.TenantId && it.InboundId == header.Id && it.DelFlag == 0);
            if (line == null || string.IsNullOrWhiteSpace(line.GoodsNo)) return;
            var item = Context.Queryable<JyGoodsItem>()
                .First(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.InboundLineId == lineId);
            if (item == null)
            {
                CreateDraftGoods(actor, header, line, imageFileId, now);
                return;
            }
            if (item.Status != 7)
                throw new CustomException("商品已进入后续业务，不能修改入库草稿：" + item.GoodsNo);
            UpdateDraftGoods(actor, item, header, line, imageFileId, now);
        }

        private void UpdateDraftGoods(JyActor actor, JyGoodsItem item, JyInbound header, JyInboundLine line, long? imageFileId, DateTime now)
        {
            Context.Updateable<JyGoodsItem>()
                .SetColumns(it => it.StoreId == header.StoreId)
                .SetColumns(it => it.SupplierId == header.SupplierId)
                .SetColumns(it => it.Qty == 0)
                .SetColumns(it => it.LockedQty == 0)
                .SetColumns(it => it.Status == 7)
                .SetColumns(it => it.Weight == line.Weight)
                .SetColumns(it => it.Unit == line.Unit)
                .SetColumns(it => it.CostPrice == line.CostPrice)
                .SetColumns(it => it.SalePrice == line.SalePrice)
                .SetColumns(it => it.PricingMode == line.PricingMode)
                .SetColumns(it => it.LaborFee == line.LaborFee)
                .SetColumns(it => it.PackagingFee == line.PackagingFee)
                .SetColumns(it => it.LaborFeeMode == line.LaborFeeMode)
                .SetColumns(it => it.StyleName == line.StyleName)
                .SetColumns(it => it.StyleItemId == line.StyleItemId)
                .SetColumns(it => it.StyleItemName == line.StyleItemName)
                .SetColumns(it => it.MaterialItemId == line.MaterialItemId)
                .SetColumns(it => it.MaterialGroup == line.MaterialGroup)
                .SetColumns(it => it.MaterialName == line.MaterialName)
                .SetColumns(it => it.MetalItemId == line.MetalItemId)
                .SetColumns(it => it.MetalName == line.MetalName)
                .SetColumns(it => it.CertTypeId == line.CertTypeId)
                .SetColumns(it => it.CertTypeName == line.CertTypeName)
                .SetColumns(it => it.ImageFileId == imageFileId)
                .SetColumns(it => it.Remark == line.Remark)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == item.Id && it.TenantId == actor.TenantId && it.Status == 7 && it.DelFlag == 0)
                .ExecuteCommand();
        }
        private void ValidateLines(string tenantId, List<JyInboundLineEditDto> lines)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                line.GoodsName = JyBillNo.NormalizeName(line.GoodsName, $"第 {i + 1} 件货品名称", 100);
                line.Qty = decimal.Round(line.Qty, 2, MidpointRounding.AwayFromZero);
                if (line.Qty <= 0 || line.Qty > 100000m)
                    throw new CustomException($"第 {i + 1} 件货品内含数量必须大于 0 且不能过大");
                if (line.CostPrice < 0 || line.SalePrice < 0)
                    throw new CustomException($"第 {i + 1} 件货品价格不能为负数");
                if (line.PricingMode is < 1 or > 2)
                    throw new CustomException($"第 {i + 1} 件货品计价方式不正确");
                if (line.LaborFee < 0)
                    throw new CustomException($"第 {i + 1} 件货品工费不能为负数");
                if (line.PackagingFee < 0)
                    throw new CustomException($"第 {i + 1} 件货品包装费不能为负数");
                line.PackagingFee = decimal.Round(line.PackagingFee, 2, MidpointRounding.AwayFromZero);
                if (line.PricingMode == 2) line.LaborFeeMode = 2;
                if (line.LaborFeeMode is < 1 or > 2)
                    throw new CustomException($"第 {i + 1} 件货品工费方式不正确");
                if (line.PricingMode == 2 && (!line.Weight.HasValue || line.Weight.Value <= 0))
                    throw new CustomException($"第 {i + 1} 件按克计价货品必须填写重量");
                line.Unit = NormalizeUnit(line.Unit, $"第 {i + 1} 件货品计量单位");
                if (line.Weight.HasValue && (line.Weight < 0 || line.Weight > 100000m))
                    throw new CustomException($"第 {i + 1} 件货品银重不正确");
                if (line.Weight.HasValue) line.Weight = decimal.Round(line.Weight.Value, 2, MidpointRounding.AwayFromZero);
                line.Remark = NormalizeText(line.Remark, 500, $"第 {i + 1} 件货品备注");
                if (line.ImageFileId <= 0) line.ImageFileId = null;
            }
            ApplyAttributes(tenantId, lines);
        }

        /// <summary>批量校验并回填款式、材质、金属成色、证书类型，名称快照以字典为准。</summary>
        private void ApplyAttributes(string tenantId, List<JyInboundLineEditDto> lines)
        {
            var typeCodes = new[] { "style", "material", "metal", "cert_type" };
            var types = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && typeCodes.Contains(it.TypeCode))
                .ToList();
            var typeIds = types.ToDictionary(it => it.TypeCode, it => it.Id);
            var ids = lines.SelectMany(it => new[] { it.StyleItemId, it.MaterialItemId, it.MetalItemId, it.CertTypeId })
                .Where(it => it.HasValue && it.Value > 0).Select(it => it.Value).Distinct().ToList();
            var items = ids.Count == 0
                ? new List<JyLookupItem>()
                : Context.Queryable<JyLookupItem>()
                    .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && ids.Contains(it.Id))
                    .ToList();
            var map = items.ToDictionary(it => it.Id);
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                line.StyleItemName = ResolveAttribute(map, typeIds, line.StyleItemId, "style", $"第 {i + 1} 件货品款式分类", true);
                line.MaterialName = ResolveAttribute(map, typeIds, line.MaterialItemId, "material", $"第 {i + 1} 件货品材质", false);
                if (line.MaterialGroup is < 1 or > 3) throw new CustomException($"第 {i + 1} 件货品材质大类不正确");
                line.MetalName = ResolveAttribute(map, typeIds, line.MetalItemId, "metal", $"第 {i + 1} 件货品金属成色", false);
                line.CertTypeName = ResolveAttribute(map, typeIds, line.CertTypeId, "cert_type", $"第 {i + 1} 件货品证书类型", false);
            }
        }

        private static string ResolveAttribute(Dictionary<long, JyLookupItem> items, Dictionary<string, long> typeIds,
            long? id, string typeCode, string label, bool requireSecondLevel)
        {
            if (!id.HasValue || id.Value <= 0) return null;
            if (!typeIds.TryGetValue(typeCode, out var typeId)
                || !items.TryGetValue(id.Value, out var item)
                || item.TypeId != typeId)
                throw new CustomException($"{label}不存在或不属于当前字典");
            if (item.Status != 0) throw new CustomException($"{label}已停用");
            if (requireSecondLevel && !item.ParentId.HasValue) throw new CustomException($"{label}请选择二级分类");
            return item.ItemName;
        }

        private void EnsureImages(string tenantId, IEnumerable<long?> ids)
        {
            var values = ids.Where(it => it.HasValue && it.Value > 0).Select(it => it.Value).Distinct().ToList();
            if (values.Count == 0) return;
            var found = Context.Queryable<SysFile>()
                .Where(it => values.Contains(it.Id) && (it.TenantId == tenantId || it.TenantId == null))
                .Count();
            if (found != values.Count) throw new CustomException("货品图片不存在或无权使用，请重新上传");
        }

        private Dictionary<long, string> SupplierNames(string tenantId, IEnumerable<long?> ids)
        {
            var values = ids.Where(it => it.HasValue).Select(it => it.Value).Distinct().ToList();
            return values.Count == 0
                ? new Dictionary<long, string>()
                : Context.Queryable<JySupplier>()
                    .Where(it => it.TenantId == tenantId && values.Contains(it.Id))
                    .ToList().ToDictionary(it => it.Id, it => it.SupplierName);
        }

        private List<JyInboundLineDto> LoadLines(string tenantId, IEnumerable<long> inboundIds)
        {
            var ids = inboundIds.Distinct().ToList();
            var rows = Context.Queryable<JyInboundLine>()
                .Where(it => it.TenantId == tenantId && ids.Contains(it.InboundId) && it.DelFlag == 0)
                .OrderBy(it => it.SortNo).ToList();
            var images = ImageUrls(tenantId, rows.Select(it => it.ImageFileId));
            return rows.Select(it => new JyInboundLineDto
            {
                Id = it.Id,
                SortNo = it.SortNo,
                GoodsNo = it.GoodsNo,
                GoodsName = it.StyleName,
                StyleName = it.StyleName,
                StyleItemId = it.StyleItemId,
                StyleItemName = it.StyleItemName,
                MaterialItemId = it.MaterialItemId,
                MaterialGroup = it.MaterialGroup,
                MaterialName = it.MaterialName,
                MetalItemId = it.MetalItemId,
                MetalName = it.MetalName,
                CertTypeId = it.CertTypeId,
                CertTypeName = it.CertTypeName,
                ItemMode = 1,
                Qty = it.Qty,
                Weight = it.Weight,
                Unit = it.Unit,
                CostPrice = it.CostPrice,
                SalePrice = it.SalePrice,
                PricingMode = it.PricingMode,
                LaborFee = it.LaborFee,
                PackagingFee = it.PackagingFee,
                LaborFeeMode = it.LaborFeeMode,
                LineCost = decimal.Round(it.CostPrice * it.Qty, 2, MidpointRounding.AwayFromZero),
                ImageFileId = it.ImageFileId,
                ImageUrl = ImageUrl(images, it.ImageFileId),
                Remark = it.Remark
            }).ToList();
        }

        private Dictionary<long, string> ImageUrls(string tenantId, IEnumerable<long?> fileIds)
        {
            var values = fileIds.Where(it => it.HasValue).Select(it => it.Value).Distinct().ToList();
            return values.Count == 0
                ? new Dictionary<long, string>()
                : Context.Queryable<SysFile>()
                    .Where(it => values.Contains(it.Id) && (it.TenantId == tenantId || it.TenantId == null))
                    .ToList().ToDictionary(it => it.Id, it => it.AccessUrl);
        }

        private static string ImageUrl(Dictionary<long, string> images, long? id)
            => id.HasValue && images.TryGetValue(id.Value, out var url) ? url : null;

        private static JyInboundDto ToDto(JyInbound it, Dictionary<long, string> suppliers)
        {
            return new JyInboundDto
            {
                Id = it.Id,
                StoreId = it.StoreId,
                InboundNo = it.InboundNo,
                SupplierId = it.SupplierId,
                SupplierName = it.SupplierId.HasValue && suppliers.TryGetValue(it.SupplierId.Value, out var name) ? name : null,
                InboundTime = it.InboundTime,
                PeriodText = it.InboundTime.ToString("yyyy-MM"),
                SourceType = it.SourceType,
                Status = it.Status,
                TotalQty = it.TotalQty,
                TotalCost = it.TotalCost,
                Remark = it.Remark,
                CreateBy = it.Create_by,
                CreateTime = it.Create_time
            };
        }

        private JyInbound Must(string tenantId, long id)
        {
            return Context.Queryable<JyInbound>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("入库单不存在");
        }

        private static string LedgerNo(string goodsNo, string suffix)
        {
            var value = goodsNo + "-" + suffix;
            return value.Length <= 32 ? value : value.Substring(0, 32);
        }

        private static string Display(Dictionary<long, string> names, long? id)
            => id.HasValue && names.TryGetValue(id.Value, out var name) ? name : null;

        private static string TrimRemark(string remark) => NormalizeText(remark, 500, "备注");

        private static readonly HashSet<string> AllowedUnits = new(StringComparer.Ordinal)
        {
            "条", "颗", "对", "件", "套"
        };

        private static string NormalizeUnit(string value, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return "件";
            if (!AllowedUnits.Contains(value)) throw new CustomException(label + "只能选择条、颗、对、件或套");
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


