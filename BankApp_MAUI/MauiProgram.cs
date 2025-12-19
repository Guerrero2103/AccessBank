using BankApp_MAUI.Data;
using BankApp_MAUI.ViewModels;
using BankApp_MAUI.Pages;
using Microsoft.Extensions.Logging;

namespace BankApp_MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();
            // Geen custom fonts nodig

        // Services registreren 
        builder.Services.AddSingleton<LocalDbContext>();
        builder.Services.AddSingleton<Synchronizer>(); // Eén centrale synchronizer


        // ViewModels registreren
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<RekeningenViewModel>();
        builder.Services.AddTransient<TransactiesViewModel>();
        builder.Services.AddTransient<OverschrijvingViewModel>();

        // Pages registreren
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<RekeningenPage>();
        builder.Services.AddTransient<TransactiesPage>();
        builder.Services.AddTransient<OverschrijvingPage>();
        
        // Shell registreren
        builder.Services.AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
