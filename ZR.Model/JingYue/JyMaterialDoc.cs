using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>物料单据。Direction：1 入库，2 出库；Status：0 草稿，1 已确认，9 作废。</summary>
    [SugarTable("jy_material_doc", "静悦物料单据")]
    public class JyMaterialDoc : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string DocNo { get; set; }
        public int Direction { get; set; }
        [SugarColumn(Length = 32)]
        public string BizType { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SupplierId { get; set; }
        public DateTime DocTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TotalWeight { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TotalAmount { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? OperatorUserId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ConfirmUserId { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? ConfirmTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
