using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Web_Coffee_API.Models;

namespace Web_Coffee_API.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About() => View("~/Views/Home/about.cshtml");

        public IActionResult Blog() => View("~/Views/Home/blog.cshtml");

        public IActionResult BlogSingle() => View("~/Views/Home/blog-single.cshtml");

        public IActionResult Cart() => View("~/Views/Home/cart.cshtml");

        public IActionResult Checkout() => View("~/Views/Home/checkout.cshtml");

        public IActionResult Contact() => View("~/Views/Home/contact.cshtml");

        public IActionResult Menu() => View("~/Views/Home/menu.cshtml");

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ProductSingle() => View("~/Views/Home/product-single.cshtml");

        public IActionResult Services() => View("~/Views/Home/services.cshtml");

        public IActionResult Shop() => View("~/Views/Home/shop.cshtml");

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
