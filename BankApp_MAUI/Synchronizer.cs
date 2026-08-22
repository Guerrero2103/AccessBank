using System.Net.Http.Json;
using System.Text.Json;
using BankApp_MAUI.Data;
using BankApp_MAUI.Models;
using BankApp_Models;
using Microsoft.EntityFrameworkCore;

namespace BankApp_MAUI
{
    // Centrale klasse voor alle communicatie en synchronisatie
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

            // JSON serialization options - gebruik case-insensitive voor flexibiliteit
            // ASP.NET Core gebruikt standaard PascalCase, maar we accepteren beide
            sOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // Accepteert zowel PascalCase als camelCase
                WriteIndented = true
            };
        }

        // Wordt getriggerd wanneer een geautoriseerde aanroep 401 Unauthorized teruggeeft,
        // wat betekent dat het opgeslagen token verlopen/ongeldig is geworden. Abonnees
        // (App.xaml.cs) sturen de gebruiker dan terug naar het loginscherm.
        public event Action? SessieVerlopen;

        private bool VerwerkOnverwachtOnbevoegd(System.Net.Http.HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine("Synchronizer: 401 Unauthorized ontvangen - token is verlopen/ongeldig, sessie wordt beëindigd");
                Logout();
                SessieVerlopen?.Invoke();
                return true;
            }
            return false;
        }

        // --- AUTHENTICATIE ---

        public async Task<bool> IsAuthorized()
        {
            // Kijk in de lokale voorkeuren (Preferences)
            string token = Preferences.Get("auth_token", "");
            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("IsAuthorized: No auth token found");
                return false;
            }

            // Controleer lokaal (zonder netwerkaanroep) of het token al verlopen is, via de
            // "exp"-claim in de JWT. Dit vangt een verlopen token proactief af i.p.v. pas te
            // wachten tot de server een 401 teruggeeft - en zorgt dat de gebruiker daarbij
            // dezelfde duidelijke "sessie verlopen"-melding + redirect krijgt als bij een
            // reactieve 401 (zie VerwerkOnverwachtOnbevoegd/SessieVerlopen).
            if (IsTokenVerlopen(token))
            {
                System.Diagnostics.Debug.WriteLine("IsAuthorized: Token is verlopen (lokale exp-controle)");
                Logout();
                SessieVerlopen?.Invoke();
                return false;
            }

            // Haal de UserId en Email op uit Preferences
            General.UserId = Preferences.Get("user_id", "");

            if (string.IsNullOrEmpty(General.UserId))
            {
                System.Diagnostics.Debug.WriteLine("IsAuthorized: No user_id found");
                return false;
            }

            // Zet authorization header voor alle requests
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Verifieer dat header correct is ingesteld
            var authHeader = client.DefaultRequestHeaders.Authorization?.ToString();
            System.Diagnostics.Debug.WriteLine($"IsAuthorized: UserId = {General.UserId}, Token length = {token.Length}, Auth header = {authHeader?.Substring(0, Math.Min(20, authHeader.Length))}...");
            return true;
        }

        // Decodeert enkel de payload van de JWT (geen handtekeningcontrole nodig - dat doet de
        // server toch al bij elke aanroep) om de "exp"-claim te lezen, zodat een verlopen token
        // lokaal en zonder netwerkverkeer gedetecteerd kan worden.
        private static bool IsTokenVerlopen(string jwt)
        {
            try
            {
                var delen = jwt.Split('.');
                if (delen.Length < 2)
                {
                    return true; // Geen geldig JWT-formaat
                }

                string payload = delen[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var bytes = Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("exp", out var expElement))
                {
                    long expUnix = expElement.GetInt64();
                    var expDatum = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                    return expDatum <= DateTimeOffset.UtcNow;
                }

                return false; // Geen exp-claim gevonden, kan niet bepalen - ga uit van geldig
            }
            catch
            {
                return true; // Kon token niet decoderen - beschouw als ongeldig/verlopen
            }
        }

        public async Task<bool> Login(string email, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Login: Attempting login for {email}");
                var loginData = new { Email = email, Password = password };
                
                // Verwijder oude authorization header eerst
                client.DefaultRequestHeaders.Authorization = null;
                
                var response = await client.PostAsJsonAsync("account/login", loginData);
                System.Diagnostics.Debug.WriteLine($"Login: Response status = {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    // Gebruik sOptions voor deserialisatie
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>(sOptions);
                    if (result != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Login: Success! UserId = {result.userId}, Email = {result.email}");
                        
                        // Sla token en user info op in Preferences
                        Preferences.Set("auth_token", result.token);
                        Preferences.Set("user_id", result.userId);
                        Preferences.Set("user_email", result.email);

                        General.UserId = result.userId;
                        
                        // Zet authorization header voor alle volgende requests
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.token);
                        System.Diagnostics.Debug.WriteLine($"Login: Authorization header set");

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
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Login: Failed with status {response.StatusCode}, error: {errorContent}");
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login: Exception = {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Login: Stack trace = {ex.StackTrace}");
                return false;
            }
        }

        public async Task<(bool Succes, string? FoutBoodschap)> RegistreerAsync(string email, string wachtwoord, string voornaam, string achternaam)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"RegistreerAsync: Attempting registration for {email}");
                var registerData = new { Email = email, Password = wachtwoord, Voornaam = voornaam, Achternaam = achternaam };

                // Verwijder oude authorization header eerst
                client.DefaultRequestHeaders.Authorization = null;

                var response = await client.PostAsJsonAsync("account/register", registerData);
                System.Diagnostics.Debug.WriteLine($"RegistreerAsync: Response status = {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    // De register-endpoint geeft dezelfde vorm terug als login (token, userId, email)
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>(sOptions);
                    if (result != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"RegistreerAsync: Success! UserId = {result.userId}, Email = {result.email}");

                        // Sla token en user info op in Preferences (zelfde als Login)
                        Preferences.Set("auth_token", result.token);
                        Preferences.Set("user_id", result.userId);
                        Preferences.Set("user_email", result.email);

                        General.UserId = result.userId;

                        // Zet authorization header voor alle volgende requests
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.token);
                        System.Diagnostics.Debug.WriteLine($"RegistreerAsync: Authorization header set");

                        // Sla gebruiker ook op in lokale SQLite database
                        var lokaleGebruiker = new LocalUser
                        {
                            Id = result.userId,
                            Email = result.email,
                            Voornaam = voornaam,
                            Achternaam = achternaam
                        };
                        await _context.SaveUserAsync(lokaleGebruiker);

                        return (true, null);
                    }
                    return (false, "Onverwacht antwoord van de server.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"RegistreerAsync: Failed with status {response.StatusCode}, error: {errorContent}");

                    string? foutBoodschap = null;
                    try
                    {
                        var errorDoc = JsonDocument.Parse(errorContent);
                        if (errorDoc.RootElement.TryGetProperty("message", out var msgProp))
                        {
                            foutBoodschap = msgProp.GetString();
                        }
                    }
                    catch
                    {
                        // Geen geldige JSON in de foutrespons, val terug op generieke melding
                    }

                    return (false, foutBoodschap ?? "Registratie mislukt.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RegistreerAsync: Exception = {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"RegistreerAsync: Stack trace = {ex.StackTrace}");
                return (false, $"Er ging iets mis: {ex.Message}");
            }
        }

        // --- PROFIEL ---

        public async Task<ProfielResponse?> HaalProfielOpAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("HaalProfielOpAsync: Ophalen profiel");
                var response = await client.GetAsync("account/profile");
                System.Diagnostics.Debug.WriteLine($"HaalProfielOpAsync: Response status = {response.StatusCode}");

                if (VerwerkOnverwachtOnbevoegd(response)) return null;

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ProfielResponse>(sOptions);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"HaalProfielOpAsync: Failed with status {response.StatusCode}, error: {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HaalProfielOpAsync: Exception = {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Succes, string? FoutBoodschap)> WerkProfielBijAsync(
            string voornaam, string achternaam, string telefoonnummer,
            string? straat, string? huisnummer, string? bus, string? postcode, string? gemeente, string? land)
        {
            try
            {
                var updateData = new
                {
                    Voornaam = voornaam,
                    Achternaam = achternaam,
                    Telefoonnummer = telefoonnummer,
                    Straat = straat,
                    Huisnummer = huisnummer,
                    Bus = bus,
                    Postcode = postcode,
                    Gemeente = gemeente,
                    Land = land
                };

                System.Diagnostics.Debug.WriteLine("WerkProfielBijAsync: Profiel bijwerken");
                var response = await client.PutAsJsonAsync("account/profile", updateData);
                System.Diagnostics.Debug.WriteLine($"WerkProfielBijAsync: Response status = {response.StatusCode}");

                if (VerwerkOnverwachtOnbevoegd(response)) return (false, "Je sessie is verlopen. Log opnieuw in.");

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"WerkProfielBijAsync: Failed with status {response.StatusCode}, error: {errorContent}");

                string? foutBoodschap = null;
                try
                {
                    var errorDoc = JsonDocument.Parse(errorContent);
                    if (errorDoc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        foutBoodschap = msgProp.GetString();
                    }
                }
                catch
                {
                    // Geen geldige JSON in de foutrespons
                }

                return (false, foutBoodschap ?? "Bijwerken van profiel mislukt.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WerkProfielBijAsync: Exception = {ex.Message}");
                return (false, $"Er ging iets mis: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("SynchronizeAll: Starting synchronization");
            
            if (!await IsAuthorized())
            {
                System.Diagnostics.Debug.WriteLine("SynchronizeAll: Not authorized, skipping sync");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("SynchronizeAll: Authorized, proceeding with sync");
                
                // 1. Upload ongesynchroniseerde transacties
                await UploadUnsyncedTransacties();

                // 2. Download rekeningen
                await DownloadRekeningen();

                // 3. Download transacties
                await DownloadTransacties();
                
                System.Diagnostics.Debug.WriteLine("SynchronizeAll: Synchronization completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SynchronizeAll error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Sync mislukt, maar we kunnen offline verder
            }
        }

        private async Task UploadUnsyncedTransacties()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UploadUnsyncedTransacties: Starting upload");
                var unsynced = await _context.GetUnsyncedTransactiesAsync();
                System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Found {unsynced.Count} unsynced transactions");
                
                if (unsynced.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("UploadUnsyncedTransacties: No unsynced transactions to upload");
                    return;
                }
                
                foreach (var localT in unsynced)
                {
                    try
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

                        System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Uploading transaction ID={localT.Id}, Bedrag={localT.Bedrag}, VanIban={localT.VanIban}, NaarIban={localT.NaarIban}");
                        var response = await client.PostAsJsonAsync("Transacties", t, sOptions);

                        System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Response status = {response.StatusCode}");

                        if (VerwerkOnverwachtOnbevoegd(response))
                        {
                            return;
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            localT.IsSynced = true;
                            localT.LastSync = DateTime.Now;
                            await _context.SaveTransactieAsync(localT);
                            System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Transaction {localT.Id} successfully synced");
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Failed to sync transaction {localT.Id}: Status={response.StatusCode}, Error={errorContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Error uploading transaction {localT.Id}: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Stack trace: {ex.StackTrace}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("UploadUnsyncedTransacties: Upload completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Critical error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"UploadUnsyncedTransacties: Stack trace: {ex.StackTrace}");
                throw; // Gooi door voor betere error handling
            }
        }

        private async Task DownloadRekeningen()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: Starting download for user {General.UserId}");
                System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: API URL = {General.ApiUrl}Rekeningen");
                
                // Verifieer JWT token in header
                var authHeader = client.DefaultRequestHeaders.Authorization?.ToString();
                if (string.IsNullOrEmpty(authHeader))
                {
                    System.Diagnostics.Debug.WriteLine("DownloadRekeningen: WARNING - Authorization header is NULL!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: Auth header present (length = {authHeader.Length})");
                }
                
                var response = await client.GetAsync("Rekeningen");

                System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: Response status = {response.StatusCode}");

                if (VerwerkOnverwachtOnbevoegd(response))
                {
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: Error response = {errorContent}");
                }
                
                if (response.IsSuccessStatusCode)
                {
                    // Lees JSON string eenmalig
                    var jsonString = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: Raw JSON response = {jsonString}");
                    
                    // Deserialiseer direct van de string (niet van de stream)
                    var rekeningen = System.Text.Json.JsonSerializer.Deserialize<List<Rekening>>(jsonString, sOptions);
                    System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: Received {rekeningen?.Count ?? 0} rekeningen");
                    
                    if (rekeningen != null && rekeningen.Count > 0)
                    {
                        foreach (var r in rekeningen)
                        {
                            // Check of rekening al bestaat
                            var existing = await _context.GetRekeningByIbanAsync(r.Iban);
                            if (existing != null)
                            {
                                // Update bestaande rekening
                                existing.Saldo = r.Saldo;
                                existing.LastSync = DateTime.Now;
                                await _context.SaveRekeningAsync(existing);
                                System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: Updated rekening {r.Iban} with saldo {r.Saldo}");
                            }
                            else
                            {
                                // Nieuwe rekening - Id wordt automatisch gegenereerd
                                var localR = new LocalRekening
                                {
                                    Iban = r.Iban,
                                    Saldo = r.Saldo,
                                    GebruikerId = General.UserId,
                                    LastSync = DateTime.Now
                                };
                                await _context.SaveRekeningAsync(localR);
                                System.Diagnostics.Debug.WriteLine($"DownloadRekeningen: Created new rekening {r.Iban} with saldo {r.Saldo}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("DownloadRekeningen: No rekeningen received or list is empty");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadRekeningen error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"DownloadRekeningen stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task DownloadTransacties()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DownloadTransacties: Starting download for user {General.UserId}");
                var response = await client.GetAsync("Transacties");

                System.Diagnostics.Debug.WriteLine($"DownloadTransacties: Response status = {response.StatusCode}");

                if (VerwerkOnverwachtOnbevoegd(response))
                {
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"DownloadTransacties: Error response = {errorContent}");
                }
                
                if (response.IsSuccessStatusCode)
                {
                    // Lees JSON string eenmalig
                    var jsonString = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"DownloadTransacties: Raw JSON response length = {jsonString.Length}");
                    
                    // Deserialiseer direct van de string (niet van de stream)
                    var transacties = System.Text.Json.JsonSerializer.Deserialize<List<Transactie>>(jsonString, sOptions);
                    System.Diagnostics.Debug.WriteLine($"DownloadTransacties: Received {transacties?.Count ?? 0} transacties");
                    
                    if (transacties != null && transacties.Count > 0)
                    {
                        // Haal bestaande transacties op om duplicaten te voorkomen
                        var existingTransacties = await _context.GetTransactiesAsync(General.UserId, 1000);
                        
                        foreach (var t in transacties)
                        {
                            // Check of transactie al bestaat op basis van IBANs, Bedrag en Datum
                            var existing = existingTransacties.FirstOrDefault(et => 
                                et.VanIban == t.VanIban && 
                                et.NaarIban == t.NaarIban && 
                                et.Bedrag == t.Bedrag && 
                                et.Datum.Date == t.Datum.Date);

                            if (existing == null)
                            {
                                // Nieuwe transactie - Id wordt automatisch gegenereerd
                                var localT = new LocalTransactie
                                {
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
                                System.Diagnostics.Debug.WriteLine($"DownloadTransacties: Created new transaction {localT.Id}");
                            }
                            else
                            {
                                // Update bestaande transactie (status kan veranderd zijn)
                                existing.Status = t.Status.ToString();
                                existing.LastSync = DateTime.Now;
                                existing.IsSynced = true;
                                await _context.SaveTransactieAsync(existing);
                                System.Diagnostics.Debug.WriteLine($"DownloadTransacties: Updated existing transaction {existing.Id}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("DownloadTransacties: No transacties received or list is empty");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadTransacties error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"DownloadTransacties stack trace: {ex.StackTrace}");
                // Gooi exception door voor betere error handling
                throw;
            }
        }

        public async Task<bool> IsOnline()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"IsOnline: Checking connectivity to {General.ApiUrl}");
                
                // Probeer een eenvoudige API call (Rekeningen endpoint)
                // Als we geautoriseerd zijn, kunnen we dit gebruiken
                if (await IsAuthorized())
                {
                    System.Diagnostics.Debug.WriteLine($"IsOnline: Authorized, checking API endpoint");
                    var response = await client.GetAsync("Rekeningen");
                    
                    System.Diagnostics.Debug.WriteLine($"IsOnline: Response status = {response.StatusCode}");
                    
                    // 200 OK = online en geautoriseerd
                    // 401 Unauthorized = online maar niet geautoriseerd (server is bereikbaar)
                    // Andere status codes = mogelijk offline of server error
                    bool isOnline = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
                    System.Diagnostics.Debug.WriteLine($"IsOnline: Result = {isOnline}");
                    return isOnline;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("IsOnline: Not authorized, cannot check connectivity");
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsOnline: Exception = {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"IsOnline: Stack trace = {ex.StackTrace}");
                return false;
            }
        }

        public async Task<(bool Success, string Message)> MaakOverschrijving(Transactie t)
        {
            try
            {
                var response = await client.PostAsJsonAsync("Transacties", t, sOptions);

                if (VerwerkOnverwachtOnbevoegd(response))
                {
                    return (false, "Je sessie is verlopen. Log opnieuw in.");
                }

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

    // Helper klasse voor profiel-API-response
    public class ProfielResponse
    {
        public string voornaam { get; set; } = string.Empty;
        public string achternaam { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? telefoonnummer { get; set; }
        public string? straat { get; set; }
        public string? huisnummer { get; set; }
        public string? bus { get; set; }
        public string? postcode { get; set; }
        public string? gemeente { get; set; }
        public string? land { get; set; }
    }
}

