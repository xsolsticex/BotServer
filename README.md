# 🤖 BotServer

Servidor backend para bots, construido con **.NET 10 / ASP.NET Core**, con persistencia en **SQLite** y listo para desplegarse con **Docker**.

---

## 📋 Tabla de contenidos

- [Características](#-características)
- [Tecnologías](#-tecnologías)
- [Requisitos previos](#-requisitos-previos)
- [Estructura del proyecto](#-estructura-del-proyecto)
- [Instalación y ejecución local](#-instalación-y-ejecución-local)
- [Ejecución con Docker](#-ejecución-con-docker)
- [Configuración](#-configuración)
- [Uso](#-uso)
- [Despliegue](#-despliegue)
- [Contribuir](#-contribuir)
- [Licencia](#-licencia)

---

## ✨ Características

- API/servidor web basado en ASP.NET Core
- Almacenamiento local con SQLite
- Imagen Docker con build multi-etapa (SDK para compilar, runtime ligero para ejecutar)
- TODO: describe aquí las funcionalidades del bot (comandos, integraciones, webhooks, etc.)

## 🛠 Tecnologías

| Componente | Tecnología |
|---|---|
| Lenguaje / Framework | C# · .NET 10 · ASP.NET Core |
| Base de datos | SQLite |
| Contenedores | Docker |
| Solución | `BotServer.slnx` |

## ✅ Requisitos previos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) (para ejecutar en local)
- [Docker](https://www.docker.com/) (opcional, para ejecutar en contenedor)
- Git

## 📁 Estructura del proyecto

```
BotServer/
├── BotServer/          # Proyecto principal (código fuente)
├── BotServer.slnx      # Archivo de solución
├── Dockerfile          # Imagen Docker multi-etapa
├── .gitattributes
└── .gitignore
```

## 🚀 Instalación y ejecución local

```bash
# 1. Clonar el repositorio
git clone https://github.com/xsolsticex/BotServer.git
cd BotServer

# 2. Restaurar dependencias
cd BotServer
dotnet restore

# 3. Ejecutar
dotnet run
```

## 🐳 Ejecución con Docker

El `Dockerfile` compila el proyecto con la imagen del SDK de .NET 10 y ejecuta el resultado sobre la imagen `aspnet:10.0`, con `sqlite3` instalado. El servidor escucha en el puerto **10000**.

```bash
# Construir la imagen
docker build -t botserver .

# Ejecutar el contenedor
docker run -d --name botserver -p 10000:10000 botserver
```

Si quieres que la base de datos SQLite persista entre reinicios, monta un volumen:

```bash
docker run -d --name botserver \
  -p 10000:10000 \
  -v botserver-data:/app \
  botserver
```

> TODO: ajusta la ruta del volumen a la carpeta donde tu aplicación guarda el archivo `.db`.

## ⚙️ Configuración

| Variable | Descripción | Valor por defecto |
|---|---|---|
| `ASPNETCORE_URLS` | URL y puerto en los que escucha el servidor | `http://+:10000` |
| TODO | Token del bot / API key | — |
| TODO | Cadena de conexión SQLite | — |

Puedes configurar la aplicación mediante `appsettings.json`, variables de entorno o *user secrets*.

> ⚠️ **Nunca** subas tokens, claves o credenciales al repositorio.

## 💡 Uso

Una vez en marcha, el servidor está disponible en:

```
http://localhost:10000
```

TODO: documenta aquí los endpoints principales, por ejemplo:

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/` | Estado del servidor |
| `POST` | `/webhook` | Recepción de eventos del bot |

## ☁️ Despliegue

El proyecto está preparado para plataformas que ejecutan contenedores Docker (Render, Railway, Fly.io, un VPS, etc.). Solo necesitas:

1. Apuntar la plataforma a este repositorio (usará el `Dockerfile`).
2. Exponer el puerto `10000`.
3. Definir las variables de entorno necesarias.

## 🤝 Contribuir

Las contribuciones son bienvenidas:

1. Haz un fork del proyecto
2. Crea una rama: `git checkout -b feature/mi-mejora`
3. Haz commit de tus cambios: `git commit -m "Añade mi mejora"`
4. Sube la rama: `git push origin feature/mi-mejora`
5. Abre un Pull Request

## 📄 Licencia

TODO: indica la licencia del proyecto (por ejemplo, MIT) y añade un archivo `LICENSE`.

---

Hecho con ❤️ por [@xsolsticex](https://github.com/xsolsticex)
