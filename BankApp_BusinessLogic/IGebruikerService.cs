using BankApp_Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankApp_BusinessLogic
{
    public interface IGebruikerService
    {
        Task<BankUser?> GetGebruikerByIdAsync(string gebruikerId);
        Task<BankUser?> GetGebruikerByEmailAsync(string email);
        Task<List<BankUser>> GetAlleGebruikersAsync();
        Task<bool> UpdateGebruikerAsync(BankUser gebruiker);
        Task<bool> VerwijderGebruikerAsync(string gebruikerId);
        Task<bool> BlokkeerGebruikerAsync(string gebruikerId);
        Task<bool> DeblokkeerGebruikerAsync(string gebruikerId);
    }
}