using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using BankApp_Web.Translations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Web.Controllers
{
    [Authorize(Roles = "Admin,Medewerker")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<BankUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, UserManager<BankUser> userManager, RoleManager<IdentityRole> roleManager, IStringLocalizer<SharedResource> localizer, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _localizer = localizer;
            _logger = logger;
        }

        // GET: Admin - Overzicht gebruikers
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            var gebruikers = _context.Users
                .Where(u => u.Deleted == DateTime.MaxValue)
                .Include(u => u.Adres)
                .AsQueryable();

            // Zoek functionaliteit
            ViewData["CurrentFilter"] = searchString;
            if (!string.IsNullOrEmpty(searchString))
            {
                gebruikers = gebruikers.Where(u => 
                    u.Email.Contains(searchString) ||
                    u.Voornaam.Contains(searchString) ||
                    u.Achternaam.Contains(searchString) ||
                    u.Telefoonnummer.Contains(searchString));
            }

            // Sortering
            ViewData["CurrentSort"] = sortOrder;
            ViewData["EmailSortParm"] = string.IsNullOrEmpty(sortOrder) ? "email_desc" : "";
            ViewData["NaamSortParm"] = sortOrder == "Naam" ? "naam_desc" : "Naam";
            ViewData["TelefoonSortParm"] = sortOrder == "Telefoon" ? "telefoon_desc" : "Telefoon";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";

            switch (sortOrder)
            {
                case "email_desc":
                    gebruikers = gebruikers.OrderByDescending(u => u.Email);
                    break;
                case "Naam":
                    gebruikers = gebruikers.OrderBy(u => u.Voornaam).ThenBy(u => u.Achternaam);
                    break;
                case "naam_desc":
                    gebruikers = gebruikers.OrderByDescending(u => u.Voornaam).ThenByDescending(u => u.Achternaam);
                    break;
                case "Telefoon":
                    gebruikers = gebruikers.OrderBy(u => u.Telefoonnummer);
                    break;
                case "telefoon_desc":
                    gebruikers = gebruikers.OrderByDescending(u => u.Telefoonnummer);
                    break;
                case "Status":
                    gebruikers = gebruikers.OrderBy(u => u.LockoutEnd);
                    break;
                case "status_desc":
                    gebruikers = gebruikers.OrderByDescending(u => u.LockoutEnd);
                    break;
                default:
                    gebruikers = gebruikers.OrderBy(u => u.Email);
                    break;
            }

            return View(await gebruikers.ToListAsync());
        }

        // GET: Admin/Gebruikers/Details/5
        public async Task<IActionResult> GebruikerDetails(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users
                .Include(u => u.Adres)
                .Include(u => u.Rekeningen)
                .Include(u => u.Kaarten)
                .FirstOrDefaultAsync(m => m.Id == id && m.Deleted == DateTime.MaxValue);

            if (gebruiker == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(gebruiker);
            ViewBag.Roles = roles;

            return View(gebruiker);
        }

        // GET: Admin/Gebruikers/Edit/5
        public async Task<IActionResult> EditGebruiker(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users
                .Include(u => u.Adres)
                .FirstOrDefaultAsync(m => m.Id == id && m.Deleted == DateTime.MaxValue);

            if (gebruiker == null)
            {
                return NotFound();
            }

            var model = new BankApp_Web.Models.ProfileViewModel
            {
                Voornaam = gebruiker.Voornaam,
                Achternaam = gebruiker.Achternaam,
                Email = gebruiker.Email ?? "",
                Telefoonnummer = gebruiker.Telefoonnummer,
                Geboortedatum = gebruiker.Geboortedatum,
                Straat = gebruiker.Adres?.Straat ?? "",
                Huisnummer = gebruiker.Adres?.Huisnummer ?? "",
                Bus = gebruiker.Adres?.Bus ?? "",
                Postcode = gebruiker.Adres?.Postcode ?? "",
                Gemeente = gebruiker.Adres?.Gemeente ?? "",
                Land = gebruiker.Adres?.Land ?? ""
            };

            ViewBag.GebruikerId = id;
            return View(model);
        }

        // POST: Admin/Gebruikers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGebruiker(string id, BankApp_Web.Models.ProfileViewModel model)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users
                .Include(u => u.Adres)
                .FirstOrDefaultAsync(u => u.Id == id && u.Deleted == DateTime.MaxValue);

            if (gebruiker == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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
                        await _context.SaveChangesAsync(); // Sla eerst op om ID te krijgen
                    }

                    gebruiker.Adres.Straat = model.Straat;
                    gebruiker.Adres.Huisnummer = model.Huisnummer;
                    gebruiker.Adres.Bus = string.IsNullOrWhiteSpace(model.Bus) ? null : model.Bus;
                    gebruiker.Adres.Postcode = model.Postcode;
                    gebruiker.Adres.Gemeente = model.Gemeente;
                    gebruiker.Adres.Land = model.Land ?? "België";
                    
                    // Zorg dat AdresId correct is gekoppeld
                    gebruiker.AdresId = gebruiker.Adres.Id;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Gebruiker succesvol bijgewerkt.";
                    return RedirectToAction(nameof(GebruikerDetails), new { id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Er is een fout opgetreden: {ex.Message}");
                }
            }

            ViewBag.GebruikerId = id;
            return View(model);
        }

        // GET: Admin/Gebruikers/Blokkeer/5
        public async Task<IActionResult> BlokkeerGebruiker(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users.FindAsync(id);
            if (gebruiker == null || gebruiker.Deleted != DateTime.MaxValue)
            {
                return NotFound();
            }

            return View(gebruiker);
        }

        // POST: Admin/Gebruikers/Blokkeer/5
        [HttpPost, ActionName("BlokkeerGebruiker")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlokkeerGebruikerConfirmed(string id)
        {
            var gebruiker = await _context.Users.FindAsync(id);
            if (gebruiker != null)
            {
                gebruiker.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                gebruiker.LockoutEnabled = true;
                await _userManager.UpdateAsync(gebruiker);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Gebruikers/DeBlokkeer/5
        public async Task<IActionResult> DeBlokkeerGebruiker(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users.FindAsync(id);
            if (gebruiker == null || gebruiker.Deleted != DateTime.MaxValue)
            {
                return NotFound();
            }

            gebruiker.LockoutEnd = null;
            gebruiker.LockoutEnabled = false;
            await _userManager.UpdateAsync(gebruiker);

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Rollen
        public async Task<IActionResult> Rollen(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users.FindAsync(id);
            if (gebruiker == null || gebruiker.Deleted != DateTime.MaxValue)
            {
                return NotFound();
            }

            var gebruikerRoles = await _userManager.GetRolesAsync(gebruiker);
            var alleRoles = _context.Roles.ToList();

            ViewBag.GebruikerId = id;
            ViewBag.GebruikerNaam = gebruiker.UserName;
            ViewBag.GebruikerRoles = gebruikerRoles;
            ViewBag.AlleRoles = alleRoles;

            return View();
        }

        // POST: Admin/Rollen/Toevoegen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoegRolToe(string gebruikerId, string rolNaam)
        {
            var gebruiker = await _context.Users.FindAsync(gebruikerId);
            if (gebruiker == null)
            {
                return NotFound();
            }

            // Een gebruiker kan maar één rol hebben
            // Verwijder eerst alle bestaande rollen
            var huidigeRoles = await _userManager.GetRolesAsync(gebruiker);
            if (huidigeRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(gebruiker, huidigeRoles);
            }

            // Voeg de nieuwe rol toe
            if (!await _userManager.IsInRoleAsync(gebruiker, rolNaam))
            {
                await _userManager.AddToRoleAsync(gebruiker, rolNaam);
            }

            return RedirectToAction(nameof(Rollen), new { id = gebruikerId });
        }

        // POST: Admin/Rollen/Verwijderen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerwijderRol(string gebruikerId, string rolNaam)
        {
            var gebruiker = await _context.Users.FindAsync(gebruikerId);
            if (gebruiker == null)
            {
                return NotFound();
            }

            var huidigeRoles = await _userManager.GetRolesAsync(gebruiker);
            
            // Een gebruiker moet altijd minstens één rol hebben
            // Als dit de laatste rol is, geef dan automatisch "Klant" rol
            if (huidigeRoles.Count == 1)
            {
                // Verwijder de huidige rol
                await _userManager.RemoveFromRoleAsync(gebruiker, rolNaam);
                // Geef automatisch "Klant" rol
                if (!await _userManager.IsInRoleAsync(gebruiker, "Klant"))
                {
                    await _userManager.AddToRoleAsync(gebruiker, "Klant");
                }
            }
            else
            {
                // Verwijder alleen de rol als er meerdere rollen zijn
                if (await _userManager.IsInRoleAsync(gebruiker, rolNaam))
                {
                    await _userManager.RemoveFromRoleAsync(gebruiker, rolNaam);
                }
            }

            return RedirectToAction(nameof(Rollen), new { id = gebruikerId });
        }

        // GET: Admin/NieuweGebruiker
        public IActionResult NieuweGebruiker()
        {
            // Wis eventuele oude TempData berichten
            TempData.Remove("SuccessMessage");
            TempData.Remove("ErrorMessage");
            return View();
        }

        // POST: Admin/NieuweGebruiker
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NieuweGebruiker(BankApp_Web.Models.RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Valideer leeftijd
            if (model.Geboortedatum == null || !IsOuderDan18(model.Geboortedatum.Value))
            {
                ModelState.AddModelError(nameof(model.Geboortedatum), "Gebruiker moet minimaal 18 jaar oud zijn.");
                return View(model);
            }

            // Valideer postcode
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Postcode, @"^\d{4}$"))
            {
                ModelState.AddModelError(nameof(model.Postcode), "Postcode moet 4 cijfers bevatten.");
                return View(model);
            }

            // Controleer of email al bestaat
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Er bestaat al een account met dit e-mailadres.");
                return View(model);
            }

            // Maak adres aan
            var adres = new Adres
            {
                Straat = model.Straat,
                Huisnummer = model.Huisnummer,
                Bus = string.IsNullOrWhiteSpace(model.Bus) ? null : model.Bus,
                Postcode = model.Postcode,
                Gemeente = model.Gemeente,
                Land = model.Land ?? "België"
            };
            _context.Adressen.Add(adres);
            await _context.SaveChangesAsync();

            // Maak gebruiker aan
            var gebruiker = new BankUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                Voornaam = model.Voornaam,
                Achternaam = model.Achternaam,
                Telefoonnummer = model.Telefoonnummer,
                Geboortedatum = model.Geboortedatum.Value,
                AdresId = adres.Id,
                Adres = adres,
                Deleted = DateTime.MaxValue
            };

            var result = await _userManager.CreateAsync(gebruiker, model.Wachtwoord);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Voeg rol "Klant" toe
            if (!await _roleManager.RoleExistsAsync("Klant"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Klant"));
            }
            await _userManager.AddToRoleAsync(gebruiker, "Klant");

            // Maak rekening aan
            string nieuweIban = "BE" + DateTime.Now.Ticks.ToString().Substring(0, 10);
            while (_context.Rekeningen.Any(r => r.Iban == nieuweIban && r.Deleted == DateTime.MaxValue))
            {
                nieuweIban = "BE" + DateTime.Now.Ticks.ToString().Substring(0, 10);
            }

            var nieuweRekening = new Rekening
            {
                Iban = nieuweIban,
                Saldo = 0.0m,
                GebruikerId = gebruiker.Id,
                Deleted = DateTime.MaxValue
            };
            _context.Rekeningen.Add(nieuweRekening);
            await _context.SaveChangesAsync();

            // Maak kaart aan
            try
            {
                string kaartNummer = GenereerUniekKaartNummer();
                var nieuweKaart = new Kaart
                {
                    KaartNummer = kaartNummer,
                    Status = KaartStatus.Actief,
                    GebruikerId = gebruiker.Id,
                    Deleted = DateTime.MaxValue
                };
                _context.Kaarten.Add(nieuweKaart);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error maar ga door
                _logger.LogError(ex, "Fout bij aanmaken kaart voor gebruiker");
            }

            TempData["UserCreated"] = _localizer["Gebruiker succesvol aangemaakt."].ToString();
            return RedirectToAction(nameof(Index));
        }

        private bool IsOuderDan18(DateTime geboortedatum)
        {
            int leeftijd = DateTime.Now.Year - geboortedatum.Year;
            if (geboortedatum.Date > DateTime.Now.AddYears(-leeftijd))
                leeftijd--;
            return leeftijd >= 18;
        }

        private string GenereerUniekKaartNummer()
        {
            string kaartNummer;
            int maxPogingen = 100;
            int poging = 0;

            do
            {
                kaartNummer = GenereerKaartNummer();
                poging++;

                bool bestaatAl = _context.Kaarten.Any(k => k.KaartNummer == kaartNummer && k.Deleted == DateTime.MaxValue);
                if (!bestaatAl)
                {
                    return kaartNummer;
                }
            } while (poging < maxPogingen);

            return GenereerKaartNummer() + "-" + DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 4));
        }

        private string GenereerKaartNummer()
        {
            byte[] bytes = new byte[8];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < 4; i++)
            {
                if (i > 0) sb.Append("-");
                sb.Append(System.BitConverter.ToUInt16(bytes, i * 2).ToString("D4"));
            }
            return sb.ToString();
        }
    }
}
