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
    public class SalesOrderDetailController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public SalesOrderDetailController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/SalesOrderDetail
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesOrderDetail>>> GetSalesOrderDetails([FromQuery] int? salesOrderId = null)
        {
            var query = _context.SalesOrderDetails.AsQueryable();

            if (salesOrderId.HasValue)
                query = query.Where(d => d.SalesOrderId == salesOrderId.Value);

            return await query.ToListAsync();
        }

        // GET: api/SalesOrderDetail/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SalesOrderDetail>> GetSalesOrderDetail(int id)
        {
            var salesOrderDetail = await _context.SalesOrderDetails.FindAsync(id);

            if (salesOrderDetail == null)
            {
                return NotFound();
            }

            return salesOrderDetail;
        }

        // PUT: api/SalesOrderDetail/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.Sales)]
        public async Task<IActionResult> PutSalesOrderDetail(int id, SalesOrderDetail salesOrderDetail)
        {
            if (id != salesOrderDetail.Id)
            {
                return BadRequest();
            }

            var lineError = ValidateLine(salesOrderDetail);
            if (lineError != null)
            {
                return lineError;
            }

            // The line total is a derived value — never trust the client's.
            salesOrderDetail.Total = (salesOrderDetail.Quantity ?? 0) * (salesOrderDetail.UnitPrice ?? 0);

            _context.Entry(salesOrderDetail).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SalesOrderDetailExists(id))
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

        // POST: api/SalesOrderDetail
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Policy = Permissions.Sales)]
        public async Task<ActionResult<SalesOrderDetail>> PostSalesOrderDetail(SalesOrderDetail salesOrderDetail)
        {
            var lineError = ValidateLine(salesOrderDetail);
            if (lineError != null)
            {
                return lineError;
            }

            // The line total is a derived value — never trust the client's.
            salesOrderDetail.Total = (salesOrderDetail.Quantity ?? 0) * (salesOrderDetail.UnitPrice ?? 0);

            _context.SalesOrderDetails.Add(salesOrderDetail);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSalesOrderDetail", new { id = salesOrderDetail.Id }, salesOrderDetail);
        }

        // DELETE: api/SalesOrderDetail/5
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Sales)]
        public async Task<IActionResult> DeleteSalesOrderDetail(int id)
        {
            var salesOrderDetail = await _context.SalesOrderDetails.FindAsync(id);
            if (salesOrderDetail == null)
            {
                return NotFound();
            }

            _context.SalesOrderDetails.Remove(salesOrderDetail);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SalesOrderDetailExists(int id)
        {
            return _context.SalesOrderDetails.Any(e => e.Id == id);
        }

        private static BadRequestObjectResult? ValidateLine(SalesOrderDetail detail)
        {
            if ((detail.Quantity ?? 0) <= 0)
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