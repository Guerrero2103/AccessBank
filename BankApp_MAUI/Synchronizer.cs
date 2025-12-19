using System.Net.Http.Json;
using System.Text.Json;
using BankApp_MAUI.Data;
using BankApp_MAUI.Models;
using BankApp_Models;
using Microsoft.EntityFrameworkCore;

namespace BankApp_MAUI
{
    // Centrale klasse voor alle communicatie en synchronisatie - zoals in Agenda-master
    public class Synchronizer
    {
        private readonly HttpClient client;
        private readonly JsonSerializerOptions sOptions;
        private readonly LocalDbContext _context;

        public Synchronizer(LocalDbContext context)
        {
            _context = context;

            // HttpClient configureren (geen SSL controle in debug mode)
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            client = new HttpClient(handler)
            {
                BaseAddress = new Uri(General.ApiUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            sOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        // --- AUTHENTICATIE ---

        public async Task<bool> IsAuthorized()
        {
            // Als we al een UserId hebben, zijn we geautoriseerd
            if (!string.IsNullOrEmpty(General.UserId))
                return true;

            // Kijk in de lokale voorkeuren (Preferences)
            string token = Preferences.Get("auth_token", "");
            if (string.IsNullOrEmpty(token))
                return false;

            // Voeg token toe aan headers
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            // Haal de UserId en Email op uit Preferences
            General.UserId = Preferences.Get("user_id", "");
            
            return !string.IsNullOrEmpty(General.UserId);
        }

        public async Task<bool> Login(string email, string password)
        {
            try
            {
                var loginData = new { Email = email, Password = password };
                var response = await client.PostAsJsonAsync("account/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result != null)
                    {
                        // Sla token en user info op in Preferences
                        Preferences.Set("auth_token", result.token);
                        Preferences.Set("user_id", result.userId);
                        Preferences.Set("user_email", result.email);

                        General.UserId = result.userId;
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.token);

                        // Sla gebruiker ook op in lokale SQLite database
                        var lokaleGebruiker = new LocalUser 
                        { 
                            Id = result.userId, 
                            Email = result.email,
                            Voornaam = result.email.Split('@')[0]
                        };
                        await _context.SaveUserAsync(lokaleGebruiker);

                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Logout()
        {
            Preferences.Clear();
            General.UserId = "";
            General.User = null;
            client.DefaultRequestHeaders.Authorization = null;
        }

        // --- SYNCHRONISATIE ---

        public async Task SynchronizeAll()
        {
            if (!await IsAuthorized()) return;

            try
            {
                // 1. Upload ongesynchroniseerde transacties
                await UploadUnsyncedTransacties();

                // 2. Download rekeningen
                await DownloadRekeningen();

                // 3. Download transacties
                await DownloadTransacties();
            }
            catch (Exception)
            {
                // Sync mislukt, maar we kunnen offline verder
            }
        }

        private async Task UploadUnsyncedTransacties()
        {
            var unsynced = await _context.GetUnsyncedTransactiesAsync();
            foreach (var localT in unsynced)
            {
                var t = new Transactie
                {
                    VanIban = localT.VanIban,
                    NaarIban = localT.NaarIban,
                    NaamOntvanger = localT.NaamOntvanger,
                    Bedrag = localT.Bedrag,
                    Omschrijving = localT.Omschrijving,
                    Datum = localT.Datum,
                    GebruikerId = General.UserId
                };

                var response = await client.PostAsJsonAsync("Transacties", t);
                if (response.IsSuccessStatusCode)
                {
                    localT.IsSynced = true;
                    localT.LastSync = DateTime.Now;
                    await _context.SaveTransactieAsync(localT);
                }
            }
        }

        private async Task DownloadRekeningen()
        {
            var response = await client.GetAsync("Rekeningen");
            if (response.IsSuccessStatusCode)
            {
                var rekeningen = await response.Content.ReadFromJsonAsync<List<Rekening>>();
                if (rekeningen != null)
                {
                    foreach (var r in rekeningen)
                    {
                        var localR = new LocalRekening
                        {
                            Id = r.Id,
                            Iban = r.Iban,
                            Saldo = r.Saldo,
                            GebruikerId = General.UserId,
                            LastSync = DateTime.Now
                        };
                        await _context.SaveRekeningAsync(localR);
                    }
                }
            }
        }

        private async Task DownloadTransacties()
        {
            var response = await client.GetAsync("Transacties");
            if (response.IsSuccessStatusCode)
            {
                var transacties = await response.Content.ReadFromJsonAsync<List<Transactie>>();
                if (transacties != null)
                {
                    foreach (var t in transacties)
                    {
                        var localT = new LocalTransactie
                        {
                            Id = t.Id,
                            VanIban = t.VanIban,
                            NaarIban = t.NaarIban,
                            NaamOntvanger = t.NaamOntvanger,
                            Bedrag = t.Bedrag,
                            Omschrijving = t.Omschrijving,
                            Datum = t.Datum,
                            Status = t.Status.ToString(),
                            GebruikerId = General.UserId,
                            IsSynced = true,
                            LastSync = DateTime.Now
                        };
                        await _context.SaveTransactieAsync(localT);
                    }
                }
            }
        }

        public async Task<bool> IsOnline()
        {
            try
            {
                var response = await client.GetAsync("health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Message)> MaakOverschrijving(Transactie t)
        {
            try
            {
                var response = await client.PostAsJsonAsync("Transacties", t);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Succes");
                }
                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }

    // Helper klasse voor API response
    public class LoginResponse
    {
        public string token { get; set; } = string.Empty;
        public string userId { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
    }
}

