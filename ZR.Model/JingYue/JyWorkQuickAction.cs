using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_work_quick_action", "静悦待办快捷事项")]
    public class JyWorkQuickAction : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(Length = 100)]
        public string ActionName { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string TitleTemplate { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int IsPreset { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
