# Harjoitustyö – viestintäsovelluksen backend

ASP.NET Core Web API (.NET 10), jolla käyttäjät voivat lähettää toisilleen julkisia ja yksityisiä viestejä. Tiedot tallennetaan Entity Framework Corella SQLite-tietokantaan. Frontendiä ei ole: rajapintaa käytetään HTTP-pyynnöillä (Swagger UI, `.http`-tiedosto, Postman, curl).

## Käynnistys

```bash
cd Harjoitustyö
dotnet run --launch-profile http
```

- API: `http://localhost:5226/api/...`
- Swagger UI (kehitystilassa): `http://localhost:5226/swagger`
- Tietokanta `messageservice.db` luodaan automaattisesti ensimmäisellä käynnistyskerralla (`EnsureCreated`). Yhteysmerkkijono on tiedostossa `appsettings.json` (`ConnectionStrings:MessageServiceDB`).

## Toiminnallisuus

- Käyttäjä voi rekisteröityä, katsoa ja muokata omaa profiiliaan, vaihtaa salasanansa ja poistaa tilinsä.
- Käyttäjä voi lähettää viestin. Jos viestillä on vastaanottaja, se on **yksityinen** ja näkyy vain lähettäjälle ja vastaanottajalle. Ilman vastaanottajaa viesti on **julkinen** ja näkyy kaikille.
- Viesti voi olla vastaus aiempaan viestiin (`previousMessageId`), jolloin viesteistä muodostuu ketju. Viestin vastaukset saa haettua omalla reitillään.
- Käyttäjä voi muokata ja poistaa vain omia viestejään. Muokatulle viestille tallennetaan muokkausaika.
- Salasanaa ei tallenneta sellaisenaan: tietokantaan tallennetaan PBKDF2-tiiviste (SHA-256, 100 000 kierrosta) ja käyttäjäkohtainen satunnainen suola.
- Viimeisin kirjautumisaika päivittyy jokaisella onnistuneella tunnistautumisella.

### Lisäominaisuudet tehtävänannon lisäksi

- Saapuneet- ja lähetetyt-kansiot (`/inbox`, `/sent`).
- Viestin vastausten haku (`/{id}/replies`).
- Syötteiden validointi (tunnuksen muoto, salasanan vähimmäispituus 8, kenttien maksimipituudet). Virheellinen pyyntö palauttaa 400 ja kuvauksen virheestä.
- Toisen käyttäjän yksityisviestin olemassaoloa ei paljasteta: siihen kohdistuva pyyntö palauttaa 404, ei 403.
- Viestin poistaminen ei poista siihen tulleita vastauksia, vaan vastausten linkki edelliseen viestiin katkaistaan (`previousMessageId = null`).
- Käyttäjän poistaminen poistaa kaikki viestit, joissa hän on lähettäjä tai vastaanottaja (yksityisviesti ei saa muuttua julkiseksi).

## Tietomallit

**User**

| Kenttä | Tyyppi | Kuvaus |
|---|---|---|
| Id | long | pääavain |
| UserName | string | tunnus, uniikki |
| Password | byte[] | salasanan tiiviste |
| Salt | byte[] | suola |
| FirstName, LastName | string | etu- ja sukunimi |
| JoinDate | DateTime? | liittymisaika |
| LastLogin | DateTime? | viimeinen loginaika |

**Message**

| Kenttä | Tyyppi | Kuvaus |
|---|---|---|
| Id | long | pääavain |
| Title | string | otsikko |
| Body | string | viesti |
| SenderId | long | vierasavain → User (lähettäjä) |
| RecipientId | long? | vierasavain → User (vastaanottaja); `null` = julkinen viesti |
| PreviousMessageId | long? | vierasavain → Message (edellinen viesti ketjussa) |
| CreatedAt | DateTime | luontiaika |
| EditedAt | DateTime? | viimeisin muokkaus |

Rajapinnan yli liikkuvat vain DTO-oliot (`UserDTO`, `RegisterUserDTO`, `UpdateUserDTO`, `MessageDTO`, `CreateMessageDTO`, `UpdateMessageDTO`). Näin esimerkiksi salasanatiiviste ja suola eivät koskaan päädy vastaukseen, ja asiakas ei voi itse asettaa kenttiä kuten lähettäjää tai id:tä.

## Rakenne

```
Harjoitustyö/
├── Controllers/     HTTP-rajapinta: reitit, statuskoodit, oikeustarkistukset käyttäjäprofiilille
├── Middleware/      BasicAuthenticationHandler: käyttäjän tunnistus jokaisesta pyynnöstä
├── Services/        Sovelluslogiikka: näkyvyys- ja omistajuussäännöt, DTO-muunnokset, salasanojen käsittely
├── Repositories/    Tietokantahaut Entity Frameworkilla
├── Data/            MessageServiceContext (DbContext): taulut ja niiden väliset suhteet
├── Models/          Entiteetit (User, Message) ja DTO:t
└── Program.cs       Palveluiden rekisteröinti (dependency injection) ja pyyntöputken määrittely
```

Kerrokset on erotettu, jotta kullakin osalla on yksi vastuu:

- **Controller** tietää vain HTTP:stä. Se lukee pyynnön, kutsuu palvelua ja muuntaa tuloksen statuskoodiksi. Se ei tiedä tietokannasta mitään.
- **Service** sisältää säännöt: kuka saa nähdä minkäkin viestin, kuka saa muokata, onko vastaanottaja olemassa. Palvelu ei tiedä HTTP:stä; se palauttaa `ServiceResult`-olion, jossa on tila (Success / NotFound / Forbidden / BadRequest / Failed).
- **Repository** tietää vain tietokannasta. Esimerkiksi `Include(Sender).Include(Recipient)` on repositoriossa, jotta palvelun DTO-muunnoksessa lähettäjä ja vastaanottaja eivät ole null.
- Kerrokset riippuvat toisistaan rajapintojen (`IMessageService`, `IMessageRepository` jne.) kautta, ja toteutukset injektoidaan `Program.cs`:ssä. Toteutuksen voi vaihtaa (esim. SQL Serveriin tai testejä varten) koskematta muihin kerroksiin.

## Tunnistautuminen

API käyttää **HTTP Basic -autentikointia**. Jokaiseen pyyntöön (paitsi rekisteröitymiseen) lisätään otsake

```
Authorization: Basic base64(tunnus:salasana)
```

`BasicAuthenticationHandler` purkaa otsakkeen ja kutsuu `UserAuthenticationService.Authenticate`-metodia. Se hakee käyttäjän, laskee annetusta salasanasta tiivisteen käyttäjän suolalla ja vertaa sitä tallennettuun tiivisteeseen vakioaikaisella vertailulla. Onnistuneen tunnistuksen jälkeen käyttäjän tunnus asetetaan pyynnön `User`-olioon (claim `Name`), josta kontrollerit ja palvelut lukevat, kuka pyynnön teki. Kontrollereissa on `[Authorize]`, joten tunnistamaton pyyntö saa vastauksen **401**.

Koska tunnus ja salasana kulkevat jokaisessa pyynnössä, tuotannossa yhteyden pitää olla HTTPS (`UseHttpsRedirection` on päällä, ja `https`-profiili kuuntelee osoitteessa `https://localhost:7053`).

## Rajapinnat

### Käyttäjät – `/api/users`

| Metodi | Reitti | Kirjautuminen | Kuvaus | Vastaus |
|---|---|---|---|---|
| POST | `/api/users` | ei | Luo käyttäjätunnus | 201 + UserDTO, 400 jos tunnus varattu tai syöte virheellinen |
| GET | `/api/users` | kyllä | Kaikki käyttäjät | 200 |
| GET | `/api/users/me` | kyllä | Omat tiedot | 200 |
| GET | `/api/users/{tunnus}` | kyllä | Yhden käyttäjän tiedot | 200 / 404 |
| PUT | `/api/users/{tunnus}` | kyllä, vain itse | Muokkaa nimeä ja/tai salasanaa | 200 / 403 |
| DELETE | `/api/users/{tunnus}` | kyllä, vain itse | Poista tili ja sen viestit | 204 / 403 |

### Viestit – `/api/messages`

| Metodi | Reitti | Kuvaus | Vastaus |
|---|---|---|---|
| GET | `/api/messages?count=20` | Uusimmat julkiset viestit (1–100 kpl) | 200 |
| GET | `/api/messages/inbox` | Minulle lähetetyt yksityisviestit | 200 |
| GET | `/api/messages/sent` | Kaikki lähettämäni viestit | 200 |
| GET | `/api/messages/{id}` | Yksi viesti, jos saan nähdä sen | 200 / 404 |
| GET | `/api/messages/{id}/replies` | Viestiin vastanneet viestit, jotka saan nähdä | 200 / 404 |
| POST | `/api/messages` | Lähetä viesti | 201 / 400 |
| PUT | `/api/messages/{id}` | Muokkaa omaa viestiä | 200 / 403 / 404 |
| DELETE | `/api/messages/{id}` | Poista oma viesti | 204 / 403 / 404 |

Kaikki viestireitit vaativat kirjautumisen.

## Käyttö

### 1. Käyttäjätunnuksen luominen

Asiakas lähettää rekisteröitymispyynnön ilman tunnistautumista:

```http
POST /api/users
Content-Type: application/json

{
  "userName": "matti",
  "password": "salasana1",
  "firstName": "Matti",
  "lastName": "Meikäläinen"
}
```

Vastaus on `201 Created` ja käyttäjän tiedot ilman salasanaa:

```json
{ "userName": "matti", "firstName": "Matti", "lastName": "Meikäläinen",
  "joinDate": "2026-11-01T13:44:33", "lastLogin": "2026-11-01T13:44:33" }
```

### 2. Muiden toimintojen käyttäminen tunnuksella

Tämän jälkeen jokaiseen pyyntöön lisätään Basic-otsake. Esimerkiksi curlilla `-u tunnus:salasana` tekee sen automaattisesti:

```bash
curl -u matti:salasana1 http://localhost:5226/api/messages
```

Swagger UI:ssa painetaan **Authorize**, annetaan tunnus ja salasana, ja sen jälkeen kaikki kokeiltavat pyynnöt lähtevät tunnistettuina. Visual Studiossa voi käyttää valmista tiedostoa `Harjoitustyö/Harjoitustyö.http`.

Julkinen viesti:

```json
POST /api/messages
{ "title": "Hei kaikki", "body": "Tämä on julkinen viesti" }
```

Yksityisviesti, joka on samalla vastaus viestiin 1:

```json
POST /api/messages
{ "title": "Re: Hei", "body": "Vain Liisalle", "recipient": "liisa", "previousMessageId": 1 }
```

## Esimerkki: mitä tapahtuu, kun käyttäjä lähettää viestin

Matti lähettää Liisalle yksityisviestin vastauksena viestiin 1:

```
POST /api/messages
Authorization: Basic bWF0dGk6c2FsYXNhbmEx
{ "title": "Re: Hei", "body": "Vain Liisalle", "recipient": "liisa", "previousMessageId": 1 }
```

1. **Middleware – `BasicAuthenticationHandler`.** ASP.NET Core ajaa autentikoinnin ennen kontrolleria. Handler purkaa base64-otsakkeen muotoon `matti:salasana1` ja kutsuu `UserAuthenticationService.Authenticate("matti", "salasana1")`. Palvelu hakee `UserRepository`n kautta käyttäjän, laskee salasanasta tiivisteen Matin suolalla ja vertaa sitä tietokannassa olevaan. Tiivisteet täsmäävät, joten `LastLogin` päivitetään ja pyyntöön liitetään identiteetti `Name = "matti"`. Jos salasana olisi väärä, käsittely päättyisi tähän vastaukseen 401.
2. **Mallin sidonta ja validointi.** `[ApiController]` muuntaa JSON-rungon `CreateMessageDTO`-olioksi ja tarkistaa data-annotaatiot (otsikko ja runko pakollisia, maksimipituudet). Virheellinen syöte palautettaisiin automaattisesti vastauksena 400.
3. **Controller – `MessagesController.PostMessage`.** Kontrolleri lukee lähettäjän tunnuksen kirjautuneesta käyttäjästä (`User.Identity.Name`), ei pyynnön rungosta, joten kukaan ei voi lähettää viestiä toisen nimissä. Se kutsuu `MessageService.NewMessageAsync(dto, "matti")`.
4. **Service – `MessageService.NewMessageAsync`.**
   - Hakee lähettäjän `UserRepository`stä ja asettaa `SenderId`:n.
   - Koska `recipient` on annettu, hakee käyttäjän `liisa`. Jos häntä ei löytyisi, palautetaan `BadRequest` ("Vastaanottajaa ei löydy"). Muuten asetetaan `RecipientId`, ja viestistä tulee yksityinen.
   - Koska `previousMessageId` on annettu, hakee viestin 1 ja tarkistaa, että Matti saa nähdä sen. Toisten yksityisviesteihin ei voi vastata.
   - Luo `Message`-entiteetin, asettaa `CreatedAt`-ajan ja antaa sen repositoriolle.
5. **Repository – `MessageRepository.AddMessageAsync`.** Lisää entiteetin `DbContext`iin ja kutsuu `SaveChangesAsync`. EF Core muodostaa `INSERT`-lauseen SQLiteen, ja tietokanta antaa viestille id:n. Vierasavainrajoitteet varmistavat, että viittaukset käyttäjiin ja edelliseen viestiin ovat olemassa.
6. **Paluu.** Palvelu hakee tallennetun viestin uudelleen `Include`-hauilla, jotta lähettäjän ja vastaanottajan tunnukset saadaan mukaan, ja muuntaa sen `MessageDTO`:ksi. Kontrolleri palauttaa `201 Created`, `Location`-otsakkeen `/api/messages/{id}` ja DTO:n:

```json
{ "id": 3, "title": "Re: Hei", "body": "Vain Liisalle", "sender": "matti",
  "recipient": "liisa", "previousMessageId": 1, "isPrivate": true,
  "createdAt": "2026-11-01T13:44:34", "editedAt": null }
```

Kun Liisa myöhemmin hakee `GET /api/messages/inbox`, viesti näkyy hänelle. Jos kolmas käyttäjä yrittää hakea `GET /api/messages/3`, `MessageService.CanView` palauttaa false, ja vastaus on 404.

## Käytetyt tekniikat

- ASP.NET Core 10 Web API, kontrollerit
- Entity Framework Core 10 + SQLite
- Repository- ja service-kerrokset, dependency injection
- Oma `AuthenticationHandler` (HTTP Basic), `[Authorize]`
- PBKDF2-salasanatiivisteet suolalla (`Rfc2898DeriveBytes.Pbkdf2`)
- Swagger / OpenAPI (Swashbuckle)
