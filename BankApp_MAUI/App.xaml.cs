using BankApp_MAUI.Data;

namespace BankApp_MAUI;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider, LocalDbContext context)
    {
        _serviceProvider = serviceProvider;
        
        InitializeComponent();
        
        // Synchroniseer automatisch als gebruiker is ingelogd
        if (Preferences.ContainsKey("auth_token"))
        {
            General.UserId = Preferences.Get("user_id", "");
            if (!string.IsNullOrEmpty(General.UserId))
            {
                // Start synchronisatie in achtergrond
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var synchronizer = serviceProvider.GetRequiredService<Synchronizer>();
                        if (await synchronizer.IsOnline())
                        {
                            await synchronizer.SynchronizeAll();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Background sync error: {ex.Message}");
                    }
                });
            }
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Check of gebruiker is ingelogd
        bool isLoggedIn = Preferences.ContainsKey("auth_token");

        if (isLoggedIn)
        {
            // Zet UserId in General
            General.UserId = Preferences.Get("user_id", "");
            
            if (string.IsNullOrEmpty(General.UserId))
            {
                isLoggedIn = false;
            }
        }

        if (isLoggedIn)
        {
            // Maak AppShell via DI
            var appShell = _serviceProvider.GetRequiredService<AppShell>();
            return new Window(appShell);
        }
        else
        {
            // Haal LoginPage via DI
            var loginPage = _serviceProvider.GetRequiredService<Pages.LoginPage>();
            return new Window(new NavigationPage(loginPage));
        }
    }
}
