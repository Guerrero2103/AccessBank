using BankApp_BusinessLogic;
using BankApp_Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Localization;
using BankApp_Web.Translations;

namespace BankApp_Web.API_Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly UserManager<BankUser> _userManager;
        private readonly SignInManager<BankUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountApiController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IRegistratieService _registratieService;
        private readonly AppDbContext _context;

        public AccountApiController(
            UserManager<BankUser> userManager,
            SignInManager<BankUser> signInManager,
            IConfiguration configuration,
            ILogger<AccountApiController> logger,
            IStringLocalizer<SharedResource> localizer,
            IRegistratieService registratieService,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
            _localizer = localizer;
            _registratieService = registratieService;
            _context = context;
        }

        // Inloggen
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid || request == null)
            {
                return BadRequest(new { message = _localizer["Ongeldige gegevens"] });
            }

            // Zoek gebruiker - probeer eerst email, dan username
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(request.Email);
            }

            if (user == null)
            {


                return Unauthorized(new { message = _localizer["Gebruiker niet gevonden"] });

            }

            // Controleer wachtwoord via SignInManager
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            
            // Als CheckPasswordSignInAsync faalt, probeer de meer directe methode
            if (!result.Succeeded)
            {
                // Sommige configuraties van Identity vereisen dit
                var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordValid)
                {

                    return Unauthorized(new { message = _localizer["Wachtwoord onjuist"] });

                }
            }

            // Maak inlogtoken aan
            var token = await GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                email = user.Email,
                userName = user.UserName,
                userId = user.Id
            });
        }

        // Registreren
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = _localizer["Ongeldige gegevens"] });
            }

            var resultaat = await _registratieService.RegistreerAsync(new RegistratieGegevens
            {
                Email = request.Email,
                Wachtwoord = request.Password,
                Voornaam = request.Voornaam,
                Achternaam = request.Achternaam
            });

            if (!resultaat.Succes || resultaat.Gebruiker == null)
            {
                return BadRequest(new { message = string.Join(", ", resultaat.Fouten) });
            }

            var user = resultaat.Gebruiker;

            // Maak inlogtoken aan
            var token = await GenerateJwtToken(user);

            return Ok(new
            {
                message = "Registratie succesvol!",

                token = token,
                email = user.Email,
                userName = user.UserName,
                userId = user.Id
            });
        }

        // Eigen profiel ophalen
        [HttpGet("profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.Adres)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Deleted == DateTime.MaxValue);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                voornaam = user.Voornaam,
                achternaam = user.Achternaam,
                email = user.Email,
                telefoonnummer = user.Telefoonnummer,
                straat = user.Adres?.Straat,
                huisnummer = user.Adres?.Huisnummer,
                bus = user.Adres?.Bus,
                postcode = user.Adres?.Postcode,
                gemeente = user.Adres?.Gemeente,
                land = user.Adres?.Land
            });
        }

        // Eigen profiel bijwerken
        [HttpPut("profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.Adres)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Deleted == DateTime.MaxValue);

            if (user == null)
            {
                return NotFound();
            }

            user.Voornaam = request.Voornaam;
            user.Achternaam = request.Achternaam;
            user.Telefoonnummer = request.Telefoonnummer;

            if (user.Adres == null)
            {
                var nieuwAdres = new Adres();
                _context.Adressen.Add(nieuwAdres);
                await _context.SaveChangesAsync();
                user.Adres = nieuwAdres;
                user.AdresId = nieuwAdres.Id;
            }

            user.Adres.Straat = request.Straat ?? string.Empty;
            user.Adres.Huisnummer = request.Huisnummer ?? string.Empty;
            user.Adres.Bus = string.IsNullOrWhiteSpace(request.Bus) ? null : request.Bus;
            user.Adres.Postcode = request.Postcode ?? string.Empty;
            user.Adres.Gemeente = request.Gemeente ?? string.Empty;
            user.Adres.Land = string.IsNullOrWhiteSpace(request.Land) ? "België" : request.Land;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profiel bijgewerkt" });
        }

        // Maak inlogtoken aan
        private async Task<string> GenerateJwtToken(BankUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? "")
            };

            // Voeg rollen toe
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "BankApp_SecretKey_MinimumLength32Characters_2025"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(7);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "BankApp",
                audience: _configuration["Jwt:Audience"] ?? "BankAppUsers",
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Gegevens voor inloggen
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Voornaam { get; set; } = string.Empty;
        public string Achternaam { get; set; } = string.Empty;
    }

    public class ProfileUpdateRequest
    {
        public string Voornaam { get; set; } = string.Empty;
        public string Achternaam { get; set; } = string.Empty;
        public string Telefoonnummer { get; set; } = string.Empty;
        public string? Straat { get; set; }
        public string? Huisnummer { get; set; }
        public string? Bus { get; set; }
        public string? Postcode { get; set; }
        public string? Gemeente { get; set; }
        public string? Land { get; set; }
    }
}