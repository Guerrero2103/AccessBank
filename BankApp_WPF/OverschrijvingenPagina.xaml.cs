using BankApp_BusinessLogic;
using BankApp_Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace BankApp_WPF
{
    public partial class OverschrijvingenPagina : Window
    {
        public OverschrijvingenPagina()
        {
            InitializeComponent();

            if (!UserSession.IsIngelogd)
            {
                MessageBox.Show("Je moet ingelogd zijn om overschrijvingen te doen.",
                    "Niet ingelogd", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
                return;
            }
            this.KeyDown += Window_KeyDown;
            this.Focusable = true;
            this.Focus();

            _ = LaadEigenRekeningenAsync();
        }

        // Vult de "Vanaf rekening"-ComboBox met de eigen rekeningen van de ingelogde gebruiker
        private async Task LaadEigenRekeningenAsync()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var rekeningService = new RekeningService(context);
                    var gebruikerId = UserSession.IngelogdeGebruiker!.Id;
                    var eigenRekeningen = await rekeningService.GetRekeningenByGebruikerIdAsync(gebruikerId);

                    cmbVanRekening.ItemsSource = eigenRekeningen;
                    if (eigenRekeningen.Any())
                    {
                        cmbVanRekening.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij laden van je rekeningen: {ex.Message}", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        

        private void BtnTerug_Click(object sender, RoutedEventArgs e)
        {
            HoofdPagina hoofdPagina = new HoofdPagina();
            hoofdPagina.Show();
            this.Close();
        }


        private void BtnAnnuleren_Click(object sender, RoutedEventArgs e)
        {
            txtIban.Clear();
            txtNaamOntvanger.Clear();
            txtBedrag.Clear();
            txtOmschrijving.Clear();
            MessageBox.Show("Overschrijving geannuleerd.", "Geannuleerd",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnVerzenden_Click(object sender, RoutedEventArgs e)
        {
            // Validaties
            if (cmbVanRekening.SelectedItem is not Rekening vanRekening)
            {
                MessageBox.Show("Kies een rekening om vanaf over te schrijven.", "Validatiefout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtIban.Text))
            {
                MessageBox.Show("Voer een IBAN in.", "Validatiefout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNaamOntvanger.Text))
            {
                MessageBox.Show("Voer de naam van de ontvanger in.", "Validatiefout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtBedrag.Text, out decimal bedrag) || bedrag <= 0)
            {
                MessageBox.Show("Voer een geldig bedrag in (groter dan 0).", "Validatiefout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string naarIban = txtIban.Text.Trim().Replace(" ", "").ToUpper();
            if (!naarIban.StartsWith("BE") || naarIban.Length < 14)
            {
                MessageBox.Show("Ongeldig IBAN formaat.", "Validatiefout",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var bevestiging = MessageBox.Show(
                $"Overschrijving bevestigen?\n\n" +
                $"Bedrag: €{bedrag:N2}\n" +
                $"Naar: {txtNaamOntvanger.Text}\n" +
                $"IBAN: {naarIban}",
                "Bevestiging",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (bevestiging != MessageBoxResult.Yes)
                return;

            try
            {
                // Maak een NIEUWE context aan voor deze transactie
                using (var context = new AppDbContext())
                {
                    var transactieService = new TransactieService(context);
                    var gebruikerId = UserSession.IngelogdeGebruiker!.Id;

                    var (succes, bericht, transactie) = await transactieService.MaakOverschrijvingAsync(
                        vanIban: vanRekening.Iban,
                        naarIban: naarIban,
                        bedrag: bedrag,
                        omschrijving: txtOmschrijving.Text.Trim(),
                        gebruikerId: gebruikerId
                    );

                    if (succes)
                    {
                        MessageBox.Show(
                            $"Overschrijving succesvol!\n\n" +
                            $"Bedrag: €{bedrag:N2}\n" +
                            $"Naar: {txtNaamOntvanger.Text}\n\n" +
                            $"Je nieuwe saldo wordt bijgewerkt.",
                            "Succes",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        txtIban.Clear();
                        txtNaamOntvanger.Clear();
                        txtBedrag.Clear();
                        txtOmschrijving.Clear();
                    }
                    else
                    {
                        MessageBox.Show($"{bericht}", "Fout",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout: {ex.Message}", "Kritieke Fout",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}