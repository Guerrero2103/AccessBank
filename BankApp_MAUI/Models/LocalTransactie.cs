using SQLite;

namespace BankApp_MAUI.Models
{
    // Lokaal transactie model voor offline storage
    [Table("Transacties")]
    public class LocalTransactie
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string VanIban { get; set; } = string.Empty;
        public string NaarIban { get; set; } = string.Empty;
        public string NaamOntvanger { get; set; } = string.Empty;
        public decimal Bedrag { get; set; }
        public string Omschrijving { get; set; } = string.Empty;
        public DateTime Datum { get; set; }
        public string Status { get; set; } = "Voltooid";
        public string GebruikerId { get; set; } = string.Empty;

        // Sync status
        public bool IsSynced { get; set; } = false;
        public DateTime LastSync { get; set; } = DateTime.MinValue;
    }
}
