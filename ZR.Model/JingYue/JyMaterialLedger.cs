using System.Text.Json.Serialization;

namespace ZR.Model.JingYue
{
    /// <summary>物料重量流水，只追加不修改。ChangeWeight 正数入库、负数出库。</summary>
    [SugarTable("jy_material_ledger", "静悦物料流水")]
    public class JyMaterialLedger
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string LedgerNo { get; set; }
        public long MaterialId { get; set; }
        [SugarColumn(Length = 32)]
        public string MaterialCode { get; set; }
        public int ChangeType { get; set; }
        public decimal ChangeWeight { get; set; }
        public decimal BeforeWeight { get; set; }
        public decimal AfterWeight { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal UnitCost { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal Amount { get; set; }
        [SugarColumn(Length = 32)]
        public string BizType { get; set; }
        public long BizId { get; set; }
        [SugarColumn(Length = 32)]
        public string BizNo { get; set; }
        public DateTime BizTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? OperatorUserId { get; set; }
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
