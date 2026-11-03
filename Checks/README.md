# Comprobaciones del proyecto

Desde la raíz del repositorio:

```bash
dotnet build Checks/Badges/BadgeChecks.csproj
dotnet build Checks/Handlers/HandlerChecks.csproj
dotnet build Checks/Markov/MarkovChecks.csproj
dotnet build Checks/Releases/ReleaseChecks.csproj
dotnet run --project Checks/Badges/BadgeChecks.csproj --no-build
dotnet run --project Checks/Handlers/HandlerChecks.csproj --no-build
dotnet run --project Checks/Markov/MarkovChecks.csproj --no-build
dotnet run --project Checks/Releases/ReleaseChecks.csproj --no-build
node --experimental-vm-modules Checks/Overlays/overlays.test.cjs
node --experimental-vm-modules Checks/Releases/overlay.test.cjs
```

Son programas de comprobación que terminan con error si falla una aserción;
`dotnet test` no los ejecuta. Entre los seis realizan 72 comprobaciones.
Usan SQLite temporal y servicios simulados, sin conectar a Twitch o Spotify ni
modificar la base del servidor. Cada carpeta contiene sus instrucciones.

La solución conserva la referencia al proyecto privado `Tests/Tests.csproj`,
ignorado por Git. Para compilar toda la solución ese proyecto debe estar presente
localmente. Los comandos anteriores permiten ejecutar las comprobaciones incluso
cuando no está disponible en el entorno en la nube. Las comprobaciones JavaScript
se ejecutan con Node.js por separado.
