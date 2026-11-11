using System.Text.Json.Serialization;

namespace ZR.Model.JingYue
{
    /// <summary>入库明细。每条明细代表一件货品，ItemMode 保留兼容但当前采购入库固定为 1。</summary>
    [SugarTable("jy_inbound_line", "静悦入库明细")]
    public class JyInboundLine
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long InboundId { get; set; }
        public int SortNo { get; set; }
        public long StyleId { get; set; }
        public int ItemMode { get; set; }
        public decimal Qty { get; set; }
        [SugarColumn(IsNullable = true)]
        public decimal? Weight { get; set; }
        /// <summary>计量单位：条/颗/对/件/套，仅作货品属性，不拆分库存数量。</summary>
        [SugarColumn(Length = 16, IsNullable = true)]
        public string Unit { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal CostPrice { get; set; }
        /// <summary>入库时记录的本次采购金属单价（按克商品）。</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal SalePrice { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int PricingMode { get; set; } = 1;
        [SugarColumn(DefaultValue = "0")]
        public decimal LaborFee { get; set; }
        /// <summary>每件商品包装费。</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal PackagingFee { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int LaborFeeMode { get; set; } = 1;
        [SugarColumn(Length = 100)]
        public string StyleName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StyleItemId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string StyleItemName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MaterialItemId { get; set; }
        [SugarColumn(DefaultValue = "3")]
        public int MaterialGroup { get; set; } = 3;
        [SugarColumn(Length = 100, IsNullable = true)]
        public string MaterialName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MetalItemId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string MetalName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? CertTypeId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string CertTypeName { get; set; }
        [SugarColumn(Length = 32, IsNullable = true)]
        public string GoodsNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ImageFileId { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Remark { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? RecognizeLogId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
        [SugarColumn(IsNullable = true)]
        [JsonProperty(propertyName: "createTime")]
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
