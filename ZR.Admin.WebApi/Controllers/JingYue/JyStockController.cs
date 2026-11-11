using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/stock")]
    public class JyStockController : BaseController
    {
        private readonly JyStockService _service;

        public JyStockController(JyStockService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:stock:list,jingyue:stock:query")]
        public IActionResult List([FromQuery] JyStockQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("summary")]
        [ActionPermissionFilter(Permission = "jingyue:stock:list,jingyue:stock:query")]
        public IActionResult Summary()
        {
            return SUCCESS(_service.Summary(Actor));
        }

        [HttpGet("ledger")]
        [ActionPermissionFilter(Permission = "jingyue:stock:query")]
        public IActionResult Ledger([FromQuery] JyStockLedgerQueryDto query)
        {
            return SUCCESS(_service.Ledger(Actor, query));
        }
    }
}