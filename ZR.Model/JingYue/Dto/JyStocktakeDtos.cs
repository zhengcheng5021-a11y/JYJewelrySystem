using ZR.Model.System.Dto;

namespace ZR.Model.JingYue.Dto
{
    public class JyStocktakeQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? TakeType { get; set; }
        public int? Status { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyStocktakeLineEditDto
    {
        public long Id { get; set; }
        public long GoodsItemId { get; set; }
        public long MaterialId { get; set; }
        public decimal ActualQty { get; set; }
        public string Remark { get; set; }
        public string CategoryKey { get; set; }
        public string CategoryName { get; set; }
    }

    public class JyStocktakeCategoryEditDto
    {
        public string CategoryKey { get; set; }
        public string CategoryName { get; set; }
        public string ParentCategoryKey { get; set; }
        public string ParentCategoryName { get; set; }
        public bool IsCompleted { get; set; }
        public string IncompleteReason { get; set; }
        public string AnomalyRemark { get; set; }
    }

    public class JyStocktakeEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public int TakeType { get; set; }
        public DateTime TakeTime { get; set; }
        public string Remark { get; set; }
        public List<JyStocktakeLineEditDto> Lines { get; set; } = new();
        public List<JyStocktakeCategoryEditDto> Categories { get; set; } = new();
    }

    public class JyStocktakeLineDto
    {
        public long Id { get; set; }
        public long StocktakeId { get; set; }
        public int SortNo { get; set; }
        public int ItemType { get; set; }
        public long? GoodsItemId { get; set; }
        public long? MaterialId { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public string StyleName { get; set; }
        public string MetalName { get; set; }
        public int MaterialGroup { get; set; }
        public string MaterialGroupName { get; set; }
        public string Unit { get; set; }
        public string CategoryKey { get; set; }
        public string CategoryName { get; set; }
        public decimal? Weight { get; set; }
        public int ItemMode { get; set; }
        public decimal BookQty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal DiffQty { get; set; }
        public decimal LockedQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiffAmount { get; set; }
        public int? ItemStatus { get; set; }
        public string StatusText { get; set; }
        public string DifferenceText { get; set; }
        public string Remark { get; set; }
    }

    public class JyStocktakeCategoryDto
    {
        public string CategoryKey { get; set; }
        public string CategoryName { get; set; }
        public string ParentCategoryKey { get; set; }
        public string ParentCategoryName { get; set; }
        public int MaterialGroup { get; set; }
        public string MaterialGroupName { get; set; }
        public int ItemType { get; set; }
        public int ItemCount { get; set; }
        public int CountedCount { get; set; }
        public decimal BookQty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal DiffQty { get; set; }
        public bool IsCompleted { get; set; }
        public string IncompleteReason { get; set; }
        public string AnomalyRemark { get; set; }
    }

    public class JyStocktakeInitDto
    {
        public int TakeType { get; set; }
        public long? StoreId { get; set; }
        public DateTime TakeTime { get; set; }
        public string Remark { get; set; }
    }

    public class JyStocktakeDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string StocktakeNo { get; set; }
        public int TakeType { get; set; }
        public string TakeTypeText { get; set; }
        public DateTime TakeTime { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public int LineCount { get; set; }
        public int DiffLineCount { get; set; }
        public decimal GoodsDiffQty { get; set; }
        public decimal MaterialDiffWeight { get; set; }
        public decimal GainAmount { get; set; }
        public decimal LossAmount { get; set; }
        public decimal DiffAmount { get; set; }
        public string DifferenceText { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? ConfirmTime { get; set; }
        public List<JyStocktakeLineDto> Lines { get; set; } = new();
        public List<JyStocktakeCategoryDto> Categories { get; set; } = new();
    }

    public class JyStocktakeOptionDto
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public string StyleName { get; set; }
        public string MetalName { get; set; }
        public string Unit { get; set; }
        public int ItemMode { get; set; }
        public decimal Qty { get; set; }
        public decimal LockedQty { get; set; }
        public decimal UnitCost { get; set; }
        public int Status { get; set; }
    }

    public class JyStocktakeDashboardDto
    {
        public int DraftCount { get; set; }
        public int ConfirmedToday { get; set; }
        public int DiffLineCount { get; set; }
        public decimal NetDiffAmount { get; set; }
        public int GoodsCount { get; set; }
        public int MaterialCount { get; set; }
        public List<JyStocktakeDto> Pending { get; set; } = new();
    }

    public class JyStocktakeReportQueryDto
    {
        public int? TakeType { get; set; }
        public string Keyword { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyStocktakeReportDto
    {
        public int DocumentCount { get; set; }
        public int LineCount { get; set; }
        public int DiffLineCount { get; set; }
        public decimal GoodsGainQty { get; set; }
        public decimal GoodsLossQty { get; set; }
        public decimal MaterialGainWeight { get; set; }
        public decimal MaterialLossWeight { get; set; }
        public decimal GainAmount { get; set; }
        public decimal LossAmount { get; set; }
        public decimal NetDiffAmount { get; set; }
        public int GoodsDocumentCount { get; set; }
        public int MaterialDocumentCount { get; set; }
        public List<JyStocktakeReportCategoryDto> Categories { get; set; } = new();
        public List<JyStocktakeDto> Documents { get; set; } = new();
        public List<JyStocktakeLineDto> Differences { get; set; } = new();
    }

    public class JyStocktakeReportCategoryDto
    {
        public int TakeType { get; set; }
        public string ParentCategoryKey { get; set; }
        public string ParentCategoryName { get; set; }
        public int DocumentCount { get; set; }
        public int ItemCount { get; set; }
        public decimal BookQty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal DiffQty { get; set; }
        public decimal DiffAmount { get; set; }
    }
}
