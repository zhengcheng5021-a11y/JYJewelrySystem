using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>销售单。OrderType：1 正品销售，2 赠送，3 代理拿货；Status：0 草稿，1 已完成，9 作废。</summary>
    [SugarTable("jy_sale_order", "静悦销售单")]
    public class JySaleOrder : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string OrderNo { get; set; }
        public int OrderType { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int IsBackfill { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MemberId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? AgentId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? PreorderId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? IntentionId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ReceptionId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ActivityId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ActivityBenefit { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TotalAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal DiscountAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal BasePayAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal AdjustAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RoundOffAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal PayAmount { get; set; }
        /// <summary>本单对冲的定制定金。不改变商品成交额。</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal CustomOffsetAmount { get; set; }
        /// <summary>扣减定金后的本次实收。积分仍按 PayAmount 计算。</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal CashAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal CostAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal PieceQty { get; set; }
        public DateTime DealTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StaffUserId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MemberCouponId { get; set; }
        [SugarColumn(Length = 50, IsNullable = true)]
        public string CustomerName { get; set; }
        [SugarColumn(Length = 30, IsNullable = true)]
        public string CustomerPhone { get; set; }
        /// <summary>客户本人微信号（开单时录入，确认收款时写入会员档案）</summary>
        [SugarColumn(Length = 64, IsNullable = true)]
        public string CustomerWechatNo { get; set; }
        /// <summary>推荐人微信号（开单时录入，用于把本次客户绑定到推荐人）</summary>
        [SugarColumn(Length = 64, IsNullable = true)]
        public string ReferrerWechatNo { get; set; }
        /// <summary>本单唯一的推荐人会员 Id；推荐关系多对多，但单张销售只归属一位</summary>
        [SugarColumn(IsNullable = true)]
        public long? ReferrerMemberId { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? CustomerBirthday { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? CustomerBirthYear { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int CustomerSex { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ReturnedAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int ReturnStatus { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}


