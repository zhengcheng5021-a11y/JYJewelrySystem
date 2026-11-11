using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/report")]
    public class JyReportController : BaseController
    {
        private readonly JyReportService _service;

        public JyReportController(JyReportService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("sales-metrics")]
        [ActionPermissionFilter(Permission = "jingyue:report:query,jingyue:sale:query")]
        public IActionResult SalesMetrics([FromQuery] JySalesMetricQueryDto query)
        {
            return SUCCESS(_service.SalesMetrics(Actor, query));
        }

        [HttpGet("store-traffic")]
        [ActionPermissionFilter(Permission = "jingyue:traffic:query,jingyue:report:query")]
        public IActionResult StoreTraffic([FromQuery] JyTrafficQueryDto query)
        {
            return SUCCESS(_service.StoreTraffic(Actor, query));
        }

        [HttpPost("store-traffic")]
        [ActionPermissionFilter(Permission = "jingyue:traffic:add,jingyue:traffic:edit")]
        [Log(Title = "静悦门店客流", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveTraffic([FromBody] JyTrafficSaveDto dto)
        {
            return SUCCESS(_service.SaveTraffic(Actor, dto));
        }

        [HttpGet("style-performance")]
        [ActionPermissionFilter(Permission = "jingyue:report:style,jingyue:report:query")]
        public IActionResult StylePerformance([FromQuery] JyStylePerformanceQueryDto query)
        {
            return SUCCESS(_service.StylePerformance(Actor, query));
        }

        [HttpGet("sales-ledger")]
        [ActionPermissionFilter(Permission = "jingyue:report:sales,jingyue:report:query,jingyue:sale:query")]
        public IActionResult SalesLedger([FromQuery] JySalesLedgerQueryDto query)
        {
            return SUCCESS(_service.SalesLedger(Actor, query));
        }

        [HttpGet("goals")]
        [ActionPermissionFilter(Permission = "jingyue:goal:query")]
        public IActionResult SalesGoals([FromQuery] JySalesGoalQueryDto query)
        {
            return SUCCESS(_service.SalesGoals(Actor, query));
        }

        [HttpGet("inventory")]
        [ActionPermissionFilter(Permission = "jingyue:report:query,jingyue:stock:query")]
        public IActionResult Inventory([FromQuery] JyInventoryQueryDto query)
        {
            return SUCCESS(_service.Inventory(Actor, query));
        }

        [HttpGet("stock-age")]
        [ActionPermissionFilter(Permission = "jingyue:report:query,jingyue:stock:query")]
        public IActionResult StockAge([FromQuery] JyStockAgeQueryDto query)
        {
            return SUCCESS(_service.StockAge(Actor, query));
        }

        [HttpGet("stock-age/details")]
        [ActionPermissionFilter(Permission = "jingyue:report:query,jingyue:stock:query")]
        public IActionResult StockAgeDetails([FromQuery] JyStockAgeDetailQueryDto query)
        {
            return SUCCESS(_service.StockAgeDetails(Actor, query));
        }

        [HttpPost("goals/year")]
        [ActionPermissionFilter(Permission = "jingyue:goal:save")]
        [Log(Title = "静悦年度销售目标", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveYearGoal([FromBody] JyGoalYearSaveDto dto)
        {
            return SUCCESS(_service.SaveYearGoal(Actor, dto));
        }

        [HttpPost("goals/month")]
        [ActionPermissionFilter(Permission = "jingyue:goal:save")]
        [Log(Title = "静悦月度销售目标", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveMonthGoal([FromBody] JyGoalMonthSaveDto dto)
        {
            return SUCCESS(_service.SaveMonthGoal(Actor, dto));
        }

        [HttpPost("goals/category")]
        [ActionPermissionFilter(Permission = "jingyue:goal:save")]
        [Log(Title = "静悦分类销售目标", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveCategoryGoal([FromBody] JyGoalCategorySaveDto dto)
        {
            return SUCCESS(_service.SaveCategoryGoal(Actor, dto));
        }

        [HttpGet("ops-breakdown")]
        [ActionPermissionFilter(Permission = "jingyue:goal:query")]
        public IActionResult OpsBreakdown([FromQuery] JyOpsBreakdownQueryDto query)
        {
            return SUCCESS(_service.OpsBreakdown(Actor, query));
        }

        [HttpPost("goals/weights")]
        [ActionPermissionFilter(Permission = "jingyue:goal:save")]
        [Log(Title = "静悦品类权重设置", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveGoalWeight([FromBody] JyGoalWeightSaveDto dto)
        {
            return SUCCESS(_service.SaveGoalWeight(Actor, dto));
        }

        [HttpPost("goals/plans")]
        [ActionPermissionFilter(Permission = "jingyue:goal:save")]
        [Log(Title = "静悦目标拆解与客户动作", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveGoalPlans([FromBody] JyGoalPlanBatchSaveDto dto)
        {
            return SUCCESS(_service.SaveGoalPlans(Actor, dto));
        }

        [HttpGet("combos")]
        [ActionPermissionFilter(Permission = "jingyue:combo:list,jingyue:goal:query")]
        public IActionResult Combos([FromQuery] JyComboQueryDto query)
        {
            return SUCCESS(_service.ListCombos(Actor, query));
        }

        [HttpGet("combos/report")]
        [ActionPermissionFilter(Permission = "jingyue:combo:list,jingyue:goal:query")]
        public IActionResult ComboReport([FromQuery] JyComboQueryDto query)
        {
            return SUCCESS(_service.ComboReport(Actor, query));
        }

        [HttpPost("combos")]
        [ActionPermissionFilter(Permission = "jingyue:combo:add,jingyue:combo:edit")]
        [Log(Title = "静悦联动组合", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveCombo([FromBody] JyComboEditDto dto)
        {
            return SUCCESS(_service.SaveCombo(Actor, dto));
        }

        [HttpPut("combos/{id}/status")]
        [ActionPermissionFilter(Permission = "jingyue:combo:edit")]
        [Log(Title = "静悦联动组合启停", BusinessType = BusinessType.UPDATE)]
        public IActionResult SetComboStatus(long id, [FromQuery] int status)
        {
            return ToResponse(_service.SetComboStatus(Actor, id, status));
        }

        [HttpDelete("combos/{id}")]
        [ActionPermissionFilter(Permission = "jingyue:combo:remove")]
        [Log(Title = "静悦联动组合", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteCombo(long id)
        {
            return ToResponse(_service.DeleteCombo(Actor, id));
        }
    }
}
