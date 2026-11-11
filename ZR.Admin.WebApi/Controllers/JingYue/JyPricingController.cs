using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/pricing")]
    public class JyPricingController : BaseController
    {
        private readonly JyPricingService _service;

        public JyPricingController(JyPricingService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("metal/list")]
        [ActionPermissionFilter(Permission = "jingyue:pricing:list,jingyue:pricing:query")]
        public IActionResult MetalList([FromQuery] JyMetalPriceQueryDto query)
        {
            return SUCCESS(_service.ListMetal(Actor, query));
        }

        [HttpPost("metal")]
        [ActionPermissionFilter(Permission = "jingyue:pricing:add,jingyue:pricing:edit")]
        [Log(Title = "静悦金属金价", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveMetal([FromBody] JyMetalPriceEditDto dto)
        {
            return SUCCESS(_service.SaveMetal(Actor, dto));
        }

        [HttpDelete("metal/{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:pricing:remove")]
        [Log(Title = "静悦金属金价", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteMetal(long id)
        {
            return ToResponse(_service.DeleteMetal(Actor, id));
        }

        [HttpGet("discount/list")]
        [ActionPermissionFilter(Permission = "jingyue:pricing:list,jingyue:pricing:query")]
        public IActionResult DiscountList([FromQuery] JyDiscountRuleQueryDto query)
        {
            return SUCCESS(_service.ListDiscount(Actor, query));
        }

        [HttpPost("discount")]
        [ActionPermissionFilter(Permission = "jingyue:pricing:add,jingyue:pricing:edit")]
        [Log(Title = "静悦会员折扣矩阵", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveDiscount([FromBody] JyDiscountRuleEditDto dto)
        {
            return SUCCESS(_service.SaveDiscount(Actor, dto));
        }

        [HttpDelete("discount/{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:pricing:remove")]
        [Log(Title = "静悦会员折扣矩阵", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteDiscount(long id)
        {
            return ToResponse(_service.DeleteDiscount(Actor, id));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:pricing:list,jingyue:pricing:query,jingyue:pricing:add,jingyue:pricing:edit")]
        public IActionResult Options()
        {
            return SUCCESS(_service.Options(Actor));
        }
    }
}
