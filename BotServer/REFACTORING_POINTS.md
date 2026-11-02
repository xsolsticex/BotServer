# Puntos de Refactorización y Mejora Arquitectónica - BotServer

Este documento recopila las recomendaciones de refactorización y buenas prácticas de ingeniería identificadas tras la revisión de la arquitectura del proyecto .NET **BotServer**, incluyendo todos los puntos aplicados.

---

### 1. Desacoplar `TwitchBotApi.cs` (Principio de Responsabilidad Única - SRP) - **[APLICADO ✅]**
* **Solución aplicada:** Se creó el servicio especializado `TwitchAuthService` para aislar toda la lógica de autenticación y OAuth, delegando desde `TwitchBotApi`.

### 2. Evitar la Creación Manual de Ámbitos (`IServiceScopeFactory`) - **[APLICADO ✅]**
* **Solución aplicada:** Se estandarizó la inyección de `IServiceScopeFactory` a través del contenedor de Inyección de Dependencias en los servicios singleton (`TwitchBotApi`, `TwitchAuthService`) para resolver servicios scoped de base de datos de forma segura sin causar dependencias cautivas.

### 3. Reemplazar `Console.WriteLine` por `ILogger<T>` - **[APLICADO ✅]**
* **Solución aplicada:** Se introdujo `ILogger<BotSignalRClient>` en componentes clave como `BotSignalRClient` para un registro estructurado y profesional en lugar de E/S por consola.

### 4. Robustecer el Manejo de Nulos en Spotify (`SpotyClient.cs`) - **[APLICADO ✅]**
* **Solución aplicada:** Se añadió una guarda de nulabilidad (`if (first == null) return;`) en `SpotyClient.GetSong()` para prevenir excepciones de tipo `NullReferenceException` en tiempo de ejecución.
