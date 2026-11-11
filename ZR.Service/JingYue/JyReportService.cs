using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    /// <summary>
    /// 销售经营指标。金额取销售单实际实付 PayAmount；退货按退货发生时间从对应周期冲减。
    /// 客件数 = 净销售件数 / 成交单数，件单数 = 净销售额 / 净销售件数。
    /// </summary>
    [AppService]
    public class JyReportService : BaseService<JySaleOrder>
    {
        public JyTrafficReportDto StoreTraffic(JyActor actor, JyTrafficQueryDto query)
        {
            query ??= new JyTrafficQueryDto();
            var today = DateTime.Today;
            var begin = (query.BeginTime ?? today).Date;
            var end = (query.EndTime ?? today).Date;
            if (begin > end) throw new CustomException("开始日期不能晚于结束日期");
            if ((end - begin).TotalDays > 3660) throw new CustomException("查询区间不能超过 10 年");

            var rows = Context.Queryable<JyStoreTraffic>()
                .Where(it => it.TenantId == actor.TenantId && it.TrafficDate >= begin && it.TrafficDate < end.AddDays(1))
                .ToList();
            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= begin && it.DealTime < end.AddDays(1))
                .Select(it => new { it.Id, it.DealTime, it.PayAmount })
                .ToList();
            var returns = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 1
                    && it.ReturnTime >= begin && it.ReturnTime < end.AddDays(1))
                .Select(it => new { it.ReturnTime, it.RefundAmount })
                .ToList();

            var granularity = string.IsNullOrWhiteSpace(query.Granularity) ? "day" : query.Granularity.Trim().ToLowerInvariant();
            if (granularity is not ("day" or "week" or "month" or "year"))
                throw new CustomException("统计粒度不正确");
            var trend = BuildTrafficTrend(begin, end, granularity, rows,
                orders.Select(it => new TrafficEvent { Time = it.DealTime, Amount = it.PayAmount }).ToList(),
                returns.Select(it => new TrafficEvent { Time = it.ReturnTime, Amount = it.RefundAmount }).ToList(), today);
            var totalVisitors = rows.Sum(it => it.VisitorCount);
            var totalOrders = orders.Count;
            var totalAmount = Round(orders.Sum(it => it.PayAmount) - returns.Sum(it => it.RefundAmount));
            return new JyTrafficReportDto
            {
                BeginTime = begin,
                EndTime = end,
                Granularity = granularity,
                VisitorCount = totalVisitors,
                WechatCount = rows.Sum(it => it.WechatCount),
                DealOrderCount = totalOrders,
                DealAmount = totalAmount,
                DealRate = totalVisitors == 0 ? 0 : Round(totalOrders * 100m / totalVisitors),
                WechatRate = totalVisitors == 0 ? 0 : Round(rows.Sum(it => it.WechatCount) * 100m / totalVisitors),
                LostStrollCount = rows.Sum(it => it.LostStrollCount),
                LostPriceCount = rows.Sum(it => it.LostPriceCount),
                LostStyleCount = rows.Sum(it => it.LostStyleCount),
                LostConsiderCount = rows.Sum(it => it.LostConsiderCount),
                LostTotal = rows.Sum(it => it.LostStrollCount + it.LostPriceCount + it.LostStyleCount + it.LostConsiderCount),
                Trend = trend
            };
        }

        public long SaveTraffic(JyActor actor, JyTrafficSaveDto dto)
        {
            if (dto == null) throw new CustomException("客流数据不能为空");
            var date = dto.TrafficDate.Date;
            if (date > DateTime.Today) throw new CustomException("不能登记未来日期的客流");
            var values = new[] { dto.VisitorCount, dto.WechatCount, dto.LostStrollCount, dto.LostPriceCount, dto.LostStyleCount, dto.LostConsiderCount };
            if (values.Any(it => it < 0 || it > 1000000)) throw new CustomException("客流人数必须是 0 到 1000000 之间的整数");
            if (dto.WechatCount > dto.VisitorCount) throw new CustomException("加微信人数不能超过进店人数");
            if (dto.LostStrollCount + dto.LostPriceCount + dto.LostStyleCount + dto.LostConsiderCount > dto.VisitorCount)
                throw new CustomException("流失分类合计不能超过进店人数");

            var current = Context.Queryable<JyStoreTraffic>()
                .First(it => it.TenantId == actor.TenantId && it.TrafficDate == date && it.StoreId == dto.StoreId);
            if (date != DateTime.Today && !dto.ConfirmHistorical)
                throw new CustomException("历史客流默认只读，修改前请确认");
            if (current == null)
            {
                return Context.Insertable(new JyStoreTraffic
                {
                    TenantId = actor.TenantId,
                    StoreId = dto.StoreId,
                    TrafficDate = date,
                    VisitorCount = dto.VisitorCount,
                    WechatCount = dto.WechatCount,
                    LostStrollCount = dto.LostStrollCount,
                    LostPriceCount = dto.LostPriceCount,
                    LostStyleCount = dto.LostStyleCount,
                    LostConsiderCount = dto.LostConsiderCount,
                    Remark = TrimRemark(dto.Remark),
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now
                }).ExecuteReturnBigIdentity();
            }
            current.VisitorCount = dto.VisitorCount;
            current.WechatCount = dto.WechatCount;
            current.LostStrollCount = dto.LostStrollCount;
            current.LostPriceCount = dto.LostPriceCount;
            current.LostStyleCount = dto.LostStyleCount;
            current.LostConsiderCount = dto.LostConsiderCount;
            current.Remark = TrimRemark(dto.Remark);
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current).UpdateColumns(it => new
            {
                it.VisitorCount, it.WechatCount, it.LostStrollCount, it.LostPriceCount,
                it.LostStyleCount, it.LostConsiderCount, it.Remark, it.Update_by, it.Update_time
            }).ExecuteCommand();
            return current.Id;
        }

        public JyStylePerformanceReportDto StylePerformance(JyActor actor, JyStylePerformanceQueryDto query)
        {
            query ??= new JyStylePerformanceQueryDto();
            var end = (query.EndTime?.Date ?? DateTime.Today);
            var begin = (query.BeginTime?.Date ?? end.AddDays(-29));
            if (begin > end) throw new CustomException("开始日期不能晚于结束日期");
            if ((end - begin).TotalDays > 3660) throw new CustomException("查询区间不能超过 10 年");
            var endExclusive = end.AddDays(1);

            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= begin && it.DealTime < endExclusive)
                .Select(it => new { it.Id, it.PayAmount, it.CostAmount })
                .ToList();
            var orderMap = orders.ToDictionary(it => it.Id);
            var items = orders.Count == 0 ? new List<JySaleOrderItem>() : Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && orderMap.Keys.Contains(it.OrderId))
                .ToList();
            var returns = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 1
                    && it.ReturnTime >= begin && it.ReturnTime < endExclusive)
                .Select(it => new { it.Id })
                .ToList();
            var returnIds = returns.Select(it => it.Id).ToList();
            var returnItems = returnIds.Count == 0 ? new List<JySaleReturnItem>() : Context.Queryable<JySaleReturnItem>()
                .Where(it => it.TenantId == actor.TenantId && returnIds.Contains(it.ReturnId))
                .ToList();

            var styleItems = StyleItems(actor.TenantId);
            var styleMap = styleItems.ToDictionary(it => it.Id);
            var parentMap = CanonicalParents(styleItems);
            var itemMap = items.ToDictionary(it => it.Id);
            var leafMap = LeafCategories(styleItems, parentMap, out var leafAlias);

            // 报表固定展示当前全部一级款式，未销售分类保留为零值，便于横向比较。
            var groups = parentMap.Values
                .GroupBy(it => it.Id)
                .Select(it => it.First())
                .OrderBy(it => it.SortNo)
                .ThenBy(it => it.Id)
                .ToDictionary(it => it.Id, it => new StyleBucket
                {
                    CategoryId = it.Id,
                    CategoryName = it.ItemName,
                    ParentCategoryId = null,
                    Children = leafMap.Values.Where(child => child.ParentId == it.Id)
                        .GroupBy(child => child.Id)
                        .Select(child => child.First())
                        .OrderBy(child => child.SortNo)
                        .ThenBy(child => child.Id)
                        .ToDictionary(child => child.Id, child => new StyleBucket
                        {
                            CategoryId = child.Id,
                            CategoryName = child.ItemName,
                            ParentCategoryId = it.Id
                        })
                });
            var orderLineTotals = items.GroupBy(it => it.OrderId).ToDictionary(it => it.Key, it => it.Sum(x => x.DealPrice * x.Qty));
            foreach (var item in items)
            {
                var (rootId, leafId) = ResolveCategoryPath(item.StyleItemId, styleMap, parentMap);
                if (leafId.HasValue) leafId = leafAlias.GetValueOrDefault(leafId.Value, leafId.Value);
                if (!groups.TryGetValue(rootId, out var bucket))
                {
                    bucket = new StyleBucket { CategoryId = rootId, CategoryName = "未分类" };
                    groups.Add(rootId, bucket);
                }
                var order = orderMap[item.OrderId];
                var lineGross = decimal.Round(item.DealPrice * item.Qty, 2, MidpointRounding.AwayFromZero);
                var totalLine = orderLineTotals[item.OrderId];
                // 订单级折扣按明细原价占比分摊，保证分类金额合计与报表实付一致。
                var ratio = totalLine <= 0 ? 0 : Math.Min(1m, Math.Max(0m, order.PayAmount / totalLine));
                var netLine = decimal.Round(lineGross * ratio, 2, MidpointRounding.AwayFromZero);
                bucket.SalesAmount += lineGross;
                bucket.NetAmount += netLine;
                bucket.SalesPieces += item.Qty;
                bucket.NetPieces += item.Qty;
                bucket.CostAmount += item.CostPrice * item.Qty;
                bucket.OrderIds.Add(item.OrderId);
                var leaf = leafId.HasValue && bucket.Children.TryGetValue(leafId.Value, out var foundLeaf) ? foundLeaf : null;
                if (leaf != null)
                {
                    leaf.SalesAmount += lineGross;
                    leaf.NetAmount += netLine;
                    leaf.SalesPieces += item.Qty;
                    leaf.NetPieces += item.Qty;
                    leaf.CostAmount += item.CostPrice * item.Qty;
                    leaf.OrderIds.Add(item.OrderId);
                }
            }
            foreach (var item in returnItems)
            {
                if (!itemMap.TryGetValue(item.SaleOrderItemId, out var saleItem)) continue;
                var (rootId, leafId) = ResolveCategoryPath(saleItem.StyleItemId, styleMap, parentMap);
                if (leafId.HasValue) leafId = leafAlias.GetValueOrDefault(leafId.Value, leafId.Value);
                if (!groups.TryGetValue(rootId, out var bucket))
                {
                    bucket = new StyleBucket { CategoryId = rootId, CategoryName = "未分类" };
                    groups.Add(rootId, bucket);
                }
                bucket.ReturnAmount += item.RefundAmount;
                bucket.ReturnPieces += item.Qty;
                bucket.NetAmount -= item.RefundAmount;
                bucket.NetPieces -= item.Qty;
                bucket.CostAmount -= item.CostPrice * item.Qty;
                var leaf = leafId.HasValue && bucket.Children.TryGetValue(leafId.Value, out var foundLeaf) ? foundLeaf : null;
                if (leaf != null)
                {
                    leaf.ReturnAmount += item.RefundAmount;
                    leaf.ReturnPieces += item.Qty;
                    leaf.NetAmount -= item.RefundAmount;
                    leaf.NetPieces -= item.Qty;
                    leaf.CostAmount -= item.CostPrice * item.Qty;
                }
            }

            var totalNet = Round(groups.Values.Sum(it => it.NetAmount));
            var rows = groups.Values.Select(it => ToStyleRow(it, totalNet)).ToList();
            var sortBy = (query.SortBy ?? "amount").Trim().ToLowerInvariant();
            // 没有任何销售或退货的分类始终置底，避免零值分类挤占经营重点。
            var ordered = sortBy switch
            {
                "gross" => rows.OrderByDescending(it => it.GrossProfit),
                "grossrate" => rows.OrderByDescending(it => it.GrossRate),
                "pieces" => rows.OrderByDescending(it => it.NetPieces),
                "avgticket" => rows.OrderByDescending(it => it.AvgTicket),
                _ => rows.OrderByDescending(it => it.NetAmount)
            };
            rows = ordered
                .ThenByDescending(it => it.SalesPieces + it.ReturnPieces > 0)
                .ThenByDescending(it => it.NetAmount)
                .ToList();
            var totalCost = Round(rows.Sum(it => it.CostAmount));
            var totalGross = Round(rows.Sum(it => it.GrossProfit));
            return new JyStylePerformanceReportDto
            {
                BeginTime = begin,
                EndTime = end,
                SalesAmount = Round(rows.Sum(it => it.SalesAmount)),
                ReturnAmount = Round(rows.Sum(it => it.ReturnAmount)),
                NetAmount = totalNet,
                NetPieces = Round(rows.Sum(it => it.NetPieces)),
                CostAmount = totalCost,
                GrossProfit = totalGross,
                GrossRate = totalNet <= 0 ? 0 : Round(totalGross * 100m / totalNet),
                OrderCount = orders.Count,
                Rows = rows
            };
        }

        /// <summary>
        /// 销售流水报表。销售额取销售明细最终实付，抹零只单独展示、不再加回销售额；
        /// 退货按退货发生日期冲减；成本同步冲减，保证利润率与净额口径一致。
        /// </summary>
        public JySalesLedgerReportDto SalesLedger(JyActor actor, JySalesLedgerQueryDto query)
        {
            query ??= new JySalesLedgerQueryDto();
            var end = (query.EndTime?.Date ?? DateTime.Today);
            var begin = (query.BeginTime?.Date ?? end);
            if (begin > end) throw new CustomException("开始日期不能晚于结束日期");
            if ((end - begin).TotalDays > 3660) throw new CustomException("查询区间不能超过 10 年");
            var endExclusive = end.AddDays(1);
            var groupBy = (query.GroupBy ?? "material").Trim().ToLowerInvariant();
            if (groupBy is not ("material" or "style" or "category"))
                throw new CustomException("汇总维度不正确");

            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= begin && it.DealTime < endExclusive)
                .ToList();
            var orderIds = orders.Select(it => it.Id).ToList();
            var items = orderIds.Count == 0 ? new List<JySaleOrderItem>() : Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && orderIds.Contains(it.OrderId))
                // 报表只读取计算所需字段，避免旧库尚未同步的库存属性列导致整张报表不可用。
                .Select(it => new JySaleOrderItem
                {
                    Id = it.Id,
                    TenantId = it.TenantId,
                    OrderId = it.OrderId,
                    GoodsItemId = it.GoodsItemId,
                    GoodsNo = it.GoodsNo,
                    Barcode = it.Barcode,
                    GoodsName = it.GoodsName,
                    StyleId = it.StyleId,
                    StyleName = it.StyleName,
                    StyleItemId = it.StyleItemId,
                    StyleItemName = it.StyleItemName,
                    MaterialItemId = it.MaterialItemId,
                    MaterialGroup = it.MaterialGroup,
                    MaterialName = it.MaterialName,
                    MetalItemId = it.MetalItemId,
                    MetalName = it.MetalName,
                    Weight = it.Weight,
                    Unit = it.Unit,
                    Qty = it.Qty,
                    CostPrice = it.CostPrice,
                    ListAmount = it.ListAmount,
                    MemberDiscountAmount = it.MemberDiscountAmount,
                    ActivityBenefit = it.ActivityBenefit,
                    PayAmount = it.PayAmount
                })
                .OrderBy(it => it.OrderId)
                .OrderBy(it => it.SortNo)
                .ToList();

            var returns = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 1
                    && it.ReturnTime >= begin && it.ReturnTime < endExclusive)
                .ToList();
            var returnIds = returns.Select(it => it.Id).ToList();
            var returnItems = returnIds.Count == 0 ? new List<JySaleReturnItem>() : Context.Queryable<JySaleReturnItem>()
                .Where(it => it.TenantId == actor.TenantId && returnIds.Contains(it.ReturnId))
                .ToList();

            // 退货可能对应查询区间之前售出的商品，必须单独补齐这些销售明细，才能正确归类和冲减成本。
            var itemMap = items.ToDictionary(it => it.Id);
            var missingItemIds = returnItems.Select(it => it.SaleOrderItemId).Distinct()
                .Where(id => !itemMap.ContainsKey(id)).ToList();
            if (missingItemIds.Count > 0)
            {
                Context.Queryable<JySaleOrderItem>()
                    .Where(it => it.TenantId == actor.TenantId && missingItemIds.Contains(it.Id))
                    .ToList().ForEach(it => itemMap[it.Id] = it);
            }

            // 查询区间内发生的退货可能对应更早的销售单。补齐全量订单快照，明细页才能展示退货来源。
            var orderMap = orders.ToDictionary(it => it.Id);
            var missingOrderIds = missingItemIds
                .Select(id => itemMap.TryGetValue(id, out var item) ? item.OrderId : 0)
                .Where(id => id > 0 && !orderMap.ContainsKey(id))
                .Distinct().ToList();
            if (missingOrderIds.Count > 0)
            {
                Context.Queryable<JySaleOrder>()
                    .Where(it => it.TenantId == actor.TenantId && missingOrderIds.Contains(it.Id))
                    .ToList().ForEach(it => orderMap[it.Id] = it);
            }
            var returnBySaleItem = returnItems
                .GroupBy(it => it.SaleOrderItemId)
                .ToDictionary(it => it.Key, it => new ReturnAggregate
                {
                    Qty = it.Sum(x => x.Qty),
                    Amount = it.Sum(x => x.RefundAmount),
                    Cost = it.Sum(x => x.CostPrice * x.Qty)
                });

            // 抹零是订单级金额，按该单各明细实付占比分摊；最后一笔承接舍入差额，保证合计完全对齐。
            var roundOffByItem = new Dictionary<long, decimal>();
            foreach (var order in orders)
            {
                var orderItems = items.Where(it => it.OrderId == order.Id).ToList();
                if (orderItems.Count == 0) continue;
                var basis = orderItems.Sum(it => Math.Max(0, it.PayAmount));
                var roundOff = Round(Math.Max(0, order.RoundOffAmount));
                if (basis <= 0 || roundOff <= 0) continue;
                var allocated = 0m;
                for (var i = 0; i < orderItems.Count; i++)
                {
                    var item = orderItems[i];
                    var amount = i == orderItems.Count - 1
                        ? Round(roundOff - allocated)
                        : Round(roundOff * Math.Max(0, item.PayAmount) / basis);
                    roundOffByItem[item.Id] = amount;
                    allocated = Round(allocated + amount);
                }
            }

            var styleItems = StyleItems(actor.TenantId);
            var styleMap = styleItems.ToDictionary(it => it.Id);
            var parentMap = CanonicalParents(styleItems);
            var leafMap = LeafCategories(styleItems, parentMap, out var leafAlias);
            var groups = new Dictionary<string, SalesLedgerBucket>();
            var totalNetForShare = 0m;

            void Accumulate(JySaleOrderItem item, decimal salesAmount, decimal costAmount,
                decimal returnAmount, decimal discountAmount, decimal roundOffAmount,
                decimal salesPieces, decimal returnPieces, long? orderId, bool isReturn)
            {
                var group = ResolveSalesLedgerGroup(item, groupBy, styleMap, parentMap, leafAlias);
                if (!groups.TryGetValue(group.Key, out var bucket))
                {
                    bucket = new SalesLedgerBucket
                    {
                        Key = group.Key,
                        Name = group.Name,
                        Level = groupBy,
                        MaterialGroupName = MaterialGroups().GetValueOrDefault(
                            item.MaterialGroup is >= 1 and <= 3 ? item.MaterialGroup : 3),
                        MaterialName = item.MaterialName ?? "未填写材质",
                        StyleParentName = group.StyleParentName,
                        StyleItemName = group.StyleItemName
                    };
                    groups.Add(group.Key, bucket);
                }
                bucket.SalesAmount += salesAmount;
                bucket.CostAmount += costAmount;
                bucket.ReturnAmount += returnAmount;
                bucket.DiscountAmount += discountAmount;
                bucket.RoundOffAmount += roundOffAmount;
                bucket.SalesPieces += salesPieces;
                bucket.ReturnPieces += returnPieces;
                if (orderId.HasValue && !isReturn) bucket.OrderIds.Add(orderId.Value);
            }

            foreach (var item in items)
            {
                var roundOff = roundOffByItem.GetValueOrDefault(item.Id, 0m);
                // 行 PayAmount 已经是分摊后的最终实付，抹零已经包含在内，不能再加一次。
                var salesAmount = Round(Math.Max(0, item.PayAmount));
                var costAmount = Round(item.CostPrice * item.Qty);
                var discountAmount = Round(item.MemberDiscountAmount + item.ActivityBenefit);
                Accumulate(item, salesAmount, costAmount, 0m, discountAmount, roundOff,
                    item.Qty, 0m, item.OrderId, false);
                totalNetForShare += salesAmount;
            }
            foreach (var returnItem in returnItems)
            {
                if (!itemMap.TryGetValue(returnItem.SaleOrderItemId, out var saleItem)) continue;
                Accumulate(saleItem, 0m, -Round(returnItem.CostPrice * returnItem.Qty),
                    Round(returnItem.RefundAmount), 0m, 0m, 0m, returnItem.Qty, null, true);
            }

            var rows = groups.Values.Select(it => ToSalesLedgerRow(it, totalNetForShare)).ToList();
            rows = rows.OrderByDescending(it => it.NetAmount)
                .ThenByDescending(it => it.SalesAmount)
                .ThenBy(it => it.GroupName)
                .ToList();

            var currentItemIds = items.Select(it => it.Id).ToHashSet();
            var returnMap = returns.ToDictionary(it => it.Id);
            var details = items.Select(item =>
            {
                var order = orderMap[item.OrderId];
                returnBySaleItem.TryGetValue(item.Id, out var returned);
                returned ??= new ReturnAggregate();
                var roundOff = roundOffByItem.GetValueOrDefault(item.Id, 0m);
                var salesAmount = Round(Math.Max(0, item.PayAmount));
                var costAmount = Round(item.CostPrice * item.Qty - returned.Cost);
                var netAmount = Round(salesAmount - returned.Amount);
                var style = ResolveSalesLedgerGroup(item, "category", styleMap, parentMap, leafAlias);
                return new JySalesLedgerDetailDto
                {
                    OrderId = order.Id,
                    ItemId = item.Id,
                    DetailKey = "sale:" + item.Id,
                    OrderNo = order.OrderNo,
                    DealTime = order.DealTime,
                    CustomerName = order.CustomerName,
                    CustomerPhone = order.CustomerPhone,
                    GoodsNo = item.GoodsNo,
                    Barcode = item.Barcode,
                    GoodsName = item.GoodsName,
                    MaterialGroupName = MaterialGroups().GetValueOrDefault(
                        item.MaterialGroup is >= 1 and <= 3 ? item.MaterialGroup : 3),
                    MaterialName = item.MaterialName,
                    StyleParentName = style.StyleParentName,
                    StyleItemName = style.StyleItemName,
                    MetalName = item.MetalName,
                    Weight = item.Weight,
                    Unit = item.Unit,
                    Qty = item.Qty,
                    ListAmount = Round(item.ListAmount),
                    DiscountAmount = Round(item.MemberDiscountAmount + item.ActivityBenefit),
                    PayAmount = salesAmount,
                    RoundOffAmount = roundOff,
                    CostAmount = costAmount,
                    GrossProfit = Round(netAmount - costAmount),
                    GrossRate = netAmount <= 0 ? 0 : Round((netAmount - costAmount) * 100m / netAmount),
                    ReturnQty = returned.Qty,
                    ReturnAmount = Round(returned.Amount),
                    NetAmount = netAmount,
                    OrderType = order.OrderType,
                    OrderTypeName = order.OrderType switch { 2 => "赠送", 3 => "代理拿货", _ => "正品销售" }
                };
            }).ToList();

            // 本期退货对应的销售发生在期外时，也补一行退货明细，避免汇总金额与明细金额对不上。
            var returnOnlyDetails = returnItems
                .Where(it => !currentItemIds.Contains(it.SaleOrderItemId))
                .Select(returnItem =>
                {
                    if (!itemMap.TryGetValue(returnItem.SaleOrderItemId, out var saleItem)
                        || !orderMap.TryGetValue(saleItem.OrderId, out var saleOrder)) return null;
                    returnMap.TryGetValue(returnItem.ReturnId, out var returnOrder);
                    var style = ResolveSalesLedgerGroup(saleItem, "category", styleMap, parentMap, leafAlias);
                    var refund = Round(returnItem.RefundAmount);
                    var returnCost = Round(returnItem.CostPrice * returnItem.Qty);
                    return new JySalesLedgerDetailDto
                    {
                        OrderId = saleOrder.Id,
                        ItemId = saleItem.Id,
                        DetailKey = "return:" + returnItem.Id,
                        OrderNo = saleOrder.OrderNo,
                        DealTime = saleOrder.DealTime,
                        ReturnTime = returnOrder?.ReturnTime,
                        CustomerName = saleOrder.CustomerName,
                        CustomerPhone = saleOrder.CustomerPhone,
                        GoodsNo = saleItem.GoodsNo,
                        Barcode = saleItem.Barcode,
                        GoodsName = saleItem.GoodsName,
                        MaterialGroupName = MaterialGroups().GetValueOrDefault(
                            saleItem.MaterialGroup is >= 1 and <= 3 ? saleItem.MaterialGroup : 3),
                        MaterialName = saleItem.MaterialName,
                        StyleParentName = style.StyleParentName,
                        StyleItemName = style.StyleItemName,
                        MetalName = saleItem.MetalName,
                        Weight = saleItem.Weight,
                        Unit = saleItem.Unit,
                        Qty = 0m,
                        ListAmount = 0m,
                        DiscountAmount = 0m,
                        PayAmount = 0m,
                        RoundOffAmount = 0m,
                        CostAmount = -returnCost,
                        GrossProfit = Round(returnCost - refund),
                        GrossRate = 0m,
                        ReturnQty = returnItem.Qty,
                        ReturnAmount = refund,
                        NetAmount = -refund,
                        OrderType = saleOrder.OrderType,
                        OrderTypeName = "销售退货"
                    };
                })
                .Where(it => it != null)
                .ToList();
            details.AddRange(returnOnlyDetails);
            details = details.OrderByDescending(it => it.ReturnTime ?? it.DealTime)
                .ThenByDescending(it => it.ItemId).ToList();

            var totalSales = Round(rows.Sum(it => it.SalesAmount));
            var totalCost = Round(rows.Sum(it => it.CostAmount));
            var totalReturn = Round(rows.Sum(it => it.ReturnAmount));
            var totalNet = Round(rows.Sum(it => it.NetAmount));
            var totalGross = Round(totalNet - totalCost);
            return new JySalesLedgerReportDto
            {
                BeginTime = begin,
                EndTime = end,
                GroupBy = groupBy,
                SalesAmount = totalSales,
                CostAmount = totalCost,
                ReturnAmount = totalReturn,
                NetAmount = totalNet,
                GrossProfit = totalGross,
                GrossRate = totalNet <= 0 ? 0 : Round(totalGross * 100m / totalNet),
                DiscountAmount = Round(rows.Sum(it => it.DiscountAmount)),
                RoundOffAmount = Round(rows.Sum(it => it.RoundOffAmount)),
                SalesPieces = Round(rows.Sum(it => it.SalesPieces)),
                ReturnPieces = Round(rows.Sum(it => it.ReturnPieces)),
                OrderCount = orders.Count,
                Rows = rows,
                Details = details
            };
        }

        public JyInventoryReportDto Inventory(JyActor actor, JyInventoryQueryDto query)
        {
            query ??= new JyInventoryQueryDto();
            var today = DateTime.Today;
            var begin = (query.BeginTime ?? today.AddDays(-29)).Date;
            var endDay = (query.EndTime ?? today).Date;
            if (begin > endDay) throw new CustomException("开始日期不能晚于结束日期");
            if ((endDay - begin).TotalDays > 3660) throw new CustomException("查询区间不能超过 10 年");
            var endExclusive = endDay.AddDays(1);

            var goods = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ToList();
            var styleItems = StyleItems(actor.TenantId);
            var styleMap = styleItems.ToDictionary(it => it.Id);
            var parentMap = CanonicalParents(styleItems);
            var leafMap = LeafCategories(styleItems, parentMap, out var leafAlias);
            var goodsIds = goods.Select(it => it.Id).ToList();
            // Historical ending stock is reconstructed from today's stock by reversing
            // every movement from the end of the requested period onward. Starting at
            // the period begin is therefore insufficient: later ledgers must also be loaded.
            var ledgers = LoadLedgers(actor.TenantId, goodsIds, begin);
            ApplyGoodsFilters(goods, styleMap, parentMap, leafAlias, query.MaterialGroup, query.StyleParentId, query.StyleItemId);
            var ledgerByGoods = ledgers.GroupBy(it => it.GoodsItemId).ToDictionary(it => it.Key, it => it.ToList());

            var rootMap = MaterialGroups().ToDictionary(it => it.Key, it => new InventoryBucket
            {
                Level = "material", MaterialGroup = it.Key, MaterialGroupName = it.Value
            });
            var rootByMaterial = rootMap.Values.ToDictionary(it => it.MaterialGroup);
            foreach (var item in goods)
            {
                var path = ResolveStylePath(item.StyleItemId, styleMap, parentMap, leafAlias);
                var materialGroup = item.MaterialGroup is >= 1 and <= 3 ? item.MaterialGroup : 3;
                var root = rootByMaterial[materialGroup];
                var parentBucket = root.Children.FirstOrDefault(it => it.StyleParentId == path.ParentId);
                if (parentBucket == null)
                {
                    parentBucket = new InventoryBucket
                    {
                        Level = "styleParent", MaterialGroup = materialGroup, MaterialGroupName = root.MaterialGroupName,
                        StyleParentId = path.ParentId, StyleParentName = path.ParentName
                    };
                    root.Children.Add(parentBucket);
                }
                var leafBucket = parentBucket.Children.FirstOrDefault(it => it.StyleItemId == path.LeafId
                    && string.Equals(it.StyleItemName, path.LeafName, StringComparison.Ordinal));
                if (leafBucket == null)
                {
                    leafBucket = new InventoryBucket
                    {
                        Level = "styleItem", MaterialGroup = materialGroup, MaterialGroupName = root.MaterialGroupName,
                        StyleParentId = path.ParentId, StyleParentName = path.ParentName,
                        StyleItemId = path.LeafId, StyleItemName = path.LeafName
                    };
                    parentBucket.Children.Add(leafBucket);
                }
                var flow = BuildGoodsFlow(item, ledgerByGoods.GetValueOrDefault(item.Id), begin, endExclusive);
                foreach (var bucket in new[] { root, parentBucket, leafBucket })
                {
                    bucket.ItemIds.Add(item.Id);
                    bucket.OpeningQty += flow.OpeningQty;
                    bucket.InboundQty += flow.InboundQty;
                    bucket.OutboundQty += flow.OutboundQty;
                    bucket.AdjustQty += flow.AdjustQty;
                    bucket.EndingQty += flow.EndingQty;
                    bucket.StockCost += StockCost(item);
                    bucket.StockSale += StockSale(item);
                    bucket.StockWeight += StockWeight(item);
                }
            }

            var roots = rootMap.Values.OrderBy(it => it.MaterialGroup)
                .Where(it => it.ItemIds.Count > 0).Select(it => ToInventoryRow(it, true)).ToList();
            return new JyInventoryReportDto
            {
                BeginTime = begin, EndTime = endDay,
                ItemCount = goods.Count,
                OpeningQty = Round(roots.Sum(it => it.OpeningQty)),
                InboundQty = Round(roots.Sum(it => it.InboundQty)),
                OutboundQty = Round(roots.Sum(it => it.OutboundQty)),
                AdjustQty = Round(roots.Sum(it => it.AdjustQty)),
                EndingQty = Round(roots.Sum(it => it.EndingQty)),
                StockCost = Round(roots.Sum(it => it.StockCost)),
                StockSale = Round(roots.Sum(it => it.StockSale)),
                StockWeight = Round(roots.Sum(it => it.StockWeight)),
                Rows = roots
            };
        }

        public JyStockAgeReportDto StockAge(JyActor actor, JyStockAgeQueryDto query)
        {
            query ??= new JyStockAgeQueryDto();
            var asOf = NormalizeAsOf(query.AsOfDate);
            var goods = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                    && it.Qty > 0 && (it.Status == 1 || it.Status == 2))
                .ToList();
            var styleItems = StyleItems(actor.TenantId);
            var styleMap = styleItems.ToDictionary(it => it.Id);
            var parentMap = CanonicalParents(styleItems);
            var leafMap = LeafCategories(styleItems, parentMap, out var leafAlias);
            ApplyGoodsFilters(goods, styleMap, parentMap, leafAlias, query.MaterialGroup, query.StyleParentId, query.StyleItemId);
            var inboundTimes = LoadFirstInboundTimes(actor.TenantId, goods, asOf);

            var roots = MaterialGroups().ToDictionary(it => it.Key, it => new StockAgeBucket
            {
                Level = "material", MaterialGroup = it.Key, MaterialGroupName = it.Value
            });
            foreach (var item in goods)
            {
                var path = ResolveStylePath(item.StyleItemId, styleMap, parentMap, leafAlias);
                var materialGroup = item.MaterialGroup is >= 1 and <= 3 ? item.MaterialGroup : 3;
                var root = roots[materialGroup];
                var parentBucket = root.Children.FirstOrDefault(it => it.StyleParentId == path.ParentId);
                if (parentBucket == null)
                {
                    parentBucket = new StockAgeBucket
                    {
                        Level = "styleParent", MaterialGroup = materialGroup, MaterialGroupName = root.MaterialGroupName,
                        StyleParentId = path.ParentId, StyleParentName = path.ParentName
                    };
                    root.Children.Add(parentBucket);
                }
                var leafBucket = parentBucket.Children.FirstOrDefault(it => it.StyleItemId == path.LeafId
                    && string.Equals(it.StyleItemName, path.LeafName, StringComparison.Ordinal));
                if (leafBucket == null)
                {
                    leafBucket = new StockAgeBucket
                    {
                        Level = "styleItem", MaterialGroup = materialGroup, MaterialGroupName = root.MaterialGroupName,
                        StyleParentId = path.ParentId, StyleParentName = path.ParentName,
                        StyleItemId = path.LeafId, StyleItemName = path.LeafName
                    };
                    parentBucket.Children.Add(leafBucket);
                }
                var inboundTime = inboundTimes.GetValueOrDefault(item.Id);
                if (inboundTime == default) inboundTime = item.Create_time.Date;
                var ageDays = Math.Max(0, (asOf - inboundTime.Date).Days);
                var bucketKey = AgeBucketKey(ageDays);
                foreach (var bucket in new[] { root, parentBucket, leafBucket })
                {
                    bucket.ItemIds.Add(item.Id);
                    bucket.TotalQty += item.Qty;
                    bucket.TotalCost += StockCost(item);
                    bucket.TotalWeight += StockWeight(item);
                    bucket.BucketQty[bucketKey] += item.Qty;
                    bucket.BucketCost[bucketKey] += StockCost(item);
                }
            }
            var totalQty = goods.Sum(it => it.Qty);
            var totalCost = goods.Sum(it => StockCost(it));
            var totalWeight = goods.Sum(it => StockWeight(it));
            var rows = roots.Values.OrderBy(it => it.MaterialGroup)
                .Where(it => it.ItemIds.Count > 0).Select(it => ToStockAgeRow(it, totalQty, totalCost, true)).ToList();
            return new JyStockAgeReportDto
            {
                AsOfDate = asOf,
                ItemCount = goods.Count,
                TotalQty = Round(totalQty),
                TotalCost = Round(totalCost),
                TotalWeight = Round(totalWeight),
                Buckets = AgeBuckets(Accumulate(roots.Values, totalQty, totalCost)),
                Rows = rows
            };
        }

        public PagedInfo<JyStockAgeDetailRowDto> StockAgeDetails(JyActor actor, JyStockAgeDetailQueryDto query)
        {
            query ??= new JyStockAgeDetailQueryDto();
            var bucket = NormalizeAgeBucket(query.Bucket);
            var asOf = NormalizeAsOf(query.AsOfDate);
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);
            var goods = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                    && it.Qty > 0 && (it.Status == 1 || it.Status == 2))
                .ToList();
            var styleItems = StyleItems(actor.TenantId);
            var styleMap = styleItems.ToDictionary(it => it.Id);
            var parentMap = CanonicalParents(styleItems);
            LeafCategories(styleItems, parentMap, out var leafAlias);
            ApplyGoodsFilters(goods, styleMap, parentMap, leafAlias, query.MaterialGroup, query.StyleParentId, query.StyleItemId);
            var inboundTimes = LoadFirstInboundTimes(actor.TenantId, goods, asOf);
            var rows = goods.Select(item =>
            {
                var inboundTime = inboundTimes.GetValueOrDefault(item.Id);
                if (inboundTime == default) inboundTime = item.Create_time.Date;
                var ageDays = Math.Max(0, (asOf - inboundTime.Date).Days);
                var path = ResolveStylePath(item.StyleItemId, styleMap, parentMap, leafAlias);
                var materialGroup = item.MaterialGroup is >= 1 and <= 3 ? item.MaterialGroup : 3;
                return new JyStockAgeDetailRowDto
                {
                    Id = item.Id, GoodsNo = item.GoodsNo, Barcode = item.Barcode, StyleName = item.StyleName,
                    StyleParentName = path.ParentName, StyleItemName = path.LeafName,
                    MaterialGroup = materialGroup, MaterialGroupName = MaterialGroups().GetValueOrDefault(materialGroup),
                    MaterialName = item.MaterialName, MetalName = item.MetalName,
                    Qty = item.Qty, Unit = item.Unit ?? "件", Weight = item.Weight,
                    CostPrice = item.CostPrice, SalePrice = item.SalePrice,
                    StockCost = StockCost(item), StockSale = StockSale(item),
                    InboundTime = inboundTime, AgeDays = ageDays,
                    Bucket = AgeBucketKey(ageDays), BucketLabel = AgeBucketLabel(ageDays), Status = item.Status
                };
            }).Where(it => it.Bucket == bucket)
              .OrderByDescending(it => it.AgeDays).ThenBy(it => it.GoodsNo)
              .ToList();
            var total = rows.Count;
            return new PagedInfo<JyStockAgeDetailRowDto>
            {
                PageIndex = page, PageSize = size, TotalNum = total,
                Result = rows.Skip((page - 1) * size).Take(size).ToList()
            };
        }

        private static JyStylePerformanceRowDto ToStyleRow(StyleBucket bucket, decimal totalNet)
        {
            var net = Round(bucket.NetAmount);
            var gross = Round(net - bucket.CostAmount);
            return new JyStylePerformanceRowDto
            {
                CategoryId = bucket.CategoryId,
                CategoryName = bucket.CategoryName,
                ParentCategoryId = bucket.ParentCategoryId,
                SalesAmount = Round(bucket.SalesAmount),
                ReturnAmount = Round(bucket.ReturnAmount),
                NetAmount = net,
                SalesPieces = Round(bucket.SalesPieces),
                ReturnPieces = Round(bucket.ReturnPieces),
                NetPieces = Round(bucket.NetPieces),
                CostAmount = Round(bucket.CostAmount),
                GrossProfit = gross,
                GrossRate = net <= 0 ? 0 : Round(gross * 100m / net),
                AvgTicket = bucket.NetPieces <= 0 ? 0 : Round(net / bucket.NetPieces),
                Share = totalNet <= 0 ? 0 : Round(net * 100m / totalNet),
                OrderCount = bucket.OrderIds.Count,
                Children = bucket.Children.Values
                    .OrderByDescending(it => it.NetAmount)
                    .ThenBy(it => it.CategoryId)
                    .Select(child => ToStyleRow(child, totalNet))
                    .ToList()
            };
        }

        private static SalesLedgerGroup ResolveSalesLedgerGroup(JySaleOrderItem item, string groupBy,
            Dictionary<long, JyLookupItem> styleMap, Dictionary<long, JyLookupItem> parentMap,
            Dictionary<long, long> leafAlias)
        {
            var materialGroup = item.MaterialGroup is >= 1 and <= 3 ? item.MaterialGroup : 3;
            var materialName = MaterialGroups().GetValueOrDefault(materialGroup);
            var (parentId, parentName, leafId, leafName) = ResolveStylePath(
                item.StyleItemId, styleMap, parentMap, leafAlias);
            var parentKey = parentId <= 0 ? "0" : parentId.ToString();
            var leafKey = leafId.HasValue ? leafId.Value.ToString() : "0";
            return groupBy switch
            {
                "style" => new SalesLedgerGroup
                {
                    Key = "style:" + parentKey,
                    Name = parentName,
                    StyleParentName = parentName,
                    StyleItemName = leafName
                },
                "category" => new SalesLedgerGroup
                {
                    Key = "category:" + parentKey + ":" + leafKey,
                    Name = parentName + " / " + leafName,
                    StyleParentName = parentName,
                    StyleItemName = leafName
                },
                _ => new SalesLedgerGroup
                {
                    Key = "material:" + materialGroup,
                    Name = materialName,
                    StyleParentName = parentName,
                    StyleItemName = leafName
                }
            };
        }

        private static JySalesLedgerRowDto ToSalesLedgerRow(SalesLedgerBucket bucket, decimal totalNetForShare)
        {
            var net = Round(bucket.SalesAmount - bucket.ReturnAmount);
            var gross = Round(net - bucket.CostAmount);
            return new JySalesLedgerRowDto
            {
                GroupKey = bucket.Key,
                GroupName = bucket.Name,
                Level = bucket.Level,
                SalesAmount = Round(bucket.SalesAmount),
                CostAmount = Round(bucket.CostAmount),
                ReturnAmount = Round(bucket.ReturnAmount),
                NetAmount = net,
                GrossProfit = gross,
                GrossRate = net <= 0 ? 0 : Round(gross * 100m / net),
                DiscountAmount = Round(bucket.DiscountAmount),
                RoundOffAmount = Round(bucket.RoundOffAmount),
                SalesPieces = Round(bucket.SalesPieces),
                ReturnPieces = Round(bucket.ReturnPieces),
                OrderCount = bucket.OrderIds.Count,
                Share = totalNetForShare <= 0 ? 0 : Round(net * 100m / totalNetForShare)
            };
        }

        public JyGoalReportDto SalesGoals(JyActor actor, JySalesGoalQueryDto query)
        {
            query ??= new JySalesGoalQueryDto();
            var today = DateTime.Today;
            var monthText = string.IsNullOrWhiteSpace(query.Month) ? today.ToString("yyyy-MM") : query.Month.Trim();
            if (!DateTime.TryParse(monthText + "-01", out var monthStart))
                throw new CustomException("月份格式应为 yyyy-MM");
            var year = monthStart.Year;
            var month = monthStart.Month;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var periodBegin = (query.PeriodBegin?.Date ?? monthStart);
            var periodEnd = (query.PeriodEnd?.Date ?? monthEnd);
            if (periodBegin > periodEnd) throw new CustomException("开始日期不能晚于结束日期");
            var periodEndExclusive = periodEnd.AddDays(1);

            var yearGoal = Context.Queryable<JySalesGoalYear>()
                .First(it => it.TenantId == actor.TenantId && it.GoalYear == year);
            var monthGoal = Context.Queryable<JySalesGoalMonth>()
                .First(it => it.TenantId == actor.TenantId && it.GoalYear == year && it.GoalMonth == month);
            var categoryGoals = Context.Queryable<JySalesGoalCategory>()
                .Where(it => it.TenantId == actor.TenantId && it.GoalYear == year && it.GoalMonth == month)
                .ToList();
            var styleItems = StyleItems(actor.TenantId);
            var parentLookup = CanonicalParents(styleItems);
            var parentMap = parentLookup.Values
                .GroupBy(it => it.Id).Select(it => it.First())
                .OrderBy(it => it.SortNo).ThenBy(it => it.Id).ToList();
            var styleMap = styleItems.ToDictionary(it => it.Id);

            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= monthStart && it.DealTime < monthEnd.AddDays(1))
                .Select(it => new { it.Id, it.DealTime, it.PayAmount, it.PieceQty, it.CostAmount })
                .ToList();
            var orderMap = orders.ToDictionary(it => it.Id);
            var items = orders.Count == 0 ? new List<JySaleOrderItem>() : Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && orderMap.Keys.Contains(it.OrderId))
                .ToList();
            var returns = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 1
                    && it.ReturnTime >= monthStart && it.ReturnTime < monthEnd.AddDays(1))
                .ToList();
            var returnIds = returns.Select(it => it.Id).ToList();
            var returnItems = returnIds.Count == 0 ? new List<JySaleReturnItem>() : Context.Queryable<JySaleReturnItem>()
                .Where(it => it.TenantId == actor.TenantId && returnIds.Contains(it.ReturnId))
                .ToList();
            var returnItemMap = returnItems.ToDictionary(it => it.SaleOrderItemId, it => it);
            var itemMap = items.ToDictionary(it => it.Id);

            var catBuckets = new Dictionary<long, StyleBucket>();
            var orderLineTotals = items.GroupBy(it => it.OrderId).ToDictionary(it => it.Key, it => it.Sum(x => x.DealPrice * x.Qty));
            foreach (var item in items)
            {
                var category = ResolveCategory(item.StyleItemId, styleMap, parentLookup);
                if (!catBuckets.TryGetValue(category.Id, out var bucket))
                {
                    bucket = new StyleBucket { CategoryId = category.Id, CategoryName = category.Name };
                    catBuckets.Add(category.Id, bucket);
                }
                var order = orderMap[item.OrderId];
                var totalLine = orderLineTotals[item.OrderId];
                var ratio = totalLine <= 0 ? 0 : Math.Min(1m, Math.Max(0m, order.PayAmount / totalLine));
                var amount = decimal.Round(item.DealPrice * item.Qty * ratio, 2, MidpointRounding.AwayFromZero);
                bucket.NetAmount += amount;
                bucket.NetPieces += item.Qty;
                bucket.CostAmount += item.CostPrice * item.Qty;
                bucket.OrderIds.Add(item.OrderId);
                if (order.DealTime >= periodBegin && order.DealTime < periodEndExclusive)
                {
                    bucket.PeriodAmount += amount;
                    bucket.PeriodPieces += item.Qty;
                }
            }
            foreach (var ret in returns)
            {
                foreach (var ri in returnItems.Where(it => it.ReturnId == ret.Id))
                {
                    if (!itemMap.TryGetValue(ri.SaleOrderItemId, out var saleItem)) continue;
                    var category = ResolveCategory(saleItem.StyleItemId, styleMap, parentLookup);
                    if (!catBuckets.TryGetValue(category.Id, out var bucket))
                    {
                        bucket = new StyleBucket { CategoryId = category.Id, CategoryName = category.Name };
                        catBuckets.Add(category.Id, bucket);
                    }
                    bucket.NetAmount -= ri.RefundAmount;
                    bucket.NetPieces -= ri.Qty;
                    bucket.CostAmount -= ri.CostPrice * ri.Qty;
                    if (ret.ReturnTime >= periodBegin && ret.ReturnTime < periodEndExclusive)
                    {
                        bucket.PeriodAmount -= ri.RefundAmount;
                        bucket.PeriodPieces -= ri.Qty;
                    }
                }
            }

            var categories = parentMap.Select(parent =>
            {
                catBuckets.TryGetValue(parent.Id, out var bucket);
                var goal = categoryGoals.FirstOrDefault(it => CanonicalCategoryId(it.CategoryId, styleMap, parentLookup) == parent.Id);
                var done = Round(bucket?.NetAmount ?? 0);
                var doneQty = Round(bucket?.NetPieces ?? 0);
                var target = goal?.TargetAmount ?? 0;
                var targetQty = goal?.TargetQty ?? 0;
                var gross = Round((bucket?.NetAmount ?? 0) - (bucket?.CostAmount ?? 0));
                var price = goal?.UnitPrice ?? 0;
                if (price <= 0 && doneQty > 0) price = Round(done / doneQty);
                return new JyGoalCategoryRowDto
                {
                    Id = goal?.Id,
                    CategoryId = parent.Id,
                    CategoryName = parent.ItemName,
                    TargetAmount = Round(target),
                    TargetQty = Round(targetQty),
                    UnitPrice = Round(price),
                    DoneAmount = done,
                    DoneQty = doneQty,
                    ActualUnitPrice = doneQty <= 0 ? 0 : Round(done / doneQty),
                    GrossProfit = gross,
                    GrossRate = done <= 0 ? 0 : Round(gross * 100m / done),
                    Percent = target <= 0 ? 0 : Round(done * 100m / target),
                    NeedAmount = Math.Max(0, Round(target - done)),
                    NeedQty = price > 0 && target > done ? (int)Math.Ceiling((target - done) / price) : null
                };
            }).ToList();
            foreach (var goal in categoryGoals)
            {
                var canonicalId = CanonicalCategoryId(goal.CategoryId, styleMap, parentLookup);
                var canonical = canonicalId > 0 ? parentLookup.GetValueOrDefault(canonicalId) : null;
                if (categories.Any(it => it.CategoryId == canonicalId)) continue;
                categories.Add(new JyGoalCategoryRowDto
                {
                    Id = goal.Id, CategoryId = canonicalId,
                    CategoryName = canonical?.ItemName ?? goal.CategoryName,
                    TargetAmount = goal.TargetAmount, TargetQty = goal.TargetQty, UnitPrice = goal.UnitPrice
                });
            }

            var doneAmount = Round(categories.Sum(it => it.DoneAmount));
            var doneQty = Round(categories.Sum(it => it.DoneQty));
            var monthTarget = monthGoal?.TargetAmount ?? 0;
            var periodDone = Round(catBuckets.Values.Sum(it => it.PeriodAmount));
            var periodTarget = monthTarget;
            if (periodBegin != monthStart || periodEnd != monthEnd)
            {
                var days = Math.Max(1, (periodEnd - periodBegin).Days + 1);
                periodTarget = Round(monthTarget * days / monthEnd.Day);
            }
            var daysLeft = month == today.Month && year == today.Year ? Math.Max(1, monthEnd.Day - today.Day + 1) : 1;
            return new JyGoalReportDto
            {
                GoalYear = year,
                GoalMonth = month,
                Month = monthStart.ToString("yyyy-MM"),
                YearTargetAmount = yearGoal?.TargetAmount ?? 0,
                YearTargetQty = yearGoal?.TargetQty ?? 0,
                MonthTargetAmount = monthTarget,
                MonthTargetQty = monthGoal?.TargetQty ?? 0,
                YearToMonthAmount = yearGoal == null || yearGoal.TargetAmount <= 0 ? 0 : Round(yearGoal.TargetAmount / 12m),
                MonthToDayAmount = monthTarget <= 0 ? 0 : Round(monthTarget / monthEnd.Day),
                DaysInMonth = monthEnd.Day,
                DaysLeft = daysLeft,
                DoneAmount = doneAmount,
                DoneQty = doneQty,
                RemainingAmount = Math.Max(0, Round(monthTarget - doneAmount)),
                NeedPerDay = Math.Max(0, Round((monthTarget - doneAmount) / daysLeft)),
                Percent = monthTarget <= 0 ? 0 : Round(doneAmount * 100m / monthTarget),
                TodayAmount = Round(orders.Where(it => it.DealTime.Date == today).Sum(it => it.PayAmount)),
                PeriodBegin = periodBegin,
                PeriodEnd = periodEnd,
                PeriodDoneAmount = periodDone,
                PeriodDoneQty = Round(catBuckets.Values.Sum(it => it.PeriodPieces)),
                PeriodTargetAmount = periodTarget,
                PeriodPercent = periodTarget <= 0 ? 0 : Round(periodDone * 100m / periodTarget),
                Categories = categories
            };
        }

        public long SaveYearGoal(JyActor actor, JyGoalYearSaveDto dto)
        {
            if (dto.GoalYear < 2000 || dto.GoalYear > 2100) throw new CustomException("目标年份不正确");
            if (dto.TargetAmount < 0 || dto.TargetQty < 0) throw new CustomException("目标金额和件数不能为负数");
            var row = Context.Queryable<JySalesGoalYear>().First(it => it.TenantId == actor.TenantId && it.GoalYear == dto.GoalYear);
            if (row == null)
                return Context.Insertable(new JySalesGoalYear { TenantId = actor.TenantId, GoalYear = dto.GoalYear, TargetAmount = dto.TargetAmount, TargetQty = dto.TargetQty, Create_by = actor.UserName, Create_time = DateTime.Now }).ExecuteReturnBigIdentity();
            row.TargetAmount = dto.TargetAmount;
            row.TargetQty = dto.TargetQty;
            row.Update_by = actor.UserName;
            row.Update_time = DateTime.Now;
            Context.Updateable(row).UpdateColumns(it => new { it.TargetAmount, it.TargetQty, it.Update_by, it.Update_time }).ExecuteCommand();
            return row.Id;
        }

        public long SaveMonthGoal(JyActor actor, JyGoalMonthSaveDto dto)
        {
            if (dto.GoalYear < 2000 || dto.GoalYear > 2100 || dto.GoalMonth < 1 || dto.GoalMonth > 12) throw new CustomException("目标月份不正确");
            if (dto.TargetAmount < 0 || dto.TargetQty < 0) throw new CustomException("目标金额和件数不能为负数");
            var row = Context.Queryable<JySalesGoalMonth>().First(it => it.TenantId == actor.TenantId && it.GoalYear == dto.GoalYear && it.GoalMonth == dto.GoalMonth);
            if (row == null)
                return Context.Insertable(new JySalesGoalMonth { TenantId = actor.TenantId, GoalYear = dto.GoalYear, GoalMonth = dto.GoalMonth, TargetAmount = dto.TargetAmount, TargetQty = dto.TargetQty, Create_by = actor.UserName, Create_time = DateTime.Now }).ExecuteReturnBigIdentity();
            row.TargetAmount = dto.TargetAmount;
            row.TargetQty = dto.TargetQty;
            row.Update_by = actor.UserName;
            row.Update_time = DateTime.Now;
            Context.Updateable(row).UpdateColumns(it => new { it.TargetAmount, it.TargetQty, it.Update_by, it.Update_time }).ExecuteCommand();
            return row.Id;
        }

        public long SaveCategoryGoal(JyActor actor, JyGoalCategorySaveDto dto)
        {
            if (dto.GoalYear < 2000 || dto.GoalYear > 2100 || dto.GoalMonth < 1 || dto.GoalMonth > 12) throw new CustomException("目标月份不正确");
            if (dto.TargetAmount < 0 || dto.TargetQty < 0 || dto.UnitPrice < 0) throw new CustomException("目标金额、件数和客单价不能为负数");
            var category = Context.Queryable<JyLookupItem>().First(it => it.Id == dto.CategoryId
                && it.TenantId == actor.TenantId && it.DelFlag == 0 && !it.ParentId.HasValue)
                ?? throw new CustomException("款式大类不存在");
            var row = Context.Queryable<JySalesGoalCategory>().First(it => it.TenantId == actor.TenantId
                && it.GoalYear == dto.GoalYear && it.GoalMonth == dto.GoalMonth && it.CategoryId == dto.CategoryId);
            if (row == null)
                return Context.Insertable(new JySalesGoalCategory
                {
                    TenantId = actor.TenantId, GoalYear = dto.GoalYear, GoalMonth = dto.GoalMonth,
                    CategoryId = dto.CategoryId, CategoryName = category.ItemName, TargetAmount = dto.TargetAmount,
                    TargetQty = dto.TargetQty, UnitPrice = dto.UnitPrice, Create_by = actor.UserName, Create_time = DateTime.Now
                }).ExecuteReturnBigIdentity();
            row.CategoryName = category.ItemName;
            row.TargetAmount = dto.TargetAmount;
            row.TargetQty = dto.TargetQty;
            row.UnitPrice = dto.UnitPrice;
            row.Update_by = actor.UserName;
            row.Update_time = DateTime.Now;
            Context.Updateable(row).UpdateColumns(it => new { it.CategoryName, it.TargetAmount, it.TargetQty, it.UnitPrice, it.Update_by, it.Update_time }).ExecuteCommand();
            return row.Id;
        }

        public JyOpsBreakdownDto OpsBreakdown(JyActor actor, JyOpsBreakdownQueryDto query)
        {
            query ??= new JyOpsBreakdownQueryDto();
            var today = query.Date?.Date ?? DateTime.Today;
            var monthText = string.IsNullOrWhiteSpace(query.Month) ? today.ToString("yyyy-MM") : query.Month.Trim();
            if (!DateTime.TryParse(monthText + "-01", out var monthStart))
                throw new CustomException("月份格式应为 yyyy-MM");
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            var dayOfMonth = today >= monthStart && today <= monthEnd ? today.Day : daysInMonth;
            var restDays = Math.Max(1, daysInMonth - dayOfMonth + 1);
            var workdays = WorkdaysInMonth(monthStart);

            var goal = SalesGoals(actor, new JySalesGoalQueryDto
            {
                Month = monthText, PeriodBegin = monthStart, PeriodEnd = monthEnd
            });
            var monthStyles = StylePerformance(actor, new JyStylePerformanceQueryDto
            {
                BeginTime = monthStart, EndTime = monthEnd, SortBy = "amount"
            });
            var threeMonthBegin = monthStart.AddMonths(-2);
            var threeMonthStyles = StylePerformance(actor, new JyStylePerformanceQueryDto
            {
                BeginTime = threeMonthBegin, EndTime = monthEnd, SortBy = "amount"
            });

            var weightRow = Context.Queryable<JySalesGoalWeightConfig>()
                .First(it => it.TenantId == actor.TenantId && it.GoalYear == monthStart.Year && it.GoalMonth == monthStart.Month);
            var weight = new JyGoalWeightSaveDto
            {
                GoalYear = monthStart.Year,
                GoalMonth = monthStart.Month,
                SalesWeight = weightRow?.SalesWeight ?? 60,
                StockWeight = weightRow?.StockWeight ?? 20,
                MarginWeight = weightRow?.MarginWeight ?? 20
            };
            var plans = Context.Queryable<JySalesGoalPlan>()
                .Where(it => it.TenantId == actor.TenantId && it.GoalYear == monthStart.Year && it.GoalMonth == monthStart.Month)
                .ToList();
            var planMap = plans.GroupBy(it => it.CategoryId).ToDictionary(it => it.Key, it => it.First());

            var styleItems = StyleItems(actor.TenantId);
            var styleMap = styleItems.ToDictionary(it => it.Id);
            var parentLookup = CanonicalParents(styleItems);
            var rootRows = threeMonthStyles.Rows
                .GroupBy(it => it.CategoryId)
                .ToDictionary(it => it.Key, it => it.First());
            var monthMap = monthStyles.Rows
                .GroupBy(it => it.CategoryId)
                .ToDictionary(it => it.Key, it => it.First());

            var stockRows = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1)
                .Select(it => new { it.StyleItemId, it.Qty, it.SalePrice, it.CostPrice, it.Create_time })
                .ToList();
            var stock = new Dictionary<long, (decimal Qty, decimal Value, decimal StaleQty)>();
            foreach (var item in stockRows)
            {
                var category = ResolveCategory(item.StyleItemId, styleMap, parentLookup);
                var qty = item.Qty <= 0 ? 0 : item.Qty;
                if (!stock.TryGetValue(category.Id, out var bucket)) bucket = (0, 0, 0);
                bucket.Qty += qty;
                bucket.Value += qty * (item.SalePrice > 0 ? item.SalePrice : item.CostPrice);
                if (item.Create_time.Date < today.AddDays(-60)) bucket.StaleQty += qty;
                stock[category.Id] = bucket;
            }

            var salesTotal = threeMonthStyles.NetAmount;
            var staleTotal = stock.Values.Sum(it => it.StaleQty);
            var rateTotal = rootRows.Values.Sum(it => Math.Max(0, it.GrossRate));
            var rawWeights = new List<JyCategoryWeightDto>();
            foreach (var parent in parentLookup.Values.GroupBy(it => it.Id).Select(it => it.First()))
            {
                var style = rootRows.GetValueOrDefault(parent.Id);
                var stk = stock.GetValueOrDefault(parent.Id);
                var sales = style?.NetAmount ?? 0;
                var salesShare = salesTotal > 0 ? sales / salesTotal : 0;
                var grossRate = Math.Max(0, style?.GrossRate ?? 0);
                var score = salesShare * weight.SalesWeight
                    + (staleTotal > 0 ? stk.StaleQty / staleTotal * weight.StockWeight : 0)
                    + (rateTotal > 0 ? grossRate / rateTotal * weight.MarginWeight : 0);
                rawWeights.Add(new JyCategoryWeightDto
                {
                    CategoryId = parent.Id,
                    CategoryName = parent.ItemName,
                    SalesAmount = Round(sales),
                    SalesQty = Round(style?.NetPieces ?? 0),
                    GrossRate = Round(grossRate),
                    StockQty = stk.Qty,
                    StockValue = Round(stk.Value),
                    StaleQty = stk.StaleQty,
                    SalesShare = Round(salesShare * 100),
                    Score = Round(score)
                });
            }
            var scoreSum = rawWeights.Sum(it => it.Score);
            foreach (var row in rawWeights)
            {
                row.Percent = scoreSum <= 0 ? 0 : Math.Round(row.Score * 100m / scoreSum, 0, MidpointRounding.AwayFromZero);
                row.Basis = $"近 3 月销售 {row.SalesQty:0} 件 / 毛利 {row.GrossRate:0.#}%"
                    + (row.StaleQty > 0 ? $" / 超 60 天 {row.StaleQty:0} 件" : string.Empty);
                row.SellPoint = row.StaleQty > 0 ? "先去化压货，再做搭配" : "按毛利稳步走量";
            }
            var weightRows = rawWeights.OrderByDescending(it => it.Percent).ThenByDescending(it => it.SalesAmount).ToList();
            if (weightRows.Count > 0)
            {
                var diff = 100 - weightRows.Sum(it => it.Percent);
                weightRows[0].Percent = Math.Max(0, weightRows[0].Percent + diff);
            }

            var salesTrend = SalesMetrics(actor, new JySalesMetricQueryDto
            {
                Granularity = "month", BeginTime = threeMonthBegin, EndTime = monthEnd
            });
            var targetCheck = BuildTargetCheck(goal.MonthTargetAmount, salesTrend, stock.Values.Sum(it => it.Value), goal.Categories.Count > 0);
            var planRows = BuildPlanRows(goal, monthStyles, monthMap, planMap, weightRows, restDays);
            var customerPlans = planRows.Select(row => BuildCustomerPlan(row, workdays)).ToList();
            var visits = BuildVisitSuggestions(actor, parentLookup, styleMap, stock, today);
            var combos = BuildCombos(actor, today.AddDays(-6), today);
            var actions = BuildTodayActions(planRows, weightRows, combos, visits, restDays);
            var weeks = BuildWeeks(goal, weightRows, restDays);

            return new JyOpsBreakdownDto
            {
                Date = today.ToString("yyyy-MM-dd"),
                Month = monthText,
                RestDays = restDays,
                StatusLine = $"数据读取状态：库存{(stock.Count > 0 ? "正常" : "无数据")} · 销售{(threeMonthStyles.OrderCount > 0 ? "正常" : "无数据")} · 客户{(visits.Count > 0 ? "正常" : "无数据")} · 目标{(goal.MonthTargetAmount > 0 ? "正常" : "未设置")}",
                Weights = weight,
                TargetCheck = targetCheck,
                CategoryWeights = weightRows,
                Weeks = weeks,
                TodayActions = actions,
                Combos = combos,
                Visits = visits,
                PlanRows = planRows,
                CustomerPlans = customerPlans
            };
        }

        public long SaveGoalWeight(JyActor actor, JyGoalWeightSaveDto dto)
        {
            if (dto == null) throw new CustomException("权重不能为空");
            if (dto.GoalYear < 2000 || dto.GoalYear > 2100 || dto.GoalMonth < 1 || dto.GoalMonth > 12)
                throw new CustomException("权重月份不正确");
            var weights = new[] { dto.SalesWeight, dto.StockWeight, dto.MarginWeight };
            if (weights.Any(it => it < 0 || it > 100)) throw new CustomException("权重必须在 0 到 100 之间");
            if (Math.Abs(weights.Sum() - 100) > 0.1m) throw new CustomException("销售占比、库存压力、利润率三项权重合计必须等于 100");
            var row = Context.Queryable<JySalesGoalWeightConfig>()
                .First(it => it.TenantId == actor.TenantId && it.GoalYear == dto.GoalYear && it.GoalMonth == dto.GoalMonth);
            if (row == null)
                return Context.Insertable(new JySalesGoalWeightConfig
                {
                    TenantId = actor.TenantId, GoalYear = dto.GoalYear, GoalMonth = dto.GoalMonth,
                    SalesWeight = dto.SalesWeight, StockWeight = dto.StockWeight, MarginWeight = dto.MarginWeight,
                    Create_by = actor.UserName, Create_time = DateTime.Now
                }).ExecuteReturnBigIdentity();
            row.SalesWeight = dto.SalesWeight;
            row.StockWeight = dto.StockWeight;
            row.MarginWeight = dto.MarginWeight;
            row.Update_by = actor.UserName;
            row.Update_time = DateTime.Now;
            Context.Updateable(row).UpdateColumns(it => new
            {
                it.SalesWeight, it.StockWeight, it.MarginWeight, it.Update_by, it.Update_time
            }).ExecuteCommand();
            return row.Id;
        }

        public int SaveGoalPlans(JyActor actor, JyGoalPlanBatchSaveDto dto)
        {
            if (dto == null || dto.Rows == null || dto.Rows.Count == 0) throw new CustomException("拆解数据不能为空");
            if (dto.GoalYear < 2000 || dto.GoalYear > 2100 || dto.GoalMonth < 1 || dto.GoalMonth > 12)
                throw new CustomException("拆解月份不正确");
            var rows = dto.Rows.Where(it => it.CategoryId > 0).ToList();
            if (rows.Count == 0) throw new CustomException("至少保留一个品类");
            if (rows.Any(it => it.PlanRatio < 0 || it.PlanRatio > 100 || it.TargetAmount < 0
                || it.GrossMarginRate < 0 || it.GrossMarginRate > 100
                || it.UnitPrice < 0 || it.ConversionRate <= 0 || it.ConversionRate > 100
                || it.NewCustomerRate < 0 || it.NewCustomerRate > 100))
                throw new CustomException("占比、毛利率和客户参数必须在有效范围内");
            var ratioSum = rows.Sum(it => it.PlanRatio);
            if (ratioSum > 0 && Math.Abs(ratioSum - 100) > 0.1m) throw new CustomException($"品类占比合计必须为 100%，当前为 {ratioSum:0.#}%");
            var categories = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && !it.ParentId.HasValue
                    && rows.Select(r => r.CategoryId).Contains(it.Id))
                .ToList().ToDictionary(it => it.Id);
            var saved = 0;
            UseTran(() =>
            {
                foreach (var row in rows)
                {
                    if (!categories.TryGetValue(row.CategoryId, out var category)) throw new CustomException("款式大类不存在或已停用");
                    var plan = Context.Queryable<JySalesGoalPlan>().First(it => it.TenantId == actor.TenantId
                        && it.GoalYear == dto.GoalYear && it.GoalMonth == dto.GoalMonth && it.CategoryId == row.CategoryId);
                    if (plan == null)
                        Context.Insertable(new JySalesGoalPlan
                        {
                            TenantId = actor.TenantId, GoalYear = dto.GoalYear, GoalMonth = dto.GoalMonth,
                            CategoryId = row.CategoryId, CategoryName = category.ItemName,
                            PlanRatio = row.PlanRatio, GrossMarginRate = row.GrossMarginRate, UnitPrice = row.UnitPrice,
                            ConversionRate = row.ConversionRate, NewCustomerRate = row.NewCustomerRate,
                            ActionHigh = TrimAction(row.ActionHigh), ActionMedium = TrimAction(row.ActionMedium), ActionLow = TrimAction(row.ActionLow),
                            Create_by = actor.UserName, Create_time = DateTime.Now
                        }).ExecuteCommand();
                    else
                    {
                        plan.CategoryName = category.ItemName;
                        plan.PlanRatio = row.PlanRatio;
                        plan.GrossMarginRate = row.GrossMarginRate;
                        plan.UnitPrice = row.UnitPrice;
                        plan.ConversionRate = row.ConversionRate;
                        plan.NewCustomerRate = row.NewCustomerRate;
                        plan.ActionHigh = TrimAction(row.ActionHigh);
                        plan.ActionMedium = TrimAction(row.ActionMedium);
                        plan.ActionLow = TrimAction(row.ActionLow);
                        plan.Update_by = actor.UserName;
                        plan.Update_time = DateTime.Now;
                        Context.Updateable(plan).UpdateColumns(it => new
                        {
                            it.CategoryName, it.PlanRatio, it.GrossMarginRate, it.UnitPrice, it.ConversionRate,
                            it.NewCustomerRate, it.ActionHigh, it.ActionMedium, it.ActionLow, it.Update_by, it.Update_time
                        }).ExecuteCommand();
                    }
                    var goal = Context.Queryable<JySalesGoalCategory>().First(it => it.TenantId == actor.TenantId
                        && it.GoalYear == dto.GoalYear && it.GoalMonth == dto.GoalMonth && it.CategoryId == row.CategoryId);
                    if (goal == null)
                        Context.Insertable(new JySalesGoalCategory
                        {
                            TenantId = actor.TenantId, GoalYear = dto.GoalYear, GoalMonth = dto.GoalMonth,
                            CategoryId = row.CategoryId, CategoryName = category.ItemName,
                            TargetAmount = row.TargetAmount, TargetQty = 0, UnitPrice = row.UnitPrice,
                            Create_by = actor.UserName, Create_time = DateTime.Now
                        }).ExecuteCommand();
                    else
                    {
                        goal.CategoryName = category.ItemName;
                        goal.TargetAmount = row.TargetAmount;
                        goal.UnitPrice = row.UnitPrice;
                        goal.Update_by = actor.UserName;
                        goal.Update_time = DateTime.Now;
                        Context.Updateable(goal).UpdateColumns(it => new
                        {
                            it.CategoryName, it.TargetAmount, it.UnitPrice, it.Update_by, it.Update_time
                        }).ExecuteCommand();
                    }
                    saved++;
                }
            });
            return saved;
        }

        private static JyTargetCheckDto BuildTargetCheck(decimal target, JySalesMetricReportDto threeMonth, decimal stockValue, bool hasGoal)
        {
            // 近 3 个月按自然月拆分，空月不计入均值；库存按 30% 的保守去化率折算可支撑销售额。
            var trendRows = threeMonth.Trend.ToList();
            var positiveMonths = trendRows.Where(it => it.NetAmount > 0).Select(it => it.NetAmount).ToList();
            var average = positiveMonths.Count == 0 ? 0 : positiveMonths.Average();
            var trend = trendRows.Count < 2 ? 0 : trendRows[^1].NetAmount - trendRows[0].NetAmount;
            var stockSupport = Round(stockValue * 0.3m);
            var ceiling = Round(Math.Max(average * 1.2m, stockSupport));
            var high = target > 0 && target > ceiling;
            var suggestion = !hasGoal || target <= 0
                ? "先在目标管理里填一个月度营业额，再按品类拆。"
                : high
                    ? $"按现有销售与库存最多支撑约 {Money(ceiling)}，当前目标偏高，建议下调到 {Money(ceiling)}，或通过联动组合和复购回访补缺口。"
                    : $"目标在数据支撑范围内（上限约 {Money(ceiling)}），可以按下面的品类权重继续拆。";
            return new JyTargetCheckDto
            {
                TargetAmount = Round(target),
                Average3Month = Round(average),
                Trend = Round(trend),
                StockSupport = stockSupport,
                Ceiling = ceiling,
                IsHigh = high,
                Suggestion = suggestion,
                MissingData = hasGoal ? null : "目标"
            };
        }

        private List<JyGoalPlanRowDto> BuildPlanRows(JyGoalReportDto goal, JyStylePerformanceReportDto month,
            Dictionary<long, JyStylePerformanceRowDto> monthMap, Dictionary<long, JySalesGoalPlan> plans,
            List<JyCategoryWeightDto> weights, int restDays)
        {
            var goalMap = goal.Categories.GroupBy(it => it.CategoryId).ToDictionary(it => it.Key, it => it.First());
            var total = goal.MonthTargetAmount > 0 ? goal.MonthTargetAmount : goal.Categories.Sum(it => it.TargetAmount);
            var result = new List<JyGoalPlanRowDto>();
            foreach (var weight in weights)
            {
                var target = goalMap.TryGetValue(weight.CategoryId, out var goalRow) ? goalRow.TargetAmount : 0;
                var style = monthMap.GetValueOrDefault(weight.CategoryId);
                var plan = plans.GetValueOrDefault(weight.CategoryId);
                var unitPrice = plan?.UnitPrice > 0 ? plan.UnitPrice
                    : goalRow?.UnitPrice > 0 ? goalRow.UnitPrice
                    : style?.AvgTicket > 0 ? style.AvgTicket : weight.SalesQty > 0 ? weight.SalesAmount / weight.SalesQty : 300;
                var doneAmount = goalRow?.DoneAmount ?? style?.NetAmount ?? 0;
                var ratio = plan != null ? plan.PlanRatio : total > 0 ? target * 100m / total : weight.Percent;
                result.Add(new JyGoalPlanRowDto
                {
                    CategoryId = weight.CategoryId,
                    CategoryName = weight.CategoryName,
                    PlanRatio = Round(ratio),
                    TargetAmount = Round(target),
                    GrossMarginRate = plan?.GrossMarginRate > 0 ? plan.GrossMarginRate : (weight.GrossRate > 0 ? weight.GrossRate : 45),
                    UnitPrice = Round(unitPrice),
                    ConversionRate = plan?.ConversionRate ?? 30,
                    NewCustomerRate = plan?.NewCustomerRate ?? 60,
                    DoneAmount = Round(doneAmount),
                    DoneQty = goalRow?.DoneQty ?? style?.NetPieces ?? 0,
                    Percent = target <= 0 ? 0 : Math.Min(999, Round(doneAmount * 100m / target)),
                    GrossRate = weight.GrossRate,
                    StockQty = weight.StockQty,
                    StaleQty = weight.StaleQty,
                    ActionHigh = plan?.ActionHigh,
                    ActionMedium = plan?.ActionMedium,
                    ActionLow = plan?.ActionLow
                });
            }
            foreach (var row in result)
            {
                row.EstimatedGross = Round(row.TargetAmount * row.GrossMarginRate / 100m);
                row.EstimatedPieces = row.UnitPrice <= 0 ? 0 : (int)Math.Ceiling(row.TargetAmount / row.UnitPrice);
                FillAutoAction(row, restDays);
            }
            // 首次未保存拆解时，用品类权重补出可编辑占比；已保存的占比原样保留。
            if (result.Sum(it => it.PlanRatio) <= 0)
            {
                var weightSum = result.Sum(it => weights.FirstOrDefault(w => w.CategoryId == it.CategoryId)?.Percent ?? 0);
                foreach (var row in result)
                {
                    var percent = weights.FirstOrDefault(w => w.CategoryId == row.CategoryId)?.Percent ?? 0;
                    row.PlanRatio = weightSum > 0 ? Round(percent * 100m / weightSum) : Round(100m / Math.Max(1, result.Count));
                }
                var diff = 100m - result.Sum(it => it.PlanRatio);
                if (result.Count > 0) result[0].PlanRatio = Round(result[0].PlanRatio + diff);
            }
            // 只设了月目标、还没维护分类目标：按占比预分一次，页面可直接微调后保存。
            if (total > 0 && result.Sum(it => it.TargetAmount) <= 0)
            {
                foreach (var row in result) row.TargetAmount = Round(total * row.PlanRatio / 100m);
                var targetDiff = total - result.Sum(it => it.TargetAmount);
                if (result.Count > 0) result[0].TargetAmount = Round(result[0].TargetAmount + targetDiff);
                foreach (var row in result)
                {
                    row.EstimatedGross = Round(row.TargetAmount * row.GrossMarginRate / 100m);
                    row.EstimatedPieces = row.UnitPrice <= 0 ? 0 : (int)Math.Ceiling(row.TargetAmount / row.UnitPrice);
                    row.Percent = row.TargetAmount <= 0 ? 0 : Math.Min(999, Round(row.DoneAmount * 100m / row.TargetAmount));
                    FillAutoAction(row, restDays);
                }
            }
            return result.OrderByDescending(it => it.TargetAmount).ThenByDescending(it => it.DoneAmount).ToList();
        }

        private static JyCategoryCustomerPlanDto BuildCustomerPlan(JyGoalPlanRowDto row, int workdays)
        {
            var orders = row.UnitPrice <= 0 ? 0 : (int)Math.Ceiling(row.TargetAmount / row.UnitPrice);
            var rate = Math.Clamp(row.ConversionRate, 1, 100) / 100m;
            var newRate = Math.Clamp(row.NewCustomerRate, 0, 100) / 100m;
            var intent = rate <= 0 ? 0 : (int)Math.Ceiling(orders / rate);
            var newCustomers = (int)Math.Ceiling(intent * newRate);
            var oldCustomers = Math.Max(0, intent - newCustomers);
            var perDayNew = (int)Math.Ceiling(newCustomers / (decimal)workdays);
            var perDayOld = (int)Math.Ceiling(oldCustomers / (decimal)workdays);
            return new JyCategoryCustomerPlanDto
            {
                CategoryId = row.CategoryId,
                CategoryName = row.CategoryName,
                TargetAmount = row.TargetAmount,
                UnitPrice = row.UnitPrice,
                ConversionRate = row.ConversionRate,
                NewCustomerRate = row.NewCustomerRate,
                Orders = orders,
                IntentCustomers = intent,
                NewConsult = newCustomers,
                OldVisit = oldCustomers,
                PerDayNew = perDayNew,
                PerDayOld = perDayOld,
                PerDayAll = perDayNew + perDayOld,
                Workdays = workdays,
                TooHeavy = perDayNew + perDayOld > 8
            };
        }

        private static void FillAutoAction(JyGoalPlanRowDto row, int restDays)
        {
            var gap = Math.Max(0, row.TargetAmount - row.DoneAmount);
            var dailyNeed = gap > 0 ? gap / Math.Max(1, restDays) : 0;
            var basis = $"剩余缺口 {Money(gap)}，剩余 {Math.Max(1, restDays)} 天，在库 {row.StockQty:0} 件";
            if (row.StaleQty > 0)
                basis += $"，其中超 60 天 {row.StaleQty:0} 件";

            if (gap > 0 && row.StaleQty > 0)
            {
                row.AutoActionState = "补目标/去库存";
                row.AutoActionMain = $"今天为「{row.CategoryName}」做 {Money(dailyNeed)} 的销售补口：先从超 60 天库存中选 2 件做主推，拍上手实拍并配一个明确穿搭场景。";
                row.AutoActionAlt = row.StockQty <= Math.Max(2, row.StaleQty)
                    ? "可售库存偏少，成交前先核对可出库件数；若不足，改约客户看款，不承诺交期。"
                    : "挑一位买过该品类的老客，发她可能感兴趣的 1 件实拍，不群发。";
            }
            else if (gap > 0)
            {
                row.AutoActionState = "补销售目标";
                row.AutoActionMain = $"今天为「{row.CategoryName}」做 {Money(dailyNeed)} 的销售补口：主推 1 件高毛利款，再准备 1 件可搭配款做连带。";
                row.AutoActionAlt = row.StockQty <= 0
                    ? "当前无可售库存，先补货或调拨，不做无效推荐。"
                    : "从买过该品类的客户里找 1 位，发新款实拍并约试戴。";
            }
            else if (row.StaleQty > 0)
            {
                row.AutoActionState = "去库存";
                row.AutoActionMain = $"「{row.CategoryName}」本月目标已达成，今天不追销售，优先处理 {row.StaleQty:0} 件超 60 天库存：做陈列调整并拍 1 组细节图。";
                row.AutoActionAlt = "找一位偏好该品类的老客做定向推荐，不做全场促销。";
            }
            else
            {
                row.AutoActionState = "观察";
                row.AutoActionMain = $"「{row.CategoryName}」当前没有目标缺口或库存压力，今天观察，不安排主动动作。";
                row.AutoActionAlt = "如出现客户主动询问，再按实际需求推荐，不硬推。";
            }
            row.AutoActionBasis = basis + $"；近 3 月毛利 {row.GrossRate:0.#}%";
        }

        private List<JyVisitSuggestionDto> BuildVisitSuggestions(JyActor actor,
            Dictionary<long, JyLookupItem> parentLookup, Dictionary<long, JyLookupItem> styleMap,
            Dictionary<long, (decimal Qty, decimal Value, decimal StaleQty)> stock, DateTime today)
        {
            var members = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.Id, OrderByType.Desc).Take(300).ToList();
            if (members.Count == 0) return new List<JyVisitSuggestionDto>();
            var memberIds = members.Select(it => it.Id).ToList();
            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.MemberId.HasValue && memberIds.Contains(it.MemberId.Value)
                    && it.DealTime >= today.AddMonths(-18))
                .Select(it => new { it.Id, it.MemberId, it.DealTime })
                .ToList();
            var orderIds = orders.Select(it => it.Id).ToList();
            var items = orderIds.Count == 0 ? new List<JySaleOrderItem>() : Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && orderIds.Contains(it.OrderId))
                .ToList();
            var itemMap = items.GroupBy(it => it.OrderId).ToDictionary(it => it.Key, it => it.ToList());
            var follows = Context.Queryable<JyMemberFollow>()
                .Where(it => it.TenantId == actor.TenantId && memberIds.Contains(it.MemberId))
                .OrderByDescending(it => it.FollowTime).ToList()
                .GroupBy(it => it.MemberId).ToDictionary(it => it.Key, it => it.First().FollowTime);
            var result = new List<JyVisitSuggestionDto>();
            foreach (var member in members)
            {
                var mine = orders.Where(it => it.MemberId == member.Id).OrderBy(it => it.DealTime).ToList();
                if (mine.Count == 0) continue;
                var catCount = new Dictionary<string, int>();
                foreach (var order in mine)
                {
                    foreach (var item in itemMap.GetValueOrDefault(order.Id) ?? new List<JySaleOrderItem>())
                    {
                        var category = ResolveCategory(item.StyleItemId, styleMap, parentLookup);
                        catCount[category.Name] = catCount.GetValueOrDefault(category.Name) + 1;
                    }
                }
                var preferred = catCount.OrderByDescending(it => it.Value).Select(it => it.Key).FirstOrDefault() ?? "暂无";
                var cycle = 0;
                if (mine.Count >= 2)
                {
                    var span = (mine[^1].DealTime - mine[0].DealTime).TotalDays;
                    cycle = Math.Max(7, (int)Math.Round(span / (mine.Count - 1)));
                }
                var last = mine[^1].DealTime.Date;
                var days = Math.Max(0, (int)(today - last).TotalDays);
                var due = cycle > 0 ? cycle - days : 0;
                var preferredRow = parentLookup.Values.GroupBy(it => it.Id).Select(it => it.First())
                    .FirstOrDefault(it => it.ItemName == preferred);
                var stale = preferredRow == null ? 0 : stock.GetValueOrDefault(preferredRow.Id).StaleQty;
                var suggest = cycle == 0
                    ? $"只成交过 {mine.Count} 次，今天发微信问一句上次那件戴得怎么样，把第二次成交做出来。"
                    : due <= 0
                        ? $"复购周期 {cycle} 天，已到期 {Math.Abs(due)} 天；优先发「{preferred}」新款实拍，只给她可见。"
                        : $"离复购还有 {due} 天；今天先在她最近动态下评论，不推销，到期当天再发「{preferred}」新款。";
                if (stale > 0) suggest += $" 当前该品类超 60 天库存 {stale:0} 件，可优先搭配去化。";
                result.Add(new JyVisitSuggestionDto
                {
                    MemberId = member.Id, MemberName = member.MemberName, Phone = member.Phone,
                    PreferredCategory = preferred, CycleDays = cycle, DaysSince = days, DueDays = due,
                    StaleQty = stale, Suggest = suggest, LastFollowTime = follows.GetValueOrDefault(member.Id)
                });
            }
            return result.OrderByDescending(it => it.DueDays).ThenBy(it => it.DaysSince).Take(8).ToList();
        }

        /// <summary>首页只读启用组合，占比固定取服务器今天往前 7 天。</summary>
        private List<JyComboDto> BuildCombos(JyActor actor, DateTime begin, DateTime end)
        {
            var rows = Context.Queryable<JySalesCombo>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.SortNo).OrderBy(it => it.Id)
                .ToList();
            var totals = ComboCategoryAmounts(actor, begin, end);
            var totalAmount = totals.Values.Sum();
            return rows.Select(row => ToComboDto(row, totals, totalAmount)).ToList();
        }

        public List<JyComboDto> ListCombos(JyActor actor, JyComboQueryDto query)
        {
            query ??= new JyComboQueryDto();
            var (begin, end) = ComboPeriod(query);
            var rows = Context.Queryable<JySalesCombo>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Home, it => it.Status == 0)
                .OrderBy(it => it.SortNo).OrderBy(it => it.Id)
                .ToList();
            var totals = ComboCategoryAmounts(actor, begin, end);
            var totalAmount = totals.Values.Sum();
            return rows.Select(row => ToComboDto(row, totals, totalAmount)).ToList();
        }

        public long SaveCombo(JyActor actor, JyComboEditDto dto)
        {
            if (dto == null) throw new CustomException("组合参数不能为空");
            var name = (dto.ComboName ?? string.Empty).Trim();
            if (name.Length == 0) throw new CustomException("请输入组合名称");
            if (name.Length > 100) throw new CustomException("组合名称不能超过 100 个字");
            if (dto.CategoryId1 <= 0 || dto.CategoryId2 <= 0) throw new CustomException("请选择两个款式分类");
            if (dto.CategoryId1 == dto.CategoryId2) throw new CustomException("两个款式分类不能相同");
            if (dto.Status is not (0 or 1)) throw new CustomException("启用状态不正确");
            if (dto.SortNo < 0) throw new CustomException("排序不能为负数");
            var fit = TrimText(dto.FitCrowd, 200, "适用人群");
            var scene = TrimText(dto.UseScene, 200, "使用场景");

            var styleTypeIds = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == actor.TenantId && it.TypeCode == "style" && it.DelFlag == 0)
                .Select(it => it.Id)
                .ToList();
            var categories = Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && !it.ParentId.HasValue
                    && styleTypeIds.Contains(it.TypeId)
                    && (it.Id == dto.CategoryId1 || it.Id == dto.CategoryId2))
                .ToList();
            if (categories.Count != 2) throw new CustomException("款式分类必须选择有效的一级分类");
            var first = categories.First(it => it.Id == dto.CategoryId1);
            var second = categories.First(it => it.Id == dto.CategoryId2);

            var duplicate = Context.Queryable<JySalesCombo>()
                .Any(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.ComboName == name
                    && it.Id != dto.Id);
            if (duplicate) throw new CustomException("组合名称已存在");

            JySalesCombo current = null;
            if (dto.Id > 0)
            {
                current = Context.Queryable<JySalesCombo>()
                    .First(it => it.Id == dto.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException("组合不存在或已删除");
            }
            if (current == null)
            {
                return Context.Insertable(new JySalesCombo
                {
                    TenantId = actor.TenantId,
                    ComboName = name,
                    CategoryId1 = first.Id,
                    CategoryName1 = first.ItemName,
                    CategoryId2 = second.Id,
                    CategoryName2 = second.ItemName,
                    FitCrowd = fit,
                    UseScene = scene,
                    Status = dto.Status,
                    SortNo = dto.SortNo,
                    DelFlag = 0,
                    Create_by = actor.UserName,
                    Create_time = DateTime.Now
                }).ExecuteReturnBigIdentity();
            }

            current.ComboName = name;
            current.CategoryId1 = first.Id;
            current.CategoryName1 = first.ItemName;
            current.CategoryId2 = second.Id;
            current.CategoryName2 = second.ItemName;
            current.FitCrowd = fit;
            current.UseScene = scene;
            current.Status = dto.Status;
            current.SortNo = dto.SortNo;
            current.Update_by = actor.UserName;
            current.Update_time = DateTime.Now;
            Context.Updateable(current).UpdateColumns(it => new
            {
                it.ComboName, it.CategoryId1, it.CategoryName1, it.CategoryId2, it.CategoryName2,
                it.FitCrowd, it.UseScene, it.Status, it.SortNo, it.Update_by, it.Update_time
            }).ExecuteCommand();
            return current.Id;
        }

        public int SetComboStatus(JyActor actor, long id, int status)
        {
            if (status is not (0 or 1)) throw new CustomException("启用状态不正确");
            return Context.Updateable<JySalesCombo>()
                .SetColumns(it => it.Status == status)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        public int DeleteCombo(JyActor actor, long id)
        {
            return Context.Updateable<JySalesCombo>()
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == DateTime.Now)
                .Where(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
        }

        public JyComboReportDto ComboReport(JyActor actor, JyComboQueryDto query)
        {
            query ??= new JyComboQueryDto();
            var (begin, end) = ComboPeriod(query);
            var granularity = string.IsNullOrWhiteSpace(query.Granularity) ? "day" : query.Granularity.Trim().ToLowerInvariant();
            if (granularity is not ("day" or "week" or "month" or "year")) throw new CustomException("统计粒度不正确");
            var rows = Context.Queryable<JySalesCombo>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.SortNo).OrderBy(it => it.Id)
                .ToList();
            var totals = ComboCategoryAmounts(actor, begin, end);
            var totalAmount = totals.Values.Sum();
            var combos = rows.Select(row => ToComboDto(row, totals, totalAmount)).ToList();

            var events = ComboSaleEvents(actor, begin, end);
            var trend = new List<JyComboTrendDto>();
            var cursor = StartOfPeriod(begin, granularity);
            while (cursor <= end)
            {
                var periodStart = cursor;
                var periodEndExclusive = NextPeriod(periodStart, granularity);
                var periodEvents = events.Where(it => it.Time >= periodStart && it.Time < periodEndExclusive).ToList();
                var periodTotal = periodEvents.Sum(it => it.Amount);
                foreach (var combo in combos)
                {
                    var source = rows.First(it => it.Id == combo.Id);
                    var comboAmount = periodEvents
                        .Where(it => it.CategoryId == source.CategoryId1 || it.CategoryId == source.CategoryId2)
                        .Sum(it => it.Amount);
                    trend.Add(new JyComboTrendDto
                    {
                        ComboId = combo.Id,
                        ComboName = combo.Name,
                        Period = periodStart.ToString("yyyy-MM-dd"),
                        Label = PeriodLabel(periodStart, granularity),
                        Amount = Round(comboAmount),
                        TotalAmount = Round(periodTotal),
                        Percent = periodTotal <= 0 ? 0 : Round(comboAmount * 100m / periodTotal)
                    });
                }
                cursor = periodEndExclusive;
            }

            return new JyComboReportDto
            {
                BeginTime = begin,
                EndTime = end,
                Granularity = granularity,
                TotalAmount = Round(totalAmount),
                Combos = combos,
                Trend = trend
            };
        }

        private (DateTime Begin, DateTime End) ComboPeriod(JyComboQueryDto query)
        {
            var today = DateTime.Today;
            var begin = (query.BeginTime ?? today.AddDays(-6)).Date;
            var end = (query.EndTime ?? today).Date;
            if (query.Home)
            {
                begin = today.AddDays(-6);
                end = today;
            }
            if (begin > end) throw new CustomException("开始日期不能晚于结束日期");
            if ((end - begin).TotalDays > 3660) throw new CustomException("查询区间不能超过 10 年");
            return (begin, end);
        }

        private JyComboDto ToComboDto(JySalesCombo row, Dictionary<long, decimal> totals, decimal totalAmount)
        {
            var amount = totals.GetValueOrDefault(row.CategoryId1 ?? 0) + totals.GetValueOrDefault(row.CategoryId2 ?? 0);
            var percent = totalAmount <= 0 ? 0 : Math.Round(amount * 100m / totalAmount, 2, MidpointRounding.AwayFromZero);
            return new JyComboDto
            {
                Id = row.Id,
                Name = row.ComboName,
                CategoryId1 = row.CategoryId1,
                CategoryId2 = row.CategoryId2,
                Categories = new List<string> { row.CategoryName1, row.CategoryName2 }.Where(it => !string.IsNullOrWhiteSpace(it)).ToList(),
                Fit = row.FitCrowd,
                Scene = row.UseScene,
                Percent = percent,
                Amount = Round(amount),
                TotalAmount = Round(totalAmount),
                Status = row.Status,
                Basis = totalAmount <= 0 ? "所选区间没有成交，先作为引流组合" : $"两品类合计销售额占比 {percent:0.##}%"
            };
        }

        /// <summary>按一级款式归并销售净额：整单实付按明细原价占比分摊，退货按明细退款冲减。</summary>
        private Dictionary<long, decimal> ComboCategoryAmounts(JyActor actor, DateTime begin, DateTime end)
        {
            return ComboSaleEvents(actor, begin, end)
                .GroupBy(it => it.CategoryId)
                .ToDictionary(it => it.Key, it => it.Sum(x => x.Amount));
        }

        private List<ComboSaleEvent> ComboSaleEvents(JyActor actor, DateTime begin, DateTime end)
        {
            var endExclusive = end.AddDays(1);
            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= begin && it.DealTime < endExclusive)
                .Select(it => new { it.Id, it.PayAmount, it.DealTime })
                .ToList();
            var orderMap = orders.ToDictionary(it => it.Id);
            var items = orders.Count == 0 ? new List<JySaleOrderItem>() : Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && orderMap.Keys.Contains(it.OrderId))
                .ToList();
            var itemMap = items.ToDictionary(it => it.Id);
            var returns = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 1
                    && it.ReturnTime >= begin && it.ReturnTime < endExclusive)
                .Select(it => new { it.Id, it.ReturnTime })
                .ToList();
            var returnIds = returns.Select(it => it.Id).ToList();
            var returnItems = returnIds.Count == 0 ? new List<JySaleReturnItem>() : Context.Queryable<JySaleReturnItem>()
                .Where(it => it.TenantId == actor.TenantId && returnIds.Contains(it.ReturnId))
                .ToList();
            var returnMap = returns.ToDictionary(it => it.Id);
            var styleItems = StyleItems(actor.TenantId);
            var styleMap = styleItems.ToDictionary(it => it.Id);
            var parentMap = CanonicalParents(styleItems);
            var lineTotals = items.GroupBy(it => it.OrderId).ToDictionary(it => it.Key, it => it.Sum(x => x.DealPrice * x.Qty));
            var events = new List<ComboSaleEvent>();
            foreach (var item in items)
            {
                if (!orderMap.TryGetValue(item.OrderId, out var order)) continue;
                var (rootId, _) = ResolveCategoryPath(item.StyleItemId, styleMap, parentMap);
                if (rootId <= 0) continue;
                var lineTotal = lineTotals.GetValueOrDefault(item.OrderId);
                var ratio = lineTotal <= 0 ? 0 : Math.Min(1m, Math.Max(0m, order.PayAmount / lineTotal));
                events.Add(new ComboSaleEvent
                {
                    Time = order.DealTime,
                    CategoryId = rootId,
                    Amount = decimal.Round(item.DealPrice * item.Qty * ratio, 2, MidpointRounding.AwayFromZero)
                });
            }
            foreach (var item in returnItems)
            {
                if (!itemMap.TryGetValue(item.SaleOrderItemId, out var saleItem)) continue;
                if (!returnMap.TryGetValue(item.ReturnId, out var ret)) continue;
                var (rootId, _) = ResolveCategoryPath(saleItem.StyleItemId, styleMap, parentMap);
                if (rootId <= 0) continue;
                events.Add(new ComboSaleEvent
                {
                    Time = ret.ReturnTime,
                    CategoryId = rootId,
                    Amount = -item.RefundAmount
                });
            }
            return events;
        }

        private static string TrimText(string value, int max, string label)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > max) throw new CustomException($"{label}不能超过 {max} 个字");
            return value;
        }

        private static List<JyWeekTargetDto> BuildWeeks(JyGoalReportDto goal, List<JyCategoryWeightDto> weights, int restDays)
        {
            var remaining = Math.Max(0, goal.MonthTargetAmount - goal.DoneAmount);
            var dayTarget = restDays <= 0 ? 0 : remaining / restDays;
            var focus = weights.Take(3).Select(it => it.CategoryName).ToList();
            return new List<JyWeekTargetDto>
            {
                new JyWeekTargetDto
                {
                    WeekLabel = $"本周（剩余 {restDays} 天）",
                    TargetAmount = Round(dayTarget * Math.Min(7, restDays)),
                    FocusCategories = string.Join(" + ", focus),
                    KeyAction = focus.Count > 0 ? $"先把「{focus[0]}」的库存和陈列动起来，再配一条联动内容" : "先补目标与品类数据"
                },
                new JyWeekTargetDto
                {
                    WeekLabel = "下周",
                    TargetAmount = Round(dayTarget * 7),
                    FocusCategories = string.Join(" + ", focus),
                    KeyAction = focus.Count > 1 ? $"围绕「{focus[0]} + {focus[1]}」做一组搭配推荐，同时约老客回访" : "延续本周主推品类"
                }
            };
        }

        private static List<JyTodayActionDto> BuildTodayActions(List<JyGoalPlanRowDto> plans,
            List<JyCategoryWeightDto> weights, List<JyComboDto> combos, List<JyVisitSuggestionDto> visits, int restDays)
        {
            var result = new List<JyTodayActionDto>();
            var used = new HashSet<long>();
            var planMap = plans.GroupBy(it => it.CategoryId).ToDictionary(it => it.Key, it => it.First());
            var weightMap = weights.GroupBy(it => it.CategoryId).ToDictionary(it => it.Key, it => it.First());

            // 1. 已到期或临近复购的客户优先，动作必须落到具体客户。
            var visit = visits
                .Where(it => it.CycleDays == 0 || it.DueDays <= 7)
                .OrderBy(it => it.DueDays)
                .ThenByDescending(it => it.StaleQty)
                .FirstOrDefault();
            if (visit != null)
            {
                var timing = visit.CycleDays == 0
                    ? "只成交过 1 次，尚未形成复购周期"
                    : visit.DueDays <= 0
                        ? $"复购周期 {visit.CycleDays} 天，已到期 {Math.Abs(visit.DueDays)} 天"
                        : $"复购周期 {visit.CycleDays} 天，还有 {visit.DueDays} 天到期";
                result.Add(new JyTodayActionDto
                {
                    CategoryName = "客户回访",
                    Priority = visit.DueDays <= 0 || visit.CycleDays == 0 ? "高" : "中",
                    ActionType = "客户跟进",
                    SourceLabel = "会员档案 / 销售记录",
                    ActionText = $"联系 {visit.MemberName}，对方偏好「{visit.PreferredCategory}」。先问上次佩戴感受，再发 1 件同品类新款实拍，争取约到店试戴。",
                    Basis = $"{timing}；最近一次购买 {visit.DaysSince} 天前"
                        + (visit.StaleQty > 0 ? $"；该品类超 60 天库存 {visit.StaleQty:0} 件，可优先做搭配" : string.Empty),
                    Goal = "复购回访",
                    QuantifiedGoal = "联系 1 人 · 争取 1 次试戴"
                });
            }

            // 2. 按剩余缺口折算今日需要完成的金额，优先处理最紧的品类。
            var gapRows = plans
                .Where(it => it.TargetAmount > 0 && it.TargetAmount > it.DoneAmount)
                .OrderByDescending(it => (it.TargetAmount - it.DoneAmount) / Math.Max(1, restDays))
                .ThenByDescending(it => it.StaleQty)
                .Take(2)
                .ToList();
            foreach (var row in gapRows)
            {
                var gap = Math.Max(0, row.TargetAmount - row.DoneAmount);
                var dailyNeed = gap / Math.Max(1, restDays);
                var autoAction = string.IsNullOrWhiteSpace(row.ActionHigh) ? row.AutoActionMain : row.ActionHigh;
                result.Add(new JyTodayActionDto
                {
                    CategoryId = row.CategoryId,
                    CategoryName = row.CategoryName,
                    Priority = restDays <= 7 || row.Percent < 40 ? "高" : "中",
                    ActionType = row.StaleQty > 0 ? "销售补口 · 去库存" : "销售补口",
                    SourceLabel = "销售目标 / 销售明细 / 库存",
                    ActionText = autoAction,
                    Basis = !string.IsNullOrWhiteSpace(row.ActionHigh) ? "使用已保存的人工动作；" + row.AutoActionBasis : row.AutoActionBasis,
                    Goal = "补销售目标",
                    QuantifiedGoal = $"今日需做 {Money(dailyNeed)} · 剩余缺口 {Money(gap)}",
                    Manual = !string.IsNullOrWhiteSpace(row.ActionHigh)
                });
                used.Add(row.CategoryId);
            }

            // 3. 从已启用组合中选择最贴近目标缺口和库存的组合，不再固定取第一条。
            var combo = combos
                .Where(it => it.CategoryId1.HasValue && it.CategoryId2.HasValue
                    && planMap.ContainsKey(it.CategoryId1.Value) && planMap.ContainsKey(it.CategoryId2.Value))
                .Select(it => new
                {
                    Combo = it,
                    Score = (planMap[it.CategoryId1.Value].TargetAmount - planMap[it.CategoryId1.Value].DoneAmount)
                        + (planMap[it.CategoryId2.Value].TargetAmount - planMap[it.CategoryId2.Value].DoneAmount)
                        + (planMap[it.CategoryId1.Value].StaleQty + planMap[it.CategoryId2.Value].StaleQty) * 100
                        + it.Amount / 10m
                })
                .OrderByDescending(it => it.Score)
                .Select(it => it.Combo)
                .FirstOrDefault();
            if (combo != null && result.Count < 5)
            {
                var stock1 = weightMap.GetValueOrDefault(combo.CategoryId1.Value)?.StockQty ?? 0;
                var stock2 = weightMap.GetValueOrDefault(combo.CategoryId2.Value)?.StockQty ?? 0;
                var lackStock = stock1 <= 0 || stock2 <= 0;
                result.Add(new JyTodayActionDto
                {
                    CategoryName = string.IsNullOrWhiteSpace(combo.Name) ? string.Join(" + ", combo.Categories) : combo.Name,
                    Priority = lackStock ? "低" : "中",
                    ActionType = lackStock ? "组合补货" : "联动组合",
                    SourceLabel = "联动组合 / 近 7 天销售 / 库存",
                    ActionText = lackStock
                        ? $"「{combo.Name}」对应的款式库存不足，今天先核对可售件数或调整组合，不要向客户推荐无法交付的搭配。"
                        : $"用「{combo.Name}」做一组搭配推荐：{string.Join(" + ", combo.Categories)}，围绕{combo.Scene ?? "日常佩戴"}场景展示，争取 1 单连带成交。",
                    Basis = combo.Basis + (string.IsNullOrWhiteSpace(combo.Fit) ? string.Empty : $"；适合 {combo.Fit}"),
                    Goal = "联动出货",
                    QuantifiedGoal = lackStock ? "先核对 1 次组合库存" : "今日争取 1 单组合成交"
                });
            }

            // 4. 没有目标缺口时，补充一件最该处理的压货。
            var stale = plans
                .Where(it => it.StaleQty > 0 && !used.Contains(it.CategoryId))
                .OrderByDescending(it => it.StaleQty)
                .FirstOrDefault();
            if (stale != null && result.Count < 5)
            {
                result.Add(new JyTodayActionDto
                {
                    CategoryId = stale.CategoryId,
                    CategoryName = stale.CategoryName,
                    Priority = stale.StaleQty >= 3 ? "中" : "低",
                    ActionType = "库存处理",
                    SourceLabel = "库存账龄 / 款式分类",
                    ActionText = $"盘点「{stale.CategoryName}」超 60 天库存，先挑 3 件调整陈列和拍照，再定向发给偏好该品类的老客，不做事先降价。",
                    Basis = $"在库 {stale.StockQty:0} 件，其中超 60 天 {stale.StaleQty:0} 件；近 3 月毛利 {stale.GrossRate:0.#}%",
                    Goal = "去库存",
                    QuantifiedGoal = $"处理 {Math.Min(3, Math.Max(1, (int)stale.StaleQty))} 件压货"
                });
            }

            if (result.Count == 0)
            {
                result.Add(new JyTodayActionDto
                {
                    CategoryName = "经营基础数据",
                    Priority = "中",
                    ActionType = "数据补全",
                    SourceLabel = "目标 / 库存 / 会员",
                    ActionText = "目前没有可用于判断的客户到期、销售缺口或库存压力。先维护本月目标，并确认在库商品和会员消费记录，再生成可执行动作。",
                    Basis = "没有触发任何自动动作规则，不生成泛化任务",
                    Goal = "补基础数据",
                    QuantifiedGoal = "完成 1 次基础数据检查"
                });
            }
            return result;
        }

        private static int WorkdaysInMonth(DateTime monthStart)
        {
            var count = 0;
            for (var day = 1; day <= DateTime.DaysInMonth(monthStart.Year, monthStart.Month); day++)
            {
                var weekday = new DateTime(monthStart.Year, monthStart.Month, day).DayOfWeek;
                if (weekday != DayOfWeek.Saturday && weekday != DayOfWeek.Sunday) count++;
            }
            return count == 0 ? 22 : count;
        }

        private static string TrimAction(string text)
        {
            text = text?.Trim();
            return string.IsNullOrEmpty(text) ? null : (text.Length > 500 ? text.Substring(0, 500) : text);
        }

        private static string Money(decimal value)
            => "¥" + Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString("N0");

        private static (long Id, string Name) ResolveCategory(long? styleItemId, Dictionary<long, JyLookupItem> styleMap, Dictionary<long, JyLookupItem> parentMap)
        {
            if (styleItemId.HasValue && styleMap.TryGetValue(styleItemId.Value, out var style))
            {
                if (style.ParentId.HasValue && parentMap.TryGetValue(style.ParentId.Value, out var parent)) return (parent.Id, parent.ItemName);
                if (parentMap.TryGetValue(style.Id, out var canonical)) return (canonical.Id, canonical.ItemName);
                return (style.Id, style.ItemName);
            }
            return (0, "未分类");
        }

        /// <summary>
        /// 返回一级分类和命中的二级款式。未分类商品归到 0，不混入任何款式。
        /// </summary>
        private static (long RootId, long? LeafId) ResolveCategoryPath(long? styleItemId,
            Dictionary<long, JyLookupItem> styleMap, Dictionary<long, JyLookupItem> parentMap)
        {
            if (!styleItemId.HasValue || !styleMap.TryGetValue(styleItemId.Value, out var style)) return (0, null);
            if (style.ParentId.HasValue && parentMap.TryGetValue(style.ParentId.Value, out var parent)) return (parent.Id, style.Id);
            if (parentMap.TryGetValue(style.Id, out var canonical)) return (canonical.Id, null);
            return (style.Id, null);
        }

        /// <summary>可单独展示的款式节点：不含重复名称的一级分类，以及挂在一级分类下的二级款式。</summary>
        private static Dictionary<long, JyLookupItem> LeafCategories(List<JyLookupItem> styleItems,
            Dictionary<long, JyLookupItem> parentMap, out Dictionary<long, long> alias)
        {
            var groups = styleItems
                .Where(it => it.ParentId.HasValue && parentMap.ContainsKey(it.ParentId.Value))
                .OrderBy(it => it.SortNo).ThenBy(it => it.Id)
                .GroupBy(it => it.ParentId.Value + "|" + it.ItemName.Trim())
                .ToList();
            alias = groups.SelectMany(group => group.Select(item => new { item.Id, CanonicalId = group.First().Id }))
                .ToDictionary(it => it.Id, it => it.CanonicalId);
            return groups.Select(it => it.First()).ToDictionary(it => it.Id);
        }

        /// <summary>
        /// 款式字典允许重复初始化，历史数据可能引用不同批次但名称相同的分类。
        /// 报表按名称归并，避免同大类被拆成多行。
        /// </summary>
        private List<JyLookupItem> StyleItems(string tenantId)
        {
            var typeIds = Context.Queryable<JyLookupType>()
                .Where(it => it.TenantId == tenantId && it.TypeCode == "style" && it.DelFlag == 0)
                .Select(it => it.Id)
                .ToList();
            return Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && it.DelFlag == 0 && typeIds.Contains(it.TypeId))
                .ToList();
        }

        private static Dictionary<long, JyLookupItem> CanonicalParents(List<JyLookupItem> styleItems)
        {
            var roots = styleItems.Where(it => !it.ParentId.HasValue).OrderBy(it => it.Id).ToList();
            var byName = roots.GroupBy(it => it.ItemName, StringComparer.Ordinal)
                .ToDictionary(it => it.Key, it => it.First(), StringComparer.Ordinal);
            return roots.ToDictionary(it => it.Id, it => byName[it.ItemName]);
        }

        private static Dictionary<int, string> MaterialGroups()
            => new() { [1] = "足金类", [2] = "足银类", [3] = "其他类" };

        private static DateTime NormalizeAsOf(DateTime? value)
            => (value ?? DateTime.Today).Date > DateTime.Today ? DateTime.Today : (value ?? DateTime.Today).Date;

        private static decimal StockWeight(JyGoodsItem item)
            => Math.Max(0, (item.Weight ?? 0) * Math.Max(0, item.Qty));

        private static decimal StockCost(JyGoodsItem item)
            => Round(Math.Max(0, item.Qty) * Math.Max(0, item.CostPrice));

        private static decimal StockSale(JyGoodsItem item)
            => Round(Math.Max(0, item.Qty) * Math.Max(0, item.SalePrice));

        private static (long ParentId, string ParentName, long? LeafId, string LeafName) ResolveStylePath(
            long? styleItemId, Dictionary<long, JyLookupItem> styleMap, Dictionary<long, JyLookupItem> parentMap,
            Dictionary<long, long> leafAlias)
        {
            if (!styleItemId.HasValue || !styleMap.TryGetValue(styleItemId.Value, out var style))
                return (0, "未分类", null, "未分类");
            if (style.ParentId.HasValue && parentMap.TryGetValue(style.ParentId.Value, out var parent))
            {
                var canonicalLeafId = leafAlias.GetValueOrDefault(style.Id, style.Id);
                var leafName = styleMap.TryGetValue(canonicalLeafId, out var canonicalLeaf)
                    ? canonicalLeaf.ItemName : style.ItemName;
                return (parent.Id, parent.ItemName, canonicalLeafId, leafName ?? "未分类");
            }
            return (style.Id, style.ItemName ?? "未分类", null, "未分类");
        }

        private static void ApplyGoodsFilters(List<JyGoodsItem> goods, Dictionary<long, JyLookupItem> styleMap,
            Dictionary<long, JyLookupItem> parentMap, Dictionary<long, long> leafAlias, int? materialGroup,
            long? styleParentId, long? styleItemId)
        {
            if (goods == null) return;
            if (materialGroup is >= 1 and <= 3)
                goods.RemoveAll(it => (it.MaterialGroup is >= 1 and <= 3 ? it.MaterialGroup : 3) != materialGroup.Value);
            if (styleParentId.HasValue || styleItemId.HasValue)
            {
                goods.RemoveAll(item =>
                {
                    var path = ResolveStylePath(item.StyleItemId, styleMap, parentMap, leafAlias);
                    if (styleParentId.HasValue && path.ParentId != styleParentId.Value) return true;
                    if (styleItemId.HasValue && path.LeafId != styleItemId.Value) return true;
                    return false;
                });
            }
        }

        private List<JyStockLedger> LoadLedgers(string tenantId, List<long> goodsIds, DateTime begin)
        {
            var result = new List<JyStockLedger>();
            foreach (var chunk in goodsIds.Distinct().Chunk(1200))
            {
                var ids = chunk.ToList();
                result.AddRange(Context.Queryable<JyStockLedger>()
                    .Where(it => it.TenantId == tenantId && ids.Contains(it.GoodsItemId) && it.BizTime >= begin)
                    .ToList());
            }
            return result;
        }

        private static LedgerFlow BuildGoodsFlow(JyGoodsItem item, List<JyStockLedger> ledgers, DateTime begin, DateTime endExclusive)
        {
            ledgers ??= new List<JyStockLedger>();
            var period = ledgers.Where(it => it.BizTime >= begin && it.BizTime < endExclusive).ToList();
            var inbound = period.Where(it => it.ChangeQty > 0 && it.ChangeType is 1 or 5 or 9).Sum(it => it.ChangeQty);
            var outbound = period.Where(it => it.ChangeQty < 0 && it.ChangeType is 2 or 3 or 6 or 11).Sum(it => Math.Abs(it.ChangeQty));
            var adjust = period.Where(it => it.ChangeType == 10).Sum(it => it.ChangeQty);
            var afterPeriod = ledgers.Where(it => it.BizTime >= endExclusive)
                .OrderBy(it => it.BizTime).ThenBy(it => it.Id).LastOrDefault();
            var currentQty = Math.Max(0, item.Qty);
            var endingQty = afterPeriod == null
                ? currentQty
                : Math.Max(0, currentQty - ledgers.Where(it => it.BizTime >= endExclusive)
                    .Sum(it => it.ChangeQty));
            return new LedgerFlow
            {
                OpeningQty = endingQty - inbound - adjust + outbound,
                InboundQty = inbound, OutboundQty = outbound, AdjustQty = adjust, EndingQty = endingQty
            };
        }

        private static JyInventoryRowDto ToInventoryRow(InventoryBucket bucket, bool includeChildren)
        {
            return new JyInventoryRowDto
            {
                Level = bucket.Level, MaterialGroup = bucket.MaterialGroup,
                MaterialGroupName = bucket.MaterialGroupName,
                StyleParentId = bucket.StyleParentId, StyleParentName = bucket.StyleParentName,
                StyleItemId = bucket.StyleItemId, StyleItemName = bucket.StyleItemName,
                ItemCount = bucket.ItemIds.Count,
                OpeningQty = Round(bucket.OpeningQty), InboundQty = Round(bucket.InboundQty),
                OutboundQty = Round(bucket.OutboundQty), AdjustQty = Round(bucket.AdjustQty),
                EndingQty = Round(bucket.EndingQty), StockCost = Round(bucket.StockCost),
                StockSale = Round(bucket.StockSale), StockWeight = Round(bucket.StockWeight),
                Children = includeChildren ? bucket.Children
                    .OrderByDescending(it => it.EndingQty).ThenBy(it => it.StyleParentName)
                    .Select(it => ToInventoryRow(it, true)).ToList() : new()
            };
        }

        private Dictionary<long, DateTime> LoadFirstInboundTimes(string tenantId, List<JyGoodsItem> goods, DateTime asOf)
        {
            var result = new Dictionary<long, DateTime>();
            foreach (var chunk in goods.Select(it => it.Id).Chunk(1200))
            {
                var ids = chunk.ToList();
                var rows = Context.Queryable<JyStockLedger>()
                    .Where(it => it.TenantId == tenantId && ids.Contains(it.GoodsItemId)
                        && it.BizTime <= asOf.AddDays(1) && it.ChangeQty > 0
                        && (it.ChangeType == 1 || it.ChangeType == 5 || it.ChangeType == 9))
                    .Select(it => new { it.GoodsItemId, it.BizTime })
                    .ToList();
                foreach (var group in rows.GroupBy(it => it.GoodsItemId))
                {
                    var first = group.Min(it => it.BizTime);
                    if (!result.TryGetValue(group.Key, out var old) || first < old) result[group.Key] = first;
                }
            }
            return result;
        }

        private static string NormalizeAgeBucket(string key)
        {
            var value = (key ?? string.Empty).Trim().ToLowerInvariant();
            if (AgeBucketKeys.Contains(value)) return value;
            throw new CustomException("库龄区间不正确");
        }

        private static readonly string[] AgeBucketKeys = { "lt3m", "m3to5", "within1y", "y1to3", "y3to5", "over5" };
        private static readonly string[] AgeBucketLabels = { "小于3个月", "3-5个月", "1年内", "1-3年", "3-5年", "5年以上" };

        private static string AgeBucketKey(int days)
        {
            if (days < 90) return "lt3m";
            if (days < 150) return "m3to5";
            if (days < 365) return "within1y";
            if (days < 1095) return "y1to3";
            if (days < 1825) return "y3to5";
            return "over5";
        }

        private static string AgeBucketLabel(int days)
        {
            var key = AgeBucketKey(days);
            var index = Array.IndexOf(AgeBucketKeys, key);
            return AgeBucketLabels[index];
        }

        private static StockAgeTotals Accumulate(IEnumerable<StockAgeBucket> roots, decimal totalQty, decimal totalCost)
        {
            var result = new StockAgeTotals();
            foreach (var root in roots)
            {
                foreach (var key in AgeBucketKeys)
                {
                    result.Qty[key] += root.BucketQty.GetValueOrDefault(key);
                    result.Cost[key] += root.BucketCost.GetValueOrDefault(key);
                }
            }
            result.TotalQty = totalQty;
            result.TotalCost = totalCost;
            return result;
        }

        private static List<JyStockAgeBucketDto> AgeBuckets(StockAgeTotals totals)
        {
            return AgeBucketKeys.Select((key, index) => new JyStockAgeBucketDto
            {
                Key = key, Label = AgeBucketLabels[index], Qty = Round(totals.Qty[key]), Cost = Round(totals.Cost[key]),
                Percent = totals.TotalQty <= 0 ? 0 : Round(totals.Qty[key] * 100m / totals.TotalQty),
                CostPercent = totals.TotalCost <= 0 ? 0 : Round(totals.Cost[key] * 100m / totals.TotalCost)
            }).ToList();
        }

        private static JyStockAgeRowDto ToStockAgeRow(StockAgeBucket bucket, decimal totalQty, decimal totalCost, bool includeChildren)
        {
            var values = new StockAgeTotals
            {
                TotalQty = bucket.TotalQty,
                TotalCost = bucket.TotalCost,
                Qty = bucket.BucketQty,
                Cost = bucket.BucketCost
            };
            return new JyStockAgeRowDto
            {
                Level = bucket.Level, MaterialGroup = bucket.MaterialGroup,
                MaterialGroupName = bucket.MaterialGroupName,
                StyleParentId = bucket.StyleParentId, StyleParentName = bucket.StyleParentName,
                StyleItemId = bucket.StyleItemId, StyleItemName = bucket.StyleItemName,
                ItemCount = bucket.ItemIds.Count, TotalQty = Round(bucket.TotalQty),
                TotalCost = Round(bucket.TotalCost), TotalWeight = Round(bucket.TotalWeight),
                Buckets = AgeBuckets(values),
                Children = includeChildren ? bucket.Children
                    .Where(it => it.ItemIds.Count > 0)
                    .OrderByDescending(it => it.TotalQty).ThenBy(it => it.StyleParentName)
                    .Select(it => ToStockAgeRow(it, totalQty, totalCost, true)).ToList() : new()
            };
        }

        private static long CanonicalCategoryId(long categoryId, Dictionary<long, JyLookupItem> styleMap,
            Dictionary<long, JyLookupItem> parentMap)
        {
            if (!styleMap.TryGetValue(categoryId, out var style)) return 0;
            if (style.ParentId.HasValue && parentMap.TryGetValue(style.ParentId.Value, out var parent)) return parent.Id;
            return parentMap.TryGetValue(style.Id, out var canonical) ? canonical.Id : style.Id;
        }

        private static List<JyTrafficDayDto> BuildTrafficTrend(DateTime begin, DateTime end, string granularity,
            List<JyStoreTraffic> rows, List<TrafficEvent> orders, List<TrafficEvent> returns, DateTime today)
        {
            var result = new List<JyTrafficDayDto>();
            var cursor = begin;
            while (cursor <= end)
            {
                var periodStart = StartOfPeriod(cursor, granularity);
                if (result.Any(it => it.Date == periodStart.ToString("yyyy-MM-dd")))
                {
                    cursor = NextPeriod(cursor, granularity);
                    continue;
                }
                var periodEndExclusive = NextPeriod(periodStart, granularity);
                var periodEnd = periodEndExclusive.AddDays(-1);
                var traffic = rows.Where(it => it.TrafficDate >= periodStart && it.TrafficDate <= periodEnd).ToList();
                // 销售/退货时间含时分秒，必须使用右开区间，否则日、周、月最后一天的业务会被漏掉。
                var periodOrders = orders.Where(it => it.Time >= periodStart && it.Time < periodEndExclusive).ToList();
                var periodReturns = returns.Where(it => it.Time >= periodStart && it.Time < periodEndExclusive).ToList();
                var visitors = traffic.Sum(it => it.VisitorCount);
                var dealAmount = periodOrders.Sum(it => it.Amount) - periodReturns.Sum(it => it.Amount);
                result.Add(new JyTrafficDayDto
                {
                    Id = traffic.Count == 1 ? traffic[0].Id : null,
                    Date = periodStart.ToString("yyyy-MM-dd"),
                    Label = PeriodLabel(periodStart, granularity),
                    VisitorCount = visitors,
                    WechatCount = traffic.Sum(it => it.WechatCount),
                    DealOrderCount = periodOrders.Count,
                    DealAmount = Round(dealAmount),
                    DealRate = visitors == 0 ? null : Round(periodOrders.Count * 100m / visitors),
                    WechatRate = visitors == 0 ? 0 : Round(traffic.Sum(it => it.WechatCount) * 100m / visitors),
                    LostStrollCount = traffic.Sum(it => it.LostStrollCount),
                    LostPriceCount = traffic.Sum(it => it.LostPriceCount),
                    LostStyleCount = traffic.Sum(it => it.LostStyleCount),
                    LostConsiderCount = traffic.Sum(it => it.LostConsiderCount),
                    LostTotal = traffic.Sum(it => it.LostStrollCount + it.LostPriceCount + it.LostStyleCount + it.LostConsiderCount),
                    Remark = traffic.Count == 1 ? traffic[0].Remark : null,
                    IsReadOnly = periodStart.Date != today
                });
                cursor = periodEnd.AddDays(1);
            }
            return result;
        }

        private static string TrimRemark(string remark)
        {
            remark = remark?.Trim();
            return string.IsNullOrEmpty(remark) ? null : (remark.Length > 500 ? remark.Substring(0, 500) : remark);
        }

        private sealed class StyleBucket
        {
            public long CategoryId { get; set; }
            public string CategoryName { get; set; }
            public long? ParentCategoryId { get; set; }
            public decimal SalesAmount { get; set; }
            public decimal ReturnAmount { get; set; }
            public decimal NetAmount { get; set; }
            public decimal SalesPieces { get; set; }
            public decimal ReturnPieces { get; set; }
            public decimal NetPieces { get; set; }
            public decimal CostAmount { get; set; }
            public decimal PeriodAmount { get; set; }
            public decimal PeriodPieces { get; set; }
            public HashSet<long> OrderIds { get; } = new();
            public Dictionary<long, StyleBucket> Children { get; set; } = new();
        }

        private sealed class SalesLedgerBucket
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string Level { get; set; }
            public string MaterialGroupName { get; set; }
            public string MaterialName { get; set; }
            public string StyleParentName { get; set; }
            public string StyleItemName { get; set; }
            public decimal SalesAmount { get; set; }
            public decimal CostAmount { get; set; }
            public decimal ReturnAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal RoundOffAmount { get; set; }
            public decimal SalesPieces { get; set; }
            public decimal ReturnPieces { get; set; }
            public HashSet<long> OrderIds { get; } = new();
        }

        private sealed class SalesLedgerGroup
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string StyleParentName { get; set; }
            public string StyleItemName { get; set; }
        }

        private sealed class ReturnAggregate
        {
            public decimal Qty { get; set; }
            public decimal Amount { get; set; }
            public decimal Cost { get; set; }
        }

        private sealed class TrafficEvent
        {
            public DateTime Time { get; set; }
            public decimal Amount { get; set; }
        }

        private sealed class ComboSaleEvent
        {
            public DateTime Time { get; set; }
            public long CategoryId { get; set; }
            public decimal Amount { get; set; }
        }

        public JySalesMetricReportDto SalesMetrics(JyActor actor, JySalesMetricQueryDto query)
        {
            query ??= new JySalesMetricQueryDto();
            var granularity = (query.Granularity ?? "day").Trim().ToLowerInvariant();
            if (granularity is not ("day" or "week" or "month" or "year"))
                throw new CustomException("统计粒度不正确");

            var end = (query.EndTime?.Date ?? DateTime.Today);
            var begin = (query.BeginTime?.Date ?? end.AddDays(-29));
            if (begin > end) throw new CustomException("开始日期不能晚于结束日期");
            if ((end - begin).TotalDays > 3660) throw new CustomException("查询区间不能超过 10 年");
            var endExclusive = end.AddDays(1);

            var orders = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= begin && it.DealTime < endExclusive)
                .Select(it => new { it.DealTime, it.PayAmount, it.PieceQty })
                .ToList();
            var returns = Context.Queryable<JySaleReturn>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 1
                    && it.ReturnTime >= begin && it.ReturnTime < endExclusive)
                .Select(it => new { it.ReturnTime, it.RefundAmount, it.ReturnQty })
                .ToList();

            var buckets = BuildBuckets(begin, end, granularity);
            foreach (var order in orders)
            {
                var key = PeriodKey(order.DealTime, granularity);
                if (!buckets.TryGetValue(key, out var bucket)) continue;
                bucket.SalesAmount += order.PayAmount;
                bucket.SalesPieces += order.PieceQty;
                bucket.OrderCount++;
            }
            foreach (var item in returns)
            {
                var key = PeriodKey(item.ReturnTime, granularity);
                if (!buckets.TryGetValue(key, out var bucket)) continue;
                bucket.ReturnAmount += item.RefundAmount;
                bucket.ReturnPieces += item.ReturnQty;
            }

            var trend = buckets.Values.OrderBy(it => it.Start).Select(it =>
            {
                var netAmount = Round(it.SalesAmount - it.ReturnAmount);
                var netPieces = Round(it.SalesPieces - it.ReturnPieces);
                return new JySalesMetricDto
                {
                    Period = it.Start.ToString("yyyy-MM-dd"),
                    Label = PeriodLabel(it.Start, granularity),
                    SalesAmount = Round(it.SalesAmount),
                    ReturnAmount = Round(it.ReturnAmount),
                    NetAmount = netAmount,
                    SalesPieces = Round(it.SalesPieces),
                    ReturnPieces = Round(it.ReturnPieces),
                    NetPieces = netPieces,
                    OrderCount = it.OrderCount,
                    PiecesPerOrder = it.OrderCount == 0 ? 0 : Round(netPieces / it.OrderCount),
                    AmountPerPiece = netPieces <= 0 ? 0 : Round(netAmount / netPieces)
                };
            }).ToList();

            var totalOrders = trend.Sum(it => it.OrderCount);
            var totalNetAmount = Round(trend.Sum(it => it.NetAmount));
            var totalNetPieces = Round(trend.Sum(it => it.NetPieces));
            return new JySalesMetricReportDto
            {
                BeginTime = begin,
                EndTime = end,
                Granularity = granularity,
                SalesAmount = Round(trend.Sum(it => it.SalesAmount)),
                ReturnAmount = Round(trend.Sum(it => it.ReturnAmount)),
                NetAmount = totalNetAmount,
                SalesPieces = Round(trend.Sum(it => it.SalesPieces)),
                ReturnPieces = Round(trend.Sum(it => it.ReturnPieces)),
                NetPieces = totalNetPieces,
                OrderCount = totalOrders,
                PiecesPerOrder = totalOrders == 0 ? 0 : Round(totalNetPieces / totalOrders),
                AmountPerPiece = totalNetPieces <= 0 ? 0 : Round(totalNetAmount / totalNetPieces),
                Trend = trend
            };
        }

        private static Dictionary<string, MetricBucket> BuildBuckets(DateTime begin, DateTime end, string granularity)
        {
            var result = new Dictionary<string, MetricBucket>();
            var cursor = StartOfPeriod(begin, granularity);
            while (cursor <= end)
            {
                result[PeriodKey(cursor, granularity)] = new MetricBucket { Start = cursor };
                cursor = NextPeriod(cursor, granularity);
            }
            return result;
        }

        private static DateTime StartOfPeriod(DateTime date, string granularity)
        {
            return granularity switch
            {
                "week" => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
                "month" => new DateTime(date.Year, date.Month, 1),
                "year" => new DateTime(date.Year, 1, 1),
                _ => date.Date
            };
        }

        private static DateTime NextPeriod(DateTime date, string granularity)
        {
            return granularity switch
            {
                "week" => date.AddDays(7),
                "month" => date.AddMonths(1),
                "year" => date.AddYears(1),
                _ => date.AddDays(1)
            };
        }

        private static string PeriodKey(DateTime date, string granularity)
            => StartOfPeriod(date, granularity).ToString("yyyy-MM-dd");

        private static string PeriodLabel(DateTime date, string granularity)
        {
            return granularity switch
            {
                "week" => date.ToString("MM-dd") + " 起",
                "month" => date.ToString("yyyy-MM"),
                "year" => date.ToString("yyyy"),
                _ => date.ToString("MM-dd")
            };
        }

        private static decimal Round(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private sealed class MetricBucket
        {
            public DateTime Start { get; set; }
            public decimal SalesAmount { get; set; }
            public decimal ReturnAmount { get; set; }
            public decimal SalesPieces { get; set; }
            public decimal ReturnPieces { get; set; }
            public int OrderCount { get; set; }
        }

        private sealed class LedgerFlow
        {
            public decimal OpeningQty { get; set; }
            public decimal InboundQty { get; set; }
            public decimal OutboundQty { get; set; }
            public decimal AdjustQty { get; set; }
            public decimal EndingQty { get; set; }
        }

        private sealed class InventoryBucket
        {
            public string Level { get; set; }
            public int MaterialGroup { get; set; }
            public string MaterialGroupName { get; set; }
            public long? StyleParentId { get; set; }
            public string StyleParentName { get; set; }
            public long? StyleItemId { get; set; }
            public string StyleItemName { get; set; }
            public HashSet<long> ItemIds { get; } = new();
            public decimal OpeningQty { get; set; }
            public decimal InboundQty { get; set; }
            public decimal OutboundQty { get; set; }
            public decimal AdjustQty { get; set; }
            public decimal EndingQty { get; set; }
            public decimal StockCost { get; set; }
            public decimal StockSale { get; set; }
            public decimal StockWeight { get; set; }
            public List<InventoryBucket> Children { get; set; } = new();
        }

        private sealed class StockAgeBucket
        {
            public string Level { get; set; }
            public int MaterialGroup { get; set; }
            public string MaterialGroupName { get; set; }
            public long? StyleParentId { get; set; }
            public string StyleParentName { get; set; }
            public long? StyleItemId { get; set; }
            public string StyleItemName { get; set; }
            public HashSet<long> ItemIds { get; } = new();
            public decimal TotalQty { get; set; }
            public decimal TotalCost { get; set; }
            public decimal TotalWeight { get; set; }
            public Dictionary<string, decimal> BucketQty { get; } = AgeBucketKeys.ToDictionary(it => it, _ => 0m);
            public Dictionary<string, decimal> BucketCost { get; } = AgeBucketKeys.ToDictionary(it => it, _ => 0m);
            public List<StockAgeBucket> Children { get; set; } = new();
        }

        private sealed class StockAgeTotals
        {
            public decimal TotalQty { get; set; }
            public decimal TotalCost { get; set; }
            public Dictionary<string, decimal> Qty { get; set; } = AgeBucketKeys.ToDictionary(it => it, _ => 0m);
            public Dictionary<string, decimal> Cost { get; set; } = AgeBucketKeys.ToDictionary(it => it, _ => 0m);
        }
    }
}
