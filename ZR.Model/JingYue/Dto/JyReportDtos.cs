namespace ZR.Model.JingYue.Dto
{
    public class JySalesMetricQueryDto
    {
        /// <summary>day / week / month / year。</summary>
        public string Granularity { get; set; } = "day";
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JySalesMetricDto
    {
        public string Period { get; set; }
        public string Label { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal SalesPieces { get; set; }
        public decimal ReturnPieces { get; set; }
        public decimal NetPieces { get; set; }
        public int OrderCount { get; set; }
        public decimal PiecesPerOrder { get; set; }
        public decimal AmountPerPiece { get; set; }
    }

    public class JySalesMetricReportDto
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Granularity { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal SalesPieces { get; set; }
        public decimal ReturnPieces { get; set; }
        public decimal NetPieces { get; set; }
        public int OrderCount { get; set; }
        public decimal PiecesPerOrder { get; set; }
        public decimal AmountPerPiece { get; set; }
        public List<JySalesMetricDto> Trend { get; set; } = new();
    }

    public class JyTrafficQueryDto
    {
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Granularity { get; set; } = "day";
    }

    public class JyTrafficSaveDto
    {
        public long Id { get; set; }
        public long? StoreId { get; set; }
        public DateTime TrafficDate { get; set; }
        public int VisitorCount { get; set; }
        public int WechatCount { get; set; }
        public int LostStrollCount { get; set; }
        public int LostPriceCount { get; set; }
        public int LostStyleCount { get; set; }
        public int LostConsiderCount { get; set; }
        public string Remark { get; set; }
        /// <summary>修改非当天数据时必须显式确认。</summary>
        public bool ConfirmHistorical { get; set; }
    }

    public class JyTrafficDayDto
    {
        public long? Id { get; set; }
        public string Date { get; set; }
        public string Label { get; set; }
        public int VisitorCount { get; set; }
        public int WechatCount { get; set; }
        public int DealOrderCount { get; set; }
        public decimal DealAmount { get; set; }
        /// <summary>进店客流未登记时为 null，避免与真实 0% 成交率混淆。</summary>
        public decimal? DealRate { get; set; }
        public decimal WechatRate { get; set; }
        public int LostStrollCount { get; set; }
        public int LostPriceCount { get; set; }
        public int LostStyleCount { get; set; }
        public int LostConsiderCount { get; set; }
        public int LostTotal { get; set; }
        public string Remark { get; set; }
        public bool IsReadOnly { get; set; }
    }

    public class JyTrafficReportDto
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Granularity { get; set; }
        public int VisitorCount { get; set; }
        public int WechatCount { get; set; }
        public int DealOrderCount { get; set; }
        public decimal DealAmount { get; set; }
        public decimal DealRate { get; set; }
        public decimal WechatRate { get; set; }
        public int LostStrollCount { get; set; }
        public int LostPriceCount { get; set; }
        public int LostStyleCount { get; set; }
        public int LostConsiderCount { get; set; }
        public int LostTotal { get; set; }
        public List<JyTrafficDayDto> Trend { get; set; } = new();
    }

    public class JyStylePerformanceQueryDto
    {
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
        /// <summary>amount / gross / grossRate / pieces / avgTicket</summary>
        public string SortBy { get; set; } = "amount";
    }

    public class JyStylePerformanceRowDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long? ParentCategoryId { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal SalesPieces { get; set; }
        public decimal ReturnPieces { get; set; }
        public decimal NetPieces { get; set; }
        public decimal CostAmount { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossRate { get; set; }
        public decimal AvgTicket { get; set; }
        public decimal Share { get; set; }
        public int OrderCount { get; set; }
        /// <summary>一级分类下的二级款式明细；二级行不再继续嵌套。</summary>
        public List<JyStylePerformanceRowDto> Children { get; set; } = new();
    }

    public class JyStylePerformanceReportDto
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal NetPieces { get; set; }
        public decimal CostAmount { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossRate { get; set; }
        public int OrderCount { get; set; }
        public List<JyStylePerformanceRowDto> Rows { get; set; } = new();
    }

    public class JySalesLedgerQueryDto
    {
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
        /// <summary>material / style / category；默认按材质大类汇总。</summary>
        public string GroupBy { get; set; } = "material";
        public string Keyword { get; set; }
    }

    public class JySalesLedgerRowDto
    {
        public string GroupKey { get; set; }
        public string GroupName { get; set; }
        public string Level { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal CostAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossRate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal SalesPieces { get; set; }
        public decimal ReturnPieces { get; set; }
        public int OrderCount { get; set; }
        public decimal Share { get; set; }
        public List<JySalesLedgerRowDto> Children { get; set; } = new();
    }

    public class JySalesLedgerDetailDto
    {
        public long OrderId { get; set; }
        public long ItemId { get; set; }
        public string DetailKey { get; set; }
        public string OrderNo { get; set; }
        public DateTime DealTime { get; set; }
        public DateTime? ReturnTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public string GoodsName { get; set; }
        public string MaterialGroupName { get; set; }
        public string MaterialName { get; set; }
        public string StyleParentName { get; set; }
        public string StyleItemName { get; set; }
        public string MetalName { get; set; }
        public decimal? Weight { get; set; }
        public string Unit { get; set; }
        public decimal Qty { get; set; }
        public decimal ListAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PayAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal CostAmount { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossRate { get; set; }
        public decimal ReturnQty { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetAmount { get; set; }
        public int OrderType { get; set; }
        public string OrderTypeName { get; set; }
    }

    public class JySalesLedgerReportDto
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public string GroupBy { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal CostAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossRate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal SalesPieces { get; set; }
        public decimal ReturnPieces { get; set; }
        public int OrderCount { get; set; }
        public List<JySalesLedgerRowDto> Rows { get; set; } = new();
        public List<JySalesLedgerDetailDto> Details { get; set; } = new();
    }

    public class JySalesGoalQueryDto
    {
        /// <summary>yyyy-MM，默认服务器当前月份。</summary>
        public string Month { get; set; }
        public DateTime? PeriodBegin { get; set; }
        public DateTime? PeriodEnd { get; set; }
    }

    public class JyGoalYearSaveDto
    {
        public int GoalYear { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetQty { get; set; }
    }

    public class JyGoalMonthSaveDto
    {
        public int GoalYear { get; set; }
        public int GoalMonth { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetQty { get; set; }
    }

    public class JyGoalCategorySaveDto
    {
        public long Id { get; set; }
        public int GoalYear { get; set; }
        public int GoalMonth { get; set; }
        public long CategoryId { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetQty { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class JyGoalCategoryRowDto
    {
        public long? Id { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetQty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DoneAmount { get; set; }
        public decimal DoneQty { get; set; }
        public decimal ActualUnitPrice { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossRate { get; set; }
        public decimal Percent { get; set; }
        public decimal NeedAmount { get; set; }
        public int? NeedQty { get; set; }
    }

    public class JyGoalReportDto
    {
        public int GoalYear { get; set; }
        public int GoalMonth { get; set; }
        public string Month { get; set; }
        public decimal YearTargetAmount { get; set; }
        public decimal YearTargetQty { get; set; }
        public decimal MonthTargetAmount { get; set; }
        public decimal MonthTargetQty { get; set; }
        public decimal YearToMonthAmount { get; set; }
        public decimal MonthToDayAmount { get; set; }
        public int DaysInMonth { get; set; }
        public int DaysLeft { get; set; }
        public decimal DoneAmount { get; set; }
        public decimal DoneQty { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal NeedPerDay { get; set; }
        public decimal Percent { get; set; }
        public decimal TodayAmount { get; set; }
        public DateTime PeriodBegin { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal PeriodDoneAmount { get; set; }
        public decimal PeriodDoneQty { get; set; }
        public decimal PeriodTargetAmount { get; set; }
        public decimal PeriodPercent { get; set; }
        public List<JyGoalCategoryRowDto> Categories { get; set; } = new();
    }

    public class JyOpsBreakdownQueryDto
    {
        /// <summary>yyyy-MM，默认服务器当前月份。</summary>
        public string Month { get; set; }
        /// <summary>yyyy-MM-dd，默认服务器今天；用于计算今日动作与周目标。</summary>
        public DateTime? Date { get; set; }
    }

    public class JyGoalWeightSaveDto
    {
        public int GoalYear { get; set; }
        public int GoalMonth { get; set; }
        public decimal SalesWeight { get; set; } = 60;
        public decimal StockWeight { get; set; } = 20;
        public decimal MarginWeight { get; set; } = 20;
    }

    public class JyGoalPlanRowSaveDto
    {
        public long CategoryId { get; set; }
        public decimal PlanRatio { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal GrossMarginRate { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ConversionRate { get; set; } = 30;
        public decimal NewCustomerRate { get; set; } = 60;
        public string ActionHigh { get; set; }
        public string ActionMedium { get; set; }
        public string ActionLow { get; set; }
    }

    public class JyGoalPlanBatchSaveDto
    {
        public int GoalYear { get; set; }
        public int GoalMonth { get; set; }
        public List<JyGoalPlanRowSaveDto> Rows { get; set; } = new();
    }

    public class JyTargetCheckDto
    {
        public decimal TargetAmount { get; set; }
        public decimal Average3Month { get; set; }
        public decimal Trend { get; set; }
        public decimal StockSupport { get; set; }
        public decimal Ceiling { get; set; }
        public bool IsHigh { get; set; }
        public string Suggestion { get; set; }
        public string MissingData { get; set; }
    }

    public class JyCategoryWeightDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Percent { get; set; }
        public decimal SalesShare { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal SalesQty { get; set; }
        public decimal GrossRate { get; set; }
        public decimal StockQty { get; set; }
        public decimal StockValue { get; set; }
        public decimal StaleQty { get; set; }
        public decimal Score { get; set; }
        public string Basis { get; set; }
        public string SellPoint { get; set; }
    }

    public class JyWeekTargetDto
    {
        public string WeekLabel { get; set; }
        public decimal TargetAmount { get; set; }
        public string FocusCategories { get; set; }
        public string KeyAction { get; set; }
    }

    public class JyTodayActionDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Priority { get; set; }
        public string ActionType { get; set; }
        public string SourceLabel { get; set; }
        public string ActionText { get; set; }
        public string Basis { get; set; }
        public string Goal { get; set; }
        public string QuantifiedGoal { get; set; }
        public bool Manual { get; set; }
    }

    public class JyComboDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long? CategoryId1 { get; set; }
        public long? CategoryId2 { get; set; }
        public List<string> Categories { get; set; } = new();
        public string Fit { get; set; }
        public string Scene { get; set; }
        public decimal Percent { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalAmount { get; set; }
        public int Status { get; set; }
        public int SortNo { get; set; }
        public string Basis { get; set; }
    }

    public class JyComboQueryDto
    {
        /// <summary>day / week / month / year。</summary>
        public string Granularity { get; set; } = "day";
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
        /// <summary>首页固定近 7 天时传 true，仅返回启用组合。</summary>
        public bool Home { get; set; }
    }

    public class JyComboEditDto
    {
        public long Id { get; set; }
        public string ComboName { get; set; }
        public long CategoryId1 { get; set; }
        public long CategoryId2 { get; set; }
        public string FitCrowd { get; set; }
        public string UseScene { get; set; }
        public int Status { get; set; }
        public int SortNo { get; set; }
    }

    public class JyComboTrendDto
    {
        public long ComboId { get; set; }
        public string ComboName { get; set; }
        public string Period { get; set; }
        public string Label { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percent { get; set; }
    }

    public class JyComboReportDto
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Granularity { get; set; }
        public decimal TotalAmount { get; set; }
        public List<JyComboDto> Combos { get; set; } = new();
        public List<JyComboTrendDto> Trend { get; set; } = new();
    }

    public class JyVisitSuggestionDto
    {
        public long MemberId { get; set; }
        public string MemberName { get; set; }
        public string Phone { get; set; }
        public string PreferredCategory { get; set; }
        public int CycleDays { get; set; }
        public int DaysSince { get; set; }
        public int DueDays { get; set; }
        public decimal StaleQty { get; set; }
        public string Suggest { get; set; }
        public DateTime? LastFollowTime { get; set; }
    }

    public class JyGoalPlanRowDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal PlanRatio { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal GrossMarginRate { get; set; }
        public decimal EstimatedGross { get; set; }
        public decimal UnitPrice { get; set; }
        public int EstimatedPieces { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal NewCustomerRate { get; set; }
        public decimal DoneAmount { get; set; }
        public decimal DoneQty { get; set; }
        public decimal Percent { get; set; }
        public decimal GrossRate { get; set; }
        public decimal StockQty { get; set; }
        public decimal StaleQty { get; set; }
        public string ActionHigh { get; set; }
        public string ActionMedium { get; set; }
        public string ActionLow { get; set; }
        public string AutoActionMain { get; set; }
        public string AutoActionAlt { get; set; }
        public string AutoActionBasis { get; set; }
        public string AutoActionState { get; set; }
    }

    public class JyCategoryCustomerPlanDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal NewCustomerRate { get; set; }
        public int Orders { get; set; }
        public int IntentCustomers { get; set; }
        public int NewConsult { get; set; }
        public int OldVisit { get; set; }
        public int PerDayNew { get; set; }
        public int PerDayOld { get; set; }
        public int PerDayAll { get; set; }
        public int Workdays { get; set; }
        public bool TooHeavy { get; set; }
    }

    public class JyOpsBreakdownDto
    {
        public string Date { get; set; }
        public string Month { get; set; }
        public int RestDays { get; set; }
        public string StatusLine { get; set; }
        public JyGoalWeightSaveDto Weights { get; set; } = new();
        public JyTargetCheckDto TargetCheck { get; set; } = new();
        public List<JyCategoryWeightDto> CategoryWeights { get; set; } = new();
        public List<JyWeekTargetDto> Weeks { get; set; } = new();
        public List<JyTodayActionDto> TodayActions { get; set; } = new();
        public List<JyComboDto> Combos { get; set; } = new();
        public List<JyVisitSuggestionDto> Visits { get; set; } = new();
        public List<JyGoalPlanRowDto> PlanRows { get; set; } = new();
        public List<JyCategoryCustomerPlanDto> CustomerPlans { get; set; } = new();
    }

    public class JyInventoryQueryDto
    {
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
        /// <summary>1 足金类，2 足银类，3 其他类。</summary>
        public int? MaterialGroup { get; set; }
        /// <summary>款式大类字典 Id。</summary>
        public long? StyleParentId { get; set; }
        /// <summary>款式分类字典 Id。</summary>
        public long? StyleItemId { get; set; }
    }

    public class JyInventoryRowDto
    {
        public string Level { get; set; }
        public int MaterialGroup { get; set; }
        public string MaterialGroupName { get; set; }
        public long? StyleParentId { get; set; }
        public string StyleParentName { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public int ItemCount { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal InboundQty { get; set; }
        public decimal OutboundQty { get; set; }
        public decimal AdjustQty { get; set; }
        public decimal EndingQty { get; set; }
        public decimal StockCost { get; set; }
        public decimal StockSale { get; set; }
        public decimal StockWeight { get; set; }
        public List<JyInventoryRowDto> Children { get; set; } = new();
    }

    public class JyInventoryReportDto
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ItemCount { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal InboundQty { get; set; }
        public decimal OutboundQty { get; set; }
        public decimal AdjustQty { get; set; }
        public decimal EndingQty { get; set; }
        public decimal StockCost { get; set; }
        public decimal StockSale { get; set; }
        public decimal StockWeight { get; set; }
        public List<JyInventoryRowDto> Rows { get; set; } = new();
    }

    public class JyStockAgeQueryDto
    {
        public DateTime? AsOfDate { get; set; }
        public int? MaterialGroup { get; set; }
        public long? StyleParentId { get; set; }
        public long? StyleItemId { get; set; }
    }

    public class JyStockAgeBucketDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public decimal Qty { get; set; }
        public decimal Percent { get; set; }
        public decimal Cost { get; set; }
        public decimal CostPercent { get; set; }
    }

    public class JyStockAgeRowDto
    {
        public string Level { get; set; }
        public int MaterialGroup { get; set; }
        public string MaterialGroupName { get; set; }
        public long? StyleParentId { get; set; }
        public string StyleParentName { get; set; }
        public long? StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalWeight { get; set; }
        public List<JyStockAgeBucketDto> Buckets { get; set; } = new();
        public List<JyStockAgeRowDto> Children { get; set; } = new();
    }

    public class JyStockAgeReportDto
    {
        public DateTime AsOfDate { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalWeight { get; set; }
        public List<JyStockAgeBucketDto> Buckets { get; set; } = new();
        public List<JyStockAgeRowDto> Rows { get; set; } = new();
    }

    public class JyStockAgeDetailQueryDto : PagerInfo
    {
        public DateTime? AsOfDate { get; set; }
        public int? MaterialGroup { get; set; }
        public long? StyleParentId { get; set; }
        public long? StyleItemId { get; set; }
        /// <summary>lt3m / m3to5 / within1y / y1to3 / y3to5 / over5。</summary>
        public string Bucket { get; set; }
    }

    public class JyStockAgeDetailRowDto
    {
        public long Id { get; set; }
        public string GoodsNo { get; set; }
        public string Barcode { get; set; }
        public string StyleName { get; set; }
        public string StyleParentName { get; set; }
        public string StyleItemName { get; set; }
        public int MaterialGroup { get; set; }
        public string MaterialGroupName { get; set; }
        public string MaterialName { get; set; }
        public string MetalName { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal? Weight { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal StockCost { get; set; }
        public decimal StockSale { get; set; }
        public DateTime InboundTime { get; set; }
        public int AgeDays { get; set; }
        public string Bucket { get; set; }
        public string BucketLabel { get; set; }
        public int Status { get; set; }
    }
}
