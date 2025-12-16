using BankApp_Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace BankApp_MAUI.Services
{
    // Service om te praten met de server
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        
        // Android emulator gebruikt ander adres dan Windows
#if ANDROID
        private const string BASE_URL = "https://10.0.2.2:5001/api";
#else
        private const string BASE_URL = "https://localhost:5001/api";
#endif

        public ApiService()
        {
            // Tijdens ontwikkeling: accepteer certificaten zonder controle
            var handler = new HttpClientHandler
            {
#if DEBUG
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#endif
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BASE_URL),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // Zet inlogtoken
        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        // Inloggen
        public async Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new { email, password };
                var response = await _httpClient.PostAsJsonAsync("/account/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    return (true, result?.Token ?? "", "Login succesvol");
                }
                else
                {
                    return (false, "", "Ongeldige inloggegevens");
                }
            }
            catch (Exception ex)
            {
                return (false, "", $"Fout: {ex.Message}");
            }
        }

        // Haal rekeningen van server op
        public async Task<List<Rekening>> GetRekeningenAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/rekeningen");
                response.EnsureSuccessStatusCode();
                
                var rekeningen = await response.Content.ReadFromJsonAsync<List<Rekening>>();
                return rekeningen ?? new List<Rekening>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij ophalen rekeningen: {ex.Message}");
                return new List<Rekening>();
            }
        }

        // Haal transacties van server op
        public async Task<List<Transactie>> GetTransactiesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/transacties");
                response.EnsureSuccessStatusCode();
                
                var transacties = await response.Content.ReadFromJsonAsync<List<Transactie>>();
                return transacties ?? new List<Transactie>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij ophalen transacties: {ex.Message}");
                return new List<Transactie>();
            }
        }

        // Stuur overschrijving naar server
        public async Task<(bool Success, string Message)> MaakOverschrijvingAsync(Transactie transactie)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/transacties", transactie);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Overschrijving succesvol");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Fout: {ex.Message}");
            }
        }

        // Controleer of er internetverbinding is
        public async Task<bool> IsOnlineAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    // Gegevens die terugkomen na inloggen
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
