# Asistencia y Despacho de Juguetes - Dia del Nino
# Contexto del proyecto para agentes IA

## Stack
- Backend: NestJS (mssql directo, sin ORM)
- Frontend: React + Vite + Tailwind CSS
- Movil: Flutter (pendiente)
- BD: SQL Server
- Infra: Linux VPS, Nginx, PM2

## Reglas base
- NO usar ORM (TypeORM, Prisma, etc.)
- Usar mssql directo con queries parametrizadas
- OUTPUT INSERTED en SQL Server (no RETURNING)
- Secretos en .env, no hardcodeados
- Imagenes en disco WebP, no Base64 en BD
- Stock protegido con UPDLOCK, ROWLOCK
- Colores: Rojo Claro #DA291C, Blanco, Gris, Negro. PROHIBIDO usar azul
- Despacho solo para colaboradores con DepartamentoGeografico = 'MANAGUA'
  (validado en dispatch.service.ts antes de ejecutar el SP)

## Estructura del proyecto
/opt/apps/asistencia/registro/
  database/       - Scripts SQL versionados
  nest/           - Backend NestJS
  react/          - Frontend React
  flutter/        - App movil (pendiente)
  documentacion/  - Docs tecnicos
  Controllers/    - Legado ASP.NET MVC
  Views/          - Vistas legado
  *.xlsx          - Censo y catalogo fuentes

## Endpoints API (prefijo: /api)
- POST /auth/sso-login
- POST /auth/dev-login (solo desarrollo)
- GET  /auth/me
- GET  /attendance/lookup/:carnet?eventoId=
- POST /attendance/register
- POST /attendance/revert
- GET  /attendance/event/:eventoId/summary
- GET  /attendance/censo
- GET  /attendance/search?q=   (busqueda por nombre)
- POST /dispatch/deliver       (multipart con foto OBLIGATORIA)
- GET  /dispatch/validate/:hijoId/:jugueteId?eventoId=
- POST /dispatch/:entregaId/revert
- PATCH /dispatch/:hijoId/foto?eventoId=   (actualizar foto evidencia)
- GET  /dispatch/event/:eventoId/summary
- GET  /catalog
- POST /catalog
- PUT  /catalog/:id
- PATCH /catalog/:id/deactivate
- POST /catalog/:id/photo
- GET  /catalog/summary (resumen inventario con entregados/reversados reales)
- POST /imports/censo/validate
- POST /imports/censo/apply
- POST /imports/catalogo/validate
- POST /imports/catalogo/apply
- GET  /imports/template/:tipo  (descargar plantilla .xlsx: censo o catalogo)
- GET  /imports/:id/errors
- GET  /reports/entregas.csv
- GET  /reports/pendientes.csv
- GET  /reports/inventario.csv
- GET  /reports/asistencia.xlsx?eventoId=
- GET  /reports/despacho.xlsx?eventoId=
- GET  /reports/inventario.xlsx (2 hojas: resumen + detalle entregas)
- GET  /health (sin auth)

## Frontend routes (React SPA bajo /asistencia/)
- /                    → Dashboard (CommandCenter)
- /attendance          → Registro de asistencia
- /dispatch            → Despacho de juguetes
- /catalog             → Catálogo de juguetes
- /import              → Importar Excel (censo y catalogo)
- /history             → Historial de movimientos
- /reports             → Reportes (asistencia, despacho, inventario)
- /admin               → Administración de usuarios/roles

## Roles
- admin: acceso total
- supervisor: despacho + reversiones
- despachador: solo asistencia y entrega
- consulta: solo lectura

## Reglas de inventario (100% exacto)
- StockActual solo se modifica via SP (UPDLOCK + ROWLOCK):
  - sp_Despacho_Entregar: decrementa
  - sp_Despacho_Reversar: incrementa
- vw_ResumenInventario cuenta Entregados contra registros reales de
  tblEntregasJuguetes (no contra StockInicial - StockActual)
- Catalogo siempre se recarga desde API tras cada delivery/revert
  (recargarCatalogo() en frontend)
- Nunca confiar en estado local para calculos de inventario

## Flujo SSO
1. Portal emite JWT con SSO_SECRET, type=SSO_PORTAL, claims: carnet, name, correo
2. Backend valida firma, expiracion y type
3. Backend crea usuario si no existe (rol por defecto: despachador)
4. Backend emite JWT interno de Asistencia

## Evento activo
- Tabla tblEventos, campo Activo = 1
- Seed: Dia del Nino 2026 (Id = 1)

## Despliegue
- Web root: /var/www/asistencia/ (NO /var/www/asistencia/dist/)
- Despues de compilar React: cp -r dist/* /var/www/asistencia/
- Limpiar assets viejos: rm -f /var/www/asistencia/assets/index-*.{js,css}
- Backend: pm2 restart api-asistencia
- Script: bash deploy/deploy.sh
