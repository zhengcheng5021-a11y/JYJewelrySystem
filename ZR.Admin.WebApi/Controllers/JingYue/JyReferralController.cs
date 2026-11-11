using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/referral")]
    public class JyReferralController : BaseController
    {
        private readonly JyReferralService _service;

        public JyReferralController(JyReferralService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:referral:list,jingyue:referral:query")]
        public IActionResult List([FromQuery] JyReferralQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("summary")]
        [ActionPermissionFilter(Permission = "jingyue:referral:list,jingyue:referral:query")]
        public IActionResult Summary([FromQuery] JyReferralQueryDto query)
        {
            return SUCCESS(_service.Summary(Actor, query));
        }

        [HttpPost("payout")]
        [ActionPermissionFilter(Permission = "jingyue:referral:payout")]
        [Log(Title = "静悦转介绍返现结算", BusinessType = BusinessType.UPDATE)]
        public IActionResult Payout([FromBody] JyReferralPayoutDto dto)
        {
            return SUCCESS(_service.Settle(Actor, dto));
        }

        /// <summary>按一级款式分类维护返点</summary>
        [HttpGet("rates")]
        [ActionPermissionFilter(Permission = "jingyue:referral:list,jingyue:referral:rate")]
        public IActionResult Rates()
        {
            return SUCCESS(_service.Rates(Actor));
        }

        [HttpPost("rates")]
        [ActionPermissionFilter(Permission = "jingyue:referral:rate")]
        [Log(Title = "静悦转介绍返点规则", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveRates([FromBody] JyReferralRateEditDto dto)
        {
            return ToResponse(_service.SaveRates(Actor, dto));
        }

        /// <summary>先调整金额、保持待返现状态</summary>
        [HttpPost("{id:long}/amount")]
        [ActionPermissionFilter(Permission = "jingyue:referral:payout")]
        [Log(Title = "静悦转介绍返现金额调整", BusinessType = BusinessType.UPDATE)]
        public IActionResult AdjustAmount(long id, [FromBody] JyReferralAmountDto dto)
        {
            if (dto == null) throw new CustomException("请填写返现金额");
            return SUCCESS(_service.AdjustAmount(Actor, id, dto.RewardAmount, dto.Remark));
        }
        [HttpPost("{id:long}/cancel-payout")]
        [ActionPermissionFilter(Permission = "jingyue:referral:payout")]
        [Log(Title = "静悦转介绍返现撤销", BusinessType = BusinessType.UPDATE)]
        public IActionResult CancelPayout(long id)
        {
            return ToResponse(_service.CancelPaid(Actor, id));
        }

        [HttpGet("graph/{memberId:long}")]
        [ActionPermissionFilter(Permission = "jingyue:referral:query,jingyue:member:query")]
        public IActionResult Graph(long memberId)
        {
            return SUCCESS(_service.Graph(Actor, memberId));
        }

        [HttpGet("referrer")]
        [ActionPermissionFilter(Permission = "jingyue:referral:query,jingyue:member:add,jingyue:member:edit,jingyue:sale:add")]
        public IActionResult Referrer([FromQuery] string keyword)
        {
            return SUCCESS(_service.FindReferrer(Actor, keyword));
        }
    }
}
