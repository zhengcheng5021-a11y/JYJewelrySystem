using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/activity")]
    public class JyActivityController : BaseController
    {
        private readonly JyActivityService _service;

        public JyActivityController(JyActivityService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:activity:list,jingyue:activity:query")]
        public IActionResult List([FromQuery] JyActivityQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:activity:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:activity:query,jingyue:activity:add,jingyue:activity:edit")]
        public IActionResult Options()
        {
            return SUCCESS(_service.Options(Actor));
        }

        [HttpGet("dashboard")]
        [ActionPermissionFilter(Permission = "jingyue:activity:list")]
        public IActionResult Dashboard()
        {
            return SUCCESS(_service.Dashboard(Actor));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:activity:add,jingyue:activity:edit")]
        [Log(Title = "静悦营销活动", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyActivityEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpPost("status")]
        [ActionPermissionFilter(Permission = "jingyue:activity:status")]
        [Log(Title = "静悦营销活动状态", BusinessType = BusinessType.UPDATE)]
        public IActionResult SetStatus([FromBody] JyActivityStatusDto dto)
        {
            return ToResponse(_service.SetStatus(Actor, dto));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:activity:remove")]
        [Log(Title = "静悦营销活动", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }
    }
}
