# Viestintäsovelluksen backend

ASP.NET Core Web API (.NET 10) + Entity Framework Core + SQLite. Käyttäjät voivat lähettää julkisia ja yksityisiä viestejä.

Käynnistys: `dotnet run --project Harjoitustyö`, Swagger UI osoitteessa `/swagger`. Tietokanta luodaan automaattisesti.

## Rakenne

```
Controllers/    HTTP-rajapinta: reitit ja statuskoodit
Services/       Sovelluslogiikka: kuka saa nähdä ja muokata mitäkin, DTO-muunnokset, salasanat
Repositories/   Tietokantahaut
Data/           DbContext: taulut ja niiden suhteet
Models/         Entiteetit (User, Message) ja DTO:t
Middleware/     Basic-autentikointi
```

Jokaisella kerroksella on yksi vastuu: kontrolleri ei tiedä tietokannasta, palvelu ei tiedä HTTP:stä ja repositorio ei sisällä sääntöjä. Kerrokset käyttävät toisiaan rajapintojen kautta, ja toteutukset injektoidaan `Program.cs`:ssä.

Salasanasta tallennetaan vain PBKDF2-tiiviste ja suola. Rajapinnan yli liikkuvat vain DTO:t, joten tiiviste ei koskaan päädy vastaukseen.

## Rajapinnat

| Metodi | Reitti | Kuvaus |
|---|---|---|
| POST | `/api/users` | Rekisteröityminen (ei vaadi kirjautumista) |
| GET | `/api/users`, `/api/users/me`, `/api/users/{tunnus}` | Käyttäjien tiedot |
| PUT / DELETE | `/api/users/{tunnus}` | Oman profiilin muokkaus / poisto |
| GET | `/api/messages` | Uusimmat julkiset viestit |
| GET | `/api/messages/inbox`, `/api/messages/sent` | Saapuneet / lähetetyt viestit |
| GET | `/api/messages/{id}`, `/api/messages/{id}/replies` | Viesti ja sen vastaukset |
| POST | `/api/messages` | Uusi viesti |
| PUT / DELETE | `/api/messages/{id}` | Oman viestin muokkaus / poisto |

Viesti on yksityinen, jos sillä on vastaanottaja; muuten se on julkinen. Yksityisen viestin näkevät vain lähettäjä ja vastaanottaja. Toisten viestejä ja profiileja ei voi muokata (403).

## Käyttö

1. **Tunnuksen luominen:**
   ```json
   POST /api/users
   { "userName": "matti", "password": "salasana1", "firstName": "Matti", "lastName": "M" }
   ```
2. **Muut toiminnot:** jokaiseen pyyntöön lisätään otsake `Authorization: Basic base64(tunnus:salasana)`. Swaggerissa tämä tehdään Authorize-painikkeella, curlissa valitsimella `-u matti:salasana1`.
   ```json
   POST /api/messages
   { "title": "Hei", "body": "Vain Liisalle", "recipient": "liisa", "previousMessageId": 1 }
   ```
   Ilman `recipient`-kenttää viesti on julkinen. `previousMessageId` kertoo, mihin viestiin tämä on vastaus.

## Esimerkki: viestin lähetys

1. **BasicAuthenticationHandler** purkaa Authorization-otsakkeen. `UserAuthenticationService` vertaa salasanan tiivistettä tietokannassa olevaan. Jos ne eivät täsmää, vastaus on 401.
2. **MessagesController** saa `CreateMessageDTO`:n. Lähettäjä otetaan kirjautuneesta käyttäjästä, ei pyynnöstä.
3. **MessageService** tarkistaa, että vastaanottaja on olemassa ja että edellinen viesti on lähettäjän nähtävissä. Sen jälkeen se luo `Message`-olion.
4. **MessageRepository** tallentaa viestin EF Corella tietokantaan.
5. Kontrolleri palauttaa `201 Created` ja `MessageDTO`:n.
