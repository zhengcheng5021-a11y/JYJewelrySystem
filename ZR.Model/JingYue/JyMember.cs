using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_member", "静悦会员档案")]
    public class JyMember : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string MemberNo { get; set; }
        [SugarColumn(Length = 50)]
        public string MemberName { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Sex { get; set; }
        [SugarColumn(Length = 30, IsNullable = true)]
        public string Phone { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? SolarBirthday { get; set; }
        [SugarColumn(Length = 30, IsNullable = true)]
        public string LunarBirthday { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int BirthCalendar { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? BirthYear { get; set; }
        [SugarColumn(Length = 50, IsNullable = true)]
        public string Occupation { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Hobby { get; set; }
        [SugarColumn(Length = 20, IsNullable = true)]
        public string WuXing { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string RecommendDirection { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? LevelId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal PointsBalance { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal YearPoints { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? LevelChangedTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int FollowStatus { get; set; }
        /// <summary>本人微信号，可被任意搜索命中</summary>
        [SugarColumn(Length = 64, IsNullable = true)]
        public string WechatNo { get; set; }
        /// <summary>推荐人（介绍人）微信号，建档时录入，用于绑定推荐关系</summary>
        [SugarColumn(Length = 64, IsNullable = true)]
        public string ReferrerWechatNo { get; set; }

        /// <summary>推荐人会员 Id；绑定后不允许修改</summary>
        [SugarColumn(IsNullable = true)]
        public long? ReferrerMemberId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SourcePublishId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StaffUserId { get; set; }
        /// <summary>客户加的哪一位员工的微信；来源 sys_user_wechat</summary>
        [SugarColumn(IsNullable = true)]
        public long? MemberWechatId { get; set; }
        /// <summary>建档时的微信号快照，员工微信改名/停用后仍保留历史</summary>
        [SugarColumn(Length = 64, IsNullable = true)]
        public string MemberWechatNo { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal YearAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TotalAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int OrderCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal AvgTicket { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? UsualStyleItemId { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? LastConsumeTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? CacheTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}

