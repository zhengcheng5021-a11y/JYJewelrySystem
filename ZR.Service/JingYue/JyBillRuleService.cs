using Infrastructure;
using Infrastructure.Attribute;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyBillRuleService : BaseService<JyBillRule>
    {
        private sealed record RuleSeed(string Type, string Name, string Prefix, int SortNo);

        private static readonly RuleSeed[] Presets =
        [
            new("inbound", "入库单", "RK", 1),
            new("sale", "正品销售", "XS", 2),
            new("gift", "赠送出库", "ZS", 3),
            new("sale_return", "销退单", "XT", 4),
            new("reserve", "预订单", "YD", 5),
            new("intent", "意向单", "YX", 6),
            new("stocktake", "盘点单", "PD", 7),
            new("goods", "货品编码", "", 8),
            new("member", "会员编号", "HY", 9),
            new("activity", "活动编号", "HD", 10),
            new("custom", "销售定制", "DZ", 11),
            new("material_inbound", "物料入库", "MR", 12),
            new("material_outbound", "物料出库", "MC", 13)
        ];

        public void EnsureFoundation(JyActor actor)
        {
            foreach (var seed in Presets)
            {
                var current = Context.Queryable<JyBillRule>()
                    .First(it => it.TenantId == actor.TenantId && it.BillType == seed.Type);
                if (current != null)
                {
                    // 历史库里的“商品编号”统一为“货品编码”；只纠正预置显示名称和无前缀规则，不覆盖用户流水。
                    if (seed.Type == "goods" && (current.BillName != seed.Name || !string.IsNullOrEmpty(current.Prefix)))
                    {
                        Context.Updateable<JyBillRule>()
                            .SetColumns(it => it.BillName == seed.Name)
                            .SetColumns(it => it.Prefix == string.Empty)
                            .Where(it => it.Id == current.Id && it.TenantId == actor.TenantId)
                            .ExecuteCommand();
                    }
                    continue;
                }
                Context.Insertable(new JyBillRule
                {
                    TenantId = actor.TenantId,
                    BillType = seed.Type,
                    BillName = seed.Name,
                    Prefix = seed.Prefix,
                    DatePattern = "yyyyMMdd",
                    SeqLength = 4,
                    ResetCycle = "day",
                    CurrentKey = string.Empty,
                    CurrentSeq = 0,
                    IsPreset = 1,
                    Status = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = "系统预置，可停用，不能删除"
                }).ExecuteCommand();
            }
        }

        public List<JyBillRuleDto> List(JyActor actor)
        {
            EnsureFoundation(actor);
            var now = DateTime.Now;
            return Context.Queryable<JyBillRule>()
                .Where(it => it.TenantId == actor.TenantId)
                .OrderBy(it => it.IsPreset, OrderByType.Desc)
                .OrderBy(it => it.Id)
                .ToList()
                .Select(it => ToDto(it, now))
                .ToList();
        }

        public long Save(JyActor actor, JyBillRuleEditDto dto)
        {
            EnsureFoundation(actor);
            dto.BillName = JyBillNo.NormalizeName(dto.BillName, "单据名称", 50);
            dto.Prefix = JyBillNo.NormalizePrefix(dto.Prefix);
            dto.DatePattern = JyBillNo.NormalizePattern(dto.DatePattern);
            dto.ResetCycle = JyBillNo.NormalizeCycle(dto.ResetCycle);
            dto.Remark = TrimRemark(dto.Remark);
            JyBillNo.ValidateRule(dto.Prefix, dto.DatePattern, dto.SeqLength, dto.ResetCycle);
            if (dto.Status != 0 && dto.Status != 1)
            {
                throw new CustomException("状态不正确");
            }

            if (dto.Id <= 0)
            {
                dto.BillType = JyBillNo.NormalizeCode(dto.BillType, "单据类型");
                if (Presets.Any(it => it.Type == dto.BillType))
                {
                    throw new CustomException("该单据类型是系统预置，不能重复新增");
                }
                EnsureTypeFree(actor.TenantId, dto.BillType, 0);
                return Context.Insertable(new JyBillRule
                {
                    TenantId = actor.TenantId,
                    BillType = dto.BillType,
                    BillName = dto.BillName,
                    Prefix = dto.Prefix,
                    DatePattern = dto.DatePattern,
                    SeqLength = dto.SeqLength,
                    ResetCycle = dto.ResetCycle,
                    CurrentKey = string.Empty,
                    CurrentSeq = 0,
                    IsPreset = 0,
                    Status = dto.Status,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now,
                    Remark = dto.Remark
                }).ExecuteReturnBigIdentity();
            }

            var current = Must(actor.TenantId, dto.Id);
            if (current.IsPreset == 1)
            {
                dto.BillType = current.BillType;
            }
            else
            {
                dto.BillType = JyBillNo.NormalizeCode(dto.BillType, "单据类型");
                EnsureTypeFree(actor.TenantId, dto.BillType, current.Id);
            }
            current.BillType = dto.BillType;
            current.BillName = dto.BillName;
            current.Prefix = dto.Prefix;
            current.DatePattern = dto.DatePattern;
            current.SeqLength = dto.SeqLength;
            current.ResetCycle = dto.ResetCycle;
            current.Status = dto.Status;
            current.Remark = dto.Remark;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current)
                .UpdateColumns(it => new
                {
                    it.BillType,
                    it.BillName,
                    it.Prefix,
                    it.DatePattern,
                    it.SeqLength,
                    it.ResetCycle,
                    it.Status,
                    it.Remark,
                    it.Update_by,
                    it.Update_time
                })
                .ExecuteCommand();
            return current.Id;
        }

        public int Delete(JyActor actor, long id)
        {
            var current = Must(actor.TenantId, id);
            if (current.IsPreset == 1)
            {
                throw new CustomException("预置单据规则不能删除，可以停用");
            }
            if (Context.Queryable<JyBillIssue>().Any(it => it.TenantId == actor.TenantId && it.RuleId == id))
            {
                throw new CustomException("已经取过号，不能删除规则");
            }
            return Context.Deleteable<JyBillRule>()
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.IsPreset == 0)
                .ExecuteCommand();
        }

        public string Preview(JyActor actor, string billType)
        {
            EnsureFoundation(actor);
            var rule = MustType(actor.TenantId, billType);
            return JyBillNo.NextPreview(rule, DateTime.Now);
        }

        public JyBillIssueDto Issue(JyActor actor, JyBillIssueRequestDto dto)
        {
            EnsureFoundation(actor);
            var billType = JyBillNo.NormalizeCode(dto?.BillType, "单据类型");
            var bizTable = NormalizeBizTable(dto?.BizTable);
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                var issued = TryIssue(actor, billType, bizTable, dto?.BizId);
                if (issued != null)
                {
                    return issued;
                }
            }
            throw new CustomException("取号冲突，请重试");
        }

        public PagedInfo<JyBillIssueDto> Issues(JyActor actor, JyBillIssueQueryDto query)
        {
            EnsureFoundation(actor);
            query ??= new JyBillIssueQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var billType = query.BillType?.Trim();
            var billNo = query.BillNo?.Trim();
            var q = Context.Queryable<JyBillIssue>()
                .Where(it => it.TenantId == actor.TenantId)
                .WhereIF(!string.IsNullOrEmpty(billType), it => it.BillType == billType)
                .WhereIF(!string.IsNullOrEmpty(billNo), it => it.BillNo.Contains(billNo));
            var total = 0;
            var rows = q.OrderBy(it => it.Id, OrderByType.Desc).ToPageList(page, size, ref total);
            var names = Context.Queryable<JyBillRule>()
                .Where(it => it.TenantId == actor.TenantId)
                .ToList()
                .ToDictionary(it => it.BillType, it => it.BillName);
            return new PagedInfo<JyBillIssueDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(it => new JyBillIssueDto
                {
                    Id = it.Id,
                    BillType = it.BillType,
                    BillName = names.TryGetValue(it.BillType, out var name) ? name : it.BillType,
                    BillNo = it.BillNo,
                    CycleKey = it.CycleKey,
                    SeqNo = it.SeqNo,
                    BizTable = it.BizTable,
                    BizId = it.BizId,
                    CreateBy = it.Create_by,
                    CreateTime = it.Create_time
                }).ToList()
            };
        }

        /// <summary>由调用方统一开启事务时使用，取号与业务写入同事务提交或回滚。</summary>
        public JyBillIssueDto IssueInTransaction(JyActor actor, string billType, string bizTable, long? bizId, DateTime? bizDate = null)
        {
            EnsureFoundation(actor);
            billType = JyBillNo.NormalizeCode(billType, "单据类型");
            bizTable = NormalizeBizTable(bizTable);
            return IssueCore(actor, billType, bizTable, bizId, bizDate);
        }

        private JyBillIssueDto TryIssue(JyActor actor, string billType, string bizTable, long? bizId)
        {
            Context.Ado.BeginTran();
            try
            {
                var issued = IssueCore(actor, billType, bizTable, bizId);
                Context.Ado.CommitTran();
                return issued;
            }
            catch
            {
                Context.Ado.RollbackTran();
                throw;
            }
        }

        private JyBillIssueDto IssueCore(JyActor actor, string billType, string bizTable, long? bizId, DateTime? bizDate = null)
        {
            var now = DateTime.Now;
            var businessDate = bizDate ?? now;
            var rule = Context.Queryable<JyBillRule>()
                .With(SqlWith.UpdLock)
                .Where(it => it.TenantId == actor.TenantId && it.BillType == billType)
                .First();
            if (rule == null)
            {
                throw new CustomException("单据规则不存在");
            }
            if (rule.Status != 0)
            {
                throw new CustomException("单据规则已停用，不能取号");
            }

            var key = JyBillNo.CycleKey(rule.ResetCycle, businessDate);
            var seq = rule.CurrentKey == key ? rule.CurrentSeq : 0;
            string billNo = null;
            for (var jump = 0; jump < 30; jump++)
            {
                seq++;
                if (seq.ToString().Length > rule.SeqLength)
                {
                    throw new CustomException("当日流水已用完，请加大流水长度或等下一周期");
                }
                var candidate = JyBillNo.Compose(rule.Prefix, rule.DatePattern, rule.SeqLength, businessDate, seq);
                var used = Context.Queryable<JyBillIssue>()
                    .Any(it => it.TenantId == actor.TenantId && it.BillNo == candidate);
                if (!used)
                {
                    billNo = candidate;
                    break;
                }
            }
            if (billNo == null)
            {
                throw new CustomException("连续号码都已占用，请调整前缀或日期格式");
            }

            var affected = Context.Updateable<JyBillRule>()
                .SetColumns(it => it.CurrentKey == key)
                .SetColumns(it => it.CurrentSeq == seq)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == rule.Id && it.TenantId == actor.TenantId
                    && it.CurrentKey == rule.CurrentKey && it.CurrentSeq == rule.CurrentSeq)
                .ExecuteCommand();
            if (affected != 1)
            {
                throw new CustomException("取号冲突，请重试");
            }

            var issue = new JyBillIssue
            {
                TenantId = actor.TenantId,
                RuleId = rule.Id,
                BillType = rule.BillType,
                BillNo = billNo,
                CycleKey = key,
                SeqNo = seq,
                BizTable = bizTable,
                BizId = bizId,
                Create_by = actor.UserName,
                Create_time = now
            };
            issue.Id = Context.Insertable(issue).ExecuteReturnBigIdentity();
            return new JyBillIssueDto
            {
                Id = issue.Id,
                BillType = issue.BillType,
                BillName = rule.BillName,
                BillNo = issue.BillNo,
                CycleKey = issue.CycleKey,
                SeqNo = issue.SeqNo,
                BizTable = issue.BizTable,
                BizId = issue.BizId,
                CreateBy = issue.Create_by,
                CreateTime = issue.Create_time
            };
        }
        private void EnsureTypeFree(string tenantId, string billType, long exceptId)
        {
            var used = Context.Queryable<JyBillRule>().Any(it =>
                it.TenantId == tenantId && it.BillType == billType && it.Id != exceptId);
            if (used)
            {
                throw new CustomException("单据类型已存在");
            }
        }

        private JyBillRule Must(string tenantId, long id)
        {
            return Context.Queryable<JyBillRule>().First(it => it.Id == id && it.TenantId == tenantId)
                ?? throw new CustomException("单据规则不存在");
        }

        private JyBillRule MustType(string tenantId, string billType)
        {
            billType = JyBillNo.NormalizeCode(billType, "单据类型");
            return Context.Queryable<JyBillRule>().First(it => it.TenantId == tenantId && it.BillType == billType)
                ?? throw new CustomException("单据规则不存在");
        }

        private static JyBillRuleDto ToDto(JyBillRule it, DateTime now)
        {
            return new JyBillRuleDto
            {
                Id = it.Id,
                BillType = it.BillType,
                BillName = it.BillName,
                Prefix = it.Prefix,
                DatePattern = it.DatePattern,
                SeqLength = it.SeqLength,
                ResetCycle = it.ResetCycle,
                CurrentKey = it.CurrentKey,
                CurrentSeq = it.CurrentSeq,
                IsPreset = it.IsPreset,
                Status = it.Status,
                Remark = it.Remark,
                NextNo = it.Status == 0 ? JyBillNo.NextPreview(it, now) : null
            };
        }

        private static string NormalizeBizTable(string value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (value.Length > 64 || !System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z][A-Za-z0-9_]{0,63}$"))
            {
                throw new CustomException("业务表名只能包含字母、数字和下划线");
            }
            return value;
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



