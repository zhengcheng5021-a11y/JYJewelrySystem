using Infrastructure;
using Infrastructure.Attribute;
using Infrastructure.Cache;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyLookupService : BaseService<JyLookupType>
    {
        private const int OptionsCacheMinutes = 24 * 60;
        private const int OptionsVersionCacheMinutes = 365 * 24 * 60;
        private sealed record TypeSeed(string Code, string Name, int LevelLimit, int SortNo);
        private sealed record ItemSeed(string Code, string Name, string ParentCode, int SortNo, int IsPreset);

        private static readonly TypeSeed[] PresetTypes =
        [
            new("style", "款式分类", 2, 1),
            new("material", "材质分类", 1, 2),
            new("metal", "金属成色", 1, 3),
            new("cert_type", "证书类型", 1, 4),
            new("coupon_type", "卡券类型", 1, 5),
            new("follow_way", "跟进方式", 1, 6),
            new("material_category", "物料分类", 2, 7)
        ];

        private static readonly ItemSeed[] StyleSamples =
        [
            new("silver", "银饰", null, 1, 0),
            new("bracelet", "手串", null, 2, 0),
            new("necklace", "项链", null, 3, 0),
            new("sandalwood", "檀木", null, 4, 0),
            new("silver-ring", "戒指", "silver", 1, 0),
            new("silver-bangle", "手镯", "silver", 2, 0),
            new("silver-earring", "耳饰", "silver", 3, 0),
            new("bracelet-bead", "珠串", "bracelet", 1, 0),
            new("necklace-pendant", "吊坠项链", "necklace", 1, 0),
            new("sandalwood-ornament", "檀木饰品", "sandalwood", 1, 0)
        ];

        private static readonly ItemSeed[] CouponSeeds =
        [
            new("discount", "折扣券", null, 1, 1),
            new("cash", "代金券", null, 2, 1),
            new("gift", "礼品券", null, 3, 1)
        ];

        private static readonly ItemSeed[] FollowWaySeeds =
        [
            new("wechat", "微信沟通", null, 1, 1),
            new("phone", "电话沟通", null, 2, 1),
            new("store", "邀约到店", null, 3, 1),
            new("moments", "朋友圈互动", null, 4, 1),
            new("sms", "短信触达", null, 5, 1),
            new("other", "其他", null, 6, 1)
        ];

        public void EnsureFoundation(JyActor actor)
        {
            var createdStyle = false;
            foreach (var seed in PresetTypes)
            {
                var exists = Context.Queryable<JyLookupType>()
                    .Any(it => it.TenantId == actor.TenantId && it.TypeCode == seed.Code && it.DelFlag == 0);
                if (exists)
                {
                    continue;
                }
                Context.Insertable(new JyLookupType
                {
                    TenantId = actor.TenantId,
                    TypeCode = seed.Code,
                    TypeName = seed.Name,
                    LevelLimit = seed.LevelLimit,
                    IsPreset = 1,
                    Status = 0,
                    SortNo = seed.SortNo,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = seed.Code == "style" ? "大类和子级两层。初始分类可改可删。" : null
                }).ExecuteCommand();
                createdStyle = createdStyle || seed.Code == "style";
            }

            var style = FindType(actor.TenantId, "style");
            if (style != null && createdStyle)
            {
                InsertItems(actor, style, StyleSamples);
            }
            else if (style != null)
            {
                var anyHistory = Context.Queryable<JyLookupItem>()
                    .Any(it => it.TenantId == actor.TenantId && it.TypeId == style.Id);
                if (!anyHistory)
                {
                    InsertItems(actor, style, StyleSamples);
                }
            }


            var coupon = FindType(actor.TenantId, "coupon_type");
            if (coupon != null)
            {
                foreach (var seed in CouponSeeds)
                {
                    var exists = Context.Queryable<JyLookupItem>()
                        .Any(it => it.TenantId == actor.TenantId && it.TypeId == coupon.Id && it.ItemCode == seed.Code && it.DelFlag == 0);
                    if (!exists)
                    {
                        InsertItems(actor, coupon, [seed]);
                    }
                }
            }

            var followWay = FindType(actor.TenantId, "follow_way");
            if (followWay != null)
            {
                foreach (var seed in FollowWaySeeds)
                {
                    var exists = Context.Queryable<JyLookupItem>()
                        .Any(it => it.TenantId == actor.TenantId && it.TypeId == followWay.Id && it.ItemCode == seed.Code && it.DelFlag == 0);
                    if (!exists) InsertItems(actor, followWay, [seed]);
                }
            }
        }

        public List<JyLookupTypeDto> ListTypes(JyActor actor)
        {
            EnsureFoundation(actor);
            var types = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList();
            var allItems = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ToList();
            var counts = allItems.GroupBy(it => it.TypeId).ToDictionary(it => it.Key, it => it.Count());
            return types
                .Select(it => new JyLookupTypeDto
                {
                    Id = it.Id,
                    TypeCode = it.TypeCode,
                    TypeName = it.TypeName,
                    LevelLimit = it.LevelLimit,
                    IsPreset = it.IsPreset,
                    Status = it.Status,
                    SortNo = it.SortNo,
                    Remark = it.Remark,
                    ItemCount = counts.GetValueOrDefault(it.Id)
                })
                .OrderBy(it => it.SortNo)
                .ThenBy(it => it.Id)
                .ToList();
        }

        public long SaveType(JyActor actor, JyLookupTypeEditDto dto)
        {
            dto.TypeName = JyBillNo.NormalizeName(dto.TypeName, "字典名称", 50);
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.LevelLimit != 1 && dto.LevelLimit != 2)
            {
                throw new CustomException("字典只允许一层或两层");
            }
            if (dto.Status != 0 && dto.Status != 1)
            {
                throw new CustomException("状态不正确");
            }

            if (dto.Id <= 0)
            {
                dto.TypeCode = JyBillNo.NormalizeCode(dto.TypeCode, "字典编码");
                if (PresetTypes.Any(it => it.Code == dto.TypeCode))
                {
                    throw new CustomException("该编码是系统预置字典，不能重复新增");
                }
                EnsureCodeFree(actor.TenantId, dto.TypeCode, 0);
                var createdId = Context.Insertable(new JyLookupType
                {
                    TenantId = actor.TenantId,
                    TypeCode = dto.TypeCode,
                    TypeName = dto.TypeName,
                    LevelLimit = dto.LevelLimit,
                    IsPreset = 0,
                    Status = dto.Status,
                    SortNo = dto.SortNo,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = dto.Remark
                }).ExecuteReturnBigIdentity();
                InvalidateOptions(actor.TenantId);
                return createdId;
            }

            var current = MustType(actor.TenantId, dto.Id);
            if (current.IsPreset == 1)
            {
                dto.TypeCode = current.TypeCode;
                dto.LevelLimit = current.LevelLimit;
            }
            else
            {
                dto.TypeCode = JyBillNo.NormalizeCode(dto.TypeCode, "字典编码");
                EnsureCodeFree(actor.TenantId, dto.TypeCode, current.Id);
                if (dto.LevelLimit < current.LevelLimit && HasChild(actor.TenantId, current.Id))
                {
                    throw new CustomException("已有子级，不能改成一层");
                }
            }

            current.TypeCode = dto.TypeCode;
            current.TypeName = dto.TypeName;
            current.LevelLimit = dto.LevelLimit;
            current.Status = dto.Status;
            current.SortNo = dto.SortNo;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current)
                .UpdateColumns(it => new { it.TypeCode, it.TypeName, it.LevelLimit, it.Status, it.SortNo, it.Remark, it.Update_by, it.Update_time })
                .ExecuteCommand();
            InvalidateOptions(actor.TenantId);
            return current.Id;
        }

        public int DeleteType(JyActor actor, long id)
        {
            var current = MustType(actor.TenantId, id);
            if (current.IsPreset == 1)
            {
                throw new CustomException("预置字典不能删除，可以停用");
            }
            if (Context.Queryable<JyLookupItem>().Any(it => it.TenantId == actor.TenantId && it.TypeId == id && it.DelFlag == 0))
            {
                throw new CustomException("请先删除该字典下的数据");
            }
            var deleted = MarkDeleted(actor, id, true);
            InvalidateOptions(actor.TenantId);
            return deleted;
        }

        public List<JyLookupItemDto> ListItems(JyActor actor, long typeId)
        {
            EnsureFoundation(actor);
            var type = MustType(actor.TenantId, typeId);
            var items = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && it.TypeId == type.Id && it.DelFlag == 0)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList();
            var lookup = items.ToDictionary(it => it.Id, it => ToDto(it, null));
            var roots = new List<JyLookupItemDto>();
            foreach (var item in items)
            {
                var dto = lookup[item.Id];
                if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
                {
                    dto.ParentName = parent.ItemName;
                    parent.Children ??= [];
                    parent.Children.Add(dto);
                }
                else
                {
                    roots.Add(dto);
                }
            }
            foreach (var dto in lookup.Values.Where(it => it.Children != null && it.Children.Count == 0))
            {
                dto.Children = null;
            }
            return roots;
        }

        public List<JyLookupOptionDto> Options(JyActor actor, string typeCode, bool onlyEnabled = false)
        {
            typeCode = (typeCode ?? string.Empty).Trim();
            if (typeCode.Length == 0) return [];

            var cache = CacheStore.Default;
            var versionKey = OptionsVersionKey(actor.TenantId);
            var version = cache.Exists(versionKey) ? cache.Get<long>(versionKey) : 0L;
            var cacheKey = OptionsCacheKey(actor.TenantId, version, typeCode, onlyEnabled);
            var cached = cache.Get<List<JyLookupOptionDto>>(cacheKey);
            if (cached != null) return cached;

            EnsureFoundation(actor);
            var type = FindType(actor.TenantId, typeCode);
            if (type == null)
            {
                return [];
            }
            var itemQuery = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0);
            if (type.LevelLimit > 1)
            {
                // 同名字典类型可能因重复初始化存在多份，层级字典需合并展示，避免旧分类丢失。
                var typeIds = Context.Queryable<JyLookupType>()
                    .Where(it => it.TenantId == actor.TenantId && it.TypeCode == type.TypeCode && it.DelFlag == 0)
                    .Select(it => it.Id)
                    .ToList();
                itemQuery = itemQuery.Where(it => typeIds.Contains(it.TypeId));
            }
            else
            {
                itemQuery = itemQuery.Where(it => it.TypeId == type.Id);
            }
            var items = itemQuery
                .WhereIF(onlyEnabled, it => it.Status == 0)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList();
            var itemIds = items.Select(it => it.Id).ToHashSet();
            items = items.Where(it => !it.ParentId.HasValue || itemIds.Contains(it.ParentId.Value)).ToList();
            var result = items.Select(it => new JyLookupOptionDto
            {
                Id = it.Id,
                ParentId = it.ParentId,
                ItemCode = it.ItemCode,
                ItemName = it.ItemName,
                Level = it.ParentId.HasValue ? 2 : 1
            }).ToList();
            var currentVersion = cache.Exists(versionKey) ? cache.Get<long>(versionKey) : 0L;
            if (currentVersion == version)
                cache.Set(cacheKey, result, OptionsCacheMinutes);
            return result;
        }

        public long SaveItem(JyActor actor, JyLookupItemEditDto dto)
        {
            var type = MustType(actor.TenantId, dto.TypeId);
            dto.ItemName = JyBillNo.NormalizeName(dto.ItemName, "名称", 100);
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.Status != 0 && dto.Status != 1)
            {
                throw new CustomException("状态不正确");
            }
            if (dto.ParentId is 0)
            {
                dto.ParentId = null;
            }

            JyLookupItem current = null;
            if (dto.Id > 0)
            {
                current = MustItem(actor.TenantId, dto.Id);
                if (current.TypeId != type.Id)
                {
                    throw new CustomException("不能把数据改到另一个字典");
                }
                dto.ItemCode = current.IsPreset == 1 ? current.ItemCode : JyBillNo.NormalizeCode(dto.ItemCode, "编码");
            }
            else
            {
                dto.ItemCode = JyBillNo.NormalizeCode(dto.ItemCode, "编码");
            }
            EnsureItemCodeFree(actor.TenantId, type.Id, dto.ItemCode, current?.Id ?? 0);
            ValidateParent(actor, type, current, dto.ParentId);

            if (current == null)
            {
                var createdId = Context.Insertable(new JyLookupItem
                {
                    TenantId = actor.TenantId,
                    TypeId = type.Id,
                    ParentId = dto.ParentId,
                    ItemCode = dto.ItemCode,
                    ItemName = dto.ItemName,
                    IsPreset = 0,
                    Status = dto.Status,
                    SortNo = dto.SortNo,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = dto.Remark
                }).ExecuteReturnBigIdentity();
                InvalidateOptions(actor.TenantId);
                return createdId;
            }

            current.ParentId = dto.ParentId;
            current.ItemCode = dto.ItemCode;
            current.ItemName = dto.ItemName;
            current.Status = dto.Status;
            current.SortNo = dto.SortNo;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current)
                .UpdateColumns(it => new { it.ParentId, it.ItemCode, it.ItemName, it.Status, it.SortNo, it.Remark, it.Update_by, it.Update_time })
                .ExecuteCommand();
            InvalidateOptions(actor.TenantId);
            return current.Id;
        }

        public int DeleteItem(JyActor actor, long id)
        {
            var current = MustItem(actor.TenantId, id);
            if (current.IsPreset == 1)
            {
                throw new CustomException("预置数据不能删除，可以停用");
            }
            if (Context.Queryable<JyLookupItem>().Any(it => it.TenantId == actor.TenantId && it.ParentId == id && it.DelFlag == 0))
            {
                throw new CustomException("请先删除子级");
            }
            if (Context.Queryable<JySalesScript>().Any(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                && (it.StyleItemId == id || it.MaterialItemId == id || it.MetalItemId == id)))
            {
                throw new CustomException("销售话术正在使用这项数据，不能删除");
            }
            var deleted = MarkDeleted(actor, id, false);
            InvalidateOptions(actor.TenantId);
            return deleted;
        }

        public JyLookupItem MustActiveItem(string tenantId, long id, string typeCode, string label)
        {
            var type = FindType(tenantId, typeCode) ?? throw new CustomException($"还没有{label}字典");
            var item = Context.Queryable<JyLookupItem>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.TypeId == type.Id && it.DelFlag == 0);
            if (item == null)
            {
                throw new CustomException($"{label}不存在或不属于当前字典");
            }
            return item;
        }

        private void ValidateParent(JyActor actor, JyLookupType type, JyLookupItem current, long? parentId)
        {
            if (type.LevelLimit <= 1)
            {
                if (parentId.HasValue)
                {
                    throw new CustomException("这个字典只有一层，不能选择上级");
                }
                return;
            }
            if (!parentId.HasValue)
            {
                if (current != null && HasChildren(actor.TenantId, current.Id) && current.ParentId.HasValue)
                {
                    throw new CustomException("已有子级的数据必须留在大类");
                }
                return;
            }
            if (current != null && parentId.Value == current.Id)
            {
                throw new CustomException("不能选择自己作为上级");
            }
            if (current != null && HasChildren(actor.TenantId, current.Id))
            {
                throw new CustomException("已有子级的大类不能再挂到别的大类下面");
            }
            var parent = MustItem(actor.TenantId, parentId.Value);
            if (parent.TypeId != type.Id || parent.ParentId.HasValue)
            {
                throw new CustomException("上级必须是同一字典下的大类，不能形成第三层");
            }
        }

        private bool HasChild(string tenantId, long typeId)
        {
            return Context.Queryable<JyLookupItem>()
                .Any(it => it.TenantId == tenantId && it.TypeId == typeId && it.ParentId != null && it.DelFlag == 0);
        }

        private bool HasChildren(string tenantId, long id)
        {
            return Context.Queryable<JyLookupItem>()
                .Any(it => it.TenantId == tenantId && it.ParentId == id && it.DelFlag == 0);
        }

        private void InsertItems(JyActor actor, JyLookupType type, IEnumerable<ItemSeed> seeds)
        {
            foreach (var seed in seeds.Where(it => it.ParentCode == null))
            {
                InsertItem(actor, type.Id, null, seed);
            }
            foreach (var seed in seeds.Where(it => it.ParentCode != null))
            {
                var parent = Context.Queryable<JyLookupItem>().First(it =>
                    it.TenantId == actor.TenantId && it.TypeId == type.Id && it.ItemCode == seed.ParentCode && it.DelFlag == 0);
                if (parent == null)
                {
                    throw new CustomException($"缺少上级分类 {seed.ParentCode}");
                }
                InsertItem(actor, type.Id, parent.Id, seed);
            }
        }

        private void InsertItem(JyActor actor, long typeId, long? parentId, ItemSeed seed)
        {
            Context.Insertable(new JyLookupItem
            {
                TenantId = actor.TenantId,
                TypeId = typeId,
                ParentId = parentId,
                ItemCode = seed.Code,
                ItemName = seed.Name,
                IsPreset = seed.IsPreset,
                Status = 0,
                SortNo = seed.SortNo,
                DelFlag = 0,
                Create_by = actor.UserName,
                Create_time = DateTime.Now
            }).ExecuteCommand();
        }

        private void EnsureCodeFree(string tenantId, string code, long exceptId)
        {
            var used = Context.Queryable<JyLookupType>().Any(it =>
                it.TenantId == tenantId && it.TypeCode == code && it.DelFlag == 0 && it.Id != exceptId);
            if (used)
            {
                throw new CustomException("字典编码已存在");
            }
        }

        private void EnsureItemCodeFree(string tenantId, long typeId, string code, long exceptId)
        {
            var used = Context.Queryable<JyLookupItem>().Any(it =>
                it.TenantId == tenantId && it.TypeId == typeId && it.ItemCode == code && it.DelFlag == 0 && it.Id != exceptId);
            if (used)
            {
                throw new CustomException("编码已存在");
            }
        }

        private JyLookupType FindType(string tenantId, string code)
        {
            var types = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == tenantId && it.TypeCode == code && it.DelFlag == 0)
                .OrderBy(it => it.Id)
                .ToList();
            if (types.Count <= 1)
            {
                return types.FirstOrDefault();
            }

            // 历史数据可能重复初始化同名类型，优先取实际有字典项的版本，避免落到空分类。
            var typeIds = types.Select(it => it.Id).ToList();
            var counts = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && typeIds.Contains(it.TypeId))
                .GroupBy(it => it.TypeId)
                .Select(it => new { it.TypeId, Count = SqlFunc.AggregateCount(it.Id) })
                .ToList()
                .ToDictionary(it => it.TypeId, it => it.Count);
            return types.OrderByDescending(it => counts.GetValueOrDefault(it.Id)).ThenBy(it => it.Id).First();
        }

        private JyLookupType MustType(string tenantId, long id)
        {
            return Context.Queryable<JyLookupType>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("字典不存在");
        }

        private JyLookupItem MustItem(string tenantId, long id)
        {
            return Context.Queryable<JyLookupItem>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("字典数据不存在");
        }

        private static string OptionsVersionKey(string tenantId) => $"jy:lookup:options:version:{tenantId}";

        private static string OptionsCacheKey(string tenantId, long version, string typeCode, bool onlyEnabled)
            => $"jy:lookup:options:{tenantId}:{version}:{typeCode}:{(onlyEnabled ? 1 : 0)}";

        private static void InvalidateOptions(string tenantId)
        {
            CacheStore.Default.Increment(OptionsVersionKey(tenantId), OptionsVersionCacheMinutes);
        }

        private int MarkDeleted(JyActor actor, long id, bool type)
        {
            if (type)
            {
                return Context.Updateable<JyLookupType>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == DateTime.Now)
                    .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
            }

            return Context.Updateable<JyLookupItem>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        private static JyLookupItemDto ToDto(JyLookupItem item, string parentName)
        {
            return new JyLookupItemDto
            {
                Id = item.Id,
                TypeId = item.TypeId,
                ParentId = item.ParentId,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                IsPreset = item.IsPreset,
                Status = item.Status,
                SortNo = item.SortNo,
                Remark = item.Remark,
                ParentName = parentName
            };
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
