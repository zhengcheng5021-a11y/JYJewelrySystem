using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>按一级款式分类使用的商品标签模板。</summary>
    [SugarTable("jy_print_template", "静悦标签打印模板")]
    public class JyPrintTemplate : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        [SugarColumn(Length = 100)]
        public string TemplateName { get; set; }

        [SugarColumn(Length = 100, IsNullable = true)]
        public string StyleItemName { get; set; }

        [SugarColumn(DefaultValue = "40")]
        public decimal WidthMm { get; set; }

        [SugarColumn(DefaultValue = "30")]
        public decimal HeightMm { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int ShowBarcode { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int ShowQrCode { get; set; }

        [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
        public string FieldsJson { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
