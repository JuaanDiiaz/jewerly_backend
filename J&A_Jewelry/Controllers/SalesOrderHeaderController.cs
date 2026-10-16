using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using J_A_Jewelry.Models;

namespace J_A_Jewelry.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesOrderHeaderController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public SalesOrderHeaderController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
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
        public async Task<IActionResult> PutSalesOrderHeader(int id, SalesOrderHeader salesOrderHeader)
        {
            if (id != salesOrderHeader.Id)
            {
                return BadRequest();
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
        public async Task<ActionResult<SalesOrderHeader>> PostSalesOrderHeader(SalesOrderHeader salesOrderHeader)
        {
            _context.SalesOrderHeaders.Add(salesOrderHeader);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSalesOrderHeader", new { id = salesOrderHeader.Id }, salesOrderHeader);
        }

        // DELETE: api/SalesOrderHeader/5
        [HttpDelete("{id}")]
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

        // POST: api/SalesOrderHeader/CompleteSale
        [HttpPost("CompleteSale")]
        public async Task<ActionResult<object>> CompleteSale([FromBody] CompleteSaleRequest request)
        {
            // Validate customer exists
            var customer = await _context.Customers.FindAsync(request.CustomerId);
            if (customer == null)
                return BadRequest("Invalid customerId");

            // Validate warehouse exists
            var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId);
            if (warehouse == null)
                return BadRequest("Invalid warehouseId");

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
            var productCategoryIds = await _context.ProductCategories
                .Where(pc => pc.ProductId != null && requestedProductIds.Contains(pc.ProductId.Value))
                .ToDictionaryAsync(pc => pc.Id, pc => new { pc.ProductId, pc.CategoryId });

            var defaultRate = activeRates.FirstOrDefault(r => r.CategoryId == null);

            decimal subtotal = 0;
            decimal taxTotal = 0;
            // Amount charged per tax rate, keyed by (rate name, rate) so the
            // SalesTaxDetail history rows map to what the Tax Totals report
            // groups on.
            var taxByRate = new Dictionary<string, decimal>();

            foreach (var item in request.Items)
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == request.WarehouseId);

                int available = inventory?.Quantity ?? 0;
                if (item.Quantity > available)
                    return BadRequest($"Insufficient stock for productId {item.ProductId}. Available: {available}, Requested: {item.Quantity}");

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

            // Create sales order header
            var header = new SalesOrderHeader
            {
                SaleDate = request.SaleDate ?? DateTime.Now,
                CustomerId = request.CustomerId,
                Subtotal = subtotal,
                TaxAmount = taxTotal,
                Total = total,
                PaymentMethodId = request.PaymentMethodId,
                SalespersonName = request.SalespersonName
            };
            _context.SalesOrderHeaders.Add(header);
            await _context.SaveChangesAsync();

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
                    Notes = $"Sale by {request.SalespersonName}"
                };
                _context.InventoryMovements.Add(movement);
                movements.Add(movement);

                // Update inventory quantity (this will also be done by PostInventoryMovement trigger, but we do it here for atomicity)
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == request.WarehouseId);
                if (inventory != null)
                {
                    inventory.Quantity = (inventory.Quantity ?? 0) - item.Quantity;
                    if (inventory.Quantity < 0) inventory.Quantity = 0;
                }
            }

            await _context.SaveChangesAsync();

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
