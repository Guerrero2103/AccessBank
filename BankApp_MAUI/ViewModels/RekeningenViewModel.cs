using BankApp_MAUI.Data;
using BankApp_MAUI.Models;
using BankApp_MAUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BankApp_MAUI.ViewModels
{
    public partial class RekeningenViewModel : BaseViewModel
    {
        private readonly LocalDbContext _localDb;
        private readonly AuthService _authService;

        [ObservableProperty]
        private ObservableCollection<LocalRekening> rekeningen = new();

        public RekeningenViewModel(LocalDbContext localDb, AuthService authService)
        {
            _localDb = localDb;
            _authService = authService;
            Title = "Mijn Rekeningen";
        }

        public async Task InitializeAsync()
        {
            await LoadRekeningenAsync();
        }

        [RelayCommand]
        private async Task LoadRekeningenAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                var (userId, _) = _authService.GetUserInfo();
                var rekeningenList = await _localDb.GetRekeningenAsync(userId);

                Rekeningen.Clear();
                foreach (var rekening in rekeningenList)
                {
                    Rekeningen.Add(rekening);
                }
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
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadRekeningenAsync();
            IsRefreshing = false;
        }
    }
}
