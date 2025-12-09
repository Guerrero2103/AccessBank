using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BankApp_Models;

namespace BankApp_WPF
{
    public partial class KlantendienstPagina : Window
    {
        private bool isDarkMode = true;

        public KlantendienstPagina()
        {
            InitializeComponent();
            this.Loaded += (s, e) => TxtNaam.Focus();
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

        // 🌓 Thema wisselen
        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;

            if (isDarkMode)
            {
                this.Background = Brushes.Black;
                BtnTheme.Content = "☀";
                SetTextColors(Brushes.White);
            }
            else
            {
                this.Background = Brushes.White;
                BtnTheme.Content = "🌙";
                SetTextColors(Brushes.Black);
            }
        }

        // Pas tekstkleuren aan
        private void SetTextColors(Brush kleur)
        {
            void Recurse(DependencyObject parent)
            {
                if (parent == null) return;
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is TextBlock tb && tb.Name != "TxtError")
                        tb.Foreground = kleur;
                    Recurse(child);
                }
            }

            Recurse(this);
        }

        // 📩 Bericht versturen
        private async void BtnVerstuur_Click(object sender, RoutedEventArgs e)
        {
            string naam = TxtNaam.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string onderwerp = TxtOnderwerp.Text.Trim();
            string bericht = TxtBericht.Text.Trim();

            TxtError.Visibility = Visibility.Collapsed;

            // Validatie
            if (string.IsNullOrEmpty(naam))
            {
                ShowError("Gelieve je naam in te vullen.");
                TxtNaam.Focus();
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                ShowError("Gelieve je e-mailadres in te vullen.");
                TxtEmail.Focus();
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowError("Ongeldig e-mailadres. Gebruik: naam@voorbeeld.be");
                TxtEmail.Focus();
                return;
            }

            if (string.IsNullOrEmpty(onderwerp))
            {
                ShowError("Gelieve een onderwerp in te vullen.");
                TxtOnderwerp.Focus();
                return;
            }

            if (string.IsNullOrEmpty(bericht))
            {
                ShowError("Gelieve je bericht in te vullen.");
                TxtBericht.Focus();
                return;
            }

            try
            {
                // Sla bericht op in database
                using var context = new AppDbContext();
                var klantBericht = new KlantBericht
                {
                    Naam = naam,
                    Email = email,
                    Onderwerp = onderwerp,
                    Bericht = bericht,
                    Datum = DateTime.Now,
                    Status = "Nieuw",
                    GebruikerId = UserSession.IsIngelogd ? UserSession.IngelogdeGebruiker?.Id : null,
                    Deleted = DateTime.MaxValue
                };

                context.KlantBerichten.Add(klantBericht);
                await context.SaveChangesAsync();

                MessageBox.Show(
                    $"Bedankt {naam}!\n\nJe bericht is succesvol verstuurd naar onze klantendienst.\n\nOnderwerp: {onderwerp}\nE-mail: {email}",
                    "Bericht Verzonden",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Velden leegmaken
                TxtNaam.Clear();
                TxtEmail.Clear();
                TxtOnderwerp.Clear();
                TxtBericht.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fout bij verzenden bericht: {ex.Message}",
                    "Fout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 🔙 Terug naar startpagina
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            HoofdPagina hoofdPagina = new HoofdPagina();
            hoofdPagina.Show();
            this.Close();
        }


        // Helper-methoden
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
    }
}
