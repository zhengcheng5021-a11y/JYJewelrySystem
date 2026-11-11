using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>按一级款式分类设置的在库件数预警线。</summary>
    [SugarTable("jy_stock_alert_rule", "静悦库存预警设置")]
    public class JyStockAlertRule : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        public long StyleItemId { get; set; }

        [SugarColumn(Length = 100)]
        public string StyleItemName { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public decimal MinQty { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }

    }
}
