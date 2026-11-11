using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/inbound")]
    public class JyInboundController : BaseController
    {
        private readonly JyInboundService _service;

        public JyInboundController(JyInboundService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:inbound:list,jingyue:inbound:query")]
        public IActionResult List([FromQuery] JyInboundQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:inbound:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpGet("dashboard")]
        [ActionPermissionFilter(Permission = "jingyue:inbound:list")]
        public IActionResult Dashboard()
        {
            return SUCCESS(_service.Dashboard(Actor));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:inbound:query,jingyue:inbound:add,jingyue:inbound:edit")]
        public IActionResult Options()
        {
            return SUCCESS(_service.Options(Actor));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:inbound:add,jingyue:inbound:edit")]
        [Log(Title = "静悦采购入库", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyInboundEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:inbound:remove")]
        [Log(Title = "静悦采购入库", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }

        [HttpPost("{id:long}/confirm")]
        [ActionPermissionFilter(Permission = "jingyue:inbound:confirm")]
        [Log(Title = "静悦确认入库", BusinessType = BusinessType.UPDATE)]
        public IActionResult Confirm(long id)
        {
            return ToResponse(_service.Confirm(Actor, id));
        }
    }
}