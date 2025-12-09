using BankApp_BusinessLogic;
using BankApp_Models;
using System;
using System.Linq;
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
                    var rekeningService = new RekeningService(context);

                    var gebruikerId = UserSession.IngelogdeGebruiker!.Id;
                    var gebruikerRekeningen = await rekeningService
                        .GetRekeningenByGebruikerIdAsync(gebruikerId);

                    var vanRekening = gebruikerRekeningen.FirstOrDefault();

                    if (vanRekening == null)
                    {
                        MessageBox.Show("Je hebt geen rekening.",
                            "Geen rekening", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

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