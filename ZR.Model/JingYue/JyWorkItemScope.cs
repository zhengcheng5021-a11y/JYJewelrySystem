using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>目标统计范围。ScopeType：1 指定会员，2 款式分类（只允许大类）。</summary>
    [SugarTable("jy_work_item_scope", "静悦目标统计范围")]
    public class JyWorkItemScope
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long WorkItemId { get; set; }
        public int ScopeType { get; set; }
        public long ScopeId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string SnapshotName { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
