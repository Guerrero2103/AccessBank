using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BankApp_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Als gebruiker niet is ingelogd, toon welcome pagina
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return View();
            }

            // Als gebruiker is ingelogd, laad saldo data voor klanten
            if (User.IsInRole("Klant"))
            {
                try
                {
                    string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
                    
                    // Haal totaal saldo op
                    var totaalSaldo = await _context.Rekeningen
                        .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                        .SumAsync(r => r.Saldo);

                    // Haal eerste rekening op voor IBAN display
                    var eersteRekening = await _context.Rekeningen
                        .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                        .FirstOrDefaultAsync();

                    ViewBag.TotaalSaldo = totaalSaldo;
                    ViewBag.EersteRekening = eersteRekening;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fout bij ophalen saldo voor homepage");
                    ViewBag.TotaalSaldo = 0.0m;
                    ViewBag.EersteRekening = null;
                }
                
                return View();
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
