using SQLite;

namespace BankApp_MAUI.Models
{
    // Lokaal user model voor offline storage
    [Table("Users")]
    public class LocalUser
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Voornaam { get; set; } = string.Empty;
        public string Achternaam { get; set; } = string.Empty;
        public string Telefoonnummer { get; set; } = string.Empty;

        // Token voor API authenticatie
        public string Token { get; set; } = string.Empty;

        // Laatste sync timestamp
        public DateTime LastSync { get; set; } = DateTime.MinValue;
    }
}
