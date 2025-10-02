# Gex

Beyond all Reason game extractor

### note

internally, gex uses the same language as the engine. a team is a single army, a side of a fight is an ally team, a unit includes buildings as well

## desc

Gex is a c# webserver that fetches public Beyond All Reason games, downloads them, replays them locally, and uses a widget to generate game stats which are saved to a DB

## building

### preq software

1. a postgresql 13 (or higher) server running
1. 7zip on the PATH
1. npm
1. dotnet 9

### build steps

1. copy `secrets.template.json` to `secrets.json` and fill out the fields
1. copy `env.template.json` to `env.json` and fill out the fields
1. `dotnet build`
1. `npm install`
1. `npm run build`
1. `dotnet run`
1. view at https://localhost:6001/

NOTE: use ^C or type `.close` into the console window to properly close the game. This will properly shutdown Gex, and cancel all running tasks

