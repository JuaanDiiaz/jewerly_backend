using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public Task<IActionResult> PutSalesTaxDetail(int id, SalesTaxDetail salesTaxDetail)
        {
            return Task.FromResult<IActionResult>(LockedLedger());
        }

        // POST: api/SalesTaxDetail
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public Task<ActionResult<SalesTaxDetail>> PostSalesTaxDetail(SalesTaxDetail salesTaxDetail)
        {
            return Task.FromResult<ActionResult<SalesTaxDetail>>(LockedLedger());
        }

        // DELETE: api/SalesTaxDetail/5
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteSalesTaxDetail(int id)
        {
            return Task.FromResult<IActionResult>(LockedLedger());
        }

        private bool SalesTaxDetailExists(int id)
        {
            return _context.SalesTaxDetails.Any(e => e.Id == id);
        }

        /// <summary>
        /// The tax rows are the audit ledger: CompleteSale writes them from
        /// the resolved rates, and the Tax Totals report groups on TaxType.
        /// Hand-writing or deleting them from a client would let a sale's
        /// recorded tax be altered after the fact, so the writes are locked.
        /// </summary>
        private static ObjectResult LockedLedger()
        {
            return new ObjectResult("Sales tax rows are written automatically at checkout; use TaxRate settings to change rates")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
