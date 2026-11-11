using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>积分兑换规则：每 AmountBase 元奖励 PointsAwarded 积分。</summary>
    [SugarTable("jy_point_rule", "静悦积分规则")]
    public class JyPointRule : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public decimal AmountBase { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public decimal PointsAwarded { get; set; }
        /// <summary>转介绍返现：每满多少元返现一次（阶梯口径）</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal CashBase { get; set; }
        /// <summary>转介绍返现：每满 CashBase 元返现金额（元）</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal CashAwarded { get; set; }
        /// <summary>转介绍返现比例（%），大于 0 时按实付金额比例返现</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal ReferralCashRate { get; set; }
        public int Status { get; set; }
    }
}

