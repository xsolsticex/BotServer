# BotServer

BotServer es un backend para bots y overlays de Twitch, desarrollado con .NET 10 y ASP.NET Core. El proyecto combina integración con Twitch, Spotify, SignalR y SQLite para ofrecer una base sólida para servicios de chat, autenticación OAuth, dashboards, overlays y automatizaciones durante transmisiones en vivo.

## Características

- Servidor web con ASP.NET Core
- Integración con Twitch mediante TwitchLib
- Soporte para comandos, eventos y automatizaciones del bot
- Autenticación OAuth para usuarios y canales de Twitch
- Integración con Spotify para autenticación y consumo de datos
- Comunicación en tiempo real con SignalR
- Persistencia de datos con SQLite
- Endpoints para overlays y paneles web
- Preparado para ejecutarse con Docker

## Tecnologías

| Componente | Tecnología |
|---|---|
| Lenguaje | C# |
| Framework | .NET 10 / ASP.NET Core |
| Base de datos | SQLite |
| Comunicación en tiempo real | SignalR |
| Integraciones | Twitch API, Spotify API |
| Contenedores | Docker |
| Solución | `BotServer.slnx` |

## Estructura del proyecto

```text
BotServer/
├── BotServer/                # Proyecto principal
│   ├── API/                  # Lógica de API y servicios relacionados
│   ├── Commands/             # Comandos del bot
│   ├── Database/             # Contexto EF Core, servicios y migraciones
│   ├── Endpoints/            # Endpoints HTTP del servidor
│   ├── Extensions/           # Extensiones para configuración
│   ├── Hubs/                 # Hub de SignalR
│   ├── Interfaces/           # Interfaces del proyecto
│   ├── Migrations/           # Migraciones de base de datos
│   ├── Properties/          # Configuración del proyecto
│   ├── SpotifyClientApi/     # Cliente y utilidades de Spotify
│   ├── TwitchBotClient/     # Cliente y handlers del bot de Twitch
│   ├── wwwroot/              # Archivos estáticos y overlays
│   ├── Program.cs            # Punto de entrada de la aplicación
│   ├── appsettings.json     # Configuración principal
│   ├── appsettings.Development.json
│   ├── BotServer.csproj     # Proyecto .NET
│   └── libman.json          # Gestión de librerías cliente
├── BotServer.slnx           # Solución
├── Dockerfile               # Imagen Docker
├── .gitignore
├── .gitattributes
└── README.md
```

## Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)
- Git
- Cuenta de Twitch
- Credenciales de Spotify (si vas a usar la integración)

## Instalación

1. Clona el repositorio:

```bash
git clone https://github.com/xsolsticex/BotServer.git
cd BotServer
```

2. Restaura dependencias:

```bash
cd BotServer
dotnet restore
```

3. Ejecuta la aplicación:

```bash
dotnet run
```

## Configuración

El proyecto usa `appsettings.json` y variables de entorno para su configuración. Algunos valores importantes que se usan en código incluyen:

| Variable | Descripción |
|---|---|
| `MAIN_CHANNEL` | Canal principal de Twitch del bot |
| `CLIENT_ID` | Client ID de Twitch para autenticación OAuth |
| `REDIRECT_URL` | URL de redirección usada en OAuth |
| `ASPNETCORE_URLS` | Dirección y puerto del servidor |
| `ConnectionStrings` o configuración equivalente | Base de datos y credenciales del entorno |

## Ejecución con Docker

El proyecto incluye un `Dockerfile` para compilar y ejecutar la aplicación en un contenedor.

### Construir la imagen

```bash
docker build -t botserver .
```

### Ejecutar el contenedor

```bash
docker run -d --name botserver -p 10000:10000 botserver
```

La aplicación está diseñada para escuchar en el puerto `10000` por defecto.

## Endpoints principales

La aplicación expone varios endpoints para autenticación, overlays y datos del bot.

### OAuth de Twitch

- `/connect`
- `/confirm`

Estos endpoints permiten iniciar y completar el flujo OAuth de Twitch para autorizar al bot o a usuarios.

### Overlays

- `/notifier/{username}`
- `/status/{username}`
- `/chat/{username}`
- `/counter/{username}`

Estos endpoints devuelven páginas HTML para overlays y widgets visuales.

### Datos de canal

- `/data/{username}`

Devuelve datos del canal, como seguidores y suscriptores.

### Perfil

- `/profile/{username}`

Devuelve la información del perfil del usuario.

### Spotify

- `/auth/spotify/{username}`

Endpoint para completar la autenticación con Spotify para un usuario concreto.

## Integraciones

### Twitch
El proyecto incluye soporte para:

- Conexión del bot a canales de Twitch
- Eventos de chat
- Mensajes y comandos
- Registro de seguidores/suscriptores
- Integración con SignalR para notificaciones en vivo

### Spotify
Se integra con la API de Spotify para autenticación y uso de datos del usuario.

### SignalR
Permite enviar eventos y mensajes en tiempo real desde el backend hacia clientes web o overlays.

## Uso

Una vez levantado el servidor, puedes abrir la aplicación en:

```text
http://localhost:10000
```

También puedes acceder a las rutas de overlay según la necesidad del proyecto, por ejemplo:

```text
http://localhost:10000/chat/tuusuario
http://localhost:10000/counter/tuusuario
http://localhost:10000/status/tuusuario
```

## Despliegue

Este backend está preparado para desplegarse sobre plataformas compatibles con contenedores Docker, como:

- Render
- Railway
- Fly.io
- VPS
- Docker Swarm
- Kubernetes

Requiere exponer el puerto `10000` y configurar correctamente las variables de entorno del entorno de producción.

## Contribuir

Las contribuciones son bienvenidas. Para colaborar:

1. Haz un fork del proyecto
2. Crea una rama nueva
3. Realiza tus cambios
4. Haz commit
5. Abre un Pull Request

```bash
git checkout -b feature/nueva-funcionalidad
git commit -am "Añade nueva funcionalidad"
git push origin feature/nueva-funcionalidad
```
