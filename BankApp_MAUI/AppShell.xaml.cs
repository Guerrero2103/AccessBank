using BankApp_MAUI.Pages;

namespace BankApp_MAUI;

public partial class AppShell : Shell
{
    private readonly IServiceProvider? _serviceProvider;

    public AppShell()
    {
        InitializeComponent();

        // Routes registreren
        Routing.RegisterRoute("main", typeof(MainPage));
        Routing.RegisterRoute("rekeningen", typeof(RekeningenPage));
        Routing.RegisterRoute("overschrijving", typeof(OverschrijvingPage));
        Routing.RegisterRoute("transacties", typeof(TransactiesPage));
    }

    // Constructor met DI voor gebruik vanaf App.xaml.cs
    public AppShell(IServiceProvider serviceProvider) : this()
    {
        _serviceProvider = serviceProvider;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Uitloggen", "Weet je zeker dat je wilt uitloggen?", "Ja", "Nee");
        
        if (confirm)
        {
            try
            {
                // Clear token en preferences via Synchronizer
                if (_serviceProvider != null)
                {
                    var synchronizer = _serviceProvider.GetRequiredService<Synchronizer>();
                    synchronizer.Logout();
                }
                else
                {
                    Preferences.Remove("auth_token");
                    Preferences.Remove("user_id");
                    Preferences.Remove("user_email");
                    General.UserId = "";
                }
                
                // Herstart app (navigeer terug naar login)
                if (_serviceProvider != null)
                {
                    var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
                    var window = Application.Current?.Windows.FirstOrDefault();
                    if (window != null)
                    {
                        window.Page = new NavigationPage(loginPage);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnLogoutClicked: {ex.Message}");
                await DisplayAlert("Fout", "Er is een fout opgetreden bij uitloggen", "OK");
            }
        }
    }
}
