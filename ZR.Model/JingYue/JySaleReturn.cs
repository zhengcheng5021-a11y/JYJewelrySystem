using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>销售退货单。Status：0 草稿，1 已完成，9 作废。</summary>
    [SugarTable("jy_sale_return", "静悦销售退货单")]
    public class JySaleReturn : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string ReturnNo { get; set; }
        public long SaleOrderId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MemberId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RefundAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ReturnQty { get; set; }
        public DateTime ReturnTime { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Reason { get; set; }
    }
}
