using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>
    /// 定制定金业务。主表保留客户与整单汇总，jy_custom_item 保存每件商品，
    /// 到货、取货、销售对冲和退款均以商品明细为单位。
    /// </summary>
    [AppService]
    public class JyCustomService : BaseService<JyCustomOrder>
    {
        private readonly JyBillRuleService _billRule;
        private readonly JyMemberService _memberService;

        public JyCustomService(JyBillRuleService billRule, JyMemberService memberService)
        {
            _billRule = billRule;
            _memberService = memberService;
        }

        public PagedInfo<JyCustomDto> List(JyActor actor, JyCustomQueryDto query)
        {
            query ??= new JyCustomQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var arrivalOrderIds = query.ArrivalStatus is 0 or 1
                ? Context.Queryable<JyCustomItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && it.ArrivalStatus == query.ArrivalStatus.Value)
                    .Select(it => it.CustomOrderId).Distinct().ToList()
                : null;
            var contactOrderIds = query.ContactPending == 1
                ? Context.Queryable<JyCustomItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && it.ArrivalStatus == 1 && it.RemainAmount > 0
                        && it.Status != 8 && it.Status != 9)
                    .Select(it => it.CustomOrderId).Distinct().ToList()
                : null;
            var total = 0;
            var rows = Context.Queryable<JyCustomOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is >= 0 and <= 9, it => it.Status == query.Status)
                .WhereIF(arrivalOrderIds != null, it => arrivalOrderIds.Contains(it.Id))
                .WhereIF(contactOrderIds != null, it => contactOrderIds.Contains(it.Id))
                .WhereIF(query.RemainOnly == 1, it => it.RemainAmount > 0 && (it.Status == 1 || it.Status == 2))
                .WhereIF(query.BeginTime.HasValue, it => it.CustomDate >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.CustomDate < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.CustomNo.Contains(keyword)
                    || it.CustomerName.Contains(keyword) || it.CustomerPhone.Contains(keyword)
                    || it.GoodsName.Contains(keyword))
                .OrderBy(it => it.CustomDate, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JyCustomDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDtos(actor.TenantId, rows, false)
            };
        }

        public JyCustomDto Detail(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            var dto = ToDtos(actor.TenantId, new List<JyCustomOrder> { row }, true).First();
            var goodsNames = dto.Items.Where(it => it.Id > 0).ToDictionary(it => it.Id, it => it.GoodsName);
            dto.Ledgers = Context.Queryable<JyCustomLedger>()
                .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == id)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToList()
                .Select(it => new JyCustomLedgerDto
                {
                    Id = it.Id,
                    CustomItemId = it.CustomItemId,
                    GoodsName = it.CustomItemId.HasValue && goodsNames.TryGetValue(it.CustomItemId.Value, out var name) ? name : null,
                    ChangeType = it.ChangeType,
                    Amount = it.Amount,
                    BalanceAfter = it.BalanceAfter,
                    SaleOrderId = it.SaleOrderId,
                    Remark = it.Remark,
                    BizTime = it.BizTime,
                    CreateBy = it.Create_by
                }).ToList();
            return dto;
        }

        public JyCustomDashboardDto Dashboard(JyActor actor)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var pendingQuery = Context.Queryable<JyCustomOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.RemainAmount > 0
                    && (it.Status == 1 || it.Status == 2));
            var pendingCount = pendingQuery.Count();
            var remainAmount = pendingQuery.Sum(it => it.RemainAmount);
            var todayRows = Context.Queryable<JyCustomOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                    && it.CustomDate >= today && it.CustomDate < tomorrow)
                .ToList();
            var recentRows = pendingQuery
                .OrderBy(it => it.CustomDate, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .Take(6).ToList();
            return new JyCustomDashboardDto
            {
                PendingCount = pendingCount,
                RemainDepositAmount = remainAmount,
                TodayCount = todayRows.Count,
                TodayDepositAmount = todayRows.Sum(it => it.DepositAmount),
                Recent = ToDtos(actor.TenantId, recentRows, false)
            };
        }

        /// <summary>销售开单按商品明细返回可用定金。</summary>
        public List<JyCustomDto> Available(JyActor actor, long? memberId, string phone)
        {
            phone = NormalizePhone(phone);
            if ((memberId is null or <= 0) && string.IsNullOrEmpty(phone)) return new List<JyCustomDto>();
            var rows = Context.Queryable<JyCustomOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.RemainAmount > 0
                    && (it.Status == 1 || it.Status == 2))
                .WhereIF(memberId > 0 && !string.IsNullOrEmpty(phone),
                    it => it.MemberId == memberId || it.CustomerPhone == phone)
                .WhereIF(memberId > 0 && string.IsNullOrEmpty(phone), it => it.MemberId == memberId)
                .WhereIF((memberId is null or <= 0) && !string.IsNullOrEmpty(phone), it => it.CustomerPhone == phone)
                .OrderBy(it => it.CustomDate)
                .Take(50)
                .ToList();
            if (rows.Count == 0) return new List<JyCustomDto>();
            var result = new List<JyCustomDto>();
            foreach (var order in ToDtos(actor.TenantId, rows, true))
            {
                foreach (var item in order.Items.Where(it => it.Id > 0 && it.AvailableAmount > 0
                    && it.Status != 8 && it.Status != 9))
                {
                    result.Add(ToAvailableDto(order, item));
                }
            }
            return result;
        }

        public long Save(JyActor actor, JyCustomEditDto dto)
        {
            if (dto == null) throw new CustomException("定制单不能为空");
            if (dto.Id > 0) return Update(actor, dto);
            PrepareHeader(actor, dto);
            var items = NormalizeItems(actor, dto);
            var totalDeposit = Round(items.Sum(it => it.DepositAmount));
            if (totalDeposit <= 0) throw new CustomException("定金必须大于 0");
            long resultId = 0;
            long? memberId = dto.MemberId;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var businessDate = dto.CustomDate?.Date ?? now.Date;
                if (memberId is null or <= 0)
                {
                    memberId = _memberService.CreateInCurrentTransaction(actor,
                        dto.StoreId > 0 ? dto.StoreId : null, dto.CustomerName, dto.CustomerPhone, dto.CustomerSex);
                }
                if (businessDate > now.Date.AddDays(1)) throw new CustomException("定制日期不能晚于明天");
                var issued = _billRule.IssueInTransaction(actor, "custom", "jy_custom_order", null, businessDate);
                var first = items[0];
                var row = new JyCustomOrder
                {
                    TenantId = actor.TenantId,
                    StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                    CustomNo = issued.BillNo,
                    CustomDate = businessDate,
                    MemberId = memberId,
                    CustomerName = dto.CustomerName,
                    CustomerPhone = dto.CustomerPhone,
                    CustomerSex = dto.CustomerSex,
                    GoodsItemId = first.GoodsItemId,
                    GoodsNo = first.GoodsNo,
                    Barcode = first.Barcode,
                    BindTime = first.BindTime,
                    PickupDate = first.PickupDate,
                    ArrivalStatus = first.ArrivalStatus,
                    ArrivalTime = first.ArrivalTime,
                    ArrivalBy = first.ArrivalBy,
                    ArrivalRemark = first.ArrivalRemark,
                    GoodsName = SummaryName(items),
                    StyleItemId = first.StyleItemId,
                    StyleItemName = first.StyleItemName,
                    MaterialItemId = first.MaterialItemId,
                    MaterialName = first.MaterialName,
                    MetalItemId = first.MetalItemId,
                    MetalName = first.MetalName,
                    Weight = first.Weight,
                    ExpectPrice = Round(items.Sum(it => it.ExpectPrice)),
                    DepositAmount = totalDeposit,
                    OffsetAmount = 0,
                    RefundAmount = 0,
                    RemainAmount = totalDeposit,
                    Status = 1,
                    StaffUserId = actor.UserId,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = now,
                    Remark = dto.Remark
                };
                row.Id = Context.Insertable(row).ExecuteReturnBigIdentity();
                foreach (var item in items)
                {
                    item.TenantId = actor.TenantId;
                    item.CustomOrderId = row.Id;
                    item.Create_by = actor.UserName;
                    item.Create_time = now;
                    item.Id = Context.Insertable(item).ExecuteReturnBigIdentity();
                    if (item.GoodsItemId.HasValue)
                    {
                        LockGoods(actor, item.GoodsItemId.Value, item.Id, now, false);
                        item.ArrivalStatus = 1;
                        item.ArrivalTime = now;
                        item.ArrivalBy = actor.UserName;
                        item.ArrivalRemark = "绑定在库货品，待联系客户取货";
                        item.BindTime = now;
                        Context.Updateable<JyCustomItem>()
                            .SetColumns(it => it.ArrivalStatus == 1)
                            .SetColumns(it => it.ArrivalTime == now)
                            .SetColumns(it => it.ArrivalBy == actor.UserName)
                            .SetColumns(it => it.ArrivalRemark == "绑定在库货品，待联系客户取货")
                            .SetColumns(it => it.BindTime == now)
                            .Where(it => it.Id == item.Id).ExecuteCommand();
                    }
                    Context.Insertable(new JyCustomLedger
                    {
                        TenantId = actor.TenantId,
                        CustomOrderId = row.Id,
                        CustomItemId = item.Id,
                        ChangeType = 1,
                        Amount = item.DepositAmount,
                        BalanceAfter = item.RemainAmount,
                        Remark = item.GoodsNo == null
                            ? "收取定金，待工坊到货"
                            : "收取定金并锁定货品 " + item.GoodsNo + "，待联系客户取货",
                        BizTime = now,
                        Create_by = actor.UserName,
                        Create_time = now
                    }).ExecuteCommand();
                }
                SyncHeader(actor, row.Id, now);
                resultId = row.Id;
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "定制单保存失败");
            return resultId;
        }

        /// <summary>修改未发生对冲、退款的定制单。已收商品不能删除或减少定金。</summary>
        private long Update(JyActor actor, JyCustomEditDto dto)
        {
            PrepareHeader(actor, dto);
            var edits = NormalizeEdits(dto);
            long resultId = 0;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var row = Context.Queryable<JyCustomOrder>().With(SqlWith.UpdLock)
                    .First(it => it.Id == dto.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException("定制单不存在或已删除");
                if (row.Status is 3 or 8 or 9) throw new CustomException("已完成、已退清或作废的定制单不能修改");
                if (row.OffsetAmount > 0 || row.RefundAmount > 0)
                    throw new CustomException("这张定制单已经发生定金对冲或退款，不能再修改");
                var reserved = Context.Queryable<JySaleCustomOffset>()
                    .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == row.Id && it.Status == 0)
                    .Sum(it => it.OffsetAmount);
                if (reserved > 0) throw new CustomException("定金已被未完成销售单占用，暂时不能修改");
                var existing = Context.Queryable<JyCustomItem>().With(SqlWith.UpdLock)
                    .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == row.Id && it.DelFlag == 0)
                    .OrderBy(it => it.SortNo).ToList();
                if (existing.Count == 0) throw new CustomException("历史定金商品明细尚未补齐，请先执行本功能增量 SQL");
                if (existing.Any(old => edits.All(it => it.Id != old.Id)))
                    throw new CustomException("已收定金的商品不能直接删除，请先办理该商品的退定金");
                var businessDate = dto.CustomDate?.Date ?? row.CustomDate.Date;
                if (businessDate > now.Date.AddDays(1)) throw new CustomException("定制日期不能晚于明天");
                long? memberId = ResolveMemberForUpdate(actor, dto, row.MemberId);

                var sort = 1;
                var nextItems = new List<JyCustomItem>();
            foreach (var edit in edits)
                {
                    if (edit.Id > 0)
                    {
                        var item = existing.First(it => it.Id == edit.Id);
                        if (Round(edit.DepositAmount) != item.DepositAmount)
                            throw new CustomException("已收定金不能直接改金额；如需调整请走退定金流程");
                    // 页面未回传绑定商品时保留原绑定，避免普通编辑意外解绑/解锁。
                    if (edit.GoodsItemId is null or <= 0)
                    {
                        edit.GoodsItemId = item.GoodsItemId;
                        if (string.IsNullOrWhiteSpace(edit.GoodsNo)) edit.GoodsNo = item.GoodsNo;
                        if (string.IsNullOrWhiteSpace(edit.Barcode)) edit.Barcode = item.Barcode;
                    }
                        var newGoodsId = edit.GoodsItemId > 0 ? edit.GoodsItemId : null;
                        if (newGoodsId != item.GoodsItemId && item.ArrivalStatus == 1)
                            throw new CustomException("已到货商品不能更换绑定货品");
                        if (newGoodsId != item.GoodsItemId)
                        {
                            if (item.GoodsItemId.HasValue) ReleaseGoods(actor, item.GoodsItemId.Value, item.Id, now, true);
                            if (newGoodsId.HasValue)
                            {
                                LockGoods(actor, newGoodsId.Value, item.Id, now, false);
                                // 绑定的是已入库的唯一条码，等同现场到货，直接进入待联系客户状态。
                                item.ArrivalStatus = 1;
                                item.ArrivalTime = now;
                                item.ArrivalBy = actor.UserName;
                                item.ArrivalRemark = "绑定在库货品，待联系客户取货";
                                item.BindTime = now;
                            }
                        }
                        ApplyItemEdit(actor, edit, item, false);
                        item.SortNo = sort++;
                        item.Update_by = actor.UserName;
                        item.Update_time = now;
                        Context.Updateable(item).UpdateColumns(it => new
                        {
                            it.SortNo, it.GoodsItemId, it.GoodsNo, it.Barcode, it.BindTime, it.GoodsName,
                            it.ArrivalStatus, it.ArrivalTime, it.ArrivalBy, it.ArrivalRemark,
                            it.PickupDate, it.StyleItemId, it.StyleItemName, it.MaterialItemId, it.MaterialName,
                            it.MetalItemId, it.MetalName, it.Weight, it.ExpectPrice, it.Remark,
                            it.Update_by, it.Update_time
                        }).ExecuteCommand();
                        nextItems.Add(item);
                    }
                    else
                    {
                        var item = new JyCustomItem
                        {
                            TenantId = actor.TenantId,
                            CustomOrderId = row.Id,
                            SortNo = sort++,
                            DepositAmount = 0,
                            OffsetAmount = 0,
                            RefundAmount = 0,
                            RemainAmount = 0,
                            Status = 1,
                            DelFlag = 0,
                            Create_by = actor.UserName,
                            Create_time = now
                        };
                        ApplyItemEdit(actor, edit, item, true);
                        if (item.DepositAmount <= 0) throw new CustomException("新增商品必须填写大于 0 的定金");
                        item.Id = Context.Insertable(item).ExecuteReturnBigIdentity();
                        if (item.GoodsItemId.HasValue)
                        {
                            LockGoods(actor, item.GoodsItemId.Value, item.Id, now, false);
                            item.ArrivalStatus = 1;
                            item.ArrivalTime = now;
                            item.ArrivalBy = actor.UserName;
                            item.ArrivalRemark = "绑定在库货品，待联系客户取货";
                            item.BindTime = now;
                            Context.Updateable<JyCustomItem>()
                                .SetColumns(it => it.ArrivalStatus == 1)
                                .SetColumns(it => it.ArrivalTime == now)
                                .SetColumns(it => it.ArrivalBy == actor.UserName)
                                .SetColumns(it => it.ArrivalRemark == "绑定在库货品，待联系客户取货")
                                .SetColumns(it => it.BindTime == now)
                                .Where(it => it.Id == item.Id).ExecuteCommand();
                        }
                        Context.Insertable(new JyCustomLedger
                        {
                            TenantId = actor.TenantId,
                            CustomOrderId = row.Id,
                            CustomItemId = item.Id,
                            ChangeType = 1,
                            Amount = item.DepositAmount,
                            BalanceAfter = item.RemainAmount,
                            Remark = "新增定制商品并收取定金",
                            BizTime = now,
                            Create_by = actor.UserName,
                            Create_time = now
                        }).ExecuteCommand();
                        nextItems.Add(item);
                    }
                }
                if (nextItems.Select(it => it.GoodsItemId).Where(it => it.HasValue)
                    .GroupBy(it => it.Value).Any(it => it.Count() > 1))
                    throw new CustomException("同一张定金单不能重复绑定同一件商品");

                var first = nextItems[0];
                var affected = Context.Updateable<JyCustomOrder>()
                    .SetColumns(it => it.CustomDate == businessDate)
                    .SetColumns(it => it.MemberId == memberId)
                    .SetColumns(it => it.CustomerName == dto.CustomerName)
                    .SetColumns(it => it.CustomerPhone == dto.CustomerPhone)
                    .SetColumns(it => it.CustomerSex == dto.CustomerSex)
                    .SetColumns(it => it.Remark == dto.Remark)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("定制单已变化，请刷新后重试");
                SyncHeader(actor, row.Id, now);
                resultId = row.Id;
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "定制单修改失败");
            return resultId;
        }

        /// <summary>软删除定金单。全部退清后逐件释放仍由本单锁定的商品。</summary>
        public int Delete(JyActor actor, long id)
        {
            if (id <= 0) throw new CustomException("请选择要删除的定制单");
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var row = Context.Queryable<JyCustomOrder>().With(SqlWith.UpdLock)
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException("定制单不存在或已删除");
                if (row.OffsetAmount > 0) throw new CustomException("已经发生销售对冲的定制单不能删除");
                if (row.RemainAmount > 0) throw new CustomException($"还有 ¥{row.RemainAmount:0.00} 定金未退清，请先办理退定金后再删除");
                var reserved = Context.Queryable<JySaleCustomOffset>()
                    .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == row.Id && it.Status == 0)
                    .Sum(it => it.OffsetAmount);
                if (reserved > 0) throw new CustomException("定金仍被未完成销售单占用，不能删除");
                var items = Context.Queryable<JyCustomItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == row.Id && it.DelFlag == 0).ToList();
                foreach (var item in items)
                {
                    if (item.GoodsItemId.HasValue) ReleaseGoods(actor, item.GoodsItemId.Value, item.Id, now, false);
                }
                Context.Updateable<JyCustomItem>().SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Status == 9)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == row.Id && it.DelFlag == 0)
                    .ExecuteCommand();
                var affected = Context.Updateable<JyCustomOrder>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Status == 9)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == row.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("定制单已变化，请刷新后重试");
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "删除定制单失败");
            return 1;
        }

        /// <summary>按商品登记到货。</summary>
        public int Arrive(JyActor actor, JyCustomArrivalDto dto)
        {
            if (dto == null || dto.Id <= 0) throw new CustomException("请选择定制单");
            var barcode = dto.Barcode?.Trim();
            if (string.IsNullOrEmpty(barcode)) throw new CustomException("到货时必须录入货品条码");
            if (barcode.Length > 64) throw new CustomException("货品条码不能超过 64 位");
            var remark = TrimRemark(dto.Remark);
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var row = MustLocked(actor, dto.Id);
                if (row.Status is 8 or 9) throw new CustomException("已退清或作废的定制单不能登记到货");
                var item = ResolveItemForOperation(actor, row.Id, dto.ItemId);
                if (item.Status is 8 or 9) throw new CustomException("该商品已退清或作废，不能登记到货");
                if (item.ArrivalStatus == 1) throw new CustomException("该商品已经登记到货，请勿重复操作");
                var goods = Context.Queryable<JyGoodsItem>().First(it => it.TenantId == actor.TenantId
                    && it.DelFlag == 0 && it.Barcode == barcode);
                if (goods == null) throw new CustomException("条码对应的商品不存在，请先在入库单完成入库");
                if (item.GoodsItemId.HasValue && item.GoodsItemId.Value != goods.Id)
                    throw new CustomException("该商品已绑定其它唯一货品，到货条码必须与原绑定商品一致");
                if (item.GoodsItemId is null && (goods.Status != 1 || goods.Qty <= 0))
                    throw new CustomException(goods.Status == 2 ? "该商品已被其它业务锁定，不能作为定制到货商品" : "该商品不在库，不能登记到货");
                if (Context.Queryable<JyCustomItem>().Any(it => it.TenantId == actor.TenantId
                    && it.CustomOrderId == row.Id && it.DelFlag == 0 && it.Id != item.Id && it.GoodsItemId == goods.Id))
                    throw new CustomException("该货品已绑定到本单的另一件商品");
                if (item.GoodsItemId is null) LockGoods(actor, goods.Id, item.Id, now, false);
                var affected = Context.Updateable<JyCustomItem>()
                    .SetColumns(it => it.ArrivalStatus == 1)
                    .SetColumns(it => it.ArrivalTime == now)
                    .SetColumns(it => it.ArrivalBy == actor.UserName)
                    .SetColumns(it => it.ArrivalRemark == remark)
                    .SetColumns(it => it.GoodsItemId == goods.Id)
                    .SetColumns(it => it.GoodsNo == goods.GoodsNo)
                    .SetColumns(it => it.Barcode == goods.Barcode)
                    .SetColumns(it => it.BindTime == (item.BindTime ?? now))
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == item.Id && it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ArrivalStatus == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("到货状态已变化，请刷新后重试");
                Context.Insertable(new JyCustomLedger
                {
                    TenantId = actor.TenantId,
                    CustomOrderId = row.Id,
                    CustomItemId = item.Id,
                    ChangeType = 5,
                    Amount = 0,
                    BalanceAfter = item.RemainAmount,
                    Remark = $"货品到店：{goods.GoodsNo} / {goods.Barcode}" + (string.IsNullOrEmpty(remark) ? string.Empty : "；" + remark),
                    BizTime = now,
                    Create_by = actor.UserName,
                    Create_time = now
                }).ExecuteCommand();
                SyncHeader(actor, row.Id, now);
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "登记到货失败");
            return 1;
        }

        /// <summary>按商品登记取货，不改变定金余额。</summary>
        public int Pickup(JyActor actor, JyCustomPickupDto dto)
        {
            if (dto == null || dto.Id <= 0) throw new CustomException("请选择定制单");
            var remark = TrimRemark(dto.Remark);
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var row = MustLocked(actor, dto.Id);
                if (row.Status is 8 or 9) throw new CustomException("已退清或作废的定制单不能登记取货");
                var item = ResolveItemForOperation(actor, row.Id, dto.ItemId);
                if (item.Status is 8 or 9) throw new CustomException("该商品已退清或作废，不能登记取货");
                if (item.ArrivalStatus != 1) throw new CustomException("商品尚未到货，不能登记取货");
                if (item.PickupStatus == 1) throw new CustomException("该商品已经登记取货，请勿重复操作");
                var affected = Context.Updateable<JyCustomItem>()
                    .SetColumns(it => it.PickupStatus == 1)
                    .SetColumns(it => it.PickupTime == now)
                    .SetColumns(it => it.PickupBy == actor.UserName)
                    .SetColumns(it => it.PickupRemark == remark)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == item.Id && it.TenantId == actor.TenantId && it.DelFlag == 0 && it.PickupStatus == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("取货状态已变化，请刷新后重试");
                Context.Insertable(new JyCustomLedger
                {
                    TenantId = actor.TenantId,
                    CustomOrderId = row.Id,
                    CustomItemId = item.Id,
                    ChangeType = 6,
                    Amount = 0,
                    BalanceAfter = item.RemainAmount,
                    Remark = "客户取货" + (string.IsNullOrEmpty(remark) ? string.Empty : "；" + remark),
                    BizTime = now,
                    Create_by = actor.UserName,
                    Create_time = now
                }).ExecuteCommand();
                SyncHeader(actor, row.Id, now);
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "登记取货失败");
            return 1;
        }

        /// <summary>精确退还某件定制商品的剩余定金。</summary>
        public int Refund(JyActor actor, JyCustomRefundDto dto)
        {
            if (dto == null || dto.Id <= 0) throw new CustomException("请选择要退定金的定制单");
            var amount = Round(dto.Amount);
            if (amount <= 0) throw new CustomException("退定金金额必须大于 0");
            var remark = TrimRemark(dto.Remark);
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var row = MustLocked(actor, dto.Id);
                if (row.Status is 8 or 9) throw new CustomException("这张定制单没有可退的剩余定金");
                var item = ResolveItemForOperation(actor, row.Id, dto.ItemId);
                if (item.Status is 8 or 9 || item.RemainAmount <= 0) throw new CustomException("该商品没有可退的剩余定金");
                var reserved = Context.Queryable<JySaleCustomOffset>()
                    .Where(it => it.TenantId == actor.TenantId && it.CustomItemId == item.Id && it.Status == 0)
                    .Sum(it => it.OffsetAmount);
                // 旧版草稿可能尚未回填 CustomItemId；单商品单仍必须扣除它占用的金额。
                if (Context.Queryable<JyCustomItem>().Count(it => it.TenantId == actor.TenantId
                    && it.CustomOrderId == row.Id && it.DelFlag == 0) == 1)
                {
                    reserved += Context.Queryable<JySaleCustomOffset>()
                        .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == row.Id
                            && it.CustomItemId == null && it.Status == 0)
                        .Sum(it => it.OffsetAmount);
                }
                var free = Round(item.RemainAmount - reserved);
                if (amount > free) throw new CustomException(reserved > 0 ? $"有 {reserved:0.00} 定金已被未完成销售单占用，当前可退 {free:0.00}" : "退定金不能超过该商品剩余定金");
                var remain = Round(item.RemainAmount - amount);
                var refunded = Round(item.RefundAmount + amount);
                var status = ResolveStatus(item.DepositAmount, item.OffsetAmount, refunded, remain);
                var affected = Context.Updateable<JyCustomItem>()
                    .SetColumns(it => it.RemainAmount == remain)
                    .SetColumns(it => it.RefundAmount == refunded)
                    .SetColumns(it => it.Status == status)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == item.Id && it.TenantId == actor.TenantId && it.RemainAmount == item.RemainAmount)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("定金余额已变化，请刷新后重试");
                Context.Insertable(new JyCustomLedger
                {
                    TenantId = actor.TenantId,
                    CustomOrderId = row.Id,
                    CustomItemId = item.Id,
                    ChangeType = 4,
                    Amount = -amount,
                    BalanceAfter = remain,
                    Remark = (string.IsNullOrEmpty(remark) ? "退定金" : remark) + "（" + item.GoodsName + "）",
                    BizTime = now,
                    Create_by = actor.UserName,
                    Create_time = now
                }).ExecuteCommand();
                if (remain <= 0 && item.GoodsItemId.HasValue) ReleaseGoods(actor, item.GoodsItemId.Value, item.Id, now, false);
                SyncHeader(actor, row.Id, now);
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "退定金失败");
            return 1;
        }

        /// <summary>销售核销后同步主表汇总，由销售服务在同一事务内调用。</summary>
        public void RecalculateHeader(JyActor actor, long customOrderId, DateTime now)
        {
            SyncHeader(actor, customOrderId, now);
        }

        private void PrepareHeader(JyActor actor, JyCustomEditDto dto)
        {
            dto.CustomerPhone = NormalizePhone(dto.CustomerPhone);
            dto.Remark = TrimRemark(dto.Remark);
            if (dto.CustomerSex is < 0 or > 2) throw new CustomException("客户性别不正确");
            JyMember member = null;
            if (dto.MemberId > 0)
            {
                member = Context.Queryable<JyMember>().First(it => it.Id == dto.MemberId.Value
                    && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (member == null || member.Status != 0) throw new CustomException("会员不存在或已停用");
            }
            else if (!string.IsNullOrEmpty(dto.CustomerPhone))
            {
                var byPhone = Context.Queryable<JyMember>().First(it => it.TenantId == actor.TenantId
                    && it.DelFlag == 0 && it.Phone == dto.CustomerPhone);
                if (byPhone != null)
                {
                    if (byPhone.Status != 0) throw new CustomException("该手机号对应的会员已停用，请先在会员档案中启用");
                    member = byPhone;
                    dto.MemberId = byPhone.Id;
                }
            }
            if (member != null)
            {
                if (string.IsNullOrEmpty(dto.CustomerPhone)) dto.CustomerPhone = member.Phone;
                if (string.IsNullOrEmpty(dto.CustomerName)) dto.CustomerName = member.MemberName;
                if (dto.CustomerSex == 0) dto.CustomerSex = member.Sex;
            }
            dto.CustomerName = TrimText(dto.CustomerName, 50, "客户姓名");
            if (string.IsNullOrEmpty(dto.CustomerName)) throw new CustomException("请填写客户姓名");
            if (string.IsNullOrEmpty(dto.CustomerPhone)) throw new CustomException("请填写客户手机号");
            if (dto.PickupDate.HasValue && dto.PickupDate.Value.Date < DateTime.Today.AddYears(-1))
                throw new CustomException("取货日期不正确");
        }

        private long? ResolveMemberForUpdate(JyActor actor, JyCustomEditDto dto, long? fallback)
        {
            JyMember member = null;
            if (dto.MemberId > 0)
            {
                member = Context.Queryable<JyMember>().First(it => it.Id == dto.MemberId.Value
                    && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (member == null || member.Status != 0) throw new CustomException("会员不存在或已停用");
            }
            else if (!string.IsNullOrEmpty(dto.CustomerPhone))
            {
                member = Context.Queryable<JyMember>().First(it => it.TenantId == actor.TenantId
                    && it.DelFlag == 0 && it.Phone == dto.CustomerPhone);
                if (member != null && member.Status != 0) throw new CustomException("该手机号对应的会员已停用");
            }
            if (member == null) return fallback;
            if (string.IsNullOrEmpty(dto.CustomerPhone)) dto.CustomerPhone = member.Phone;
            if (string.IsNullOrEmpty(dto.CustomerName)) dto.CustomerName = member.MemberName;
            if (dto.CustomerSex == 0) dto.CustomerSex = member.Sex;
            return member.Id;
        }

        private List<JyCustomItemEditDto> NormalizeEdits(JyCustomEditDto dto)
        {
            var edits = (dto.Items ?? new List<JyCustomItemEditDto>()).Where(it => it != null).ToList();
            if (edits.Count == 0)
            {
                edits.Add(new JyCustomItemEditDto
                {
                    GoodsItemId = dto.GoodsItemId,
                    GoodsNo = dto.GoodsNo,
                    Barcode = dto.Barcode,
                    GoodsName = dto.GoodsName,
                    StyleItemId = dto.StyleItemId,
                    MaterialItemId = dto.MaterialItemId,
                    MetalItemId = dto.MetalItemId,
                    Weight = dto.Weight,
                    ExpectPrice = dto.ExpectPrice,
                    DepositAmount = dto.DepositAmount,
                    PickupDate = dto.PickupDate
                });
            }
            if (edits.Count > 50) throw new CustomException("一张定金单最多录入 50 件商品");
            return edits;
        }

        private List<JyCustomItem> NormalizeItems(JyActor actor, JyCustomEditDto dto)
        {
            var edits = NormalizeEdits(dto);
            var result = new List<JyCustomItem>();
            var sort = 1;
            foreach (var edit in edits)
            {
                var item = new JyCustomItem
                {
                    SortNo = edit.SortNo > 0 ? edit.SortNo : sort++,
                    DepositAmount = 0,
                    OffsetAmount = 0,
                    RefundAmount = 0,
                    RemainAmount = 0,
                    Status = 1,
                    DelFlag = 0
                };
                ApplyItemEdit(actor, edit, item, true);
                result.Add(item);
            }
            if (result.Select(it => it.GoodsItemId).Where(it => it.HasValue)
                .GroupBy(it => it.Value).Any(it => it.Count() > 1))
                throw new CustomException("同一张定金单不能重复绑定同一件商品");
            return result;
        }

        /// <summary>规范化单件商品。新增时校验定金，修改时保留原定金与流水。</summary>
        private void ApplyItemEdit(JyActor actor, JyCustomItemEditDto edit, JyCustomItem item, bool newItem)
        {
            edit.GoodsNo = TrimText(edit.GoodsNo, 32, "货号");
            edit.Barcode = TrimText(edit.Barcode, 64, "条码");
            edit.GoodsName = TrimText(edit.GoodsName, 100, "定制货品");
            edit.Remark = TrimRemark(edit.Remark);
            if (edit.DepositAmount < 0 || edit.DepositAmount > 10000000m) throw new CustomException("定金金额不正确");
            if (edit.ExpectPrice < 0 || edit.ExpectPrice > 10000000m) throw new CustomException("预计售价不正确");
            if (edit.Weight is < 0 or > 100000m) throw new CustomException("克重不正确");
            if (edit.PickupDate.HasValue && edit.PickupDate.Value.Date < DateTime.Today.AddYears(-1))
                throw new CustomException("取货日期不正确");
            JyGoodsItem goods = null;
            var hasGoods = edit.GoodsItemId > 0 || !string.IsNullOrWhiteSpace(edit.Barcode) || !string.IsNullOrWhiteSpace(edit.GoodsNo);
            if (hasGoods)
            {
                if (edit.GoodsItemId > 0)
                {
                    goods = Context.Queryable<JyGoodsItem>().First(it => it.Id == edit.GoodsItemId.Value
                        && it.TenantId == actor.TenantId && it.DelFlag == 0);
                }
                else
                {
                    var code = (edit.Barcode ?? edit.GoodsNo).Trim();
                    goods = Context.Queryable<JyGoodsItem>().First(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                        && (it.Barcode == code || it.GoodsNo == code));
                }
                if (goods == null) throw new CustomException("绑定商品不存在或已删除");
                edit.GoodsItemId = goods.Id;
                edit.GoodsNo = goods.GoodsNo;
                edit.Barcode = goods.Barcode;
            }
            item.GoodsItemId = edit.GoodsItemId > 0 ? edit.GoodsItemId : null;
            item.GoodsNo = edit.GoodsNo;
            item.Barcode = edit.Barcode;
            item.BindTime = goods == null ? null : (item.BindTime ?? DateTime.Now);
            item.GoodsName = string.IsNullOrEmpty(edit.GoodsName) ? goods?.StyleName : edit.GoodsName;
            if (string.IsNullOrEmpty(item.GoodsName)) throw new CustomException("请填写定制货品名称");
            item.PickupDate = edit.PickupDate?.Date;
            item.StyleItemId = edit.StyleItemId > 0 ? edit.StyleItemId : goods?.StyleItemId;
            item.StyleItemName = LookupName(actor.TenantId, item.StyleItemId);
            item.MaterialItemId = edit.MaterialItemId > 0 ? edit.MaterialItemId : goods?.MaterialItemId;
            item.MaterialName = LookupName(actor.TenantId, item.MaterialItemId);
            item.MetalItemId = edit.MetalItemId > 0 ? edit.MetalItemId : goods?.MetalItemId;
            item.MetalName = LookupName(actor.TenantId, item.MetalItemId);
            item.Weight = edit.Weight ?? goods?.Weight;
            item.ExpectPrice = Round(edit.ExpectPrice > 0 ? edit.ExpectPrice : (goods?.SalePrice ?? 0));
            item.Remark = edit.Remark;
            if (newItem)
            {
                item.DepositAmount = Round(edit.DepositAmount);
                if (item.DepositAmount <= 0) throw new CustomException("每件商品都必须填写大于 0 的定金");
                item.RemainAmount = item.DepositAmount;
            }
        }

        private JyCustomOrder MustLocked(JyActor actor, long id)
        {
            return Context.Queryable<JyCustomOrder>().With(SqlWith.UpdLock)
                .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("定制单不存在或已删除");
        }

        /// <summary>未传 ItemId 时仅允许单商品旧单兜底。</summary>
        private JyCustomItem ResolveItemForOperation(JyActor actor, long orderId, long? itemId)
        {
            var q = Context.Queryable<JyCustomItem>().With(SqlWith.UpdLock)
                .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == orderId && it.DelFlag == 0);
            JyCustomItem item;
            if (itemId > 0)
            {
                item = q.First(it => it.Id == itemId.Value) ?? throw new CustomException("定金商品明细不存在或已删除");
            }
            else
            {
                var items = q.OrderBy(it => it.SortNo).ToList();
                if (items.Count != 1) throw new CustomException("请选择要操作的具体定制商品");
                item = items[0];
            }
            return item;
        }

        private void LockGoods(JyActor actor, long goodsItemId, long customItemId, DateTime now, bool allowAlreadyOwned)
        {
            var updated = Context.Updateable<JyGoodsItem>()
                .SetColumns(it => it.Status == 2)
                .SetColumns(it => it.LockedQty == 1)
                .SetColumns(it => it.LockBizType == "custom")
                .SetColumns(it => it.LockBizId == customItemId)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == goodsItemId && it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Qty > 0
                    && ((it.Status == 1 && it.LockedQty == 0)
                        || (allowAlreadyOwned && it.Status == 2 && it.LockedQty == 1
                            && it.LockBizType == "custom" && it.LockBizId == customItemId)))
                .ExecuteCommand();
            if (updated != 1) throw new CustomException("商品已被其他业务锁定或已不在库，请刷新后重试");
        }

        private void ReleaseGoods(JyActor actor, long goodsItemId, long customItemId, DateTime now, bool requireOwned)
        {
            var goods = Context.Queryable<JyGoodsItem>().With(SqlWith.UpdLock)
                .First(it => it.Id == goodsItemId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("绑定商品不存在或已删除");
            if (goods.Status == 2 && goods.LockBizType == "custom" && goods.LockBizId == customItemId)
            {
                var unlocked = Context.Updateable<JyGoodsItem>()
                    .SetColumns(it => it.Status == 1)
                    .SetColumns(it => it.LockedQty == 0)
                    .SetColumns(it => it.LockBizType == null)
                    .SetColumns(it => it.LockBizId == null)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == goods.Id && it.TenantId == actor.TenantId && it.DelFlag == 0
                        && it.Status == 2 && it.LockBizType == "custom" && it.LockBizId == customItemId)
                    .ExecuteCommand();
                if (unlocked != 1) throw new CustomException("绑定商品状态已变化，请刷新后重试");
            }
            else if (requireOwned && (goods.Status == 2 || goods.LockedQty != 0))
            {
                throw new CustomException("绑定商品已被其他业务占用，不能操作");
            }
        }

        /// <summary>按商品明细重新汇总主表；主表只做汇总和列表展示，业务余额以明细为准。</summary>
        private void SyncHeader(JyActor actor, long customOrderId, DateTime now)
        {
            var items = Context.Queryable<JyCustomItem>()
                .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == customOrderId && it.DelFlag == 0)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList();
            if (items.Count == 0) throw new CustomException("定制单没有有效商品明细");

            var deposit = Round(items.Sum(it => it.DepositAmount));
            var offset = Round(items.Sum(it => it.OffsetAmount));
            var refund = Round(items.Sum(it => it.RefundAmount));
            var remain = Round(items.Sum(it => it.RemainAmount));
            var status = ResolveHeaderStatus(items);
            var first = items[0];
            var pickupDate = items.Where(it => it.PickupDate.HasValue)
                .Select(it => it.PickupDate).OrderBy(it => it).FirstOrDefault();
            var arrival = items.FirstOrDefault(it => it.ArrivalStatus == 1);

            var affected = Context.Updateable<JyCustomOrder>()
                .SetColumns(it => it.GoodsItemId == first.GoodsItemId)
                .SetColumns(it => it.GoodsNo == first.GoodsNo)
                .SetColumns(it => it.Barcode == first.Barcode)
                .SetColumns(it => it.BindTime == first.BindTime)
                .SetColumns(it => it.PickupDate == pickupDate)
                .SetColumns(it => it.ArrivalStatus == (arrival == null ? 0 : 1))
                .SetColumns(it => it.ArrivalTime == (arrival == null ? null : arrival.ArrivalTime))
                .SetColumns(it => it.ArrivalBy == (arrival == null ? null : arrival.ArrivalBy))
                .SetColumns(it => it.ArrivalRemark == (arrival == null ? null : arrival.ArrivalRemark))
                .SetColumns(it => it.GoodsName == SummaryName(items))
                .SetColumns(it => it.StyleItemId == first.StyleItemId)
                .SetColumns(it => it.StyleItemName == first.StyleItemName)
                .SetColumns(it => it.MaterialItemId == first.MaterialItemId)
                .SetColumns(it => it.MaterialName == first.MaterialName)
                .SetColumns(it => it.MetalItemId == first.MetalItemId)
                .SetColumns(it => it.MetalName == first.MetalName)
                .SetColumns(it => it.Weight == first.Weight)
                .SetColumns(it => it.ExpectPrice == Round(items.Sum(it => it.ExpectPrice)))
                .SetColumns(it => it.DepositAmount == deposit)
                .SetColumns(it => it.OffsetAmount == offset)
                .SetColumns(it => it.RefundAmount == refund)
                .SetColumns(it => it.RemainAmount == remain)
                .SetColumns(it => it.Status == status)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == customOrderId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
            if (affected != 1) throw new CustomException("定制单汇总同步失败，请刷新后重试");
        }

        private List<JyCustomDto> ToDtos(string tenantId, List<JyCustomOrder> orders, bool includeLedgers)
        {
            if (orders == null || orders.Count == 0) return new List<JyCustomDto>();
            var orderIds = orders.Select(it => it.Id).ToList();
            var items = Context.Queryable<JyCustomItem>()
                .Where(it => it.TenantId == tenantId && orderIds.Contains(it.CustomOrderId) && it.DelFlag == 0)
                .OrderBy(it => it.SortNo)
                .OrderBy(it => it.Id)
                .ToList();
            var itemIds = items.Select(it => it.Id).ToList();
            var reservedRows = itemIds.Count == 0
                ? new List<JySaleCustomOffset>()
                : Context.Queryable<JySaleCustomOffset>()
                    .Where(it => it.TenantId == tenantId && it.Status == 0
                        && it.CustomItemId.HasValue && itemIds.Contains(it.CustomItemId.Value))
                    .ToList();
            var reservedByItem = reservedRows
                .GroupBy(it => it.CustomItemId.Value)
                .ToDictionary(g => g.Key, g => Round(g.Sum(it => it.OffsetAmount)));
            // 兼容尚未回填 CustomItemId 的旧版草稿对冲。
            var legacyReserved = Context.Queryable<JySaleCustomOffset>()
                .Where(it => it.TenantId == tenantId && orderIds.Contains(it.CustomOrderId)
                    && it.Status == 0 && it.CustomItemId == null)
                .ToList()
                .GroupBy(it => it.CustomOrderId)
                .ToDictionary(g => g.Key, g => Round(g.Sum(it => it.OffsetAmount)));

            var itemDtos = new Dictionary<long, List<JyCustomItemDto>>();
            var customNos = orders.ToDictionary(it => it.Id, it => it.CustomNo);
            foreach (var group in items.GroupBy(it => it.CustomOrderId))
            {
                var list = new List<JyCustomItemDto>();
                foreach (var item in group.OrderBy(it => it.SortNo).ThenBy(it => it.Id))
                {
                    reservedByItem.TryGetValue(item.Id, out var reserved);
                    var legacy = 0m;
                    if (group.Count() == 1) legacyReserved.TryGetValue(item.CustomOrderId, out legacy);
                    var dto = ToItemDto(item, customNos[item.CustomOrderId]);
                    dto.AvailableAmount = Round(Math.Max(0, item.RemainAmount - reserved - legacy));
                    dto.ContactPending = item.ArrivalStatus == 1 && item.RemainAmount > 0 && item.Status is not (8 or 9);
                    list.Add(dto);
                }
                itemDtos[group.Key] = list;
            }

            var ledgers = includeLedgers
                ? Context.Queryable<JyCustomLedger>()
                    .Where(it => it.TenantId == tenantId && orderIds.Contains(it.CustomOrderId))
                    .OrderBy(it => it.Id, OrderByType.Desc)
                    .ToList()
                : new List<JyCustomLedger>();
            var goodsNames = items.ToDictionary(it => it.Id, it => it.GoodsName);

            return orders.Select(order =>
            {
                itemDtos.TryGetValue(order.Id, out var orderItems);
                orderItems ??= new List<JyCustomItemDto>();
                var first = orderItems.FirstOrDefault();
                var dto = new JyCustomDto
                {
                    Id = order.Id,
                    CustomNo = order.CustomNo,
                    CustomDate = order.CustomDate,
                    MemberId = order.MemberId,
                    MemberName = order.CustomerName,
                    CustomerName = order.CustomerName,
                    CustomerPhone = order.CustomerPhone,
                    CustomerSex = order.CustomerSex,
                    GoodsName = SummaryName(orderItems.Select(it => new JyCustomItem { GoodsName = it.GoodsName }).ToList()),
                    GoodsItemId = first?.GoodsItemId,
                    GoodsNo = first?.GoodsNo,
                    Barcode = first?.Barcode,
                    BindTime = first?.BindTime,
                    PickupDate = order.PickupDate,
                    ArrivalStatus = orderItems.Any(it => it.ArrivalStatus == 1) ? 1 : 0,
                    ArrivalTime = orderItems.Where(it => it.ArrivalTime.HasValue).Select(it => it.ArrivalTime).FirstOrDefault(),
                    ArrivalBy = orderItems.FirstOrDefault(it => it.ArrivalStatus == 1)?.ArrivalBy,
                    ArrivalRemark = orderItems.FirstOrDefault(it => it.ArrivalStatus == 1)?.ArrivalRemark,
                    ContactPending = orderItems.Any(it => it.ContactPending),
                    StyleItemId = first?.StyleItemId,
                    StyleItemName = first?.StyleItemName,
                    MaterialItemId = first?.MaterialItemId,
                    MaterialName = first?.MaterialName,
                    MetalItemId = first?.MetalItemId,
                    MetalName = first?.MetalName,
                    Weight = first?.Weight,
                    ExpectPrice = Round(orderItems.Sum(it => it.ExpectPrice)),
                    DepositAmount = Round(orderItems.Sum(it => it.DepositAmount)),
                    OffsetAmount = Round(orderItems.Sum(it => it.OffsetAmount)),
                    RefundAmount = Round(orderItems.Sum(it => it.RefundAmount)),
                    RemainAmount = Round(orderItems.Sum(it => it.RemainAmount)),
                    AvailableAmount = Round(orderItems.Sum(it => it.AvailableAmount)),
                    Status = order.Status,
                    Remark = order.Remark,
                    CreateBy = order.Create_by,
                    CreateTime = order.Create_time,
                    ItemCount = orderItems.Count,
                    ArrivedItemCount = orderItems.Count(it => it.ArrivalStatus == 1),
                    PickedItemCount = orderItems.Count(it => it.PickupStatus == 1),
                    Items = orderItems
                };
                if (includeLedgers)
                {
                    dto.Ledgers = ledgers.Where(it => it.CustomOrderId == order.Id).Select(it => new JyCustomLedgerDto
                    {
                        Id = it.Id,
                        CustomItemId = it.CustomItemId,
                        GoodsName = it.CustomItemId.HasValue && goodsNames.TryGetValue(it.CustomItemId.Value, out var name) ? name : null,
                        ChangeType = it.ChangeType,
                        Amount = it.Amount,
                        BalanceAfter = it.BalanceAfter,
                        SaleOrderId = it.SaleOrderId,
                        Remark = it.Remark,
                        BizTime = it.BizTime,
                        CreateBy = it.Create_by
                    }).ToList();
                }
                return dto;
            }).ToList();
        }

        private static JyCustomItemDto ToItemDto(JyCustomItem item, string customNo)
        {
            return new JyCustomItemDto
            {
                Id = item.Id,
                CustomOrderId = item.CustomOrderId,
                CustomNo = customNo,
                SortNo = item.SortNo,
                GoodsItemId = item.GoodsItemId,
                GoodsNo = item.GoodsNo,
                Barcode = item.Barcode,
                BindTime = item.BindTime,
                GoodsName = item.GoodsName,
                PickupDate = item.PickupDate,
                ArrivalStatus = item.ArrivalStatus,
                ArrivalTime = item.ArrivalTime,
                ArrivalBy = item.ArrivalBy,
                ArrivalRemark = item.ArrivalRemark,
                PickupStatus = item.PickupStatus,
                PickupTime = item.PickupTime,
                PickupBy = item.PickupBy,
                PickupRemark = item.PickupRemark,
                StyleItemId = item.StyleItemId,
                StyleItemName = item.StyleItemName,
                MaterialItemId = item.MaterialItemId,
                MaterialName = item.MaterialName,
                MetalItemId = item.MetalItemId,
                MetalName = item.MetalName,
                Weight = item.Weight,
                ExpectPrice = item.ExpectPrice,
                DepositAmount = item.DepositAmount,
                OffsetAmount = item.OffsetAmount,
                RefundAmount = item.RefundAmount,
                RemainAmount = item.RemainAmount,
                Status = item.Status,
                Remark = item.Remark
            };
        }

        private static JyCustomDto ToAvailableDto(JyCustomDto order, JyCustomItemDto item)
        {
            return new JyCustomDto
            {
                Id = order.Id,
                ItemId = item.Id,
                CustomNo = order.CustomNo,
                CustomDate = order.CustomDate,
                MemberId = order.MemberId,
                MemberName = order.MemberName,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerSex = order.CustomerSex,
                GoodsName = item.GoodsName,
                GoodsItemId = item.GoodsItemId,
                GoodsNo = item.GoodsNo,
                Barcode = item.Barcode,
                BindTime = item.BindTime,
                PickupDate = item.PickupDate,
                ArrivalStatus = item.ArrivalStatus,
                ArrivalTime = item.ArrivalTime,
                ArrivalBy = item.ArrivalBy,
                ArrivalRemark = item.ArrivalRemark,
                ContactPending = item.ContactPending,
                StyleItemId = item.StyleItemId,
                StyleItemName = item.StyleItemName,
                MaterialItemId = item.MaterialItemId,
                MaterialName = item.MaterialName,
                MetalItemId = item.MetalItemId,
                MetalName = item.MetalName,
                Weight = item.Weight,
                ExpectPrice = item.ExpectPrice,
                DepositAmount = item.DepositAmount,
                OffsetAmount = item.OffsetAmount,
                RefundAmount = item.RefundAmount,
                RemainAmount = item.RemainAmount,
                AvailableAmount = item.AvailableAmount,
                Status = item.Status,
                Remark = item.Remark,
                ItemCount = 1,
                ArrivedItemCount = item.ArrivalStatus == 1 ? 1 : 0,
                PickedItemCount = item.PickupStatus == 1 ? 1 : 0,
                Items = new List<JyCustomItemDto> { item }
            };
        }

        private static string SummaryName(IReadOnlyCollection<JyCustomItem> items)
        {
            if (items == null || items.Count == 0) return "定制商品";
            var first = items.First().GoodsName;
            var text = items.Count == 1 ? first : first + " 等" + items.Count + "件";
            return text.Length <= 100 ? text : text.Substring(0, 97) + "...";
        }

        private string LookupName(string tenantId, long? id)
        {
            if (!id.HasValue || id.Value <= 0) return null;
            return Context.Queryable<JyLookupItem>()
                .Where(it => it.Id == id.Value && it.TenantId == tenantId && it.DelFlag == 0)
                .Select(it => it.ItemName)
                .First();
        }

        private JyCustomOrder Must(string tenantId, long id)
        {
            return Context.Queryable<JyCustomOrder>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("定制单不存在或已删除");
        }

        /// <summary>单件定金状态。兼容销售服务原有调用。</summary>
        public static int ResolveStatus(decimal deposit, decimal offset, decimal refund, decimal remain)
        {
            if (remain > 0) return offset > 0 || refund > 0 ? 2 : 1;
            if (offset > 0) return 3;
            if (deposit > 0 && refund >= deposit) return 8;
            return 1;
        }

        private static int ResolveHeaderStatus(IReadOnlyCollection<JyCustomItem> items)
        {
            if (items.Count == 0) return 1;
            var remain = Round(items.Sum(it => it.RemainAmount));
            var offset = Round(items.Sum(it => it.OffsetAmount));
            var refund = Round(items.Sum(it => it.RefundAmount));
            if (remain > 0) return offset > 0 || refund > 0 ? 2 : 1;
            if (offset > 0) return 3;
            return 8;
        }

        private static decimal Round(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string NormalizePhone(string phone)
        {
            phone = phone?.Trim();
            if (string.IsNullOrEmpty(phone)) return null;
            if (phone.Length > 30 || phone.Any(ch => !char.IsDigit(ch) && ch != '-' && ch != '+'))
                throw new CustomException("手机号只能包含数字、加号或中横线，最长 30 位");
            return phone;
        }

        private static string TrimText(string value, int max, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > max) throw new CustomException(label + "不能超过 " + max + " 个字");
            return value;
        }

        private static string TrimRemark(string remark)
        {
            remark = remark?.Trim();
            if (string.IsNullOrEmpty(remark)) return null;
            if (remark.Length > 500) throw new CustomException("备注不能超过 500 个字");
            return remark;
        }
    }
}
