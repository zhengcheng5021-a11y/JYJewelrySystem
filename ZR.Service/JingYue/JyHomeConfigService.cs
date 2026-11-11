using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyHomeConfigService : BaseService<JyMember>
    {
        private static readonly JyPrintFieldDto[] PrintFields =
        [
            new() { Key = "goodsNo", Label = "商品编号", SortNo = 1 },
            new() { Key = "barcode", Label = "商品条码", SortNo = 2 },
            new() { Key = "goodsName", Label = "款式名称", SortNo = 3 },
            new() { Key = "styleCategory", Label = "款式分类", SortNo = 4 },
            new() { Key = "material", Label = "材质", SortNo = 5 },
            new() { Key = "metal", Label = "金属成色", SortNo = 6 },
            new() { Key = "weight", Label = "重量", SortNo = 7 },
            new() { Key = "salePrice", Label = "售价", SortNo = 8 },
            new() { Key = "costPrice", Label = "成本价", SortNo = 9 },
            new() { Key = "certType", Label = "证书类型", SortNo = 10 },
            new() { Key = "certNo", Label = "证书编号", SortNo = 11 },
            new() { Key = "supplier", Label = "供应商", SortNo = 12 },
            new() { Key = "inboundDate", Label = "入库日期", SortNo = 13 },
            new() { Key = "storeNote", Label = "门店备注", SortNo = 14 },
            new() { Key = "customText1", Label = "自定义文本1", SortNo = 15 },
            new() { Key = "customText2", Label = "自定义文本2", SortNo = 16 }
        ];

        private static readonly Regex PrintTokenPattern = new(@"\{(goodsNo|barcode|goodsName|styleCategory|material|metal|weight|salePrice|costPrice|certType|certNo|supplier|inboundDate|storeNote|customText1|customText2)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly ChineseLunisolarCalendar LunarCalendar = new();
        private static readonly Dictionary<string, int> LunarMonths = new(StringComparer.OrdinalIgnoreCase)
        {
            ["正"] = 1, ["一"] = 1, ["二"] = 2, ["三"] = 3, ["四"] = 4, ["五"] = 5, ["六"] = 6,
            ["七"] = 7, ["八"] = 8, ["九"] = 9, ["十"] = 10, ["冬"] = 11, ["十一"] = 11, ["腊"] = 12, ["十二"] = 12
        };

        private sealed record StyleCatalog(
            List<JyLookupItem> Roots,
            Dictionary<long, long> RootIdByItemId,
            Dictionary<long, decimal> QtyByRootId,
            decimal TotalQty,
            decimal UnclassifiedQty);

        public JyBirthdayReminderResultDto BirthdayReminders(JyActor actor)
        {
            var today = DateTime.Today;
            var rows = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0
                    && (it.SolarBirthday != null || it.LunarBirthday != null))
                .ToList();
            var levelIds = rows.Where(it => it.LevelId.HasValue).Select(it => it.LevelId.Value).Distinct().ToList();
            var levels = levelIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == actor.TenantId && levelIds.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id, it => it.LevelName);
            var styleIds = rows.Where(it => it.UsualStyleItemId.HasValue).Select(it => it.UsualStyleItemId.Value).Distinct().ToList();
            var styles = styleIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && styleIds.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id, it => it.ItemName);
            var result = new JyBirthdayReminderResultDto { WithinDays = 7 };
            foreach (var member in rows)
            {
                var upcoming = ResolveUpcomingBirthday(member, today);
                if (upcoming == null || upcoming.Value.DaysUntil > 7) continue;
                result.Rows.Add(new JyBirthdayReminderDto
                {
                    MemberId = member.Id,
                    MemberNo = member.MemberNo,
                    MemberName = member.MemberName,
                    Phone = member.Phone,
                    LevelName = member.LevelId.HasValue && levels.TryGetValue(member.LevelId.Value, out var level) ? level : "未分级",
                    BirthdayDate = upcoming.Value.Date.ToString("yyyy-MM-dd"),
                    DaysUntil = upcoming.Value.DaysUntil,
                    CalendarText = upcoming.Value.IsLunar ? "农历生日" : "公历生日",
                    TotalAmount = member.TotalAmount,
                    UsualStyleName = member.UsualStyleItemId.HasValue && styles.TryGetValue(member.UsualStyleItemId.Value, out var style) ? style : null
                });
            }
            result.Rows = result.Rows.OrderBy(it => it.DaysUntil).ThenByDescending(it => it.TotalAmount).ThenBy(it => it.MemberId).Take(100).ToList();
            result.TodayCount = result.Rows.Count(it => it.DaysUntil == 0);
            result.UpcomingCount = result.Rows.Count;
            return result;
        }

        public JyStockAlertResultDto StockAlerts(JyActor actor)
        {
            var catalog = LoadStyleCatalog(actor.TenantId);
            var rules = Context.Queryable<JyStockAlertRule>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.Id).ToList();
            if (rules.Count == 0) return new JyStockAlertResultDto
            {
                TotalQty = catalog.TotalQty,
                UnclassifiedQty = catalog.UnclassifiedQty
            };
            var normalizedRules = rules
                .Select(it =>
                {
                    var rootId = catalog.RootIdByItemId.TryGetValue(it.StyleItemId, out var mappedId)
                        ? mappedId
                        : catalog.Roots.FirstOrDefault(r => r.ItemName == it.StyleItemName)?.Id ?? 0;
                    return new { Rule = it, RootId = rootId };
                })
                .Where(it => it.RootId > 0)
                .GroupBy(it => it.RootId)
                .Select(it => it.OrderByDescending(r => r.Rule.Update_time).ThenByDescending(r => r.Rule.Id).First())
                .ToList();
            var result = new JyStockAlertResultDto();
            foreach (var item in normalizedRules)
            {
                var rule = item.Rule;
                var root = catalog.Roots.First(it => it.Id == item.RootId);
                var current = catalog.QtyByRootId.TryGetValue(item.RootId, out var qty) ? qty : 0m;
                var level = StockAlertLevel(current, rule.MinQty);
                result.Rows.Add(new JyStockAlertRuleDto
                {
                    Id = rule.Id,
                    StyleItemId = item.RootId,
                    StyleItemName = root.ItemName,
                    MinQty = rule.MinQty,
                    CurrentQty = current,
                    AlertLevel = level,
                    Status = rule.Status,
                    Remark = rule.Remark,
                    UpdateTime = rule.Update_time
                });
            }
            result.Rows = result.Rows.OrderByDescending(it => it.AlertLevel).ThenBy(it => it.CurrentQty).ToList();
            result.AlertCount = result.Rows.Count(it => it.AlertLevel > 0);
            result.LowCount = result.Rows.Count(it => it.AlertLevel == 2);
            result.NearCount = result.Rows.Count(it => it.AlertLevel == 1);
            result.TotalQty = catalog.TotalQty;
            result.UnclassifiedQty = catalog.UnclassifiedQty;
            return result;
        }

        public List<JyStockAlertRuleDto> StockAlertSettings(JyActor actor)
        {
            var catalog = LoadStyleCatalog(actor.TenantId);
            if (catalog.Roots.Count == 0) throw new CustomException("款式分类字典不存在，请先初始化静悦基础资料");
            var rules = Context.Queryable<JyStockAlertRule>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0).ToList();
            return catalog.Roots.Select(it =>
            {
                var rule = rules
                    .Where(r => r.StyleItemId == it.Id || r.StyleItemName == it.ItemName)
                    .OrderByDescending(r => r.Update_time).ThenByDescending(r => r.Id)
                    .FirstOrDefault();
                var current = catalog.QtyByRootId.TryGetValue(it.Id, out var qty) ? qty : 0m;
                return new JyStockAlertRuleDto
                {
                    Id = rule?.Id ?? 0,
                    StyleItemId = it.Id,
                    StyleItemName = it.ItemName,
                    MinQty = rule?.MinQty ?? 0m,
                    CurrentQty = current,
                    AlertLevel = rule?.Status == 0 ? StockAlertLevel(current, rule.MinQty) : 0,
                    Status = rule?.Status ?? 0,
                    Remark = rule?.Remark,
                    UpdateTime = rule?.Update_time
                };
            }).ToList();
        }

        public JyStockAlertRuleDto SaveStockAlert(JyActor actor, JyStockAlertEditDto dto)
        {
            if (dto == null || dto.StyleItemId <= 0) throw new CustomException("请选择一级款式分类");
            if (dto.MinQty < 0 || dto.MinQty > 999999) throw new CustomException("最低库存量需在 0 到 999999 之间");
            if (dto.Status is < 0 or > 1) throw new CustomException("库存预警状态不正确");
            var catalog = LoadStyleCatalog(actor.TenantId);
            var rootId = catalog.RootIdByItemId.TryGetValue(dto.StyleItemId, out var mappedId) ? mappedId : dto.StyleItemId;
            var style = catalog.Roots.FirstOrDefault(it => it.Id == rootId)
                ?? throw new CustomException("一级款式分类不存在");
            if (style.Status != 0) throw new CustomException("库存预警只能设置启用中的一级款式分类");
            var now = DateTime.Now;
            var remark = NormalizeRemark(dto.Remark);
            var current = Context.Queryable<JyStockAlertRule>().First(it => it.TenantId == actor.TenantId
                && (it.StyleItemId == style.Id || it.StyleItemName == style.ItemName) && it.DelFlag == 0);
            if (current == null)
            {
                current = new JyStockAlertRule
                {
                    TenantId = actor.TenantId,
                    StyleItemId = style.Id,
                    StyleItemName = style.ItemName,
                    MinQty = dto.MinQty,
                    Status = dto.Status,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = now,
                    Remark = remark
                };
                current.Id = Context.Insertable(current).ExecuteReturnBigIdentity();
            }
            else
            {
                Context.Updateable<JyStockAlertRule>()
                    .SetColumns(it => it.StyleItemName == style.ItemName)
                    .SetColumns(it => it.MinQty == dto.MinQty)
                    .SetColumns(it => it.Status == dto.Status)
                    .SetColumns(it => it.Remark == remark)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == current.Id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
            }
            return StockAlertSettings(actor).First(it => it.StyleItemId == style.Id);
        }

        public JyPrintTemplateOptionsDto PrintOptions(JyActor actor)
        {
            var templates = LoadTemplates(actor.TenantId).Select(ToTemplateDto).ToList();
            var bindings = LoadBindings(actor.TenantId).Select(it =>
            {
                var template = templates.FirstOrDefault(t => t.Id == it.TemplateId);
                return new JyPrintTemplateBindingDto
                {
                    Id = it.Id,
                    StyleItemId = it.StyleItemId,
                    StyleItemName = it.StyleItemName,
                    TemplateId = it.TemplateId,
                    TemplateName = template?.TemplateName
                };
            }).ToList();
            return new JyPrintTemplateOptionsDto
            {
                Templates = templates,
                Bindings = bindings,
                AvailableFields = PrintFields.Select(it => new JyPrintFieldDto { Key = it.Key, FieldKey = it.Key, Label = it.Label, SortNo = it.SortNo, Enabled = 1, Type = "text" }).ToList()
            };
        }

        public JyPrintTemplateDto SavePrintTemplate(JyActor actor, JyPrintTemplateEditDto dto)
        {
            if (dto == null) throw new CustomException("模板内容不能为空");
            var name = NormalizeText(dto.TemplateName, "模板名称", 100, true);
            if (dto.WidthMm < 20 || dto.WidthMm > 120 || dto.HeightMm < 15 || dto.HeightMm > 180)
                throw new CustomException("模板宽度需在 20-120 毫米、高度需在 15-180 毫米之间");
            if (dto.GapMm < 0 || dto.GapMm > 10) throw new CustomException("纸张间距需在 0-10 毫米之间");
            if (dto.OffsetMm < 0 || dto.OffsetMm > 20) throw new CustomException("左边距需在 0-20 毫米之间");
            if (dto.Status is < 0 or > 1) throw new CustomException("模板状态不正确");
            var fields = NormalizeFields(dto.Fields, dto.WidthMm, dto.HeightMm);
            var now = DateTime.Now;
            var remark = NormalizeRemark(dto.Remark);
            var layoutJson = JsonConvert.SerializeObject(new
            {
                version = 1,
                gapMm = dto.GapMm,
                offsetMm = dto.OffsetMm,
                copies = Math.Clamp(dto.Copies, 1, 50),
                port = NormalizePort(dto.Port),
                speed = Math.Clamp(dto.Speed, 1, 10),
                density = Math.Clamp(dto.Density, 0, 15),
                elements = fields
            });
            long id = dto.Id;
            if (id <= 0)
            {
                id = Context.Insertable(new JyPrintTemplate
                {
                    TenantId = actor.TenantId,
                    TemplateName = name,
                    WidthMm = dto.WidthMm,
                    HeightMm = dto.HeightMm,
                    ShowBarcode = dto.ShowBarcode == 1 ? 1 : 0,
                    ShowQrCode = dto.ShowQrCode == 1 ? 1 : 0,
                    FieldsJson = layoutJson,
                    Remark = remark,
                    Status = dto.Status,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = now
                }).ExecuteReturnBigIdentity();
            }
            else
            {
                var current = MustTemplate(actor.TenantId, id);
                Context.Updateable<JyPrintTemplate>()
                    .SetColumns(it => it.TemplateName == name)
                    .SetColumns(it => it.WidthMm == dto.WidthMm)
                    .SetColumns(it => it.HeightMm == dto.HeightMm)
                    .SetColumns(it => it.ShowBarcode == (dto.ShowBarcode == 1 ? 1 : 0))
                    .SetColumns(it => it.ShowQrCode == (dto.ShowQrCode == 1 ? 1 : 0))
                    .SetColumns(it => it.FieldsJson == layoutJson)
                    .SetColumns(it => it.Remark == remark)
                    .SetColumns(it => it.Status == dto.Status)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == current.Id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
            }
            return ToTemplateDto(MustTemplate(actor.TenantId, id));
        }

        public int DeletePrintTemplate(JyActor actor, long id)
        {
            var template = MustTemplate(actor.TenantId, id);
            var bound = Context.Queryable<JyPrintTemplateBinding>().Any(it => it.TenantId == actor.TenantId && it.TemplateId == id && it.DelFlag == 0);
            if (bound) throw new CustomException("模板已绑定款式分类，请先解除绑定后再删除");
            return Context.Updateable<JyPrintTemplate>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == template.Id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
        }

        public JyPrintTemplateBindingDto SavePrintBinding(JyActor actor, JyPrintTemplateBindingEditDto dto)
        {
            if (dto == null || dto.StyleItemId <= 0 || dto.TemplateId <= 0) throw new CustomException("请选择一级款式分类和模板");
            var style = Context.Queryable<JyLookupItem>()
                .First(it => it.Id == dto.StyleItemId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("一级款式分类不存在");
            if (style.ParentId.HasValue || style.Status != 0) throw new CustomException("只能绑定启用中的一级款式分类");
            var template = MustTemplate(actor.TenantId, dto.TemplateId);
            if (template.Status != 0) throw new CustomException("模板已停用，不能绑定");
            var current = Context.Queryable<JyPrintTemplateBinding>()
                .First(it => it.TenantId == actor.TenantId && it.StyleItemId == style.Id && it.DelFlag == 0);
            if (current == null)
            {
                Context.Insertable(new JyPrintTemplateBinding
                {
                    TenantId = actor.TenantId,
                    StyleItemId = style.Id,
                    StyleItemName = style.ItemName,
                    TemplateId = template.Id,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now
                }).ExecuteCommand();
            }
            else
            {
                Context.Updateable<JyPrintTemplateBinding>()
                    .SetColumns(it => it.StyleItemName == style.ItemName)
                    .SetColumns(it => it.TemplateId == template.Id)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == DateTime.Now)
                    .Where(it => it.Id == current.Id && it.TenantId == actor.TenantId && it.DelFlag == 0).ExecuteCommand();
            }
            return PrintOptions(actor).Bindings.First(it => it.StyleItemId == style.Id);
        }

        public string RenderLabel(JyActor actor, long goodsItemId, JyPrintTemplateDto template)
        {
            if (goodsItemId <= 0 || template == null) throw new CustomException("打印数据或模板不能为空");
            var goods = LoadPrintGoods(actor, goodsItemId);
            var values = PrintValues(actor, goods);
            var css = "@page{size:" + template.WidthMm.ToString("0.##", CultureInfo.InvariantCulture) + "mm " + template.HeightMm.ToString("0.##", CultureInfo.InvariantCulture) + "mm;margin:0}body{margin:0;padding:2mm;font-family:\"Microsoft YaHei\",sans-serif;color:#222;font-size:9pt}.label{box-sizing:border-box;width:" + template.WidthMm.ToString("0.##", CultureInfo.InvariantCulture) + "mm;height:" + template.HeightMm.ToString("0.##", CultureInfo.InvariantCulture) + "mm;padding:2mm;border:1px solid #888;overflow:hidden}.line{display:flex;justify-content:space-between;gap:2mm;margin:0 0 1.2mm;font-size:8.5pt}.line span:first-child{color:#666}.line b{text-align:right}.codes{display:flex;align-items:center;justify-content:center;gap:1mm;margin-top:1mm}.bar{font-family:\"Courier New\",monospace;font-size:10pt;letter-spacing:.4mm;white-space:nowrap}.qr{display:grid;place-items:center;width:15mm;height:15mm;border:1px solid #555;font-size:6pt}";
            var html = new System.Text.StringBuilder("<!doctype html><html><head><meta charset=\"utf-8\"><title>" + Html(template.TemplateName) + "</title><style>" + css + "</style></head><body><div class=\"label\">");
            foreach (var field in template.Fields.Where(it => it.Enabled == 1).OrderBy(it => it.SortNo))
            {
                if (!values.TryGetValue(field.Key, out var value) || string.IsNullOrWhiteSpace(value)) continue;
                html.Append("<div class=\"line\"><span>").Append(Html(field.Label)).Append("</span><b>").Append(Html(value)).Append("</b></div>");
            }
            if (template.ShowBarcode == 1 && !string.IsNullOrWhiteSpace(goods.Barcode))
                html.Append("<div class=\"codes\"><div class=\"bar\">").Append(Html(goods.Barcode)).Append("</div></div>");
            if (template.ShowQrCode == 1 && !string.IsNullOrWhiteSpace(goods.Barcode))
                html.Append("<div class=\"codes\"><div class=\"qr\">QR</div><small>").Append(Html(goods.Barcode)).Append("</small></div>");
            html.Append("</div></body></html>");
            return html.ToString();
        }

        public JyPrintAgentLabelDto RenderAgentLabel(JyActor actor, long goodsItemId, JyPrintTemplateDto template)
        {
            if (goodsItemId <= 0 || template == null) throw new CustomException("打印数据或模板不能为空");
            var goods = LoadPrintGoods(actor, goodsItemId);
            var values = PrintValues(actor, goods);
            var elements = template.Fields.Where(it => it.Enabled == 1).OrderBy(it => it.SortNo).ToList();
            var customText1 = elements.FirstOrDefault(it => it.FieldKey == "customText1")?.CustomText;
            var customText2 = elements.FirstOrDefault(it => it.FieldKey == "customText2")?.CustomText;
            if (!string.IsNullOrWhiteSpace(customText1)) values["customText1"] = customText1;
            if (!string.IsNullOrWhiteSpace(customText2)) values["customText2"] = customText2;
            return new JyPrintAgentLabelDto
            {
                Name = values.TryGetValue("goodsName", out var name) ? name : goods.StyleName,
                Sku = goods.GoodsNo,
                Price = goods.SalePrice.ToString("0.00", CultureInfo.InvariantCulture),
                Barcode = goods.Barcode,
                WidthMm = template.WidthMm,
                HeightMm = template.HeightMm,
                GapMm = template.GapMm,
                OffsetMm = template.OffsetMm,
                Copies = template.Copies,
                Port = template.Port,
                Speed = template.Speed,
                Density = template.Density,
                Elements = elements,
                Data = values.Where(it => !string.IsNullOrWhiteSpace(it.Value))
                    .ToDictionary(it => it.Key, it => it.Value, StringComparer.OrdinalIgnoreCase),
                Fields = elements.Select(it => new JyPrintFieldDto
                {
                    Key = it.Key, Label = it.Label, SortNo = it.SortNo, Enabled = 1,
                    Value = values.TryGetValue(it.Key, out var value) ? value : null
                }).Where(it => !string.IsNullOrWhiteSpace(it.Value)).ToList(),
                ShowBarcode = template.ShowBarcode,
                ShowQrCode = template.ShowQrCode
            };
        }

        public JyPrintAgentLabelDto RenderAgentLabelForInboundLine(JyActor actor, long inboundLineId, JyPrintTemplateDto template)
        {
            if (inboundLineId <= 0 || template == null) throw new CustomException("打印数据或模板不能为空");
            var goodsId = Context.Queryable<JyGoodsItem>()
                .Where(it => it.InboundLineId == inboundLineId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .OrderBy(it => it.Id).Select(it => it.Id).First();
            if (goodsId <= 0) throw new CustomException("入库明细尚未生成货品档案，暂时不能打印标签");
            return RenderAgentLabel(actor, goodsId, template);
        }

        private JyGoodsItem LoadPrintGoods(JyActor actor, long goodsItemId)
        {
            return Context.Queryable<JyGoodsItem>()
                .First(it => it.Id == goodsItemId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("商品不存在");
        }

        private Dictionary<string, string> PrintValues(JyActor actor, JyGoodsItem goods)
        {
            var supplier = goods.SupplierId.HasValue ? Context.Queryable<JySupplier>()
                .First(it => it.Id == goods.SupplierId.Value && it.TenantId == actor.TenantId)?.SupplierName : null;
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["goodsNo"] = goods.GoodsNo,
                ["barcode"] = goods.Barcode,
                ["goodsName"] = goods.StyleName,
                ["styleCategory"] = goods.StyleItemName,
                ["material"] = goods.MaterialName,
                ["metal"] = goods.MetalName,
                ["weight"] = goods.Weight.HasValue ? goods.Weight.Value.ToString("0.###") + " g" : null,
                ["salePrice"] = "¥" + goods.SalePrice.ToString("0.00"),
                ["costPrice"] = "¥" + goods.CostPrice.ToString("0.00"),
                ["certType"] = goods.CertTypeName,
                ["certNo"] = goods.CertNo,
                ["supplier"] = supplier,
                ["inboundDate"] = goods.Create_time.ToString("yyyy-MM-dd"),
                ["storeNote"] = goods.Remark,
                ["customText1"] = null,
                ["customText2"] = null
            };
        }

        private List<JyPrintTemplate> LoadTemplates(string tenantId)
        {
            return Context.Queryable<JyPrintTemplate>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0)
                .OrderBy(it => it.Status).OrderBy(it => it.Id, OrderByType.Desc).ToList();
        }

        private List<JyPrintTemplateBinding> LoadBindings(string tenantId)
        {
            return Context.Queryable<JyPrintTemplateBinding>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0).ToList();
        }

        private JyPrintTemplate MustTemplate(string tenantId, long id)
        {
            return Context.Queryable<JyPrintTemplate>().First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("打印模板不存在");
        }

        private static JyPrintTemplateDto ToTemplateDto(JyPrintTemplate template)
        {
            var fields = new List<JyPrintFieldDto>();
            var layout = new PrintLayoutFile();
            try
            {
                var json = template.FieldsJson ?? "[]";
                if (json.TrimStart().StartsWith("{", StringComparison.Ordinal))
                {
                    layout = JsonConvert.DeserializeObject<PrintLayoutFile>(json) ?? new PrintLayoutFile();
                    fields = layout.Elements ?? new List<JyPrintFieldDto>();
                }
                else
                {
                    fields = JsonConvert.DeserializeObject<List<JyPrintFieldDto>>(json) ?? new List<JyPrintFieldDto>();
                }
            }
            catch (JsonException) { }
            return new JyPrintTemplateDto
            {
                Id = template.Id,
                TemplateName = template.TemplateName,
                WidthMm = template.WidthMm,
                HeightMm = template.HeightMm,
                GapMm = layout.GapMm > 0 ? layout.GapMm : 2,
                OffsetMm = layout.OffsetMm >= 0 ? layout.OffsetMm : 4,
                Copies = layout.Copies > 0 ? layout.Copies : 1,
                Port = string.IsNullOrWhiteSpace(layout.Port) ? "USB," : layout.Port,
                Speed = layout.Speed > 0 ? layout.Speed : 4,
                Density = layout.Density > 0 ? layout.Density : 8,
                ShowBarcode = template.ShowBarcode,
                ShowQrCode = template.ShowQrCode,
                Fields = LayoutFields(fields, template.WidthMm, template.HeightMm).OrderBy(it => it.SortNo).ToList(),
                Remark = template.Remark,
                Status = template.Status,
                UpdateTime = template.Update_time
            };
        }

        private static List<JyPrintFieldDto> NormalizeFields(List<JyPrintFieldDto> fields, decimal widthMm, decimal heightMm)
        {
            var source = (fields ?? new List<JyPrintFieldDto>()).Where(it => it.Enabled != 0).ToList();
            if (source.Count == 0) throw new CustomException("模板至少放置一个打印元素");
            if (source.Count > 48) throw new CustomException("单个模板最多放置 48 个元素");
            var normalized = new List<JyPrintFieldDto>();
            foreach (var item in source)
            {
                var key = (item.FieldKey ?? item.Key ?? "").Trim();
                var catalog = PrintFields.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
                if (catalog == null) throw new CustomException("打印元素引用了不存在的商品字段：" + key);
                var type = (item.Type ?? "text").Trim().ToLowerInvariant();
                if (type is not ("text" or "barcode" or "qrcode"))
                    throw new CustomException("打印元素类型只能是 text、barcode 或 qrcode");
                if (item.X < -5 || item.Y < -5 || item.X > widthMm + 5 || item.Y > heightMm + 5)
                    throw new CustomException("打印元素坐标超出标签范围");
                if (item.Width <= 0 || item.Height <= 0 || item.Width > widthMm || item.Height > heightMm)
                    throw new CustomException("打印元素宽高必须大于 0 且不能超过标签尺寸");
                if ((item.CustomText ?? "").Length > 80)
                    throw new CustomException("自定义文本不能超过 80 个字");
                var rotation = item.Rotation is 0 or 90 or 180 or 270 ? item.Rotation : 0;
                normalized.Add(new JyPrintFieldDto
                {
                    Key = catalog.Key,
                    FieldKey = catalog.Key,
                    Label = string.IsNullOrWhiteSpace(item.Label) ? "" : item.Label.Trim(),
                    CustomText = string.IsNullOrWhiteSpace(item.CustomText) ? null : item.CustomText.Trim(),
                    Type = type,
                    X = Math.Round(item.X, 2),
                    Y = Math.Round(item.Y, 2),
                    Width = Math.Round(item.Width, 2),
                    Height = Math.Round(item.Height, 2),
                    FontSize = Math.Clamp(item.FontSize <= 0 ? 16 : item.FontSize, 5, 72),
                    Bold = item.Bold,
                    Rotation = rotation,
                    Enabled = 1,
                    SortNo = normalized.Count + 1
                });
            }
            return normalized;
        }

        private static List<JyPrintFieldDto> LayoutFields(List<JyPrintFieldDto> fields, decimal widthMm, decimal heightMm)
        {
            var list = fields ?? new List<JyPrintFieldDto>();
            if (list.Count == 0) return list;
            if (list.Any(it => it.Width > 0 && it.Height > 0)) 
                return list.Select(it => new JyPrintFieldDto
                {
                    Key = it.Key, FieldKey = string.IsNullOrWhiteSpace(it.FieldKey) ? it.Key : it.FieldKey,
                    Label = it.Label, CustomText = it.CustomText, Type = string.IsNullOrWhiteSpace(it.Type) ? "text" : it.Type,
                    X = it.X, Y = it.Y, Width = it.Width, Height = it.Height,
                    FontSize = it.FontSize <= 0 ? 16 : it.FontSize, Bold = it.Bold,
                    Rotation = it.Rotation, Enabled = it.Enabled, SortNo = it.SortNo, Value = it.Value
                }).ToList();
            var rowHeight = Math.Max(4m, Math.Min(7m, (heightMm - 4m) / Math.Max(1, list.Count)));
            return list.Select((it, index) => new JyPrintFieldDto
            {
                Key = it.Key, FieldKey = string.IsNullOrWhiteSpace(it.FieldKey) ? it.Key : it.FieldKey,
                Label = it.Label, CustomText = it.CustomText, Type = "text", X = 2, Y = 2 + index * rowHeight,
                Width = Math.Max(5m, widthMm - 4m), Height = rowHeight,
                FontSize = index == 0 ? 18 : 14, Bold = index == 0,
                Rotation = 0, Enabled = it.Enabled, SortNo = index + 1, Value = it.Value
            }).ToList();
        }

        private static string NormalizePort(string port)
        {
            var value = string.IsNullOrWhiteSpace(port) ? "USB," : port.Trim();
            if (value.Equals("USB", StringComparison.OrdinalIgnoreCase)) return "USB,";
            if (value.StartsWith("USB,", StringComparison.OrdinalIgnoreCase) && value.Length <= 80) return value;
            if (value.StartsWith("NET,", StringComparison.OrdinalIgnoreCase) && value.Length <= 80) return value;
            if (Regex.IsMatch(value, @"^COM\d{1,2},\d{3,7}$", RegexOptions.IgnoreCase)) return value;
            throw new CustomException("通讯端口只支持 USB,、NET,地址 或 COM口,波特率");
        }

        private sealed class PrintLayoutFile
        {
            public int Version { get; set; }
            public decimal GapMm { get; set; } = 2;
            public decimal OffsetMm { get; set; } = 4;
            public int Copies { get; set; } = 1;
            public string Port { get; set; } = "USB,";
            public int Speed { get; set; } = 4;
            public int Density { get; set; } = 8;
            public List<JyPrintFieldDto> Elements { get; set; } = new();
        }

        private static (DateTime Date, int DaysUntil, bool IsLunar)? ResolveUpcomingBirthday(JyMember member, DateTime today)
        {
            if (member.BirthCalendar == 2 || (!member.SolarBirthday.HasValue && !string.IsNullOrWhiteSpace(member.LunarBirthday)))
            {
                var lunar = NextLunarBirthday(member.LunarBirthday, today);
                if (lunar.HasValue) return (lunar.Value, (lunar.Value - today).Days, true);
            }
            if (member.SolarBirthday.HasValue)
            {
                var source = member.SolarBirthday.Value;
                var date = SafeDate(today.Year, source.Month, source.Day);
                if (date < today) date = SafeDate(today.Year + 1, source.Month, source.Day);
                return (date, (date - today).Days, false);
            }
            return null;
        }

        private static DateTime? NextLunarBirthday(string text, DateTime today)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = Regex.Match(text.Trim(), @"(?:(?<leap>闰)\s*)?(?<month>正|一|二|三|四|五|六|七|八|九|十|十一|十二|冬|腊)\s*月\s*(?<day>初[一二三四五六七八九十]|十[一二三四五六七八九]?|二十[一二三四五六七八九]?|三十|[1-9]|[12]\d|30)");
            if (!match.Success) return null;
            var dayText = match.Groups["day"].Value;
            if (!LunarMonths.TryGetValue(match.Groups["month"].Value, out var month)) return null;
            var day = ParseLunarDay(dayText);
            if (day <= 0) return null;
            var leap = match.Groups["leap"].Success;
            var startYear = LunarCalendar.GetYear(today) - 1;
            for (var year = startYear; year <= startYear + 2; year++)
            {
                var date = TryLunarDate(year, month, day, leap);
                if (!date.HasValue) continue;
                if (date.Value >= today) return date;
            }
            return null;
        }

        private static DateTime? TryLunarDate(int year, int month, int day, bool leap)
        {
            try
            {
                var leapMonth = LunarCalendar.GetLeapMonth(year);
                var actualMonth = month;
                if (leapMonth > 0)
                {
                    if (leap && leapMonth != month + 1) return null;
                    if (month >= leapMonth) actualMonth++;
                }
                if (day > LunarCalendar.GetDaysInMonth(year, actualMonth)) return null;
                return LunarCalendar.ToDateTime(year, actualMonth, day, 0, 0, 0, 0);
            }
            catch (ArgumentOutOfRangeException) { return null; }
        }

        private static int ParseLunarDay(string text)
        {
            if (int.TryParse(text, out var plain)) return plain;
            if (text.StartsWith("初")) return Math.Max(1, "一二三四五六七八九十".IndexOf(text[1..2]) + 1);
            if (text == "十") return 10;
            if (text.StartsWith("十")) return 10 + Math.Max(0, "一二三四五六七八九".IndexOf(text[1..2]) + 1);
            if (text.StartsWith("二十")) return text == "二十" ? 20 : 20 + Math.Max(0, "一二三四五六七八九".IndexOf(text[2..3]) + 1);
            if (text == "三十") return 30;
            return 0;
        }

        private static DateTime SafeDate(int year, int month, int day)
        {
            return new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
        }

        private StyleCatalog LoadStyleCatalog(string tenantId)
        {
            var typeIds = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == tenantId && it.TypeCode == "style" && it.DelFlag == 0)
                .Select(it => it.Id)
                .ToList();
            var items = typeIds.Count == 0 ? new List<JyLookupItem>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && typeIds.Contains(it.TypeId) && it.DelFlag == 0)
                .OrderBy(it => it.SortNo).OrderBy(it => it.Id)
                .ToList();

            // 款式字典可能被重复初始化，按“名称 + 父级名称”归并成唯一的大类和子类。
            var roots = items.Where(it => !it.ParentId.HasValue)
                .GroupBy(it => it.ItemName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(it => it.OrderBy(r => r.SortNo).ThenBy(r => r.Id).First())
                .OrderBy(it => it.SortNo).ThenBy(it => it.Id)
                .ToList();
            var rootByName = roots.ToDictionary(it => it.ItemName.Trim(), StringComparer.OrdinalIgnoreCase);
            var rootByOldId = roots.ToDictionary(it => it.Id);
            var childByNameAndParent = items.Where(it => it.ParentId.HasValue)
                .Select(it => new
                {
                    Item = it,
                    Parent = items.FirstOrDefault(p => p.Id == it.ParentId.Value)
                })
                .Where(it => it.Parent != null && rootByName.ContainsKey(it.Parent.ItemName.Trim()))
                .GroupBy(it => rootByName[it.Parent.ItemName.Trim()].Id + "|" + it.Item.ItemName.Trim())
                .Select(it => it.OrderBy(r => r.Item.SortNo).ThenBy(r => r.Item.Id).First())
                .ToList();

            var rootIdByItemId = new Dictionary<long, long>();
            foreach (var root in roots) rootIdByItemId[root.Id] = root.Id;
            foreach (var child in childByNameAndParent)
            {
                rootIdByItemId[child.Item.Id] = rootByName[child.Parent.ItemName.Trim()].Id;
            }

            var goods = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && it.Status == 1)
                .Select(it => new { it.StyleItemId, it.Qty, it.StyleItemName })
                .ToList();
            var qtyByRootId = new Dictionary<long, decimal>();
            var totalQty = 0m;
            var unclassifiedQty = 0m;
            foreach (var row in goods)
            {
                totalQty += row.Qty;
                var rootId = row.StyleItemId.HasValue && rootIdByItemId.TryGetValue(row.StyleItemId.Value, out var mapped)
                    ? mapped
                    : roots.FirstOrDefault(r => r.ItemName == row.StyleItemName)?.Id ?? 0;
                if (rootId <= 0)
                {
                    unclassifiedQty += row.Qty;
                    continue;
                }
                qtyByRootId[rootId] = qtyByRootId.TryGetValue(rootId, out var qty) ? qty + row.Qty : row.Qty;
            }

            return new StyleCatalog(roots, rootIdByItemId, qtyByRootId, totalQty, unclassifiedQty);
        }

        private static int StockAlertLevel(decimal current, decimal minQty)
        {
            if (minQty <= 0) return 0;
            if (current < minQty) return 2;
            return current <= minQty + 2 ? 1 : 0;
        }

        private static string NormalizeText(string value, string label, int max, bool required)
        {
            value = value?.Trim();
            if (required && string.IsNullOrEmpty(value)) throw new CustomException(label + "不能为空");
            if (value?.Length > max) throw new CustomException(label + "不能超过 " + max + " 个字");
            return value;
        }

        private static string NormalizeRemark(string value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > 500) throw new CustomException("备注不能超过 500 个字");
            return value;
        }

        private static string Html(string value)
        {
            return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}
