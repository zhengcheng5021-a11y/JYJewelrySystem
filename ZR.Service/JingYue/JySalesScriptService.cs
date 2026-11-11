using Infrastructure;
using Infrastructure.Attribute;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JySalesScriptService : BaseService<JySalesScript>
    {
        private readonly JyLookupService _lookup;

        public JySalesScriptService(JyLookupService lookup)
        {
            _lookup = lookup;
        }

        public PagedInfo<JyScriptDto> List(JyActor actor, JyScriptQueryDto query)
        {
            _lookup.EnsureFoundation(actor);
            query ??= new JyScriptQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var scriptType = NormalizeType(query.ScriptType, false);
            var q = Context.Queryable<JySalesScript>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is 0 or 1, it => it.Status == query.Status)
                .WhereIF(query.StyleItemId > 0, it => it.StyleItemId == query.StyleItemId)
                .WhereIF(!string.IsNullOrEmpty(scriptType), it => it.ScriptType == scriptType)
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.Title.Contains(keyword) || it.Content.Contains(keyword));
            var total = 0;
            // SortNo 复用为星级：0 视为默认 3 星，再按创建时间和 Id 倒序。
            var rows = q.OrderBy(it => it.SortNo == 0 ? 3 : it.SortNo, OrderByType.Desc)
                .OrderBy(it => it.Create_time, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            var names = NameMap(actor.TenantId, rows);
            return new PagedInfo<JyScriptDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(it => ToDto(it, names)).ToList()
            };
        }

        public long Save(JyActor actor, JyScriptEditDto dto)
        {
            _lookup.EnsureFoundation(actor);
            dto.Title = JyBillNo.NormalizeName(dto.Title, "话术标题", 100);
            dto.ScriptType = NormalizeType(dto.ScriptType, true);
            dto.Content = (dto.Content ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(dto.Content) || dto.Content.Length > 4000)
            {
                throw new CustomException("话术内容不能为空，且不能超过 4000 个字");
            }
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.Status != 0 && dto.Status != 1)
            {
                throw new CustomException("状态不正确");
            }
            if (dto.SortNo < 0 || dto.SortNo > 5)
            {
                throw new CustomException("话术星级必须在 1-5 星之间");
            }
            if (dto.SortNo == 0) dto.SortNo = 3;
            dto.StyleItemId = NullIfZero(dto.StyleItemId);
            dto.MaterialItemId = NullIfZero(dto.MaterialItemId);
            dto.MetalItemId = NullIfZero(dto.MetalItemId);

            JySalesScript current = null;
            if (dto.Id > 0)
            {
                current = Must(actor.TenantId, dto.Id);
            }
            EnsureRef(actor.TenantId, dto.StyleItemId, "style", "款式分类", true, current?.StyleItemId);
            EnsureRef(actor.TenantId, dto.MaterialItemId, "material", "材质分类", false, current?.MaterialItemId);
            EnsureRef(actor.TenantId, dto.MetalItemId, "metal", "金属成色", false, current?.MetalItemId);

            if (current == null)
            {
                return Context.Insertable(new JySalesScript
                {
                    TenantId = actor.TenantId,
                    StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                    Title = dto.Title,
                    ScriptType = dto.ScriptType,
                    StyleItemId = dto.StyleItemId,
                    MaterialItemId = dto.MaterialItemId,
                    MetalItemId = dto.MetalItemId,
                    Content = dto.Content,
                    Status = dto.Status,
                    SortNo = dto.SortNo,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = dto.Remark
                }).ExecuteReturnBigIdentity();
            }

            current.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
            current.Title = dto.Title;
            current.ScriptType = dto.ScriptType;
            current.StyleItemId = dto.StyleItemId;
            current.MaterialItemId = dto.MaterialItemId;
            current.MetalItemId = dto.MetalItemId;
            current.Content = dto.Content;
            current.Status = dto.Status;
            current.SortNo = dto.SortNo;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current)
                .UpdateColumns(it => new
                {
                    it.StoreId,
                    it.Title,
                    it.ScriptType,
                    it.StyleItemId,
                    it.MaterialItemId,
                    it.MetalItemId,
                    it.Content,
                    it.Status,
                    it.SortNo,
                    it.Remark,
                    it.Update_by,
                    it.Update_time
                })
                .ExecuteCommand();
            return current.Id;
        }

        public int Delete(JyActor actor, long id)
        {
            Must(actor.TenantId, id);
            return Context.Updateable<JySalesScript>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        private void EnsureRef(string tenantId, long? id, string typeCode, string label, bool allowParent, long? previous)
        {
            if (!id.HasValue)
            {
                return;
            }
            var item = _lookup.MustActiveItem(tenantId, id.Value, typeCode, label);
            if (!allowParent && item.ParentId.HasValue)
            {
                throw new CustomException($"{label}只能选择一层数据");
            }
            if (item.Status != 0 && id != previous)
            {
                throw new CustomException($"{label}已停用，不能新引用");
            }
        }

        private Dictionary<long, JyLookupItem> NameMap(string tenantId, IEnumerable<JySalesScript> rows)
        {
            var ids = rows.SelectMany(it => new[] { it.StyleItemId, it.MaterialItemId, it.MetalItemId })
                .Where(it => it.HasValue)
                .Select(it => it.Value)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<long, JyLookupItem>();
            }
            var items = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && ids.Contains(it.Id))
                .ToList();
            var parentIds = items.Where(it => it.ParentId.HasValue).Select(it => it.ParentId.Value).Distinct().ToList();
            if (parentIds.Count > 0)
            {
                var parents = Context.Queryable<JyLookupItem>()
                    .Where(it => it.TenantId == tenantId && parentIds.Contains(it.Id))
                    .ToList();
                items.AddRange(parents.Where(it => items.All(x => x.Id != it.Id)));
            }
            return items.ToDictionary(it => it.Id);
        }

        private static JyScriptDto ToDto(JySalesScript it, Dictionary<long, JyLookupItem> names)
        {
            return new JyScriptDto
            {
                Id = it.Id,
                StoreId = it.StoreId,
                Title = it.Title,
                ScriptType = it.ScriptType,
                StyleItemId = it.StyleItemId,
                MaterialItemId = it.MaterialItemId,
                MetalItemId = it.MetalItemId,
                StyleName = Display(names, it.StyleItemId),
                MaterialName = Display(names, it.MaterialItemId),
                MetalName = Display(names, it.MetalItemId),
                Content = it.Content,
                Status = it.Status,
                SortNo = it.SortNo is >= 1 and <= 5 ? it.SortNo : 3,
                Remark = it.Remark,
                CreateTime = it.Create_time
            };
        }

        private static string Display(Dictionary<long, JyLookupItem> names, long? id)
        {
            if (!id.HasValue || !names.TryGetValue(id.Value, out var item))
            {
                return null;
            }
            if (item.ParentId.HasValue && names.TryGetValue(item.ParentId.Value, out var parent))
            {
                return parent.ItemName + " / " + item.ItemName;
            }
            return item.ItemName;
        }

        private static long? NullIfZero(long? id) => id is null or 0 ? null : id;

        private static readonly string[] ScriptTypes = ["接待开场", "产品介绍", "异议处理", "成交推进", "售后维护", "其他"];

        private static string NormalizeType(string value, bool required)
        {
            value = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(value))
            {
                if (required) return "其他";
                return null;
            }
            if (!ScriptTypes.Contains(value)) throw new CustomException("话术类型不正确");
            return value;
        }

        private JySalesScript Must(string tenantId, long id)
        {
            return Context.Queryable<JySalesScript>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("销售话术不存在");
        }

        private static string TrimRemark(string remark)
        {
            remark = remark?.Trim();
            if (string.IsNullOrEmpty(remark))
            {
                return null;
            }
            if (remark.Length > 500)
            {
                throw new CustomException("备注不能超过 500 个字");
            }
            return remark;
        }
    }
}
