using Microsoft.AspNetCore.Mvc;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    /// <summary>静悦业务统一服务器时钟。前端仅用于显示和筛选，业务落库仍以后端时间为准。</summary>
    [Route("jingyue/time")]
    public class JyTimeController : BaseController
    {
        [HttpGet("now")]
        public IActionResult Now()
        {
            var now = DateTime.Now;
            var offset = TimeZoneInfo.Local.GetUtcOffset(now);
            var unspecified = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            return SUCCESS(new
            {
                now = now.ToString("yyyy-MM-dd HH:mm:ss"),
                date = now.ToString("yyyy-MM-dd"),
                timestamp = new DateTimeOffset(unspecified, offset).ToUnixTimeMilliseconds(),
                utcOffsetMinutes = (int)offset.TotalMinutes,
                timeZone = TimeZoneInfo.Local.DisplayName
            });
        }
    }
}
