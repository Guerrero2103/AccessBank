using BankApp_Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace BankApp_WPF
{
    public partial class StartPagina : Window
    {
        private bool isDarkTheme = true; // bepaalt welk thema actief is

        public StartPagina()
        {
            InitializeComponent();

            // Database setup en seeding (automatisch voor alle teamleden)
            InitializeDatabaseAsync();
        }

        // Automatische database initialisatie en seeding
        private async void InitializeDatabaseAsync()
        {
            try
            {
                using var context = new AppDbContext();
                
                // Controleer of database bestaat en migraties zijn uitgevoerd
                if (!context.Database.CanConnect())
                {
                    MessageBox.Show(
                        "Database bestaat nog niet!\n\n" +
                        "Volg deze stappen:\n" +
                        "1. Open Package Manager Console\n" +
                        "2. Selecteer BankApp_Models project\n" +
                        "3. Voer uit: Add-Migration InitialCreate\n" +
                        "4. Voer uit: Update-Database\n\n" +
                        "Zie MIGRATIES.md voor details.",
                        "Database Setup Vereist",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Controleer of er al data is (rollen bestaan)
                var hasRoles = await context.Roles.AnyAsync();
                var hasUsers = await context.Users.AnyAsync();

                // Seed altijd (voor nieuwe users of saldo updates)
                await AppDbContext.Seeder(context);
                
                // Toon alleen bericht als het de eerste keer is
                if (!hasRoles || !hasUsers)
                {
                    MessageBox.Show(
                        "Database is succesvol geïnitialiseerd!\n\n" +
                        "Test accounts:\n" +
                        "• Klant: jan.peeters@example.com / Password123!\n" +
                        "• Medewerker: sarah.janssens@example.com / Password123!\n" +
                        "• Admin: admin@bankapp.local / Admin123!",
                        "Database Setup Voltooid",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fout bij database initialisatie:\n{ex.Message}\n\n" +
                    "Zorg dat migraties zijn uitgevoerd (zie MIGRATIES.md)",
                    "Database Fout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 🔹 Registratiepagina openen
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegistratiePagina registratiePagina = new RegistratiePagina();
            registratiePagina.Show();
            this.Close();
        }

        // 🔹 Loginpagina openen
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginPagina loginPagina = new LoginPagina();
            loginPagina.Show();
            this.Close();
        }

        // 🔹 Card Stop actie
        private void BtnCardStop_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Weet je zeker dat je je bankkaart wilt blokkeren?",
                "Card Stop",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                CardStopPagina cardStopPagina = new CardStopPagina();
                cardStopPagina.Show();
                this.Close();
            }
        }

        // 🔹 Thema wisselen
        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;

            if (isDarkTheme)
            {
                this.Background = Brushes.Black;
                BtnTheme.Content = "☀";
            }
            else
            {
                this.Background = Brushes.White;
                BtnTheme.Content = "🌙";
            }
        }

        // 🔹 Help-venster
        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "AccessBank Hulp\n\n" +
                "👥 Registreren - Maak een nieuw account aan\n" +
                "🔐 Inloggen - Meld aan met je bestaande gegevens\n" +
                "💳 Card Stop - Blokkeer je bankkaart bij verlies of diefstal\n\n" +
                "Voor verdere hulp, contacteer support@accessbank.be",
                "Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
