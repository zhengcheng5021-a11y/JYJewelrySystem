using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/supplier")]
    public class JySupplierController : BaseController
    {
        private readonly JySupplierService _service;

        public JySupplierController(JySupplierService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:supplier:list")]
        public IActionResult List([FromQuery] JySupplierQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:supplier:add,jingyue:supplier:edit")]
        [Log(Title = "静悦供应商", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JySupplierEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("{id}")]
        [ActionPermissionFilter(Permission = "jingyue:supplier:remove")]
        [Log(Title = "静悦供应商", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }
    }
}
