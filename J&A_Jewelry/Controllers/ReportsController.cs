using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using J_A_Jewelry.Services.Identity;
using J_A_Jewelry.Models;

namespace J_A_Jewelry.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Permissions.Reports)]
    public class ReportsController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public ReportsController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        private IQueryable<SalesOrderHeader> SalesInRange(DateTime? fromDate, DateTime? toDate)
        {
            var q = _context.SalesOrderHeaders.AsNoTracking()
                .Where(h => h.SaleDate.HasValue);
            if (fromDate.HasValue)
                q = q.Where(h => h.SaleDate >= fromDate);
            if (toDate.HasValue)
                q = q.Where(h => h.SaleDate < toDate.Value.AddDays(1));
            return q;
        }

        // GET: api/Reports/SalesSummary?from_date=2026-01-01&to_date=2026-01-31&group_by=day|month
        [HttpGet("SalesSummary")]
        public async Task<ActionResult<IEnumerable<SalesSummaryRow>>> SalesSummary(
            [FromQuery(Name = "from_date")] DateTime? fromDate = null,
            [FromQuery(Name = "to_date")] DateTime? toDate = null,
            [FromQuery(Name = "group_by")] string groupBy = "day")
        {
            var src = SalesInRange(fromDate, toDate).Where(h => h.Total.HasValue);

            object grouped;
            if (string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase))
            {
                grouped = await src
                    .GroupBy(h => new { h.SaleDate!.Value.Year, h.SaleDate.Value.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Day = 0, Revenue = g.Sum(x => x.Total)!.Value, Count = g.Count() })
                    .ToListAsync();
            }
            else
            {
                grouped = await src
                    .GroupBy(h => new { h.SaleDate!.Value.Year, h.SaleDate.Value.Month, h.SaleDate.Value.Day })
                    .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Revenue = g.Sum(x => x.Total)!.Value, Count = g.Count() })
                    .ToListAsync();
            }

            var rows = ((IEnumerable<dynamic>)grouped).Cast<dynamic>()
                .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
                .Select(x => new SalesSummaryRow
                {
                    Period = x.Day == 0
                        ? string.Format("{0:D2}-{1:D2}", x.Year, x.Month)
                        : string.Format("{0:D2}-{1:D2}-{2:D2}", x.Year, x.Month, x.Day),
                    Revenue = (decimal)x.Revenue,
                    OrderCount = (int)x.Count,
                    AverageTicket = x.Count == 0 ? 0 : Math.Round((decimal)x.Revenue / (int)x.Count, 2)
                })
                .ToList();

            return rows;
        }

        // GET: api/Reports/TopProducts?from_date&to_date&limit=10
        [HttpGet("TopProducts")]
        public async Task<ActionResult<IEnumerable<TopProductRow>>> TopProducts(
            [FromQuery(Name = "from_date")] DateTime? fromDate = null,
            [FromQuery(Name = "to_date")] DateTime? toDate = null,
            [FromQuery] int limit = 10)
        {
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            // EF can't join on top of a grouped subquery — aggregate in SQL,
            // materialize the small row set, then enrich names in memory.
            var aggregated = await (from d in _context.SalesOrderDetails.AsNoTracking()
                                    join h in SalesInRange(fromDate, toDate) on d.SalesOrderId equals h.Id
                                    group d by d.ProductId into g
                                    select new
                                    {
                                        Key = g.Key,
                                        QuantitySold = g.Sum(x => x.Quantity),
                                        Revenue = g.Sum(x => x.Total)
                                    })
                                   .ToListAsync();

            var productIds = aggregated.Where(x => x.Key.HasValue).Select(x => x.Key!.Value).Distinct().ToList();
            var productNames = await _context.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Description ?? "");

            var rows = aggregated
                .Where(x => x.Key.HasValue)
                .OrderByDescending(x => x.Revenue ?? 0)
                .Take(limit)
                .Select(x => new TopProductRow
                {
                    ProductId = x.Key!.Value,
                    Description = productNames.TryGetValue(x.Key.Value, out var name) ? name : "",
                    QuantitySold = x.QuantitySold ?? 0,
                    Revenue = x.Revenue ?? 0
                })
                .ToList();

            return rows;
        }

        // GET: api/Reports/CategoryShare?from_date&to_date
        [HttpGet("CategoryShare")]
        public async Task<ActionResult<IEnumerable<CategoryShareRow>>> CategoryShare(
            [FromQuery(Name = "from_date")] DateTime? fromDate = null,
            [FromQuery(Name = "to_date")] DateTime? toDate = null)
        {
            var aggregated = await (from d in _context.SalesOrderDetails.AsNoTracking()
                                    join h in SalesInRange(fromDate, toDate) on d.SalesOrderId equals h.Id
                                    join pc in _context.ProductCategories.AsNoTracking() on d.ProductId equals pc.ProductId
                                    group d by pc.CategoryId into g
                                    select new
                                    {
                                        Key = g.Key,
                                        QuantitySold = g.Sum(x => x.Quantity),
                                        Revenue = g.Sum(x => x.Total)
                                    })
                                   .ToListAsync();

            var categoryIds = aggregated.Where(x => x.Key.HasValue).Select(x => x.Key!.Value).Distinct().ToList();
            var categoryNames = await _context.Categories.AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Description ?? "");

            var rows = aggregated
                .Where(x => x.Key.HasValue)
                .OrderByDescending(x => x.Revenue ?? 0)
                .Select(x => new CategoryShareRow
                {
                    CategoryId = x.Key,
                    CategoryName = categoryNames.TryGetValue(x.Key!.Value, out var name) ? name : "",
                    QuantitySold = x.QuantitySold ?? 0,
                    Revenue = x.Revenue ?? 0
                })
                .ToList();

            return rows;
        }

        // GET: api/Reports/StockLevels?warehouse_id=1&below_quantity=5
        [HttpGet("StockLevels")]
        public async Task<ActionResult<IEnumerable<StockLevelRow>>> StockLevels(
            [FromQuery(Name = "warehouse_id")] int? warehouseId = null,
            [FromQuery(Name = "below_quantity")] int? belowQuantity = null)
        {
            var query = from i in _context.Inventories.AsNoTracking()
                        join p in _context.Products.AsNoTracking() on i.ProductId equals p.Id
                        join w in _context.Warehouses.AsNoTracking() on i.WarehouseId equals w.Id into ws
                        from w in ws.DefaultIfEmpty()
                        select new StockLevelRow
                        {
                            ProductId = p.Id,
                            Description = p.Description ?? "",
                            Picture = p.Picture,
                            WarehouseId = i.WarehouseId,
                            WarehouseName = w != null ? w.Name : null,
                            Quantity = i.Quantity ?? 0
                        };

            if (warehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);

            query = query.OrderBy(x => x.Quantity);

            if (belowQuantity.HasValue)
                query = query.Where(x => x.Quantity <= belowQuantity.Value);

            return await query.ToListAsync();
        }

        // GET: api/Reports/CustomerBalances?only_with_balance=true
        [HttpGet("CustomerBalances")]
        public async Task<ActionResult<IEnumerable<CustomerBalanceRow>>> CustomerBalances(
            [FromQuery(Name = "only_with_balance")] bool onlyWithBalance = false)
        {
            // Single payment methods settle at the counter — the sale itself is
            // the receipt — so those totals count as paid without a
            // CustomerPayment row; only the instalment method (same three-way
            // name match the client uses) leaves a balance carried by
            // CustomerPayments.
            var instalmentMethodIds = _context.PaymentMethods.AsNoTracking()
                .Where(m => (m.Description ?? "").ToLower().Contains("multiple")
                         || (m.Description ?? "").ToLower().Contains("varias")
                         || (m.Description ?? "").ToLower().Contains("parcial"))
                .Select(m => m.Id)
                .ToList();

            var salesSums = await _context.SalesOrderHeaders.AsNoTracking()
                .GroupBy(h => h.CustomerId)
                .Select(g => new { CustomerId = g.Key, Total = g.Sum(x => x.Total) })
                .ToListAsync();

            var settledSums = await _context.SalesOrderHeaders.AsNoTracking()
                .Where(x => x.PaymentMethodId == null
                            || !instalmentMethodIds.Contains(x.PaymentMethodId.Value))
                .GroupBy(h => h.CustomerId)
                .Select(g => new { CustomerId = g.Key, Total = g.Sum(x => x.Total) })
                .ToListAsync();

            var paymentSums = await _context.CustomerPayments.AsNoTracking()
                .GroupBy(p => p.CustomerId)
                .Select(g => new { CustomerId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            var salesTotals = salesSums.ToDictionary(x => x.CustomerId, x => x.Total ?? 0m);
            var settledTotals = settledSums.ToDictionary(x => x.CustomerId, x => x.Total ?? 0m);
            var paymentTotals = paymentSums.ToDictionary(x => x.CustomerId, x => x.Total ?? 0m);

            var rows = await _context.Customers.AsNoTracking()
                .Select(c => new CustomerBalanceRow
                {
                    CustomerId = c.Id,
                    Name = c.Name ?? "",
                    TotalSales = 0m,
                    TotalPaid = 0m,
                    Outstanding = 0m
                })
                .ToListAsync();
            foreach (var row in rows)
            {
                row.TotalSales = salesTotals.GetValueOrDefault(row.CustomerId);
                row.TotalPaid = settledTotals.GetValueOrDefault(row.CustomerId)
                              + paymentTotals.GetValueOrDefault(row.CustomerId);
                row.Outstanding = row.TotalSales - row.TotalPaid;
            }

            if (onlyWithBalance)
                rows = rows.Where(r => r.Outstanding > 0.005m)
                           .OrderByDescending(r => r.Outstanding)
                           .ToList();

            return rows;
        }

        // GET: api/Reports/PurchasesBySupplier?from_date&to_date
        [HttpGet("PurchasesBySupplier")]
        public async Task<ActionResult<IEnumerable<PurchaseBySupplierRow>>> PurchasesBySupplier(
            [FromQuery(Name = "from_date")] DateTime? fromDate = null,
            [FromQuery(Name = "to_date")] DateTime? toDate = null)
        {
            var q = _context.PurchaseOrderHeaders.AsNoTracking()
                .Where(po => po.OrderDate.HasValue);
            if (fromDate.HasValue)
                q = q.Where(po => po.OrderDate >= fromDate);
            if (toDate.HasValue)
                q = q.Where(po => po.OrderDate < toDate.Value.AddDays(1));

            var aggregated = await (from po in q
                                    group po by po.SupplierId into g
                                    select new
                                    {
                                        Key = g.Key,
                                        PurchaseCount = g.Count(),
                                        TotalSpent = g.Sum(x => x.Total)
                                    })
                                   .ToListAsync();

            var supplierIds = aggregated.Where(x => x.Key.HasValue).Select(x => x.Key!.Value).Distinct().ToList();
            var supplierNames = await _context.Suppliers.AsNoTracking()
                .Where(s => supplierIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name ?? "");

            var rows = aggregated
                .Where(x => x.Key.HasValue)
                .OrderByDescending(x => x.TotalSpent ?? 0)
                .Select(x => new PurchaseBySupplierRow
                {
                    SupplierId = x.Key,
                    SupplierName = supplierNames.TryGetValue(x.Key!.Value, out var name) ? name : "",
                    PurchaseCount = x.PurchaseCount,
                    TotalSpent = x.TotalSpent ?? 0
                })
                .ToList();

            return rows;
        }

        // GET: api/Reports/TaxTotals?from_date&to_date
        [HttpGet("TaxTotals")]
        public async Task<ActionResult<IEnumerable<TaxTotalRow>>> TaxTotals(
            [FromQuery(Name = "from_date")] DateTime? fromDate = null,
            [FromQuery(Name = "to_date")] DateTime? toDate = null)
        {
            var rows = await (from st in _context.SalesTaxDetails.AsNoTracking()
                              join h in SalesInRange(fromDate, toDate) on st.SalesOrderId equals h.Id
                              group st by st.TaxType into g
                              select new TaxTotalRow
                              {
                                  TaxType = g.Key ?? "",
                                  TotalAmount = g.Sum(x => x.TaxAmount) ?? 0
                              })
                             .ToListAsync();

            return rows;
        }

        // GET: api/Reports/SalespersonPerformance?from_date&to_date
        [HttpGet("SalespersonPerformance")]
        public async Task<ActionResult<IEnumerable<SalespersonRow>>> SalespersonPerformance(
            [FromQuery(Name = "from_date")] DateTime? fromDate = null,
            [FromQuery(Name = "to_date")] DateTime? toDate = null)
        {
            var rows = await SalesInRange(fromDate, toDate)
                .Where(h => h.Total.HasValue && h.SalespersonName != null && h.SalespersonName != "")
                .GroupBy(h => h.SalespersonName)
                .Select(g => new SalespersonRow
                {
                    Name = g.Key!,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(x => x.Total) ?? 0
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToListAsync();

            return rows;
        }
    }
}