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
    public class CustomerPaymentController : ControllerBase
    {
        private readonly IsetechcJewelryInventoryContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<CustomerPaymentController> _logger;

        public CustomerPaymentController(IsetechcJewelryInventoryContext context,
            Microsoft.Extensions.Logging.ILogger<CustomerPaymentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/CustomerPayment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerPayment>>> GetCustomerPayments([FromQuery] int? salesOrderId = null)
        {
            var query = _context.CustomerPayments.AsQueryable();

            if (salesOrderId.HasValue)
                query = query.Where(p => p.SalesOrderId == salesOrderId.Value);

            return await query.ToListAsync();
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
        [Authorize(Policy = Permissions.Customers)]
        public async Task<IActionResult> PutCustomerPayment(int id, CustomerPayment customerPayment)
        {
            if (id != customerPayment.Id)
            {
                return BadRequest();
            }

            var amountError = ValidateAmount(customerPayment);
            if (amountError != null)
            {
                return amountError;
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
        [Authorize(Policy = Permissions.Customers)]
        public async Task<ActionResult<CustomerPayment>> PostCustomerPayment(CustomerPayment customerPayment)
        {
            var amountError = ValidateAmount(customerPayment);
            if (amountError != null)
            {
                return amountError;
            }

            // A payment is applied against the order's outstanding balance
            // (computed at read time by the CustomerBalances report) — so an
            // unusable amount here would corrupt that balance silently.
            if (customerPayment.CustomerId == null && customerPayment.SalesOrderId == null)
            {
                return BadRequest("Payment must reference a customer or a sales order");
            }

            _context.CustomerPayments.Add(customerPayment);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "CustomerPayment recorded Id={Id} CustomerId={CustomerId} SalesOrderId={SalesOrderId} Amount={Amount}",
                customerPayment.Id, customerPayment.CustomerId, customerPayment.SalesOrderId, customerPayment.Amount);

            return CreatedAtAction("GetCustomerPayment", new { id = customerPayment.Id }, customerPayment);
        }

        // DELETE: api/CustomerPayment/5
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Customers)]
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

        private static BadRequestObjectResult? ValidateAmount(CustomerPayment payment)
        {
            if (payment.Amount == null || payment.Amount <= 0)
            {
                return new BadRequestObjectResult("Payment amount must be greater than zero");
            }
            return null;
        }
    }
}