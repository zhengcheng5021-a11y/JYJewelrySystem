using System.Text.Json.Serialization;

namespace ZR.Model.JingYue
{
    /// <summary>盘点明细。正品按件、数量单位；物料按克。BookQty 是保存草稿时的账面快照。</summary>
    [SugarTable("jy_stocktake_line", "静悦盘点明细")]
    public class JyStocktakeLine
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long StocktakeId { get; set; }
        public int SortNo { get; set; }
        public int ItemType { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? GoodsItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MaterialId { get; set; }
        [SugarColumn(Length = 32)]
        public string ItemNo { get; set; }
        [SugarColumn(Length = 100)]
        public string ItemName { get; set; }
        [SugarColumn(Length = 16, IsNullable = true)]
        public string Unit { get; set; }
        [SugarColumn(Length = 150, IsNullable = true)]
        public string CategoryKey { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string CategoryName { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string Barcode { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string StyleName { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string MetalName { get; set; }
        [SugarColumn(DefaultValue = "1", IsNullable = true)]
        public int ItemMode { get; set; } = 1;
        public decimal BookQty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal DiffQty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal LockedQty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal UnitCost { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal DiffAmount { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? ItemStatus { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Remark { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
        [SugarColumn(IsNullable = true)]
        [JsonProperty(propertyName: "createTime")]
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
