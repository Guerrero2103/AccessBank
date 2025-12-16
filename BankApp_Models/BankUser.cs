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
            using var userManager = new Microsoft.AspNetCore.Identity.UserManager<BankUser>(
                new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<BankUser>(context),
                null!, new Microsoft.AspNetCore.Identity.PasswordHasher<BankUser>(),
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

            // Voeg gebruikers toe
            if (!context.Users.Any())
            {
                var adres1 = await context.Adressen.FindAsync(1);
                var adres2 = await context.Adressen.FindAsync(2);
                var adres3 = await context.Adressen.FindAsync(3);

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

                await userManager.CreateAsync(user1, "Password123!");
                await userManager.CreateAsync(user2, "Password123!");
                await userManager.CreateAsync(admin, "Admin123!");

                await userManager.AddToRoleAsync(user1, "Klant");
                await userManager.AddToRoleAsync(user2, "Medewerker");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}

