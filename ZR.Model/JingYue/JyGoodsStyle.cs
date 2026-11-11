using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>珠宝商品款式档案。Status：0 启用，1 停用。</summary>
    [SugarTable("jy_goods_style", "静悦商品款式")]
    public class JyGoodsStyle : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string StyleCode { get; set; }
        [SugarColumn(Length = 100)]
        public string StyleName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StyleItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MaterialItemId { get; set; }
        /// <summary>材质大类：1 足金，2 足银，3 其他；用于归类而不替代材质字典。</summary>
        [SugarColumn(DefaultValue = "3")]
        public int MaterialGroup { get; set; } = 3;
        [SugarColumn(IsNullable = true)]
        public long? MetalItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SupplierId { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int ItemModeDefault { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal CostPrice { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal SalePrice { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal PackagingFee { get; set; }
        [SugarColumn(Length = 16, IsNullable = true)]
        public string Unit { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int IsXiangzhu { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MainFileId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? VideoFileId { get; set; }
        [SugarColumn(Length = 1000, IsNullable = true)]
        public string Introduce { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
