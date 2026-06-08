using KitaKo.Models;
using KitaKo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KitaKo.Controllers
{
    [Route("api/[controller]")]
    public class UtangsController : AuthenticatedApiController
    {
        private readonly UtangsService _utangsService;

        public UtangsController(UtangsService utangsService)
        {
            _utangsService = utangsService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Utang>>> GetUtangs()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return Ok(await _utangsService.GetUtangsAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Utang>> GetUtang(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var utang = await _utangsService.GetUtangAsync(userId, id);
            return utang == null ? NotFound() : Ok(utang);
        }

        [HttpPost]
        public async Task<ActionResult<Utang>> PostUtang(UtangRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var utang = await _utangsService.CreateUtangAsync(userId, request);
            return CreatedAtAction(nameof(GetUtang), new { id = utang.Id }, utang);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUtang(int id, UtangRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return await _utangsService.UpdateUtangAsync(userId, id, request)
                ? NoContent()
                : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUtang(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return await _utangsService.DeleteUtangAsync(userId, id)
                ? NoContent()
                : NotFound();
        }
    }
}
