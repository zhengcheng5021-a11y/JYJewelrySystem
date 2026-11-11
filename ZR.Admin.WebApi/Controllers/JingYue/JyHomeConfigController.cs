using Microsoft.AspNetCore.Mvc;
using Infrastructure;
using ZR.Model.JingYue.Dto;
using ZR.Service.JingYue;

namespace ZR.Admin.WebApi.Controllers.JingYue
{
    [Route("jingyue/home-config")]
    public class JyHomeConfigController : BaseController
    {
        private readonly JyHomeConfigService _service;

        public JyHomeConfigController(JyHomeConfigService service)
        {
            _service = service;
        }

        private JyActor Actor => new(App.GetCurrentTenantId(), HttpContext.GetName(), HttpContext.GetUId());

        [HttpGet("birthdays")]
        [ActionPermissionFilter(Permission = "jingyue:member:list,jingyue:member:query")]
        public IActionResult Birthdays()
        {
            return SUCCESS(_service.BirthdayReminders(Actor));
        }

        [HttpGet("stock-alerts")]
        [ActionPermissionFilter(Permission = "jingyue:stock:list,jingyue:stock:query,jingyue:dict:list")]
        public IActionResult StockAlerts()
        {
            return SUCCESS(_service.StockAlerts(Actor));
        }

        [HttpGet("stock-alert-settings")]
        [ActionPermissionFilter(Permission = "jingyue:stock:list,jingyue:stock:query,jingyue:dict:list")]
        public IActionResult StockAlertSettings()
        {
            return SUCCESS(_service.StockAlertSettings(Actor));
        }

        [HttpPost("stock-alert-settings")]
        [ActionPermissionFilter(Permission = "jingyue:stock:alert,jingyue:dict:edit")]
        [Log(Title = "静悦库存预警设置", BusinessType = BusinessType.UPDATE)]
        public IActionResult SaveStockAlert([FromBody] JyStockAlertEditDto dto)
        {
            return SUCCESS(_service.SaveStockAlert(Actor, dto));
        }

        [HttpGet("print-options")]
        [ActionPermissionFilter(Permission = "jingyue:stock:list,jingyue:stock:query,jingyue:dict:list")]
        public IActionResult PrintOptions()
        {
            return SUCCESS(_service.PrintOptions(Actor));
        }

        [HttpPost("print-template")]
        [ActionPermissionFilter(Permission = "jingyue:print:template,jingyue:dict:edit")]
        [Log(Title = "静悦标签打印模板", BusinessType = BusinessType.UPDATE)]
        public IActionResult SavePrintTemplate([FromBody] JyPrintTemplateEditDto dto)
        {
            return SUCCESS(_service.SavePrintTemplate(Actor, dto));
        }

        [HttpDelete("print-template/{id:long}")]
        [ActionPermissionFilter(Permission = "jingyue:print:template,jingyue:dict:edit")]
        [Log(Title = "静悦标签打印模板", BusinessType = BusinessType.DELETE)]
        public IActionResult DeletePrintTemplate(long id)
        {
            return ToResponse(_service.DeletePrintTemplate(Actor, id));
        }

        [HttpPost("print-binding")]
        [ActionPermissionFilter(Permission = "jingyue:print:template,jingyue:dict:edit")]
        [Log(Title = "静悦标签模板绑定", BusinessType = BusinessType.UPDATE)]
        public IActionResult SavePrintBinding([FromBody] JyPrintTemplateBindingEditDto dto)
        {
            return SUCCESS(_service.SavePrintBinding(Actor, dto));
        }

        [HttpPost("print-render")]
        [ActionPermissionFilter(Permission = "jingyue:stock:list,jingyue:stock:query,jingyue:inbound:list,jingyue:inbound:query")]
        public IActionResult RenderLabel([FromBody] JyPrintRenderRequestDto dto)
        {
            if (dto == null || (dto.GoodsItemId <= 0 && dto.InboundLineId <= 0)) throw new CustomException("请选择入库商品");
            var options = _service.PrintOptions(Actor);
            var template = options.Templates.FirstOrDefault(it => it.Id == dto.TemplateId && it.Status == 0)
                ?? throw new CustomException("打印模板不存在或已停用");
            var label = dto.InboundLineId > 0
                ? _service.RenderAgentLabelForInboundLine(Actor, dto.InboundLineId, template)
                : _service.RenderAgentLabel(Actor, dto.GoodsItemId, template);
            var html = dto.GoodsItemId > 0 ? _service.RenderLabel(Actor, dto.GoodsItemId, template) : null;
            return SUCCESS(new { html, label });
        }
    }
}
