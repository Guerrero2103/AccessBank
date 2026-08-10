# BankApp Project - AccessBank

> **Let op:** deze solution bevat ook de projecten `BankApp_Web` en `BankApp_MAUI`, die horen bij het vak .NET Advanced. Voor de WPF-opdracht (dit vak) zijn enkel `BankApp_Models` en `BankApp_WPF` relevant.

## Over dit project

Dit is een banktoepassing gebouwd met .NET 9.0 MAUI voor mobiel en desktop. Het project is gemaakt door drie personen die elk verschillende onderdelen hebben ontwikkeld.

## Wat doet deze app?

De BankApp is een moderne banktoepassing waar gebruikers kunnen:
- Inloggen en registreren
- Hun rekeningen bekijken
- Saldo raadplegen
- Overschrijvingen maken
- Transacties bekijken
- Kaarten beheren
- Contact opnemen met de klantendienst

De app werkt zowel online als offline en synchroniseert automatisch de gegevens wanneer er internet is.

## Technologieën

- .NET 9.0
- MAUI voor Android en Windows
- ASP.NET Core Web API
- Entity Framework Core
- SQLite voor lokale database
- Identity Framework voor gebruikersbeheer
- JWT (Json Web Token) tokens voor authenticatie

## Licenties van gebruikte libraries

Alle NuGet-packages die in dit project gebruikt worden zijn **MIT-gelicentieerd** — dat betekent dat ze vrij gebruikt, aangepast en herverdeeld mogen worden (ook commercieel), zolang de copyright-vermelding van de oorspronkelijke auteur behouden blijft.

| Package | Versie | Licentie | Gebruikt in |
|---|---|---|---|
| Microsoft.EntityFrameworkCore | 9.0.0 | MIT | Models, Web, MAUI |
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.0 | MIT | Models, Web, MAUI |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | MIT | Models, Web |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 / 9.0.10 | MIT | Models, Web, WPF, Cons |
| Microsoft.EntityFrameworkCore.Tools | 9.0.0 | MIT | Models, Web |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.0 / 9.0.10 | MIT | Models, WPF |
| Microsoft.AspNetCore.Identity.UI | 9.0.0 | MIT | Web |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.0 | MIT | Web |
| Microsoft.AspNetCore.OpenApi | 9.0.11 | MIT | Web |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.10 | MIT | WPF |
| Microsoft.Extensions.Logging.Debug | 9.0.8 | MIT | MAUI |
| Microsoft.VisualStudio.Web.CodeGeneration.Design | 9.0.0 | MIT | Web |
| Swashbuckle.AspNetCore.Swagger / SwaggerGen / SwaggerUI | 9.0.6 | MIT | Web |
| System.IdentityModel.Tokens.Jwt | 8.2.1 | MIT | Web |
| CommunityToolkit.Mvvm | 8.4.0 | MIT | MAUI |
| Microsoft.Maui.Controls | (MauiVersion) | MIT | MAUI |
| Newtonsoft.Json | 13.0.3 | MIT | MAUI |
| sqlite-net-pcl | 1.8.116 | MIT | MAUI |

## AI-ondersteuning bij de ontwikkeling

Een deel van de code in dit project is tot stand gekomen met hulp van **Claude Code** (Anthropic), een AI-coding-assistent, onder begeleiding en met review van het team. Dit betreft specifiek:

- De autorisatiecontrole in `TransactiesController.PostTransactie` (controle of `VanIban` toebehoort aan de ingelogde gebruiker) en het bijhorende testscenario in `docs/test-transactie-autorisatie.md`
- De centrale `RegistratieService` in `BankApp_BusinessLogic`, die de drie registratiepaden (Identity Pages, `AccountController`, `AccountApiController`) samenvoegt en de ontbrekende rekening/kaart-aanmaak bij API-registratie herstelt
- Soft-delete op `LogEntry` (property + query filter + EF Core-migratie) en de `Dummy`-objecten op de modellen
- Rollenbeheer (blokkeren/deblokkeren) in `BankApp_WPF/AdminPagina`, het daadwerkelijk gebruiken van `SaldoCardControl` in `HoofdPagina`, en de rekening-`ComboBox` in `OverschrijvingenPagina`
- Het consistent maken van de XAML-styling (gedeelde `Style`-resources) in `LoginPagina` en `RegistratiePagina`
- Deze README-secties (licenties en AI-vermelding)

De oorspronkelijke basisapplicatie (zie "Verdeling van het werk" hieronder) is door de drie teamleden zelf gebouwd, zonder AI-ondersteuning.

## Verdeling van het werk

### Tyvian: Gebruikersbeheer en Authenticatie

Deze persoon heeft alle functionaliteit gebouwd rondom gebruikers en inloggen:

**Models:**
- BankUser model met alle gebruikersvelden zoals voornaam, achternaam, telefoonnummer
- Adres model voor het bewaren van adressen
- LogEntry model voor het bijhouden van wat er gebeurt in de app

**MAUI (mobiele app):**
- LoginPage met het scherm om in te loggen
- LoginViewModel met alle logica voor het inloggen

**Web API:**
- AccountApiController met endpoints voor inloggen en registreren
- JWT token generatie voor veilige authenticatie

**Services:**
- GebruikerService met methodes om gebruikers te beheren

Kortom: alles wat te maken heeft met inloggen, uitloggen, gebruikers aanmaken en beheren is door deze persoon gemaakt.

### Huzeyfe: Rekeningen en Transacties

Deze persoon heeft de kernfunctionaliteit van de bank gebouwd:

**Models:**
- Rekening model met IBAN en saldo
- Transactie model voor overschrijvingen met bedrag, datum en status

**MAUI (mobiele app):**
- RekeningenPage om alle rekeningen te tonen
- RekeningenViewModel met de logica voor rekeningen ophalen
- OverschrijvingPage om geld over te maken
- OverschrijvingViewModel met validatie en verwerking
- General.cs met de API URL en globale instellingen
- Synchronizer.cs, de belangrijkste service die alle communicatie met de API regelt

**Web API:**
- RekeningenController voor het ophalen en aanmaken van rekeningen
- TransactiesController voor het verwerken van overschrijvingen

**Services:**
- RekeningService met IBAN generatie en saldo beheer
- TransactieService met validatie en verwerking van overschrijvingen

Kortom: alles wat te maken heeft met rekeningen bekijken, saldo's, en geld overmaken is door deze persoon gemaakt.

### Abdullah: Dashboard, Transacties en Extra's

Deze persoon heeft de gebruikersinterface en extra functionaliteit gebouwd:

**Models:**
- Kaart model voor bankkaarten met status
- KlantBericht model voor berichten aan de klantenservice

**MAUI (mobiele app):**
- MainPage als hoofdscherm met dashboard
- MainViewModel met saldo overzicht en navigatie
- TransactiesPage om alle transacties te bekijken
- TransactiesViewModel met filtering en overzicht
- MauiProgram.cs met alle configuratie en services
- AndroidManifest.xml voor Android instellingen

**Web Controllers:**
- MedewerkerController voor medewerkers om transacties goed te keuren
- HomeController voor de homepagina

**Services:**
- KaartService voor het beheren van kaarten
- KlantBerichtService voor berichten van klanten
- LoggingService voor het bijhouden van logs

Kortom: alles wat te maken heeft met het dashboard, transacties bekijken, kaarten beheren en klantenservice is door deze persoon gemaakt.

## Architectuur

De app bestaat uit drie lagen:

**BankApp_Models**
Hier staan alle modellen die overal gebruikt worden zoals BankUser, Rekening, Transactie, Kaart, etz.

**BankApp_Web**
Dit is de ASP.NET Core web applicatie met:
- Controllers voor de website
- API Controllers voor de mobiele app
- Views voor de webpaginas

**BankApp_MAUI**
Dit is de mobiele en desktop app met:
- Pages voor de schermen
- ViewModels voor de logica
- Services voor API communicatie
- Lokale SQLite database voor offline gebruik

## Hoe werkt de synchronisatie?

De app gebruikt de Synchronizer class die:
- Checkt of er internet is
- Haalt data op van de API als er internet is
- Bewaart alles lokaal in SQLite
- Stuurt nieuwe transacties naar de API zodra er weer internet is
- Werkt dus gewoon door als er geen internet is

## Database

De app gebruikt twee databases:

**SQL Server (online via de API):**
Alle echte data zoals gebruikers, rekeningen, transacties

**SQLite (lokaal op de telefoon):**
Een kopie van de data zodat de app offline kan werken

## Beveiliging

- Wachtwoorden worden veilig opgeslagen met hashing
- API calls zijn beveiligd met JWT tokens
- HTTPS wordt gebruikt voor alle communicatie
- Gebruikers kunnen alleen hun eigen data zien
- Soft-delete: data wordt nooit echt verwijderd maar alleen verborgen


## Status van het project

Het basisproject is af met:
- Werkende login en registratie
- Rekeningen bekijken
- Overschrijvingen maken
- Transacties overzicht
- Online en offline werking
- Data synchronisatie

## Voor ontwikkelaars

**Vereisten:**
- Visual Studio 2022
- .NET 9.0 SDK
- Android SDK (voor mobiele ontwikkeling)

**Hoe te starten:**
1. Clone de repository
2. Open AccessBank.sln in Visual Studio
3. Herstel NuGet packages
4. Start eerst BankApp_Web (de API)
5. Start daarna BankApp_MAUI

**Database setup:**
De database wordt automatisch aangemaakt bij de eerste start. Er zijn standaard testgebruikers beschikbaar.

## Test Gebruikers

Je kan de app testen met deze gebruikers:

**Klant Account:**
- Email: jan.peeters@example.com
- Wachtwoord: Password123!
- Rol: Klant (normale gebruiker)

**Medewerker Account:**
- Email: sarah.janssens@example.com
- Wachtwoord: Password123!
- Rol: Medewerker (kan transacties goedkeuren)

**Admin Account:**
- Email: admin@bankapp.local
- Wachtwoord: Admin123!
- Rol: Admin (volledige toegang)

## Screenshots

### Web Applicatie
![Web Home](Foto's/Web%20Home.png)

### Mobiele App (MAUI)
![App Home](Foto's/App%20Home.png)



