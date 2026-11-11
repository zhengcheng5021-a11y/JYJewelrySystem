using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>
    /// 转介绍奖励服务。
    /// 规则：客户 A 建档时把 B 绑定为被推荐人（B.ReferrerMemberId = A），此后 B 每张
    /// 已完成销售单都会按当前积分规则里的返现口径生成一条奖励台账，永久保留。
    /// </summary>
    [AppService]
    public class JyReferralService : BaseService<JyReferralReward>
    {
        private static readonly string[] StatusText = ["待返现", "已返现", "已作废"];

        public static string StatusName(int status)
            => status >= 0 && status < StatusText.Length ? StatusText[status] : "未知";

        /* ---------------- 台账查询 ---------------- */

        public PagedInfo<JyReferralRewardDto> List(JyActor actor, JyReferralQueryDto query)
        {
            query ??= new JyReferralQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 500);
            var keyword = query.Keyword?.Trim();

            var memberIds = new List<long>();
            if (!string.IsNullOrEmpty(keyword))
            {
                memberIds = Context.Queryable<JyMember>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && (it.MemberName.Contains(keyword) || it.Phone.Contains(keyword)
                            || it.WechatNo.Contains(keyword) || it.ReferrerWechatNo.Contains(keyword)))
                    .Select(it => it.Id).ToList();
            }

            var q = Context.Queryable<JyReferralReward>()
                .Where(it => it.TenantId == actor.TenantId)
                .WhereIF(query.Status.HasValue, it => it.Status == query.Status.Value)
                .WhereIF(query.ReferrerMemberId > 0, it => it.ReferrerMemberId == query.ReferrerMemberId)
                .WhereIF(query.ReferralMemberId > 0, it => it.ReferralMemberId == query.ReferralMemberId)
                .WhereIF(query.BeginTime.HasValue, it => it.BizTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.BizTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it =>
                    it.SaleOrderNo.Contains(keyword)
                    || (memberIds.Count > 0 && (memberIds.Contains(it.ReferrerMemberId) || memberIds.Contains(it.ReferralMemberId))));

            var total = 0;
            var rows = q.OrderBy(it => it.BizTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);

            return new PagedInfo<JyReferralRewardDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDtos(actor.TenantId, rows)
            };
        }

        public JyReferralSummaryDto Summary(JyActor actor, JyReferralQueryDto query)
        {
            query ??= new JyReferralQueryDto();
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var rows = Context.Queryable<JyReferralReward>()
                .Where(it => it.TenantId == actor.TenantId)
                .WhereIF(query.ReferrerMemberId > 0, it => it.ReferrerMemberId == query.ReferrerMemberId)
                .WhereIF(query.BeginTime.HasValue, it => it.BizTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.BizTime < query.EndTime.Value.Date.AddDays(1))
                .Select(it => new { it.Status, it.RewardAmount, it.BizTime })
                .ToList();

            return new JyReferralSummaryDto
            {
                TotalCount = rows.Count,
                TotalAmount = rows.Where(it => it.Status != 2).Sum(it => it.RewardAmount),
                PendingCount = rows.Count(it => it.Status == 0),
                PendingAmount = rows.Where(it => it.Status == 0).Sum(it => it.RewardAmount),
                PaidCount = rows.Count(it => it.Status == 1),
                PaidAmount = rows.Where(it => it.Status == 1).Sum(it => it.RewardAmount),
                VoidCount = rows.Count(it => it.Status == 2),
                VoidAmount = rows.Where(it => it.Status == 2).Sum(it => it.RewardAmount),
                MonthPendingAmount = rows.Where(it => it.Status == 0 && it.BizTime >= monthStart).Sum(it => it.RewardAmount)
            };
        }

        /// <summary>
        /// 结算返现。两种入口：
        /// 1) 传 BeginTime/EndTime：把该区间内所有「待返现」记录一次结清（用户通常不是当天发放，回补时用区间）；
        /// 2) 只传 Ids：结算勾选的记录。
        /// 结算前可逐单调整金额，调整值写入台账并保留系统原始金额。
        /// </summary>
        public JyReferralSettleResultDto Settle(JyActor actor, JyReferralPayoutDto dto)
        {
            if (dto == null) throw new CustomException("请选择需要结算的返现记录");
            var now = DateTime.Now;
            var remark = (dto.PayoutRemark ?? string.Empty).Trim();
            if (remark.Length > 500) throw new CustomException("返现说明不能超过 500 个字");

            var byRange = dto.BeginTime.HasValue || dto.EndTime.HasValue;
            var begin = (dto.BeginTime ?? dto.EndTime ?? now).Date;
            var end = (dto.EndTime ?? dto.BeginTime ?? now).Date;
            if (begin > end) throw new CustomException("结算开始日期不能晚于结束日期");
            if ((end - begin).TotalDays > 366) throw new CustomException("单次结算区间不能超过一年");

            var q = Context.Queryable<JyReferralReward>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 0);
            if (byRange)
            {
                q = q.Where(it => it.BizTime >= begin && it.BizTime < end.AddDays(1));
            }
            else
            {
                var ids = (dto.Ids ?? new List<long>()).Distinct().ToList();
                if (ids.Count == 0) throw new CustomException("请勾选需要结算的返现记录，或选择结算日期区间");
                if (ids.Count > 500) throw new CustomException("单次最多结算 500 条");
                q = q.Where(it => ids.Contains(it.Id));
            }

            var rows = q.ToList();
            if (rows.Count == 0) throw new CustomException("所选范围内没有待返现记录");

            var adjustMap = (dto.Adjusts ?? new List<JyReferralAdjustDto>())
                .GroupBy(it => it.Id).ToDictionary(it => it.Key, it => it.Last());
            var batchNo = "RF" + now.ToString("yyyyMMddHHmmss") + Random.Shared.Next(100, 999);

            var tran = UseTran(() =>
            {
                foreach (var row in rows)
                {
                    var amount = row.RewardAmount;
                    var adjusted = row.IsAdjusted;
                    var rowRemark = row.Remark;
                    if (adjustMap.TryGetValue(row.Id, out var adj))
                    {
                        if (adj.RewardAmount < 0 || adj.RewardAmount > 1000000m)
                            throw new CustomException("调整后的返现金额不合法");
                        if (adj.RewardAmount != row.RewardAmount)
                        {
                            amount = decimal.Round(adj.RewardAmount, 2, MidpointRounding.AwayFromZero);
                            adjusted = 1;
                            var note = (adj.Remark ?? string.Empty).Trim();
                            if (note.Length > 200) throw new CustomException("调整说明不能超过 200 个字");
                            rowRemark = string.IsNullOrEmpty(note)
                                ? $"转介绍返现（人工调整为 {amount:0.00}，系统原值 {(row.OriginalAmount > 0 ? row.OriginalAmount : row.RewardAmount):0.00}）"
                                : note;
                        }
                    }
                    var affected = Context.Updateable<JyReferralReward>()
                        .SetColumns(it => it.Status == 1)
                        .SetColumns(it => it.RewardAmount == amount)
                        .SetColumns(it => it.IsAdjusted == adjusted)
                        .SetColumns(it => it.Remark == rowRemark)
                        .SetColumns(it => it.PayoutTime == now)
                        .SetColumns(it => it.PayoutBy == actor.UserName)
                        .SetColumns(it => it.PayoutRemark == (string.IsNullOrEmpty(remark) ? null : remark))
                        .SetColumns(it => it.SettleBatchNo == batchNo)
                        .SetColumns(it => it.SettleBeginTime == begin)
                        .SetColumns(it => it.SettleEndTime == end)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.TenantId == actor.TenantId && it.Id == row.Id && it.Status == 0)
                        .ExecuteCommand();
                    if (affected != 1) throw new CustomException("返现记录状态已变化，请刷新后重试");
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "保存返现结算失败");

            return new JyReferralSettleResultDto
            {
                BatchNo = batchNo,
                Count = rows.Count,
                Amount = rows.Sum(it => adjustMap.TryGetValue(it.Id, out var a) ? a.RewardAmount : it.RewardAmount),
                BeginTime = begin,
                EndTime = end
            };
        }

        /// <summary>只调整待返现金额，不改变状态；便于先逐单核对、最后按区间统一结算。</summary>
        public decimal AdjustAmount(JyActor actor, long id, decimal amount, string remark)
        {
            if (amount < 0 || amount > 1000000m) throw new CustomException("返现金额不合法");
            remark = (remark ?? string.Empty).Trim();
            if (remark.Length > 200) throw new CustomException("调整说明不能超过 200 个字");
            var row = Context.Queryable<JyReferralReward>()
                .First(it => it.TenantId == actor.TenantId && it.Id == id);
            if (row == null) throw new CustomException("返现记录不存在");
            if (row.Status != 0) throw new CustomException("只有待返现记录可以调整金额");

            amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            var original = row.OriginalAmount > 0 ? row.OriginalAmount : row.RewardAmount;
            var note = string.IsNullOrEmpty(remark)
                ? $"转介绍返现（人工调整为 {amount:0.00}，系统原值 {original:0.00}）"
                : remark;
            var affected = Context.Updateable<JyReferralReward>()
                .SetColumns(it => it.RewardAmount == amount)
                .SetColumns(it => it.IsAdjusted == 1)
                .SetColumns(it => it.Remark == note)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.TenantId == actor.TenantId && it.Id == id && it.Status == 0)
                .ExecuteCommand();
            if (affected != 1) throw new CustomException("返现记录状态已变化，请刷新后重试");
            return amount;
        }
        /// <summary>单条登记（保留旧接口语义），内部走同一套结算逻辑。</summary>
        public int MarkPaid(JyActor actor, JyReferralPayoutDto dto)
        {
            return Settle(actor, dto).Count;
        }
        /// <summary>撤销发放登记（误操作时把已返现改回待返现）。</summary>
        public int CancelPaid(JyActor actor, long id)
        {
            var now = DateTime.Now;
            return Context.Updateable<JyReferralReward>()
                .SetColumns(it => it.Status == 0)
                .SetColumns(it => it.PayoutTime == null)
                .SetColumns(it => it.PayoutBy == null)
                .SetColumns(it => it.PayoutRemark == null)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.TenantId == actor.TenantId && it.Id == id && it.Status == 1)
                .ExecuteCommand();
        }

        /* ---------------- 推荐关系网 ---------------- */

        /// <summary>以某个客户为中心，向上追溯推荐人、向下展开被推荐人，并附带各自单据摘要。</summary>
        public JyReferralGraphDto Graph(JyActor actor, long memberId)
        {
            var focus = Context.Queryable<JyMember>()
                .First(it => it.Id == memberId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("会员不存在");

            var cache = new Dictionary<long, JyMember>();
            cache[focus.Id] = focus;

            JyMember GetMember(long id)
            {
                if (cache.TryGetValue(id, out var cached)) return cached;
                var row = Context.Queryable<JyMember>()
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (row == null) return null;
                cache[id] = row;
                return row;
            }

            // 上行链路：先展开直接推荐人（可能多位），再逐层追溯各自上级，最多 3 层。
            var upChain = new List<JyReferralNodeDto>();
            var visited = new HashSet<long> { focus.Id };
            var frontierUp = new List<long> { focus.Id };
            for (var depth = 1; depth <= 3 && frontierUp.Count > 0; depth++)
            {
                var parentIds = Context.Queryable<JyMemberReferrer>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && frontierUp.Contains(it.MemberId))
                    .OrderBy(it => it.IsPrimary, OrderByType.Desc).OrderBy(it => it.Id)
                    .Select(it => new { it.MemberId, it.ReferrerMemberId }).ToList();
                var next = new List<long>();
                foreach (var link in parentIds)
                {
                    var parent = GetMember(link.ReferrerMemberId);
                    if (parent == null || !visited.Add(parent.Id)) continue;
                    upChain.Add(BuildNode(actor, parent, -depth, link.MemberId, false));
                    next.Add(parent.Id);
                }
                frontierUp = next;
            }

            // 下行链路：BFS 展开，限制 3 层、最多 200 人
            var downList = new List<JyReferralNodeDto>();
            var frontier = new List<JyMember> { focus };
            var downVisited = new HashSet<long> { focus.Id };
            for (var depth = 1; depth <= 3 && frontier.Count > 0 && downList.Count < 200; depth++)
            {
                var ids = frontier.Select(it => it.Id).ToList();
                var links = Context.Queryable<JyMemberReferrer>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && ids.Contains(it.ReferrerMemberId))
                    .OrderBy(it => it.IsPrimary, OrderByType.Desc).OrderBy(it => it.Id).ToList();
                var childIds = links.Select(it => it.MemberId).Distinct().ToList();
                var children = childIds.Count == 0 ? new List<JyMember>() : Context.Queryable<JyMember>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && childIds.Contains(it.Id))
                    .OrderBy(it => it.LastConsumeTime, OrderByType.Desc)
                    .ToList()
                    .Where(it => downVisited.Add(it.Id))
                    .ToList();
                foreach (var child in children)
                {
                    if (downList.Count >= 200) break;
                    cache[child.Id] = child;
                    var parentId = links.FirstOrDefault(it => it.MemberId == child.Id)?.ReferrerMemberId
                        ?? child.ReferrerMemberId;
                    downList.Add(BuildNode(actor, child, depth, parentId, false));
                }
                frontier = children;
            }

            var node = BuildNode(actor, focus, 0, null, true);
            node.Referrers = LoadReferrers(actor, focus.Id);
            var rewards = Context.Queryable<JyReferralReward>()
                .Where(it => it.TenantId == actor.TenantId && it.ReferrerMemberId == focus.Id)
                .OrderBy(it => it.BizTime, OrderByType.Desc)
                .Take(200).ToList();

            return new JyReferralGraphDto
            {
                Focus = node,
                UpChain = upChain,
                DownList = downList,
                Rewards = ToDtos(actor.TenantId, rewards),
                Summary = Summary(actor, new JyReferralQueryDto { ReferrerMemberId = focus.Id })
            };
        }

        private JyReferralNodeDto BuildNode(JyActor actor, JyMember member, int depth, long? parentId, bool isFocus)
        {
            var levelName = member.LevelId.HasValue
                ? Context.Queryable<JyMemberLevel>()
                    .Where(it => it.Id == member.LevelId.Value && it.TenantId == actor.TenantId)
                    .Select(it => it.LevelName).First()
                : null;

            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.MemberId == member.Id)
                .OrderBy(it => it.DealTime, OrderByType.Desc)
                .Take(isFocus ? 8 : 3)
                .Select(it => new JyReferralOrderBriefDto
                {
                    OrderId = it.Id,
                    OrderNo = it.OrderNo,
                    DealTime = it.DealTime,
                    PayAmount = it.PayAmount,
                    PieceQty = it.PieceQty
                }).ToList();

            var rewards = Context.Queryable<JyReferralReward>()
                .Where(it => it.TenantId == actor.TenantId && it.ReferrerMemberId == member.Id)
                .Select(it => new { it.Status, it.RewardAmount }).ToList();

            return new JyReferralNodeDto
            {
                MemberId = member.Id,
                MemberNo = member.MemberNo,
                MemberName = member.MemberName,
                Phone = member.Phone,
                WechatNo = member.WechatNo,
                LevelName = levelName ?? "未分级",
                TotalAmount = member.TotalAmount,
                OrderCount = member.OrderCount,
                IsFocus = isFocus,
                Depth = depth,
                ParentMemberId = parentId,
                RewardTotal = rewards.Where(it => it.Status != 2).Sum(it => it.RewardAmount),
                RewardPending = rewards.Where(it => it.Status == 0).Sum(it => it.RewardAmount),
                Orders = orders
            };
        }

        private List<JyMemberReferrerDto> LoadReferrers(JyActor actor, long memberId)
        {
            var links = Context.Queryable<JyMemberReferrer>()
                .Where(it => it.TenantId == actor.TenantId && it.MemberId == memberId && it.DelFlag == 0)
                .OrderBy(it => it.IsPrimary, OrderByType.Desc).OrderBy(it => it.Id).ToList();
            if (links.Count == 0) return new List<JyMemberReferrerDto>();
            var ids = links.Select(it => it.ReferrerMemberId).Distinct().ToList();
            var members = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && ids.Contains(it.Id))
                .Select(it => new { it.Id, it.MemberNo, it.MemberName, it.WechatNo, it.Phone })
                .ToList().ToDictionary(it => it.Id);
            return links.Select(it =>
            {
                members.TryGetValue(it.ReferrerMemberId, out var referrer);
                return new JyMemberReferrerDto
                {
                    Id = it.Id,
                    MemberId = it.MemberId,
                    ReferrerMemberId = it.ReferrerMemberId,
                    ReferrerName = referrer?.MemberName,
                    ReferrerNo = referrer?.MemberNo,
                    ReferrerWechatNo = referrer?.WechatNo,
                    ReferrerPhone = referrer?.Phone,
                    IsPrimary = it.IsPrimary,
                    Source = it.Source,
                    CreateTime = it.Create_time,
                    Remark = it.Remark
                };
            }).ToList();
        }

        /* ---------------- 推荐人搜索（建档/开单用） ---------------- */

        /// <summary>按手机号或微信号查找可作推荐人的会员。</summary>
        public List<JyCustomerCandidateDto> FindReferrer(JyActor actor, string keyword)
        {
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword)) return new List<JyCustomerCandidateDto>();
            if (keyword.Length > 50) throw new CustomException("查询内容不能超过 50 个字");
            var rows = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0
                    && (it.Phone == keyword || it.WechatNo == keyword
                        || it.MemberName == keyword
                        || it.Phone.Contains(keyword) || it.WechatNo.Contains(keyword) || it.MemberName.Contains(keyword)))
                .OrderBy(it => it.LastConsumeTime, OrderByType.Desc)
                .Take(8).ToList();
            return rows.Select(it => new JyCustomerCandidateDto
            {
                MemberId = it.Id,
                MemberNo = it.MemberNo,
                MemberName = it.MemberName,
                Phone = it.Phone,
                WechatNo = it.WechatNo,
                Sex = it.Sex,
                SolarBirthday = it.SolarBirthday,
                BirthYear = it.BirthYear
            }).ToList();
        }

        /* ---------------- 销售确认时自动生成奖励 ---------------- */

        /// <summary>销售单确认时调用（必须在调用方事务内）：为买家生成推荐人奖励台账。</summary>
        public void AwardForSaleInTransaction(JyActor actor, JySaleOrder sale, DateTime now)
        {
            if (sale == null || !sale.MemberId.HasValue) return;
            if (sale.OrderType != 1) return;
            if (sale.PayAmount <= 0) return;

            var buyer = Context.Queryable<JyMember>()
                .First(it => it.Id == sale.MemberId.Value && it.TenantId == actor.TenantId && it.DelFlag == 0);
            if (buyer == null) return;
            var referrerId = ResolveSaleReferrerId(actor, sale);
            if (!referrerId.HasValue) return;
            var referrerMemberId = referrerId.Value;
            if (referrerMemberId == buyer.Id) return;

            var exists = Context.Queryable<JyReferralReward>()
                .Any(it => it.TenantId == actor.TenantId && it.SaleOrderId == sale.Id);
            if (exists) return;

            var (rate, amount, summary) = CalcReward(actor, sale);
            if (amount <= 0) return;

            Context.Insertable(new JyReferralReward
            {
                TenantId = actor.TenantId,
                StoreId = sale.StoreId,
                ReferrerMemberId = referrerMemberId,
                ReferralMemberId = buyer.Id,
                SaleOrderId = sale.Id,
                SaleOrderNo = sale.OrderNo,
                OrderAmount = sale.PayAmount,
                StyleSummary = summary,
                RewardRate = rate,
                RewardAmount = amount,
                OriginalAmount = amount,
                IsAdjusted = 0,
                Status = 0,
                BizTime = sale.DealTime == default ? now : sale.DealTime,
                Create_by = actor.UserName,
                Create_time = now,
                Remark = "转介绍返现，待线下发放"
            }).ExecuteCommand();
        }

        /// <summary>销售单作废时调用：把对应奖励台账置为已作废（保留记录）。</summary>
        public void VoidForSaleInTransaction(JyActor actor, long saleOrderId, DateTime now)
        {
            Context.Updateable<JyReferralReward>()
                .SetColumns(it => it.Status == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == saleOrderId && it.Status != 2)
                .ExecuteCommand();
        }

        /// <summary>
        /// 本单返现归属只认销售单上明确选择的推荐人。
        /// 会员档案中的历史推荐关系仍用于客户关系展示，但不会在未选择时自动产生新返点。
        /// </summary>
        private long? ResolveSaleReferrerId(JyActor actor, JySaleOrder sale)
        {
            if (sale.ReferrerMemberId.HasValue)
                return sale.ReferrerMemberId.Value;
            if (!string.IsNullOrWhiteSpace(sale.ReferrerWechatNo))
            {
                var byWechat = Context.Queryable<JyMember>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && it.WechatNo == sale.ReferrerWechatNo)
                    .Select(it => it.Id).ToList();
                if (byWechat.Count == 1 && byWechat[0] != sale.MemberId) return byWechat[0];
            }
            return null;
        }

        /* ---------------- 款式分类返点规则 ---------------- */

        /// <summary>一级款式分类及其当前在库件数，供规则维护页面使用。</summary>
        public List<JyReferralRateDto> Rates(JyActor actor)
        {
            var roots = StyleRoots(actor);
            var rates = Context.Queryable<JyReferralRate>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ToList().ToDictionary(it => it.StyleItemId);
            var leafToRoot = LeafToRoot(actor);
            var goods = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && (it.Status == 1 || it.Status == 2))
                .Select(it => new { it.StyleItemId, it.Qty }).ToList();
            var stock = new Dictionary<long, decimal>();
            foreach (var g in goods)
            {
                if (!g.StyleItemId.HasValue || !leafToRoot.TryGetValue(g.StyleItemId.Value, out var rootId)) continue;
                stock[rootId] = stock.TryGetValue(rootId, out var v) ? v + g.Qty : g.Qty;
            }
            return roots.Select((root, index) =>
            {
                rates.TryGetValue(root.Id, out var rule);
                return new JyReferralRateDto
                {
                    Id = rule?.Id ?? 0,
                    StyleItemId = root.Id,
                    StyleName = root.ItemName,
                    Rate = rule?.Rate ?? 0,
                    IsPreset = rule?.IsPreset ?? 0,
                    Status = rule?.Status ?? 0,
                    SortNo = rule?.SortNo ?? (index + 1),
                    StockQty = stock.TryGetValue(root.Id, out var qty) ? qty : 0,
                    Remark = rule?.Remark
                };
            }).ToList();
        }

        /// <summary>批量保存分类返点。只维护已存在的一级分类，未传的分类保持原值。</summary>
        public int SaveRates(JyActor actor, JyReferralRateEditDto dto)
        {
            if (dto?.Items == null || dto.Items.Count == 0) throw new CustomException("没有需要保存的返点规则");
            var roots = StyleRoots(actor).ToDictionary(it => it.Id, it => it.ItemName);
            foreach (var item in dto.Items)
            {
                if (!roots.ContainsKey(item.StyleItemId)) throw new CustomException("款式分类不存在或已停用");
                if (item.Rate < 0 || item.Rate > 100) throw new CustomException("返点比例必须在 0 ~ 100 之间");
                if ((item.Remark ?? string.Empty).Length > 200) throw new CustomException("备注不能超过 200 个字");
            }
            var now = DateTime.Now;
            var tran = UseTran(() =>
            {
                for (var i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    var exist = Context.Queryable<JyReferralRate>().First(it =>
                        it.TenantId == actor.TenantId && it.StyleItemId == item.StyleItemId);
                    var rate = decimal.Round(item.Rate, 2, MidpointRounding.AwayFromZero);
                    if (exist == null)
                    {
                        Context.Insertable(new JyReferralRate
                        {
                            TenantId = actor.TenantId,
                            StyleItemId = item.StyleItemId,
                            StyleName = roots[item.StyleItemId],
                            Rate = rate,
                            IsPreset = 1,
                            Status = 0,
                            SortNo = i + 1,
                            DelFlag = 0,
                            Create_by = actor.UserName,
                            Create_time = now,
                            Remark = string.IsNullOrWhiteSpace(item.Remark) ? null : item.Remark.Trim()
                        }).ExecuteCommand();
                    }
                    else
                    {
                        Context.Updateable<JyReferralRate>()
                            .SetColumns(it => it.Rate == rate)
                            .SetColumns(it => it.StyleName == roots[item.StyleItemId])
                            .SetColumns(it => it.SortNo == i + 1)
                            .SetColumns(it => it.DelFlag == 0)
                            .SetColumns(it => it.Status == 0)
                            .SetColumns(it => it.Remark == (string.IsNullOrWhiteSpace(item.Remark) ? null : item.Remark.Trim()))
                            .SetColumns(it => it.Update_by == actor.UserName)
                            .SetColumns(it => it.Update_time == now)
                            .Where(it => it.Id == exist.Id && it.TenantId == actor.TenantId)
                            .ExecuteCommand();
                    }
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "保存返点规则失败");
            return dto.Items.Count;
        }

        /// <summary>
        /// 按销售明细的一级款式分类分别计算返现：
        /// 每个分类的成交额按整单实付比例折算，再乘以该分类返点。
        /// </summary>
        private (decimal Rate, decimal Amount, string Summary) CalcReward(JyActor actor, JySaleOrder sale)
        {
            if (sale == null || sale.PayAmount <= 0) return (0m, 0m, null);
            var items = Context.Queryable<JySaleOrderItem>()
                .Where(it => it.OrderId == sale.Id).ToList();
            if (items.Count == 0) return (0m, 0m, null);

            var leafToRoot = LeafToRoot(actor);
            var roots = StyleRoots(actor).ToDictionary(it => it.Id, it => it.ItemName);
            var rates = Context.Queryable<JyReferralRate>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .Select(it => new { it.StyleItemId, it.Rate }).ToList()
                .ToDictionary(it => it.StyleItemId, it => it.Rate);

            var groups = new Dictionary<long, decimal>();
            foreach (var item in items)
            {
                if (item.IsGift == 1) continue;
                var amount = item.DealPrice * item.Qty;
                if (amount <= 0) continue;
                var rootId = item.StyleItemId.HasValue && leafToRoot.TryGetValue(item.StyleItemId.Value, out var r) ? r : 0L;
                if (rootId == 0)
                {
                    // 未维护分类归属时落到 0 号分组，页面显示「未分类」，不参与返点
                    groups[0] = groups.TryGetValue(0, out var u0) ? u0 + amount : amount;
                    continue;
                }
                groups[rootId] = groups.TryGetValue(rootId, out var u) ? u + amount : amount;
            }
            if (groups.Count == 0) return (0m, 0m, null);

            var gross = groups.Values.Sum();
            var factor = gross > 0 ? sale.PayAmount / gross : 1m;
            var parts = new List<string>();
            var total = 0m;
            foreach (var kv in groups.OrderByDescending(it => it.Value))
            {
                if (kv.Key == 0) continue;
                if (!rates.TryGetValue(kv.Key, out var rate) || rate <= 0) continue;
                var baseAmount = decimal.Round(kv.Value * factor, 2, MidpointRounding.AwayFromZero);
                var reward = decimal.Round(baseAmount * rate / 100m, 2, MidpointRounding.AwayFromZero);
                if (reward <= 0) continue;
                total += reward;
                parts.Add($"{roots.GetValueOrDefault(kv.Key, "未分类")} {rate:0.##}% ¥{reward:0.00}");
            }
            if (total <= 0) return (0m, 0m, null);
            var weighted = decimal.Round(total / sale.PayAmount * 100m, 2, MidpointRounding.AwayFromZero);
            return (weighted, total, string.Join("；", parts));
        }

        /// <summary>当前租户的一级款式分类。</summary>
        private List<JyLookupItem> StyleRoots(JyActor actor)
        {
            var typeIds = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == actor.TenantId && it.TypeCode == "style" && it.DelFlag == 0)
                .Select(it => it.Id).ToList();
            if (typeIds.Count == 0) return new List<JyLookupItem>();
            return Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && typeIds.Contains(it.TypeId)
                    && it.DelFlag == 0 && it.ParentId == null)
                .OrderBy(it => it.SortNo).OrderBy(it => it.Id).ToList();
        }

        /// <summary>叶子款式分类 → 一级分类 Id。</summary>
        private Dictionary<long, long> LeafToRoot(JyActor actor)
        {
            var typeIds = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == actor.TenantId && it.TypeCode == "style" && it.DelFlag == 0)
                .Select(it => it.Id).ToList();
            if (typeIds.Count == 0) return new Dictionary<long, long>();
            var items = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && typeIds.Contains(it.TypeId) && it.DelFlag == 0)
                .Select(it => new { it.Id, it.ParentId }).ToList();
            var map = new Dictionary<long, long>();
            var parent = items.ToDictionary(it => it.Id, it => it.ParentId);
            foreach (var item in items)
            {
                var current = item.Id;
                var guard = 0;
                while (parent.TryGetValue(current, out var p) && p.HasValue && guard++ < 10) current = p.Value;
                map[item.Id] = current;
            }
            return map;
        }
        /* ---------------- 工具 ---------------- */

        private List<JyReferralRewardDto> ToDtos(string tenantId, List<JyReferralReward> rows)
        {
            if (rows.Count == 0) return new List<JyReferralRewardDto>();
            var memberIds = rows.Select(it => it.ReferrerMemberId)
                .Concat(rows.Select(it => it.ReferralMemberId)).Distinct().ToList();
            var members = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == tenantId && memberIds.Contains(it.Id))
                .Select(it => new { it.Id, it.MemberName, it.Phone, it.WechatNo })
                .ToList().ToDictionary(it => it.Id);
            return rows.Select(it =>
            {
                members.TryGetValue(it.ReferrerMemberId, out var referrer);
                members.TryGetValue(it.ReferralMemberId, out var referral);
                return new JyReferralRewardDto
                {
                    Id = it.Id,
                    ReferrerMemberId = it.ReferrerMemberId,
                    ReferrerName = referrer?.MemberName,
                    ReferrerPhone = referrer?.Phone,
                    ReferrerWechatNo = referrer?.WechatNo,
                    ReferralMemberId = it.ReferralMemberId,
                    ReferralName = referral?.MemberName,
                    ReferralPhone = referral?.Phone,
                    SaleOrderId = it.SaleOrderId,
                    SaleOrderNo = it.SaleOrderNo,
                    OrderAmount = it.OrderAmount,
                    StyleSummary = it.StyleSummary,
                    RewardRate = it.RewardRate,
                    RewardAmount = it.RewardAmount,
                    OriginalAmount = it.OriginalAmount > 0 ? it.OriginalAmount : it.RewardAmount,
                    IsAdjusted = it.IsAdjusted,
                    Status = it.Status,
                    PayoutTime = it.PayoutTime,
                    PayoutBy = it.PayoutBy,
                    PayoutRemark = it.PayoutRemark,
                    SettleBatchNo = it.SettleBatchNo,
                    SettleBeginTime = it.SettleBeginTime,
                    SettleEndTime = it.SettleEndTime,
                    BizTime = it.BizTime,
                    Remark = it.Remark
                };
            }).ToList();
        }
    }
}
