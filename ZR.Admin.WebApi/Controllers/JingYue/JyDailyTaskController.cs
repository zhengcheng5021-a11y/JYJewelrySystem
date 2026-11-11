using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    /// <summary>每日必做。复用待办菜单权限，避免额外菜单配置。</summary>
    [Route("jingyue/dailytask")]
    public class JyDailyTaskController : BaseController
    {
        private readonly JyDailyTaskService _service;

        public JyDailyTaskController(JyDailyTaskService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query")]
        public IActionResult List([FromQuery] JyDailyTaskQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("templates")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:query")]
        public IActionResult Templates()
        {
            return SUCCESS(_service.Templates(Actor));
        }

        [HttpPost("template")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:add,jingyue:workitem:edit")]
        [Log(Title = "每日必做模板", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveTemplate([FromBody] JyDailyTaskTemplateEditDto dto)
        {
            return SUCCESS(_service.SaveTemplate(Actor, dto));
        }

        [HttpDelete("template/{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:remove")]
        [Log(Title = "每日必做模板", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteTemplate(long id)
        {
            return ToResponse(_service.DeleteTemplate(Actor, id));
        }

        [HttpPost("{id:long}/complete")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:complete")]
        [Log(Title = "每日必做完成", BusinessType = BusinessType.UPDATE)]
        public IActionResult Toggle(long id, [FromQuery] bool completed = true)
        {
            return ToResponse(_service.Toggle(Actor, id, completed));
        }

        [HttpPost("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:edit")]
        [Log(Title = "每日必做内容", BusinessType = BusinessType.UPDATE)]
        public IActionResult Update(long id, [FromBody] JyDailyTaskEditDto dto)
        {
            if (dto == null || dto.Id != id) throw new CustomException("必做项参数不一致");
            return ToResponse(_service.UpdateItem(Actor, dto));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:remove")]
        [Log(Title = "每日必做删除", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.DeleteItem(Actor, id));
        }

        [HttpPost("sort")]
        [ActionPermissionFilter(Permission = "jingyue:workitem:edit")]
        [Log(Title = "每日必做排序", BusinessType = BusinessType.UPDATE)]
        public IActionResult Sort([FromBody] JyDailyTaskSortDto dto)
        {
            return ToResponse(_service.Sort(Actor, dto));
        }
    }
}