# Comprobaciones de handlers

Desde la raíz del repositorio:

```bash
dotnet run --project Checks/Handlers/HandlerChecks.csproj
```

Las 13 comprobaciones usan SQLite temporal, el contenedor real de dependencias y
un cliente SignalR de prueba. No arrancan el bot ni acceden a Twitch.

Verifican resolución de dependencias, aislamiento por evento, insignias globales
y personalizadas, perfiles almacenados, exclusión de comandos, entrenamiento de
Markov, permisos del contador, normalización de colores y mensajes concurrentes.

Responsabilidades:

- `BotEventHandler`: adapta los callbacks de Twitch y crea un ámbito por evento.
- `BotCommandHandler`: procesa comandos y sus permisos.
- `ChatMessageHandler`: coordina el aprendizaje y el envío del mensaje al overlay.
- `ChatBadgeService`: resuelve insignias personalizadas y globales.
- `ChatProfileService`: consulta perfiles y registra usuarios.
- `ChannelEventHandler`: gestiona conexión, registro de EventSub y suscripciones.

Los handlers son scoped y el adaptador es singleton. Los servicios de un mismo
ámbito consultan SQLite secuencialmente para no compartir un DbContext entre
operaciones simultáneas. Los callbacks que requieren la API remota de Twitch
no se ejecutan en estas comprobaciones.
