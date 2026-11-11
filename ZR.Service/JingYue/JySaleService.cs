using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JySaleService : BaseService<JySaleOrder>
    {
        private readonly JyBillRuleService _billRule;
        private JyReferralService Referral => App.GetService<JyReferralService>();
        private JyCustomService CustomService => App.GetService<JyCustomService>();

        public JySaleService(JyBillRuleService billRule)
        {
            _billRule = billRule;
        }

        private JyWorkItemService WorkItem => App.GetService<JyWorkItemService>();

        public PagedInfo<JySaleDto> List(JyActor actor, JySaleQueryDto query)
        {
            query ??= new JySaleQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is >= 0 and <= 9, it => it.Status == query.Status)
                .WhereIF(query.OrderType is >= 1 and <= 3, it => it.OrderType == query.OrderType)
                .WhereIF(query.BeginTime.HasValue, it => it.DealTime >= query.BeginTime.Value.Date)
                .WhereIF(query.EndTime.HasValue, it => it.DealTime < query.EndTime.Value.Date.AddDays(1))
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.OrderNo.Contains(keyword) || it.Remark.Contains(keyword)
                    || it.CustomerName.Contains(keyword) || it.CustomerPhone.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.Status)
                .OrderBy(it => it.DealTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JySaleDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDtos(actor.TenantId, rows)
            };
        }

        public JySaleDto Detail(JyActor actor, long id)
        {
            var row = Must(actor.TenantId, id);
            var dto = ToDtos(actor.TenantId, new List<JySaleOrder> { row }).First();
            dto.Items = LoadItems(actor.TenantId, new[] { row.Id });
            dto.Offsets = LoadOffsets(actor.TenantId, row.Id);
            return dto;
        }

        /// <summary>按手机号/姓名识别客户；手机号精确优先，其次姓名精确，再返回模糊候选。</summary>
        public JyCustomerMatchDto MatchCustomer(JyActor actor, string keyword)
        {
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword)) return new JyCustomerMatchDto();
            if (keyword.Length > 50) throw new CustomException("客户查询内容不能超过 50 个字");

            var exact = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .First(it => it.Phone == keyword)
                ?? Context.Queryable<JyMember>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                    .First(it => it.WechatNo == keyword)
                ?? Context.Queryable<JyMember>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                    .First(it => it.MemberName == keyword);
            var candidates = exact == null
                ? Context.Queryable<JyMember>()
                    .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0
                        && (it.MemberName.Contains(keyword) || it.Phone.Contains(keyword) || it.WechatNo.Contains(keyword)))
                    .OrderBy(it => it.LastConsumeTime, OrderByType.Desc).Take(8).ToList()
                : new List<JyMember> { exact };
            var levelIds = candidates.Where(it => it.LevelId.HasValue).Select(it => it.LevelId.Value).Distinct().ToList();
            var levels = levelIds.Count == 0 ? new Dictionary<long, JyMemberLevel>() : Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == actor.TenantId && levelIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            var result = new JyCustomerMatchDto
            {
                Exists = exact != null,
                MatchCount = candidates.Count,
                Candidates = candidates.Select(it =>
                {
                    levels.TryGetValue(it.LevelId ?? 0, out var level);
                    return new JyCustomerCandidateDto
                    {
                        MemberId = it.Id,
                        MemberNo = it.MemberNo,
                        MemberName = it.MemberName,
                        Phone = it.Phone,
                        WechatNo = it.WechatNo,
                        Sex = it.Sex,
                        LevelName = level?.LevelName ?? "未分级",
                        SolarBirthday = it.SolarBirthday,
                        BirthYear = it.BirthYear,
                        BirthdayText = BirthdayText(it.SolarBirthday, it.BirthYear)
                    };
                }).ToList()
            };
            if (exact != null)
            {
                levels.TryGetValue(exact.LevelId ?? 0, out var exactLevel);
                result.MemberId = exact.Id;
                result.MemberNo = exact.MemberNo;
                result.MemberName = exact.MemberName;
                result.Phone = exact.Phone;
                result.WechatNo = exact.WechatNo;
                result.Sex = exact.Sex;
                result.LevelId = exact.LevelId;
                result.LevelName = exactLevel?.LevelName ?? "未分级";
                result.DiscountRate = exactLevel?.DiscountRate ?? 1m;
                result.PointsBalance = exact.PointsBalance;
                result.YearPoints = exact.YearPoints;
                result.TotalAmount = exact.TotalAmount;
                result.OrderCount = exact.OrderCount;
                result.LastConsumeTime = exact.LastConsumeTime;
                result.SolarBirthday = exact.SolarBirthday;
                result.BirthYear = exact.BirthYear;
                result.BirthdayText = BirthdayText(exact.SolarBirthday, exact.BirthYear);
                result.Referrers = LoadReferrers(actor.TenantId, exact.Id);
            }
            return result;
        }

        public List<JyMemberReferrerDto> CustomerReferrers(JyActor actor, long memberId)
        {
            var member = Context.Queryable<JyMember>()
                .First(it => it.Id == memberId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("会员不存在");
            return LoadReferrers(actor.TenantId, member.Id);
        }

        /// <summary>开单时展示该客户已维护的多位推荐人，默认带出主推荐人。</summary>
        private List<JyMemberReferrerDto> LoadReferrers(string tenantId, long memberId)
        {
            var links = Context.Queryable<JyMemberReferrer>()
                .Where(it => it.TenantId == tenantId && it.MemberId == memberId && it.DelFlag == 0)
                .OrderBy(it => it.IsPrimary, OrderByType.Desc).OrderBy(it => it.Id).ToList();
            if (links.Count == 0) return new List<JyMemberReferrerDto>();
            var ids = links.Select(it => it.ReferrerMemberId).Distinct().ToList();
            var members = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == tenantId && ids.Contains(it.Id))
                .Select(it => new { it.Id, it.MemberNo, it.MemberName, it.WechatNo, it.Phone })
                .ToList().ToDictionary(it => it.Id);
            var orderIds = links.Where(it => it.SaleOrderId.HasValue)
                .Select(it => it.SaleOrderId.Value).Distinct().ToList();
            var orders = orderIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == tenantId && orderIds.Contains(it.Id))
                .Select(it => new { it.Id, it.OrderNo }).ToList().ToDictionary(it => it.Id, it => it.OrderNo);
            return links.Select(it =>
            {
                members.TryGetValue(it.ReferrerMemberId, out var referrer);
                return new JyMemberReferrerDto
                {
                    Id = it.Id,
                    MemberId = it.MemberId,
                    ReferrerMemberId = it.ReferrerMemberId,
                    ReferrerName = referrer?.MemberName,
                    ReferrerNo = referrer?.MemberNo,
                    ReferrerWechatNo = referrer?.WechatNo,
                    ReferrerPhone = referrer?.Phone,
                    IsPrimary = it.IsPrimary,
                    Source = it.Source,
                    SaleOrderId = it.SaleOrderId,
                    SaleOrderNo = it.SaleOrderId.HasValue && orders.TryGetValue(it.SaleOrderId.Value, out var no) ? no : null,
                    CreateTime = it.Create_time,
                    Remark = it.Remark
                };
            }).ToList();
        }

        /// <summary>扫码/输入编码返回可售货品。已绑定当前客户的定制定金商品也可售，但必须走定金冲抵。</summary>
        public List<JyStockItemDto> FindSaleGoods(JyActor actor, string keyword, long? memberId = null, string phone = null)
        {
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword)) return new List<JyStockItemDto>();
            if (keyword.Length > 64) throw new CustomException("商品查询内容不能超过 64 个字");
            phone = NormalizePhone(phone);
            var lockedIds = FindCustomerLockedGoodsIds(actor, memberId, phone);
            var q = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Qty > 0
                    && (it.Status == 1 || (it.Status == 2 && lockedIds.Contains(it.Id))));
            var exact = q.Where(it => it.Barcode == keyword || it.GoodsNo == keyword).ToList();
            if (exact.Count > 0) return exact.Select(ToStockItem).ToList();
            return q.Where(it => it.GoodsNo.Contains(keyword) || it.Barcode.Contains(keyword)
                    || it.StyleName.Contains(keyword) || it.StyleItemName.Contains(keyword))
                .OrderBy(it => it.Id, OrderByType.Desc).Take(20).ToList()
                .Select(ToStockItem).ToList();
        }

        public JySaleDashboardDto Dashboard(JyActor actor)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var weekStart = today.AddDays(-6);
            var rows = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.DealTime >= weekStart && it.DealTime < tomorrow)
                .ToList();
            var todayRows = rows.Where(it => it.DealTime >= today && it.DealTime < tomorrow).ToList();
            var trend = new List<JySaleTrendDto>();
            for (var i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var next = day.AddDays(1);
                var dayRows = rows.Where(it => it.DealTime >= day && it.DealTime < next).ToList();
                trend.Add(new JySaleTrendDto
                {
                    Date = day.ToString("MM-dd"),
                    Amount = dayRows.Sum(it => it.PayAmount),
                    Orders = dayRows.Count
                });
            }
            var recentRows = Context.Queryable<JySaleOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1)
                .OrderBy(it => it.DealTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .Take(6).ToList();
            return new JySaleDashboardDto
            {
                TodayAmount = todayRows.Sum(it => it.PayAmount),
                TodayOrders = todayRows.Count,
                TodayPieces = todayRows.Sum(it => it.PieceQty),
                AvgTicket = todayRows.Count == 0 ? 0 : Math.Round(todayRows.Sum(it => it.PayAmount) / todayRows.Count, 2),
                Recent = ToDtos(actor.TenantId, recentRows),
                Trend = trend
            };
        }

        public JySaleOptionsDto Options(JyActor actor)
        {
            _billRule.EnsureFoundation(actor);
            var goods = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .Take(1000).ToList();
            var members = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .OrderBy(it => it.LastConsumeTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .Take(300).ToList();
            var now = DateTime.Now;
            var activities = Context.Queryable<JyActivity>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1
                    && it.StartTime <= now && it.EndTime >= now)
                .OrderBy(it => it.EndTime).ToList();
            return new JySaleOptionsDto
            {
                Goods = goods.Select(ToStockItem).ToList(),
                Members = members.Select(it => new JyOptionDto
                {
                    Id = it.Id,
                    Code = it.MemberNo,
                    Name = string.IsNullOrWhiteSpace(it.Phone) ? it.MemberName : it.MemberName + " · " + it.Phone
                }).ToList(),
                Activities = activities.Select(it => new JyOptionDto
                {
                    Id = it.Id,
                    Code = it.ActivityNo,
                    Name = it.ActivityName
                }).ToList()
            };
        }

        public JySalePreviewDto Preview(JyActor actor, JySaleEditDto dto)
        {
            return BuildCalculation(actor, dto).ToPreview();
        }

        public long Save(JyActor actor, JySaleEditDto dto)
        {
            if (dto == null) throw new CustomException("销售单不能为空");
            dto.Remark = TrimRemark(dto.Remark);
            dto.CustomerName = TrimText(dto.CustomerName, 50, "客户姓名");
            dto.CustomerPhone = NormalizePhone(dto.CustomerPhone);
            dto.CustomerWechatNo = NormalizeWechat(dto.CustomerWechatNo);
            dto.ReferrerWechatNo = NormalizeWechat(dto.ReferrerWechatNo);
            if (dto.ReferrerMemberId > 0) EnsureReferrer(actor, dto.MemberId, dto.ReferrerMemberId.Value);
            if (dto.CustomerSex is < 0 or > 2) throw new CustomException("客户性别不正确");
            if (dto.AdjustAmount < -1000000m || dto.AdjustAmount > 1000000m)
                throw new CustomException("手工调价金额超出允许范围");
            if (dto.RoundOffAmount < 0 || dto.RoundOffAmount > 1000000m)
                throw new CustomException("抹零金额不能为负数且不能过大");
            var calc = BuildCalculation(actor, dto);
            if (dto.Id > 0)
            {
                var current = Must(actor.TenantId, dto.Id);
                if (current.Status != 0) throw new CustomException("已完成或作废的销售单不能修改");
            }

            long resultId = 0;
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                JySaleOrder header;
                if (dto.Id > 0)
                {
                    header = Must(actor.TenantId, dto.Id);
                    if (header.Status != 0) throw new CustomException("销售单状态已变化，请刷新后重试");
                    header.StoreId = dto.StoreId > 0 ? dto.StoreId : null;
                    header.OrderType = dto.OrderType;
                    header.MemberId = dto.MemberId > 0 ? dto.MemberId : null;
                    header.ActivityId = dto.ActivityId > 0 ? dto.ActivityId : null;
                    header.TotalAmount = calc.TotalAmount;
                    header.DiscountAmount = calc.MemberDiscount;
                    header.ActivityBenefit = calc.ActivityBenefit;
                    header.BasePayAmount = calc.BasePayAmount;
                    header.AdjustAmount = calc.AdjustAmount;
                    header.RoundOffAmount = calc.RoundOffAmount;
                    header.PayAmount = calc.PayAmount;
                    header.CostAmount = calc.CostAmount;
                    header.PieceQty = calc.PieceQty;
                    header.CustomerName = dto.CustomerName;
                    header.CustomerPhone = dto.CustomerPhone;
                    header.CustomerWechatNo = dto.CustomerWechatNo;
                    var editReferrer = ResolveSaleReferrer(actor, header.MemberId, dto.ReferrerMemberId,
                        dto.ReferrerWechatNo, header.Id, null);
                    header.ReferrerMemberId = editReferrer?.Id;
                    header.ReferrerWechatNo = editReferrer?.WechatNo ?? dto.ReferrerWechatNo;
                    header.CustomerSex = dto.CustomerSex;
                    header.CustomerBirthday = dto.CustomerBirthday;
                    header.CustomerBirthYear = dto.CustomerBirthYear;
                    header.Remark = dto.Remark;
                    header.Update_by = actor.UserName;
                    header.Update_time = now;
                    var affected = Context.Updateable(header).UpdateColumns(it => new
                    {
                        it.StoreId, it.OrderType, it.MemberId, it.ActivityId, it.TotalAmount,
                        it.DiscountAmount, it.ActivityBenefit, it.BasePayAmount, it.AdjustAmount,
                        it.RoundOffAmount, it.PayAmount, it.CostAmount, it.PieceQty,
                        it.CustomerName, it.CustomerPhone, it.CustomerWechatNo, it.ReferrerWechatNo, it.ReferrerMemberId, it.CustomerSex,
                        it.CustomerBirthday, it.CustomerBirthYear,
                        it.Remark, it.Update_by, it.Update_time
                    }).Where(it => it.Id == header.Id && it.TenantId == actor.TenantId
                        && it.DelFlag == 0 && it.Status == 0).ExecuteCommand();
                    if (affected != 1) throw new CustomException("销售单状态已变化，请刷新后重试");
                    if (header.MemberId.HasValue && header.ReferrerMemberId.HasValue)
                        BindReferrer(actor, header.MemberId.Value, header.ReferrerMemberId.Value, 2, header.Id, now);
                    Context.Deleteable<JySaleOrderItem>()
                        .Where(it => it.TenantId == actor.TenantId && it.OrderId == header.Id)
                        .ExecuteCommand();
                }
                else
                {
                    var billType = dto.OrderType == 2 ? "gift" : "sale";
                    var issued = _billRule.IssueInTransaction(actor, billType, "jy_sale_order", null);
                    var createReferrer = ResolveSaleReferrer(actor, dto.MemberId, dto.ReferrerMemberId,
                        dto.ReferrerWechatNo, null, null);
                    header = new JySaleOrder
                    {
                        TenantId = actor.TenantId,
                        StoreId = dto.StoreId > 0 ? dto.StoreId : null,
                        OrderNo = issued.BillNo,
                        OrderType = dto.OrderType,
                        IsBackfill = 0,
                        MemberId = dto.MemberId > 0 ? dto.MemberId : null,
                        ActivityId = dto.ActivityId > 0 ? dto.ActivityId : null,
                        ActivityBenefit = calc.ActivityBenefit,
                        Status = 0,
                        TotalAmount = calc.TotalAmount,
                        DiscountAmount = calc.MemberDiscount,
                        BasePayAmount = calc.BasePayAmount,
                        AdjustAmount = calc.AdjustAmount,
                        RoundOffAmount = calc.RoundOffAmount,
                        PayAmount = calc.PayAmount,
                        CostAmount = calc.CostAmount,
                        PieceQty = calc.PieceQty,
                        CustomerName = dto.CustomerName,
                        CustomerPhone = dto.CustomerPhone,
                        CustomerWechatNo = dto.CustomerWechatNo,
                        ReferrerMemberId = createReferrer?.Id,
                        ReferrerWechatNo = createReferrer?.WechatNo ?? dto.ReferrerWechatNo,
                        CustomerSex = dto.CustomerSex,
                        CustomerBirthday = dto.CustomerBirthday,
                        CustomerBirthYear = dto.CustomerBirthYear,
                        DealTime = now,
                        DelFlag = 0,
                        Create_by = actor.UserName,
                        Create_time = now,
                        Remark = dto.Remark
                    };
                    header.Id = Context.Insertable(header).ExecuteReturnBigIdentity();
                }
                resultId = header.Id;

                var lines = calc.Items.Select((it, index) => new JySaleOrderItem
                {
                    TenantId = actor.TenantId,
                    OrderId = header.Id,
                    SortNo = index + 1,
                    GoodsItemId = it.GoodsItemId,
                    GoodsNo = it.GoodsNo,
                    Barcode = it.Barcode,
                    GoodsName = it.GoodsName ?? it.StyleName,
                    StyleId = calc.GoodsMap[it.GoodsItemId].StyleId,
                    StyleName = it.StyleName,
                    StyleItemId = calc.GoodsMap[it.GoodsItemId].StyleItemId,
                    StyleItemName = it.StyleItemName,
                    MaterialItemId = calc.GoodsMap[it.GoodsItemId].MaterialItemId,
                    MaterialGroup = calc.GoodsMap[it.GoodsItemId].MaterialGroup,
                    MaterialName = it.MaterialName,
                    MetalItemId = calc.GoodsMap[it.GoodsItemId].MetalItemId,
                    MetalName = it.MetalName,
                    Weight = it.Weight,
                    Unit = it.Unit,
                    Qty = it.Qty,
                    StockQty = it.StockQty,
                    CostPrice = it.CostPrice,
                    SalePrice = it.SalePrice,
                    DealPrice = it.DealPrice,
                    PricingMode = it.PricingMode,
                    MetalPriceId = it.MetalPriceId,
                    MarketMetalPricePerGram = it.MarketMetalPricePerGram,
                    MetalPricePerGram = it.MetalPricePerGram,
                    PriceSnapshotTime = it.PriceSnapshotTime,
                    LaborFee = it.LaborFee,
                    PackagingFee = it.PackagingFee,
                    LaborFeeMode = it.LaborFeeMode,
                    MaterialAmount = it.MaterialAmount,
                    LaborAmount = it.LaborAmount,
                    PackagingAmount = it.PackagingAmount,
                    ListAmount = it.ListAmount,
                    MemberDiscountRuleId = it.MemberDiscountRuleId,
                    MemberDiscountRate = it.MemberDiscountRate,
                    MemberDiscountAmount = it.MemberDiscountAmount,
                    ActivityBenefit = it.ActivityBenefit,
                    PayAmount = it.PayAmount,
                    IsGift = it.IsGift,
                    PriceSource = it.IsGift == 1 || it.DealPrice == it.SalePrice ? 1 : 3,
                    ReturnedQty = 0,
                    Create_time = now
                }).ToList();
                Context.Insertable(lines).ExecuteCommand();
                ReplaceDraftOffsets(actor, header, dto, now);
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "销售单保存失败");
            return resultId;
        }
        public int Confirm(JyActor actor, long id)
        {
            var tran = UseTran(() =>
            {
                var now = DateTime.Now;
                var header = Context.Queryable<JySaleOrder>()
                    .With(SqlWith.UpdLock)
                    .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0);
                if (header == null) throw new CustomException("销售单不存在");
                if (header.Status != 0) throw new CustomException("销售单已完成或作废，不能重复确认");
                var storedItems = Context.Queryable<JySaleOrderItem>()
                    .Where(it => it.TenantId == actor.TenantId && it.OrderId == id)
                    .OrderBy(it => it.SortNo).ToList();
                if (storedItems.Count == 0) throw new CustomException("销售单没有商品，不能确认");

                var dto = new JySaleEditDto
                {
                    Id = id,
                    StoreId = header.StoreId,
                    OrderType = header.OrderType,
                    MemberId = header.MemberId,
                    ActivityId = header.ActivityId,
                    // 金额以草稿快照为准，这里只重算商品与库存；避免会员等级变化影响旧草稿的折扣校验。
                    ApplyMemberDiscount = false,
                    AdjustAmount = 0,
                    RoundOffAmount = 0,
                    CustomerName = header.CustomerName,
                    CustomerPhone = header.CustomerPhone,
                    CustomerWechatNo = header.CustomerWechatNo,
                    ReferrerWechatNo = header.ReferrerWechatNo,
                    ReferrerMemberId = header.ReferrerMemberId,
                    CustomerSex = header.CustomerSex,
                    CustomerBirthday = header.CustomerBirthday,
                    CustomerBirthYear = header.CustomerBirthYear,
                    Remark = header.Remark,
                    Items = storedItems.Select(it => new JySaleItemEditDto
                    {
                        GoodsItemId = it.GoodsItemId,
                        Qty = it.Qty,
                        StockQty = it.StockQty,
                        DealPrice = it.DealPrice,
                        IsGift = it.IsGift,
                        Weight = it.Weight,
                        Unit = it.Unit,
                        PreservePriceSnapshot = true,
                        PricingMode = it.PricingMode,
                        MetalPriceId = it.MetalPriceId,
                        MarketMetalPricePerGram = it.MarketMetalPricePerGram,
                        MetalPricePerGram = it.MetalPricePerGram,
                        PriceSnapshotTime = it.PriceSnapshotTime,
                        LaborFee = it.LaborFee,
                        PackagingFee = it.PackagingFee,
                        LaborFeeMode = it.LaborFeeMode,
                        MaterialAmount = it.MaterialAmount,
                        LaborAmount = it.LaborAmount,
                        ListAmount = it.ListAmount,
                        MemberDiscountRuleId = it.MemberDiscountRuleId,
                        MemberDiscountRate = it.MemberDiscountRate,
                        MemberDiscountAmount = it.MemberDiscountAmount,
                        ActivityBenefit = it.ActivityBenefit,
                        PayAmount = it.PayAmount
                    }).ToList()
                };
                if (header.MemberId == null && (!string.IsNullOrWhiteSpace(header.CustomerPhone)
                    || !string.IsNullOrWhiteSpace(header.CustomerWechatNo)))
                {
                    var existingMember = Context.Queryable<JyMember>().First(it => it.TenantId == actor.TenantId
                        && it.DelFlag == 0
                        && ((it.Phone != null && it.Phone == header.CustomerPhone)
                            || (it.WechatNo != null && it.WechatNo == header.CustomerWechatNo)));
                    if (existingMember != null)
                    {
                        header.MemberId = existingMember.Id;
                        dto.MemberId = existingMember.Id;
                    }
                    else if (!string.IsNullOrWhiteSpace(header.CustomerName))
                    {
                        var defaultLevelId = Context.Queryable<JyMemberLevel>()
                            .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                            .OrderBy(it => it.SortNo).First()?.Id;
                        var issued = _billRule.IssueInTransaction(actor, "member", "jy_member", null);
                        var newMember = new JyMember
                        {
                            TenantId = actor.TenantId,
                            StoreId = header.StoreId,
                            MemberNo = issued.BillNo,
                            MemberName = header.CustomerName,
                            Sex = header.CustomerSex,
                            Phone = header.CustomerPhone,
                            WechatNo = header.CustomerWechatNo,
                            ReferrerWechatNo = header.ReferrerWechatNo,
                            ReferrerMemberId = header.ReferrerMemberId
                                ?? ResolveReferrerForSale(actor, header.ReferrerWechatNo),
                            SolarBirthday = header.CustomerBirthday,
                            BirthYear = header.CustomerBirthYear,
                            LevelId = defaultLevelId,
                            LevelChangedTime = defaultLevelId.HasValue ? now : null,
                            Status = 0,
                            DelFlag = 0,
                            Create_by = actor.UserName,
                            Create_time = now,
                            Remark = "柜台开单自动创建"
                        };
                        newMember.Id = Context.Insertable(newMember).ExecuteReturnBigIdentity();
                        header.MemberId = newMember.Id;
                        dto.MemberId = newMember.Id;
                        if (newMember.ReferrerMemberId.HasValue)
                            BindReferrer(actor, newMember.Id, newMember.ReferrerMemberId.Value, 2, header.Id, now);
                    }
                }
                else if (header.MemberId.HasValue && header.ReferrerMemberId.HasValue)
                {
                    BindReferrer(actor, header.MemberId.Value, header.ReferrerMemberId.Value, 2, header.Id, now);
                }
                var calc = BuildCalculation(actor, dto, true);
                // 草稿保存时已经按会员等级折扣算过，确认收款只沿用该结果。
                // 若在这里重新按当前等级计算，会员等级调整或未带 ApplyMemberDiscount 都会把折扣加回实收。
                var storedDiscount = RoundMoney(Math.Max(0, Math.Min(header.DiscountAmount, calc.TotalAmount)));
                var storedActivity = RoundMoney(Math.Max(0, Math.Min(header.ActivityBenefit,
                    calc.TotalAmount - storedDiscount)));
                // 以草稿的成交价、折扣、活动优惠、调价和抹零重新组合最终应收。
                // 不直接信任旧的 PayAmount，避免历史错误草稿把会员折扣重新加回。
                var storedBase = RoundMoney(Math.Max(0, calc.TotalAmount - storedDiscount - storedActivity));
                var storedRoundOff = RoundMoney(Math.Min(Math.Max(0, header.RoundOffAmount),
                    Math.Max(0, storedBase + header.AdjustAmount)));
                var storedAdjust = RoundMoney(header.AdjustAmount);
                var storedPay = RoundMoney(Math.Max(0, storedBase + storedAdjust - storedRoundOff));
                calc.MemberDiscount = storedDiscount;
                calc.ActivityBenefit = storedActivity;
                calc.BasePayAmount = storedBase;
                calc.AdjustAmount = storedAdjust;
                calc.RoundOffAmount = storedRoundOff;
                calc.PayAmount = storedPay;
                // 旧草稿没有行级快照，或历史订单金额与行实付不一致时，重新分摊到行。
                if (calc.Items.Any(it => it.ListAmount <= 0)
                    || Math.Abs(calc.Items.Sum(it => it.PayAmount) - calc.PayAmount) > 0.01m)
                    AllocateLinePayments(calc.Items, calc.PayAmount);
                WriteLineSnapshots(actor, storedItems, calc.Items, now);
                calc.PointsEarned = CalculatePoints(actor, calc.PayAmount, calc.PointsMultiple);

                foreach (var line in calc.Items)
                {
                    var goods = calc.GoodsMap[line.GoodsItemId];
                    var before = goods.Qty;
                var after = goods.ItemMode == 1 ? 0 : before - line.Qty;
                    if (after < 0) throw new CustomException($"商品 {goods.GoodsNo} 库存不足");
                    var newStatus = after <= 0 ? (line.IsGift == 1 ? 4 : 3) : 1;
                    var affected = Context.Updateable<JyGoodsItem>()
                        .SetColumns(it => it.Qty == after)
                        .SetColumns(it => it.Status == newStatus)
                        .SetColumns(it => it.LockedQty == 0)
                        .SetColumns(it => it.LockBizType == null)
                        .SetColumns(it => it.LockBizId == null)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == goods.Id && it.TenantId == actor.TenantId
                            && it.DelFlag == 0 && (it.Status == 1 || it.Status == 2) && it.Qty >= line.Qty)
                        .ExecuteCommand();
                    if (affected != 1) throw new CustomException($"商品 {goods.GoodsNo} 已被其他单据占用，请刷新后重试");
                    Context.Insertable(new JyStockLedger
                    {
                        TenantId = actor.TenantId,
                        StoreId = header.StoreId,
                        LedgerNo = LedgerNo(goods.GoodsNo, line.IsGift == 1 ? "GF" : "SL"),
                        GoodsItemId = goods.Id,
                        GoodsNo = goods.GoodsNo,
                        ChangeType = line.IsGift == 1 ? 6 : 2,
                        ChangeQty = -line.Qty,
                        BeforeQty = before,
                        AfterQty = after,
                        BizType = "sale",
                        BizId = header.Id,
                        BizTime = now,
                        Create_by = actor.UserName,
                        Create_time = now,
                        Remark = header.OrderNo
                    }).ExecuteCommand();
                }

                header.TotalAmount = calc.TotalAmount;
                header.DiscountAmount = calc.MemberDiscount;
                header.ActivityBenefit = calc.ActivityBenefit;
                header.BasePayAmount = calc.BasePayAmount;
                header.AdjustAmount = calc.AdjustAmount;
                header.RoundOffAmount = calc.RoundOffAmount;
                header.PayAmount = calc.PayAmount;
                header.CostAmount = calc.CostAmount;
                header.PieceQty = calc.PieceQty;
                EnsureAutoOffsets(actor, header, calc, now);
                ApplyConfirmedOffsets(actor, header, now);
                header.Status = 1;
                header.DealTime = now;
                header.Update_by = actor.UserName;
                header.Update_time = now;
                var updated = Context.Updateable(header).UpdateColumns(it => new
                {
                    it.TotalAmount, it.DiscountAmount, it.ActivityBenefit, it.BasePayAmount,
                    it.AdjustAmount, it.RoundOffAmount, it.PayAmount,
                    it.CostAmount, it.PieceQty, it.CustomOffsetAmount, it.CashAmount, it.MemberId, it.Status, it.DealTime, it.Update_by, it.Update_time
                }).Where(it => it.Id == header.Id && it.Status == 0).ExecuteCommand();
                if (updated != 1) throw new CustomException("销售单状态已变化，请刷新后重试");

                if (header.OrderType == 1 && header.ActivityId.HasValue && header.MemberId.HasValue && calc.Activity != null)
                {
                    Context.Insertable(new JyActivityMember
                    {
                        TenantId = actor.TenantId,
                        ActivityId = calc.Activity.Id,
                        MemberId = header.MemberId.Value,
                        SaleOrderId = header.Id,
                        BenefitAmount = calc.ActivityBenefit,
                        JoinTime = now,
                        Create_time = now
                    }).ExecuteCommand();
                    Context.Updateable<JyActivity>()
                        .SetColumns(it => it.UsedAmount == it.UsedAmount + calc.ActivityBenefit)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == calc.Activity.Id && it.TenantId == actor.TenantId)
                        .ExecuteCommand();
                }

                if (header.MemberId.HasValue && header.OrderType == 1 && calc.Member != null)
                {
                    ApplyMemberConsumption(actor, calc.Member, header, calc.PointsEarned, now);
                }

                // 转介绍奖励：买家若绑定了推荐人，则按当前返现口径生成台账（待线下发放）
                if (header.MemberId.HasValue)
                {
                    Referral.AwardForSaleInTransaction(actor, header, now);
                }
            });
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "确认销售失败");
            var confirmed = Context.Queryable<JySaleOrder>().First(it => it.Id == id && it.TenantId == actor.TenantId);
            if (confirmed?.DealTime != null)
                WorkItem.TryRecalculateByBusinessDate(actor, confirmed.DealTime);
            return 1;
        }

        public int Delete(JyActor actor, long id)
        {
            var tran = UseTran(() => VoidSale(actor, id));
            if (!tran.IsSuccess) throw new CustomException(tran.ErrorMessage ?? "销售单删除失败");
            var voided = Context.Queryable<JySaleOrder>().First(it => it.Id == id && it.TenantId == actor.TenantId);
            // 只有已确认销售作废才需要重算；草稿删除不应改变销售目标口径。
            if (voided?.Status == 9)
                WorkItem.TryRecalculateByBusinessDate(actor, voided.DealTime);
            return 1;
        }
        private SaleCalc BuildCalculation(JyActor actor, JySaleEditDto dto, bool forConfirm = false)
        {
            if (dto == null) throw new CustomException("销售单不能为空");
            if (dto.OrderType is < 1 or > 3) throw new CustomException("销售类型不正确");
            if (dto.Items == null || dto.Items.Count == 0) throw new CustomException("请至少选择一件商品");
            if (dto.Items.Count > 100) throw new CustomException("单张销售单最多录入 100 件商品");

            var duplicate = dto.Items.GroupBy(it => it.GoodsItemId).FirstOrDefault(it => it.Key <= 0 || it.Count() > 1);
            if (duplicate != null) throw new CustomException("销售明细中存在重复或无效的商品");

            var goodsIds = dto.Items.Select(it => it.GoodsItemId).Distinct().ToList();
            var goodsRows = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && goodsIds.Contains(it.Id))
                .ToList();
            if (goodsRows.Count != goodsIds.Count) throw new CustomException("部分商品不存在或已删除");
            var goodsMap = goodsRows.ToDictionary(it => it.Id);
            var styleIds = goodsRows.Where(it => it.StyleItemId.HasValue)
                .Select(it => it.StyleItemId.Value).Distinct().ToList();
            var styleParents = styleIds.Count == 0 ? new Dictionary<long, long?>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == actor.TenantId && styleIds.Contains(it.Id))
                .ToList().ToDictionary(it => it.Id, it => it.ParentId);

            var member = dto.MemberId > 0
                ? Context.Queryable<JyMember>().First(it => it.Id == dto.MemberId.Value
                    && it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                : null;
            if (dto.MemberId > 0 && member == null) throw new CustomException("会员不存在或已停用");
            var memberLevel = member?.LevelId > 0
                ? Context.Queryable<JyMemberLevel>().First(it => it.Id == member.LevelId.Value
                    && it.TenantId == actor.TenantId && it.DelFlag == 0)
                : null;
            var discountRate = memberLevel?.DiscountRate ?? 1m;
            if (discountRate <= 0 || discountRate > 1) discountRate = 1m;
            var discountRules = LoadDiscountRules(actor, memberLevel?.Id);
            var metalPrices = new Dictionary<long, JyMetalPrice>();

            var calc = new SaleCalc
            {
                GoodsMap = goodsMap,
                Member = member,
                MemberLevelName = memberLevel?.LevelName,
                DiscountRate = discountRate
            };

            foreach (var edit in dto.Items)
            {
                var goods = goodsMap[edit.GoodsItemId];
                if (goods.Status == 2)
                {
                    ValidateLockedGoods(actor, dto, dto.MemberId, goods);
                }
                else if (goods.Status != 1)
                {
                    throw new CustomException($"商品 {goods.GoodsNo} 已不在库，不能销售");
                }
                if (edit.Qty <= 0) throw new CustomException($"商品 {goods.GoodsNo} 的数量必须大于 0");
                if (goods.ItemMode == 1 && edit.Qty != 1m)
                    throw new CustomException($"单件商品 {goods.GoodsNo} 的数量只能为 1");
                if (edit.Qty > goods.Qty) throw new CustomException($"商品 {goods.GoodsNo} 库存不足，当前可售 {goods.Qty}");
                if (edit.DealPrice < 0) throw new CustomException("成交价不能为负数");
                if (edit.Weight is < 0 or > 100000m)
                    throw new CustomException($"商品 {goods.GoodsNo} 的银重不正确");
                var unit = string.IsNullOrWhiteSpace(edit.Unit) ? goods.Unit : edit.Unit.Trim();
                if (!string.IsNullOrEmpty(unit) && unit.Length > 16)
                    throw new CustomException($"商品 {goods.GoodsNo} 的数量单位不能超过 16 个字");

                var isGift = dto.OrderType == 2 || edit.IsGift == 1;
                var weight = edit.Weight ?? goods.Weight;
                var pricingMode = edit.PreservePriceSnapshot && edit.PricingMode > 0
                    ? edit.PricingMode : (goods.PricingMode > 0 ? goods.PricingMode : 1);
                // 按克成交价是每克金价，不能用按件商品售价做上限；统一限制异常大金额即可。
                if (edit.DealPrice > 0 && edit.DealPrice > 1000000m)
                    throw new CustomException($"商品 {goods.GoodsNo} 的成交价明显异常");
                var laborFee = edit.PreservePriceSnapshot ? edit.LaborFee : goods.LaborFee;
                var laborFeeMode = edit.PreservePriceSnapshot && edit.LaborFeeMode > 0
                    ? edit.LaborFeeMode : (goods.LaborFeeMode > 0 ? goods.LaborFeeMode : 1);
                if (pricingMode == 2 && (!weight.HasValue || weight.Value <= 0))
                    throw new CustomException($"商品 {goods.GoodsNo} 为按克计价，必须先填写重量");
                var metalPrice = edit.PreservePriceSnapshot && edit.MetalPricePerGram > 0
                    ? null
                    : LoadCurrentMetalPrice(actor, goods, metalPrices);
                if (pricingMode == 2 && metalPrice == null && (!edit.PreservePriceSnapshot || edit.MetalPricePerGram <= 0))
                    throw new CustomException($"商品 {goods.GoodsNo} 的金属成色尚未维护有效金价");
                var unitPrice = isGift ? 0m : (edit.DealPrice > 0 ? edit.DealPrice
                    : (pricingMode == 2 ? (metalPrice?.PricePerGram ?? edit.MetalPricePerGram) : goods.SalePrice));
                var marketPricePerGram = edit.PreservePriceSnapshot
                    ? edit.MarketMetalPricePerGram
                    : (pricingMode == 2 && edit.MarketMetalPricePerGram > 0
                        ? edit.MarketMetalPricePerGram : (metalPrice?.PricePerGram ?? 0m));
                if (pricingMode == 2 && !edit.PreservePriceSnapshot && edit.DealPrice <= 0)
                    unitPrice = marketPricePerGram;
                var materialAmount = isGift ? 0m : (pricingMode == 2
                    ? decimal.Round(weight.Value * (unitPrice + laborFee), 2, MidpointRounding.AwayFromZero)
                    : decimal.Round(unitPrice * edit.Qty, 2, MidpointRounding.AwayFromZero));
                var effectiveLaborMode = pricingMode == 2 ? 2 : laborFeeMode;
                var laborAmount = isGift || pricingMode == 2 ? 0m : (effectiveLaborMode == 2
                    ? decimal.Round((weight ?? 0m) * laborFee, 2, MidpointRounding.AwayFromZero)
                    : decimal.Round(laborFee * edit.Qty, 2, MidpointRounding.AwayFromZero));
                var packagingFee = edit.PreservePriceSnapshot ? edit.PackagingFee : goods.PackagingFee;
                var packagingAmount = isGift ? 0m : decimal.Round(packagingFee * edit.Qty, 2, MidpointRounding.AwayFromZero);
                var stockQty = edit.StockQty ?? goods.Qty;
                if (stockQty <= 0 || stockQty > 100000m)
                    throw new CustomException($"商品 {goods.GoodsNo} 的内含数量必须大于 0 且不能过大");
                var listAmount = decimal.Round(materialAmount + laborAmount + packagingAmount, 2, MidpointRounding.AwayFromZero);
                styleParents.TryGetValue(goods.StyleItemId ?? 0, out var styleParentId);
                var rule = MatchDiscountRule(discountRules, goods.StyleItemId, styleParentId, pricingMode);
                // 未配置矩阵时沿用会员等级原有折扣，保证历史会员规则继续生效。
                var lineDiscountRate = rule?.DiscountRate ?? discountRate;
                if (lineDiscountRate <= 0 || lineDiscountRate > 1) lineDiscountRate = 1m;
                var discountBase = rule?.DiscountBase == 2 ? listAmount : materialAmount;
                var memberDiscountAmount = edit.PreservePriceSnapshot
                    ? edit.MemberDiscountAmount
                    : (dto.OrderType == 1 && member != null && dto.ApplyMemberDiscount && !isGift
                        ? decimal.Round(discountBase * (1m - lineDiscountRate), 2, MidpointRounding.AwayFromZero)
                        : 0m);
                calc.Items.Add(new JySaleItemDto
                {
                    GoodsItemId = goods.Id,
                    GoodsNo = goods.GoodsNo,
                    Barcode = goods.Barcode,
                    GoodsName = goods.StyleName,
                    StyleName = goods.StyleName,
                    MaterialName = goods.MaterialName,
                    MetalName = goods.MetalName,
                    Weight = weight,
                    Unit = unit,
                    Qty = edit.Qty,
                    StockQty = stockQty,
                    CostPrice = goods.CostPrice,
                    SalePrice = goods.SalePrice,
                    DealPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero),
                    PricingMode = pricingMode,
                    MetalPriceId = edit.PreservePriceSnapshot ? edit.MetalPriceId : metalPrice?.Id,
                    MarketMetalPricePerGram = marketPricePerGram,
                    MetalPricePerGram = edit.PreservePriceSnapshot
                        ? edit.MetalPricePerGram : (pricingMode == 2 ? unitPrice : (metalPrice?.PricePerGram ?? 0m)),
                    PriceSnapshotTime = edit.PreservePriceSnapshot ? edit.PriceSnapshotTime : DateTime.Now,
                    LaborFee = laborFee,
                    PackagingFee = packagingFee,
                    LaborFeeMode = effectiveLaborMode,
                    MaterialAmount = materialAmount,
                    LaborAmount = laborAmount,
                    PackagingAmount = packagingAmount,
                    ListAmount = listAmount,
                    MemberDiscountRuleId = rule?.Id,
                    MemberDiscountRate = lineDiscountRate,
                    MemberDiscountAmount = memberDiscountAmount,
                    PayAmount = edit.PreservePriceSnapshot
                        ? edit.PayAmount
                        : decimal.Round(Math.Max(0, listAmount - memberDiscountAmount), 2, MidpointRounding.AwayFromZero),
                    IsGift = isGift ? 1 : 0,
                    LineAmount = listAmount
                });
                calc.TotalAmount += listAmount;
                calc.MemberDiscount += memberDiscountAmount;
                calc.CostAmount += goods.CostPrice * edit.Qty;
                if (!forConfirm || goods.ItemMode != 1) calc.PieceQty += edit.Qty;
                else calc.PieceQty += 1;
            }

            calc.TotalAmount = decimal.Round(calc.TotalAmount, 2, MidpointRounding.AwayFromZero);
            calc.CostAmount = decimal.Round(calc.CostAmount, 2, MidpointRounding.AwayFromZero);
            calc.MemberDiscount = decimal.Round(calc.MemberDiscount, 2, MidpointRounding.AwayFromZero);
            var preserveSnapshot = dto.Items.All(it => it.PreservePriceSnapshot);
            var activityApplied = false;
            if (!preserveSnapshot && dto.OrderType == 1 && dto.ActivityId > 0)
            {
                var activity = LoadActivity(actor, dto.ActivityId.Value, member, calc.Items);
                if (activity != null)
                {
                    calc.Activity = activity;
                    calc.ActivityName = activity.ActivityName;
                    if (activity.ActivityType is 1 or 2 or 5)
                    {
                        var eligibleItems = calc.Items.Where(it => it.IsGift == 0
                            && ActivityCovers(activity, goodsMap[it.GoodsItemId])).ToList();
                        var eligible = eligibleItems.Sum(it => it.LineAmount);
                        var benefit = eligible <= 0 ? 0m : activity.ActivityType switch
                        {
                            1 or 5 => decimal.Round(eligible * (1m - NormalizeRate(activity.DiscountRate)), 2, MidpointRounding.AwayFromZero),
                            2 => Math.Min(eligible, activity.ReduceAmount),
                            _ => 0m
                        };
                        if (activity.BudgetAmount > 0)
                        {
                            var remaining = Math.Max(0, activity.BudgetAmount - activity.UsedAmount);
                            benefit = Math.Min(benefit, remaining);
                        }
                        benefit = RoundMoney(Math.Min(Math.Max(0, benefit), calc.TotalAmount));
                        // 会员折扣和活动优惠不叠加，按整单优惠金额取更高的一项。
                        if (benefit > calc.MemberDiscount)
                        {
                            calc.MemberDiscount = 0m;
                            foreach (var item in calc.Items)
                            {
                                item.MemberDiscountRuleId = null;
                                item.MemberDiscountRate = 1m;
                                item.MemberDiscountAmount = 0m;
                                item.PayAmount = item.ListAmount;
                            }
                            calc.ActivityBenefit = benefit;
                            AllocateLineActivity(eligibleItems, benefit);
                            activityApplied = true;
                        }
                        else
                        {
                            calc.ActivityBenefit = 0m;
                            calc.Activity = null;
                            calc.ActivityName = null;
                        }
                    }
                    if (activity.ActivityType == 3) calc.GiftName = activity.GiftName;
                    if (activity.ActivityType == 4) calc.PointsMultiple = activity.PointsMultiple <= 0 ? 1 : activity.PointsMultiple;
                }
            }

            calc.BasePayAmount = RoundMoney(Math.Max(0, calc.TotalAmount - calc.MemberDiscount - calc.ActivityBenefit));
            // 活动未胜出（或没有活动）时，恢复“行标价 - 行会员折扣”的行实付。
            if (!preserveSnapshot && !activityApplied) AllocateLineActivity(calc.Items, 0m);
            calc.AdjustAmount = decimal.Round(dto.AdjustAmount, 2, MidpointRounding.AwayFromZero);
            calc.RoundOffAmount = decimal.Round(dto.RoundOffAmount, 2, MidpointRounding.AwayFromZero);
            if (calc.RoundOffAmount > calc.BasePayAmount + calc.AdjustAmount)
                throw new CustomException("抹零金额不能大于应收金额");
            if (dto.CustomOffsets != null && dto.CustomOffsets.Where(it => it != null).Sum(it => it.OffsetAmount)
                > calc.BasePayAmount + calc.AdjustAmount - calc.RoundOffAmount + 0.009m)
                throw new CustomException("定金对冲不能超过本次实收金额");
            // 成交价合计 → 会员等级折扣 → 抹零。应付基准是折扣后的金额，
            // 抹零只从本次应收中扣减，不能反过来影响商品金额或折扣金额。
            var rawPayAmount = decimal.Round(calc.BasePayAmount + calc.AdjustAmount - calc.RoundOffAmount, 2, MidpointRounding.AwayFromZero);
            if (rawPayAmount < 0) throw new CustomException("调整后的实付金额不能为负数");
            calc.PayAmount = rawPayAmount;
            if (!preserveSnapshot) AllocateLinePayments(calc.Items, calc.PayAmount);
            if (member != null) calc.PointsEarned = CalculatePoints(actor, calc.PayAmount, calc.PointsMultiple);
            return calc;
        }

        /// <summary>读取当前会员等级可用的折扣矩阵；匹配优先级为二级分类、一级分类、全部。</summary>
        private List<JyMemberDiscountRule> LoadDiscountRules(JyActor actor, long? memberLevelId)
        {
            if (!memberLevelId.HasValue || memberLevelId.Value <= 0) return new List<JyMemberDiscountRule>();
            var now = DateTime.Now;
            return Context.Queryable<JyMemberDiscountRule>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0
                    && it.MemberLevelId == memberLevelId.Value
                    && (it.EffectiveTime == null || it.EffectiveTime <= now)
                    && (it.ExpireTime == null || it.ExpireTime > now))
                .ToList();
        }

        private static JyMemberDiscountRule MatchDiscountRule(List<JyMemberDiscountRule> rules, long? styleItemId,
            long? styleParentId, int pricingMode)
        {
            if (rules == null || rules.Count == 0) return null;
            return rules
                .Where(it => !it.PricingMode.HasValue || it.PricingMode.Value == pricingMode)
                .Select(it => new
                {
                    Rule = it,
                    StyleRank = !it.StyleItemId.HasValue ? 0
                        : it.StyleItemId == styleItemId ? 2
                        : it.StyleItemId == styleParentId ? 1 : -1
                })
                .Where(it => it.StyleRank >= 0)
                .OrderByDescending(it => it.StyleRank)
                .ThenByDescending(it => it.Rule.PricingMode.HasValue ? 1 : 0)
                .ThenByDescending(it => it.Rule.Priority)
                .ThenByDescending(it => it.Rule.Id)
                .Select(it => it.Rule)
                .FirstOrDefault();
        }

        /// <summary>把订单级活动优惠按行折后金额分摊，便于退货按原行实付计算。</summary>
        private static void AllocateLineActivity(List<JySaleItemDto> items, decimal totalBenefit)
        {
            if (items == null || items.Count == 0) return;
            foreach (var item in items)
            {
                item.ActivityBenefit = 0m;
                item.PayAmount = RoundMoney(Math.Max(0, item.ListAmount - item.MemberDiscountAmount));
            }
            totalBenefit = RoundMoney(Math.Max(0, totalBenefit));
            if (totalBenefit <= 0) return;
            var basis = items.Sum(it => it.PayAmount);
            if (basis <= 0) return;
            var allocated = 0m;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var amount = i == items.Count - 1
                    ? RoundMoney(totalBenefit - allocated)
                    : RoundMoney(totalBenefit * item.PayAmount / basis);
                amount = Math.Min(amount, item.PayAmount);
                item.ActivityBenefit = amount;
                item.PayAmount = RoundMoney(item.PayAmount - amount);
                allocated = RoundMoney(allocated + amount);
            }
        }

        /// <summary>把订单调价和抹零按行折后金额分摊，保证各行实付之和等于订单实付。</summary>
        private static void AllocateLinePayments(List<JySaleItemDto> items, decimal orderPayAmount)
        {
            if (items == null || items.Count == 0) return;
            var basis = items.Sum(it => it.PayAmount);
            if (basis <= 0) return;
            orderPayAmount = RoundMoney(Math.Max(0, orderPayAmount));
            var allocated = 0m;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var amount = i == items.Count - 1
                    ? RoundMoney(orderPayAmount - allocated)
                    : RoundMoney(orderPayAmount * item.PayAmount / basis);
                item.PayAmount = Math.Max(0, amount);
                allocated = RoundMoney(allocated + item.PayAmount);
            }
        }

        /// <summary>确认草稿后把最终优惠、活动和行实付回写到销售明细，退货按这份快照结算。</summary>
        private void WriteLineSnapshots(JyActor actor, List<JySaleOrderItem> storedItems,
            List<JySaleItemDto> calculatedItems, DateTime now)
        {
            var calcMap = calculatedItems.ToDictionary(it => it.GoodsItemId);
            foreach (var stored in storedItems)
            {
                if (!calcMap.TryGetValue(stored.GoodsItemId, out var calc)) continue;
                Context.Updateable<JySaleOrderItem>()
                    .SetColumns(it => it.MaterialAmount == calc.MaterialAmount)
                    .SetColumns(it => it.LaborAmount == calc.LaborAmount)
                    .SetColumns(it => it.PackagingFee == calc.PackagingFee)
                    .SetColumns(it => it.PackagingAmount == calc.PackagingAmount)
                    .SetColumns(it => it.StockQty == calc.StockQty)
                    .SetColumns(it => it.ListAmount == calc.ListAmount)
                    .SetColumns(it => it.MemberDiscountRuleId == calc.MemberDiscountRuleId)
                    .SetColumns(it => it.MemberDiscountRate == calc.MemberDiscountRate)
                    .SetColumns(it => it.MemberDiscountAmount == calc.MemberDiscountAmount)
                    .SetColumns(it => it.ActivityBenefit == calc.ActivityBenefit)
                    .SetColumns(it => it.PayAmount == calc.PayAmount)
                    .Where(it => it.Id == stored.Id && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
            }
        }

        private JyMetalPrice LoadCurrentMetalPrice(JyActor actor, JyGoodsItem goods, Dictionary<long, JyMetalPrice> cache)
        {
            if (!goods.MetalItemId.HasValue || goods.MetalItemId.Value <= 0) return null;
            if (cache.TryGetValue(goods.MetalItemId.Value, out var cached)) return cached;
            var now = DateTime.Now;
            var price = Context.Queryable<JyMetalPrice>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0
                    && it.MetalItemId == goods.MetalItemId.Value
                    && it.EffectiveTime <= now
                    && (it.ExpireTime == null || it.ExpireTime > now))
                .OrderBy(it => it.EffectiveTime, OrderByType.Desc)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .First();
            cache[goods.MetalItemId.Value] = price;
            return price;
        }

        private JyActivity LoadActivity(JyActor actor, long activityId, JyMember member, List<JySaleItemDto> items)
        {
            var now = DateTime.Now;
            var activity = Context.Queryable<JyActivity>().First(it => it.Id == activityId
                && it.TenantId == actor.TenantId && it.DelFlag == 0);
            if (activity == null) throw new CustomException("活动不存在");
            if (activity.Status != 1) throw new CustomException("活动未启用");
            if (activity.StartTime > now || activity.EndTime < now) throw new CustomException("活动不在有效期内");
            if (activity.MemberLevelId.HasValue)
            {
                if (member == null || member.LevelId != activity.MemberLevelId)
                    throw new CustomException("当前会员等级不满足活动参与条件");
            }
            if (activity.JoinLimit > 0)
            {
                if (member == null) throw new CustomException("该活动仅限会员参与");
                var joined = Context.Queryable<JyActivityMember>()
                    .Count(it => it.TenantId == actor.TenantId && it.ActivityId == activity.Id && it.MemberId == member.Id);
                if (joined >= activity.JoinLimit) throw new CustomException("该会员已达到此活动的参与次数上限");
            }
            if (activity.BudgetAmount > 0 && activity.UsedAmount >= activity.BudgetAmount)
                throw new CustomException("活动预算已用完");
            var hasEligible = activity.ScopeType == 1 || items.Any(it =>
                it.GoodsItemId > 0 && Context.Queryable<JyGoodsItem>()
                    .Any(g => g.Id == it.GoodsItemId && g.StyleItemId == activity.StyleItemId));
            if (!hasEligible) throw new CustomException("所选商品不在活动范围内");
            return activity;
        }

        private static bool ActivityCovers(JyActivity activity, JyGoodsItem goods)
            => activity.ScopeType == 1 || (activity.StyleItemId.HasValue && goods.StyleItemId == activity.StyleItemId);

        private static decimal NormalizeRate(decimal? rate)
            => rate is > 0 and <= 1 ? rate.Value : 1m;

        private void ApplyMemberConsumption(JyActor actor, JyMember member, JySaleOrder sale, decimal pointsEarned, DateTime now)
        {
            member = Context.Queryable<JyMember>()
                .With(SqlWith.UpdLock)
                .First(it => it.Id == member.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("会员信息不存在，请刷新后重试");
            var newTotalAmount = member.TotalAmount + sale.PayAmount;
            var newYearAmount = member.YearAmount + sale.PayAmount;
            var newOrderCount = member.OrderCount + 1;
            var newPoints = member.PointsBalance + pointsEarned;
            var newYearPoints = member.YearPoints + pointsEarned;
            var newLevelId = ResolveUpgradedLevel(actor, member, newYearPoints);

            var affected = Context.Updateable<JyMember>()
                .SetColumns(it => it.TotalAmount == newTotalAmount)
                .SetColumns(it => it.YearAmount == newYearAmount)
                .SetColumns(it => it.OrderCount == newOrderCount)
                .SetColumns(it => it.AvgTicket == (newTotalAmount / newOrderCount))
                .SetColumns(it => it.PointsBalance == newPoints)
                .SetColumns(it => it.YearPoints == newYearPoints)
                .SetColumns(it => it.LastConsumeTime == now)
                .SetColumnsIF(newLevelId.HasValue, it => it.LevelId == newLevelId.Value)
                .SetColumnsIF(newLevelId.HasValue, it => it.LevelChangedTime == now)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == member.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
            if (affected != 1) throw new CustomException("会员信息已变化，请刷新后重试");

            Context.Insertable(new JyPointLedger
            {
                TenantId = actor.TenantId,
                MemberId = member.Id,
                ChangeType = 1,
                ChangePoints = pointsEarned,
                BalanceAfter = newPoints,
                YearPointsAfter = newYearPoints,
                SaleOrderId = sale.Id,
                Reason = "销售单 " + sale.OrderNo,
                BizTime = now,
                Create_time = now
            }).ExecuteCommand();
        }

        private long? ResolveUpgradedLevel(JyActor actor, JyMember member, decimal yearPoints)
        {
            var levels = Context.Queryable<JyMemberLevel>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0)
                .ToList();
            var match = levels.Where(it => it.MinPoints <= yearPoints && (!it.MaxPoints.HasValue || yearPoints <= it.MaxPoints.Value))
                .OrderByDescending(it => it.SortNo).FirstOrDefault();
            if (match == null) return null;
            var current = member.LevelId.HasValue ? levels.FirstOrDefault(it => it.Id == member.LevelId.Value) : null;
            return current == null || match.SortNo > current.SortNo ? match.Id : null;
        }

        private JyStockItemDto ToStockItem(JyGoodsItem it)
        {
            return new JyStockItemDto
            {
                Id = it.Id,
                GoodsNo = it.GoodsNo,
                Barcode = it.Barcode,
                StyleId = it.StyleId,
                StyleName = it.StyleName,
                StyleItemName = it.StyleItemName,
                MaterialGroup = it.MaterialGroup,
                MaterialName = it.MaterialName,
                MetalName = it.MetalName,
                ItemMode = it.ItemMode,
                Qty = it.Qty,
                LockedQty = it.LockedQty,
                LockBizType = it.LockBizType,
                LockBizId = it.LockBizId,
                LockText = it.Status == 2 ? (it.LockBizType == "custom" ? "定制定金锁定" : "已锁定") : null,
                Weight = it.Weight,
                Unit = it.Unit,
                CostPrice = it.CostPrice,
                SalePrice = it.SalePrice,
                PricingMode = it.PricingMode,
                LaborFee = it.LaborFee,
                PackagingFee = it.PackagingFee,
                LaborFeeMode = it.LaborFeeMode,
                Status = it.Status,
                CreateTime = it.Create_time
            };
        }

        private List<JySaleDto> ToDtos(string tenantId, List<JySaleOrder> rows)
        {
            if (rows.Count == 0) return new List<JySaleDto>();
            var memberIds = rows.Where(it => it.MemberId.HasValue).Select(it => it.MemberId.Value).Distinct().ToList();
            var activityIds = rows.Where(it => it.ActivityId.HasValue).Select(it => it.ActivityId.Value).Distinct().ToList();
            var members = memberIds.Count == 0 ? new Dictionary<long, JyMember>() : Context.Queryable<JyMember>()
                .Where(it => it.TenantId == tenantId && memberIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            var activities = activityIds.Count == 0 ? new Dictionary<long, JyActivity>() : Context.Queryable<JyActivity>()
                .Where(it => it.TenantId == tenantId && activityIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            var referrerIds = rows.Where(it => it.ReferrerMemberId.HasValue)
                .Select(it => it.ReferrerMemberId.Value)
                .Concat(members.Values.Where(it => it.ReferrerMemberId.HasValue).Select(it => it.ReferrerMemberId.Value))
                .Distinct().ToList();
            var referrerNames = referrerIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JyMember>()
                .Where(it => it.TenantId == tenantId && referrerIds.Contains(it.Id))
                .Select(it => new { it.Id, it.MemberName }).ToList().ToDictionary(it => it.Id, it => it.MemberName);
            return rows.Select(it =>
            {
                members.TryGetValue(it.MemberId ?? 0, out var member);
                activities.TryGetValue(it.ActivityId ?? 0, out var activity);
                // 单据只展示本单明确选择的推荐人，不把会员档案的历史推荐关系回填到单据。
                var referrerId = it.ReferrerMemberId;
                return new JySaleDto
                {
                    Id = it.Id,
                    OrderNo = it.OrderNo,
                    OrderType = it.OrderType,
                    MemberId = it.MemberId,
                    MemberName = member?.MemberName,
                    MemberNo = member?.MemberNo,
                    ActivityId = it.ActivityId,
                    ActivityName = activity?.ActivityName,
                    Status = it.Status,
                    TotalAmount = it.TotalAmount,
                    DiscountAmount = it.DiscountAmount,
                    ApplyMemberDiscount = it.DiscountAmount > 0,
                    ActivityBenefit = it.ActivityBenefit,
                    BasePayAmount = it.BasePayAmount,
                    AdjustAmount = it.AdjustAmount,
                    RoundOffAmount = it.RoundOffAmount,
                    PayAmount = it.PayAmount,
                    CustomOffsetAmount = it.CustomOffsetAmount,
                    CashAmount = it.CashAmount == 0 && it.CustomOffsetAmount == 0 ? it.PayAmount : it.CashAmount,
                    CostAmount = it.CostAmount,
                    PieceQty = it.PieceQty,
                    CustomerName = it.CustomerName ?? member?.MemberName,
                    CustomerPhone = it.CustomerPhone ?? member?.Phone,
                    CustomerWechatNo = it.CustomerWechatNo ?? member?.WechatNo,
                    ReferrerWechatNo = it.ReferrerWechatNo,
                    ReferrerMemberId = referrerId,
                    ReferrerName = referrerId.HasValue && referrerNames.TryGetValue(referrerId.Value, out var referrerName)
                        ? referrerName : null,
                    CustomerSex = it.CustomerName != null ? it.CustomerSex : (member?.Sex ?? 0),
                    CustomerBirthday = it.CustomerBirthday ?? member?.SolarBirthday,
                    CustomerBirthYear = it.CustomerBirthday.HasValue ? it.CustomerBirthYear : member?.BirthYear,
                    CustomerBirthdayText = BirthdayText(it.CustomerBirthday ?? member?.SolarBirthday,
                        it.CustomerBirthday.HasValue ? it.CustomerBirthYear : member?.BirthYear),
                    ReturnedAmount = it.ReturnedAmount,
                    ReturnStatus = it.ReturnStatus,
                    DealTime = it.DealTime,
                    Remark = it.Remark,
                    CreateBy = it.Create_by,
                    CreateTime = it.Create_time
                };
            }).ToList();
        }

        private List<JySaleItemDto> LoadItems(string tenantId, IEnumerable<long> orderIds)
        {
            var ids = orderIds.Distinct().ToList();
            if (ids.Count == 0) return new List<JySaleItemDto>();
            var rows = Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == tenantId && ids.Contains(it.OrderId))
                .OrderBy(it => it.SortNo).ToList();
            // 旧销售单没有单位快照，回查当前货品单位仅用于展示；银重仍以成交快照为准。
            var goodsIds = rows.Select(it => it.GoodsItemId).Distinct().ToList();
            var goodsRows = goodsIds.Count == 0 ? new List<JyGoodsItem>() : Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == tenantId && goodsIds.Contains(it.Id))
                .ToList();
            var goodsUnits = goodsRows.ToDictionary(it => it.Id, it => it.Unit);
            var inboundLineIds = goodsRows.Where(it => it.InboundLineId.HasValue)
                .Select(it => it.InboundLineId.Value).Distinct().ToList();
            var inboundQty = inboundLineIds.Count == 0 ? new Dictionary<long, decimal>() : Context.Queryable<JyInboundLine>()
                .Where(it => it.TenantId == tenantId && inboundLineIds.Contains(it.Id))
                .ToList().ToDictionary(it => it.Id, it => it.Qty);
            var legacyStockQty = goodsRows.ToDictionary(it => it.Id, it =>
                it.InboundLineId.HasValue && inboundQty.TryGetValue(it.InboundLineId.Value, out var qty) ? (decimal?)qty : null);
            var styleIds = rows.Where(it => it.StyleItemId.HasValue).Select(it => it.StyleItemId.Value).Distinct().ToList();
            var styles = styleIds.Count == 0 ? new Dictionary<long, JyLookupItem>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && styleIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            var parentIds = styles.Values.Where(it => it.ParentId.HasValue).Select(it => it.ParentId.Value).Distinct().ToList();
            var parents = parentIds.Count == 0 ? new Dictionary<long, JyLookupItem>() : Context.Queryable<JyLookupItem>()
                .Where(it => it.TenantId == tenantId && parentIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id);
            return rows
                .Select(it => new JySaleItemDto
                {
                    Id = it.Id,
                    SortNo = it.SortNo,
                    GoodsItemId = it.GoodsItemId,
                    GoodsNo = it.GoodsNo,
                    Barcode = it.Barcode,
                    GoodsName = it.GoodsName,
                    StyleName = it.StyleName,
                    StyleItemId = it.StyleItemId,
                    StyleItemName = it.StyleItemName,
                    StyleCategoryName = StyleCategoryName(it.StyleItemId, styles, parents),
                    MaterialGroup = it.MaterialGroup,
                    MaterialName = it.MaterialName,
                    MetalName = it.MetalName,
                    Weight = it.Weight,
                    Unit = string.IsNullOrWhiteSpace(it.Unit) && goodsUnits.TryGetValue(it.GoodsItemId, out var unit)
                        ? unit : it.Unit,
                    Qty = it.Qty,
                    StockQty = it.StockQty ?? (legacyStockQty.TryGetValue(it.GoodsItemId, out var stockQty) ? stockQty : null),
                    CostPrice = it.CostPrice,
                    SalePrice = it.SalePrice,
                    DealPrice = it.DealPrice,
                    PricingMode = it.PricingMode,
                    MetalPriceId = it.MetalPriceId,
                    MarketMetalPricePerGram = it.MarketMetalPricePerGram,
                    MetalPricePerGram = it.MetalPricePerGram,
                    PriceSnapshotTime = it.PriceSnapshotTime,
                    LaborFee = it.LaborFee,
                    PackagingFee = it.PackagingFee,
                    LaborFeeMode = it.LaborFeeMode,
                    PackagingAmount = it.PackagingAmount,
                    MaterialAmount = it.MaterialAmount,
                    LaborAmount = it.LaborAmount,
                    ListAmount = it.ListAmount,
                    MemberDiscountRuleId = it.MemberDiscountRuleId,
                    MemberDiscountRate = it.MemberDiscountRate,
                    MemberDiscountAmount = it.MemberDiscountAmount,
                    ActivityBenefit = it.ActivityBenefit,
                    PayAmount = it.PayAmount,
                    // 旧销售单没有行标价快照时回退到成交价 × 数量。
                    LineAmount = it.ListAmount > 0 ? it.ListAmount
                        : decimal.Round(it.DealPrice * it.Qty, 2, MidpointRounding.AwayFromZero),
                    IsGift = it.IsGift,
                    ReturnedQty = it.ReturnedQty,
                    ReturnableQty = Math.Max(0, it.Qty - it.ReturnedQty)
                }).ToList();
        }

        private static string StyleCategoryName(long? styleItemId, Dictionary<long, JyLookupItem> styles,
            Dictionary<long, JyLookupItem> parents)
        {
            if (!styleItemId.HasValue || !styles.TryGetValue(styleItemId.Value, out var style)) return null;
            if (style.ParentId.HasValue && parents.TryGetValue(style.ParentId.Value, out var parent))
                return parent.ItemName + " / " + style.ItemName;
            return style.ItemName;
        }

        private JySaleOrder Must(string tenantId, long id)
        {
            return Context.Queryable<JySaleOrder>()
                .First(it => it.Id == id && it.TenantId == tenantId && it.DelFlag == 0)
                ?? throw new CustomException("销售单不存在");
        }

        /// <summary>生日展示：年份未知时仅显示月日。</summary>
        private static string BirthdayText(DateTime? birthday, int? birthYear)
        {
            if (!birthday.HasValue) return null;
            var yearKnown = birthYear.HasValue && birthYear.Value > 1900;
            return yearKnown ? birthday.Value.ToString("yyyy-MM-dd") : birthday.Value.ToString("MM-dd");
        }

        /// <summary>查询该客户当前被定制定金锁定的商品 Id 集合。</summary>
        private List<long> FindCustomerLockedGoodsIds(JyActor actor, long? memberId, string phone)
        {
            var q = Context.Queryable<JyCustomOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0
                    && (it.Status == 1 || it.Status == 2));
            if (memberId.HasValue && memberId.Value > 0)
            {
                var mid = memberId.Value;
                q = q.Where(it => it.MemberId == mid);
            }
            else if (!string.IsNullOrEmpty(phone))
            {
                var p = phone;
                q = q.Where(it => it.CustomerPhone == p);
            }
            else
            {
                return new List<long>();
            }
            var orderIds = q.ToList().Select(it => it.Id).Distinct().ToList();
            if (orderIds.Count == 0) return new List<long>();
            return Context.Queryable<JyCustomItem>()
                .Where(it => it.TenantId == actor.TenantId && orderIds.Contains(it.CustomOrderId)
                    && it.DelFlag == 0 && it.GoodsItemId != null
                    && (it.Status == 1 || it.Status == 2) && it.RemainAmount > 0)
                .ToList().Where(it => it.GoodsItemId.HasValue)
                .Select(it => it.GoodsItemId.Value).Distinct().ToList();
        }

        /// <summary>校验被定制定金锁定的商品是否允许本单销售。</summary>
        private void ValidateLockedGoods(JyActor actor, JySaleEditDto dto, long? memberId, JyGoodsItem goods)
        {
            if (goods.LockBizType != "custom" || !goods.LockBizId.HasValue)
                throw new CustomException($"商品 {goods.GoodsNo} 已被锁定，不能销售");
            var item = Context.Queryable<JyCustomItem>()
                .First(it => it.Id == goods.LockBizId.Value && it.TenantId == actor.TenantId && it.DelFlag == 0);
            if (item == null)
                throw new CustomException($"商品 {goods.GoodsNo} 的定制定金商品明细不存在，请执行定金明细数据回填");
            var order = Context.Queryable<JyCustomOrder>()
                .First(it => it.Id == item.CustomOrderId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException($"商品 {goods.GoodsNo} 的定制定金单不存在，请核对");
            if (order.Status is not (1 or 2) || item.Status is not (1 or 2))
                throw new CustomException($"商品 {goods.GoodsNo} 的定制定金单已失效，不能销售");
            if (item.RemainAmount <= 0) throw new CustomException($"商品 {goods.GoodsNo} 的定制定金已冲抵完毕，请核对");
            if (item.GoodsItemId != goods.Id)
                throw new CustomException($"商品 {goods.GoodsNo} 与定制定金明细绑定货品不一致");
            if (order.MemberId.HasValue && order.MemberId.Value > 0)
            {
                if (memberId != order.MemberId)
                    throw new CustomException($"商品 {goods.GoodsNo} 的定制定金不属于当前客户");
            }
            else
            {
                var phone = NormalizePhone(dto.CustomerPhone);
                if (string.IsNullOrEmpty(phone) || order.CustomerPhone != phone)
                    throw new CustomException($"商品 {goods.GoodsNo} 的定制定金与当前客户不匹配");
            }
        }

        /// <summary>确认销售时，为已绑定本次商品的定制定金自动生成冲抵计划；手工计划优先。</summary>
        private void EnsureAutoOffsets(JyActor actor, JySaleOrder header, SaleCalc calc, DateTime now)
        {
            var hasPlan = Context.Queryable<JySaleCustomOffset>()
                .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == header.Id && it.Status == 0)
                .Any();
            if (hasPlan) return;
            var customItemIds = calc.GoodsMap.Values
                .Where(it => it.Status == 2 && it.LockBizType == "custom" && it.LockBizId.HasValue)
                .Select(it => it.LockBizId.Value).Distinct().ToList();
            if (customItemIds.Count == 0) return;
            var items = Context.Queryable<JyCustomItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && customItemIds.Contains(it.Id))
                .OrderBy(it => it.Id).ToList();
            if (items.Count == 0) return;
            var orderIds = items.Select(it => it.CustomOrderId).Distinct().ToList();
            var orders = Context.Queryable<JyCustomOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && orderIds.Contains(it.Id))
                .ToList().ToDictionary(it => it.Id);
            var room = RoundMoney(header.PayAmount);
            var plans = new List<JySaleCustomOffset>();
            foreach (var item in items)
            {
                if (room <= 0) break;
                if (!orders.TryGetValue(item.CustomOrderId, out var order)) continue;
                if (order.Status is not (1 or 2) || item.Status is not (1 or 2) || item.RemainAmount <= 0) continue;
                if (header.MemberId.HasValue && order.MemberId.HasValue && header.MemberId != order.MemberId) continue;
                var reserved = Context.Queryable<JySaleCustomOffset>()
                    .Where(it => it.TenantId == actor.TenantId && it.CustomItemId == item.Id
                        && it.Status == 0 && it.SaleOrderId != header.Id)
                    .Sum(it => it.OffsetAmount);
                var available = RoundMoney(item.RemainAmount - reserved);
                if (available <= 0) continue;
                var amount = RoundMoney(Math.Min(available, room));
                if (amount <= 0) continue;
                room = RoundMoney(room - amount);
                plans.Add(new JySaleCustomOffset
                {
                    TenantId = actor.TenantId,
                    SaleOrderId = header.Id,
                    CustomOrderId = order.Id,
                    CustomItemId = item.Id,
                    CustomNo = order.CustomNo,
                    OffsetAmount = amount,
                    Status = 0,
                    Create_time = now
                });
            }
            if (plans.Count > 0) Context.Insertable(plans).ExecuteCommand();
        }

        /// <summary>商品恢复到在库或定制定金锁定状态。</summary>
        public (int Status, decimal LockedQty, string LockBizType, long? LockBizId) ResolveGoodsRestore(JyActor actor, long goodsItemId)
        {
            var item = Context.Queryable<JyCustomItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.GoodsItemId == goodsItemId
                    && (it.Status == 1 || it.Status == 2) && it.RemainAmount > 0)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .First();
            if (item != null) return (2, 1m, "custom", item.Id);
            return (1, 0m, null, null);
        }

        private static string LedgerNo(string goodsNo, string suffix)
        {
            var value = goodsNo + "-" + suffix + "-" + DateTime.Now.ToString("HHmmssfff");
            return value.Length <= 32 ? value : value.Substring(0, 32);
        }

        private decimal CalculatePoints(JyActor actor, decimal payAmount, decimal activityMultiple)
        {
            if (payAmount <= 0) return 0;
            var rule = Context.Queryable<JyPointRule>()
                .Where(it => it.TenantId == actor.TenantId && it.Status == 0)
                .OrderBy(it => it.Id, OrderByType.Desc).First();
            var baseAmount = rule == null || rule.AmountBase <= 0 ? 1m : rule.AmountBase;
            var awarded = rule == null || rule.PointsAwarded < 0 ? 1m : rule.PointsAwarded;
            var multiple = activityMultiple <= 0 ? 1m : activityMultiple;
            return Math.Floor(payAmount / baseAmount * awarded * multiple);
        }

        /// <summary>微信号允许字母、数字、下划线、中横线、点，最长 64 位。空值返回 null。</summary>
        private static string NormalizeWechat(string wechat)
        {
            wechat = wechat?.Trim();
            if (string.IsNullOrEmpty(wechat)) return null;
            if (wechat.Length > 64) throw new CustomException("微信号不能超过 64 个字符");
            foreach (var ch in wechat)
            {
                if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '-' && ch != '.')
                    throw new CustomException("微信号只能包含字母、数字、下划线、中横线或点");
            }
            return wechat;
        }

        /// <summary>按推荐人微信号解析会员 Id；找不到直接报错，避免推荐关系静默丢失。</summary>
        private long? ResolveReferrerForSale(JyActor actor, string referrerWechatNo)
        {
            var wechat = NormalizeWechat(referrerWechatNo);
            if (string.IsNullOrEmpty(wechat)) return null;
            var matches = Context.Queryable<JyMember>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 0 && it.WechatNo == wechat)
                .Select(it => it.Id).ToList();
            if (matches.Count == 0)
                throw new CustomException($"未找到微信号为 {wechat} 的推荐人，请先在会员档案中维护该客户微信号");
            if (matches.Count > 1)
                throw new CustomException($"微信号 {wechat} 对应多个会员，请先修正会员档案中的重复微信号");
            return matches[0];
        }

        /// <summary>校验本单推荐人：必须是有效会员，且不能是买家本人。</summary>
        private void EnsureReferrer(JyActor actor, long? memberId, long referrerId)
        {
            if (referrerId <= 0) return;
            var referrer = Context.Queryable<JyMember>()
                .First(it => it.Id == referrerId && it.TenantId == actor.TenantId
                    && it.DelFlag == 0 && it.Status == 0)
                ?? throw new CustomException("所选推荐人不存在或已停用");
            var buyerId = memberId.GetValueOrDefault();
            if (buyerId > 0 && referrer.Id == buyerId)
                throw new CustomException("推荐人不能是客户本人");
        }

        /// <summary>
        /// 解析本单唯一推荐人：优先会员 Id，其次微信号；新客户不传时可为空。
        /// 已选推荐人会自动补入客户的多推荐人关系（同人只留一条）。
        /// </summary>
        private JyMember ResolveSaleReferrer(JyActor actor, long? memberId, long? referrerId,
            string referrerWechatNo, long? saleOrderId, DateTime? now)
        {
            JyMember referrer = null;
            if (referrerId > 0)
            {
                referrer = Context.Queryable<JyMember>()
                    .First(it => it.Id == referrerId.Value && it.TenantId == actor.TenantId
                        && it.DelFlag == 0 && it.Status == 0)
                    ?? throw new CustomException("所选推荐人不存在或已停用");
            }
            else
            {
                var id = ResolveReferrerForSale(actor, referrerWechatNo);
                if (id.HasValue)
                    referrer = Context.Queryable<JyMember>()
                        .First(it => it.Id == id.Value && it.TenantId == actor.TenantId && it.DelFlag == 0);
            }
            if (referrer == null) return null;
            if (memberId.HasValue && memberId.Value == referrer.Id)
                throw new CustomException("推荐人不能是客户本人");
            if (memberId.HasValue && saleOrderId.HasValue)
                BindReferrer(actor, memberId.Value, referrer.Id, 2, saleOrderId, now ?? DateTime.Now);
            return referrer;
        }

        /// <summary>销售单绑定推荐关系；同一客户同一推荐人只保留一条，不覆盖历史。</summary>
        private void BindReferrer(JyActor actor, long memberId, long referrerId, int source,
            long? saleOrderId, DateTime now)
        {
            var active = Context.Queryable<JyMemberReferrer>()
                .Where(it => it.TenantId == actor.TenantId && it.MemberId == memberId
                    && it.DelFlag == 0)
                .ToList();
            var existing = Context.Queryable<JyMemberReferrer>()
                .First(it => it.TenantId == actor.TenantId && it.MemberId == memberId
                    && it.ReferrerMemberId == referrerId);
            // 已隐藏的关系被新单重新启用时，如果客户已有默认推荐人，不能产生第二个默认。
            var isPrimary = existing != null && existing.DelFlag == 0
                ? (existing.IsPrimary == 1 || active.Count == 0 ? 1 : 0)
                : (active.Count == 0 ? 1 : 0);
            var referrer = Context.Queryable<JyMember>()
                .First(it => it.Id == referrerId && it.TenantId == actor.TenantId);
            if (existing != null)
            {
                Context.Updateable<JyMemberReferrer>()
                    .SetColumns(it => it.DelFlag == 0)
                    .SetColumns(it => it.IsPrimary == isPrimary)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == existing.Id && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
                if (isPrimary == 1 && referrer != null)
                {
                    Context.Updateable<JyMember>()
                        .SetColumns(it => it.ReferrerMemberId == referrerId)
                        .SetColumns(it => it.ReferrerWechatNo == referrer.WechatNo)
                        .SetColumns(it => it.Update_by == actor.UserName)
                        .SetColumns(it => it.Update_time == now)
                        .Where(it => it.Id == memberId && it.TenantId == actor.TenantId)
                        .ExecuteCommand();
                }
                return;
            }
            Context.Insertable(new JyMemberReferrer
            {
                TenantId = actor.TenantId,
                MemberId = memberId,
                ReferrerMemberId = referrerId,
                Source = source,
                SaleOrderId = saleOrderId,
                IsPrimary = isPrimary,
                DelFlag = 0,
                Create_by = actor.UserName,
                Create_time = now,
                Remark = source == 2 ? "销售开单补录推荐关系" : null
            }).ExecuteCommand();
            if (isPrimary == 1 && referrer != null)
            {
                Context.Updateable<JyMember>()
                    .SetColumns(it => it.ReferrerMemberId == referrerId)
                    .SetColumns(it => it.ReferrerWechatNo == referrer.WechatNo)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == memberId && it.TenantId == actor.TenantId)
                    .ExecuteCommand();
            }
        }
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


        private void ReplaceDraftOffsets(JyActor actor, JySaleOrder header, JySaleEditDto dto, DateTime now)
        {
            Context.Deleteable<JySaleCustomOffset>()
                .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == header.Id && it.Status == 0)
                .ExecuteCommand();
            var plans = NormalizeOffsetPlans(actor, header, dto?.CustomOffsets, header.PayAmount, now);
            if (plans.Count > 0) Context.Insertable(plans).ExecuteCommand();
            var total = RoundMoney(plans.Sum(it => it.OffsetAmount));
            header.CustomOffsetAmount = total;
            header.CashAmount = RoundMoney(header.PayAmount - total);
            var affected = Context.Updateable<JySaleOrder>()
                .SetColumns(it => it.CustomOffsetAmount == header.CustomOffsetAmount)
                .SetColumns(it => it.CashAmount == header.CashAmount)
                .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId && it.Status == 0)
                .ExecuteCommand();
            if (affected != 1) throw new CustomException("销售单状态已变化，请刷新后重试");
        }

        private List<JySaleCustomOffset> NormalizeOffsetPlans(JyActor actor, JySaleOrder header, List<JyCustomOffsetEditDto> inputs, decimal payAmount, DateTime now)
        {
            inputs = (inputs ?? new List<JyCustomOffsetEditDto>()).Where(it => it != null && it.OffsetAmount > 0).ToList();
            if (inputs.Count == 0) return new List<JySaleCustomOffset>();
            if (inputs.Any(it => it.CustomOrderId <= 0))
                throw new CustomException("定金对冲中存在无效的定制单");
            var ids = inputs.Select(it => it.CustomOrderId).Distinct().ToList();
            var orders = Context.Queryable<JyCustomOrder>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && ids.Contains(it.Id))
                .ToList();
            if (orders.Count != ids.Count) throw new CustomException("部分定制单不存在");
            var map = orders.ToDictionary(it => it.Id);
            var orderItems = Context.Queryable<JyCustomItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && ids.Contains(it.CustomOrderId))
                .OrderBy(it => it.SortNo).OrderBy(it => it.Id).ToList()
                .GroupBy(it => it.CustomOrderId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var plans = new List<JySaleCustomOffset>();
            var usedItems = new HashSet<long>();
            foreach (var input in inputs)
            {
                var order = map[input.CustomOrderId];
                if (!orderItems.TryGetValue(order.Id, out var items) || items.Count == 0)
                    throw new CustomException($"定制单 {order.CustomNo} 的商品明细尚未补齐");
                JyCustomItem item;
                if (input.CustomItemId > 0)
                {
                    item = items.FirstOrDefault(it => it.Id == input.CustomItemId.Value)
                        ?? throw new CustomException($"定制单 {order.CustomNo} 的商品明细不存在");
                }
                else
                {
                    if (items.Count != 1) throw new CustomException($"定制单 {order.CustomNo} 包含多件商品，请选择具体核销商品");
                    item = items[0];
                }
                if (!usedItems.Add(item.Id)) throw new CustomException($"定制单 {order.CustomNo} 的同一件商品不能重复对冲");
                if (order.Status is not (1 or 2) || item.Status is not (1 or 2) || item.RemainAmount <= 0)
                    throw new CustomException($"定制单 {order.CustomNo} 没有可对冲的定金");
                if (header.MemberId > 0 && order.MemberId > 0 && header.MemberId != order.MemberId)
                    throw new CustomException($"定制单 {order.CustomNo} 不属于当前会员");
                if (!(header.MemberId > 0 && order.MemberId == header.MemberId)
                    && (string.IsNullOrEmpty(header.CustomerPhone) || order.CustomerPhone != header.CustomerPhone))
                    throw new CustomException($"定制单 {order.CustomNo} 与当前客户不匹配");
                var reserved = Context.Queryable<JySaleCustomOffset>()
                    .Where(it => it.TenantId == actor.TenantId && it.CustomItemId == item.Id && it.Status == 0 && it.SaleOrderId != header.Id)
                    .Sum(it => it.OffsetAmount);
                if (items.Count == 1)
                {
                    reserved += Context.Queryable<JySaleCustomOffset>()
                        .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == order.Id
                            && it.CustomItemId == null && it.Status == 0 && it.SaleOrderId != header.Id)
                        .Sum(it => it.OffsetAmount);
                }
                var available = RoundMoney(item.RemainAmount - reserved);
                var amount = RoundMoney(input.OffsetAmount);
                if (amount <= 0 || amount > available)
                    throw new CustomException($"定制单 {order.CustomNo} / {item.GoodsName} 可对冲定金不足，当前可对冲 {available:0.00}");
                plans.Add(new JySaleCustomOffset
                {
                    TenantId = actor.TenantId,
                    SaleOrderId = header.Id,
                    CustomOrderId = order.Id,
                    CustomItemId = item.Id,
                    CustomNo = order.CustomNo,
                    OffsetAmount = amount,
                    Status = 0,
                    Create_time = now
                });
            }
            var total = RoundMoney(plans.Sum(it => it.OffsetAmount));
            if (total > payAmount) throw new CustomException("定金对冲不能超过商品实付金额");
            return plans;
        }

        private void ApplyConfirmedOffsets(JyActor actor, JySaleOrder header, DateTime now)
        {
            var plans = Context.Queryable<JySaleCustomOffset>()
                .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == header.Id && it.Status == 0)
                .ToList();
            decimal total = 0;
            var touchedOrders = new HashSet<long>();
            foreach (var plan in plans)
            {
                var order = Context.Queryable<JyCustomOrder>().With(SqlWith.UpdLock)
                    .First(it => it.Id == plan.CustomOrderId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException($"定制单 {plan.CustomNo} 不存在");
                var item = ResolveOffsetItem(actor, order.Id, plan.CustomItemId);
                var reserved = Context.Queryable<JySaleCustomOffset>()
                    .Where(it => it.TenantId == actor.TenantId && it.CustomItemId == item.Id && it.Status == 0 && it.SaleOrderId != header.Id)
                    .Sum(it => it.OffsetAmount);
                var available = RoundMoney(item.RemainAmount - reserved);
                if (plan.OffsetAmount <= 0 || plan.OffsetAmount > available)
                    throw new CustomException($"定制单 {order.CustomNo} / {item.GoodsName} 可对冲定金不足");
                total += plan.OffsetAmount;
            }
            total = RoundMoney(total);
            if (total > header.PayAmount) throw new CustomException("定金对冲不能超过商品实付金额");
            foreach (var plan in plans)
            {
                var order = Context.Queryable<JyCustomOrder>().With(SqlWith.UpdLock)
                    .First(it => it.Id == plan.CustomOrderId && it.TenantId == actor.TenantId && it.DelFlag == 0);
                var item = ResolveOffsetItem(actor, order.Id, plan.CustomItemId);
                var remain = RoundMoney(item.RemainAmount - plan.OffsetAmount);
                var offset = RoundMoney(item.OffsetAmount + plan.OffsetAmount);
                var status = JyCustomService.ResolveStatus(item.DepositAmount, offset, item.RefundAmount, remain);
                var affected = Context.Updateable<JyCustomItem>()
                    .SetColumns(it => it.RemainAmount == remain)
                    .SetColumns(it => it.OffsetAmount == offset)
                    .SetColumns(it => it.Status == status)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == item.Id && it.TenantId == actor.TenantId && it.RemainAmount == item.RemainAmount)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("定金余额已变化，请刷新后重试");
                Context.Insertable(new JyCustomLedger
                {
                    TenantId = actor.TenantId,
                    CustomOrderId = order.Id,
                    CustomItemId = item.Id,
                    ChangeType = 2,
                    Amount = -plan.OffsetAmount,
                    BalanceAfter = remain,
                    SaleOrderId = header.Id,
                    Remark = "销售对冲 " + header.OrderNo + "（" + item.GoodsName + "）",
                    BizTime = now,
                    Create_by = actor.UserName,
                    Create_time = now
                }).ExecuteCommand();
                var marked = Context.Updateable<JySaleCustomOffset>()
                    .SetColumns(it => it.Status == 1)
                    .Where(it => it.Id == plan.Id && it.Status == 0)
                    .ExecuteCommand();
                if (marked != 1) throw new CustomException("定金对冲状态已变化，请刷新后重试");
                if (!plan.CustomItemId.HasValue)
                {
                    Context.Updateable<JySaleCustomOffset>().SetColumns(it => it.CustomItemId == item.Id)
                        .Where(it => it.Id == plan.Id).ExecuteCommand();
                }
                touchedOrders.Add(order.Id);
            }
            foreach (var orderId in touchedOrders) CustomService.RecalculateHeader(actor, orderId, now);
            header.CustomOffsetAmount = total;
            header.CashAmount = RoundMoney(Math.Max(0, header.PayAmount - total));
        }

        private void VoidSale(JyActor actor, long id)
        {
            var now = DateTime.Now;
            var header = Context.Queryable<JySaleOrder>().With(SqlWith.UpdLock)
                .First(it => it.Id == id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                ?? throw new CustomException("销售单不存在");
            if (header.Status == 0)
            {
                Context.Deleteable<JySaleCustomOffset>()
                    .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == header.Id && it.Status == 0)
                    .ExecuteCommand();
                var affected = Context.Updateable<JySaleOrder>()
                    .SetColumns(it => it.DelFlag == 2)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId && it.Status == 0 && it.DelFlag == 0)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("销售单状态已变化，请刷新后重试");
                return;
            }
            if (header.Status != 1) throw new CustomException("这张销售单不能删除");
            if (header.ReturnStatus != 0 || header.ReturnedAmount > 0)
                throw new CustomException("销售单已有退货，不能直接删除");
            var hasReturn = Context.Queryable<JySaleReturn>().Any(it => it.TenantId == actor.TenantId && it.SaleOrderId == header.Id && it.Status != 9);
            if (hasReturn) throw new CustomException("销售单已有退货，不能直接删除");
            var items = Context.Queryable<JySaleOrderItem>()
                .Where(it => it.TenantId == actor.TenantId && it.OrderId == header.Id).ToList();
            // 1. 先校验每件商品的最新库存流水确实是本销售单，避免误删后续业务
            foreach (var line in items)
            {
                var latest = Context.Queryable<JyStockLedger>()
                    .Where(it => it.TenantId == actor.TenantId && it.GoodsItemId == line.GoodsItemId)
                    .OrderBy(it => it.Id, OrderByType.Desc)
                    .First();
                if (latest == null || latest.BizType != "sale" || latest.BizId != header.Id)
                    throw new CustomException($"货品 {line.GoodsNo} 已有新的库存业务，不能删除这张销售单");
            }
            // 2. 先冲回定金，恢复定制单余额后再判断商品是否仍应锁定
            ReverseConfirmedOffsets(actor, header, now);
            foreach (var line in items)
            {
                var goods = Context.Queryable<JyGoodsItem>().With(SqlWith.UpdLock)
                    .First(it => it.Id == line.GoodsItemId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException($"货品 {line.GoodsNo} 不存在");
                var before = goods.Qty;
                var after = before + line.Qty;
                var restore = ResolveGoodsRestore(actor, goods.Id);
                var affectedGoods = Context.Updateable<JyGoodsItem>()
                    .SetColumns(it => it.Qty == after)
                    .SetColumns(it => it.Status == restore.Status)
                    .SetColumns(it => it.LockedQty == restore.LockedQty)
                    .SetColumns(it => it.LockBizType == restore.LockBizType)
                    .SetColumns(it => it.LockBizId == restore.LockBizId)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == goods.Id && it.TenantId == actor.TenantId && it.Qty == before)
                    .ExecuteCommand();
                if (affectedGoods != 1) throw new CustomException($"货品 {line.GoodsNo} 库存已变化，请刷新后重试");
                Context.Insertable(new JyStockLedger
                {
                    TenantId = actor.TenantId,
                    StoreId = header.StoreId,
                    LedgerNo = LedgerNo(line.GoodsNo, "VD"),
                    GoodsItemId = goods.Id,
                    GoodsNo = line.GoodsNo,
                    ChangeType = 9,
                    ChangeQty = line.Qty,
                    BeforeQty = before,
                    AfterQty = after,
                    BizType = "sale_void",
                    BizId = header.Id,
                    BizTime = now,
                    Create_by = actor.UserName,
                    Create_time = now,
                    Remark = "作废销售 " + header.OrderNo
                }).ExecuteCommand();
            }
            ReverseMemberForVoid(actor, header, now);
            Referral.VoidForSaleInTransaction(actor, header.Id, now);
            var voided = Context.Updateable<JySaleOrder>()
                .SetColumns(it => it.Status == 9)
                .SetColumns(it => it.DelFlag == 2)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == header.Id && it.TenantId == actor.TenantId && it.Status == 1 && it.DelFlag == 0)
                .ExecuteCommand();
            if (voided != 1) throw new CustomException("销售单状态已变化，请刷新后重试");
        }

        private void ReverseConfirmedOffsets(JyActor actor, JySaleOrder header, DateTime now)
        {
            var plans = Context.Queryable<JySaleCustomOffset>()
                .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == header.Id && it.Status == 1)
                .ToList();
            var touchedOrders = new HashSet<long>();
            foreach (var plan in plans)
            {
                var order = Context.Queryable<JyCustomOrder>().With(SqlWith.UpdLock)
                    .First(it => it.Id == plan.CustomOrderId && it.TenantId == actor.TenantId && it.DelFlag == 0)
                    ?? throw new CustomException($"定制单 {plan.CustomNo} 不存在，不能冲回定金");
                var item = ResolveOffsetItem(actor, order.Id, plan.CustomItemId);
                var remain = RoundMoney(item.RemainAmount + plan.OffsetAmount);
                var offset = RoundMoney(Math.Max(0, item.OffsetAmount - plan.OffsetAmount));
                var status = JyCustomService.ResolveStatus(item.DepositAmount, offset, item.RefundAmount, remain);
                var affected = Context.Updateable<JyCustomItem>()
                    .SetColumns(it => it.RemainAmount == remain)
                    .SetColumns(it => it.OffsetAmount == offset)
                    .SetColumns(it => it.Status == status)
                    .SetColumns(it => it.Update_by == actor.UserName)
                    .SetColumns(it => it.Update_time == now)
                    .Where(it => it.Id == item.Id && it.RemainAmount == item.RemainAmount)
                    .ExecuteCommand();
                if (affected != 1) throw new CustomException("定金余额已变化，请刷新后重试");
                Context.Insertable(new JyCustomLedger
                {
                    TenantId = actor.TenantId,
                    CustomOrderId = order.Id,
                    CustomItemId = item.Id,
                    ChangeType = 3,
                    Amount = plan.OffsetAmount,
                    BalanceAfter = remain,
                    SaleOrderId = header.Id,
                    Remark = "销售作废冲回 " + header.OrderNo + "（" + item.GoodsName + "）",
                    BizTime = now,
                    Create_by = actor.UserName,
                    Create_time = now
                }).ExecuteCommand();
                Context.Updateable<JySaleCustomOffset>()
                    .SetColumns(it => it.Status == 9)
                    .Where(it => it.Id == plan.Id && it.Status == 1)
                    .ExecuteCommand();
                if (!plan.CustomItemId.HasValue)
                {
                    Context.Updateable<JySaleCustomOffset>().SetColumns(it => it.CustomItemId == item.Id)
                        .Where(it => it.Id == plan.Id).ExecuteCommand();
                }
                touchedOrders.Add(order.Id);
            }
            foreach (var orderId in touchedOrders) CustomService.RecalculateHeader(actor, orderId, now);
        }

        private void ReverseMemberForVoid(JyActor actor, JySaleOrder sale, DateTime now)
        {
            if (!sale.MemberId.HasValue || sale.OrderType != 1) return;
            var member = Context.Queryable<JyMember>().With(SqlWith.UpdLock)
                .First(it => it.Id == sale.MemberId.Value && it.TenantId == actor.TenantId && it.DelFlag == 0);
            if (member == null) return;
            var earned = Context.Queryable<JyPointLedger>()
                .Where(it => it.TenantId == actor.TenantId && it.SaleOrderId == sale.Id && it.ChangeType == 1)
                .Sum(it => it.ChangePoints);
            var actual = Math.Min(Math.Max(0, earned), Math.Max(0, member.PointsBalance));
            var newBalance = member.PointsBalance - actual;
            var newYearPoints = Math.Max(0, member.YearPoints - actual);
            var newTotal = Math.Max(0, member.TotalAmount - sale.PayAmount);
            var newYear = Math.Max(0, member.YearAmount - sale.PayAmount);
            var newCount = Math.Max(0, member.OrderCount - 1);
            var avg = newCount > 0 ? newTotal / newCount : 0m;
            var affected = Context.Updateable<JyMember>()
                .SetColumns(it => it.PointsBalance == newBalance)
                .SetColumns(it => it.YearPoints == newYearPoints)
                .SetColumns(it => it.TotalAmount == newTotal)
                .SetColumns(it => it.YearAmount == newYear)
                .SetColumns(it => it.OrderCount == newCount)
                .SetColumns(it => it.AvgTicket == avg)
                .SetColumns(it => it.Update_by == actor.UserName)
                .SetColumns(it => it.Update_time == now)
                .Where(it => it.Id == member.Id && it.TenantId == actor.TenantId && it.DelFlag == 0)
                .ExecuteCommand();
            if (affected != 1) throw new CustomException("会员信息已变化，请刷新后重试");
            if (actual > 0)
            {
                Context.Insertable(new JyPointLedger
                {
                    TenantId = actor.TenantId,
                    MemberId = member.Id,
                    ChangeType = 5,
                    ChangePoints = -actual,
                    BalanceAfter = newBalance,
                    YearPointsAfter = newYearPoints,
                    SaleOrderId = sale.Id,
                    OperatorId = actor.UserId,
                    Reason = "作废销售 " + sale.OrderNo,
                    BizTime = now,
                    Create_time = now
                }).ExecuteCommand();
            }
        }

        private List<JySaleCustomOffsetDto> LoadOffsets(string tenantId, long saleId)
        {
            var rows = Context.Queryable<JySaleCustomOffset>()
                .Where(it => it.TenantId == tenantId && it.SaleOrderId == saleId && it.Status != 9)
                .OrderBy(it => it.Id)
                .ToList();
            if (rows.Count == 0) return new List<JySaleCustomOffsetDto>();
            var ids = rows.Select(it => it.CustomOrderId).Distinct().ToList();
            var orders = Context.Queryable<JyCustomOrder>()
                .Where(it => it.TenantId == tenantId && ids.Contains(it.Id)).ToList()
                .ToDictionary(it => it.Id);
            var itemIds = rows.Where(it => it.CustomItemId.HasValue)
                .Select(it => it.CustomItemId.Value).Distinct().ToList();
            var items = itemIds.Count == 0
                ? new Dictionary<long, JyCustomItem>()
                : Context.Queryable<JyCustomItem>()
                    .Where(it => it.TenantId == tenantId && itemIds.Contains(it.Id)).ToList()
                    .ToDictionary(it => it.Id);
            return rows.Select(it =>
            {
                orders.TryGetValue(it.CustomOrderId, out var order);
                JyCustomItem item = null;
                if (it.CustomItemId.HasValue) items.TryGetValue(it.CustomItemId.Value, out item);
                return new JySaleCustomOffsetDto
                {
                    Id = it.Id,
                    CustomOrderId = it.CustomOrderId,
                    CustomItemId = it.CustomItemId,
                    CustomNo = it.CustomNo,
                    GoodsName = item?.GoodsName ?? order?.GoodsName,
                    OffsetAmount = it.OffsetAmount,
                    Status = it.Status,
                    RemainAmount = item?.RemainAmount ?? order?.RemainAmount ?? 0
                };
            }).ToList();
        }

        /// <summary>旧版对冲没有 CustomItemId 时，只在定金单确属单件商品时自动解析。</summary>
        private JyCustomItem ResolveOffsetItem(JyActor actor, long customOrderId, long? customItemId)
        {
            var q = Context.Queryable<JyCustomItem>().With(SqlWith.UpdLock)
                .Where(it => it.TenantId == actor.TenantId && it.CustomOrderId == customOrderId && it.DelFlag == 0);
            if (customItemId > 0)
            {
                return q.First(it => it.Id == customItemId.Value)
                    ?? throw new CustomException("定金商品明细不存在或已删除");
            }
            var items = q.OrderBy(it => it.SortNo).ToList();
            if (items.Count != 1) throw new CustomException("该定金单包含多件商品，请选择具体核销商品");
            return items[0];
        }

        private static decimal RoundMoney(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private sealed class SaleCalc
        {
            public List<JySaleItemDto> Items { get; set; } = new();
            public Dictionary<long, JyGoodsItem> GoodsMap { get; set; } = new();
            public JyMember Member { get; set; }
            public JyActivity Activity { get; set; }
            public string ActivityName { get; set; }
            public string MemberLevelName { get; set; }
            public string GiftName { get; set; }
            public decimal DiscountRate { get; set; } = 1m;
            public decimal PointsMultiple { get; set; } = 1m;
            public decimal TotalAmount { get; set; }
            public decimal MemberDiscount { get; set; }
            public decimal ActivityBenefit { get; set; }
            public decimal BasePayAmount { get; set; }
            public decimal AdjustAmount { get; set; }
            public decimal RoundOffAmount { get; set; }
            public decimal PayAmount { get; set; }
            public decimal CostAmount { get; set; }
            public decimal PieceQty { get; set; }
            public decimal PointsEarned { get; set; }

            public JySalePreviewDto ToPreview()
            {
                return new JySalePreviewDto
                {
                    TotalAmount = TotalAmount,
                    MemberDiscount = MemberDiscount,
                    ActivityBenefit = ActivityBenefit,
                    BasePayAmount = BasePayAmount,
                    AdjustAmount = AdjustAmount,
                    RoundOffAmount = RoundOffAmount,
                    PayAmount = PayAmount,
                    PointsEarned = PointsEarned,
                    ActivityName = ActivityName,
                    MemberLevelName = MemberLevelName,
                    DiscountRate = DiscountRate,
                    GiftName = GiftName,
                    Items = Items
                };
            }
        }
    }
}





