using BankApp_BusinessLogic;
using BankApp_Models;
using BankApp_Web.Translations;
using BankApp_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BankApp_Web.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<BankUser> _userManager;
        private readonly SignInManager<BankUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly AppDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Microsoft.Extensions.Localization.IStringLocalizer<BankApp_Web.Translations.SharedResource> _localizer;
        private readonly IRegistratieService _registratieService;

        public AccountController(
            UserManager<BankUser> userManager,
            SignInManager<BankUser> signInManager,
            ILogger<AccountController> logger,
            AppDbContext context,
            RoleManager<IdentityRole> roleManager,
            Microsoft.Extensions.Localization.IStringLocalizer<BankApp_Web.Translations.SharedResource> localizer,
            IRegistratieService registratieService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _roleManager = roleManager;
            _localizer = localizer;
            _registratieService = registratieService;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(BankApp_Web.Models.LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Zoek gebruiker eerst via email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Probeer ook via username
                user = await _userManager.FindByNameAsync(model.Email);
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, _localizer["Onjuiste email of wachtwoord."]);
                return View(model);
            }

            // Gebruik PasswordSignInAsync met de gevonden username
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName ?? user.Email ?? "", 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("Gebruiker ingelogd: {Email}", model.Email);
                
                // Redirect op basis van rol
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (await _userManager.IsInRoleAsync(user, "Medewerker"))
                {
                    return RedirectToAction("Index", "Medewerker");
                }
                
                return RedirectToAction("Index", "Home");
            }
            else if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, _localizer["Account is geblokkeerd."]);
            }
            else if (result.RequiresTwoFactor)
            {
                ModelState.AddModelError(string.Empty, _localizer["Twee-factor authenticatie vereist."]);
            }
            else
            {
                ModelState.AddModelError(string.Empty, _localizer["Onjuiste email of wachtwoord."]);
            }

            return View(model);
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(BankApp_Web.Models.RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Valideer formulier
            var validatieFouten = ValideerRegistratie(model);
            if (!string.IsNullOrEmpty(validatieFouten))
            {
                ModelState.AddModelError(string.Empty, validatieFouten);
                return View(model);
            }

            var resultaat = await _registratieService.RegistreerAsync(new RegistratieGegevens
            {
                Email = model.Email,
                Wachtwoord = model.Wachtwoord,
                Voornaam = model.Voornaam,
                Achternaam = model.Achternaam,
                Telefoonnummer = model.Telefoonnummer,
                Geboortedatum = model.Geboortedatum,
                Straat = model.Straat,
                Huisnummer = model.Huisnummer,
                Bus = model.Bus,
                Postcode = model.Postcode,
                Gemeente = model.Gemeente,
                Land = model.Land
            });

            if (!resultaat.Succes || resultaat.Gebruiker == null)
            {
                foreach (var fout in resultaat.Fouten)
                {
                    ModelState.AddModelError(string.Empty, fout);
                }
                return View(model);
            }

            // Log gebruiker automatisch in
            await _signInManager.SignInAsync(resultaat.Gebruiker, isPersistent: false);
            _logger.LogInformation("Nieuwe gebruiker geregistreerd: {Email}", model.Email);

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile
        [Authorize] // Overschrijft [AllowAnonymous] op class niveau
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Laad gebruiker met adres en rekeningen
            var gebruiker = await _context.Users
                .Include(u => u.Adres)
                .Include(u => u.Rekeningen)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (gebruiker == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Voornaam = gebruiker.Voornaam,
                Achternaam = gebruiker.Achternaam,
                Email = gebruiker.Email ?? "",
                Telefoonnummer = gebruiker.Telefoonnummer,
                Geboortedatum = gebruiker.Geboortedatum,
                Straat = gebruiker.Adres?.Straat ?? "",
                Huisnummer = gebruiker.Adres?.Huisnummer ?? "",
                Bus = gebruiker.Adres?.Bus,
                Postcode = gebruiker.Adres?.Postcode ?? "",
                Gemeente = gebruiker.Adres?.Gemeente ?? "",
                Land = gebruiker.Adres?.Land ?? "",
                Iban = gebruiker.Rekeningen?.FirstOrDefault(r => r.Deleted == DateTime.MaxValue)?.Iban ?? "Geen rekening gevonden"
            };

            return View(model);
        }

        // POST: Account/Profile
        [Authorize] // Overschrijft [AllowAnonymous] op class niveau
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Laad gebruiker met adres
                var gebruiker = await _context.Users
                    .Include(u => u.Adres)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (gebruiker == null)
                {
                    return NotFound();
                }

                // Update gebruiker gegevens
                gebruiker.Voornaam = model.Voornaam;
                gebruiker.Achternaam = model.Achternaam;
                gebruiker.Telefoonnummer = model.Telefoonnummer;
                gebruiker.Geboortedatum = model.Geboortedatum;

                // Update of maak adres aan
                if (gebruiker.Adres == null)
                {
                    gebruiker.Adres = new Adres();
                    _context.Adressen.Add(gebruiker.Adres);
                }

                gebruiker.Adres.Straat = model.Straat;
                gebruiker.Adres.Huisnummer = model.Huisnummer;
                gebruiker.Adres.Bus = string.IsNullOrWhiteSpace(model.Bus) ? null : model.Bus;
                gebruiker.Adres.Postcode = model.Postcode;
                gebruiker.Adres.Gemeente = model.Gemeente;
                gebruiker.Adres.Land = model.Land ?? "België";
                gebruiker.AdresId = gebruiker.Adres.Id;

                // Update wachtwoord als ingevuld
                if (!string.IsNullOrEmpty(model.NieuweWachtwoord))
                {
                    if (model.NieuweWachtwoord != model.WachtwoordBevestigen)
                    {
                        ModelState.AddModelError("WachtwoordBevestigen", _localizer["Wachtwoorden komen niet overeen."]);
                        return View(model);
                    }

                    var token = await _userManager.GeneratePasswordResetTokenAsync(gebruiker);
                    var result = await _userManager.ResetPasswordAsync(gebruiker, token, model.NieuweWachtwoord);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(model);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Profiel succesvol bijgewerkt."].ToString();
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij bijwerken profiel");
                ModelState.AddModelError("", _localizer["Er is een fout opgetreden bij het bijwerken van uw profiel."]);
                return View(model);
            }
        }

        // POST: Account/DeleteProfile
        [Authorize] // Overschrijft [AllowAnonymous] op class niveau
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Soft delete gebruiker
                user.Deleted = DateTime.Now;
                await _context.SaveChangesAsync();

                // Log uit
                await _signInManager.SignOutAsync();
                _logger.LogInformation("Gebruiker profiel verwijderd: {Email}", user.Email);

                TempData["SuccessMessage"] = _localizer["Uw profiel is succesvol verwijderd."].ToString();
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwijderen profiel");
                ModelState.AddModelError("", _localizer["Er is een fout opgetreden bij het verwijderen van uw profiel."]);
                return RedirectToAction("Profile");
            }
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Gebruiker uitgelogd");
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        private string ValideerRegistratie(BankApp_Web.Models.RegisterViewModel model)
        {
            var fouten = new StringBuilder();

            if (string.IsNullOrWhiteSpace(model.Voornaam) || model.Voornaam.Length < 2)
                fouten.AppendLine(_localizer["Voornaam moet minimaal 2 tekens bevatten."]);

            if (string.IsNullOrWhiteSpace(model.Achternaam) || model.Achternaam.Length < 2)
                fouten.AppendLine(_localizer["Achternaam moet minimaal 2 tekens bevatten."]);

            if (!IsGeldigEmail(model.Email))
                fouten.AppendLine(_localizer["Voer een geldig e-mailadres in."]);

            if (model.Wachtwoord.Length < 8)
                fouten.AppendLine(_localizer["Wachtwoord moet minimaal 8 tekens bevatten."]);
            else if (!HeeftHoofdletterEnCijfer(model.Wachtwoord))
                fouten.AppendLine(_localizer["Wachtwoord moet minimaal 1 hoofdletter en 1 cijfer bevatten."]);

            if (model.Wachtwoord != model.WachtwoordBevestigen)
                fouten.AppendLine(_localizer["Wachtwoorden komen niet overeen."]);

            if (string.IsNullOrWhiteSpace(model.Telefoonnummer))
                fouten.AppendLine(_localizer["Telefoonnummer is verplicht."]);
            else if (!Regex.IsMatch(model.Telefoonnummer.Replace(" ", "").Replace("+", ""), @"^\d+$"))
                fouten.AppendLine(_localizer["Telefoonnummer mag alleen cijfers bevatten."]);

            if (!model.Geboortedatum.HasValue)
                fouten.AppendLine(_localizer["Geboortedatum is verplicht."]);
            else if (!IsOuderDan18(model.Geboortedatum.Value))
                fouten.AppendLine(_localizer["Je moet minimaal 18 jaar oud zijn."]);

            if (string.IsNullOrWhiteSpace(model.Straat))
                fouten.AppendLine(_localizer["Straatnaam is verplicht."]);

            if (string.IsNullOrWhiteSpace(model.Huisnummer))
                fouten.AppendLine(_localizer["Huisnummer is verplicht."]);

            if (string.IsNullOrWhiteSpace(model.Postcode))
                fouten.AppendLine(_localizer["Postcode is verplicht."]);
            else if (!Regex.IsMatch(model.Postcode, @"^\d{4}$"))
                fouten.AppendLine(_localizer["Postcode moet 4 cijfers bevatten."]);

            if (string.IsNullOrWhiteSpace(model.Gemeente))
                fouten.AppendLine(_localizer["Gemeente is verplicht."]);

            return fouten.ToString();
        }

        private bool IsGeldigEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

        private bool IsOuderDan18(DateTime geboortedatum)
        {
            int leeftijd = DateTime.Now.Year - geboortedatum.Year;
            if (geboortedatum.Date > DateTime.Now.AddYears(-leeftijd))
                leeftijd--;
            return leeftijd >= 18;
        }

        private bool HeeftHoofdletterEnCijfer(string wachtwoord) =>
            Regex.IsMatch(wachtwoord, @"[A-Z]") && Regex.IsMatch(wachtwoord, @"\d");

        // GET: Account/ConfirmEmail
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"E-mail verificatie: gebruiker niet gevonden (ID: {userId})");
                ViewBag.ErrorMessage = "Gebruiker niet gevonden.";
                return View("Error");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation($"E-mail bevestigd voor gebruiker: {user.Email}");
                ViewBag.SuccessMessage = "Je e-mailadres is bevestigd! Je kunt nu inloggen.";
                return View("ConfirmEmail");
            }
            else
            {
                _logger.LogWarning($"E-mail verificatie mislukt voor gebruiker: {user.Email}");
                ViewBag.ErrorMessage = "E-mail verificatie mislukt. De link is mogelijk verlopen of ongeldig.";
                return View("Error");
            }
        }

        // GET: Account/ResendConfirmation
        [HttpGet]
        public IActionResult ResendConfirmation()
        {
            return View();
        }

        // POST: Account/ResendConfirmation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "E-mailadres is verplicht.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Voor veiligheid: toon altijd succes bericht (voorkomt email enumeration)
                ViewBag.SuccessMessage = "Als dit e-mailadres bestaat, is er een verificatie e-mail verstuurd.";
                return View();
            }

            if (user.EmailConfirmed)
            {
                ViewBag.InfoMessage = "Dit e-mailadres is al bevestigd.";
                return View();
            }

            // Genereer nieuwe verificatie token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, token = token },
                protocol: Request.Scheme);

            // E-mail verificatie is uitgeschakeld - gebruikers kunnen direct inloggen
            _logger.LogInformation($"E-mail verificatie aangevraagd voor: {email} (niet verstuurd - verificatie uitgeschakeld)");

            ViewBag.SuccessMessage = "Als dit e-mailadres bestaat, is er een verificatie e-mail verstuurd.";
            return View();
        }
    }
}
