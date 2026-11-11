using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 珠宝业务字典类型。LevelLimit 只允许 1 或 2。
    /// </summary>
    [SugarTable("jy_lookup_type", "静悦业务字典类型")]
    public class JyLookupType : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        [SugarColumn(Length = 32)]
        public string TypeCode { get; set; }

        [SugarColumn(Length = 50)]
        public string TypeName { get; set; }

        /// <summary>
        /// 层级上限：1 只有一层，2 为大类加子级。
        /// </summary>
        [SugarColumn(DefaultValue = "1")]
        public int LevelLimit { get; set; } = 1;

        /// <summary>
        /// 预置类型只允许停用，不允许删除，也不允许改编码和层级。
        /// </summary>
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