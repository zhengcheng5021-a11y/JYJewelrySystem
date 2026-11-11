using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/material")]
    public class JyMaterialController : BaseController
    {
        private readonly JyMaterialService _service;
        private readonly JyLookupService _lookup;
        private readonly JyBillRuleService _billRule;

        public JyMaterialController(JyMaterialService service, JyLookupService lookup, JyBillRuleService billRule)
        {
            _service = service;
            _lookup = lookup;
            _billRule = billRule;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:material:list,jingyue:material:query")]
        public IActionResult List([FromQuery] JyMaterialQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("{id}")]
        [ActionPermissionFilter(Permission = "jingyue:material:list,jingyue:material:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpGet("summary")]
        [ActionPermissionFilter(Permission = "jingyue:material:list,jingyue:material:query")]
        public IActionResult Summary()
        {
            return SUCCESS(_service.Summary(Actor));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:material:list,jingyue:material:query,jingyue:materialdoc:list,jingyue:materialdoc:query,jingyue:materialdoc:add,jingyue:materialdoc:edit")]
        public IActionResult Options()
        {
            return SUCCESS(_service.Options(Actor, _lookup));
        }

        [HttpGet("available")]
        [ActionPermissionFilter(Permission = "jingyue:material:list,jingyue:material:query,jingyue:materialdoc:list,jingyue:materialdoc:query,jingyue:materialdoc:add,jingyue:materialdoc:edit")]
        public IActionResult Available()
        {
            return SUCCESS(_service.AvailableOptions(Actor));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:material:add,jingyue:material:edit")]
        [Log(Title = "静悦物料档案", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyMaterialEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("{id}")]
        [ActionPermissionFilter(Permission = "jingyue:material:remove")]
        [Log(Title = "静悦物料档案", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }

        [HttpGet("doc/list")]
        [ActionPermissionFilter(Permission = "jingyue:materialdoc:list,jingyue:materialdoc:query")]
        public IActionResult DocList([FromQuery] JyMaterialDocQueryDto query)
        {
            return SUCCESS(_service.ListDocs(Actor, query));
        }

        [HttpGet("doc/{id}")]
        [ActionPermissionFilter(Permission = "jingyue:materialdoc:list,jingyue:materialdoc:query")]
        public IActionResult DocDetail(long id)
        {
            return SUCCESS(_service.DetailDoc(Actor, id));
        }

        [HttpGet("doc/dashboard")]
        [ActionPermissionFilter(Permission = "jingyue:materialdoc:list,jingyue:materialdoc:query")]
        public IActionResult DocDashboard()
        {
            return SUCCESS(_service.Dashboard(Actor));
        }

        [HttpPost("doc")]
        [ActionPermissionFilter(Permission = "jingyue:materialdoc:add,jingyue:materialdoc:edit")]
        [Log(Title = "静悦物料单据", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveDoc([FromBody] JyMaterialDocEditDto dto)
        {
            return SUCCESS(_service.SaveDoc(Actor, dto, _billRule));
        }

        [HttpDelete("doc/{id}")]
        [ActionPermissionFilter(Permission = "jingyue:materialdoc:remove")]
        [Log(Title = "静悦物料单据", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteDoc(long id)
        {
            return ToResponse(_service.DeleteDoc(Actor, id));
        }

        [HttpPost("doc/{id}/confirm")]
        [ActionPermissionFilter(Permission = "jingyue:materialdoc:confirm")]
        [Log(Title = "确认物料单据", BusinessType = BusinessType.UPDATE)]
        public IActionResult ConfirmDoc(long id)
        {
            return ToResponse(_service.ConfirmDoc(Actor, id));
        }

        [HttpGet("ledger")]
        [ActionPermissionFilter(Permission = "jingyue:materialledger:list,jingyue:materialledger:query")]
        public IActionResult Ledger([FromQuery] JyMaterialLedgerQueryDto query)
        {
            return SUCCESS(_service.Ledger(Actor, query));
        }
    }
}
