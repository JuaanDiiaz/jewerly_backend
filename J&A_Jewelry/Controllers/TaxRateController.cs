using System;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using J_A_Jewelry.Services.Identity;
using J_A_Jewelry.Models;

namespace J_A_Jewelry.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Permissions.AdminData)]
    public class TaxRateController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<TaxRateController> _logger;

        public TaxRateController(IsetechcJewelryInventoryContext context,
            Microsoft.Extensions.Logging.ILogger<TaxRateController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/TaxRate
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaxRate>>> GetTaxRates()
        {
            return await _context.TaxRates.ToListAsync();
        }

        // GET: api/TaxRate/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaxRate>> GetTaxRate(int id)
        {
            var taxRate = await _context.TaxRates.FindAsync(id);

            if (taxRate == null)
            {
                return NotFound();
            }

            return taxRate;
        }

        // PUT: api/TaxRate/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTaxRate(int id, TaxRate taxRate)
        {
            if (id != taxRate.Id)
            {
                return BadRequest();
            }

            var validation = await ValidateTaxRate(taxRate);
            if (validation != null)
            {
                return validation;
            }

            // Only one active rate per scope: saving an active rate for a
            // category (or as the default) deactivates the others in that
            // same scope. Serializable isolation (plus the same rule inside
            // PostTaxRate) makes the deactivate-and-save step atomic — two
            // racing saves cannot both leave an active rate behind.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            if (taxRate.IsActive)
            {
                var others = await _context.TaxRates
                    .Where(r => r.Id != id && r.IsActive && r.CategoryId == taxRate.CategoryId)
                    .ToListAsync();
                foreach (var other in others) other.IsActive = false;
            }

            _context.Entry(taxRate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaxRateExists(id))
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

        // POST: api/TaxRate
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TaxRate>> PostTaxRate(TaxRate taxRate)
        {
            var validation = await ValidateTaxRate(taxRate);
            if (validation != null)
            {
                return validation;
            }

            // Only one active rate per scope (see PutTaxRate); serializable so
            // deactivate-and-insert is atomic against racing saves.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            if (taxRate.IsActive)
            {
                var others = await _context.TaxRates
                    .Where(r => r.IsActive && r.CategoryId == taxRate.CategoryId)
                    .ToListAsync();
                foreach (var other in others) other.IsActive = false;
            }

            _context.TaxRates.Add(taxRate);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "TaxRate created Id={Id} CategoryId={CategoryId} Rate={Rate} Active={IsActive}",
                taxRate.Id, taxRate.CategoryId, taxRate.Rate, taxRate.IsActive);

            return CreatedAtAction("GetTaxRate", new { id = taxRate.Id }, taxRate);
        }

        // DELETE: api/TaxRate/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaxRate(int id)
        {
            var taxRate = await _context.TaxRates.FindAsync(id);
            if (taxRate == null)
            {
                return NotFound();
            }

            _context.TaxRates.Remove(taxRate);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Shared POST/PUT checks: the category must exist when given, and the
        /// rate itself must be a sensible fraction (0..1, e.g. 0.16 for 16%).
        /// </summary>
        private async Task<ActionResult?> ValidateTaxRate(TaxRate taxRate)
        {
            if (taxRate.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == taxRate.CategoryId);
                if (!categoryExists)
                {
                    return BadRequest($"Invalid categoryId {taxRate.CategoryId}");
                }
            }

            if (taxRate.Rate is null or < 0 or > 1)
            {
                return BadRequest("Rate must be between 0 and 1 (e.g. 0.16 for 16%)");
            }

            return null;
        }

        private bool TaxRateExists(int id)
        {
            return _context.TaxRates.Any(e => e.Id == id);
        }
    }
}