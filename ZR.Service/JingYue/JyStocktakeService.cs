using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyStocktakeService : BaseService<JyStocktake>
    {
        private const int TakeTypeGoods = 1;
        private const int TakeTypeMaterial = 2;
        private const int MaxLines = 20000;
        private const decimal Epsilon = 0.00005m;

        public PagedInfo<JyStocktakeDto> List(JyActor actor, JyStocktakeQueryDto query)
        {
            query ??= new JyStocktakeQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JyStocktake>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.TakeType is TakeTypeGoods or TakeTypeMaterial, it => it.TakeType == query.TakeType)
                .WhereIF(query.Status is 0 or 1 or 9, it => it.Status == query.Status)
                .WhereIF(query.BeginTime.HasValue, it => it.TakeTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.TakeTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.StocktakeNo.Contains(keyword) || it.Remark.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.Status).OrderBy(it => it.TakeTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc).ToPageList(page, size, ref total);
            return new PagedInfo<JyStocktakeDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(ToDto).ToList()
            };
        }

        public JyStocktakeDto Detail(JyActor actor, long id)
        {
            var header = Must(actor.TenantId, id);
            var dto = ToDto(header);
            dto.Lines = LineDtos(actor.TenantId, header.Id);
            dto.Categories = CategoryDtos(actor.TenantId, header.Id, dto.Lines);
            return dto;
        }

        public JyStocktakeDashboardDto Dashboard(JyActor actor)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var rows = Context.Queryable<JyStocktake>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ToList();
            var pendingRows = rows.Where(it => it.Status == 0)
                .OrderBy(it => it.TakeTime).ThenByDescending(it => it.Id).Take(6).ToList();
            var confirmedToday = rows.Where(it => it.Status == 1 && it.TakeTime >= today && it.TakeTime < tomorrow).ToList();
            return new JyStocktakeDashboardDto
            {
                DraftCount = rows.Count(it => it.Status == 0),
                ConfirmedToday = confirmedToday.Count,
                DiffLineCount = confirmedToday.Sum(it => it.DiffLineCount),
                NetDiffAmount = confirmedToday.Sum(it => it.DiffAmount),
                GoodsCount = confirmedToday.Where(it => it.TakeType == TakeTypeGoods).Sum(it => it.LineCount),
                MaterialCount = confirmedToday.Where(it => it.TakeType == TakeTypeMaterial).Sum(it => it.LineCount),
                Pending = pendingRows.Select(ToDto).ToList()
            };
        }

        public List<JyStocktakeOptionDto> Options(JyActor actor, int takeType, string keyword)
        {
            keyword = keyword?.Trim();
            if (keyword?.Length > 64) throw new CustomException("查询内容不能超过 64 个字");
            if (takeType == TakeTypeGoods)
            {
                var q = Context.Queryable<JyGoodsItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && (it.Status == 1 || it.Status == 2 || it.Status == 5));
                if (!string.IsNullOrEmpty(keyword))
                {
                    q = q.Where(it => it.GoodsNo.Contains(keyword) || it.Barcode.Contains(keyword)
                        || it.StyleName.Contains(keyword) || it.StyleItemName.Contains(keyword));
                }
                return q.OrderBy(it => it.Id, OrderByType.Desc).Take(80).ToList().Select(it => new JyStocktakeOptionDto
                {
                    Id = it.Id, Code = it.GoodsNo, Name = it.StyleName, Barcode = it.Barcode,
                    StyleName = it.StyleItemName, MetalName = it.MetalName, Unit = it.Unit,
                    ItemMode = it.ItemMode, Qty = it.Qty, LockedQty = it.LockedQty,
                    UnitCost = it.CostPrice, Status = it.Status
                }).ToList();
            }
            if (takeType == TakeTypeMaterial)
            {
                var q = Context.Queryable<JyMaterial>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0);
                if (!string.IsNullOrEmpty(keyword))
                {
                    q = q.Where(it => it.MaterialCode.Contains(keyword) || it.MaterialName.Contains(keyword));
                }
                return q.OrderBy(it => it.MaterialCode).Take(80).ToList().Select(it => new JyStocktakeOptionDto
                {
                    Id = it.Id, Code = it.MaterialCode, Name = it.MaterialName, Unit = "g",
                    ItemMode = 2, Qty = it.StockWeight, LockedQty = it.LockedWeight,
                    UnitCost = it.AvgCostPerGram, Status = it.Status
                }).ToList();
            }
            throw new CustomException("盘点类型不正确");
        }

        /// <summary>按当前库存创建全量盘点快照。分类和账面数在此刻固定，后续盘点不会漏掉新建前已存在的条目。</summary>
        public long Initialize(JyActor actor, JyStocktakeInitDto dto, JyBillRuleService billRule)
        {
            if (dto == null) throw new CustomException("盘点单不能为空");
            if (dto.TakeType != TakeTypeGoods && dto.TakeType != TakeTypeMaterial)
                throw new CustomException("盘点类型不正确");
            if (dto.TakeTime == default) dto.TakeTime = DateTime.Now;
            if (dto.TakeTime.Year < 2000 || dto.TakeTime.Year > 2100)
                throw new CustomException("盘点时间不正确");
            dto.Remark = TrimRemark(dto.Remark);

            var now = DateTime.Now;
            long resultId = 0;
            var tran = UseTran(() =>
            {
                billRule.EnsureFoundation(actor);
                var issued = billRule.IssueInTransaction(actor, "stocktake", "jy_stocktake", null, dto.TakeTime);
                var header = new JyStocktake
                {
                    TenantId = actor.TenantId, StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                    StocktakeNo = issued.BillNo, TakeType = dto.TakeType, TakeTime = dto.TakeTime,
                    Status = 0, OperatorUserId = actor.UserId, DelFlag = 0,
                    Create_by = actor.UserName, Create_time = now, Remark = dto.Remark
                };
                header.Id = Context.Insertable(header).ExecuteReturnBigIdentity();

                var lines = dto.TakeType == TakeTypeGoods
                    ? SnapshotGoods(actor.TenantId, header.Id, now)
                    : SnapshotMaterials(actor.TenantId, header.Id, now);
                if (lines.Count == 0)
                    throw new CustomException(dto.TakeType == TakeTypeGoods ? "当前没有可盘点的在库商品" : "当前没有启用的物料档案");
                if (lines.Count > MaxLines) throw new CustomException($"盘点明细过多，不能超过 {MaxLines} 条");
                InsertStocktakeLines(lines);

                var categories = BuildCategoryRows(actor, header, lines, now);
                foreach (var category in categories)
                {
                    foreach (var line in lines.Where(it => (it.CategoryKey ?? "uncategorized") == category.CategoryKey))
                    {
                        line.CategoryKey = category.CategoryKey;
                        line.CategoryName = category.CategoryName;
                    }
                }
                UpdateStocktakeLineCategories(actor, header.Id, lines);
                SaveCategoryRows(actor, header, categories);
                var affected = Context.Updateable<JyStocktake>()
                    .SetColumns(it => it.LineCount == lines.Count)
                    .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("盘点单创建失败，请重试");
                resultId = header.Id;
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "盘点初始化失败");
            return resultId;
        }

        /// <summary>保存草稿。账面数在保存时锁定；确认时要求库存仍与账面快照一致。</summary>
        public long Save(JyActor actor, JyStocktakeEditDto dto, JyBillRuleService billRule)
        {
            if (dto == null) throw new CustomException("盘点单不能为空");
            if (dto.TakeType != TakeTypeGoods && dto.TakeType != TakeTypeMaterial)
                throw new CustomException("盘点类型不正确");
            dto.Lines ??= new List<JyStocktakeLineEditDto>();
            if (dto.Lines.Count == 0) throw new CustomException("盘点单至少需要一条明细");
            if (dto.Lines.Count > MaxLines) throw new CustomException($"单张盘点单不能超过 {MaxLines} 条明细");
            if (dto.TakeTime == default) dto.TakeTime = DateTime.Now;
            if (dto.TakeTime.Year < 2000 || dto.TakeTime.Year > 2100)
                throw new CustomException("盘点时间不正确");
            dto.Remark = TrimRemark(dto.Remark);

            var now = DateTime.Now;
            long resultId = 0;
            var tran = UseTran(() =>
            {
                JyStocktake header;
                if (dto.Id > 0)
                {
                    header = Context.Queryable<JyStocktake>().With(SqlWith.UpdLock)
                        .First(it => it.Id == dto.Id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                    if (header == null) throw new CustomException("盘点单不存在");
                    if (header.Status != 0) throw new CustomException("已确认或作废的盘点单不能修改");
                    if (header.TakeType != dto.TakeType) throw new CustomException("盘点类型不能修改");
                }
                else
                {
                    billRule.EnsureFoundation(actor);
                    var issued = billRule.IssueInTransaction(actor, "stocktake", "jy_stocktake", null, dto.TakeTime);
                    header = new JyStocktake
                    {
                        TenantId = actor.TenantId, StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                        StocktakeNo = issued.BillNo, TakeType = dto.TakeType, TakeTime = dto.TakeTime,
                        Status = 0, OperatorUserId = actor.UserId, DelFlag = 0,
                        Create_by = actor.UserName, Create_time = now, Remark = dto.Remark
                    };
                    header.Id = Context.Insertable(header).ExecuteReturnBigIdentity();
                }

                List<JyStocktakeLine> normalized;
                if (dto.Id > 0)
                {
                    normalized = UpdateSnapshotLines(actor, header, dto.Lines);
                    PersistSnapshotLines(actor, normalized);
                }
                else
                {
                    normalized = dto.TakeType == TakeTypeGoods
                        ? BuildGoodsLines(actor.TenantId, header.Id, dto.Lines)
                        : BuildMaterialLines(actor.TenantId, header.Id, dto.Lines);
                    for (var i = 0; i < normalized.Count; i++)
                    {
                        normalized[i].SortNo = (i + 1) * 10;
                        normalized[i].Create_time = now;
                    }
                    InsertStocktakeLines(normalized);
                }
                SaveCategories(actor, header, normalized, dto.Categories, now);
                EnsureParentAnomalyRemarks(dto.Categories, normalized, header.TakeType);

                var diffCount = normalized.Count(it => Math.Abs(it.DiffQty) > Epsilon);
                var goodsDiff = dto.TakeType == TakeTypeGoods ? normalized.Sum(it => it.DiffQty) : 0;
                var materialDiff = dto.TakeType == TakeTypeMaterial ? normalized.Sum(it => it.DiffQty) : 0;
                var gain = normalized.Where(it => it.DiffQty > Epsilon).Sum(it => it.DiffAmount);
                var loss = normalized.Where(it => it.DiffQty < -Epsilon).Sum(it => Math.Abs(it.DiffAmount));
                var affected = Context.Updateable<JyStocktake>()
                    .SetColumns(it => it.StoreId == (dto.StoreId > 0 ? dto.StoreId : null))
                    .SetColumns(it => it.TakeTime == dto.TakeTime)
                    .SetColumns(it => it.LineCount == normalized.Count)
                    .SetColumns(it => it.DiffLineCount == diffCount)
                    .SetColumns(it => it.GoodsDiffQty == goodsDiff)
                    .SetColumns(it => it.MaterialDiffWeight == materialDiff)
                    .SetColumns(it => it.GainAmount == gain)
                    .SetColumns(it => it.LossAmount == loss)
                    .SetColumns(it => it.DiffAmount == gain - loss)
                    .SetColumns(it => it.Remark == dto.Remark)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("盘点单状态已变化，请刷新后重试");
                resultId = header.Id;
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "盘点单保存失败");
            return resultId;
        }

        public int Confirm(JyActor actor, long id)
        {
            var affected = 0;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var header = Context.Queryable<JyStocktake>().With(SqlWith.UpdLock)
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (header == null) throw new CustomException("盘点单不存在");
                if (header.Status != 0) throw new CustomException("盘点单已确认或作废，不能重复操作");

                var lines = Context.Queryable<JyStocktakeLine>()
                    .Where(it => it.TenantId == actor.TenantId && it.StocktakeId == id && it.DelFlag == 0)
                    .OrderBy(it => it.SortNo).ToList();
                if (lines.Count == 0) throw new CustomException("盘点单没有有效明细，不能确认");
                if (lines.Count > MaxLines) throw new CustomException($"盘点单明细过多，不能超过 {MaxLines} 条");

                var categories = Context.Queryable<JyStocktakeCategory>()
                    .Where(it => it.TenantId == actor.TenantId && it.StocktakeId == id).ToList();
                var effectiveLines = lines;
                if (categories.Count > 0)
                {
                    foreach (var category in categories.Where(it => it.IsCompleted == 0))
                    {
                        if (string.IsNullOrWhiteSpace(category.IncompleteReason))
                            throw new CustomException($"分类「{category.CategoryName}」未盘点完成，请填写未完成原因");
                    }
                    var completedKeys = categories.Where(it => it.IsCompleted == 1)
                        .Select(it => it.CategoryKey).ToHashSet(StringComparer.Ordinal);
                    if (completedKeys.Count == 0)
                        throw new CustomException("请至少完成一个分类后再确认盘点");
                    var knownKeys = categories.Select(it => it.CategoryKey).ToHashSet(StringComparer.Ordinal);
                    if (lines.Any(it => !knownKeys.Contains(it.CategoryKey ?? string.Empty)))
                        throw new CustomException("盘点分类进度不完整，请重新打开盘点单并保存后再确认");
                    effectiveLines = lines.Where(it => completedKeys.Contains(it.CategoryKey ?? string.Empty)).ToList();
                    var completedCategories = categories.Where(it => it.IsCompleted == 1).ToList();
                    EnsureParentAnomalyRemarks(completedCategories.Select(it => new JyStocktakeCategoryEditDto
                    {
                        CategoryKey = it.CategoryKey, CategoryName = it.CategoryName,
                        ParentCategoryKey = it.ParentCategoryKey, ParentCategoryName = it.ParentCategoryName,
                        IsCompleted = true, AnomalyRemark = it.AnomalyRemark
                    }).ToList(), effectiveLines, header.TakeType);
                }

                if (header.TakeType == TakeTypeGoods) ConfirmGoods(actor, header, effectiveLines, now);
                else if (header.TakeType == TakeTypeMaterial) ConfirmMaterials(actor, header, effectiveLines, now);
                else throw new CustomException("盘点类型不正确");

                var diffLines = effectiveLines.Where(it => Math.Abs(it.DiffQty) > Epsilon).ToList();
                var gain = diffLines.Where(it => it.DiffQty > 0).Sum(it => it.DiffAmount);
                var loss = diffLines.Where(it => it.DiffQty < 0).Sum(it => Math.Abs(it.DiffAmount));
                var goodsDiff = header.TakeType == TakeTypeGoods ? effectiveLines.Sum(it => it.DiffQty) : 0;
                var materialDiff = header.TakeType == TakeTypeMaterial ? effectiveLines.Sum(it => it.DiffQty) : 0;

                // 差异行也可能在保存和确认之间被改过；整单一起写回，异常时事务回滚。
                foreach (var line in effectiveLines)
                {
                    var lineAffected = Context.Updateable<JyStocktakeLine>()
                        .SetColumns(it => it.DiffQty == line.DiffQty)
                        .SetColumns(it => it.DiffAmount == line.DiffAmount)
                        .SetColumns(it => it.ItemStatus == line.ItemStatus)
                        .Where(it => it.Id == line.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                        .ExecuteCommand();
                    if (lineAffected != 1) throw new CustomException("盘点明细已变化，请刷新后重试");
                }

                affected = Context.Updateable<JyStocktake>().SetColumns(it => it.Status == 1)
                    .SetColumns(it => it.DiffLineCount == diffLines.Count)
                    .SetColumns(it => it.GoodsDiffQty == goodsDiff)
                    .SetColumns(it => it.MaterialDiffWeight == materialDiff)
                    .SetColumns(it => it.GainAmount == gain)
                    .SetColumns(it => it.LossAmount == loss)
                    .SetColumns(it => it.DiffAmount == gain - loss)
                    .SetColumns(it => it.ConfirmUserId == actor.UserId)
                    .SetColumns(it => it.ConfirmTime == now)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("盘点单状态已变化，请刷新后重试");
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "盘点确认失败");
            return affected;
        }

        public int Delete(JyActor actor, long id, JyBillRuleService billRule)
        {
            var affected = 0;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var header = Context.Queryable<JyStocktake>().With(SqlWith.UpdLock)
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (header == null) throw new CustomException("盘点单不存在");
                if (header.Status != 0 && header.Status != 1) throw new CustomException("当前盘点单状态不允许删除");
                affected = Context.Updateable<JyStocktake>().SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Status == 9)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && (it.Status == 0 || it.Status == 1) && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("盘点单状态已变化，请刷新后重试");
                Context.Updateable<JyStocktakeLine>().SetColumns(it => it.DelFlag == 2)
                    .Where(it => it.TenantId == actor.TenantId && it.StocktakeId == id)
                    .ExecuteCommand();
                Context.Deleteable<JyStocktakeCategory>()
                    .Where(it => it.TenantId == actor.TenantId && it.StocktakeId == id)
                    .ExecuteCommand();
                if (!string.IsNullOrEmpty(header.StocktakeNo))
                {
                    Context.Updateable<JyBillIssue>().SetColumns(it => it.BizId == null)
                        .Where(it => it.TenantId == actor.TenantId && it.BizTable == "jy_stocktake"
                            && it.BillType == "stocktake" && it.BillNo == header.StocktakeNo)
                        .ExecuteCommand();
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "盘点单删除失败");
            return affected;
        }

        public JyStocktakeReportDto Report(JyActor actor, JyStocktakeReportQueryDto query)
        {
            query ??= new JyStocktakeReportQueryDto();
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JyStocktake>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1)
                .WhereIF(query.TakeType is TakeTypeGoods or TakeTypeMaterial, it => it.TakeType == query.TakeType)
                .WhereIF(query.BeginTime.HasValue, it => it.TakeTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.TakeTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.StocktakeNo.Contains(keyword) || it.Remark.Contains(keyword));
            var headers = q.OrderBy(it => it.TakeTime, OrderByType.Desc).OrderBy(it => it.Id, OrderByType.Desc)
                .Take(2000).ToList();
            var ids = headers.Select(it => it.Id).ToList();
            var lines = ids.Count == 0 ? new List<JyStocktakeLine>() : Context.Queryable<JyStocktakeLine>()
                .Where(it => it.TenantId == actor.TenantId && ids.Contains(it.StocktakeId) && it.DelFlag == 0)
                .OrderBy(it => it.SortNo).ToList();
            var categoryRows = ids.Count == 0 ? new List<JyStocktakeCategory>() : Context.Queryable<JyStocktakeCategory>()
                .Where(it => it.TenantId == actor.TenantId && ids.Contains(it.StocktakeId)).ToList();
            var differences = lines.Where(it => Math.Abs(it.DiffQty) > Epsilon)
                .OrderByDescending(it => it.Create_time).ThenBy(it => it.SortNo).ToList();
            return new JyStocktakeReportDto
            {
                DocumentCount = headers.Count,
                LineCount = lines.Count,
                DiffLineCount = differences.Count,
                GoodsGainQty = differences.Where(it => it.ItemType == TakeTypeGoods && it.DiffQty > 0).Sum(it => it.DiffQty),
                GoodsLossQty = differences.Where(it => it.ItemType == TakeTypeGoods && it.DiffQty < 0).Sum(it => Math.Abs(it.DiffQty)),
                MaterialGainWeight = differences.Where(it => it.ItemType == TakeTypeMaterial && it.DiffQty > 0).Sum(it => it.DiffQty),
                MaterialLossWeight = differences.Where(it => it.ItemType == TakeTypeMaterial && it.DiffQty < 0).Sum(it => Math.Abs(it.DiffQty)),
                GainAmount = differences.Where(it => it.DiffQty > 0).Sum(it => it.DiffAmount),
                LossAmount = differences.Where(it => it.DiffQty < 0).Sum(it => Math.Abs(it.DiffAmount)),
                NetDiffAmount = differences.Sum(it => it.DiffAmount),
                GoodsDocumentCount = headers.Count(it => it.TakeType == TakeTypeGoods),
                MaterialDocumentCount = headers.Count(it => it.TakeType == TakeTypeMaterial),
                Categories = BuildReportCategories(categoryRows, lines),
                Documents = headers.Select(ToDto).ToList(),
                Differences = differences.Select(ToLineDto).ToList()
            };
        }

        private static List<JyStocktakeReportCategoryDto> BuildReportCategories(
            List<JyStocktakeCategory> categories, List<JyStocktakeLine> lines)
        {
            var categoryByKey = categories.GroupBy(it => (it.StocktakeId, it.CategoryKey))
                .ToDictionary(it => it.Key, it => it.First());
            var lineByCategory = lines.GroupBy(it => (it.StocktakeId, CategoryKey: it.CategoryKey ?? "uncategorized"))
                .ToDictionary(it => it.Key, it => it.ToList());
            var rows = categories.Select(category =>
            {
                var key = string.IsNullOrWhiteSpace(category.ParentCategoryKey)
                    ? "parent-" + category.CategoryKey : category.ParentCategoryKey;
                var name = string.IsNullOrWhiteSpace(category.ParentCategoryName)
                    ? category.CategoryName : category.ParentCategoryName;
                var groupedLines = lineByCategory.GetValueOrDefault((category.StocktakeId, category.CategoryKey)) ?? new List<JyStocktakeLine>();
                return new { TakeType = category.ItemType, Key = key, Name = name, StocktakeId = category.StocktakeId, Lines = groupedLines };
            }).ToList();

            // 历史单据可能没有分类快照；按明细分类名补一个一级分组，避免汇总漏数。
            rows.AddRange(lines.Where(line => !categoryByKey.ContainsKey((line.StocktakeId, line.CategoryKey ?? "uncategorized")))
                .GroupBy(line => new { line.StocktakeId, Key = line.CategoryKey ?? "uncategorized", Name = line.CategoryName ?? "未分类", line.ItemType })
                .Select(group => new
                {
                    TakeType = group.Key.ItemType,
                    Key = "parent-" + group.Key.Key,
                    Name = group.Key.Name,
                    StocktakeId = group.Key.StocktakeId,
                    Lines = group.ToList()
                }));

            return rows.GroupBy(it => new { it.TakeType, it.Key, it.Name })
                .Select(group =>
                {
                    var groupedLines = group.SelectMany(it => it.Lines).ToList();
                    return new JyStocktakeReportCategoryDto
                    {
                        TakeType = group.Key.TakeType,
                        ParentCategoryKey = group.Key.Key,
                        ParentCategoryName = group.Key.Name,
                        DocumentCount = group.Select(it => it.StocktakeId).Distinct().Count(),
                        ItemCount = groupedLines.Count,
                        BookQty = groupedLines.Sum(it => it.BookQty),
                        ActualQty = groupedLines.Sum(it => it.ActualQty),
                        DiffQty = groupedLines.Sum(it => it.DiffQty),
                        DiffAmount = groupedLines.Sum(it => it.DiffAmount)
                    };
                })
                .OrderBy(it => it.TakeType).ThenBy(it => it.ParentCategoryName)
                .ToList();
        }

        private void ConfirmGoods(JyActor actor, JyStocktake header, List<JyStocktakeLine> lines, DateTime now)
        {
            if (lines.Any(it => !it.GoodsItemId.HasValue)) throw new CustomException("盘点明细缺少货品 ID");
            var ids = lines.Select(it => it.GoodsItemId.Value).ToList();
            if (ids.Distinct().Count() != ids.Count) throw new CustomException("盘点单存在重复货品");

            foreach (var line in lines.OrderBy(it => it.GoodsItemId.Value))
            {
                var goodsId = line.GoodsItemId.Value;
                var goods = Context.Queryable<JyGoodsItem>().With(SqlWith.UpdLock)
                    .First(it => it.Id == goodsId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (goods == null) throw new CustomException($"货品 {line.ItemNo} 已删除，请重新盘点");
                if (goods.Status != 1 && goods.Status != 2 && goods.Status != 5)
                    throw new CustomException($"货品 {goods.GoodsNo} 当前状态不能盘点");
                if (goods.Qty != line.BookQty)
                    throw new CustomException($"货品 {goods.GoodsNo} 库存已变化，请重新盘点");

                var actual = NormalizeActual(line.ActualQty, "货品 " + goods.GoodsNo);
                if (goods.ItemMode == 1 && actual != 0 && actual != 1)
                    throw new CustomException($"单件货品 {goods.GoodsNo} 的实盘件数只能为 0 或 1");
                if (actual + Epsilon < goods.LockedQty)
                    throw new CustomException($"货品 {goods.GoodsNo} 实盘数量不能小于锁定数量 {goods.LockedQty}");
                var diff = actual - goods.Qty;
                var afterStatus = actual > 0 ? (goods.LockedQty > 0 ? 2 : 1) : 5;
                line.ActualQty = actual;
                line.DiffQty = diff;
                line.UnitCost = goods.CostPrice;
                line.DiffAmount = RoundMoney(diff * goods.CostPrice);
                line.ItemStatus = afterStatus;

                var updated = Context.Updateable<JyGoodsItem>().SetColumns(it => it.Qty == actual)
                    .SetColumns(it => it.Status == afterStatus)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == goods.Id && it.TenantId == actor.TenantId
                        && it.Qty == goods.Qty && it.DelFlag == 0)
                    .ExecuteCommand();
                if (updated != 1) throw new CustomException($"货品 {goods.GoodsNo} 库存已变化，请重新盘点");

                if (diff != 0)
                {
                    Context.Insertable(new JyStockLedger
                    {
                        TenantId = actor.TenantId, StoreId = header.StoreId,
                        LedgerNo = LedgerNo(header.StocktakeNo, goods.Id),
                        GoodsItemId = goods.Id, GoodsNo = goods.GoodsNo, ChangeType = 10,
                        ChangeQty = diff, BeforeQty = goods.Qty, AfterQty = actual,
                        BizType = "stocktake", BizId = header.Id, BizTime = header.TakeTime,
                        Create_by = actor.UserName, Create_time = now,
                        Remark = string.IsNullOrWhiteSpace(line.Remark) ? header.Remark : line.Remark
                    }).ExecuteCommand();
                }
            }
        }

        private void ConfirmMaterials(JyActor actor, JyStocktake header, List<JyStocktakeLine> lines, DateTime now)
        {
            if (lines.Any(it => !it.MaterialId.HasValue)) throw new CustomException("盘点明细缺少物料 ID");
            var ids = lines.Select(it => it.MaterialId.Value).ToList();
            if (ids.Distinct().Count() != ids.Count) throw new CustomException("盘点单存在重复物料");

            foreach (var line in lines.OrderBy(it => it.MaterialId.Value))
            {
                var materialId = line.MaterialId.Value;
                var material = Context.Queryable<JyMaterial>().With(SqlWith.UpdLock)
                    .First(it => it.Id == materialId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (material == null) throw new CustomException($"物料 {line.ItemNo} 已删除，请重新盘点");
                if (material.Status != 0) throw new CustomException($"物料 {material.MaterialCode} 已停用");
                if (material.StockWeight != line.BookQty)
                    throw new CustomException($"物料 {material.MaterialCode} 库存已变化，请重新盘点");

                var actual = NormalizeActual(line.ActualQty, "物料 " + material.MaterialCode);
                if (actual + Epsilon < material.LockedWeight)
                    throw new CustomException($"物料 {material.MaterialCode} 实盘重量不能小于锁定重量 {material.LockedWeight}");
                var diff = actual - material.StockWeight;
                line.ActualQty = actual;
                line.DiffQty = diff;
                line.UnitCost = material.AvgCostPerGram;
                line.DiffAmount = RoundMoney(diff * material.AvgCostPerGram);
                line.ItemStatus = material.Status;

                var updated = Context.Updateable<JyMaterial>().SetColumns(it => it.StockWeight == actual)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == material.Id && it.TenantId == actor.TenantId
                        && it.StockWeight == material.StockWeight && it.DelFlag == 0)
                    .ExecuteCommand();
                if (updated != 1) throw new CustomException($"物料 {material.MaterialCode} 库存已变化，请重新盘点");

                if (diff != 0)
                {
                    Context.Insertable(new JyMaterialLedger
                    {
                        TenantId = actor.TenantId, StoreId = header.StoreId,
                        LedgerNo = LedgerNo(header.StocktakeNo, material.Id),
                        MaterialId = material.Id, MaterialCode = material.MaterialCode, ChangeType = 10,
                        ChangeWeight = diff, BeforeWeight = material.StockWeight, AfterWeight = actual,
                        UnitCost = material.AvgCostPerGram, Amount = line.DiffAmount,
                        BizType = "stocktake", BizId = header.Id, BizNo = header.StocktakeNo,
                        BizTime = header.TakeTime, OperatorUserId = actor.UserId,
                        Create_by = actor.UserName, Create_time = now,
                        Remark = string.IsNullOrWhiteSpace(line.Remark) ? header.Remark : line.Remark
                    }).ExecuteCommand();
                }
            }
        }

        private List<JyStocktakeLine> BuildGoodsLines(string tenantId, long stocktakeId, List<JyStocktakeLineEditDto> input)
        {
            var ids = input.Select(it => it.GoodsItemId).Where(it => it > 0).Distinct().ToList();
            if (ids.Count != input.Count) throw new CustomException("货品不能为空且同一货品只能出现一次");
            var goods = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && ids.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id);
            if (goods.Count != ids.Count) throw new CustomException("部分货品不存在或已删除");
            return input.Select(line =>
            {
                if (!goods.TryGetValue(line.GoodsItemId, out var item)) throw new CustomException("货品不存在");
                if (item.Status != 1 && item.Status != 2 && item.Status != 5)
                    throw new CustomException($"货品 {item.GoodsNo} 当前状态不能盘点");
                var actual = NormalizeActual(line.ActualQty, "货品 " + item.GoodsNo);
                if (item.ItemMode == 1 && actual != 0 && actual != 1)
                    throw new CustomException($"单件货品 {item.GoodsNo} 的实盘数量只能为 0 或 1");
                if (actual + Epsilon < item.LockedQty)
                    throw new CustomException($"货品 {item.GoodsNo} 实盘数量不能小于锁定数量 {item.LockedQty}");
                // 商品数量固定是件数；Unit 只是业务属性，不能当数量乘数使用。
                return new JyStocktakeLine
                {
                    TenantId = tenantId, StocktakeId = stocktakeId, ItemType = TakeTypeGoods,
                    GoodsItemId = item.Id, ItemNo = item.GoodsNo, ItemName = item.StyleName,
                    Unit = item.Unit, BookQty = item.Qty, ActualQty = actual,
                    DiffQty = actual - item.Qty, LockedQty = item.LockedQty, UnitCost = item.CostPrice,
                    DiffAmount = RoundMoney((actual - item.Qty) * item.CostPrice), ItemStatus = item.Status,
                    CategoryKey = line.CategoryKey, CategoryName = line.CategoryName,
                    Remark = TrimLineRemark(line.Remark), DelFlag = 0
                };
            }).ToList();
        }

        private List<JyStocktakeLine> BuildMaterialLines(string tenantId, long stocktakeId, List<JyStocktakeLineEditDto> input)
        {
            var ids = input.Select(it => it.MaterialId).Where(it => it > 0).Distinct().ToList();
            if (ids.Count != input.Count) throw new CustomException("物料不能为空且同一物料只能出现一次");
            var materials = Context.Queryable<JyMaterial>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && ids.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id);
            if (materials.Count != ids.Count) throw new CustomException("部分物料不存在或已删除");
            return input.Select(line =>
            {
                if (!materials.TryGetValue(line.MaterialId, out var item)) throw new CustomException("物料不存在");
                if (item.Status != 0) throw new CustomException($"物料 {item.MaterialCode} 已停用");
                var actual = NormalizeActual(line.ActualQty, "物料 " + item.MaterialCode);
                if (actual + Epsilon < item.LockedWeight)
                    throw new CustomException($"物料 {item.MaterialCode} 实盘重量不能小于锁定重量 {item.LockedWeight}");
                return new JyStocktakeLine
                {
                    TenantId = tenantId, StocktakeId = stocktakeId, ItemType = TakeTypeMaterial,
                    MaterialId = item.Id, ItemNo = item.MaterialCode, ItemName = item.MaterialName,
                    Unit = "g", BookQty = item.StockWeight, ActualQty = actual,
                    DiffQty = actual - item.StockWeight, LockedQty = item.LockedWeight,
                    UnitCost = item.AvgCostPerGram,
                    DiffAmount = RoundMoney((actual - item.StockWeight) * item.AvgCostPerGram),
                    CategoryKey = line.CategoryKey, CategoryName = line.CategoryName,
                    ItemStatus = item.Status, Remark = TrimLineRemark(line.Remark), DelFlag = 0
                };
            }).ToList();
        }

        private List<JyStocktakeLine> SnapshotGoods(string tenantId, long stocktakeId, DateTime now)
        {
            var rows = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0
                    && it.Qty > 0 && (it.Status == 1 || it.Status == 2))
                .OrderBy(it => it.StyleName).OrderBy(it => it.GoodsNo).ToList();
            var styleItems = StyleDictionary(tenantId);
            return rows.Select((item, index) =>
            {
                var materialGroup = NormalizeMaterialGroup(item.MaterialGroup);
                JyLookupItem style = null;
                if (item.StyleItemId.HasValue) styleItems.TryGetValue(item.StyleItemId.Value, out style);
                var categoryName = style?.ItemName ?? item.StyleItemName ?? item.StyleName ?? "未分类";
                return new JyStocktakeLine
                {
                    TenantId = tenantId, StocktakeId = stocktakeId, SortNo = (index + 1) * 10,
                    ItemType = TakeTypeGoods, GoodsItemId = item.Id, ItemNo = item.GoodsNo,
                    ItemName = item.StyleName, Barcode = item.Barcode, StyleName = item.StyleItemName,
                    MetalName = item.MetalName, ItemMode = item.ItemMode, Unit = item.Unit,
                    BookQty = item.Qty, ActualQty = item.Qty,
                    DiffQty = 0, LockedQty = item.LockedQty, UnitCost = item.CostPrice, DiffAmount = 0,
                    ItemStatus = item.Status,
                    CategoryKey = style == null ? $"m{materialGroup}:uncategorized" : $"m{materialGroup}:s{style.Id}",
                    CategoryName = style == null ? "未分类" : categoryName,
                    DelFlag = 0, Create_time = now
                };
            }).ToList();
        }

        private Dictionary<long, JyLookupItem> StyleDictionary(string tenantId)
        {
            var typeIds = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == tenantId && it.TypeCode == "style" && it.DelFlag == 0)
                .Select(it => it.Id).ToList();
            return typeIds.Count == 0 ? new Dictionary<long, JyLookupItem>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && typeIds.Contains(it.TypeId))
                .ToList().ToDictionary(it => it.Id);
        }

        private static int NormalizeMaterialGroup(int materialGroup)
            => materialGroup is >= 1 and <= 3 ? materialGroup : 3;

        private static string MaterialGroupName(int materialGroup)
            => NormalizeMaterialGroup(materialGroup) switch
            {
                1 => "足金类",
                2 => "足银类",
                _ => "其他类"
            };

        private List<JyStocktakeLine> SnapshotMaterials(string tenantId, long stocktakeId, DateTime now)
        {
            var rows = Context.Queryable<JyMaterial>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.CategoryItemId).OrderBy(it => it.MaterialCode).ToList();
            var lookupIds = rows.Where(it => it.CategoryItemId.HasValue).Select(it => it.CategoryItemId.Value)
                .Distinct().ToList();
            var categories = lookupIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && lookupIds.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id, it => it.ItemName);
            return rows.Select((item, index) =>
            {
                var key = item.CategoryItemId?.ToString() ?? "uncategorized";
                var name = item.CategoryItemId.HasValue
                    ? categories.GetValueOrDefault(item.CategoryItemId.Value) ?? "未分类"
                    : "未分类";
                return new JyStocktakeLine
                {
                    TenantId = tenantId, StocktakeId = stocktakeId, SortNo = (index + 1) * 10,
                    ItemType = TakeTypeMaterial, MaterialId = item.Id, ItemNo = item.MaterialCode,
                    ItemName = item.MaterialName, Unit = "g", ItemMode = 2, BookQty = item.StockWeight,
                    ActualQty = item.StockWeight, DiffQty = 0, LockedQty = item.LockedWeight,
                    UnitCost = item.AvgCostPerGram, DiffAmount = 0, ItemStatus = item.Status,
                    CategoryKey = key, CategoryName = name, DelFlag = 0, Create_time = now
                };
            }).ToList();
        }

        private List<JyStocktakeLine> UpdateSnapshotLines(JyActor actor, JyStocktake header, List<JyStocktakeLineEditDto> input)
        {
            var current = Context.Queryable<JyStocktakeLine>()
                .Where(it => it.TenantId == actor.TenantId && it.StocktakeId == header.Id && it.DelFlag == 0)
                .ToList();
            var currentById = current.ToDictionary(it => it.Id);
            if (input.Any(it => it.Id > 0 && !currentById.ContainsKey(it.Id)))
                throw new CustomException("盘点明细已变化，请刷新后重试");

            var normalized = new List<JyStocktakeLine>();
            foreach (var line in input)
            {
                if (line.Id <= 0) throw new CustomException("盘点明细必须来自新建时的库存快照，不能手工追加");
                var row = currentById[line.Id];
                var actual = NormalizeActual(line.ActualQty, "明细 " + row.ItemNo);
                if (header.TakeType == TakeTypeGoods && row.GoodsItemId.HasValue)
                {
                    var itemMode = Context.Queryable<JyGoodsItem>()
                        .Where(it => it.Id == row.GoodsItemId.Value && it.TenantId == actor.TenantId)
                        .Select(it => it.ItemMode).First();
                    if (itemMode == 1 && actual != 0 && actual != 1)
                        throw new CustomException($"单件货品 {row.ItemNo} 的实盘件数只能为 0 或 1");
                }
                if (actual + Epsilon < row.LockedQty)
                    throw new CustomException($"明细 {row.ItemNo} 实盘数量不能小于锁定数量 {row.LockedQty}");
                row.ActualQty = actual;
                row.DiffQty = actual - row.BookQty;
                row.DiffAmount = RoundMoney(row.DiffQty * row.UnitCost);
                row.Remark = TrimLineRemark(line.Remark);
                normalized.Add(row);
            }
            if (normalized.Count != current.Count)
                throw new CustomException("盘点快照必须全部保留，不能删除明细；请按分类完成或填写未完成原因");
            return normalized;
        }

        /// <summary>草稿保存时写回实盘结果；任一明细未更新则整单回滚。</summary>
        private void PersistSnapshotLines(JyActor actor, List<JyStocktakeLine> lines)
        {
            foreach (var chunk in lines.Chunk(500))
            {
                var rows = chunk.ToList();
                var affected = Context.Updateable(rows)
                    .UpdateColumns(it => new { it.ActualQty, it.DiffQty, it.DiffAmount, it.Remark })
                    .WhereColumns(it => new { it.Id, it.TenantId })
                    .ExecuteCommand();
                if (affected != rows.Count)
                    throw new CustomException("盘点明细保存失败，请刷新后重试");
            }
        }

        private void InsertStocktakeLines(List<JyStocktakeLine> lines)
        {
            foreach (var chunk in lines.Chunk(500))
            {
                Context.Insertable(chunk.ToList()).ExecuteCommand();
            }
        }

        private List<JyStocktakeCategory> BuildCategoryRows(JyActor actor, JyStocktake header, List<JyStocktakeLine> lines, DateTime now)
        {
            var parentMap = header.TakeType == TakeTypeGoods
                ? LoadStyleParents(actor.TenantId, lines.Select(it => it.CategoryKey))
                : new Dictionary<string, (string Key, string Name)>();
            return lines.GroupBy(it => it.CategoryKey ?? "uncategorized")
                .OrderBy(it => it.Min(x => x.SortNo)).Select(group =>
                {
                    var parent = parentMap.GetValueOrDefault(group.Key);
                    return new JyStocktakeCategory
                    {
                        TenantId = actor.TenantId, StocktakeId = header.Id, ItemType = header.TakeType,
                        CategoryKey = group.Key,
                        CategoryName = group.Select(it => it.CategoryName).FirstOrDefault(it => !string.IsNullOrWhiteSpace(it)) ?? "未分类",
                        ParentCategoryKey = parent.Key,
                        ParentCategoryName = parent.Name,
                        ItemCount = group.Count(), IsCompleted = 0,
                        Update_by = actor.UserName, Update_time = now
                    };
                }).ToList();
        }

        /// <summary>
        /// 新快照使用 m{材质}:s{款式}，兼容历史纯款式 ID。父级也带材质，
        /// 因此同名的款式大类可以按材质分别统计和完成。
        /// </summary>
        private Dictionary<string, (string Key, string Name)> LoadStyleParents(string tenantId, IEnumerable<string> categoryKeys)
        {
            var result = new Dictionary<string, (string Key, string Name)>();
            var parsed = (categoryKeys ?? Enumerable.Empty<string>()).Where(it => !string.IsNullOrWhiteSpace(it))
                .Distinct().Select(key =>
                {
                    var (materialGroup, styleId) = ParseCategoryKey(key);
                    return new { Key = key, MaterialGroup = materialGroup, StyleId = styleId };
                }).ToList();
            var ids = parsed.Where(it => it.StyleId.HasValue).Select(it => it.StyleId.Value).Distinct().ToList();
            foreach (var chunk in ids.Chunk(200))
            {
                var items = Context.Queryable<JyLookupItem>()
                    .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && chunk.Contains(it.Id)).ToList();
                var parentIds = items.Where(it => it.ParentId.HasValue).Select(it => it.ParentId.Value).Distinct().ToList();
                var parents = parentIds.Count == 0 ? new Dictionary<long, JyLookupItem>() : Context.Queryable<JyLookupItem>()
                    .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && parentIds.Contains(it.Id)).ToList()
                    .ToDictionary(it => it.Id);
                foreach (var item in items)
                {
                    var parent = item.ParentId.HasValue ? parents.GetValueOrDefault(item.ParentId.Value) : null;
                    var parentId = parent?.Id ?? item.Id;
                    var parentName = parent?.ItemName ?? item.ItemName ?? "未分类";
                    foreach (var key in parsed.Where(it => it.StyleId == item.Id))
                    {
                        result[key.Key] = key.MaterialGroup.HasValue
                            ? ($"m{key.MaterialGroup}:p{parentId}", parentName)
                            : ($"parent-{parentId}", parentName);
                    }
                }
            }
            foreach (var key in parsed.Where(it => !it.StyleId.HasValue && it.MaterialGroup.HasValue))
                result[key.Key] = ($"m{key.MaterialGroup}:p0", "未分类");
            return result;
        }

        private static (int? MaterialGroup, long? StyleId) ParseCategoryKey(string key)
        {
            var value = (key ?? string.Empty).Trim();
            if (value.StartsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                var parts = value.Split(':', 2);
                if (parts.Length == 2 && int.TryParse(parts[0][1..], out var materialGroup))
                {
                    var stylePart = parts[1];
                    if (stylePart.StartsWith("s", StringComparison.OrdinalIgnoreCase)
                        && long.TryParse(stylePart[1..], out var styleId))
                        return (materialGroup, styleId);
                    return (materialGroup, null);
                }
            }
            return long.TryParse(value, out var legacyId) ? (null, legacyId) : (null, null);
        }

        private void UpdateStocktakeLineCategories(JyActor actor, long stocktakeId, List<JyStocktakeLine> lines)
        {
            foreach (var chunk in lines.Chunk(500))
            {
                foreach (var row in chunk)
                {
                    Context.Updateable<JyStocktakeLine>()
                        .SetColumns(it => it.CategoryKey == row.CategoryKey)
                        .SetColumns(it => it.CategoryName == row.CategoryName)
                        .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId && it.StocktakeId == stocktakeId)
                        .ExecuteCommand();
                }
            }
        }

        private void SaveCategories(JyActor actor, JyStocktake header, List<JyStocktakeLine> lines, List<JyStocktakeCategoryEditDto> input, DateTime now)
        {
            var existing = Context.Queryable<JyStocktakeCategory>()
                .Where(it => it.TenantId == actor.TenantId && it.StocktakeId == header.Id).ToList();
            if (existing.Count == 0)
            {
                existing = BuildCategoryRows(actor, header, lines, now);
            }
            var inputByKey = (input ?? new List<JyStocktakeCategoryEditDto>())
                .Where(it => !string.IsNullOrWhiteSpace(it.CategoryKey))
                .GroupBy(it => it.CategoryKey).ToDictionary(it => it.Key, it => it.First());
            var lineGroup = lines.GroupBy(it => it.CategoryKey ?? "uncategorized").ToDictionary(it => it.Key, it => it.ToList());
            var parentMap = header.TakeType == TakeTypeGoods
                ? LoadStyleParents(actor.TenantId, lines.Select(it => it.CategoryKey))
                : new Dictionary<string, (string Key, string Name)>();
            foreach (var category in existing)
            {
                var edit = inputByKey.GetValueOrDefault(category.CategoryKey);
                if (string.IsNullOrWhiteSpace(category.ParentCategoryKey) && parentMap.TryGetValue(category.CategoryKey, out var parent))
                {
                    category.ParentCategoryKey = parent.Key;
                    category.ParentCategoryName = parent.Name;
                }
                category.ItemCount = lineGroup.TryGetValue(category.CategoryKey, out var group) ? group.Count : 0;
                category.IsCompleted = edit?.IsCompleted == true ? 1 : 0;
                category.IncompleteReason = TrimCategoryReason(edit?.IncompleteReason);
                category.AnomalyRemark = TrimCategoryReason(edit?.AnomalyRemark);
                category.Update_by = actor.UserName;
                category.Update_time = now;
            }
            SaveCategoryRows(actor, header, existing);
        }

        private void SaveCategoryRows(JyActor actor, JyStocktake header, List<JyStocktakeCategory> rows)
        {
            Context.Deleteable<JyStocktakeCategory>()
                .Where(it => it.TenantId == actor.TenantId && it.StocktakeId == header.Id).ExecuteCommand();
            foreach (var chunk in rows.Chunk(200))
            {
                Context.Insertable(chunk.ToList()).ExecuteCommand();
            }
        }

        private List<JyStocktakeCategoryDto> CategoryDtos(string tenantId, long stocktakeId, List<JyStocktakeLineDto> lines)
        {
            var rows = Context.Queryable<JyStocktakeCategory>()
                .Where(it => it.TenantId == tenantId && it.StocktakeId == stocktakeId)
                .OrderBy(it => it.Id).ToList();
            if (rows.Count == 0)
            {
                var groups = lines.GroupBy(it => new { Key = it.CategoryKey ?? "uncategorized", Name = it.CategoryName ?? "未分类" });
                return groups.Select(it => new JyStocktakeCategoryDto
                {
                    CategoryKey = it.Key.Key, CategoryName = it.Key.Name,
                    ParentCategoryKey = "parent-" + it.Key.Key, ParentCategoryName = it.Key.Name,
                    ItemType = it.First().ItemType, ItemCount = it.Count(), CountedCount = it.Count(),
                    BookQty = it.Sum(line => line.BookQty), ActualQty = it.Sum(line => line.ActualQty),
                    DiffQty = it.Sum(line => line.DiffQty), IsCompleted = true
                }).ToList();
            }
            var lineMap = lines.GroupBy(it => it.CategoryKey ?? "uncategorized").ToDictionary(it => it.Key, it => it.ToList());
            var parentMap = rows.Any(it => it.ItemType == TakeTypeGoods && string.IsNullOrWhiteSpace(it.ParentCategoryKey))
                ? LoadStyleParents(tenantId, rows.Select(it => it.CategoryKey))
                : new Dictionary<string, (string Key, string Name)>();
            return rows.Where(it => lineMap.TryGetValue(it.CategoryKey ?? "uncategorized", out var lines) && lines.Count > 0).Select(it =>
            {
                var group = lineMap[it.CategoryKey ?? "uncategorized"];
                var parent = parentMap.GetValueOrDefault(it.CategoryKey);
                var parentKey = !string.IsNullOrWhiteSpace(it.ParentCategoryKey) ? it.ParentCategoryKey
                    : !string.IsNullOrWhiteSpace(parent.Key) ? parent.Key : "parent-" + it.CategoryKey;
                var parentName = !string.IsNullOrWhiteSpace(it.ParentCategoryName) ? it.ParentCategoryName
                    : !string.IsNullOrWhiteSpace(parent.Name) ? parent.Name : it.CategoryName;
                return new JyStocktakeCategoryDto
                {
                    CategoryKey = it.CategoryKey, CategoryName = it.CategoryName,
                    ParentCategoryKey = parentKey, ParentCategoryName = parentName,
                    MaterialGroup = group.Count > 0 ? group[0].MaterialGroup : MaterialGroupFromKey(it.CategoryKey),
                    MaterialGroupName = group.Count > 0 ? group[0].MaterialGroupName : MaterialGroupName(MaterialGroupFromKey(it.CategoryKey)),
                    ItemType = it.ItemType, ItemCount = it.ItemCount, CountedCount = group.Count,
                    BookQty = group.Sum(line => line.BookQty), ActualQty = group.Sum(line => line.ActualQty),
                    DiffQty = group.Sum(line => line.DiffQty),
                    IsCompleted = it.IsCompleted == 1, IncompleteReason = it.IncompleteReason,
                    AnomalyRemark = it.AnomalyRemark
                };
            }).ToList();
        }

        private static int MaterialGroupFromKey(string key)
        {
            var parsed = ParseCategoryKey(key);
            return NormalizeMaterialGroup(parsed.MaterialGroup ?? 3);
        }

        private List<JyStocktakeLineDto> LineDtos(string tenantId, long stocktakeId)
        {
            var rows = Context.Queryable<JyStocktakeLine>()
                .Where(it => it.TenantId == tenantId && it.StocktakeId == stocktakeId && it.DelFlag == 0)
                .OrderBy(it => it.SortNo).ToList();
            var goodsIds = rows.Where(it => it.GoodsItemId.HasValue).Select(it => it.GoodsItemId.Value).ToList();
            var goods = goodsIds.Count == 0 ? new Dictionary<long, JyGoodsItem>() : Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == tenantId && goodsIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            return rows.Select(it =>
            {
                var dto = ToLineDto(it);
                if (it.GoodsItemId.HasValue && goods.TryGetValue(it.GoodsItemId.Value, out var item))
                {
                    dto.Barcode ??= item.Barcode;
                    dto.StyleName ??= item.StyleItemName;
                    dto.MetalName ??= item.MetalName;
                    dto.Weight = item.Weight;
                    dto.MaterialGroup = NormalizeMaterialGroup(item.MaterialGroup);
                    dto.MaterialGroupName = MaterialGroupName(dto.MaterialGroup);
                }
                return dto;
            }).ToList();
        }

        private static JyStocktakeLineDto ToLineDto(JyStocktakeLine it)
        {
            return new JyStocktakeLineDto
            {
                Id = it.Id, StocktakeId = it.StocktakeId, SortNo = it.SortNo, ItemType = it.ItemType, GoodsItemId = it.GoodsItemId,
                MaterialId = it.MaterialId, ItemNo = it.ItemNo, ItemName = it.ItemName, Unit = it.Unit,
                Barcode = it.Barcode, StyleName = it.StyleName, MetalName = it.MetalName, ItemMode = it.ItemMode,
                CategoryKey = it.CategoryKey, CategoryName = it.CategoryName,
                BookQty = it.BookQty, ActualQty = it.ActualQty, DiffQty = it.DiffQty,
                LockedQty = it.LockedQty, UnitCost = it.UnitCost, DiffAmount = it.DiffAmount,
                ItemStatus = it.ItemStatus, Remark = it.Remark,
                StatusText = it.ItemStatus switch { 1 => "在库", 2 => "锁定", 5 => "盘亏", _ => null },
                DifferenceText = Math.Abs(it.DiffQty) <= Epsilon ? "账实相符" : (it.DiffQty > 0 ? "盘盈" : "盘亏")
            };
        }

        private static JyStocktakeDto ToDto(JyStocktake it)
        {
            return new JyStocktakeDto
            {
                Id = it.Id, StoreId = it.StoreId, StocktakeNo = it.StocktakeNo, TakeType = it.TakeType,
                TakeTypeText = it.TakeType == TakeTypeGoods ? "正品盘点" : "物料盘点",
                TakeTime = it.TakeTime, Status = it.Status,
                StatusText = it.Status == 0 ? "待确认" : it.Status == 1 ? "已确认" : "已作废",
                LineCount = it.LineCount, DiffLineCount = it.DiffLineCount,
                GoodsDiffQty = it.GoodsDiffQty, MaterialDiffWeight = it.MaterialDiffWeight,
                GainAmount = it.GainAmount, LossAmount = it.LossAmount, DiffAmount = it.DiffAmount,
                DifferenceText = it.DiffLineCount == 0 ? "账实相符" : (it.DiffAmount > 0 ? "净盘盈" : it.DiffAmount < 0 ? "净盘亏" : "有差异"),
                Remark = it.Remark, CreateBy = it.Create_by, CreateTime = it.Create_time,
                ConfirmTime = it.ConfirmTime
            };
        }

        private JyStocktake Must(string tenantId, long id)
        {
            return Context.Queryable<JyStocktake>().First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("盘点单不存在");
        }

        private static decimal NormalizeActual(decimal actual, string label)
        {
            if (actual < 0) throw new CustomException($"{label}实盘数量不能为负数");
            if (actual > 1000000m) throw new CustomException($"{label}实盘数量明显异常");
            return decimal.Round(actual, 4, MidpointRounding.AwayFromZero);
        }

        private static decimal RoundMoney(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string LedgerNo(string stocktakeNo, long itemId)
        {
            var suffix = "-PD-" + itemId;
            return stocktakeNo.Length + suffix.Length <= 32
                ? stocktakeNo + suffix
                : stocktakeNo.Substring(0, 32 - suffix.Length) + suffix;
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

        private static string TrimCategoryReason(string reason)
        {
            reason = reason?.Trim();
            if (string.IsNullOrEmpty(reason)) return null;
            if (reason.Length > 200) throw new CustomException("分类未完成原因不能超过 200 个字");
            return reason;
        }

        /// <summary>正品一级分类账实不一致时必须说明原因；物料仍按二级分类盘点。</summary>
        private static void EnsureParentAnomalyRemarks(List<JyStocktakeCategoryEditDto> categories,
            List<JyStocktakeLine> lines, int takeType)
        {
            if (takeType != TakeTypeGoods) return;
            var categoryMap = (categories ?? new List<JyStocktakeCategoryEditDto>())
                .Where(it => !string.IsNullOrWhiteSpace(it.CategoryKey))
                .GroupBy(it => it.CategoryKey).ToDictionary(it => it.Key, it => it.First());
            var parentGroups = lines.GroupBy(line =>
            {
                categoryMap.TryGetValue(line.CategoryKey ?? string.Empty, out var category);
                var key = category?.ParentCategoryKey;
                if (string.IsNullOrWhiteSpace(key)) key = "parent-" + (line.CategoryKey ?? "uncategorized");
                var name = category?.ParentCategoryName;
                if (string.IsNullOrWhiteSpace(name)) name = line.CategoryName ?? "未分类";
                return new { Key = key, Name = name };
            });
            foreach (var group in parentGroups)
            {
                var diff = group.Sum(line => line.DiffQty);
                if (Math.Abs(diff) <= Epsilon) continue;
                var remarks = group.Select(line => categoryMap.GetValueOrDefault(line.CategoryKey ?? string.Empty)?.AnomalyRemark)
                    .Where(it => !string.IsNullOrWhiteSpace(it)).ToList();
                if (remarks.Count == 0 && group.Any(line => categoryMap.GetValueOrDefault(line.CategoryKey ?? string.Empty)?.IsCompleted == true))
                    throw new CustomException($"一级分类「{group.Key.Name}」账实不一致，请填写异常备注");
            }
        }
    }
}
