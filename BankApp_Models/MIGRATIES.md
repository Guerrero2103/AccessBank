# Database Migraties - BankApp

## ⚠️ BELANGRIJK: Voor Alle 3 Teamleden!

Iedereen moet deze stappen uitvoeren op hun lokale machine. Zie ook `SETUP_GUIDE_3_PERSONEN.md` voor de volledige setup guide.

## Overzicht

Dit project gebruikt Entity Framework Core Migraties voor database management. De database wordt NIET meer aangemaakt via `Database.EnsureCreated()`, maar via migraties.

## Eerste Setup (Iedereen Moet Dit Doen!)

### Stap 1: Maak Initial Migration

Open **Package Manager Console** in Visual Studio en selecteer het **BankApp_Models** project:

```powershell
Add-Migration InitialCreate
```

Dit maakt een nieuwe migratie aan met alle tabellen (Identity tables + custom tables).

### Stap 2: Update Database

```powershell
Update-Database
```

Dit maakt de database aan en voert alle migraties uit.

### Stap 3: Seed Data (Automatisch!)

**Goed nieuws!** Seeding gebeurt **automatisch** wanneer je de applicatie start.

De `StartPagina` controleert bij opstarten of er data is, en seed automatisch als nodig.

**Handmatig seeden (optioneel):**
```csharp
using var context = new AppDbContext();
await AppDbContext.Seeder(context);
```

## Nieuwe Migraties Toevoegen

Wanneer je wijzigingen maakt aan de modellen:

1. **Maak nieuwe migratie:**
   ```powershell
   Add-Migration NaamVanWijziging
   ```

2. **Update database:**
   ```powershell
   Update-Database
   ```

## Migraties Verwijderen

Als je een migratie wilt verwijderen (voor je deze hebt gecommit):

```powershell
Remove-Migration
```

## Database Resetten

⚠️ **WAARSCHUWING**: Dit verwijdert alle data!

```powershell
Update-Database 0
Update-Database
```

## Command Line (Alternatief)

Als je de command line gebruikt i.p.v. Package Manager Console:

```bash
# In de BankApp_Models directory
dotnet ef migrations add InitialCreate --project BankApp/BankApp_Models
dotnet ef database update --project BankApp/BankApp_Models
```

## Migratie Bestanden

Migraties worden opgeslagen in:
```
BankApp/BankApp_Models/Migrations/
```

**NIET** deze bestanden handmatig bewerken, tenzij je weet wat je doet!

## Troubleshooting

### "No migrations found"
- Zorg dat je in het juiste project bent (BankApp_Models)
- Controleer of de Migrations folder bestaat

### "Database already exists"
- Verwijder de oude database file (`bankapp.db`)
- Run `Update-Database` opnieuw

### "Migration conflicts"
- Controleer of er uncommitted changes zijn
- Gebruik `Remove-Migration` en maak opnieuw aan

