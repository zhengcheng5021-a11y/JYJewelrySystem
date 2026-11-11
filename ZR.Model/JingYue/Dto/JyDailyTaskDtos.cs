namespace ZR.Model.JingYue.Dto
{
    /// <summary>每日必做查询。TaskDate 为空时按服务器当天。</summary>
    public class JyDailyTaskQueryDto
    {
        public DateTime? TaskDate { get; set; }
        /// <summary>null 全部，0 未完成，1 已完成。</summary>
        public int? Status { get; set; }
    }

    public class JyDailyTaskTemplateEditDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        /// <summary>数量或文本，例如“5 件”“3 位客户”。</summary>
        public string Content { get; set; }
        public string Remark { get; set; }
        public int SortNo { get; set; }
        /// <summary>0 启用，1 停用。</summary>
        public int Status { get; set; }
    }

    public class JyDailyTaskEditDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Remark { get; set; }
    }

    public class JyDailyTaskSortDto
    {
        public List<long> Ids { get; set; } = new();
    }

    public class JyDailyTaskTemplateDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Remark { get; set; }
        public int SortNo { get; set; }
        public int Status { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class JyDailyTaskItemDto
    {
        public long Id { get; set; }
        public long TemplateId { get; set; }
        public DateTime TaskDate { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Remark { get; set; }
        public int SortNo { get; set; }
        public int IsCompleted { get; set; }
        public DateTime? CompletedTime { get; set; }
        public string CompletedBy { get; set; }
    }

    public class JyDailyTaskSummaryDto
    {
        public int Total { get; set; }
        public int Done { get; set; }
        public decimal Percent { get; set; }
        public DateTime ServerDate { get; set; }
    }

    public class JyDailyTaskListDto
    {
        public DateTime Date { get; set; }
        public JyDailyTaskSummaryDto Summary { get; set; }
        public List<JyDailyTaskItemDto> Items { get; set; } = new();
    }
}