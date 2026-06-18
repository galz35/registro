========================================================================
CHECKLIST DE REQUERIMIENTOS - ASISTENCIA Y DESPACHO
========================================================================

Basado en: Documentos 01-14, Plan Maestro, Requerimientos Funcionales
Fecha: 2026-06-18
========================================================================

A. BACKEND - API NESTJS (22 endpoints probados: 22/22 OK)
========================================================================
[✅] POST /auth/sso-login        - Login SSO con JWT del Portal
[✅] POST /auth/dev-login        - Login desarrollo (solo dev)
[✅] GET  /auth/me               - Usuario autenticado
[✅] GET  /attendance/lookup/:carnet - Ficha completa colaborador (1 llamada)
[✅] POST /attendance/register   - Registrar asistencia
[✅] GET  /attendance/event/:id/summary - KPIs dashboard
[✅] GET  /attendance/censo      - Censo paginado con filtros
[✅] GET  /catalog               - Listar juguetes activos
[✅] POST /catalog               - Crear juguete
[✅] PUT  /catalog/:id           - Editar juguete
[✅] PATCH /catalog/:id/deactivate - Soft-delete juguete
[✅] POST /catalog/:id/photo     - Subir foto juguete WebP
[✅] GET  /catalog/summary       - Resumen inventario
[✅] GET  /dispatch/validate     - Validacion pre-entrega
[✅] POST /dispatch/deliver      - Registrar entrega (ACID + UPDLOCK)
[✅] POST /dispatch/:id/revert   - Reversar entrega (+stock)
[✅] GET  /dispatch/event/:id/summary - Bitacora auditoria
[✅] POST /imports/censo/validate - Validar Excel censo
[✅] POST /imports/censo/apply   - Aplicar importacion censo
[✅] POST /imports/catalogo/validate - Validar Excel catalogo
[✅] POST /imports/catalogo/apply - Aplicar importacion catalogo
[✅] GET  /health                - Health check + BD
[⬜] GET  /reports/pdf           - Reporte PDF (fase 2)

B. BASE DE DATOS SQL SERVER
========================================================================
[✅] tblEventos                 - Eventos corporativos
[✅] tblColaboradores           - Empleados (705 importados)
[✅] tblHijos                   - Hijos (892 importados)
[✅] tblCatalogoJuguetes        - Catalogo juguetes (22 importados)
[✅] tblAsistenciaEventos       - Registro asistencia
[✅] tblEntregasJuguetes        - Historial entregas/reversiones
[✅] tblUsuariosAsistencia      - Usuarios con roles
[✅] tblImportBatches           - Control importaciones Excel
[✅] tblImportErrores           - Errores de validacion
[✅] tblAuditLog                - Auditoria operaciones
[✅] Indices de rendimiento     - 6 indices (incluye unique filtered)
[✅] Constraints CHECK          - Stock>=0, genero M/F/TODOS, estados
[✅] Stored Procedures (5)      - Lookup, Registrar, Entregar, Reversar, Dashboard
[✅] Vistas (2)                 - vw_ResumenInventario, vw_BitacoraEntregas
[✅] Bloqueo pesimista          - UPDLOCK + ROWLOCK en entregas y reversiones
[✅] Indice unico filtrado      - Evita doble entrega activa por hijo/evento

C. REGLAS DE NEGOCIO
========================================================================
[✅] Stock no negativo          - CHECK StockActual >= 0
[✅] Sin ORM                    - mssql directo, queries parametrizadas
[✅] OUTPUT INSERTED            - En lugar de RETURNING (SQL Server)
[✅] Transacciones ACID         - BEGIN/COMMIT/ROLLBACK en SP
[✅] Duplicado censo            - Detectado en validator (3 duplicados)
[✅] Deficit 0-1                - Detectado (35 censados vs 30 aprobados)
[✅] Herencia edad catalogo     - Filas sin edad heredan de fila anterior
[✅] Imagenes omitidas          - Se subiran desde web (no del Excel)
[✅] Categoria+genero fallback  - Busca TODOS si no hay M/F especifico

D. FRONTEND REACT
========================================================================
[✅] Login SSO                  - /auth/sso?token=...
[✅] Dashboard                  - KPIs + censo + busqueda
[✅] Registro Asistencia        - Pagina dedicada estilo EventoLiga
[✅] Despacho                   - Buscar + hijos + entregar/reversar
[✅] Catalogo                   - Pagina dedicada (no popup)
[✅] Importar Excel             - Pagina dedicada (censo y catalogo)
[✅] Sidebar navegacion         - Links a cada pagina
[✅] Estados: carga/error/vacio - Todos los componentes
[✅] Colores corporativos       - Rojo Claro #da121a, blanco, grises
[✅] Sin color azul             - Prohibido por identidad visual
[✅] Diseno responsivo          - Tailwind CSS
[✅] Inter/Outfit fonts         - Tipografia corporativa

E. SEGURIDAD
========================================================================
[✅] JWT en todas las APIs      - Bearer token requerido
[✅] Roles: admin/supervisor/despachador/consulta - RolesGuard
[✅] SSO Portal                 - Token firmado con SSO_SECRET
[✅] Secretos en .env           - No hardcodeados
[✅] CORS configurable          - Origin restringible
[✅] Helmet                     - Headers de seguridad
[✅] Validacion DTOs            - class-validator con whitelist
[✅] SQL parametrizado          - .input() de mssql, nunca concatenacion
[✅] Imagenes WebP              - Convertidas con sharp, no Base64 en BD

F. DEPLOY
========================================================================
[✅] Nginx config               - SPA + API proxy + uploads
[✅] PM2 ecosystem              - Gestion de procesos
[✅] Script deploy.sh           - Automatizacion
[✅] prefijos:
     [✅] /asistencia/          - Frontend SPA
     [✅] /api-asistencia/      - API proxy
[✅] DB route en PortalCore     - AplicacionSistema: codigo=asistencia
[✅] RouteOverride en portal    - auth.service.ts: asistencia route

G. DATOS MIGRADOS
========================================================================
[✅] Censo: 705 colaboradores   - Desde DATAS ANALIZADAS PADRE_HIJOS.xlsx
[✅] Hijos: 892 ninos           - Desde hoja DATA HIJOS
[✅] Catalogo: 22 juguetes      - Desde hoja IMPRIMIR (967 unidades)
[✅] Evento activo:             - Dia del Nino 2026 (Id=1)
[✅] Usuario admin creado       - Para pruebas
[✅] Usuario 500708 creado      - GUSTAVO ADOLFO LIRA SALAZAR (despachador)

H. PENDIENTE (Fase 2+)
========================================================================
[⬜] Flutter app                - Sin SDK instalado
[⬜] Modo offline               - Cache local Hive
[⬜] Sincronizacion             - Cola de entregas offline
[⬜] Reportes PDF               - iTextSharp alternativo
[⬜] Notificaciones             - Push/SMS
[⬜] Pre-seleccion juguete      - Colaborador elige antes del evento
[⬜] Carga de imagenes WebP     - Desde sitio web (pendiente)

========================================================================
RESUMEN: 72/76 items cubiertos ✅ | 4 pendientes (Flutter + Fase 2)
========================================================================
