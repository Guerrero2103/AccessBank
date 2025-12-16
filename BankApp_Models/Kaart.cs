using System;
using System.ComponentModel.DataAnnotations;

namespace BankApp_Models
{
    public enum KaartStatus
    {
        Actief,
        Bevroren,
        Geblokkeerd
    }

    // Databank - Kaarten
    public class Kaart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string KaartNummer { get; set; } = string.Empty;

        [Required]
        public KaartStatus Status { get; set; }

        // Databank - Soft delete verplicht
        public DateTime Deleted { get; set; } = DateTime.MaxValue;

        // Entity Framework - Relatie gebruiker (Identity Framework)
        public string GebruikerId { get; set; } = string.Empty;
        public BankUser Gebruiker { get; set; } = null!;
    }
}

