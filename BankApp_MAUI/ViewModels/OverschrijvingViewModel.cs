using BankApp_MAUI.Data;
using BankApp_MAUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BankApp_MAUI.ViewModels
{
    public partial class OverschrijvingViewModel : BaseViewModel
    {
        private readonly LocalDbContext _localDb;
        private readonly Synchronizer _synchronizer;

        [ObservableProperty]
        private ObservableCollection<LocalRekening> eigenRekeningen = new();

        [ObservableProperty]
        private LocalRekening? geselecteerdeRekening;

        [ObservableProperty]
        private string naarIban = string.Empty;

        [ObservableProperty]
        private decimal bedrag;

        [ObservableProperty]
        private string omschrijving = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        public OverschrijvingViewModel(LocalDbContext localDb, Synchronizer synchronizer)
        {
            _localDb = localDb;
            _synchronizer = synchronizer;
            Title = "Overschrijving";
        }

        public async Task InitializeAsync()
        {
            await LoadRekeningenAsync();
        }

        private async Task LoadRekeningenAsync()
        {
            // Gebruik General.UserId
            var rekeningen = await _localDb.GetRekeningenAsync(General.UserId);

            EigenRekeningen.Clear();
            foreach (var rekening in rekeningen)
            {
                EigenRekeningen.Add(rekening);
            }

            if (EigenRekeningen.Any())
            {
                GeselecteerdeRekening = EigenRekeningen.First();
            }
        }

        [RelayCommand]
        private async Task VerzendOverschrijvingAsync()
        {
            if (IsBusy) return;

            // Validatie
            if (GeselecteerdeRekening == null)
            {
                ErrorMessage = "Selecteer een rekening";
                return;
            }

            if (string.IsNullOrWhiteSpace(NaarIban))
            {
                ErrorMessage = "Vul de ontvanger IBAN in";
                return;
            }

            if (Bedrag <= 0)
            {
                ErrorMessage = "Bedrag moet groter zijn dan 0";
                return;
            }

            if (GeselecteerdeRekening.Saldo < Bedrag)
            {
                ErrorMessage = "Onvoldoende saldo";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                string userId = General.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    ErrorMessage = "Gebruiker niet ingelogd";
                    return;
                }

                // Maak nieuwe transactie (altijd eerst lokaal opslaan)
                var transactie = new LocalTransactie
                {
                    VanIban = GeselecteerdeRekening.Iban,
                    NaarIban = NaarIban,
                    Bedrag = Bedrag,
                    Omschrijving = Omschrijving,
                    Datum = DateTime.Now,
                    GebruikerId = userId,
                    IsSynced = false,
                    Status = Bedrag >= 500 ? "Wachtend" : "Voltooid"
                };

                // Bewaar in lokale database (offline-first)
                await _localDb.SaveTransactieAsync(transactie);
                System.Diagnostics.Debug.WriteLine($"Overschrijving opgeslagen lokaal: {transactie.Id}, IsSynced: {transactie.IsSynced}");

                // Update lokaal saldo (als status Voltooid is)
                if (transactie.Status == "Voltooid")
                {
                    var rekening = await _localDb.GetRekeningByIbanAsync(GeselecteerdeRekening.Iban);
                    if (rekening != null)
                    {
                        rekening.Saldo -= Bedrag;
                        await _localDb.SaveRekeningAsync(rekening);
                        System.Diagnostics.Debug.WriteLine($"Lokaal saldo aangepast: {rekening.Saldo}");
                    }
                }

                // Probeer direct te verzenden als er internet is
                bool isOnline = await _synchronizer.IsOnline();
                if (isOnline)
                {
                    System.Diagnostics.Debug.WriteLine("Online - probeer direct te synchroniseren");
                    var apiTransactie = new BankApp_Models.Transactie
                    {
                        VanIban = transactie.VanIban,
                        NaarIban = transactie.NaarIban,
                        Bedrag = transactie.Bedrag,
                        Omschrijving = transactie.Omschrijving,
                        GebruikerId = userId
                    };

                    var (success, message) = await _synchronizer.MaakOverschrijving(apiTransactie);
                    
                    if (success)
                    {
                        transactie.IsSynced = true;
                        transactie.LastSync = DateTime.Now;
                        await _localDb.SaveTransactieAsync(transactie);
                        System.Diagnostics.Debug.WriteLine("Overschrijving succesvol gesynchroniseerd");
                        
                        // Synchroniseer rekeningen om saldo bij te werken
                        await _synchronizer.SynchronizeAll();
                        
                        SuccessMessage = Bedrag >= 500 
                            ? "Overschrijving in behandeling (€500+)" 
                            : "Overschrijving succesvol!";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Synchronisatie mislukt: {message}");
                        ErrorMessage = $"Overschrijving opgeslagen maar niet verzonden: {message}";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Offline - overschrijving wordt later gesynchroniseerd");
                    SuccessMessage = "Overschrijving opgeslagen (offline). Wordt verzonden bij synchronisatie.";
                }

                // Herlaad rekeningen om saldo te updaten
                await LoadRekeningenAsync();

                // Maak velden leeg
                NaarIban = string.Empty;
                Bedrag = 0;
                Omschrijving = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
