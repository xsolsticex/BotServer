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
│   ├── Configuration/        # Configuración del entorno
│   ├── Data/                 # Contexto EF Core, modelos, DTO, servicios y migraciones
│   ├── Features/
│   │   ├── Badges/           # Resolución de insignias del chat
│   │   └── Chat/             # Mensajes, perfiles y modelo de Markov
│   ├── Integrations/
│   │   ├── Spotify/          # Cliente y tokens de Spotify
│   │   └── Twitch/           # API, Auth, Chat/Commands y EventSub/Conditions
│   ├── Web/
│   │   ├── Endpoints/        # Rutas HTTP
│   │   ├── Hubs/             # Hub de SignalR
│   │   └── SignalR/          # Cliente interno del bot para los overlays
│   ├── Extensions/           # Registro de servicios
│   ├── Properties/           # Recursos y configuración de lanzamiento
│   ├── wwwroot/              # Archivos estáticos y overlays
│   ├── Program.cs            # Punto de entrada de la aplicación
│   ├── appsettings.json     # Configuración principal
│   ├── appsettings.Development.json
│   ├── BotServer.csproj     # Proyecto .NET
│   └── libman.json          # Gestión de librerías cliente
├── Tests/
│   ├── Badges/               # Comprobaciones de SQLite e insignias
│   ├── Handlers/             # Comprobaciones de eventos y comandos
│   ├── Markov/               # Comprobaciones del modelo y temporizadores
│   └── Overlays/             # Comprobaciones JavaScript
├── BotServer.slnx            # Aplicación y proyectos de comprobación .NET
├── Dockerfile               # Imagen Docker
├── .gitignore
├── .gitattributes
└── README.md
```

## Requisitos previos

Las instrucciones de compilación y las 48 comprobaciones locales están en
[Tests/README.md](Tests/README.md). Los namespaces siguen la nueva estructura.
Las migraciones conservan sus identificadores y operaciones de SQLite; las
futuras migraciones deben guardarse en `BotServer/Data/Migrations`.

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

### Frases de Markov

El bot aprende fragmentos aleatorios de hasta 30 palabras consecutivas de los
mensajes del chat. Una tarea periódica del bot publica automáticamente una frase
en cada canal conectado que tenga datos y esté en directo, cada 5 minutos por defecto. No utiliza
un comando. Puedes cambiar el intervalo con `Markov:IntervalSeconds` en la
configuración o la variable de entorno `Markov__IntervalSeconds` (entre 1 y 86400
segundos). El primer envío se realiza después del primer intervalo; los canales
sin datos se omiten. Los comandos y los mensajes del propio bot no se usan como
entrenamiento. La tarea se cancela al detener el bot y omite los envíos mientras
está desconectado.
Antes de cada envío consulta el estado del directo en Twitch. Si el canal está
offline o la consulta falla, omite ese envío y vuelve a comprobarlo en el siguiente
intervalo. Esta consulta usa las credenciales de aplicación de Twitch existentes.

Los modelos se mantienen únicamente en memoria, separados por canal, y se vacían
cada hora desde el arranque del servicio (también si no hay actividad). Reiniciar
el servidor también los elimina. Se admiten hasta 64 canales y 4000 transiciones
por canal; al alcanzar el límite se deja de aprender hasta el siguiente reinicio
horario. Las respuestas tienen un máximo de 30 palabras y 400 caracteres.

Para comprobar el modelo, sus límites y el reinicio horario sin conectar a Twitch:

```bash
dotnet run --project Tests/Markov/MarkovChecks.csproj
```

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
