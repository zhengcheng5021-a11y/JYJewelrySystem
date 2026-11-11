using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>日 / 周 / 月 / 季 / 年复盘。保存时可复用现有待办与目标服务。</summary>
    [AppService]
    public class JyMemberReviewService : BaseService<JyMemberReview>
    {
        private static readonly string[] PeriodNames = ["", "每日复盘", "周复盘", "月度复盘", "季度复盘", "年复盘"];
        private readonly JyWorkItemService _workItem;

        public JyMemberReviewService(JyWorkItemService workItem)
        {
            _workItem = workItem;
        }

        public PagedInfo<JyReviewDto> List(JyActor actor, JyReviewQueryDto query)
        {
            query ??= new JyReviewQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var q = Context.Queryable<JyMemberReview>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.PeriodType is >= 1 and <= 5, it => it.PeriodType == query.PeriodType)
                .WhereIF(query.BeginDate.HasValue, it => it.ReviewDate >= query.BeginDate.Value.Date)
                .WhereIF(query.EndDate.HasValue, it => it.ReviewDate < query.EndDate.Value.Date.AddDays(1));
            var total = 0;
            var rows = q.OrderBy(it => it.ReviewDate, OrderByType.Desc).OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            var workIds = rows.Where(it => it.WorkItemId.HasValue).Select(it => it.WorkItemId.Value).Distinct().ToList();
            var workNos = workIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyWorkItem>()
                .Where(it => it.TenantId == actor.TenantId && workIds.Contains(it.Id))
                .ToList().ToDictionary(it => it.Id, it => it.ItemNo);
            return new PagedInfo<JyReviewDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(it => ToDto(it, workNos.GetValueOrDefault(it.WorkItemId ?? 0))).ToList()
            };
        }

        public JyReviewDto Detail(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            var workNo = row.WorkItemId.HasValue ? Context.Queryable<JyWorkItem>()
                .Where(it => it.Id == row.WorkItemId.Value && it.TenantId == actor.TenantId)
                .Select(it => it.ItemNo).First() : null;
            return ToDto(row, workNo);
        }

        public long Save(JyActor actor, JyReviewEditDto dto)
        {
            if (dto == null) throw new CustomException("复盘内容不能为空");
            if (dto.PeriodType is < 1 or > 5) throw new CustomException("复盘周期不正确");
            var title = (dto.Title ?? string.Empty).Trim();
            if (title.Length > 100) throw new CustomException("复盘标题不能超过 100 个字");
            var content = (dto.Content ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(content)) throw new CustomException("复盘内容不能为空");
            if (content.Length > 4000) throw new CustomException("复盘内容不能超过 4000 个字");
            var reviewDate = (dto.ReviewDate ?? DateTime.Today).Date;
            if (reviewDate > DateTime.Today) throw new CustomException("复盘日期不能晚于今天");
            var (start, end) = PeriodRange(dto.PeriodType, reviewDate);
            title = string.IsNullOrEmpty(title) ? $"{PeriodNames[dto.PeriodType]} {reviewDate:yyyy-MM-dd}" : title;
            if (dto.Priority is < 1 or > 4) dto.Priority = 3;

            var current = dto.Id > 0 ? Must(actor.TenantId, dto.Id) : null;
            if (current?.WorkItemId > 0 && dto.GenerateTodo == 0)
                throw new CustomException("这条复盘已生成待办，完成情况请直接在待办中维护");
            long id = 0;
            var tran = UseTran(() =>
            {
                if (current == null)
                {
                    id = Context.Insertable(new JyMemberReview
                    {
                        TenantId = actor.TenantId,
                        PeriodType = dto.PeriodType,
                        ReviewDate = reviewDate,
                        PeriodStart = start,
                        PeriodEnd = end,
                        Title = title,
                        Content = content,
                        GenerateTodo = dto.GenerateTodo == 1 ? 1 : 0,
                        DelFlag = 0,
                        Create_by = actor.UserName,
                        Create_time = DateTime.Now
                    }).ExecuteReturnBigIdentity();
                }
                else
                {
                    id = current.Id;
                    current.PeriodType = dto.PeriodType;
                    current.ReviewDate = reviewDate;
                    current.PeriodStart = start;
                    current.PeriodEnd = end;
                    current.Title = title;
                    current.Content = content;
                    current.Update_by = actor.UserName;
                    current.Update_time = DateTime.Now;
                    Context.Updateable(current).UpdateColumns(it => new
                    {
                        it.PeriodType, it.ReviewDate, it.PeriodStart, it.PeriodEnd, it.Title, it.Content,
                        it.Update_by, it.Update_time
                    }).ExecuteCommand();
                }

                if (dto.GenerateTodo == 1 && !(current?.WorkItemId > 0))
                {
                    var workId = _workItem.Save(actor, new JyWorkItemEditDto
                    {
                        Title = $"复盘：{title}",
                        ItemKind = 1,
                        Priority = dto.Priority,
                        HasDateRange = 1,
                        StartDate = start,
                        EndDate = end,
                        ProgressMode = 1,
                        Remark = content.Length > 480 ? content[..480] : content,
                        OwnerUserId = actor.UserId,
                        OwnerName = actor.UserName
                    });
                    Context.Updateable<JyMemberReview>()
                        .SetColumns(it => it.WorkItemId == workId)
                        .SetColumns(it => it.GenerateTodo == 1)
                        .Where(it => it.Id == id && it.TenantId == actor.TenantId)
                        .ExecuteCommand();
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "复盘保存失败");
            return id;
        }

        public int Delete(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            if (row.WorkItemId.HasValue && Context.Queryable<JyWorkItem>().Any(it => it.TenantId == actor.TenantId
                && it.Id == row.WorkItemId.Value && it.DelFlag == 0))
                throw new CustomException("这条复盘已生成待办，请先在待办与目标中删除关联待办");
            return Context.Updateable<JyMemberReview>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        private JyMemberReview Must(string tenantId, long id)
        {
            return Context.Queryable<JyMemberReview>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("复盘记录不存在");
        }

        private static JyReviewDto ToDto(JyMemberReview it, string workItemNo)
        {
            return new JyReviewDto
            {
                Id = it.Id,
                PeriodType = it.PeriodType,
                PeriodName = PeriodNames[it.PeriodType],
                ReviewDate = it.ReviewDate,
                PeriodStart = it.PeriodStart,
                PeriodEnd = it.PeriodEnd,
                Title = it.Title,
                Content = it.Content,
                GenerateTodo = it.GenerateTodo,
                WorkItemId = it.WorkItemId,
                WorkItemNo = workItemNo,
                CreateBy = it.Create_by,
                CreateTime = it.Create_time,
                UpdateTime = it.Update_time
            };
        }

        private static (DateTime Start, DateTime End) PeriodRange(int periodType, DateTime date)
        {
            return periodType switch
            {
                1 => (date, date),
                2 => WeekRange(date),
                3 => (new DateTime(date.Year, date.Month, 1), new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1)),
                4 => QuarterRange(date),
                _ => (new DateTime(date.Year, 1, 1), new DateTime(date.Year, 12, 31))
            };
        }

        private static (DateTime Start, DateTime End) WeekRange(DateTime date)
        {
            var offset = ((int)date.DayOfWeek + 6) % 7;
            var start = date.AddDays(-offset);
            return (start, start.AddDays(6));
        }

        private static (DateTime Start, DateTime End) QuarterRange(DateTime date)
        {
            var month = (date.Month - 1) / 3 * 3 + 1;
            var start = new DateTime(date.Year, month, 1);
            return (start, start.AddMonths(3).AddDays(-1));
        }
    }
}
