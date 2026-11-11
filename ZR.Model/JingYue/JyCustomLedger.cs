namespace ZR.Model.JingYue
{
    /// <summary>定金流水。ChangeType：1 收定金，2 销售对冲，3 作废冲回，4 退定金。</summary>
    [SugarTable("jy_custom_ledger", "静悦定金流水")]
    public class JyCustomLedger
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long CustomOrderId { get; set; }
        /// <summary>对应的定金商品明细；旧版单商品流水兼容为空。</summary>
        [SugarColumn(IsNullable = true)]
        public long? CustomItemId { get; set; }
        public int ChangeType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SaleOrderId { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Remark { get; set; }
        public DateTime BizTime { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string Create_by { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
