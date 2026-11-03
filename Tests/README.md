# Comprobaciones del proyecto

Desde la raíz del repositorio:

```bash
dotnet build BotServer.slnx
dotnet run --project Tests/Badges/BadgeChecks.csproj --no-build
dotnet run --project Tests/Handlers/HandlerChecks.csproj --no-build
dotnet run --project Tests/Markov/MarkovChecks.csproj --no-build
node --experimental-vm-modules Tests/Overlays/overlays.test.cjs
```

Son programas de comprobación que terminan con error si falla una aserción;
`dotnet test` no los ejecuta. Entre los cuatro realizan 48 comprobaciones.
Usan SQLite temporal y servicios simulados, sin conectar a Twitch o Spotify ni
modificar la base del servidor. Cada carpeta contiene sus instrucciones.

La solución incluye la aplicación y los tres proyectos .NET. Las comprobaciones
JavaScript se ejecutan con Node.js por separado.
