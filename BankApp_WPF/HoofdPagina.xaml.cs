using BankApp_Models;
using BankApp_BusinessLogic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System;

namespace BankApp_WPF
{
    public partial class HoofdPagina : Window
    {
        public HoofdPagina()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                Console.WriteLine("HoofdPagina wordt geladen...");

                // Check of gebruiker is ingelogd
                if (!UserSession.IsIngelogd)
                {
                    MessageBox.Show("Je bent niet ingelogd.", "Fout",
                        MessageBoxButton.OK, MessageBoxImage.Warning);

                    LoginPagina loginPagina = new LoginPagina();
                    loginPagina.Show();
                    this.Close();
                    return;
                }

                Console.WriteLine($"Gebruiker ingelogd: {UserSession.IngelogdeGebruiker?.Email}");

                // Test database verbinding
                try
                {
                    using (var context = new AppDbContext())
                    {
                        Console.WriteLine("Database context aangemaakt");
                    }
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Database fout: {dbEx.Message}");
                    MessageBox.Show($"Database fout: {dbEx.Message}\n\n{dbEx.StackTrace}",
                        "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw;
                }

                // Controleer of gebruiker medewerker of admin is en ga naar juiste pagina
                await CheckEnRedirectMedewerker();
                
                // Laad gebruikersgegevens
                LoadUserData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRASH in HoofdPagina constructor: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Er is een fout opgetreden bij het laden van de hoofdpagina:\n\n" +
                    $"{ex.Message}\n\n" +
                    $"Stack trace:\n{ex.StackTrace}",
                    "Kritieke Fout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Ga terug naar login
                StartPagina startPagina = new StartPagina();
                startPagina.Show();
                this.Close();
            }
        }

        private async void LoadUserData()
        {
            try
            {
                Console.WriteLine("LoadUserData gestart...");

                if (!UserSession.IsIngelogd || UserSession.IngelogdeGebruiker == null)
                {
                    Console.WriteLine("Geen ingelogde gebruiker gevonden");
                    MessageBox.Show("Gebruikerssessie is verlopen. Log opnieuw in.", "Fout",
                        MessageBoxButton.OK, MessageBoxImage.Warning);

                    LoginPagina loginPagina = new LoginPagina();
                    loginPagina.Show();
                    this.Close();
                    return;
                }

                var gebruikerId = UserSession.IngelogdeGebruiker.Id;
                Console.WriteLine($"Gebruiker ID: {gebruikerId}");

                using (var context = new AppDbContext())
                {
                    var rekeningService = new RekeningService(context);

                    // Haal rekeningen op
                    Console.WriteLine("Rekeningen ophalen...");
                    var rekeningen = await rekeningService.GetRekeningenByGebruikerIdAsync(gebruikerId);

                    if (rekeningen == null)
                    {
                        Console.WriteLine("Rekeningen is NULL");
                        rekeningen = new System.Collections.Generic.List<Rekening>();
                    }

                    Console.WriteLine($"Aantal rekeningen: {rekeningen.Count}");

                    // Maak nieuwe rekening aan als gebruiker er nog geen heeft
                    if (rekeningen.Count == 0)
                    {
                        Console.WriteLine("Nieuwe zichtrekening aanmaken...");
                        var nieuwe = await rekeningService.MaakRekeningAanAsync(gebruikerId);
                        rekeningen = new System.Collections.Generic.List<Rekening> { nieuwe };
                        Console.WriteLine($"Rekening aangemaakt: {nieuwe.Iban}");
                    }

                    // Haal totaal saldo op
                    Console.WriteLine("Totaal saldo ophalen...");
                    var totaalSaldo = await rekeningService.GetTotaalSaldoAsync(gebruikerId);
                    Console.WriteLine($"Totaal saldo: €{totaalSaldo}");

                    // Toon saldo op scherm
                    lblTotalSaldo.Content = $"€{totaalSaldo:N2}";

                    // Toon rekeningnummer
                    var zichtRekening = rekeningen.FirstOrDefault();

                    if (zichtRekening != null)
                    {
                        lblAccountNumber.Content = $"Zichtrekening {zichtRekening.Iban}";
                        Console.WriteLine($"Zichtrekening: {zichtRekening.Iban}");
                    }

                    Console.WriteLine("LoadUserData voltooid!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout in LoadUserData: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Fout bij laden gegevens:\n\n{ex.Message}\n\n" +
                    $"Stack trace:\n{ex.StackTrace}",
                    "Fout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Zet standaard waarden
                lblTotalSaldo.Content = "€0.00";
                lblAccountNumber.Content = "Geen rekening";
            }
        }

        private void BtnViewSaldo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaldoRaadplegenPagina saldoRaadplegenPagina = new SaldoRaadplegenPagina();
                saldoRaadplegenPagina.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OverschrijvingenPagina overschrijvingenPagina = new OverschrijvingenPagina();
                overschrijvingenPagina.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProfilePage profilePage = new ProfilePage();
                profilePage.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnContact_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                KlantendienstPagina klantendienstPagina = new KlantendienstPagina();
                klantendienstPagina.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Controleer of gebruiker medewerker of admin is en ga naar juiste pagina
        private async Task CheckEnRedirectMedewerker()
        {
            try
            {
                if (!UserSession.IsIngelogd || UserSession.IngelogdeGebruiker == null)
                    return;

                using var context = new AppDbContext();
                using var userManager = new UserManager<BankUser>(
                    new UserStore<BankUser>(context),
                    null!, new PasswordHasher<BankUser>(),
                    null!, null!, null!, null!, null!, null!);

                var gebruiker = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == UserSession.IngelogdeGebruiker.Id);

                if (gebruiker != null)
                {
                    await context.Entry(gebruiker).ReloadAsync();
                    var roles = await userManager.GetRolesAsync(gebruiker);
                    
                    bool isAdmin = roles != null && roles.Any(r =>
                        r.Equals("Admin", StringComparison.OrdinalIgnoreCase));
                    bool isMedewerker = roles != null && roles.Any(r =>
                        r.Equals("Medewerker", StringComparison.OrdinalIgnoreCase));

                    // Admin gaat naar adminpagina
                    if (isAdmin)
                    {
                        Console.WriteLine("Admin gedetecteerd in HoofdPagina - redirect naar AdminPagina");
                        AdminPagina adminPagina = new AdminPagina();
                        adminPagina.Show();
                        this.Close();
                        return;
                    }
                    
                    // Medewerker gaat naar medewerkerpagina
                    if (isMedewerker)
                    {
                        Console.WriteLine("Medewerker gedetecteerd in HoofdPagina - redirect naar MedewerkerPagina");
                        MedewerkerPagina medewerkerPagina = new MedewerkerPagina();
                        medewerkerPagina.Show();
                        this.Close();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij checken medewerker rol: {ex.Message}");
            }
        }


        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Weet je zeker dat je wilt uitloggen?",
                    "Uitloggen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    UserSession.LogUit();
                    StartPagina startPagina = new StartPagina();
                    startPagina.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}