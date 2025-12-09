using BankApp_Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BankApp_WPF
{
    public partial class RegistratiePagina : Window
    {
        public RegistratiePagina()
        {
            InitializeComponent();
            LandBox.Text = "België";
            this.KeyDown += Window_KeyDown;
            this.Focusable = true;
            this.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                e.Handled = true;
                StartPagina startPagina = new StartPagina();
                startPagina.Show();
                this.Close();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            StartPagina startPagina = new StartPagina();
            startPagina.Show();
            this.Close();
        }

        private async void RegistreerBtn_Click(object sender, RoutedEventArgs e)
        {
            string fouten = ValideerFormulier();

            if (!string.IsNullOrEmpty(fouten))
            {
                MessageBox.Show(fouten, "Fouten bij registratie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                using (var userManager = new UserManager<BankUser>(
                    new UserStore<BankUser>(db),
                    null!, new PasswordHasher<BankUser>(),
                    null!, null!, null!, null!, null!, null!))
                {
                    // Zorg dat rollen bestaan voordat we ze toewijzen
                    var roleManager = new RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>(
                        new RoleStore<Microsoft.AspNetCore.Identity.IdentityRole>(db),
                        null!, null!, null!, null!);

                    // Controleer en maak rollen aan als ze niet bestaan
                    string[] rollen = { "Klant", "Medewerker", "Admin" };
                    foreach (var rolNaam in rollen)
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

                    // Controleer of e-mailadres al bestaat
                    bool bestaatAl = await db.Users.AnyAsync(u => u.Email == EmailBox.Text.Trim());
                    if (bestaatAl)
                    {
                        MessageBox.Show("Er bestaat al een account met dit e-mailadres.",
                                        "Registratie mislukt",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return;
                    }

                    // Nieuwe adres aanmaken
                    var adres = new Adres
                    {
                        Straat = StraatBox.Text.Trim(),
                        Huisnummer = HuisnrBox.Text.Trim(),
                        Bus = string.IsNullOrWhiteSpace(BusBox.Text) ? null : BusBox.Text.Trim(),
                        Postcode = PostcodeBox.Text.Trim(),
                        Gemeente = GemeenteBox.Text.Trim(),
                        Land = LandBox.Text.Trim()
                    };

                    // Nieuwe gebruiker aanmaken met Identity Framework
                    var gebruiker = new BankUser
                    {
                        UserName = EmailBox.Text.Trim(),
                        Email = EmailBox.Text.Trim(),
                        EmailConfirmed = true,
                        Voornaam = VoornaamBox.Text.Trim(),
                        Achternaam = AchternaamBox.Text.Trim(),
                        Telefoonnummer = TelefoonBox.Text.Trim(),
                        Geboortedatum = GeboortePicker.SelectedDate.Value,
                        Adres = adres
                    };

                    // Maak gebruiker aan met Identity UserManager
                    var result = await userManager.CreateAsync(gebruiker, WachtwoordBox.Password);
                    if (!result.Succeeded)
                    {
                        MessageBox.Show($"Fout bij aanmaken account: {string.Join(", ", result.Errors.Select(e => e.Description))}",
                                        "Registratie mislukt",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return;
                    }

                    // Voeg standaard rol "Klant" toe
                    await userManager.AddToRoleAsync(gebruiker, "Klant");

                    // Wacht tot gebruiker is opgeslagen
                    await db.SaveChangesAsync();
                    await db.Entry(gebruiker).ReloadAsync();

                    // Automatisch een zichtrekening aanmaken
                    var nieuweRekening = new Rekening
                    {
                        Iban = "BE" + DateTime.Now.Ticks.ToString().Substring(0, 10),
                        Saldo = 0.0m,
                        GebruikerId = gebruiker.Id,
                        Deleted = DateTime.MaxValue // Soft-delete: niet verwijderd
                    };

                    db.Rekeningen.Add(nieuweRekening);
                    await db.SaveChangesAsync();

                    // Automatisch een kaart aanmaken voor de nieuwe gebruiker
                    try
                    {
                        string kaartNummer = GenereerUniekKaartNummer(db);
                        var nieuweKaart = new Kaart
                        {
                            KaartNummer = kaartNummer,
                            Status = KaartStatus.Actief,
                            GebruikerId = gebruiker.Id,
                            Deleted = DateTime.MaxValue // Soft-delete: niet verwijderd
                        };

                        db.Kaarten.Add(nieuweKaart);
                        await db.SaveChangesAsync();
                        
                        Console.WriteLine($"Kaart aangemaakt: {kaartNummer} voor gebruiker {gebruiker.Id}");
                    }
                    catch (Exception kaartEx)
                    {
                        Console.WriteLine($"Fout bij aanmaken kaart: {kaartEx.Message}");
                        // Kaart is optioneel, registratie kan doorgaan
                    }
                }

                MessageBox.Show(
                    "Registratie geslaagd!\n\n" +
                    "Uw account is succesvol aangemaakt.\n" +
                    "Uw IBAN en kaartnummer worden automatisch toegewezen.\n\n" +
                    "U wordt nu doorgestuurd naar de login pagina.",
                    "Succes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                MaakVeldenLeeg();

                LoginPagina loginPagina = new LoginPagina();
                loginPagina.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er trad een fout op bij het registreren:\n{ex.Message}",
                                "Databasefout",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }


        // --- VALIDATIE EN HELPERS ---

        private string ValideerFormulier()
        {
            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrWhiteSpace(VoornaamBox.Text))
                sb.AppendLine("• Voornaam is verplicht.");
            else if (VoornaamBox.Text.Length < 2)
                sb.AppendLine("• Voornaam moet minimaal 2 tekens bevatten.");

            if (string.IsNullOrWhiteSpace(AchternaamBox.Text))
                sb.AppendLine("• Achternaam is verplicht.");
            else if (AchternaamBox.Text.Length < 2)
                sb.AppendLine("• Achternaam moet minimaal 2 tekens bevatten.");

            if (!IsGeldigEmail(EmailBox.Text))
                sb.AppendLine("• Voer een geldig e-mailadres in.");

            if (WachtwoordBox.Password.Length < 8)
                sb.AppendLine("• Wachtwoord moet minimaal 8 tekens bevatten.");
            else if (!HeeftHoofdletterEnCijfer(WachtwoordBox.Password))
                sb.AppendLine("• Wachtwoord moet minimaal 1 hoofdletter en 1 cijfer bevatten.");

            if (WachtwoordBox.Password != BevestigBox.Password)
                sb.AppendLine("• Wachtwoorden komen niet overeen.");

            if (string.IsNullOrWhiteSpace(TelefoonBox.Text))
                sb.AppendLine("• Telefoonnummer is verplicht.");
            else if (!Regex.IsMatch(TelefoonBox.Text.Replace(" ", "").Replace("+", ""), @"^\d+$"))
                sb.AppendLine("• Telefoonnummer mag alleen cijfers bevatten (+ en spaties zijn toegestaan).");

            if (GeboortePicker.SelectedDate == null)
                sb.AppendLine("• Geboortedatum is verplicht.");
            else if (!IsOuderDan18(GeboortePicker.SelectedDate.Value))
                sb.AppendLine("• Je moet minimaal 18 jaar oud zijn.");

            if (string.IsNullOrWhiteSpace(StraatBox.Text))
                sb.AppendLine("• Straatnaam is verplicht.");

            if (string.IsNullOrWhiteSpace(HuisnrBox.Text))
                sb.AppendLine("• Huisnummer is verplicht.");

            if (string.IsNullOrWhiteSpace(PostcodeBox.Text))
                sb.AppendLine("• Postcode is verplicht.");
            else if (!Regex.IsMatch(PostcodeBox.Text, @"^\d{4}$"))
                sb.AppendLine("• Postcode moet 4 cijfers bevatten.");

            if (string.IsNullOrWhiteSpace(GemeenteBox.Text))
                sb.AppendLine("• Gemeente is verplicht.");

            if (string.IsNullOrWhiteSpace(LandBox.Text))
                sb.AppendLine("• Land is verplicht.");

            return sb.ToString();
        }

        private bool IsGeldigEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

        private bool IsOuderDan18(DateTime geboortedatum)
        {
            int leeftijd = DateTime.Now.Year - geboortedatum.Year;
            if (geboortedatum.Date > DateTime.Now.AddYears(-leeftijd))
                leeftijd--;
            return leeftijd >= 18;
        }

        private bool HeeftHoofdletterEnCijfer(string wachtwoord) =>
            Regex.IsMatch(wachtwoord, @"[A-Z]") && Regex.IsMatch(wachtwoord, @"\d");

        private void MaakVeldenLeeg()
        {
            VoornaamBox.Clear();
            AchternaamBox.Clear();
            EmailBox.Clear();
            WachtwoordBox.Clear();
            BevestigBox.Clear();
            TelefoonBox.Clear();
            StraatBox.Clear();
            HuisnrBox.Clear();
            BusBox.Clear();
            PostcodeBox.Clear();
            GemeenteBox.Clear();
            LandBox.Clear();
            GeboortePicker.SelectedDate = null;
        }

        // HashWachtwoord niet meer nodig - Identity Framework doet dit automatisch

        // Genereer uniek kaartnummer (formaat: XXXX-XXXX-XXXX-XXXX)
        private string GenereerKaartNummer()
        {
            // Gebruik DateTime.Ticks als seed voor betere randomisatie
            Random random = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
            string kaartNummer = "";

            // Genereer 4 groepen van 4 cijfers
            for (int i = 0; i < 4; i++)
            {
                if (i > 0) kaartNummer += "-";
                kaartNummer += random.Next(1000, 10000).ToString();
            }

            return kaartNummer;
        }

        // Genereer uniek kaartnummer en controleer of het al bestaat
        private string GenereerUniekKaartNummer(AppDbContext db)
        {
            string kaartNummer;
            int maxPogingen = 100; // Maximaal 100 pogingen om uniek nummer te vinden
            int poging = 0;

            do
            {
                kaartNummer = GenereerKaartNummer();
                poging++;

                // Controleer of kaartnummer al bestaat
                bool bestaatAl = db.Kaarten.Any(k => k.KaartNummer == kaartNummer);
                if (!bestaatAl)
                {
                    return kaartNummer;
                }
            } while (poging < maxPogingen);

            // Als na 100 pogingen nog geen uniek nummer gevonden, voeg timestamp toe
            return GenereerKaartNummer() + "-" + DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 4));
        }
    }
}

