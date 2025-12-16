using BankApp_MAUI.Data;
using BankApp_MAUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankApp_MAUI.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly LocalDbContext _localDb;
        private readonly AuthService _authService;
        private readonly SyncService _syncService;

        [ObservableProperty]
        private decimal totaalSaldo;

        [ObservableProperty]
        private string gebruikerNaam = string.Empty;

        [ObservableProperty]
        private bool isOnline;

        [ObservableProperty]
        private DateTime? laatsteSyncTijd;

        public MainViewModel(LocalDbContext localDb, AuthService authService, SyncService syncService)
        {
            _localDb = localDb;
            _authService = authService;
            _syncService = syncService;
            Title = "Dashboard";
        }

        [RelayCommand]
        private async Task NavigateToRekeningenAsync()
        {
            await Shell.Current.GoToAsync("//rekeningen");
        }

        [RelayCommand]
        private async Task NavigateToOverschrijvingAsync()
        {
            await Shell.Current.GoToAsync("//overschrijving");
        }

        [RelayCommand]
        private async Task NavigateToTransactiesAsync()
        {
            await Shell.Current.GoToAsync("//transacties");
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                var (userId, email) = _authService.GetUserInfo();
                GebruikerNaam = email;

                // Haal rekeningen op en tel saldo bij elkaar
                var rekeningen = await _localDb.GetRekeningenAsync(userId);
                TotaalSaldo = rekeningen.Sum(r => r.Saldo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SyncDataAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                bool success = await _syncService.SyncAllAsync();
                
                if (success)
                {
                    IsOnline = true;
                    LaatsteSyncTijd = DateTime.Now;
                    await LoadDataAsync();
                }
                else
                {
                    IsOnline = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sync fout: {ex.Message}");
                IsOnline = false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
