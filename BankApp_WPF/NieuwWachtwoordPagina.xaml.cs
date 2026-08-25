using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using BankApp_Models;
using Microsoft.AspNetCore.Identity;

namespace BankApp_WPF
{
    public partial class NieuwWachtwoordPagina : Window
    {
        private readonly string _email;

        public NieuwWachtwoordPagina(string email)
        {
            InitializeComponent();
            _email = email;
            TxtEmailInfo.Text = $"Vul de resetcode in die verstuurd werd voor {email}.";
            this.Loaded += (s, e) => TxtCode.Focus();
        }

        private void BtnWijzigen_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;
            TxtSucces.Visibility = Visibility.Collapsed;

            string code = TxtCode.Text.Trim();
            string nieuwWachtwoord = TxtNieuwWachtwoord.Password;
            string bevestigWachtwoord = TxtBevestigWachtwoord.Password;

            if (string.IsNullOrWhiteSpace(code))
            {
                ToonFout("Vul de resetcode in.");
                return;
            }

            if (string.IsNullOrWhiteSpace(nieuwWachtwoord))
            {
                ToonFout("Vul een nieuw wachtwoord in.");
                return;
            }

            if (nieuwWachtwoord != bevestigWachtwoord)
            {
                ToonFout("De wachtwoorden komen niet overeen.");
                return;
            }

            if (!IsWachtwoordSterkGenoeg(nieuwWachtwoord))
            {
                ToonFout("Wachtwoord moet minstens 8 tekens bevatten, met een hoofdletter, kleine letter, cijfer en speciaal teken.");
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    var gebruiker = context.Users
                        .FirstOrDefault(u => u.Email != null && u.Email.ToLower() == _email.ToLower()
                            && u.Deleted == DateTime.MaxValue);

                    if (gebruiker == null)
                    {
                        ToonFout("Gebruiker niet gevonden.");
                        return;
                    }

                    if (string.IsNullOrEmpty(gebruiker.WachtwoordResetCode) || gebruiker.WachtwoordResetVervaltijd == null)
                    {
                        ToonFout("Er is geen actieve resetaanvraag. Vraag een nieuwe code aan.");
                        return;
                    }

                    if (gebruiker.WachtwoordResetVervaltijd.Value < DateTime.UtcNow)
                    {
                        ToonFout("De resetcode is verlopen. Vraag een nieuwe code aan.");
                        return;
                    }

                    if (gebruiker.WachtwoordResetCode != code)
                    {
                        ToonFout("Onjuiste resetcode.");
                        return;
                    }

                    // Wachtwoord rechtstreeks hashen en opslaan i.p.v. via Identity's
                    // ResetPasswordAsync-tokenflow: de UserManager die in deze WPF-app
                    // gebruikt wordt is niet gekoppeld aan een geregistreerde
                    // token-provider (zie IdentityManagerFactory), dus deze aanpak
                    // (zelfde als in BankUser.Seeder) is de betrouwbare optie hier.
                    gebruiker.PasswordHash = new PasswordHasher<BankUser>().HashPassword(gebruiker, nieuwWachtwoord);

                    // Code is verbruikt: opruimen zodat ze niet hergebruikt kan worden
                    gebruiker.WachtwoordResetCode = null;
                    gebruiker.WachtwoordResetVervaltijd = null;

                    context.SaveChanges();

                    TxtSucces.Text = "Wachtwoord succesvol gewijzigd! Je kan nu inloggen met je nieuwe wachtwoord.";
                    TxtSucces.Visibility = Visibility.Visible;
                    TxtCode.Clear();
                    TxtNieuwWachtwoord.Clear();
                    TxtBevestigWachtwoord.Clear();
                }
            }
            catch (Exception ex)
            {
                ToonFout($"Er ging iets mis: {ex.Message}");
            }
        }

        private void BtnTerug_Click(object sender, RoutedEventArgs e)
        {
            var loginPagina = new LoginPagina();
            loginPagina.Show();
            this.Close();
        }

        private void ToonFout(string bericht)
        {
            TxtError.Text = bericht;
            TxtError.Visibility = Visibility.Visible;
        }

        // Zelfde validatieregels als bij registratie (RegistratiePagina.HeeftHoofdletterEnCijfer)
        private bool IsWachtwoordSterkGenoeg(string wachtwoord)
        {
            return wachtwoord.Length >= 8
                && Regex.IsMatch(wachtwoord, @"[A-Z]")
                && Regex.IsMatch(wachtwoord, @"[a-z]")
                && Regex.IsMatch(wachtwoord, @"\d")
                && Regex.IsMatch(wachtwoord, @"[^a-zA-Z0-9]");
        }
    }
}
