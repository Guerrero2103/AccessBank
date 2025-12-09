using BankApp_Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankApp_BusinessLogic
{
    public interface IRekeningService
    {
        Task<Rekening> GetRekeningByIdAsync(int rekeningId);
        Task<Rekening> GetRekeningByIbanAsync(string iban);
        Task<List<Rekening>> GetRekeningenByGebruikerIdAsync(string gebruikerId);
        Task<Rekening> MaakRekeningAanAsync(string gebruikerId);
        Task<bool> UpdateSaldoAsync(int rekeningId, decimal nieuwSaldo);
        Task<decimal> GetTotaalSaldoAsync(string gebruikerId);
    }
}