# Gex

Beyond all Reason game extractor

### note

internally, gex uses the same language as the engine. a team is a single army, a side of a fight is an ally team, a unit includes buildings as well

## desc

Gex is a c# webserver that fetches public Beyond All Reason games, downloads them, replays them locally, and uses a widget to generate game stats which are saved to a DB

## building

1. setup a postgres version 13 or higher
1. copy `secrets.template.json` to `secrets.json` and fill out the fields
1. copy `env.template.json` to `env.json` and fill out the fields
1. `dotnet build`
1. `npm install`
1. `npm run build`
1. `dotnet run`
1. view at https://localhost:6001/

NOTE: type `.close` into the console window to properly close the game. This will kill any BAR games running as well, and properly cancel everything

