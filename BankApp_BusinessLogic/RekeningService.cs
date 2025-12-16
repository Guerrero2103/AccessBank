using BankApp_Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_BusinessLogic
{
    public class RekeningService : IRekeningService
    {
        private readonly AppDbContext _context;

        public RekeningService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Rekening> GetRekeningByIdAsync(int rekeningId)
        {
            return await _context.Rekeningen
                .Where(r => r.Deleted == DateTime.MaxValue)
                .Include(r => r.Gebruiker)
                .FirstOrDefaultAsync(r => r.Id == rekeningId);
        }

        public async Task<Rekening> GetRekeningByIbanAsync(string iban)
        {
            return await _context.Rekeningen
                .Where(r => r.Deleted == DateTime.MaxValue)
                .Include(r => r.Gebruiker)
                .FirstOrDefaultAsync(r => r.Iban == iban);
        }

        public async Task<List<Rekening>> GetRekeningenByGebruikerIdAsync(string gebruikerId)
        {
            // Haal alle rekeningen van gebruiker op
            return await _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .ToListAsync();
        }

        public async Task<Rekening> MaakRekeningAanAsync(string gebruikerId)
        {
            // Controleer of gebruiker bestaat
            var gebruiker = (from user in _context.Users
                            where user.Id == gebruikerId && user.Deleted == DateTime.MaxValue
                            select user)
                           .FirstOrDefaultAsync();
            
            if (await gebruiker == null)
                throw new ArgumentException("Gebruiker niet gevonden");

            string iban = GenereerIBAN();

            while (await _context.Rekeningen.AnyAsync(r => r.Iban == iban))
            {
                iban = GenereerIBAN();
            }

            var rekening = new Rekening
            {
                GebruikerId = gebruikerId,
                Iban = iban,
                Saldo = 0,
                Deleted = DateTime.MaxValue
            };

            _context.Rekeningen.Add(rekening);
            await _context.SaveChangesAsync();

            return rekening;
        }

        public async Task<bool> UpdateSaldoAsync(int rekeningId, decimal nieuwSaldo)
        {
            var rekening = await _context.Rekeningen
                .FirstOrDefaultAsync(r => r.Id == rekeningId && r.Deleted == DateTime.MaxValue);
            if (rekening == null)
                return false;

            rekening.Saldo = nieuwSaldo;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotaalSaldoAsync(string gebruikerId)
        {
            // Bereken totaal saldo van alle rekeningen
            var totaalSaldo = (from rekening in _context.Rekeningen
                              where rekening.GebruikerId == gebruikerId && rekening.Deleted == DateTime.MaxValue
                              select rekening.Saldo)
                             .SumAsync();
            
            return await totaalSaldo;
        }

        private string GenereerIBAN()
        {
            Random random = new Random();
            string accountNummer = random.Next(100000000, 999999999).ToString().PadLeft(12, '0');
            int checkDigit = random.Next(10, 99);
            return $"BE{checkDigit}{accountNummer}";
        }
    }
}