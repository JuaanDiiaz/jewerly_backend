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
    public class SupplierProductController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public SupplierProductController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/SupplierProduct
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierProduct>>> GetSupplierProducts()
        {
            return await _context.SupplierProducts.ToListAsync();
        }

        // GET: api/SupplierProduct/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierProduct>> GetSupplierProduct(int id)
        {
            var supplierProduct = await _context.SupplierProducts.FindAsync(id);

            if (supplierProduct == null)
            {
                return NotFound();
            }

            return supplierProduct;
        }

        // PUT: api/SupplierProduct/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSupplierProduct(int id, SupplierProduct supplierProduct)
        {
            if (id != supplierProduct.Id)
            {
                return BadRequest();
            }

            _context.Entry(supplierProduct).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupplierProductExists(id))
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

        // POST: api/SupplierProduct
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SupplierProduct>> PostSupplierProduct(SupplierProduct supplierProduct)
        {
            _context.SupplierProducts.Add(supplierProduct);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSupplierProduct", new { id = supplierProduct.Id }, supplierProduct);
        }

        // DELETE: api/SupplierProduct/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplierProduct(int id)
        {
            var supplierProduct = await _context.SupplierProducts.FindAsync(id);
            if (supplierProduct == null)
            {
                return NotFound();
            }

            _context.SupplierProducts.Remove(supplierProduct);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SupplierProductExists(int id)
        {
            return _context.SupplierProducts.Any(e => e.Id == id);
        }
    }
}
