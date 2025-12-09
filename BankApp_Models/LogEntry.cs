using System;
using System.ComponentModel.DataAnnotations;

namespace BankApp_Models
{
    // Foutbehandeling - Logging model
    public class LogEntry
    {
        [Key]
        public int Id { get; set; }

        // Foutbehandeling - Tijdstip
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        // Foutbehandeling - Applicatiebron
        [MaxLength(100)]
        public string Application { get; set; } = "BankApp";

        // Foutbehandeling - Logniveau
        [MaxLength(20)]
        public string LogLevel { get; set; } = "Information";

        // Foutbehandeling - Bericht
        [Required]
        public string Message { get; set; } = string.Empty;

        // Foutbehandeling - Exception details
        public string? ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }

        // Identity Framework - Optionele user link
        public string? GebruikerId { get; set; }
        public BankUser? Gebruiker { get; set; }
    }
}

