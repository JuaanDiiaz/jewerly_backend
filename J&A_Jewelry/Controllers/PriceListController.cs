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
    public class PriceListController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public PriceListController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/PriceList
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PriceList>>> GetPriceLists()
        {
            return await _context.PriceLists.ToListAsync();
        }

        // GET: api/PriceList/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PriceList>> GetPriceList(int id)
        {
            var priceList = await _context.PriceLists.FindAsync(id);

            if (priceList == null)
            {
                return NotFound();
            }

            return priceList;
        }

        // PUT: api/PriceList/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPriceList(int id, PriceList priceList)
        {
            if (id != priceList.Id)
            {
                return BadRequest();
            }

            _context.Entry(priceList).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PriceListExists(id))
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

        // POST: api/PriceList
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PriceList>> PostPriceList(PriceList priceList)
        {
            _context.PriceLists.Add(priceList);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPriceList", new { id = priceList.Id }, priceList);
        }

        // DELETE: api/PriceList/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePriceList(int id)
        {
            var priceList = await _context.PriceLists.FindAsync(id);
            if (priceList == null)
            {
                return NotFound();
            }

            _context.PriceLists.Remove(priceList);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PriceListExists(int id)
        {
            return _context.PriceLists.Any(e => e.Id == id);
        }
    }
}
