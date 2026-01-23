using BankApp_Models;
using BankApp_Web.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Web.Controllers
{
    [AllowAnonymous]
    public class KlantendienstController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<BankUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public KlantendienstController(
            AppDbContext context,
            UserManager<BankUser> userManager,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: Klantendienst
        public IActionResult Index()
        {
            // Als gebruiker is ingelogd, vul email automatisch in
            if (User.Identity.IsAuthenticated)
            {
                var user = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                {
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserName = $"{user.Voornaam} {user.Achternaam}";
                }
            }
            return View();
        }

        // POST: Klantendienst
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(KlantendienstViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string? gebruikerId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.FindByNameAsync(User.Identity.Name);
                    gebruikerId = user?.Id;
                }

                var bericht = new KlantBericht
                {
                    Naam = model.Naam,
                    Email = model.Email,
                    Onderwerp = model.Onderwerp,
                    Bericht = model.Bericht,
                    Datum = DateTime.Now,
                    Status = "Nieuw",
                    GebruikerId = gebruikerId,
                    Deleted = DateTime.MaxValue
                };

                _context.KlantBerichten.Add(bericht);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = _localizer["Uw bericht is succesvol verzonden. We nemen zo spoedig mogelijk contact met u op."].ToString();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", _localizer["Er is een fout opgetreden bij het verzenden van uw bericht."]);
                return View(model);
            }
        }
    }

    public class KlantendienstViewModel
    {
        [Required(ErrorMessage = "Naam is verplicht")]
        public string Naam { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mail formaat")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Onderwerp is verplicht")]
        public string Onderwerp { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bericht is verplicht")]
        public string Bericht { get; set; } = string.Empty;
    }
}
