using System.Text.Json.Serialization;

namespace ZR.Model.JingYue
{
    /// <summary>物料单据明细。Weight 为正数，由单据方向决定入库或出库。</summary>
    [SugarTable("jy_material_doc_line", "静悦物料单据明细")]
    public class JyMaterialDocLine
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long DocId { get; set; }
        public int SortNo { get; set; }
        public long MaterialId { get; set; }
        [SugarColumn(Length = 32)]
        public string MaterialCode { get; set; }
        [SugarColumn(Length = 100)]
        public string MaterialName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? CategoryItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MetalItemId { get; set; }
        public decimal Weight { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal UnitCost { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal Amount { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Remark { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
        [SugarColumn(IsNullable = true)]
        [JsonProperty(propertyName: "createTime")]
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
