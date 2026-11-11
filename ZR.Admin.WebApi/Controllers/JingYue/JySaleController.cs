using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/sale")]
    public class JySaleController : BaseController
    {
        private readonly JySaleService _service;

        public JySaleController(JySaleService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:sale:list,jingyue:sale:query")]
        public IActionResult List([FromQuery] JySaleQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:sale:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpGet("customer")]
        [ActionPermissionFilter(Permission = "jingyue:sale:query,jingyue:sale:add,jingyue:sale:edit")]
        public IActionResult Customer([FromQuery] string keyword)
        {
            return SUCCESS(_service.MatchCustomer(Actor, keyword));
        }

        /// <summary>开单时读取客户的多位推荐人，用于本次单选归属。</summary>
        [HttpGet("customer/{memberId:long}/referrers")]
        [ActionPermissionFilter(Permission = "jingyue:sale:query,jingyue:sale:add,jingyue:sale:edit")]
        public IActionResult CustomerReferrers(long memberId)
        {
            return SUCCESS(_service.CustomerReferrers(Actor, memberId));
        }

        [HttpGet("goods")]
        [ActionPermissionFilter(Permission = "jingyue:sale:query,jingyue:sale:add,jingyue:sale:edit")]
        public IActionResult Goods([FromQuery] string keyword, [FromQuery] long? memberId, [FromQuery] string phone = "")
        {
            return SUCCESS(_service.FindSaleGoods(Actor, keyword, memberId, phone));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:sale:query,jingyue:sale:add,jingyue:sale:edit")]
        public IActionResult Options()
        {
            return SUCCESS(_service.Options(Actor));
        }

        [HttpGet("dashboard")]
        [ActionPermissionFilter(Permission = "jingyue:sale:list")]
        public IActionResult Dashboard()
        {
            return SUCCESS(_service.Dashboard(Actor));
        }

        [HttpPost("preview")]
        [ActionPermissionFilter(Permission = "jingyue:sale:query,jingyue:sale:add,jingyue:sale:edit")]
        public IActionResult Preview([FromBody] JySaleEditDto dto)
        {
            return SUCCESS(_service.Preview(Actor, dto));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:sale:add,jingyue:sale:edit")]
        [Log(Title = "静悦销售开单", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JySaleEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpPost("{id:long}/confirm")]
        [ActionPermissionFilter(Permission = "jingyue:sale:confirm")]
        [Log(Title = "静悦确认销售", BusinessType = BusinessType.UPDATE)]
        public IActionResult Confirm(long id)
        {
            return ToResponse(_service.Confirm(Actor, id));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:sale:remove")]
        [Log(Title = "静悦销售单", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }
    }
}
