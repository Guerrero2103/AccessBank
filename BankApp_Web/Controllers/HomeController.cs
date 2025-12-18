using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BankApp_Web.Controllers
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
            // Als gebruiker niet is ingelogd, toon welcome pagina
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return View();
            }

            // Redirect op basis van rol
            if (User.IsInRole("Klant"))
            {
                return RedirectToAction("Index", "Rekeningen");
            }
            else if (User.IsInRole("Medewerker"))
            {
                return RedirectToAction("Index", "Medewerker");
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            // Als gebruiker is ingelogd maar geen rol heeft, blijf op home pagina
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Taal wijzigen
        public IActionResult ChangeLanguage(string code, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(code)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
