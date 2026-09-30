# waggo-api

Backend de **Waggo — Plataforma Inteligente para Paseo Seguro de Perros**.

Waggo conecta a dueños de perros con paseadores **verificados**: el dueño solicita un paseo, sigue el recorrido en vivo y paga solo cuando el servicio termina bien. Este repo es la API REST que usan la app móvil y la web: identidad y roles, mascotas, paseos, seguimiento GPS, mensajería, pagos y tarifas.

.NET 10 · ASP.NET Core Minimal APIs · Clean Architecture · PostgreSQL + PostGIS · Serilog · FluentValidation · Docker · TDD.

> App móvil: [Faebb/waggo-mobile](https://github.com/Faebb/waggo-mobile) · Cómo usar todo en conjunto: [Faebb/waggo-workspace](https://github.com/Faebb/waggo-workspace)

## Requisitos
- .NET SDK 10
- Docker Desktop (para `docker compose` y para las pruebas de integración con Testcontainers)

## Inicio rápido
```bash
cp .env.example .env
docker compose up -d --build           # API :8080 + PostGIS :5432
curl http://localhost:8080/health/ready
curl "http://localhost:8080/api/v1/pricing/quote?walkType=Individual&durationMinutes=60"
```
Sin Docker para la API (solo la BD en contenedor):
```bash
docker compose up -d db
dotnet run --project Waggo.Api      # http://localhost:5080
```
- OpenAPI (solo Development): `/openapi/v1.json`.
- En Development no se necesita token: un usuario de desarrollo con los roles `owner`, `walker` y `admin` firma cada request. Para probar otro rol envía el header `X-Dev-Roles: walker`; para actuar como otro usuario, `X-Dev-User-Id: otro-dueno`.
- En Development y en las pruebas, la API aplica las migraciones pendientes al arrancar. En otros ambientes se aplican como un paso del despliegue.
- Health checks: `/health/live` (el proceso responde) y `/health/ready` (PostgreSQL disponible).

## Comandos
| Comando | Qué hace |
|---|---|
| `dotnet build` | Compila; los warnings (incluidas las convenciones) son errores |
| `dotnet test --filter "FullyQualifiedName!~IntegrationTests"` | Loop TDD rápido, sin Docker |
| `dotnet test` | Todas las pruebas (las de integración necesitan Docker) |
| `dotnet format` | Aplica el formato y las convenciones de `.editorconfig` |

## Arquitectura
Cada capa es un proyecto en la raíz del repo; las pruebas están en `Tests/` y replican las carpetas del proyecto que prueban.

```
Waggo.sln
├─ Waggo.Domain          → reglas de negocio puras: entidades, value objects, enums, errores, excepciones
├─ Waggo.Application     → casos de uso (Commands/Queries + handlers + validadores de negocio), interfaces
├─ Waggo.Infrastructure  → EF Core + Npgsql, implementaciones de las interfaces, opciones
├─ Waggo.Api             → endpoints Minimal API, DTO y su validación, middleware, auth, respuestas
└─ Tests/
   ├─ Waggo.Domain.UnitTests
   ├─ Waggo.Application.UnitTests
   ├─ Waggo.Infrastructure.UnitTests (cifrado de columnas)
   ├─ Waggo.Api.UnitTests          (validadores de DTO, middleware, respuestas)
   ├─ Waggo.Api.IntegrationTests   (API completa + PostgreSQL real; requiere Docker)
   └─ Waggo.ArchitectureTests      (reglas de dependencia entre capas)
```
- Regla de dependencia: `Api → Infrastructure → Application → Domain`, verificada por `Waggo.ArchitectureTests`.
- Dentro de cada capa, carpetas **por tipo** (`Enums/`, `ValueObjects/`, `Exceptions/`...) con una subcarpeta por módulo (`Pricing/`). Detalle: vault `03 Desarrollo/Estructura de carpetas del backend.md` (ADR-012).

## Cómo se escribe el código
| Tema | Regla |
|---|---|
| Convenciones (ADR-008) | Nunca `var`; `_camelCase` y `s_camelCase` en campos; un tipo por archivo; código en inglés, mensajes al usuario en español. El build y el CI las hacen cumplir. |
| Respuestas (ADR-007) | Toda operación devuelve `WaggoResponse<T>` (pilas `Errors`, `Warnings`, `Infos`, `IsValid`), que se encadenan con `ConcatStacks`. Todo endpoint responde `WaggoApiResponse<T>`. |
| Logging | Serilog. Los métodos no escriben al log: quien llama decide con `response.WriteLogs(logger, "Operacion")`. |
| Validación (ADR-009) | FluentValidation en dos capas: el DTO en `Waggo.Api` y las reglas de negocio en `Waggo.Application`. |
| Errores (ADR-010) | Errores esperados en `WaggoResponse`; lo que impide continuar lanza una excepción custom que el middleware convierte en el HTTP correcto. El usuario nunca ve la excepción real. |
| Seguridad (ADR-011) | OAuth 2.0 con JWT; roles `owner`, `walker` y `admin`; cada endpoint declara su política. |

Formato de toda respuesta:
```json
{ "success": false, "data": null, "pagination": null,
  "errors": [{ "code": "Pricing.InvalidDuration", "message": "..." }],
  "warnings": [], "infos": [], "traceId": "..." }
```

## Flujo TDD
1. 🔴 Escribe la prueba que falla (aceptación en `IntegrationTests`, luego unitarias).
2. 🟢 Escribe el mínimo código para pasarla.
3. 🔵 Refactoriza con las pruebas en verde.

Commits: [Conventional Commits](https://www.conventionalcommits.org/) — `test(pricing): …` → `feat(pricing): …` → `refactor(pricing): …`. Los PR entran con *squash and merge* y su título se valida en CI.

## Trabajar con Claude Code
- `CLAUDE.md` tiene las reglas del repo: basta con clonar y abrir `claude` aquí para trabajar solo en el backend.
- Para el flujo completo (specs, agentes TDD, checklists, backend + mobile) abre Claude desde [waggo-workspace](https://github.com/Faebb/waggo-workspace).

## Slice de ejemplo: cotización de tarifa (RF-019 / RF-018)
`GET /api/v1/pricing/quote?walkType=Individual&durationMinutes=60` (rol `owner`)
```json
{ "success": true,
  "data": { "walkType": "Individual", "durationMinutes": 60, "currency": "COP",
            "total": 23000, "commission": 4600, "walkerPayout": 18400 },
  "pagination": null, "errors": [], "warnings": [], "infos": [], "traceId": "..." }
```
Las tarifas y la comisión son **provisionales** y se configuran en `appsettings.json › Pricing`.

## Mascotas (RF-004)
Endpoints del dueño (rol `owner`), siempre limitados a sus propios perros:
| Método | Ruta | Qué hace |
|---|---|---|
| `POST` | `/api/v1/pets` | Registra un perro: `name`, `size` (`Small`, `Medium`, `Large`) y opcionales `breed`, `birthDate`, `weightKg`, `medicalNotes` |
| `GET` | `/api/v1/pets` | Lista mis perros ordenados por nombre (máximo 10) |
| `GET` | `/api/v1/pets/{id}` | Detalle de uno de mis perros; `404 Pets.NotFound` si no existe o es de otro dueño |

## Paseos (RF-007, lado del dueño)
Como pedir un Uber: el dueño elige de 1 a 3 de sus perros, tipo, duración, punto de recogida y hora (ahora o hasta 14 días), y el precio queda congelado con la tarifa vigente.
| Método | Ruta | Qué hace |
|---|---|---|
| `POST` | `/api/v1/walks` | Pide un paseo: `petIds`, `walkType`, `durationMinutes`, `pickupAddress`, `latitude`, `longitude` y opcionales `scheduledFor` (vacío = ahora) y `notes` |
| `GET` | `/api/v1/walks` | Mis paseos, el más reciente primero |
| `GET` | `/api/v1/walks/{id}` | Detalle (la app lo consulta para saber cuándo lo acepta un paseador) |
| `POST` | `/api/v1/walks/{id}/cancel` | Cancela un paseo `Requested` o `Accepted`; si no, `422 Walks.CannotCancel` |

Lado del paseador (rol `walker`), como un conductor que ve solicitudes:
| Método | Ruta | Qué hace |
|---|---|---|
| `GET` | `/api/v1/walks/available?latitude=&longitude=` | Solicitudes abiertas de otros usuarios con lo que gana el paseador; con ubicación, las más cercanas primero y `distanceKm` |
| `POST` | `/api/v1/walks/{id}/accept` | Acepta; el primero gana. `409 Walks.NotAvailable` si ya la tomó otro, `422 Walks.OwnWalk` si es su propio paseo |
| `GET` | `/api/v1/walks/assigned` | Paseos que aceptó |

Con ubicación, `available` usa **PostGIS** (RF-006): `ST_DWithin` sobre el índice GiST devuelve solo las solicitudes a menos de 5 km, ordenadas por `ST_Distance`.

`GET /api/v1/walks/{id}` responde al dueño y al paseador asignado. Dos aceptaciones simultáneas no pueden ganar ambas: `walks` usa `xmin` de PostgreSQL como token de concurrencia optimista.

## Paseo en vivo (RF-008, RF-011)
| Método | Ruta | Qué hace |
|---|---|---|
| `POST` | `/api/v1/walks/{id}/start` | El paseador asignado inicia el paseo (`Accepted → InProgress`) |
| `POST` | `/api/v1/walks/{id}/track` | Lote de 1–100 posiciones `{ latitude, longitude, recordedAt }` mientras está en curso |
| `GET` | `/api/v1/walks/{id}/track` | Ruta ordenada con `distanceKm` y `elapsedMinutes` (dueño o paseador) |
| `POST` | `/api/v1/walks/{id}/finish` | El paseador termina el paseo (`InProgress → Completed`) |

**Emergencia (RF-012)**: `POST /api/v1/walks/{id}/emergency` (dueño o paseador asignado; `message`, `latitude` y `longitude` opcionales) mientras el paseo está aceptado o en curso, y `GET /api/v1/walks/{id}/alerts` para ver las alertas, la más reciente primero. Se guardan en `tracking.walk_alerts`.

**Alertas automáticas (RF-009, RF-010)**: al recibir cada lote de posiciones, la API crea alertas sin `raisedBy` cuando el paseo sale de un radio de 1,5 km alrededor de la recogida (`Geofence`, al cruzar el borde) o cuando el paseador lleva 10 minutos a menos de 30 m (`Anomaly`, una vez por detención). Se ven en el mismo `GET /api/v1/walks/{id}/alerts`. Los umbrales son provisionales (`WalkMonitor`).

**Chat (RF-013)**: `POST /api/v1/walks/{id}/messages` `{ text }` y `GET /api/v1/walks/{id}/messages?after={messageId}` para el dueño y el paseador asignado. Se escribe mientras el paseo está aceptado o en curso (`422 Messages.ChatClosed` después) y se lee siempre. Tabla `messaging.walk_messages`.

Las posiciones se guardan en `tracking.track_points` (GiST en la posición, BRIN en el tiempo). Por ahora la app consulta la ruta cada pocos segundos; SignalR (RNF-007) llegará en otro slice.

Estados: `Requested → Accepted → InProgress → Completed`, o `Cancelled`. El punto de recogida se guarda como `geography(Point, 4326)` de PostGIS con índice GiST, para el matching por cercanía (RF-006).

## Base de datos y datos sensibles
- EF Core + Npgsql con nombres `snake_case` y un schema por módulo (`pets`, `walks`, …). Las posiciones usan PostGIS a través de NetTopologySuite; el dominio solo conoce su `GeoPoint`.
- Las columnas sensibles (hoy `pets.medical_notes`) se guardan **cifradas con AES-256-GCM** (RNF-003). La llave es `Encryption:Key`: 32 bytes aleatorios en base64 (`openssl rand -base64 32`). En Development viene en `appsettings.Development.json`; en cualquier otro ambiente se configura con un secreto o con la variable `Encryption__Key`, y la API no arranca sin ella.

## Migraciones (EF Core)
`dotnet-ef` está en el manifiesto local de herramientas (`dotnet-tools.json`):
```bash
dotnet tool restore
dotnet ef migrations add <Nombre> -p Waggo.Infrastructure -s Waggo.Api -o Persistence/Migrations
```
Las migraciones son código generado: `.editorconfig` las excluye de las reglas de estilo.

## Licencia
[MIT](LICENSE)
