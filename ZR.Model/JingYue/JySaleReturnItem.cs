using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>销售退货明细。</summary>
    [SugarTable("jy_sale_return_item", "静悦销售退货明细")]
    public class JySaleReturnItem
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long ReturnId { get; set; }
        public long SaleOrderItemId { get; set; }
        public long GoodsItemId { get; set; }
        [SugarColumn(Length = 32)]
        public string GoodsNo { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string Barcode { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string GoodsName { get; set; }
        public decimal Qty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RefundPrice { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RefundAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal CostPrice { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int IsGift { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
