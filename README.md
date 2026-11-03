# BotServer

BotServer es un backend para bots y overlays de Twitch, desarrollado con .NET 10 y ASP.NET Core. El proyecto combina integración con Twitch, Spotify, SignalR y SQLite para ofrecer una base sólida para servicios de chat, autenticación OAuth, dashboards, overlays y automatizaciones durante transmisiones en vivo.

## Características

- Servidor web con ASP.NET Core
- Integración con Twitch mediante TwitchLib
- Soporte para comandos, eventos y automatizaciones del bot
- Frases de Markov por canal, enviadas automáticamente solo durante el directo
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
│   │   ├── Chat/             # Mensajes, perfiles y modelo de Markov
│   │   └── Releases/         # Lanzamientos de hoy y mañana
│   ├── Integrations/
│   │   ├── Igdb/             # Consulta de lanzamientos y autenticación de aplicación
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
├── Tests/                    # Proyecto local ignorado: Tests.csproj
├── Checks/
│   ├── Badges/               # Comprobaciones de SQLite e insignias
│   ├── Handlers/             # Comprobaciones de eventos y comandos
│   ├── Markov/               # Comprobaciones del modelo y temporizadores
│   ├── Releases/             # Comprobaciones de IGDB y del calendario
│   └── Overlays/             # Comprobaciones JavaScript
├── BotServer.slnx            # Aplicación, Tests privado y comprobaciones .NET
├── Dockerfile               # Imagen Docker
├── .gitignore
├── .gitattributes
└── README.md
```

## Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Git
- Credenciales de una aplicación de Twitch para ejecutar el bot
- Node.js 24 para las comprobaciones JavaScript
- Docker, si utilizas contenedores
- Credenciales de Spotify (si vas a usar la integración)

## Instalación

1. Clona el repositorio:

```bash
git clone https://github.com/xsolsticex/BotServer.git
cd BotServer
```

2. Restaura y compila la aplicación desde la raíz del repositorio:

```bash
dotnet restore BotServer/BotServer.csproj
dotnet build BotServer/BotServer.csproj --no-restore
```

3. Configura las variables y la ruta de SQLite descritas abajo. Para arrancar
con una dirección explícita, sin los perfiles de lanzamiento:

```bash
dotnet run --project BotServer/BotServer.csproj --no-launch-profile --urls http://localhost:10000
```

En Visual Studio, abre `BotServer.slnx` y selecciona `BotServer` como proyecto de
inicio. La solución conserva `Tests/Tests.csproj`, un proyecto privado ignorado
por Git. Debe estar presente en tu copia para compilar toda la solución; si no lo
tienes, compila la aplicación con los comandos anteriores.

## Configuración

Las credenciales de las integraciones se leen de variables de entorno. La
configuración de ASP.NET Core y Markov también admite `appsettings.json`.
Configura los secretos en tu entorno o plataforma de despliegue.

| Variable | Descripción |
|---|---|
| `MAIN_CHANNEL` | Canal principal del bot; por defecto, `xhipibotx` |
| `CLIENT_ID` | Identificador de la aplicación de Twitch |
| `CLIENT_SECRET` | Secreto de la aplicación de Twitch |
| `REDIRECT_URL` | Redirección OAuth en modo remoto; debe coincidir con la registrada en Twitch |
| `HOOK_SECRET` | Clave de verificación de webhooks EventSub; necesaria para arrancar |
| `SPOTIFY_ID` | Identificador de la aplicación de Spotify |
| `SPOTIFY_SECRET` | Secreto de la aplicación de Spotify |
| `ASPNETCORE_URLS` | Direcciones de escucha; Docker configura `http://+:10000` |
| `Markov__IntervalSeconds` | Intervalo de frases: 300 segundos por defecto, entre 1 y 86400 |

### Entorno y base de datos

`Configuration/AppEnvironment.cs` define el modo en código; actualmente es
`remote`. No se selecciona mediante `ASPNETCORE_ENVIRONMENT`:

| Modo | Base de datos | SignalR interno del bot |
|---|---|---|
| `remote` | `/data/twitch.db` | `https://botserver-qccm.onrender.com/chatHub` |
| `local` | `twitch.db` junto al ejecutable | `http://localhost:8000/chatHub` |

En modo remoto, `/data` debe existir y permitir escritura. Para desarrollo con el
bot conectado al hub local, cambia `Mode` a `local` y usa el puerto 8000 en el
comando de arranque. Las URLs de OAuth y Spotify también contienen valores fijos
en sus servicios; cambiar el puerto de escucha no los actualiza.

SQLite solo crea el archivo cuando falta. Al arrancar se aplican las migraciones
pendientes sobre la base existente, sin borrarla ni recrearla. Están en
`Data/Migrations` y conservan sus identificadores tras la reorganización.
La identidad de las insignias es `(ChannelId, SetId)`, lo que permite compartir
una misma URL entre varios canales.

## Ejecución con Docker

El proyecto incluye un `Dockerfile` para compilar y ejecutar la aplicación en un contenedor.

### Construir la imagen

```bash
docker build -t botserver .
```

### Ejecutar el contenedor

```bash
docker run -d --name botserver -p 10000:10000 \
  --mount source=botserver-data,target=/data \
  --env-file /ruta/a/variables-del-bot.env \
  botserver
```

Sustituye la ruta del archivo de variables por tu archivo local de configuración.
El volumen conserva SQLite al reemplazar el contenedor. La imagen escucha en el
puerto `10000`; el cliente SignalR interno sigue la URL del modo seleccionado.

## Endpoints principales

La aplicación expone varios endpoints para autenticación, overlays y datos del bot.

### OAuth de Twitch

- `/connect`
- `/confirm`

Estos endpoints permiten iniciar y completar el flujo OAuth de Twitch para autorizar al bot o a usuarios.

### Overlays

- `/releases` — tarjetas de los lanzamientos de hoy y mañana, sin depender del canal
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

El hub está en `/chatHub`. Los overlays usan el módulo compartido
`wwwroot/scripts/overlay-connection.js`, con reintentos progresivos, recuperación
del grupo del canal y cierre de la conexión al abandonar la página. Los recursos
web se solicitan al mismo servidor que sirve el overlay.

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
dotnet run --project Checks/Markov/MarkovChecks.csproj
```

### Lanzamientos de hoy y mañana

Añade `/releases` como fuente de navegador de OBS (ancho recomendado: 960 px,
alto: 520 px). Su fondo es transparente y las tarjetas rotan cada 15 segundos.
El backend consulta [IGDB](https://api-docs.igdb.com/) desde
`/api/releases`, usando `CLIENT_ID` y `CLIENT_SECRET` de tu aplicación Twitch;
el HTML nunca recibe el secreto ni el token.

El intervalo comprende hoy y mañana completos en `Europe/Madrid`, desde las
00:00 de hoy hasta las 00:00 de pasado mañana (límite excluido), incluidos los
cambios de horario. No es una ventana móvil de 48 horas desde la consulta.
Se consultan lanzamientos con día confirmado, sin filtro regional.
Se muestra una tarjeta por identificador de juego de IGDB, agrupando únicamente
las plataformas que reciben ese juego hoy o mañana. Si las fechas varían,
se muestran junto a cada plataforma. Las descripciones se muestran en el idioma
que devuelve IGDB.

Los resultados se almacenan en memoria durante tres horas; las listas vacías se
actualizan cada diez minutos. La precisión del día se obtiene de `date_format`,
sin filtrar por el campo obsoleto `category`. El overlay comprueba
actualizaciones cada 15 minutos y reintenta errores al minuto. Si falla una
actualización, puede mostrar datos del mismo intervalo señalados como pendientes
de actualización. A medianoche se invalida el calendario anterior, aunque su
caché no haya caducado, y el overlay solicita el nuevo calendario.
Los estados de error y de intervalo sin lanzamientos se muestran por separado.
El JSON utiliza `periodStart` y `periodEnd`; la ruta antigua `/api/releases/week`
también devuelve este nuevo intervalo, no una semana completa.

Las tarjetas no muestran precios. Se excluyen los juegos etiquetados en IGDB
como `free-to-play`, `f2p` o `free2play`, sin peticiones adicionales a la API.
Como ese etiquetado puede estar incompleto, no se garantiza que todos los juegos
restantes sean de pago. Las portadas proceden de `images.igdb.com`, con atribución a IGDB.
Permite las conexiones HTTPS a `id.twitch.tv`, `api.igdb.com` y
`images.igdb.com` en los entornos con red restringida. Consulta también los
[requisitos de uso de IGDB](https://api-docs.igdb.com/#about).

Con el servidor escuchando en el puerto 10000, usa estas URLs como fuentes de
navegador en OBS, sustituyendo `tuusuario` por el canal:

```text
http://localhost:10000/chat/tuusuario
http://localhost:10000/counter/tuusuario
http://localhost:10000/status/tuusuario
http://localhost:10000/notifier/tuusuario
```

La ruta `/` no tiene una página de inicio configurada. Usa las rutas de overlay.

### Comprobaciones

Los programas de `Checks/` están separados de tu proyecto privado `Tests/` y
realizan 84 comprobaciones locales sin conectarse a Twitch, IGDB ni Spotify:

```bash
dotnet run --project Checks/Badges/BadgeChecks.csproj
dotnet run --project Checks/Handlers/HandlerChecks.csproj
dotnet run --project Checks/Markov/MarkovChecks.csproj
dotnet run --project Checks/Releases/ReleaseChecks.csproj
node --experimental-vm-modules Checks/Overlays/overlays.test.cjs
node --experimental-vm-modules Checks/Releases/overlay.test.cjs
```

Son programas ejecutables con aserciones: `dotnet test` no los ejecuta.
Consulta [Checks/README.md](Checks/README.md) para más detalles.

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
