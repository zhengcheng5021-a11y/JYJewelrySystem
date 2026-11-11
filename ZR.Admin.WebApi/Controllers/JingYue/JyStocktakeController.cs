using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/stocktake")]
    public class JyStocktakeController : BaseController
    {
        private readonly JyStocktakeService _service;
        private readonly JyBillRuleService _billRule;

        public JyStocktakeController(JyStocktakeService service, JyBillRuleService billRule)
        {
            _service = service;
            _billRule = billRule;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:list,jingyue:stocktake:query")]
        public IActionResult List([FromQuery] JyStocktakeQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:list,jingyue:stocktake:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpGet("dashboard")]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:list,jingyue:stocktake:query")]
        public IActionResult Dashboard()
        {
            return SUCCESS(_service.Dashboard(Actor));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:list,jingyue:stocktake:query,jingyue:stocktake:add,jingyue:stocktake:edit")]
        public IActionResult Options([FromQuery] int takeType, [FromQuery] string keyword = null)
        {
            return SUCCESS(_service.Options(Actor, takeType, keyword));
        }

        [HttpPost("initialize")]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:add")]
        [Log(Title = "新建静悦盘点快照", BusinessType = BusinessType.INSERT)]
        public IActionResult Initialize([FromBody] JyStocktakeInitDto dto)
        {
            return SUCCESS(_service.Initialize(Actor, dto, _billRule));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:add,jingyue:stocktake:edit")]
        [Log(Title = "静悦盘点单", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyStocktakeEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto, _billRule));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:remove")]
        [Log(Title = "静悦盘点单", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id, _billRule));
        }

        [HttpPost("{id:long}/confirm")]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:confirm")]
        [Log(Title = "确认静悦盘点", BusinessType = BusinessType.UPDATE)]
        public IActionResult Confirm(long id)
        {
            return ToResponse(_service.Confirm(Actor, id));
        }

        [HttpGet("report")]
        [ActionPermissionFilter(Permission = "jingyue:stocktake:report,jingyue:stocktake:list,jingyue:stocktake:query")]
        public IActionResult Report([FromQuery] JyStocktakeReportQueryDto query)
        {
            return SUCCESS(_service.Report(Actor, query));
        }
    }
}
