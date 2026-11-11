using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/lookup")]
    public class JyLookupController : BaseController
    {
        private readonly JyLookupService _service;

        public JyLookupController(JyLookupService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName());

        [HttpGet("types")]
        [ActionPermissionFilter(Permission = "jingyue:lookup:list")]
        public IActionResult Types()
        {
            return SUCCESS(_service.ListTypes(Actor));
        }

        [HttpPost("type")]
        [ActionPermissionFilter(Permission = "jingyue:lookup:add,jingyue:lookup:edit")]
        [Log(Title = "静悦字典类型", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveType([FromBody] JyLookupTypeEditDto dto)
        {
            return SUCCESS(_service.SaveType(Actor, dto));
        }

        [HttpDelete("type/{id}")]
        [ActionPermissionFilter(Permission = "jingyue:lookup:remove")]
        [Log(Title = "静悦字典类型", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteType(long id)
        {
            return ToResponse(_service.DeleteType(Actor, id));
        }

        [HttpGet("items")]
        [ActionPermissionFilter(Permission = "jingyue:lookup:list")]
        public IActionResult Items([FromQuery] long typeId)
        {
            return SUCCESS(_service.ListItems(Actor, typeId));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:lookup:query,jingyue:script:list,jingyue:script:add,jingyue:script:edit,jingyue:custom:add,jingyue:custom:query,jingyue:material:list,jingyue:material:query,jingyue:material:add,jingyue:material:edit,jingyue:materialdoc:list,jingyue:materialdoc:query,jingyue:materialdoc:add,jingyue:materialdoc:edit")]
        public IActionResult Options([FromQuery] string typeCode)
        {
            return SUCCESS(_service.Options(Actor, typeCode));
        }

        [HttpPost("item")]
        [ActionPermissionFilter(Permission = "jingyue:lookup:add,jingyue:lookup:edit")]
        [Log(Title = "静悦字典数据", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveItem([FromBody] JyLookupItemEditDto dto)
        {
            return SUCCESS(_service.SaveItem(Actor, dto));
        }

        [HttpDelete("item/{id}")]
        [ActionPermissionFilter(Permission = "jingyue:lookup:remove")]
        [Log(Title = "静悦字典数据", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteItem(long id)
        {
            return ToResponse(_service.DeleteItem(Actor, id));
        }
    }
}
