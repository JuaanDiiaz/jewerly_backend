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
    public class PriceListDetailController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public PriceListDetailController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/PriceListDetail
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PriceListDetail>>> GetPriceListDetails()
        {
            return await _context.PriceListDetails.ToListAsync();
        }

        // GET: api/PriceListDetail/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PriceListDetail>> GetPriceListDetail(int id)
        {
            var priceListDetail = await _context.PriceListDetails.FindAsync(id);

            if (priceListDetail == null)
            {
                return NotFound();
            }

            return priceListDetail;
        }

        // PUT: api/PriceListDetail/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPriceListDetail(int id, PriceListDetail priceListDetail)
        {
            if (id != priceListDetail.Id)
            {
                return BadRequest();
            }

            _context.Entry(priceListDetail).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PriceListDetailExists(id))
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

        // POST: api/PriceListDetail
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PriceListDetail>> PostPriceListDetail(PriceListDetail priceListDetail)
        {
            _context.PriceListDetails.Add(priceListDetail);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPriceListDetail", new { id = priceListDetail.Id }, priceListDetail);
        }

        // DELETE: api/PriceListDetail/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePriceListDetail(int id)
        {
            var priceListDetail = await _context.PriceListDetails.FindAsync(id);
            if (priceListDetail == null)
            {
                return NotFound();
            }

            _context.PriceListDetails.Remove(priceListDetail);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PriceListDetailExists(int id)
        {
            return _context.PriceListDetails.Any(e => e.Id == id);
        }
    }
}
