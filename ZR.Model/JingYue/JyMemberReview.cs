using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>日 / 周 / 月 / 季 / 年经营复盘。复盘可独立保存，也可生成待办留痕。</summary>
    [SugarTable("jy_member_review", "静悦多周期复盘")]
    public class JyMemberReview : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        /// <summary>1 日、2 周、3 月、4 季、5 年。</summary>
        public int PeriodType { get; set; }
        public DateTime ReviewDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        [SugarColumn(Length = 100)]
        public string Title { get; set; }
        [SugarColumn(Length = 4000, IsNullable = true)]
        public string Content { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int GenerateTodo { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? WorkItemId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
