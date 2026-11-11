using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using ZR.Model.JingYue;
using ZR.Model.JingYue.Dto;

namespace ZR.Service.JingYue
{
    [AppService]
    public class JyStockService : BaseService<JyGoodsItem>
    {
        public PagedInfo<JyStockItemDto> List(JyActor actor, JyStockQueryDto query)
        {
            query ??= new JyStockQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .WhereIF(query.Status is >= 1 and <= 8, it => it.Status == query.Status)
                .WhereIF(query.StyleId > 0, it => it.StyleId == query.StyleId)
                .WhereIF(query.StyleItemId > 0, it => it.StyleItemId == query.StyleItemId)
                .WhereIF(!string.IsNullOrEmpty(keyword), it =>
                    it.GoodsNo.Contains(keyword) || it.Barcode.Contains(keyword) || it.StyleName.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.Status).OrderBy(it => it.Id, OrderByType.Desc).ToPageList(page, size, ref total);
            return new PagedInfo<JyStockItemDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = ToDtos(actor.TenantId, rows)
            };
        }

        public JyStockSummaryDto Summary(JyActor actor)
        {
            var baseQuery = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0);
            var onShelf = baseQuery.Where(it => it.Status == 1).ToList();
            var rows = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0 && it.Status == 1)
                .OrderBy(it => it.Id, OrderByType.Desc)
                .Take(100).ToList();
            var statusCounts = Context.Queryable<JyGoodsItem>()
                .Where(it => it.TenantId == actor.TenantId && it.DelFlag == 0)
                .GroupBy(it => it.Status)
                .Select(it => new { it.Status, Count = SqlFunc.AggregateCount(it.Id) })
                .ToList()
                .ToDictionary(it => it.Status, it => it.Count);
            return new JyStockSummaryDto
            {
                OnShelfCount = onShelf.Count,
                TotalQty = onShelf.Sum(it => it.Qty),
                StockCost = onShelf.Sum(it => it.CostPrice * it.Qty),
                StockSale = onShelf.Sum(it => it.SalePrice * it.Qty),
                LowStockCount = onShelf.Count(it => it.Qty <= 1),
                StatusCounts = statusCounts,
                Rows = ToDtos(actor.TenantId, rows)
            };
        }

        public PagedInfo<JyStockLedgerDto> Ledger(JyActor actor, JyStockLedgerQueryDto query)
        {
            query ??= new JyStockLedgerQueryDto();
            var page = query.PageNum <= 0 ? 1 : query.PageNum;
            var size = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var keyword = query.Keyword?.Trim();
            var q = Context.Queryable<JyStockLedger>()
                .Where(it => it.TenantId == actor.TenantId)
                .WhereIF(query.GoodsItemId > 0, it => it.GoodsItemId == query.GoodsItemId)
                .WhereIF(query.ChangeType.HasValue, it => it.ChangeType == query.ChangeType)
                .WhereIF(!string.IsNullOrEmpty(keyword), it => it.LedgerNo.Contains(keyword) || it.GoodsNo.Contains(keyword));
            var total = 0;
            var rows = q.OrderBy(it => it.BizTime, OrderByType.Desc).OrderBy(it => it.Id, OrderByType.Desc)
                .ToPageList(page, size, ref total);
            return new PagedInfo<JyStockLedgerDto>
            {
                PageIndex = page,
                PageSize = size,
                TotalNum = total,
                Result = rows.Select(it => new JyStockLedgerDto
                {
                    Id = it.Id,
                    LedgerNo = it.LedgerNo,
                    GoodsItemId = it.GoodsItemId,
                    GoodsNo = it.GoodsNo,
                    ChangeType = it.ChangeType,
                    ChangeQty = it.ChangeQty,
                    BeforeQty = it.BeforeQty,
                    AfterQty = it.AfterQty,
                    BizType = it.BizType,
                    BizTypeText = BizTypeText(it.BizType, it.ChangeType),
                    BizId = it.BizId,
                    BizTime = it.BizTime,
                    CreateBy = it.Create_by,
                    Remark = it.Remark
                }).ToList()
            };
        }

        private static string BizTypeText(string bizType, int changeType)
        {
            var type = (bizType ?? string.Empty).Trim().ToLowerInvariant();
            return type switch
            {
                "inbound" or "purchase" => "采购入库",
                "sale" => "销售出库",
                "gift" => "赠送出库",
                "sale_return" => "销售退货",
                "factory_return" => "返厂出库",
                "sale_void" => "销售作废",
                "custom" => "定制出库",
                "inventory" or "check" => "库存盘点",
                "outbound" => "其他出库",
                "transfer" => "库存调拨",
                _ => changeType switch
                {
                    1 => "采购入库",
                    2 => "销售出库",
                    5 => "销售退货",
                    6 => "赠送出库",
                    9 => "销售作废",
                    10 => "库存盘点",
                    11 => "返厂出库",
                    _ => "库存变动"
                }
            };
        }

        private List<JyStockItemDto> ToDtos(string tenantId, List<JyGoodsItem> rows)
        {
            if (rows.Count == 0) return new List<JyStockItemDto>();
            var supplierIds = rows.Where(it => it.SupplierId.HasValue).Select(it => it.SupplierId.Value).Distinct().ToList();
            var suppliers = supplierIds.Count == 0 ? new Dictionary<long, string>() : Context.Queryable<JySupplier>()
                .Where(it => it.TenantId == tenantId && supplierIds.Contains(it.Id)).ToList().ToDictionary(it => it.Id, it => it.SupplierName);
            return rows.Select(it => new JyStockItemDto
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
                SupplierName = it.SupplierId.HasValue && suppliers.TryGetValue(it.SupplierId.Value, out var name) ? name : null,
                CreateTime = it.Create_time
            }).ToList();
        }
    }
}

