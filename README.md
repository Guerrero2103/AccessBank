# BankApp Project - AccessBank

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



