using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 销售定制单。Status 表示定金业务状态：0 草稿，1 已收定金，2 部分处理，3 已对冲完，8 已退清，9 作废。
    /// ArrivalStatus 独立表示货品是否到店：0 未到货，1 已到货。
    /// </summary>
    [SugarTable("jy_custom_order", "静悦销售定制")]
    public class JyCustomOrder : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(Length = 32)]
        public string CustomNo { get; set; }
        public DateTime CustomDate { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MemberId { get; set; }
        [SugarColumn(Length = 50, IsNullable = true)]
        public string CustomerName { get; set; }
        [SugarColumn(Length = 30, IsNullable = true)]
        public string CustomerPhone { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int CustomerSex { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? GoodsItemId { get; set; }
        [SugarColumn(Length = 32, IsNullable = true)]
        public string GoodsNo { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string Barcode { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? BindTime { get; set; }
        /// <summary>客户约定取货日期。</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? PickupDate { get; set; }
        /// <summary>0 未到货，1 已到货。</summary>
        [SugarColumn(DefaultValue = "0")]
        public int ArrivalStatus { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? ArrivalTime { get; set; }
        [SugarColumn(Length = 64, IsNullable = true)]
        public string ArrivalBy { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string ArrivalRemark { get; set; }
        [SugarColumn(Length = 100)]
        public string GoodsName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StyleItemId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string StyleItemName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MaterialItemId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string MaterialName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? MetalItemId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string MetalName { get; set; }
        [SugarColumn(IsNullable = true)]
        public decimal? Weight { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ExpectPrice { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal DepositAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal OffsetAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RefundAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal RemainAmount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StaffUserId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
