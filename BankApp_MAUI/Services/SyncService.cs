using BankApp_MAUI.Data;
using BankApp_MAUI.Models;

namespace BankApp_MAUI.Services
{
    // Service om gegevens te synchroniseren tussen app en server
    public class SyncService
    {
        private readonly ApiService _apiService;
        private readonly LocalDbContext _localDb;
        private readonly AuthService _authService;

        public SyncService(ApiService apiService, LocalDbContext localDb, AuthService authService)
        {
            _apiService = apiService;
            _localDb = localDb;
            _authService = authService;
        }

        // Synchroniseer alle gegevens
        public async Task<bool> SyncAllAsync()
        {
            try
            {
                // Controleer internetverbinding
                bool isOnline = await _apiService.IsOnlineAsync();
                if (!isOnline)
                {
                    return false;
                }

                var (userId, _) = _authService.GetUserInfo();

                // 1. Stuur lokale transacties naar server
                await UploadUnsyncedTransactiesAsync();

                // 2. Haal rekeningen van server op
                await SyncRekeningenAsync(userId);

                // 3. Haal transacties van server op
                await SyncTransactiesAsync(userId);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sync fout: {ex.Message}");
                return false;
            }
        }

        // Stuur lokale transacties naar server die nog niet verzonden zijn
        private async Task UploadUnsyncedTransactiesAsync()
        {
            var unsyncedTransacties = await _localDb.GetUnsyncedTransactiesAsync();

            foreach (var lokalTransactie in unsyncedTransacties)
            {
                var transactie = new BankApp_Models.Transactie
                {
                    VanIban = lokalTransactie.VanIban,
                    NaarIban = lokalTransactie.NaarIban,
                    NaamOntvanger = lokalTransactie.NaamOntvanger,
                    Bedrag = lokalTransactie.Bedrag,
                    Omschrijving = lokalTransactie.Omschrijving,
                    Datum = lokalTransactie.Datum,
                    GebruikerId = lokalTransactie.GebruikerId
                };

                var (success, _) = await _apiService.MaakOverschrijvingAsync(transactie);
                
                if (success)
                {
                    lokalTransactie.IsSynced = true;
                    lokalTransactie.LastSync = DateTime.Now;
                    await _localDb.SaveTransactieAsync(lokalTransactie);
                }
            }
        }

        // Haal rekeningen van server op en bewaar lokaal
        private async Task SyncRekeningenAsync(string gebruikerId)
        {
            var apiRekeningen = await _apiService.GetRekeningenAsync();

            foreach (var apiRekening in apiRekeningen)
            {
                var lokalRekening = new LocalRekening
                {
                    Id = apiRekening.Id,
                    Iban = apiRekening.Iban,
                    Saldo = apiRekening.Saldo,
                    GebruikerId = gebruikerId,
                    LastSync = DateTime.Now
                };

                await _localDb.SaveRekeningAsync(lokalRekening);
            }
        }

        // Haal transacties van server op en bewaar lokaal
        private async Task SyncTransactiesAsync(string gebruikerId)
        {
            var apiTransacties = await _apiService.GetTransactiesAsync();

            foreach (var apiTransactie in apiTransacties)
            {
                var lokalTransactie = new LocalTransactie
                {
                    Id = apiTransactie.Id,
                    VanIban = apiTransactie.VanIban,
                    NaarIban = apiTransactie.NaarIban,
                    NaamOntvanger = apiTransactie.NaamOntvanger,
                    Bedrag = apiTransactie.Bedrag,
                    Omschrijving = apiTransactie.Omschrijving,
                    Datum = apiTransactie.Datum,
                    Status = apiTransactie.Status.ToString(),
                    GebruikerId = gebruikerId,
                    IsSynced = true,
                    LastSync = DateTime.Now
                };

                await _localDb.SaveTransactieAsync(lokalTransactie);
            }
        }
    }
}
