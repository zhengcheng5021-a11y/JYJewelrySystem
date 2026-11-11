using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_member_level", "静悦会员等级")]
    public class JyMemberLevel : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(Length = 32)]
        public string LevelCode { get; set; }
        [SugarColumn(Length = 50)]
        public string LevelName { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal MinPoints { get; set; }
        [SugarColumn(IsNullable = true)]
        public decimal? MaxPoints { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public decimal DiscountRate { get; set; }
        public int SortNo { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int KeepOnYearClear { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
