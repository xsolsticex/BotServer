# Comprobaciones de insignias

Desde la raíz del repositorio:

```bash
dotnet run --project BadgeChecks/BadgeChecks.csproj
```

Las 11 comprobaciones usan una base SQLite temporal y no acceden a Twitch ni a
la base del servidor. Verifican la migración desde el esquema anterior,
preservación de filas, URLs compartidas, claves duplicadas, actualización
idempotente, búsqueda por canal y cargas simultáneas desde conexiones separadas.

La migración `20261103160000_CustomBadgeIdentity` sustituye el índice único global
de `BadgeUrl` por uno de `(ChannelId, SetId)`. Las claves antiguas se normalizan;
si varias filas del mismo canal compartían la clave, se conserva la más reciente
con esa clave y se mantienen las otras como `legacy:<Id>:<clave anterior>`.
Ninguna fila ni URL se elimina. La siguiente carga desde Twitch utiliza el
identificador real del set y actualiza su URL.

El arranque ejecuta las migraciones pendientes sobre el archivo existente; solo
crea el archivo si falta. No borra ni recrea la base de datos. Volver al índice
antiguo puede fallar si ya existen URLs compartidas, en lugar de eliminar datos.
