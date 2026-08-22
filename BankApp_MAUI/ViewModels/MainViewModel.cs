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

        [ObservableProperty]
        private string syncErrorMessage = string.Empty;

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

        [RelayCommand]
        private async Task NavigateToProfielAsync()
        {
            await Shell.Current.GoToAsync("//profiel");
        }

        public async Task InitializeAsync()
        {
            // Laad eerst lokale data
            await LoadDataAsync();
            
            // Synchroniseer automatisch als er internet is
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wacht even om UI te laten laden
                    await Task.Delay(1000);
                    
                    // Zorg dat General.UserId is ingesteld
                    if (string.IsNullOrEmpty(General.UserId))
                    {
                        General.UserId = Preferences.Get("user_id", "");
                    }
                    
                    if (!string.IsNullOrEmpty(General.UserId))
                    {
                        bool isOnline = await _synchronizer.IsOnline();
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            IsOnline = isOnline;
                        });
                        
                        if (isOnline)
                        {
                            System.Diagnostics.Debug.WriteLine("MainViewModel: Starting background sync");
                            try
                            {
                                await _synchronizer.SynchronizeAll();
                                
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    LaatsteSyncTijd = DateTime.Now;
                                    SyncErrorMessage = string.Empty; // Clear any previous errors
                                });
                                
                                // Herlaad data na synchronisatie - op main thread
                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    await LoadDataAsync();
                                });
                                
                                System.Diagnostics.Debug.WriteLine("MainViewModel: Background sync completed successfully");
                            }
                            catch (Exception syncEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"MainViewModel: Sync error: {syncEx.Message}");
                                System.Diagnostics.Debug.WriteLine($"MainViewModel: Stack trace: {syncEx.StackTrace}");
                                
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    SyncErrorMessage = $"Sync fout: {syncEx.Message}";
                                    LaatsteSyncTijd = DateTime.Now; // Update tijd ook bij fout
                                });
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("MainViewModel: Offline, skipping sync");
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                SyncErrorMessage = "Offline - geen synchronisatie mogelijk";
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Background sync error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: Stack trace: {ex.StackTrace}");
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SyncErrorMessage = $"Fout: {ex.Message}";
                    });
                }
            });
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                // Zorg dat General.UserId is ingesteld
                if (string.IsNullOrEmpty(General.UserId))
                {
                    General.UserId = Preferences.Get("user_id", "");
                }

                // Gebruik General.UserId
                GebruikerNaam = Preferences.Get("user_email", "Gebruiker");

                // Haal rekeningen op en tel saldo bij elkaar
                if (!string.IsNullOrEmpty(General.UserId))
                {
                    var rekeningen = await _localDb.GetRekeningenAsync(General.UserId);
                    TotaalSaldo = rekeningen.Sum(r => r.Saldo);
                    System.Diagnostics.Debug.WriteLine($"Loaded {rekeningen.Count} rekeningen, totaal saldo: {TotaalSaldo}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Warning: General.UserId is leeg, kan geen rekeningen laden");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout in LoadDataAsync: {ex.Message}");
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
            SyncErrorMessage = string.Empty;

            try
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: Manual sync started");
                
                // Check online status eerst
                IsOnline = await _synchronizer.IsOnline();
                
                if (!IsOnline)
                {
                    SyncErrorMessage = "Geen internetverbinding";
                    System.Diagnostics.Debug.WriteLine("MainViewModel: Offline, cannot sync");
                    return;
                }
                
                // Gebruik Synchronizer
                await _synchronizer.SynchronizeAll();
                
                LaatsteSyncTijd = DateTime.Now;
                SyncErrorMessage = string.Empty; // Clear any errors
                
                // Herlaad data na synchronisatie
                await LoadDataAsync();
                
                System.Diagnostics.Debug.WriteLine("MainViewModel: Manual sync completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainViewModel: Sync fout: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"MainViewModel: Stack trace: {ex.StackTrace}");
                
                SyncErrorMessage = $"Sync fout: {ex.Message}";
                IsOnline = false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
