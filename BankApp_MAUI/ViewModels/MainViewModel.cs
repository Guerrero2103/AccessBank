using BankApp_MAUI.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankApp_MAUI.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly LocalDbContext _localDb;
        private readonly Synchronizer _synchronizer;

        [ObservableProperty]
        private decimal totaalSaldo;

        [ObservableProperty]
        private string gebruikerNaam = string.Empty;

        [ObservableProperty]
        private bool isOnline;

        [ObservableProperty]
        private DateTime? laatsteSyncTijd;

        public MainViewModel(LocalDbContext localDb, Synchronizer synchronizer)
        {
            _localDb = localDb;
            _synchronizer = synchronizer;
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
                // Gebruik General.UserId
                GebruikerNaam = Preferences.Get("user_email", "Gebruiker");

                // Haal rekeningen op en tel saldo bij elkaar
                var rekeningen = await _localDb.GetRekeningenAsync(General.UserId);
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
                // Gebruik Synchronizer
                await _synchronizer.SynchronizeAll();
                
                IsOnline = await _synchronizer.IsOnline();
                LaatsteSyncTijd = DateTime.Now;
                await LoadDataAsync();
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
