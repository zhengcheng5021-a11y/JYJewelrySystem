using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 门店客流手工登记。成交单数和成交金额不落库，查询时按销售单实时聚合。
    /// </summary>
    [SugarTable("jy_store_traffic", "静悦门店客流")]
    public class JyStoreTraffic : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        public DateTime TrafficDate { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int VisitorCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int WechatCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int LostStrollCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int LostPriceCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int LostStyleCount { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int LostConsiderCount { get; set; }
    }
}
