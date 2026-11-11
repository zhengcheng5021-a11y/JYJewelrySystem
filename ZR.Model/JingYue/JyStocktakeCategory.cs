namespace ZR.Model.JingYue
{
    /// <summary>盘点单中的分类进度快照。</summary>
    [SugarTable("jy_stocktake_category", "静悦盘点分类进度")]
    public class JyStocktakeCategory
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long StocktakeId { get; set; }
        public int ItemType { get; set; }
        [SugarColumn(Length = 150)]
        public string CategoryKey { get; set; }
        [SugarColumn(Length = 100)]
        public string CategoryName { get; set; }
        /// <summary>一级款式分类快照；历史单据为空时由款式字典补算。</summary>
        [SugarColumn(Length = 150, IsNullable = true)]
        public string ParentCategoryKey { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string ParentCategoryName { get; set; }
        public int ItemCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int IsCompleted { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string IncompleteReason { get; set; }
        /// <summary>一级分类账实差异说明；与未完成原因分开保存。</summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string AnomalyRemark { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string Update_by { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? Update_time { get; set; }
    }
}
