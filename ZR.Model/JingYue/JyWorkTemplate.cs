using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_work_template", "静悦目标模板")]
    public class JyWorkTemplate : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(Length = 100)]
        public string TemplateName { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Description { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
