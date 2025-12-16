using BankApp_MAUI.Models;
using SQLite;
using System.IO;

namespace BankApp_MAUI.Data
{
    // Lokale database voor offline gebruik
    public class LocalDbContext
    {
        private readonly SQLiteAsyncConnection _database;

        public LocalDbContext()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "bankapp_local.db");
            _database = new SQLiteAsyncConnection(dbPath);

            // Maak tabellen aan
            _database.CreateTableAsync<LocalUser>().Wait();
            _database.CreateTableAsync<LocalRekening>().Wait();
            _database.CreateTableAsync<LocalTransactie>().Wait();
        }

        // Gebruiker ophalen
        public Task<LocalUser> GetUserAsync(string id)
        {
            return _database.Table<LocalUser>()
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();
        }

        public Task<int> SaveUserAsync(LocalUser user)
        {
            if (!string.IsNullOrEmpty(user.Id))
            {
                return _database.UpdateAsync(user);
            }
            else
            {
                return _database.InsertAsync(user);
            }
        }

        // Rekeningen ophalen
        public Task<List<LocalRekening>> GetRekeningenAsync(string gebruikerId)
        {
            return _database.Table<LocalRekening>()
                .Where(r => r.GebruikerId == gebruikerId)
                .ToListAsync();
        }

        public Task<LocalRekening> GetRekeningByIbanAsync(string iban)
        {
            return _database.Table<LocalRekening>()
                .Where(r => r.Iban == iban)
                .FirstOrDefaultAsync();
        }

        public Task<int> SaveRekeningAsync(LocalRekening rekening)
        {
            if (rekening.Id != 0)
            {
                return _database.UpdateAsync(rekening);
            }
            else
            {
                return _database.InsertAsync(rekening);
            }
        }

        public Task<int> SaveRekeningenAsync(List<LocalRekening> rekeningen)
        {
            return _database.InsertAllAsync(rekeningen);
        }

        // Transacties ophalen
        public Task<List<LocalTransactie>> GetTransactiesAsync(string gebruikerId, int limit = 50)
        {
            return _database.Table<LocalTransactie>()
                .Where(t => t.GebruikerId == gebruikerId)
                .OrderByDescending(t => t.Datum)
                .Take(limit)
                .ToListAsync();
        }

        public Task<List<LocalTransactie>> GetUnsyncedTransactiesAsync()
        {
            return _database.Table<LocalTransactie>()
                .Where(t => !t.IsSynced)
                .ToListAsync();
        }

        public Task<int> SaveTransactieAsync(LocalTransactie transactie)
        {
            if (transactie.Id != 0)
            {
                return _database.UpdateAsync(transactie);
            }
            else
            {
                return _database.InsertAsync(transactie);
            }
        }

        public Task<int> SaveTransactiesAsync(List<LocalTransactie> transacties)
        {
            return _database.InsertAllAsync(transacties);
        }

        // Verwijder alle gegevens (bij uitloggen)
        public async Task ClearAllDataAsync()
        {
            await _database.DeleteAllAsync<LocalUser>();
            await _database.DeleteAllAsync<LocalRekening>();
            await _database.DeleteAllAsync<LocalTransactie>();
        }
    }
}
