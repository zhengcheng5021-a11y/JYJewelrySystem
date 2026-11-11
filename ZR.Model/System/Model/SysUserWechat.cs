namespace ZR.Model.System
{
    /// <summary>
    /// 用户维护的私人微信账号。客户档案只能从这里选择，不直接手写微信号。
    /// </summary>
    [SugarTable("sys_user_wechat", "用户微信账号")]
    [Tenant("0")]
    [SkipMigration("结构由人工执行 SQL 维护")]
    public class SysUserWechat : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        public long UserId { get; set; }

        [SugarColumn(Length = 64, ColumnDescription = "微信号")]
        public string WechatNo { get; set; }

        [SugarColumn(DefaultValue = "0", ColumnDescription = "是否默认微信")]
        public int IsPrimary { get; set; }

        [SugarColumn(DefaultValue = "0", ColumnDescription = "0 正常，1 停用")]
        public int Status { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
