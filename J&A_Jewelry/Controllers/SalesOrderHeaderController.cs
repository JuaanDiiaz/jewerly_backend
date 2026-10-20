using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using J_A_Jewelry.Models;
using J_A_Jewelry.Services.Identity;

namespace J_A_Jewelry.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesOrderHeaderController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<SalesOrderHeaderController> _logger;

        public SalesOrderHeaderController(IsetechcJewelryInventoryContext context,
            Microsoft.Extensions.Logging.ILogger<SalesOrderHeaderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/SalesOrderHeader?page=1&page_size=50&from_date&to_date
        [HttpGet]
        public async Task<ActionResult<PagedResult<SalesOrderHeader>>> GetSalesOrderHeaders(
            [FromQuery(Name = "page")] int? page = null,
            [FromQuery(Name = "page_size")] int? pageSize = null,
            [FromQuery(Name = "from_date")] DateTime? fromDate = null,
            [FromQuery(Name = "to_date")] DateTime? toDate = null)
        {
            var safePage = Math.Max(1, page ?? 1);
            var safeSize = Math.Clamp(pageSize ?? 50, 1, 200);

            var query = _context.SalesOrderHeaders.AsNoTracking().AsQueryable();
            if (fromDate.HasValue)
                query = query.Where(h => h.SaleDate >= fromDate);
            if (toDate.HasValue)
                query = query.Where(h => h.SaleDate < toDate.Value.AddDays(1));

            var result = new PagedResult<SalesOrderHeader>
            {
                Page = safePage,
                PageSize = safeSize,
                TotalCount = await query.CountAsync(),
                Data = await query
                    .OrderByDescending(h => h.Id)
                    .Skip((safePage - 1) * safeSize)
                    .Take(safeSize)
                    .ToListAsync()
            };

            return result;
        }

        // GET: api/SalesOrderHeader/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SalesOrderHeader>> GetSalesOrderHeader(int id)
        {
            var salesOrderHeader = await _context.SalesOrderHeaders.FindAsync(id);

            if (salesOrderHeader == null)
            {
                return NotFound();
            }

            return salesOrderHeader;
        }

        // PUT: api/SalesOrderHeader/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.Sales)]
        public async Task<IActionResult> PutSalesOrderHeader(int id, SalesOrderHeader salesOrderHeader)
        {
            if (id != salesOrderHeader.Id)
            {
                return BadRequest();
            }

            var moneyError = ValidateHeaderMoney(salesOrderHeader);
            if (moneyError != null)
            {
                return moneyError;
            }

            _context.Entry(salesOrderHeader).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SalesOrderHeaderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/SalesOrderHeader
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Policy = Permissions.Sales)]
        public async Task<ActionResult<SalesOrderHeader>> PostSalesOrderHeader(SalesOrderHeader salesOrderHeader)
        {
            var moneyError = ValidateHeaderMoney(salesOrderHeader);
            if (moneyError != null)
            {
                return moneyError;
            }

            _context.SalesOrderHeaders.Add(salesOrderHeader);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSalesOrderHeader", new { id = salesOrderHeader.Id }, salesOrderHeader);
        }

        // DELETE: api/SalesOrderHeader/5
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Sales)]
        public async Task<IActionResult> DeleteSalesOrderHeader(int id)
        {
            var salesOrderHeader = await _context.SalesOrderHeaders.FindAsync(id);
            if (salesOrderHeader == null)
            {
                return NotFound();
            }

            _context.SalesOrderHeaders.Remove(salesOrderHeader);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SalesOrderHeaderExists(int id)
        {
            return _context.SalesOrderHeaders.Any(e => e.Id == id);
        }

        /// <summary>
        /// Rejects negative money on the raw CRUD endpoints. CompleteSale
        /// recomputes these fields from the items; here the client values are
        /// the only source, so at minimum they cannot go below zero.
        /// </summary>
        private static BadRequestObjectResult? ValidateHeaderMoney(SalesOrderHeader header)
        {
            if ((header.Subtotal ?? 0) < 0 || (header.TaxAmount ?? 0) < 0 || (header.Total ?? 0) < 0)
            {
                return new BadRequestObjectResult("Subtotal, taxAmount, and total cannot be negative");
            }
            return null;
        }

        // POST: api/SalesOrderHeader/CompleteSale
        [HttpPost("CompleteSale")]
        [Authorize(Policy = Permissions.Sales)]
        public async Task<ActionResult<object>> CompleteSale([FromBody] CompleteSaleRequest request)
        {
            // Structural validation before any lookups: the sale needs items,
            // and every line must be a real purchase of goods.
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("At least one item is required");

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    return BadRequest($"Quantity for productId {item.ProductId} must be at least 1");
                if (item.UnitPrice < 0)
                    return BadRequest($"Unit price for productId {item.ProductId} cannot be negative");
            }

            // Validate customer exists
            var customer = await _context.Customers.FindAsync(request.CustomerId);
            if (customer == null)
                return BadRequest("Invalid customerId");

            // Validate warehouse exists
            var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId);
            if (warehouse == null)
                return BadRequest("Invalid warehouseId");

            if (request.PaymentMethodId.HasValue)
            {
                var methodExists = await _context.PaymentMethods
                    .AnyAsync(m => m.Id == request.PaymentMethodId.Value);
                if (!methodExists)
                    return BadRequest("Invalid paymentMethodId");
            }

            // Validate all items have sufficient stock and calculate totals.
            // Pricing is IVA-exclusive: the posted unit price is pre-tax and
            // the server computes the tax from the product's category rate
            // (falling back to the rate with a null CategoryId, i.e. the
            // default rate for uncategorized products). Per-line rounding,
            // mid-point away from zero, then summed.
            var activeRates = await _context.TaxRates
                .Where(r => r.IsActive)
                .ToListAsync();
            var requestedProductIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var productsExist = await _context.Products.CountAsync(p => requestedProductIds.Contains(p.Id));
            if (productsExist != requestedProductIds.Count)
                return BadRequest("Invalid productId in items");
            var productCategoryIds = await _context.ProductCategories
                .Where(pc => pc.ProductId != null && requestedProductIds.Contains(pc.ProductId.Value))
                .ToDictionaryAsync(pc => pc.Id, pc => new { pc.ProductId, pc.CategoryId });

            var defaultRate = activeRates.FirstOrDefault(r => r.CategoryId == null);

            // Friendly pre-flight stock check with per-product demand totals
            // (two lines for the same product count once here). This produces
            // a readable error with the available count; the authoritative
            // guard is the conditional UPDATE inside the transaction below,
            // so two concurrent sales of the last unit cannot both succeed.
            var demandByProduct = request.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
            var stockByProduct = await _context.Inventories
                .Where(i => i.WarehouseId == request.WarehouseId && demandByProduct.Keys.Contains(i.ProductId!.Value))
                .ToDictionaryAsync(i => i.ProductId!.Value, i => i.Quantity ?? 0);
            foreach (var (productId, demand) in demandByProduct)
            {
                var available = stockByProduct.GetValueOrDefault(productId);
                if (demand > available)
                    return BadRequest($"Insufficient stock for productId {productId}. Available: {available}, Requested: {demand}");
            }

            decimal subtotal = 0;
            decimal taxTotal = 0;
            // Amount charged per tax rate, keyed by (rate name, rate) so the
            // SalesTaxDetail history rows map to what the Tax Totals report
            // groups on.
            var taxByRate = new Dictionary<string, decimal>();

            foreach (var item in request.Items)
            {
                var categoryIds = productCategoryIds.Values
                    .Where(pc => pc.ProductId == item.ProductId)
                    .Select(pc => pc.CategoryId)
                    .ToList();
                var rate = activeRates.FirstOrDefault(r => r.CategoryId.HasValue && categoryIds.Contains(r.CategoryId))
                           ?? defaultRate;

                decimal itemBase = item.Quantity * item.UnitPrice;
                decimal itemTax = rate?.Rate != null
                    ? decimal.Round(itemBase * rate.Rate.Value, 2, MidpointRounding.AwayFromZero)
                    : 0;
                subtotal += itemBase;
                taxTotal += itemTax;
                if (itemTax > 0 && rate != null)
                {
                    var key = rate.Name ?? $"Rate #{rate.Id}";
                    taxByRate[key] = taxByRate.GetValueOrDefault(key) + itemTax;
                }
            }

            decimal total = subtotal + taxTotal;

            // Create sales order header — the salesperson is the LOGGED-IN
            // user, stamped from the JWT 'name' claim (client-supplied value
            // is ignored; the register no longer hand-types a seller).
            var header = new SalesOrderHeader
            {
                SaleDate = request.SaleDate ?? DateTime.Now,
                CustomerId = request.CustomerId,
                Subtotal = subtotal,
                TaxAmount = taxTotal,
                Total = total,
                PaymentMethodId = request.PaymentMethodId,
                SalespersonName = User.FindFirstValue("name") ?? request.SalespersonName
            };
            // Everything below is one unit of work: if the stock reservation,
            // the details, or the tax rows fail, the header is rolled back too.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            _context.SalesOrderHeaders.Add(header);
            await _context.SaveChangesAsync();

            // Authoritative stock reservation: a conditional UPDATE only
            // matches rows that still hold the units, so the oversell window
            // between a check and the decrement is closed.
            foreach (var (productId, demand) in demandByProduct)
            {
                var affected = await _context.Inventories
                    .Where(i => i.ProductId == productId && i.WarehouseId == request.WarehouseId && (i.Quantity ?? 0) >= demand)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.Quantity, i => (i.Quantity ?? 0) - demand));
                if (affected == 0)
                {
                    _logger.LogWarning(
                        "CompleteSale stock reservation denied HeaderId={HeaderId} ProductId={ProductId} WarehouseId={WarehouseId} Requested={Requested}",
                        header.Id, productId, request.WarehouseId, demand);
                    return BadRequest($"Insufficient stock for productId {productId}. Requested: {demand}");
                }
            }

            // Create details and inventory movements for each item
            var details = new List<SalesOrderDetail>();
            var movements = new List<InventoryMovement>();

            foreach (var item in request.Items)
            {
                // Create sales order detail
                var detail = new SalesOrderDetail
                {
                    SalesOrderId = header.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Total = item.Quantity * item.UnitPrice
                };
                _context.SalesOrderDetails.Add(detail);
                details.Add(detail);

                // Create inventory movement (OUT)
                var movement = new InventoryMovement
                {
                    ProductId = item.ProductId,
                    WarehouseId = request.WarehouseId,
                    MovementType = "OUT",
                    Quantity = item.Quantity,
                    MovementDate = request.SaleDate ?? DateTime.Now,
                    SalesOrderId = header.Id,
                    Notes = $"Sale by {User.FindFirstValue("name") ?? request.SalespersonName}"
                };
                _context.InventoryMovements.Add(movement);
                movements.Add(movement);
            }

            // Record the per-rate tax amounts in SalesTaxDetail — the rows the
            // Tax Totals report groups on.
            var taxDetails = taxByRate
                .Select(kvp => new SalesTaxDetail
                {
                    SalesOrderId = header.Id,
                    TaxType = kvp.Key,
                    TaxAmount = kvp.Value
                })
                .ToList();
            _context.SalesTaxDetails.AddRange(taxDetails);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "CompleteSale committed HeaderId={HeaderId} CustomerId={CustomerId} WarehouseId={WarehouseId} PaymentMethodId={PaymentMethodId} Lines={DetailCount} Subtotal={Subtotal} Tax={Tax} Total={Total}",
                header.Id, request.CustomerId, request.WarehouseId, request.PaymentMethodId,
                details.Count, subtotal, taxTotal, total);

            return Ok(new
            {
                salesOrderId = header.Id,
                header = header,
                details = details,
                movements = movements,
                taxSummary = taxByRate.Select(kvp => new { taxType = kvp.Key, taxAmount = kvp.Value })
            });
        }
    }

    public class CompleteSaleRequest
    {
        public int CustomerId { get; set; }
        public int? PaymentMethodId { get; set; }
        public string? SalespersonName { get; set; }
        public int WarehouseId { get; set; }
        public DateTime? SaleDate { get; set; }
        public List<CompleteSaleItemRequest> Items { get; set; } = new();
    }

    public class CompleteSaleItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}