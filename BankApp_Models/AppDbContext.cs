using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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

        // Constructor
        public AppDbContext()
        {
            // Database.EnsureCreated() verwijderd - gebruik migraties i.p.v.
        }

        // SQLite configuratie
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
        public static async Task Seeder(AppDbContext context)
        {
            await BankUser.Seeder(context);

            // Seed rekeningen en kaarten na users zijn aangemaakt
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


