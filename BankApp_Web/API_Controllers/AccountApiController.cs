using BankApp_Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BankApp_Web.API_Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly UserManager<BankUser> _userManager;
        private readonly SignInManager<BankUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AccountApiController(
            UserManager<BankUser> userManager,
            SignInManager<BankUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        // Inloggen
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Ongeldige gegevens" });
            }

            // Zoek gebruiker
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Ongeldige inloggegevens" });
            }

            // Controleer wachtwoord
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Ongeldige inloggegevens" });
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
                return BadRequest(new { message = "Ongeldige gegevens" });
            }

            // Controleer of email al bestaat
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email is al in gebruik" });
            }

            var user = new BankUser
            {
                UserName = request.Email.Split('@')[0],
                Email = request.Email,
                Voornaam = request.Voornaam,
                Achternaam = request.Achternaam,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            // Geef gebruiker de rol "Klant"
            await _userManager.AddToRoleAsync(user, "Klant");

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
}
