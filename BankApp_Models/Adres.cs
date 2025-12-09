using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankApp_Models
{
    // Databank - Adres tabel volgens ERD
    public class Adres
    {
        [Key]
        public int Id { get; set; }

        // Databank - Straatgegevens
        [Required, MaxLength(100)]
        public string Straat { get; set; } = string.Empty;

        // Databank - Huisnummer
        [Required, MaxLength(10)]
        public string Huisnummer { get; set; } = string.Empty;

        // Databank - Bus
        [MaxLength(10)]
        public string? Bus { get; set; }

        // Databank - Postcode
        [Required, MaxLength(10)]
        public string Postcode { get; set; } = string.Empty;

        // Databank - Gemeente
        [Required, MaxLength(100)]
        public string Gemeente { get; set; } = string.Empty;

        // Databank - Land
        [Required, MaxLength(100)]
        public string Land { get; set; } = string.Empty;

        // Soft-delete verplichting
        public DateTime Deleted { get; set; } = DateTime.MaxValue;

        // Entity Framework - Navigatie naar gebruiker (Identity Framework)
        public BankUser? Gebruiker { get; set; }

        // Databank - Dummy record
        public static readonly Adres Dummy = new()
        {
            Id = -1,
            Straat = "-",
            Huisnummer = "-",
            Bus = null,
            Postcode = "-",
            Gemeente = "-",
            Land = "-",
            Deleted = DateTime.MaxValue
        };
    }
}

