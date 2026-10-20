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
    public class PurchaseOrderDetailController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public PurchaseOrderDetailController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/PurchaseOrderDetail
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrderDetail>>> GetPurchaseOrderDetails([FromQuery] int? purchaseOrderId = null)
        {
            var query = _context.PurchaseOrderDetails.AsQueryable();

            if (purchaseOrderId.HasValue)
                query = query.Where(d => d.PurchaseOrderId == purchaseOrderId.Value);

            return await query.ToListAsync();
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
        [Authorize(Policy = Permissions.Purchases)]
        public async Task<IActionResult> PutPurchaseOrderDetail(int id, PurchaseOrderDetail purchaseOrderDetail)
        {
            if (id != purchaseOrderDetail.Id)
            {
                return BadRequest();
            }

            var lineError = ValidateLine(purchaseOrderDetail);
            if (lineError != null)
            {
                return lineError;
            }

            // The line total is a derived value — never trust the client's.
            purchaseOrderDetail.Total = (purchaseOrderDetail.Quantity ?? 0) * (purchaseOrderDetail.UnitPrice ?? 0);

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
        [Authorize(Policy = Permissions.Purchases)]
        public async Task<ActionResult<PurchaseOrderDetail>> PostPurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            var lineError = ValidateLine(purchaseOrderDetail);
            if (lineError != null)
            {
                return lineError;
            }

            // The line total is a derived value — never trust the client's.
            purchaseOrderDetail.Total = (purchaseOrderDetail.Quantity ?? 0) * (purchaseOrderDetail.UnitPrice ?? 0);

            _context.PurchaseOrderDetails.Add(purchaseOrderDetail);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPurchaseOrderDetail", new { id = purchaseOrderDetail.Id }, purchaseOrderDetail);
        }

        // DELETE: api/PurchaseOrderDetail/5
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Purchases)]
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

        private static BadRequestObjectResult? ValidateLine(PurchaseOrderDetail detail)
        {
            if (detail.Quantity == null || detail.Quantity <= 0)
            {
                return new BadRequestObjectResult("Quantity must be at least 1");
            }
            if (detail.UnitPrice != null && detail.UnitPrice < 0)
            {
                return new BadRequestObjectResult("Unit price cannot be negative");
            }
            return null;
        }
    }
}