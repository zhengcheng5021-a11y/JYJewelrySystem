using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/memberreview")]
    public class JyMemberReviewController : BaseController
    {
        private readonly JyMemberReviewService _service;

        public JyMemberReviewController(JyMemberReviewService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:member:query,jingyue:workitem:query")]
        public IActionResult List([FromQuery] JyReviewQueryDto query) => SUCCESS(_service.List(Actor, query));

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:member:query,jingyue:workitem:query")]
        public IActionResult Detail(long id) => SUCCESS(_service.Detail(Actor, id));

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:member:edit,jingyue:workitem:edit")]
        [Log(Title = "静悦多周期复盘", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyReviewEditDto dto) => SUCCESS(_service.Save(Actor, dto));

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:member:remove,jingyue:workitem:remove")]
        [Log(Title = "静悦多周期复盘", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id) => ToResponse(_service.Delete(Actor, id));
    }
}
