using BankApp_Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BankApp_WPF
{
    // De WPF-pagina's bouwen UserManager/RoleManager buiten dependency injection om
    // (er is geen ASP.NET Core-hostcontainer beschikbaar). Deze factory zorgt dat ze
    // altijd een echte ILookupNormalizer en logger krijgen: zonder normalizer matcht
    // FindByEmailAsync/RoleExistsAsync niet met de in de database opgeslagen
    // genormaliseerde waarden, en zonder logger gooit Identity een
    // ArgumentNullException zodra het intern een mislukte actie probeert te loggen
    // (bv. een foute inlogpoging via CheckPasswordAsync).
    internal static class IdentityManagerFactory
    {
        public static UserManager<BankUser> CreateUserManager(AppDbContext context) =>
            new UserManager<BankUser>(
                new UserStore<BankUser>(context),
                null!,
                new PasswordHasher<BankUser>(),
                null!,
                null!,
                new UpperInvariantLookupNormalizer(),
                null!,
                null!,
                NullLogger<UserManager<BankUser>>.Instance);

        public static RoleManager<IdentityRole> CreateRoleManager(AppDbContext context) =>
            new RoleManager<IdentityRole>(
                new RoleStore<IdentityRole>(context),
                null!,
                new UpperInvariantLookupNormalizer(),
                null!,
                NullLogger<RoleManager<IdentityRole>>.Instance);
    }
}
