namespace ZR.Model.JingYue
{
    /// <summary>销售单与定制单的定金对冲。Status：0 草稿计划，1 已核销，9 已冲回。</summary>
    [SugarTable("jy_sale_custom_offset", "静悦销售定金对冲")]
    public class JySaleCustomOffset
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long SaleOrderId { get; set; }
        public long CustomOrderId { get; set; }
        /// <summary>被核销的定金商品明细；旧版单商品对冲兼容为空。</summary>
        [SugarColumn(IsNullable = true)]
        public long? CustomItemId { get; set; }
        [SugarColumn(Length = 32)]
        public string CustomNo { get; set; }
        public decimal OffsetAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
