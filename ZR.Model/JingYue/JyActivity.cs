using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>营销活动。活动类型：1 折扣，2 满减，3 赠礼，4 积分倍数，5 会员专享。</summary>
    [SugarTable("jy_activity", "静悦营销活动")]
    public class JyActivity : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string ActivityNo { get; set; }
        [SugarColumn(Length = 100)]
        public string ActivityName { get; set; }
        public int ActivityType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int ScopeType { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StyleItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public decimal? DiscountRate { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ReduceAmount { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string GiftName { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public decimal PointsMultiple { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MemberLevelId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int JoinLimit { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal BudgetAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal UsedAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
