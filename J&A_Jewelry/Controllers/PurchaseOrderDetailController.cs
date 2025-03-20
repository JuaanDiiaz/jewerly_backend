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
    public class PurchaseOrderDetailController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public PurchaseOrderDetailController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/PurchaseOrderDetail
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrderDetail>>> GetPurchaseOrderDetails()
        {
            return await _context.PurchaseOrderDetails.ToListAsync();
        }

        // GET: api/PurchaseOrderDetail/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseOrderDetail>> GetPurchaseOrderDetail(int id)
        {
            var purchaseOrderDetail = await _context.PurchaseOrderDetails.FindAsync(id);

            if (purchaseOrderDetail == null)
            {
                return NotFound();
            }

            return purchaseOrderDetail;
        }

        // PUT: api/PurchaseOrderDetail/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPurchaseOrderDetail(int id, PurchaseOrderDetail purchaseOrderDetail)
        {
            if (id != purchaseOrderDetail.Id)
            {
                return BadRequest();
            }

            _context.Entry(purchaseOrderDetail).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderDetailExists(id))
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

        // POST: api/PurchaseOrderDetail
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PurchaseOrderDetail>> PostPurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            _context.PurchaseOrderDetails.Add(purchaseOrderDetail);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPurchaseOrderDetail", new { id = purchaseOrderDetail.Id }, purchaseOrderDetail);
        }

        // DELETE: api/PurchaseOrderDetail/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseOrderDetail(int id)
        {
            var purchaseOrderDetail = await _context.PurchaseOrderDetails.FindAsync(id);
            if (purchaseOrderDetail == null)
            {
                return NotFound();
            }

            _context.PurchaseOrderDetails.Remove(purchaseOrderDetail);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PurchaseOrderDetailExists(int id)
        {
            return _context.PurchaseOrderDetails.Any(e => e.Id == id);
        }
    }
}
