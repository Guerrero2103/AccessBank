using BankApp_Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BankApp_Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<BankUser> _signInManager;
        private readonly UserManager<BankUser> _userManager;
        private readonly IUserStore<BankUser> _userStore;
        private readonly IUserEmailStore<BankUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public RegisterModel(
            UserManager<BankUser> userManager,
            IUserStore<BankUser> userStore,
            SignInManager<BankUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "Het {0} moet minimaal {2} en maximaal {1} tekens lang zijn.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Wachtwoord")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Bevestig wachtwoord")]
            [Compare("Password", ErrorMessage = "Het wachtwoord en bevestigingswachtwoord komen niet overeen.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                user.EmailConfirmed = true; // Geen email verificatie nodig

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Gebruiker heeft een nieuw account aangemaakt met wachtwoord.");

                    // Geef gebruiker automatisch de rol "Klant"
                    if (await _roleManager.RoleExistsAsync("Klant"))
                    {
                        await _userManager.AddToRoleAsync(user, "Klant");
                        _logger.LogInformation($"Rol 'Klant' toegekend aan gebruiker {user.Email}");
                    }
                    else
                    {
                        _logger.LogWarning("Rol 'Klant' bestaat niet in de database!");
                    }

                    // Wacht tot gebruiker is opgeslagen
                    await _context.SaveChangesAsync();
                    await _context.Entry(user).ReloadAsync();

                    // Automatisch een zichtrekening aanmaken
                    var nieuweRekening = new Rekening
                    {
                        Iban = "BE" + DateTime.Now.Ticks.ToString().Substring(0, 10),
                        Saldo = 0.0m,
                        GebruikerId = user.Id,
                        Deleted = DateTime.MaxValue // Soft-delete: niet verwijderd
                    };

                    _context.Rekeningen.Add(nieuweRekening);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Rekening aangemaakt: {nieuweRekening.Iban} voor gebruiker {user.Id}");

                    // Automatisch een kaart aanmaken voor de nieuwe gebruiker
                    try
                    {
                        string kaartNummer = GenereerUniekKaartNummer();
                        var nieuweKaart = new Kaart
                        {
                            KaartNummer = kaartNummer,
                            Status = KaartStatus.Actief,
                            GebruikerId = user.Id,
                            Deleted = DateTime.MaxValue // Soft-delete: niet verwijderd
                        };

                        _context.Kaarten.Add(nieuweKaart);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Kaart aangemaakt: {kaartNummer} voor gebruiker {user.Id}");
                    }
                    catch (Exception kaartEx)
                    {
                        _logger.LogWarning(kaartEx, $"Fout bij aanmaken kaart: {kaartEx.Message}");
                        // Kaart is optioneel, registratie kan doorgaan
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private BankUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<BankUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Kan geen instantie maken van '{nameof(BankUser)}'. " +
                    $"Zorg ervoor dat '{nameof(BankUser)}' geen abstracte klasse is en een parameterloze constructor heeft.");
            }
        }

        private IUserEmailStore<BankUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("De standaard UI vereist een user store met email support.");
            }
            return (IUserEmailStore<BankUser>)_userStore;
        }

        // Genereer uniek kaartnummer (formaat: XXXX-XXXX-XXXX-XXXX)
        private string GenereerKaartNummer()
        {
            // Gebruik DateTime.Ticks als seed voor betere randomisatie
            Random random = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
            string kaartNummer = "";

            // Genereer 4 groepen van 4 cijfers
            for (int i = 0; i < 4; i++)
            {
                if (i > 0) kaartNummer += "-";
                kaartNummer += random.Next(1000, 10000).ToString();
            }

            return kaartNummer;
        }

        // Genereer uniek kaartnummer en controleer of het al bestaat
        private string GenereerUniekKaartNummer()
        {
            string kaartNummer;
            int maxPogingen = 100; // Maximaal 100 pogingen om uniek nummer te vinden
            int poging = 0;

            do
            {
                kaartNummer = GenereerKaartNummer();
                poging++;

                // Controleer of kaartnummer al bestaat
                bool bestaatAl = _context.Kaarten.Any(k => k.KaartNummer == kaartNummer);
                if (!bestaatAl)
                {
                    return kaartNummer;
                }
            } while (poging < maxPogingen);

            // Als na 100 pogingen nog geen uniek nummer gevonden, voeg timestamp toe
            return GenereerKaartNummer() + "-" + DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 4));
        }
    }
}
