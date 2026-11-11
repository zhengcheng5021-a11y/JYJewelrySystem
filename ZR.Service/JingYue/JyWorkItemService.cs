using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using System.Text.Json;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;
using ZR.Model.System;

namespace ZR.Service.JingYue
{
    /// <summary>
    /// 待办与目标工作台。自动指标按目标区间做全量快照重算，重复业务事件不会重复累计。
    /// </summary>
    [AppService]
    public class JyWorkItemService : BaseService<JyWorkItem>
    {
        private const string MetricSales = "sales_net_amount";
        private const string MetricCategory = "category_sales_amount";
        private const string MetricCategoryQty = "category_sales_qty";
        private const string MetricFollow = "customer_follow_count";
        private const string MetricDeal = "customer_deal_count";
        private const string MetricCustom = "custom_value";
        private readonly JyLookupService _lookup;

        public JyWorkItemService(JyLookupService lookup)
        {
            _lookup = lookup;
        }

        public PagedInfo<JyWorkItemDto> List(JyActor actor, JyWorkItemQueryDto query)
        {
            query ??= new JyWorkItemQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            if (query.BeginTime.HasValue && query.EndTime.HasValue && query.BeginTime.Value.Date > query.EndTime.Value.Date)
                throw new CustomException("开始日期不能晚于结束日期");

            var now = DateTime.Now;
            var today = now.Date;
            var weekEnd = today.AddDays(7);
            var keyword = query.Keyword?.Trim();
            var view = (query.View ?? "all").Trim().ToLowerInvariant();
            // 父目标不能先被自身字段过滤掉，否则“仅子目标命中高优先级/关键字”的目标组会消失。
            // 组内筛选必须针对同一候选节点判断，避免不同子目标分别命中不同条件造成误判。
            var parents = Context.Queryable<JyWorkItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ParentId == null)
                .ToList();
            var allChildren = Context.Queryable<JyWorkItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ParentId != null).ToList();
            var childMap = allChildren.GroupBy(it => it.ParentId.Value).ToDictionary(g => g.Key, g => g.ToList());
            bool MatchesFilters(JyWorkItem c) =>
                (query.Status is not (0 or 1) || c.Status == query.Status)
                && (query.Priority is not (>= 1 and <= 4) || c.Priority == query.Priority)
                && (query.ItemKind is not (>= 1 and <= 6) || c.ItemKind == query.ItemKind)
                && (query.OwnerUserId is not > 0 || c.OwnerUserId == query.OwnerUserId)
                && (string.IsNullOrEmpty(keyword) || (c.Title ?? string.Empty).Contains(keyword) || (c.Remark ?? string.Empty).Contains(keyword))
                && (!query.BeginTime.HasValue || c.EndDate >= query.BeginTime.Value.Date)
                && (!query.EndTime.HasValue || c.StartDate < query.EndTime.Value.Date.AddDays(1));
            bool MatchesView(JyWorkItem c) => view switch
            {
                "today" => c.Status == 0 && IsDueToday(c, today),
                "week" => c.Status == 0 && c.HasDateRange == 1 && ((c.DueTime >= today && c.DueTime < weekEnd) || (c.DueTime == null && c.EndDate >= today && c.EndDate < weekEnd)),
                "overdue" => c.Status == 0 && IsOverdue(c, now),
                "done" => c.Status == 1,
                "high" => c.Status == 0 && c.Priority == 4,
                _ => true
            };
            bool Match(JyWorkItem it)
            {
                var children = childMap.GetValueOrDefault(it.Id) ?? new List<JyWorkItem>();
                if (children.Count == 0) return MatchesFilters(it) && MatchesView(it);

                // 父目标字段命中筛选时带出整组；否则必须由同一个子目标同时命中筛选与视图条件。
                var filtersMatched = MatchesFilters(it) || children.Any(MatchesFilters);
                if (!filtersMatched) return false;

                if (view == "done") return children.All(c => c.Status == 1);
                if (view == "all") return true;

                return children.Any(c => MatchesFilters(c) && MatchesView(c));
            }
            var matched = parents.Where(Match).ToList();
            var ordered = matched.ToList();
            ordered.Sort((a, b) => CompareGroups(a, b, childMap, now, today, weekEnd));
            var total = ordered.Count;
            var rows = ordered.Skip((page - 1) * size).Take(size).ToList();
            var ids = rows.Select(it => it.Id).ToList();
            var children = ids.SelectMany(id => childMap.GetValueOrDefault(id) ?? new List<JyWorkItem>()).ToList();
            children.Sort((a, b) => CompareItems(a, b, now, today, weekEnd));
            var allIds = ids.Concat(children.Select(it => it.Id)).ToList();
            var scopes = allIds.Count == 0 ? new List<JyWorkItemScope>() : Context.Queryable<JyWorkItemScope>()
                .Where(it => it.TenantId == actor.TenantId && allIds.Contains(it.WorkItemId)).ToList();
            return new PagedInfo<JyWorkItemDto> { PageIndex = page, PageSize = size, TotalNum = total, Result = ToDtos(rows, children, scopes) };
        }
        public JyWorkItemDto Detail(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            var now = DateTime.Now;
            var today = now.Date;
            var weekEnd = today.AddDays(7);
            var children = Context.Queryable<JyWorkItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ParentId == id)
                .ToList();
            children.Sort((a, b) => CompareItems(a, b, now, today, weekEnd));
            var allIds = children.Select(it => it.Id).Append(id).ToList();
            var scopes = Context.Queryable<JyWorkItemScope>()
                .Where(it => it.TenantId == actor.TenantId && allIds.Contains(it.WorkItemId)).ToList();
            return ToDtos(new List<JyWorkItem> { row }, children, scopes).First();
        }

        public JyWorkStatsDto Stats(JyActor actor)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            if (today.DayOfWeek == DayOfWeek.Sunday) weekStart = today.AddDays(-6);
            var rows = Context.Queryable<JyWorkItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0).ToList();
            var parentIds = rows.Where(it => it.ParentId == null).Select(it => it.Id).ToHashSet();
            var parentWithChildren = rows.Where(it => it.ParentId.HasValue).Select(it => it.ParentId.Value).ToHashSet();
            // 有子目标时父目标只做汇总，不重复计入可执行事项；独立待办本身计入。
            var leaves = rows.Where(it => it.ParentId.HasValue || !parentWithChildren.Contains(it.Id)).ToList();
            var total = leaves.Count;
            var done = leaves.Count(it => it.Status == 1);
            return new JyWorkStatsDto
            {
                GroupCount = parentIds.Count,
                TotalCount = total,
                OpenCount = leaves.Count(it => it.Status == 0),
                TodayCount = leaves.Count(it => it.Status == 0 && IsDueToday(it, today)),
                OverdueCount = leaves.Count(it => it.Status == 0 && IsOverdue(it, now)),
                HighPriorityCount = leaves.Count(it => it.Status == 0 && it.Priority == 4),
                ImportantUrgentCount = leaves.Count(it => it.Status == 0 && it.Priority == 4),
                ImportantNotUrgentCount = leaves.Count(it => it.Status == 0 && it.Priority == 3),
                NotImportantUrgentCount = leaves.Count(it => it.Status == 0 && it.Priority == 2),
                NotImportantNotUrgentCount = leaves.Count(it => it.Status == 0 && it.Priority == 1),
                DoneThisWeek = leaves.Count(it => it.Status == 1 && it.FinishTime.HasValue && it.FinishTime.Value >= weekStart && it.FinishTime.Value < today.AddDays(1)),
                CompletionRate = total == 0 ? 0 : Math.Round(done * 100m / total, 1, MidpointRounding.AwayFromZero),
                ServerDate = now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        public JyWorkOptionsDto Options(JyActor actor)
        {
            var owners = Context.Queryable<SysUser>()
                .Where(it => it.Status == 0 && it.DelFlag == 0)
                .OrderBy(it => it.UserId)
                .ToList()
                .Select(it => new JyWorkOwnerOptionDto { UserId = it.UserId, NickName = it.NickName, UserName = it.UserName })
                .ToList();
            var categories = _lookup.Options(actor, "style", true).Where(it => it.Level == 1).ToList();
            var members = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.Id, OrderByType.Desc).Take(300).ToList()
                .Select(it => new JyWorkMemberOptionDto { Id = it.Id, MemberNo = it.MemberNo, MemberName = it.MemberName, Phone = it.Phone })
                .ToList();
            var quickActions = QueryQuickActions(actor);
            return new JyWorkOptionsDto { Owners = owners, Categories = categories, Members = members, QuickActions = quickActions };
        }

        public long Save(JyActor actor, JyWorkItemEditDto dto)
        {
            if (dto == null) throw new CustomException("待办内容不能为空");
            dto.Title = TrimText(dto.Title, 200, "标题");
            dto.Remark = TrimText(dto.Remark, 500, "备注", false);
            if (dto.ItemKind is < 1 or > 6) throw new CustomException("待办类型不正确");
            if (dto.Priority is < 1 or > 4) throw new CustomException("四象限优先级不正确");

            var now = DateTime.Now;
            JyWorkItem current = null;
            long? oldParentId = null;
            var hasChildren = false;
            if (dto.Id > 0)
            {
                current = Must(actor.TenantId, dto.Id);
                oldParentId = current.ParentId;
                hasChildren = current.ParentId == null && Context.Queryable<JyWorkItem>()
                    .Any(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ParentId == current.Id);
            }

            var isChild = dto.ParentId.HasValue && dto.ParentId > 0;
            if (hasChildren && isChild) throw new CustomException("已有子目标的事项不能移动到其他目标下，最多支持两层");
            JyWorkItem parent = null;
            if (isChild)
            {
                if (current != null && dto.ParentId.Value == current.Id) throw new CustomException("不能将事项设置为自己的父目标");
                parent = Must(actor.TenantId, dto.ParentId.Value);
                if (parent.ParentId.HasValue) throw new CustomException("待办与目标最多支持两层");
            }

            var inheritedHasDateRange = parent?.HasDateRange ?? 0;
            var hasDateRange = isChild
                ? inheritedHasDateRange
                : (dto.IsPerformanceRelated == 1 || dto.HasDateRange == 1 ? 1 : 0);
            var isPerformanceRelated = isChild
                ? (parent?.IsPerformanceRelated ?? 0)
                : (dto.IsPerformanceRelated == 1 ? 1 : 0);
            if (isPerformanceRelated == 1 && hasDateRange != 1)
                throw new CustomException("关联业绩的待办必须设置时间区间");

            var start = dto.StartDate?.Date ?? current?.StartDate.Date ?? DateTime.Today;
            var end = dto.EndDate?.Date ?? current?.EndDate.Date ?? start;
            if (hasDateRange == 1 && end < start) throw new CustomException("结束日期不能早于开始日期");
            if (hasDateRange == 0) end = start;

            var targetMode = dto.TargetMode is 1 or 2 ? dto.TargetMode : 1;
            if (isChild && isPerformanceRelated == 1 && targetMode == 2)
            {
                if (dto.TargetValue <= 0 || dto.TargetValue != decimal.Truncate(dto.TargetValue))
                    throw new CustomException("件数目标必须输入大于 0 的整数");
            }
            if (isChild && isPerformanceRelated == 1 && targetMode == 1)
            {
                if (dto.TargetValue <= 0) throw new CustomException("金额目标必须大于 0");
                dto.TargetValue = decimal.Round(dto.TargetValue, 2, MidpointRounding.AwayFromZero);
            }

            long? categoryId = null;
            string categoryName = null;
            if (isChild && isPerformanceRelated == 1)
            {
                if (dto.CategoryId is not > 0) throw new CustomException("关联业绩的子目标必须选择款式分类");
                var category = _lookup.MustActiveItem(actor.TenantId, dto.CategoryId.Value, "style", "款式分类");
                if (category.ParentId.HasValue) throw new CustomException("款式分类只能选择父级大类");
                categoryId = category.Id;
                categoryName = category.ItemName;
            }

            DateTime? finishDate = dto.FinishDate?.Date;
            if (isChild && hasDateRange == 1)
            {
                finishDate ??= parent?.EndDate.Date;
                if (finishDate.HasValue && (finishDate.Value < start || finishDate.Value > end))
                    throw new CustomException("子目标完成日期必须在待办事项的时间区间内");
            }

            var progressMode = dto.ProgressMode is >= 1 and <= 3 ? dto.ProgressMode : 1;
            if (hasChildren) progressMode = 3;
            // 关联业绩的子目标必须走自动统计，否则销量变化不会回写进度。
            if (isChild) progressMode = isPerformanceRelated == 1 ? 2 : 1;
            if (progressMode == 3 && !isChild && !hasChildren)
                throw new CustomException("只有带子目标的目标可以设置为子目标汇总");
            var metric = isChild
                ? (isPerformanceRelated == 1 ? (targetMode == 2 ? MetricCategoryQty : MetricCategory) : null)
                : NormalizeMetric(dto.MetricCode, progressMode);
            if (progressMode == 2 && string.IsNullOrEmpty(metric)) throw new CustomException("自动指标目标必须选择统计指标");
            if (progressMode == 2 && metric != MetricCustom && dto.TargetValue <= 0) throw new CustomException("自动统计目标值必须大于 0");
            var scopes = NormalizeScopes(actor, dto.Scopes, metric, progressMode);
            if (isChild && isPerformanceRelated == 1 && categoryId.HasValue && scopes.Count == 0)
                scopes.Add((2, categoryId.Value, categoryName));

            var ownerUserId = dto.OwnerUserId is > 0 ? dto.OwnerUserId : (current?.OwnerUserId ?? actor.UserId);
            var ownerName = !string.IsNullOrWhiteSpace(dto.OwnerName)
                ? ResolveOwnerName(actor, ownerUserId, dto.OwnerName)
                : (current?.OwnerName ?? actor.UserName);

            long id = 0;
            var tran = UseTran(() =>
            {
                if (dto.Id <= 0)
                {
                    if (isChild && parent != null && (parent.ProgressMode != 3 || parent.MetricCode != null))
                    {
                        Context.Updateable<JyWorkItem>()
                            .SetColumns(it => it.ProgressMode == 3)
                            .SetColumns(it => it.MetricCode == null)
                            .SetColumns(it => it.TargetValue == 0)
                            .SetColumns(it => it.Update_by == actor.UserName)
                            .SetColumns(it => it.Update_time == now)
                            .Where(it => it.Id == parent.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                            .ExecuteCommand();
                    }
                    var entity = new JyWorkItem
                    {
                        TenantId = actor.TenantId,
                        StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                        ParentId = isChild ? dto.ParentId : null,
                        ItemNo = NextItemNo(Context, actor.TenantId, now),
                        Title = dto.Title,
                        ItemKind = isChild ? 1 : dto.ItemKind,
                        PeriodType = dto.PeriodType is >= 1 and <= 6 ? dto.PeriodType : 3,
                        StartDate = start,
                        EndDate = end,
                        DueTime = hasDateRange == 1 ? (dto.DueTime ?? end.AddDays(1).AddSeconds(-1)) : null,
                        TargetFinishDate = isChild && hasDateRange == 1 ? finishDate : null,
                        Priority = dto.Priority,
                        OwnerUserId = ownerUserId,
                        OwnerName = ownerName,
                        Status = 0,
                        ProgressMode = progressMode,
                        MetricCode = metric,
                        TargetValue = dto.TargetValue,
                        CurrentValue = progressMode == 1 ? dto.CurrentValue : 0,
                        CurrentAuxValue = progressMode == 1 ? dto.CurrentAuxValue : 0,
                        ProgressPercent = progressMode == 1 ? CalcPercent(dto.CurrentValue, dto.TargetValue) : 0,
                        AutoComplete = dto.AutoComplete == false ? 0 : 1,
                        ReminderTime = hasDateRange == 1 ? dto.ReminderTime : null,
                        SortNo = dto.SortNo,
                        Remark = dto.Remark,
                        DelFlag = 0,
                        Create_by = actor.UserName,
                        Create_time = now,
                        HasDateRange = hasDateRange,
                        IsPerformanceRelated = isPerformanceRelated,
                        TargetMode = targetMode,
                        CategoryId = categoryId,
                        CategoryName = categoryName
                    };
                    id = Context.Insertable(entity).ExecuteReturnBigIdentity();
                }
                else
                {
                    id = current.Id;
                    current.ParentId = isChild ? dto.ParentId : null;
                    current.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
                    current.Title = dto.Title;
                    current.ItemKind = isChild ? 1 : dto.ItemKind;
                    current.PeriodType = dto.PeriodType is >= 1 and <= 6 ? dto.PeriodType : 3;
                    current.StartDate = start;
                    current.EndDate = end;
                    current.DueTime = hasDateRange == 1 ? (dto.DueTime ?? end.AddDays(1).AddSeconds(-1)) : null;
                    current.TargetFinishDate = isChild && hasDateRange == 1 ? finishDate : null;
                    current.Priority = dto.Priority;
                    current.OwnerUserId = ownerUserId;
                    current.OwnerName = ownerName;
                    current.HasDateRange = hasDateRange;
                    current.IsPerformanceRelated = isPerformanceRelated;
                    current.TargetMode = targetMode;
                    current.CategoryId = categoryId;
                    current.CategoryName = categoryName;
                    if (progressMode == 1)
                    {
                        current.ProgressMode = 1;
                        current.MetricCode = null;
                        current.TargetValue = dto.TargetValue;
                        current.CurrentValue = dto.CurrentValue;
                        current.CurrentAuxValue = dto.CurrentAuxValue;
                        current.ProgressPercent = CalcPercent(dto.CurrentValue, dto.TargetValue);
                        current.Status = dto.TargetValue > 0 && dto.CurrentValue >= dto.TargetValue ? 1 : 0;
                        current.FinishTime = current.Status == 1 ? (current.FinishTime ?? now) : null;
                    }
                    else if (progressMode == 2)
                    {
                        current.ProgressMode = 2;
                        current.MetricCode = metric;
                        current.TargetValue = dto.TargetValue;
                    }
                    else
                    {
                        current.ProgressMode = 3;
                        current.MetricCode = null;
                        current.TargetValue = 0;
                    }
                    current.AutoComplete = dto.AutoComplete == false ? 0 : 1;
                    current.ReminderTime = hasDateRange == 1 ? dto.ReminderTime : null;
                    current.SortNo = dto.SortNo;
                    current.Remark = dto.Remark;
                    current.Update_by = actor.UserName;
                    current.Update_time = now;
                    Context.Updateable(current).UpdateColumns(it => new
                    {
                        it.ParentId, it.StoreId, it.Title, it.ItemKind, it.PeriodType, it.StartDate, it.EndDate,
                        it.DueTime, it.TargetFinishDate, it.Priority, it.OwnerUserId, it.OwnerName, it.ProgressMode, it.MetricCode,
                        it.TargetValue, it.CurrentValue, it.CurrentAuxValue, it.ProgressPercent, it.Status,
                        it.FinishTime, it.AutoComplete, it.ReminderTime, it.SortNo, it.Remark, it.Update_by, it.Update_time,
                        it.HasDateRange, it.IsPerformanceRelated, it.TargetMode, it.CategoryId, it.CategoryName
                    }).ExecuteCommand();
                }

                Context.Deleteable<JyWorkItemScope>()
                    .Where(it => it.TenantId == actor.TenantId && it.WorkItemId == id).ExecuteCommand();
                if (scopes.Count > 0)
                {
                    Context.Insertable(scopes.Select(it => new JyWorkItemScope
                    {
                        TenantId = actor.TenantId, WorkItemId = id, ScopeType = it.ScopeType,
                        ScopeId = it.ScopeId, SnapshotName = it.SnapshotName, Create_time = now
                    }).ToList()).ExecuteCommand();
                }
                if (dto.SaveAsTemplate && !isChild)
                {
                    var templateItems = new List<JyWorkTemplateItemDto>();
                    var children = Context.Queryable<JyWorkItem>()
                        .Where(it => it.TenantId == actor.TenantId && it.ParentId == id && it.DelFlag == 0)
                        .OrderBy(it => it.SortNo).OrderBy(it => it.Priority, OrderByType.Desc).OrderBy(it => it.Id).ToList();
                    if (children.Count > 0)
                    {
                        var childIds = children.Select(it => it.Id).ToList();
                        var childScopes = Context.Queryable<JyWorkItemScope>()
                            .Where(it => it.TenantId == actor.TenantId && childIds.Contains(it.WorkItemId)).ToList()
                            .GroupBy(it => it.WorkItemId).ToDictionary(g => g.Key, g => g.ToList());
                        templateItems = children.Select(child =>
                        {
                            var itemScopes = childScopes.GetValueOrDefault(child.Id) ?? new List<JyWorkItemScope>();
                            return new JyWorkTemplateItemDto
                            {
                                Title = child.Title, ItemKind = child.ItemKind, PeriodType = child.PeriodType,
                                Priority = child.Priority, MetricCode = child.MetricCode, TargetValue = child.TargetValue,
                                TargetMode = child.TargetMode, CategoryId = child.CategoryId, CategoryName = child.CategoryName,
                                ScopeJson = itemScopes.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(itemScopes.Select(s => new
                                {
                                    s.ScopeType, s.ScopeId, s.SnapshotName
                                })),
                                Remark = child.Remark
                            };
                        }).ToList();
                    }
                    else
                    {
                        templateItems.Add(new JyWorkTemplateItemDto
                        {
                            Title = dto.Title, ItemKind = dto.ItemKind, PeriodType = dto.PeriodType,
                            Priority = dto.Priority, MetricCode = metric, TargetValue = dto.TargetValue,
                            TargetMode = targetMode, CategoryId = categoryId, CategoryName = categoryName,
                            ScopeJson = scopes.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(scopes.Select(s => new
                            {
                                s.ScopeType, s.ScopeId, s.SnapshotName
                            })),
                            Remark = dto.Remark
                        });
                    }
                    SaveTemplateInternal(actor, new JyWorkTemplateEditDto
                    {
                        TemplateName = string.IsNullOrWhiteSpace(dto.TemplateName) ? dto.Title : dto.TemplateName,
                        Description = dto.Remark,
                        Status = 0,
                        Items = templateItems
                    }, now);
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "待办保存失败");
            if (progressMode == 2) Recalculate(actor, id);
            else if (isChild) RecalculateParent(actor, dto.ParentId.Value);
            if (oldParentId.HasValue && oldParentId != (isChild ? dto.ParentId : null))
                RecalculateParent(actor, oldParentId.Value);
            if (hasChildren) RecalculateParent(actor, id);
            return id;
        }
        public int Delete(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            if (Context.Queryable<JyWorkItem>().Any(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ParentId == id))
                throw new CustomException("请先删除该目标下的子目标");
            var affected = Context.Updateable<JyWorkItem>()
                .SetColumns(it => it.DelFlag == 2).SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
            if (affected > 0 && row.ParentId.HasValue) RecalculateParent(actor, row.ParentId.Value);
            return affected;
        }

        public int Complete(JyActor actor, long id, bool completed = true)
        {
            var row = Must(actor.TenantId, id);
            var children = Context.Queryable<JyWorkItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ParentId == id).ToList();
            var now = DateTime.Now;
            // 完成父事项时级联完成全部子目标；反选父事项时由子目标真实进度重新汇总。
            if (completed && children.Count > 0)
            {
                var childIds = children.Select(it => it.Id).ToList();
                Context.Updateable<JyWorkItem>()
                    .SetColumns(it => it.Status == 1)
                    .SetColumns(it => it.ProgressPercent == 100m)
                    .SetColumns(it => it.FinishTime == now)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && childIds.Contains(it.Id))
                    .ExecuteCommand();
            }
            if (!completed && children.Count > 0)
            {
                // 恢复父事项时同步恢复其子目标，避免父事项已恢复而子目标仍全部完成；
                // 自动统计子目标随后按真实销售数据重算，已达标的仍会保持完成。
                foreach (var child in children)
                {
                    var childPercentValue = child.ProgressMode == 1 ? CalcPercent(child.CurrentValue, child.TargetValue) : child.ProgressPercent;
                    Context.Updateable<JyWorkItem>()
                        .SetColumns(it => it.Status == 0)
                        .SetColumns(it => it.FinishTime == null)
                        .SetColumns(it => it.ProgressPercent == childPercentValue)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == child.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                        .ExecuteCommand();
                    if (child.ProgressMode == 2) RecalculateInternal(actor, child);
                }
                children = Context.Queryable<JyWorkItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ParentId == id).ToList();
                var childPercent = decimal.Round(children.Average(it => Math.Min(100m, it.ProgressPercent)), 2, MidpointRounding.AwayFromZero);
                return Context.Updateable<JyWorkItem>()
                    .SetColumns(it => it.Status == 0)
                    .SetColumns(it => it.FinishTime == null)
                    .SetColumns(it => it.ProgressPercent == childPercent)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
            }
            var status = completed ? 1 : 0;
            // 自动统计目标先刷新真实销售额，再完成；完成状态不回写虚假的 100%。
            if (completed && row.ProgressMode == 2)
            {
                RecalculateInternal(actor, row);
                row = Must(actor.TenantId, id);
            }
            var percent = completed
                ? (row.ProgressMode == 2 ? row.ProgressPercent : 100m)
                : row.ProgressMode == 1 ? CalcPercent(row.CurrentValue, row.TargetValue) : row.ProgressPercent;
            var affected = Context.Updateable<JyWorkItem>()
                .SetColumns(it => it.Status == status)
                .SetColumns(it => it.FinishTime == (completed ? now : null))
                .SetColumns(it => it.ProgressPercent == percent)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
            if (affected > 0)
            {
                if (!completed && row.ProgressMode == 2) RecalculateInternal(actor, Must(actor.TenantId, id));
                if (row.ParentId.HasValue) RecalculateParent(actor, row.ParentId.Value);
                // 父事项级联完成后，重新汇总父事项自身进度与状态。
                else if (completed && children.Count > 0) RecalculateParent(actor, id);
            }
            return affected;
        }

        public int Progress(JyActor actor, long id, JyWorkProgressEditDto dto)
        {
            if (dto == null) throw new CustomException("进度内容不能为空");
            var row = Must(actor.TenantId, id);
            if (row.ProgressMode != 1) throw new CustomException("只有手工目标可以直接修改进度");
            var now = DateTime.Now;
            var percent = CalcPercent(dto.CurrentValue, row.TargetValue);
            var completed = row.AutoComplete == 1 && row.TargetValue > 0 && dto.CurrentValue >= row.TargetValue;
            var tran = UseTran(() =>
            {
                Context.Insertable(new JyWorkItemProgressLog
                {
                    TenantId = actor.TenantId, WorkItemId = row.Id, MetricCode = MetricCustom,
                    BeforeValue = row.CurrentValue, ChangeValue = dto.CurrentValue - row.CurrentValue, AfterValue = dto.CurrentValue,
                    ProgressPercent = percent, SourceType = "manual", BusinessDate = now,
                    UniqueKey = $"MAN:{row.Id}:{now:yyyyMMddHHmmssfff}", Create_time = now,
                    Remark = TrimText(dto.Remark, 500, "备注", false)
                }).ExecuteCommand();
                Context.Updateable<JyWorkItem>()
                    .SetColumns(it => it.CurrentValue == dto.CurrentValue)
                    .SetColumns(it => it.CurrentAuxValue == dto.CurrentAuxValue)
                    .SetColumns(it => it.ProgressPercent == percent)
                    .SetColumns(it => it.Status == (completed ? 1 : 0))
                    .SetColumns(it => it.FinishTime == (completed ? now : null))
                    .SetColumns(it => it.Update_by == actor.UserName).SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "进度保存失败");
            if (row.ParentId.HasValue) RecalculateParent(actor, row.ParentId.Value);
            return 1;
        }

        public int Recalculate(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            RecalculateInternal(actor, row);
            if (row.ParentId.HasValue) RecalculateParent(actor, row.ParentId.Value);
            return 1;
        }

        /// <summary>业务事件发生后安全重算；失败只记录日志，不影响销售/退货主流程。</summary>
        public void TryRecalculateByBusinessDate(JyActor actor, DateTime businessDate)
        {
            try
            {
                var rows = Context.Queryable<JyWorkItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ProgressMode == 2
                        && it.StartDate <= businessDate.Date && it.EndDate >= businessDate.Date).ToList();
                foreach (var row in rows) RecalculateInternal(actor, row);
                foreach (var parentId in rows.Where(it => it.ParentId.HasValue).Select(it => it.ParentId.Value).Distinct())
                    RecalculateParent(actor, parentId);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ConsoleColor.Yellow, $"[待办目标重算] {ex.Message}");
            }
        }

        public void TryRecalculateByWorkItem(JyActor actor, long id)
        {
            try { Recalculate(actor, id); }
            catch (Exception ex) { Log.WriteLine(ConsoleColor.Yellow, $"[待办目标重算] {ex.Message}"); }
        }

        /// <summary>
        /// 客户跟进保存后，按服务器业务日期重算覆盖该日期的客户类自动目标。
        /// 不要求跟进记录先绑定某个目标，避免全库客户目标长期使用旧快照。
        /// </summary>
        public void TryRecalculateCustomerMetricsByBusinessDate(JyActor actor, DateTime businessDate)
        {
            try
            {
                var rows = Context.Queryable<JyWorkItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ProgressMode == 2
                        && (it.MetricCode == MetricFollow || it.MetricCode == MetricDeal)
                        && it.StartDate <= businessDate.Date && it.EndDate >= businessDate.Date).ToList();
                foreach (var row in rows) RecalculateInternal(actor, row);
                foreach (var parentId in rows.Where(it => it.ParentId.HasValue).Select(it => it.ParentId.Value).Distinct())
                    RecalculateParent(actor, parentId);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ConsoleColor.Yellow, $"[客户目标重算] {ex.Message}");
            }
        }

        public JyWorkEvidenceDto Evidence(JyActor actor, long id)
        {
            var item = Detail(actor, id);
            var logs = Context.Queryable<JyWorkItemProgressLog>()
                .Where(it => it.TenantId == actor.TenantId && it.WorkItemId == id)
                .OrderBy(it => it.Create_time, OrderByType.Desc).Take(100).ToList()
                .Select(it => new JyWorkProgressLogDto
                {
                    Id = it.Id, WorkItemId = it.WorkItemId, MetricCode = it.MetricCode, BeforeValue = it.BeforeValue,
                    ChangeValue = it.ChangeValue, AfterValue = it.AfterValue, ProgressPercent = it.ProgressPercent,
                    SourceType = it.SourceType, SourceId = it.SourceId, BusinessDate = it.BusinessDate,
                    Remark = it.Remark, CreateTime = it.Create_time
                }).ToList();
            var followRows = Context.Queryable<JyMemberFollow>()
                .Where(it => it.TenantId == actor.TenantId && it.WorkItemId == id)
                .OrderBy(it => it.FollowTime, OrderByType.Desc).Take(100).ToList();
            var memberIds = followRows.Select(it => it.MemberId).Distinct().ToList();
            var members = memberIds.Count == 0 ? new List<JyMember>() : Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && memberIds.Contains(it.Id)).ToList();
            var map = members.ToDictionary(it => it.Id);
            var follows = followRows.Select(it => new JyMemberFollowDto
            {
                Id = it.Id, MemberId = it.MemberId,
                MemberNo = map.TryGetValue(it.MemberId, out var m) ? m.MemberNo : null,
                MemberName = map.TryGetValue(it.MemberId, out var m2) ? m2.MemberName : null,
                Phone = map.TryGetValue(it.MemberId, out var m3) ? m3.Phone : null,
                FollowTime = it.FollowTime, FollowType = it.FollowType, Content = it.Content, Result = it.Result,
                NextFollowTime = it.NextFollowTime, StaffUserId = it.StaffUserId, WorkItemId = it.WorkItemId, CreateTime = it.Create_time
            }).ToList();
            return new JyWorkEvidenceDto { Item = item, Logs = logs, Follows = follows };
        }

        private void RecalculateInternal(JyActor actor, JyWorkItem row)
        {
            if (row.ProgressMode != 2 || string.IsNullOrWhiteSpace(row.MetricCode)) return;
            var before = row.CurrentValue;
            var aux = row.CurrentAuxValue;
            decimal value;
            switch (row.MetricCode)
            {
                case MetricSales:
                    value = SalesNet(actor, row.StartDate, row.EndDate, null);
                    break;
                case MetricCategory:
                    value = SalesNet(actor, row.StartDate, row.EndDate, LoadScopeIds(actor, row.Id, 2));
                    break;
                case MetricCategoryQty:
                    value = SalesQty(actor, row.StartDate, row.EndDate, LoadScopeIds(actor, row.Id, 2));
                    break;
                case MetricFollow:
                    var scopeMembers = LoadScopeIds(actor, row.Id, 1);
                    var followQuery = Context.Queryable<JyMemberFollow>()
                        .Where(it => it.TenantId == actor.TenantId && it.FollowTime >= row.StartDate && it.FollowTime < row.EndDate.AddDays(1));
                    if (scopeMembers.Count > 0) followQuery = followQuery.Where(it => scopeMembers.Contains(it.MemberId));
                    var followRows = followQuery.ToList();
                    value = followRows.Select(it => it.MemberId).Distinct().Count();
                    aux = followRows.Count;
                    break;
                case MetricDeal:
                    value = DealCustomerCount(actor, row.StartDate, row.EndDate, LoadScopeIds(actor, row.Id, 1));
                    break;
                case MetricCustom:
                    value = row.CurrentValue;
                    break;
                default:
                    return;
            }
            value = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
            // 已手动完成的目标保持完成，后续销售重算只刷新实际值，不把人工确认的结果反向冲掉。
            var completed = row.Status == 1 || (row.AutoComplete == 1 && row.TargetValue > 0 && value >= row.TargetValue);
            var percent = completed ? 100m : CalcPercent(value, row.TargetValue);
            var status = completed ? 1 : 0;
            if (value == before && aux == row.CurrentAuxValue && row.ProgressPercent == percent && row.Status == status) return;
            var now = DateTime.Now;
            // 同一快照状态重复回算会在上方提前返回；真正变化时写入独立流水，避免唯一键阻塞后续变化。
            var key = $"SNAP:{row.Id}:{now:yyyyMMddHHmmssfff}:{value}";
            var tran = UseTran(() =>
            {
                Context.Insertable(new JyWorkItemProgressLog
                {
                    TenantId = actor.TenantId, WorkItemId = row.Id, MetricCode = row.MetricCode,
                    BeforeValue = before, ChangeValue = value - before, AfterValue = value,
                    ProgressPercent = percent, SourceType = "metric_snapshot", BusinessDate = now.Date,
                    UniqueKey = key, Create_time = now,
                    Remark = $"全量重算 {row.StartDate:yyyy-MM-dd} 至 {row.EndDate:yyyy-MM-dd}"
                }).ExecuteCommand();
                Context.Updateable<JyWorkItem>()
                    .SetColumns(it => it.CurrentValue == value)
                    .SetColumns(it => it.CurrentAuxValue == aux)
                    .SetColumns(it => it.ProgressPercent == percent)
                    .SetColumns(it => it.Status == status)
                    .SetColumns(it => it.FinishTime == (status == 1 ? now : null))
                    .SetColumns(it => it.Update_by == actor.UserName).SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
            });
            if (!tran.IsSuccess)
            {
                Context.Updateable<JyWorkItem>()
                    .SetColumns(it => it.CurrentValue == value)
                    .SetColumns(it => it.CurrentAuxValue == aux)
                    .SetColumns(it => it.ProgressPercent == percent)
                    .SetColumns(it => it.Status == status)
                    .SetColumns(it => it.FinishTime == (status == 1 ? now : null))
                    .SetColumns(it => it.Update_by == actor.UserName).SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
            }
        }

        private void RecalculateParent(JyActor actor, long parentId)
        {
            var parent = Context.Queryable<JyWorkItem>().First(it => it.Id == parentId && it.TenantId == actor.TenantId && it.DelFlag == 0);
            if (parent == null) return;
            var children = Context.Queryable<JyWorkItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ParentId == parentId).ToList();
            var percent = children.Count == 0
                ? 0m
                : decimal.Round(children.Average(it => it.Status == 1 ? 100m : Math.Min(100m, it.ProgressPercent)), 2, MidpointRounding.AwayFromZero);
            var done = children.Count(it => it.Status == 1);
            var status = children.Count > 0 && done == children.Count ? 1 : 0;
            var now = DateTime.Now;
            Context.Updateable<JyWorkItem>()
                .SetColumns(it => it.ProgressPercent == percent)
                .SetColumns(it => it.Status == status)
                .SetColumns(it => it.FinishTime == (status == 1 ? (parent.FinishTime ?? now) : null))
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == parentId && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
        }

        private List<long> CategoryScopeIds(JyActor actor, JyWorkItem row)
        {
            var ids = LoadScopeIds(actor, row.Id, 2);
            if (ids.Count == 0 && row.CategoryId is > 0) ids.Add(row.CategoryId.Value);
            return ids;
        }

        /// <summary>销售件数：销售明细数量减退货数量；退货按退货发生月份冲减，不与金额折扣混算。</summary>
        private decimal SalesQty(JyActor actor, DateTime start, DateTime end, List<long> categoryIds)
        {
            var sales = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= start.Date && it.DealTime < end.Date.AddDays(1)).ToList();
            var saleIds = sales.Select(it => it.Id).ToList();
            var lines = saleIds.Count == 0 ? new List<JySaleOrderItem>() : Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && saleIds.Contains(it.OrderId)).ToList();
            if (categoryIds != null && categoryIds.Count > 0)
            {
                var goodsIds = lines.Where(it => it.GoodsItemId > 0).Select(it => it.GoodsItemId).Distinct().ToList();
                var goods = goodsIds.Count == 0 ? new List<JyGoodsItem>() : Context.Queryable<JyGoodsItem>()
                    .Where(it => it.TenantId == actor.TenantId && goodsIds.Contains(it.Id)).ToList();
                var goodsMap = goods.ToDictionary(it => it.Id);
                var styles = Context.Queryable<JyLookupItem>().Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0).ToList();
                var styleMap = styles.ToDictionary(it => it.Id);
                var parentByName = styles.Where(it => it.ParentId == null).GroupBy(it => it.ItemName).ToDictionary(g => g.Key, g => g.Select(it => it.Id).ToList());
                lines = lines.Where(it => ResolveParentCategoryIds(it, goodsMap, styleMap, parentByName).Any(id => categoryIds.Contains(id))).ToList();
            }
            var qty = lines.Sum(it => it.Qty);

            var returns = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 1
                    && it.ReturnTime >= start.Date && it.ReturnTime < end.Date.AddDays(1)).ToList();
            var returnIds = returns.Select(it => it.Id).ToList();
            var returnLines = returnIds.Count == 0 ? new List<JySaleReturnItem>() : Context.Queryable<JySaleReturnItem>()
                .Where(it => it.TenantId == actor.TenantId && returnIds.Contains(it.ReturnId)).ToList();
            if (categoryIds != null && categoryIds.Count > 0 && returnLines.Count > 0)
            {
                var saleLineIds = returnLines.Select(it => it.SaleOrderItemId).Distinct().ToList();
                var saleLines = Context.Queryable<JySaleOrderItem>().Where(it => it.TenantId == actor.TenantId && saleLineIds.Contains(it.Id)).ToList();
                var saleLineMap = saleLines.ToDictionary(it => it.Id);
                var goodsIds = saleLines.Where(it => it.GoodsItemId > 0).Select(it => it.GoodsItemId).Distinct().ToList();
                var goods = goodsIds.Count == 0 ? new List<JyGoodsItem>() : Context.Queryable<JyGoodsItem>().Where(it => it.TenantId == actor.TenantId && goodsIds.Contains(it.Id)).ToList();
                var goodsMap = goods.ToDictionary(it => it.Id);
                var styles = Context.Queryable<JyLookupItem>().Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0).ToList();
                var styleMap = styles.ToDictionary(it => it.Id);
                var parentByName = styles.Where(it => it.ParentId == null).GroupBy(it => it.ItemName).ToDictionary(g => g.Key, g => g.Select(it => it.Id).ToList());
                returnLines = returnLines.Where(it => saleLineMap.TryGetValue(it.SaleOrderItemId, out var saleLine)
                    && ResolveParentCategoryIds(saleLine, goodsMap, styleMap, parentByName).Any(id => categoryIds.Contains(id))).ToList();
            }
            return qty - returnLines.Sum(it => it.Qty);
        }
        private decimal SalesNet(JyActor actor, DateTime start, DateTime end, List<long> categoryIds)
        {
            var saleQuery = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= start.Date && it.DealTime < end.Date.AddDays(1));
            var returnQuery = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 1
                    && it.ReturnTime >= start.Date && it.ReturnTime < end.Date.AddDays(1));
            if (categoryIds == null || categoryIds.Count == 0)
                return saleQuery.Sum(it => it.PayAmount) - returnQuery.Sum(it => it.RefundAmount);

            var sales = saleQuery.ToList();
            var saleIds = sales.Select(it => it.Id).ToList();
            var lines = saleIds.Count == 0 ? new List<JySaleOrderItem>() : Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && saleIds.Contains(it.OrderId)).ToList();
            var allGoodsIds = lines.Where(it => it.GoodsItemId > 0).Select(it => it.GoodsItemId).Distinct().ToList();
            var allGoods = allGoodsIds.Count == 0 ? new List<JyGoodsItem>() : Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && allGoodsIds.Contains(it.Id)).ToList();
            var goodsMap = allGoods.ToDictionary(it => it.Id);
            var styles = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0).ToList();
            var styleMap = styles.ToDictionary(it => it.Id);
            var parentByName = styles.Where(it => it.ParentId == null)
                .GroupBy(it => it.ItemName).ToDictionary(g => g.Key, g => g.Select(it => it.Id).ToList());
            decimal salesAmount = 0;
            foreach (var order in sales)
            {
                var orderLines = lines.Where(it => it.OrderId == order.Id).ToList();
                var total = order.TotalAmount > 0 ? order.TotalAmount : orderLines.Sum(it => it.DealPrice * it.Qty);
                if (total <= 0) continue;
                foreach (var line in orderLines)
                {
                    var parentIds = ResolveParentCategoryIds(line, goodsMap, styleMap, parentByName);
                    if (!parentIds.Any(id => categoryIds.Contains(id))) continue;
                    salesAmount += line.DealPrice * line.Qty * Math.Min(1m, order.PayAmount / total);
                }
            }
            var refunds = returnQuery.ToList();
            var returnIds = refunds.Select(it => it.Id).ToList();
            var returnLines = returnIds.Count == 0 ? new List<JySaleReturnItem>() : Context.Queryable<JySaleReturnItem>()
                .Where(it => it.TenantId == actor.TenantId && returnIds.Contains(it.ReturnId)).ToList();
            var saleLineIds = returnLines.Select(it => it.SaleOrderItemId).Distinct().ToList();
            var saleLines = saleLineIds.Count == 0 ? new List<JySaleOrderItem>() : Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && saleLineIds.Contains(it.Id)).ToList();
            var saleLineMap = saleLines.ToDictionary(it => it.Id);
            decimal refundAmount = 0;
            foreach (var line in returnLines)
            {
                if (!saleLineMap.TryGetValue(line.SaleOrderItemId, out var saleLine)) continue;
                var parentIds = ResolveParentCategoryIds(saleLine, goodsMap, styleMap, parentByName);
                if (parentIds.Any(id => categoryIds.Contains(id))) refundAmount += line.RefundAmount;
            }
            return salesAmount - refundAmount;
        }

        private static HashSet<long> ResolveParentCategoryIds(JySaleOrderItem line, Dictionary<long, JyGoodsItem> goodsMap,
            Dictionary<long, JyLookupItem> styleMap, Dictionary<string, List<long>> parentByName)
        {
            var result = new HashSet<long>();
            void AddItem(long? id)
            {
                if (!id.HasValue || !styleMap.TryGetValue(id.Value, out var node)) return;
                var root = node;
                while (root.ParentId.HasValue && styleMap.TryGetValue(root.ParentId.Value, out var p)) root = p;
                result.Add(root.Id);
                if (parentByName.TryGetValue(root.ItemName ?? string.Empty, out var sameNames))
                    foreach (var other in sameNames) result.Add(other);
            }
            AddItem(line.StyleItemId);
            if (goodsMap.TryGetValue(line.GoodsItemId, out var goods)) AddItem(goods.StyleItemId);
            var names = new[] { line.StyleItemName, line.StyleName, goods?.StyleItemName, goods?.StyleName, line.GoodsName };
            foreach (var name in names.Where(it => !string.IsNullOrWhiteSpace(it)))
                foreach (var pair in parentByName)
                    if (name.Contains(pair.Key) || pair.Key.Contains(name)) result.UnionWith(pair.Value);
            return result;
        }

        private decimal DealCustomerCount(JyActor actor, DateTime start, DateTime end, List<long> memberIds)
        {
            var rows = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1 && it.MemberId != null
                    && it.DealTime >= start.Date && it.DealTime < end.Date.AddDays(1)).ToList();
            if (memberIds.Count > 0) rows = rows.Where(it => it.MemberId.HasValue && memberIds.Contains(it.MemberId.Value)).ToList();
            return rows.Select(it => it.MemberId.Value).Distinct().Count();
        }

        private List<long> LoadScopeIds(JyActor actor, long workItemId, int type)
        {
            return Context.Queryable<JyWorkItemScope>()
                .Where(it => it.TenantId == actor.TenantId && it.WorkItemId == workItemId && it.ScopeType == type)
                .Select(it => it.ScopeId).ToList();
        }

        private List<(int ScopeType, long ScopeId, string SnapshotName)> NormalizeScopes(JyActor actor,
            List<JyWorkScopeEditDto> input, string metric, int progressMode)
        {
            var result = new List<(int, long, string)>();
            if (input == null || input.Count == 0) return result;
            if (progressMode != 2) throw new CustomException("只有自动统计目标可以设置统计范围");
            foreach (var scope in input.GroupBy(it => new { it.ScopeType, it.ScopeId }).Select(g => g.First()))
            {
                if (scope.ScopeType == 1)
                {
                    var member = Context.Queryable<JyMember>().First(it => it.Id == scope.ScopeId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                    if (member == null) throw new CustomException("指定会员不存在");
                    if (metric != MetricFollow && metric != MetricDeal) throw new CustomException("指定会员范围只适用于客户类目标");
                    result.Add((1, scope.ScopeId, member.MemberName));
                }
                else if (scope.ScopeType == 2)
                {
                    var item = Context.Queryable<JyLookupItem>().First(it => it.Id == scope.ScopeId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                    if (item == null) throw new CustomException("款式分类不存在");
                    if (item.ParentId.HasValue) throw new CustomException("品类目标只能选择父级分类");
                    if (metric != MetricCategory) throw new CustomException("款式分类范围只适用于品类销售目标");
                    result.Add((2, scope.ScopeId, item.ItemName));
                }
                else throw new CustomException("统计范围类型不正确");
            }
            return result;
        }

        private static string NormalizeMetric(string metric, int progressMode)
        {
            if (progressMode != 2) return null;
            metric = (metric ?? string.Empty).Trim().ToLowerInvariant();
            return metric switch
            {
                MetricSales or MetricCategory or MetricCategoryQty or MetricFollow or MetricDeal or MetricCustom => metric,
                _ => throw new CustomException("统计指标不正确")
            };
        }

        private static decimal CalcPercent(decimal current, decimal target)
        {
            if (target <= 0) return 0;
            return decimal.Round(Math.Min(100m, Math.Max(0m, current * 100m / target)), 2, MidpointRounding.AwayFromZero);
        }

        private JyWorkItem Must(string tenantId, long id)
        {
            return Context.Queryable<JyWorkItem>().First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("待办或目标不存在");
        }

        private List<JyWorkItemDto> ToDtos(List<JyWorkItem> parents, List<JyWorkItem> children, List<JyWorkItemScope> scopes)
        {
            var childMap = children.GroupBy(it => it.ParentId ?? 0).ToDictionary(g => g.Key, g => g.ToList());
            var scopeMap = scopes.GroupBy(it => it.WorkItemId).ToDictionary(g => g.Key, g => g.Select(s => new JyWorkScopeDto
                { ScopeType = s.ScopeType, ScopeId = s.ScopeId, SnapshotName = s.SnapshotName }).ToList());
            return parents.Select(it => ToDto(it, childMap.GetValueOrDefault(it.Id) ?? new List<JyWorkItem>(),
                scopeMap.GetValueOrDefault(it.Id) ?? new List<JyWorkScopeDto>(), scopeMap)).ToList();
        }

        private static JyWorkItemDto ToDto(JyWorkItem it, List<JyWorkItem> children, List<JyWorkScopeDto> scopes,
            Dictionary<long, List<JyWorkScopeDto>> scopeMap)
        {
            var now = DateTime.Now;
            return new JyWorkItemDto
            {
                Id = it.Id, ParentId = it.ParentId, StoreId = it.StoreId, ItemNo = it.ItemNo, Title = it.Title,
                ItemKind = it.ItemKind, ItemKindName = KindName(it.ItemKind), PeriodType = it.PeriodType,
                StartDate = it.StartDate, EndDate = it.EndDate, DueTime = it.DueTime, FinishDate = it.TargetFinishDate, Priority = it.Priority,
                PriorityName = PriorityName(it.Priority), OwnerUserId = it.OwnerUserId, OwnerName = it.OwnerName,
                Status = it.Status, StatusName = it.Status == 1 ? "已完成" : "进行中", ProgressMode = it.ProgressMode,
                MetricCode = it.MetricCode, TargetValue = it.TargetValue, CurrentValue = it.CurrentValue,
                CurrentAuxValue = it.CurrentAuxValue, ProgressPercent = it.ProgressPercent, AutoComplete = it.AutoComplete,
                ReminderTime = it.ReminderTime, FinishTime = it.FinishTime, SettledTime = it.SettledTime,
                SortNo = it.SortNo, TemplateId = it.TemplateId, Remark = it.Remark, HasDateRange = it.HasDateRange,
                IsPerformanceRelated = it.IsPerformanceRelated, TargetMode = it.TargetMode,
                TargetModeName = it.TargetMode == 2 ? "件数" : "金额", CategoryId = it.CategoryId, CategoryName = it.CategoryName,
                CreateTime = it.Create_time, CreateBy = it.Create_by, IsOverdue = it.Status == 0 && IsOverdue(it, now),
                ChildCount = children.Count, DoneChildCount = children.Count(c => c.Status == 1), Scopes = scopes,
                Children = children.Select(c => ToDto(c, new List<JyWorkItem>(),
                    scopeMap.GetValueOrDefault(c.Id) ?? new List<JyWorkScopeDto>(), scopeMap)).ToList()
            };
        }

        private static bool IsDueToday(JyWorkItem it, DateTime today) => it.HasDateRange == 1 && (it.DueTime?.Date ?? it.EndDate.Date) == today;
        private static bool IsOverdue(JyWorkItem it, DateTime now) => it.HasDateRange == 1 && (it.DueTime.HasValue ? it.DueTime.Value < now : it.EndDate.Date < now.Date);
        private static DateTime EffectiveDue(JyWorkItem it) => it.DueTime ?? it.EndDate.Date.AddDays(1).AddSeconds(-1);

        /// <summary>
        /// 统一排序：未完成、逾期、今日到期、高优先级、7 天内到期、截止时间、排序号。
        /// 父目标组按“最紧急的可执行子目标”参与排序。
        /// </summary>
        private static int CompareGroups(JyWorkItem a, JyWorkItem b, Dictionary<long, List<JyWorkItem>> childMap,
            DateTime now, DateTime today, DateTime weekEnd)
        {
            var childrenA = childMap.GetValueOrDefault(a.Id) ?? new List<JyWorkItem>();
            var childrenB = childMap.GetValueOrDefault(b.Id) ?? new List<JyWorkItem>();
            var doneA = childrenA.Count > 0 ? childrenA.All(it => it.Status == 1) : a.Status == 1;
            var doneB = childrenB.Count > 0 ? childrenB.All(it => it.Status == 1) : b.Status == 1;
            var statusCompare = (doneA ? 1 : 0).CompareTo(doneB ? 1 : 0);
            if (statusCompare != 0) return statusCompare;

            var repA = MostUrgent(childrenA, a, now, today, weekEnd);
            var repB = MostUrgent(childrenB, b, now, today, weekEnd);
            var result = CompareUrgency(repA, repB, now, today, weekEnd);
            if (result != 0) return result;
            result = a.SortNo.CompareTo(b.SortNo);
            return result != 0 ? result : b.Id.CompareTo(a.Id);
        }

        private static JyWorkItem MostUrgent(List<JyWorkItem> children, JyWorkItem parent, DateTime now, DateTime today, DateTime weekEnd)
        {
            var active = children.Where(it => it.Status == 0).ToList();
            if (active.Count == 0) return children.Count == 0 ? parent : children.First();
            active.Sort((a, b) => CompareUrgency(a, b, now, today, weekEnd));
            return active[0];
        }

        private static int CompareItems(JyWorkItem a, JyWorkItem b, DateTime now, DateTime today, DateTime weekEnd)
        {
            var result = (a.Status == 1 ? 1 : 0).CompareTo(b.Status == 1 ? 1 : 0);
            if (result != 0) return result;
            result = CompareUrgency(a, b, now, today, weekEnd);
            if (result != 0) return result;
            result = a.SortNo.CompareTo(b.SortNo);
            return result != 0 ? result : b.Id.CompareTo(a.Id);
        }

        private static int CompareUrgency(JyWorkItem a, JyWorkItem b, DateTime now, DateTime today, DateTime weekEnd)
        {
            var result = (IsOverdue(a, now) ? 0 : 1).CompareTo(IsOverdue(b, now) ? 0 : 1);
            if (result != 0) return result;
            result = (IsDueToday(a, today) ? 0 : 1).CompareTo(IsDueToday(b, today) ? 0 : 1);
            if (result != 0) return result;
            result = b.Priority.CompareTo(a.Priority);
            if (result != 0) return result;
            result = (IsDueWithinWeek(a, today, weekEnd) ? 0 : 1).CompareTo(IsDueWithinWeek(b, today, weekEnd) ? 0 : 1);
            if (result != 0) return result;
            result = EffectiveDue(a).CompareTo(EffectiveDue(b));
            if (result != 0) return result;
            return a.SortNo.CompareTo(b.SortNo);
        }

        private static bool IsDueWithinWeek(JyWorkItem it, DateTime today, DateTime weekEnd)
        {
            var due = EffectiveDue(it);
            return due >= today && due < weekEnd;
        }

        /// <summary>从前端只提交用户 ID 时回填可靠的负责人名称。</summary>
        public string ResolveOwnerName(JyActor actor, long? ownerUserId, string submittedName)
        {
            if (ownerUserId is not > 0) return TrimText(submittedName, 50, "负责人", false);
            var owner = Context.Queryable<SysUser>()
                .First(it => it.UserId == ownerUserId.Value && it.DelFlag == 0 && it.Status == 0);
            if (owner == null) throw new CustomException("负责人不存在或已停用");
            return TrimText(owner.NickName ?? owner.UserName, 50, "负责人", false);
        }

        private static string KindName(int kind) => kind switch
        {
            1 => "普通待办", 2 => "客户跟进", 3 => "品类销售", 4 => "期间销售", 5 => "自定义量化", 6 => "汇总目标", _ => "待办"
        };

        private static string PriorityName(int priority) => priority switch { 4 => "重要且紧急", 3 => "重要不紧急", 2 => "不重要但紧急", 1 => "不重要不紧急", _ => "不重要不紧急" };

        private static string TrimText(string value, int max, string label, bool required = true)
        {
            value = (value ?? string.Empty).Trim();
            if (required && string.IsNullOrEmpty(value)) throw new CustomException($"{label}不能为空");
            if (value.Length > max) throw new CustomException($"{label}不能超过 {max} 个字");
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public List<JyWorkQuickActionDto> QuickActions(JyActor actor)
        {
            return QueryQuickActions(actor);
        }

        public long SaveQuickAction(JyActor actor, JyWorkQuickActionEditDto dto)
        {
            if (dto == null) throw new CustomException("快捷事项内容不能为空");
            var actionName = TrimText(dto.ActionName, 100, "快捷事项名称");
            var titleTemplate = TrimText(dto.TitleTemplate, 200, "待办标题模板", false) ?? actionName;
            if (dto.SortNo < 0) throw new CustomException("排序号不能为负数");
            var now = DateTime.Now;
            JyWorkQuickAction row;
            if (dto.Id > 0)
            {
                row = Context.Queryable<JyWorkQuickAction>().First(it => it.Id == dto.Id && it.DelFlag == 0
                    && (it.TenantId == actor.TenantId || it.TenantId == "0"));
                if (row == null) throw new CustomException("快捷事项不存在");
                if (row.TenantId == "0")
                {
                    row = new JyWorkQuickAction
                    {
                        TenantId = actor.TenantId, ActionName = actionName, TitleTemplate = titleTemplate,
                        SortNo = dto.SortNo, Status = dto.Status, IsPreset = 1, DelFlag = 0,
                        Create_by = actor.UserName, Create_time = now
                    };
                    row.Id = Context.Insertable(row).ExecuteReturnBigIdentity();
                    return row.Id;
                }
            }
            else
            {
                row = Context.Queryable<JyWorkQuickAction>().First(it => it.TenantId == actor.TenantId
                    && it.ActionName == actionName && it.DelFlag == 0);
                if (row != null) throw new CustomException("快捷事项名称已存在");
            }
            if (row == null)
            {
                row = new JyWorkQuickAction
                {
                    TenantId = actor.TenantId, ActionName = actionName, TitleTemplate = titleTemplate,
                    SortNo = dto.SortNo, Status = dto.Status, IsPreset = 0, DelFlag = 0,
                    Create_by = actor.UserName, Create_time = now
                };
                row.Id = Context.Insertable(row).ExecuteReturnBigIdentity();
                return row.Id;
            }
            row.ActionName = actionName;
            row.TitleTemplate = titleTemplate;
            row.SortNo = dto.SortNo;
            row.Status = dto.Status;
            row.Update_by = actor.UserName;
            row.Update_time = now;
            Context.Updateable(row).UpdateColumns(it => new
            {
                it.ActionName, it.TitleTemplate, it.SortNo, it.Status, it.Update_by, it.Update_time
            }).ExecuteCommand();
            return row.Id;
        }

        public int DeleteQuickAction(JyActor actor, long id)
        {
            var row = Context.Queryable<JyWorkQuickAction>().First(it => it.Id == id && it.DelFlag == 0
                && (it.TenantId == actor.TenantId || it.TenantId == "0"));
            if (row == null) throw new CustomException("快捷事项不存在");
            if (row.TenantId == "0") throw new CustomException("系统预设快捷事项不能删除，可以停用");
            return Context.Updateable<JyWorkQuickAction>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
        }

        private List<JyWorkQuickActionDto> QueryQuickActions(JyActor actor)
        {
            var rows = Context.Queryable<JyWorkQuickAction>()
                .Where(it => it.DelFlag == 0 && it.Status == 0 && (it.TenantId == actor.TenantId || it.TenantId == "0"))
                .OrderBy(it => it.SortNo).OrderBy(it => it.Id).ToList();
            return rows.GroupBy(it => it.ActionName)
                .Select(g => g.OrderByDescending(it => it.TenantId == actor.TenantId).First())
                .OrderBy(it => it.SortNo).ThenBy(it => it.Id)
                .Select(it => new JyWorkQuickActionDto
                {
                    Id = it.Id, ActionName = it.ActionName, TitleTemplate = it.TitleTemplate,
                    SortNo = it.SortNo, Status = it.Status, IsPreset = it.IsPreset, Remark = it.Remark
                }).ToList();
        }
        /// <summary>
        /// 在业务事务内分配待办编号。严格识别 WT+yyyyMMdd+4 位数字，
        /// 忽略历史模板生成的随机后缀；SQL Server 应用锁保证并发取号不重复。
        /// </summary>
        public string NextItemNo(ISqlSugarClient db, string tenantId, DateTime now)
        {
            if (db == null) throw new CustomException("待办编号生成失败：数据库连接不能为空");
            var prefix = "WT" + now.ToString("yyyyMMdd");
            var resource = $"jy_work_item_no:{tenantId}:{now:yyyyMMdd}";
            // 必须在 UseTran 业务事务中调用。应用锁随事务释放，流水持久化与业务插入同成同败。
            var sql = @"
DECLARE @r int;
EXEC @r = sp_getapplock @Resource=@Resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;
IF @r < 0
BEGIN
    RAISERROR(N'待办编号正在被其他操作占用，请稍后重试', 16, 1);
    RETURN;
END;

DECLARE @MaxValid int;
SELECT @MaxValid = ISNULL(MAX(CAST(RIGHT(ItemNo, 4) AS int)), 0)
  FROM dbo.jy_work_item
 WHERE TenantId = @TenantId
   AND ItemNo IS NOT NULL
   AND LEN(ItemNo) = 14
   AND ItemNo LIKE @Pattern;

DECLARE @Current int;
SELECT TOP 1 @Current = CurrentNo
  FROM dbo.jy_biz_sequence
 WHERE TenantId = @TenantId AND BizType = @BizType AND BizDate = @BizDate
 ORDER BY Id;

DECLARE @Next int;
SET @Next = CASE WHEN ISNULL(@Current, 0) > ISNULL(@MaxValid, 0) THEN @Current ELSE ISNULL(@MaxValid, 0) END + 1;

-- 流水号可能因历史迁移、并发或异常回滚落后于实际数据。
-- 在应用锁事务内逐号跳过活跃数据已占用的编号，最后再把流水推进到真正使用的号码。
DECLARE @Guard int = 0;
WHILE @Next <= 9999 AND EXISTS (
    SELECT 1 FROM dbo.jy_work_item WITH (UPDLOCK, HOLDLOCK)
     WHERE TenantId = @TenantId AND ItemNo = @Prefix + RIGHT('0000' + CAST(@Next AS varchar(4)), 4) AND DelFlag = 0
)
BEGIN
    SET @Next = @Next + 1;
    SET @Guard = @Guard + 1;
    IF @Guard > 9999 BREAK;
END;
IF @Next > 9999
BEGIN
    RAISERROR(N'当日待办编号已用完，请联系管理员', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.jy_biz_sequence WHERE TenantId = @TenantId AND BizType = @BizType AND BizDate = @BizDate)
    UPDATE dbo.jy_biz_sequence
       SET CurrentNo = @Next, UpdateTime = GETDATE()
     WHERE TenantId = @TenantId AND BizType = @BizType AND BizDate = @BizDate;
ELSE
    INSERT INTO dbo.jy_biz_sequence (TenantId, BizType, BizDate, CurrentNo, UpdateTime)
    VALUES (@TenantId, @BizType, @BizDate, @Next, GETDATE());

SELECT @Next AS NextNo;
";
            var table = db.Ado.GetDataTable(sql,
                new SugarParameter("@Resource", resource),
                new SugarParameter("@TenantId", tenantId),
                new SugarParameter("@Pattern", prefix + "[0-9][0-9][0-9][0-9]"),
                new SugarParameter("@Prefix", prefix),
                new SugarParameter("@BizType", prefix),
                new SugarParameter("@BizDate", now.Date));
            if (table == null || table.Rows.Count == 0) throw new CustomException("待办编号生成失败");
            var seq = Convert.ToInt32(table.Rows[0]["NextNo"]);
            return prefix + seq.ToString("D4");
        }

        private void SaveTemplateInternal(JyActor actor, JyWorkTemplateEditDto dto, DateTime now)
        {
            var templateName = (dto.TemplateName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(templateName))
                throw new CustomException("模板名称不能为空");
            if (templateName.Length > 100)
                throw new CustomException("模板名称不能超过 100 个字");
            if (dto.Items == null || dto.Items.Count == 0)
                throw new CustomException("目标模板至少需要一个事项");
            if (dto.Items.Any(it => string.IsNullOrWhiteSpace(it.Title) || it.Title.Trim().Length > 200))
                throw new CustomException("模板事项标题不能为空且不能超过 200 个字");

            var template = new JyWorkTemplate
            {
                TenantId = actor.TenantId, TemplateName = templateName,
                Description = TrimText(dto.Description, 500, "模板说明", false),
                Status = 0, DelFlag = 0, Create_by = actor.UserName, Create_time = now
            };
            template.Id = Context.Insertable(template).ExecuteReturnBigIdentity();
            var items = dto.Items.Select((it, index) => new JyWorkTemplateItem
            {
                TenantId = actor.TenantId, TemplateId = template.Id, SortNo = index + 1,
                Title = it.Title.Trim(), ItemKind = it.ItemKind, PeriodType = it.PeriodType,
                Priority = it.Priority, MetricCode = it.MetricCode, TargetValue = it.TargetValue,
                TargetMode = it.TargetMode, CategoryId = it.CategoryId, CategoryName = it.CategoryName,
                ScopeJson = it.ScopeJson, Remark = TrimText(it.Remark, 500, "事项备注", false)
            }).ToList();
            Context.Insertable(items).ExecuteCommand();
        }
    }
}







