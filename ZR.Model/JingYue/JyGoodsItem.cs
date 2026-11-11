using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>库存商品。Status：1 在库，2 定制定金锁定，3 销售出库，4 赠送出库，5 盘亏，6 停用，7 入库中，8 返厂出库。</summary>
    [SugarTable("jy_goods_item", "静悦库存商品")]
    public class JyGoodsItem : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string GoodsNo { get; set; }
        [SugarColumn(Length = 64)]
        public string Barcode { get; set; }
        public long StyleId { get; set; }
        public int ItemMode { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public decimal Qty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal LockedQty { get; set; }
        [SugarColumn(IsNullable = true)]
        public decimal? Weight { get; set; }
        /// <summary>计量单位：条/颗/对/件/套，仅作货品属性，不拆分库存数量。</summary>
        [SugarColumn(Length = 16, IsNullable = true)]
        public string Unit { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal CostPrice { get; set; }
        /// <summary>来源入库时的金属价快照；销售按实时销售金价结算。</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal SalePrice { get; set; }
        /// <summary>计价方式：1 按件，2 按克。</summary>
        [SugarColumn(DefaultValue = "1")]
        public int PricingMode { get; set; } = 1;
        /// <summary>工费；LaborFeeMode=1 时是每件固定金额，=2 时是每克金额。</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal LaborFee { get; set; }
        /// <summary>每件商品包装费。</summary>
        [SugarColumn(DefaultValue = "0")]
        public decimal PackagingFee { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int LaborFeeMode { get; set; } = 1;
        [SugarColumn(IsNullable = true)]
        public long? SupplierId { get; set; }
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
        [SugarColumn(Length = 64, IsNullable = true)]
        public string CertNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? CertFileId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ImageFileId { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int Status { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? InboundId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? InboundLineId { get; set; }
        [SugarColumn(Length = 32, IsNullable = true)]
        public string LockBizType { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? LockBizId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
