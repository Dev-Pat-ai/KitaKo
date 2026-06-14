using KitaKo.Models;
using KitaKo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KitaKo.Controllers
{
    [Route("api/[controller]")]
    public class InventoryController : AuthenticatedApiController
    {
        private readonly InventoryService _inventoryService;

        public InventoryController(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetInventory()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return Ok(await _inventoryService.GetInventoryAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryItem>> GetInventoryItem(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var item = await _inventoryService.GetInventoryItemAsync(userId, id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<InventoryItem>> PostInventoryItem(InventoryItemRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var item = await _inventoryService.CreateInventoryItemAsync(userId, request);
                return CreatedAtAction(nameof(GetInventoryItem), new { id = item.Id }, item);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InventoryItem>> PutInventoryItem(int id, InventoryItemRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var item = await _inventoryService.UpdateInventoryItemAsync(userId, id, request);
                return item == null ? NotFound() : Ok(item);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/sell")]
        public async Task<ActionResult<InventorySale>> SellInventoryItem(int id, InventorySaleRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            try
            {
                return Ok(await _inventoryService.SellInventoryItemAsync(userId, id, request));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}