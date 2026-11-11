using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_sales_goal_category", "静悦分类销售目标")]
    public class JySalesGoalCategory : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public int GoalYear { get; set; }
        public int GoalMonth { get; set; }
        public long CategoryId { get; set; }
        [SugarColumn(Length = 100)]
        public string CategoryName { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TargetAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TargetQty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal UnitPrice { get; set; }
    }
}
