using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>会员等级折扣矩阵。PricingMode：1 按件，2 按克，NULL 表示全部；DiscountBase：1 商品/金料，2 含工费。</summary>
    [SugarTable("jy_member_discount_rule", "静悦会员折扣矩阵")]
    public class JyMemberDiscountRule : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long MemberLevelId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StyleItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? PricingMode { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public decimal DiscountRate { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int DiscountBase { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Priority { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? EffectiveTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? ExpireTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
