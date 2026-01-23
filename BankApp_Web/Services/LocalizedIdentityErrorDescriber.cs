using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using BankApp_Web.Translations;

namespace BankApp_Web.Services
{
    // Custom IdentityErrorDescriber voor meertalige foutmeldingen
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public override IdentityError DefaultError()
        {
            return new IdentityError
            {
                Code = nameof(DefaultError),
                Description = _localizer["Er is een onbekende fout opgetreden."]
            };
        }

        public override IdentityError ConcurrencyFailure()
        {
            return new IdentityError
            {
                Code = nameof(ConcurrencyFailure),
                Description = _localizer["Optimistische concurrency fout, het object is gewijzigd."]
            };
        }

        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description = _localizer["Wachtwoord is onjuist."]
            };
        }

        public override IdentityError InvalidToken()
        {
            return new IdentityError
            {
                Code = nameof(InvalidToken),
                Description = _localizer["Ongeldige token."]
            };
        }

        public override IdentityError LoginAlreadyAssociated()
        {
            return new IdentityError
            {
                Code = nameof(LoginAlreadyAssociated),
                Description = _localizer["Een gebruiker met deze login bestaat al."]
            };
        }

        public override IdentityError InvalidUserName(string? userName)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                Description = _localizer["Gebruikersnaam '{0}' is ongeldig, kan alleen letters of cijfers bevatten.", userName ?? ""]
            };
        }

        public override IdentityError InvalidEmail(string? email)
        {
            return new IdentityError
            {
                Code = nameof(InvalidEmail),
                Description = _localizer["E-mailadres '{0}' is ongeldig.", email ?? ""]
            };
        }

        public override IdentityError DuplicateUserName(string? userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = _localizer["Gebruikersnaam '{0}' is al in gebruik.", userName ?? ""]
            };
        }

        public override IdentityError DuplicateEmail(string? email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = _localizer["E-mailadres '{0}' is al in gebruik.", email ?? ""]
            };
        }

        public override IdentityError InvalidRoleName(string? role)
        {
            return new IdentityError
            {
                Code = nameof(InvalidRoleName),
                Description = _localizer["Rolnaam '{0}' is ongeldig.", role ?? ""]
            };
        }

        public override IdentityError DuplicateRoleName(string? role)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateRoleName),
                Description = _localizer["Rolnaam '{0}' is al in gebruik.", role ?? ""]
            };
        }

        public override IdentityError UserAlreadyHasPassword()
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyHasPassword),
                Description = _localizer["Gebruiker heeft al een wachtwoord ingesteld."]
            };
        }

        public override IdentityError UserLockoutNotEnabled()
        {
            return new IdentityError
            {
                Code = nameof(UserLockoutNotEnabled),
                Description = _localizer["Lockout is niet ingeschakeld voor deze gebruiker."]
            };
        }

        public override IdentityError UserAlreadyInRole(string? role)
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyInRole),
                Description = _localizer["Gebruiker heeft al de rol '{0}'.", role ?? ""]
            };
        }

        public override IdentityError UserNotInRole(string? role)
        {
            return new IdentityError
            {
                Code = nameof(UserNotInRole),
                Description = _localizer["Gebruiker heeft niet de rol '{0}'.", role ?? ""]
            };
        }

        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = _localizer["Wachtwoord moet minimaal {0} tekens lang zijn.", length]
            };
        }

        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = _localizer["Wachtwoord moet minimaal één niet-alfanumeriek teken bevatten."]
            };
        }

        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = _localizer["Wachtwoord moet minimaal één cijfer ('0'-'9') bevatten."]
            };
        }

        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = _localizer["Wachtwoord moet minimaal één kleine letter ('a'-'z') bevatten."]
            };
        }

        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = _localizer["Wachtwoord moet minimaal één hoofdletter ('A'-'Z') bevatten."]
            };
        }
    }
}
