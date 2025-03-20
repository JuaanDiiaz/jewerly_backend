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
        public async Task<ActionResult<IEnumerable<InventoryMovement>>> GetInventoryMovements()
        {
            return await _context.InventoryMovements.ToListAsync();
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
