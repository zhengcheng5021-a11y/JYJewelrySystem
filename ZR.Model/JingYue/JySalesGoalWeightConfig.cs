using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>月度品类权重配置。默认销售占比 60、库存压力 20、利润率 20，可按月调整。</summary>
    [SugarTable("jy_sales_goal_weight_config", "静悦月度品类权重")]
    public class JySalesGoalWeightConfig : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public int GoalYear { get; set; }
        public int GoalMonth { get; set; }
        public decimal SalesWeight { get; set; } = 60;
        public decimal StockWeight { get; set; } = 20;
        public decimal MarginWeight { get; set; } = 20;
    }
}
