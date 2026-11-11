using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>商品返厂单。Status：0 草稿，1 已返厂确认，9 作废。</summary>
    [SugarTable("jy_factory_return", "静悦商品返厂")]
    public class JyFactoryReturn : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string ReturnNo { get; set; }
        public DateTime ReturnDate { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SupplierId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string SupplierName { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TotalQty { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TotalCost { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}