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
    public class PurchaseOrderHeaderController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public PurchaseOrderHeaderController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/PurchaseOrderHeader
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrderHeader>>> GetPurchaseOrderHeaders()
        {
            return await _context.PurchaseOrderHeaders.ToListAsync();
        }

        // GET: api/PurchaseOrderHeader/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseOrderHeader>> GetPurchaseOrderHeader(int id)
        {
            var purchaseOrderHeader = await _context.PurchaseOrderHeaders.FindAsync(id);

            if (purchaseOrderHeader == null)
            {
                return NotFound();
            }

            return purchaseOrderHeader;
        }

        // PUT: api/PurchaseOrderHeader/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPurchaseOrderHeader(int id, PurchaseOrderHeader purchaseOrderHeader)
        {
            if (id != purchaseOrderHeader.Id)
            {
                return BadRequest();
            }

            _context.Entry(purchaseOrderHeader).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderHeaderExists(id))
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

        // POST: api/PurchaseOrderHeader
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PurchaseOrderHeader>> PostPurchaseOrderHeader(PurchaseOrderHeader purchaseOrderHeader)
        {
            _context.PurchaseOrderHeaders.Add(purchaseOrderHeader);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPurchaseOrderHeader", new { id = purchaseOrderHeader.Id }, purchaseOrderHeader);
        }

        // DELETE: api/PurchaseOrderHeader/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseOrderHeader(int id)
        {
            var purchaseOrderHeader = await _context.PurchaseOrderHeaders.FindAsync(id);
            if (purchaseOrderHeader == null)
            {
                return NotFound();
            }

            _context.PurchaseOrderHeaders.Remove(purchaseOrderHeader);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PurchaseOrderHeaderExists(int id)
        {
            return _context.PurchaseOrderHeaders.Any(e => e.Id == id);
        }

        // POST: api/PurchaseOrderHeader/{id}/Complete
        [HttpPost("{id}/Complete")]
        public async Task<ActionResult<object>> CompletePurchaseOrder(int id, [FromBody] CompletePurchaseRequest request)
        {
            var header = await _context.PurchaseOrderHeaders.FindAsync(id);
            if (header == null)
                return NotFound("Purchase order not found");

            if (header.Status == "Completed")
                return BadRequest("Purchase order is already completed");

            // Validate warehouse exists
            var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId);
            if (warehouse == null)
                return BadRequest("Invalid warehouseId");

            // Get all details for this order
            var details = await _context.PurchaseOrderDetails
                .Where(d => d.PurchaseOrderId == id)
                .ToListAsync();

            if (!details.Any())
                return BadRequest("No items in this purchase order");

            // Create inventory movements and update quantities for each item
            var movements = new List<InventoryMovement>();

            foreach (var detail in details)
            {
                // Create inventory movement (IN)
                var movement = new InventoryMovement
                {
                    ProductId = detail.ProductId,
                    WarehouseId = request.WarehouseId,
                    MovementType = "IN",
                    Quantity = detail.Quantity,
                    MovementDate = request.ReceptionDate ?? DateTime.Now,
                    PurchaseOrderId = id,
                    Notes = request.Notes ?? ""
                };
                _context.InventoryMovements.Add(movement);
                movements.Add(movement);

                // Update inventory quantity (also handled by InventoryMovement Post trigger for consistency)
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == detail.ProductId && i.WarehouseId == request.WarehouseId);
                if (inventory != null)
                {
                    inventory.Quantity = (inventory.Quantity ?? 0) + detail.Quantity;
                }
            }

            // Update header status and reception date
            header.Status = "Completed";
            header.ReceptionDate = request.ReceptionDate ?? DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                purchaseOrderId = header.Id,
                header = header,
                movements = movements
            });
        }
    }

    public class CompletePurchaseRequest
    {
        public int WarehouseId { get; set; }
        public DateTime? ReceptionDate { get; set; }
        public string? Notes { get; set; }
    }
}
