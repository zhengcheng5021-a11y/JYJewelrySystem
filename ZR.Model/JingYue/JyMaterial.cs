using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>物料档案。库存重量统一按克保存，只允许通过物料单据确认变更。</summary>
    [SugarTable("jy_material", "静悦物料档案")]
    public class JyMaterial : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string MaterialCode { get; set; }
        [SugarColumn(Length = 100)]
        public string MaterialName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? CategoryItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MetalItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SupplierId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal StockWeight { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal LockedWeight { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal AvgCostPerGram { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal SalePricePerGram { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
