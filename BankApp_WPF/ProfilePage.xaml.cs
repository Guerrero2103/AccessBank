using BankApp_Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BankApp_WPF
{
    public partial class ProfilePage : Window
    {
        private BankUser? _gebruiker;

        public ProfilePage()
        {
            InitializeComponent();
            Loaded += ProfilePage_Loaded;
            
            this.KeyDown += Window_KeyDown;
            this.Focusable = true;
            this.Focus();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                e.Handled = true;
                HoofdPagina hoofdPagina = new HoofdPagina();
                hoofdPagina.Show();
                this.Close();

            }
        }

        private async void ProfilePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Controleer of er iemand is ingelogd
            if (UserSession.IngelogdeGebruiker == null)
            {
                MessageBox.Show("Geen gebruiker actief. Log eerst in.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            using (var context = new AppDbContext())
            {
                _gebruiker = await context.Users
                    .Include(g => g.Adres)
                    .Include(g => g.Rekeningen)
                    .FirstOrDefaultAsync(g => g.Id == UserSession.IngelogdeGebruiker.Id);
            }

            if (_gebruiker == null)
            {
                MessageBox.Show("Gebruiker niet gevonden in de database.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            UserSession.IngelogdeGebruiker = _gebruiker;

            // Vul alle velden met de huidige gegevens
            VoornaamTextBox.Text = _gebruiker.Voornaam ?? "";
            AchternaamTextBox.Text = _gebruiker.Achternaam ?? "";
            EmailTextBox.Text = _gebruiker.Email ?? "";
            PhoneTextBox.Text = _gebruiker.Telefoonnummer ?? "";
            IbanTextBox.Text = _gebruiker.Rekeningen?.FirstOrDefault()?.Iban ?? "Geen rekening gevonden";
            BirthdatePicker.SelectedDate = _gebruiker.Geboortedatum;
            StraatTextBox.Text = _gebruiker.Adres?.Straat ?? "";
            HuisnummerTextBox.Text = _gebruiker.Adres?.Huisnummer ?? "";
            BusTextBox.Text = _gebruiker.Adres?.Bus ?? "";
            PostcodeTextBox.Text = _gebruiker.Adres?.Postcode ?? "";
            GemeenteTextBox.Text = _gebruiker.Adres?.Gemeente ?? "";
            LandTextBox.Text = _gebruiker.Adres?.Land ?? "";

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            HoofdPagina hoofd = new HoofdPagina();
            hoofd.Show();
            Close();
        }


        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gebruiker == null)
            {
                MessageBox.Show("Geen actieve gebruiker gevonden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                using (var userManager = new UserManager<BankUser>(
                    new UserStore<BankUser>(context),
                    null, new PasswordHasher<BankUser>(),
                    null, null, null, null, null, null))
                {
                    var gebruikerInDb = await context.Users
                        .Include(g => g.Adres)
                        .FirstOrDefaultAsync(g => g.Id == _gebruiker.Id);

                    if (gebruikerInDb == null)
                    {
                        MessageBox.Show("Gebruiker niet gevonden in de database.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Validatie
                    if (string.IsNullOrWhiteSpace(VoornaamTextBox.Text))
                    {
                        MessageBox.Show("Voornaam is verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(AchternaamTextBox.Text))
                    {
                        MessageBox.Show("Achternaam is verplicht.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                    {
                        MessageBox.Show("E-mail mag niet leeg zijn.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (BirthdatePicker.SelectedDate == null)
                    {
                        MessageBox.Show("Ongeldige geboortedatum.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Wachtwoordoptie (optioneel aanpassen)
                    if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        if (PasswordBox.Password != ConfirmPasswordBox.Password)
                        {
                            MessageBox.Show("De wachtwoorden komen niet overeen.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var token = await userManager.GeneratePasswordResetTokenAsync(gebruikerInDb);
                        await userManager.ResetPasswordAsync(gebruikerInDb, token, PasswordBox.Password);
                    }

                    // Update velden
                    gebruikerInDb.Voornaam = VoornaamTextBox.Text.Trim();
                    gebruikerInDb.Achternaam = AchternaamTextBox.Text.Trim();
                    gebruikerInDb.Email = EmailTextBox.Text.Trim();
                    gebruikerInDb.UserName = EmailTextBox.Text.Trim();
                    gebruikerInDb.Telefoonnummer = PhoneTextBox.Text.Trim();
                    gebruikerInDb.Geboortedatum = BirthdatePicker.SelectedDate.Value;

                    // Update adres
                    if (gebruikerInDb.Adres == null)
                    {
                        gebruikerInDb.Adres = new Adres();
                    }
                    gebruikerInDb.Adres.Straat = StraatTextBox.Text.Trim();
                    gebruikerInDb.Adres.Huisnummer = HuisnummerTextBox.Text.Trim();
                    gebruikerInDb.Adres.Bus = string.IsNullOrWhiteSpace(BusTextBox.Text) ? null : BusTextBox.Text.Trim();
                    gebruikerInDb.Adres.Postcode = PostcodeTextBox.Text.Trim();
                    gebruikerInDb.Adres.Gemeente = GemeenteTextBox.Text.Trim();
                    gebruikerInDb.Adres.Land = LandTextBox.Text.Trim();

                    // Opslaan in DB
                    await context.SaveChangesAsync();

                    // Bijwerken in UserSession
                    await context.Entry(gebruikerInDb).Reference(g => g.Adres).LoadAsync();
                    UserSession.IngelogdeGebruiker = gebruikerInDb;
                    _gebruiker = gebruikerInDb;

                    MessageBox.Show("Gegevens succesvol opgeslagen!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fout opgetreden bij het opslaan: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        // DeleteButton: voorlopig geen functionaliteit
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gebruiker == null)
            {
                MessageBox.Show("Geen actieve gebruiker gevonden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var bevestiging = MessageBox.Show(
                "Weet je zeker dat je je profiel wilt verwijderen?\n" +
                "Je account wordt gedeactiveerd, maar je gegevens blijven bewaard voor administratie.",
                "Bevestig verwijdering",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (bevestiging != MessageBoxResult.Yes)
                return;

            try
            {
                using (var context = new AppDbContext())
                {
                    var gebruikerInDb = await context.Users
                        .Include(g => g.Adres)
                        .FirstOrDefaultAsync(g => g.Id == _gebruiker.Id);

                    if (gebruikerInDb == null)
                    {
                        MessageBox.Show("Gebruiker niet gevonden in database.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Soft delete
                    gebruikerInDb.Deleted = DateTime.UtcNow;
                    if (gebruikerInDb.Adres != null)
                        gebruikerInDb.Adres.Deleted = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }

                // Clear sessie
                UserSession.IngelogdeGebruiker = null;

                MessageBox.Show("Je account is gedeactiveerd. Bedankt om onze bank te gebruiken!", "Account gedeactiveerd", MessageBoxButton.OK, MessageBoxImage.Information);

                // Terug naar loginpagina
                LoginPagina login = new LoginPagina();
                login.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fout opgetreden bij het verwijderen: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



    }
}
