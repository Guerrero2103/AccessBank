# Testscenario: rekening-eigenaarschap bij overschrijvingen

Dit scenario controleert dat een gebruiker geen overschrijving meer kan doen
vanaf de IBAN van een andere gebruiker (fix in `PostTransactie`,
`BankApp_Web/API_Controllers/TransactiesController.cs`).

## Voorbereiding

Start de API lokaal:

```bash
cd BankApp_Web
dotnet run
```

Wacht tot je deze regel ziet:

```
Now listening on: http://localhost:5000
```

Laat deze terminal open staan en voer de commando's hieronder uit in een
**tweede** terminal.

**Testgebruikers (uit README):**

| Gebruiker | E-mail | Wachtwoord | Eigen IBAN |
|---|---|---|---|
| Jan (klant) | jan.peeters@example.com | Password123! | BE12345678901234 |
| Sarah (medewerker) | sarah.janssens@example.com | Password123! | BE11223344556677 |

## Stap 1 — Inloggen als Jan

```bash
curl -s -X POST http://localhost:5000/api/account/login \
  -H "Content-Type: application/json" \
  -d '{"email":"jan.peeters@example.com","password":"Password123!"}'
```

Kopieer de waarde van `"token"` uit de response — dat is de toegangssleutel
die je in de volgende stappen gebruikt. Vervang `PLAK_HIER_JOUW_TOKEN`
hieronder telkens door die waarde.

## Stap 2 — Negatieve test: geld halen van andermans IBAN

```bash
curl -i -X POST http://localhost:5000/api/Transacties \
  -H "Authorization: Bearer PLAK_HIER_JOUW_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"vanIban":"BE11223344556677","naarIban":"BE12345678901234","bedrag":100,"omschrijving":"test misbruik"}'
```

`vanIban` is hier Sarah's rekening, niet die van Jan.

**Verwacht resultaat:** `HTTP/1.1 403 Forbidden`.

## Stap 3 — Controleren dat er niks stiekem is gebeurd

```bash
curl -s http://localhost:5000/api/Transacties -H "Authorization: Bearer PLAK_HIER_JOUW_TOKEN"
```

**Verwacht resultaat:** de lijst bevat geen transactie met
`"Bedrag":100` en `"test misbruik"`.

## Stap 4 — Positieve controletest: normale overschrijving moet nog werken

```bash
curl -i -X POST http://localhost:5000/api/Transacties \
  -H "Authorization: Bearer PLAK_HIER_JOUW_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"vanIban":"BE12345678901234","naarIban":"BE11223344556677","bedrag":25,"omschrijving":"test normaal"}'
```

`vanIban` is nu wel Jan's eigen rekening.

**Verwacht resultaat:** `HTTP/1.1 201 Created`.

## Samenvatting

| Stap | Test | Verwacht resultaat |
|---|---|---|
| 2 | Overschrijving vanaf andermans IBAN | `403 Forbidden` |
| 3 | Geen stiekeme transactie aangemaakt | Geen nieuwe regel in de historie |
| 4 | Overschrijving vanaf eigen IBAN | `201 Created` |

Klaar? Stop de server met `Ctrl+C` in de eerste terminal.

## Postman-alternatief

Maak een collection met dezelfde 3 requests (login, POST Transacties met
vreemde IBAN, GET Transacties). Sla het token na de login-request op als
collection-variabele via een "Tests"-script:

```js
pm.collectionVariables.set("token", pm.response.json().token);
```

en gebruik `{{token}}` in de Authorization-header (type "Bearer Token")
van de andere requests.
