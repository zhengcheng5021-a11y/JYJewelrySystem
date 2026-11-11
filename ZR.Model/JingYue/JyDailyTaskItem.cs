using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 每日必做当天快照。TemplateId 指向模板分项，标题/内容/备注在生成时快照，之后可单独修改并回写模板。
    /// </summary>
    [SugarTable("jy_daily_task_item", "静悦每日必做当天项")]
    public class JyDailyTaskItem : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long TemplateId { get; set; }
        /// <summary>业务日期，按服务器日期分区。</summary>
        public DateTime TaskDate { get; set; }
        [SugarColumn(Length = 200)]
        public string Title { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Content { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }
        /// <summary>0 未完成，1 已完成。</summary>
        [SugarColumn(DefaultValue = "0")]
        public int IsCompleted { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? CompletedTime { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string CompletedBy { get; set; }
    }
}
