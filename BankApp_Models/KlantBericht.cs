using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankApp_Models
{
    // Klantendienst - Contact berichten model
    public class KlantBericht
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Naam { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Onderwerp { get; set; } = string.Empty;

        [Required]
        public string Bericht { get; set; } = string.Empty;

        public DateTime Datum { get; set; } = DateTime.Now;

        // Status: Nieuw, InBehandeling, Afgehandeld
        public string Status { get; set; } = "Nieuw";

        // Medewerker die het bericht behandelt
        public string? BehandeldDoor { get; set; }
        public DateTime? BehandeldOp { get; set; }

        // Databank - Soft delete verplicht
        public DateTime Deleted { get; set; } = DateTime.MaxValue;

        // Identity Framework - Optionele user link
        public string? GebruikerId { get; set; }
        public BankUser? Gebruiker { get; set; }
    }
}

