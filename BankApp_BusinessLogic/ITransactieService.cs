using BankApp_Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankApp_BusinessLogic
{
    public interface ITransactieService
    {
        Task<Transactie> GetTransactieByIdAsync(int transactieId);
        Task<List<Transactie>> GetTransactiesByRekeningIdAsync(int rekeningId, int aantal = 50);
        Task<List<Transactie>> GetTransactiesByGebruikerIdAsync(string gebruikerId, int aantal = 50);
        Task<(bool Succes, string Bericht, Transactie Transactie)> MaakOverschrijvingAsync(
            string vanIban,
            string naarIban,
            decimal bedrag,
            string omschrijving,
            string gebruikerId);
        Task<List<Transactie>> GetWachtendeOverschrijvingenAsync();
        Task<(bool Succes, string Bericht)> BevestigOverschrijvingAsync(int transactieId, string medewerkerId);
        Task<(bool Succes, string Bericht)> AfwijsOverschrijvingAsync(int transactieId, string medewerkerId, string reden);
    }
}