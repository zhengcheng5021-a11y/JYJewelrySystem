using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 客户推荐关系历史。同一客户可以有多个推荐人，不同阶段由不同朋友介绍；
    /// 每张销售单只选择其中一位，用于返现归属。
    /// </summary>
    [SugarTable("jy_member_referrer", "静悦会员推荐关系")]
    public class JyMemberReferrer : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        /// <summary>被推荐客户</summary>
        public long MemberId { get; set; }
        /// <summary>推荐人会员 Id</summary>
        public long ReferrerMemberId { get; set; }
        /// <summary>1 建档录入，2 开单补录，3 手工维护</summary>
        [SugarColumn(DefaultValue = "1")]
        public int Source { get; set; }
        /// <summary>若由某张销售单补录，记录来源单</summary>
        [SugarColumn(IsNullable = true)]
        public long? SaleOrderId { get; set; }
        /// <summary>1 默认推荐人（开单优先带出）</summary>
        [SugarColumn(DefaultValue = "0")]
        public int IsPrimary { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
