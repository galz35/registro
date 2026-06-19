# Sistema de Asistencia y Despacho de Juguetes - Día del Niño

## 📋 Descripción
Sistema para registro de asistencia y despacho de juguetes en eventos corporativos. 
Migración del monolito ASP.NET MVC a stack moderno React + NestJS + SQL Server.

## 🌐 Acceso
- **URL**: https://rhclaroni.com/asistencia/
- **API**: https://rhclaroni.com/api-asistencia/
- **Servidor**: VPS Linux con Nginx + PM2
- **Base de datos**: SQL Server 2022 (localhost:1433)

## 🏗️ Stack Tecnológico

| Componente | Tecnología | Puerto |
|------------|-----------|--------|
| Frontend | React 19 + Vite + TypeScript | Nginx (443/80) |
| Backend | NestJS 11 + TypeScript | 3000 |
| Base de datos | Microsoft SQL Server 2022 | 1433 |
| Servidor web | Nginx | 80/443 |
| Gestor procesos | PM2 | — |
| Imágenes | Sharp (WebP) | — |

## 🗄️ Base de Datos

**Base**: `Asistencia` en SQL Server (localhost)

### Tablas principales

| Tabla | Descripción |
|-------|-------------|
| `tblEventos` | Eventos corporativos (Día del Niño) |
| `tblColaboradores` | Empleados (705 registros del censo + búsqueda en Portal) |
| `tblHijos` | Hijos de colaboradores (892 registros) |
| `tblCatalogoJuguetes` | Catálogo de juguetes con stock (22 items, 967 unidades) |
| `tblAsistenciaEventos` | Registro de asistencia al evento |
| `tblEntregasJuguetes` | Historial de entregas y reversiones |
| `tblUsuariosAsistencia` | Usuarios del sistema con roles |
| `tblImportBatches` | Control de importaciones Excel |
| `tblImportErrores` | Errores de validación de importaciones |

### Procedimientos almacenados

| SP | Función |
|----|---------|
| `sp_Asistencia_LookupCarnet` | Busca colaborador + hijos + juguete sugerido |
| `sp_Asistencia_Registrar` | Registra asistencia (con adultos/niños) |
| `sp_Despacho_Entregar` | Entrega con UPDLOCK + ROWLOCK + validación stock |
| `sp_Despacho_Reversar` | Reversa entrega y restaura stock |
| `sp_Dashboard_ResumenEvento` | KPIs del dashboard |

### Vistas

| Vista | Descripción |
|-------|-------------|
| `vw_ResumenInventario` | Inventario por juguete con % de despacho (Entregados contra registros reales) |
| `vw_BitacoraEntregas` | Auditoría consolidada de entregas |

## 🔐 Autenticación

### SSO Portal
```
POST /api-asistencia/auth/sso-login
Content-Type: application/json

{
  "token": "<jwt_generado_por_portal>"
}
```

El token debe tener:
- `type`: `"SSO_PORTAL"`
- `carnet`: carnet del empleado
- `firma`: con `SSO_SECRET`

### Dev Login (solo desarrollo)
```
POST /api-asistencia/auth/dev-login
Content-Type: application/json

{
  "carnet": "500708",
  "password": "any"
}
```

### Roles del sistema
- `admin`: acceso total
- `supervisor`: despacho + reversiones
- `despachador`: asistencia y entrega

## 📡 API REST

Todas las rutas usan prefijo `/api-asistencia/` que Nginx proxy al backend en puerto 3000 con prefijo `/api/`.

### Health Check
```http
GET /api-asistencia/health
Response: {"status":"ok","database":"connected"}
```

### Autenticación
```http
GET /api-asistencia/auth/me
Authorization: Bearer <token>
Response: {"carnet":"500708","nombre":"...","rol":"admin"}
```

### Asistencia

#### Buscar colaborador (ENDPOINT PRINCIPAL)
```http
GET /api-asistencia/attendance/lookup/{carnet}?eventoId=1
Authorization: Bearer <token>
Response: {
  "colaborador": { "carnet":"500708", "nombre":"...", "gerencia":"...", "ubicacion":"..." },
  "inactivo": false,
  "asistio": true/false,
  "fechaAsistencia": "2026-06-18T10:32:00",
  "adultos": 1,
  "ninos": 0,
  "fotoHcm": "data:image/jpeg;base64,...",
  "hijos": [
    {
      "id": 324,
      "nombreHijo": "ESTEBAN ADOLFO LIRA FONSECA",
      "edadHijo": 3.3,
      "generoHijo": "M",
      "categoria": "ENTRE 03.1-4",
      "estadoEntrega": "DELIVERED" | null,
      "entregaId": 3,
      "fechaEntrega": "2026-06-18T20:20:52.000Z",
      "recibidoPor": "COLABORADOR" | null,
      "fotoEvidenciaUrl": "/uploads/fotos_evidencia/...",
      "jugueteSugerido": {
        "id": 5,
        "nombreJuguete": "BUILDING BLOCK TABLE",
        "stockActual": 54
      }
    }
  ],
  "familiaresHcm": [
    { "nombre": "JENNIFER GUISSEL FONSECA MONTIEL", "tipoRela": "CONYUGE", "edad": 36 }
  ]
}
```

#### Registrar asistencia
```http
POST /api-asistencia/attendance/register
Content-Type: application/json
Authorization: Bearer <token>

{
  "eventoId": 1,
  "carnet": "500708",
  "adultos": 1,
  "ninos": 0
}
```

#### Reversar asistencia
```http
POST /api-asistencia/attendance/revert
Content-Type: application/json
Authorization: Bearer <token>

{
  "eventoId": 1,
  "carnet": "500708"
}
```

#### Búsqueda por nombre
```http
GET /api-asistencia/attendance/search?q=LIRA
Authorization: Bearer <token>
Response: [
  { "carnet":"500708", "nombre":"GUSTAVO ADOLFO LIRA SALAZAR", "gerencia":"GERENCIA DE RECURSOS HUMANOS", "ubicacion":"COORD. DE SOPORTE A LA OPERACION" },
  { "carnet":"1005989", "nombre":"GABRIELA CASTELLON LIRA", ... }
]
```

#### KPIs del evento
```http
GET /api-asistencia/attendance/event/1/summary
Authorization: Bearer <token>
Response: {
  "TotalNinos": 892,
  "Entregados": 1,
  "Pendientes": 891,
  "stockCritico": [],
  "avanceCategoria": [...]
}
```

#### Censo paginado
```http
GET /api-asistencia/attendance/censo?eventoId=1&pagina=1&porPagina=50&busqueda=GUSTAVO&estado=pendientes
Authorization: Bearer <token>
Response: {
  "data": [...],
  "total": 705,
  "pagina": 1,
  "porPagina": 50,
  "totalPaginas": 15
}
```

### Despacho

#### Validar entrega
```http
GET /api-asistencia/dispatch/validate/{hijoId}/{jugueteId}?eventoId=1
Authorization: Bearer <token>
Response: {
  "stockDisponible": true,
  "stockActual": 54,
  "hijoYaEntregado": false,
  "colaboradorAsistio": true,
  "esValido": true
}
```

#### Entregar juguete (MULTIPART)
```http
POST /api-asistencia/dispatch/deliver
Content-Type: multipart/form-data
Authorization: Bearer <token>

eventoId: 1
hijoId: 324
jugueteId: 5
carnetColaborador: 500708
recibidoPor: COLABORADOR | CONYUGE | TERCERO
nombreReceptor: (solo si tercero)
foto: (archivo imagen OBLIGATORIO, se convierte a WebP)
```

**NOTA**: La foto de evidencia es **obligatoria** para entregar. El modal de entrega incluye subir foto y cámara.

#### Actualizar foto de evidencia (NUEVO)
```http
PATCH /api-asistencia/dispatch/{hijoId}/foto?eventoId=1
Content-Type: multipart/form-data
Authorization: Bearer <token>

foto: (archivo imagen OBLIGATORIO)
```
Actualiza la foto de evidencia de una entrega ya completada (DELIVERED). No modifica stock ni estado.

#### Reversar entrega
```http
POST /api-asistencia/dispatch/{entregaId}/revert
Content-Type: application/json
Authorization: Bearer <token>

{
  "motivo": "Error en la entrega - se seleccionó el hijo incorrecto"
}
```

#### Auditoría de entregas
```http
GET /api-asistencia/dispatch/event/1/summary?pagina=1&porPagina=25
Authorization: Bearer <token>
Response: {
  "data": [{ "entregaId":1, "colaboradorNombre":"...", "hijoNombre":"...", "estado":"DELIVERED", ... }],
  "total": 5,
  "pagina": 1
}
```

### Catálogo

#### Listar juguetes
```http
GET /api-asistencia/catalog
Authorization: Bearer <token>
Response: [
  { "id":1, "categoria":"ENTRE 0-1", "genero":"TODOS", "nombreJuguete":"GAME BLANKET", "stockActual":30, ... }
]
```

#### Crear juguete
```http
POST /api-asistencia/catalog
Content-Type: multipart/form-data
Authorization: Bearer <token>

categoria: ENTRE 0-1
genero: TODOS
nombreJuguete: Nuevo Juguete
stockInicial: 50
foto: (archivo imagen opcional)
```

#### Editar juguete
```http
PUT /api-asistencia/catalog/{id}
Content-Type: multipart/form-data
Authorization: Bearer <token>

nombreJuguete: Nombre actualizado
stockInicial: 60
foto: (archivo imagen opcional)
```

#### Subir foto
```http
POST /api-asistencia/catalog/{id}/photo
Content-Type: multipart/form-data
Authorization: Bearer <token>

foto: (archivo imagen)
```

### Reportes (CSV)
```http
GET /api-asistencia/reports/entregas.csv?eventoId=1
GET /api-asistencia/reports/pendientes.csv?eventoId=1
GET /api-asistencia/reports/inventario.csv
Authorization: Bearer <token>
```

## 🚀 Cómo consumir desde Flutter

### Flujo de Autenticación

El flujo de login para Flutter es IDÉNTICO al que usa portal-planer. No se autentica directamente contra Asistencia, sino que usa el Portal como proveedor de identidad:

```
Flutter App
    │
    ├─ 1. Usuario escribe usuario + contraseña del Portal
    │
    ├─ 2. POST https://rhclaroni.com/api/auth/login-empleado
    │     { "usuario": "500708", "clave": "password_del_portal",
    │       "tipo_login": "empleado", "returnUrl": "/asistencia" }
    │     ↑━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━→ Portal API
    │
    ├─ 3. ←── Portal responde:
    │     { "ticket": "eyJ...jwt_firmado_por_portal", "usuario": {...} }
    │
    ├─ 4. POST https://rhclaroni.com/api-asistencia/auth/sso-login
    │     { "token": "eyJ...jwt_firmado_por_portal" }
    │     ↑━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━→ NestJS Asistencia
    │
    ├─ 5. NestJS valida:
    │     • Firma del JWT con SSO_SECRET
    │     • Expiración
    │     • type === "SSO_PORTAL"
    │     • carnet existe
    │     → Busca o crea usuario en tblUsuariosAsistencia
    │     → Asigna rol (despachador por defecto)
    │
    ├─ 6. ←── NestJS responde:
    │     { "access_token": "eyJ...jwt_asistencia",
    │       "user": { "carnet":"500708", "nombre":"...", "rol":"despachador" } }
    │
    └─ 7. Flutter guarda SOLO el access_token en flutter_secure_storage
          • NUNCA guardar la contraseña del Portal
          • El access_token expira (configurable: 8h)
          • Para renovar, repetir el flujo desde el paso 2
```

**Reglas de seguridad:**
- ❌ Flutter NO debe validar ni conocer la contraseña del Portal
- ❌ No guardar contraseña en el dispositivo
- ✅ Solo guardar `access_token` en `flutter_secure_storage`
- ✅ Usar `Authorization: Bearer <access_token>` en todas las llamadas
- ✅ Borrar tokens al hacer logout
- ✅ Mostrar nombre/carnet del operador activo en pantalla
- ✅ Botón de logout visible

### Endpoints que Flutter debe consumir

Todas las rutas usan el prefijo `https://rhclaroni.com/api-asistencia/` con `Authorization: Bearer <token>`.

#### 1. Login (contra Portal, no contra Asistencia)
```dart
// PASO 1: Login contra Portal (NO contra Asistencia)
final portalRes = await http.post(
  Uri.parse('https://rhclaroni.com/api/auth/login-empleado'),
  body: {
    'usuario': '500708',          // carnet o correo
    'clave': 'password_portal',    // contraseña del Portal
    'tipo_login': 'empleado',
    'returnUrl': '/asistencia',
  },
);
// Response: { "ticket": "jwt...", "usuario": {...} }

// PASO 2: Canjear ticket por JWT de Asistencia
final asisRes = await http.post(
  Uri.parse('https://rhclaroni.com/api-asistencia/auth/sso-login'),
  headers: { 'Content-Type': 'application/json' },
  body: jsonEncode({ 'token': ticket }),
);
// Response: { "access_token": "jwt...", "user": { "carnet":"500708", "rol":"despachador" } }

// PASO 3: Guardar SOLO access_token
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
final storage = FlutterSecureStorage();
await storage.write(key: 'token', value: data['access_token']);
```

#### 2. Escanear carnet (Endpoint principal - devuelve TODO)
```dart
final res = await http.get(
  Uri.parse('https://rhclaroni.com/api-asistencia/attendance/lookup/$carnet?eventoId=1'),
  headers: { 'Authorization': 'Bearer $token' },
);
final data = jsonDecode(res.body);
// data.colaborador          → { carnet, nombre, gerencia, ubicacion }
// data.fotoHcm              → "data:image/jpeg;base64,..." o null
// data.asistio              → true/false
// data.hijos                → [
//   {
//     id, nombreHijo, edadHijo, generoHijo, categoria,
//     estadoEntrega, entregaId, fechaEntrega, recibidoPor,
//     fotoEvidenciaUrl,
//     jugueteSugerido: { id, nombreJuguete, stockActual, fotoUrl }
//   }
// ]
// data.familiaresHcm        → [{ nombre, tipoRela, edad }]
// data.inactivo             → true/false (si está inactivo en Portal)
```

#### 3. Registrar asistencia
```dart
final res = await http.post(
  Uri.parse('https://rhclaroni.com/api-asistencia/attendance/register'),
  headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer $token' },
  body: jsonEncode({ 'eventoId': 1, 'carnet': '500708', 'adultos': 1, 'ninos': 0 }),
);
```

#### 4. Entregar juguete con foto (MULTIPART)
```dart
var request = http.MultipartRequest(
  'POST',
  Uri.parse('https://rhclaroni.com/api-asistencia/dispatch/deliver'),
);
request.headers['Authorization'] = 'Bearer $token';
request.fields['eventoId'] = '1';
request.fields['hijoId'] = '324';
request.fields['jugueteId'] = '5';
request.fields['carnetColaborador'] = '500708';
request.fields['recibidoPor'] = 'COLABORADOR'; // COLABORADOR | CONYUGE | TERCERO
if (nombreReceptor != null) request.fields['nombreReceptor'] = nombreReceptor;
if (fotoFile != null) {
  request.files.add(await http.MultipartFile.fromPath('foto', fotoFile.path));
}
final res = await request.send();
final body = await res.stream.bytesToString();
// Response: { "entregaId": 6, "stockRestante": 53 }
```

#### 5. Reversar entrega
```dart
final res = await http.post(
  Uri.parse('https://rhclaroni.com/api-asistencia/dispatch/$entregaId/revert'),
  headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer $token' },
  body: jsonEncode({ 'motivo': 'Error al entregar - se seleccionó el hijo incorrecto' }),
);
// Response: { "success": true }
```

#### 6. Validar entrega (antes de abrir modal)
```dart
final res = await http.get(
  Uri.parse('https://rhclaroni.com/api-asistencia/dispatch/validate/$hijoId/$jugueteId?eventoId=1'),
  headers: { 'Authorization': 'Bearer $token' },
);
// Response: { "stockDisponible": true, "stockActual": 54, "esValido": true }
```

#### 7. Catálogo de juguetes
```dart
final res = await http.get(
  Uri.parse('https://rhclaroni.com/api-asistencia/catalog'),
  headers: { 'Authorization': 'Bearer $token' },
);
// Response: [{ id, categoria, genero, nombreJuguete, stockActual, fotoUrl }]
```

## 📁 Estructura del Proyecto

```
/opt/apps/asistencia/registro/
├── database/                    # Scripts SQL versionados
│   ├── 001_create_database.sql
│   ├── 002_create_tables.sql
│   ├── 003_create_indexes.sql
│   ├── 004_create_procedures.sql
│   ├── 005_seed_initial.sql
│   └── 006_create_views.sql
├── nest/                        # Backend NestJS
│   ├── src/
│   │   ├── attendance/          # Módulo de asistencia
│   │   ├── auth/                # Módulo de autenticación (JWT + SSO)
│   │   ├── catalog/             # Módulo de catálogo
│   │   ├── common/              # Guards, decorators, filtros
│   │   ├── database/            # Conexión SQL Server (pool mssql)
│   │   ├── dispatch/            # Módulo de despacho
│   │   ├── imports/             # Importación Excel
│   │   ├── integration/         # Integración HCM Oracle Cloud
│   │   └── reports/             # Reportes CSV
│   └── .env                     # Variables de entorno
├── react/                       # Frontend React
│   ├── src/
│   │   ├── components/          # Componentes compartidos
│   │   ├── context/             # AuthContext
│   │   ├── hooks/               # Custom hooks (usePolling, useDebounce)
│   │   ├── pages/               # Páginas
│   │   │   ├── LoginPage.tsx
│   │   │   ├── SsoHandlerPage.tsx
│   │   │   ├── CommandCenter.tsx    # Dashboard
│   │   │   ├── AttendancePage.tsx   # Registro de asistencia
│   │   │   ├── DispatchPage.tsx     # Despacho de juguetes
│   │   │   ├── CatalogPage.tsx      # Catálogo
│   │   │   ├── HistoryPage.tsx      # Historial
│   │   │   └── ImportPage.tsx       # Importar Excel
│   │   └── services/            # API client (axios)
│   └── .env                     # VITE_API_URL=/api-asistencia
├── flutter/                     # App móvil (pendiente de desarrollo)
├── deploy/                      # Configuración de despliegue
│   ├── nginx.conf               # Configuración Nginx
│   ├── ecosystem.config.js      # Configuración PM2
│   └── deploy.sh                # Script de despliegue
├── CHECKLIST.md                 # Checklist de requerimientos
├── AGENTS.md                    # Contexto para agentes IA
└── design_reference.txt         # Referencia de diseño
```

## 🔧 Despliegue

### Backend
```bash
cd /opt/apps/asistencia/registro/nest
npm run build
pm2 start dist/main.js --name api-asistencia
pm2 save
```

### Frontend
```bash
cd /opt/apps/asistencia/registro/react
npm run build
cp -r dist/* /var/www/asistencia/
```

### Nginx
El frontend se sirve en `/asistencia/` y la API en `/api-asistencia/`.
Config: `/etc/nginx/snippets/asistencia_routes.conf`

## ✅ Tests Realizados
22/22 pruebas de API pasaron exitosamente:
- Health check
- Auth (SSO + dev-login)
- Attendance lookup, register, summary, censo, search, revert
- Catalog list, summary, create
- Dispatch validate, deliver, audit, revert, updateFoto
- Reports CSV

## 🆕 Últimas funcionalidades agregadas

### Búsqueda por nombre en asistencia
```
GET /api-asistencia/attendance/search?q=nombre
Authorization: Bearer <token>
```
Busca colaboradores en BD local + Portal (bdplaner.dbo.p_Usuarios).

### Quién asiste (asistioPor)
Al registrar asistencia, se guarda quién asistió realmente:
- `COLABORADOR` / `CONYUGE` / `TERCERO` + nombre
- Se muestra en tabla Asistieron y en detalle de colaborador

### Foto de evidencia OBLIGATORIA
- La foto es requerida antes de entregar (no permite omitirla)
- Modal de entrega individual incluye subir foto + cámara
- Actualización de foto en entregas ya completadas: `PATCH /dispatch/:hijoId/foto?eventoId=`
- En tabla Despachados Completos, botón 📷 abre foto completa directamente

### Refrescar datos en despacho
- Botón Refrescar en header global y en sección Pendientes
- Recarga catálogo + lista de asistidos + limpia ficha seleccionada

### Catálogo de juguetes mejorado
- 👁️ Ver movimientos por juguete (cargado desde vw_BitacoraEntregas)
- Resumen inventario con entregados/reversados reales: `GET /catalog/summary`
- vw_ResumenInventario corregido: cuenta registros reales (no StockInicial-StockActual)

### Importación Excel con preview
- Botón "Plantilla" descarga .xlsx con formato correcto
- Flujo de 2 pasos: validar (preview con errores/duplicados) → procesar
- Endpoint: `GET /imports/template/:tipo` (censo | catalogo)

### Reportes (nuevo módulo)
- `/asistencia/reports` — 3 secciones apiladas en 1 página (sin tabs):
  - **Asistencia**: KPIs (asistieron, adultos, niños, hijos) + tabla detallada + botón Excel
  - **Despacho**: búsqueda, paginación, botón Excel
  - **Inventario**: KPIs de stock, tabla con % despacho, 👁️ detalle por juguete, botón Excel
- Descarga Excel: `GET /reports/asistencia.xlsx`, `GET /reports/despacho.xlsx`, `GET /reports/inventario.xlsx`
  (inventario.xlsx incluye 2 hojas: resumen + detalle de cada entrega)

### Restricción por departamento (MANAGUA)
- El campo `DepartamentoGeografico` se migró desde el Excel (última columna) a los 705 colaboradores
- 428 de MANAGUA, 277 de otros departamentos
- El despacho solo permite entregar a colaboradores con `DepartamentoGeografico = 'MANAGUA'`
- Validado en `dispatch.service.ts` antes de ejecutar el SP
- Import ahora hace UPDATE si el colaborador ya existe (antes solo INSERT)

### Otras mejoras
- Botones ✕ para limpiar inputs de búsqueda (attendance, filtros)
- Limpieza automática de búsqueda al registrar asistencia
- **Auto-llenar Niños** al cargar colaborador en registro (se llena con `hijos.length`)
- **Reset de formulario** al registrar asistencia (adultos→1, niños→0, quién asiste→Colaborador)
- **Validación Tercero**: mostrar error si no se ingresa nombre al seleccionar Tercero en despacho
- **Advertencia no-Managua** en registro: si el colaborador no es de MANAGUA, muestra mensaje rojo
  indicando que no aplica para despacho de juguetes (se puede registrar, pero no recibir juguete)
- **Validación contra Portal**: al buscar colaborador, consulta `bdplaner.dbo.p_Usuarios` para verificar
  que esté activo. Si está de baja en Portal, lo desactiva localmente y rechaza el registro
- Preview de foto con botón ✕ y "click fuera para cerrar"
- Paginación: Pendientes 3 items, Completos 5 items
- Filtros de tabla con fondo blanco y búsqueda por carnet+nombre+gerencia
- Fix: historial de movimientos en catálogo (bug `data.data`)
- Fix: deploy a `/var/www/asistencia/` (no `dist/`) + limpieza de assets viejos
- Deploy script corregido

## 📊 Dashboards
- `/asistencia/` — Dashboard con KPIs y censo paginado
- `/asistencia/attendance` — Registro de asistencia + Contactos HCM + búsqueda por nombre
- `/asistencia/dispatch` — Despacho con foto obligatoria, cámara, batch, refrescar
- `/asistencia/catalog` — Catálogo con edición, fotos, historial de movimientos, resumen inventario
- `/asistencia/history` — Historial de movimientos (bug corregido)
- `/asistencia/reports` — Reportes consolidados (asistencia, despacho, inventario)
- `/asistencia/import` — Importación Excel con preview y plantilla descargable
- `/asistencia/admin` — Administración de usuarios y roles

## 🔐 Variables de Entorno (.env)
```
PORT=3000
DB_SERVER=localhost
DB_PORT=1433
DB_USER=sa
DB_PASSWORD=TuPasswordFuerte!2026
DB_DATABASE=Asistencia
JWT_SECRET=claro_despacho_jwt_secret_2026
SSO_SECRET="ClaroSSO_Shared_Secret_2026_!#"
HCM_API_URL=https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/workers
HCM_USERNAME=Claro_RhOnline_WS_SS
HCM_PASSWORD="HCM-RH0nl1ne@#3"
```

> **Nota**: Los valores con `#` deben ir entre comillas dobles en el `.env`
