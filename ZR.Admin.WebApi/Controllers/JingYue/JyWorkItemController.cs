using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    /// <summary>待办与目标工作台。</summary>
    [Route("jingyue/workitem")]
    public class JyWorkItemController : BaseController
    {
        private readonly JyWorkItemService _service;

        public JyWorkItemController(JyWorkItemService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query")]
        public IActionResult List([FromQuery] JyWorkItemQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("stats")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query")]
        public IActionResult Stats()
        {
            return SUCCESS(_service.Stats(Actor));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query,jingyue:workitem:add,jingyue:workitem:edit")]
        public IActionResult Options()
        {
            return SUCCESS(_service.Options(Actor));
        }

        [HttpGet("quick-actions")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query,jingyue:workitem:add,jingyue:workitem:edit")]
        public IActionResult QuickActions()
        {
            return SUCCESS(_service.QuickActions(Actor));
        }

        [HttpPost("quick-actions")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:add,jingyue:workitem:edit")]
        [Log(Title = "静悦待办快捷事项", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveQuickAction([FromBody] JyWorkQuickActionEditDto dto)
        {
            return SUCCESS(_service.SaveQuickAction(Actor, dto));
        }

        [HttpDelete("quick-actions/{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:remove")]
        [Log(Title = "静悦待办快捷事项", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteQuickAction(long id)
        {
            return ToResponse(_service.DeleteQuickAction(Actor, id));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpGet("{id:long}/evidence")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query")]
        public IActionResult Evidence(long id)
        {
            return SUCCESS(_service.Evidence(Actor, id));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:workitem:add,jingyue:workitem:edit")]
        [Log(Title = "静悦待办与目标", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyWorkItemEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:remove")]
        [Log(Title = "静悦待办与目标", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }

        [HttpPost("{id:long}/complete")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:complete")]
        [Log(Title = "静悦待办完成", BusinessType = BusinessType.UPDATE)]
        public IActionResult Complete(long id, [FromQuery] bool completed = true)
        {
            return ToResponse(_service.Complete(Actor, id, completed));
        }

        [HttpPost("{id:long}/progress")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:edit")]
        [Log(Title = "静悦目标进度", BusinessType = BusinessType.UPDATE)]
        public IActionResult Progress(long id, [FromBody] JyWorkProgressEditDto dto)
        {
            return ToResponse(_service.Progress(Actor, id, dto));
        }

        [HttpPost("{id:long}/recalculate")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:edit")]
        [Log(Title = "静悦目标重算", BusinessType = BusinessType.UPDATE)]
        public IActionResult Recalculate(long id)
        {
            return ToResponse(_service.Recalculate(Actor, id));
        }
    }
}
