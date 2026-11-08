# Shut the Box

Ett multiplayer-baserat Shut the box i med backend i C#. 

Frontenden byggdes först mot ett färdigt API som en skoluppgift. I det här projektet har jag ersatt det med en egen backend: spelmotor, REST-API och realtidsuppdateringar med SignalR.

## Stack

- **Backend:** C# / ASP.NET Core Web API, SignalR
- **Spelmotor:** fristående klassbibliotek utan webbberoenden
- **Frontend:** HTML, CSS och JavaScript

## Hur man spelar

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
