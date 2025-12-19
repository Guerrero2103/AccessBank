using BankApp_MAUI.Data;
using BankApp_MAUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BankApp_MAUI.ViewModels
{
    public partial class RekeningenViewModel : BaseViewModel
    {
        private readonly LocalDbContext _localDb;

        [ObservableProperty]
        private ObservableCollection<LocalRekening> rekeningen = new();

        public RekeningenViewModel(LocalDbContext localDb)
        {
            _localDb = localDb;
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
                // Gebruik General.UserId - zoals Agenda-master
                var rekeningenList = await _localDb.GetRekeningenAsync(General.UserId);

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
