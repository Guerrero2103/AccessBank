using BankApp_MAUI.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace BankApp_MAUI.ViewModels
{
    public partial class RegistratieViewModel : BaseViewModel
    {
        private readonly Synchronizer _synchronizer;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string voornaam = string.Empty;

        [ObservableProperty]
        private string achternaam = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string wachtwoord = string.Empty;

        [ObservableProperty]
        private string wachtwoordBevestiging = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public RegistratieViewModel(Synchronizer synchronizer, IServiceProvider serviceProvider)
        {
            _synchronizer = synchronizer;
            _serviceProvider = serviceProvider;
            Title = "Registreren";
        }

        [RelayCommand]
        private async Task RegistreerAsync()
        {
            if (IsBusy) return;

            ErrorMessage = string.Empty;

            // Validatie - consistent met de serververeisten (ook toegepast in WPF)
            if (string.IsNullOrWhiteSpace(Voornaam) || string.IsNullOrWhiteSpace(Achternaam) ||
                string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Wachtwoord))
            {
                ErrorMessage = "Vul alle velden in.";
                return;
            }

            if (Wachtwoord != WachtwoordBevestiging)
            {
                ErrorMessage = "De wachtwoorden komen niet overeen.";
                return;
            }

            if (!IsWachtwoordSterkGenoeg(Wachtwoord))
            {
                ErrorMessage = "Wachtwoord moet minstens een hoofdletter, kleine letter, cijfer en speciaal teken bevatten.";
                return;
            }

            IsBusy = true;

            try
            {
                var (succes, foutBoodschap) = await _synchronizer.RegistreerAsync(Email, Wachtwoord, Voornaam, Achternaam);

                if (succes)
                {
                    // Registratie + automatische login geslaagd - zelfde navigatiepatroon als LoginViewModel.LoginAsync
                    var appShell = _serviceProvider.GetRequiredService<AppShell>();
                    if (Application.Current?.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = appShell;
                    }
                }
                else
                {
                    ErrorMessage = foutBoodschap ?? "Registratie mislukt. Probeer opnieuw.";
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
        private void GaNaarLogin()
        {
            // Zelfde patroon als AppShell.OnLogoutClicked: LoginPage in een nieuwe NavigationPage
            var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new NavigationPage(loginPage);
            }
        }

        private bool IsWachtwoordSterkGenoeg(string wachtwoord)
        {
            return wachtwoord.Length >= 8
                && Regex.IsMatch(wachtwoord, @"[A-Z]")
                && Regex.IsMatch(wachtwoord, @"[a-z]")
                && Regex.IsMatch(wachtwoord, @"\d")
                && Regex.IsMatch(wachtwoord, @"[\W_]");
        }
    }
}
