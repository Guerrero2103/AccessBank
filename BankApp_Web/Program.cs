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

var builder = WebApplication.CreateBuilder(args);

// Database verbindingsstring instellen
var connectionString = builder.Configuration.GetConnectionString("AppDbContextConnection") 
    ?? throw new InvalidOperationException("Connection string 'AppDbContextConnection' not found.");

// Database context toevoegen
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Gebruikersbeheer instellen
builder.Services.AddDefaultIdentity<BankUser>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Inlogtoken instellingen voor MAUI app
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "BankApp_SecretKey_MinimumLength32Characters_2025"))
        };
    });

// Webpagina's toevoegen
builder.Services.AddControllersWithViews();

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
builder.Services.AddControllers();

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
builder.Services.AddMvc()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// Database vullen met startgegevens
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Maak database aan als die nog niet bestaat
        context.Database.EnsureCreated();
        // Vul database met testgegevens
        await AppDbContext.Seeder(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Fout bij het seeden van de database.");
    }
}

// API documentatie alleen tijdens ontwikkeling tonen
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BankApp_Web API v1"));
}

// Taalinstellingen
var supportedCultures = new[] { "nl", "en", "fr" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

// Foutafhandeling instellen
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
