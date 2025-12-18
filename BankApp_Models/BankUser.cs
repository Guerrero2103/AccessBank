using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Models
{
    // Identity Framework - BankUser erft van IdentityUser (vereiste)
    public class BankUser : IdentityUser
    {
        // Extra eigenschappen voor de gebruiker (minstens één extra eigenschap vereist)
        [Required]
        [MaxLength(50)]
        public string Voornaam { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Achternaam { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Telefoonnummer { get; set; } = string.Empty;

        [Required]
        public DateTime Geboortedatum { get; set; }

        // Databank - Soft delete verplicht
        public DateTime Deleted { get; set; } = DateTime.MaxValue;

        // Databank - Adres koppeling
        public int? AdresId { get; set; }
        public Adres? Adres { get; set; }

        // Databank - Adres wrappers voor bestaande UI (backward compatibility)
        [NotMapped]
        public string Straatnaam
        {
            get => Adres?.Straat ?? string.Empty;
            set
            {
                EnsureAdres();
                Adres!.Straat = value;
            }
        }

        [NotMapped]
        public string Huisnummer
        {
            get => Adres?.Huisnummer ?? string.Empty;
            set
            {
                EnsureAdres();
                Adres!.Huisnummer = value;
            }
        }

        [NotMapped]
        public string? Bus
        {
            get => Adres?.Bus;
            set
            {
                EnsureAdres();
                Adres!.Bus = string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        [NotMapped]
        public string Postcode
        {
            get => Adres?.Postcode ?? string.Empty;
            set
            {
                EnsureAdres();
                Adres!.Postcode = value;
            }
        }

        [NotMapped]
        public string Gemeente
        {
            get => Adres?.Gemeente ?? string.Empty;
            set
            {
                EnsureAdres();
                Adres!.Gemeente = value;
            }
        }

        [NotMapped]
        public string Land
        {
            get => Adres?.Land ?? string.Empty;
            set
            {
                EnsureAdres();
                Adres!.Land = value;
            }
        }

        // Entity Framework - Navigatie-eigenschappen
        public ICollection<Rekening> Rekeningen { get; set; } = new List<Rekening>();
        public ICollection<Kaart> Kaarten { get; set; } = new List<Kaart>();
        public ICollection<Transactie> Transacties { get; set; } = new List<Transactie>();
        public ICollection<LogEntry> Logs { get; set; } = new List<LogEntry>();

        private void EnsureAdres()
        {
            if (Adres == null)
            {
                Adres = new Adres();
            }
        }

        // Seeder methode voor Identity Framework (zoals Agenda-master)
        public static async Task Seeder(AppDbContext context)
        {
            // Gebruik dezelfde PasswordHasher configuratie als de applicatie
            var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<BankUser>();

            using var userManager = new Microsoft.AspNetCore.Identity.UserManager<BankUser>(
                new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<BankUser>(context),
                null!, passwordHasher,
                null!, null!, null!, null!, null!, null!);

            using var roleManager = new Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>(
                new Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<Microsoft.AspNetCore.Identity.IdentityRole>(context),
                null!, null!, null!, null!);

            // Voeg rollen toe (Identity Roles) via RoleManager
            if (!await roleManager.RoleExistsAsync("Klant"))
            {
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Klant"));
            }
            if (!await roleManager.RoleExistsAsync("Medewerker"))
            {
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Medewerker"));
            }
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));
            }

            // Voeg test gebruikers toe (maak opnieuw aan als ze niet bestaan)
            var adres1 = await context.Adressen.FindAsync(1);
            var adres2 = await context.Adressen.FindAsync(2);
            var adres3 = await context.Adressen.FindAsync(3);

            // Test gebruiker 1: Jan Peeters (Klant)
            var existingUser1 = await userManager.FindByEmailAsync("jan.peeters@example.com");
            if (existingUser1 == null)
            {
                var user1 = new BankUser
                {
                    UserName = "jan.peeters",
                    Email = "jan.peeters@example.com",
                    EmailConfirmed = true,
                    Voornaam = "Jan",
                    Achternaam = "Peeters",
                    Telefoonnummer = "0478123456",
                    Geboortedatum = new DateTime(1990, 4, 15),
                    AdresId = 1,
                    Adres = adres1
                };

                var result1 = await userManager.CreateAsync(user1, "Password123!");
                if (result1.Succeeded)
                {
                    await userManager.AddToRoleAsync(user1, "Klant");
                    await context.SaveChangesAsync();
                }
                else
                {
                    // Log fouten voor debugging
                    var errors = string.Join(", ", result1.Errors.Select(e => e.Description));
                    Console.WriteLine($"Fout bij aanmaken gebruiker 1: {errors}");
                }
            }
            else
            {
                // Reset wachtwoord als gebruiker al bestaat
                var token = await userManager.GeneratePasswordResetTokenAsync(existingUser1);
                var resetResult = await userManager.ResetPasswordAsync(existingUser1, token, "Password123!");
                if (resetResult.Succeeded)
                {
                    await context.SaveChangesAsync();
                }
            }

            // Test gebruiker 2: Sarah Janssens (Medewerker)
            var existingUser2 = await userManager.FindByEmailAsync("sarah.janssens@example.com");
            if (existingUser2 == null)
            {
                var user2 = new BankUser
                {
                    UserName = "sarah.janssens",
                    Email = "sarah.janssens@example.com",
                    EmailConfirmed = true,
                    Voornaam = "Sarah",
                    Achternaam = "Janssens",
                    Telefoonnummer = "0498765432",
                    Geboortedatum = new DateTime(1985, 10, 2),
                    AdresId = 2,
                    Adres = adres2
                };

                var result2 = await userManager.CreateAsync(user2, "Password123!");
                if (result2.Succeeded)
                {
                    await userManager.AddToRoleAsync(user2, "Medewerker");
                    await context.SaveChangesAsync();
                }
                else
                {
                    // Log fouten voor debugging
                    var errors = string.Join(", ", result2.Errors.Select(e => e.Description));
                    Console.WriteLine($"Fout bij aanmaken gebruiker 2: {errors}");
                }
            }
            else
            {
                // Reset wachtwoord als gebruiker al bestaat
                var token = await userManager.GeneratePasswordResetTokenAsync(existingUser2);
                var resetResult = await userManager.ResetPasswordAsync(existingUser2, token, "Password123!");
                if (resetResult.Succeeded)
                {
                    await context.SaveChangesAsync();
                }
            }

            // Test gebruiker 3: Admin
            var existingAdmin = await userManager.FindByEmailAsync("admin@bankapp.local");
            if (existingAdmin == null)
            {
                var admin = new BankUser
                {
                    UserName = "admin",
                    Email = "admin@bankapp.local",
                    EmailConfirmed = true,
                    Voornaam = "Admin",
                    Achternaam = "Beheerder",
                    Telefoonnummer = "0412345678",
                    Geboortedatum = new DateTime(1975, 6, 25),
                    AdresId = 3,
                    Adres = adres3
                };

                var result3 = await userManager.CreateAsync(admin, "Admin123!");
                if (result3.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    await context.SaveChangesAsync();
                }
                else
                {
                    // Log fouten voor debugging
                    var errors = string.Join(", ", result3.Errors.Select(e => e.Description));
                    Console.WriteLine($"Fout bij aanmaken admin: {errors}");
                }
            }
            else
            {
                // Reset wachtwoord als gebruiker al bestaat
                var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
                var resetResult = await userManager.ResetPasswordAsync(existingAdmin, token, "Admin123!");
                if (resetResult.Succeeded)
                {
                    await context.SaveChangesAsync();
                }
            }

            // Zorg dat alle wijzigingen worden opgeslagen
            await context.SaveChangesAsync();
        }
    }
}