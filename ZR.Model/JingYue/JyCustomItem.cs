using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 定金商品明细。一张定金单可以包含多件商品，每件商品独立收取、到货、取货、对冲和退款。
    /// Status 与定金单状态一致：1 已收定金，2 部分处理，3 已对冲完，8 已退清，9 作废。
    /// </summary>
    [SugarTable("jy_custom_item", "静悦定金商品明细")]
    public class JyCustomItem : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long CustomOrderId { get; set; }
        public int SortNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? GoodsItemId { get; set; }
        [SugarColumn(Length = 32, IsNullable = true)]
        public string GoodsNo { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string Barcode { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? BindTime { get; set; }
        [SugarColumn(Length = 100)]
        public string GoodsName { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? PickupDate { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int ArrivalStatus { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? ArrivalTime { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string ArrivalBy { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string ArrivalRemark { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int PickupStatus { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? PickupTime { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string PickupBy { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string PickupRemark { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StyleItemId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string StyleItemName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MaterialItemId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string MaterialName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MetalItemId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string MetalName { get; set; }
        [SugarColumn(IsNullable = true)]
        public decimal? Weight { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ExpectPrice { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal DepositAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal OffsetAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RefundAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RemainAmount { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int Status { get; set; } = 1;
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
