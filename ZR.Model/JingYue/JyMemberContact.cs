using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_member_contact", "静悦会员联系方式")]
    public class JyMemberContact : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long MemberId { get; set; }
        public int ContactType { get; set; }
        public int SlotNo { get; set; }
        [SugarColumn(Length = 64)]
        public string ContactValue { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
