using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_activity_member", "静悦活动参与记录")]
    public class JyActivityMember
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long ActivityId { get; set; }
        public long MemberId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SaleOrderId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal BenefitAmount { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
