using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for AdminPagina.xaml
    /// </summary>
    public partial class AdminPagina : Window
    {
        // Property voor data binding van klanten
        private System.Collections.ObjectModel.ObservableCollection<BankUser> _klanten;
        public System.Collections.ObjectModel.ObservableCollection<BankUser> Klanten
        {
            get { return _klanten; }
            set { _klanten = value; }
        }

        public AdminPagina()
        {
            InitializeComponent();
            Klanten = new System.Collections.ObjectModel.ObservableCollection<BankUser>();
            LaadKlanten(); // Laad klanten bij opstarten
            LaadKaarten(); // Laad kaarten voor Kaarten tab
            
            // Event handler voor zoekveld wordt via XAML gebonden
        }

        // Laad alle klanten uit de database
        private void LaadKlanten()
        {
            try
            {
                using var context = new AppDbContext();
                var gebruikers = context.Users.Include(g => g.Adres).Include(g => g.Rekeningen).ToList();
                    Klanten.Clear();
                foreach (var g in gebruikers) Klanten.Add(g);
                    KlantenListBox.ItemsSource = Klanten;
                    KlantenTellerTextBlock.Text = $"Klanten overzicht ({gebruikers.Count})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij laden klanten: {ex.Message}", "Database Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Zoek klanten (LINQ QUERY SYNTAX) - Case-insensitive
        private void ZoekKlanten(string zoekterm)
        {
            if (string.IsNullOrWhiteSpace(zoekterm)) { LaadKlanten(); return; }
            try
            {
                using var context = new AppDbContext();
                string zoekLower = zoekterm.ToLower();
                var result = (from g in context.Users
                             where (g.Voornaam != null && g.Voornaam.ToLower().Contains(zoekLower)) || 
                                   (g.Achternaam != null && g.Achternaam.ToLower().Contains(zoekLower)) || 
                                   (g.Email != null && g.Email.ToLower().Contains(zoekLower)) || 
                                   (g.Telefoonnummer != null && g.Telefoonnummer.Contains(zoekterm)) || 
                                   (g.UserName != null && g.UserName.ToLower().Contains(zoekLower)) ||
                                   (g.Adres != null && (
                                       (g.Adres.Gemeente != null && g.Adres.Gemeente.ToLower().Contains(zoekLower)) || 
                                       (g.Adres.Straat != null && g.Adres.Straat.ToLower().Contains(zoekLower)) || 
                                       (g.Adres.Postcode != null && g.Adres.Postcode.Contains(zoekterm))))
                             select g).Include(g => g.Adres).Include(g => g.Rekeningen).ToList();
                Klanten.Clear();
                foreach (var g in result) Klanten.Add(g);
                KlantenListBox.ItemsSource = Klanten;
                KlantenTellerTextBlock.Text = $"Zoekresultaten ({result.Count}) voor '{zoekterm}'";
            }
            catch (Exception ex) { MessageBox.Show($"Fout bij zoeken: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnZoek_Click(object sender, RoutedEventArgs e) => ZoekKlanten(ZoekTextBox?.Text.Trim() ?? "");
        private void BtnToonAlles_Click(object sender, RoutedEventArgs e) { ZoekTextBox?.Clear(); LaadKlanten(); }
        private void BtnZoekKaart_Click(object sender, RoutedEventArgs e) => ZoekKaarten(ZoekKaartTextBox?.Text.Trim() ?? "");


        // Laad alle kaarten
        private void LaadKaarten()
        {
            try
            {
                using var context = new AppDbContext();
                var kaarten = context.Kaarten.Include(k => k.Gebruiker).ToList();
                    KaartenListBox.ItemsSource = kaarten;
                KaartenTellerTextBlock.Text = $"Kaart beheer ({kaarten.Count})";
            }
            catch (Exception ex) { MessageBox.Show($"Fout bij laden kaarten: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // Zoek kaarten (LINQ QUERY SYNTAX) - Case-insensitive
        private void ZoekKaarten(string zoekterm)
        {
            if (string.IsNullOrWhiteSpace(zoekterm)) { LaadKaarten(); return; }
            try
            {
                using var context = new AppDbContext();
                string zoekLower = zoekterm.ToLower();
                var result = (from k in context.Kaarten
                             where k.KaartNummer.Contains(zoekterm) || 
                                   (k.Gebruiker != null && k.Gebruiker.Email != null && k.Gebruiker.Email.ToLower().Contains(zoekLower))
                             select k).Include(k => k.Gebruiker).ToList();
                KaartenListBox.ItemsSource = result;
                KaartenTellerTextBlock.Text = $"Zoekresultaten ({result.Count}) voor '{zoekterm}'";
            }
            catch (Exception ex) { MessageBox.Show($"Fout bij zoeken: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnTerug_Click(object sender, RoutedEventArgs e)
        {
            StartPagina startPagina = new StartPagina();
            startPagina.Show();
            this.Close();
        }

        // ========

        //  KLANTEN TAB - Event Handlers 

        // Klant toevoegen of bijwerken
        private async void BtnKlantOpslaan_Click(object sender, RoutedEventArgs e)
        {
            // Validatie: controleer of verplichte velden zijn ingevuld
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Voornaam is verplicht!", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Achternaam is verplicht!", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Email is verplicht!", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (BirthDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Geboortedatum is verplicht!", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                using (var userManager = new UserManager<BankUser>(
                    new UserStore<BankUser>(context),
                    null!, new PasswordHasher<BankUser>(),
                    null!, null!, null!, null!, null!, null!))
                {
                    // Zorg dat rollen bestaan voordat we ze toewijzen
                    var roleManager = new RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>(
                        new RoleStore<Microsoft.AspNetCore.Identity.IdentityRole>(context),
                        null!, null!, null!, null!);

                    // Controleer en maak rollen aan als ze niet bestaan
                    string[] rollenNamen = { "Klant", "Medewerker", "Admin" };
                    foreach (var rolNaam in rollenNamen)
                    {
                        if (!await roleManager.RoleExistsAsync(rolNaam))
                        {
                            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole
                            {
                                Name = rolNaam,
                                NormalizedName = rolNaam.ToUpper()
                            });
                        }
                    }

                    // Controleer of het een update of nieuwe klant is
                    bool isUpdate = !string.IsNullOrEmpty(CustomerIdTextBox.Text);

                    if (isUpdate)
                    {
                        // BESTAANDE KLANT BIJWERKEN
                        string customerId = CustomerIdTextBox.Text;
                        var gebruiker = context.Users
                            .Include(g => g.Adres)
                            .FirstOrDefault(g => g.Id == customerId);

                        if (gebruiker != null)
                        {
                            // Update gegevens
                            gebruiker.Voornaam = FirstNameTextBox.Text.Trim();
                            gebruiker.Achternaam = LastNameTextBox.Text.Trim();
                            gebruiker.Email = EmailTextBox.Text.Trim();
                            gebruiker.Telefoonnummer = PhoneNumberTextBox.Text.Trim();
                            gebruiker.Geboortedatum = BirthDatePicker.SelectedDate.Value;
                            gebruiker.Straatnaam = StreetNameTextBox.Text.Trim();
                            gebruiker.Huisnummer = HouseNumberTextBox.Text.Trim();
                            gebruiker.Bus = string.IsNullOrWhiteSpace(BusTextBox.Text) ? null : BusTextBox.Text.Trim();
                            gebruiker.Postcode = PostcodeTextBox.Text.Trim();
                            gebruiker.Gemeente = CityTextBox.Text.Trim();
                            gebruiker.Land = CountryTextBox.Text.Trim();

                            // Update rol via Identity UserManager (gebruik bestaande userManager)
                            // Verwijder alle rollen en voeg nieuwe toe
                            var huidigeRollen = await userManager.GetRolesAsync(gebruiker);
                            await userManager.RemoveFromRolesAsync(gebruiker, huidigeRollen);

                            string[] rollen = { "Klant", "Medewerker", "Admin" };
                            await userManager.AddToRoleAsync(gebruiker, rollen[RoleComboBox.SelectedIndex >= 0 && RoleComboBox.SelectedIndex < 3 ? RoleComboBox.SelectedIndex : 0]);

                            // Update wachtwoord alleen als er een nieuw wachtwoord is ingevoerd
                            if (!string.IsNullOrEmpty(PasswordBox.Password))
                            {
                                var token = await userManager.GeneratePasswordResetTokenAsync(gebruiker);
                                await userManager.ResetPasswordAsync(gebruiker, token, PasswordBox.Password);
                            }

                            // Sla wijzigingen op
                            await context.SaveChangesAsync();

                            MessageBox.Show($"Klant {customerId} bijgewerkt!\n\n" +
                                $"Email: {gebruiker.Email}",
                                "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Form leegmaken na bijwerken
                            ClearForm();
                            LaadKlanten(); // Herlaad klanten lijst
                        }
                        else
                        {
                            MessageBox.Show("Gebruiker niet gevonden in database!",
                                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        // NIEUWE KLANT TOEVOEGEN
                        // Maak nieuwe adres aan
                        var adres = new Adres
                        {
                            Straat = StreetNameTextBox.Text.Trim(),
                            Huisnummer = HouseNumberTextBox.Text.Trim(),
                            Bus = string.IsNullOrWhiteSpace(BusTextBox.Text) ? null : BusTextBox.Text.Trim(),
                            Postcode = PostcodeTextBox.Text.Trim(),
                            Gemeente = CityTextBox.Text.Trim(),
                            Land = CountryTextBox.Text.Trim()
                        };

                        // Maak nieuwe gebruiker aan met Identity Framework
                        var nieuweGebruiker = new BankUser
                        {
                            UserName = EmailTextBox.Text.Trim(),
                            Email = EmailTextBox.Text.Trim(),
                            EmailConfirmed = true,
                            Voornaam = FirstNameTextBox.Text.Trim(),
                            Achternaam = LastNameTextBox.Text.Trim(),
                            Telefoonnummer = PhoneNumberTextBox.Text.Trim(),
                            Geboortedatum = BirthDatePicker.SelectedDate.Value,
                            Adres = adres
                        };

                        // Maak gebruiker aan met Identity UserManager
                        var result = await userManager.CreateAsync(nieuweGebruiker, PasswordBox.Password);
                        if (!result.Succeeded)
                        {
                            MessageBox.Show($"Fout bij aanmaken gebruiker: {string.Join(", ", result.Errors.Select(e => e.Description))}",
                                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Wacht tot gebruiker is opgeslagen
                        await context.SaveChangesAsync();
                        await context.Entry(nieuweGebruiker).ReloadAsync();

                        string[] rollen = { "Klant", "Medewerker", "Admin" };
                        await userManager.AddToRoleAsync(nieuweGebruiker, rollen[RoleComboBox.SelectedIndex >= 0 && RoleComboBox.SelectedIndex < 3 ? RoleComboBox.SelectedIndex : 0]);
                        await context.SaveChangesAsync();

                        // Automatisch een zichtrekening aanmaken
                        var nieuweRekening = new Rekening
                        {
                            Iban = "BE" + DateTime.Now.Ticks.ToString().Substring(0, 10),
                            Saldo = 0.0m,
                            GebruikerId = nieuweGebruiker.Id
                        };
                        context.Rekeningen.Add(nieuweRekening);
                        await context.SaveChangesAsync();

                        // Automatisch een kaart aanmaken voor de nieuwe gebruiker
                        string kaartNummer = GenereerUniekKaartNummer(context);
                        var nieuweKaart = new Kaart
                        {
                            KaartNummer = kaartNummer,
                            Status = KaartStatus.Actief,
                            GebruikerId = nieuweGebruiker.Id
                        };
                        context.Kaarten.Add(nieuweKaart);
                        await context.SaveChangesAsync();

                        MessageBox.Show($"Nieuwe klant toegevoegd!\n\n" +
                            $"Email: {EmailTextBox.Text}\n" +
                            $"ID: {nieuweGebruiker.Id}\n" +
                            $"IBAN: {nieuweRekening.Iban}\n" +
                            $"Kaartnummer: {nieuweKaart.KaartNummer}",
                            "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Form leegmaken na toevoegen
                        ClearForm();
                        LaadKlanten(); // Herlaad klanten lijst
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij opslaan: {ex.Message}",
                    "Database Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Password hashing functie met SHA256 (zelfde als LoginPagina)
        // HashPassword niet meer nodig - Identity Framework doet dit automatisch

        // Genereer uniek kaartnummer (formaat: XXXX-XXXX-XXXX-XXXX)
        private string GenereerUniekKaartNummer(AppDbContext db)
        {
            var random = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
            for (int i = 0; i < 100; i++)
            {
                var nummer = string.Join("-", Enumerable.Range(0, 4).Select(_ => random.Next(1000, 10000).ToString()));
                if (!db.Kaarten.Any(k => k.KaartNummer == nummer)) return nummer;
                }
            return string.Join("-", Enumerable.Range(0, 4).Select(_ => random.Next(1000, 10000).ToString())) + "-" + DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 4));
        }



        // Bewerken - laad klant gegevens in formulier voor bewerken
        private async void BtnBewerken_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string customerId = btn.Tag.ToString();

            try
            {
                using (var context = new AppDbContext())
                using (var userManager = new UserManager<BankUser>(
                    new UserStore<BankUser>(context),
                    null!, new PasswordHasher<BankUser>(),
                    null!, null!, null!, null!, null!, null!))
                {
                    // Zoek gebruiker in database
                    var gebruiker = context.Users
                        .Include(g => g.Adres)
                        .FirstOrDefault(g => g.Id == customerId);

                    if (gebruiker != null)
                    {
                        // Vul formulier met gebruikersgegevens
                        CustomerIdTextBox.Text = gebruiker.Id;
                        FirstNameTextBox.Text = gebruiker.Voornaam ?? "";
                        LastNameTextBox.Text = gebruiker.Achternaam ?? "";
                        EmailTextBox.Text = gebruiker.Email ?? "";
                        PhoneNumberTextBox.Text = gebruiker.Telefoonnummer ?? "";
                        BirthDatePicker.SelectedDate = gebruiker.Geboortedatum;
                        StreetNameTextBox.Text = gebruiker.Straatnaam ?? "";
                        HouseNumberTextBox.Text = gebruiker.Huisnummer ?? "";
                        BusTextBox.Text = gebruiker.Bus ?? "";
                        PostcodeTextBox.Text = gebruiker.Postcode ?? "";
                        CityTextBox.Text = gebruiker.Gemeente ?? "";
                        CountryTextBox.Text = gebruiker.Land ?? "";

                        var rollen = await userManager.GetRolesAsync(gebruiker);
                        RoleComboBox.SelectedIndex = rollen.Contains("Admin") ? 2 : rollen.Contains("Medewerker") ? 1 : 0;

                        // Verander knop tekst naar "Bijwerken"
                        SaveCustomerButton.Content = "✓ Klant bijwerken";

                        // Ga naar Klanten tab
                        // Zoek de TabControl en selecteer eerste tab (Klanten)
                        var mainGrid = this.Content as Grid;
                        if (mainGrid != null)
                        {
                            var tabControl = mainGrid.Children.OfType<TabControl>().FirstOrDefault();
                            if (tabControl != null)
                            {
                                tabControl.SelectedIndex = 0; // Eerste tab = Klanten
                            }
                        }

                        MessageBox.Show($"Klant {customerId} geladen voor bewerken.\n\nGa naar Klanten tab om te bewerken.",
                            "Klant Bewerken", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Gebruiker niet gevonden!",
                            "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij laden klant: {ex.Message}",
                    "Database Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Verwijderen - klant verwijderen
        private void BtnVerwijderen_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string customerId = btn.Tag.ToString();

            // Bevestiging vragen
            var result = MessageBox.Show(
                $"Weet u zeker dat u klant {customerId} wilt verwijderen?\n\n" +
                "Dit verwijdert ook alle gekoppelde rekeningen, kaarten en transacties!",
                "Bevestigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new AppDbContext())
                    {
                        // Zoek gebruiker
                        var gebruiker = context.Users
                            .Include(g => g.Adres)
                            .FirstOrDefault(g => g.Id == customerId);

                        if (gebruiker != null)
                        {
                            // Soft delete gebruiker en adres
                            gebruiker.Deleted = DateTime.UtcNow;
                            if (gebruiker.Adres != null)
                                gebruiker.Adres.Deleted = DateTime.UtcNow;
                            context.SaveChanges();

                            MessageBox.Show($"Klant {customerId} is verwijderd!",
                                "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                            LaadKlanten(); // Herlaad klanten lijst
                        }
                        else
                        {
                            MessageBox.Show("Gebruiker niet gevonden!",
                                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij verwijderen: {ex.Message}",
                        "Database Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        // Annuleren knop - leeg formulier
        private void BtnAnnuleren_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Helper methode: form leegmaken
        private void ClearForm()
        {
            CustomerIdTextBox.Text = ""; // Leeg maken voor nieuwe klant
            EmailTextBox.Clear();
            PasswordBox.Clear();
            PhoneNumberTextBox.Clear();
            BirthDatePicker.SelectedDate = null;
            StreetNameTextBox.Clear();
            HouseNumberTextBox.Clear();
            BusTextBox.Clear();
            PostcodeTextBox.Clear();
            CityTextBox.Clear();
            CountryTextBox.Text = "België";
            RoleComboBox.SelectedIndex = -1;
            FirstNameTextBox.Clear();
            LastNameTextBox.Clear();

            // Verander knop tekst terug naar "Toevoegen"
            SaveCustomerButton.Content = "👤 Klant toevoegen";
        }



        //   KAARTEN TAB - Event Handlers ==========

        // Kaart actief maken
        private void BtnActiefMaken_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int cardId = int.Parse(btn.Tag.ToString());

            var result = MessageBox.Show(
                $"Kaart {cardId} actief maken?",
                "Bevestigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new AppDbContext())
                    {
                        // Zoek kaart in database
                        var kaart = context.Kaarten
                            .FirstOrDefault(k => k.Id == cardId && k.Deleted == DateTime.MaxValue);

                        if (kaart != null)
                        {
                            // Update status naar Actief
                            kaart.Status = KaartStatus.Actief;
                            context.SaveChanges();

                            MessageBox.Show($"Kaart {cardId} is actief gemaakt!",
                                "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                            LaadKaarten(); // Herlaad kaarten lijst
                        }
                        else
                        {
                            MessageBox.Show("Kaart niet gevonden!",
                                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij actief maken kaart: {ex.Message}",
                        "Database Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //   Kaart bevriezen
        private void BtnBevriezen_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int cardId = int.Parse(btn.Tag.ToString());

            var result = MessageBox.Show(
                $"Kaart {cardId} bevriezen?",
                "Bevestigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new AppDbContext())
                    {
                        // Zoek kaart in database
                        var kaart = context.Kaarten
                            .FirstOrDefault(k => k.Id == cardId && k.Deleted == DateTime.MaxValue);

                        if (kaart != null)
                        {
                            // Update status naar Bevroren
                            kaart.Status = KaartStatus.Bevroren;
                            context.SaveChanges();

                            MessageBox.Show($"Kaart {cardId} is bevroren!",
                                "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                            LaadKaarten(); // Herlaad kaarten lijst
                        }
                        else
                        {
                            MessageBox.Show("Kaart niet gevonden!",
                                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij bevriezen kaart: {ex.Message}",
                        "Database Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Kaart blokkeren
        private void BtnBlokkeren_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int cardId = int.Parse(btn.Tag.ToString());

            var result = MessageBox.Show(
                $"Kaart {cardId} permanent blokkeren?\n\n" +
                "Deze actie kan niet ongedaan worden gemaakt!",
                "Waarschuwing",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new AppDbContext())
                    {
                        // Zoek kaart in database
                        var kaart = context.Kaarten
                            .FirstOrDefault(k => k.Id == cardId && k.Deleted == DateTime.MaxValue);

                        if (kaart != null)
                        {
                            // Update status naar Geblokkeerd
                            kaart.Status = KaartStatus.Geblokkeerd;
                            context.SaveChanges();

                            MessageBox.Show($"Kaart {cardId} is geblokkeerd!",
                                "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                            LaadKaarten(); // Herlaad kaarten lijst
                        }
                        else
                        {
                            MessageBox.Show("Kaart niet gevonden!",
                                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij blokkeren kaart: {ex.Message}",
                        "Database Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}