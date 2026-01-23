using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BankApp_Web.Controllers
{
    public class LanguagesController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (string.IsNullOrEmpty(culture))
            {
                culture = "nl"; // Default naar Nederlands
            }

            // Valideer dat culture een geldige waarde is
            var supportedCultures = new[] { "nl", "en", "fr" };
            if (!supportedCultures.Contains(culture))
            {
                culture = "nl";
            }

            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
            
            // Cookie opties voor development (HTTP) en production (HTTPS)
            var cookieOptions = new CookieOptions 
            { 
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true, // Belangrijk voor GDPR compliance
                HttpOnly = false, // Moet false zijn zodat JavaScript het kan lezen
                SameSite = SameSiteMode.Lax, // Toestaan cross-site requests
                Secure = false // In development (HTTP) moet dit false zijn
            };

            // In production, gebruik Secure = true
            // Maar in development (localhost met HTTPS) moet Secure ook true zijn
            cookieOptions.Secure = Request.IsHttps;

            // Verwijder oude cookie eerst met dezelfde opties
            var deleteOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            };
            Response.Cookies.Delete(CookieRequestCultureProvider.DefaultCookieName, deleteOptions);
            
            // Voeg nieuwe cookie toe
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                cookieValue,
                cookieOptions
            );

            // Zorg dat returnUrl niet leeg is en valideer het
            if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/")
            {
                returnUrl = Url.Content("~/");
            }

            // Valideer returnUrl - moet een lokale URL zijn
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Content("~/");
            }

            // Verwijder alle query parameters (vooral _lang en _t) om accumulatie te voorkomen
            var cleanUrl = returnUrl;
            if (cleanUrl.Contains("?"))
            {
                cleanUrl = cleanUrl.Substring(0, cleanUrl.IndexOf("?"));
            }
            
            // Verwijder ook hash (#) als die er is
            if (cleanUrl.Contains("#"))
            {
                cleanUrl = cleanUrl.Substring(0, cleanUrl.IndexOf("#"));
            }
            
            // Forceer cache control headers om cookie refresh te garanderen
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            // Gebruik alleen de base URL zonder query parameters
            // De cookie bevat al de taal informatie, dus query parameters zijn niet nodig
            return Redirect(cleanUrl);
        }
    }
}
