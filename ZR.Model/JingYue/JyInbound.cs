using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>采购入库单。状态：0 草稿，1 已确认。</summary>
    [SugarTable("jy_inbound", "静悦入库单")]
    public class JyInbound : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string InboundNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SupplierId { get; set; }
        public DateTime InboundTime { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int SourceType { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TotalQty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TotalCost { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? RecognizeLogId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
