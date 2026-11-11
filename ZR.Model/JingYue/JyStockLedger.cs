using System.Text.Json.Serialization;

namespace ZR.Model.JingYue
{
    /// <summary>库存流水。ChangeType：1 入库，2 销售，5 销退，6 赠送，10 盘点。</summary>
    [SugarTable("jy_stock_ledger", "静悦库存流水")]
    public class JyStockLedger
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string LedgerNo { get; set; }
        public long GoodsItemId { get; set; }
        [SugarColumn(Length = 32)]
        public string GoodsNo { get; set; }
        public int ChangeType { get; set; }
        public decimal ChangeQty { get; set; }
        public decimal BeforeQty { get; set; }
        public decimal AfterQty { get; set; }
        [SugarColumn(Length = 32)]
        public string BizType { get; set; }
        public long BizId { get; set; }
        public DateTime BizTime { get; set; }
        [SugarColumn(IsNullable = true, Length = 64)]
        [JsonProperty(propertyName: "createBy")]
        public string Create_by { get; set; }
        [SugarColumn(IsNullable = true)]
        [JsonProperty(propertyName: "createTime")]
        public DateTime Create_time { get; set; } = DateTime.Now;
        [SugarColumn(IsNullable = true, Length = 500)]
        public string Remark { get; set; }
    }
}
