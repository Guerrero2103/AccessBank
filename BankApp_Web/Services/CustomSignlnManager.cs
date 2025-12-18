using BankApp_Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BankApp_Web.Services
{
    // Custom SignInManager die email OF username accepteert bij login
    public class CustomSignInManager : SignInManager<BankUser>
    {

        private readonly UserManager<BankUser> _userManager;

        public CustomSignInManager(
            UserManager<BankUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<BankUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<BankUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<BankUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {

            _userManager = userManager;
        }

        // Override PasswordSignInAsync om email EN username te ondersteunen
        public override async Task<SignInResult> PasswordSignInAsync(string userNameOrEmail, string password, bool isPersistent, bool lockoutOnFailure)
        {
            // Probeer eerst als username
            // Zoek gebruiker op username
            var user = await _userManager.FindByNameAsync(userNameOrEmail);

            // Als niet gevonden, probeer als email
            if (user == null)
            {

                user = await _userManager.FindByEmailAsync(userNameOrEmail);
            }

            if (user == null)
            {

                return SignInResult.Failed;
            }

            return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        }
    }
}

