using Microsoft.AspNetCore.Mvc;
using KitaKo.Services;
using KitaKo.Models;

namespace KitaKo.Controllers
{
    public class HomeController : Controller
    {
        private readonly AuthService _authService;

        public HomeController(AuthService authService)
        {
            _authService = authService;
        }

        // Landing Page
        public IActionResult Index()
        {
            return View();
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }

        // Sales & Budget Page
        public IActionResult SalesTracker()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // Utang Logs Page
        public IActionResult UtangLogs()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // Expenses Page
        public IActionResult Expenses()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        //Go to Login
        //Go to Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //Login
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _authService.Login(model.EmailOrUsername, model.Password);

                if (user != null)
                {
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("Username", user.Username);

                    TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";
                    return RedirectToAction("Dashboard");
                }

                ModelState.AddModelError("", "Invalid email/username or password");
            }
            return View(model);
        }

        //Go to Signup
        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        //Signup
        [HttpPost]
        public IActionResult Signup(SignupViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("", "Passwords do not match");
                    return View(model);
                }

                var user = _authService.Register(model.Username, model.Email, model.Password);

                if (user != null)
                {
                    TempData["SuccessMessage"] = "Account created successfully! Please login.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", "Username or email already exists");
            }
            return View(model);
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index");
        }

        // GET: Profile
        public IActionResult Profile()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(userIdStr);
            var user = _authService.GetUserById(userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // GET: Edit Profile
        [HttpGet]
        public IActionResult EditProfile()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(userIdStr);
            var user = _authService.GetUserById(userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ProfileEditViewModel
            {
                Username = user.Username ?? string.Empty,
                StoreName = user.StoreName,
                ProfilePhotoUrl = user.ProfilePhotoUrl
            };

            return View(model);
        }



        //Edit Profile
        [HttpPost]
        public IActionResult EditProfile(ProfileEditViewModel model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(userIdStr);

            // Get current user data
            var currentUser = _authService.GetUserById(userId);
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            model.ProfilePhotoUrl = currentUser.ProfilePhotoUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //update username if it's provided and different
            string usernameToUpdate = string.IsNullOrWhiteSpace(model.Username)
                ? currentUser.Username
                : model.Username.Trim();

            //update store name if it's provided
            string storeNameToUpdate = model.StoreName ?? currentUser.StoreName ?? string.Empty;

            //update photo if it's provided
            string photoUrlToUpdate = currentUser.ProfilePhotoUrl ?? string.Empty; // Default to existing photo
            if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                try
                {
                    var photoUrl = _authService.SaveProfilePhoto(model.ProfilePhoto, userId);
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        photoUrlToUpdate = photoUrl;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(model.ProfilePhoto), ex.Message);
                    return View(model);
                }
            }

            //Update profile
            var success = _authService.UpdateProfile(userId, usernameToUpdate, storeNameToUpdate, photoUrlToUpdate);

            if (!success)
            {
                ModelState.AddModelError("", "Username already taken");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.CurrentPassword) != string.IsNullOrEmpty(model.NewPassword))
            {
                ModelState.AddModelError("", "Current password and new password must both be provided.");
                return View(model);
            }

            //BOTH current and new passwords must be provided
            if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                var passwordChanged = _authService.ChangePassword(userId, model.CurrentPassword, model.NewPassword);
                if (!passwordChanged)
                {
                    ModelState.AddModelError("", "Current password is incorrect");
                    //Reload the user data to show
                    var updatedUser = _authService.GetUserById(userId);
                    if (updatedUser != null)
                    {
                        model.Username = updatedUser.Username ?? string.Empty;
                        model.StoreName = updatedUser.StoreName;
                        model.ProfilePhotoUrl = updatedUser.ProfilePhotoUrl;
                    }
                    return View(model);
                }
            }

            //Updated with new username
            HttpContext.Session.SetString("Username", usernameToUpdate);
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // About
        public IActionResult About()
        {
            return View();
        }
    }
}
