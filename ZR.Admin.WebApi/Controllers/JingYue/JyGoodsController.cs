using Microsoft.AspNetCore.Mvc;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/goods")]
    public class JyGoodsController : BaseController
    {
        private readonly JyGoodsService _service;

        public JyGoodsController(JyGoodsService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName());

        /// <summary>商品档案来自已完成的采购入库，按单件商品查询。</summary>
        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "jingyue:goods:list,jingyue:goods:query")]
        public IActionResult List([FromQuery] JyGoodsItemQueryDto query)
        {
            return SUCCESS(_service.List(Actor, query));
        }

        [HttpGet("{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:goods:query")]
        public IActionResult Detail(long id)
        {
            return SUCCESS(_service.Detail(Actor, id));
        }

        /// <summary>保留分类/材质选项，供库存等页面筛选使用。</summary>
        [HttpGet("options")]
        [ActionPermissionFilter(Permission = "jingyue:goods:query,jingyue:goods:edit")]
        public IActionResult Options()
        {
            return SUCCESS(_service.Options(Actor));
        }

        /// <summary>只允许编辑已入库商品，不提供手工新建入口。</summary>
        [HttpPost("item")]
        [ActionPermissionFilter(Permission = "jingyue:goods:edit")]
        [Log(Title = "静悦商品档案", BusinessType = BusinessType.UPDATE)]
        public IActionResult Save([FromBody] JyGoodsItemEditDto dto)
        {
            return SUCCESS(_service.Save(Actor, dto));
        }
    }
}
