using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>金属成色历史金价。销售按生效时间取当前有效记录，单据保存价格快照。</summary>
    [SugarTable("jy_metal_price", "静悦金属金价")]
    public class JyMetalPrice : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        public long MetalItemId { get; set; }
        [SugarColumn(Length = 100)]
        public string MetalName { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal PricePerGram { get; set; }
        public DateTime EffectiveTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? ExpireTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
