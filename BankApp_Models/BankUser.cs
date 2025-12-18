using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Models
{
    // Identity Framework - DbContext afgeleid van IdentityDbContext (vereiste)
    public class AppDbContext : IdentityDbContext<BankUser>
    {
        // Tabellen (DbSets)
        public DbSet<Adres> Adressen { get; set; }
        public DbSet<Rekening> Rekeningen { get; set; }
        public DbSet<Transactie> Transacties { get; set; }
        public DbSet<Kaart> Kaarten { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<KlantBericht> KlantBerichten { get; set; }

        // Constructor voor WPF (parameterless)
        public AppDbContext()
        {
            // Database.EnsureCreated() verwijderd - gebruik migraties i.p.v.
        }

        // Constructor voor ASP.NET Core / MAUI (met options)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // SQLite configuratie (alleen voor WPF parameterless constructor)
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string solutionPath = Path.GetFullPath(Path.Combine(basePath, @"..\..\..\..\"));
                string dbPath = Path.Combine(solutionPath, "BankApp_Models", "bankapp.db");

                Console.WriteLine($"[DB PATH] {dbPath}");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        // Modelconfiguratie + seeding
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Databank - Soft-delete filters
            modelBuilder.Entity<BankUser>().HasQueryFilter(u => u.Deleted == DateTime.MaxValue);
            modelBuilder.Entity<Adres>().HasQueryFilter(a => a.Deleted == DateTime.MaxValue);
            modelBuilder.Entity<Rekening>().HasQueryFilter(r => r.Deleted == DateTime.MaxValue);
            modelBuilder.Entity<Kaart>().HasQueryFilter(k => k.Deleted == DateTime.MaxValue);
            modelBuilder.Entity<Transactie>().HasQueryFilter(t => t.Deleted == DateTime.MaxValue);
            modelBuilder.Entity<KlantBericht>().HasQueryFilter(kb => kb.Deleted == DateTime.MaxValue);

            // === RELATIES ===
            modelBuilder.Entity<BankUser>()
                .HasOne(u => u.Adres)
                .WithOne(a => a.Gebruiker)
                .HasForeignKey<BankUser>(u => u.AdresId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Rekening>()
                .HasOne(r => r.Gebruiker)
                .WithMany(u => u.Rekeningen)
                .HasForeignKey(r => r.GebruikerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Kaart>()
                .HasOne(k => k.Gebruiker)
                .WithMany(u => u.Kaarten)
                .HasForeignKey(k => k.GebruikerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KlantBericht>()
                .HasOne(kb => kb.Gebruiker)
                .WithMany()
                .HasForeignKey(kb => kb.GebruikerId)
                .OnDelete(DeleteBehavior.SetNull);

            // === ADRESSEN SEEDEN ===
            modelBuilder.Entity<Adres>().HasData(
                new Adres { Id = 1, Straat = "Kerkstraat", Huisnummer = "12", Bus = "A", Postcode = "2000", Gemeente = "Antwerpen", Land = "België", Deleted = DateTime.MaxValue },
                new Adres { Id = 2, Straat = "Stationslaan", Huisnummer = "45", Bus = null, Postcode = "3000", Gemeente = "Leuven", Land = "België", Deleted = DateTime.MaxValue },
                new Adres { Id = 3, Straat = "Marktplein", Huisnummer = "1", Bus = null, Postcode = "1000", Gemeente = "Brussel", Land = "België", Deleted = DateTime.MaxValue }
            );

            // Gebruikers seeding gebeurt via BankUser.Seeder() methode (zie BankUser.cs)

            // Rekeningen seeding gebeurt via Seeder() methode (na users zijn aangemaakt)

            // Transacties seeding gebeurt via Seeder() methode (na users zijn aangemaakt)

            // Kaarten seeding gebeurt via Seeder() methode (na users zijn aangemaakt)

            // === LOGGING SEEDEN ===
            modelBuilder.Entity<LogEntry>().HasData(
                new LogEntry
                {
                    Id = 1,
                    Message = "Seed voltooid",
                    LogLevel = "Information",
                    Application = "BankApp",
                    GebruikerId = null,
                    TimeStamp = new DateTime(2024, 1, 1)
                }
            );
        }

        // Seeder methode voor Identity Framework (zoals Agenda-master)
        // Deze wordt gebruikt vanuit WPF
        public static async Task Seeder(AppDbContext context)
        {
            await BankUser.Seeder(context);
        }

        // Seeder methode met Dependency Injection (voor ASP.NET Core)
        public static async Task SeederWithDI(
            AppDbContext context,
            Microsoft.AspNetCore.Identity.UserManager<BankUser> userManager,
            Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager,
            ILogger logger)
        {
            // Seed rollen
            logger.LogInformation("Seeding rollen...");
            foreach (var roleName in new[] { "Klant", "Medewerker", "Admin" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Rol '{roleName}' aangemaakt");
                    }
                    else
                    {
                        logger.LogError($"Fout bij aanmaken rol '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // Seed adressen als ze nog niet bestaan
            if (!context.Adressen.Any())
            {
                logger.LogInformation("Seeding adressen...");
                context.Adressen.AddRange(
                    new Adres { Id = 1, Straat = "Kerkstraat", Huisnummer = "12", Bus = "A", Postcode = "2000", Gemeente = "Antwerpen", Land = "België", Deleted = DateTime.MaxValue },
                    new Adres { Id = 2, Straat = "Stationslaan", Huisnummer = "45", Bus = null, Postcode = "3000", Gemeente = "Leuven", Land = "België", Deleted = DateTime.MaxValue },
                    new Adres { Id = 3, Straat = "Marktplein", Huisnummer = "1", Bus = null, Postcode = "1000", Gemeente = "Brussel", Land = "België", Deleted = DateTime.MaxValue }
                );
                await context.SaveChangesAsync();
                logger.LogInformation("Adressen aangemaakt");
            }

            // Seed test gebruikers
            logger.LogInformation("Seeding test gebruikers...");
            var testAdres1 = await context.Adressen.FindAsync(1);
            var testAdres2 = await context.Adressen.FindAsync(2);
            var testAdres3 = await context.Adressen.FindAsync(3);

            // Test gebruiker 1: Jan Peeters (Klant)
            var user1Email = "jan.peeters@example.com";
            var testUser1 = await userManager.FindByEmailAsync(user1Email);
            if (testUser1 == null)
            {
                testUser1 = new BankUser
                {
                    UserName = "jan.peeters",
                    Email = user1Email,
                    EmailConfirmed = true,
                    Voornaam = "Jan",
                    Achternaam = "Peeters",
                    Telefoonnummer = "0478123456",
                    Geboortedatum = new DateTime(1990, 4, 15),
                    AdresId = 1,
                    Adres = testAdres1
                };

                var result = await userManager.CreateAsync(testUser1, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser1, "Klant");
                    logger.LogInformation($"Gebruiker '{user1Email}' aangemaakt met rol Klant");
                }
                else
                {
                    logger.LogError($"Fout bij aanmaken '{user1Email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Reset wachtwoord
                var token = await userManager.GeneratePasswordResetTokenAsync(testUser1);
                var resetResult = await userManager.ResetPasswordAsync(testUser1, token, "Password123!");
                if (resetResult.Succeeded)
                {
                    logger.LogInformation($"Wachtwoord gereset voor '{user1Email}'");
                }
            }

            // Test gebruiker 2: Sarah Janssens (Medewerker)
            var user2Email = "sarah.janssens@example.com";
            var testUser2 = await userManager.FindByEmailAsync(user2Email);
            if (testUser2 == null)
            {
                testUser2 = new BankUser
                {
                    UserName = "sarah.janssens",
                    Email = user2Email,
                    EmailConfirmed = true,
                    Voornaam = "Sarah",
                    Achternaam = "Janssens",
                    Telefoonnummer = "0498765432",
                    Geboortedatum = new DateTime(1985, 10, 2),
                    AdresId = 2,
                    Adres = testAdres2
                };

                var result = await userManager.CreateAsync(testUser2, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser2, "Medewerker");
                    logger.LogInformation($"Gebruiker '{user2Email}' aangemaakt met rol Medewerker");
                }
                else
                {
                    logger.LogError($"Fout bij aanmaken '{user2Email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Reset wachtwoord
                var token = await userManager.GeneratePasswordResetTokenAsync(testUser2);
                var resetResult = await userManager.ResetPasswordAsync(testUser2, token, "Password123!");
                if (resetResult.Succeeded)
                {
                    logger.LogInformation($"Wachtwoord gereset voor '{user2Email}'");
                }
            }

            // Test gebruiker 3: Admin
            var adminEmail = "admin@bankapp.local";
            var testAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (testAdmin == null)
            {
                testAdmin = new BankUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Voornaam = "Admin",
                    Achternaam = "Beheerder",
                    Telefoonnummer = "0412345678",
                    Geboortedatum = new DateTime(1975, 6, 25),
                    AdresId = 3,
                    Adres = testAdres3
                };

                var result = await userManager.CreateAsync(testAdmin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testAdmin, "Admin");
                    logger.LogInformation($"Gebruiker '{adminEmail}' aangemaakt met rol Admin");
                }
                else
                {
                    logger.LogError($"Fout bij aanmaken '{adminEmail}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Reset wachtwoord
                var token = await userManager.GeneratePasswordResetTokenAsync(testAdmin);
                var resetResult = await userManager.ResetPasswordAsync(testAdmin, token, "Admin123!");
                if (resetResult.Succeeded)
                {
                    logger.LogInformation($"Wachtwoord gereset voor '{adminEmail}'");
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Test gebruikers seeding voltooid");

            // Seed rekeningen en transacties alleen voor nieuwe gebruikers (vermijd duplicates)
            if (!context.Rekeningen.Any())
            {
                logger.LogInformation("Seeding rekeningen en transacties...");

                // Haal gebruikers opnieuw op voor rekeningen
                var klant = await context.Users.FirstOrDefaultAsync(u => u.Email == "jan.peeters@example.com");
                var medewerker = await context.Users.FirstOrDefaultAsync(u => u.Email == "sarah.janssens@example.com");
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@bankapp.local");

                // Klant rekening
                if (klant != null)
                {
                    context.Rekeningen.Add(
                        new Rekening { Iban = "BE12345678901234", Saldo = 5000.00m, GebruikerId = klant.Id, Deleted = DateTime.MaxValue }
                    );
                    context.Kaarten.Add(
                        new Kaart { KaartNummer = "1111-2222-3333-4444", Status = KaartStatus.Actief, GebruikerId = klant.Id, Deleted = DateTime.MaxValue }
                    );
                }

                // Medewerker rekening
                if (medewerker != null)
                {
                    context.Rekeningen.Add(
                        new Rekening { Iban = "BE11223344556677", Saldo = 7500.00m, GebruikerId = medewerker.Id, Deleted = DateTime.MaxValue }
                    );
                    context.Kaarten.Add(
                        new Kaart { KaartNummer = "5555-6666-7777-8888", Status = KaartStatus.Actief, GebruikerId = medewerker.Id, Deleted = DateTime.MaxValue }
                    );
                }

                // Admin rekening
                if (adminUser != null)
                {
                    context.Rekeningen.Add(
                        new Rekening { Iban = "BE99887766554433", Saldo = 10000.00m, GebruikerId = adminUser.Id, Deleted = DateTime.MaxValue }
                    );
                    context.Kaarten.Add(
                        new Kaart { KaartNummer = "9999-8888-7777-6666", Status = KaartStatus.Actief, GebruikerId = adminUser.Id, Deleted = DateTime.MaxValue }
                    );
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Rekeningen en kaarten seeding voltooid");
            }
        }

        // Seed rekeningen en kaarten na users zijn aangemaakt  
        private static async Task SeedRekeningenEnKaarten(AppDbContext context)
        {
            if (!context.Rekeningen.Any())
            {
                var user1 = await context.Users.FirstOrDefaultAsync(u => u.Email == "jan.peeters@example.com");
                var user2 = await context.Users.FirstOrDefaultAsync(u => u.Email == "sarah.janssens@example.com");
                var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@bankapp.local");

                // User1 (Klant) - Jan Peeters
                if (user1 != null)
                {
                    context.Rekeningen.Add(
                        new Rekening { Iban = "BE12345678901234", Saldo = 5000.00m, GebruikerId = user1.Id, Deleted = DateTime.MaxValue }
                    );
                    context.Kaarten.Add(
                        new Kaart { KaartNummer = "1111-2222-3333-4444", Status = KaartStatus.Actief, GebruikerId = user1.Id, Deleted = DateTime.MaxValue }
                    );

                    // Transacties voor user1
                    context.Transacties.Add(
                        new Transactie
                        {
                            VanIban = "BE12345678901234",
                            NaarIban = "BE11223344556677",
                            NaamOntvanger = "Sarah Janssens",
                            Bedrag = 50.00m,
                            Omschrijving = "Cadeau",
                            Datum = new DateTime(2024, 5, 10),
                            Deleted = DateTime.MaxValue,
                            GebruikerId = user1.Id
                        }
                    );
                }

                // User2 (Medewerker) - Sarah Janssens
                if (user2 != null)
                {
                    context.Rekeningen.Add(
                        new Rekening { Iban = "BE11223344556677", Saldo = 7500.00m, GebruikerId = user2.Id, Deleted = DateTime.MaxValue }
                    );
                    context.Kaarten.Add(
                        new Kaart { KaartNummer = "5555-6666-7777-8888", Status = KaartStatus.Actief, GebruikerId = user2.Id, Deleted = DateTime.MaxValue }
                    );
                }

                // Admin - Admin Beheerder
                if (admin != null)
                {
                    context.Rekeningen.Add(
                        new Rekening { Iban = "BE99887766554433", Saldo = 10000.00m, GebruikerId = admin.Id, Deleted = DateTime.MaxValue }
                    );
                    context.Kaarten.Add(
                        new Kaart { KaartNummer = "9999-8888-7777-6666", Status = KaartStatus.Actief, GebruikerId = admin.Id, Deleted = DateTime.MaxValue }
                    );
                }

                await context.SaveChangesAsync();
            }
            else
            {
                // Als rekeningen al bestaan, update saldo's voor test gebruikers
                var user1 = await context.Users.FirstOrDefaultAsync(u => u.Email == "jan.peeters@example.com");
                var user2 = await context.Users.FirstOrDefaultAsync(u => u.Email == "sarah.janssens@example.com");
                var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@bankapp.local");

                // Update saldo's voor bestaande rekeningen
                if (user1 != null)
                {
                    var rekening1 = await context.Rekeningen.FirstOrDefaultAsync(r => r.GebruikerId == user1.Id && r.Deleted == DateTime.MaxValue);
                    if (rekening1 != null)
                    {
                        rekening1.Saldo = 5000.00m;
                    }
                    else
                    {
                        // Maak rekening aan als die nog niet bestaat
                        context.Rekeningen.Add(
                            new Rekening { Iban = "BE12345678901234", Saldo = 5000.00m, GebruikerId = user1.Id, Deleted = DateTime.MaxValue }
                        );
                        context.Kaarten.Add(
                            new Kaart { KaartNummer = "1111-2222-3333-4444", Status = KaartStatus.Actief, GebruikerId = user1.Id, Deleted = DateTime.MaxValue }
                        );
                    }
                }

                if (user2 != null)
                {
                    var rekening2 = await context.Rekeningen.FirstOrDefaultAsync(r => r.GebruikerId == user2.Id && r.Deleted == DateTime.MaxValue);
                    if (rekening2 != null)
                    {
                        rekening2.Saldo = 7500.00m;
                    }
                    else
                    {
                        // Maak rekening aan als die nog niet bestaat
                        context.Rekeningen.Add(
                            new Rekening { Iban = "BE11223344556677", Saldo = 7500.00m, GebruikerId = user2.Id, Deleted = DateTime.MaxValue }
                        );
                        context.Kaarten.Add(
                            new Kaart { KaartNummer = "5555-6666-7777-8888", Status = KaartStatus.Actief, GebruikerId = user2.Id, Deleted = DateTime.MaxValue }
                        );
                    }
                }

                if (admin != null)
                {
                    var rekeningAdmin = await context.Rekeningen.FirstOrDefaultAsync(r => r.GebruikerId == admin.Id && r.Deleted == DateTime.MaxValue);
                    if (rekeningAdmin != null)
                    {
                        rekeningAdmin.Saldo = 10000.00m;
                    }
                    else
                    {
                        // Maak rekening aan als die nog niet bestaat
                        context.Rekeningen.Add(
                            new Rekening { Iban = "BE99887766554433", Saldo = 10000.00m, GebruikerId = admin.Id, Deleted = DateTime.MaxValue }
                        );
                        context.Kaarten.Add(
                            new Kaart { KaartNummer = "9999-8888-7777-6666", Status = KaartStatus.Actief, GebruikerId = admin.Id, Deleted = DateTime.MaxValue }
                        );
                    }
                }

                await context.SaveChangesAsync();
            }
        }
    }
}


