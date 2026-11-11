using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/member")]
    public class JyMemberController : BaseController
    {
        private readonly JyMemberService _service;

        public JyMemberController(JyMemberService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:member:list,jingyue:member:query")]
        public IActionResult List([FromQuery] JyMemberQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("export")]
        [ActionPermissionFilter(Permission = "jingyue:member:export")]
        [Log(Title = "静悦会员档案导出", BusinessType = BusinessType.EXPORT)]
        public IActionResult Export([FromQuery] JyMemberQueryDto query)
        {
            var list = _service.Export(Actor, query);
            if (list.Count == 0)
            {
                return ToResponse(ResultCode.FAIL, "没有符合当前筛选条件的会员数据");
            }
            var result = ExportExcelMini(list, "会员档案", "静悦会员档案");
            return ExportExcel(result.Item2, result.Item1);
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:member:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:member:query,jingyue:member:add,jingyue:member:edit")]
        public IActionResult Options()
        {
            return SUCCESS(_service.Options(Actor));
        }

        [HttpGet("dashboard")]
        [ActionPermissionFilter(Permission = "jingyue:member:list")]
        public IActionResult Dashboard()
        {
            return SUCCESS(_service.Dashboard(Actor));
        }

        [HttpPost]
        [ActionPermissionFilter(Permission = "jingyue:member:add,jingyue:member:edit")]
        [Log(Title = "静悦会员档案", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyMemberEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }

        [HttpDelete("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:member:remove")]
        [Log(Title = "静悦会员档案", BusinessType = BusinessType.DELETE)]
        public IActionResult Delete(long id)
        {
            return ToResponse(_service.Delete(Actor, id));
        }

        [HttpGet("levels")]
        [ActionPermissionFilter(Permission = "jingyue:member:level,jingyue:member:query")]
        public IActionResult Levels()
        {
            return SUCCESS(_service.Levels(Actor));
        }

        [HttpPost("level")]
        [ActionPermissionFilter(Permission = "jingyue:member:level")]
        [Log(Title = "静悦会员等级", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveLevel([FromBody] JyMemberLevelEditDto dto)
        {
            return SUCCESS(_service.SaveLevel(Actor, dto));
        }

        [HttpPost("points")]
        [ActionPermissionFilter(Permission = "jingyue:member:points")]
        [Log(Title = "静悦会员积分", BusinessType = BusinessType.UPDATE)]
        public IActionResult AdjustPoints([FromBody] JyPointAdjustDto dto)
        {
            return ToResponse(_service.AdjustPoints(Actor, dto));
        }

        [HttpGet("{id:long}/history")]
        [ActionPermissionFilter(Permission = "jingyue:member:query")]
        public IActionResult History(long id)
        {
            return SUCCESS(_service.GetHistory(Actor, id));
        }

        /// <summary>客户的推荐关系历史（多对一维护，销售单只选其中一位）。</summary>
        [HttpGet("{id:long}/referrers")]
        [ActionPermissionFilter(Permission = "jingyue:member:query")]
        public IActionResult Referrers(long id)
        {
            return SUCCESS(_service.Referrers(Actor, id));
        }

        /// <summary>新增推荐人；已有返现历史的关系不允许物理删除，只做逻辑隐藏。</summary>
        [HttpPost("{id:long}/referrer")]
        [ActionPermissionFilter(Permission = "jingyue:member:edit")]
        [Log(Title = "静悦会员推荐关系", BusinessType = BusinessType.UPDATE)]
        public IActionResult AddReferrer(long id, [FromBody] JyMemberReferrerEditDto dto)
        {
            return SUCCESS(_service.AddReferrer(Actor, id, dto));
        }

        [HttpPost("{id:long}/referrer/{linkId:long}/primary")]
        [ActionPermissionFilter(Permission = "jingyue:member:edit")]
        [Log(Title = "静悦会员主推荐人", BusinessType = BusinessType.UPDATE)]
        public IActionResult SetPrimaryReferrer(long id, long linkId)
        {
            return ToResponse(_service.SetPrimaryReferrer(Actor, id, linkId));
        }

        /// <summary>逻辑移出推荐关系（隐藏），历史返现与来源单永久保留。</summary>
        [HttpDelete("{id:long}/referrer/{linkId:long}")]
        [ActionPermissionFilter(Permission = "jingyue:member:edit")]
        [Log(Title = "静悦会员推荐关系", BusinessType = BusinessType.DELETE)]
        public IActionResult RemoveReferrer(long id, long linkId)
        {
            return ToResponse(_service.RemoveReferrer(Actor, id, linkId));
        }

        [HttpGet("point-rule")]
        [ActionPermissionFilter(Permission = "jingyue:member:pointrule,jingyue:member:query")]
        public IActionResult PointRule()
        {
            return SUCCESS(_service.GetPointRule(Actor));
        }

        [HttpPost("point-rule")]
        [ActionPermissionFilter(Permission = "jingyue:member:pointrule")]
        [Log(Title = "静悦积分规则", BusinessType = BusinessType.UPDATE)]
        public IActionResult SavePointRule([FromBody] JyPointRuleEditDto dto)
        {
            return SUCCESS(_service.SavePointRule(Actor, dto));
        }

        [HttpPost("points/clear-all")]
        [ActionPermissionFilter(Permission = "jingyue:member:clearpoints")]
        [Log(Title = "静悦会员实时积分清零", BusinessType = BusinessType.UPDATE)]
        public IActionResult ClearAllPoints()
        {
            return SUCCESS(_service.ClearAllPoints(Actor));
        }

        [HttpGet("points")]
        [ActionPermissionFilter(Permission = "jingyue:member:query,jingyue:member:points")]
        public IActionResult Points([FromQuery] JyPointLedgerQueryDto query)
        {
            return SUCCESS(_service.Points(Actor, query));
        }
    }
}
