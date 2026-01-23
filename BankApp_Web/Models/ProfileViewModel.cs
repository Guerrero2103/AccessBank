using System.ComponentModel.DataAnnotations;

namespace BankApp_Web.Models
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Voornaam is verplicht")]
        [Display(Name = "Voornaam")]
        public string Voornaam { get; set; } = string.Empty;

        [Required(ErrorMessage = "Achternaam is verplicht")]
        [Display(Name = "Achternaam")]
        public string Achternaam { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty; // Read-only

        [Display(Name = "Telefoonnummer")]
        public string Telefoonnummer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Geboortedatum is verplicht")]
        [DataType(DataType.Date)]
        [Display(Name = "Geboortedatum")]
        public DateTime Geboortedatum { get; set; }

        [Required(ErrorMessage = "Straatnaam is verplicht")]
        [Display(Name = "Straatnaam")]
        public string Straat { get; set; } = string.Empty;

        [Required(ErrorMessage = "Huisnummer is verplicht")]
        [Display(Name = "Huisnummer")]
        public string Huisnummer { get; set; } = string.Empty;

        [Display(Name = "Bus (optioneel)")]
        public string? Bus { get; set; }

        [Required(ErrorMessage = "Postcode is verplicht")]
        [Display(Name = "Postcode")]
        public string Postcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gemeente is verplicht")]
        [Display(Name = "Gemeente")]
        public string Gemeente { get; set; } = string.Empty;

        [Display(Name = "Land")]
        public string? Land { get; set; } = "België";

        [Display(Name = "IBAN")]
        public string Iban { get; set; } = string.Empty; // Read-only

        [Display(Name = "Nieuwe Wachtwoord")]
        [DataType(DataType.Password)]
        public string? NieuweWachtwoord { get; set; }

        [Display(Name = "Wachtwoord Bevestigen")]
        [DataType(DataType.Password)]
        [Compare("NieuweWachtwoord", ErrorMessage = "Wachtwoorden komen niet overeen")]
        public string? WachtwoordBevestigen { get; set; }
    }
}
