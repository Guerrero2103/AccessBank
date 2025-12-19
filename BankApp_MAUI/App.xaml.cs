using BankApp_MAUI.Data;

namespace BankApp_MAUI;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider, LocalDbContext context, Synchronizer synchronizer)
    {
        _serviceProvider = serviceProvider;

        InitializeComponent();

        // Database initialiseren en synchroniseren
        Task.Run(async () => {
            await synchronizer.SynchronizeAll();
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Check of gebruiker is ingelogd via Preferences (of General)
        bool isLoggedIn = Preferences.ContainsKey("auth_token");

        if (isLoggedIn)
        {
            // Zet UserId alvast in General voor gebruik in ViewModels
            General.UserId = Preferences.Get("user_id", "");

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