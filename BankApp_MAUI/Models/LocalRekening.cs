using SQLite;

namespace BankApp_MAUI.Models
{
    // Lokaal rekening model voor offline storage
    [Table("Rekeningen")]
    public class LocalRekening
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Iban { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
        public string GebruikerId { get; set; } = string.Empty;

        // Laatste sync timestamp
        public DateTime LastSync { get; set; } = DateTime.MinValue;
    }
}
