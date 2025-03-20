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
    }
}
