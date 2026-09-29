<!-- Título del PR = commit en main (squash and merge). Formato Conventional Commits: feat(<modulo>): <resumen> (RF-XXX) -->

## ¿Qué cambia?
<!-- Descripción breve -->

## Requerimiento
- RF / historia: <!-- ej. RF-019 · WAG-12 -->

## Evidencia TDD
- [ ] La prueba se escribió antes que el código (commit `test(...)` antes de `feat(...)`)
- Pruebas agregadas/modificadas:
  -

## Checklist (Definition of Done)
- [ ] `dotnet test` pasa en local
- [ ] Sin warnings nuevos
- [ ] Tests de arquitectura en verde
- [ ] Si cambia el contrato de la API: PR enlazado en `waggo-mobile`
