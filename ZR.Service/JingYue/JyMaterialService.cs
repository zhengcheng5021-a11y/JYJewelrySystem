using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyMaterialService : BaseService<JyMaterial>
    {
        private static readonly HashSet<string> InboundBizTypes = new(StringComparer.Ordinal)
        { "purchase", "return", "old_material", "other_in" };
        private static readonly HashSet<string> OutboundBizTypes = new(StringComparer.Ordinal)
        { "consume", "loss", "transfer", "other_out" };

        public PagedInfo<JyMaterialDto> List(JyActor actor, JyMaterialQueryDto query)
        {
            query ??= new JyMaterialQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JyMaterial>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is 0 or 1, it => it.Status == query.Status)
                .WhereIF(query.CategoryItemId > 0, it => it.CategoryItemId == query.CategoryItemId)
                .WhereIF(query.SupplierId > 0, it => it.SupplierId == query.SupplierId)
                .WhereIF(query.MetalItemId > 0, it => it.MetalItemId == query.MetalItemId)
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.MaterialCode.Contains(keyword)
                    || it.MaterialName.Contains(keyword) || it.Remark.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.Status).OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JyMaterialDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDtos(actor.TenantId, rows)
            };
        }

        public JyMaterialDto Detail(JyActor actor, long id)
        {
            return ToDtos(actor.TenantId, new List<JyMaterial> { Must(actor.TenantId, id) }).First();
        }

        public JyMaterialSummaryDto Summary(JyActor actor)
        {
            var rows = Context.Queryable<JyMaterial>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0).ToList();
            var active = rows.Where(it => it.Status == 0).ToList();
            return new JyMaterialSummaryDto
            {
                MaterialCount = active.Count,
                TotalStockWeight = active.Sum(it => it.StockWeight),
                TotalStockCost = active.Sum(it => it.StockWeight * it.AvgCostPerGram),
                LowStockCount = active.Count(it => it.StockWeight <= 0),
                Recent = ToDtos(actor.TenantId, rows.OrderByDescending(it => it.Id).Take(6).ToList())
            };
        }

        public JyMaterialOptionsDto Options(JyActor actor, JyLookupService lookup)
        {
            lookup.EnsureFoundation(actor);
            var suppliers = Context.Queryable<JySupplier>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.SupplierName).ToList();
            return new JyMaterialOptionsDto
            {
                Categories = lookup.Options(actor, "material_category", true),
                Metals = lookup.Options(actor, "metal", true),
                Suppliers = suppliers.Select(it => new JyOptionDto
                {
                    Id = it.Id, Code = it.SupplierCode, Name = it.SupplierName
                }).ToList()
            };
        }

        public long Save(JyActor actor, JyMaterialEditDto dto)
        {
            if (dto == null) throw new CustomException("物料不能为空");
            dto.MaterialCode = JyBillNo.NormalizeCode(dto.MaterialCode, "物料编码");
            dto.MaterialName = JyBillNo.NormalizeName(dto.MaterialName, "物料名称", 100);
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.Status != 0 && dto.Status != 1) throw new CustomException("状态不正确");
            if (dto.SalePricePerGram < 0) throw new CustomException("销售价/克不能为负数");
            if (dto.SalePricePerGram > 1000000m) throw new CustomException("销售价/克明显异常");
            ValidateReferences(actor.TenantId, dto.CategoryItemId, dto.MetalItemId, dto.SupplierId);
            EnsureCodeFree(actor.TenantId, dto.MaterialCode, dto.Id);

            if (dto.Id <= 0)
            {
                return Context.Insertable(new JyMaterial
                {
                    TenantId = actor.TenantId,
                    StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                    MaterialCode = dto.MaterialCode,
                    MaterialName = dto.MaterialName,
                    CategoryItemId = dto.CategoryItemId > 0 ? dto.CategoryItemId : null,
                    MetalItemId = dto.MetalItemId > 0 ? dto.MetalItemId : null,
                    SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null,
                    StockWeight = 0, LockedWeight = 0, AvgCostPerGram = 0,
                    SalePricePerGram = dto.SalePricePerGram,
                    Status = dto.Status, DelFlag = 0,
                    Create_by = actor.UserName, Create_time = DateTime.Now, Remark = dto.Remark
                }).ExecuteReturnBigIdentity();
            }

            var current = Must(actor.TenantId, dto.Id);
            current.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
            current.MaterialCode = dto.MaterialCode;
            current.MaterialName = dto.MaterialName;
            current.CategoryItemId = dto.CategoryItemId > 0 ? dto.CategoryItemId : null;
            current.MetalItemId = dto.MetalItemId > 0 ? dto.MetalItemId : null;
            current.SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null;
            current.SalePricePerGram = dto.SalePricePerGram;
            current.Status = dto.Status;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current).UpdateColumns(it => new
            {
                it.StoreId, it.MaterialCode, it.MaterialName, it.CategoryItemId, it.MetalItemId,
                it.SupplierId, it.SalePricePerGram, it.Status, it.Remark, it.Update_by, it.Update_time
            }).ExecuteCommand();
            return current.Id;
        }

        public int Delete(JyActor actor, long id)
        {
            var current = Must(actor.TenantId, id);
            if (Math.Abs(current.StockWeight) > 0.00005m)
                throw new CustomException("物料仍有库存，不能删除，请先出库或停用");
            var used = Context.Queryable<JyMaterialDocLine>()
                .Any(it => it.TenantId == actor.TenantId && it.MaterialId == id && it.DelFlag == 0);
            var ledger = Context.Queryable<JyMaterialLedger>()
                .Any(it => it.TenantId == actor.TenantId && it.MaterialId == id);
            if (used || ledger) throw new CustomException("物料已有单据或流水，只能停用，不能删除");
            return Context.Updateable<JyMaterial>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        public PagedInfo<JyMaterialDocDto> ListDocs(JyActor actor, JyMaterialDocQueryDto query)
        {
            query ??= new JyMaterialDocQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            if (query.BeginTime.HasValue && query.EndTime.HasValue && query.BeginTime.Value.Date > query.EndTime.Value.Date)
                throw new CustomException("开始日期不能晚于结束日期");
            var q = Context.Queryable<JyMaterialDoc>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Direction is 1 or 2, it => it.Direction == query.Direction)
                .WhereIF(query.Status is 0 or 1, it => it.Status == query.Status)
                .WhereIF(query.SupplierId > 0, it => it.SupplierId == query.SupplierId)
                .WhereIF(query.BeginTime.HasValue, it => it.DocTime >= query.BeginTime.Value)
                .WhereIF(query.EndTime.HasValue, it => it.DocTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.DocNo.Contains(keyword) || it.Remark.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.Status).OrderBy(it => it.DocTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc).ToPageList(page, size, ref total);
            var suppliers = SupplierNames(actor.TenantId, rows.Select(it => it.SupplierId));
            return new PagedInfo<JyMaterialDocDto>
            {
                PageIndex = page, PageSize = size, TotalNum = total,
                Result = rows.Select(it => ToDto(it, suppliers)).ToList()
            };
        }

        public JyMaterialDocDto DetailDoc(JyActor actor, long id)
        {
            var row = MustDoc(actor.TenantId, id);
            var dto = ToDto(row, SupplierNames(actor.TenantId, new[] { row.SupplierId }));
            dto.Lines = LoadLines(actor.TenantId, row.Id);
            return dto;
        }

        public JyMaterialDocDashboardDto Dashboard(JyActor actor)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var rows = Context.Queryable<JyMaterialDoc>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0).ToList();
            var pending = rows.Where(it => it.Status == 0).OrderByDescending(it => it.Id).Take(6).ToList();
            var suppliers = SupplierNames(actor.TenantId, pending.Select(it => it.SupplierId));
            return new JyMaterialDocDashboardDto
            {
                DraftCount = rows.Count(it => it.Status == 0),
                ConfirmedToday = rows.Count(it => it.Status == 1 && it.DocTime >= today && it.DocTime < tomorrow),
                TodayInWeight = rows.Where(it => it.Status == 1 && it.Direction == 1 && it.DocTime >= today && it.DocTime < tomorrow).Sum(it => it.TotalWeight),
                TodayOutWeight = rows.Where(it => it.Status == 1 && it.Direction == 2 && it.DocTime >= today && it.DocTime < tomorrow).Sum(it => it.TotalWeight),
                Pending = pending.Select(it => ToDto(it, suppliers)).ToList()
            };
        }

        public long SaveDoc(JyActor actor, JyMaterialDocEditDto dto, JyBillRuleService billRule)
        {
            if (dto == null) throw new CustomException("物料单据不能为空");
            if (dto.Direction is not (1 or 2)) throw new CustomException("单据方向不正确");
            dto.BizType = NormalizeBizType(dto.Direction, dto.BizType);
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.DocTime == default) dto.DocTime = DateTime.Now;
            if (dto.Lines == null || dto.Lines.Count == 0) throw new CustomException("请至少录入一条物料明细");
            if (dto.Lines.Count > 100) throw new CustomException("单张物料单最多录入 100 条明细");
            if (dto.Direction == 1 && dto.SupplierId is not > 0)
                throw new CustomException("物料入库必须选择供应商");
            ValidateSupplier(actor.TenantId, dto.SupplierId);
            EnsureLines(actor.TenantId, dto.Lines);

            var totalWeight = dto.Lines.Sum(it => it.Weight);
            var totalAmount = dto.Lines.Sum(it => decimal.Round(it.UnitCost * it.Weight, 2, MidpointRounding.AwayFromZero));
            var result = 0L;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                JyMaterialDoc header;
                if (dto.Id > 0)
                {
                    header = Context.Queryable<JyMaterialDoc>().With(SqlWith.UpdLock)
                        .First(it => it.Id == dto.Id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                    if (header == null) throw new CustomException("物料单据不存在");
                    if (header.Status != 0) throw new CustomException("已确认的物料单据不能修改");
                    if (header.Direction != dto.Direction) throw new CustomException("已保存的单据不能改变入库/出库方向");
                    header.StoreId = dto.StoreId > 0 ? dto.StoreId : header.StoreId;
                    header.BizType = dto.BizType;
                    header.SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null;
                    header.DocTime = dto.DocTime;
                    header.TotalWeight = totalWeight;
                    header.TotalAmount = totalAmount;
                    header.Remark = dto.Remark;
                    header.Update_by = actor.UserName;
                    header.Update_time = now;
                    Context.Updateable(header).UpdateColumns(it => new
                    {
                        it.StoreId, it.BizType, it.SupplierId, it.DocTime,
                        it.TotalWeight, it.TotalAmount, it.Remark, it.Update_by, it.Update_time
                    }).ExecuteCommand();
                }
                else
                {
                    var billType = dto.Direction == 1 ? "material_inbound" : "material_outbound";
                    var issued = billRule.IssueInTransaction(actor, billType, "jy_material_doc", null, dto.DocTime);
                    header = new JyMaterialDoc
                    {
                        TenantId = actor.TenantId,
                        StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                        DocNo = issued.BillNo, Direction = dto.Direction, BizType = dto.BizType,
                        SupplierId = dto.SupplierId > 0 ? dto.SupplierId : null,
                        DocTime = dto.DocTime, Status = 0, TotalWeight = totalWeight, TotalAmount = totalAmount,
                        OperatorUserId = actor.UserId, DelFlag = 0,
                        Create_by = actor.UserName, Create_time = now, Remark = dto.Remark
                    };
                    header.Id = Context.Insertable(header).ExecuteReturnBigIdentity();
                    Context.Updateable<JyBillIssue>().SetColumns(it => it.BizId == header.Id)
                        .Where(it => it.Id == issued.Id && it.TenantId == actor.TenantId).ExecuteCommand();
                }

                // Keep SortNo unique for historical rows: clear the active flag first,
                // then move archived numbers out of the (DocId, SortNo) unique index.
                const int archivedSortOffset = 1000000;
                var maxArchivedSort = Context.Queryable<JyMaterialDocLine>()
                    .Where(it => it.DocId == header.Id)
                    .Max(it => (int?)it.SortNo) ?? 0;
                Context.Updateable<JyMaterialDocLine>()
                    .SetColumns(it => new JyMaterialDocLine
                    {
                        DelFlag = 2,
                        SortNo = it.SortNo + archivedSortOffset + maxArchivedSort
                    })
                    .Where(it => it.TenantId == actor.TenantId && it.DocId == header.Id && it.DelFlag == 0)
                    .ExecuteCommand();
                var sort = 0;
                foreach (var line in dto.Lines)
                {
                    var material = Context.Queryable<JyMaterial>().With(SqlWith.UpdLock)
                        .First(it => it.Id == line.MaterialId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                    if (material == null) throw new CustomException($"第 {sort + 1} 行物料不存在");
                    if (material.Status != 0) throw new CustomException($"物料 {material.MaterialCode} 已停用");
                    sort++;
                    Context.Insertable(new JyMaterialDocLine
                    {
                        TenantId = actor.TenantId, DocId = header.Id, SortNo = sort,
                        MaterialId = material.Id, MaterialCode = material.MaterialCode,
                        MaterialName = material.MaterialName, CategoryItemId = material.CategoryItemId,
                        MetalItemId = material.MetalItemId, Weight = line.Weight,
                        UnitCost = line.UnitCost,
                        Amount = decimal.Round(line.UnitCost * line.Weight, 2, MidpointRounding.AwayFromZero),
                        Remark = TrimLineRemark(line.Remark), DelFlag = 0, Create_time = now
                    }).ExecuteCommand();
                }
                result = header.Id;
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "物料单据保存失败");
            return result;
        }

        public int DeleteDoc(JyActor actor, long id)
        {
            var current = MustDoc(actor.TenantId, id);
            if (current.Status != 0) throw new CustomException("只有草稿单据可以删除");
            var affected = 0;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                Context.Updateable<JyMaterialDocLine>().SetColumns(it => it.DelFlag == 2)
                    .Where(it => it.TenantId == actor.TenantId && it.DocId == id && it.DelFlag == 0).ExecuteCommand();
                affected = Context.Updateable<JyMaterialDoc>().SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Update_by == actor.UserName).SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "物料单据删除失败");
            return affected;
        }

        public int ConfirmDoc(JyActor actor, long id)
        {
            var affected = 0;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var header = Context.Queryable<JyMaterialDoc>().With(SqlWith.UpdLock)
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (header == null) throw new CustomException("物料单据不存在");
                if (header.Status != 0) throw new CustomException("物料单据已确认，不能重复操作");
                var lines = Context.Queryable<JyMaterialDocLine>()
                    .Where(it => it.TenantId == actor.TenantId && it.DocId == id && it.DelFlag == 0)
                    .OrderBy(it => it.SortNo).ToList();
                if (lines.Count == 0) throw new CustomException("单据没有有效明细，不能确认");

                foreach (var group in lines.GroupBy(it => it.MaterialId))
                {
                    var material = Context.Queryable<JyMaterial>().With(SqlWith.UpdLock)
                        .First(it => it.Id == group.Key && it.TenantId == actor.TenantId && it.DelFlag == 0);
                    if (material == null) throw new CustomException("物料不存在或已删除");
                    if (material.Status != 0) throw new CustomException($"物料 {material.MaterialCode} 已停用");
                    var before = material.StockWeight;
                    var weight = group.Sum(it => it.Weight);
                    var amount = group.Sum(it => it.Amount);
                    decimal after;
                    decimal ledgerCost;
                    decimal avgCost;
                    if (header.Direction == 1)
                    {
                        after = before + weight;
                        avgCost = after <= 0 ? 0 : decimal.Round((before * material.AvgCostPerGram + amount) / after, 6, MidpointRounding.AwayFromZero);
                        ledgerCost = weight <= 0 ? 0 : decimal.Round(amount / weight, 6, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        if (weight > before - material.LockedWeight + 0.00005m)
                            throw new CustomException($"物料 {material.MaterialCode} 可用重量不足，当前可用 {decimal.Round(before - material.LockedWeight, 4)} 克");
                        after = before - weight;
                        avgCost = material.AvgCostPerGram;
                        ledgerCost = material.AvgCostPerGram;
                    }
                    if (after < 0) throw new CustomException($"物料 {material.MaterialCode} 库存重量不足");
                    var updated = Context.Updateable<JyMaterial>()
                        .SetColumns(it => it.StockWeight == after)
                        .SetColumns(it => it.AvgCostPerGram == avgCost)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == material.Id && it.TenantId == actor.TenantId
                            && it.StockWeight == before && it.DelFlag == 0)
                        .ExecuteCommand();
                    if (updated != 1) throw new CustomException("物料库存已变化，请刷新后重试");

                    var ledgerAmount = header.Direction == 2
                        ? decimal.Round(ledgerCost * weight, 2, MidpointRounding.AwayFromZero) : amount;
                    Context.Insertable(new JyMaterialLedger
                    {
                        TenantId = actor.TenantId, StoreId = header.StoreId,
                        LedgerNo = LedgerNo(header.DocNo, material.Id, header.Direction),
                        MaterialId = material.Id, MaterialCode = material.MaterialCode,
                        ChangeType = header.Direction,
                        ChangeWeight = header.Direction == 1 ? weight : -weight,
                        BeforeWeight = before, AfterWeight = after, UnitCost = ledgerCost,
                        Amount = ledgerAmount, BizType = header.BizType, BizId = header.Id,
                        BizNo = header.DocNo, BizTime = header.DocTime, OperatorUserId = actor.UserId,
                        Create_by = actor.UserName, Create_time = now, Remark = header.Remark
                    }).ExecuteCommand();
                }

                affected = Context.Updateable<JyMaterialDoc>()
                    .SetColumns(it => it.Status == 1)
                    .SetColumns(it => it.ConfirmUserId == actor.UserId)
                    .SetColumns(it => it.ConfirmTime == now)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("物料单据状态已变化，请刷新后重试");
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "物料单据确认失败");
            return affected;
        }

        public PagedInfo<JyMaterialLedgerDto> Ledger(JyActor actor, JyMaterialLedgerQueryDto query)
        {
            query ??= new JyMaterialLedgerQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JyMaterialLedger>()
                .Where(it => it.TenantId == actor.TenantId)
                .WhereIF(query.MaterialId > 0, it => it.MaterialId == query.MaterialId)
                .WhereIF(query.ChangeType is 1 or 2, it => it.ChangeType == query.ChangeType)
                .WhereIF(query.BeginTime.HasValue, it => it.BizTime >= query.BeginTime.Value)
                .WhereIF(query.EndTime.HasValue, it => it.BizTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.BizNo.Contains(keyword)
                    || it.MaterialCode.Contains(keyword) || it.LedgerNo.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.BizTime, OrderByType.Desc).OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            var names = MaterialNames(actor.TenantId, rows.Select(it => it.MaterialId));
            return new PagedInfo<JyMaterialLedgerDto>
            {
                PageIndex = page, PageSize = size, TotalNum = total,
                Result = rows.Select(it => new JyMaterialLedgerDto
                {
                    Id = it.Id, LedgerNo = it.LedgerNo, MaterialId = it.MaterialId,
                    MaterialCode = it.MaterialCode, MaterialName = names.GetValueOrDefault(it.MaterialId),
                    ChangeType = it.ChangeType,
                    ChangeTypeText = it.ChangeType == 1 ? "物料入库" : "物料出库",
                    ChangeWeight = it.ChangeWeight, BeforeWeight = it.BeforeWeight,
                    AfterWeight = it.AfterWeight, UnitCost = it.UnitCost, Amount = it.Amount,
                    BizType = it.BizType, BizTypeText = BizTypeText(it.BizType), BizId = it.BizId,
                    BizNo = it.BizNo, BizTime = it.BizTime, CreateBy = it.Create_by, Remark = it.Remark
                }).ToList()
            };
        }

        public List<JyMaterialDto> AvailableOptions(JyActor actor)
        {
            var rows = Context.Queryable<JyMaterial>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.MaterialCode).ToList();
            return ToDtos(actor.TenantId, rows);
        }

        private void EnsureLines(string tenantId, List<JyMaterialDocLineEditDto> lines)
        {
            if (lines.GroupBy(it => it.MaterialId).Any(it => it.Key <= 0 || it.Count() > 1))
                throw new CustomException("同一物料在一张单据中只能出现一次");
            foreach (var line in lines)
            {
                if (line.Weight <= 0) throw new CustomException("物料重量必须大于 0");
                if (line.Weight > 1000000m) throw new CustomException("单条物料重量明显异常");
                if (line.UnitCost < 0) throw new CustomException("单位成本不能为负数");
                if (line.UnitCost > 1000000m) throw new CustomException("单位成本明显异常");
                line.Remark = TrimLineRemark(line.Remark);
            }
            var ids = lines.Select(it => it.MaterialId).Distinct().ToList();
            var found = Context.Queryable<JyMaterial>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && ids.Contains(it.Id)).ToList();
            if (found.Count != ids.Count) throw new CustomException("部分物料不存在或已删除");
        }

        private void ValidateReferences(string tenantId, long? categoryItemId, long? metalItemId, long? supplierId)
        {
            if (categoryItemId > 0 && !LookupItemExists(tenantId, categoryItemId.Value, "material_category"))
                throw new CustomException("物料分类不存在或不属于物料分类");
            if (metalItemId > 0 && !LookupItemExists(tenantId, metalItemId.Value, "metal"))
                throw new CustomException("金属成色不存在");
            ValidateSupplier(tenantId, supplierId);
        }

        private bool LookupItemExists(string tenantId, long id, string typeCode)
        {
            return Context.Queryable<JyLookupItem, JyLookupType>((i, t) => i.TypeId == t.Id)
                .Where((i, t) => i.Id == id && i.TenantId == tenantId && i.DelFlag == 0
                    && t.TenantId == tenantId && t.DelFlag == 0 && t.TypeCode == typeCode)
                .Any();
        }

        private void ValidateSupplier(string tenantId, long? supplierId)
        {
            if (supplierId is not > 0) return;
            var supplier = Context.Queryable<JySupplier>()
                .First(it => it.Id == supplierId.Value && it.TenantId == tenantId && it.DelFlag == 0);
            if (supplier == null) throw new CustomException("供应商不存在");
            if (supplier.Status != 0) throw new CustomException("供应商已停用，不能新增单据");
        }

        private void EnsureCodeFree(string tenantId, string code, long exceptId)
        {
            if (Context.Queryable<JyMaterial>().Any(it => it.TenantId == tenantId
                && it.MaterialCode == code && it.DelFlag == 0 && it.Id != exceptId))
                throw new CustomException("物料编码已存在");
        }

        private JyMaterial Must(string tenantId, long id)
        {
            return Context.Queryable<JyMaterial>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("物料不存在");
        }

        private JyMaterialDoc MustDoc(string tenantId, long id)
        {
            return Context.Queryable<JyMaterialDoc>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("物料单据不存在");
        }

        private List<JyMaterialDto> ToDtos(string tenantId, List<JyMaterial> rows)
        {
            if (rows.Count == 0) return new List<JyMaterialDto>();
            var lookupIds = rows.Where(it => it.CategoryItemId.HasValue).Select(it => it.CategoryItemId.Value)
                .Concat(rows.Where(it => it.MetalItemId.HasValue).Select(it => it.MetalItemId.Value))
                .Distinct().ToList();
            var lookups = lookupIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && lookupIds.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id, it => it.ItemName);
            var suppliers = SupplierNames(tenantId, rows.Select(it => it.SupplierId));
            return rows.Select(it => new JyMaterialDto
            {
                Id = it.Id, StoreId = it.StoreId, MaterialCode = it.MaterialCode,
                MaterialName = it.MaterialName, CategoryItemId = it.CategoryItemId,
                CategoryName = it.CategoryItemId.HasValue ? lookups.GetValueOrDefault(it.CategoryItemId.Value) : null,
                MetalItemId = it.MetalItemId,
                MetalName = it.MetalItemId.HasValue ? lookups.GetValueOrDefault(it.MetalItemId.Value) : null,
                SupplierId = it.SupplierId,
                SupplierName = suppliers.GetValueOrDefault(it.SupplierId ?? 0),
                StockWeight = it.StockWeight, LockedWeight = it.LockedWeight,
                AvailableWeight = it.StockWeight - it.LockedWeight,
                AvgCostPerGram = it.AvgCostPerGram, SalePricePerGram = it.SalePricePerGram,
                Status = it.Status, Remark = it.Remark, CreateBy = it.Create_by, CreateTime = it.Create_time
            }).ToList();
        }

        private List<JyMaterialDocLineDto> LoadLines(string tenantId, long docId)
        {
            var rows = Context.Queryable<JyMaterialDocLine>()
                .Where(it => it.TenantId == tenantId && it.DocId == docId && it.DelFlag == 0)
                .OrderBy(it => it.SortNo).ToList();
            var lookupIds = rows.Where(it => it.CategoryItemId.HasValue).Select(it => it.CategoryItemId.Value)
                .Concat(rows.Where(it => it.MetalItemId.HasValue).Select(it => it.MetalItemId.Value))
                .Distinct().ToList();
            var lookups = lookupIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && lookupIds.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id, it => it.ItemName);
            return rows.Select(it => new JyMaterialDocLineDto
            {
                Id = it.Id, SortNo = it.SortNo, MaterialId = it.MaterialId,
                MaterialCode = it.MaterialCode, MaterialName = it.MaterialName,
                CategoryName = it.CategoryItemId.HasValue ? lookups.GetValueOrDefault(it.CategoryItemId.Value) : null,
                MetalName = it.MetalItemId.HasValue ? lookups.GetValueOrDefault(it.MetalItemId.Value) : null,
                Weight = it.Weight, UnitCost = it.UnitCost, Amount = it.Amount, Remark = it.Remark
            }).ToList();
        }

        private Dictionary<long, string> SupplierNames(string tenantId, IEnumerable<long?> ids)
        {
            var values = ids.Where(it => it.HasValue).Select(it => it.Value).Distinct().ToList();
            return values.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JySupplier>()
                .Where(it => it.TenantId == tenantId && values.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id, it => it.SupplierName);
        }

        private Dictionary<long, string> MaterialNames(string tenantId, IEnumerable<long> ids)
        {
            var values = ids.Distinct().ToList();
            return values.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyMaterial>()
                .Where(it => it.TenantId == tenantId && values.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id, it => it.MaterialName);
        }

        private static JyMaterialDocDto ToDto(JyMaterialDoc it, Dictionary<long, string> suppliers)
        {
            return new JyMaterialDocDto
            {
                Id = it.Id, StoreId = it.StoreId, DocNo = it.DocNo,
                Direction = it.Direction, DirectionText = it.Direction == 1 ? "物料入库" : "物料出库",
                BizType = it.BizType, BizTypeText = BizTypeText(it.BizType),
                SupplierId = it.SupplierId,
                SupplierName = it.SupplierId.HasValue ? suppliers.GetValueOrDefault(it.SupplierId.Value) : null,
                DocTime = it.DocTime, Status = it.Status,
                StatusText = it.Status == 0 ? "草稿" : "已确认",
                TotalWeight = it.TotalWeight, TotalAmount = it.TotalAmount,
                Remark = it.Remark, CreateBy = it.Create_by, CreateTime = it.Create_time,
                ConfirmTime = it.ConfirmTime
            };
        }

        private static string NormalizeBizType(int direction, string bizType)
        {
            bizType = (bizType ?? string.Empty).Trim().ToLowerInvariant();
            var allowed = direction == 1 ? InboundBizTypes : OutboundBizTypes;
            if (!allowed.Contains(bizType)) throw new CustomException("物料业务类型不正确");
            return bizType;
        }

        private static string BizTypeText(string bizType)
        {
            return (bizType ?? string.Empty).ToLowerInvariant() switch
            {
                "purchase" => "采购入库", "return" => "退回入库",
                "old_material" => "旧料入库", "other_in" => "其他入库",
                "consume" => "领用出库", "loss" => "损耗出库",
                "transfer" => "调拨出库", "other_out" => "其他出库",
                _ => "物料变动"
            };
        }

        private static string LedgerNo(string docNo, long materialId, int direction)
        {
            var suffix = "-" + (direction == 1 ? "IN" : "OUT") + "-" + materialId;
            return docNo.Length + suffix.Length <= 32
                ? docNo + suffix
                : docNo.Substring(0, 32 - suffix.Length) + suffix;
        }

        private static string TrimRemark(string remark)
        {
            remark = remark?.Trim();
            if (string.IsNullOrEmpty(remark)) return null;
            if (remark.Length > 500) throw new CustomException("备注不能超过 500 个字");
            return remark;
        }

        private static string TrimLineRemark(string remark)
        {
            remark = remark?.Trim();
            if (string.IsNullOrEmpty(remark)) return null;
            if (remark.Length > 200) throw new CustomException("明细备注不能超过 200 个字");
            return remark;
        }
    }
}
