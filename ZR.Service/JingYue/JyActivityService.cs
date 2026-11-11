using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyActivityService : BaseService<JyActivity>
    {
        private readonly JyBillRuleService _billRule;
        private readonly JyLookupService _lookup;

        public JyActivityService(JyBillRuleService billRule, JyLookupService lookup)
        {
            _billRule = billRule;
            _lookup = lookup;
        }

        public PagedInfo<JyActivityDto> List(JyActor actor, JyActivityQueryDto query)
        {
            query ??= new JyActivityQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JyActivity>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is >= 0 and <= 3, it => it.Status == query.Status)
                .WhereIF(query.ActivityType is >= 1 and <= 5, it => it.ActivityType == query.ActivityType)
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.ActivityNo.Contains(keyword) || it.ActivityName.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.Status == 1 ? 0 : it.Status == 0 ? 1 : 2)
                .OrderBy(it => it.StartTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JyActivityDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDtos(actor.TenantId, rows)
            };
        }

        public JyActivityDto Detail(JyActor actor, long id)
        {
            return ToDtos(actor.TenantId, new List<JyActivity> { Must(actor.TenantId, id) }).First();
        }

        public JyActivityOptionsDto Options(JyActor actor)
        {
            _lookup.EnsureFoundation(actor);
            var levels = Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.SortNo)
                .ToList()
                .Select(it => new JyMemberLevelDto
                {
                    Id = it.Id,
                    LevelCode = it.LevelCode,
                    LevelName = it.LevelName,
                    MinPoints = it.MinPoints,
                    MaxPoints = it.MaxPoints,
                    DiscountRate = it.DiscountRate,
                    SortNo = it.SortNo,
                    KeepOnYearClear = it.KeepOnYearClear,
                    Status = it.Status,
                    Remark = it.Remark
                }).ToList();
            return new JyActivityOptionsDto
            {
                Levels = levels,
                Categories = _lookup.Options(actor, "style")
            };
        }

        public JyActivityDashboardDto Dashboard(JyActor actor)
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonth = monthStart.AddMonths(1);
            var runningQuery = Context.Queryable<JyActivity>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1 && it.StartTime <= now && it.EndTime >= now);
            var upcomingQuery = Context.Queryable<JyActivity>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1 && it.StartTime > now);
            var monthBenefit = Context.Queryable<JyActivityMember>()
                .Where(it => it.TenantId == actor.TenantId && it.JoinTime >= monthStart && it.JoinTime < nextMonth)
                .Sum(it => it.BenefitAmount);
            var joinCount = Context.Queryable<JyActivityMember>()
                .Where(it => it.TenantId == actor.TenantId && it.JoinTime >= monthStart && it.JoinTime < nextMonth)
                .Count();
            var running = runningQuery.OrderBy(it => it.EndTime).Take(8).ToList();
            var upcoming = upcomingQuery.OrderBy(it => it.StartTime).Take(8).ToList();
            return new JyActivityDashboardDto
            {
                RunningCount = runningQuery.Count(),
                UpcomingCount = upcomingQuery.Count(),
                MonthBenefit = monthBenefit,
                JoinCount = joinCount,
                Running = ToDtos(actor.TenantId, running),
                Upcoming = ToDtos(actor.TenantId, upcoming)
            };
        }
        public long Save(JyActor actor, JyActivityEditDto dto)
        {
            if (dto == null) throw new CustomException("活动信息不能为空");
            dto.ActivityName = JyBillNo.NormalizeName(dto.ActivityName, "活动名称", 100);
            dto.GiftName = EmptyOrMax(dto.GiftName, 200, "赠品名称");
            dto.Remark = EmptyOrMax(dto.Remark, 500, "备注");
            if (dto.ActivityType is < 1 or > 5) throw new CustomException("活动类型不正确");
            if (dto.Status is < 0 or > 3) throw new CustomException("活动状态不正确");
            if (dto.ScopeType is not (1 or 2)) throw new CustomException("活动范围不正确");
            if (dto.StartTime == default || dto.EndTime == default) throw new CustomException("请选择活动起止时间");
            if (dto.StartTime >= dto.EndTime) throw new CustomException("结束时间必须晚于开始时间");
            if (dto.EndTime <= DateTime.Now && dto.Status == 1)
                throw new CustomException("活动已过结束时间，不能设为进行中");
            if (dto.ScopeType == 2 && !dto.StyleItemId.HasValue)
                throw new CustomException("指定款式范围时必须选择款式分类");
            if (dto.StyleItemId.HasValue)
            {
                var item = _lookup.MustActiveItem(actor.TenantId, dto.StyleItemId.Value, "style", "活动款式分类");
                if (item.Status != 0) throw new CustomException("所选活动款式分类已停用");
            }
            if (dto.MemberLevelId.HasValue)
            {
                var level = Context.Queryable<JyMemberLevel>().First(it => it.Id == dto.MemberLevelId
                    && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (level == null) throw new CustomException("会员等级不存在");
                if (level.Status != 0) throw new CustomException("所选会员等级已停用");
            }
            if (dto.ActivityType == 5 && !dto.MemberLevelId.HasValue)
                throw new CustomException("会员专享活动必须选择会员等级");
            if (dto.ActivityType is 1 or 5 && (dto.DiscountRate is null || dto.DiscountRate <= 0 || dto.DiscountRate > 1))
                throw new CustomException("折扣率必须大于 0 且不超过 1");
            if (dto.ActivityType == 2 && dto.ReduceAmount <= 0)
                throw new CustomException("满减金额必须大于 0");
            if (dto.ActivityType == 3 && string.IsNullOrEmpty(dto.GiftName))
                throw new CustomException("请填写赠品名称");
            if (dto.ActivityType == 4 && dto.PointsMultiple < 1)
                throw new CustomException("积分倍数不能小于 1");
            if (dto.ReduceAmount < 0 || dto.BudgetAmount < 0 || dto.JoinLimit < 0)
                throw new CustomException("金额和参与次数不能为负数");
            if (dto.PointsMultiple <= 0) throw new CustomException("积分倍数必须大于 0");
            if (dto.ActivityType != 1 && dto.ActivityType != 5) dto.DiscountRate = null;
            if (dto.ActivityType != 2) dto.ReduceAmount = 0;
            if (dto.ActivityType != 3) dto.GiftName = null;
            if (dto.ActivityType != 4) dto.PointsMultiple = 1;
            if (dto.ScopeType == 1) dto.StyleItemId = null;

            if (dto.Id <= 0)
            {
                var result = 0L;
                var tran = UseTran(() =>
                {
                    var issued = _billRule.IssueInTransaction(actor, "activity", "jy_activity", null);
                    result = Context.Insertable(new JyActivity
                    {
                        TenantId = actor.TenantId,
                        StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                        ActivityNo = issued.BillNo,
                        ActivityName = dto.ActivityName,
                        ActivityType = dto.ActivityType,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        ScopeType = dto.ScopeType,
                        StyleItemId = dto.StyleItemId,
                        DiscountRate = dto.DiscountRate,
                        ReduceAmount = dto.ReduceAmount,
                        GiftName = dto.GiftName,
                        PointsMultiple = dto.PointsMultiple,
                        MemberLevelId = dto.MemberLevelId,
                        JoinLimit = dto.JoinLimit,
                        BudgetAmount = dto.BudgetAmount,
                        UsedAmount = 0,
                        Status = dto.Status,
                        DelFlag = 0,
                        Create_by = actor.UserName,
                        Create_time = DateTime.Now,
                        Remark = dto.Remark
                    }).ExecuteReturnBigIdentity();
                });
                if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "活动新增失败");
                return result;
            }

            var current = Must(actor.TenantId, dto.Id);
            if (current.UsedAmount > 0 && dto.BudgetAmount > 0 && dto.BudgetAmount < current.UsedAmount)
                throw new CustomException("活动预算不能低于已使用金额");
            if (dto.Status == 1 && current.UsedAmount >= dto.BudgetAmount && dto.BudgetAmount > 0)
                throw new CustomException("活动预算已用完，不能设为进行中");
            current.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
            current.ActivityName = dto.ActivityName;
            current.ActivityType = dto.ActivityType;
            current.StartTime = dto.StartTime;
            current.EndTime = dto.EndTime;
            current.ScopeType = dto.ScopeType;
            current.StyleItemId = dto.StyleItemId;
            current.DiscountRate = dto.DiscountRate;
            current.ReduceAmount = dto.ReduceAmount;
            current.GiftName = dto.GiftName;
            current.PointsMultiple = dto.PointsMultiple;
            current.MemberLevelId = dto.MemberLevelId;
            current.JoinLimit = dto.JoinLimit;
            current.BudgetAmount = dto.BudgetAmount;
            current.Status = dto.Status;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current)
                .UpdateColumns(it => new
                {
                    it.StoreId, it.ActivityName, it.ActivityType, it.StartTime, it.EndTime,
                    it.ScopeType, it.StyleItemId, it.DiscountRate, it.ReduceAmount, it.GiftName,
                    it.PointsMultiple, it.MemberLevelId, it.JoinLimit, it.BudgetAmount,
                    it.Status, it.Remark, it.Update_by, it.Update_time
                })
                .ExecuteCommand();
            return current.Id;
        }

        public int SetStatus(JyActor actor, JyActivityStatusDto dto)
        {
            if (dto == null || dto.Id <= 0) throw new CustomException("请选择活动");
            if (dto.Status is < 0 or > 3) throw new CustomException("活动状态不正确");
            var current = Must(actor.TenantId, dto.Id);
            if (dto.Status == 1)
            {
                if (current.EndTime <= DateTime.Now) throw new CustomException("活动已过结束时间，不能启用");
                if (current.BudgetAmount > 0 && current.UsedAmount >= current.BudgetAmount)
                    throw new CustomException("活动预算已用完，不能启用");
            }
            return Context.Updateable<JyActivity>()
                .SetColumns(it => it.Status == dto.Status)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == dto.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        public int Delete(JyActor actor, long id)
        {
            var current = Must(actor.TenantId, id);
            if (current.UsedAmount > 0 || Context.Queryable<JyActivityMember>()
                .Any(it => it.TenantId == actor.TenantId && it.ActivityId == id))
                throw new CustomException("活动已有参与记录或优惠金额，不能删除，可改为停用");
            return Context.Updateable<JyActivity>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        private List<JyActivityDto> ToDtos(string tenantId, List<JyActivity> rows)
        {
            if (rows.Count == 0) return new List<JyActivityDto>();
            var styleIds = rows.Where(it => it.StyleItemId.HasValue).Select(it => it.StyleItemId.Value).Distinct().ToList();
            var levelIds = rows.Where(it => it.MemberLevelId.HasValue).Select(it => it.MemberLevelId.Value).Distinct().ToList();
            var activityIds = rows.Select(it => it.Id).ToList();
            var styles = styleIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && styleIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id, it => it.ItemName);
            var levels = levelIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == tenantId && levelIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id, it => it.LevelName);
            var joins = Context.Queryable<JyActivityMember>()
                .Where(it => it.TenantId == tenantId && activityIds.Contains(it.ActivityId))
                .GroupBy(it => it.ActivityId)
                .Select(it => new { it.ActivityId, Count = SqlFunc.AggregateCount(it.Id) })
                .ToList().ToDictionary(it => it.ActivityId, it => it.Count);
            return rows.Select(it => new JyActivityDto
            {
                Id = it.Id,
                ActivityNo = it.ActivityNo,
                ActivityName = it.ActivityName,
                ActivityType = it.ActivityType,
                StartTime = it.StartTime,
                EndTime = it.EndTime,
                ScopeType = it.ScopeType,
                StyleItemId = it.StyleItemId,
                StyleItemName = it.StyleItemId.HasValue && styles.TryGetValue(it.StyleItemId.Value, out var style) ? style : null,
                DiscountRate = it.DiscountRate,
                ReduceAmount = it.ReduceAmount,
                GiftName = it.GiftName,
                PointsMultiple = it.PointsMultiple,
                MemberLevelId = it.MemberLevelId,
                MemberLevelName = it.MemberLevelId.HasValue && levels.TryGetValue(it.MemberLevelId.Value, out var level) ? level : null,
                JoinLimit = it.JoinLimit,
                BudgetAmount = it.BudgetAmount,
                UsedAmount = it.UsedAmount,
                Status = it.Status,
                Remark = it.Remark,
                JoinCount = joins.TryGetValue(it.Id, out var count) ? count : 0,
                CreateTime = it.Create_time
            }).ToList();
        }

        private JyActivity Must(string tenantId, long id)
        {
            return Context.Queryable<JyActivity>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("活动不存在");
        }

        private static string EmptyOrMax(string value, int max, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > max) throw new CustomException($"{label}不能超过 {max} 个字");
            return value;
        }
    }
}



