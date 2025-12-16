using BankApp_MAUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankApp_MAUI.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public LoginViewModel(ApiService apiService, AuthService authService, IServiceProvider serviceProvider)
        {
            _apiService = apiService;
            _authService = authService;
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
                var (success, token, message) = await _apiService.LoginAsync(Email, Password);

                if (success)
                {
                    // Bewaar inlogtoken en gebruikersgegevens
                    _authService.SaveToken(token);
                    _authService.SaveUserInfo("user_id", Email);
                    _apiService.SetAuthToken(token);

                    // Ga naar hoofdpagina
                    var appShell = _serviceProvider.GetRequiredService<AppShell>();
                    Application.Current!.MainPage = appShell;
                }
                else
                {
                    ErrorMessage = message;
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
