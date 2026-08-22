using BankApp_MAUI.Data;

namespace BankApp_MAUI;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider, LocalDbContext context, Synchronizer synchronizer)
    {
        _serviceProvider = serviceProvider;

        InitializeComponent();

        // Sessie verlopen (401 van de API): stuur gebruiker terug naar het loginscherm
        // met een duidelijke melding, i.p.v. dat sync-acties stil blijven falen.
        synchronizer.SessieVerlopen += () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Application.Current?.Windows.Count > 0)
                {
                    Preferences.Set("session_expired_message", "Je sessie is verlopen. Log opnieuw in.");
                    var loginPage = serviceProvider.GetRequiredService<Pages.LoginPage>();
                    Application.Current.Windows[0].Page = new NavigationPage(loginPage);
                }
            });
        };

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
        // Check of gebruiker is ingelogd via Preferences (of General)
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