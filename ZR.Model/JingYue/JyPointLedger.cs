using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_point_ledger", "静悦积分流水")]
    public class JyPointLedger
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long MemberId { get; set; }
        public int ChangeType { get; set; }
        public decimal ChangePoints { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal YearPointsAfter { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SaleOrderId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ReturnId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? OperatorId { get; set; }
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Reason { get; set; }
        public DateTime BizTime { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
    }
}
