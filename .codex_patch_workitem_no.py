from pathlib import Path

root = Path(r"D:\work\其他文件\静悦平台\ZrAdminNetCore-master")
p = root / "ZR.Service/JingYue/JyWorkItemService.cs"
s = p.read_text(encoding="utf-8-sig")
s = s.replace("ItemNo = NextItemNo(actor.TenantId, now),", "ItemNo = NextItemNo(Context, actor.TenantId, now),")
s = s.replace(
    "public string NextItemNo(string tenantId, DateTime now)\n        {",
    "public string NextItemNo(ISqlSugarClient db, string tenantId, DateTime now)\n        {\n            if (db == null) throw new CustomException(\"待办编号生成失败：数据库连接不能为空\");"
)
s = s.replace("var table = Context.Ado.GetDataTable(sql,", "var table = db.Ado.GetDataTable(sql,")
p.write_text(s, encoding="utf-8")

p = root / "ZR.Service/JingYue/JyWorkTemplateService.cs"
s = p.read_text(encoding="utf-8-sig")
s = s.replace("_workItem.NextItemNo(actor.TenantId, now)", "_workItem.NextItemNo(Context, actor.TenantId, now)")
p.write_text(s, encoding="utf-8")
print("patched")
