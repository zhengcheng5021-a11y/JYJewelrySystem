using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/sale/return")]
    public class JySaleReturnController : BaseController
    {
        private readonly JySaleReturnService _service;

        public JySaleReturnController(JySaleReturnService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        /// <summary>查询可退货的已完成销售单。</summary>
        [HttpGet("orders")]
        [ActionPermissionFilter(Permission = "jingyue:sale:return")]
        public IActionResult Orders([FromQuery] JySaleReturnQueryDto query)
        {
            return SUCCESS(_service.ReturnableOrders(Actor, query));
        }

        /// <summary>查看原销售单及可退数量。</summary>
        [HttpGet("sale/{saleOrderId:long}")]
        [ActionPermissionFilter(Permission = "jingyue:sale:return")]
        public IActionResult SaleDetail(long saleOrderId)
        {
            return SUCCESS(_service.ReturnableDetail(Actor, saleOrderId));
        }

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:sale:return,jingyue:sale:query")]
        public IActionResult List([FromQuery] JySaleReturnQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:sale:return,jingyue:sale:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:sale:return")]
        [Log(Title = "静悦销售退货", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JySaleReturnEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }
    }
}
