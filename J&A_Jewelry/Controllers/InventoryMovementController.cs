using System;
using System.Linq;
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
    public class InventoryMovementController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<InventoryMovementController> _logger;

        public InventoryMovementController(IsetechcJewelryInventoryContext context,
            Microsoft.Extensions.Logging.ILogger<InventoryMovementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/InventoryMovement?page=1&page_size=50&from_date&to_date&productId&warehouseId&movementType
        [HttpGet]
        public async Task<ActionResult<PagedResult<InventoryMovement>>> GetInventoryMovements(
            [FromQuery(Name = "page")] int? page = null,
            [FromQuery(Name = "page_size")] int? pageSize = null,
            [FromQuery(Name = "from_date")] DateTime? fromDate = null,
            [FromQuery(Name = "to_date")] DateTime? toDate = null,
            [FromQuery(Name = "productId")] int? productId = null,
            [FromQuery(Name = "warehouseId")] int? warehouseId = null,
            [FromQuery(Name = "movementType")] string? movementType = null)
        {
            var safePage = Math.Max(1, page ?? 1);
            var safeSize = Math.Clamp(pageSize ?? 50, 1, 200);

            var query = _context.InventoryMovements.AsNoTracking().AsQueryable();
            if (productId.HasValue)
                query = query.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue)
                query = query.Where(m => m.WarehouseId == warehouseId.Value);
            if (!string.IsNullOrEmpty(movementType))
                query = query.Where(m => m.MovementType == movementType);
            if (fromDate.HasValue)
                query = query.Where(m => m.MovementDate >= fromDate);
            if (toDate.HasValue)
                query = query.Where(m => m.MovementDate < toDate.Value.AddDays(1));

            var result = new PagedResult<InventoryMovement>
            {
                Page = safePage,
                PageSize = safeSize,
                TotalCount = await query.CountAsync(),
                Data = await query
                    .OrderByDescending(m => m.Id)
                    .Skip((safePage - 1) * safeSize)
                    .Take(safeSize)
                    .ToListAsync()
            };

            return result;
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
        [Authorize(Policy = Permissions.Inventory)]
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
        [Authorize(Policy = Permissions.Inventory)]
        public async Task<ActionResult<InventoryMovement>> PostInventoryMovement(InventoryMovement inventoryMovement)
        {
            // Only typed, positive movements may touch stock. An unrecognized
            // type used to be recorded while applying nothing to stock,
            // desynchronizing the movement log from the inventory table.
            if (inventoryMovement.ProductId == null || inventoryMovement.WarehouseId == null)
            {
                return BadRequest("ProductId and WarehouseId are required");
            }
            var type = inventoryMovement.MovementType?.Trim().ToUpperInvariant();
            if (type is not ("IN" or "OUT" or "ADJUSTMENT"))
            {
                return BadRequest("MovementType must be IN, OUT, or ADJUSTMENT");
            }
            if (inventoryMovement.Quantity == null || inventoryMovement.Quantity == 0)
            {
                return BadRequest("Quantity cannot be zero");
            }
            if (!await _context.Products.AnyAsync(p => p.Id == inventoryMovement.ProductId))
            {
                return BadRequest($"Invalid productId {inventoryMovement.ProductId}");
            }
            if (!await _context.Warehouses.AnyAsync(w => w.Id == inventoryMovement.WarehouseId))
            {
                return BadRequest($"Invalid warehouseId {inventoryMovement.WarehouseId}");
            }
            // IN/OUT are directional and take a magnitude; ADJUSTMENT is a
            // signed delta (negative = shrinkage from a physical count).
            if (type != "ADJUSTMENT" && inventoryMovement.Quantity <= 0)
            {
                return BadRequest("Quantity must be positive for IN and OUT movements");
            }

            // Movement and stock change land together or not at all — posting
            // them in two saves used to leave a half-applied movement.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == inventoryMovement.ProductId && i.WarehouseId == inventoryMovement.WarehouseId);

            if (inventory == null)
            {
                if (type == "OUT")
                {
                    return BadRequest("No stock record exists for this product and warehouse, so nothing can be removed");
                }
                _context.Inventories.Add(new Inventory
                {
                    ProductId = inventoryMovement.ProductId,
                    WarehouseId = inventoryMovement.WarehouseId,
                    Quantity = inventoryMovement.Quantity,
                    Location = string.Empty,
                });
            }
            else
            {
                var current = inventory.Quantity ?? 0;
                inventory.Quantity = type switch
                {
                    "IN" => current + inventoryMovement.Quantity.Value,
                    "OUT" => current - inventoryMovement.Quantity.Value,
                    _ => current + inventoryMovement.Quantity.Value, // ADJUSTMENT
                };

                // Stock truth: refused rather than clamped, for the same
                // reason UpdateQuantity refuses below-zero results.
                if (inventory.Quantity < 0)
                {
                    _logger.LogWarning(
                        "Movement refused below-zero ProductId={ProductId} WarehouseId={WarehouseId} Type={Type} Quantity={Quantity} Current={Current}",
                        inventoryMovement.ProductId, inventoryMovement.WarehouseId,
                        type, inventoryMovement.Quantity, current);
                    return BadRequest(
                        $"Insufficient stock: {type} of {inventoryMovement.Quantity} for productId {inventoryMovement.ProductId} in warehouse {inventoryMovement.WarehouseId} would drop on-hand below zero (currently {current})");
                }
            }

            _context.InventoryMovements.Add(inventoryMovement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Movement committed Id={Id} ProductId={ProductId} WarehouseId={WarehouseId} Type={Type} Quantity={Quantity} NewStock={NewStock}",
                inventoryMovement.Id, inventoryMovement.ProductId, inventoryMovement.WarehouseId,
                type, inventoryMovement.Quantity, inventory?.Quantity);

            return CreatedAtAction("GetInventoryMovement", new { id = inventoryMovement.Id }, inventoryMovement);
        }

        // DELETE: api/InventoryMovement/5
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Inventory)]
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