using System.ComponentModel.DataAnnotations;

namespace BankApp_Web.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "E-mail is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mail formaat")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
    }
}
