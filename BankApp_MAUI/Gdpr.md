# GDPR-documentatie — BankApp MAUI

## 1. Welke persoonsgegevens verwerkt de applicatie?

De applicatie verwerkt de volgende persoonsgegevens van gebruikers:

| Gegeven | Doel |
|---|---|
| E-mailadres | Authenticatie (inloggen), identificatie van de gebruiker, communicatie |
| Voornaam en achternaam | Identificatie van de gebruiker, beveiliging |
| Adres | Verzenden van documenten per post |
| Telefoonnummer | Contactname met de gebruiker |
| IBAN / rekeningnummer (automatisch gegenereerd door het systeem) | Uniek technisch identificatiemiddel om rekeningen en transacties correct toe te wijzen. Dit is geen door de gebruiker zelf aangeleverd gegeven, maar wordt door de applicatie zelf gegenereerd bij aanmaak van een rekening. |

Elk van deze gegevens wordt enkel verzameld voor het hierboven vermelde, specifieke doel (doelbindingsprincipe), en niet zonder duidelijke reden.

## 2. Bewaartermijn en soft-delete

De applicatie gebruikt een **soft-delete**-mechanisme: wanneer een gebruiker, kaart, rekening of transactie wordt "verwijderd", wordt het record niet onmiddellijk fysiek uit de databank gewist. In plaats daarvan wordt een `Deleted`-veld ingesteld op het tijdstip van verwijdering, waardoor het record niet langer zichtbaar is in de applicatie (via een automatisch query-filter), maar technisch nog aanwezig blijft.

**Waarom niet onmiddellijk volledig verwijderen?**
Financiële gegevens (met name transactiegeschiedenis) kunnen onderhevig zijn aan een wettelijke bewaarplicht, bijvoorbeeld in het kader van:
- **Antiwitwaswetgeving (AML)** — banken en financiële instellingen zijn doorgaans verplicht om transactiegegevens een bepaalde periode te bewaren, ook indien een klant om verwijdering vraagt, met het oog op mogelijk fraude- of witwasonderzoek.
- **Boekhoudkundige/fiscale bewaarplicht** — financiële transacties moeten vaak gedurende een wettelijk bepaalde termijn traceerbaar blijven voor controledoeleinden.

Om deze reden kan data niet zomaar onmiddellijk en onherroepelijk gewist worden op het moment dat een gebruiker daarom vraagt: dit zou in strijd zijn met deze wettelijke verplichtingen. Soft-delete biedt hiervoor een tussenoplossing: de gegevens verdwijnen onmiddellijk uit het zicht van de gebruiker en van de dagelijkse werking van de applicatie, maar blijven beschikbaar zolang de wettelijke bewaartermijn dit vereist.

## 3. Wat gebeurt er na de bewaartermijn?

Soft-delete is in deze applicatie uitdrukkelijk een **tussenstap, geen eindpunt**. Na het verstrijken van de relevante wettelijke bewaartermijn dienen soft-deleted records **volledig en definitief verwijderd** te worden uit de databank, zodat het recht op vergetelheid (art. 17 AVG/GDPR) ook effectief gerealiseerd wordt.

*(Opmerking: dit proces van definitieve verwijdering na afloop van de bewaartermijn is in deze applicatie momenteel niet geautomatiseerd geïmplementeerd — het betreft hier de beoogde/verwachte architectuur en aanpak. In een productieomgeving zou dit typisch gebeuren via een periodiek achtergrondproces dat records controleert op ouderdom en, waar de bewaartermijn verstreken is, deze definitief verwijdert.)*

## 4. Waar worden de gegevens opgeslagen?

- **Lokaal (MAUI-app):** een selectie van gegevens wordt lokaal opgeslagen via SQLite, zodat de applicatie ook offline bruikbaar is (bv. eigen rekeninggegevens, transactiehistoriek).
- **Online (server):** de volledige, actuele gegevens worden centraal bewaard in de databank die via de Web API wordt aangesproken. De lokale SQLite-data wordt periodiek gesynchroniseerd met deze centrale bron.

## 5. Toestemming en authenticatie

Bij registratie geeft de gebruiker actief zijn gegevens op via een registratieformulier. Toegang tot de applicatie en de bijhorende gegevens is beveiligd via een account (e-mailadres + wachtwoord), met JWT-authenticatie voor communicatie tussen de MAUI-app en de server.

---

*Dit document is opgesteld als onderdeel van de projectdocumentatie voor het vak .NET Advanced (MAUI-project), met als doel aan te tonen op welke wijze de applicatie rekening houdt met de principes van de AVG/GDPR.*