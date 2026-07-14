using BankApp_Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BankApp_BusinessLogic
{
    // Bundelt de stappen die op alle drie de registratiepaden identiek horen te zijn:
    // gebruiker aanmaken, rol "Klant" toekennen, zichtrekening + kaart aanmaken.
    public class RegistratieService : IRegistratieService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<BankUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IRekeningService _rekeningService;

        public RegistratieService(
            AppDbContext context,
            UserManager<BankUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IRekeningService rekeningService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _rekeningService = rekeningService;
        }

        public async Task<RegistratieResultaat> RegistreerAsync(RegistratieGegevens gegevens)
        {
            var resultaat = new RegistratieResultaat();

            var bestaandeGebruiker = await _userManager.FindByEmailAsync(gegevens.Email);
            if (bestaandeGebruiker != null)
            {
                resultaat.Fouten.Add("Er bestaat al een account met dit e-mailadres.");
                return resultaat;
            }

            Adres? adres = null;
            if (!string.IsNullOrWhiteSpace(gegevens.Straat))
            {
                adres = new Adres
                {
                    Straat = gegevens.Straat ?? string.Empty,
                    Huisnummer = gegevens.Huisnummer ?? string.Empty,
                    Bus = string.IsNullOrWhiteSpace(gegevens.Bus) ? null : gegevens.Bus,
                    Postcode = gegevens.Postcode ?? string.Empty,
                    Gemeente = gegevens.Gemeente ?? string.Empty,
                    Land = string.IsNullOrWhiteSpace(gegevens.Land) ? "België" : gegevens.Land
                };
                _context.Adressen.Add(adres);
                await _context.SaveChangesAsync();
            }

            var gebruiker = new BankUser
            {
                UserName = gegevens.Email,
                Email = gegevens.Email,
                EmailConfirmed = true,
                Voornaam = gegevens.Voornaam,
                Achternaam = gegevens.Achternaam,
                Telefoonnummer = gegevens.Telefoonnummer,
                Geboortedatum = gegevens.Geboortedatum ?? DateTime.MinValue,
                AdresId = adres?.Id,
                Adres = adres
            };

            var createResult = await _userManager.CreateAsync(gebruiker, gegevens.Wachtwoord);
            if (!createResult.Succeeded)
            {
                resultaat.Fouten.AddRange(createResult.Errors.Select(e => e.Description));
                return resultaat;
            }

            if (await _roleManager.RoleExistsAsync("Klant"))
            {
                await _userManager.AddToRoleAsync(gebruiker, "Klant");
            }

            await _rekeningService.MaakRekeningAanAsync(gebruiker.Id);

            try
            {
                string kaartNummer = await GenereerUniekKaartNummerAsync();
                var kaart = new Kaart
                {
                    KaartNummer = kaartNummer,
                    Status = KaartStatus.Actief,
                    GebruikerId = gebruiker.Id,
                    Deleted = DateTime.MaxValue
                };
                _context.Kaarten.Add(kaart);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Kaart is optioneel: registratie mag doorgaan zonder kaart
            }

            resultaat.Succes = true;
            resultaat.Gebruiker = gebruiker;
            return resultaat;
        }

        private async Task<string> GenereerUniekKaartNummerAsync()
        {
            string kaartNummer;
            int poging = 0;
            const int maxPogingen = 100;

            do
            {
                kaartNummer = GenereerKaartNummer();
                poging++;

                bool bestaatAl = await _context.Kaarten.AnyAsync(k => k.KaartNummer == kaartNummer);
                if (!bestaatAl)
                {
                    return kaartNummer;
                }
            } while (poging < maxPogingen);

            return GenereerKaartNummer() + "-" + DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 4));
        }

        // Kaartnummer formaat: XXXX-XXXX-XXXX-XXXX, cryptografisch gegenereerd
        private static string GenereerKaartNummer()
        {
            byte[] bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var sb = new StringBuilder();
            for (int i = 0; i < 4; i++)
            {
                if (i > 0) sb.Append("-");
                sb.Append(BitConverter.ToUInt16(bytes, i * 2).ToString("D4"));
            }
            return sb.ToString();
        }
    }
}
