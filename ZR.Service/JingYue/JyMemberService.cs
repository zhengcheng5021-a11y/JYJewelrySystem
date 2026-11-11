using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;
using ZR.Model.System;
using ZR.Model.System.Dto;
using ZR.Model.System.Enums;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyMemberService : BaseService<JyMember>
    {
        private sealed record LevelSeed(string Code, string Name, decimal Min, decimal? Max, decimal Discount, int Sort);

        private static readonly LevelSeed[] DefaultLevels =
        [
            new("normal", "普通会员", 0m, 999m, 1.00m, 1),
            new("silver", "银卡会员", 1000m, 4999m, 0.98m, 2),
            new("gold", "金卡会员", 5000m, 19999m, 0.95m, 3),
            new("platinum", "铂金会员", 20000m, null, 0.92m, 4),
            new("diamond", "钻石会员", 50000m, null, 0.88m, 5)
        ];

        private readonly JyBillRuleService _billRule;
        private readonly JyLookupService _lookup;

        public JyMemberService(JyBillRuleService billRule, JyLookupService lookup)
        {
            _billRule = billRule;
            _lookup = lookup;
        }

        public PagedInfo<JyMemberDto> List(JyActor actor, JyMemberQueryDto query)
        {
            EnsureLevels(actor);
            query ??= new JyMemberQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var q = BuildQuery(actor, query);
            var total = 0;
            var rows = ApplySort(q, query).ToPageList(page, size, ref total);
            return new PagedInfo<JyMemberDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDtos(actor.TenantId, rows)
            };
        }

        /// <summary>导出与列表共用同一组筛选条件，不分页；上限防止误导出拖垮服务。</summary>
        public List<JyMemberExportDto> Export(JyActor actor, JyMemberQueryDto query)
        {
            EnsureLevels(actor);
            query ??= new JyMemberQueryDto();
            var rows = ApplySort(BuildQuery(actor, query), query)
                .Take(50000)
                .ToList();
            var members = ToDtos(actor.TenantId, rows);
            return members.Select(it => new JyMemberExportDto
            {
                MemberNo = it.MemberNo,
                MemberName = it.MemberName,
                SexName = it.Sex == 1 ? "女士" : it.Sex == 2 ? "先生" : "未标注",
                Phone = it.Phone,
                WechatNo = it.WechatNo,
                LevelName = it.LevelName,
                FollowStatusName = FollowStatusName(it.FollowStatus),
                StatusName = it.Status == 0 ? "正常" : "停用",
                Birthday = it.SolarBirthday.HasValue ? $"{it.SolarBirthday:yyyy-MM-dd}（公历）" : it.LunarBirthday,
                YearPoints = it.YearPoints,
                PointsBalance = it.PointsBalance,
                TotalAmount = it.TotalAmount,
                OrderCount = it.OrderCount,
                AvgTicket = it.AvgTicket,
                LastConsumeTime = it.LastConsumeTime,
                UsualStyleName = it.UsualStyleName,
                ReferrerNames = it.Referrers.Count == 0 ? "未绑定" : string.Join("、", it.Referrers.Select(r =>
                    (r.ReferrerName ?? r.ReferrerWechatNo ?? "未命名") + (r.IsPrimary == 1 ? "（默认）" : ""))),
                CreateTime = it.CreateTime,
                Remark = it.Remark
            }).ToList();
        }

        private ISugarQueryable<JyMember> BuildQuery(JyActor actor, JyMemberQueryDto query)
        {
            var keyword = query.Keyword?.Trim();
            var memberWechat = query.MemberWechatNo?.Trim();
            long.TryParse(memberWechat, out var memberWechatId);
            return Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is 0 or 1, it => it.Status == query.Status)
                .WhereIF(query.LevelId > 0, it => it.LevelId == query.LevelId)
                .WhereIF(query.FollowStatus is >= 0 and <= 3, it => it.FollowStatus == query.FollowStatus)
                .WhereIF(!string.IsNullOrEmpty(memberWechat), it =>
                    it.MemberWechatNo == memberWechat || it.MemberWechatId == memberWechatId)
                .WhereIF(!string.IsNullOrEmpty(keyword), it =>
                    it.MemberNo.Contains(keyword) || it.MemberName.Contains(keyword) || it.Phone.Contains(keyword)
                    || it.WechatNo.Contains(keyword) || it.ReferrerWechatNo.Contains(keyword));
        }

        private static string FollowStatusName(int status)
        {
            return status switch
            {
                1 => "跟进中",
                2 => "暂缓",
                3 => "已完成",
                _ => "待跟进"
            };
        }

        /// <summary>
        /// 列表和导出统一排序。字段与方向都走白名单，避免客户端把任意列拼进 SQL。
        /// </summary>
        private static ISugarQueryable<JyMember> ApplySort(ISugarQueryable<JyMember> q, JyMemberQueryDto query)
        {
            var asc = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var order = asc ? OrderByType.Asc : OrderByType.Desc;
            var sortBy = (query.SortBy ?? string.Empty).Trim();
            if (string.Equals(sortBy, "totalAmount", StringComparison.OrdinalIgnoreCase))
            {
                return q.OrderBy(it => it.TotalAmount, order)
                    .OrderBy(it => it.Id, OrderByType.Desc);
            }
            if (string.Equals(sortBy, "createTime", StringComparison.OrdinalIgnoreCase))
            {
                return q.OrderBy(it => it.Create_time, order)
                    .OrderBy(it => it.Id, order);
            }
            return q.OrderBy(it => it.LastConsumeTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc);
        }

        public JyMemberDto Detail(JyActor actor, long id)
        {
            EnsureLevels(actor);
            return ToDtos(actor.TenantId, new List<JyMember> { Must(actor.TenantId, id) }).First();
        }

        public JyMemberOptionsDto Options(JyActor actor)
        {
            EnsureLevels(actor);
            _lookup.EnsureFoundation(actor);
            return new JyMemberOptionsDto
            {
                Levels = ActiveLevels(actor.TenantId),
                Categories = _lookup.Options(actor, "style"),
                StaffWechats = Context.Queryable<SysUserWechat>()
                    .InnerJoin<SysUser>((w, u) => w.UserId == u.UserId)
                    .Where((w, u) => w.DelFlag == 0 && w.Status == 0
                        && u.DelFlag == DelFlagEnum.Normal && u.Status == 0)
                    .OrderBy((w, u) => u.NickName)
                    .OrderBy((w, u) => w.IsPrimary, OrderByType.Desc)
                    .OrderBy((w, u) => w.Id)
                    .Select((w, u) => new SysUserWechatOptionDto
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        UserName = u.UserName,
                        NickName = u.NickName,
                        WechatNo = w.WechatNo,
                        IsPrimary = w.IsPrimary
                    }).ToList()
            };
        }

        public JyMemberDashboardDto Dashboard(JyActor actor)
        {
            EnsureLevels(actor);
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonth = monthStart.AddMonths(1);
            var baseQuery = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0);
            var total = baseQuery.Count();
            var newThisMonth = baseQuery.Count(it => it.Create_time >= monthStart && it.Create_time < nextMonth);
            var activeThisMonth = baseQuery.Count(it => it.LastConsumeTime >= monthStart && it.LastConsumeTime < nextMonth);
            var monthAmount = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1 && it.MemberId.HasValue
                    && it.DealTime >= monthStart && it.DealTime < nextMonth)
                .Sum(it => it.PayAmount);
            var recentRows = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .OrderBy(it => it.Id, OrderByType.Desc).Take(6).ToList();
            var levels = LoadLevels(actor.TenantId);
            var levelNames = levels.ToDictionary(it => it.Id, it => it.LevelName);
            var levelCounts = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .GroupBy(it => it.LevelId)
                .Select(it => new { it.LevelId, Count = SqlFunc.AggregateCount(it.Id) })
                .ToList();
            return new JyMemberDashboardDto
            {
                TotalMembers = total,
                NewThisMonth = newThisMonth,
                ActiveThisMonth = activeThisMonth,
                MonthAmount = monthAmount,
                Recent = ToDtos(actor.TenantId, recentRows),
                Levels = levelCounts.Select(it => new JyMemberLevelStatDto
                {
                    LevelName = it.LevelId.HasValue && levelNames.TryGetValue(it.LevelId.Value, out var name) ? name : "未分级",
                    Count = it.Count
                }).OrderByDescending(it => it.Count).ToList()
            };
        }

        public long Save(JyActor actor, JyMemberEditDto dto)
        {
            if (dto == null) throw new CustomException("会员档案不能为空");
            EnsureLevels(actor);
            dto.MemberName = JyBillNo.NormalizeName(dto.MemberName, "会员姓名", 50);
            dto.Phone = NormalizePhone(dto.Phone);
            dto.WechatNo = NormalizeWechat(dto.WechatNo);
            dto.ReferrerWechatNo = NormalizeWechat(dto.ReferrerWechatNo);
            dto.MemberWechatId = dto.MemberWechatId > 0 ? dto.MemberWechatId : null;
            dto.Occupation = EmptyOrMax(dto.Occupation, 50, "职业");
            dto.Hobby = EmptyOrMax(dto.Hobby, 500, "爱好");
            dto.WuXing = EmptyOrMax(dto.WuXing, 20, "五行");
            dto.RecommendDirection = EmptyOrMax(dto.RecommendDirection, 100, "推荐方向");
            dto.LunarBirthday = EmptyOrMax(dto.LunarBirthday, 30, "农历生日");
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.Sex is < 0 or > 2) throw new CustomException("性别选项不正确");
            if (dto.BirthCalendar is not (1 or 2)) throw new CustomException("生日历法不正确");
            if (dto.FollowStatus is < 0 or > 3) throw new CustomException("跟进状态不正确");
            if (dto.Status is not (0 or 1)) throw new CustomException("会员状态不正确");
            if (dto.SolarBirthday.HasValue && dto.SolarBirthday.Value.Date > DateTime.Today)
                throw new CustomException("生日不能晚于今天");
            if (dto.BirthYear.HasValue && (dto.BirthYear < 1900 || dto.BirthYear > DateTime.Today.Year))
                throw new CustomException("出生年份不正确");
            if (dto.LevelId > 0)
            {
                var level = Context.Queryable<JyMemberLevel>().First(it => it.Id == dto.LevelId
                    && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (level == null) throw new CustomException("会员等级不存在");
                if (level.Status != 0) throw new CustomException("所选会员等级已停用");
            }
            if (dto.UsualStyleItemId > 0)
            {
                var item = _lookup.MustActiveItem(actor.TenantId, dto.UsualStyleItemId.Value, "style", "常购款式分类");
                if (item.Status != 0) throw new CustomException("所选常购款式分类已停用");
            }

            var referrerPlans = NormalizeReferrers(actor, dto.Referrers, dto.ReferrerWechatNo, dto.Id);
            var memberWechat = ResolveMemberWechat(dto.MemberWechatId);
            if (dto.Id <= 0)
            {
                var primary = referrerPlans.FirstOrDefault(it => it.IsPrimary) ?? referrerPlans.FirstOrDefault();
                var defaultLevelId = dto.LevelId > 0 ? dto.LevelId : ActiveLevels(actor.TenantId).FirstOrDefault()?.Id;
                var result = 0L;
                var tran = UseTran(() =>
                {
                    var issued = _billRule.IssueInTransaction(actor, "member", "jy_member", null);
                    result = Context.Insertable(new JyMember
                    {
                        TenantId = actor.TenantId,
                        StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                        MemberNo = issued.BillNo,
                        MemberName = dto.MemberName,
                        Sex = dto.Sex,
                        Phone = dto.Phone,
                        WechatNo = dto.WechatNo,
                        ReferrerWechatNo = primary?.ReferrerWechatNo,
                        ReferrerMemberId = primary?.ReferrerMemberId,
                        SolarBirthday = dto.SolarBirthday?.Date,
                        LunarBirthday = dto.LunarBirthday,
                        BirthCalendar = dto.BirthCalendar,
                        BirthYear = dto.BirthYear,
                        Occupation = dto.Occupation,
                        Hobby = dto.Hobby,
                        WuXing = dto.WuXing,
                        RecommendDirection = dto.RecommendDirection,
                        LevelId = defaultLevelId,
                        LevelChangedTime = defaultLevelId.HasValue ? DateTime.Now : null,
                        FollowStatus = dto.FollowStatus,
                        StaffUserId = dto.StaffUserId > 0 ? dto.StaffUserId : null,
                        MemberWechatId = memberWechat?.Id,
                        MemberWechatNo = memberWechat?.WechatNo,
                        UsualStyleItemId = dto.UsualStyleItemId > 0 ? dto.UsualStyleItemId : null,
                        Status = dto.Status,
                        DelFlag = 0,
                        Create_by = actor.UserName,
                        Create_time = DateTime.Now,
                        Remark = dto.Remark
                    }).ExecuteReturnBigIdentity();
                    SaveReferrers(actor, result, referrerPlans, 1, now: DateTime.Now);
                });
                if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "会员新增失败");
                return result;
            }

            var current = Must(actor.TenantId, dto.Id);
            current.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
            current.MemberName = dto.MemberName;
            current.Sex = dto.Sex;
            current.Phone = dto.Phone;
            current.WechatNo = dto.WechatNo;
            current.SolarBirthday = dto.SolarBirthday?.Date;
            current.LunarBirthday = dto.LunarBirthday;
            current.BirthCalendar = dto.BirthCalendar;
            current.BirthYear = dto.BirthYear;
            current.Occupation = dto.Occupation;
            current.Hobby = dto.Hobby;
            current.WuXing = dto.WuXing;
            current.RecommendDirection = dto.RecommendDirection;
            current.LevelId = dto.LevelId > 0 ? dto.LevelId : current.LevelId;
            current.FollowStatus = dto.FollowStatus;
            current.StaffUserId = dto.StaffUserId > 0 ? dto.StaffUserId : null;
            current.MemberWechatId = memberWechat?.Id;
            current.MemberWechatNo = memberWechat?.WechatNo;
            current.UsualStyleItemId = dto.UsualStyleItemId > 0 ? dto.UsualStyleItemId : null;
            current.Status = dto.Status;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            var tranEdit = UseTran(() =>
            {
                Context.Updateable(current)
                .UpdateColumns(it => new
                {
                    it.StoreId,
                    it.MemberName,
                    it.Sex,
                    it.Phone,
                    it.WechatNo,
                    it.SolarBirthday,
                    it.LunarBirthday,
                    it.BirthCalendar,
                    it.BirthYear,
                    it.Occupation,
                    it.Hobby,
                    it.WuXing,
                    it.RecommendDirection,
                    it.LevelId,
                    it.FollowStatus,
                    it.StaffUserId,
                    it.MemberWechatId,
                    it.MemberWechatNo,
                    it.UsualStyleItemId,
                    it.Status,
                    it.Remark,
                    it.Update_by,
                    it.Update_time
                })
                .ExecuteCommand();
                if (dto.ReferrersTouched) SyncReferrers(actor, current, referrerPlans);
            });
            if (!tranEdit.IsSuccess) throw new CustomException(tranEdit.ErrorMessage ?? "会员档案保存失败");
            return current.Id;
        }

        /// <summary>
        /// 在当前事务内创建会员（不新开事务），供销售定制等业务自动建档使用。
        /// 调用方必须已处于 UseTran 事务中，发生异常时随外层事务一起回滚。
        /// </summary>
        public long CreateInCurrentTransaction(JyActor actor, long? storeId, string name, string phone, int sex)
        {
            EnsureLevels(actor);
            var memberName = JyBillNo.NormalizeName(name, "会员姓名", 50);
            var mobile = NormalizePhone(phone);
            if (string.IsNullOrEmpty(mobile)) throw new CustomException("会员手机号不能为空");
            if (sex is < 0 or > 2) throw new CustomException("性别选项不正确");
            // 事务内按手机号二次校验，避免同一客户重复建档
            var exists = Context.Queryable<JyMember>().First(it => it.TenantId == actor.TenantId
                && it.DelFlag == 0 && it.Phone == mobile);
            if (exists != null)
            {
                if (exists.Status != 0) throw new CustomException("该手机号对应的会员已停用，请先在会员档案中启用");
                return exists.Id;
            }
            var defaultLevelId = ActiveLevels(actor.TenantId).FirstOrDefault()?.Id;
            var issued = _billRule.IssueInTransaction(actor, "member", "jy_member", null);
            return Context.Insertable(new JyMember
            {
                TenantId = actor.TenantId,
                StoreId = storeId > 0 ? storeId : null,
                MemberNo = issued.BillNo,
                MemberName = memberName,
                Sex = sex,
                Phone = mobile,
                BirthCalendar = 1,
                LevelId = defaultLevelId,
                LevelChangedTime = defaultLevelId.HasValue ? DateTime.Now : null,
                FollowStatus = 0,
                StaffUserId = actor.UserId,
                Status = 0,
                DelFlag = 0,
                Create_by = actor.UserName,
                Create_time = DateTime.Now,
                Remark = "销售定制自动建档"
            }).ExecuteReturnBigIdentity();
        }

        public int Delete(JyActor actor, long id)
        {
            Must(actor.TenantId, id);
            if (Context.Queryable<JySaleOrder>().Any(it => it.TenantId == actor.TenantId
                && it.MemberId == id && it.DelFlag == 0))
                throw new CustomException("该会员已有销售记录，不能删除，可改为停用");
            if (Context.Queryable<JyActivityMember>().Any(it => it.TenantId == actor.TenantId && it.MemberId == id))
                throw new CustomException("该会员已有活动参与记录，不能删除，可改为停用");
            return Context.Updateable<JyMember>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        public List<JyMemberReferrerDto> Referrers(JyActor actor, long id)
        {
            Must(actor.TenantId, id);
            return BuildReferrers(actor.TenantId, new List<long> { id }).GetValueOrDefault(id)
                ?? new List<JyMemberReferrerDto>();
        }

        public long AddReferrer(JyActor actor, long id, JyMemberReferrerEditDto dto)
        {
            if (dto == null) throw new CustomException("推荐人信息不能为空");
            Must(actor.TenantId, id);
            var plans = NormalizeReferrers(actor,
                new List<JyMemberReferrerEditDto> { dto }, null, id);
            if (plans.Count == 0) throw new CustomException("请选择推荐人或填写推荐人微信");
            var plan = plans[0];
            var active = Context.Queryable<JyMemberReferrer>()
                .Where(it => it.TenantId == actor.TenantId && it.MemberId == id && it.DelFlag == 0)
                .ToList();
            if (active.Any(it => it.ReferrerMemberId == plan.ReferrerMemberId))
                throw new CustomException("该推荐人已在客户的关系记录中");
            var isPrimary = active.Count == 0 || dto.IsPrimary;
            var now = DateTime.Now;
            // 若该关系此前被逻辑隐藏，恢复历史行时也必须取消其他默认标记，保证只存在一个默认。
            if (isPrimary)
            {
                Context.Updateable<JyMemberReferrer>()
                    .SetColumns(it => it.IsPrimary == 0)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.TenantId == actor.TenantId && it.MemberId == id
                        && it.DelFlag == 0 && it.ReferrerMemberId != plan.ReferrerMemberId)
                    .ExecuteCommand();
            }
            plan.IsPrimary = isPrimary;
            SaveReferrers(actor, id, new List<JyMemberReferrerEditDto> { plan }, 3, now: now);
            if (isPrimary)
            {
                Context.Updateable<JyMember>()
                    .SetColumns(it => it.ReferrerMemberId == plan.ReferrerMemberId)
                    .SetColumns(it => it.ReferrerWechatNo == plan.ReferrerWechatNo)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
            }
            return Context.Queryable<JyMemberReferrer>()
                .Where(it => it.TenantId == actor.TenantId && it.MemberId == id
                    && it.ReferrerMemberId == plan.ReferrerMemberId && it.DelFlag == 0)
                .Select(it => it.Id).First();
        }

        public int SetPrimaryReferrer(JyActor actor, long id, long linkId)
        {
            Must(actor.TenantId, id);
            var link = Context.Queryable<JyMemberReferrer>()
                .First(it => it.Id == linkId && it.TenantId == actor.TenantId
                    && it.MemberId == id && it.DelFlag == 0)
                ?? throw new CustomException("推荐关系不存在");
            var now = DateTime.Now;
            var tran = UseTran(() =>
            {
                Context.Updateable<JyMemberReferrer>()
                    .SetColumns(it => it.IsPrimary == 0)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.TenantId == actor.TenantId && it.MemberId == id && it.DelFlag == 0)
                    .ExecuteCommand();
                Context.Updateable<JyMemberReferrer>()
                    .SetColumns(it => it.IsPrimary == 1)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == linkId && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
                if (link.Source != 2 || !link.SaleOrderId.HasValue)
                {
                    Context.Updateable<JyMemberReferrer>()
                        .SetColumns(it => it.Source == 3)
                        .Where(it => it.Id == linkId && it.TenantId == actor.TenantId)
                        .ExecuteCommand();
                }
                var referrer = Context.Queryable<JyMember>().First(it => it.Id == link.ReferrerMemberId);
                Context.Updateable<JyMember>()
                    .SetColumns(it => it.ReferrerMemberId == link.ReferrerMemberId)
                    .SetColumns(it => it.ReferrerWechatNo == (referrer == null ? null : referrer.WechatNo))
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "设置主推荐人失败");
            return 1;
        }

        /// <summary>只隐藏关系，不物理删除；销售单与返现台账保留原推荐人。</summary>
        public int RemoveReferrer(JyActor actor, long id, long linkId)
        {
            Must(actor.TenantId, id);
            var link = Context.Queryable<JyMemberReferrer>()
                .First(it => it.Id == linkId && it.TenantId == actor.TenantId
                    && it.MemberId == id && it.DelFlag == 0)
                ?? throw new CustomException("推荐关系不存在");
            var now = DateTime.Now;
            var tran = UseTran(() =>
            {
                Context.Updateable<JyMemberReferrer>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == linkId && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
                if (link.IsPrimary == 1)
                {
                    var next = Context.Queryable<JyMemberReferrer>()
                        .Where(it => it.TenantId == actor.TenantId && it.MemberId == id
                            && it.DelFlag == 0 && it.Id != linkId)
                        .OrderBy(it => it.Id).First();
                    if (next != null)
                    {
                        Context.Updateable<JyMemberReferrer>()
                            .SetColumns(it => it.IsPrimary == 1)
                            .Where(it => it.Id == next.Id && it.TenantId == actor.TenantId)
                            .ExecuteCommand();
                    }
                    var referrer = next == null ? null : Context.Queryable<JyMember>().First(it => it.Id == next.ReferrerMemberId);
                    Context.Updateable<JyMember>()
                        .SetColumns(it => it.ReferrerMemberId == (next == null ? null : (long?)next.ReferrerMemberId))
                        .SetColumns(it => it.ReferrerWechatNo == (referrer == null ? null : referrer.WechatNo))
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == id && it.TenantId == actor.TenantId)
                        .ExecuteCommand();
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "移除推荐关系失败");
            return 1;
        }

        public List<JyMemberLevelDto> Levels(JyActor actor)
        {
            EnsureLevels(actor);
            return LoadLevels(actor.TenantId).Select(ToLevelDto).ToList();
        }

        public long SaveLevel(JyActor actor, JyMemberLevelEditDto dto)
        {
            if (dto == null) throw new CustomException("会员等级不能为空");
            EnsureLevels(actor);
            dto.LevelCode = JyBillNo.NormalizeCode(dto.LevelCode, "等级编码");
            dto.LevelName = JyBillNo.NormalizeName(dto.LevelName, "等级名称", 50);
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.MinPoints < 0) throw new CustomException("起始积分不能为负数");
            if (dto.MaxPoints.HasValue && dto.MaxPoints.Value < dto.MinPoints)
                throw new CustomException("结束积分不能小于起始积分");
            if (dto.DiscountRate <= 0 || dto.DiscountRate > 1)
                throw new CustomException("折扣率必须大于 0 且不超过 1");
            if (dto.SortNo < 0) throw new CustomException("排序号不能为负数");
            if (dto.KeepOnYearClear is not (0 or 1)) throw new CustomException("年度清零设置不正确");
            if (dto.Status is not (0 or 1)) throw new CustomException("等级状态不正确");
            if (Context.Queryable<JyMemberLevel>().Any(it => it.TenantId == actor.TenantId
                && it.LevelCode == dto.LevelCode && it.DelFlag == 0 && it.Id != dto.Id))
                throw new CustomException("等级编码已存在");
            if (dto.Status == 0)
            {
                var others = Context.Queryable<JyMemberLevel>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && it.Status == 0 && it.Id != dto.Id)
                    .ToList();
                var newMax = dto.MaxPoints ?? decimal.MaxValue;
                if (others.Any(it => dto.MinPoints <= (it.MaxPoints ?? decimal.MaxValue)
                    && it.MinPoints <= newMax))
                    throw new CustomException("积分区间与现有等级重叠，请调整起止积分");
            }

            if (dto.Id <= 0)
            {
                return Context.Insertable(new JyMemberLevel
                {
                    TenantId = actor.TenantId,
                    LevelCode = dto.LevelCode,
                    LevelName = dto.LevelName,
                    MinPoints = dto.MinPoints,
                    MaxPoints = dto.MaxPoints,
                    DiscountRate = dto.DiscountRate,
                    SortNo = dto.SortNo,
                    KeepOnYearClear = dto.KeepOnYearClear,
                    Status = dto.Status,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = dto.Remark
                }).ExecuteReturnBigIdentity();
            }

            var current = Context.Queryable<JyMemberLevel>().First(it => it.Id == dto.Id
                && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("会员等级不存在");
            current.LevelCode = dto.LevelCode;
            current.LevelName = dto.LevelName;
            current.MinPoints = dto.MinPoints;
            current.MaxPoints = dto.MaxPoints;
            current.DiscountRate = dto.DiscountRate;
            current.SortNo = dto.SortNo;
            current.KeepOnYearClear = dto.KeepOnYearClear;
            current.Status = dto.Status;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current)
                .UpdateColumns(it => new
                {
                    it.LevelCode,
                    it.LevelName,
                    it.MinPoints,
                    it.MaxPoints,
                    it.DiscountRate,
                    it.SortNo,
                    it.KeepOnYearClear,
                    it.Status,
                    it.Remark,
                    it.Update_by,
                    it.Update_time
                })
                .ExecuteCommand();
            return current.Id;
        }

        public int AdjustPoints(JyActor actor, JyPointAdjustDto dto)
        {
            if (dto == null || dto.MemberId <= 0) throw new CustomException("请选择会员");
            if (dto.ChangePoints == 0) throw new CustomException("调整积分不能为 0");
            dto.Reason = (dto.Reason ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(dto.Reason)) throw new CustomException("请填写积分调整原因");
            if (dto.Reason.Length > 200) throw new CustomException("积分调整原因不能超过 200 个字");
            EnsureLevels(actor);
            var affected = 0;
            var tran = UseTran(() =>
            {
                var member = Context.Queryable<JyMember>()
                    .With(SqlWith.UpdLock)
                    .First(it => it.Id == dto.MemberId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (member == null) throw new CustomException("会员不存在");
                if (member.Status != 0) throw new CustomException("会员已停用，不能调整积分");
                var newBalance = member.PointsBalance + dto.ChangePoints;
                if (newBalance < 0) throw new CustomException("调整后积分不能为负数");
                var newYearPoints = Math.Max(0, member.YearPoints + dto.ChangePoints);
                var now = DateTime.Now;
                var newLevelId = ResolveUpgradedLevel(actor, member, newYearPoints);
                affected = Context.Updateable<JyMember>()
                    .SetColumns(it => it.PointsBalance == newBalance)
                    .SetColumns(it => it.YearPoints == newYearPoints)
                    .SetColumnsIF(newLevelId.HasValue, it => it.LevelId == newLevelId.Value)
                    .SetColumnsIF(newLevelId.HasValue, it => it.LevelChangedTime == now)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == member.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("会员信息已变化，请刷新后重试");
                Context.Insertable(new JyPointLedger
                {
                    TenantId = actor.TenantId,
                    MemberId = member.Id,
                    ChangeType = 3,
                    ChangePoints = dto.ChangePoints,
                    BalanceAfter = newBalance,
                    YearPointsAfter = newYearPoints,
                    OperatorId = actor.UserId,
                    Reason = dto.Reason,
                    BizTime = now,
                    Create_time = now
                }).ExecuteCommand();
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "积分调整失败");
            return affected;
        }

        public PagedInfo<JyPointLedgerDto> Points(JyActor actor, JyPointLedgerQueryDto query)
        {
            query ??= new JyPointLedgerQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var q = Context.Queryable<JyPointLedger>()
                .Where(it => it.TenantId == actor.TenantId)
                .WhereIF(query.MemberId > 0, it => it.MemberId == query.MemberId)
                .WhereIF(query.ChangeType.HasValue, it => it.ChangeType == query.ChangeType);
            var total = 0;
            var rows = q.OrderBy(it => it.BizTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            var memberIds = rows.Select(it => it.MemberId).Distinct().ToList();
            var members = memberIds.Count == 0 ? new Dictionary<long, JyMember>() : Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && memberIds.Contains(it.Id))
                .ToList().ToDictionary(it => it.Id);
            return new PagedInfo<JyPointLedgerDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(it =>
                {
                    members.TryGetValue(it.MemberId, out var member);
                    return new JyPointLedgerDto
                    {
                        Id = it.Id,
                        MemberId = it.MemberId,
                        MemberNo = member?.MemberNo,
                        MemberName = member?.MemberName,
                        ChangeType = it.ChangeType,
                        ChangePoints = it.ChangePoints,
                        BalanceAfter = it.BalanceAfter,
                        YearPointsAfter = it.YearPointsAfter,
                        Reason = it.Reason,
                        BizTime = it.BizTime
                    };
                }).ToList()
            };
        }

        /// <summary>会员历史留痕：档案、最近消费单与积分流水。</summary>
        public JyMemberHistoryDto GetHistory(JyActor actor, long memberId)
        {
            EnsureLevels(actor);
            var member = Must(actor.TenantId, memberId);
            var memberDto = ToDtos(actor.TenantId, new List<JyMember> { member }).First();
            var sales = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.MemberId == memberId)
                .OrderBy(it => it.DealTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .Take(100).ToList();
            var points = Context.Queryable<JyPointLedger>()
                .Where(it => it.TenantId == actor.TenantId && it.MemberId == memberId)
                .OrderBy(it => it.BizTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .Take(100).ToList();
            return new JyMemberHistoryDto
            {
                Member = memberDto,
                Sales = sales.Select(it => new JySaleDto
                {
                    Id = it.Id,
                    OrderNo = it.OrderNo,
                    OrderType = it.OrderType,
                    MemberId = it.MemberId,
                    MemberName = member.MemberName,
                    MemberNo = member.MemberNo,
                    ActivityId = it.ActivityId,
                    Status = it.Status,
                    TotalAmount = it.TotalAmount,
                    DiscountAmount = it.DiscountAmount,
                    ActivityBenefit = it.ActivityBenefit,
                    BasePayAmount = it.BasePayAmount,
                    AdjustAmount = it.AdjustAmount,
                    RoundOffAmount = it.RoundOffAmount,
                    PayAmount = it.PayAmount,
                    PieceQty = it.PieceQty,
                    CustomerName = it.CustomerName ?? member.MemberName,
                    CustomerPhone = it.CustomerPhone ?? member.Phone,
                    CustomerSex = it.CustomerName != null ? it.CustomerSex : member.Sex,
                    ReturnedAmount = it.ReturnedAmount,
                    ReturnStatus = it.ReturnStatus,
                    DealTime = it.DealTime,
                    Remark = it.Remark,
                    CreateBy = it.Create_by,
                    CreateTime = it.Create_time
                }).ToList(),
                Points = points.Select(it => new JyPointLedgerDto
                {
                    Id = it.Id,
                    MemberId = it.MemberId,
                    MemberNo = member.MemberNo,
                    MemberName = member.MemberName,
                    ChangeType = it.ChangeType,
                    ChangePoints = it.ChangePoints,
                    BalanceAfter = it.BalanceAfter,
                    YearPointsAfter = it.YearPointsAfter,
                    Reason = it.Reason,
                    BizTime = it.BizTime
                }).ToList()
            };
        }

        /// <summary>读取当前积分规则；没有维护过时返回 1 元 1 积分的默认值。</summary>
        public JyPointRuleDto GetPointRule(JyActor actor)
        {
            var rule = Context.Queryable<JyPointRule>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 0)
                .OrderBy(it => it.Id, OrderByType.Desc).First();
            if (rule == null)
            {
                return new JyPointRuleDto
                {
                    AmountBase = 1m,
                    PointsAwarded = 1m,
                    CashBase = 0m,
                    CashAwarded = 0m,
                    ReferralCashRate = 0m,
                    Status = 0,
                    Remark = "默认规则：每消费 1 元累计 1 积分"
                };
            }
            return new JyPointRuleDto
            {
                Id = rule.Id,
                AmountBase = rule.AmountBase,
                PointsAwarded = rule.PointsAwarded,
                CashBase = rule.CashBase,
                CashAwarded = rule.CashAwarded,
                ReferralCashRate = rule.ReferralCashRate,
                Status = rule.Status,
                Remark = rule.Remark,
                CreateBy = rule.Create_by,
                CreateTime = rule.Create_time,
                UpdateTime = rule.Update_time
            };
        }

        /// <summary>维护唯一当前积分规则。保存启用规则时自动停用其他规则。</summary>
        public long SavePointRule(JyActor actor, JyPointRuleEditDto dto)
        {
            if (dto == null) throw new CustomException("积分规则不能为空");
            if (dto.AmountBase <= 0) throw new CustomException("金额基数必须大于 0");
            if (dto.AmountBase > 1000000m) throw new CustomException("金额基数不能超过 1000000");
            if (dto.PointsAwarded < 0) throw new CustomException("奖励积分不能为负数");
            if (dto.PointsAwarded > 1000000m) throw new CustomException("奖励积分不能超过 1000000");
            if (dto.CashBase < 0) throw new CustomException("返现基数不能为负数");
            if (dto.CashAwarded < 0) throw new CustomException("返现金额不能为负数");
            if (dto.ReferralCashRate is < 0 or > 100) throw new CustomException("返现比例必须在 0 到 100 之间");
            dto.Remark = TrimRemark(dto.Remark);
            var ruleId = 0L;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var current = dto.Id > 0
                    ? Context.Queryable<JyPointRule>().With(SqlWith.UpdLock)
                        .First(it => it.Id == dto.Id && it.TenantId == actor.TenantId)
                    : Context.Queryable<JyPointRule>().With(SqlWith.UpdLock)
                        .Where(it => it.TenantId == actor.TenantId && it.Status == 0)
                        .OrderBy(it => it.Id, OrderByType.Desc).First();
                if (current != null)
                {
                    Context.Updateable<JyPointRule>()
                        .SetColumns(it => it.AmountBase == dto.AmountBase)
                        .SetColumns(it => it.PointsAwarded == dto.PointsAwarded)
                        .SetColumns(it => it.CashBase == dto.CashBase)
                        .SetColumns(it => it.CashAwarded == dto.CashAwarded)
                        .SetColumns(it => it.ReferralCashRate == dto.ReferralCashRate)
                        .SetColumns(it => it.Status == (dto.Status == 0 ? 0 : 1))
                        .SetColumns(it => it.Remark == dto.Remark)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == current.Id && it.TenantId == actor.TenantId)
                        .ExecuteCommand();
                    ruleId = current.Id;
                }
                else
                {
                    ruleId = Context.Insertable(new JyPointRule
                    {
                        TenantId = actor.TenantId,
                        AmountBase = dto.AmountBase,
                        PointsAwarded = dto.PointsAwarded,
                        CashBase = dto.CashBase,
                        CashAwarded = dto.CashAwarded,
                        ReferralCashRate = dto.ReferralCashRate,
                        Status = dto.Status == 0 ? 0 : 1,
                        Remark = dto.Remark,
                        Create_by = actor.UserName,
                        Create_time = now,
                        Update_by = actor.UserName,
                        Update_time = now
                    }).ExecuteReturnBigIdentity();
                }
                if (dto.Status == 0)
                {
                    Context.Updateable<JyPointRule>()
                        .SetColumns(it => it.Status == 1)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.TenantId == actor.TenantId && it.Id != ruleId && it.Status == 0)
                        .ExecuteCommand();
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "积分规则保存失败");
            return ruleId;
        }

        /// <summary>全库清零实时可用积分；累计积分、年度积分、等级与累计消费保持不变。</summary>
        public JyPointClearResultDto ClearAllPoints(JyActor actor)
        {
            var now = DateTime.Now;
            var batchNo = "CLR" + now.ToString("yyyyMMddHHmmssfff");
            var result = new JyPointClearResultDto { BatchNo = batchNo, ClearTime = now };
            var tran = UseTran(() =>
            {
                var members = Context.Queryable<JyMember>()
                    .With(SqlWith.UpdLock)
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.PointsBalance > 0)
                    .ToList();
                if (members.Count == 0) return;
                result.MemberCount = members.Count;
                result.ClearedPoints = members.Sum(it => it.PointsBalance);
                var ledger = members.Select(it => new JyPointLedger
                {
                    TenantId = actor.TenantId,
                    MemberId = it.Id,
                    ChangeType = 4,
                    ChangePoints = -it.PointsBalance,
                    BalanceAfter = 0,
                    YearPointsAfter = it.YearPoints,
                    OperatorId = actor.UserId,
                    Reason = "实时积分清零 " + batchNo + " 操作人:" + actor.UserName,
                    BizTime = now,
                    Create_time = now
                }).ToList();
                foreach (var chunk in ledger.Chunk(100))
                {
                    Context.Insertable(chunk.ToList()).ExecuteCommand();
                }
                foreach (var chunk in members.Select(it => it.Id).Chunk(500))
                {
                    var ids = chunk.ToList();
                    Context.Updateable<JyMember>()
                        .SetColumns(it => it.PointsBalance == 0)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.TenantId == actor.TenantId && ids.Contains(it.Id) && it.DelFlag == 0)
                        .ExecuteCommand();
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "积分清零失败");
            return result;
        }

        private void EnsureLevels(JyActor actor)
        {
            if (Context.Queryable<JyMemberLevel>().Any(it => it.TenantId == actor.TenantId && it.DelFlag == 0))
                return;
            foreach (var seed in DefaultLevels)
            {
                Context.Insertable(new JyMemberLevel
                {
                    TenantId = actor.TenantId,
                    LevelCode = seed.Code,
                    LevelName = seed.Name,
                    MinPoints = seed.Min,
                    MaxPoints = seed.Max,
                    DiscountRate = seed.Discount,
                    SortNo = seed.Sort,
                    KeepOnYearClear = 1,
                    Status = 0,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = "系统预置等级"
                }).ExecuteCommand();
            }
        }

        private List<JyMemberLevel> LoadLevels(string tenantId)
        {
            return Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList();
        }

        private List<JyMemberLevelDto> ActiveLevels(string tenantId)
        {
            return LoadLevels(tenantId).Where(it => it.Status == 0).Select(ToLevelDto).ToList();
        }

        private long? ResolveUpgradedLevel(JyActor actor, JyMember member, decimal yearPoints)
        {
            var levels = LoadLevels(actor.TenantId).Where(it => it.Status == 0).ToList();
            var match = levels.Where(it => it.MinPoints <= yearPoints
                && (!it.MaxPoints.HasValue || yearPoints <= it.MaxPoints.Value))
                .OrderByDescending(it => it.SortNo).FirstOrDefault();
            if (match == null) return null;
            var current = member.LevelId.HasValue ? levels.FirstOrDefault(it => it.Id == member.LevelId.Value) : null;
            return current == null || match.SortNo > current.SortNo ? match.Id : null;
        }

        private List<JyMemberDto> ToDtos(string tenantId, List<JyMember> rows)
        {
            if (rows.Count == 0) return new List<JyMemberDto>();
            var memberIds = rows.Select(it => it.Id).ToList();
            var levelIds = rows.Where(it => it.LevelId.HasValue).Select(it => it.LevelId.Value).Distinct().ToList();
            var styleIds = rows.Where(it => it.UsualStyleItemId.HasValue).Select(it => it.UsualStyleItemId.Value).Distinct().ToList();
            var levels = levelIds.Count == 0 ? new Dictionary<long, JyMemberLevel>() : Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == tenantId && levelIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            var styles = styleIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && styleIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id, it => it.ItemName);
            var referrerIds = rows.Where(it => it.ReferrerMemberId.HasValue)
                .Select(it => it.ReferrerMemberId.Value).Distinct().ToList();
            var referrerNames = referrerIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyMember>()
                .Where(it => it.TenantId == tenantId && referrerIds.Contains(it.Id))
                .Select(it => new { it.Id, it.MemberName }).ToList().ToDictionary(it => it.Id, it => it.MemberName);
            var referrerMap = BuildReferrers(tenantId, memberIds);
            var preferences = BuildStylePreferences(tenantId, memberIds);
            var latestFollows = BuildLatestFollows(tenantId, memberIds);
            var memberWechatIds = rows.Where(it => it.MemberWechatId.HasValue)
                .Select(it => it.MemberWechatId.Value).Distinct().ToList();
            var memberWechats = memberWechatIds.Count == 0
                ? new Dictionary<long, SysUserWechat>()
                : Context.Queryable<SysUserWechat>()
                    .Where(it => memberWechatIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            var memberWechatUserIds = memberWechats.Values.Select(it => it.UserId).Distinct().ToList();
            var memberWechatUsers = memberWechatUserIds.Count == 0
                ? new Dictionary<long, SysUser>()
                : Context.Queryable<SysUser>()
                    .Where(it => memberWechatUserIds.Contains(it.UserId)).ToList().ToDictionary(it => it.UserId);
            return rows.Select(it =>
            {
                levels.TryGetValue(it.LevelId ?? 0, out var level);
                preferences.TryGetValue(it.Id, out var memberPreferences);
                latestFollows.TryGetValue(it.Id, out var latestFollow);
                memberWechats.TryGetValue(it.MemberWechatId ?? 0, out var memberWechat);
                var memberWechatUser = memberWechat == null ? null : memberWechatUsers.GetValueOrDefault(memberWechat.UserId);
                return new JyMemberDto
                {
                    Id = it.Id,
                    MemberNo = it.MemberNo,
                    MemberName = it.MemberName,
                    Sex = it.Sex,
                    Phone = it.Phone,
                    WechatNo = it.WechatNo,
                    ReferrerWechatNo = it.ReferrerWechatNo,
                    ReferrerMemberId = it.ReferrerMemberId,
                    ReferrerName = it.ReferrerMemberId.HasValue && referrerNames.TryGetValue(it.ReferrerMemberId.Value, out var rn) ? rn : null,
                    Referrers = referrerMap.GetValueOrDefault(it.Id) ?? new List<JyMemberReferrerDto>(),
                    SolarBirthday = it.SolarBirthday,
                    LunarBirthday = it.LunarBirthday,
                    BirthCalendar = it.BirthCalendar,
                    BirthYear = it.BirthYear,
                    Occupation = it.Occupation,
                    Hobby = it.Hobby,
                    WuXing = it.WuXing,
                    RecommendDirection = it.RecommendDirection,
                    LevelId = it.LevelId,
                    LevelName = level?.LevelName ?? "未分级",
                    DiscountRate = level?.DiscountRate ?? 1m,
                    PointsBalance = it.PointsBalance,
                    YearPoints = it.YearPoints,
                    MemberWechatId = it.MemberWechatId,
                    MemberWechatNo = it.MemberWechatNo,
                    MemberWechatStaffName = memberWechatUser?.NickName ?? memberWechatUser?.UserName,
                    FollowStatus = it.FollowStatus,
                    YearAmount = it.YearAmount,
                    TotalAmount = it.TotalAmount,
                    OrderCount = it.OrderCount,
                    AvgTicket = it.AvgTicket,
                    UsualStyleName = it.UsualStyleItemId.HasValue && styles.TryGetValue(it.UsualStyleItemId.Value, out var style) ? style : null,
                    StylePreferences = memberPreferences ?? new List<JyMemberStylePreferenceDto>(),
                    LastFollowTime = latestFollow?.FollowTime,
                    LastFollowTypeName = latestFollow?.FollowTypeName,
                    LastFollowContent = latestFollow?.Content,
                    FollowCount = latestFollow?.FollowCount ?? 0,
                    LastConsumeTime = it.LastConsumeTime,
                    Status = it.Status,
                    Remark = it.Remark,
                    CreateTime = it.Create_time
                };
            }).ToList();
        }

        /// <summary>按已完成销售单汇总客户在一级款式分类下的实付金额与件数。</summary>
        private Dictionary<long, List<JyMemberStylePreferenceDto>> BuildStylePreferences(string tenantId, List<long> memberIds)
        {
            var result = new Dictionary<long, List<JyMemberStylePreferenceDto>>();
            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && it.Status == 1
                    && it.MemberId.HasValue && memberIds.Contains(it.MemberId.Value))
                .Select(it => new { it.Id, MemberId = it.MemberId.Value, it.PayAmount })
                .ToList();
            if (orders.Count == 0) return result;
            var orderMap = orders.ToDictionary(it => it.Id);
            var orderIds = orderMap.Keys.ToList();
            var lines = Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == tenantId && orderIds.Contains(it.OrderId))
                .Select(it => new { it.OrderId, it.StyleItemId, it.StyleItemName, it.Qty, it.DealPrice, it.IsGift })
                .ToList();
            if (lines.Count == 0) return result;

            var styleIds = lines.Where(it => it.StyleItemId.HasValue).Select(it => it.StyleItemId.Value).Distinct().ToList();
            var styleRows = styleIds.Count == 0 ? new List<JyLookupItem>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && styleIds.Contains(it.Id)).ToList();
            var styleMap = styleRows.ToDictionary(it => it.Id);
            var allIds = styleRows.SelectMany(it => new[] { it.Id, it.ParentId ?? 0 }).Distinct().ToList();
            var allLookupRows = allIds.Count == 0 ? styleRows : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && allIds.Contains(it.Id)).ToList().Concat(styleRows).ToList();
            var allRows = allLookupRows.GroupBy(it => it.Id).ToDictionary(g => g.Key, g => g.First());

            foreach (var group in lines.GroupBy(it => orderMap[it.OrderId].MemberId))
            {
                var orderCounts = new Dictionary<long, HashSet<long>>();
                var buckets = new Dictionary<long, (string Name, decimal Amount, decimal Qty)>();
                foreach (var line in group)
                {
                    if (!line.StyleItemId.HasValue) continue;
                    var style = styleMap.GetValueOrDefault(line.StyleItemId.Value);
                    if (style == null) continue;
                    var root = style.ParentId.HasValue && allRows.TryGetValue(style.ParentId.Value, out var parent) ? parent : style;
                    var amount = line.IsGift == 1 ? 0 : line.DealPrice * line.Qty;
                    if (buckets.TryGetValue(root.Id, out var bucket))
                        buckets[root.Id] = (root.ItemName, bucket.Amount + amount, bucket.Qty + line.Qty);
                    else
                        buckets[root.Id] = (root.ItemName, amount, line.Qty);
                    if (!orderCounts.TryGetValue(root.Id, out var set)) orderCounts[root.Id] = set = new HashSet<long>();
                    set.Add(line.OrderId);
                }
                result[group.Key] = buckets
                    .Select(it => new JyMemberStylePreferenceDto
                    {
                        StyleItemId = it.Key,
                        StyleName = it.Value.Name,
                        Amount = it.Value.Amount,
                        Qty = it.Value.Qty,
                        OrderCount = orderCounts.GetValueOrDefault(it.Key)?.Count ?? 0
                    })
                    .OrderByDescending(it => it.Amount).ThenByDescending(it => it.Qty)
                    .ToList();
            }
            return result;
        }

        private sealed record LatestFollow(DateTime FollowTime, string FollowTypeName, string Content, int FollowCount);

        private Dictionary<long, LatestFollow> BuildLatestFollows(string tenantId, List<long> memberIds)
        {
            var result = new Dictionary<long, LatestFollow>();
            var rows = Context.Queryable<JyMemberFollow>()
                .Where(it => it.TenantId == tenantId && memberIds.Contains(it.MemberId))
                .OrderBy(it => it.FollowTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .Select(it => new { it.Id, it.MemberId, it.FollowTime, it.FollowType, it.FollowTypeName, it.Content })
                .ToList();
            foreach (var group in rows.GroupBy(it => it.MemberId))
            {
                var first = group.First();
                var name = string.IsNullOrWhiteSpace(first.FollowTypeName)
                    ? first.FollowType switch { 1 => "电话沟通", 2 => "微信沟通", 3 => "邀约到店", 4 => "短信触达", _ => "其他" }
                    : first.FollowTypeName;
                result[group.Key] = new LatestFollow(first.FollowTime, name, first.Content, group.Count());
            }
            return result;
        }

        private JyMember Must(string tenantId, long id)
        {
            return Context.Queryable<JyMember>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("会员不存在");
        }

        /// <summary>维护客户的多位推荐人；同一个推荐人只保留一条有效关系。</summary>
        private List<JyMemberReferrerEditDto> NormalizeReferrers(JyActor actor, List<JyMemberReferrerEditDto> items,
            string legacyWechat, long memberId)
        {
            var plans = new List<JyMemberReferrerEditDto>();
            var seen = new HashSet<long>();
            foreach (var item in items ?? new List<JyMemberReferrerEditDto>())
            {
                if (item == null) continue;
                var referrer = ResolveReferrerMember(actor, item.ReferrerMemberId, item.ReferrerWechatNo);
                if (referrer == null) continue;
                if (memberId > 0 && referrer.Id == memberId) throw new CustomException("推荐人不能是客户本人");
                if (!seen.Add(referrer.Id)) continue;
                plans.Add(new JyMemberReferrerEditDto
                {
                    ReferrerMemberId = referrer.Id,
                    ReferrerWechatNo = referrer.WechatNo,
                    IsPrimary = item.IsPrimary,
                    Remark = EmptyOrMax(item.Remark, 200, "推荐关系备注")
                });
            }
            if (plans.Count == 0 && !string.IsNullOrWhiteSpace(legacyWechat))
            {
                var referrer = ResolveReferrerMember(actor, 0, legacyWechat);
                if (referrer != null)
                {
                    if (memberId > 0 && referrer.Id == memberId) throw new CustomException("推荐人不能是客户本人");
                    plans.Add(new JyMemberReferrerEditDto
                    {
                        ReferrerMemberId = referrer.Id,
                        ReferrerWechatNo = referrer.WechatNo,
                        IsPrimary = true
                    });
                }
            }
            if (plans.Count > 0)
            {
                var primaryIndex = plans.FindIndex(it => it.IsPrimary);
                if (primaryIndex < 0) primaryIndex = 0;
                for (var i = 0; i < plans.Count; i++) plans[i].IsPrimary = i == primaryIndex;
            }
            return plans;
        }

        private JyMember ResolveReferrerMember(JyActor actor, long memberId, string wechatNo)
        {
            if (memberId > 0)
            {
                return Context.Queryable<JyMember>()
                    .First(it => it.Id == memberId && it.TenantId == actor.TenantId
                        && it.DelFlag == 0 && it.Status == 0)
                    ?? throw new CustomException("所选推荐人不存在或已停用");
            }
            var wechat = NormalizeWechat(wechatNo);
            if (string.IsNullOrEmpty(wechat)) return null;
            var matches = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                    && it.Status == 0 && it.WechatNo == wechat).ToList();
            if (matches.Count == 0)
                throw new CustomException($"未找到微信号为 {wechat} 的推荐人，请先在会员档案中维护该客户微信号");
            if (matches.Count > 1)
                throw new CustomException($"微信号 {wechat} 对应多个会员，请先修正会员档案中的重复微信号");
            return matches[0];
        }

        /// <summary>
        /// 客户档案关联员工微信。必须来自用户管理维护的启用微信，不能手填；
        /// 同时校验所属员工账号有效，避免把客户挂到离职或停用账号。
        /// </summary>
        private SysUserWechat ResolveMemberWechat(long? memberWechatId)
        {
            if (!memberWechatId.HasValue || memberWechatId.Value <= 0) return null;
            var wechat = Context.Queryable<SysUserWechat>()
                .First(it => it.Id == memberWechatId.Value && it.DelFlag == 0 && it.Status == 0)
                ?? throw new CustomException("所选员工微信不存在或已停用");
            var userExists = Context.Queryable<SysUser>()
                .Any(it => it.UserId == wechat.UserId && it.DelFlag == DelFlagEnum.Normal && it.Status == 0);
            if (!userExists) throw new CustomException("该微信所属员工已停用或删除");
            return wechat;
        }

        private void SaveReferrers(JyActor actor, long memberId, List<JyMemberReferrerEditDto> plans,
            int source, long? saleOrderId = null, DateTime? now = null)
        {
            if (plans.Count == 0) return;
            var time = now ?? DateTime.Now;
            foreach (var plan in plans)
            {
                UpsertReferrer(actor, memberId, plan.ReferrerMemberId, source, saleOrderId,
                    plan.IsPrimary, plan.Remark, time);
            }
        }

        /// <summary>
        /// 同一客户与推荐人只保留一条物理记录。关系被逻辑隐藏后再次添加时恢复原行，
        /// 不覆盖来源销售单和历史审计信息，避免唯一索引冲突。
        /// </summary>
        private long UpsertReferrer(JyActor actor, long memberId, long referrerId, int source,
            long? saleOrderId, bool isPrimary, string remark, DateTime now)
        {
            var existing = Context.Queryable<JyMemberReferrer>()
                .First(it => it.TenantId == actor.TenantId && it.MemberId == memberId
                    && it.ReferrerMemberId == referrerId);
            if (existing == null)
            {
                return Context.Insertable(new JyMemberReferrer
                {
                    TenantId = actor.TenantId,
                    MemberId = memberId,
                    ReferrerMemberId = referrerId,
                    Source = source,
                    SaleOrderId = saleOrderId,
                    IsPrimary = isPrimary ? 1 : 0,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = now,
                    Remark = remark
                }).ExecuteReturnBigIdentity();
            }
            Context.Updateable<JyMemberReferrer>()
                .SetColumns(it => it.DelFlag == 0)
                .SetColumns(it => it.IsPrimary == (isPrimary ? 1 : 0))
                .SetColumns(it => it.Remark == remark)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == existing.Id && it.TenantId == actor.TenantId)
                .ExecuteCommand();
            return existing.Id;
        }

        /// <summary>
        /// 编辑档案时同步推荐人：已有关系不做物理删除，未提交的只置 DelFlag=2 隐藏，
        /// 保证历史返现和关系网可追溯。
        /// </summary>
        private void SyncReferrers(JyActor actor, JyMember member, List<JyMemberReferrerEditDto> plans)
        {
            var now = DateTime.Now;
            var existing = Context.Queryable<JyMemberReferrer>()
                .Where(it => it.TenantId == actor.TenantId && it.MemberId == member.Id && it.DelFlag == 0)
                .ToList();
            var keepIds = plans.Select(it => it.ReferrerMemberId).ToHashSet();
            var removeIds = existing.Where(it => !keepIds.Contains(it.ReferrerMemberId)).Select(it => it.Id).ToList();
            if (removeIds.Count > 0)
            {
                Context.Updateable<JyMemberReferrer>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.TenantId == actor.TenantId && removeIds.Contains(it.Id))
                    .ExecuteCommand();
            }
            foreach (var plan in plans)
            {
                var current = existing.FirstOrDefault(it => it.ReferrerMemberId == plan.ReferrerMemberId);
                UpsertReferrer(actor, member.Id, plan.ReferrerMemberId,
                    current?.Source ?? 3, current?.SaleOrderId, plan.IsPrimary, plan.Remark, now);
            }
            var primary = plans.FirstOrDefault(it => it.IsPrimary) ?? plans.FirstOrDefault();
            var primaryId = primary == null ? (long?)null : primary.ReferrerMemberId;
            var primaryWechat = primary?.ReferrerWechatNo;
            member.ReferrerMemberId = primary?.ReferrerMemberId;
            member.ReferrerWechatNo = primary?.ReferrerWechatNo;
            Context.Updateable<JyMember>()
                .SetColumns(it => it.ReferrerMemberId == primaryId)
                .SetColumns(it => it.ReferrerWechatNo == primaryWechat)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == member.Id && it.TenantId == actor.TenantId)
                .ExecuteCommand();
        }

        private Dictionary<long, List<JyMemberReferrerDto>> BuildReferrers(string tenantId, List<long> memberIds)
        {
            var result = new Dictionary<long, List<JyMemberReferrerDto>>();
            if (memberIds.Count == 0) return result;
            var links = Context.Queryable<JyMemberReferrer>()
                .Where(it => it.TenantId == tenantId && memberIds.Contains(it.MemberId) && it.DelFlag == 0)
                .OrderBy(it => it.IsPrimary, OrderByType.Desc).OrderBy(it => it.Id).ToList();
            if (links.Count == 0) return result;
            var referrerIds = links.Select(it => it.ReferrerMemberId).Distinct().ToList();
            var members = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == tenantId && referrerIds.Contains(it.Id))
                .Select(it => new { it.Id, it.MemberNo, it.MemberName, it.WechatNo, it.Phone })
                .ToList().ToDictionary(it => it.Id);
            var orderIds = links.Where(it => it.SaleOrderId.HasValue).Select(it => it.SaleOrderId.Value).Distinct().ToList();
            var orders = orderIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == tenantId && orderIds.Contains(it.Id))
                .Select(it => new { it.Id, it.OrderNo }).ToList().ToDictionary(it => it.Id, it => it.OrderNo);
            foreach (var link in links)
            {
                members.TryGetValue(link.ReferrerMemberId, out var referrer);
                var dto = new JyMemberReferrerDto
                {
                    Id = link.Id,
                    MemberId = link.MemberId,
                    ReferrerMemberId = link.ReferrerMemberId,
                    ReferrerName = referrer?.MemberName,
                    ReferrerNo = referrer?.MemberNo,
                    ReferrerWechatNo = referrer?.WechatNo,
                    ReferrerPhone = referrer?.Phone,
                    IsPrimary = link.IsPrimary,
                    Source = link.Source,
                    SaleOrderId = link.SaleOrderId,
                    SaleOrderNo = link.SaleOrderId.HasValue && orders.TryGetValue(link.SaleOrderId.Value, out var no) ? no : null,
                    CreateTime = link.Create_time,
                    Remark = link.Remark
                };
                if (!result.TryGetValue(link.MemberId, out var list))
                {
                    list = new List<JyMemberReferrerDto>();
                    result[link.MemberId] = list;
                }
                list.Add(dto);
            }
            return result;
        }

        private static JyMemberLevelDto ToLevelDto(JyMemberLevel it)
        {
            return new JyMemberLevelDto
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
            };
        }

        /// <summary>微信号允许字母、数字、下划线、中横线，最长 64 位。空值返回 null。</summary>
        private static string NormalizeWechat(string wechat)
        {
            wechat = wechat?.Trim();
            if (string.IsNullOrEmpty(wechat)) return null;
            if (wechat.Length > 64) throw new CustomException("微信号不能超过 64 个字符");
            foreach (var ch in wechat)
            {
                if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '-' && ch != '.')
                    throw new CustomException("微信号只能包含字母、数字、下划线、中横线或点");
            }
            return wechat;
        }

        /// <summary>按推荐人微信号解析会员 Id；找不到则直接报错，避免推荐关系丢失。</summary>
        private long? ResolveReferrer(JyActor actor, string referrerWechatNo, long? exceptId)
        {
            var wechat = NormalizeWechat(referrerWechatNo);
            if (string.IsNullOrEmpty(wechat)) return null;
            var matches = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0 && it.WechatNo == wechat)
                .Select(it => it.Id).ToList();
            if (exceptId.HasValue) matches = matches.Where(it => it != exceptId.Value).ToList();
            if (matches.Count == 0) throw new CustomException($"未找到微信号为 {wechat} 的推荐人，请先在会员档案中维护该客户的微信号");
            if (matches.Count > 1) throw new CustomException($"微信号 {wechat} 对应多个会员，请先修正会员档案中的重复微信号");
            return matches[0];
        }
        private static string NormalizePhone(string phone)
        {
            phone = phone?.Trim();
            if (string.IsNullOrEmpty(phone)) return null;
            if (phone.Length > 30 || phone.Any(ch => !char.IsDigit(ch) && ch != '-' && ch != '+'))
                throw new CustomException("手机号只能包含数字、加号或中横线，最长 30 位");
            return phone;
        }

        private static string EmptyOrMax(string value, int max, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > max) throw new CustomException($"{label}不能超过 {max} 个字");
            return value;
        }

        private static string TrimRemark(string remark)
        {
            remark = remark?.Trim();
            if (string.IsNullOrEmpty(remark)) return null;
            if (remark.Length > 500) throw new CustomException("备注不能超过 500 个字");
            return remark;
        }
    }
}






