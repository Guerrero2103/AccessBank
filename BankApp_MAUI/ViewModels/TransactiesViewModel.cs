using BankApp_MAUI.Data;
using BankApp_MAUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BankApp_MAUI.ViewModels
{
    public partial class TransactiesViewModel : BaseViewModel
    {
        private readonly LocalDbContext _localDb;

        [ObservableProperty]
        private ObservableCollection<LocalTransactie> transacties = new();

        [ObservableProperty]
        private string filterStatus = "Alle";

        public TransactiesViewModel(LocalDbContext localDb)
        {
            _localDb = localDb;
            Title = "Transacties";
        }

        public async Task InitializeAsync()
        {
            await LoadTransactiesAsync();
        }

        [RelayCommand]
        private async Task LoadTransactiesAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                // Gebruik General.UserId - zoals Agenda-master
                var transactiesList = await _localDb.GetTransactiesAsync(General.UserId);

                // Filter gebruiken
                if (FilterStatus != "Alle")
                {
                    transactiesList = transactiesList
                        .Where(t => t.Status == FilterStatus)
                        .ToList();
                }

                Transacties.Clear();
                foreach (var transactie in transactiesList)
                {
                    Transacties.Add(transactie);
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
        private async Task FilterChangedAsync()
        {
            await LoadTransactiesAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadTransactiesAsync();
            IsRefreshing = false;
        }
    }
}
