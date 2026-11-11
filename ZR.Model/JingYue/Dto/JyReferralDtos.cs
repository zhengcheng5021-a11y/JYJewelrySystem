namespace ZR.Model.JingYue.Dto
{
    public class JyReferralQueryDto : PagerInfo
    {
        /// <summary>推荐人/被推荐人 姓名、手机号、微信号、推荐单号 模糊搜索</summary>
        public string Keyword { get; set; }
        /// <summary>0 待返现，1 已返现，2 已作废；null 全部</summary>
        public int? Status { get; set; }
        public long? ReferrerMemberId { get; set; }
        public long? ReferralMemberId { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyReferralRewardDto
    {
        public long Id { get; set; }
        public long ReferrerMemberId { get; set; }
        public string ReferrerName { get; set; }
        public string ReferrerPhone { get; set; }
        public string ReferrerWechatNo { get; set; }
        public long ReferralMemberId { get; set; }
        public string ReferralName { get; set; }
        public string ReferralPhone { get; set; }
        public long SaleOrderId { get; set; }
        public string SaleOrderNo { get; set; }
        public decimal OrderAmount { get; set; }
        /// <summary>按款式分类汇总的返点说明</summary>
        public string StyleSummary { get; set; }
        public decimal RewardRate { get; set; }
        /// <summary>系统按分类规则算出的原始返现金额</summary>
        public decimal OriginalAmount { get; set; }
        /// <summary>0 未改金额，1 已人工修改</summary>
        public int IsAdjusted { get; set; }
        public decimal RewardAmount { get; set; }
        public int Status { get; set; }
        public DateTime? PayoutTime { get; set; }
        public string PayoutBy { get; set; }
        public string PayoutRemark { get; set; }
        public string SettleBatchNo { get; set; }
        public DateTime? SettleBeginTime { get; set; }
        public DateTime? SettleEndTime { get; set; }
        public DateTime BizTime { get; set; }
        public string Remark { get; set; }
    }

    public class JyReferralSummaryDto
    {
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int PendingCount { get; set; }
        public decimal PendingAmount { get; set; }
        public int PaidCount { get; set; }
        public decimal PaidAmount { get; set; }
        public int VoidCount { get; set; }
        public decimal VoidAmount { get; set; }
        /// <summary>本月新增待返现</summary>
        public decimal MonthPendingAmount { get; set; }
    }

    public class JyReferralPayoutDto
    {
        /// <summary>可选：勾选结算的台账 Id。与 BeginTime/EndTime 同时传时，以日期区间为准。</summary>
        public List<long> Ids { get; set; } = new();
        public string PayoutRemark { get; set; }
        /// <summary>结算区间开始（含当天）</summary>
        public DateTime? BeginTime { get; set; }
        /// <summary>结算区间结束（含当天）</summary>
        public DateTime? EndTime { get; set; }
        /// <summary>结算前允许逐单调整返现金额</summary>
        public List<JyReferralAdjustDto> Adjusts { get; set; } = new();
    }

    public class JyReferralAdjustDto
    {
        public long Id { get; set; }
        public decimal RewardAmount { get; set; }
        public string Remark { get; set; }
    }

    public class JyReferralAmountDto
    {
        public decimal RewardAmount { get; set; }
        public string Remark { get; set; }
    }
    /// <summary>按款式分类维护的返点规则</summary>
    public class JyReferralRateDto
    {
        public long Id { get; set; }
        public long StyleItemId { get; set; }
        public string StyleName { get; set; }
        public decimal Rate { get; set; }
        public int IsPreset { get; set; }
        public int Status { get; set; }
        public int SortNo { get; set; }
        /// <summary>该分类下在库件数，仅用于页面展示</summary>
        public decimal StockQty { get; set; }
        public string Remark { get; set; }
    }

    public class JyReferralRateEditDto
    {
        public long Id { get; set; }
        public List<JyReferralRateItemDto> Items { get; set; } = new();
    }

    public class JyReferralRateItemDto
    {
        public long StyleItemId { get; set; }
        public decimal Rate { get; set; }
        public string Remark { get; set; }
    }

    /// <summary>一次区间结算的结果</summary>
    public class JyReferralSettleResultDto
    {
        public string BatchNo { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>推荐关系网节点：当前客户 + 上行推荐人 + 下行被推荐人。</summary>
    public class JyReferralNodeDto
    {
        public long MemberId { get; set; }
        public string MemberNo { get; set; }
        public string MemberName { get; set; }
        public string Phone { get; set; }
        public string WechatNo { get; set; }
        public string LevelName { get; set; }
        public decimal TotalAmount { get; set; }
        public int OrderCount { get; set; }
        public bool IsFocus { get; set; }
        /// <summary>相对聚焦客户的层级：负数=上级，0=客户本人，正数=下级</summary>
        public int Depth { get; set; }
        public long? ParentMemberId { get; set; }
        /// <summary>当前客户已维护的推荐人（可能多位）</summary>
        public List<JyMemberReferrerDto> Referrers { get; set; } = new();
        /// <summary>该客户自身作为推荐人累计已产生的奖励总额</summary>
        public decimal RewardTotal { get; set; }
        public decimal RewardPending { get; set; }
        public List<JyReferralOrderBriefDto> Orders { get; set; } = new();
    }

    public class JyReferralOrderBriefDto
    {
        public long OrderId { get; set; }
        public string OrderNo { get; set; }
        public DateTime DealTime { get; set; }
        public decimal PayAmount { get; set; }
        public decimal PieceQty { get; set; }
    }

    public class JyReferralGraphDto
    {
        public JyReferralNodeDto Focus { get; set; }
        /// <summary>上行链路：由近及远</summary>
        public List<JyReferralNodeDto> UpChain { get; set; } = new();
        /// <summary>下行链路：直接与间接被推荐人</summary>
        public List<JyReferralNodeDto> DownList { get; set; } = new();
        /// <summary>当前客户的推荐奖励台账</summary>
        public List<JyReferralRewardDto> Rewards { get; set; } = new();
        public JyReferralSummaryDto Summary { get; set; }
    }
}

