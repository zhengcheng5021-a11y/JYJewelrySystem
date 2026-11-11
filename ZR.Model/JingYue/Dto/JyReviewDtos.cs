namespace ZR.Model.JingYue.Dto
{
    public class JyReviewQueryDto : PagerInfo
    {
        public int? PeriodType { get; set; }
        public DateTime? BeginDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class JyReviewEditDto
    {
        public long Id { get; set; }
        public int PeriodType { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int GenerateTodo { get; set; }
        public int Priority { get; set; } = 2;
    }

    public class JyReviewDto
    {
        public long Id { get; set; }
        public int PeriodType { get; set; }
        public string PeriodName { get; set; }
        public DateTime ReviewDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int GenerateTodo { get; set; }
        public long? WorkItemId { get; set; }
        public string WorkItemNo { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }
}
