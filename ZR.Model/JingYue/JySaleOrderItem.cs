using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>销售单商品快照。</summary>
    [SugarTable("jy_sale_order_item", "静悦销售明细")]
    public class JySaleOrderItem
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long OrderId { get; set; }
        public int SortNo { get; set; }
        public long GoodsItemId { get; set; }
        [SugarColumn(Length = 32)]
        public string GoodsNo { get; set; }
        [SugarColumn(Length = 64)]
        public string Barcode { get; set; }
        [SugarColumn(Length = 100)]
        public string GoodsName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StyleId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
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
        public decimal? Weight { get; set; }
        /// <summary>销售时记录的商品内含数量；仅作库存属性快照，不参与库存扣减或计价。</summary>
        [SugarColumn(IsNullable = true)]
        public decimal? StockQty { get; set; }
        /// <summary>成交时的数量单位快照，例如条、颗、对、件、套。</summary>
        [SugarColumn(Length = 16, IsNullable = true)]
        public string Unit { get; set; }
        public decimal Qty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal CostPrice { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal SalePrice { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal DealPrice { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int PricingMode { get; set; } = 1;
        [SugarColumn(IsNullable = true)]
        public long? MetalPriceId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal MarketMetalPricePerGram { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal MetalPricePerGram { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? PriceSnapshotTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal LaborFee { get; set; }
        /// <summary>成交时的每件商品包装费快照。</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal PackagingFee { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal PackagingAmount { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int LaborFeeMode { get; set; } = 1;
        [SugarColumn(DefaultValue = "0")]
        public decimal MaterialAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal LaborAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ListAmount { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MemberDiscountRuleId { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public decimal MemberDiscountRate { get; set; } = 1;
        [SugarColumn(DefaultValue = "0")]
        public decimal MemberDiscountAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ActivityBenefit { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal PayAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int IsGift { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int PriceSource { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ReturnedQty { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? RecognizeLogId { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
