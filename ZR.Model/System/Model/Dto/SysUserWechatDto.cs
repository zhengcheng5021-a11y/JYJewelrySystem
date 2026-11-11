namespace ZR.Model.System.Dto
{
    public class SysUserWechatDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string WechatNo { get; set; }
        public int IsPrimary { get; set; }
        public int Status { get; set; }
    }
}
