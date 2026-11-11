using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    /// <summary>客户跟进记录。</summary>
    [Route("jingyue/memberfollow")]
    public class JyMemberFollowController : BaseController
    {
        private readonly JyMemberFollowService _service;

        public JyMemberFollowController(JyMemberFollowService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query,jingyue:workitem:follow")]
        public IActionResult List([FromQuery] JyMemberFollowQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:workitem:follow")]
        [Log(Title = "静悦客户跟进", BusinessType = BusinessType.INSERT)]
        public IActionResult Save([FromBody] JyMemberFollowEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }
    }
}
