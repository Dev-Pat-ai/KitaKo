using KitaKo.Models;
using KitaKo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KitaKo.Controllers
{
    [Route("api/[controller]")]
    public class SalesController : AuthenticatedApiController
    {
        private readonly SalesService _salesService;

        public SalesController(SalesService salesService)
        {
            _salesService = salesService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sale>>> GetSales()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return Ok(await _salesService.GetSalesAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Sale>> GetSale(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var sale = await _salesService.GetSaleAsync(userId, id);
            return sale == null ? NotFound() : Ok(sale);
        }

        [HttpPost]
        public async Task<ActionResult<Sale>> PostSale(SaleRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var sale = await _salesService.CreateSaleAsync(userId, request);
            return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, sale);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutSale(int id, SaleRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return await _salesService.UpdateSaleAsync(userId, id, request)
                ? NoContent()
                : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSale(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return await _salesService.DeleteSaleAsync(userId, id)
                ? NoContent()
                : NotFound();
        }
    }
}
