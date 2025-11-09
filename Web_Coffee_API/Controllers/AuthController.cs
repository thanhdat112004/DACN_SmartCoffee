using Microsoft.AspNetCore.Mvc;

namespace Web_Coffee_API.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.IsAuthenticated = false;
            return View("~/Views/Auth/Login.cshtml");
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.IsAuthenticated = false;
            return View("~/Views/Auth/Register.cshtml");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewBag.IsAuthenticated = false;
            return View("~/Views/Auth/ForgotPassword.cshtml");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            ViewBag.IsAuthenticated = false;
            return View("~/Views/Auth/VerifyOtp.cshtml");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            ViewBag.IsAuthenticated = true;
            ViewBag.UserName = "Coffee Lover";
            return View("~/Views/Auth/Profile.cshtml");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Placeholder: redirect to login until backend logic is implemented.
            return RedirectToAction(nameof(Login));
        }
    }
}

