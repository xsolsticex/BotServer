# Comprobaciones de overlays

Desde la raíz del repositorio, con Node.js 24:

```bash
node --experimental-vm-modules JsChecks/overlays.test.cjs
```

Ejecuta ocho pruebas con SignalR, DOM, red y temporizadores simulados, sin
dependencias npm ni acceso a Twitch. Comprueba reintentos de conexión y de Join,
recuperación del grupo, cierre y restauración de la página, eventos del contador,
límites del chat, respuestas HTTP obsoletas y alertas consecutivas.

La opción de módulos VM puede emitir una advertencia experimental de Node.js.
