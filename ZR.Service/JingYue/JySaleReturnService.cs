using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>销售退货：整单或按明细部分退货，库存、积分与会员累计消费同事务回滚。</summary>
    [AppService]
    public class JySaleReturnService : BaseService<JySaleReturn>
    {
        private readonly JyBillRuleService _billRule;
        private readonly JySaleService _saleService;

        public JySaleReturnService(JyBillRuleService billRule, JySaleService saleService)
        {
            _billRule = billRule;
            _saleService = saleService;
        }

        private JyWorkItemService WorkItem => App.GetService<JyWorkItemService>();

        /// <summary>查询可退货的已完成销售单。</summary>
        public PagedInfo<JySaleDto> ReturnableOrders(JyActor actor, JySaleReturnQueryDto query)
        {
            query ??= new JySaleReturnQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1 && it.ReturnStatus != 2)
                .WhereIF(query.BeginTime.HasValue, it => it.DealTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.DealTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.OrderNo.Contains(keyword)
                    || it.CustomerName.Contains(keyword) || it.CustomerPhone.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.DealTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JySaleDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToSaleDtos(actor.TenantId, rows)
            };
        }

        public JySaleDto ReturnableDetail(JyActor actor, long saleOrderId)
        {
            var sale = _saleService.Detail(actor, saleOrderId);
            if (sale.Status != 1) throw new CustomException("只有已完成的销售单可以退货");
            if (sale.ReturnStatus == 2) throw new CustomException("该销售单已全部退货");
            return sale;
        }

        public PagedInfo<JySaleReturnDto> List(JyActor actor, JySaleReturnQueryDto query)
        {
            query ??= new JySaleReturnQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId)
                .WhereIF(query.Status is >= 0 and <= 9, it => it.Status == query.Status)
                .WhereIF(query.BeginTime.HasValue, it => it.ReturnTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.ReturnTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.ReturnNo.Contains(keyword) || it.Reason.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.ReturnTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JySaleReturnDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToReturnDtos(actor.TenantId, rows)
            };
        }

        public JySaleReturnDto Detail(JyActor actor, long id)
        {
            var row = Context.Queryable<JySaleReturn>()
                .First(it => it.Id == id && it.TenantId == actor.TenantId)
                ?? throw new CustomException("退货单不存在");
            return ToReturnDtos(actor.TenantId, new List<JySaleReturn> { row }).First();
        }

        /// <summary>提交退货。支持整单或任意明细组合，退款金额按原单实付比例分摊。</summary>
        public long Save(JyActor actor, JySaleReturnEditDto dto)
        {
            if (dto == null) throw new CustomException("退货单不能为空");
            if (dto.SaleOrderId <= 0) throw new CustomException("请选择原销售单");
            if (dto.Items == null || dto.Items.Count == 0) throw new CustomException("请选择要退货的商品");
            if (dto.Items.Count > 100) throw new CustomException("单张退货单最多支持 100 条明细");
            dto.Reason = TrimText(dto.Reason, 200, "退货原因");
            dto.Remark = TrimText(dto.Remark, 500, "退货备注");
            var duplicate = dto.Items.GroupBy(it => it.SaleOrderItemId).FirstOrDefault(it => it.Key <= 0 || it.Count() > 1);
            if (duplicate != null) throw new CustomException("退货明细重复或无效");

            long resultId = 0;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var sale = Context.Queryable<JySaleOrder>()
                    .With(SqlWith.UpdLock)
                    .First(it => it.Id == dto.SaleOrderId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (sale == null) throw new CustomException("原销售单不存在");
                if (sale.Status != 1) throw new CustomException("只有已完成的销售单可以退货");
                if (sale.ReturnStatus == 2) throw new CustomException("该销售单已全部退货");

                var saleItems = Context.Queryable<JySaleOrderItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.OrderId == sale.Id)
                    .ToList();
                var saleMap = saleItems.ToDictionary(it => it.Id);
                var requests = new List<(JySaleOrderItem Item, decimal Qty)>();
                foreach (var edit in dto.Items)
                {
                    if (!saleMap.TryGetValue(edit.SaleOrderItemId, out var item))
                        throw new CustomException("退货明细不属于当前销售单");
                    if (edit.Qty <= 0) throw new CustomException($"商品 {item.GoodsNo} 的退货数量必须大于 0");
                    var returnable = item.Qty - item.ReturnedQty;
                    if (edit.Qty > returnable)
                        throw new CustomException($"商品 {item.GoodsNo} 最多还能退 {returnable} 件");
                    requests.Add((item, edit.Qty));
                }

                var remainingRefund = Math.Max(0, sale.PayAmount - sale.ReturnedAmount);
                var effectiveRate = sale.TotalAmount > 0 ? Math.Min(1m, sale.PayAmount / sale.TotalAmount) : 0m;
                // 新单按原明细实付单价退款；旧单没有行实付快照时仍按整单实付比例兜底。
                var refundPrices = requests.Select(it => RefundUnitPrice(it.Item, effectiveRate)).ToList();
                var calculated = requests.Select((it, index) =>
                    decimal.Round(refundPrices[index] * it.Qty, 2, MidpointRounding.AwayFromZero)).ToList();
                var refundTotal = calculated.Sum();
                if (refundTotal > remainingRefund)
                {
                    refundTotal = remainingRefund;
                    var running = 0m;
                    for (var i = 0; i < calculated.Count; i++)
                    {
                        if (i == calculated.Count - 1) calculated[i] = decimal.Round(refundTotal - running, 2, MidpointRounding.AwayFromZero);
                        else if (running + calculated[i] > refundTotal) calculated[i] = decimal.Round(Math.Max(0, refundTotal - running), 2, MidpointRounding.AwayFromZero);
                        running += calculated[i];
                    }
                }
                refundTotal = decimal.Round(Math.Max(0, refundTotal), 2, MidpointRounding.AwayFromZero);

                var issued = _billRule.IssueInTransaction(actor, "sale_return", "jy_sale_return", null);
                var header = new JySaleReturn
                {
                    TenantId = actor.TenantId,
                    StoreId = sale.StoreId,
                    ReturnNo = issued.BillNo,
                    SaleOrderId = sale.Id,
                    MemberId = sale.MemberId,
                    Status = 1,
                    RefundAmount = refundTotal,
                    ReturnQty = requests.Sum(it => it.Qty),
                    ReturnTime = now,
                    Reason = dto.Reason,
                    Remark = dto.Remark,
                    Create_by = actor.UserName,
                    Create_time = now
                };
                header.Id = Context.Insertable(header).ExecuteReturnBigIdentity();
                resultId = header.Id;

                for (var i = 0; i < requests.Count; i++)
                {
                    var request = requests[i];
                    Context.Insertable(new JySaleReturnItem
                    {
                        TenantId = actor.TenantId,
                        ReturnId = header.Id,
                        SaleOrderItemId = request.Item.Id,
                        GoodsItemId = request.Item.GoodsItemId,
                        GoodsNo = request.Item.GoodsNo,
                        Barcode = request.Item.Barcode,
                        GoodsName = request.Item.GoodsName,
                        Qty = request.Qty,
                        RefundPrice = refundPrices[i],
                        RefundAmount = calculated[i],
                        CostPrice = request.Item.CostPrice,
                        IsGift = request.Item.IsGift,
                        Create_time = now
                    }).ExecuteCommand();

                    var itemAffected = Context.Updateable<JySaleOrderItem>()
                        .SetColumns(it => it.ReturnedQty == it.ReturnedQty + request.Qty)
                        .Where(it => it.Id == request.Item.Id && it.TenantId == actor.TenantId
                            && it.ReturnedQty == request.Item.ReturnedQty)
                        .ExecuteCommand();
                    if (itemAffected != 1) throw new CustomException($"商品 {request.Item.GoodsNo} 的退货数量已变化，请刷新后重试");
                }

                var goodsGroups = requests.GroupBy(it => it.Item.GoodsItemId);
                foreach (var group in goodsGroups)
                {
                    var qty = group.Sum(it => it.Qty);
                    var goods = Context.Queryable<JyGoodsItem>()
                        .With(SqlWith.UpdLock)
                        .First(it => it.Id == group.Key && it.TenantId == actor.TenantId && it.DelFlag == 0);
                    if (goods == null) throw new CustomException("退货商品档案不存在");
                    var before = goods.Qty;
                    var after = before + qty;
                    // 退货后若该货品仍存在有效定制定金锁定，则恢复为锁定状态，否则回到在库
                    var restore = _saleService.ResolveGoodsRestore(actor, goods.Id);
                    var affected = Context.Updateable<JyGoodsItem>()
                        .SetColumns(it => it.Qty == after)
                        .SetColumns(it => it.Status == restore.Status)
                        .SetColumns(it => it.LockedQty == restore.LockedQty)
                        .SetColumns(it => it.LockBizType == restore.LockBizType)
                        .SetColumns(it => it.LockBizId == restore.LockBizId)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == goods.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                        .ExecuteCommand();
                    if (affected != 1) throw new CustomException("退货商品状态已变化，请刷新后重试");
                    Context.Insertable(new JyStockLedger
                    {
                        TenantId = actor.TenantId,
                        StoreId = sale.StoreId,
                        LedgerNo = LedgerNo(goods.GoodsNo, "XT"),
                        GoodsItemId = goods.Id,
                        GoodsNo = goods.GoodsNo,
                        ChangeType = 5,
                        ChangeQty = qty,
                        BeforeQty = before,
                        AfterQty = after,
                        BizType = "sale_return",
                        BizId = header.Id,
                        BizTime = now,
                        Create_by = actor.UserName,
                        Create_time = now,
                        Remark = sale.OrderNo
                    }).ExecuteCommand();
                }

                var allReturned = saleItems.All(it =>
                {
                    var qty = requests.Where(r => r.Item.Id == it.Id).Sum(r => r.Qty);
                    return it.ReturnedQty + qty >= it.Qty;
                });
                var newReturnedAmount = decimal.Round(Math.Min(sale.PayAmount, sale.ReturnedAmount + refundTotal), 2, MidpointRounding.AwayFromZero);
                var returnAffected = Context.Updateable<JySaleOrder>()
                    .SetColumns(it => it.ReturnedAmount == newReturnedAmount)
                    .SetColumns(it => it.ReturnStatus == (allReturned ? 2 : 1))
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == sale.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    .ExecuteCommand();
                if (returnAffected != 1) throw new CustomException("销售单状态已变化，请刷新后重试");

                ReverseMember(actor, sale, header, newReturnedAmount, allReturned, now);
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "退货提交失败");
            var returned = Context.Queryable<JySaleReturn>().First(it => it.Id == resultId && it.TenantId == actor.TenantId);
            if (returned != null)
                WorkItem.TryRecalculateByBusinessDate(actor, returned.ReturnTime);
            return resultId;
        }

        private static decimal RefundUnitPrice(JySaleOrderItem item, decimal effectiveRate)
        {
            if (item.PayAmount > 0 && item.Qty > 0)
                return decimal.Round(item.PayAmount / item.Qty, 2, MidpointRounding.AwayFromZero);
            return decimal.Round(item.DealPrice * effectiveRate, 2, MidpointRounding.AwayFromZero);
        }

        private void ReverseMember(JyActor actor, JySaleOrder sale, JySaleReturn header, decimal returnedAmount, bool allReturned, DateTime now)
        {
            if (!sale.MemberId.HasValue) return;
            var member = Context.Queryable<JyMember>()
                .With(SqlWith.UpdLock)
                .First(it => it.Id == sale.MemberId.Value && it.TenantId == actor.TenantId && it.DelFlag == 0);
            if (member == null) return;

            var earned = Context.Queryable<JyPointLedger>()
                .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == sale.Id && it.ChangeType == 1)
                .Sum(it => it.ChangePoints);
            var alreadyReversed = Math.Abs(Context.Queryable<JyPointLedger>()
                .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == sale.Id && it.ChangeType == 2)
                .Sum(it => it.ChangePoints));
            var targetReversal = allReturned || sale.PayAmount <= 0
                ? earned
                : Math.Floor(earned * Math.Min(1m, returnedAmount / sale.PayAmount));
            var pointsToReverse = Math.Max(0, targetReversal - alreadyReversed);
            var actualReversal = Math.Min(pointsToReverse, Math.Max(0, member.PointsBalance));
            var newBalance = member.PointsBalance - actualReversal;
            var newYearPoints = Math.Max(0, member.YearPoints - actualReversal);
            var newTotalAmount = Math.Max(0, member.TotalAmount - header.RefundAmount);
            var newYearAmount = Math.Max(0, member.YearAmount - header.RefundAmount);
            var newAvgTicket = member.OrderCount > 0 ? newTotalAmount / member.OrderCount : 0m;

            var affected = Context.Updateable<JyMember>()
                .SetColumns(it => it.PointsBalance == newBalance)
                .SetColumns(it => it.YearPoints == newYearPoints)
                .SetColumns(it => it.TotalAmount == newTotalAmount)
                .SetColumns(it => it.YearAmount == newYearAmount)
                .SetColumns(it => it.AvgTicket == newAvgTicket)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == member.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
            if (affected != 1) throw new CustomException("会员信息已变化，请刷新后重试");
            if (actualReversal > 0)
            {
                Context.Insertable(new JyPointLedger
                {
                    TenantId = actor.TenantId,
                    MemberId = member.Id,
                    ChangeType = 2,
                    ChangePoints = -actualReversal,
                    BalanceAfter = newBalance,
                    YearPointsAfter = newYearPoints,
                    SaleOrderId = sale.Id,
                    ReturnId = header.Id,
                    OperatorId = actor.UserId,
                    Reason = "销售退货 " + header.ReturnNo,
                    BizTime = now,
                    Create_time = now
                }).ExecuteCommand();
            }
        }

        private List<JySaleDto> ToSaleDtos(string tenantId, List<JySaleOrder> rows)
        {
            if (rows.Count == 0) return new List<JySaleDto>();
            var memberIds = rows.Where(it => it.MemberId.HasValue).Select(it => it.MemberId.Value).Distinct().ToList();
            var members = memberIds.Count == 0 ? new Dictionary<long, JyMember>() : Context.Queryable<JyMember>()
                .Where(it => it.TenantId == tenantId && memberIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            return rows.Select(it =>
            {
                members.TryGetValue(it.MemberId ?? 0, out var member);
                return new JySaleDto
                {
                    Id = it.Id,
                    OrderNo = it.OrderNo,
                    OrderType = it.OrderType,
                    MemberId = it.MemberId,
                    MemberName = member?.MemberName,
                    MemberNo = member?.MemberNo,
                    Status = it.Status,
                    TotalAmount = it.TotalAmount,
                    DiscountAmount = it.DiscountAmount,
                    ActivityBenefit = it.ActivityBenefit,
                    BasePayAmount = it.BasePayAmount,
                    AdjustAmount = it.AdjustAmount,
                    RoundOffAmount = it.RoundOffAmount,
                    PayAmount = it.PayAmount,
                    CustomOffsetAmount = it.CustomOffsetAmount,
                    CashAmount = it.CashAmount == 0 && it.CustomOffsetAmount == 0 ? it.PayAmount : it.CashAmount,
                    PieceQty = it.PieceQty,
                    CustomerName = it.CustomerName ?? member?.MemberName,
                    CustomerPhone = it.CustomerPhone ?? member?.Phone,
                    CustomerSex = it.CustomerName != null ? it.CustomerSex : (member?.Sex ?? 0),
                    ReturnedAmount = it.ReturnedAmount,
                    ReturnStatus = it.ReturnStatus,
                    DealTime = it.DealTime,
                    Remark = it.Remark,
                    CreateBy = it.Create_by,
                    CreateTime = it.Create_time
                };
            }).ToList();
        }

        private List<JySaleReturnDto> ToReturnDtos(string tenantId, List<JySaleReturn> rows)
        {
            if (rows.Count == 0) return new List<JySaleReturnDto>();
            var ids = rows.Select(it => it.Id).ToList();
            var saleIds = rows.Select(it => it.SaleOrderId).Distinct().ToList();
            var memberIds = rows.Where(it => it.MemberId.HasValue).Select(it => it.MemberId.Value).Distinct().ToList();
            var sales = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == tenantId && saleIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            var members = memberIds.Count == 0 ? new Dictionary<long, JyMember>() : Context.Queryable<JyMember>()
                .Where(it => it.TenantId == tenantId && memberIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            var items = Context.Queryable<JySaleReturnItem>()
                .Where(it => it.TenantId == tenantId && ids.Contains(it.ReturnId)).ToList()
                .GroupBy(it => it.ReturnId).ToDictionary(it => it.Key, it => it.ToList());
            return rows.Select(it =>
            {
                sales.TryGetValue(it.SaleOrderId, out var sale);
                members.TryGetValue(it.MemberId ?? 0, out var member);
                items.TryGetValue(it.Id, out var lines);
                return new JySaleReturnDto
                {
                    Id = it.Id,
                    ReturnNo = it.ReturnNo,
                    SaleOrderId = it.SaleOrderId,
                    SaleOrderNo = sale?.OrderNo,
                    MemberId = it.MemberId,
                    MemberName = member?.MemberName,
                    CustomerName = sale?.CustomerName ?? member?.MemberName,
                    CustomerPhone = sale?.CustomerPhone ?? member?.Phone,
                    Status = it.Status,
                    RefundAmount = it.RefundAmount,
                    ReturnQty = it.ReturnQty,
                    ReturnTime = it.ReturnTime,
                    Reason = it.Reason,
                    Remark = it.Remark,
                    CreateBy = it.Create_by,
                    CreateTime = it.Create_time,
                    Items = (lines ?? new List<JySaleReturnItem>()).Select(line => new JySaleReturnItemDto
                    {
                        Id = line.Id,
                        SaleOrderItemId = line.SaleOrderItemId,
                        GoodsItemId = line.GoodsItemId,
                        GoodsNo = line.GoodsNo,
                        Barcode = line.Barcode,
                        GoodsName = line.GoodsName,
                        Qty = line.Qty,
                        RefundPrice = line.RefundPrice,
                        RefundAmount = line.RefundAmount,
                        IsGift = line.IsGift
                    }).ToList()
                };
            }).ToList();
        }

        private static string LedgerNo(string goodsNo, string suffix)
        {
            var value = goodsNo + "-" + suffix + "-" + DateTime.Now.ToString("HHmmssfff");
            return value.Length <= 32 ? value : value.Substring(0, 32);
        }

        private static string TrimText(string value, int max, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > max) throw new CustomException(label + "不能超过 " + max + " 个字");
            return value;
        }
    }
}
