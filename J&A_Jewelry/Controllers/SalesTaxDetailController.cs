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
    public class SalesTaxDetailController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public SalesTaxDetailController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/SalesTaxDetail
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesTaxDetail>>> GetSalesTaxDetails()
        {
            return await _context.SalesTaxDetails.ToListAsync();
        }

        // GET: api/SalesTaxDetail/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SalesTaxDetail>> GetSalesTaxDetail(int id)
        {
            var salesTaxDetail = await _context.SalesTaxDetails.FindAsync(id);

            if (salesTaxDetail == null)
            {
                return NotFound();
            }

            return salesTaxDetail;
        }

        // PUT: api/SalesTaxDetail/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSalesTaxDetail(int id, SalesTaxDetail salesTaxDetail)
        {
            if (id != salesTaxDetail.Id)
            {
                return BadRequest();
            }

            _context.Entry(salesTaxDetail).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SalesTaxDetailExists(id))
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

        // POST: api/SalesTaxDetail
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SalesTaxDetail>> PostSalesTaxDetail(SalesTaxDetail salesTaxDetail)
        {
            _context.SalesTaxDetails.Add(salesTaxDetail);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSalesTaxDetail", new { id = salesTaxDetail.Id }, salesTaxDetail);
        }

        // DELETE: api/SalesTaxDetail/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSalesTaxDetail(int id)
        {
            var salesTaxDetail = await _context.SalesTaxDetails.FindAsync(id);
            if (salesTaxDetail == null)
            {
                return NotFound();
            }

            _context.SalesTaxDetails.Remove(salesTaxDetail);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SalesTaxDetailExists(int id)
        {
            return _context.SalesTaxDetails.Any(e => e.Id == id);
        }
    }
}
