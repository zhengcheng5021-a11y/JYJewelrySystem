using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 一次取号记录。只新增，不修改，不删除。
    /// </summary>
    [SugarTable("jy_bill_issue", "静悦单据取号记录")]
    public class JyBillIssue : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        public long RuleId { get; set; }

        [SugarColumn(Length = 32)]
        public string BillType { get; set; }

        [SugarColumn(Length = 32)]
        public string BillNo { get; set; }

        [SugarColumn(Length = 16)]
        public string CycleKey { get; set; }

        public int SeqNo { get; set; }

        [SugarColumn(Length = 64, IsNullable = true)]
        public string BizTable { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? BizId { get; set; }
    }
}