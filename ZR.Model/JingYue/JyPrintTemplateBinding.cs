using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>一级款式分类与打印模板的绑定，每个分类只启用一个默认模板。</summary>
    [SugarTable("jy_print_template_binding", "静悦标签模板分类绑定")]
    public class JyPrintTemplateBinding : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        public long StyleItemId { get; set; }

        [SugarColumn(Length = 100)]
        public string StyleItemName { get; set; }

        public long TemplateId { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
