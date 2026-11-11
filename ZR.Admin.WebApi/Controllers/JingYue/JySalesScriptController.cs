using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/script")]
    public class JySalesScriptController : BaseController
    {
        private readonly JySalesScriptService _service;

        public JySalesScriptController(JySalesScriptService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:script:list")]
        public IActionResult List([FromQuery] JyScriptQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:script:add,jingyue:script:edit")]
        [Log(Title = "静悦销售话术", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyScriptEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("{id}")]
        [ActionPermissionFilter(Permission = "jingyue:script:remove")]
        [Log(Title = "静悦销售话术", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return SUCCESS(_service.Delete(Actor, id));
        }
    }
}
