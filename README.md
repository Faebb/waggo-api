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
- En Development no se necesita token: un usuario de desarrollo con los roles `owner`, `walker` y `admin` firma cada request. Para probar otro rol envía el header `X-Dev-Roles: walker`.
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

## Migraciones (EF Core)
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Nombre> -p Waggo.Infrastructure -s Waggo.Api -o Persistence/Migrations
```
