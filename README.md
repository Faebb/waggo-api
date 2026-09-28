# waggo-api

Backend de **Waggo — Plataforma Inteligente para Paseo Seguro de Perros**.
.NET 10 · ASP.NET Core Minimal APIs · Clean Architecture · PostgreSQL + PostGIS · Docker · TDD.

> App móvil: [Faebb/waggo-mobile](https://github.com/Faebb/waggo-mobile)

## Requisitos
- .NET SDK 10
- Docker Desktop (para `docker compose` y para las pruebas de integración con Testcontainers)

## Inicio rápido
```bash
cp .env.example .env
docker compose up -d --build
curl http://localhost:8080/health
curl "http://localhost:8080/api/v1/pricing/quote?walkType=Individual&durationMinutes=60"
```
Sin Docker para la API (solo la BD en contenedor):
```bash
docker compose up -d db
dotnet run --project Waggo.Api      # http://localhost:5080
```
OpenAPI: `/openapi/v1.json`.

## Arquitectura
```
Waggo.sln
├─ Layers/ (solution folder)
│  ├─ Waggo.Domain          → entidades, value objects, reglas (sin dependencias)
│  ├─ Waggo.Application     → casos de uso (queries/commands + handlers), puertos
│  ├─ Waggo.Infrastructure  → EF Core + Npgsql, adaptadores externos
│  └─ Waggo.Api             → endpoints, DI, configuración
└─ Tests/                        (carpeta física + carpeta de solución)
   ├─ Waggo.Domain.UnitTests
   ├─ Waggo.Application.UnitTests
   ├─ Waggo.Api.IntegrationTests   (requiere Docker)
   └─ Waggo.ArchitectureTests      (reglas de dependencia entre capas)
```
Cada capa es un proyecto en la raíz del repositorio (agrupadas en la carpeta de solución `Layers`); todos los proyectos de pruebas viven en la carpeta `Tests/`.
Dentro de cada capa el código se agrupa **por módulo** (`Pricing`, `Identity`, `Walks`, `Tracking`, `Payments`...) para poder extraer módulos a microservicios más adelante (RNF-009).

Regla de dependencia: `Api → Infrastructure → Application → Domain`. La verifica `Waggo.ArchitectureTests`.

## Flujo TDD
1. 🔴 Escribe la prueba que falla (aceptación en `IntegrationTests`, luego unitarias en `Domain`/`Application`).
2. 🟢 Escribe el mínimo código para pasarla.
3. 🔵 Refactoriza con las pruebas en verde.

```bash
dotnet test                                              # todo
dotnet test --filter "FullyQualifiedName!~IntegrationTests"   # loop rápido (sin Docker)
dotnet watch test --project Tests/Waggo.Domain.UnitTests
```

Commits: [Conventional Commits](https://www.conventionalcommits.org/) — `test(pricing): …` → `feat(pricing): …` → `refactor(pricing): …`.

## Slice de ejemplo: cotización de tarifa (RF-019 / RF-018)
`GET /api/v1/pricing/quote?walkType=Individual&durationMinutes=60`
```json
{ "walkType": "Individual", "durationMinutes": 60, "currency": "COP",
  "total": 23000, "commission": 4600, "walkerPayout": 18400 }
```
Las tarifas y la comisión son **provisionales** y se configuran en `appsettings.json › Pricing`.

## Migraciones (EF Core)
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Nombre> -p Waggo.Infrastructure -s Waggo.Api -o Persistence/Migrations
```
