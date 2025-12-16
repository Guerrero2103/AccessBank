using BankApp_Models;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace BankApp_Web.Services
{
    // Database logging opties
    public class DbLoggerOptions
    {
        public DbLoggerOptions()
        {
        }
    }

    [ProviderAlias("Database")]
    public class DbLoggerProvider : ILoggerProvider
    {
        public readonly DbLoggerOptions Options;

        public DbLoggerProvider(IOptions<DbLoggerOptions> _options)
        {
            Options = _options.Value;
        }

        // Maakt een nieuwe logger instantie aan
        public ILogger CreateLogger(string categoryName)
        {
            return new DbLogger(this);
        }

        public void Dispose()
        {
        }
    }

    // Database logger implementatie
    public class DbLogger : ILogger
    {
        private readonly DbLoggerProvider _dbLoggerProvider;
        private readonly AppDbContext _context;

        static public LogLevel DefaultLogLevel = LogLevel.Warning;

        // Constructor
        public DbLogger([NotNull] DbLoggerProvider dbLoggerProvider)
        {
            _dbLoggerProvider = dbLoggerProvider;
            _context = new AppDbContext();
        }

        // Interface implementatie
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        // Controleer of logging nodig is voor dit niveau
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= DefaultLogLevel && logLevel != LogLevel.None;
        }

        // Logica om te loggen naar database
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                try
                {
                    var logEntry = new LogEntry
                    {
                        Application = "BankApp_Web",
                        LogLevel = logLevel.ToString(),
                        Message = formatter(state, exception),
                        TimeStamp = DateTime.UtcNow,
                        GebruikerId = null // Kan later worden aangepast met huidige gebruiker
                    };

                    _context.LogEntries.Add(logEntry);
                    _context.SaveChanges();
                }
                catch
                {
                    // Stil falen als database logging niet werkt
                }
            }
        }
    }

    // Extension method voor logging builder
    public static class DbLoggerExtensions
    {
        public static ILoggingBuilder AddDbLogger(this ILoggingBuilder builder, Action<DbLoggerOptions> configure)
        {
            builder.Services.AddSingleton<ILoggerProvider, DbLoggerProvider>();
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
