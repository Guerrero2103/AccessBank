using BankApp_MAUI.Data;
using BankApp_MAUI.Models;
using BankApp_MAUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BankApp_MAUI.ViewModels
{
    public partial class OverschrijvingViewModel : BaseViewModel
    {
        private readonly LocalDbContext _localDb;
        private readonly ApiService _apiService;
        private readonly AuthService _authService;

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

        public OverschrijvingViewModel(LocalDbContext localDb, ApiService apiService, AuthService authService)
        {
            _localDb = localDb;
            _apiService = apiService;
            _authService = authService;
            Title = "Overschrijving";
        }

        public async Task InitializeAsync()
        {
            await LoadRekeningenAsync();
        }

        private async Task LoadRekeningenAsync()
        {
            var (userId, _) = _authService.GetUserInfo();
            var rekeningen = await _localDb.GetRekeningenAsync(userId);

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
                var (userId, _) = _authService.GetUserInfo();

                // Maak nieuwe transactie
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

                // Bewaar in lokale database
                await _localDb.SaveTransactieAsync(transactie);

                // Probeer direct te verzenden als er internet is
                bool isOnline = await _apiService.IsOnlineAsync();
                if (isOnline)
                {
                    var apiTransactie = new BankApp_Models.Transactie
                    {
                        VanIban = transactie.VanIban,
                        NaarIban = transactie.NaarIban,
                        Bedrag = transactie.Bedrag,
                        Omschrijving = transactie.Omschrijving,
                        GebruikerId = userId
                    };

                    var (success, message) = await _apiService.MaakOverschrijvingAsync(apiTransactie);
                    
                    if (success)
                    {
                        transactie.IsSynced = true;
                        await _localDb.SaveTransactieAsync(transactie);
                        
                        SuccessMessage = Bedrag >= 500 
                            ? "Overschrijving in behandeling (€500+)" 
                            : "Overschrijving succesvol!";
                    }
                    else
                    {
                        ErrorMessage = message;
                    }
                }
                else
                {
                    SuccessMessage = "Overschrijving opgeslagen (offline). Wordt verzonden bij synchronisatie.";
                }

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
