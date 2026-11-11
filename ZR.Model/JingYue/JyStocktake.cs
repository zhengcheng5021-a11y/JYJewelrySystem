using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>盘点单。TakeType：1 正品，2 物料；Status：0 草稿，1 已确认，9 作废。</summary>
    [SugarTable("jy_stocktake", "静悦盘点单")]
    public class JyStocktake : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string StocktakeNo { get; set; }
        public int TakeType { get; set; }
        public DateTime TakeTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int LineCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DiffLineCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal GoodsDiffQty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal MaterialDiffWeight { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal GainAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal LossAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal DiffAmount { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? OperatorUserId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ConfirmUserId { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? ConfirmTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
