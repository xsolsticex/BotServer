# Lanzamientos semanales

```bash
dotnet run --project Checks/Releases/ReleaseChecks.csproj
node --experimental-vm-modules Checks/Releases/overlay.test.cjs
```

Las 22 comprobaciones usan respuestas HTTP simuladas; no necesitan credenciales
ni consultan IGDB. Cubren la semana de Madrid y sus cambios de horario, los
límites inclusivo/exclusivo, una tarjeta por juego, fechas por plataforma,
precisión de fecha, portadas, caché, errores, paginación y renovación del token.
Se verifica el campo actual `date_format` sin `category`, así como los formatos
de día, mes y precisión desconocida y la actualización de semanas vacías.
Las seis pruebas JavaScript adicionales comprueban renderizado, rotación,
errores, semana vacía y limpieza al cerrar o restaurar la página.

Para comprobar la integración real, configura `CLIENT_ID` y `CLIENT_SECRET` de
una aplicación Twitch, permite `id.twitch.tv`, `api.igdb.com` e `images.igdb.com`
y abre `/releases` con la aplicación en marcha. `/api/releases/week` debe devolver
la semana actual y los juegos de IGDB; una lista vacía es un resultado válido.
Un HTTP 503 indica un error de configuración o del proveedor, no una semana vacía.
