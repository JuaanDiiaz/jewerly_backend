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

        // GET: api/SalesOrderHeader
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesOrderHeader>>> GetSalesOrderHeaders()
        {
            return await _context.SalesOrderHeaders.ToListAsync();
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

            // Validate all items have sufficient stock and calculate total
            decimal total = 0;
            var itemValidations = new List<(int productId, int qty, decimal price, int available)>();

            foreach (var item in request.Items)
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == request.WarehouseId);

                int available = inventory?.Quantity ?? 0;
                if (item.Quantity > available)
                    return BadRequest($"Insufficient stock for productId {item.ProductId}. Available: {available}, Requested: {item.Quantity}");

                decimal itemTotal = item.Quantity * item.UnitPrice;
                total += itemTotal;
                itemValidations.Add((item.ProductId, item.Quantity, item.UnitPrice, available));
            }

            // Create sales order header
            var header = new SalesOrderHeader
            {
                SaleDate = request.SaleDate ?? DateTime.Now,
                CustomerId = request.CustomerId,
                Total = total,
                PaymentMethodId = request.PaymentMethodId,
                Notes = request.SalespersonName ?? ""
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

            return Ok(new
            {
                salesOrderId = header.Id,
                header = header,
                details = details,
                movements = movements
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
