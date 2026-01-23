using System.ComponentModel.DataAnnotations;

namespace BankApp_Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-mail is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mail formaat")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht")]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Onthoud mij")]
        public bool RememberMe { get; set; }
    }
}
