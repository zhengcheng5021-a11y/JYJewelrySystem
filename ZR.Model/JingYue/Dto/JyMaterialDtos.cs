using ZR.Model.System.Dto;

namespace ZR.Model.JingYue.Dto
{
    public class JyMaterialQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public long? CategoryItemId { get; set; }
        public long? SupplierId { get; set; }
        public long? MetalItemId { get; set; }
    }

    public class JyMaterialDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public long? CategoryItemId { get; set; }
        public string CategoryName { get; set; }
        public long? MetalItemId { get; set; }
        public string MetalName { get; set; }
        public long? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public decimal StockWeight { get; set; }
        public decimal LockedWeight { get; set; }
        public decimal AvailableWeight { get; set; }
        public decimal AvgCostPerGram { get; set; }
        public decimal SalePricePerGram { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyMaterialEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public long? CategoryItemId { get; set; }
        public long? MetalItemId { get; set; }
        public long? SupplierId { get; set; }
        public decimal SalePricePerGram { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyMaterialSummaryDto
    {
        public int MaterialCount { get; set; }
        public decimal TotalStockWeight { get; set; }
        public decimal TotalStockCost { get; set; }
        public int LowStockCount { get; set; }
        public List<JyMaterialDto> Recent { get; set; } = new();
    }

    public class JyMaterialOptionsDto
    {
        public List<JyLookupOptionDto> Categories { get; set; } = new();
        public List<JyLookupOptionDto> Metals { get; set; } = new();
        public List<JyOptionDto> Suppliers { get; set; } = new();
    }

    public class JyMaterialDocQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Direction { get; set; }
        public int? Status { get; set; }
        public long? SupplierId { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyMaterialDocLineEditDto
    {
        public long Id { get; set; }
        public long MaterialId { get; set; }
        public decimal Weight { get; set; }
        public decimal UnitCost { get; set; }
        public string Remark { get; set; }
    }

    public class JyMaterialDocEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public int Direction { get; set; }
        public string BizType { get; set; }
        public long? SupplierId { get; set; }
        public DateTime DocTime { get; set; }
        public string Remark { get; set; }
        public List<JyMaterialDocLineEditDto> Lines { get; set; } = new();
    }

    public class JyMaterialDocLineDto
    {
        public long Id { get; set; }
        public int SortNo { get; set; }
        public long MaterialId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string CategoryName { get; set; }
        public string MetalName { get; set; }
        public decimal Weight { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Amount { get; set; }
        public string Remark { get; set; }
    }

    public class JyMaterialDocDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string DocNo { get; set; }
        public int Direction { get; set; }
        public string DirectionText { get; set; }
        public string BizType { get; set; }
        public string BizTypeText { get; set; }
        public long? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public DateTime DocTime { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalAmount { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? ConfirmTime { get; set; }
        public List<JyMaterialDocLineDto> Lines { get; set; } = new();
    }

    public class JyMaterialDocDashboardDto
    {
        public int DraftCount { get; set; }
        public int ConfirmedToday { get; set; }
        public decimal TodayInWeight { get; set; }
        public decimal TodayOutWeight { get; set; }
        public List<JyMaterialDocDto> Pending { get; set; } = new();
    }

    public class JyMaterialLedgerQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public long? MaterialId { get; set; }
        public int? ChangeType { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyMaterialLedgerDto
    {
        public long Id { get; set; }
        public string LedgerNo { get; set; }
        public long MaterialId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public int ChangeType { get; set; }
        public string ChangeTypeText { get; set; }
        public decimal ChangeWeight { get; set; }
        public decimal BeforeWeight { get; set; }
        public decimal AfterWeight { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Amount { get; set; }
        public string BizType { get; set; }
        public string BizTypeText { get; set; }
        public long BizId { get; set; }
        public string BizNo { get; set; }
        public DateTime BizTime { get; set; }
        public string CreateBy { get; set; }
        public string Remark { get; set; }
    }
}
