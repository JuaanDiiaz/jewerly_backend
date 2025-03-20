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
    public class CustomerPaymentController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;

        public CustomerPaymentController(IsetechcJewelryInventoryContext context)
        {
            _context = context;
        }

        // GET: api/CustomerPayment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerPayment>>> GetCustomerPayments()
        {
            return await _context.CustomerPayments.ToListAsync();
        }

        // GET: api/CustomerPayment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerPayment>> GetCustomerPayment(int id)
        {
            var customerPayment = await _context.CustomerPayments.FindAsync(id);

            if (customerPayment == null)
            {
                return NotFound();
            }

            return customerPayment;
        }

        // PUT: api/CustomerPayment/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomerPayment(int id, CustomerPayment customerPayment)
        {
            if (id != customerPayment.Id)
            {
                return BadRequest();
            }

            _context.Entry(customerPayment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerPaymentExists(id))
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

        // POST: api/CustomerPayment
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CustomerPayment>> PostCustomerPayment(CustomerPayment customerPayment)
        {
            _context.CustomerPayments.Add(customerPayment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomerPayment", new { id = customerPayment.Id }, customerPayment);
        }

        // DELETE: api/CustomerPayment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomerPayment(int id)
        {
            var customerPayment = await _context.CustomerPayments.FindAsync(id);
            if (customerPayment == null)
            {
                return NotFound();
            }

            _context.CustomerPayments.Remove(customerPayment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CustomerPaymentExists(int id)
        {
            return _context.CustomerPayments.Any(e => e.Id == id);
        }
    }
}
