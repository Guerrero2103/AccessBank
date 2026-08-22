using BankApp_MAUI.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankApp_MAUI.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly Synchronizer _synchronizer;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public LoginViewModel(Synchronizer synchronizer, IServiceProvider serviceProvider)
        {
            _synchronizer = synchronizer;
            _serviceProvider = serviceProvider;
            Title = "Login";

            // Toon een melding als we hier zijn beland omdat de sessie verliep (zie App.xaml.cs)
            var sessieVerlopenMelding = Preferences.Get("session_expired_message", string.Empty);
            if (!string.IsNullOrEmpty(sessieVerlopenMelding))
            {
                ErrorMessage = sessieVerlopenMelding;
                Preferences.Remove("session_expired_message");
            }
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (IsBusy) return;

            // Validatie
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vul alle velden in";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                bool success = await _synchronizer.Login(Email, Password);

                if (success)
                {

                    // Synchroniseer direct na login
                    try
                    {
                        if (await _synchronizer.IsOnline())
                        {
                            await _synchronizer.SynchronizeAll();
                        }
                    }
                    catch (Exception syncEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Sync error after login: {syncEx.Message}");
                    }
                    
                    // Ga naar hoofdpagina
                    var appShell = _serviceProvider.GetRequiredService<AppShell>();
                    if (Application.Current?.Windows.Count > 0)
                    {

                        Application.Current.Windows[0].Page = appShell;
                    }
                }
                else
                {
                    ErrorMessage = "Ongeldige inloggegevens of geen verbinding";
                }
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

        [RelayCommand]
        private void GaNaarRegistratie()
        {
            var registratiePage = _serviceProvider.GetRequiredService<RegistratiePage>();
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new NavigationPage(registratiePage);
            }
        }
    }
}