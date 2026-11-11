namespace ZR.Model.JingYue
{
    /// <summary>商品返厂明细，每条对应一件实物商品。</summary>
    [SugarTable("jy_factory_return_item", "静悦返厂明细")]
    public class JyFactoryReturnItem
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long ReturnId { get; set; }
        public long GoodsItemId { get; set; }
        [SugarColumn(Length = 32)]
        public string GoodsNo { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string Barcode { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string GoodsName { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public decimal Qty { get; set; } = 1;
        [SugarColumn(DefaultValue = "0")]
        public decimal CostPrice { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Reason { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}