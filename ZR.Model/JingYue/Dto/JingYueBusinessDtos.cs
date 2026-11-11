using System.Text.Json.Serialization;
using ZR.Model.System.Dto;

namespace ZR.Model.JingYue.Dto
{
    public class JyInboundQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public long? SupplierId { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyInboundLineEditDto
    {
        public long Id { get; set; }
        public string GoodsName { get; set; }
        /// <summary>本件货品内含数量，例如 1 对、2 套、5 颗。</summary>
        public decimal Qty { get; set; } = 1;
        /// <summary>单件银重（克），与计量单位相互独立。</summary>
        public decimal? Weight { get; set; }
        /// <summary>计量单位，固定选项：条/颗/对/件/套。</summary>
        public string Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int PricingMode { get; set; } = 1;
        public decimal LaborFee { get; set; }
        public decimal PackagingFee { get; set; }
        public int LaborFeeMode { get; set; } = 1;
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        [Newtonsoft.Json.JsonConverter(typeof(ZR.Model.JingYue.IdToStringConverter))]
        public long? ImageFileId { get; set; }
        public string Remark { get; set; }
        public long? StyleItemId { get; set; }
        public long? MaterialItemId { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public long? MetalItemId { get; set; }
        public long? CertTypeId { get; set; }
        /// <summary>由服务端按字典回填，客户端传值会被覆盖。</summary>
        public string StyleItemName { get; set; }
        public string MaterialName { get; set; }
        public string MetalName { get; set; }
        public string CertTypeName { get; set; }
    }

    public class JyInboundEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public long? SupplierId { get; set; }
        public DateTime InboundTime { get; set; }
        public int SourceType { get; set; } = 1;
        public string Remark { get; set; }
        public List<JyInboundLineEditDto> Lines { get; set; } = new();
    }

    public class JyInboundLineDto
    {
        public long Id { get; set; }
        public int SortNo { get; set; }
        public long StyleId { get; set; }
        public string StyleCode { get; set; }
        public string GoodsName { get; set; }
        public string StyleName { get; set; }
        public string GoodsNo { get; set; }
        public int ItemMode { get; set; }
        public decimal Qty { get; set; }
        public decimal? Weight { get; set; }
        public string Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int PricingMode { get; set; }
        public decimal LaborFee { get; set; }
        public decimal PackagingFee { get; set; }
        public decimal PackagingAmount { get; set; }
        public int LaborFeeMode { get; set; }
        public decimal LineCost { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public long? MaterialItemId { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public string MaterialName { get; set; }
        public long? MetalItemId { get; set; }
        public string MetalName { get; set; }
        public long? CertTypeId { get; set; }
        public string CertTypeName { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        [Newtonsoft.Json.JsonConverter(typeof(ZR.Model.JingYue.IdToStringConverter))]
        public long? ImageFileId { get; set; }
        public string ImageUrl { get; set; }
        public string Remark { get; set; }
    }

    public class JyInboundDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string InboundNo { get; set; }
        public long? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public DateTime InboundTime { get; set; }
        public string PeriodText { get; set; }
        public int SourceType { get; set; }
        public int Status { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalCost { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public List<JyInboundLineDto> Lines { get; set; } = new();
    }

    public class JyInboundDashboardDto
    {
        public int DraftCount { get; set; }
        public int ConfirmedToday { get; set; }
        public decimal TodayQty { get; set; }
        public decimal TodayCost { get; set; }
        public int PendingSupplierCount { get; set; }
        public List<JyInboundDto> Pending { get; set; } = new();
        public List<JyInboundTrendDto> Trend { get; set; } = new();
    }

    public class JyInboundTrendDto
    {
        public string Date { get; set; }
        public decimal Qty { get; set; }
        public decimal Cost { get; set; }
    }

    public class JyInboundOptionsDto
    {
        public List<JyOptionDto> Suppliers { get; set; } = new();
        public List<JyOptionDto> Styles { get; set; } = new();
        public List<JyLookupOptionDto> Categories { get; set; } = new();
        public List<JyLookupOptionDto> Materials { get; set; } = new();
        public List<JyLookupOptionDto> Metals { get; set; } = new();
        public List<JyLookupOptionDto> CertTypes { get; set; } = new();
    }

    public class JyOptionDto
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ParentName { get; set; }
        public int ItemMode { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PackagingFee { get; set; }
        public string MaterialName { get; set; }
        public string MetalName { get; set; }
        public long? StyleItemId { get; set; }
        public long? MaterialItemId { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public long? MetalItemId { get; set; }
    }

    public class JyGoodsStyleQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public long? StyleItemId { get; set; }
        public long? SupplierId { get; set; }
    }

    public class JyGoodsItemQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public string Period { get; set; }
        public long? InboundId { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyGoodsItemEditDto
    {
        public long Id { get; set; }
        public string GoodsName { get; set; }
        public decimal? Weight { get; set; }
        public string Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int PricingMode { get; set; } = 1;
        public decimal LaborFee { get; set; }
        public decimal PackagingFee { get; set; }
        public int LaborFeeMode { get; set; } = 1;
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        [Newtonsoft.Json.JsonConverter(typeof(ZR.Model.JingYue.IdToStringConverter))]
        public long? ImageFileId { get; set; }
        public int Status { get; set; } = 1;
        public string Remark { get; set; }
        public long? StyleItemId { get; set; }
        public long? MaterialItemId { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public long? MetalItemId { get; set; }
        public long? CertTypeId { get; set; }
        /// <summary>由服务端按字典回填，客户端传值会被覆盖。</summary>
        public string StyleItemName { get; set; }
        public string MaterialName { get; set; }
        public string MetalName { get; set; }
        public string CertTypeName { get; set; }
    }

    public class JyGoodsItemDto
    {
        public long Id { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public string GoodsName { get; set; }
        public long StyleId { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public long? MaterialItemId { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public string MaterialName { get; set; }
        public long? MetalItemId { get; set; }
        public string MetalName { get; set; }
        public long? CertTypeId { get; set; }
        public string CertTypeName { get; set; }
        public decimal Qty { get; set; }
        public decimal? Weight { get; set; }
        public string Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int PricingMode { get; set; }
        public decimal LaborFee { get; set; }
        public decimal PackagingFee { get; set; }
        public int LaborFeeMode { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        [Newtonsoft.Json.JsonConverter(typeof(ZR.Model.JingYue.IdToStringConverter))]
        public long? ImageFileId { get; set; }
        public string ImageUrl { get; set; }
        public int Status { get; set; }
        public decimal LockedQty { get; set; }
        public string LockBizType { get; set; }
        public long? LockBizId { get; set; }
        public string LockText { get; set; }
        public string SupplierName { get; set; }
        public string InboundNo { get; set; }
        public DateTime? InboundTime { get; set; }
        public DateTime CreateTime { get; set; }
        public string Remark { get; set; }
    }

    public class JyGoodsStyleEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string StyleCode { get; set; }
        public string StyleName { get; set; }
        public long? StyleItemId { get; set; }
        public long? MaterialItemId { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public long? MetalItemId { get; set; }
        public long? SupplierId { get; set; }
        public int ItemModeDefault { get; set; } = 1;
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PackagingFee { get; set; }
        public string Unit { get; set; }
        public int IsXiangzhu { get; set; }
        public string Introduce { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyGoodsStyleDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string StyleCode { get; set; }
        public string StyleName { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public long? MaterialItemId { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public string MaterialName { get; set; }
        public long? MetalItemId { get; set; }
        public string MetalName { get; set; }
        public long? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public int ItemModeDefault { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PackagingFee { get; set; }
        public string Unit { get; set; }
        public int IsXiangzhu { get; set; }
        public string Introduce { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public int StockQty { get; set; }
        public decimal StockPieceQty { get; set; }
        public decimal StockAmount { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyGoodsOptionsDto
    {
        public List<JyLookupOptionDto> Categories { get; set; } = new();
        public List<JyLookupOptionDto> Materials { get; set; } = new();
        public List<JyLookupOptionDto> Metals { get; set; } = new();
        public List<JyLookupOptionDto> CertTypes { get; set; } = new();
        public List<JyOptionDto> Suppliers { get; set; } = new();
    }
    public class JyStockQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public long? StyleId { get; set; }
        public long? StyleItemId { get; set; }
    }

    public class JyStockItemDto
    {
        public long Id { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public long StyleId { get; set; }
        public string StyleName { get; set; }
        public string StyleItemName { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public string MaterialName { get; set; }
        public string MetalName { get; set; }
        public int ItemMode { get; set; }
        public decimal Qty { get; set; }
        public decimal LockedQty { get; set; }
        public decimal? Weight { get; set; }
        public string Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int PricingMode { get; set; } = 1;
        public decimal LaborFee { get; set; }
        public decimal PackagingFee { get; set; }
        public int LaborFeeMode { get; set; } = 1;
        public int Status { get; set; }
        public string SupplierName { get; set; }
        public string LockBizType { get; set; }
        public long? LockBizId { get; set; }
        public string LockText { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyStockSummaryDto
    {
        public int OnShelfCount { get; set; }
        public decimal TotalQty { get; set; }
        public decimal StockCost { get; set; }
        public decimal StockSale { get; set; }
        public int LowStockCount { get; set; }
        public Dictionary<int, int> StatusCounts { get; set; } = new();
        public List<JyStockItemDto> Rows { get; set; } = new();
    }

    public class JyStockLedgerQueryDto : PagerInfo
    {
        public long? GoodsItemId { get; set; }
        public string Keyword { get; set; }
        public int? ChangeType { get; set; }
    }

    public class JyStockLedgerDto
    {
        public long Id { get; set; }
        public string LedgerNo { get; set; }
        public long GoodsItemId { get; set; }
        public string GoodsNo { get; set; }
        public int ChangeType { get; set; }
        public decimal ChangeQty { get; set; }
        public decimal BeforeQty { get; set; }
        public decimal AfterQty { get; set; }
        public string BizType { get; set; }
        /// <summary>库存流水的业务类型中文名称，保留 BizType 供已有调用方兼容。</summary>
        public string BizTypeText { get; set; }
        public long BizId { get; set; }
        public DateTime BizTime { get; set; }
        public string CreateBy { get; set; }
        public string Remark { get; set; }
    }

    public class JySaleQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public int? OrderType { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JySaleItemEditDto
    {
        public long GoodsItemId { get; set; }
        public decimal Qty { get; set; }
        /// <summary>可编辑的内含数量快照，不影响销售件数与库存扣减。</summary>
        public decimal? StockQty { get; set; }
        public decimal DealPrice { get; set; }
        public decimal MarketMetalPricePerGram { get; set; }
        public int IsGift { get; set; }
        /// <summary>本单银重（克）；为空时沿用货品档案。</summary>
        public decimal? Weight { get; set; }
        /// <summary>本单数量单位；为空时沿用货品档案。</summary>
        public string Unit { get; set; }
        /// <summary>确认草稿时保留已保存的价格快照，不重新取金价或会员折扣。</summary>
        [global::System.Text.Json.Serialization.JsonIgnore]
        public bool PreservePriceSnapshot { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public int PricingMode { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public long? MetalPriceId { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal MetalPricePerGram { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public DateTime? PriceSnapshotTime { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal LaborFee { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal PackagingFee { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal PackagingAmount { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public int LaborFeeMode { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal MaterialAmount { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal LaborAmount { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal ListAmount { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public long? MemberDiscountRuleId { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal MemberDiscountRate { get; set; } = 1m;
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal MemberDiscountAmount { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal ActivityBenefit { get; set; }
        [global::System.Text.Json.Serialization.JsonIgnore]
        public decimal PayAmount { get; set; }
    }

    public class JySaleEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public int OrderType { get; set; } = 1;
        public long? MemberId { get; set; }
        public long? ActivityId { get; set; }
        /// <summary>手工调价，正数加价、负数减价；与抹零分开保存。</summary>
        public decimal AdjustAmount { get; set; }
        /// <summary>抹零金额，始终以正数保存。</summary>
        public decimal RoundOffAmount { get; set; }
        /// <summary>是否按会员等级折扣。默认 false，不选就不抵扣。</summary>
        public bool ApplyMemberDiscount { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        /// <summary>客户本人微信号</summary>
        public string CustomerWechatNo { get; set; }
        /// <summary>推荐人微信号；仅当本单客户为新客户时用于绑定推荐关系</summary>
        public string ReferrerWechatNo { get; set; }
        /// <summary>本次成交的推荐人会员 Id；一张单只允许一位，优先于微信号</summary>
        public long? ReferrerMemberId { get; set; }
        public int CustomerSex { get; set; }
        /// <summary>生日；未知年份时日期年份为 1900，BirthYear 为空。</summary>
        public DateTime? CustomerBirthday { get; set; }
        public int? CustomerBirthYear { get; set; }
        public string Remark { get; set; }
        public List<JySaleItemEditDto> Items { get; set; } = new();
        public List<JyCustomOffsetEditDto> CustomOffsets { get; set; } = new();
    }

    public class JyCustomerCandidateDto
    {
        public long MemberId { get; set; }
        public string MemberNo { get; set; }
        public string MemberName { get; set; }
        public string Phone { get; set; }
        public string WechatNo { get; set; }
        public int Sex { get; set; }
        public string LevelName { get; set; }
        public long? ReferrerMemberId { get; set; }
        public string ReferrerName { get; set; }
        public DateTime? SolarBirthday { get; set; }
        public int? BirthYear { get; set; }
        public string BirthdayText { get; set; }
    }

    public class JyCustomerMatchDto
    {
        public bool Exists { get; set; }
        public long? MemberId { get; set; }
        public string MemberNo { get; set; }
        public string MemberName { get; set; }
        public string Phone { get; set; }
        public string WechatNo { get; set; }
        public int Sex { get; set; }
        public long? LevelId { get; set; }
        public string LevelName { get; set; }
        public decimal DiscountRate { get; set; } = 1m;
        public decimal PointsBalance { get; set; }
        public decimal YearPoints { get; set; }
        public decimal TotalAmount { get; set; }
        public int OrderCount { get; set; }
        public DateTime? LastConsumeTime { get; set; }
        public DateTime? SolarBirthday { get; set; }
        public int? BirthYear { get; set; }
        public string BirthdayText { get; set; }
        public int MatchCount { get; set; }
        /// <summary>库内会员已维护的推荐人，供开单选择本次归属</summary>
        public List<JyMemberReferrerDto> Referrers { get; set; } = new();
        public List<JyCustomerCandidateDto> Candidates { get; set; } = new();
    }

    public class JySaleReturnItemEditDto
    {
        public long SaleOrderItemId { get; set; }
        public decimal Qty { get; set; }
    }

    public class JySaleReturnEditDto
    {
        public long SaleOrderId { get; set; }
        public DateTime? ReturnTime { get; set; }
        public string Reason { get; set; }
        public string Remark { get; set; }
        public List<JySaleReturnItemEditDto> Items { get; set; } = new();
    }

    public class JySaleReturnQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JySaleReturnItemDto
    {
        public long Id { get; set; }
        public long SaleOrderItemId { get; set; }
        public long GoodsItemId { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public string GoodsName { get; set; }
        public decimal Qty { get; set; }
        public decimal RefundPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public int IsGift { get; set; }
        public decimal SoldQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public decimal ReturnableQty { get; set; }
    }

    public class JySaleReturnDto
    {
        public long Id { get; set; }
        public string ReturnNo { get; set; }
        public long SaleOrderId { get; set; }
        public string SaleOrderNo { get; set; }
        public long? MemberId { get; set; }
        public string MemberName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int Status { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal ReturnQty { get; set; }
        public DateTime ReturnTime { get; set; }
        public string Reason { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public List<JySaleReturnItemDto> Items { get; set; } = new();
    }

    public class JySaleItemDto
    {
        public long Id { get; set; }
        public int SortNo { get; set; }
        public long GoodsItemId { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public string GoodsName { get; set; }
        public string StyleName { get; set; }
        public long? StyleItemId { get; set; }
        public int MaterialGroup { get; set; } = 3;
        public string StyleItemName { get; set; }
        /// <summary>款式分类的父级大类名称；两级分类时展示“大类 / 子类”。</summary>
        public string StyleCategoryName { get; set; }
        public string MaterialName { get; set; }
        public string MetalName { get; set; }
        public decimal? Weight { get; set; }
        public string Unit { get; set; }
        public decimal Qty { get; set; }
        public decimal? StockQty { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal DealPrice { get; set; }
        public int PricingMode { get; set; }
        public long? MetalPriceId { get; set; }
        public decimal MarketMetalPricePerGram { get; set; }
        public decimal MetalPricePerGram { get; set; }
        public DateTime? PriceSnapshotTime { get; set; }
        public decimal LaborFee { get; set; }
        public decimal PackagingFee { get; set; }
        public int LaborFeeMode { get; set; }
        public decimal MaterialAmount { get; set; }
        public decimal LaborAmount { get; set; }
        public decimal PackagingAmount { get; set; }
        public decimal ListAmount { get; set; }
        public long? MemberDiscountRuleId { get; set; }
        public decimal MemberDiscountRate { get; set; } = 1m;
        public decimal MemberDiscountAmount { get; set; }
        public decimal ActivityBenefit { get; set; }
        public decimal PayAmount { get; set; }
        public decimal LineAmount { get; set; }
        public int IsGift { get; set; }
        public decimal ReturnedQty { get; set; }
        public decimal ReturnableQty { get; set; }
    }

    public class JyMetalPriceQueryDto : PagerInfo
    {
        public long? MetalItemId { get; set; }
        public int? Status { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyMetalPriceEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public long MetalItemId { get; set; }
        public decimal PricePerGram { get; set; }
        public DateTime EffectiveTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyMetalPriceDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public long MetalItemId { get; set; }
        public string MetalName { get; set; }
        public decimal PricePerGram { get; set; }
        public DateTime EffectiveTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public DateTime CreateTime { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class JyDiscountRuleQueryDto : PagerInfo
    {
        public long? MemberLevelId { get; set; }
        public long? StyleItemId { get; set; }
        public int? PricingMode { get; set; }
        public int? Status { get; set; }
    }

    public class JyDiscountRuleEditDto
    {
        public long Id { get; set; }
        public long MemberLevelId { get; set; }
        public long? StyleItemId { get; set; }
        public int? PricingMode { get; set; }
        public decimal DiscountRate { get; set; } = 1m;
        public int DiscountBase { get; set; } = 1;
        public int Priority { get; set; }
        public DateTime? EffectiveTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyDiscountRuleDto
    {
        public long Id { get; set; }
        public long MemberLevelId { get; set; }
        public string MemberLevelName { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public string StyleParentName { get; set; }
        public int? PricingMode { get; set; }
        public decimal DiscountRate { get; set; }
        public int DiscountBase { get; set; }
        public int Priority { get; set; }
        public DateTime? EffectiveTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyPricingOptionsDto
    {
        public List<JyLookupOptionDto> Metals { get; set; } = new();
        public List<JyLookupOptionDto> Categories { get; set; } = new();
        public List<JyMemberLevelDto> Levels { get; set; } = new();
    }

    public class JySaleDto
    {
        public long Id { get; set; }
        public string OrderNo { get; set; }
        public int OrderType { get; set; }
        public long? MemberId { get; set; }
        public string MemberName { get; set; }
        public string MemberNo { get; set; }
        public DateTime? CustomerBirthday { get; set; }
        public int? CustomerBirthYear { get; set; }
        public string CustomerBirthdayText { get; set; }
        public long? CustomOrderId { get; set; }
        public string CustomNo { get; set; }
        public long? ActivityId { get; set; }
        public string ActivityName { get; set; }
        public int Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        /// <summary>是否使用会员等级折扣；有折扣的历史草稿重新打开时还原为已勾选。</summary>
        public bool ApplyMemberDiscount { get; set; }
        public decimal ActivityBenefit { get; set; }
        public decimal BasePayAmount { get; set; }
        public decimal AdjustAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal PayAmount { get; set; }
        public decimal CustomOffsetAmount { get; set; }
        public decimal CashAmount { get; set; }
        public decimal CostAmount { get; set; }
        public decimal PieceQty { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerWechatNo { get; set; }
        public string ReferrerWechatNo { get; set; }
        public long? ReferrerMemberId { get; set; }
        public string ReferrerName { get; set; }
        public int CustomerSex { get; set; }
        public decimal ReturnedAmount { get; set; }
        public int ReturnStatus { get; set; }
        public DateTime DealTime { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public List<JySaleItemDto> Items { get; set; } = new();
        public List<JySaleCustomOffsetDto> Offsets { get; set; } = new();
    }

    public class JySaleDashboardDto
    {
        public decimal TodayAmount { get; set; }
        public int TodayOrders { get; set; }
        public decimal TodayPieces { get; set; }
        public decimal AvgTicket { get; set; }
        public List<JySaleDto> Recent { get; set; } = new();
        public List<JySaleTrendDto> Trend { get; set; } = new();
    }

    public class JySaleTrendDto
    {
        public string Date { get; set; }
        public decimal Amount { get; set; }
        public int Orders { get; set; }
    }

    public class JySaleOptionsDto
    {
        public List<JyStockItemDto> Goods { get; set; } = new();
        public List<JyOptionDto> Members { get; set; } = new();
        public List<JyOptionDto> Activities { get; set; } = new();
    }

    public class JySalePreviewDto
    {
        public decimal TotalAmount { get; set; }
        public decimal MemberDiscount { get; set; }
        public decimal ActivityBenefit { get; set; }
        public decimal BasePayAmount { get; set; }
        public decimal AdjustAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal PayAmount { get; set; }
        public decimal CustomOffsetAmount { get; set; }
        public decimal CashAmount { get; set; }
        public decimal PointsEarned { get; set; }
        public string ActivityName { get; set; }
        public string MemberLevelName { get; set; }
        public decimal DiscountRate { get; set; }
        public string GiftName { get; set; }
        public List<JySaleItemDto> Items { get; set; } = new();
    }

    public class JyMemberQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        /// <summary>按客户绑定的员工微信筛选；传微信快照或 sys_user_wechat.Id</summary>
        public string MemberWechatNo { get; set; }
        public int? Status { get; set; }
        public long? LevelId { get; set; }
        public int? FollowStatus { get; set; }
        /// <summary>排序字段：totalAmount 累计消费，createTime 建档时间</summary>
        public string SortBy { get; set; }
        /// <summary>排序方向：asc 升序，desc 降序</summary>
        public string SortDirection { get; set; }
    }

    public class JyMemberExportDto
    {
        [ExcelColumn(Name = "会员号", Width = 16)]
        public string MemberNo { get; set; }

        [ExcelColumn(Name = "会员姓名", Width = 14)]
        public string MemberName { get; set; }

        [ExcelColumn(Name = "性别", Width = 8)]
        public string SexName { get; set; }

        [ExcelColumn(Name = "手机号", Width = 18)]
        public string Phone { get; set; }

        [ExcelColumn(Name = "微信号", Width = 20)]
        public string WechatNo { get; set; }

        [ExcelColumn(Name = "会员等级", Width = 14)]
        public string LevelName { get; set; }

        [ExcelColumn(Name = "跟进状态", Width = 12)]
        public string FollowStatusName { get; set; }

        [ExcelColumn(Name = "状态", Width = 8)]
        public string StatusName { get; set; }

        [ExcelColumn(Name = "生日", Width = 14)]
        public string Birthday { get; set; }

        [ExcelColumn(Name = "累计积分", Width = 12)]
        public decimal YearPoints { get; set; }

        [ExcelColumn(Name = "可用积分", Width = 12)]
        public decimal PointsBalance { get; set; }

        [ExcelColumn(Name = "累计消费", Width = 14)]
        public decimal TotalAmount { get; set; }

        [ExcelColumn(Name = "成交次数", Width = 10)]
        public int OrderCount { get; set; }

        [ExcelColumn(Name = "平均客单", Width = 12)]
        public decimal AvgTicket { get; set; }

        [ExcelColumn(Name = "最近消费", Width = 18, Format = "yyyy-MM-dd HH:mm")]
        public DateTime? LastConsumeTime { get; set; }

        [ExcelColumn(Name = "常购款式", Width = 16)]
        public string UsualStyleName { get; set; }

        [ExcelColumn(Name = "推荐人", Width = 20)]
        public string ReferrerNames { get; set; }

        [ExcelColumn(Name = "建档时间", Width = 18, Format = "yyyy-MM-dd HH:mm")]
        public DateTime CreateTime { get; set; }

        [ExcelColumn(Name = "备注", Width = 30)]
        public string Remark { get; set; }
    }

    public class JyMemberEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string MemberName { get; set; }
        public int Sex { get; set; }
        public string Phone { get; set; }
        /// <summary>本人微信号，可被任意搜索命中</summary>
        public string WechatNo { get; set; }
        /// <summary>推荐人（介绍人）微信号；只用于建档时绑定推荐关系</summary>
        public string ReferrerWechatNo { get; set; }
        /// <summary>客户可关联多个推荐人；销售单仍只选其中一位</summary>
        public List<JyMemberReferrerEditDto> Referrers { get; set; } = new();
        /// <summary>前端是否提交了推荐人区域；未提交时不改动已有关系</summary>
        public bool ReferrersTouched { get; set; }
        public DateTime? SolarBirthday { get; set; }
        public string LunarBirthday { get; set; }
        public int BirthCalendar { get; set; } = 1;
        public int? BirthYear { get; set; }
        public string Occupation { get; set; }
        public string Hobby { get; set; }
        public string WuXing { get; set; }
        public string RecommendDirection { get; set; }
        public long? LevelId { get; set; }
        public int FollowStatus { get; set; }
        public long? StaffUserId { get; set; }
        /// <summary>客户加的员工微信 Id；0/空表示未选择，只能从用户管理维护的微信中选择</summary>
        public long? MemberWechatId { get; set; }
        public long? UsualStyleItemId { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyMemberDto
    {
        public long Id { get; set; }
        public string MemberNo { get; set; }
        public string MemberName { get; set; }
        public int Sex { get; set; }
        public string Phone { get; set; }
        public string WechatNo { get; set; }
        public string ReferrerWechatNo { get; set; }
        public long? ReferrerMemberId { get; set; }
        public string ReferrerName { get; set; }
        public List<JyMemberReferrerDto> Referrers { get; set; } = new();
        public DateTime? SolarBirthday { get; set; }
        public string LunarBirthday { get; set; }
        public int BirthCalendar { get; set; }
        public int? BirthYear { get; set; }
        public string Occupation { get; set; }
        public string Hobby { get; set; }
        public string WuXing { get; set; }
        public string RecommendDirection { get; set; }
        public long? LevelId { get; set; }
        public string LevelName { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal PointsBalance { get; set; }
        public decimal YearPoints { get; set; }
        /// <summary>客户加的员工微信，员工姓名来自 sys_user</summary>
        public long? MemberWechatId { get; set; }
        public string MemberWechatNo { get; set; }
        public string MemberWechatStaffName { get; set; }
        public int FollowStatus { get; set; }
        public decimal YearAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int OrderCount { get; set; }
        public decimal AvgTicket { get; set; }
        public string UsualStyleName { get; set; }
        public List<JyMemberStylePreferenceDto> StylePreferences { get; set; } = new();
        public DateTime? LastFollowTime { get; set; }
        public string LastFollowTypeName { get; set; }
        public string LastFollowContent { get; set; }
        public int FollowCount { get; set; }
        public DateTime? LastConsumeTime { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyMemberReferrerEditDto
    {
        public long Id { get; set; }
        /// <summary>推荐人会员 Id；与微信号二选一</summary>
        public long ReferrerMemberId { get; set; }
        public string ReferrerWechatNo { get; set; }
        public bool IsPrimary { get; set; }
        public string Remark { get; set; }
    }

    public class JyMemberReferrerDto
    {
        public long Id { get; set; }
        public long MemberId { get; set; }
        public long ReferrerMemberId { get; set; }
        public string ReferrerName { get; set; }
        public string ReferrerNo { get; set; }
        public string ReferrerWechatNo { get; set; }
        public string ReferrerPhone { get; set; }
        public int IsPrimary { get; set; }
        /// <summary>1 建档录入，2 开单补录，3 手工维护</summary>
        public int Source { get; set; }
        public long? SaleOrderId { get; set; }
        public string SaleOrderNo { get; set; }
        public DateTime CreateTime { get; set; }
        public string Remark { get; set; }
    }

    public class JyMemberStylePreferenceDto
    {
        public long StyleItemId { get; set; }
        public string StyleName { get; set; }
        public decimal Amount { get; set; }
        public decimal Qty { get; set; }
        public int OrderCount { get; set; }
    }

    public class JyMemberLevelDto
    {
        public long Id { get; set; }
        public string LevelCode { get; set; }
        public string LevelName { get; set; }
        public decimal MinPoints { get; set; }
        public decimal? MaxPoints { get; set; }
        public decimal DiscountRate { get; set; }
        public int SortNo { get; set; }
        public int KeepOnYearClear { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyMemberOptionsDto
    {
        public List<JyMemberLevelDto> Levels { get; set; } = new();
        public List<JyLookupOptionDto> Categories { get; set; } = new();
        /// <summary>可关联的员工微信，来源用户管理</summary>
        public List<SysUserWechatOptionDto> StaffWechats { get; set; } = new();
    }

    public class JyActivityOptionsDto
    {
        public List<JyMemberLevelDto> Levels { get; set; } = new();
        public List<JyLookupOptionDto> Categories { get; set; } = new();
    }

    public class JyMemberDashboardDto
    {
        public int TotalMembers { get; set; }
        public int NewThisMonth { get; set; }
        public int ActiveThisMonth { get; set; }
        public decimal MonthAmount { get; set; }
        public List<JyMemberDto> Recent { get; set; } = new();
        public List<JyMemberLevelStatDto> Levels { get; set; } = new();
    }

    public class JyMemberLevelStatDto
    {
        public string LevelName { get; set; }
        public int Count { get; set; }
    }

    public class JyMemberLevelEditDto
    {
        public long Id { get; set; }
        public string LevelCode { get; set; }
        public string LevelName { get; set; }
        public decimal MinPoints { get; set; }
        public decimal? MaxPoints { get; set; }
        public decimal DiscountRate { get; set; } = 1;
        public int SortNo { get; set; }
        public int KeepOnYearClear { get; set; } = 1;
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyPointAdjustDto
    {
        public long MemberId { get; set; }
        public decimal ChangePoints { get; set; }
        public string Reason { get; set; }
    }

    public class JyPointLedgerQueryDto : PagerInfo
    {
        public long? MemberId { get; set; }
        public int? ChangeType { get; set; }
    }

    public class JyPointLedgerDto
    {
        public long Id { get; set; }
        public long MemberId { get; set; }
        public string MemberNo { get; set; }
        public string MemberName { get; set; }
        public int ChangeType { get; set; }
        public decimal ChangePoints { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal YearPointsAfter { get; set; }
        public string Reason { get; set; }
        public DateTime BizTime { get; set; }
    }

    public class JyPointRuleDto
    {
        public long Id { get; set; }
        public decimal AmountBase { get; set; } = 1m;
        public decimal PointsAwarded { get; set; } = 1m;
        public decimal CashBase { get; set; }
        public decimal CashAwarded { get; set; }
        public decimal ReferralCashRate { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }

    public class JyPointRuleEditDto
    {
        public long Id { get; set; }
        public decimal AmountBase { get; set; } = 1m;
        public decimal PointsAwarded { get; set; } = 1m;
        public decimal CashBase { get; set; }
        public decimal CashAwarded { get; set; }
        public decimal ReferralCashRate { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyMemberHistoryDto
    {
        public JyMemberDto Member { get; set; }
        public List<JySaleDto> Sales { get; set; } = new();
        public List<JyPointLedgerDto> Points { get; set; } = new();
    }

    public class JyPointClearResultDto
    {
        public int MemberCount { get; set; }
        public decimal ClearedPoints { get; set; }
        public string BatchNo { get; set; }
        public DateTime ClearTime { get; set; }
    }

    public class JyActivityQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public int? ActivityType { get; set; }
    }

    public class JyActivityEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public string ActivityName { get; set; }
        public int ActivityType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ScopeType { get; set; } = 1;
        public long? StyleItemId { get; set; }
        public decimal? DiscountRate { get; set; }
        public decimal ReduceAmount { get; set; }
        public string GiftName { get; set; }
        public decimal PointsMultiple { get; set; } = 1;
        public long? MemberLevelId { get; set; }
        public int JoinLimit { get; set; }
        public decimal BudgetAmount { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyActivityDto
    {
        public long Id { get; set; }
        public string ActivityNo { get; set; }
        public string ActivityName { get; set; }
        public int ActivityType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ScopeType { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public decimal? DiscountRate { get; set; }
        public decimal ReduceAmount { get; set; }
        public string GiftName { get; set; }
        public decimal PointsMultiple { get; set; }
        public long? MemberLevelId { get; set; }
        public string MemberLevelName { get; set; }
        public int JoinLimit { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal UsedAmount { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public int JoinCount { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyActivityStatusDto
    {
        public long Id { get; set; }
        public int Status { get; set; }
    }

    public class JyActivityDashboardDto
    {
        public int RunningCount { get; set; }
        public int UpcomingCount { get; set; }
        public decimal MonthBenefit { get; set; }
        public int JoinCount { get; set; }
        public List<JyActivityDto> Running { get; set; } = new();
        public List<JyActivityDto> Upcoming { get; set; } = new();
    }

    public class JyCustomDashboardDto
    {
        public int PendingCount { get; set; }
        public decimal RemainDepositAmount { get; set; }
        public int TodayCount { get; set; }
        public decimal TodayDepositAmount { get; set; }
        public List<JyCustomDto> Recent { get; set; } = new();
    }

    public class JyCustomOffsetEditDto
    {
        public long CustomOrderId { get; set; }
        /// <summary>要核销的定金商品明细；旧版单商品请求可不传。</summary>
        public long? CustomItemId { get; set; }
        public decimal OffsetAmount { get; set; }
    }

    public class JySaleCustomOffsetDto
    {
        public long Id { get; set; }
        public long CustomOrderId { get; set; }
        public long? CustomItemId { get; set; }
        public string CustomNo { get; set; }
        public string GoodsName { get; set; }
        public decimal OffsetAmount { get; set; }
        public int Status { get; set; }
        public decimal RemainAmount { get; set; }
    }

    public class JyCustomQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        /// <summary>0 待联系客户，1 已到货；为空时查询全部。</summary>
        public int? ArrivalStatus { get; set; }
        /// <summary>1 表示只查询已到货、仍需联系客户的定制单。</summary>
        public int? ContactPending { get; set; }
        public int? RemainOnly { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyCustomEditDto
    {
        /// <summary>大于 0 表示修改现有定制单；0 表示新增。</summary>
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public long? MemberId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int CustomerSex { get; set; }
        public string GoodsName { get; set; }
        public long? GoodsItemId { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public long? StyleItemId { get; set; }
        public long? MaterialItemId { get; set; }
        public long? MetalItemId { get; set; }
        public decimal? Weight { get; set; }
        public decimal ExpectPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public DateTime? CustomDate { get; set; }
        public DateTime? PickupDate { get; set; }
        public string Remark { get; set; }
        /// <summary>多商品明细。新版本传 Items；为空时兼容旧版单商品字段。</summary>
        public List<JyCustomItemEditDto> Items { get; set; } = new();
    }

    public class JyCustomItemEditDto
    {
        /// <summary>大于 0 表示修改已有明细；0 表示新增。</summary>
        public long Id { get; set; }
        public int SortNo { get; set; }
        public long? GoodsItemId { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public string GoodsName { get; set; }
        public long? StyleItemId { get; set; }
        public long? MaterialItemId { get; set; }
        public long? MetalItemId { get; set; }
        public decimal? Weight { get; set; }
        public decimal ExpectPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public DateTime? PickupDate { get; set; }
        public string Remark { get; set; }
    }

    public class JyCustomArrivalDto
    {
        public long Id { get; set; }
        /// <summary>要登记到货的商品明细；旧版单商品请求可不传。</summary>
        public long? ItemId { get; set; }
        public string Barcode { get; set; }
        public string Remark { get; set; }
    }

    public class JyCustomPickupDto
    {
        public long Id { get; set; }
        public long? ItemId { get; set; }
        public string Remark { get; set; }
    }

    public class JyCustomRefundDto
    {
        public long Id { get; set; }
        /// <summary>要退款的商品明细，新版本必填。</summary>
        public long? ItemId { get; set; }
        public decimal Amount { get; set; }
        public string Remark { get; set; }
    }

    public class JyCustomLedgerDto
    {
        public long Id { get; set; }
        public long? CustomItemId { get; set; }
        public string GoodsName { get; set; }
        public int ChangeType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public long? SaleOrderId { get; set; }
        public string Remark { get; set; }
        public DateTime BizTime { get; set; }
        public string CreateBy { get; set; }
    }

    public class JyFactoryReturnQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public long? SupplierId { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyFactoryReturnItemEditDto
    {
        public long GoodsItemId { get; set; }
        public decimal Qty { get; set; } = 1;
        public string Reason { get; set; }
    }

    public class JyFactoryReturnEditDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public long? SupplierId { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Remark { get; set; }
        public List<JyFactoryReturnItemEditDto> Items { get; set; } = new();
    }

    public class JyFactoryReturnItemDto
    {
        public long Id { get; set; }
        public long GoodsItemId { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public string GoodsName { get; set; }
        public decimal Qty { get; set; }
        public decimal CostPrice { get; set; }
        public string Reason { get; set; }
        public int SortNo { get; set; }
    }

    public class JyFactoryReturnDto
    {
        public long Id { get; set; }
        public string ReturnNo { get; set; }
        public DateTime ReturnDate { get; set; }
        public long? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public int Status { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalCost { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public List<JyFactoryReturnItemDto> Items { get; set; } = new();
    }

    public class JyCustomDto
    {
        public long Id { get; set; }
        /// <summary>销售可用的商品明细 Id；可用定金查询时用于逐件核销。</summary>
        public long? ItemId { get; set; }
        public string CustomNo { get; set; }
        public DateTime CustomDate { get; set; }
        public long? MemberId { get; set; }
        public string MemberName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int CustomerSex { get; set; }
        public string GoodsName { get; set; }
        public long? GoodsItemId { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public DateTime? BindTime { get; set; }
        public DateTime? PickupDate { get; set; }
        public int ArrivalStatus { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public string ArrivalBy { get; set; }
        public string ArrivalRemark { get; set; }
        public bool ContactPending { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public long? MaterialItemId { get; set; }
        public string MaterialName { get; set; }
        public long? MetalItemId { get; set; }
        public string MetalName { get; set; }
        public decimal? Weight { get; set; }
        public decimal ExpectPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal OffsetAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal RemainAmount { get; set; }
        public decimal AvailableAmount { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public int ItemCount { get; set; }
        public int ArrivedItemCount { get; set; }
        public int PickedItemCount { get; set; }
        public List<JyCustomItemDto> Items { get; set; } = new();
        public List<JyCustomLedgerDto> Ledgers { get; set; } = new();
    }

    public class JyCustomItemDto
    {
        public long Id { get; set; }
        public long CustomOrderId { get; set; }
        public string CustomNo { get; set; }
        public int SortNo { get; set; }
        public long? GoodsItemId { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public DateTime? BindTime { get; set; }
        public string GoodsName { get; set; }
        public DateTime? PickupDate { get; set; }
        public int ArrivalStatus { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public string ArrivalBy { get; set; }
        public string ArrivalRemark { get; set; }
        public int PickupStatus { get; set; }
        public DateTime? PickupTime { get; set; }
        public string PickupBy { get; set; }
        public string PickupRemark { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public long? MaterialItemId { get; set; }
        public string MaterialName { get; set; }
        public long? MetalItemId { get; set; }
        public string MetalName { get; set; }
        public decimal? Weight { get; set; }
        public decimal ExpectPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal OffsetAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal RemainAmount { get; set; }
        public decimal AvailableAmount { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public bool ContactPending { get; set; }
    }
}



