using System;
using System.Linq;
using System.Windows;
using BankApp_Models;
using Microsoft.EntityFrameworkCore;

namespace BankApp_WPF
{
    public partial class WachtwoordVergetenPagina : Window
    {
        private string? _emailMetAangevraagdeCode;

        public WachtwoordVergetenPagina()
        {
            InitializeComponent();
            this.Loaded += (s, e) => TxtEmail.Focus();
        }

        private void BtnVerstuur_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            string email = TxtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ToonFout("Vul je e-mailadres in.");
                return;
            }

            if (!IsGeldigEmail(email))
            {
                ToonFout("Ongeldig e-mailadres.");
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    var gebruiker = context.Users
                        .FirstOrDefault(u => u.Email != null && u.Email.ToLower() == email.ToLower()
                            && u.Deleted == DateTime.MaxValue);

                    if (gebruiker == null)
                    {
                        ToonFout("Geen account gevonden met dit e-mailadres.");
                        return;
                    }

                    // Genereer een tijdelijke, willekeurige 6-cijferige resetcode
                    var random = new Random();
                    string code = random.Next(100000, 1000000).ToString();

                    gebruiker.WachtwoordResetCode = code;
                    gebruiker.WachtwoordResetVervaltijd = DateTime.UtcNow.AddMinutes(15);
                    context.SaveChanges();

                    // In een productieomgeving zou deze code per e-mail verstuurd worden.
                    // Dit project heeft geen e-mailserver/SMTP-configuratie, dus tonen we
                    // de code hier rechtstreeks voor demo-/testdoeleinden.
                    TxtDemoCode.Text = $"Voor demo-doeleinden: uw resetcode is {code} — geldig tot " +
                        $"{gebruiker.WachtwoordResetVervaltijd.Value.ToLocalTime():HH:mm}. " +
                        "In productie zou dit per e-mail verstuurd worden.";
                    BorderDemoCode.Visibility = Visibility.Visible;

                    _emailMetAangevraagdeCode = email;
                    BtnVolgende.Visibility = Visibility.Visible;
                    BtnVerstuur.Content = "Nieuwe code aanvragen";
                }
            }
            catch (Exception ex)
            {
                ToonFout($"Er ging iets mis: {ex.Message}");
            }
        }

        private void BtnVolgende_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_emailMetAangevraagdeCode))
            {
                ToonFout("Vraag eerst een resetcode aan.");
                return;
            }

            var nieuwWachtwoordPagina = new NieuwWachtwoordPagina(_emailMetAangevraagdeCode);
            nieuwWachtwoordPagina.Show();
            this.Close();
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

        private bool IsGeldigEmail(string email)
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
    }
}
