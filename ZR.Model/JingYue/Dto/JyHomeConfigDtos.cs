namespace ZR.Model.JingYue.Dto
{
    public class JyBirthdayReminderDto
    {
        public long MemberId { get; set; }
        public string MemberNo { get; set; }
        public string MemberName { get; set; }
        public string Phone { get; set; }
        public string LevelName { get; set; }
        public string BirthdayDate { get; set; }
        public int DaysUntil { get; set; }
        public string CalendarText { get; set; }
        public decimal TotalAmount { get; set; }
        public string UsualStyleName { get; set; }
    }

    public class JyBirthdayReminderResultDto
    {
        public int WithinDays { get; set; } = 7;
        public int TodayCount { get; set; }
        public int UpcomingCount { get; set; }
        public List<JyBirthdayReminderDto> Rows { get; set; } = new();
    }

    public class JyStockAlertRuleDto
    {
        public long Id { get; set; }
        public long StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public decimal MinQty { get; set; }
        public decimal CurrentQty { get; set; }
        public int AlertLevel { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public DateTime? UpdateTime { get; set; }
    }

    public class JyStockAlertEditDto
    {
        public long StyleItemId { get; set; }
        public decimal MinQty { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }

    public class JyStockAlertResultDto
    {
        public int AlertCount { get; set; }
        public int LowCount { get; set; }
        public int NearCount { get; set; }
        public decimal TotalQty { get; set; }
        public decimal UnclassifiedQty { get; set; }
        public List<JyStockAlertRuleDto> Rows { get; set; } = new();
    }

    public class JyPrintFieldDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string CustomText { get; set; }
        public int Enabled { get; set; } = 1;
        public int SortNo { get; set; }
        public string Value { get; set; }
        public string Type { get; set; } = "text";
        public string FieldKey { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int FontSize { get; set; } = 16;
        public bool Bold { get; set; }
        public int Rotation { get; set; }
    }

    public class JyPrintTemplateDto
    {
        public long Id { get; set; }
        public string TemplateName { get; set; }
        public string StyleItemName { get; set; }
        public decimal WidthMm { get; set; }
        public decimal HeightMm { get; set; }
        public decimal GapMm { get; set; } = 2;
        public decimal OffsetMm { get; set; } = 4;
        public int Copies { get; set; } = 1;
        public string Port { get; set; } = "USB,";
        public int Speed { get; set; } = 4;
        public int Density { get; set; } = 8;
        public int ShowBarcode { get; set; }
        public int ShowQrCode { get; set; }
        public List<JyPrintFieldDto> Fields { get; set; } = new();
        public string Remark { get; set; }
        public int Status { get; set; }
        public DateTime? UpdateTime { get; set; }
    }

    public class JyPrintTemplateEditDto
    {
        public long Id { get; set; }
        public string TemplateName { get; set; }
        public decimal WidthMm { get; set; } = 40;
        public decimal HeightMm { get; set; } = 30;
        public decimal GapMm { get; set; } = 2;
        public decimal OffsetMm { get; set; } = 4;
        public int Copies { get; set; } = 1;
        public string Port { get; set; } = "USB,";
        public int Speed { get; set; } = 4;
        public int Density { get; set; } = 8;
        public int ShowBarcode { get; set; }
        public int ShowQrCode { get; set; }
        public List<JyPrintFieldDto> Fields { get; set; } = new();
        public string Remark { get; set; }
        public int Status { get; set; }
    }

    public class JyPrintTemplateBindingDto
    {
        public long Id { get; set; }
        public long StyleItemId { get; set; }
        public string StyleItemName { get; set; }
        public long TemplateId { get; set; }
        public string TemplateName { get; set; }
    }

    public class JyPrintTemplateBindingEditDto
    {
        public long StyleItemId { get; set; }
        public long TemplateId { get; set; }
    }

    public class JyPrintTemplateOptionsDto
    {
        public List<JyPrintTemplateDto> Templates { get; set; } = new();
        public List<JyPrintTemplateBindingDto> Bindings { get; set; } = new();
        public List<JyPrintFieldDto> AvailableFields { get; set; } = new();
    }

    public class JyPrintRenderRequestDto
    {
        public long GoodsItemId { get; set; }
        public long InboundLineId { get; set; }
        public long TemplateId { get; set; }
    }

    public class JyPrintAgentLabelDto
    {
        public string Name { get; set; }
        public string Sku { get; set; }
        public string Price { get; set; }
        public string Barcode { get; set; }
        public decimal WidthMm { get; set; }
        public decimal HeightMm { get; set; }
        public decimal GapMm { get; set; }
        public decimal OffsetMm { get; set; }
        public int Copies { get; set; }
        public string Port { get; set; }
        public int Speed { get; set; }
        public int Density { get; set; }
        public List<JyPrintFieldDto> Fields { get; set; } = new();
        public List<JyPrintFieldDto> Elements { get; set; } = new();
        public Dictionary<string, string> Data { get; set; } = new();
        public int ShowBarcode { get; set; }
        public int ShowQrCode { get; set; }
    }
}
