using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 供应商档案。StoreId 对应 sys_dept.DeptId，空表示租户通用。
    /// </summary>
    [SugarTable("jy_supplier", "静悦供应商")]
    public class JySupplier : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }

        [SugarColumn(Length = 32)]
        public string SupplierCode { get; set; }

        [SugarColumn(Length = 100)]
        public string SupplierName { get; set; }

        [SugarColumn(Length = 50, IsNullable = true)]
        public string ContactName { get; set; }

        [SugarColumn(Length = 30, IsNullable = true)]
        public string ContactPhone { get; set; }

        [SugarColumn(Length = 200, IsNullable = true)]
        public string Address { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}