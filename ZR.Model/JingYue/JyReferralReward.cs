using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 转介绍奖励台账。一条销售单最多产生一条奖励（UX 唯一约束保证），
    /// 永久保留，只做状态流转：0 待返现，1 已返现，2 已作废（来源销售单作废）。
    /// </summary>
    [SugarTable("jy_referral_reward", "静悦转介绍奖励台账")]
    public class JyReferralReward : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        /// <summary>推荐人会员 Id（客户 A）</summary>
        public long ReferrerMemberId { get; set; }
        /// <summary>被推荐人会员 Id（客户 B，本单买家）</summary>
        public long ReferralMemberId { get; set; }
        public long SaleOrderId { get; set; }
        [SugarColumn(Length = 32)]
        public string SaleOrderNo { get; set; }
        /// <summary>本单实付金额（按实付计算奖励）</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal OrderAmount { get; set; }
        /// <summary>按款式分类汇总的返点说明，如「银饰 3% ¥8.00；手串 2% ¥4.00」</summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string StyleSummary { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RewardRate { get; set; }
        /// <summary>系统按分类规则算出的原始返现金额，人工改金额后保留用于对账</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal OriginalAmount { get; set; }
        /// <summary>0 未改金额，1 已人工修改</summary>
        [SugarColumn(DefaultValue = "0")]
        public int IsAdjusted { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RewardAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? PayoutTime { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string PayoutBy { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string PayoutRemark { get; set; }
        /// <summary>结算批次号，一次区间结算共用一个批次</summary>
        [SugarColumn(Length = 32, IsNullable = true)]
        public string SettleBatchNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? SettleBeginTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? SettleEndTime { get; set; }
        public DateTime BizTime { get; set; }
    }
}

