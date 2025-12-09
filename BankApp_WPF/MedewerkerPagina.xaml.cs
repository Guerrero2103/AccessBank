using BankApp_BusinessLogic;
using BankApp_Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BankApp_WPF
{
    public partial class MedewerkerPagina : Window
    {
        private readonly ITransactieService _transactieService;
        private ObservableCollection<Transactie> _wachtendeOverschrijvingen;
        private ObservableCollection<BankUser> _klanten;
        private ObservableCollection<KlantBericht> _klantBerichten;

        public MedewerkerPagina()
        {
            InitializeComponent();

            var context = new AppDbContext();
            _transactieService = new TransactieService(context);

            _wachtendeOverschrijvingen = new ObservableCollection<Transactie>();
            _klanten = new ObservableCollection<BankUser>();
            _klantBerichten = new ObservableCollection<KlantBericht>();

            // Controleer of gebruiker medewerker is
            if (!UserSession.IsIngelogd || UserSession.IngelogdeGebruiker == null)
            {
                MessageBox.Show("Je moet ingelogd zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            // Laad data
            LaadWachtendeOverschrijvingen();
            LaadKlanten();
            LaadKlantBerichten();

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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Log uit en ga naar login pagina
            UserSession.LogUit();
            LoginPagina loginPagina = new LoginPagina();
            loginPagina.Show();
            this.Close();
        }


        // ========== TAB 1: WACHTENDE OVERSCHRIJVINGEN ==========

        private async void LaadWachtendeOverschrijvingen()
        {
            try
            {
                var wachtend = await _transactieService.GetWachtendeOverschrijvingenAsync();
                _wachtendeOverschrijvingen.Clear();
                foreach (var t in wachtend)
                {
                    _wachtendeOverschrijvingen.Add(t);
                }
                WachtendeOverschrijvingenListBox.ItemsSource = _wachtendeOverschrijvingen;

                GeenWachtendTextBlock.Visibility = _wachtendeOverschrijvingen.Count == 0 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
                
                // Update teller if exists
                if (WachtendeOverschrijvingenListBox.ItemsSource != null)
                {
                    var count = _wachtendeOverschrijvingen.Count;
                    // You can add a TextBlock for count if needed
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij laden: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnZoekWachtend_Click(object sender, RoutedEventArgs e)
        {
            string zoekterm = ZoekWachtendTextBox?.Text.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(zoekterm))
            {
                LaadWachtendeOverschrijvingen();
                return;
            }

            var gefilterd = _wachtendeOverschrijvingen.Where(t =>
                t.Gebruiker?.Voornaam?.Contains(zoekterm, StringComparison.OrdinalIgnoreCase) == true ||
                t.Gebruiker?.Achternaam?.Contains(zoekterm, StringComparison.OrdinalIgnoreCase) == true ||
                t.Gebruiker?.Email?.Contains(zoekterm, StringComparison.OrdinalIgnoreCase) == true ||
                t.Bedrag.ToString().Contains(zoekterm)
            ).ToList();

            WachtendeOverschrijvingenListBox.ItemsSource = gefilterd;
        }

        private void BtnResetWachtend_Click(object sender, RoutedEventArgs e)
        {
            ZoekWachtendTextBox.Text = "";
            LaadWachtendeOverschrijvingen();
        }

        private void BtnBelKlant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                Transactie transactie = (Transactie)btn.Tag;

                if (transactie?.Gebruiker == null)
                {
                    MessageBox.Show("Klantgegevens niet beschikbaar.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string telefoon = transactie.Gebruiker.Telefoonnummer;
                string naam = $"{transactie.Gebruiker.Voornaam} {transactie.Gebruiker.Achternaam}";

                if (string.IsNullOrWhiteSpace(telefoon))
                {
                    MessageBox.Show("Geen telefoonnummer beschikbaar voor deze klant.", "Fout", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Kopieer naar clipboard
                System.Windows.Clipboard.SetText(telefoon);

                MessageBox.Show(
                    $"Telefoonnummer gekopieerd naar clipboard!\n\n" +
                    $"Klant: {naam}\n" +
                    $"Telefoon: {telefoon}\n\n" +
                    $"Bel de klant en bevestig de overschrijving daarna.",
                    "Telefoonnummer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnBevestig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                Transactie transactie = (Transactie)btn.Tag;

                if (transactie == null)
                {
                    MessageBox.Show("Transactie niet gevonden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var bevestiging = MessageBox.Show(
                    $"Weet u zeker dat u deze overschrijving wilt bevestigen?\n\n" +
                    $"Bedrag: €{transactie.Bedrag:N2}\n" +
                    $"Van: {transactie.VanIban}\n" +
                    $"Naar: {transactie.NaarIban}\n" +
                    $"Klant: {transactie.Gebruiker?.Voornaam} {transactie.Gebruiker?.Achternaam}",
                    "Bevestigen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (bevestiging != MessageBoxResult.Yes)
                    return;

                string medewerkerId = UserSession.IngelogdeGebruiker?.Id ?? "";
                var result = await _transactieService.BevestigOverschrijvingAsync(transactie.Id, medewerkerId);

                if (result.Succes)
                {
                    MessageBox.Show(result.Bericht, "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    LaadWachtendeOverschrijvingen(); // Herlaad lijst
                }
                else
                {
                    MessageBox.Show(result.Bericht, "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAfwijs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                Transactie transactie = (Transactie)btn.Tag;

                if (transactie == null)
                {
                    MessageBox.Show("Transactie niet gevonden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Vraag reden
                var redenWindow = new Window
                {
                    Title = "Reden voor afwijzing",
                    Width = 500,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = System.Windows.Media.Brushes.Black
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                var label = new TextBlock
                {
                    Text = "Geef een reden voor afwijzing:",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 18,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                var textBox = new TextBox
                {
                    Height = 150,
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = System.Windows.Media.Brushes.DarkGray,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var okButton = new Button
                {
                    Content = "Afwijzen",
                    Width = 100,
                    Height = 40,
                    Background = System.Windows.Media.Brushes.Red,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = Cursors.Hand
                };
                var cancelButton = new Button
                {
                    Content = "Annuleren",
                    Width = 100,
                    Height = 40,
                    Background = System.Windows.Media.Brushes.Gray,
                    Foreground = System.Windows.Media.Brushes.White,
                    Cursor = Cursors.Hand
                };

                string reden = "";
                okButton.Click += (s, args) => { reden = textBox.Text; redenWindow.DialogResult = true; };
                cancelButton.Click += (s, args) => { redenWindow.DialogResult = false; };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(label);
                stackPanel.Children.Add(textBox);
                stackPanel.Children.Add(buttonPanel);
                redenWindow.Content = stackPanel;

                if (redenWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(reden))
                {
                    string medewerkerId = UserSession.IngelogdeGebruiker?.Id ?? "";
                    var result = await _transactieService.AfwijsOverschrijvingAsync(transactie.Id, medewerkerId, reden);

                    if (result.Succes)
                    {
                        MessageBox.Show(result.Bericht, "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                        LaadWachtendeOverschrijvingen(); // Herlaad lijst
                    }
                    else
                    {
                        MessageBox.Show(result.Bericht, "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== TAB 2: KLANTGEGEVENS BEHEREN ==========

        private async void LaadKlanten()
        {
            try
            {
                using var context = new AppDbContext();
                var klanten = await context.Users
                    .Include(u => u.Adres)
                    .Where(u => u.Deleted == DateTime.MaxValue)
                    .OrderBy(u => u.Achternaam)
                    .ThenBy(u => u.Voornaam)
                    .ToListAsync();

                _klanten.Clear();
                foreach (var k in klanten)
                {
                    _klanten.Add(k);
                }
                KlantenListBox.ItemsSource = _klanten;

                // Update teller
                KlantenTellerTextBlock.Text = $"Klanten overzicht ({_klanten.Count})";

                GeenKlantenTextBlock.Visibility = _klanten.Count == 0 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij laden: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnZoekKlant_Click(object sender, RoutedEventArgs e)
        {
            string zoekterm = ZoekKlantTextBox?.Text.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(zoekterm))
            {
                LaadKlanten();
                return;
            }

            var gefilterd = _klanten.Where(k =>
                k.Voornaam?.Contains(zoekterm, StringComparison.OrdinalIgnoreCase) == true ||
                k.Achternaam?.Contains(zoekterm, StringComparison.OrdinalIgnoreCase) == true ||
                k.Email?.Contains(zoekterm, StringComparison.OrdinalIgnoreCase) == true ||
                k.Adres?.Straat?.Contains(zoekterm, StringComparison.OrdinalIgnoreCase) == true ||
                k.Adres?.Gemeente?.Contains(zoekterm, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();

            KlantenListBox.ItemsSource = gefilterd;
            
            // Update teller
            KlantenTellerTextBlock.Text = $"Klanten overzicht ({gefilterd.Count})";
        }

        private void BtnResetKlant_Click(object sender, RoutedEventArgs e)
        {
            ZoekKlantTextBox.Text = "";
            LaadKlanten();
        }

        private async void BtnBewerkenKlant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                string klantId = btn.Tag?.ToString() ?? "";

                if (string.IsNullOrEmpty(klantId))
                {
                    MessageBox.Show("Klant ID niet gevonden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using var context = new AppDbContext();
                var gebruiker = await context.Users
                    .Include(u => u.Adres)
                    .FirstOrDefaultAsync(u => u.Id == klantId);

                if (gebruiker == null)
                {
                    MessageBox.Show("Klant niet gevonden in database.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Toon bewerkingsformulier in een popup window
                var bewerkWindow = new Window
                {
                    Title = $"Klant Bewerken: {gebruiker.Voornaam} {gebruiker.Achternaam}",
                    Width = 800,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1f2e"))
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                // Voornaam
                var voornaamLabel = new TextBlock { Text = "Voornaam *", Foreground = Brushes.White, FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
                var voornaamBox = new TextBox { Text = gebruiker.Voornaam, Height = 35, FontSize = 14, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f2533")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a4152")), Padding = new Thickness(10, 8, 0, 0), Margin = new Thickness(0, 0, 0, 15) };
                stackPanel.Children.Add(voornaamLabel);
                stackPanel.Children.Add(voornaamBox);

                // Achternaam
                var achternaamLabel = new TextBlock { Text = "Achternaam *", Foreground = Brushes.White, FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
                var achternaamBox = new TextBox { Text = gebruiker.Achternaam, Height = 35, FontSize = 14, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f2533")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a4152")), Padding = new Thickness(10, 8, 0, 0), Margin = new Thickness(0, 0, 0, 15) };
                stackPanel.Children.Add(achternaamLabel);
                stackPanel.Children.Add(achternaamBox);

                // Straat
                var straatLabel = new TextBlock { Text = "Straatnaam *", Foreground = Brushes.White, FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
                var straatBox = new TextBox { Text = gebruiker.Adres?.Straat ?? "", Height = 35, FontSize = 14, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f2533")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a4152")), Padding = new Thickness(10, 8, 0, 0), Margin = new Thickness(0, 0, 0, 15) };
                stackPanel.Children.Add(straatLabel);
                stackPanel.Children.Add(straatBox);

                // Huisnummer
                var huisnummerLabel = new TextBlock { Text = "Huisnummer *", Foreground = Brushes.White, FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
                var huisnummerBox = new TextBox { Text = gebruiker.Adres?.Huisnummer ?? "", Height = 35, FontSize = 14, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f2533")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a4152")), Padding = new Thickness(10, 8, 0, 0), Margin = new Thickness(0, 0, 0, 15) };
                stackPanel.Children.Add(huisnummerLabel);
                stackPanel.Children.Add(huisnummerBox);

                // Postcode
                var postcodeLabel = new TextBlock { Text = "Postcode *", Foreground = Brushes.White, FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
                var postcodeBox = new TextBox { Text = gebruiker.Adres?.Postcode ?? "", Height = 35, FontSize = 14, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f2533")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a4152")), Padding = new Thickness(10, 8, 0, 0), Margin = new Thickness(0, 0, 0, 15) };
                stackPanel.Children.Add(postcodeLabel);
                stackPanel.Children.Add(postcodeBox);

                // Gemeente
                var gemeenteLabel = new TextBlock { Text = "Gemeente *", Foreground = Brushes.White, FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
                var gemeenteBox = new TextBox { Text = gebruiker.Adres?.Gemeente ?? "", Height = 35, FontSize = 14, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f2533")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a4152")), Padding = new Thickness(10, 8, 0, 0), Margin = new Thickness(0, 0, 0, 20) };
                stackPanel.Children.Add(gemeenteLabel);
                stackPanel.Children.Add(gemeenteBox);

                // Buttons
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
                var saveButton = new Button { Content = "Opslaan", Width = 120, Height = 40, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e")), Foreground = Brushes.White, Margin = new Thickness(0, 0, 10, 0), Cursor = Cursors.Hand };
                var cancelButton = new Button { Content = "Annuleren", Width = 120, Height = 40, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280")), Foreground = Brushes.White, Cursor = Cursors.Hand };

                saveButton.Click += async (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(voornaamBox.Text) || string.IsNullOrWhiteSpace(achternaamBox.Text))
                    {
                        MessageBox.Show("Voornaam en achternaam zijn verplicht.", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    using var updateContext = new AppDbContext();
                    var dbKlant = await updateContext.Users
                        .Include(u => u.Adres)
                        .FirstOrDefaultAsync(u => u.Id == klantId);

                    if (dbKlant != null)
                    {
                        dbKlant.Voornaam = voornaamBox.Text.Trim();
                        dbKlant.Achternaam = achternaamBox.Text.Trim();

                        if (dbKlant.Adres == null)
                        {
                            dbKlant.Adres = new Adres();
                        }

                        dbKlant.Adres.Straat = straatBox.Text.Trim();
                        dbKlant.Adres.Huisnummer = huisnummerBox.Text.Trim();
                        dbKlant.Adres.Postcode = postcodeBox.Text.Trim();
                        dbKlant.Adres.Gemeente = gemeenteBox.Text.Trim();

                        await updateContext.SaveChangesAsync();
                        MessageBox.Show("Klantgegevens bijgewerkt!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                        bewerkWindow.Close();
                        LaadKlanten();
                    }
                };

                cancelButton.Click += (s, args) => bewerkWindow.Close();
                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonPanel);

                bewerkWindow.Content = new ScrollViewer { Content = stackPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                bewerkWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnKlantOpslaan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                BankUser klant = (BankUser)btn.Tag;

                if (klant == null)
                {
                    MessageBox.Show("Klant niet gevonden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Haal de TextBox waarden op uit de DataTemplate via VisualTree
                var parent = VisualTreeHelper.GetParent(btn) as DependencyObject;
                while (parent != null && !(parent is Border))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (parent == null) return;

                var voornaamBox = FindVisualChild<TextBox>(parent, "EditVoornaam");
                var achternaamBox = FindVisualChild<TextBox>(parent, "EditAchternaam");
                var straatBox = FindVisualChild<TextBox>(parent, "EditStraat");
                var huisnummerBox = FindVisualChild<TextBox>(parent, "EditHuisnummer");
                var busBox = FindVisualChild<TextBox>(parent, "EditBus");
                var postcodeBox = FindVisualChild<TextBox>(parent, "EditPostcode");
                var gemeenteBox = FindVisualChild<TextBox>(parent, "EditGemeente");

                // Validatie
                if (voornaamBox == null || string.IsNullOrWhiteSpace(voornaamBox.Text))
                {
                    MessageBox.Show("Voornaam is verplicht.", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (achternaamBox == null || string.IsNullOrWhiteSpace(achternaamBox.Text))
                {
                    MessageBox.Show("Achternaam is verplicht.", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update klant
                using var context = new AppDbContext();
                var dbKlant = await context.Users
                    .Include(u => u.Adres)
                    .FirstOrDefaultAsync(u => u.Id == klant.Id);

                if (dbKlant == null)
                {
                    MessageBox.Show("Klant niet gevonden in database.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                dbKlant.Voornaam = voornaamBox.Text.Trim();
                dbKlant.Achternaam = achternaamBox.Text.Trim();

                // Update adres
                if (dbKlant.Adres == null)
                {
                    dbKlant.Adres = new Adres();
                }

                if (straatBox != null) dbKlant.Adres.Straat = straatBox.Text.Trim();
                if (huisnummerBox != null) dbKlant.Adres.Huisnummer = huisnummerBox.Text.Trim();
                if (busBox != null) dbKlant.Adres.Bus = string.IsNullOrWhiteSpace(busBox.Text) ? null : busBox.Text.Trim();
                if (postcodeBox != null) dbKlant.Adres.Postcode = postcodeBox.Text.Trim();
                if (gemeenteBox != null) dbKlant.Adres.Gemeente = gemeenteBox.Text.Trim();

                await context.SaveChangesAsync();

                MessageBox.Show($"Klant {dbKlant.Voornaam} {dbKlant.Achternaam} bijgewerkt!", "Succes", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LaadKlanten(); // Herlaad lijst
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (child as FrameworkElement)?.Name == name)
                    return t;

                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        // ========== TAB 3: KLANTBERICHTEN ==========

        private async void LaadKlantBerichten()
        {
            try
            {
                using var context = new AppDbContext();
                var berichten = await context.KlantBerichten
                    .OrderByDescending(b => b.Datum)
                    .ToListAsync();

                _klantBerichten.Clear();
                foreach (var b in berichten)
                {
                    _klantBerichten.Add(b);
                }
                KlantBerichtenListBox.ItemsSource = _klantBerichten;

                GeenBerichtenTextBlock.Visibility = _klantBerichten.Count == 0 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij laden berichten: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnBehandeld_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                var bericht = btn.Tag as KlantBericht;

                if (bericht == null)
                {
                    MessageBox.Show("Bericht niet gevonden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using var context = new AppDbContext();
                var dbBericht = await context.KlantBerichten
                    .FirstOrDefaultAsync(b => b.Id == bericht.Id);

                if (dbBericht != null)
                {
                    dbBericht.Status = "Afgehandeld";
                    dbBericht.BehandeldDoor = UserSession.IngelogdeGebruiker?.Id;
                    dbBericht.BehandeldOp = DateTime.Now;

                    await context.SaveChangesAsync();
                    MessageBox.Show("Bericht gemarkeerd als afgehandeld.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    LaadKlantBerichten();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}

