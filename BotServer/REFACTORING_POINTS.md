# Puntos de Refactorización y Mejora Arquitectónica - BotServer

Este documento recopila las recomendaciones de refactorización y buenas prácticas de ingeniería identificadas tras la revisión y optimización de la arquitectura del proyecto .NET **BotServer**.

---

### 1. Desacoplar `TwitchBotApi.cs` (Principio de Responsabilidad Única - SRP) - **[APLICADO ✅]**
* **Solución aplicada:** Se creó el servicio especializado `TwitchAuthService` para aislar toda la lógica de autenticación y OAuth, delegando desde `TwitchBotApi`.

### 2. Evitar la Creación Manual de Ámbitos (`IServiceScopeFactory`) - **[APLICADO ✅]**
* **Solución aplicada:** Se estandarizó la inyección de `IServiceScopeFactory` a través del contenedor de Inyección de Dependencias en los servicios singleton (`TwitchBotApi`, `TwitchAuthService`) para resolver servicios scoped de base de datos de forma segura sin causar dependencias cautivas.

### 3. Reemplazar `Console.WriteLine` por `ILogger<T>` - **[APLICADO ✅]**
* **Solución aplicada:** Se introdujo `ILogger` estructurado en componentes clave como `BotSignalRClient` y `TwitchBot`.

### 4. Robustecer el Manejo de Nulos en Spotify (`SpotyClient.cs`) - **[APLICADO ✅]**
* **Solución aplicada:** Se añadió una guarda de nulabilidad (`if (first == null) return;`) en `SpotyClient.GetSong()` para prevenir excepciones de tipo `NullReferenceException` en tiempo de ejecución.

### 5. Paralelización de Operaciones Asíncronas y Eliminación de Deadlocks - **[APLICADO ✅]**
* **Solución aplicada:** Se optimizaron los manejadores de eventos (`BotEventHandler`, `TwitchBotApi`) utilizando `Task.WhenAll` para peticiones independientes y se reemplazó el uso síncrono de `.Result` por `await`.

### 6. Reestructuración de Directorios y Cohesión Arquitectónica - **[APLICADO ✅]**
* **Solución aplicada:** Se reubicaron los clientes de Spotify en `API/Spotify/`, los servicios en segundo plano (`EventSubHostedService`) en `HostedServices/`, y las interfaces globales en `Interfaces/`, mejorando significativamente la organización del proyecto.
