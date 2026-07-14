using BankApp_Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankApp_BusinessLogic
{
    // Gegevens die de drie registratiepaden (Identity Pages, AccountController, AccountApiController)
    // elk in verschillende mate verzamelen. Niet-verzamelde velden blijven op hun default staan.
    public class RegistratieGegevens
    {
        public string Email { get; set; } = string.Empty;
        public string Wachtwoord { get; set; } = string.Empty;
        public string Voornaam { get; set; } = string.Empty;
        public string Achternaam { get; set; } = string.Empty;
        public string Telefoonnummer { get; set; } = string.Empty;
        public DateTime? Geboortedatum { get; set; }
        public string? Straat { get; set; }
        public string? Huisnummer { get; set; }
        public string? Bus { get; set; }
        public string? Postcode { get; set; }
        public string? Gemeente { get; set; }
        public string? Land { get; set; }
    }

    public class RegistratieResultaat
    {
        public bool Succes { get; set; }
        public List<string> Fouten { get; set; } = new();
        public BankUser? Gebruiker { get; set; }
    }

    public interface IRegistratieService
    {
        Task<RegistratieResultaat> RegistreerAsync(RegistratieGegevens gegevens);
    }
}
