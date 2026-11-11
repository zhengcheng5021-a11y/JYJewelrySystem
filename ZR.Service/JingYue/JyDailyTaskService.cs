using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>
    /// 每日必做。模板启用后按服务器日期生成当天快照，历史快照保留；
    /// 当天修改会同步回写模板，删除模板只停止以后生成。
    /// </summary>
    [AppService]
    public class JyDailyTaskService : BaseService<JyDailyTaskTemplate>
    {
        /// <summary>列出某天必做，并在打开页面时补齐当天快照。</summary>
        public JyDailyTaskListDto List(JyActor actor, JyDailyTaskQueryDto query)
        {
            var today = DateTime.Now.Date;
            var date = (query?.TaskDate ?? today).Date;
            // 未来日期回退到当天；历史日期只读取既有快照，不能用今天的模板反向补造历史。
            if (date > today) date = today;
            if (date == today) Generate(actor, today);

            var items = Context.Queryable<JyDailyTaskItem>()
                .Where(it => it.TenantId == actor.TenantId && it.TaskDate == date)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList();
            var done = items.Count(it => it.IsCompleted == 1);
            return new JyDailyTaskListDto
            {
                Date = date,
                Summary = new JyDailyTaskSummaryDto
                {
                    Total = items.Count,
                    Done = done,
                    Percent = items.Count == 0 ? 0 : Math.Round(done * 100m / items.Count, 0),
                    ServerDate = today
                },
                Items = items.Select(ToDto).ToList()
            };
        }

        public List<JyDailyTaskTemplateDto> Templates(JyActor actor)
        {
            return Context.Queryable<JyDailyTaskTemplate>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList()
                .Select(it => new JyDailyTaskTemplateDto
                {
                    Id = it.Id,
                    Title = it.Title,
                    Content = it.Content,
                    Remark = it.Remark,
                    SortNo = it.SortNo,
                    Status = it.Status,
                    EffectiveDate = it.EffectiveDate
                })
                .ToList();
        }

        public long SaveTemplate(JyActor actor, JyDailyTaskTemplateEditDto dto)
        {
            if (dto == null) throw new CustomException("参数不能为空");
            var title = NormalizeTitle(dto.Title);
            var content = EmptyOrMax(dto.Content, 200, "数量或说明");
            var remark = EmptyOrMax(dto.Remark, 500, "备注");
            if (dto.Status != 0 && dto.Status != 1) throw new CustomException("状态不正确");

            if (dto.Id <= 0)
            {
                var id = Context.Insertable(new JyDailyTaskTemplate
                {
                    TenantId = actor.TenantId,
                    Title = title,
                    Content = content,
                    Remark = remark,
                    SortNo = dto.SortNo,
                    Status = dto.Status,
                    DelFlag = 0,
                    EffectiveDate = DateTime.Now.Date,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now
                }).ExecuteReturnBigIdentity();
                // 新建后立即生成当天快照，用户无需再次刷新。
                Generate(actor, DateTime.Now.Date);
                return id;
            }

            var row = Must(actor.TenantId, dto.Id);
            row.Title = title;
            row.Content = content;
            row.Remark = remark;
            row.SortNo = dto.SortNo;
            row.Status = dto.Status;
            row.Update_by = actor.UserName;
            row.Update_time = DateTime.Now;
            Context.Updateable(row)
                .UpdateColumns(it => new { it.Title, it.Content, it.Remark, it.SortNo, it.Status, it.Update_by, it.Update_time })
                .ExecuteCommand();
            // 当天已生成的快照跟随模板修改，历史日期不受影响。
            var today = DateTime.Now.Date;
            Context.Updateable<JyDailyTaskItem>()
                .SetColumns(it => it.Title == title)
                .SetColumns(it => it.Content == content)
                .SetColumns(it => it.Remark == remark)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.TenantId == actor.TenantId && it.TemplateId == row.Id && it.TaskDate == today)
                .ExecuteCommand();
            return row.Id;
        }

        public int DeleteTemplate(JyActor actor, long id)
        {
            Must(actor.TenantId, id);
            return Context.Updateable<JyDailyTaskTemplate>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Status == 1)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        public int Toggle(JyActor actor, long id, bool completed)
        {
            var row = Context.Queryable<JyDailyTaskItem>()
                .First(it => it.Id == id && it.TenantId == actor.TenantId)
                ?? throw new CustomException("必做项不存在");
            EnsureToday(row.TaskDate);
            return Context.Updateable<JyDailyTaskItem>()
                .SetColumns(it => it.IsCompleted == (completed ? 1 : 0))
                .SetColumns(it => it.CompletedTime == (completed ? DateTime.Now : (DateTime?)null))
                .SetColumns(it => it.CompletedBy == (completed ? actor.UserName : null))
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId)
                .ExecuteCommand();
        }

        public int UpdateItem(JyActor actor, JyDailyTaskEditDto dto)
        {
            if (dto == null || dto.Id <= 0) throw new CustomException("必做项不存在");
            var title = NormalizeTitle(dto.Title);
            var content = EmptyOrMax(dto.Content, 200, "数量或说明");
            var remark = EmptyOrMax(dto.Remark, 500, "备注");
            var row = Context.Queryable<JyDailyTaskItem>()
                .First(it => it.Id == dto.Id && it.TenantId == actor.TenantId)
                ?? throw new CustomException("必做项不存在");
            EnsureToday(row.TaskDate);

            UseTran(() =>
            {
                Context.Updateable<JyDailyTaskItem>()
                    .SetColumns(it => it.Title == title)
                    .SetColumns(it => it.Content == content)
                    .SetColumns(it => it.Remark == remark)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == DateTime.Now)
                    .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
                // 修改历史日期只改快照，不回写模板；修改当天才更新模板，供明天继续生成。
                if (row.TaskDate == DateTime.Now.Date)
                {
                    Context.Updateable<JyDailyTaskTemplate>()
                        .SetColumns(it => it.Title == title)
                        .SetColumns(it => it.Content == content)
                        .SetColumns(it => it.Remark == remark)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == DateTime.Now)
                        .Where(it => it.Id == row.TemplateId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                        .ExecuteCommand();
                }
            });
            return 1;
        }

        public int Sort(JyActor actor, JyDailyTaskSortDto dto)
        {
            var ids = (dto?.Ids ?? new List<long>()).Where(it => it > 0).Distinct().ToList();
            if (ids.Count == 0) throw new CustomException("排序数据不能为空");
            var rows = Context.Queryable<JyDailyTaskItem>()
                .Where(it => it.TenantId == actor.TenantId && ids.Contains(it.Id))
                .ToList();
            if (rows.Count != ids.Count) throw new CustomException("存在无效的必做项");
            var date = rows[0].TaskDate;
            if (rows.Any(it => it.TaskDate != date)) throw new CustomException("只能对同一天的必做排序");
            EnsureToday(date);

            UseTran(() =>
            {
                for (var i = 0; i < ids.Count; i++)
                {
                    var sort = (i + 1) * 10;
                    var item = rows.First(it => it.Id == ids[i]);
                    Context.Updateable<JyDailyTaskItem>()
                        .SetColumns(it => it.SortNo == sort)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == DateTime.Now)
                        .Where(it => it.Id == item.Id && it.TenantId == actor.TenantId)
                        .ExecuteCommand();
                    if (date == DateTime.Now.Date)
                    {
                        Context.Updateable<JyDailyTaskTemplate>()
                            .SetColumns(it => it.SortNo == sort)
                            .SetColumns(it => it.Update_by == actor.UserName)
                            .SetColumns(it => it.Update_time == DateTime.Now)
                            .Where(it => it.Id == item.TemplateId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                            .ExecuteCommand();
                    }
                }
            });
            return ids.Count;
        }

        /// <summary>删除当天必做项。模板同步逻辑删除，次日不再生成；历史日期不允许删除。</summary>
        public int DeleteItem(JyActor actor, long id)
        {
            var row = Context.Queryable<JyDailyTaskItem>()
                .First(it => it.Id == id && it.TenantId == actor.TenantId)
                ?? throw new CustomException("必做项不存在");
            EnsureToday(row.TaskDate);
            UseTran(() =>
            {
                Context.Updateable<JyDailyTaskTemplate>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Status == 1)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == DateTime.Now)
                    .Where(it => it.Id == row.TemplateId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
                Context.Deleteable<JyDailyTaskItem>()
                    .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
            });
            return 1;
        }

        /// <summary>按模板补齐某天快照。唯一索引兜底，重复调用不会重复插入。</summary>
        public int Generate(JyActor actor, DateTime date)
        {
            date = date.Date;
            var templates = Context.Queryable<JyDailyTaskTemplate>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0
                    && it.EffectiveDate <= date)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList();
            if (templates.Count == 0) return 0;
            var templateIds = templates.Select(it => it.Id).ToList();
            var exists = Context.Queryable<JyDailyTaskItem>()
                .Where(it => it.TenantId == actor.TenantId && it.TaskDate == date && templateIds.Contains(it.TemplateId))
                .ToList();
            var existIds = exists.Select(it => it.TemplateId).ToHashSet();
            var now = DateTime.Now;
            var inserts = templates.Where(it => !existIds.Contains(it.Id)).Select((it, index) => new JyDailyTaskItem
            {
                TenantId = actor.TenantId,
                TemplateId = it.Id,
                TaskDate = date,
                Title = it.Title,
                Content = it.Content,
                Remark = it.Remark,
                SortNo = it.SortNo,
                IsCompleted = 0,
                Create_by = actor.UserName,
                Create_time = now
            }).ToList();
            if (inserts.Count == 0) return 0;
            // 并发打开同一日期时唯一索引可能拦截重复插入，此处按项忽略，不影响其它项。
            var inserted = 0;
            foreach (var item in inserts)
            {
                try
                {
                    inserted += Context.Insertable(item).ExecuteCommand();
                }
                catch (Exception ex) when (ex.Message.Contains("UX_jy_daily_task_item"))
                {
                    // 另一请求已生成同一条快照，跳过。
                }
            }
            return inserted;
        }

        /// <summary>历史日期只读，编辑/删除/勾选/排序只允许当天。</summary>
        private static void EnsureToday(DateTime taskDate)
        {
            if (taskDate.Date != DateTime.Now.Date)
                throw new CustomException("历史必做只读，只有当天的数据可以修改或删除");
        }

        private JyDailyTaskTemplate Must(string tenantId, long id)
        {
            return Context.Queryable<JyDailyTaskTemplate>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("必做模板不存在");
        }

        private static JyDailyTaskItemDto ToDto(JyDailyTaskItem it)
        {
            return new JyDailyTaskItemDto
            {
                Id = it.Id,
                TemplateId = it.TemplateId,
                TaskDate = it.TaskDate,
                Title = it.Title,
                Content = it.Content,
                Remark = it.Remark,
                SortNo = it.SortNo,
                IsCompleted = it.IsCompleted,
                CompletedTime = it.CompletedTime,
                CompletedBy = it.CompletedBy
            };
        }

        private static string NormalizeTitle(string value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) throw new CustomException("必做标题不能为空");
            if (value.Length > 200) throw new CustomException("必做标题不能超过 200 个字");
            return value;
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
