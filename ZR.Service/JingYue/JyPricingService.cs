using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>金价与会员折扣矩阵维护。销售计算只读取这里启用的有效记录。</summary>
    [AppService]
    public class JyPricingService : BaseService<JyMetalPrice>
    {
        private readonly JyLookupService _lookup;

        public JyPricingService(JyLookupService lookup)
        {
            _lookup = lookup;
        }

        public PagedInfo<JyMetalPriceDto> ListMetal(JyActor actor, JyMetalPriceQueryDto query)
        {
            query ??= new JyMetalPriceQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var q = Context.Queryable<JyMetalPrice>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.MetalItemId > 0, it => it.MetalItemId == query.MetalItemId)
                .WhereIF(query.Status is >= 0 and <= 1, it => it.Status == query.Status)
                .WhereIF(query.BeginTime.HasValue, it => it.EffectiveTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.EffectiveTime < query.EndTime.Value.Date.AddDays(1));
            var total = 0;
            var rows = q.OrderBy(it => it.EffectiveTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            var now = DateTime.Now;
            return new PagedInfo<JyMetalPriceDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(it => new JyMetalPriceDto
                {
                    Id = it.Id,
                    StoreId = it.StoreId,
                    MetalItemId = it.MetalItemId,
                    MetalName = it.MetalName,
                    PricePerGram = it.PricePerGram,
                    EffectiveTime = it.EffectiveTime,
                    ExpireTime = it.ExpireTime,
                    Status = it.Status,
                    Remark = it.Remark,
                    CreateTime = it.Create_time,
                    IsCurrent = it.Status == 0 && it.EffectiveTime <= now
                        && (!it.ExpireTime.HasValue || it.ExpireTime.Value > now)
                }).ToList()
            };
        }

        public long SaveMetal(JyActor actor, JyMetalPriceEditDto dto)
        {
            if (dto == null) throw new CustomException("金价信息不能为空");
            if (dto.MetalItemId <= 0) throw new CustomException("请选择金属成色");
            if (dto.PricePerGram <= 0 || dto.PricePerGram > 1000000m)
                throw new CustomException("每克价格必须大于 0 且不能过大");
            if (dto.Status is < 0 or > 1) throw new CustomException("金价状态不正确");
            if (dto.EffectiveTime == default) dto.EffectiveTime = DateTime.Now;
            if (dto.ExpireTime.HasValue && dto.ExpireTime.Value <= dto.EffectiveTime)
                throw new CustomException("失效时间必须晚于生效时间");

            _lookup.EnsureFoundation(actor);
            var metal = MustLookup(actor.TenantId, dto.MetalItemId, "metal", "金属成色");
            dto.PricePerGram = Round(dto.PricePerGram, 2);
            dto.Remark = Trim(dto.Remark, 500, "备注");
            var now = DateTime.Now;
            if (dto.Id > 0)
            {
                var current = MustMetal(actor.TenantId, dto.Id);
                current.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
                current.MetalItemId = metal.Id;
                current.MetalName = metal.ItemName;
                current.PricePerGram = dto.PricePerGram;
                current.EffectiveTime = dto.EffectiveTime;
                current.ExpireTime = dto.ExpireTime;
                current.Status = dto.Status;
                current.Remark = dto.Remark;
                current.Update_by = actor.UserName;
                current.Update_time = now;
                Context.Updateable(current)
                    .UpdateColumns(it => new
                    {
                        it.StoreId, it.MetalItemId, it.MetalName, it.PricePerGram,
                        it.EffectiveTime, it.ExpireTime, it.Status, it.Remark,
                        it.Update_by, it.Update_time
                    })
                    .Where(it => it.Id == current.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
                return current.Id;
            }
            var row = new JyMetalPrice
            {
                TenantId = actor.TenantId,
                StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                MetalItemId = metal.Id,
                MetalName = metal.ItemName,
                PricePerGram = dto.PricePerGram,
                EffectiveTime = dto.EffectiveTime,
                ExpireTime = dto.ExpireTime,
                Status = dto.Status,
                DelFlag = 0,
                Create_by = actor.UserName,
                Create_time = now,
                Remark = dto.Remark
            };
            return Context.Insertable(row).ExecuteReturnBigIdentity();
        }

        public int DeleteMetal(JyActor actor, long id)
        {
            var current = MustMetal(actor.TenantId, id);
            if (Context.Queryable<JySaleOrderItem>().Any(it => it.TenantId == actor.TenantId
                && it.MetalPriceId == current.Id))
                throw new CustomException("该金价已被销售单引用，不能删除；可以改为停用");
            return Context.Updateable<JyMetalPrice>()
                .SetColumns(it => it.DelFlag == 1)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        public PagedInfo<JyDiscountRuleDto> ListDiscount(JyActor actor, JyDiscountRuleQueryDto query)
        {
            query ??= new JyDiscountRuleQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var q = Context.Queryable<JyMemberDiscountRule>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.MemberLevelId > 0, it => it.MemberLevelId == query.MemberLevelId)
                .WhereIF(query.StyleItemId > 0, it => it.StyleItemId == query.StyleItemId)
                .WhereIF(query.PricingMode is >= 1 and <= 2, it => it.PricingMode == query.PricingMode)
                .WhereIF(query.Status is >= 0 and <= 1, it => it.Status == query.Status);
            var total = 0;
            var rows = q.OrderBy(it => it.MemberLevelId)
                .OrderBy(it => it.Priority, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JyDiscountRuleDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDiscountDtos(actor.TenantId, rows)
            };
        }

        public long SaveDiscount(JyActor actor, JyDiscountRuleEditDto dto)
        {
            if (dto == null) throw new CustomException("折扣规则不能为空");
            if (dto.MemberLevelId <= 0) throw new CustomException("请选择会员等级");
            if (dto.DiscountRate <= 0 || dto.DiscountRate > 1)
                throw new CustomException("折扣率必须大于 0 且不超过 1，例如 0.85 表示 8.5 折");
            if (dto.DiscountBase is < 1 or > 2) throw new CustomException("折扣基数不正确");
            if (dto.PricingMode.HasValue && dto.PricingMode is < 1 or > 2)
                throw new CustomException("计价方式不正确");
            if (dto.Status is < 0 or > 1) throw new CustomException("折扣状态不正确");
            if (dto.EffectiveTime.HasValue && dto.ExpireTime.HasValue
                && dto.ExpireTime.Value <= dto.EffectiveTime.Value)
                throw new CustomException("失效时间必须晚于生效时间");
            if (dto.StyleItemId <= 0) dto.StyleItemId = null;

            var level = Context.Queryable<JyMemberLevel>().First(it => it.Id == dto.MemberLevelId
                && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("会员等级不存在");
            if (dto.StyleItemId.HasValue)
            {
                _lookup.EnsureFoundation(actor);
                MustLookup(actor.TenantId, dto.StyleItemId.Value, "style", "款式分类");
            }
            var duplicateQuery = Context.Queryable<JyMemberDiscountRule>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0
                    && it.MemberLevelId == dto.MemberLevelId
                    && it.StyleItemId == dto.StyleItemId
                    && it.PricingMode == dto.PricingMode);
            if (dto.Id > 0) duplicateQuery = duplicateQuery.Where(it => it.Id != dto.Id);
            var duplicate = duplicateQuery.Any();
            if (duplicate)
                throw new CustomException("该会员等级、款式分类和计价方式已存在启用规则，请修改原规则");

            dto.Remark = Trim(dto.Remark, 500, "备注");
            var now = DateTime.Now;
            if (dto.Id > 0)
            {
                var current = MustDiscount(actor.TenantId, dto.Id);
                current.MemberLevelId = level.Id;
                current.StyleItemId = dto.StyleItemId;
                current.PricingMode = dto.PricingMode;
                current.DiscountRate = Round(dto.DiscountRate, 4);
                current.DiscountBase = dto.DiscountBase;
                current.Priority = dto.Priority;
                current.EffectiveTime = dto.EffectiveTime;
                current.ExpireTime = dto.ExpireTime;
                current.Status = dto.Status;
                current.Remark = dto.Remark;
                current.Update_by = actor.UserName;
                current.Update_time = now;
                Context.Updateable(current)
                    .UpdateColumns(it => new
                    {
                        it.MemberLevelId, it.StyleItemId, it.PricingMode, it.DiscountRate,
                        it.DiscountBase, it.Priority, it.EffectiveTime, it.ExpireTime,
                        it.Status, it.Remark, it.Update_by, it.Update_time
                    })
                    .Where(it => it.Id == current.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
                return current.Id;
            }
            return Context.Insertable(new JyMemberDiscountRule
            {
                TenantId = actor.TenantId,
                MemberLevelId = level.Id,
                StyleItemId = dto.StyleItemId,
                PricingMode = dto.PricingMode,
                DiscountRate = Round(dto.DiscountRate, 4),
                DiscountBase = dto.DiscountBase,
                Priority = dto.Priority,
                EffectiveTime = dto.EffectiveTime,
                ExpireTime = dto.ExpireTime,
                Status = dto.Status,
                DelFlag = 0,
                Create_by = actor.UserName,
                Create_time = now,
                Remark = dto.Remark
            }).ExecuteReturnBigIdentity();
        }

        public int DeleteDiscount(JyActor actor, long id)
        {
            MustDiscount(actor.TenantId, id);
            return Context.Updateable<JyMemberDiscountRule>()
                .SetColumns(it => it.DelFlag == 1)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        public JyPricingOptionsDto Options(JyActor actor)
        {
            _lookup.EnsureFoundation(actor);
            var levels = Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.SortNo)
                .ToList()
                .Select(it => new JyMemberLevelDto
                {
                    Id = it.Id,
                    LevelCode = it.LevelCode,
                    LevelName = it.LevelName,
                    MinPoints = it.MinPoints,
                    MaxPoints = it.MaxPoints,
                    DiscountRate = it.DiscountRate,
                    SortNo = it.SortNo,
                    KeepOnYearClear = it.KeepOnYearClear,
                    Status = it.Status,
                    Remark = it.Remark
                }).ToList();
            return new JyPricingOptionsDto
            {
                Metals = _lookup.Options(actor, "metal", true),
                Categories = _lookup.Options(actor, "style"),
                Levels = levels
            };
        }

        private List<JyDiscountRuleDto> ToDiscountDtos(string tenantId, List<JyMemberDiscountRule> rows)
        {
            if (rows.Count == 0) return new List<JyDiscountRuleDto>();
            var levelIds = rows.Select(it => it.MemberLevelId).Distinct().ToList();
            var levels = Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == tenantId && levelIds.Contains(it.Id))
                .ToList().ToDictionary(it => it.Id, it => it.LevelName);
            var styleIds = rows.Where(it => it.StyleItemId.HasValue)
                .Select(it => it.StyleItemId.Value).Distinct().ToList();
            var styles = styleIds.Count == 0 ? new Dictionary<long, JyLookupItem>()
                : Context.Queryable<JyLookupItem>()
                    .Where(it => it.TenantId == tenantId && styleIds.Contains(it.Id))
                    .ToList().ToDictionary(it => it.Id);
            var parentIds = styles.Values.Where(it => it.ParentId.HasValue)
                .Select(it => it.ParentId.Value).Distinct().ToList();
            var parents = parentIds.Count == 0 ? new Dictionary<long, JyLookupItem>()
                : Context.Queryable<JyLookupItem>()
                    .Where(it => it.TenantId == tenantId && parentIds.Contains(it.Id))
                    .ToList().ToDictionary(it => it.Id);
            return rows.Select(it =>
            {
                styles.TryGetValue(it.StyleItemId ?? 0, out var style);
                parents.TryGetValue(style?.ParentId ?? 0, out var parent);
                return new JyDiscountRuleDto
                {
                    Id = it.Id,
                    MemberLevelId = it.MemberLevelId,
                    MemberLevelName = levels.TryGetValue(it.MemberLevelId, out var name) ? name : null,
                    StyleItemId = it.StyleItemId,
                    StyleItemName = style?.ItemName,
                    StyleParentName = parent?.ItemName,
                    PricingMode = it.PricingMode,
                    DiscountRate = it.DiscountRate,
                    DiscountBase = it.DiscountBase,
                    Priority = it.Priority,
                    EffectiveTime = it.EffectiveTime,
                    ExpireTime = it.ExpireTime,
                    Status = it.Status,
                    Remark = it.Remark,
                    CreateTime = it.Create_time
                };
            }).ToList();
        }

        private JyMetalPrice MustMetal(string tenantId, long id)
            => Context.Queryable<JyMetalPrice>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("金价记录不存在");

        private JyMemberDiscountRule MustDiscount(string tenantId, long id)
            => Context.Queryable<JyMemberDiscountRule>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("折扣规则不存在");

        private JyLookupItem MustLookup(string tenantId, long id, string typeCode, string label)
        {
            var type = Context.Queryable<JyLookupType>()
                .First(it => it.TenantId == tenantId && it.TypeCode == typeCode && it.DelFlag == 0)
                ?? throw new CustomException($"{label}字典不存在");
            return Context.Queryable<JyLookupItem>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.TypeId == type.Id
                    && it.DelFlag == 0 && it.Status == 0)
                ?? throw new CustomException($"{label}不存在或已停用");
        }

        private static decimal Round(decimal value, int digits)
            => decimal.Round(value, digits, MidpointRounding.AwayFromZero);

        private static string Trim(string value, int max, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > max) throw new CustomException($"{label}不能超过 {max} 个字");
            return value;
        }
    }
}
