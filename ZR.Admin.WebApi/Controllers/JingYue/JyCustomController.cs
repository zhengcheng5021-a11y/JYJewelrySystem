using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/custom")]
    public class JyCustomController : BaseController
    {
        private readonly JyCustomService _service;

        public JyCustomController(JyCustomService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:custom:list,jingyue:custom:query,jingyue:custom:refund")]
        public IActionResult List([FromQuery] JyCustomQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("dashboard")]
        [ActionPermissionFilter(Permission = "jingyue:custom:list,jingyue:custom:query,jingyue:custom:add")]
        public IActionResult Dashboard()
        {
            return SUCCESS(_service.Dashboard(Actor));
        }

        [HttpGet("available")]
        [ActionPermissionFilter(Permission = "jingyue:custom:query,jingyue:sale:add,jingyue:sale:edit,jingyue:sale:query")]
        public IActionResult Available([FromQuery] long? memberId, [FromQuery] string phone = "")
        {
            return SUCCESS(_service.Available(Actor, memberId, phone));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:custom:query,jingyue:custom:list,jingyue:custom:refund")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:custom:add,jingyue:custom:edit")]
        [Log(Title = "静悦销售定制", BusinessType = BusinessType.INSERT)]
        public IActionResult Save([FromBody] JyCustomEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:custom:remove")]
        [Log(Title = "静悦销售定制", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }

        [HttpPost("{id:long}/arrive")]
        [ActionPermissionFilter(Permission = "jingyue:custom:arrive,jingyue:custom:edit")]
        [Log(Title = "静悦定制到货", BusinessType = BusinessType.UPDATE)]
        public IActionResult Arrive(long id, [FromBody] JyCustomArrivalDto dto)
        {
            if (dto == null) throw new CustomException("到货参数不能为空");
            dto.Id = id;
            return ToResponse(_service.Arrive(Actor, dto));
        }

        [HttpPost("{id:long}/pickup")]
        [ActionPermissionFilter(Permission = "jingyue:custom:arrive,jingyue:custom:edit")]
        [Log(Title = "静悦定制取货", BusinessType = BusinessType.UPDATE)]
        public IActionResult Pickup(long id, [FromBody] JyCustomPickupDto dto)
        {
            dto ??= new JyCustomPickupDto();
            dto.Id = id;
            return ToResponse(_service.Pickup(Actor, dto));
        }

        [HttpPost("refund")]
        [ActionPermissionFilter(Permission = "jingyue:custom:refund")]
        [Log(Title = "静悦定金退回", BusinessType = BusinessType.UPDATE)]
        public IActionResult Refund([FromBody] JyCustomRefundDto dto)
        {
            return ToResponse(_service.Refund(Actor, dto));
        }
    }
}
