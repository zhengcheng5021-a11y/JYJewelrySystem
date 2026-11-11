using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>客户跟进记录。跟进时间与操作人始终由服务器写入。</summary>
    [AppService]
    public class JyMemberFollowService : BaseService<JyMemberFollow>
    {
        private readonly JyWorkItemService _workItem;
        private readonly JyLookupService _lookup;

        public JyMemberFollowService(JyWorkItemService workItem, JyLookupService lookup)
        {
            _workItem = workItem;
            _lookup = lookup;
        }

        public PagedInfo<JyMemberFollowDto> List(JyActor actor, JyMemberFollowQueryDto query)
        {
            query ??= new JyMemberFollowQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JyMemberFollow>()
                .Where(it => it.TenantId == actor.TenantId)
                .WhereIF(query.MemberId > 0, it => it.MemberId == query.MemberId)
                .WhereIF(query.WorkItemId > 0, it => it.WorkItemId == query.WorkItemId)
                .WhereIF(query.BeginTime.HasValue, it => it.FollowTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.FollowTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.Content.Contains(keyword) || it.Result.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.FollowTime, OrderByType.Desc).ToPageList(page, size, ref total);
            var memberIds = rows.Select(it => it.MemberId).Distinct().ToList();
            var members = memberIds.Count == 0 ? new List<JyMember>() : Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && memberIds.Contains(it.Id)).ToList();
            var map = members.ToDictionary(it => it.Id);
            return new PagedInfo<JyMemberFollowDto>
            {
                PageIndex = page, PageSize = size, TotalNum = total,
                Result = rows.Select(it => new JyMemberFollowDto
                {
                    Id = it.Id, MemberId = it.MemberId,
                    MemberNo = map.TryGetValue(it.MemberId, out var m) ? m.MemberNo : null,
                    MemberName = map.TryGetValue(it.MemberId, out var m2) ? m2.MemberName : null,
                    Phone = map.TryGetValue(it.MemberId, out var m3) ? m3.Phone : null,
                    FollowTime = it.FollowTime, FollowType = it.FollowType, LookupItemId = it.LookupItemId,
                    FollowTypeName = it.FollowTypeName ?? LegacyFollowName(it.FollowType), Content = it.Content, Result = it.Result,
                    NextFollowTime = it.NextFollowTime, StaffUserId = it.StaffUserId, WorkItemId = it.WorkItemId,
                    CreateTime = it.Create_time
                }).ToList()
            };
        }

        public long Save(JyActor actor, JyMemberFollowEditDto dto)
        {
            if (dto == null) throw new CustomException("跟进内容不能为空");
            if (dto.MemberId <= 0) throw new CustomException("请选择客户");
            var content = (dto.Content ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(content)) throw new CustomException("跟进内容不能为空");
            if (content.Length > 1000) throw new CustomException("跟进内容不能超过 1000 个字");
            var result = (dto.Result ?? string.Empty).Trim();
            if (result.Length > 500) throw new CustomException("跟进结果不能超过 500 个字");
            var member = Context.Queryable<JyMember>().First(it => it.Id == dto.MemberId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("客户不存在");
            if (dto.WorkItemId > 0)
            {
                var item = Context.Queryable<JyWorkItem>().First(it => it.Id == dto.WorkItemId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException("关联的待办或目标不存在");
                if (item.ItemKind != 2) throw new CustomException("只有客户跟进类型的待办可以关联跟进记录");
                var hasChildren = Context.Queryable<JyWorkItem>().Any(it => it.TenantId == actor.TenantId
                    && it.DelFlag == 0 && it.ParentId == item.Id);
                if (hasChildren) throw new CustomException("请选择客户跟进目标下的具体小目标");
            }
            var now = DateTime.Now;
            _lookup.EnsureFoundation(actor);
            long? lookupItemId = null;
            string followTypeName = null;
            if (dto.LookupItemId is > 0)
            {
                var way = _lookup.MustActiveItem(actor.TenantId, dto.LookupItemId.Value, "follow_way", "跟进方式");
                lookupItemId = way.Id;
                followTypeName = way.ItemName;
            }
            else
            {
                var way = Context.Queryable<JyLookupType>()
                    .First(it => it.TenantId == actor.TenantId && it.TypeCode == "follow_way" && it.DelFlag == 0);
                if (way != null)
                {
                    var item = Context.Queryable<JyLookupItem>()
                        .Where(it => it.TenantId == actor.TenantId && it.TypeId == way.Id && it.DelFlag == 0 && it.Status == 0)
                        .OrderBy(it => it.SortNo).OrderBy(it => it.Id).First();
                    lookupItemId = item?.Id;
                    followTypeName = item?.ItemName;
                }
                followTypeName ??= LegacyFollowName(dto.FollowType);
            }
            var row = new JyMemberFollow
            {
                TenantId = actor.TenantId, MemberId = member.Id, FollowTime = now,
                FollowType = dto.FollowType is >= 1 and <= 5 ? dto.FollowType : 1,
                LookupItemId = lookupItemId, FollowTypeName = followTypeName,
                Content = content, Result = string.IsNullOrEmpty(result) ? null : result,
                NextFollowTime = dto.NextFollowTime, StaffUserId = actor.UserId,
                WorkItemId = dto.WorkItemId > 0 ? dto.WorkItemId : null,
                SourceType = "manual", Create_by = actor.UserName, Create_time = now
            };
            row.Id = Context.Insertable(row).ExecuteReturnBigIdentity();
            // 跟进时间由服务器生成；按该业务日期重算全部客户类目标，不依赖跟进是否手工关联了某个事项。
            _workItem.TryRecalculateCustomerMetricsByBusinessDate(actor, row.FollowTime);
            return row.Id;
        }

        private static string LegacyFollowName(int followType) => followType switch
        {
            1 => "电话沟通",
            2 => "微信沟通",
            3 => "邀约到店",
            4 => "短信触达",
            _ => "其他"
        };
    }
}
