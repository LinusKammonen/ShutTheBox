# Shut the Box

Ett multiplayer-tärningsspel i webbläsaren med egen backend i C#. Spelare loggar in, skapar eller går med i rum och spelar Shut the Box i realtid mot varandra, eller ensamma i singleplayer. Andra kan titta på pågående spel som åskådare.

Frontenden byggdes först mot ett färdigt kurs-API. I det här projektet har jag ersatt det med en egen backend: spelmotor, REST-API och realtidsuppdateringar med SignalR.

## Tekniker

- **Backend:** C# / .NET 10, ASP.NET Core Web API, SignalR, JWT-autentisering
- **Spelmotor:** fristående klassbibliotek utan webbberoenden
- **Tester:** xUnit
- **Frontend:** HTML, CSS och JavaScript (ES-moduler), SignalR-klient

## Projektstruktur

```
ShutTheBox_GameEngine/        Spelmotorn: regler, tärningar, giltiga drag, turordning, poäng, rum
ShutTheBox_GameEngine.Tests/  Enhetstester för spelmotorn
ShutTheBox_Api/               ASP.NET Core Web API + SignalR-hubb
  Controllers/                REST-endpoints under /v1
  Hubs/                       SignalR-hubb på /hubs/game
  wwwroot/                    Frontenden
```

## Regler

- Varje spelare har en egen låda med brickorna 1–9.
- Slå tärningarna och stäng brickor vars summa är lika med kastet.
- När 7, 8 och 9 är stängda slår man bara en tärning.
- Spelaren fortsätter tills alla brickor är stängda eller inga giltiga drag finns.
- Poängen är summan av de öppna brickorna. Lägst poäng vinner.

## Kom igång

Kräver [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project ShutTheBox_Api --launch-profile http
```

Öppna sedan http://localhost:5162 och logga in med valfritt användarnamn. Öppna ett inkognitofönster med ett annat namn för att testa multiplayer.

Köra testerna:

```bash
dotnet test
```

## API

| Metod | Endpoint | Beskrivning |
|---|---|---|
| POST | `/v1/auth/login` | Logga in med användarnamn, returnerar JWT |
| GET | `/v1/auth/me` | Inloggad användare |
| POST | `/v1/rooms` | Skapa rum (`maxPlayers: 1` startar singleplayer direkt) |
| GET | `/v1/rooms/open` | Öppna rum |
| POST | `/v1/rooms/{kod}/join` · `/leave` | Gå med i / lämna rum |
| POST | `/v1/games/from-room/{kod}/start` | Starta spelet (endast värden) |
| GET | `/v1/games/active` · `/current` | Pågående spel / mitt spel |
| GET | `/v1/games/{kod}/state` | Spelets tillstånd |
| POST | `/v1/games/{kod}/roll` · `/move` · `/leave` | Slå, stäng brickor, lämna |
| GET | `/v1/games/{kod}` | Slutresultat |

SignalR-händelser: `LobbyUpdated`, `RoomUpdated`, `GameUpdated`, `GameFinished`.

## Begränsningar

- All data ligger i minnet och försvinner när servern startas om.
- Inloggningen sker med enbart användarnamn, utan lösenord.
- JWT-nyckeln i `appsettings.json` är endast för utveckling.
