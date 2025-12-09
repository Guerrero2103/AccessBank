using BankApp_Models;
using Microsoft.EntityFrameworkCore;

namespace BankApp_Cons;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("    BankApp Database Setup Tool");
        Console.WriteLine("===========================================\n");

        // SQLite database pad
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string solutionPath = Path.GetFullPath(Path.Combine(basePath, @"..\..\..\..\"));
        string dbPath = Path.Combine(solutionPath, "BankApp_Models", "bankapp.db");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        using (var context = new AppDbContext())
        {
            Console.WriteLine("1. Creating database...");
            await context.Database.EnsureCreatedAsync();
            Console.WriteLine("   ✓ Database created successfully!\n");

            Console.WriteLine("2. Checking for existing data...");
            var userCount = await context.Gebruikers.CountAsync();

            if (userCount == 0)
            {
                Console.WriteLine("   No users found. Seeding initial data...\n");
                await SeedInitialData(context);
            }
            else
            {
                Console.WriteLine($"   ✓ Database already contains {userCount} user(s).\n");
            }

            Console.WriteLine("===========================================");
            Console.WriteLine("    Database Setup Complete!");
            Console.WriteLine("===========================================\n");

            Console.WriteLine("Test Accounts:");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Admin Account:");
            Console.WriteLine("  Email: beheerder@bankapp.local");
            Console.WriteLine("  Password: admin_pw_789");
            Console.WriteLine("\nUser Account:");
            Console.WriteLine("  Email: jan.peeters@example.com");
            Console.WriteLine("  Password: hashed_pw_123");
            Console.WriteLine("------------------------------------------------\n");

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    static async Task SeedInitialData(AppDbContext context)
    {
        try
        {
            // De data is al geseeded via OnModelCreating in AppDbContext
            Console.WriteLine("   ✓ Database already contains seeded data!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n   ✗ Error: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }
}