using Microsoft.AspNetCore.Mvc;

namespace KitaKo.Controllers
{
    [ApiController]
    public abstract class AuthenticatedApiController : ControllerBase
    {
        protected bool TryGetCurrentUserId(out int userId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out userId);
        }
    }
}
