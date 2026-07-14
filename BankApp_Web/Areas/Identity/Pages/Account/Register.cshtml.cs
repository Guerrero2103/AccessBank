using BankApp_BusinessLogic;
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
        private readonly IRegistratieService _registratieService;

        public RegisterModel(
            UserManager<BankUser> userManager,
            IUserStore<BankUser> userStore,
            SignInManager<BankUser> signInManager,
            ILogger<RegisterModel> logger,
            IRegistratieService registratieService)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _registratieService = registratieService;
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
                var resultaat = await _registratieService.RegistreerAsync(new RegistratieGegevens
                {
                    Email = Input.Email,
                    Wachtwoord = Input.Password
                });

                if (resultaat.Succes && resultaat.Gebruiker != null)
                {
                    _logger.LogInformation("Gebruiker heeft een nieuw account aangemaakt met wachtwoord.");
                    await _signInManager.SignInAsync(resultaat.Gebruiker, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var fout in resultaat.Fouten)
                {
                    ModelState.AddModelError(string.Empty, fout);
                }
            }

            return Page();
        }

        private IUserEmailStore<BankUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("De standaard UI vereist een user store met email support.");
            }
            return (IUserEmailStore<BankUser>)_userStore;
        }
    }
}
