using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BankApp_Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankApp_WPF
{
    public partial class LoginPagina : Window
    {
        private bool isDarkMode = true;

        public LoginPagina()
        {
            InitializeComponent();
            
            this.Loaded += (s, e) => TxtEmail.Focus();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            StartPagina startPagina = new StartPagina();
            startPagina.Show();
            this.Close();
        }

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;

            if (isDarkMode)
            {
                this.Background = Brushes.Black;
                BtnTheme.Content = new TextBlock { Text = "☀", Foreground = Brushes.White, FontSize = 24 };
            }
            else
            {
                this.Background = Brushes.White;
                BtnTheme.Content = new TextBlock { Text = "🌙", Foreground = Brushes.White, FontSize = 24 };
            }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtEmail.Text.Trim();
            string password = TxtPassword.Password;

            TxtError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(email))
            {
                ShowError("Gelieve je email in te vullen.");
                TxtEmail.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Gelieve je wachtwoord in te vullen.");
                TxtPassword.Focus();
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowError("Ongeldig email formaat.");
                TxtEmail.Focus();
                return;
            }

            var (gebruiker, isGeblokkeerd) = await ValidateLoginAsync(email, password);

            if (isGeblokkeerd)
            {
                ShowError("Uw account is geblokkeerd. Neem contact op met de klantendienst.");
                TxtPassword.Clear();
                return;
            }

            if (gebruiker != null)
            {
                // Bewaar ingelogde gebruiker
                UserSession.IngelogdeGebruiker = gebruiker;

                MessageBox.Show($"Welkom {gebruiker.Email}!", "Login Succesvol",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Controleer welke rol gebruiker heeft
                bool isAdmin = false;
                
                using (var context = new AppDbContext())
                using (var userManager = IdentityManagerFactory.CreateUserManager(context))
                {
                    // Haal gebruiker opnieuw op om rollen te kunnen bekijken
                    var gebruikerMetRollen = await context.Users
                        .FirstOrDefaultAsync(u => u.Id == gebruiker.Id);

                    if (gebruikerMetRollen != null)
                    {
                        // Laad rollen opnieuw
                        await context.Entry(gebruikerMetRollen).ReloadAsync();
                        
                        var roles = await userManager.GetRolesAsync(gebruikerMetRollen);
                        
                        // Toon rollen voor controle
                        if (roles != null && roles.Any())
                        {
                            string rollenString = string.Join(", ", roles);
                            Console.WriteLine($"Gebruiker rollen: {rollenString}");
                        }
                        else
                        {
                            Console.WriteLine("Geen rollen gevonden voor gebruiker!");
                        }

                        // Controleer of gebruiker Admin of Medewerker is
                        bool isAdminRole = roles != null && roles.Any(r => 
                            r.Equals("Admin", StringComparison.OrdinalIgnoreCase));
                        bool isMedewerkerRole = roles != null && roles.Any(r => 
                            r.Equals("Medewerker", StringComparison.OrdinalIgnoreCase));
                        
                        isAdmin = isAdminRole;
                        
                        // Ga naar juiste pagina op basis van rol
                        if (isAdminRole)
                        {
                            Console.WriteLine("Admin gedetecteerd - ga naar AdminPagina");
                            AdminPagina adminPagina = new AdminPagina();
                            adminPagina.Show();
                            this.Close();
                            return;
                        }
                        else if (isMedewerkerRole)
                        {
                            Console.WriteLine("Medewerker gedetecteerd - ga naar MedewerkerPagina");
                            MedewerkerPagina medewerkerPagina = new MedewerkerPagina();
                            medewerkerPagina.Show();
                            this.Close();
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Gebruiker niet gevonden in context voor rol check!");
                    }
                }

                // Als rollen niet werken, controleer email
                if (!isAdmin && gebruiker.Email != null && 
                    (gebruiker.Email.Equals("admin@bankapp.local", StringComparison.OrdinalIgnoreCase) ||
                     gebruiker.Email.Equals("beheerder@bankapp.local", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("Admin gedetecteerd via email fallback");
                    AdminPagina adminPagina = new AdminPagina();
                    adminPagina.Show();
                    this.Close();
                    return;
                }

                // Normale klant - ga naar hoofdpagina
                Console.WriteLine("Klant - ga naar HoofdPagina");
                HoofdPagina hoofd = new HoofdPagina();
                hoofd.Show();
                this.Close();
            }
            else
            {
                ShowError("Onjuiste email of wachtwoord.");
                TxtPassword.Clear();
                TxtEmail.Focus();
            }
        }

        private void LinkForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var wachtwoordVergetenPagina = new WachtwoordVergetenPagina();
            wachtwoordVergetenPagina.Show();
            this.Close();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnBack_Click(sender, e);
            }
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Controleer inloggegevens
        private async Task<(BankUser? Gebruiker, bool IsGeblokkeerd)> ValidateLoginAsync(string email, string password)
        {
            using var context = new AppDbContext();
            using var userManager = IdentityManagerFactory.CreateUserManager(context);

            var gebruiker = await context.Users
                .Include(u => u.Adres)
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == email.ToLower() &&
                    u.Deleted == DateTime.MaxValue);

            if (gebruiker == null)
                return (null, false);

            // Controleer lockout vóór het wachtwoord (zelfde volgorde als ASP.NET Core Identity's
            // SignInManager), zodat BtnGebruikerBlokkeren_Click (die LockoutEnd zet) ook echt effect heeft
            if (gebruiker.LockoutEnd != null && gebruiker.LockoutEnd > DateTimeOffset.UtcNow)
                return (null, true);

            // Controleer wachtwoord
            var result = await userManager.CheckPasswordAsync(gebruiker, password);
            if (result)
            {
                return (gebruiker, false);
            }

            return (null, false);
        }
    }
}