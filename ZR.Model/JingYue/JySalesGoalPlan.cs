using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>分类经营参数。目标金额仍存 jy_sales_goal_category，本表只保存拆解参数与手工动作。</summary>
    [SugarTable("jy_sales_goal_plan", "静悦分类经营参数")]
    public class JySalesGoalPlan : SysBase
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
        public decimal PlanRatio { get; set; }
        public decimal GrossMarginRate { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ConversionRate { get; set; } = 30;
        public decimal NewCustomerRate { get; set; } = 60;
        [SugarColumn(Length = 500, IsNullable = true)]
        public string ActionHigh { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string ActionMedium { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string ActionLow { get; set; }
    }
}
