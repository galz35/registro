using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using RestSharp;
using slnRhonline.Models;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using System.IO;
using ImageMagick;
using System.Web;
using Entities;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Data;
using System.Text;
using System.IO.Compression;
using System.Numerics;

namespace slnRhonline.Controllers
{
    public class HelpdeskController : Controller
    {
        private static ServiceReference1.ClaroAsemClient ClaroWCF = new ServiceReference1.ClaroAsemClient();

        // URL base del API REST externo para operaciones de escritura (este API maneja envío de correo y registro de chat)
        private readonly string apiBaseUrl = "http://172.26.54.66/apihcm/api/helpdesk/";
        // Cadena de conexión para operaciones de lectura (GET)
        private readonly string connectionString = "Data Source=192.168.8.234;Connection Timeout=60;Initial Catalog=SIGHO1;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;";

        // GET: MisCasos - Vista de casos del usuario (lectura directa)
        public ActionResult MisCasos()
        {
            var eEmployee = Session["User"] as Entities.Employees;
            if (eEmployee == null)
                return RedirectToAction("Login", "Account");

            using (var db = new SqlConnection(connectionString))
            {
                // Empleados por Gerencia
                var empleados = db.Query<emp2024>(
                    "dbo.usp_Empleados_PorGerencia",
                    new { Gerencia = eEmployee.GERENCIA },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                Session["listempleadocaso"] = empleados;
            }

            using (var db = new SqlConnection(connectionString))
            {
                // Casos del Usuario
                var casos = db.Query<Caso>(
                    "dbo.usp_Casos_PorUsuario",
                    new { UsuarioID = eEmployee.EmployeeNumber, Correo = eEmployee.EmailAddress },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(casos);
            }
        }

        // GET: TodosLosCasos - Vista de todos los casos (lectura directa)
        public ActionResult TodosLosCasos()
        {
            return View();
        }

        public JsonResult TodosLosCasosjson()
        {
            using (var db = new SqlConnection(connectionString))
            {
                var casos = db.Query<Caso>("SELECT * FROM Caso").ToList();
                return Json(new { data = casos }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult ObtenerEmpleados()
        {
            List<emp2024> empleados = Session["listempleadocaso"] as List<emp2024>;
            if (empleados == null)
            {
                using (var db = new SqlConnection(connectionString))
                {
                    string query = "SELECT carnet, nombre_completo, correo FROM EMP2024";
                    empleados = db.Query<emp2024>(query).ToList();
                }
            }
            var empleadosDTO = empleados.Select(e => new
            {
                carnet = e.carnet,
                nombre_completo = e.nombre_completo,
                correo = e.correo
            }).ToList();
            return Json(empleadosDTO, JsonRequestBehavior.AllowGet);
        }

        #region Operaciones de Escritura (API REST Externa)

        // POST: Crear Caso - se consume el API REST en la ruta: api/helpdesk/crear?token=021092
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CrearCaso(Caso caso, IEnumerable<HttpPostedFileBase> archivos)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Error al crear el caso." });

            using (System.Data.IDbConnection db = new SqlConnection(connectionString))
            {
                caso.FechaCreacion = DateTime.Now;
                caso.Estado = "Abierto";

                // ⚠️ Igual que bin: guarda en columna ParaQuien (si tu SELECT usa 'Correo', usa CrearCasox)
                var sqlInsert =
                    "INSERT INTO Caso (ParaQuien, Titulo, Descripcion, Estado, Prioridad, TipoCaso, FechaCreacion) " +
                    "VALUES (@ParaQuien, @Titulo, @Descripcion, @Estado, @Prioridad, @TipoCaso, @FechaCreacion); " +
                    "SELECT CAST(SCOPE_IDENTITY() as int);";

                int casoId = db.Query<int>(sqlInsert, caso).Single();

                // 📎 Adjuntos → WebP
                if (archivos != null && archivos.Any())
                {
                    foreach (var f in archivos)
                    {
                        if (f == null || f.ContentLength <= 0) continue;
                        var a = new Archivo
                        {
                            CasoID = casoId,
                            NombreArchivo = f.FileName,
                            TipoArchivo = f.ContentType,
                            DatosArchivo = ConvertToWebP(f),
                            FechaSubida = DateTime.Now
                        };
                        db.Execute("INSERT INTO Archivo (CasoID, NombreArchivo, TipoArchivo, DatosArchivo, FechaSubida) " +
                                   "VALUES (@CasoID, @NombreArchivo, @TipoArchivo, @DatosArchivo, @FechaSubida)", a);
                    }
                }

                return Json(new { success = true, message = "Caso creado exitosamente." });
            }
        }
        public string ConvertToBase64(byte[] datos)
        {
            return Convert.ToBase64String(datos);
        }
        private byte[] ConvertToWebP(HttpPostedFileBase archivo)
        {
            using (var src = new MemoryStream())
            {
                archivo.InputStream.CopyTo(src);
                src.Position = 0;
                using (var img = new MagickImage(src))
                {
                    img.Format = MagickFormat.WebP;
                    img.Quality = 70;
                    using (var outMs = new MemoryStream())
                    {
                        img.Write(outMs);
                        return outMs.ToArray();
                    }
                }
            }
        }
        //        [HttpPost]
        //        [ValidateInput(false)] // ← evita bloqueo por HTML en 'Descripcion'
        //        public JsonResult CrearCasox()
        //        {
        //            try
        //            {
        //                var u = Session["User"] as Entities.Employees;
        //                if (u == null) return Json(new { success = false, message = "Sesión expirada." });

        //                // Lee directo del form (no depende del binder)
        //                var caso = new slnRhonline.Models.Caso
        //                {
        //                    UsuarioID = u.EmployeeNumber,
        //                    ParaQuien = (Request.Form["ParaQuien"] ?? "").Trim(),
        //                    Titulo = (Request.Form["Titulo"] ?? "").Trim(),
        //                    Descripcion = Request.Unvalidated["Descripcion"], // ← sin validación
        //                    Prioridad = (Request.Form["Prioridad"] ?? "").Trim(),
        //                    TipoCaso = (Request.Form["TipoCaso"] ?? "").Trim(),
        //                    Estado = "Abierto",
        //                    FechaCreacion = DateTime.Now
        //                };

        //                using (var db = new SqlConnection(connectionString))
        //                {
        //                    const string insCaso = @"
        //INSERT INTO Caso (Correo,UsuarioID,Titulo,Descripcion,Estado,Prioridad,TipoCaso,FechaCreacion)
        //VALUES (@ParaQuien,@UsuarioID,@Titulo,@Descripcion,@Estado,@Prioridad,@TipoCaso,@FechaCreacion);
        //SELECT CAST(SCOPE_IDENTITY() AS int);";
        //                    int casoId = db.Query<int>(insCaso, caso).Single();

        //                    // Archivos desde Request.Files
        //                    for (int i = 0; i < Request.Files.Count; i++)
        //                    {
        //                        var f = Request.Files[i];
        //                        if (f == null || f.ContentLength <= 0) continue;

        //                        var a = new Archivo
        //                        {
        //                            CasoID = casoId,
        //                            NombreArchivo = Path.GetFileName(f.FileName),
        //                            TipoArchivo = "image/webp",
        //                            DatosArchivo = ConvertToWebP(f),
        //                            FechaSubida = DateTime.Now
        //                        };
        //                        db.Execute(@"INSERT INTO Archivo (CasoID,NombreArchivo,TipoArchivo,DatosArchivo,FechaSubida)
        //                             VALUES (@CasoID,@NombreArchivo,@TipoArchivo,@DatosArchivo,@FechaSubida)", a);
        //                    }
        //                }

        //                return Json(new { success = true, message = "Caso creado exitosamente." });
        //            }
        //            catch (HttpRequestValidationException)
        //            {
        //                return Json(new { success = false, message = "Contenido inválido en la descripción." });
        //            }
        //            catch (Exception ex)
        //            {
        //                return Json(new { success = false, message = "Error: " + ex.Message });
        //            }
        //        }
        // ⚠️ Cambia la acción a async y envía el correo de creación a Soporte usando SOLO el ID
        [HttpPost]
        [ValidateInput(false)] // permite HTML en 'Descripcion'
        public async Task<JsonResult> CrearCasox()
        {
            try
            {
                var u = Session["User"] as Entities.Employees;
                if (u == null) return Json(new { success = false, message = "Sesión expirada." });

                // 1) Construir entidad desde el form
                var caso = new slnRhonline.Models.Caso
                {
                    UsuarioID = u.EmployeeNumber,
                    ParaQuien = (Request.Form["ParaQuien"] ?? "").Trim(),
                    Titulo = (Request.Form["Titulo"] ?? "").Trim(),
                    Descripcion = Request.Unvalidated["Descripcion"], // sin validación
                    Prioridad = (Request.Form["Prioridad"] ?? "").Trim(),
                    TipoCaso = (Request.Form["TipoCaso"] ?? "").Trim(),
                    Estado = "Abierto",
                    FechaCreacion = DateTime.Now
                };

                int casoId;
                using (var db = new SqlConnection(connectionString))
                {
                    // 2) Insertar Caso y obtener ID
                    const string insCaso = @"
INSERT INTO Caso (Correo,UsuarioID,Titulo,Descripcion,Estado,Prioridad,TipoCaso,FechaCreacion)
VALUES (@ParaQuien,@UsuarioID,@Titulo,@Descripcion,@Estado,@Prioridad,@TipoCaso,@FechaCreacion);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    casoId = db.Query<int>(insCaso, caso).Single();

                    // 3) Guardar archivos (convertidos a WEBP)
                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        var f = Request.Files[i];
                        if (f == null || f.ContentLength <= 0) continue;

                        var a = new Archivo
                        {
                            CasoID = casoId,
                            NombreArchivo = Path.GetFileName(f.FileName),
                            TipoArchivo = "image/webp",
                            DatosArchivo = ConvertToWebP(f), // ← tu método existente
                            FechaSubida = DateTime.Now
                        };

                        db.Execute(@"INSERT INTO Archivo (CasoID,NombreArchivo,TipoArchivo,DatosArchivo,FechaSubida)
                             VALUES (@CasoID,@NombreArchivo,@TipoArchivo,@DatosArchivo,@FechaSubida)", a);
                    }
                }

                // 4) Notificar a Soporte (usa solo el ID; obtiene el caso internamente)
                //    Si el envío falla, no romper la creación del caso.
                try { await GenerarCorreoCreacionCasoSoporteAsync(casoId); } catch { /* log opcional */ }

                // 5) Respuesta JSON con el ID del ticket
                return Json(new { success = true, id = casoId, message = "Caso creado exitosamente." });
            }
            catch (HttpRequestValidationException)
            {
                return Json(new { success = false, message = "Contenido inválido en la descripción." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Requiere: using System.Web; using System.Configuration;

        // 📧 Notificación a SOPORTE al crear caso (solo con ID). Sin “Soporte asignado”.
        //    Resuelve URL detalle automáticamente (AppSettings["HelpdeskBaseUrl"] o dominio actual).
        public async Task<string> GenerarCorreoCreacionCasoSoporteAsync(int id)
        {
            string conclu = "";
            var caso = await ObtenerCasoByIdAsync(id);
            if (caso == null) throw new InvalidOperationException("Caso no encontrado.");
            UpsertCasoEnSesion(caso);

            // Destinatarios
           
           
            // Datos (encode)
            string tituloEnc = HttpUtility.HtmlEncode(caso.Titulo ?? "-");
            string estadoEnc = HttpUtility.HtmlEncode(caso.Estado ?? "Abierto");
            string tipoCasoEnc = HttpUtility.HtmlEncode(caso.TipoCaso ?? "-");
            string prioridadEnc = HttpUtility.HtmlEncode(caso.Prioridad ?? "-");
            string descripcionEnc = HttpUtility.HtmlEncode(caso.Descripcion ?? "-");

            DateTime fcre = caso.FechaCreacion  ;
            string fCreTxt = fcre.ToString("dd/MM/yyyy HH:mm");

            string nomAutorEnc = HttpUtility.HtmlEncode(caso.NombreAutor ?? "-");
            string cargoAutorEnc = HttpUtility.HtmlEncode(caso.CargoAutor ?? "-");
            string areaAutorEnc = HttpUtility.HtmlEncode(caso.AreaAutor ?? "-");
            string telAutorEnc = HttpUtility.HtmlEncode(caso.TelefonoAutor ?? "-");
            string mailAutorEnc = HttpUtility.HtmlEncode(caso.CorreoAutor ?? "-");

            string nomAfectadoEnc = HttpUtility.HtmlEncode(caso.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(caso.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(caso.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(caso.TelefonoResponsable ?? "-");

            string titulo1 = $"Helpdesk Tick-{id}: Caso Creado - {tituloEnc}";

            // HTML (mismo estilo; sin “Soporte asignado”)
            // Header compacto: Ticket #, Título y Creado dentro del <h1>
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#e11d48;color:#fff;padding:22px 26px}}
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;gap:8px;align-items:center}}
  .ticket,.titulo,.crea{{white-space:nowrap}} .dash{{opacity:.8}}
  .badge-new{{display:inline-block;background:#eef2ff;color:#1e1b4b;border:1px solid #c7d2fe;padding:4px 10px;border-radius:999px;font-size:12px}}
  .sub{{display:none}}
  .cnt{{padding:22px 26px}}
  .row{{margin-bottom:14px}}
  .key{{color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:.04em}}
  .val{{font-size:15px;margin-top:2px}}
  .blockq{{background:#f8f9fa;border-left:5px solid #e11d48;margin:16px 0;padding:14px 16px;color:#374151}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}} .pair span{{color:#6b7280}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;padding:6px 10px;font-size:12px;background:#f9fafb}}
  .cta{{margin:18px 0}} .btn{{display:inline-block;text-decoration:none;background:#e11d48;color:#fff;border-radius:10px;padding:10px 16px;font-weight:bold}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;padding:16px}}
  @media (max-width:520px){{ .hdr h1{{font-size:18px}} .titulo{{flex:1 1 100%}} }}
</style>
</head>
<body>
  <div class='wrap'>
    <div class='hdr'>
      <h1>
        <span class='ticket'>Ticket #<strong>{id}</strong></span>
        <span class='dash'>—</span>
        <span class='titulo'><strong>{tituloEnc}</strong></span>
        <span class='badge-new'>Estado: {estadoEnc}</span>
       </h1>
    </div>

    <div class='cnt'>
      <div class='kpis'>
        <div class='pill'>Tipo de caso: <strong>{tipoCasoEnc}</strong></div>
        <div class='pill'>Prioridad: <strong>{prioridadEnc}</strong></div>
        <div class='pill'>Creación: <strong>{fCreTxt}</strong></div>
      </div>

      <table class='sum' role='presentation' aria-hidden='true'>
        <tr>
          <th>Reportado</th>
          <td>{descripcionEnc}</td>
        </tr>
        <tr>
          <th>Mensaje para Soporte</th>
          <td>
            <div class='blockq'>
              Se ha creado un nuevo caso por <strong>{nomAutorEnc}</strong>. Por favor, revisar y asignar para su atención.
            </div>
          </td>
        </tr>
      </table>

      <div class='grid'>
        <div class='card'>
          <h3>Solicitante</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomAutorEnc}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAutorEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAutorEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAutorEnc}</div>
          <div class='pair'><span>Correo:&nbsp;</span><strong>{mailAutorEnc}</strong></div>
        </div>

        <div class='card'>
          <h3>Colaborador afectado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomAfectadoEnc}-{caso.carnetResponsable}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAfectadoEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAfectadoEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAfectadoEnc}</div>
        </div>
      </div>
    </div>

    <div class='ftr'>
      Mensaje automático para Soporte. No responder a este correo.
    </div>
  </div>
</body>
</html>";

            int idcorreo = 0;
            string m = EncodeHtmlToNumeric(mensaje);
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    var correoCasoId = db.ExecuteScalar<int>(
                        "dbo.usp_CorreoCaso_Insert",
                        new
                        {
                            CasoID = id,
                            TipoCaso = caso.TipoCaso,   // ej. "Incidente", "Requerimiento", etc.
                        Estado = "Abierto",              // o el que aplique
                        Asunto = titulo1,
                            ContenidoHtml = m                   // ← HTML en entidades numéricas
                    },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    idcorreo = correoCasoId;
                    System.Diagnostics.Debug.WriteLine($"CorreoCasoID insertado: {correoCasoId}");
                }
            }catch(Exception e)
            {

                return e.Message;
            }
            string correo= getcorreohelpapi("gustavo.lira@claro.com.ni", "candida.sanchez@claro.com.ni", titulo1, idcorreo+"", id);

            string contenidoMensaje = $"Nuevo ticket {id}: {tituloEnc}. Afectado: {nomAfectadoEnc}-{caso.carnetResponsable}. Indicente: {descripcionEnc}";
            contenidoMensaje = contenidoMensaje.Replace("\r", " ").Replace("\n", " ").Trim();
            // cadena de conexión
            string cs = "Server=192.168.8.234;Database=MensajeBD;User Id=sarh;Password=ktSrW2n_4pR7;";

                    using (SqlConnection conn = new SqlConnection(cs))
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_InsertarWhatsAppDM", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Telefono",  "86219104");
                            cmd.Parameters.AddWithValue("@ContenidoMensaje", contenidoMensaje);
                            cmd.Parameters.AddWithValue("@Tipo", "Helpdesk");
                    cmd.Parameters.AddWithValue("@IdCita", id.ToString());
                    cmd.Parameters.AddWithValue("@Unidad", "N/A");
                    var pRegistrado = cmd.Parameters.Add("@EsRegistrado", SqlDbType.Bit);
                            pRegistrado.Direction = ParameterDirection.Output;

                            conn.Open();
                            cmd.ExecuteNonQuery();

                            bool registrado = (bool)pRegistrado.Value;      // true = DM, false = NoRegistrado

                             

                        }
                    }
          
       
            return "EXITO";
        }

        // PUT: Atender Caso - se consume el API REST en la ruta: api/helpdesk/atender?token=021092
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AtenderCaso(int id, string notasCierre, bool cerrarCaso)
        {
            try
            {
                long filas;
                using (var db = new SqlConnection(connectionString))
                {
                    filas = await db.ExecuteScalarAsync<long>(
                        "dbo.usp_Caso_Atender",
                        new { ID = id, NotasCierre = notasCierre, CerrarCaso = cerrarCaso },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                }
                if (filas <= 0) return Json(new { success = false, message = "Caso no encontrado o sin cambios." });

                // 📧 Notificación según estado
                try
                {
                    if (cerrarCaso)
                    {
                        // correo de cierre (usa solo ID + notas internas)
                          GenerarCorreoCierreCaso(id, "Cerrado"); // ↓ snippet más abajo
                    }
                    else
                    {
                        // correo "En Proceso" (usa solo ID)
                          GenerarCorreoCierreCaso(id, "Cerrado"); // ↓ snippet más abajo
                    }
                }
                catch { /* log opcional */ }

                return Json(new { success = true, message = "Caso actualizado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar el caso: " + ex.Message });
            }
        }

        // Controller: usa SP y dispara correo de cierre por ID
        // Requiere: using Dapper; using System.Data.SqlClient;
        [HttpPost]
        public async Task<JsonResult> AtenderCasocerrado(int CasoID, string NotasCierre)
        {
            try
            {
                var me = Session["User"] as Entities.Employees;
                if (me == null) return Json(new { success = false, message = "Sesión expirada." });

                long filas;
                using (var db = new SqlConnection(connectionString))
                {
                    filas = await db.ExecuteScalarAsync<long>(
                        "dbo.usp_Caso_Cerrar",
                        new { CasoID, NotasCierre, SoporteID = me.EmployeeNumber },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                }
                if (filas <= 0) return Json(new { success = false, message = "No se encontró el caso." });

                // 📧 Correo de cierre (solo ID)
                try {   GenerarCorreoCierreCaso(CasoID, "Cerrado"); } catch { /* log opcional */ }

                return Json(new { success = true, message = "Caso atendido correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar el caso: " + ex.Message });
            }
        }

        public async Task<IEnumerable<CasoView>> ObtenerCasosAsync()
        {
            var eEmployee = Session["User"] as Entities.Employees;

            using (var db = new SqlConnection(connectionString))
            {
                var casosx = db.Query<CasoView>(
                "dbo.usp_Casos_Listar_v2",
                new { Permitido = eEmployee.EmailAddress },
                commandType: System.Data.CommandType.StoredProcedure
            ).AsList();

                var lista = casosx.ToList();                 // cache en sesión
                Session["casos"] = lista;
                return lista;
            }
        }

        public CasoView ObtenerCasoPorId(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var caso = db.QueryFirstOrDefault<CasoView>(
                    "dbo.usp_CasosObtenerPorId",
                    new { Id = id },
                    commandType: System.Data.CommandType.StoredProcedure
                );
               
                return caso;
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RecategorizarCaso(int idCaso, int idTipo, int idSubtipo, string usuario)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    // SP que persiste recategorización + auditoría básica
                    var afectados = cn.Execute(
                        "usp_Helpdesk_Caso_Recategorizar",
                        new
                        {
                            IdCaso = idCaso,
                            IdTipo = idTipo,
                            IdSubtipo = idSubtipo,
                            Usuario = usuario   // ← usuario que ejecuta la acción
                    },
                        commandType: CommandType.StoredProcedure
                    );

                    return Json(new { ok = afectados > 0, afectados });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }


        // PUT: Marcar En Proceso - se consume el API REST en la ruta: api/helpdesk/proceso?token=021092
        [HttpGet]
        public JsonResult ObtenerTiposCasos()
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    // SP de catálogo de tipos
                    var tipos = cn.Query(
                        "usp_Helpdesk_Tipos_Listar",
                        commandType: CommandType.StoredProcedure
                    );

                    return Json(new { ok = true, data = tipos }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Respuesta controlada de error
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult ObtenerSubtiposPorTipo(int idTipo)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    // SP de subtipos filtrado por tipo
                    var subtipos = cn.Query(
                        "usp_Helpdesk_Subtipos_PorTipo",
                        new { IdTipo = idTipo },           // ← parámetros nombrados
                        commandType: CommandType.StoredProcedure
                    );

                    return Json(new { ok = true, data = subtipos }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public async Task<JsonResult> MarcarEnProceso(int id)
        {
            try
            {
                var me = Session["User"] as Entities.Employees;
                if (me == null) return Json(new { success = false, message = "Sesión expirada." });

                long filas;
                using (var db = new SqlConnection(connectionString))
                {
                    filas = await db.ExecuteScalarAsync<long>(
                        "dbo.usp_Caso_MarcarEnProceso",
                        new { ID = id, SoporteID = me.EmployeeNumber },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                }
                if (filas <= 0) return Json(new { success = false, message = "No se pudo marcar en proceso." });

                // 📧 Notificación "En Proceso" (solo ID)
                try {   GenerarCorreoUsuario(id, "En Proceso"); } catch { /* log opcional */ }

                return Json(new { success = true, message = "Caso pasó a 'En Proceso'." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        #endregion

        #region Operaciones de Lectura (Directas)
        public ActionResult Details(int id)
            {
                using (var db = new SqlConnection(connectionString))
                {
                    var caso = db.QueryFirstOrDefault<Caso>("SELECT * FROM Caso WHERE ID = @ID", new { ID = id });
                    if (caso == null)
                        return HttpNotFound();

                    var archivos = db.Query<Archivo>("SELECT * FROM Archivo WHERE CasoID = @CasoID", new { CasoID = id }).ToList();
                    if (archivos != null && archivos.Any())
                    {
                        var file = archivos.FirstOrDefault();
                        caso.data = ConvertToBase64(file.DatosArchivo);
                        caso.DatosArchivo = file.DatosArchivo;
                        caso.TipoArchivo = file.TipoArchivo;
                        caso.NombreArchivo = file.NombreArchivo;
                    }
                    return Json(caso, JsonRequestBehavior.AllowGet);
                }
            }
        #endregion

        public async Task<JsonResult> ObtenerCasos()
        {
            var casos = await ObtenerCasosAsync();
            return Json(casos, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> ObtenerCasosPaginados()
        {
            var casos = await ObtenerCasosPaginadosAsync();
            return Json(new { data = casos }, JsonRequestBehavior.AllowGet);
        }



        // 📊 Query enriquecida (igual que bin)

        public async Task<IEnumerable<CasoView>> ObtenerCasosPaginadosAsync()
        {
            var casos = await ObtenerCasosAsync();
            return casos; // ← retorno obligatorio
        }



        // ✉️ Plantillas correo (igual que bin)
        // Requiere: using System.Web; // HtmlEncode
        // Requiere: using System.Web; using Dapper; using System.Data.SqlClient;

        // ✅ Helper: obtener 1 caso por ID (mismos campos que tu listado)
        private async Task<CasoView> ObtenerCasoByIdAsync(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                return await db.QueryFirstOrDefaultAsync<CasoView>(
                    "dbo.usp_Caso_ObtenerPorId",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );
            }
        }


        // ✅ Actualiza Session["casos"] con el caso puntual
        private void UpsertCasoEnSesion(CasoView caso)
        {
            var lista = Session["casos"] as List<CasoView>;
            if (lista == null) { lista = new List<CasoView>(); }
            var idx = lista.FindIndex(x => x.ID == caso.ID);
            if (idx >= 0) lista[idx] = caso; else lista.Add(caso);
            Session["casos"] = lista;
        }

        // 📧 Correo a SOPORTE: aviso de creación de caso (mismo estilo visual + botón “Revisar ahora”)
        public async Task<string> GenerarCorreoCreacionCasoSoporteAsync(
            string nombreUsuario, string titulo, int id, string toSoporte, string ccAutor, string urlDetalle)
        {
            // 1) Obtener el caso puntual y refrescar sesión
            var caso = await ObtenerCasoByIdAsync(id);
            if (caso != null) UpsertCasoEnSesion(caso);

            // 2) Mapear/encode (valores seguros)
            string nombreUsuarioEnc = HttpUtility.HtmlEncode(nombreUsuario ?? "-"); // Solicitante (quien crea)
            string tituloEnc = HttpUtility.HtmlEncode(titulo ?? (caso?.Titulo ?? "-"));
            string estadoEnc = HttpUtility.HtmlEncode(caso?.Estado ?? "Abierto");
            string tipoCasoEnc = HttpUtility.HtmlEncode(caso?.TipoCaso ?? "-");
            string prioridadEnc = HttpUtility.HtmlEncode(caso?.Prioridad ?? "-");
            string descripcionEnc = HttpUtility.HtmlEncode(caso?.Descripcion ?? "-");

            DateTime fcre = caso?.FechaCreacion ?? DateTime.Now;
            string fCreTxt = fcre.ToString("dd/MM/yyyy HH:mm");

            // Solicitante (autor)
            string nomAutorEnc = HttpUtility.HtmlEncode(caso?.NombreAutor ?? nombreUsuario);
            string cargoAutorEnc = HttpUtility.HtmlEncode(caso?.CargoAutor ?? "-");
            string areaAutorEnc = HttpUtility.HtmlEncode(caso?.AreaAutor ?? "-");
            string telAutorEnc = HttpUtility.HtmlEncode(caso?.TelefonoAutor ?? "-");
            string mailAutorEnc = HttpUtility.HtmlEncode(caso?.CorreoAutor ?? "-");

            // Colaborador afectado
            string nomAfectadoEnc = HttpUtility.HtmlEncode(caso?.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(caso?.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(caso?.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(caso?.TelefonoResponsable ?? "-");

            // Soporte asignado (probablemente vacío al crear)
            string nomSoporteEnc = HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(caso?.Nombresoport) ? "Pendiente de asignación" : caso.Nombresoport);
            string cargoSoporteEnc = HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(caso?.Cargosoport) ? "-" : caso.Cargosoport);
            string areaSoporteEnc = HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(caso?.Areasoport) ? "-" : caso.Areasoport);
            string telSoporteEnc = HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(caso?.Telefonosoport) ? "-" : caso.Telefonosoport);

            string urlDetalleEnc = HttpUtility.HtmlEncode(urlDetalle ?? "#");

            // 3) Asunto
            string titulo1 = $"Helpdesk Tick-{id}: Caso Creado - {tituloEnc}";

            // 4) HTML (coherente con los otros correos)
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#e11d48;color:#fff;padding:22px 26px}}
  .hdr h1{{margin:0;font-size:22px;line-height:1.2}}
  .sub{{font-size:12px;opacity:.9;margin-top:6px}}
  .badge-new{{display:inline-block;background:#eef2ff;color:#1e1b4b;border:1px solid #c7d2fe;padding:4px 10px;border-radius:999px;font-size:12px;margin-left:10px}}
  .cnt{{padding:22px 26px}}
  .row{{margin-bottom:14px}}
  .key{{color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:.04em}}
  .val{{font-size:15px;margin-top:2px}}
  .chip{{display:inline-block;background:#fee2e2;color:#7f1d1d;border:1px solid #fecaca;padding:4px 10px;border-radius:999px;font-size:12px}}
  .blockq{{background:#f8f9fa;border-left:5px solid #e11d48;margin:16px 0;padding:14px 16px;color:#374151}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}}
  .pair span{{color:#6b7280}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;padding:6px 10px;font-size:12px;background:#f9fafb}}
  .cta{{margin:18px 0}}
  .btn{{display:inline-block;text-decoration:none;background:#e11d48;color:#fff;border-radius:10px;padding:10px 16px;font-weight:bold}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;padding:16px}}
</style>
</head>
<body>
  <div class='wrap'>
    <div class='hdr'>
      <h1>Nuevo Caso Creado <span class='badge-new'>Estado: {estadoEnc}</span></h1>
      <div class='sub'>Ticket #{id} • Creado: {fCreTxt}</div>
    </div>

    <div class='cnt'>
      <div class='row'>
        <div class='key'>Título</div>
        <div class='val'><strong>{tituloEnc}</strong></div>
      </div>

      <div class='kpis'>
        <div class='pill'>Tipo de caso: <strong>{tipoCasoEnc}</strong></div>
        <div class='pill'>Prioridad: <strong>{prioridadEnc}</strong></div>
        <div class='pill'>Creación: <strong>{fCreTxt}</strong></div>
      </div>

      <table class='sum' role='presentation' aria-hidden='true'>
        <tr>
          <th>Reportado</th>
          <td>{descripcionEnc}</td>
        </tr>
        <tr>
          <th>Mensaje para Soporte</th>
          <td>
            <div class='blockq'>
              Se ha creado un nuevo caso por <strong>{nomAutorEnc}</strong>. Por favor, revisar y asignar para su atención.
            </div>
          </td>
        </tr>
      </table>

      <div class='grid'>
        <div class='card'>
          <h3>Solicitante</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomAutorEnc}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAutorEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAutorEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAutorEnc}</div>
          <div class='pair'><span>Correo:&nbsp;</span><strong>{mailAutorEnc}</strong></div>
        </div>

        <div class='card'>
          <h3>Colaborador afectado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomAfectadoEnc}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAfectadoEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAfectadoEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAfectadoEnc}</div>
        </div>

        <div class='card'>
          <h3>Soporte asignado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomSoporteEnc}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoSoporteEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaSoporteEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telSoporteEnc}</div>
        </div>
      </div>

      <div class='cta'>
        <a class='btn' href='{urlDetalleEnc}'>Revisar ahora</a>
      </div>
    </div>

    <div class='ftr'>
      Mensaje automático para Soporte. No responder a este correo.
    </div>
  </div>
</body>
</html>";

            // 5) Enviar (mantén tu flujo: destinatario soporte, cc autor si aplica)
            return getcorreohelp(toSoporte, ccAutor, titulo1, mensaje, id);
        }

        public string GenerarCorreoUsuario( int id,string estado )
        {
            // 1) Obtener caso desde la sesión
            //List<CasoView> casosx = (List<CasoView>)Session["casos"];
            var micasoatender = ObtenerCasoPorId(id);

            // 2) Sanitizar (solicitante y campos del caso)
            string nombreUsuarioEnc = HttpUtility.HtmlEncode(micasoatender.NombreAutor   ?? "-");          // Solicitante (quien creó el ticket)
             string tituloEnc = HttpUtility.HtmlEncode(micasoatender.Titulo ?? "-");

            string descripcionEnc = HttpUtility.HtmlEncode(micasoatender?.Descripcion ?? "-");
            string tipoCasoEnc = HttpUtility.HtmlEncode(micasoatender?.TipoCaso ?? "-");

            DateTime inicio = micasoatender?.FechaCreacion ?? DateTime.Now;
            DateTime ahora = DateTime.Now;
            if (ahora < inicio) ahora = inicio; // seguridad
            string fIniTxt = inicio.ToString("dd/MM/yyyy HH:mm");
            string fActTxt = ahora.ToString("dd/MM/yyyy HH:mm");
            TimeSpan ts = ahora - inicio;
            string duracionTxt = $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";

            // 3) Colaborador afectado (de su equipo)
            string nomAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.TelefonoResponsable ?? "-");

            // 4) Soporte que atiende (detalle del agente)
            string nomSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Nombresoport ?? "Mesa de Ayuda");
            string cargoSoporteDet = HttpUtility.HtmlEncode(micasoatender?.Cargosoport ?? "-");
            string areaSoporteDet = HttpUtility.HtmlEncode(micasoatender?.Areasoport ?? "-");
            string telSoporteDet = HttpUtility.HtmlEncode(micasoatender?.Telefonosoport ?? "-");

            // 5) Asunto
            string titulo1 = $"Helpdesk Tick-{id}: {tituloEnc}";
            string titulo2 = EncodeHtmlToNumeric(titulo1);
           
            var a = micasoatender
            ;

            // 6) HTML (paleta rojo/blanco/gris; estado EN PROCESO con badge ámbar)
            // Header compacto: Ticket #, Título y Última actualización dentro del <h1>
            // Tema “silver/neutral” (sin azul) para estado EN PROCESO
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#9ca3af;color:#111;padding:22px 26px}} /* silver */
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;gap:8px;align-items:center}}
  .ticket,.titulo,.act{{white-space:nowrap}}
  .dash{{opacity:.8}}
  .badge-prog{{display:inline-block;background:#f3f4f6;color:#374151;border:1px solid #d1d5db;padding:4px 10px;border-radius:999px;font-size:12px}} /* neutral */
  .sub{{display:none}}
  .cnt{{padding:22px 26px}}
  .row{{margin-bottom:14px}}
  .key{{color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:.04em}}
  .val{{font-size:15px;margin-top:2px}}
  .blockq{{background:#f8f9fa;border-left:5px solid #9ca3af;margin:16px 0;padding:14px 16px;color:#374151}} /* borde silver */
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}} .pair span{{color:#6b7280}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;padding:6px 10px;font-size:12px;background:#f3f4f6;color:#374151}} /* neutral */
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;padding:16px}}
  @media (max-width:520px){{ .hdr h1{{font-size:18px}} .titulo{{flex:1 1 100%}} }}
</style>
</head>
<body>
  <div class='wrap'>
    <div class='hdr'>
      <h1>
        <span class='ticket'>Ticket #<strong>{id}</strong></span>
        <span class='dash'>—</span>
        <span class='titulo'><strong>{tituloEnc}</strong></span>
        <span class='badge-prog'>Estado: {micasoatender.Estado}</span>
       </h1>
    </div>

    <div class='cnt'>
      <div class='kpis'>
        <div class='pill'>Tipo de caso: <strong>{tipoCasoEnc}</strong></div>
        <div class='pill'>Creación: <strong>{fIniTxt}</strong></div>
        <div class='pill'>Actualización: <strong>{fActTxt}</strong></div>
        <div class='pill'>Duración: <strong>{duracionTxt}</strong></div>
      </div>

      <table class='sum' role='presentation' aria-hidden='true'>
        <tr>
          <th>Reportado</th>
          <td>{descripcionEnc}</td>
        </tr>
        <tr>
          <th>Mensaje</th>
          <td><div class='blockq'>
            Estimado(a) <strong>{nombreUsuarioEnc}</strong>, su caso se encuentra <strong>En Proceso</strong>. Nuestro equipo está trabajando para resolverlo a la brevedad.
          </div></td>
        </tr>
      </table>

      <div class='grid'>
        <div class='card'>
          <h3>Solicitante</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nombreUsuarioEnc}</strong></div>
        </div>

        <div class='card'>
          <h3>Colaborador afectado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomAfectadoEnc}-{micasoatender.carnetResponsable}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAfectadoEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAfectadoEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAfectadoEnc}</div>
        </div>

        <div class='card'>
          <h3>Soporte asignado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomSoporteEnc}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoSoporteDet}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaSoporteDet}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telSoporteDet}</div>
        </div>
      </div>
    </div>

    <div class='ftr'>
      Mensaje automático, no responder.
    </div>
  </div>
</body>
</html>";


            int idcorreo = 0;
            string m = EncodeHtmlToNumeric(mensaje);
            using (var db = new SqlConnection(connectionString))
            {
                var correoCasoId = db.ExecuteScalar<int>(
                    "dbo.usp_CorreoCaso_Insert",
                    new
                    {
                        CasoID = id,
                        TipoCaso = micasoatender.TipoCaso,   // ej. "Incidente", "Requerimiento", etc.
                        Estado = estado,              // o el que aplique
                        Asunto = titulo1,
                        ContenidoHtml = m                   // ← HTML en entidades numéricas
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                idcorreo = correoCasoId;
                System.Diagnostics.Debug.WriteLine($"CorreoCasoID insertado: {correoCasoId}");
            }
            // 7) Enviar con tu flujo actual
            return getcorreohelpapi("", "", titulo1, mensaje, id);
        }
        public static string EncodeHtmlToNumeric(string html)
        {
            // Convertir el HTML a bytes
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            // Comprimir los bytes para reducir tamaño
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                byte[] compressed = ms.ToArray();
                // Para asegurar que BigInteger sea positivo, agregamos un byte nulo
                byte[] positive = compressed.Concat(new byte[] { 0 }).ToArray();
                BigInteger bigInt = new BigInteger(positive);
                return bigInt.ToString(); // Representación decimal: solo dígitos
            }
        }
        public string getcorreohelpapi(string correo, string copia, string titulo, string mensaje, int id)
        {
            string output = null;
            try
            {

                string apiUrl = "http://172.26.54.66/apihcm/api/values/correo/correohelpdesk2025?correo=" + "1" + "&titulo=" + titulo + "&destinatarioCopia=" + "prueba" + "&mensaje=" + id;
                string mensaje3 = "";
                var client = new RestClient(apiUrl);
                client.Timeout = -1;  
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                Console.WriteLine(response.Content);
               mensaje3 = response.Content;
                if (mensaje3 != null && mensaje3 != "")
                { }
                output = mensaje3;
            }
            catch (Exception e) { output = "no se envio :" + e.Message; }
            if (output.Contains("EXITO") == true)
            { return "EXITO"; }
            return output;
             
            //// email.To.Add("gustavo.lira@claro.com.ni");s

            //email.To.Add(correo);


            //if (id > 0)
            //{
            //    List<CasoView> casosx = new List<CasoView>();
            //    casosx = (List<CasoView>)Session["casos"];
            //    var a = casosx.Where(x => x.ID == id).FirstOrDefault();
            //    if (correo != a.CorreoAutor)
            //    {
            //        email.To.Add(a.CorreoAutor);
            //    }

          
        }
        public string getcorreohelp(string correo, string copia, string titulo, string mensaje,int id)
        {
            
            string output = null;
            MailMessage email = new MailMessage();
            //// email.To.Add("gustavo.lira@claro.com.ni");s

            //email.To.Add(correo);


            //if (id > 0)
            //{
            //    List<CasoView> casosx = new List<CasoView>();
            //    casosx = (List<CasoView>)Session["casos"];
            //    var a = casosx.Where(x => x.ID == id).FirstOrDefault();
            //    if (correo != a.CorreoAutor)
            //    {
            //        email.To.Add(a.CorreoAutor);
            //    }

            //}
            email.From = new MailAddress("Rhonline.helpdeks@claro.com.ni");
            email.Subject = titulo;
            email.SubjectEncoding = System.Text.Encoding.UTF8;
            //email.Bcc.Add(destinatarioCopia);
            email.To.Add("gustavo.lira@claro.com.ni");
          


            email.Body = mensaje;
            email.BodyEncoding = System.Text.Encoding.UTF8;
            email.IsBodyHtml = true;
            email.Priority = MailPriority.Normal;




            ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
            SmtpClient cliente = new SmtpClient("10.200.5.23", 587); // IP y puerto de FortiMail
            cliente.Credentials = new NetworkCredential("recursoshumanos@claro.com.ni", "Enero&272025"); // Eliminar antes de producción
                                                                                                         //cliente.Credentials = new NetworkCredential("transporte@claro.com.ni", "Enero&r546"); // Eliminar antes de producción
            cliente.EnableSsl = true;


            try
            {
                cliente.Send(email);
                email.Dispose();
                output = "EXITO";
            }
            catch (Exception ex)
            {
                output = ex.InnerException.Message;
            }
            return output;
        }
        public string GenerarCorreoCreacionCaso(string nombreUsuario, string titulo, int id, string correoUsuario, string correoResponsable)
            {
                string titulo1 = "Helpdesk Tick-" + id + ": Caso Creado - " + titulo;
                string mensaje = $@"<html><head><style>body{{font-family:Arial;color:#333;background:#f4f4f9;margin:0;padding:20px;}}
.container{{background:#fff;padding:20px;border-radius:5px;box-shadow:0 0 10px rgba(0,0,0,.1);width:600px;margin:auto}}
h2{{color:#d32f2f;text-align:center}}.footer{{margin-top:20px;text-align:center;font-size:.9em;color:#777}}
.signature{{border-top:1px solid #ccc;padding-top:15px;margin-top:15px;text-align:center;color:#333}}.signature p{{margin:0;font-size:.85em}}</style></head>
<body><div class='container'><h2>Su Caso ha sido Creado</h2>
<p>Estimado(a) <strong>{nombreUsuario}</strong>,</p>
<p>Su caso <strong>{titulo}</strong> fue creado con ticket <strong>{id}</strong>.</p>
<p>Atentamente,<br><strong>Equipo de Soporte a la operación</strong></p>
<div class='signature'><p>Soporte a la operación</p><p>Tel: <strong>22745505</strong> / <strong>2274510</strong></p><p>Correo: <strong>soporte.operacion@claro.com.ni</strong></p></div>
<div class='footer'><p>Mensaje automático, no responder.</p></div></div></body></html>";
                return getcorreohelp(correoUsuario, correoResponsable, titulo1, mensaje,id);
            }

            public string GenerarCorreoCierreCaso( int id,string estado)
            {
            // ⚠️ Requiere: using System.Web;  // Para HtmlEncode

            // 1) Obtener el caso desde sesión (como indicaste)
            // Requiere: using System.Web;  // HtmlEncode
            // Snippet: correo de cierre IMPACTANTE con secciones Solicitante, Colaborador afectado y Soporte

            // 1) Cargar caso de sesión
             var micasoatender =  ObtenerCasoPorId(id); 

            // 2) Mapear / sanitizar
             string notasCierreEnc = HttpUtility.HtmlEncode(micasoatender.NotasCierre ?? "-");
             string tituloEnc = HttpUtility.HtmlEncode(micasoatender.Titulo ?? "-");

            string descripcionEnc = HttpUtility.HtmlEncode(micasoatender?.Descripcion ?? "-");
            string tipoCasoEnc = HttpUtility.HtmlEncode(micasoatender?.TipoCaso ?? "-");

            DateTime inicio = micasoatender?.FechaCreacion ?? DateTime.Now;
            DateTime fin = micasoatender?.FechaFinalizacion ?? DateTime.Now;
            if (fin < inicio) fin = inicio; // seguridad
            string fIniTxt = inicio.ToString("dd/MM/yyyy HH:mm");
            string fFinTxt = fin.ToString("dd/MM/yyyy HH:mm");
            TimeSpan ts = fin - inicio;
            string duracionTxt = $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";

            // Colaborador afectado (de su equipo)
            string nombreUsuarioEnc = HttpUtility.HtmlEncode(micasoatender?.NombreAutor ?? "-");
            string nomAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.TelefonoResponsable ?? "-");

            // Soporte que atendió el caso
            string nomSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Nombresoport ?? "Mesa de Ayuda");
            string cargoSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Cargosoport ?? "-");
            string areaSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Areasoport ?? "-");
            string telSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Telefonosoport ?? "-");
 
            // 3) Asunto
            string titulo1 = $"Helpdesk Tick-{id}: {tituloEnc}";

            // 4) HTML (paleta rojo/blanco/gris + chip verde pastel). Nota: CSS con llaves dobles {{ }}
            // Header muestra Ticket #, Título y Cierre en el <h1>
            // Versión en verde (header y acentos). Cambié solo colores clave.
            // - Header: #16a34a (verde)
            // - Borde de bloque (blockq): #16a34a
            // - Badge ok ya era verde; se mantiene.
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#16a34a;color:#fff;padding:22px 26px}} /* verde */
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;gap:8px;align-items:center}}
  .ticket,.titulo,.cierre{{white-space:nowrap}}
  .dash{{opacity:.8}}
  .badge-ok{{display:inline-block;background:#e8f7ee;color:#14532d;border:1px solid #c6f1d5;padding:4px 10px;border-radius:999px;font-size:12px}}
  .sub{{font-size:12px;opacity:.9;margin-top:6px;display:none}}
  .cnt{{padding:22px 26px}}
  .row{{margin-bottom:14px}}
  .key{{color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:.04em}}
  .val{{font-size:15px;margin-top:2px}}
  .chip{{display:inline-block;background:#ecfdf5;color:#064e3b;border:1px solid #a7f3d0;padding:4px 10px;border-radius:999px;font-size:12px}} /* chip tono verde suave */
  .blockq{{background:#f8f9fa;border-left:5px solid #16a34a;margin:16px 0;padding:14px 16px;color:#374151}} /* borde verde */
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}}
  .pair span{{color:#6b7280}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;padding:6px 10px;font-size:12px;background:#f9fafb}}
  .sig{{border-top:1px solid #e5e7eb;margin-top:18px;padding-top:14px;font-size:13px;color:#111}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;padding:16px}}
  @media (max-width:520px){{ .hdr h1{{font-size:18px}} .titulo{{flex:1 1 100%}} }}
</style>
</head>
<body>
  <div class='wrap'>
    <div class='hdr'>
      <h1>
        <span class='ticket'>Ticket #<strong>{id}</strong></span>
        <span class='dash'>—</span>
        <span class='titulo'><strong>{tituloEnc}</strong></span>
        <span class='badge-ok'>Estado: {micasoatender.Estado}</span>
       </h1>
    </div>

    <div class='cnt'>
      <div class='kpis'>
        <div class='pill'>Tipo de caso: <strong>{tipoCasoEnc}</strong></div>
        <div class='pill'>Creación: <strong>{fIniTxt}</strong></div>
        <div class='pill'>Cierre: <strong>{fFinTxt}</strong></div>
        <div class='pill'>Duración: <strong>{duracionTxt}</strong></div>
      </div>

      <table class='sum' role='presentation' aria-hidden='true'>
        <tr>
          <th>Reportado</th>
          <td>{descripcionEnc}</td>
        </tr>
        <tr>
          <th>Notas de cierre</th>
          <td><div class='blockq'>{notasCierreEnc}</div></td>
        </tr>
      </table>

      <div class='grid'>
        <div class='card'>
          <h3>Solicitante</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nombreUsuarioEnc}</strong></div>
        </div>

        <div class='card'>
          <h3>Colaborador afectado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomAfectadoEnc}-{micasoatender.carnetResponsable}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAfectadoEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAfectadoEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAfectadoEnc}</div>
        </div>

        <div class='card'>
          <h3>Soporte que atendió</h3>
          <div class='pair'><span>Nombre:&nbsp;</span><strong>{nomSoporteEnc}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoSoporteEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaSoporteEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telSoporteEnc}</div>
        </div>
      </div>

      <div class='sig'>
        Ticket cerrado automáticamente. Si el inconveniente persiste, por favor cree un nuevo ticket desde el portal.
      </div>
    </div>

    <div class='ftr'>
      Mensaje automático, no responder.
    </div>
  </div>
</body>
</html>";

            int idcorreo = 0;
            string m = EncodeHtmlToNumeric(mensaje);
            using (var db = new SqlConnection(connectionString))
            {
                var correoCasoId = db.ExecuteScalar<int>(
                    "dbo.usp_CorreoCaso_Insert",
                    new
                    {
                        CasoID = id,
                        TipoCaso = micasoatender.TipoCaso,   // ej. "Incidente", "Requerimiento", etc.
                        Estado =  estado,              // o el que aplique
                        Asunto = titulo1,
                        ContenidoHtml = m                   // ← HTML en entidades numéricas
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                idcorreo = correoCasoId;
                System.Diagnostics.Debug.WriteLine($"CorreoCasoID insertado: {correoCasoId}");
            }
            // 5) Envío (tu flujo actual)
            return getcorreohelpapi("", "", titulo1, mensaje, id);


            // 5) Enviar (manteniendo tu flujo actual)
            //return getcorreohelp("", "", titulo1, mensaje, id);


            //            string titulo1 = "Helpdesk Tick-" + id + ": " + titulo;
            //                string mensaje = @"<html><head><style>body{font-family:Arial;color:#333;background:#f4f4f9;margin:0;padding:20px;}
            //.container{background:#fff;padding:20px;border-radius:5px;box-shadow:0 0 10px rgba(0,0,0,.1);width:600px;margin:auto}
            //h2{color:#d32f2f;text-align:center}blockquote{background:#f8f9fa;padding:15px;border-left:5px solid #d32f2f;color:#555;font-style:italic}
            //.footer{margin-top:20px;text-align:center;font-size:.9em;color:#777}.signature{border-top:1px solid #ccc;padding-top:15px;margin-top:15px;text-align:center;color:#333}
            //.signature p{margin:0;font-size:.85em}</style></head><body><div class='container'><h2>Caso Cerrado Exitosamente</h2>
            //<p>Estimado(a) <strong>" + nombreUsuario + @"</strong>,</p>
            //<p>Su caso fue <strong>cerrado</strong>. Nota final:</p><blockquote>" + notasCierre + @"</blockquote>
            //<p>Atendido por: <strong>" + correoSoporte + @"</strong></p>
            //<div class='signature'><p>Soporte a la operacion</p><p>Tel: <strong>22745505</strong> / <strong>2274510</strong></p><p>Correo: <strong>soporte.operacion@claro.com.ni</strong></p></div>
            //<div class='footer'><p>Mensaje automático, no responder.</p></div></div></body></html>";
            //                return getcorreohelp(c, c1, titulo1, mensaje,  id);
        }
    }
    } 
 
