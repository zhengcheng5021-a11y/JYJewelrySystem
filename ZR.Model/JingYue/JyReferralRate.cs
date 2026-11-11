using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 转介绍返点规则：按一级款式分类维护返点百分比。
    /// 生成返现台账时按订单明细的款式分类逐项计算，未配置的分类不产生返现。
    /// </summary>
    [SugarTable("jy_referral_rate", "静悦转介绍返点规则")]
    public class JyReferralRate : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        /// <summary>一级款式分类 Id（jy_lookup_item 根节点）</summary>
        public long StyleItemId { get; set; }
        [SugarColumn(Length = 100)]
        public string StyleName { get; set; }
        /// <summary>返点百分比，例如 3.5 表示 3.5%</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal Rate { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int IsPreset { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
