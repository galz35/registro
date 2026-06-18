# Reglas IA Del Proyecto Asistencia

Este proyecto usa las reglas de referencia clonadas en:

- `/opt/apps/asistencia/configcode`

Antes de modificar codigo o documentacion, leer:

- `/opt/apps/asistencia/configcode/rules/GOLDEN_RULES.md`
- `/opt/apps/asistencia/registro/documentacion/09_diagnostico_integral_reinicio_implementacion.txt`
- `/opt/apps/asistencia/registro/documentacion/10_plan_maestro_implementacion_perfecta.txt`
- `/opt/apps/asistencia/registro/documentacion/11_diccionario_excel_y_reglas_migracion.txt`
- `/opt/apps/asistencia/registro/documentacion/12_reglas_ia_configcode_aplicadas.txt`

## Reglas Obligatorias

1. No usar ORMs ni query builders.
   - Prohibido: TypeORM, Prisma, Sequelize, Drizzle, Knex, Entity Framework.
   - Backend Node/NestJS debe usar `mssql` con queries parametrizadas.

2. SQL Server usa `OUTPUT INSERTED`, no `RETURNING`.
   - `RETURNING` aplica a PostgreSQL.
   - En este proyecto, cualquier insert/update que necesite devolver datos debe usar `OUTPUT INSERTED.<campo>`.

3. No concatenar input del usuario en SQL.
   - Usar siempre `.input()` de `mssql`.
   - Para nombres dinamicos de columnas/tablas, usar whitelist.

4. No hardcodear secretos.
   - `DB_PASSWORD`, `JWT_SECRET`, `SSO_SECRET`, credenciales HCM y puertos van en `.env`.
   - No guardar `.env` real en git.

5. SSO Portal es parte base de la arquitectura.
   - Validar firma, expiracion y `type`/`token_type = SSO_PORTAL`.
   - Emitir JWT interno de asistencia despues del intercambio.

6. La importacion Excel debe ser validada antes de aplicar.
   - No escribir tablas finales si hay errores bloqueantes.
   - Registrar batches y errores.

7. El despacho debe ser transaccional.
   - Usar SQL Server transaction.
   - Bloquear stock con `UPDLOCK, ROWLOCK`.
   - No permitir stock negativo ni doble entrega activa por hijo/evento.

8. Imagenes en disco, no Base64 en SQL Server.
   - Convertir imagenes a WebP.
   - Guardar solo ruta y metadata.

9. Implementacion completa.
   - No dejar placeholders tipo `TODO: implementar`, `...rest of code`, o stubs que aparenten funcionar.

10. Validar antes de cerrar una tarea.
    - Backend: build, lint y pruebas relevantes.
    - React: typecheck/build.
    - Flutter: analyze/test cuando el SDK este disponible.

## Guias De Referencia Aplicables

- NestJS: `/opt/apps/asistencia/configcode/docs/nestjs/index.md`
- SQL Server: `/opt/apps/asistencia/configcode/docs/sql-server/index.md`
- SQL Server avanzado: `/opt/apps/asistencia/configcode/docs/sql-server/advanced.md`
- Seguridad: `/opt/apps/asistencia/configcode/docs/security/index.md`
- React: `/opt/apps/asistencia/configcode/docs/react/index.md`
- Flutter: `/opt/apps/asistencia/configcode/docs/flutter/index.md`
- Testing: `/opt/apps/asistencia/configcode/docs/testing/index.md`
- DevOps: `/opt/apps/asistencia/configcode/docs/devops/index.md`
- UX: `/opt/apps/asistencia/configcode/docs/ux-design/index.md`

