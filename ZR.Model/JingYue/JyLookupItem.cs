using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 珠宝业务字典项。两层时 ParentId 指向大类，大类本身 ParentId 为空。
    /// </summary>
    [SugarTable("jy_lookup_item", "静悦业务字典项")]
    public class JyLookupItem : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        public long TypeId { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? ParentId { get; set; }

        [SugarColumn(Length = 32)]
        public string ItemCode { get; set; }

        [SugarColumn(Length = 100)]
        public string ItemName { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int IsPreset { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}