using System.ComponentModel.DataAnnotations;

namespace BankApp_Web.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Resetcode is verplicht")]
        [StringLength(6, ErrorMessage = "De resetcode bestaat uit 6 cijfers.", MinimumLength = 6)]
        [Display(Name = "Resetcode")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nieuw wachtwoord is verplicht")]
        [StringLength(100, ErrorMessage = "Wachtwoord moet minimaal {2} tekens bevatten.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Nieuw Wachtwoord")]
        public string NieuweWachtwoord { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord bevestigen is verplicht")]
        [DataType(DataType.Password)]
        [Compare("NieuweWachtwoord", ErrorMessage = "Wachtwoorden komen niet overeen")]
        [Display(Name = "Wachtwoord Bevestigen")]
        public string WachtwoordBevestigen { get; set; } = string.Empty;
    }
}
