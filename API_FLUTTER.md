========================================================================
DOCUMENTACION TECNICA - API ASISTENCIA Y DESPACHO
Version: 1.0.0
Fecha: 2026-06-19
Proposito: Guia para desarrollo Flutter
========================================================================

URL BASE: https://rhclaroni.com/api-asistencia/
SERVIDOR: VPS Linux + Nginx + PM2
BACKEND:  NestJS (http://localhost:3000/api/)
FRONTEND: React (https://rhclaroni.com/asistencia/)

========================================================================
1. AUTENTICACION (FLUJO COMPLETO)
========================================================================

1.1 Login contra Portal (NO contra Asistencia directamente)

  POST https://rhclaroni.com/api/auth/login-empleado
  Content-Type: application/json

  {
    "usuario": "500708",           // carnet o correo
    "clave": "password_del_portal", // contraseña del Portal
    "tipo_login": "empleado",
    "returnUrl": "/asistencia"
  }

  Respuesta del Portal:
  {
    "ticket": "eyJ...jwt_firmado_por_portal",
    "usuario": { ... }
  }

1.2 Canje de ticket por JWT de Asistencia

  POST https://rhclaroni.com/api-asistencia/auth/sso-login
  Content-Type: application/json

  {
    "token": "eyJ...jwt_ticket_del_portal"
  }

  Respuesta:
  {
    "access_token": "eyJ...jwt_asistencia",
    "user": {
      "carnet": "500708",
      "nombre": "GUSTAVO ADOLFO LIRA SALAZAR",
      "rol": "despachador"       // admin | supervisor | despachador | consulta
    }
  }

1.3 Almacenamiento seguro

  - Guardar SOLO el access_token en flutter_secure_storage
  - NUNCA guardar la contraseña del Portal
  - Enviar en todas las llamadas: Authorization: Bearer <access_token>
  - Borrar al hacer logout

========================================================================
2. ENDPOINTS PRINCIPALES
========================================================================

2.1 BUSQUEDA POR NOMBRE (NUEVO)
  ---
  GET /attendance/search?q=LIRA
  Authorization: Bearer <token>

  Busca colaboradores por nombre/apellido en:
  - tblColaboradores (local)
  - bdplaner.dbo.p_Usuarios (Portal, si hay pocos resultados locales)

  Respuesta:
  [
    {
      "carnet": "500708",
      "nombre": "GUSTAVO ADOLFO LIRA SALAZAR",
      "gerencia": "GERENCIA DE RECURSOS HUMANOS",
      "ubicacion": "COORD. DE SOPORTE A LA OPERACION"
    },
    { "carnet": "1005989", "nombre": "GABRIELA CASTELLON LIRA", ... }
  ]

  Uso en Flutter:
  - Si el usuario escribe texto (no numerico) -> usar este endpoint
  - Si el usuario escribe un numero (carnet, >= 4 digitos) -> usar lookup directo
  - Mostrar lista de resultados y dejar que el usuario seleccione

2.2 BUSQUEDA POR CARNET (FICHA COMPLETA)
  ---
  GET /attendance/lookup/{carnet}?eventoId=1
  Authorization: Bearer <token>

  ENDPOINT PRINCIPAL para Flutter (scanner + ficha).

  Responde con TODOS los datos del colaborador en 1 sola llamada:
  {
    "colaborador": {
      "carnet": "500708",
      "nombre": "GUSTAVO ADOLFO LIRA SALAZAR",
      "puesto": "ANALISTA DE SOPORTE RH",
      "gerencia": "GERENCIA DE RECURSOS HUMANOS",
      "ubicacion": "COORD. DE SOPORTE A LA OPERACION",
      "inactivo": false
    },
    "inactivo": false,
    "asistio": true,                    // ya registró asistencia?
    "fechaAsistencia": "2026-06-18T10:32:00",
    "adultos": 1,                        // adultos registrados en asistencia
    "ninos": 0,                          // niños registrados en asistencia
    "fotoHcm": "data:image/jpeg;base64,...",  // foto desde Oracle HCM (o null)
    "hijos": [
      {
        "id": 324,
        "nombreHijo": "ESTEBAN ADOLFO LIRA FONSECA",
        "edadHijo": 3.3,
        "generoHijo": "M",
        "categoria": "ENTRE 03.1-4",
        "estadoEntrega": "DELIVERED",     // null | "DELIVERED" | "REVERTED"
        "entregaId": 3,
        "fechaEntrega": "2026-06-18T20:20:52.000Z",
        "recibidoPor": "COLABORADOR",     // null | "COLABORADOR" | "CONYUGE" | "TERCERO"
        "nombreReceptor": null,
        "fotoEvidenciaUrl": "/asistencia-uploads/fotos_evidencia/....webp",  // null si no hay foto
        "jugueteSugerido": {
          "id": 5,
          "nombreJuguete": "BUILDING BLOCK TABLE",
          "stockActual": 54,
          "fotoUrl": "/asistencia-uploads/fotos_juguetes/....webp"  // null si no hay foto
        }
      }
    ],
    "familiaresHcm": [                   // contactos desde Oracle HCM
      {
        "nombre": "JENNIFER GUISSEL FONSECA MONTIEL",
        "tipoRela": "CONYUGE",
        "edad": 36
      }
    ]
  }

2.3 REGISTRAR ASISTENCIA
  ---
  POST /attendance/register
  Content-Type: application/json
  Authorization: Bearer <token>

  {
    "eventoId": 1,
    "carnet": "500708",
    "adultos": 1,          // opcional, default 1
    "ninos": 0             // opcional, default 0
  }

  Respuesta: 201
  {
    "id": 3,
    "eventoId": 1,
    "carnet": "500708",
    "fecha": "2026-06-18T18:01:29.000Z",
    "registradoPor": "500708"
  }

  Codigos de error:
  - 409: "El colaborador ya tiene asistencia registrada para este evento."
  - 404: "El colaborador no existe o no esta activo."

2.4 REVERSAR ASISTENCIA
  ---
  POST /attendance/revert
  Content-Type: application/json
  Authorization: Bearer <token>

  {
    "eventoId": 1,
    "carnet": "500708"
  }

  Respuesta: 201 { "success": true, "message": "Asistencia revertida" }

2.5 VALIDAR ENTREGA (antes de abrir modal)
  ---
  GET /dispatch/validate/{hijoId}/{jugueteId}?eventoId=1
  Authorization: Bearer <token>

  Respuesta:
  {
    "stockDisponible": true,
    "stockActual": 54,
    "hijoYaEntregado": false,
    "colaboradorAsistio": true,
    "esValido": true
  }

2.6 ENTREGAR JUGUETE (MULTIPART)
  ---
  POST /dispatch/deliver
  Content-Type: multipart/form-data
  Authorization: Bearer <token>

  Campos del formulario:
    eventoId: 1
    hijoId: 324
    jugueteId: 5
    carnetColaborador: 500708
    recibidoPor: COLABORADOR | CONYUGE | TERCERO
    nombreReceptor: (obligatorio si recibidoPor=TERCERO)
    foto: (archivo de imagen OPCIONAL, se convierte a WebP)

  La foto de evidencia es 1 por colaborador (no por hijo).
  El backend la comprime a WebP 80%, max 1024px.

  Respuesta: 201
  { "entregaId": 6, "stockRestante": 53 }

2.7 REVERSAR ENTREGA
  ---
  POST /dispatch/{entregaId}/revert
  Content-Type: application/json
  Authorization: Bearer <token>

  { "motivo": "Error al entregar - hijo incorrecto" }
  (minimo 10 caracteres)

  Respuesta: 201 { "success": true }

  NOTA: Al reversar, el stock del juguete se incrementa en +1
        y el hijo vuelve a estado pendiente (se puede re-entregar).

2.8 CATALOGO DE JUGUETES
  ---
  GET /catalog
  Authorization: Bearer <token>

  Respuesta: [
    {
      "id": 1,
      "categoria": "ENTRE 0-1",
      "genero": "TODOS",         // M | F | TODOS
      "nombreJuguete": "GAME BLANKET 8801-31 - MANTA PARA BEBE",
      "stockInicial": 30,
      "stockActual": 30,
      "fotoUrl": "/asistencia-uploads/fotos_juguetes/....webp"
    }
  ]

  Se usa en Flutter para mostrar juguetes alternativos cuando
  el sugerido tiene stock = 0. Filtrar por categoria + genero.

2.9 RESUMEN DEL EVENTO (KPIs)
  ---
  GET /attendance/event/1/summary
  Authorization: Bearer <token>

  Respuesta:
  {
    "TotalNinos": 892,
    "TotalColaboradores": 705,
    "Asistidos": 3,
    "Entregados": 1,
    "Pendientes": 891,
    "stockCritico": [...],
    "avanceCategoria": [...]
  }

2.10 CENSO PAGINADO
  ---
  GET /attendance/censo?eventoId=1&pagina=1&porPagina=10&busqueda=GUSTAVO&estado=pendientes
  Authorization: Bearer <token>

  Parametros:
  - eventoId: obligatorio
  - pagina: numero de pagina (default 1)
  - porPagina: items por pagina (default 50)
  - busqueda: texto para filtrar por carnet/nombre/hijo
  - estado: "pendientes" | "completos" (opcional)

  Filtros:
  - "pendientes": colaboradores con asistencia y entregas pendientes
  - "completos": colaboradores con asistencia + TODOS los hijos entregados

  Respuesta:
  {
    "data": [ { "Carnet":"...", "Nombre":"...", "TotalHijos":2, "Entregados":1, "Asistio":1, "TotalAdultos":2, "TotalNinos":1 } ],
    "total": 705,
    "pagina": 1,
    "porPagina": 10,
    "totalPaginas": 71
  }

2.11 HISTORIAL DE MOVIMIENTOS
  ---
  GET /dispatch/event/1/summary?pagina=1&porPagina=25
  Authorization: Bearer <token>

  Respuesta:
  {
    "data": [
      {
        "entregaId": 1,
        "colaboradorNombre": "GUSTAVO ADOLFO LIRA SALAZAR",
        "colaboradorCarnet": "500708",
        "hijoNombre": "ESTEBAN ADOLFO LIRA FONSECA",
        "nombreJuguete": "BLUEY",
        "estado": "DELIVERED",          // DELIVERED | REVERTED
        "recibidoPor": "COLABORADOR",
        "fechaEntrega": "2026-06-18T21:21:28.000Z",
        "usuarioDespacho": "500708"
      }
    ],
    "total": 5
  }

2.12 HEALTH CHECK
  ---
  GET /health
  (Sin autenticacion)

  Respuesta: { "status": "ok", "database": "connected" }

========================================================================
3. RUTAS PARA ARCHIVOS ESTATICOS
========================================================================

  Las fotos se sirven via Nginx en /asistencia-uploads/

  Foto de juguete:  https://rhclaroni.com/asistencia-uploads/fotos_juguetes/<uuid>.webp
  Foto evidencia:    https://rhclaroni.com/asistencia-uploads/fotos_evidencia/<uuid>.webp

  Las rutas se devuelven en las respuestas de la API como:
  - jugueteSugerido.fotoUrl
  - hijo.fotoEvidenciaUrl

========================================================================
4. ADMINISTRACION DE USUARIOS Y ROLES
========================================================================

  Solo accesible para rol "admin".

  GET /admin/users
  GET /admin/search-portal?q=nombre
  POST /admin/set-role   { "carnet": "500708", "rol": "admin" }

  Roles disponibles:
  - admin: acceso total
  - supervisor: despacho + reversiones
  - despachador: asistencia y entrega (rol por defecto)
  - consulta: solo lectura

========================================================================
5. FLUJO RECOMENDADO PARA FLUTTER
========================================================================

  PANTALLA 1: LOGIN
  1. Usuario ingresa usuario + contraseña del Portal
  2. POST /api/auth/login-empleado (contra Portal)
  3. POST /api-asistencia/auth/sso-login (canjea ticket)
  4. Guarda access_token en flutter_secure_storage

  PANTALLA 2: SCANNER
  1. Abre camara con mobile_scanner
  2. Al detectar codigo de barras:
     GET /attendance/lookup/{carnet}?eventoId=1
  3. Recibe ficha completa del colaborador
  4. Muestra: foto (fotoHcm), nombre, hijos, juguetes sugeridos

  PANTALLA 3: FICHA COLABORADOR
  1. Si no ha asistido: boton "Registrar Asistencia"
     POST /attendance/register { eventoId:1, carnet:"500708", adultos:1, ninos:0 }
  2. Muestra hijos con su juguete sugerido y stock
  3. Boton "Entregar" por hijo:
     POST /dispatch/deliver (multipart con foto opcional)
  4. Boton "Reversar" si ya entregado:
     POST /dispatch/{id}/revert { motivo:"..." }

========================================================================
6. NOTAS IMPORTANTES
========================================================================

  - El carnet debe tener 6 digitos para consultas HCM (se padding con 0 a la izquierda)
    Ej: "772" → "000772" para fotos y contactos de Oracle HCM
  - Las fotos de evidencia y juguetes se sirven bajo /asistencia-uploads/
  - Los valores con # en .env deben ir entre comillas dobles
    Ej: SSO_SECRET="ClaroSSO_Shared_Secret_2026_!#"
  - SweetAlert2 usado para toasts en React. En Flutter usar SnackBar o similar
