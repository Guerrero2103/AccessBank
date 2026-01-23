using System.ComponentModel.DataAnnotations;

namespace BankApp_Web.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Voornaam is verplicht")]
        [Display(Name = "Voornaam")]
        public string Voornaam { get; set; } = string.Empty;

        [Required(ErrorMessage = "Achternaam is verplicht")]
        [Display(Name = "Achternaam")]
        public string Achternaam { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mail formaat")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht")]
        [StringLength(100, ErrorMessage = "Wachtwoord moet minimaal {2} tekens bevatten.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string Wachtwoord { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord bevestigen is verplicht")]
        [DataType(DataType.Password)]
        [Compare("Wachtwoord", ErrorMessage = "Wachtwoorden komen niet overeen")]
        [Display(Name = "Wachtwoord Bevestigen")]
        public string WachtwoordBevestigen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefoonnummer is verplicht")]
        [Display(Name = "Telefoonnummer")]
        public string Telefoonnummer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Geboortedatum is verplicht")]
        [DataType(DataType.Date)]
        [Display(Name = "Geboortedatum")]
        public DateTime? Geboortedatum { get; set; }

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
    }
}
