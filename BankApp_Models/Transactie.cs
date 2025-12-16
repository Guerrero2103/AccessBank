using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankApp_Models
{
    public enum TransactieStatus
    {
        Voltooid,
        Wachtend,
        Afgewezen
    }

    // Databank - Transacties
    public class Transactie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string VanIban { get; set; } = string.Empty;

        [Required]
        public string NaarIban { get; set; } = string.Empty;

        public string NaamOntvanger { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Bedrag { get; set; }

        public string Omschrijving { get; set; } = string.Empty;

        public DateTime Datum { get; set; }

        // Medewerker bevestiging velden
        public TransactieStatus Status { get; set; } = TransactieStatus.Voltooid;
        public string? AfwijzingsReden { get; set; }
        public DateTime? BevestigdOp { get; set; }
        public string? BevestigdDoor { get; set; } // Medewerker ID

        // Databank - Soft delete verplicht
        public DateTime Deleted { get; set; } = DateTime.MaxValue;

        // Identity Frameworks - Uitvoerder
        public string? GebruikerId { get; set; }
        public BankUser? Gebruiker { get; set; }
    }
}

