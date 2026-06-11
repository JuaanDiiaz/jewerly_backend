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
    public class InventoryMovementController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public InventoryMovementController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/InventoryMovement
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryMovement>>> GetInventoryMovements(
            [FromQuery] int? productId = null,
            [FromQuery] int? warehouseId = null,
            [FromQuery] string? movementType = null)
        {
            var query = _context.InventoryMovements.AsQueryable();

            if (productId.HasValue)
                query = query.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue)
                query = query.Where(m => m.WarehouseId == warehouseId.Value);
            if (!string.IsNullOrEmpty(movementType))
                query = query.Where(m => m.MovementType == movementType);

            return await query.ToListAsync();
        }

        // GET: api/InventoryMovement/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryMovement>> GetInventoryMovement(int id)
        {
            var inventoryMovement = await _context.InventoryMovements.FindAsync(id);

            if (inventoryMovement == null)
            {
                return NotFound();
            }

            return inventoryMovement;
        }

        // PUT: api/InventoryMovement/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInventoryMovement(int id, InventoryMovement inventoryMovement)
        {
            if (id != inventoryMovement.Id)
            {
                return BadRequest();
            }

            _context.Entry(inventoryMovement).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventoryMovementExists(id))
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

        // POST: api/InventoryMovement
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<InventoryMovement>> PostInventoryMovement(InventoryMovement inventoryMovement)
        {
            _context.InventoryMovements.Add(inventoryMovement);
            await _context.SaveChangesAsync();

            // Automatically update Inventory.Quantity based on movement type
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == inventoryMovement.ProductId && i.WarehouseId == inventoryMovement.WarehouseId);

            if (inventory != null && inventoryMovement.Quantity.HasValue)
            {
                switch (inventoryMovement.MovementType?.ToUpperInvariant())
                {
                    case "IN":
                        inventory.Quantity = (inventory.Quantity ?? 0) + inventoryMovement.Quantity;
                        break;
                    case "OUT":
                        inventory.Quantity = (inventory.Quantity ?? 0) - inventoryMovement.Quantity;
                        if (inventory.Quantity < 0) inventory.Quantity = 0;
                        break;
                    case "ADJUSTMENT":
                        inventory.Quantity = (inventory.Quantity ?? 0) + inventoryMovement.Quantity;
                        if (inventory.Quantity < 0) inventory.Quantity = 0;
                        break;
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction("GetInventoryMovement", new { id = inventoryMovement.Id }, inventoryMovement);
        }

        // DELETE: api/InventoryMovement/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryMovement(int id)
        {
            var inventoryMovement = await _context.InventoryMovements.FindAsync(id);
            if (inventoryMovement == null)
            {
                return NotFound();
            }

            _context.InventoryMovements.Remove(inventoryMovement);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool InventoryMovementExists(int id)
        {
            return _context.InventoryMovements.Any(e => e.Id == id);
        }
    }
}
