using BankApp_MAUI.Data;

namespace BankApp_MAUI;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider, LocalDbContext context)
    {
        _serviceProvider = serviceProvider;
        
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Check of gebruiker is ingelogd
        bool isLoggedIn = Preferences.ContainsKey("auth_token");

        if (isLoggedIn)
        {
            return new Window(new AppShell());
        }
        else
        {
            // Haal LoginPage via DI
            var loginPage = _serviceProvider.GetRequiredService<Pages.LoginPage>();
            return new Window(new NavigationPage(loginPage));
        }
    }
}
