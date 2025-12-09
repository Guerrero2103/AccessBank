using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace BankApp_Models
{
    // Design-time factory voor Entity Framework migrations
    // Dit is nodig zodat EF Core weet hoe de DbContext te maken tijdens migrations
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            // Database pad voor design-time (tijdens migrations)
            // Zoek naar solution root vanuit de Models project folder
            string currentDir = Directory.GetCurrentDirectory();
            string solutionPath = Path.GetFullPath(Path.Combine(currentDir, "..", ".."));
            string dbPath = Path.Combine(solutionPath, "BankApp_Models", "bankapp.db");
            
            // Zorg dat directory bestaat
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            
            return new AppDbContext();
        }
    }
}

