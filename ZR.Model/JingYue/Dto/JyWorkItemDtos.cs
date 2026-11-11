using System.Text.Json.Serialization;

namespace ZR.Model.JingYue.Dto
{
    public class JyWorkItemQueryDto : PagerInfo
    {
        public string Keyword { get; set; }
        /// <summary>all/today/week/overdue/done/high</summary>
        public string View { get; set; }
        public int? Status { get; set; }
        public int? Priority { get; set; }
        public int? ItemKind { get; set; }
        public long? OwnerUserId { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyWorkScopeEditDto
    {
        public int ScopeType { get; set; }
        public long ScopeId { get; set; }
        /// <summary>保存模板时保留范围名称，避免以后改名导致历史模板失真。</summary>
        public string SnapshotName { get; set; }
    }

    public class JyWorkItemEditDto
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public long? StoreId { get; set; }
        public string Title { get; set; }
        public int ItemKind { get; set; } = 1;
        public int PeriodType { get; set; } = 3;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DueTime { get; set; }
        public DateTime? FinishDate { get; set; }
        /// <summary>四象限：4 重要且紧急、3 重要不紧急、2 不重要但紧急、1 不重要不紧急。</summary>
        public int Priority { get; set; } = 2;
        public long? OwnerUserId { get; set; }
        public string OwnerName { get; set; }
        public int ProgressMode { get; set; } = 1;
        public string MetricCode { get; set; }
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal CurrentAuxValue { get; set; }
        public bool? AutoComplete { get; set; }
        public DateTime? ReminderTime { get; set; }
        public int SortNo { get; set; }
        public string Remark { get; set; }
        /// <summary>0 普通记事，1 有日期区间。</summary>
        public int HasDateRange { get; set; }
        /// <summary>主事项是否关联业绩。</summary>
        public int IsPerformanceRelated { get; set; }
        /// <summary>子目标：1 金额，2 件数。</summary>
        public int TargetMode { get; set; } = 1;
        public long? CategoryId { get; set; }
        public List<JyWorkScopeEditDto> Scopes { get; set; } = new();
        public bool SaveAsTemplate { get; set; }
        public string TemplateName { get; set; }
    }

    public class JyWorkProgressEditDto
    {
        public decimal CurrentValue { get; set; }
        public decimal CurrentAuxValue { get; set; }
        public string Remark { get; set; }
    }

    public class JyWorkItemDto
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public long? StoreId { get; set; }
        public string ItemNo { get; set; }
        public string Title { get; set; }
        public int ItemKind { get; set; }
        public string ItemKindName { get; set; }
        public int PeriodType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? DueTime { get; set; }
        public DateTime? FinishDate { get; set; }
        public int Priority { get; set; }
        public string PriorityName { get; set; }
        public long? OwnerUserId { get; set; }
        public string OwnerName { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public int ProgressMode { get; set; }
        public string MetricCode { get; set; }
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal CurrentAuxValue { get; set; }
        public decimal ProgressPercent { get; set; }
        public int AutoComplete { get; set; }
        public DateTime? ReminderTime { get; set; }
        public DateTime? FinishTime { get; set; }
        public DateTime? SettledTime { get; set; }
        public int SortNo { get; set; }
        public long? TemplateId { get; set; }
        public string Remark { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateBy { get; set; }
        public bool IsOverdue { get; set; }
        public int ChildCount { get; set; }
        public int DoneChildCount { get; set; }
        public int HasDateRange { get; set; }
        public int IsPerformanceRelated { get; set; }
        public int TargetMode { get; set; }
        public string TargetModeName { get; set; }
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<JyWorkScopeDto> Scopes { get; set; } = new();
        public List<JyWorkItemDto> Children { get; set; } = new();
    }

    public class JyWorkScopeDto
    {
        public int ScopeType { get; set; }
        public long ScopeId { get; set; }
        public string SnapshotName { get; set; }
    }

    public class JyWorkStatsDto
    {
        /// <summary>主事项数量（含独立待办）。</summary>
        public int GroupCount { get; set; }
        /// <summary>可执行事项数量；主事项有子目标时只统计子目标。</summary>
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int TodayCount { get; set; }
        public int OverdueCount { get; set; }
        public int DoneThisWeek { get; set; }
        public int HighPriorityCount { get; set; }
        public int ImportantUrgentCount { get; set; }
        public int ImportantNotUrgentCount { get; set; }
        public int NotImportantUrgentCount { get; set; }
        public int NotImportantNotUrgentCount { get; set; }
        public decimal CompletionRate { get; set; }
        public string ServerDate { get; set; }
    }

    public class JyWorkProgressLogDto
    {
        public long Id { get; set; }
        public long WorkItemId { get; set; }
        public string MetricCode { get; set; }
        public decimal BeforeValue { get; set; }
        public decimal ChangeValue { get; set; }
        public decimal AfterValue { get; set; }
        public decimal ProgressPercent { get; set; }
        public string SourceType { get; set; }
        public long? SourceId { get; set; }
        public DateTime? BusinessDate { get; set; }
        public string Remark { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyMemberFollowQueryDto : PagerInfo
    {
        public long? MemberId { get; set; }
        public long? WorkItemId { get; set; }
        public string Keyword { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class JyMemberFollowEditDto
    {
        public long MemberId { get; set; }
        public int FollowType { get; set; } = 1;
        /// <summary>数据字典「跟进方式」词条 ID。</summary>
        public long? LookupItemId { get; set; }
        public string Content { get; set; }
        public string Result { get; set; }
        public DateTime? NextFollowTime { get; set; }
        public long? WorkItemId { get; set; }
    }

    public class JyMemberFollowDto
    {
        public long Id { get; set; }
        public long MemberId { get; set; }
        public string MemberNo { get; set; }
        public string MemberName { get; set; }
        public string Phone { get; set; }
        public DateTime FollowTime { get; set; }
        public int FollowType { get; set; }
        public long? LookupItemId { get; set; }
        public string FollowTypeName { get; set; }
        public string Content { get; set; }
        public string Result { get; set; }
        public DateTime? NextFollowTime { get; set; }
        public long? StaffUserId { get; set; }
        public string StaffName { get; set; }
        public long? WorkItemId { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class JyWorkTemplateDto
    {
        public long Id { get; set; }
        public string TemplateName { get; set; }
        public string Description { get; set; }
        public int Status { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreateTime { get; set; }
        public List<JyWorkTemplateItemDto> Items { get; set; } = new();
    }

    public class JyWorkTemplateItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public int ItemKind { get; set; }
        public int PeriodType { get; set; }
        public int Priority { get; set; }
        public string MetricCode { get; set; }
        public decimal TargetValue { get; set; }
        public string ScopeJson { get; set; }
        public string Remark { get; set; }
        public int TargetMode { get; set; } = 1;
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }
    }

    public class JyWorkTemplateEditDto
    {
        public long Id { get; set; }
        public string TemplateName { get; set; }
        public string Description { get; set; }
        public int Status { get; set; }
        public List<JyWorkTemplateItemDto> Items { get; set; } = new();
    }

    public class JyWorkTemplateApplyDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? PeriodType { get; set; }
        public string Title { get; set; }
        public int? Priority { get; set; }
        public long? OwnerUserId { get; set; }
        public string OwnerName { get; set; }
        public int HasDateRange { get; set; } = 1;
        public int IsPerformanceRelated { get; set; }
    }

    public class JyWorkTemplateApplyResultDto
    {
        public long ParentId { get; set; }
        public string Title { get; set; }
        public int ItemCount { get; set; }
    }

    public class JyWorkEvidenceDto
    {
        public JyWorkItemDto Item { get; set; }
        public List<JyWorkProgressLogDto> Logs { get; set; } = new();
        public List<JyMemberFollowDto> Follows { get; set; } = new();
    }

    public class JyWorkOptionsDto
    {
        public List<JyWorkOwnerOptionDto> Owners { get; set; } = new();
        public List<JyLookupOptionDto> Categories { get; set; } = new();
        public List<JyWorkMemberOptionDto> Members { get; set; } = new();
        public List<JyWorkQuickActionDto> QuickActions { get; set; } = new();
    }

    public class JyWorkOwnerOptionDto
    {
        public long UserId { get; set; }
        public string NickName { get; set; }
        public string UserName { get; set; }
    }

    public class JyWorkMemberOptionDto
    {
        public long Id { get; set; }
        public string MemberNo { get; set; }
        public string MemberName { get; set; }
        public string Phone { get; set; }
    }

    public class JyWorkQuickActionDto
    {
        public long Id { get; set; }
        public string ActionName { get; set; }
        public string TitleTemplate { get; set; }
        public int SortNo { get; set; }
        public int Status { get; set; }
        public int IsPreset { get; set; }
        public string Remark { get; set; }
    }

    public class JyWorkQuickActionEditDto
    {
        public long Id { get; set; }
        public string ActionName { get; set; }
        public string TitleTemplate { get; set; }
        public int SortNo { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }
}

