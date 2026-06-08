using KitaKo.Models;
using KitaKo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KitaKo.Controllers
{
    [Route("api/[controller]")]
    public class SettingsController : AuthenticatedApiController
    {
        private readonly FinancialSettingsService _financialSettingsService;

        public SettingsController(FinancialSettingsService financialSettingsService)
        {
            _financialSettingsService = financialSettingsService;
        }

        [HttpGet("financial")]
        public async Task<ActionResult<UserFinancialSettings>> GetFinancialSettings()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return Ok(await _financialSettingsService.GetOrCreateSettingsAsync(userId));
        }

        [HttpPut("financial")]
        public async Task<ActionResult<UserFinancialSettings>> UpdateFinancialSettings(FinancialSettingsRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return Ok(await _financialSettingsService.UpdateSettingsAsync(userId, request));
        }
    }
}
