using BankApp_BusinessLogic;
using BankApp_Models;
using BankApp_Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Database verbindingsstring instellen (SQLite)
var connectionString = builder.Configuration.GetConnectionString("AppDbContextConnection")
    ?? "Data Source=bankapp.db";

// Database pad bepalen (in de Models folder)
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "..", "BankApp_Models", "bankapp.db");
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

// Database context toevoegen (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Gebruikersbeheer instellen
builder.Services.AddIdentity<BankUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Email-based login toestaan
    options.User.RequireUniqueEmail = true;

    // Email verificatie niet vereist
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// Custom IdentityErrorDescriber voor meertalige foutmeldingen
builder.Services.AddScoped<IdentityErrorDescriber, LocalizedIdentityErrorDescriber>();
builder.Services.AddScoped<IRekeningService, RekeningService>();
builder.Services.AddScoped<IRegistratieService, RegistratieService>();

// Configureer SignInManager om email te accepteren
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
});

// Gebruik custom SignInManager die email ondersteunt
builder.Services.AddScoped<SignInManager<BankUser>, BankApp_Web.Services.CustomSignInManager>();

// Configureer redirect na login/registratie
builder.Services.ConfigureApplicationCookie(options =>
{
    // Standaard paden - gebruik custom Account controller
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Home/Error";

    // Cookie instellingen voor development (HTTP) en production (HTTPS)
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
    // Secure wordt automatisch ingesteld op basis van IsHttps in de request
    // In development (HTTP) is Secure = false, in production (HTTPS) is Secure = true

    // Na succesvolle login/registratie: altijd naar Home/Index (niet naar returnUrl)
    // HomeController zal dan de juiste redirect doen op basis van rol
    options.Events.OnRedirectToReturnUrl = context =>
    {
        // Negeer returnUrl en ga altijd naar Home/Index
        // Dit zorgt ervoor dat na registratie/login altijd de juiste pagina wordt getoond
        context.Response.Redirect("/Home/Index");
        return Task.CompletedTask;
    };
});

// Inlogtoken instellingen voor MAUI app
// JWT authentication toevoegen ZONDER default scheme te overschrijven
// AddIdentity heeft al cookie authentication als default geconfigureerd
// We voegen JWT toe als extra scheme zonder de defaults te overschrijven
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "BankApp_SecretKey_MinimumLength32Characters_2025")),
            // Map claims correct
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

// Webpagina's toevoegen (wordt later geconfigureerd met localization)

// Toestemming geven voor MAUI app om te verbinden
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMAUI", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// API endpoints toevoegen
// Configureer JSON serialization voor API controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Handle circular references (Rekening.Gebruiker → BankUser.Rekeningen)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // Gebruik PascalCase (standaard ASP.NET Core)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Data Protection configureren (voor cookies en temp data)
// In development: sla keys op in een lokale folder
if (builder.Environment.IsDevelopment())
{
    var keysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("BankApp");
}
else
{
    // In production: gebruik een gedeelde key store (bijv. Azure Key Vault of database)
    builder.Services.AddDataProtection()
        .SetApplicationName("BankApp");
}

// API documentatie instellen
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BankApp_Web API",
        Version = "v1",
        Description = "RESTful API voor BankApp MAUI applicatie"
    });
});

// Database logging instellen
builder.Logging.AddDbLogger(options =>
{
    builder.Configuration.GetSection("Logging");
});

// Meertaligheid instellen
builder.Services.AddLocalization(options => options.ResourcesPath = "Translations");
builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(options =>
    {
        // Configureer DataAnnotations localization om SharedResource te gebruiken
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(BankApp_Web.Translations.SharedResource));
    });

// IViewLocalizer gebruikt standaard view-specifieke resources
// Om SharedResource te gebruiken, moeten we de views aanpassen om IStringLocalizer<SharedResource> te gebruiken
// OF we kunnen een custom ViewLocalizerFactory maken (complex)
// Voor nu: gebruik de standaard IViewLocalizer en zorg dat SharedResource beschikbaar is via IStringLocalizer

var app = builder.Build();

// Database vullen met startgegevens
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<BankUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Controleer of database bestaat en voer migrations uit
        try
        {
            // Check of database bereikbaar is
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("⚠️ Database is niet bereikbaar!");
                logger.LogWarning("Mogelijke oorzaken:");
                logger.LogWarning("1. Database bestand bestaat niet of pad is onjuist");
                logger.LogWarning("2. Geen schrijfrechten op database locatie");
                logger.LogWarning("3. Database is gelocked door andere proces");
                logger.LogWarning("");
                logger.LogWarning("Database wordt automatisch aangemaakt bij eerste gebruik.");
            }

            // Check of er migrations zijn om uit te voeren
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation($"Uitvoeren van {pendingMigrations.Count()} pending migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Database migrations uitgevoerd");
            }
            else
            {
                logger.LogInformation("✅ Database is up-to-date, geen migrations nodig");
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException sqliteEx)
        {
            // SQLite specifieke errors
            logger.LogError(sqliteEx, "❌ SQLite database fout: {Message}", sqliteEx.Message);
            logger.LogWarning("App start wel, maar database functionaliteit werkt mogelijk niet.");
        }
        catch (Exception migrationEx)
        {
            logger.LogWarning(migrationEx, "⚠️ Fout bij migrations: {Message}", migrationEx.Message);
            logger.LogWarning("App start wel, maar database seeding is overgeslagen.");
        }

        // Vul database met testgegevens - gebruik DI UserManager
        await AppDbContext.SeederWithDI(context, userManager, roleManager, logger);
        logger.LogInformation("Database seeding voltooid");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fout bij het seeden van de database: {Message}", ex.Message);
        if (ex.InnerException != null)
        {
            logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
        }
    }
}

// API documentatie alleen tijdens ontwikkeling tonen
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BankApp_Web API v1"));

    // Schakel Browser Link uit in development om cookie waarschuwingen te voorkomen
    // Browser Link probeert HTTP requests te maken terwijl cookies Secure zijn
}
else
{
    // Production: geen development tools
}

// Foutafhandeling instellen
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    // HTTPS redirect alleen in production
    app.UseHttpsRedirection();
}

// Taalinstellingen - MOET voor UseStaticFiles() en UseRouting() komen
var supportedCultures = new[] { "nl", "en", "fr" };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("nl"),
    SupportedCultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList(),
    SupportedUICultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList()
};

// Gebruik cookie provider voor taal opslag (als primair)
localizationOptions.RequestCultureProviders.Clear();
localizationOptions.RequestCultureProviders.Add(new CookieRequestCultureProvider
{
    CookieName = CookieRequestCultureProvider.DefaultCookieName
});

app.UseRequestLocalization(localizationOptions);

app.UseStaticFiles();

app.UseRouting();

// Toestemming geven voor MAUI app
app.UseCors("AllowMAUI");

app.UseAuthentication();
app.UseAuthorization();

// Standaard route instellen
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// API routes toevoegen
app.MapControllers();

app.Run();