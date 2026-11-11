using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/bill")]
    public class JyBillRuleController : BaseController
    {
        private readonly JyBillRuleService _service;

        public JyBillRuleController(JyBillRuleService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName());

        [HttpGet("rules")]
        [ActionPermissionFilter(Permission = "jingyue:bill:list")]
        public IActionResult Rules()
        {
            return SUCCESS(_service.List(Actor));
        }

        [HttpPost("rule")]
        [ActionPermissionFilter(Permission = "jingyue:bill:add,jingyue:bill:edit")]
        [Log(Title = "静悦单据编号", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyBillRuleEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("rule/{id}")]
        [ActionPermissionFilter(Permission = "jingyue:bill:remove")]
        [Log(Title = "静悦单据编号", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }

        [HttpGet("preview")]
        [ActionPermissionFilter(Permission = "jingyue:bill:query,jingyue:bill:issue")]
        public IActionResult Preview([FromQuery] string billType)
        {
            return SUCCESS(_service.Preview(Actor, billType));
        }

        [HttpPost("issue")]
        [ActionPermissionFilter(Permission = "jingyue:bill:issue")]
        [Log(Title = "静悦单据取号", BusinessType = BusinessType.INSERT)]
        public IActionResult Issue([FromBody] JyBillIssueRequestDto dto)
        {
            return SUCCESS(_service.Issue(Actor, dto));
        }

        [HttpGet("issues")]
        [ActionPermissionFilter(Permission = "jingyue:bill:list,jingyue:bill:query")]
        public IActionResult Issues([FromQuery] JyBillIssueQueryDto query)
        {
            return SUCCESS(_service.Issues(Actor, query));
        }
    }
}
