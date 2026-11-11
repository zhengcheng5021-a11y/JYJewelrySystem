using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/factory")]
    public class JyFactoryReturnController : BaseController
    {
        private readonly JyFactoryReturnService _service;

        public JyFactoryReturnController(JyFactoryReturnService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:factory:list,jingyue:factory:query")]
        public IActionResult List([FromQuery] JyFactoryReturnQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:factory:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpGet("goods")]
        [ActionPermissionFilter(Permission = "jingyue:factory:query,jingyue:factory:add,jingyue:factory:edit")]
        public IActionResult Goods([FromQuery] string keyword)
        {
            return SUCCESS(_service.FindGoods(Actor, keyword));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:factory:add,jingyue:factory:edit")]
        [Log(Title = "静悦商品返厂", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyFactoryReturnEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpPost("{id:long}/confirm")]
        [ActionPermissionFilter(Permission = "jingyue:factory:confirm")]
        [Log(Title = "静悦确认返厂", BusinessType = BusinessType.UPDATE)]
        public IActionResult Confirm(long id)
        {
            return ToResponse(_service.Confirm(Actor, id));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:factory:remove")]
        [Log(Title = "静悦商品返厂", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }
    }
}