using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 单据编号规则。序号按租户和单据类型条件更新。
    /// </summary>
    [SugarTable("jy_bill_rule", "静悦单据编号规则")]
    public class JyBillRule : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        [SugarColumn(Length = 32)]
        public string BillType { get; set; }

        [SugarColumn(Length = 50)]
        public string BillName { get; set; }

        [SugarColumn(Length = 16)]
        public string Prefix { get; set; }

        [SugarColumn(Length = 16, DefaultValue = "yyyyMMdd")]
        public string DatePattern { get; set; }

        [SugarColumn(DefaultValue = "4")]
        public int SeqLength { get; set; } = 4;

        [SugarColumn(Length = 16, DefaultValue = "day")]
        public string ResetCycle { get; set; }

        [SugarColumn(Length = 16, DefaultValue = "")]
        public string CurrentKey { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int CurrentSeq { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int IsPreset { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
    }
}