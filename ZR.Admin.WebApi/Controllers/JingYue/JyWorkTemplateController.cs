using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    /// <summary>目标模板维护与应用。</summary>
    [Route("jingyue/worktemplate")]
    public class JyWorkTemplateController : BaseController
    {
        private readonly JyWorkTemplateService _service;

        public JyWorkTemplateController(JyWorkTemplateService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query,jingyue:workitem:template")]
        public IActionResult List([FromQuery] int pageNum = 1, [FromQuery] int pageSize = 50)
        {
            return SUCCESS(_service.List(Actor, pageNum, pageSize));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query,jingyue:workitem:template")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:workitem:template")]
        [Log(Title = "静悦目标模板", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyWorkTemplateEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:template")]
        [Log(Title = "静悦目标模板", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }

        [HttpPost("{id:long}/apply")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:add")]
        [Log(Title = "静悦应用目标模板", BusinessType = BusinessType.INSERT)]
        public IActionResult Apply(long id, [FromBody] JyWorkTemplateApplyDto dto)
        {
            return SUCCESS(_service.Apply(Actor, id, dto));
        }
    }
}
