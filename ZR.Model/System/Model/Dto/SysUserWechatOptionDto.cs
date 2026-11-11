namespace ZR.Model.System.Dto
{
    /// <summary>客户档案可选的员工微信，包含员工归属与当前是否默认</summary>
    public class SysUserWechatOptionDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string NickName { get; set; }
        public string WechatNo { get; set; }
        public int IsPrimary { get; set; }
    }
}
