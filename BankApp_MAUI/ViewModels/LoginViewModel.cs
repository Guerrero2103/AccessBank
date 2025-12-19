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
                    // Ga naar hoofdpagina - nieuwe stijl voor .NET 9
                    if (Application.Current?.Windows.Count > 0)
                    {
                        var appShell = _serviceProvider.GetRequiredService<AppShell>();
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
    }
}