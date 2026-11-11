using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>客户跟进记录。FollowTime 由服务器写入，不接受客户端时间。</summary>
    [SugarTable("jy_member_follow", "静悦客户跟进记录")]
    public class JyMemberFollow : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long MemberId { get; set; }
        public DateTime FollowTime { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int FollowType { get; set; }
        /// <summary>Dict item for follow-up method.</summary>
        [SugarColumn(IsNullable = true)]
        public long? LookupItemId { get; set; }
        /// <summary>Snapshot of the follow-up method name.</summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string FollowTypeName { get; set; }
        [SugarColumn(Length = 1000, IsNullable = true)]
        public string Content { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Result { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? NextFollowTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StaffUserId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? WorkItemId { get; set; }
        [SugarColumn(Length = 32, IsNullable = true)]
        public string SourceType { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SourceId { get; set; }
    }
}
