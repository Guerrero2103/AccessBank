using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankApp_Models
{
    // Databank - Rekening tabel (alleen zichtrekeningen)
    public class Rekening
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(34)]
        public string Iban { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; }

        // Databank - Soft delete verplicht
        public DateTime Deleted { get; set; } = DateTime.MaxValue;

        // Entity Framework - Relatie gebruiker (Identity Framework)
        public string GebruikerId { get; set; } = string.Empty;
        public BankUser Gebruiker { get; set; } = null!;

        // Databank - Dummy rekening
        public static readonly Rekening Dummy = new()
        {
            Id = -1,
            Iban = "BE00000000000000",
            Saldo = 0,
            Deleted = DateTime.MinValue,
            GebruikerId = "-"
        };
    }
}
