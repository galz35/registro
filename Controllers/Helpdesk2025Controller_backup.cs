using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Dapper;
using ImageMagick;
using Newtonsoft.Json;
using RestSharp;
using slnRhonline.Models;   // Caso, Archivo, CasoView, etc.
using Entities;
using Employees = Entities.Employees;
using ClosedXML.Excel;

namespace slnRhonline.Controllers
{

    // GET: Helpdesk2025
    // GET: Helpdesk2025
    public class Helpdesk2025Controller : Controller
    {
        /* ===== Config ===== */
        public readonly string connectionString = "Data Source=192.168.8.234;Connection Timeout=60;Initial Catalog=Helpdesk2025;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;";
        public readonly string msgDbConn = "Server=192.168.8.234;Database=MensajeBD;User Id=sarh;Password=ktSrW2n_4pR7;"; // WhatsApp
        public readonly string csSigho = "Data Source=192.168.8.234;Initial Catalog=SIGHO1;User ID=sarh;Password=ktSrW2n_4pR7;MultipleActiveResultSets=True;Connection Timeout=60;";
        public readonly string csHelpdesk = "Data Source=192.168.8.234;Connection Timeout=60;Initial Catalog=Helpdesk2025;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;";

        private readonly string correoApiBase = "http://172.26.54.66/apihcm/api/values/correo/correohelpdesk2025";            // getcorreohelpapi
        private const string MAIL_FROM = "Rhonline.helpdeks@claro.com.ni";
        private const string MAIL_SMTP_HOST = "10.200.5.23";
        private const int MAIL_SMTP_PORT = 587;
        private const string MAIL_SMTP_USER = "recursoshumanos@claro.com.ni"; // ⚠️ retirar en prod
        private const string MAIL_SMTP_PASS = "Enero&272025";                 // ⚠️ retirar en prod
        private class TipoCasoDto { public int TipoCasoID { get; set; } public string Nombre { get; set; } }
        private class SubtipoCasoDto { public int SubtipoCasoID { get; set; } public int TipoCasoID { get; set; } public string Nombre { get; set; } }
        private class EmpDto { public string carnet { get; set; } public string nombre_completo { get; set; } public string correo { get; set; } }
        // ====== DTOs (sin dynamic) ======
        //[HttpPost]
        //[ValidateAntiForgeryToken] // Si tienes esto, el JS de arriba funcionará ahora
        //public JsonResult Admin_ToggleSoporte(string id, bool activo)
        //{
        //    // Tu lógica para actualizar el campo Bit/Bool en la base de datos
        //    // return Json(new { success = true });
        //}

        public class CasoDetalleDto
        {
            public int ID { get; set; }
            public DateTime? FechaCreacion { get; set; }
            public DateTime? FechaActualizacion { get; set; }
            public string UsuarioID { get; set; }        // carnet solicitante
            public string Descripcion { get; set; }
            public string Fgerencia { get; set; }
            public string Titulo { get; set; }
            public string Estado { get; set; }
            public string Prioridad { get; set; }
            public int? TipoCasoID { get; set; }
            public int? SubtipoCasoID { get; set; }
            public DateTime? FechaAtencion { get; set; }
            public DateTime? FechaFinalizacion { get; set; }
            public string NotasCierre { get; set; }
            public string SoporteID { get; set; }        // carnet soporte
            public string Correo { get; set; }           // afectado (correo)

            public string TipoNombre { get; set; }
            public string SubtipoNombre { get; set; }

            public string CorreoAutor { get; set; }
            public string NombreAutor { get; set; }
            public string CargoAutor { get; set; }
            public string AreaAutor { get; set; }
            public string TelefonoAutor { get; set; }
            public string GerenciaAutor { get; set; }
            public string UrlAutor { get; set; }

            public string NombreResponsable { get; set; }
            public string CargoResponsable { get; set; }
            public string AreaResponsable { get; set; }
            public string TelefonoResponsable { get; set; }
            public string CorreoResponsable { get; set; }
            public string GerenciaResponsable { get; set; }
            public string UrlResponsable { get; set; }

            public string Nombresoport { get; set; }
            public string Cargosoport { get; set; }
            public string Areasoport { get; set; }
            public string Telefonosoport { get; set; }
            public string CorreoSoport { get; set; }
            public string UrlSoport { get; set; }

            // Para el preview de adjunto
            public byte[] DatosArchivo { get; set; }
            public string TipoArchivo { get; set; }
            public string NombreArchivo { get; set; }
            public string data { get; set; } // base64 embebido
            public int? TiempoAtencionMinutos { get; set; }
            public string TiempoAtencion { get; set; }

            public string Departamento { get; set; }        // ← nuevo
            public string Edificio { get; set; }            // ← nuevo (NombreUbicacion)
            public int? CantidadAfectados { get; set; }     // ← nuevo
            public int? DiasCondicion { get; set; }         // ← nuevo
            public List<ArchivoDto> Adjuntos { get; set; } // NUEVO
        }
        /* ==========================================
   NUEVAS FUNCIONES: CHAT / NOTAS
   ========================================== */
        // ==== C# (ASP.NET MVC + Dapper) =====
        public class NotaDto
        {
            public int NotaID { get; set; }
            public int CasoID { get; set; }
            public string AutorNombre { get; set; }
            public string Mensaje { get; set; }
            public string FechaStr { get; set; }
            public bool EsInterna { get; set; }
        }

        [HttpPost]
        public ActionResult ObtenerNotas(int id)
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    var notas = db.Query<NotaDto>(
                        "sp_ObtenerNotasCaso",
                        new { CasoID = id },
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    return Json(new { success = true, data = notas });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerSubEstados()
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    const string sql = @"SELECT SubEstadoID, Nombre 
                                 FROM SubEstado 
                                 WHERE Activo = 1 
                                 ORDER BY Nombre";
                    var lista = db.Query<SubEstadoDto>(sql).ToList(); // mapeo tipado
                    return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult CrearSubEstado(string nombre)
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@Nombre", nombre);
                    p.Add("@NewID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    db.Execute("sp_AgregarSubEstadoRapido", p, commandType: CommandType.StoredProcedure);

                    int newId = p.Get<int>("@NewID");
                    return Json(new { success = true, id = newId });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public ActionResult ActualizarSubEstadoCaso(int casoId, int subEstadoId)
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    db.Execute("sp_ActualizarSubEstadoCaso",
                        new { CasoID = casoId, SubEstadoID = subEstadoId },
                        commandType: CommandType.StoredProcedure);
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [HttpPost]
        public ActionResult AgregarNotaUsuario(int casoId, string mensaje)
        {
            try
            {
                // IMPORTANTE: Aquí tomo el nombre de la sesión o identidad actual
                // Ajusta "Session["UserNombre"]" a como guardes tú el nombre del usuario logueado.
                string autor = "Usuario";
                if (Session["NombreUsuario"] != null) autor = Session["NombreUsuario"].ToString();
                else if (User.Identity.IsAuthenticated) autor = User.Identity.Name;

                using (var db = new SqlConnection(connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@CasoID", casoId);
                    p.Add("@AutorNombre", autor);
                    p.Add("@Mensaje", mensaje);
                    p.Add("@EsInterna", 0); // 0 porque es el usuario escribiendo

                    db.Execute("sp_AgregarNota", p, commandType: CommandType.StoredProcedure);

                    // Opcional: Aquí podrías llamar a tu SendMail para avisar al técnico
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // DTO simple para SP de notificación (ajusta a tus campos reales)
        public class NotificacionCasoDto
        {
            public int CasoID { get; set; }
            public string Titulo { get; set; }
            public string Estado { get; set; }
            public string SoporteNombre { get; set; }
            public string CorreoSoporte { get; set; }
            public string CorreoSolicitante { get; set; }
        }


        public class ArchivoDto
        {
            public int Id { get; set; }
            public int CasoID { get; set; }
            public string NombreArchivo { get; set; }
            public string TipoArchivo { get; set; }
            public string data { get; set; }
            public byte[] DatosArchivo { get; set; }
            public DateTime? FechaSubida { get; set; }
            public string UrlDescarga { get; internal set; }
        }

        public class PersonaDto
        {
            public string Carnet { get; set; }
            public string Correo { get; set; }
            public string Nombre { get; set; }
            public string Cargo { get; set; }
            public string Area { get; set; }
            public string Gerencia { get; set; }
            public string Telefono { get; set; }
            public string Url { get; set; }
            // public string JefeInmediato { get; set; } // si existe en SP
        }

        public sealed class AsignarCasoResult
        {
            public int Ok { get; set; }
            public string SoporteCarnet { get; set; }
        }

        /* ====== Vistas ====== */
        private string CurrentUserEmail()
        {
            // Ajusta a tu autenticación real
            // Si tienes Session["User"] con Employees:
            // var u = Session["User"] as Entities.Employees; return u?.EmailAddress;
            return User?.Identity?.Name ?? ""; // fallback
        }
        // Vista:   Casos (usuario final)
        public ActionResult MisCasos()
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return RedirectToAction("Login", "Account");

            // Empleados por gerencia -> se usa para datalist (lo recupera el endpoint ObtenerEmpleados)
            using (var db = new SqlConnection(connectionString))
            {
                var empleados = db.Query<emp2024>(
                    "dbo.usp_Empleados_PorGerencia",
                    new { Gerencia = u.GERENCIA },
                    commandType: CommandType.StoredProcedure
                ).ToList();
                Session["listempleadocaso"] = empleados;
            }

            // Casos del usuario (por Id o por Correo)
            using (var db = new SqlConnection(connectionString))
            {
                var casos = db.Query<Caso>(
                    "dbo.usp_Casos_PorUsuario",
                    new { UsuarioID = u.EmployeeNumber, Correo = u.EmailAddress },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                // Opcional: contadores en ViewBag (la vista no depende de esto)
                ViewBag.TotalCasos = casos.Count;
                ViewBag.Pendientes = casos.Count(c => c.Estado == "Pendiente");
                ViewBag.EnProceso = casos.Count(c => c.Estado == "En Proceso");
                ViewBag.Cerrados = casos.Count(c => c.Estado == "Cerrado");
                ViewBag.Cancelados = casos.Count(c => c.Estado == "Cancelado");

                return View(casos);
            }
        }
        [HttpGet]
        public ActionResult Soporte()
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpGet]
        public ActionResult MiSoporte()
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);

            using (var cn = new SqlConnection(csHelpdesk))
            {
                // SP debe devolver fila fuerte (no dynamic)
                var row = cn.QueryFirstOrDefault<SoporteYoDto>(
                    "dbo.usp_Soporte_ResolverPorCorreo3",
                    new { Correo = u.EmailAddress },
                    commandType: CommandType.StoredProcedure);

                if (row == null) row = new SoporteYoDto { EsAdmin = false, Carnet = null, SoporteID = u.EmailAddress, Email = u.EmailAddress, Nombre = u.FullName };

                return Json(new { success = true, data = row }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult MiSoporte2()
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null)
                return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);

            using (var cn = new SqlConnection(csHelpdesk))
            {
                var row = cn.QueryFirstOrDefault<SoporteYoDto2>(
                    "dbo.usp_Soporte_ResolverPorCorreo2",
                    new { Correo = u.EmailAddress },
                    commandType: CommandType.StoredProcedure);

                // row NO debería ser null ya, pero protejo igual
                if (row == null)
                {
                    row = new SoporteYoDto2
                    {
                        SoporteID = u.EmailAddress,
                        Nombre = u.FullName,
                        Correo = u.EmailAddress,
                        Area = "",
                        Activo = true,
                        EsAdmin = false,
                        Carnet = null,
                        EsJefe = false
                    };
                }

                return Json(new { success = true, data = row }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult ObtenerCasosSoporte()
        {
            try
            {
                var u = Session["User"] as Entities.Employees;
                if (u == null)
                    return Json(new { success = false, message = "Sesión expirada." },
                                JsonRequestBehavior.AllowGet);

                var correo = u.EmailAddress;

                using (var cn = new SqlConnection(csHelpdesk))
                {
                    cn.Open();
                    // IMPORTANTE: el SP debe devolver columnas con los nombres del POCO de arriba.
                    var data = cn.Query<CasoListadoDto>(
                        "dbo.usp_Casos_Listar_v6",
                        new { Correo = correo },
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message },
                            JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> AsignarCaso(int CasoID, string SoporteID, string Nota)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) { }

            using (var db = new SqlConnection(csHelpdesk))
            {
                AsignarCasoResult r = new AsignarCasoResult(); ;
                var p = new DynamicParameters();
                p.Add("@CasoID", CasoID);
                p.Add("@SoporteID", SoporteID);     // ← aquí sigues enviando el CORREO
                p.Add("@Nota", Nota);               // ← NUEVO en el SP
                p.Add("@UsuarioAccion", u.FullName);
                try
                {
                    r = await db.QuerySingleAsync<AsignarCasoResult>(
               "dbo.usp_Caso_Asignar",
               p,
               commandType: CommandType.StoredProcedure
           );

                    if (r.Ok == 1)
                    {


                        EnviarCorreoAsignacion(CasoID, SoporteID, Nota);

                        return Json(new { success = true, carnet = r.SoporteCarnet });
                    }
                    return Json(new { success = false, message = "No se pudo asignar (resultado inesperado)." });

                }
                catch (Exception e)
                {
                    return Json(new { success = false, message = e.Message });
                }
            }
        }

        [HttpGet]
        public JsonResult GetTiposCaso()
        {
            using (var db = new SqlConnection(connectionString))
            {
                var tipos = db.Query<TipoCasoDto>(
                    "SELECT TipoCasoID, Nombre FROM dbo.TipoCaso WHERE Activo=1 ORDER BY Nombre"
                ).ToList();
                return Json(new { success = true, data = tipos }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetSubtiposCaso(int tipoCasoId)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var subs = db.Query<SubtipoCasoDto>(
                    "SELECT SubtipoCasoID, TipoCasoID, Nombre FROM dbo.SubtipoCaso WHERE Activo=1 AND TipoCasoID=@tipo ORDER BY Nombre",
                    new { tipo = tipoCasoId }
                ).ToList();
                return Json(new { success = true, data = subs }, JsonRequestBehavior.AllowGet);
            }
        }

        // Vista: Soporte (board operativo)
        public ActionResult TodosLosCasos() => View();

        // Vista: Dashboard (reportería)
        public ActionResult Dashboard() => View();

        /* ====== Data Básica ====== */

        // DT para soporte (rápido)
        public JsonResult TodosLosCasosjson()
        {
            using (var db = new SqlConnection(connectionString))
            {
                var casos = db.Query<Caso>("SELECT * FROM dbo.Caso").ToList();
                return Json(new { data = casos }, JsonRequestBehavior.AllowGet);
            }
        }

        // Catálogo empleados (cache en sesión)
        public JsonResult ObtenerEmpleados()
        {
            var empleados = Session["listempleadocaso"] as List<emp2024>;
            if (empleados == null)
            {
                using (var db = new SqlConnection(connectionString))
                {
                    const string q = "SELECT carnet, nombre_completo, correo FROM SIGHO1.dbo.EMP2024";
                    empleados = db.Query<emp2024>(q).ToList();
                }
            }
            var dto = empleados.Select(e => new { e.carnet, e.nombre_completo, e.correo }).ToList();
            return Json(dto, JsonRequestBehavior.AllowGet);
        }

        // Catálogo Tipo
        public JsonResult ListarTipoCaso()
        {
            using (var db = new SqlConnection(connectionString))
            {
                var tipos = db.Query<dynamic>("dbo.usp_TipoCaso_Listar", commandType: CommandType.StoredProcedure);
                return Json(tipos, JsonRequestBehavior.AllowGet);
            }
        }

        // Catálogo Subtipo por Tipo
        public JsonResult ListarSubtipoPorTipo(int tipoCasoID)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var subs = db.Query<dynamic>(
                    "dbo.usp_SubtipoCaso_PorTipo",
                    new { TipoCasoID = tipoCasoID },
                    commandType: CommandType.StoredProcedure
                );
                return Json(subs, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult CasoDetalles(int id)
        {
            using (var cn = new SqlConnection(csHelpdesk))
            {
                var caso = cn.QueryFirstOrDefault<CasoRow>(
                    "dbo.usp_CasosObtenerPorId_v2",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);

                if (caso == null)
                    return Json(new { }, JsonRequestBehavior.AllowGet);

                // Adjuntos (igual que lo tenías)
                var file = cn.QueryFirstOrDefault<Archivo>(
                    "SELECT TOP (1) * FROM dbo.Archivo WHERE CasoID=@CasoID ORDER BY ID DESC",
                    new { CasoID = id });

                if (file != null && file.DatosArchivo != null)
                {
                    var base64 = Convert.ToBase64String(file.DatosArchivo);
                    // anexar shape dinámico mínimo (o agrega en CasoRow propiedades opcionales)
                    var dto = new
                    {
                        caso.ID,
                        caso.Titulo,
                        caso.Estado,
                        caso.Prioridad,
                        caso.FechaCreacion,
                        caso.TipoNombre,
                        caso.SubtipoNombre,
                        caso.NombreAutor,
                        caso.NombreResponsable,
                        caso.Nombresoport,
                        Descripcion = caso.Descripcion, // si tu CasoRow ya trae Descripcion, úsalo directo
                        data = base64,
                        NombreArchivo = file.NombreArchivo
                    };
                    return Json(dto, JsonRequestBehavior.AllowGet);
                }

                return Json(caso, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult ObtenerCasosPorEstado(string estado)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);

            using (var cn = new SqlConnection(csHelpdesk))
            {
                var filas = cn.Query<CasoRow>(
                //   "dbo.usp_Casos_ListarPorEstado_v2",
                "dbo.usp_Casos_ListarPorEstado_v4",
                new { Correo = u.EmailAddress, Estado = estado },
                commandType: CommandType.StoredProcedure).ToList();

                return Json(new { success = true, data = filas }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult KPIs()
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);

            using (var cn = new SqlConnection(csHelpdesk))
            {
                var k = cn.QueryFirstOrDefault<KpiDto>(
                    //"dbo.usp_Casos_KPIs",
                    "dbo.usp_Casos_KPIs_v2",
                    new { Correo = u.EmailAddress },
                    commandType: CommandType.StoredProcedure) ?? new KpiDto();

                return Json(new { success = true, data = k }, JsonRequestBehavior.AllowGet);
            }
        }
        /* ====== Crear Caso ====== */

        // Crear Caso (binder MVC + múltiples adjuntos → WEBP)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CrearCaso(Caso caso, IEnumerable<HttpPostedFileBase> archivos)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Error de validación." });

            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    caso.FechaCreacion = DateTime.Now;
                    caso.Estado = "Pendiente";

                    // Guardar en helpdesk1.dbo.Caso (usando campo Correo como afectado/ParaQuien)
                    const string ins =
@"INSERT INTO dbo.Caso (Correo, UsuarioID, Titulo, Descripcion, Estado, Prioridad, TipoCasoID, SubtipoCasoID, TipoCaso, FechaCreacion)
  VALUES (@Correo, @UsuarioID, @Titulo, @Descripcion, @Estado, @Prioridad, @TipoCasoID, @SubtipoCasoID, @TipoCaso, @FechaCreacion);
  SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int id = db.Query<int>(ins, caso).Single();

                    if (archivos != null)
                    {
                        foreach (var f in archivos)
                        {
                            if (f == null || f.ContentLength <= 0) continue;
                            var a = new Archivo
                            {
                                CasoID = id,
                                NombreArchivo = Path.GetFileName(f.FileName),
                                TipoArchivo = "image/webp",
                                DatosArchivo = ConvertToWebP(f),
                                FechaSubida = DateTime.Now
                            };
                            db.Execute(
@"INSERT INTO dbo.Archivo (CasoID,NombreArchivo,TipoArchivo,DatosArchivo,FechaSubida)
  VALUES (@CasoID,@NombreArchivo,@TipoArchivo,@DatosArchivo,@FechaSubida)", a);
                        }
                    }

                    // Correo a soporte (no bloquea)
                    try { _ = GenerarCorreoCreacionCasoSoporteAsync(id); } catch { }

                    return Json(new { success = true, id, message = "Caso creado." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpGet]
        public JsonResult ListarDepartamentos()
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    var rows = cn.Query<string>(
                        @"SELECT DISTINCT LTRIM(RTRIM(emp.Departamento)) AS Departamento
                  FROM SIGHO1.dbo.EMP2024 emp
                  WHERE ISNULL(LTRIM(RTRIM(emp.Departamento)),'') <> ''
                  ORDER BY Departamento");
                    return Json(new { success = true, data = rows }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ListarEdificios(string departamento)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    var rows = cn.Query<string>(
                        @"SELECT DISTINCT LTRIM(RTRIM(emp.Nombreubicacion)) AS NombreUbicacion
                  FROM SIGHO1.dbo.EMP2024 emp
                  WHERE ISNULL(LTRIM(RTRIM(emp.Departamento)),'') = ISNULL(@Departamento,'')
                    AND ISNULL(LTRIM(RTRIM(emp.Nombreubicacion)),'') <> ''
                  ORDER BY NombreUbicacion",
                        new { Departamento = departamento }
                    );
                    return Json(new { success = true, data = rows }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // Crear Caso (form no-strict + HTML en Descripcion)
        [HttpPost]
        [ValidateInput(false)]
        public async Task<JsonResult> CrearCasox()
        {
            try
            {
                var u = Session["User"] as Entities.Employees;
                if (u == null) return Json(new { success = false, message = "Sesión expirada." });

                string paraQuien = (Request.Form["ParaQuien"] ?? "").Trim();
                string titulo = (Request.Form["Titulo"] ?? "").Trim();
                string descripcion = Request.Unvalidated["Descripcion"] ?? "";
                string prioridad = (Request.Form["Prioridad"] ?? "Media").Trim();

                int tipoId = SafeInt(Request.Form["TipoCasoID"]);
                int subId = SafeInt(Request.Form["SubtipoCasoID"]);
                string tipoTexto = (Request.Form["TipoCaso"] ?? "").Trim();

                // HS opcional
                string departamento = (Request.Form["Departamento"] ?? "").Trim();
                string edificio = (Request.Form["Edificio"] ?? "").Trim();
                int? cantAfectados = string.IsNullOrWhiteSpace(Request.Form["CantidadAfectados"]) ? (int?)null : SafeInt(Request.Form["CantidadAfectados"]);
                int? diasCondicion = string.IsNullOrWhiteSpace(Request.Form["DiasCondicion"]) ? (int?)null : SafeInt(Request.Form["DiasCondicion"]);

                if (string.IsNullOrWhiteSpace(tipoTexto))
                {
                    using (var db = new SqlConnection(connectionString))
                    {
                        var t = await db.QueryFirstOrDefaultAsync<string>(
                            "SELECT Nombre FROM dbo.TipoCaso WHERE TipoCasoID=@i", new { i = tipoId });
                        var s = await db.QueryFirstOrDefaultAsync<string>(
                            "SELECT Nombre FROM dbo.SubtipoCaso WHERE SubtipoCasoID=@i", new { i = subId });
                        tipoTexto = (t ?? "-") + (string.IsNullOrWhiteSpace(s) ? "" : " - " + s);
                    }
                }
                bool esObrasCiviles = tipoTexto.IndexOf("obras civiles", StringComparison.OrdinalIgnoreCase) >= 0;
                if (esObrasCiviles && Request.Files.Count == 0)
                {
                    return Json(new { success = false, message = "Para los casos de Obras Civiles es obligatorio adjuntar evidencia (imágenes o documentos)." });
                }
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();
                    using (var tx = cn.BeginTransaction())
                    {
                        // 1) Crear caso
                        var p = new DynamicParameters();
                        p.Add("@Correo", paraQuien);
                        p.Add("@UsuarioID", u.EmployeeNumber);
                        p.Add("@Titulo", titulo);
                        p.Add("@Descripcion", descripcion);
                        p.Add("@Prioridad", prioridad);
                        p.Add("@TipoCaso", tipoTexto);
                        p.Add("@TipoCasoID", tipoId);
                        p.Add("@SubtipoCasoID", subId);

                        // HS opcional
                        p.Add("@Departamento", departamento);
                        p.Add("@Edificio", edificio);
                        p.Add("@CantidadAfectados", cantAfectados, DbType.Int32);
                        p.Add("@DiasCondicion", diasCondicion, DbType.Int32);

                        p.Add("@CasoID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                        await cn.ExecuteAsync("dbo.usp_Caso_Crearx", p, tx, commandType: CommandType.StoredProcedure);
                        var casoId = p.Get<int>("@CasoID");

                        // 2) Adjuntos (imágenes → WebP; documentos → binario crudo)
                        for (int i = 0; i < Request.Files.Count; i++)
                        {
                            var f = Request.Files[i];
                            if (f == null || f.ContentLength <= 0) continue;

                            // Nombre y MIME reportado por el navegador
                            var nombre = Path.GetFileName(f.FileName);
                            var mime = (f.ContentType ?? "").ToLowerInvariant();

                            // Normaliza MIME por extensión si viene vacío/genérico
                            if (string.IsNullOrEmpty(mime) || mime == "application/octet-stream")
                            {
                                var ext = (Path.GetExtension(nombre) ?? "").ToLowerInvariant();
                                mime = MapMimeByExtension(ext); // ↓ helper más abajo
                            }

                            byte[] datos;
                            string mimeFinal;
                            // Si es imagen → convertir a WebP (reduce peso y uniforma)
                            if (mime.StartsWith("image/"))
                            {
                                datos = ToWebP(f);          // compresión ~70% (tu helper existente)
                                mimeFinal = "image/webp";
                            }
                            else
                            {
                                // PDF/Word/Excel u otros → guardar crudo
                                datos = ReadFully(f.InputStream, f.ContentLength);
                                mimeFinal = string.IsNullOrEmpty(mime) ? "application/octet-stream" : mime;
                            }

                            var p2 = new DynamicParameters();
                            p2.Add("@CasoID", casoId);
                            p2.Add("@NombreArchivo", nombre);
                            p2.Add("@TipoArchivo", mimeFinal);
                            p2.Add("@DatosArchivo", datos, DbType.Binary);
                            p2.Add("@Tipo", "Reportan"); // etiqueta de origen

                            await cn.ExecuteScalarAsync<long>(
                                "dbo.usp_Archivo_Insert", p2, tx, commandType: CommandType.StoredProcedure);
                        }

                        tx.Commit();

                        // 3) Notificación (best effort)
                        try { await GenerarCorreoCreacionCasoSoporteAsync(casoId); } catch { }

                        return Json(new { success = true, id = casoId, message = "Caso creado exitosamente." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        // === Helpers =======================================================================
        // Lee todo el stream a byte[] (compatible .NET Framework / C# 7.3)
        private static byte[] ReadFully(Stream input, int contentLength)
        {
            if (contentLength > 0)
            {
                var buffer = new byte[contentLength];
                int read, offset = 0;
                while (offset < contentLength &&
                       (read = input.Read(buffer, offset, contentLength - offset)) > 0)
                {
                    offset += read;
                }
                return buffer;
            }
            // Fallback si no viene ContentLength
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        // Mapea MIME por extensión cuando el navegador no lo provee bien
        private static string MapMimeByExtension(string ext)
        {
            switch (ext)
            {
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".webp": return "image/webp";
                default: return "application/octet-stream";
            }
        }
        private static byte[] ToWebP(HttpPostedFileBase file)
        {
            using (var ms = new MemoryStream())
            {
                file.InputStream.CopyTo(ms);
                ms.Position = 0;
                using (var img = new MagickImage(ms))
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
        private static int SafeInt(string v)
        {
            int x;
            return int.TryParse((v ?? "").Trim(), out x) ? x : 0;
        }
        /* ====== Estados / Atención ====== */

        // En Proceso (marca con tu SP)
        [HttpPost]
        public async Task<JsonResult> MarcarEnProceso(int id)
        {
            try
            {
                var me = Session["User"] as Employees;
                if (me == null) return Json(new { success = false, message = "Sesión expirada." });

                long filas;
                using (var db = new SqlConnection(connectionString))
                {
                    filas = await db.ExecuteScalarAsync<long>(
                        "dbo.usp_Caso_MarcarEnProceso",
                        new { ID = id, SoporteID = me.EmployeeNumber },
                        commandType: CommandType.StoredProcedure
                    );
                }
                if (filas <= 0) return Json(new { success = false, message = "No se pudo marcar en proceso." });

                try { GenerarCorreoUsuario(id, "En Proceso"); } catch { }

                return Json(new { success = true, message = "En Proceso." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Atender caso (toggle En Proceso / Cerrar)
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
                        commandType: CommandType.StoredProcedure
                    );
                }
                if (filas <= 0) return Json(new { success = false, message = "Caso no encontrado o sin cambios." });

                try
                {
                    if (cerrarCaso) { GenerarCorreoCierreCaso(id, "Cerrado"); }
                    else { GenerarCorreoUsuario(id, "En Proceso"); }
                }
                catch { }

                return Json(new { success = true, message = "Actualizado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        // ASP.NET MVC (.NET Fx) — C# 7.3
        // Cerrar caso + adjunto opcional (imagen→WEBP | PDF sin tocar)
        [HttpPost]
        [ValidateInput(false)]
        public async Task<JsonResult> CerrarCasox(int id, string notasCierre)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada." });

            using (var db = new SqlConnection(csHelpdesk))
            {
                // (1) cerrar caso
                var filas = await db.ExecuteScalarAsync<long>(
                    "dbo.usp_Caso_Cerrar",
                    new { CasoID = id, NotasCierre = notasCierre ?? "", SoporteID = u.EmployeeNumber },
                    commandType: CommandType.StoredProcedure
                );
                if (filas <= 0) return Json(new { success = false, message = "No se pudo cerrar el caso." });

                // (2) correo de cierre (best-effort)
                try { GenerarCorreoCierreCaso(id, "Cerrado"); } catch { /* log opcional */ }

                // (3) adjunto opcional (solo 1er archivo como antes)
                if (Request.Files.Count > 0)
                {
                    var f = Request.Files[0];
                    if (f != null && f.ContentLength > 0)
                    {
                        // --- MIME permitido (img o pdf) ---
                        var mime = (f.ContentType ?? string.Empty).ToLowerInvariant();
                        var ext = (Path.GetExtension(f.FileName) ?? string.Empty).ToLowerInvariant();

                        var esPdf = mime == "application/pdf" || ext == ".pdf";
                        var esImagen = IsImagenMime(mime, ext); // jpg/png/webp

                        if (!esPdf && !esImagen)
                            return Json(new { success = false, message = "Adjunto no permitido (use JPG/PNG/WEBP o PDF)." });

                        byte[] datos;
                        string tipoArchivo;
                        string nombreArchivo;

                        if (esPdf)
                        {
                            // PDF: guardar bytes tal cual
                            datos = ReadAllBytes(f.InputStream);
                            tipoArchivo = "application/pdf";
                            nombreArchivo = Path.GetFileName(f.FileName);
                        }
                        else
                        {
                            // Imagen: convertir a WEBP (usa tu método existente)
                            // ToWebP(HttpPostedFileBase) → byte[]
                            datos = ToWebP(f);
                            tipoArchivo = "image/webp";

                            // Renombrar extensión para coherencia visual al descargar
                            var baseName = Path.GetFileNameWithoutExtension(f.FileName);
                            nombreArchivo = baseName + ".webp";
                        }

                        // Insertar en tabla de archivos
                        await db.ExecuteAsync(
                            "dbo.usp_Archivo_Insert",
                            new
                            {
                                CasoID = id,
                                NombreArchivo = nombreArchivo,
                                TipoArchivo = tipoArchivo,
                                DatosArchivo = datos,
                                Tipo = "Soporte"
                            },
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }
            }
            return Json(new { success = true });
        }

        // === Helpers mínimos ===

        // Detecta imagen por MIME/Extensión (jpg/png/webp)
        private static bool IsImagenMime(string mime, string ext)
        {
            if (string.IsNullOrEmpty(mime)) mime = string.Empty;
            if (string.IsNullOrEmpty(ext)) ext = string.Empty;

            mime = mime.ToLowerInvariant();
            ext = ext.ToLowerInvariant();

            // MIME comunes
            if (mime.StartsWith("image/")) return true;

            // Fallback por extensión (cuando IIS entrega octet-stream)
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp";
        }

        private static byte[] ReadAllBytes(Stream input)
        {
            if (input == null) return new byte[0];
            if (input.CanSeek) input.Position = 0;
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CerrarCaso(int CasoID, string NotasCierre, HttpPostedFileBase AdjuntoEvidencia)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null)
                return Json(new { success = false, message = "Sesión expirada." });

            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    await cn.OpenAsync();

                    var casoInfo = await cn.QueryFirstOrDefaultAsync<dynamic>(
                        @"SELECT ID, FechaCreacion, TipoCaso FROM dbo.Caso WHERE ID = @ID",
                        new { ID = CasoID }
                    );

                    if (casoInfo == null)
                        return Json(new { success = false, message = "Caso no encontrado." });

                    // 2) NUEVA VALIDACIÓN: Obras Civiles Obligatorio
                    string nombreTipo = casoInfo.TipoCaso != null ? casoInfo.TipoCaso.ToString() : "";
                    bool esObrasCiviles = nombreTipo.IndexOf("obras civiles", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool hayAdjunto = (AdjuntoEvidencia != null && AdjuntoEvidencia.ContentLength > 0);

                    if (esObrasCiviles && !hayAdjunto)
                    {
                        return Json(new { success = false, message = "Para los casos de Obras Civiles es OBLIGATORIO adjuntar evidencia (foto o documento)." });
                    }

                    DateTime fechaCreacion = (DateTime)casoInfo.FechaCreacion;
                    DateTime fechaFin = DateTime.Now;

                    var tiempoMin = CalcularTiempoAtencionHabilMin(fechaCreacion, fechaFin);
                    var tiempoTexto = CalcularTiempoAtencionHabilStr(fechaCreacion, fechaFin);

                    using (var tx = cn.BeginTransaction())
                    {
                        // 1) Llamar SP de cierre
                        var p = new DynamicParameters();
                        p.Add("@Correo", u.EmailAddress);
                        p.Add("@CasoID", CasoID);
                        p.Add("@NotasCierre", (object)NotasCierre ?? DBNull.Value);
                        p.Add("@UsuarioAccion", u.FullName);
                        p.Add("@FechaFinalizacion", fechaFin);
                        p.Add("@TiempoAtencion", (object)tiempoTexto ?? DBNull.Value);
                        p.Add("@TiempoAtencionMinutos", tiempoMin);

                        var ok = await cn.ExecuteScalarAsync<int>(
                            "dbo.usp_Caso_Cerrar_v1",
                            p,
                            tx,
                            commandType: CommandType.StoredProcedure
                        );

                        if (ok != 1)
                        {
                            tx.Rollback();
                            return Json(new { success = false, message = "No fue posible cerrar el caso." });
                        }

                        // 2) Guardar evidencia (opcional) con nuevos formatos
                        if (hayAdjunto)
                        {
                            var ext = Path.GetExtension(AdjuntoEvidencia.FileName).ToLowerInvariant();
                            var contentType = (AdjuntoEvidencia.ContentType ?? "").ToLowerInvariant();

                            var esImagen = new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext) || contentType.StartsWith("image/");
                            var esDocumento = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" }.Contains(ext);

                            if (!esImagen && !esDocumento)
                            {
                                tx.Rollback();
                                return Json(new { success = false, message = "Archivo no permitido. Use Imagen, PDF, Word o Excel." });
                            }

                            if (AdjuntoEvidencia.ContentLength > 10 * 1024 * 1024) // 10MB
                            {
                                tx.Rollback();
                                return Json(new { success = false, message = "El archivo excede 10MB." });
                            }

                            byte[] datosFinales;
                            string tipoArchivoFinal;
                            string nombreArchivoFinal = Path.GetFileName(AdjuntoEvidencia.FileName);

                            if (esImagen)
                            {
                                // Convertir imagen a WebP para optimizar espacio
                                datosFinales = ToWebP(AdjuntoEvidencia);
                                tipoArchivoFinal = "image/webp";
                                nombreArchivoFinal = Path.GetFileNameWithoutExtension(nombreArchivoFinal) + ".webp";
                            }
                            else
                            {
                                // PDF, Word o Excel: Leer bytes originales sin convertir
                                datosFinales = ReadAllBytes(AdjuntoEvidencia.InputStream);
                                tipoArchivoFinal = contentType;

                                // FIX PARA "String or binary data would be truncated":
                                // Muchos content-types de Office superan los 50 caracteres (ej. application/vnd.openxmlformats-officedocument.spreadsheetml.sheet).
                                // Para evitar el truncamiento en la base de datos, los re-mapeamos a algo corto si detectamos su extensión.
                                if (ext.Contains("doc"))
                                {
                                    tipoArchivoFinal = "application/msword";
                                }
                                else if (ext.Contains("xls"))
                                {
                                    tipoArchivoFinal = "application/vnd.ms-excel";
                                }
                                else if (ext == ".pdf")
                                {
                                    tipoArchivoFinal = "application/pdf";
                                }

                                // Seguro anti-truncado final: Si por algún motivo de otro archivo sigue siendo gigante, córtalo a 50
                                if (tipoArchivoFinal.Length > 50)
                                {
                                    tipoArchivoFinal = tipoArchivoFinal.Substring(0, 50);
                                }
                            }

                            var p2 = new DynamicParameters();
                            p2.Add("@CasoID", CasoID);
                            p2.Add("@NombreArchivo", nombreArchivoFinal);
                            p2.Add("@TipoArchivo", tipoArchivoFinal);
                            p2.Add("@DatosArchivo", datosFinales, DbType.Binary);
                            p2.Add("@UsuarioAccion", u.EmailAddress);
                            p2.Add("@Tipo", "Soporte");

                            await cn.ExecuteAsync(
                                "dbo.usp_Archivo_InsertarEvidencia",
                                p2,
                                tx,
                                commandType: CommandType.StoredProcedure
                            );
                        }

                        tx.Commit();
                        GenerarCorreoCierreCaso(CasoID, "Cerrado");

                        return Json(new { success = true });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        //        [HttpPost]
        //        [ValidateAntiForgeryToken]

        //        public async Task<ActionResult> CerrarCaso(
        //    int CasoID,
        //    string NotasCierre,
        //    HttpPostedFileBase AdjuntoEvidencia
        //)
        //        {
        //            var u = Session["User"] as Entities.Employees;
        //            if (u == null)
        //                return Json(new { success = false, message = "Sesión expirada." });

        //            try
        //            {
        //                using (var cn = new SqlConnection(csHelpdesk))
        //                {
        //                    await cn.OpenAsync();

        //                    // Traemos la FechaCreacion para calcular duración
        //                    var casoInfo = await cn.QueryFirstOrDefaultAsync<dynamic>(
        //                        @"SELECT ID, FechaCreacion
        //                  FROM dbo.Caso
        //                  WHERE ID = @ID",
        //                        new { ID = CasoID }
        //                    );

        //                    if (casoInfo == null)
        //                    {
        //                        return Json(new { success = false, message = "Caso no encontrado." });
        //                    }

        //                    DateTime fechaCreacion = (DateTime)casoInfo.FechaCreacion;
        //                    DateTime fechaFin = DateTime.Now;

        //                    // calculamos duración hábil
        //                    var tiempoMin = CalcularTiempoAtencionHabilMin(fechaCreacion, fechaFin);
        //                    var tiempoTexto = CalcularTiempoAtencionHabilStr(fechaCreacion, fechaFin);

        //                    using (var tx = cn.BeginTransaction())
        //                    {
        //                        // 1) Llamar SP actualizado
        //                        var p = new DynamicParameters();
        //                        p.Add("@Correo", u.EmailAddress);
        //                        p.Add("@CasoID", CasoID);
        //                        p.Add("@NotasCierre", (object)NotasCierre ?? DBNull.Value);
        //                        p.Add("@UsuarioAccion", u.FullName);
        //                        p.Add("@FechaFinalizacion", fechaFin);
        //                        p.Add("@TiempoAtencion", (object)tiempoTexto ?? DBNull.Value);
        //                        p.Add("@TiempoAtencionMinutos", tiempoMin);

        //                        var ok = await cn.ExecuteScalarAsync<int>(
        //                            "dbo.usp_Caso_Cerrar_v1",
        //                            p,
        //                            tx,
        //                            commandType: CommandType.StoredProcedure
        //                        );

        //                        if (ok != 1)
        //                        {
        //                            tx.Rollback();
        //                            return Json(new { success = false, message = "No fue posible cerrar el caso." });
        //                        }

        //                        // 2) Guardar evidencia (opcional)
        //                        if (AdjuntoEvidencia != null && AdjuntoEvidencia.ContentLength > 0)
        //                        {
        //                            var allowed = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        //                            var contentType = (AdjuntoEvidencia.ContentType ?? "").ToLowerInvariant();

        //                            if (!allowed.Contains(contentType))
        //                            {
        //                                tx.Rollback();
        //                                return Json(new { success = false, message = "Tipo de archivo no permitido." });
        //                            }
        //                            if (AdjuntoEvidencia.ContentLength > 10 * 1024 * 1024) // 10MB
        //                            {
        //                                tx.Rollback();
        //                                return Json(new { success = false, message = "El archivo excede 10MB." });
        //                            }

        //                            var bytes = ToWebP(AdjuntoEvidencia); // tu helper actual

        //                            var p2 = new DynamicParameters();
        //                            p2.Add("@CasoID", CasoID);
        //                            p2.Add("@NombreArchivo", Path.GetFileName(AdjuntoEvidencia.FileName));
        //                            p2.Add("@TipoArchivo", "image/webp");
        //                            p2.Add("@DatosArchivo", bytes, DbType.Binary);
        //                            p2.Add("@UsuarioAccion", u.EmailAddress);
        //                            p2.Add("@Tipo", "Soporte");

        //                            await cn.ExecuteAsync(
        //                                "dbo.usp_Archivo_InsertarEvidencia",
        //                                p2,
        //                                tx,
        //                                commandType: CommandType.StoredProcedure
        //                            );
        //                        }

        //                        // 3) Commit
        //                        tx.Commit();

        //                        // 4) Correo de notificación
        //                        GenerarCorreoCierreCaso(CasoID, "Cerrado");

        //                        return Json(new { success = true });
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                return Json(new { success = false, message = ex.Message });
        //            }
        //        }

        /* ================= Helpers ================= */

        // Devuelve minutos hábiles (Lun-Vie). No cuenta sábado ni domingo.
        private int CalcularTiempoAtencionHabilMin(DateTime inicio, DateTime fin)
        {
            if (fin < inicio)
                fin = inicio;

            double totalMinutosHabiles = 0;
            DateTime cursor = inicio;

            while (cursor < fin)
            {
                if (cursor.DayOfWeek == DayOfWeek.Saturday ||
                    cursor.DayOfWeek == DayOfWeek.Sunday)
                {
                    cursor = cursor.Date.AddDays(1);
                    continue;
                }

                DateTime finDia = cursor.Date.AddDays(1);
                if (finDia > fin) finDia = fin;

                var delta = finDia - cursor;
                totalMinutosHabiles += delta.TotalMinutes;

                cursor = finDia;
            }

            return (int)Math.Round(totalMinutosHabiles);
        }

        // Convierte esos minutos en string tipo "Xd Yh Zm"
        private string CalcularTiempoAtencionHabilStr(DateTime inicio, DateTime fin)
        {
            int minutos = CalcularTiempoAtencionHabilMin(inicio, fin);

            // Interpretación simple: 1 día = 24h hábiles (porque ya excluimos fines de semana)
            int dias = minutos / (60 * 24);
            int remMin = minutos % (60 * 24);

            int horas = remMin / 60;
            int mins = remMin % 60;

            return $"{dias}d {horas}h {mins}m";
        }
        // Cerrar caso directo (con soporte actual)
        [HttpPost]
        public async Task<JsonResult> AtenderCasocerrado(int CasoID, string NotasCierre)
        {
            try
            {
                var me = Session["User"] as Employees;
                if (me == null) return Json(new { success = false, message = "Sesión expirada." });

                long filas;
                using (var db = new SqlConnection(connectionString))
                {
                    filas = await db.ExecuteScalarAsync<long>(
                        "dbo.usp_Caso_Cerrar",
                        new { CasoID, NotasCierre, SoporteID = me.EmployeeNumber },
                        commandType: CommandType.StoredProcedure
                    );
                }
                if (filas <= 0) return Json(new { success = false, message = "No se encontró el caso." });

                try { GenerarCorreoCierreCaso(CasoID, "Cerrado"); } catch { }

                return Json(new { success = true, message = "Caso cerrado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }



        // Agregar evento (nota / cambio estado) + evidencia
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AgregarEvento(int casoID, string tipoEvento, string nota, bool notaVisible, IEnumerable<HttpPostedFileBase> evidencias)
        {
            try
            {
                var me = Session["User"] as Employees;
                if (me == null) return Json(new { success = false, message = "Sesión expirada." });

                long eventoId;
                using (var db = new SqlConnection(connectionString))
                {
                    var r = db.QueryFirstOrDefault(
                        "dbo.usp_Caso_AgregarEvento",
                        new { CasoID = casoID, TipoEvento = tipoEvento, Nota = nota, NotaVisible = notaVisible, UsuarioAccion = me.EmployeeNumber },
                        commandType: CommandType.StoredProcedure);
                    eventoId = Convert.ToInt64(r.EventoID);
                }

                if (evidencias != null)
                {
                    using (var db = new SqlConnection(connectionString))
                    {
                        foreach (var f in evidencias)
                        {
                            if (f == null || f.ContentLength <= 0) continue;

                            byte[] data = ConvertToWebP(f); // compacta
                            db.Execute(
@"INSERT INTO dbo.CasoEventoArchivo (EventoID,NombreArchivo,TipoArchivo,DatosArchivo)
  VALUES (@EventoID,@NombreArchivo,@TipoArchivo,@DatosArchivo)",
                                new
                                {
                                    EventoID = eventoId,
                                    NombreArchivo = Path.GetFileName(f.FileName),
                                    TipoArchivo = "image/webp",
                                    DatosArchivo = data
                                });
                        }
                    }
                }

                return Json(new { success = true, message = "Evento agregado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Cambiar estado simple (wrapper si lo deseas)
        [HttpPost]
        public JsonResult CambiarEstado(int casoID, string nuevoEstado, string nota)
        {
            try
            {
                return AgregarEvento(casoID, nuevoEstado, nota, true, null);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /* ====== Lectura enriquecida ====== */

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

        public async Task<IEnumerable<CasoView>> ObtenerCasosAsync()
        {
            var eEmployee = Session["User"] as Employees;
            using (var db = new SqlConnection(connectionString))
            {
                var casos = db.Query<CasoView>(
                    "dbo.usp_Casos_Listar_v2",
                    new { Permitido = eEmployee.EmailAddress },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                Session["casos"] = casos;
                return casos;
            }
        }

        public async Task<IEnumerable<CasoView>> ObtenerCasosPaginadosAsync()
        {
            var casos = await ObtenerCasosAsync();
            return casos;
        }

        public JsonResult Details2(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var caso = db.QueryFirstOrDefault<CasoDetalle>(
                                  "dbo.usp_CasosObtenerPorId",
                                  new { Id = id },
                                  commandType: CommandType.StoredProcedure
                              );
                if (caso == null) return Json(null, JsonRequestBehavior.AllowGet);

                var archivos = db.Query<Archivo>("SELECT * FROM dbo.Archivo WHERE CasoID=@CasoID", new { CasoID = id }).ToList();
                if (archivos?.Any() == true)
                {
                    var file = archivos.First();
                    caso.data = Convert.ToBase64String(file.DatosArchivo);
                    caso.DatosArchivo = file.DatosArchivo;
                    caso.TipoArchivo = file.TipoArchivo;
                    caso.NombreArchivo = file.NombreArchivo;
                }
                return Json(caso, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult Details3(int id)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    var caso = cn.QueryFirstOrDefault<CasoDetalleDto>(
                        "dbo.usp_Casos_Detalle_v1",
                        new { Id = id },
                        commandType: CommandType.StoredProcedure);

                    if (caso == null)
                        return Json(null, JsonRequestBehavior.AllowGet);

                    // Traer último adjunto (si tu tabla tiene FechaSubida; sino usa ID DESC)
                    var files = cn.Query<ArchivoDto>(
                        @"SELECT   * 
                          FROM dbo.Archivo 
                          WHERE CasoID=@CasoID 
                          ORDER BY ISNULL(FechaSubida, GETDATE()) DESC, Id DESC",
                        new { CasoID = id });

                    //if (file != null && file.DatosArchivo != null && file.DatosArchivo.Length > 0)
                    //{
                    //    caso.DatosArchivo = file.DatosArchivo;
                    //    caso.TipoArchivo = file.TipoArchivo;
                    //    caso.NombreArchivo = file.NombreArchivo;
                    //    caso.data = Convert.ToBase64String(file.DatosArchivo);
                    //}
                    if (files.Count() > 0)
                    {
                        caso.Adjuntos = new List<ArchivoDto>();
                    }
                    foreach (var f in files)
                    {
                        if (f.DatosArchivo != null && f.DatosArchivo.Length > 0)
                        {
                            f.data = Convert.ToBase64String(f.DatosArchivo);
                            caso.Adjuntos.Add(f);
                        }
                    }
                    return Json(caso, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult Details(int id)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    var caso = cn.QueryFirstOrDefault<CasoDetalleDto>(
                        "dbo.usp_Casos_Detalle_v1",
                        new { Id = id },
                        commandType: CommandType.StoredProcedure);

                    if (caso == null) return Json(null, JsonRequestBehavior.AllowGet);

                    // 1) Traer SOLO metadata primero (sin BLOB) para no inflar payload
                    var filesMeta = cn.Query<ArchivoDto>(@"
                SELECT 
                    Id, CasoID, NombreArchivo, TipoArchivo, 
                    ISNULL(FechaSubida, GETDATE()) AS FechaSubida
                FROM dbo.Archivo
                WHERE CasoID = @CasoID
                ORDER BY ISNULL(FechaSubida, GETDATE()) DESC, Id DESC",
                        new { CasoID = id }).ToList();

                    if (filesMeta != null && filesMeta.Count > 0)
                        caso.Adjuntos = new List<ArchivoDto>();

                    foreach (var meta in filesMeta)
                    {
                        // 2) Si es imagen → incluir base64 inline (data URL). Si es PDF → solo URL descarga.
                        var tipo = (meta.TipoArchivo ?? string.Empty).ToLowerInvariant();
                        var esImagen = tipo.StartsWith("image/");

                        if (esImagen)
                        {
                            // Leer bytes SOLO de esta fila (1 hit por adjunto imagen)
                            var bin = cn.ExecuteScalar<byte[]>(
                                "SELECT DatosArchivo FROM dbo.Archivo WHERE Id=@Id",
                                new { meta.Id });

                            if (bin != null && bin.Length > 0)
                                //f.data = Convert.ToBase64String(meta.DatosArchivo);

                                meta.data = Convert.ToBase64String(bin);
                        }
                        else
                        {
                            // PDF/otros → devolver link de descarga (sin base64)
                            // Crea una acción FileResult: FileArchivo(int id) que haga stream del BLOB
                            meta.UrlDescarga = Url.Action("FileArchivo", "Helpdesk2025", new { id = meta.Id });
                        }

                        caso.Adjuntos.Add(meta);
                    }

                    return Json(caso, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult FileArchivo(int id)
        {
            using (var cn = new SqlConnection(csHelpdesk))
            {
                var row = cn.QueryFirstOrDefault(
                    @"SELECT NombreArchivo, TipoArchivo, DatosArchivo 
              FROM dbo.Archivo WHERE Id=@Id",
                    new { Id = id });

                if (row == null || row.DatosArchivo == null) return HttpNotFound();

                var fileName = (string)row.NombreArchivo ?? "archivo";
                var mime = (string)row.TipoArchivo ?? "application/octet-stream";
                var bytes = (byte[])row.DatosArchivo;

                Response.AppendHeader("Content-Disposition", "inline; filename=\"" + fileName + "\"");
                return File(bytes, mime);
            }
        }

        [HttpGet]
        public JsonResult KPIsCasos(bool equipo = false)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada." });

            var carnet = u.EmployeeNumber;
            if (string.IsNullOrEmpty(carnet))
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            var resumen = new { Pend = 0, Proc = 0, Cerr = 0 };

            try
            {
                // Podemos simplemente reutilizar sp_Helpdesk_CasosPorJefe y contar en memoria:
                var tmp = new List<CasoJefeVM>();

                using (var cn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("dbo.sp_Helpdesk_CasosPorJefe", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CarnetJefe", carnet);
                    cmd.Parameters.AddWithValue("@Equipo", equipo ? 1 : 0);

                    cn.Open();
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            tmp.Add(new CasoJefeVM
                            {
                                Estado = rd["Estado"] as string
                            });
                        }
                    }
                }

                int p = 0, pr = 0, ce = 0;
                foreach (var c in tmp)
                {
                    if (c.Estado == "Pendiente" || c.Estado == "Asignado") p++;
                    else if (c.Estado == "En Proceso") pr++;
                    else if (c.Estado == "Cerrado") ce++;
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Pendiente = p,
                        EnProceso = pr,
                        Cerrado = ce
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        // C# MVC — JSON grande y consistente (evita truncado → "Unexpected end of JSON input")
        private ContentResult JsonNet(object payload, int? statusCode = null)
        {
            var json = JsonConvert.SerializeObject(payload,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss",
                    NullValueHandling = NullValueHandling.Include
                });

            if (statusCode.HasValue) Response.StatusCode = statusCode.Value;

            return new ContentResult
            {
                Content = json,
                ContentType = "application/json; charset=utf-8",
                ContentEncoding = Encoding.UTF8
            };
        }


        [HttpGet]
        public async Task<JsonResult> Admin_ListarSoportes()
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var data = await db.QueryAsync<SoporteAdminItem>(
                    "dbo.usp_Soporte_Listar",
                    commandType: CommandType.StoredProcedure
                );
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult Admin_Visibilidad_Listar(string viewerId)
        {
            try
            {
                using (var db = new SqlConnection(csHelpdesk))
                {
                    var sql = @"
                SELECT 
                    v.Id,
                    v.ViewerSoporteID as  ViewerID,
                    v.TargetSoporteID as CorreoTarget,
                    s.Nombre AS NombreTarget 
                FROM dbo.SoporteVisibilidad v
                LEFT JOIN dbo.Soporte s 
                       ON  s.SoporteID = v.TargetSoporteID   -- si guardas carnet/id
                        OR s.Email     = v.TargetSoporteID   -- si guardas correo
                WHERE v.ViewerSoporteID = @viewer";

                    // Encapsulado en lista fuertemente tipada
                    var lista = db.Query<SoporteVisibilidadDto2>(sql, new { viewer = viewerId }).ToList();

                    return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { Response.StatusCode = 500; return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Admin_Visibilidad_Agregar(string viewerId, string targetId)
        {
            if (string.IsNullOrWhiteSpace(viewerId) || string.IsNullOrWhiteSpace(targetId))
                return Json(new { success = false, message = "Datos incompletos" });

            if (string.Equals(viewerId, targetId, StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "No puedes agregarte a ti mismo" });

            try
            {
                using (var db = new SqlConnection(csHelpdesk))          // <- csHelpdesk NO debe ser null
                {
                    db.Open();                                          // <- importante antes de BeginTransaction

                    using (var tx = db.BeginTransaction())              // <- aquí ya hay conexión abierta
                    {
                        try
                        {
                            // 1) Evitar duplicado
                            var existe = db.ExecuteScalar<int>(
                                @"SELECT COUNT(1) 
                  FROM dbo.SoporteVisibilidad 
                  WHERE ViewerSoporteID = @v 
                    AND TargetSoporteID = @t",
                                new { v = viewerId, t = targetId },
                                transaction: tx
                            );

                            if (existe > 0)
                            {
                                tx.Rollback();
                                return Json(new { success = true, duplicated = true },
                                            JsonRequestBehavior.AllowGet);
                            }

                            // 2) Insertar visibilidad
                            var sqlIns = @"
                INSERT INTO dbo.SoporteVisibilidad (ViewerSoporteID, TargetSoporteID, CreadoPor)
                VALUES (@v, @t, @u);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            var id = db.ExecuteScalar<int>(
                                sqlIns,
                                new
                                {
                                    v = viewerId,
                                    t = targetId,
                                    u = (User?.Identity?.Name ?? "sistema") // aquí no puede haber NullRef por el ?.
                                },
                                transaction: tx
                            );

                            tx.Commit();
                            return Json(new { success = true, duplicated = false, id = id },
                                        JsonRequestBehavior.AllowGet);
                        }
                        catch
                        {
                            if (tx != null) tx.Rollback();
                            throw;
                        }
                    }
                }

            }
            catch (SqlException ex) { Response.StatusCode = 500; return Json(new { success = false, message = "SQL: " + ex.Message }); }
            catch (Exception ex) { Response.StatusCode = 500; return Json(new { success = false, message = ex.Message }); }
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Admin_Visibilidad_Quitar(int id)
        {
            try
            {
                using (var db = new SqlConnection(csHelpdesk))
                {
                    var rows = db.Execute("DELETE FROM dbo.Soporte_VisibilidadExtra WHERE Id=@id", new { id });
                    return Json(new { success = rows > 0 });
                }
            }
            catch (Exception ex) { Response.StatusCode = 500; return Json(new { success = false, message = ex.Message }); }
        }
        [HttpGet]
        public async Task<JsonResult> Admin_ObtenerSoporte(string id)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    using (var multi = await cn.QueryMultipleAsync(
                        "dbo.usp_Soporte_Obtener",
                        new { SoporteID = id },
                        commandType: CommandType.StoredProcedure))
                    {
                        var s = await multi.ReadFirstOrDefaultAsync<SoporteVm>();
                        var p = (await multi.ReadAsync<SoportePermisoDto>()).ToList();
                        if (s == null) return Json(new { success = false, message = "No existe el soporte." }, JsonRequestBehavior.AllowGet);
                        s.Permisos = p;
                        return Json(new { success = true, data = s }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult PersonaInfo(string carnet = null, string correo = null)
        {
            // Ideal mover a web.config
            const string WS_USER = "Claro_RhOnline_WS_SS";
            const string WS_PASS = "HCM-RH0nl1ne@#3";
            string consulta = "";
            if (carnet != null && carnet != "")
            {
                consulta = "usp_Emp2024_Obtener_v2";
            }
            else if (correo != null && correo != "")
            {
                consulta = "usp_Emp2024_Obtener_v3";

            }
            else
            {
                return Json(new { success = false, message = "revise correo" }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    var p = new DynamicParameters();
                    if (carnet != null && carnet != "")
                    {
                        p.Add("@Carnet", (object)carnet ?? DBNull.Value);
                    }
                    else
                    if (correo != null && correo != "")
                    {
                        p.Add("@Correo", (object)correo ?? DBNull.Value);

                    }
                    var persona = cn.QueryFirstOrDefault<PersonaDto>(
                    consulta, p, commandType: CommandType.StoredProcedure);

                    if (persona == null)
                        return Json(new { success = false, message = "No se encontró la persona." }, JsonRequestBehavior.AllowGet);

                    string imageDataURL = null;

                    if (!string.IsNullOrWhiteSpace(persona.Url))
                    {
                        try
                        {
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                            var request = (HttpWebRequest)WebRequest.Create(persona.Url);
                            string authHeader = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                                    .GetBytes($"{WS_USER}:{WS_PASS}"));
                            request.Headers.Add("Authorization", "Basic " + authHeader);
                            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                            using (var response = (HttpWebResponse)request.GetResponse())
                            using (var stream = response.GetResponseStream())
                            {
                                if (response.StatusCode == HttpStatusCode.OK && stream != null)
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        stream.CopyTo(ms);
                                        var bytes = ms.ToArray();
                                        var contentType = response.ContentType ?? "image/jpeg";
                                        var base64 = Convert.ToBase64String(bytes);
                                        imageDataURL = $"data:{contentType};base64,{base64}";
                                    }
                                }
                            }
                        }
                        catch (WebException webEx)
                        {
                            // Si falla la foto, retornamos igual los datos
                            var http = webEx.Response as HttpWebResponse;
                            var msg = (http != null) ? $"Foto HTTP {http.StatusCode}" : webEx.Message;
                            return Json(new { success = true, data = persona, imageDataURL = (string)null, photoError = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    return Json(new { success = true, data = persona, imageDataURL }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public async Task<JsonResult> CancelarCasox(int id, string motivo)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada." });

            using (var db = new SqlConnection(csHelpdesk))
            {
                var filas = await db.ExecuteScalarAsync<long>(
                    "dbo.usp_Caso_Cancelar",
                    new { CasoID = id, Motivo = motivo ?? "", UsuarioID = u.EmployeeNumber },
                    commandType: CommandType.StoredProcedure
                );
                if (filas <= 0) return Json(new { success = false, message = "No se pudo cancelar el caso (¿ya no está Pendiente?)." });
            }
            GenerarCorreoCancelacionCaso(id, "Cancelado", motivo);
            return Json(new { success = true });
        }
        [HttpPost]
        public JsonResult CancelarCaso(int id)
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    var rows = db.Execute(@"
UPDATE dbo.Caso
   SET Estado='Cancelado', FechaActualizacion=SYSDATETIME()
 WHERE ID=@id AND Estado='Pendiente';", new { id });
                    if (rows <= 0) return Json(new { success = false, message = "El caso no está en estado Pendiente o no existe." });
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public CasoView ObtenerCasoPorId(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                return db.QueryFirstOrDefault<CasoView>(
                    "dbo.usp_CasosObtenerPorId",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        /* ====== Dashboard (reportería) ====== */

        public JsonResult DashboardData(string desde, string hasta)
        {
            DateTime d1 = string.IsNullOrWhiteSpace(desde) ? DateTime.Today.AddDays(-30) : DateTime.Parse(desde);
            DateTime d2 = string.IsNullOrWhiteSpace(hasta) ? DateTime.Today : DateTime.Parse(hasta);

            using (var db = new SqlConnection(connectionString))
            {
                var kpi = db.Query("dbo.usp_Dashboard_KPI", new { Desde = d1.Date, Hasta = d2.Date }, commandType: CommandType.StoredProcedure).ToList();
                var dist = db.Query("dbo.usp_Dashboard_TipoSubtipo", new { Desde = d1.Date, Hasta = d2.Date }, commandType: CommandType.StoredProcedure).ToList();
                var aging = db.QueryFirstOrDefault("dbo.usp_Dashboard_Aging", new { Desde = d1.Date, Hasta = d2.Date }, commandType: CommandType.StoredProcedure);

                return Json(new { kpi, dist, aging }, JsonRequestBehavior.AllowGet);
            }
        }

        /* ====== Helpers ====== */

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

        private void UpsertCasoEnSesion(CasoView caso)
        {
            var lista = Session["casos"] as List<CasoView> ?? new List<CasoView>();
            var idx = lista.FindIndex(x => x.ID == caso.ID);
            if (idx >= 0) lista[idx] = caso; else lista.Add(caso);
            Session["casos"] = lista;
        }

        /* ====== Correo ====== */

        // Creación → Soporte (usa solo ID; persiste HTML en CorreoCaso; usa API)
        public async Task<string> GenerarCorreoCreacionCasoSoporteAsync(int id)
        {
            var caso = await ObtenerCasoByIdAsync(id);
            if (caso == null) throw new InvalidOperationException("Caso no encontrado.");
            UpsertCasoEnSesion(caso);

            // ===== Campos base (HTML-encode) =====
            string tituloEnc = HttpUtility.HtmlEncode(caso.Titulo ?? "-");
            string estadoEnc = HttpUtility.HtmlEncode(caso.Estado ?? "Pendiente");
            string tipoCasoEnc = HttpUtility.HtmlEncode(caso.TipoCaso ?? "-");
            string prioridadEnc = HttpUtility.HtmlEncode(caso.Prioridad ?? "-");
            string descripcionEnc = HttpUtility.HtmlEncode(caso.Descripcion ?? "-");

            DateTime fcre = caso.FechaCreacion;
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

            // ===== Detectar el subtipo especial (ID 41) o por nombre =====
            bool esHsEdificio =
                (caso.TipoCasoID == 5) ||
                (
                    ((caso.TipoCaso ?? "").IndexOf("higiene", StringComparison.OrdinalIgnoreCase) >= 0 &&
                     (caso.TipoCaso ?? "").IndexOf("seguridad", StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (caso.TipoCaso ?? "").IndexOf("inconveniente", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (caso.TipoCaso ?? "").IndexOf("edificio", StringComparison.OrdinalIgnoreCase) >= 0
                );

            // ===== Campos adicionales (HTML-encode / stringify) =====
            string deptoEnc = HttpUtility.HtmlEncode(caso.Departamento ?? "-");
            string edificEnc = HttpUtility.HtmlEncode(caso.Edificio ?? "-");
            string cantEnc = (caso.CantidadAfectados.HasValue ? caso.CantidadAfectados.Value.ToString() : "-");
            string diasEnc = (caso.DiasCondicion.HasValue ? caso.DiasCondicion.Value.ToString() : "-");

            // Card extra (solo para el subtipo especial)
            string hsCard = esHsEdificio
                ? $@"
        <div class='card'>
          <h3>Ubicación / Impacto</h3>
          <div class='pair'><span>Departamento:&nbsp;</span><strong>{deptoEnc}</strong></div>
          <div class='pair'><span>Edificio:&nbsp;</span>{edificEnc}</div>
          <div class='pair'><span>Personal afectado:&nbsp;</span>{cantEnc}</div>
          <div class='pair'><span>Días con la condición:&nbsp;</span>{diasEnc}</div>
        </div>"
                : "";

            string asunto = $"Helpdesk Tick-{id}: Caso Creado - {tituloEnc}";

            // ===== HTML unificado tipo Asignación (rojo creación) =====
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;
        box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#e11d48;color:#fff;padding:22px 26px}}
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;
           gap:8px;align-items:center}}
  .ticket,.titulo,.act{{white-space:nowrap}}
  .dash{{opacity:.8}}
  .badge-new{{display:inline-block;background:#fee2e2;color:#7f1d1d;
              border:1px solid #fecaca;padding:4px 10px;border-radius:999px;
              font-size:12px}}
  .cnt{{padding:22px 26px}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;
         padding:6px 10px;font-size:12px;background:#f3f4f6;color:#111}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;
                             font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .blockq{{background:#f8f9fa;border-left:5px solid #e11d48;margin:16px 0;
           padding:14px 16px;color:#374151}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;
         padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}}
  .pair span{{color:#6b7280}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;
        padding:16px}}
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
              Se ha creado un nuevo caso por <strong>{nomAutorEnc}</strong>.
              Por favor, revisar y asignar para su atención.
            </div>
          </td>
        </tr>
      </table>
   {hsCard}
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
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nomAfectadoEnc}-{caso.carnetResponsable}</strong></div>
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

            // ===== Guardar correo y enviar (sin cambios en C#) =====
            int idcorreo = 0;
            string payload = EncodeHtmlToNumeric(mensaje);

            using (var db = new SqlConnection(connectionString))
            {
                idcorreo = db.ExecuteScalar<int>(
                    "dbo.usp_CorreoCaso_Insert",
                    new { CasoID = id, TipoCaso = caso.TipoCaso, Estado = "Pendiente", Asunto = asunto, ContenidoHtml = payload },
                    commandType: CommandType.StoredProcedure
                );
            }

            string apiResp = getcorreohelpapi("soporte@claro.com.ni", caso.CorreoAutor ?? "", asunto, idcorreo.ToString(), id);
            return apiResp;
        }


        public string GenerarCorreoCancelacionCaso(int id, string estado, string motivo)
        {
            var micasoatender = ObtenerCasoPorId(id);

            // Sanitizar
            string nombreUsuarioEnc = HttpUtility.HtmlEncode(micasoatender?.NombreAutor ?? "-");
            string tituloEnc = HttpUtility.HtmlEncode(micasoatender?.Titulo ?? "-");
            string descripcionEnc = HttpUtility.HtmlEncode(micasoatender?.Descripcion ?? "-");
            string tipoCasoEnc = HttpUtility.HtmlEncode(micasoatender?.TipoCaso ?? "-");
            string motivoEnc = HttpUtility.HtmlEncode(motivo ?? "-");

            DateTime inicio = micasoatender?.FechaCreacion ?? DateTime.Now;
            DateTime fin = DateTime.Now; if (fin < inicio) fin = inicio;
            string fIniTxt = inicio.ToString("dd/MM/yyyy HH:mm");
            string fFinTxt = fin.ToString("dd/MM/yyyy HH:mm");
            var ts = fin - inicio;
            string duracionTxt = $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";

            // Afectado
            string nomAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.TelefonoResponsable ?? "-");

            // Ubicación / Impacto
            bool esHS = (micasoatender?.TipoCasoID == 5)
                        || ((micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("higiene")
                            && (micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("edificio"));
            string depEnc = HttpUtility.HtmlEncode(micasoatender?.Departamento ?? "-");
            string ediEnc = HttpUtility.HtmlEncode(micasoatender?.Edificio ?? "-");
            string perEnc = HttpUtility.HtmlEncode(micasoatender?.CantidadAfectados?.ToString() ?? "-");
            string diasEnc = HttpUtility.HtmlEncode(micasoatender?.DiasCondicion?.ToString() ?? "-");

            string bloqueUbicacion = esHS
                ? $@"<tr>
               <th>Ubicación / Impacto</th>
               <td>
                 <div><strong>Departamento:</strong> {depEnc}</div>
                 <div><strong>Edificio:</strong> {ediEnc}</div>
                 <div><strong>Personal afectado:</strong> {perEnc}</div>
                 <div><strong>Días con la condición:</strong> {diasEnc}</div>
               </td>
             </tr>"
                : "";

            string asunto = $"Helpdesk Tick-{id}: {tituloEnc}";

            // ===== HTML con estructura tipo Asignación (rojo cancelación) =====
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;
        box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#dc2626;color:#fff;padding:22px 26px}}
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;
           gap:8px;align-items:center}}
  .ticket,.titulo,.act{{white-space:nowrap}}
  .dash{{opacity:.8}}
  .badge-canc{{display:inline-block;background:#fee2e2;color:#7f1d1d;
               border:1px solid #fecaca;padding:4px 10px;border-radius:999px;
               font-size:12px}}
  .cnt{{padding:22px 26px}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;
         padding:6px 10px;font-size:12px;background:#f3f4f6;color:#111}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;
                             font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .blockq{{background:#fef2f2;border-left:5px solid #dc2626;margin:16px 0;
           padding:14px 16px;color:#7f1d1d}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;
         padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}}
  .pair span{{color:#6b7280}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;
        padding:16px}}
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
        <span class='badge-canc'>Estado: {estado}</span>
      </h1>
    </div>

    <div class='cnt'>
      <div class='kpis'>
        <div class='pill'>Tipo de caso: <strong>{tipoCasoEnc}</strong></div>
        <div class='pill'>Creación: <strong>{fIniTxt}</strong></div>
        <div class='pill'>Cancelación: <strong>{fFinTxt}</strong></div>
        <div class='pill'>Duración: <strong>{duracionTxt}</strong></div>
      </div>

      <table class='sum' role='presentation' aria-hidden='true'>
        <tr>
          <th>Descripción</th>
          <td>{descripcionEnc}</td>
        </tr>
        {bloqueUbicacion}
        <tr>
          <th>Motivo de cancelación</th>
          <td>
            <div class='blockq'>{motivoEnc}</div>
          </td>
        </tr>
      </table>

      <div class='grid'>
        <div class='card'>
          <h3>Solicitante</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nombreUsuarioEnc}</strong></div>
        </div>

        <div class='card'>
          <h3>Colaborador afectado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nomAfectadoEnc}-{micasoatender.carnetResponsable}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAfectadoEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAfectadoEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAfectadoEnc}</div>
        </div>
      </div>
    </div>

    <div class='ftr'>Mensaje automático, no responder.</div>
  </div>
</body>
</html>";

            // Persistir y enviar (igual)
            int idcorreo = 0;
            string m = EncodeHtmlToNumeric(mensaje);
            using (var db = new SqlConnection(connectionString))
            {
                idcorreo = db.ExecuteScalar<int>(
                    "dbo.usp_CorreoCaso_Insert",
                    new
                    {
                        CasoID = id,
                        TipoCaso = micasoatender.TipoCaso,
                        Estado = estado,
                        Asunto = asunto,
                        ContenidoHtml = m
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );
            }
            return getcorreohelpapi("", "", asunto, mensaje, id);
        }


        public string GenerarCorreoUsuario(int id, string estado)
        {
            var micasoatender = ObtenerCasoPorId(id);

            // Sanitizar
            string nombreUsuarioEnc = HttpUtility.HtmlEncode(micasoatender.NombreAutor ?? "-");
            string tituloEnc = HttpUtility.HtmlEncode(micasoatender.Titulo ?? "-");
            string descripcionEnc = HttpUtility.HtmlEncode(micasoatender?.Descripcion ?? "-");
            string tipoCasoEnc = HttpUtility.HtmlEncode(micasoatender?.TipoCaso ?? "-");

            DateTime inicio = micasoatender?.FechaCreacion ?? DateTime.Now;
            DateTime ahora = DateTime.Now;
            if (ahora < inicio) ahora = inicio;
            string fIniTxt = inicio.ToString("dd/MM/yyyy HH:mm");
            string fActTxt = ahora.ToString("dd/MM/yyyy HH:mm");
            TimeSpan ts = ahora - inicio;
            string duracionTxt = $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";

            // Afectado
            string nomAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.TelefonoResponsable ?? "-");

            // Soporte
            string nomSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Nombresoport ?? "Mesa de Ayuda");
            string cargoSoporteDet = HttpUtility.HtmlEncode(micasoatender?.Cargosoport ?? "-");
            string areaSoporteDet = HttpUtility.HtmlEncode(micasoatender?.Areasoport ?? "-");
            string telSoporteDet = HttpUtility.HtmlEncode(micasoatender?.Telefonosoport ?? "-");

            // Ubicación / Impacto
            bool esHS = (micasoatender?.TipoCasoID == 5)
                        || ((micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("higiene")
                            && (micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("edificio"));
            string depEnc = HttpUtility.HtmlEncode(micasoatender?.Departamento ?? "-");
            string ediEnc = HttpUtility.HtmlEncode(micasoatender?.Edificio ?? "-");
            string perEnc = HttpUtility.HtmlEncode(micasoatender?.CantidadAfectados?.ToString() ?? "-");
            string diasEnc = HttpUtility.HtmlEncode(micasoatender?.DiasCondicion?.ToString() ?? "-");

            string bloqueUbicacion = esHS
                ? $@"<tr>
               <th>Ubicación / Impacto</th>
               <td>
                 <div><strong>Departamento:</strong> {depEnc}</div>
                 <div><strong>Edificio:</strong> {ediEnc}</div>
                 <div><strong>Personal afectado:</strong> {perEnc}</div>
                 <div><strong>Días con la condición:</strong> {diasEnc}</div>
               </td>
             </tr>"
                : "";

            string titulo1 = $"Helpdesk Tick-{id}: {tituloEnc}";

            // ===== HTML con estructura tipo Asignación (gris seguimiento usuario) =====
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;
        box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#9ca3af;color:#111;padding:22px 26px}}
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;
           gap:8px;align-items:center}}
  .ticket,.titulo,.act{{white-space:nowrap}}
  .dash{{opacity:.8}}
  .badge-prog{{display:inline-block;background:#f3f4f6;color:#374151;
               border:1px solid #d1d5db;padding:4px 10px;border-radius:999px;
               font-size:12px}}
  .cnt{{padding:22px 26px}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;
         padding:6px 10px;font-size:12px;background:#f3f4f6;color:#374151}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;
                             font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .blockq{{background:#f8f9fa;border-left:5px solid #9ca3af;margin:16px 0;
           padding:14px 16px;color:#374151}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;
         padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}}
  .pair span{{color:#6b7280}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;
        padding:16px}}
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
        {bloqueUbicacion}
        <tr>
          <th>Mensaje</th>
          <td>
            <div class='blockq'>
              Estimado(a) <strong>{nombreUsuarioEnc}</strong>, su caso se encuentra
              <strong>En Proceso</strong>. Nuestro equipo está trabajando para
              resolverlo a la brevedad.
            </div>
          </td>
        </tr>
      </table>

      <div class='grid'>
        <div class='card'>
          <h3>Solicitante</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nombreUsuarioEnc}</strong></div>
        </div>

        <div class='card'>
          <h3>Colaborador afectado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nomAfectadoEnc}-{micasoatender.carnetResponsable}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAfectadoEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAfectadoEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAfectadoEnc}</div>
        </div>

        <div class='card'>
          <h3>Soporte asignado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nomSoporteEnc}</strong></div>
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
                        TipoCaso = micasoatender.TipoCaso,
                        Estado = estado,
                        Asunto = titulo1,
                        ContenidoHtml = m
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                idcorreo = correoCasoId;
            }
            return getcorreohelpapi("", "", titulo1, mensaje, id);
        }


        public string GenerarCorreoCierreCaso(int id, string estado)
        {
            var micasoatender = ObtenerCasoPorId(id);

            string notasCierreEnc = HttpUtility.HtmlEncode(micasoatender.NotasCierre ?? "-");
            string tituloEnc = HttpUtility.HtmlEncode(micasoatender.Titulo ?? "-");
            string descripcionEnc = HttpUtility.HtmlEncode(micasoatender?.Descripcion ?? "-");
            string tipoCasoEnc = HttpUtility.HtmlEncode(micasoatender?.TipoCaso ?? "-");

            DateTime inicio = micasoatender?.FechaCreacion ?? DateTime.Now;
            DateTime fin = micasoatender?.FechaFinalizacion ?? DateTime.Now;
            if (fin < inicio) fin = inicio;
            string fIniTxt = inicio.ToString("dd/MM/yyyy HH:mm");
            string fFinTxt = fin.ToString("dd/MM/yyyy HH:mm");
            TimeSpan ts = fin - inicio;
            string duracionTxt = $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";

            string nombreUsuarioEnc = HttpUtility.HtmlEncode(micasoatender?.NombreAutor ?? "-");
            string nomAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.TelefonoResponsable ?? "-");

            string nomSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Nombresoport ?? "Mesa de Ayuda");
            string cargoSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Cargosoport ?? "-");
            string areaSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Areasoport ?? "-");
            string telSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Telefonosoport ?? "-");

            string titulo1 = $"Helpdesk Tick-{id}: {tituloEnc}";

            // Ubicación / Impacto
            bool esHS = (micasoatender?.TipoCasoID == 5)
                        || ((micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("higiene")
                            && (micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("edificio"));
            string depEnc = HttpUtility.HtmlEncode(micasoatender?.Departamento ?? "-");
            string ediEnc = HttpUtility.HtmlEncode(micasoatender?.Edificio ?? "-");
            string perEnc = HttpUtility.HtmlEncode(micasoatender?.CantidadAfectados?.ToString() ?? "-");
            string diasEnc = HttpUtility.HtmlEncode(micasoatender?.DiasCondicion?.ToString() ?? "-");

            string bloqueUbicacion = esHS
                ? $@"<tr>
               <th>Ubicación / Impacto</th>
               <td>
                 <div><strong>Departamento:</strong> {depEnc}</div>
                 <div><strong>Edificio:</strong> {ediEnc}</div>
                 <div><strong>Personal afectado:</strong> {perEnc}</div>
                 <div><strong>Días con la condición:</strong> {diasEnc}</div>
               </td>
             </tr>"
                : "";

            // ===== HTML con estructura tipo Asignación (verde cierre) =====
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;
        box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#16a34a;color:#fff;padding:22px 26px}}
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;
           gap:8px;align-items:center}}
  .ticket,.titulo,.cierre{{white-space:nowrap}}
  .dash{{opacity:.8}}
  .badge-ok{{display:inline-block;background:#e8f7ee;color:#14532d;
             border:1px solid #c6f1d5;padding:4px 10px;border-radius:999px;
             font-size:12px}}
  .cnt{{padding:22px 26px}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;
         padding:6px 10px;font-size:12px;background:#f9fafb}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;
                             font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .blockq{{background:#f8f9fa;border-left:5px solid #16a34a;margin:16px 0;
           padding:14px 16px;color:#374151}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;
         padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}}
  .pair span{{color:#6b7280}}
  .sig{{border-top:1px solid #e5e7eb;margin-top:18px;padding-top:14px;
        font-size:13px;color:#111}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;
        padding:16px}}
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
        {bloqueUbicacion}
        <tr>
          <th>Notas de cierre</th>
          <td>
            <div class='blockq'>{notasCierreEnc}</div>
          </td>
        </tr>
      </table>

      <div class='grid'>
        <div class='card'>
          <h3>Solicitante</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nombreUsuarioEnc}</strong></div>
        </div>

        <div class='card'>
          <h3>Colaborador afectado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nomAfectadoEnc}-{micasoatender.carnetResponsable}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoAfectadoEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaAfectadoEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telAfectadoEnc}</div>
        </div>

        <div class='card'>
          <h3>Soporte que atendió</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{nomSoporteEnc}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoSoporteEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaSoporteEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telSoporteEnc}</div>
        </div>
      </div>

      <div class='sig'>
        Ticket cerrado automáticamente. Si el inconveniente persiste,
        por favor cree un nuevo ticket desde el portal.
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
                        TipoCaso = micasoatender.TipoCaso,
                        Estado = estado,
                        Asunto = titulo1,
                        ContenidoHtml = m
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                idcorreo = correoCasoId;
            }
            return getcorreohelpapi("", "", titulo1, mensaje, id);
        }


        public string GenerarCorreoAsignacionCaso(int id, string estado, string nota = null)
        {
            // 1) Cargar caso actualizado (ya con soporte asignado)
            var micasoatender = ObtenerCasoPorId(id);

            // 2) Sanitizar
            string nombreUsuarioEnc = HttpUtility.HtmlEncode(micasoatender?.NombreAutor ?? "-");
            string tituloEnc = HttpUtility.HtmlEncode(micasoatender?.Titulo ?? "-");
            string descripcionEnc = HttpUtility.HtmlEncode(micasoatender?.Descripcion ?? "-");
            string tipoCasoEnc = HttpUtility.HtmlEncode(micasoatender?.TipoCaso ?? "-");

            DateTime inicio = micasoatender?.FechaCreacion ?? DateTime.Now;
            DateTime ahora = DateTime.Now; if (ahora < inicio) ahora = inicio;
            string fIniTxt = inicio.ToString("dd/MM/yyyy HH:mm");
            string fActTxt = ahora.ToString("dd/MM/yyyy HH:mm");
            var ts = ahora - inicio;
            string duracionTxt = $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";

            // Afectado
            string nomAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.TelefonoResponsable ?? "-");

            // Soporte asignado
            string nomSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Nombresoport ?? "Mesa de Ayuda");
            string cargoSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Cargosoport ?? "-");
            string areaSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Areasoport ?? "-");
            string telSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Telefonosoport ?? "-");

            // Nota opcional del asignador
            string notaEnc = HttpUtility.HtmlEncode(nota ?? "-");

            // === NUEVO: detectar HS/edificio (id 41 o por texto) + armar bloque Ubicación/Impacto
            bool esHS = (micasoatender?.TipoCasoID == 5)
                        || ((micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("higiene")
                            && (micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("edificio"));

            string depEnc = HttpUtility.HtmlEncode(micasoatender?.Departamento ?? "-");
            string ediEnc = HttpUtility.HtmlEncode(micasoatender?.Edificio ?? "-");
            string perEnc = HttpUtility.HtmlEncode(micasoatender?.CantidadAfectados?.ToString() ?? "-");
            string diasEnc = HttpUtility.HtmlEncode(micasoatender?.DiasCondicion?.ToString() ?? "-");

            string bloqueUbicacion = esHS
                ? $@"<tr>
               <th>Ubicación / Impacto</th>
               <td>
                 <div><strong>Departamento:</strong> {depEnc}</div>
                 <div><strong>Edificio:</strong> {ediEnc}</div>
                 <div><strong>Personal afectado:</strong> {perEnc}</div>
                 <div><strong>Días con la condición:</strong> {diasEnc}</div>
               </td>
             </tr>"
                : "";

            // 3) Asunto
            string asunto = $"Helpdesk Tick-{id}: {tituloEnc}";

            // 4) HTML
            string mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
 <style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#facc15;color:#fff;padding:22px 26px}} /* reemplazo azul fuerte -> amarillo */
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;gap:8px;align-items:center}}
  .ticket,.titulo,.act{{white-space:nowrap}}
  .dash{{opacity:.8}}
  .badge-asig{{display:inline-block;background:#fef9c3;color:#854d0e;border:1px solid #fde68a;padding:4px 10px;border-radius:999px;font-size:12px}} /* paleta amarilla equivalente */
  .cnt{{padding:22px 26px}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;padding:6px 10px;font-size:12px;background:#f3f4f6;color:#111}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .blockq{{background:#f8f9fa;border-left:5px solid #facc15;margin:16px 0;padding:14px 16px;color:#374151}} /* borde lateral amarillo */
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}} .pair span{{color:#6b7280}}
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
        <span class='badge-asig'>Estado: {estado}</span>
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
          <th>Descripción</th>
          <td>{descripcionEnc}</td>
        </tr>
        {bloqueUbicacion}
        <tr>
          <th>Nota del asignador</th>
          <td><div class='blockq'>{notaEnc}</div></td>
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
          <div class='pair'><span>Cargo:&nbsp;</span>{cargoSoporteEnc}</div>
          <div class='pair'><span>Área:&nbsp;</span>{areaSoporteEnc}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{telSoporteEnc}</div>
        </div>
      </div>
    </div>

    <div class='ftr'>Mensaje automático, no responder.</div>
  </div>
</body>
</html>";

            // 5) Persistir e enviar (igual que tenías)
            int idcorreo = 0;
            string m = EncodeHtmlToNumeric(mensaje);
            using (var db = new SqlConnection(connectionString))
            {
                idcorreo = db.ExecuteScalar<int>(
                    "dbo.usp_CorreoCaso_Insert",
                    new
                    {
                        CasoID = id,
                        TipoCaso = micasoatender.TipoCaso,
                        Estado = estado,
                        Asunto = asunto,
                        ContenidoHtml = m
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );
            }
            return getcorreohelpapi("", "", asunto, "", id);
        }
        // ====== Controller ======
        [HttpPost]
        public ActionResult NotificarNotaCaso(int casoId, string estado, string mensaje)
        {
            try
            {
                // Genera HTML, registra en tabla de correo y envía usando la misma lógica de cierre
                string resultado = GenerarCorreoNotaSeguimiento(casoId, estado, mensaje);
                return Json(new { success = true, message = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult NotificarNotaCasousuario(int casoId, string mensaje)
        {
            try
            {
                // siempre tomo el estado actual del caso desde BD


                string resultado = GenerarCorreoNotaSeguimiento(casoId, "", mensaje);
                return Json(new { success = true, message = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ====== Nuevo: correo para NOTA de seguimiento (una sola nota, la recién guardada) ======
        public string GenerarCorreoNotaSeguimiento(int id, string estado, string nota)
        {
            var micasoatender = ObtenerCasoPorId(id);

            // Nota recién guardada (no historial)
            string notaEnc = HttpUtility.HtmlEncode(nota ?? "-");

            string tituloEnc = HttpUtility.HtmlEncode(micasoatender?.Titulo ?? "-");
            string descripcionEnc = HttpUtility.HtmlEncode(micasoatender?.Descripcion ?? "-");
            string tipoCasoEnc = HttpUtility.HtmlEncode(micasoatender?.TipoCaso ?? "-");

            DateTime inicio = micasoatender?.FechaCreacion ?? DateTime.Now;
            DateTime fin = DateTime.Now; // ahora = momento de la nota
            if (fin < inicio) fin = inicio;

            string fIniTxt = inicio.ToString("dd/MM/yyyy HH:mm");
            string fFinTxt = fin.ToString("dd/MM/yyyy HH:mm");
            TimeSpan ts = fin - inicio;
            string duracionTxt = string.Format("{0}d {1}h {2}m", (int)ts.TotalDays, ts.Hours, ts.Minutes);

            string nombreUsuarioEnc = HttpUtility.HtmlEncode(micasoatender?.NombreAutor ?? "-");
            string nomAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.NombreResponsable ?? "-");
            string cargoAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.CargoResponsable ?? "-");
            string areaAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.AreaResponsable ?? "-");
            string telAfectadoEnc = HttpUtility.HtmlEncode(micasoatender?.TelefonoResponsable ?? "-");

            string nomSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Nombresoport ?? "Mesa de Ayuda");
            string cargoSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Cargosoport ?? "-");
            string areaSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Areasoport ?? "-");
            string telSoporteEnc = HttpUtility.HtmlEncode(micasoatender?.Telefonosoport ?? "-");

            string titulo1 = string.Format("Helpdesk Tick-{0}: {1} (Actualización)", id, tituloEnc);

            // HS (igual que en cierre)
            bool esHS = (micasoatender?.TipoCasoID == 5)
                        || ((micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("higiene")
                            && (micasoatender?.TipoCaso ?? "").ToLowerInvariant().Contains("edificio"));
            string depEnc = HttpUtility.HtmlEncode(micasoatender?.Departamento ?? "-");
            string ediEnc = HttpUtility.HtmlEncode(micasoatender?.Edificio ?? "-");
            string perEnc = HttpUtility.HtmlEncode(micasoatender?.CantidadAfectados?.ToString() ?? "-");
            string diasEnc = HttpUtility.HtmlEncode(micasoatender?.DiasCondicion?.ToString() ?? "-");

            string bloqueUbicacion = esHS
                ? string.Format(@"
        <tr>
            <th>Ubicación / Impacto</th>
            <td>
                <div><strong>Departamento:</strong> {0}</div>
                <div><strong>Edificio:</strong> {1}</div>
                <div><strong>Personal afectado:</strong> {2}</div>
                <div><strong>Días con la condición:</strong> {3}</div>
            </td>
        </tr>", depEnc, ediEnc, perEnc, diasEnc)
                : string.Empty;

            // ===== HTML tipo “trabajo correo”, similar al de cierre pero naranja = nota =====
            // 👇 Antes del string.Format: calculamos si la nota es de usuario o de soporte
            string etiquetaNota = string.IsNullOrWhiteSpace(estado)
                ? "Última nota de usuario"
                : "Última nota de soporte";

            string mensaje = string.Format(@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;
        box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#f59e0b;color:#111;padding:22px 26px}}
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;
           gap:8px;align-items:center}}
  .ticket,.titulo,.cierre{{white-space:nowrap}}
  .dash{{opacity:.8}}
  .badge-ok{{display:inline-block;background:#fef3c7;color:#92400e;
             border:1px solid #facc15;padding:4px 10px;border-radius:999px;
             font-size:12px}}
  .cnt{{padding:22px 26px}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;
         padding:6px 10px;font-size:12px;background:#f9fafb}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;
                             font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .blockq{{background:#f8f9fa;border-left:5px solid #f59e0b;margin:16px 0;
           padding:14px 16px;color:#374151}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;
         padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}}
  .pair span{{color:#6b7280}}
  .sig{{border-top:1px solid #e5e7eb;margin-top:18px;padding-top:14px;
        font-size:13px;color:#111}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;
        padding:16px}}
  @media (max-width:520px){{ .hdr h1{{font-size:18px}} .titulo{{flex:1 1 100%}} }}
</style>
</head>
<body>
  <div class='wrap'>
    <div class='hdr'>
      <h1>
        <span class='ticket'>Ticket #<strong>{0}</strong></span>
        <span class='dash'>—</span>
        <span class='titulo'><strong>{1}</strong></span>
        <span class='badge-ok'>Actualización: {2}</span>
      </h1>
    </div>

    <div class='cnt'>
      <div class='kpis'>
        <div class='pill'>Tipo de caso: <strong>{3}</strong></div>
        <div class='pill'>Creación: <strong>{4}</strong></div>
        <div class='pill'>Últ. actualización: <strong>{5}</strong></div>
        <div class='pill'>Duración: <strong>{6}</strong></div>
      </div>

      <table class='sum' role='presentation' aria-hidden='true'>
        <tr>
          <th>Resumen del caso</th>
          <td>{7}</td>
        </tr>
        {8}
        <tr>
          <th>{20}</th> <!-- 👈 aquí va dinámica: usuario/soporte -->
          <td>
            <div class='blockq'>{9}</div>
          </td>
        </tr>
      </table>

      <div class='grid'>
        <div class='card'>
          <h3>Solicitante</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{10}</strong></div>
        </div>

        <div class='card'>
          <h3>Colaborador afectado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{11}-{12}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{13}</div>
          <div class='pair'><span>Área:&nbsp;</span>{14}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{15}</div>
        </div>

        <div class='card'>
          <h3>Soporte asignado</h3>
          <div class='pair'><span>Nombre:&nbsp;</span>
            <strong>{16}</strong></div>
          <div class='pair'><span>Cargo:&nbsp;</span>{17}</div>
          <div class='pair'><span>Área:&nbsp;</span>{18}</div>
          <div class='pair'><span>Teléfono:&nbsp;</span>{19}</div>
        </div>
      </div>

      <div class='sig'>
        Esta es una notificación automática de actualización de caso.
        Si requiere responder, hágalo desde el portal de Helpdesk.
      </div>
    </div>

    <div class='ftr'>
      Mensaje automático, no responder a este correo.
    </div>
  </div>
</body>
</html>",
                id,                              // {0}
                tituloEnc,                       // {1}
                HttpUtility.HtmlEncode(estado ?? micasoatender?.Estado ?? "-"), // {2}
                tipoCasoEnc,                     // {3}
                fIniTxt,                         // {4}
                fFinTxt,                         // {5}
                duracionTxt,                     // {6}
                descripcionEnc,                  // {7}
                bloqueUbicacion,                 // {8}
                notaEnc,                         // {9}
                nombreUsuarioEnc,                // {10}
                nomAfectadoEnc,                  // {11}
                HttpUtility.HtmlEncode(micasoatender?.carnetResponsable ?? "-"), // {12}
                cargoAfectadoEnc,                // {13}
                areaAfectadoEnc,                 // {14}
                telAfectadoEnc,                  // {15}
                nomSoporteEnc,                   // {16}
                cargoSoporteEnc,                 // {17}
                areaSoporteEnc,                  // {18}
                telSoporteEnc,                   // {19}
                HttpUtility.HtmlEncode(etiquetaNota) // {20} ← "Última nota de usuario/soporte"
            );


            // Guarda correo en tabla (igual patrón que cierre)
            int idcorreo = 0;
            string mNum = EncodeHtmlToNumeric(mensaje);
            using (var db = new SqlConnection(connectionString))
            {
                var correoCasoId = db.ExecuteScalar<int>(
                    "dbo.usp_CorreoCaso_Insert",
                    new
                    {
                        CasoID = id,
                        TipoCaso = micasoatender.TipoCaso,
                        Estado = estado,
                        Asunto = titulo1,
                        ContenidoHtml = mNum
                    },
                    commandType: CommandType.StoredProcedure
                );
                idcorreo = correoCasoId;
            }

            // Envía usando el mismo helper que ya usas en cierre
            return getcorreohelpapi("", "", titulo1, mensaje, id);
        }

        // Asignación → Soporte asignado + autor
        private string EnviarCorreoAsignacion(int casoID, string soporteID, string areaSoporte)
        {
            return GenerarCorreoAsignacionCaso(casoID, "Asignado", areaSoporte);
            //                var c = ObtenerCasoPorId(casoID);
            //                string soporteTxt = HttpUtility.HtmlEncode(soporteID ?? "-");
            //                string areaTxt = HttpUtility.HtmlEncode(areaSoporte ?? "-");
            //                string titulo = HttpUtility.HtmlEncode(c?.Titulo ?? "-");
            //                string asunto = $"Helpdesk Tick-{casoID}: Asignado a {soporteTxt} ({areaTxt})";
            //                string html = $@"<html><body><div style='font-family:Arial'>
            //<h2>Ticket asignado</h2>
            //<p>El ticket <b>#{casoID}</b> — <b>{titulo}</b> fue asignado a <b>{soporteTxt}</b> ({areaTxt}).</p>
            //</div></body></html>";

            //                using (var db = new SqlConnection(connectionString))
            //                {
            //                    var idcorreo = db.ExecuteScalar<int>(
            //                        "dbo.usp_CorreoCaso_Insert",
            //                        new { CasoID = casoID, TipoCaso = c.TipoCaso, Estado = "Asignado", Asunto = asunto, ContenidoHtml = EncodeHtmlToNumeric(html) },
            //                        commandType: CommandType.StoredProcedure
            //                    );
            //                }
            //                return getcorreohelpapi("", "", asunto, "0", casoID);
        }

        /* ====== Transporte correo ====== */

        // API (HTTP GET simple)
        public string getcorreohelpapi(string correo, string copia, string titulo, string mensaje, int id)
        {
            try
            {
                string apiUrl = $"{correoApiBase}?correo=1&titulo={Uri.EscapeDataString(titulo)}&destinatarioCopia=prueba&mensaje={id}";
                var client = new RestClient(apiUrl) { Timeout = -1 };
                var request = new RestRequest(Method.GET);
                var resp = client.Execute(request)?.Content ?? "";
                return resp.Contains("EXITO") ? "EXITO" : resp;
            }
            catch (Exception e)
            {
                return "no se envio :" + e.Message;
            }
        }
        public class AdminGuardarSoporteRequest
        {
            public string SoporteID { get; set; }
            public string Email { get; set; }
            public string Nombre { get; set; }
            public string Area { get; set; }
            public bool Activo { get; set; }
            public bool EsAdmin { get; set; }
            public bool EsSuper { get; set; }
            public PermisoReq[] Permisos { get; set; }
        }
        public class GuardarSoporteReq
        {
            public string SoporteID { get; set; }
            public string Email { get; set; }
            public string Nombre { get; set; }
            public string Area { get; set; }
            public bool Activo { get; set; }
            public bool EsAdmin { get; set; }
            public bool EsSuper { get; set; }
            public List<SoportePermisoReq> Permisos { get; set; }
        }
        public class SoportePermisoReq
        {
            public int TipoCasoID { get; set; }
            public int? SubtipoCasoID { get; set; }
        }
        public class PermisoReq
        {
            public int TipoCasoID { get; set; }
            public int? SubtipoCasoID { get; set; }
        }
        [HttpGet]
        public JsonResult Admin_ObtenerSoporteDetalle(string soporteId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(soporteId))
                    return Json(new { success = 0, message = "SoporteID requerido" }, JsonRequestBehavior.AllowGet);

                using (var cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    // TABLA real: Soporte (sin S al final)
                    const string sql = @"
SELECT TOP 1
    SoporteID, Email, Nombre, Area, Activo, FechaUpd, EsAdmin, EsSuper
FROM Soporte
WHERE SoporteID = @SoporteID;
";
                    var d = cn.QueryFirstOrDefault(sql, new { SoporteID = soporteId.Trim() });

                    return Json(new { success = 1, data = d }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = 0, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult Admin_ObtenerCatalogoPermisos(string soporteId)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    // IMPORTANTÍSIMO:
                    // - Tu tabla de permisos asignados es [dbo].[SoportePermiso]
                    // - Aquí solo armo el dataset para pintar la tabla:
                    //   TipoCaso + SubtipoCaso + Permitido
                    //
                    // Ajusta nombres de catálogos si en tu BD se llaman distinto.
                    //
                    // Asumido (típico):
                    //   TipoCaso(TipoCasoID, Nombre)
                    //   SubtipoCaso(SubtipoCasoID, TipoCasoID, Nombre)
                    //
                    // Si tus tablas se llaman diferente, cambia SOLO este SQL.

                    const string sql = @"
DECLARE @SoporteID VARCHAR(200) = @pSoporteID;

;WITH Cat AS (
    SELECT
        tc.TipoCasoID,
        tc.Nombre AS TipoCaso,
        CAST(NULL AS INT) AS SubtipoCasoID,
        CAST(NULL AS VARCHAR(200)) AS SubtipoCaso
    FROM TipoCaso tc

    UNION ALL

    SELECT
        st.TipoCasoID,
        tc.Nombre AS TipoCaso,
        st.SubtipoCasoID,
        st.Nombre AS SubtipoCaso
    FROM SubtipoCaso st
    INNER JOIN TipoCaso tc ON tc.TipoCasoID = st.TipoCasoID
)
SELECT
    c.TipoCasoID,
    c.TipoCaso,
    c.SubtipoCasoID,
    c.SubtipoCaso,
    CASE WHEN EXISTS (
        SELECT 1
        FROM SoportePermiso sp
        WHERE sp.SoporteID = @SoporteID
          AND sp.Activo = 1
          AND sp.TipoCasoID = c.TipoCasoID
          AND (
              (c.SubtipoCasoID IS NULL AND sp.SubtipoCasoID IS NULL)
              OR (c.SubtipoCasoID IS NOT NULL AND sp.SubtipoCasoID = c.SubtipoCasoID)
          )
    ) THEN 1 ELSE 0 END AS Permitido
FROM Cat c
ORDER BY c.TipoCasoID, CASE WHEN c.SubtipoCasoID IS NULL THEN 0 ELSE 1 END, c.SubtipoCasoID;
";

                    var rows = cn.Query(sql, new { pSoporteID = (soporteId ?? "").Trim() }).ToList();
                    return Json(rows, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = 0, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]

        public ActionResult AdminSoportePermisos( )
        {
             return View();
        }
        
        // POST: /Helpdesk2025/Admin_GuardarSoporte
        [HttpPost]
        public JsonResult Admin_GuardarSoporte(AdminGuardarSoporteRequest req)
        {
            try
            {
                // ===== Validaciones mínimas =====
                var soporteId = (req.SoporteID ?? "").Trim();
                var email = (req.Email ?? "").Trim();
                var nombre = (req.Nombre ?? "").Trim();

                if (string.IsNullOrWhiteSpace(soporteId) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(nombre))
                {
                    return Json(new { success = false, message = "SoporteID, Email y Nombre son obligatorios." });
                }

                if (req.Permisos == null || req.Permisos.Length == 0)
                {
                    return Json(new { success = false, message = "Debe seleccionar al menos un permiso." });
                }

                // Normaliza permisos (sin basura / duplicados)
                var permisos = req.Permisos
                    .Where(x => x != null && x.TipoCasoID > 0)
                    .GroupBy(x => new { x.TipoCasoID, x.SubtipoCasoID })
                    .Select(g => new { TipoCasoID = g.Key.TipoCasoID, SubtipoCasoID = g.Key.SubtipoCasoID })
                    .ToList();

                if (permisos.Count == 0)
                    return Json(new { success = false, message = "Hay permisos inválidos (TipoCasoID)." });

                // JSON que consume el SP: [{ "TipoCasoID":4, "SubtipoCasoID":null }, ...]
                var permisosJson = JsonConvert.SerializeObject(permisos);

                using (var cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    // Llama el SP que te dejé: dbo.sp_Admin_GuardarSoporte
                    // Llama el SP: dbo.sp_Admin_GuardarSoporte
                    var p = new DynamicParameters();
                    p.Add("@SoporteID", soporteId, DbType.String);
                    p.Add("@Email", email, DbType.String);
                    p.Add("@Nombre", nombre, DbType.String);
                    p.Add("@Area", (req.Area ?? "").Trim(), DbType.String);
                    p.Add("@Activo", req.Activo, DbType.Boolean);
                    p.Add("@PermisosJson", permisosJson, DbType.String);

                    // El SP retorna: success, message
                    var r = cn.QueryFirstOrDefault<dynamic>(
                        "dbo.sp_Admin_GuardarSoporte",
                        p,
                        commandType: CommandType.StoredProcedure
                    );

                    // Por si el SP retorna null por alguna razón
                    if (r == null)
                        return Json(new { success = false, message = "No hubo respuesta del procedimiento." });
                    // dynamic -> object seguro
                    object rawSuccess = null;
                    object rawMessage = null;

                    try { rawSuccess = r.success; } catch { }
                    try { rawMessage = r.message; } catch { }

                    return Json(new
                    {
                        success = ToBool(rawSuccess),
                        message = rawMessage == null ? "" : rawMessage.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private static bool ToBool(object v)
        {
            if (v == null || v is DBNull) return false;

            // bool real
            if (v is bool) return (bool)v;

            // numéricos
            if (v is byte) return ((byte)v) != 0;
            if (v is short) return ((short)v) != 0;
            if (v is int) return ((int)v) != 0;
            if (v is long) return ((long)v) != 0;

            // string: "1","0","true","false","Y","N"
            var s = v.ToString().Trim();
            if (s == "1") return true;
            if (s == "0") return false;

            bool b;
            if (bool.TryParse(s, out b)) return b;

            int n;
            if (int.TryParse(s, out n)) return n != 0;

            return false;
        }
        // SMTP directo (fallback)
        public string getcorreohelp(string correo, string copia, string titulo, string mensaje, int id)
        {
            try
            {
                var email = new MailMessage
                {
                    From = new MailAddress(MAIL_FROM),
                    Subject = titulo,
                    SubjectEncoding = Encoding.UTF8,
                    Body = mensaje,
                    BodyEncoding = Encoding.UTF8,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };
                email.To.Add("gustavo.lira@claro.com.ni");

                ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, e) => true;
                var smtp = new SmtpClient(MAIL_SMTP_HOST, MAIL_SMTP_PORT)
                {
                    Credentials = new NetworkCredential(MAIL_SMTP_USER, MAIL_SMTP_PASS),
                    EnableSsl = true
                };
                smtp.Send(email);
                email.Dispose();
                return "EXITO";
            }
            catch (Exception ex)
            {
                return ex.InnerException?.Message ?? ex.Message;
            }
        }

        /* ====== Utilidades ====== */

        public static string EncodeHtmlToNumeric(string html)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                byte[] compressed = ms.ToArray();
                byte[] positive = compressed.Concat(new byte[] { 0 }).ToArray();
                BigInteger bigInt = new BigInteger(positive);
                return bigInt.ToString();
            }
        }
        // ====== C# (ASP.NET MVC + Dapper) ======
        // Modelos mínimos
        public class SoporteVisibilidadDto
        {
            public int ID { get; set; }                 // PK relación
            public string ViewerID { get; set; }        // Soporte que verá
            public string CarnetTarget { get; set; }    // Colaborador/Soporte objetivo
            public string NombreTarget { get; set; }    // Nombre objetivo (opcional)
            public DateTime Fecha { get; set; }         // Fecha alta
        }
        public class SoporteVisibilidadDto2
        {
            public int ID { get; set; }              // v.Id
            public string ViewerID { get; set; }     // v.ViewerSoporteID
            public string CarnetTarget { get; set; } // v.TargetSoporteID
            public string CorreoTarget { get; set; } // s.Email o el mismo TargetSoporteID
            public string NombreTarget { get; set; } // s.Nombre o el mismo TargetSoporteID
            public DateTime Fecha { get; set; }      // v.Fecha (si la tienes)

            // Texto final para mostrar en la tabla
            public string Etiqueta
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(CorreoTarget)) return CorreoTarget.Trim();
                    if (!string.IsNullOrWhiteSpace(NombreTarget)) return NombreTarget.Trim();
                    if (!string.IsNullOrWhiteSpace(CarnetTarget)) return CarnetTarget.Trim();
                    return "-";
                }
            }
        }
        public class RespuestaJson<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }

        // ====== Endpoints para “ver lo de otro soporte” ======
        [HttpGet]
        public async Task<JsonResult> Admin_GetVisibilidad(string viewerId)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    var list = await cn.QueryAsync<SoporteVisibilidadDto>(
                        "dbo.usp_SoporteVisibilidad_Listar",
                        new { ViewerID = viewerId },
                        commandType: CommandType.StoredProcedure);

                    return Json(new RespuestaJson<IEnumerable<SoporteVisibilidadDto>>
                    {
                        Success = true,
                        Data = list
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new RespuestaJson<object>
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Admin_AddVisibilidad(string viewerId, string targetId)
        {
            if (string.IsNullOrWhiteSpace(viewerId) || string.IsNullOrWhiteSpace(targetId))
                return Json(new { Success = false, Message = "viewerId y targetId son obligatorios." });

            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    // SP debe validar duplicados y existencia
                    var id = await cn.ExecuteScalarAsync<int>(
                        "dbo.usp_SoporteVisibilidad_Agregar",
                        new { ViewerID = viewerId, TargetID = targetId },
                        commandType: CommandType.StoredProcedure);

                    return Json(new { Success = true, ID = id });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Admin_DelVisibilidad(int id)
        {
            if (id <= 0) return Json(new { Success = false, Message = "ID inválido." });

            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    await cn.ExecuteAsync(
                        "dbo.usp_SoporteVisibilidad_Eliminar",
                        new { ID = id },
                        commandType: CommandType.StoredProcedure);

                    return Json(new { Success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        //[HttpPost]
        //public async Task<JsonResult> GuardarSoporte(SoporteUpsertRequest m)
        //{
        //    if (m == null || string.IsNullOrWhiteSpace(m.SoporteID) ||
        //        string.IsNullOrWhiteSpace(m.Email) || string.IsNullOrWhiteSpace(m.Nombre))
        //        return Json(new { success = false, message = "Datos de soporte incompletos." });

        //    using (var db = new SqlConnection(csHelpdesk))
        //    {
        //        await db.OpenAsync();
        //        using (var tx = db.BeginTransaction())
        //        {
        //            try
        //            {
        //                await db.ExecuteAsync("dbo.usp_Soporte_Upsert", new
        //                {
        //                    SoporteID = m.SoporteID,
        //                    Email = m.Email,
        //                    Nombre = m.Nombre,
        //                    Area = m.Area,
        //                    Activo = m.Activo
        //                }, tx, (int?)CommandType.StoredProcedure);

        //                // TVP permisos
        //                var tvp = new DataTable();
        //                tvp.Columns.Add("TipoCasoID", typeof(int));
        //                tvp.Columns.Add("SubtipoCasoID", typeof(int));
        //                if (m.Permisos != null)
        //                {
        //                    foreach (var p in m.Permisos)
        //                    {
        //                        var row = tvp.NewRow();
        //                        row["TipoCasoID"] = p.TipoCasoID;
        //                        row["SubtipoCasoID"] = (object)(p.SubtipoCasoID ?? (int?)null) ?? DBNull.Value;
        //                        tvp.Rows.Add(row);
        //                    }
        //                }

        //                var dp = new DynamicParameters();
        //                dp.Add("@SoporteID", m.SoporteID);
        //                dp.Add("@Permisos", tvp.AsTableValuedParameter("dbo.TVP_SoporteTipo"));

        //                await db.ExecuteAsync("dbo.usp_SoporteTipo_Reemplazar", dp, tx, (int?)CommandType.StoredProcedure);

        //                tx.Commit();
        //                return Json(new { success = true, message = "Soporte guardado correctamente." });
        //            }
        //            catch (Exception ex)
        //            {
        //                tx.Rollback();
        //                return Json(new { success = false, message = "Error al guardar soporte: " + ex.Message });
        //            }
        //        }
        //    }
        //}
        [HttpPost]
        public async Task<JsonResult> GuardarSoporte(SoporteUpsertRequest m)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.SoporteID) || string.IsNullOrWhiteSpace(m.Email) || string.IsNullOrWhiteSpace(m.Nombre))
                return Json(new { success = false, message = "Datos de soporte incompletos." });

            using (var db = new SqlConnection(csHelpdesk))
            {
                await db.OpenAsync();
                using (var tx = db.BeginTransaction())
                {
                    try
                    {
                        // Upsert soporte
                        await db.ExecuteAsync("dbo.usp_Soporte_Upsert", new
                        {
                            SoporteID = m.SoporteID,
                            Email = m.Email,
                            Nombre = m.Nombre,
                            Area = m.Area,
                            Activo = m.Activo
                        }, transaction: tx, commandType: CommandType.StoredProcedure);

                        // TVP permisos
                        var tvp = new DataTable();
                        tvp.Columns.Add("TipoCasoID", typeof(int));
                        tvp.Columns.Add("SubtipoCasoID", typeof(int));
                        if (m.Permisos != null)
                        {
                            foreach (var p in m.Permisos)
                            {
                                var row = tvp.NewRow();
                                row["TipoCasoID"] = p.TipoCasoID;
                                row["SubtipoCasoID"] = (object)(p.SubtipoCasoID ?? (int?)null) ?? DBNull.Value;
                                tvp.Rows.Add(row);
                            }
                        }

                        var dp = new DynamicParameters();
                        dp.Add("@SoporteID", m.SoporteID, DbType.String);
                        dp.Add("@Permisos", tvp.AsTableValuedParameter("dbo.TVP_SoporteTipo"));

                        await db.ExecuteAsync("dbo.usp_SoporteTipo_Reemplazar", dp,
                            transaction: tx, commandType: CommandType.StoredProcedure);

                        tx.Commit();
                        return Json(new { success = true, message = "Soporte guardado correctamente." });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return Json(new { success = false, message = "Error al guardar soporte: " + ex.Message });
                    }
                }
            }
        }

        [HttpPost]
        public async Task<JsonResult> Admin_CambiarEstadoSoporte(string id, bool activo)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "SoporteID requerido." });

            using (var db = new SqlConnection(csHelpdesk))
            {
                var filas = await db.ExecuteScalarAsync<int>(
                    "dbo.usp_Soporte_CambiarEstado",
                    new { SoporteID = id, Activo = activo },
                    commandType: CommandType.StoredProcedure
                );
                return Json(new { success = filas > 0, message = filas > 0 ? "Estado actualizado." : "No se actualizó ningún registro." });
            }
        }
        [HttpGet]
        public async Task<JsonResult> ArbolTipoSubtipo()
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                using (var multi = await db.QueryMultipleAsync(
                    "dbo.usp_TipoSubtipo_Tree",
                    commandType: CommandType.StoredProcedure))
                {
                    var tipos = (await multi.ReadAsync<TipoNode>()).AsList();
                    var subs = (await multi.ReadAsync<SubNode>()).AsList();

                    var map = new Dictionary<int, TipoNode>();
                    foreach (var t in tipos)
                    {
                        t.Subtipos = new List<SubNode>();
                        map[t.TipoCasoID] = t;
                    }
                    foreach (var s in subs)
                    {
                        if (map.TryGetValue(s.TipoCasoID, out var t))
                            t.Subtipos.Add(s);
                    }
                    return Json(new { success = true, data = tipos }, JsonRequestBehavior.AllowGet);
                }
            }
        }
        [HttpGet]
        public JsonResult ObtenerSoportes(int tipoId, int? subtipoId)
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var data = db.Query<SoporteLite>(
                    "dbo.usp_Soportes_PorTipoSubtipo",
                    new { TipoCasoID = tipoId, SubtipoCasoID = subtipoId },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        public class SoporteLite { public string SoporteID { get; set; } public string Nombre { get; set; } public string Email { get; set; } public string Area { get; set; } }
        public class TipoNode
        {
            public int TipoCasoID { get; set; }
            public string Nombre { get; set; }
            public List<SubNode> Subtipos { get; set; }
        }
        public class SubNode
        {
            public int SubtipoCasoID { get; set; }
            public int TipoCasoID { get; set; }
            public string Nombre { get; set; }
        }

        #region soporte usuario
        public class SoporteAdminItem
        {
            public string SoporteID { get; set; }
            public string Email { get; set; }
            public string Nombre { get; set; }
            public string Area { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaUpd { get; set; }
            public int TotalPermisos { get; set; }
        }
        public class SoporteInfo
        {
            public string SoporteID { get; set; }
            public string Email { get; set; }
            public string Nombre { get; set; }
            public string Area { get; set; }
            public bool Activo { get; set; }
            public List<SoportePermisoDetalle> Permisos { get; set; }
        }
        public class SoportePermisoDetalle
        {
            public int SoporteTipoID { get; set; }
            public int TipoCasoID { get; set; }
            public string TipoNombre { get; set; }
            public int? SubtipoCasoID { get; set; }
            public string SubtipoNombre { get; set; }
        }



        // ===== DTOs =====


        // ===== Catálogos =====
        [HttpGet]
        public async Task<JsonResult> ListarTiposCasos()
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var lista = await db.QueryAsync<TipoItem>("dbo.usp_TipoCaso_Listar", commandType: CommandType.StoredProcedure);
                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<JsonResult> ListarSubtiposPorTipo(int tipoId)
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var lista = await db.QueryAsync<SubtipoItem>("dbo.usp_SubtipoCaso_ListarPorTipo",
                    new { TipoCasoID = tipoId }, commandType: CommandType.StoredProcedure);
                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===== Buscar empleados en sigho1.dbo.emp2024 =====
        [HttpGet]
        public async Task<JsonResult> BuscarEmpleados(string q)
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var lista = await db.QueryAsync<EmpleadoItem>("dbo.usp_Empleados_Listar",
                    new { q }, commandType: CommandType.StoredProcedure);
                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===== Guardar/Actualizar Soporte y su matriz de permisos =====
        [HttpGet]
        public ActionResult KPI()
        {
            return View();
        }

        [HttpGet]
        public JsonResult KPIResumen(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPIResumenDto>("dbo.usp_KPI_Casos_Resumen", p, commandType: CommandType.StoredProcedure).FirstOrDefault();
                return Json(new { success = true, data = data ?? new KPIResumenDto() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPIPorDia(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPIPorDiaDto>("dbo.usp_KPI_Casos_PorDia", p, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPIPorTipo(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPIPorTipoDto>("dbo.usp_KPI_Casos_PorTipo", p, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPIPorGerencia(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPIPorGerenciaDto>("dbo.usp_KPI_Casos_PorGerencia", p, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPITiempos(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPITiemposDto>("dbo.usp_KPI_Casos_Tiempos", p, commandType: CommandType.StoredProcedure).FirstOrDefault();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPITopUsuarios(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia, int? top)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);
            p.Add("@Top", top ?? 10);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPITopUsuarioDto>("dbo.usp_KPI_TopUsuarios", p, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        // ====== Filtros auxiliares ======
        [HttpGet]
        public ActionResult ExportKpiCsv(DateTime? desde, DateTime? hasta, string gerencia, int? tipoCasoId, int? subtipoCasoId, bool? soloAtendidos)
        {
            DateTime d1 = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime d2 = hasta ?? DateTime.Today;
            bool onlyAttended = soloAtendidos ?? true;

            using (var db = new SqlConnection(connectionString))
            {
                var rows = db.Query<KpiExportRow>(
                    "dbo.usp_KPI_Casos_Export",
                    new { Desde = d1, Hasta = d2, Gerencia = string.IsNullOrWhiteSpace(gerencia) ? null : gerencia, TipoCasoID = tipoCasoId, SubtipoCasoID = subtipoCasoId, SoloAtendidos = onlyAttended ? 1 : 0 },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("ID,FechaCreacion,FechaAtencion,FechaCierre,Tipo,Subtipo,Soporte,SoporteEmail,GerenciaAfectado,Titulo,Descripcion,NotasCierre");
                foreach (var x in rows)
                {
                    Func<string, string> esc = s => string.IsNullOrEmpty(s) ? "" : "\"" + s.Replace("\"", "\"\"") + "\"";
                    sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}\r\n",
                        x.ID,
                        x.FechaCreacion.ToString("yyyy-MM-dd HH:mm"),
                        x.FechaAtencion.HasValue ? x.FechaAtencion.Value.ToString("yyyy-MM-dd HH:mm") : "",
                        x.FechaFinalizacion.HasValue ? x.FechaFinalizacion.Value.ToString("yyyy-MM-dd HH:mm") : "",
                        esc(x.TipoNombre),
                        esc(x.SubtipoNombre),
                        esc(x.SoporteNombre),
                        esc(x.SoporteEmail),
                        esc(x.GerenciaAfectado),
                        esc(x.Titulo),
                        esc(x.Descripcion),
                        esc(x.NotasCierre)
                    );
                }

                var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                var fileName = string.Format("KPI_Casos_{0:yyyyMMdd}_{1:yyyyMMdd}.csv", d1, d2);
                return File(bytes, "text/csv", fileName);
            }
        }
        [HttpGet]
        public JsonResult KPI_ListarGerencias()
        {
            using (var db = new SqlConnection(connectionString))
            {
                var rows = db.Query<string>("SELECT DISTINCT OGERENCIA as GERENCIA FROM sigho1.dbo.emp2024 WHERE ISNULL(OGERENCIA,'')<>'' and  OGERENCIA!='NI COORD. COMERCIAL SF MGA. ESTE-1' ORDER BY OGERENCIA").ToList();
                return Json(new { success = true, data = rows }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPI_ListarTipos()
        {
            using (var db = new SqlConnection(connectionString))
            {
                var rows = db.Query<dynamic>("SELECT TipoCasoID, Nombre FROM dbo.TipoCaso ORDER BY Nombre").ToList();
                return Json(new { success = true, data = rows }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult KPI_ListarTiposYSubtipos()
        {
            using (var db = new SqlConnection(connectionString))
            {
                // Traemos Tipo/Subtipo en una sola consulta
                var rows = db.Query<dynamic>(@"
            SELECT 
                tc.TipoCasoID,
                tc.Nombre      AS TipoNombre,
                st.SubtipoCasoID,
                st.Nombre      AS SubtipoNombre
            FROM dbo.TipoCaso tc
            INNER JOIN dbo.SubtipoCaso st
                ON tc.TipoCasoID = st.TipoCasoID
            ORDER BY tc.Nombre, st.Nombre;
        ").ToList();

                // Armamos estructura { TipoCasoID, Nombre, Subtipos:[...] }
                var lookup = new Dictionary<int, TipoDto>();

                foreach (var r in rows)
                {
                    int tipoId = (int)r.TipoCasoID;

                    if (!lookup.ContainsKey(tipoId))
                    {
                        lookup[tipoId] = new TipoDto
                        {
                            TipoCasoID = tipoId,
                            Nombre = (string)r.TipoNombre,
                            Subtipos = new List<SubtipoDto>()
                        };
                    }

                    lookup[tipoId].Subtipos.Add(new SubtipoDto
                    {
                        SubtipoCasoID = (int)r.SubtipoCasoID,
                        Nombre = (string)r.SubtipoNombre
                    });
                }

                var result = lookup.Values
                                   .OrderBy(x => x.Nombre)
                                   .ToList();

                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
        }

        // DTOs locales (pueden ir dentro del Controller como clases privadas)
        private class TipoDto
        {
            public int TipoCasoID { get; set; }
            public string Nombre { get; set; }
            public List<SubtipoDto> Subtipos { get; set; }
        }

        private class SubtipoDto
        {
            public int SubtipoCasoID { get; set; }
            public string Nombre { get; set; }
        }

        [HttpGet]
        public JsonResult KPI_ListarSubtipos(int tipoCasoId)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var rows = db.Query<dynamic>("SELECT SubtipoCasoID, Nombre FROM dbo.SubtipoCaso WHERE TipoCasoID=@t ORDER BY Nombre", new { t = tipoCasoId }).ToList();
                return Json(new { success = true, data = rows }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Recategorizar(RecategorizacionVM vm)
        {
            if (vm == null || vm.CasoID <= 0 || vm.TipoCasoID <= 0 || vm.SubtipoCasoID <= 0)
                return Json(new { success = false, message = "Parámetros inválidos." });

            // usuario que ejecuta (ajusta según tu sesión)
            var usuarioAccion = (User != null && User.Identity != null) ? (User.Identity.Name ?? "") : "";

            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();

                    var p = new DynamicParameters();
                    p.Add("@CasoID", vm.CasoID, DbType.Int32);
                    p.Add("@TipoCasoID", vm.TipoCasoID, DbType.Int32);
                    p.Add("@SubtipoCasoID", vm.SubtipoCasoID, DbType.Int32);
                    p.Add("@Nota", vm.Nota, DbType.String);
                    p.Add("@UsuarioAccion", usuarioAccion, DbType.String);

                    // SP debe devolver una sola fila con los datos consolidados para correo
                    var dto = await cn.QueryFirstOrDefaultAsync<RecategorizacionDTO>(
                        "dbo.usp_Helpdesk_ReCategorizarCaso",
                        p, commandType: CommandType.StoredProcedure
                    );

                    if (dto == null)
                        return Json(new { success = false, message = "No se encontró el caso o no se aplicó el cambio." });

                    // genera HTML como "creación", pero indicando recategorización

                    // asunto claro
                    var asunto = "[Helpdesk] Caso #" + dto.ID + " recategorizado a " + (dto.TipoNuevo ?? "-") + " / " + (dto.SubtipoNuevo ?? "-");

                    // destinos (elige tu estrategia: del SP, o arma to/cc aquí)
                    var to = (dto.DestinatariosCSV ?? "").Trim();
                    if (string.IsNullOrEmpty(to))
                    {
                        // Fallback: autor + afectado + soporte
                        to = JoinCsv(dto.CorreoAutor, dto.CorreoResponsable, dto.CorreoSoporte);
                    }

                    // enviar – usa tu helper actual de envío HTML
                    // IMPORTANTE: reemplaza por tu servicio real (p.ej. CorreoHelper/CorreoService)
                    await GenerarCorreoRecategorizacionCasoAsync(vm.CasoID, dto);

                    return Json(new { success = true, message = "Recategorizado y notificado.", data = dto });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateInput(false)]

        public JsonResult RecategorizarCaso(int casoId, int tipoId, int subtipoId)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@CasoID", casoId, DbType.Int32);
                    p.Add("@TipoID", tipoId, DbType.Int32);
                    p.Add("@SubtipoID", subtipoId, DbType.Int32);

                    // OUTPUTs del SP
                    p.Add("@TipoAnterior", dbType: DbType.String, size: 120, direction: ParameterDirection.Output);
                    p.Add("@SubtipoAnterior", dbType: DbType.String, size: 120, direction: ParameterDirection.Output);
                    p.Add("@TipoNuevo", dbType: DbType.String, size: 120, direction: ParameterDirection.Output);
                    p.Add("@SubtipoNuevo", dbType: DbType.String, size: 120, direction: ParameterDirection.Output);

                    // Ejecuta 1 sola vez (SP hace todo y además retorna un SELECT para depurar si quieres)
                    cn.Execute("dbo.usp_Helpdesk_Caso_Recategorizacion_Detalle", p, commandType: CommandType.StoredProcedure);

                    // Armar DTO desde OUTPUTs
                    var dto = new RecategorizacionDTO
                    {
                        TipoAnterior = p.Get<string>("@TipoAnterior") ?? "-",
                        SubtipoAnterior = p.Get<string>("@SubtipoAnterior") ?? "-",
                        TipoNuevo = p.Get<string>("@TipoNuevo") ?? "-",
                        SubtipoNuevo = p.Get<string>("@SubtipoNuevo") ?? "-"
                    };

                    // Generar/registrar correo (bloqueante, sin async)
                    try { _ = GenerarCorreoRecategorizacionCasoAsync(casoId, dto); } catch { /* log opcional */ }

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }


        // PUT: Marcar En Proceso - se consume el API REST en la ruta: api/helpdesk/proceso?token=021092
        [HttpGet]
        [AllowAnonymous] // ← habilita prueba rápida; si no corresponde, quítalo
        public JsonResult ObtenerTiposCasos()
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    var tipos = cn.Query(
                        "usp_Helpdesk_Tipos_Listar",
                        commandType: CommandType.StoredProcedure
                    );
                    // clave: mismo contrato que KPIs → success + data
                    return Json(new { success = true, data = tipos }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public JsonResult ObtenerSubtiposPorTipo(int tipoId)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    var subtipos = cn.Query(
                        "usp_Helpdesk_Subtipos_PorTipo",
                        new { TipoID = tipoId },
                        commandType: CommandType.StoredProcedure
                    );
                    return Json(new { success = true, data = subtipos }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // === HTML correo (similar a "creación") pero enfatiza el cambio de categoría ===
        // C# 7.3 — Recategorización con mismo “look & flow” del correo de creación.
        // Usa misma paleta/estructura, inserta en usp_CorreoCaso_Insert y envía con getcorreohelpapi.
        private async Task<string> GenerarCorreoRecategorizacionCasoAsync(int id, RecategorizacionDTO dto)
        {
            var c = ObtenerCasoPorId(id);

            // Encode básico anti-inyección
            Func<string, string> E = s => HttpUtility.HtmlEncode(s ?? "-");
            Func<DateTime?, string> EDate = d => d.HasValue ? d.Value.ToString("dd/MM/yyyy HH:mm") : "-";

            // Campos (según DTO)
            var idTxt = c.ID;                                  // ID del caso
            var titulo = E(c.Titulo);
            var tipoAnt = E(dto.TipoAnterior);
            var subAnt = E(dto.SubtipoAnterior);
            var tipoNew = E(dto.TipoNuevo);
            var subNew = E(dto.SubtipoNuevo);
            var prioridad = E(c.Prioridad);
            var fCreTxt = EDate(c.FechaCreacion);
            var fRecTxt = DateTime.Now.ToString();         // si no existe en tu DTO, usa DateTime.Now
            if (fRecTxt == "-") fRecTxt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            var nomAutor = E(c.NombreAutor);
            var nomAfect = E(c.NombreResponsable);
            var nomSop = E(c.Nombresoport);

            var asunto = $"Helpdesk Tick-{idTxt}: Caso Recategorizado - {titulo}";

            // HTML unificado (mismo estilo que “Creación”)
            var mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#e11d48;color:#fff;padding:22px 26px}}
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;gap:8px;align-items:center}}
  .ticket,.titulo,.act{{white-space:nowrap}} .dash{{opacity:.8}}
  .badge-new{{display:inline-block;background:#fee2e2;color:#7f1d1d;border:1px solid #fecaca;padding:4px 10px;border-radius:999px;font-size:12px}}
  .cnt{{padding:22px 26px}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;padding:6px 10px;font-size:12px;background:#f3f4f6;color:#111}}
  .chg{{border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;margin:10px 0;background:#f9fafb}}
  .chg .row{{display:flex;flex-wrap:wrap;gap:10px;align-items:center}}
  .arrow{{opacity:.7;padding:0 6px}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}} .pair span{{color:#6b7280}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;padding:16px}}
  @media (max-width:520px){{ .hdr h1{{font-size:18px}} .titulo{{flex:1 1 100%}} }}
</style>
</head>
<body>
  <div class='wrap'>
    <div class='hdr'>
      <h1>
        <span class='ticket'>Ticket #<strong>{idTxt}</strong></span>
        <span class='dash'>—</span>
        <span class='titulo'><strong>{titulo}</strong></span>
        <span class='badge-new'>Recategorizado</span>
      </h1>
    </div>

    <div class='cnt'>
      <div class='kpis'>
        <div class='pill'>Tipo (antes): <strong>{tipoAnt} / {subAnt}</strong></div>
        <div class='pill'>Tipo (ahora): <strong>{tipoNew} / {subNew}</strong></div>
        <div class='pill'>Prioridad: <strong>{prioridad}</strong></div>
        <div class='pill'>Creación: <strong>{fCreTxt}</strong></div>
        <div class='pill'>Recategorización: <strong>{fRecTxt}</strong></div>
      </div>

      <div class='chg'>
        <div class='row'>
          <div><strong>Antes:</strong> {tipoAnt} / {subAnt}</div>
          <div class='arrow'>⟶</div>
          <div><strong>Ahora:</strong> {tipoNew} / {subNew}</div>
        </div>
      </div>

      <table class='sum' role='presentation' aria-hidden='true'>
        <tr><th>Solicitante</th><td>{nomAutor}</td></tr>
        <tr><th>Colaborador afectado</th><td>{nomAfect}</td></tr>
        <tr><th>Soporte</th><td>{nomSop}</td></tr>
      </table>

    
    </div>

    <div class='ftr'>Mensaje automático de Helpdesk. No responder a este correo.</div>
  </div>
</body>
</html>";

            // Persistencia + envío (mismo flujo que “Creación”)
            var payload = EncodeHtmlToNumeric(mensaje);           // ↳ convierte a entidades numéricas
            var idcorreo = 0;

            using (var db = new SqlConnection(connectionString))
            {
                idcorreo = db.ExecuteScalar<int>(
                    "dbo.usp_CorreoCaso_Insert",
                    new
                    {
                        CasoID = c.ID,
                        TipoCaso = tipoNew,                       // guarda tipo “nuevo” como referencia
                        Estado = "Pendiente",
                        Asunto = asunto,
                        ContenidoHtml = payload
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            // Destinatarios: soporte + autor (ajusta si tu DTO trae correo de autor/soporte)
            var apiResp = getcorreohelpapi("soporte@claro.com.ni", c.CorreoAutor ?? "", asunto, idcorreo.ToString(), c.ID);

            await Task.Yield(); // mantiene firma async sin bloqueos
            return apiResp;
        }

        // util: une correos en CSV evitando vacíos/duplicados
        private static string JoinCsv(params string[] correos)
        {
            var hs = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < correos.Length; i++)
            {
                var c = (correos[i] ?? "").Trim();
                if (c.Length > 0) hs.Add(c);
            }
            return string.Join(",", hs);
        }
    }
    #endregion

    public class KpiExportRow
    {
        public int ID { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaAtencion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
        public string SoporteNombre { get; set; }
        public string SoporteEmail { get; set; }
        public string GerenciaAfectado { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string NotasCierre { get; set; }
    }
    public class SoporteUpsertReq
    {
        public string SoporteID { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public List<PermisoLite> Permisos { get; set; } = new List<PermisoLite>();
    }
    public class PermisoLite
    {
        public int TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }
    }

    public class SoporteListadoDto
    {
        public string SoporteID { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaUpd { get; set; }
    }

    public class SoportePermisoDto
    {
        public int TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
    }
    public class CasoDetalle
    {
        public int ID { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaEliminacion { get; set; }
        public bool Eliminado { get; set; }
        public string UsuarioID { get; set; }
        public DateTime? FechaCreacionCaso { get; set; }
        public string Descripcion { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string Prioridad { get; set; }
        public string TipoCaso { get; set; }
        public DateTime? FechaAtencion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string NotasCierre { get; set; }
        public string SoporteID { get; set; }   // carnet
        public string Correo { get; set; }

        public string CorreoAutor { get; set; }
        public string NombreAutor { get; set; }
        public string CargoAutor { get; set; }
        public string AreaAutor { get; set; }
        public string TelefonoAutor { get; set; }
        public string GerenciaAutor { get; set; }

        public string NombreResponsable { get; set; }
        public string CargoResponsable { get; set; }
        public string AreaResponsable { get; set; }
        public string TelefonoResponsable { get; set; }

        public string Nombresoport { get; set; }
        public string Cargosoport { get; set; }
        public string Areasoport { get; set; }
        public string Telefonosoport { get; set; }
        public string CorreoSoport { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }

        // Campos para adjunto (se llenan en el controller)
        public byte[] DatosArchivo { get; set; }
        public string TipoArchivo { get; set; }
        public string NombreArchivo { get; set; }
        public string data { get; set; } // base64 para la vista
    }
    public class SoporteVm
    {
        public string SoporteID { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public List<SoportePermisoDto> Permisos { get; set; } = new List<SoportePermisoDto>();
    }
    // Request para guardar

    public class TipoItem { public int TipoCasoID { get; set; } public string Nombre { get; set; } }
    public class SubtipoItem { public int SubtipoCasoID { get; set; } public int TipoCasoID { get; set; } public string Nombre { get; set; } }
    public class EmpleadoItem { public string Correo { get; set; } public string Nombre { get; set; } public string Area { get; set; } }

    public class SoportePermisoItem { public int TipoCasoID { get; set; } public int? SubtipoCasoID { get; set; } }
    public class SoporteUpsertRequest
    {
        public string SoporteID { get; set; }   // sugerido: correo
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public List<SoportePermisoItem> Permisos { get; set; }
    }
    // DTOs (colócalos en carpeta Models o dentro del controller)
    public class TipoCasoDto
    {
        public int TipoCasoID { get; set; }
        public string Nombre { get; set; }
    }
    public sealed class SoporteResolucion
    {
        // ← Deben coincidir con los nombres de columnas que retorna el SP
        public string SoporteID { get; set; }  // carnet o id

        public string Correo { get; set; }        // correo del soporte (SoporteID lógico)
        public string Carnet { get; set; }        // identificador interno (para Caso.SoporteID)
        public bool EsAdmin { get; set; }       // 1/0 en SQL → bool en C#
        public bool Activo { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }

        // Opcional, por si lo devuelves también:
        // public string SoporteID { get; set; }  // si decides devolver además el mismo correo
    }
    public sealed class CasoListadoDto
    {
        // Caso
        public int ID { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string Prioridad { get; set; }
        public DateTime? FechaCreacion { get; set; }

        public string UsuarioID { get; set; }      // carnet solicitante
        public string Correo { get; set; }         // correo afectado
        public string SoporteID { get; set; }      // carnet soporte (si existe)

        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
        public int? TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }

        // ► Enriquecidos (joins a emp2024)
        public string NombreAutor { get; set; }
        public string CargoAutor { get; set; }
        public string AreaAutor { get; set; }
        public string TelefonoAutor { get; set; }

        public string NombreResponsable { get; set; }
        public string CargoResponsable { get; set; }
        public string AreaResponsable { get; set; }
        public string TelefonoResponsable { get; set; }

        public string Nombresoport { get; set; }
        public string Cargosoport { get; set; }
        public string Areasoport { get; set; }
        public string Telefonosoport { get; set; }
        public string CorreoSoport { get; set; }
    }
    public sealed class CasoRow
    {
        public int ID { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string Prioridad { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string UsuarioID { get; set; }
        public string Correo { get; set; }
        public string SUBGERENTE { get; set; }
        public string SoporteID { get; set; }         // CARNET del soporte asignado
        public int? TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
        public string NombreAutor { get; set; }
        public string NombreResponsable { get; set; }
        public string Nombresoport { get; set; }
        public string Descripcion { get; set; }
        public int? TiempoAtencionMinutos { get; set; }   // para métricas / promedio
        public string TiempoAtencion { get; set; }        // para mostrar "2h 13m"
        public DateTime? FechaFinalizacion { get; set; }  // ya existe en tu DB

    }

    public sealed class KpiDto
    {
        public int Total { get; set; }
        public int Pendiente { get; set; }
        public int Asignado { get; set; }
        public int EnProceso { get; set; }
        public int Cerrado { get; set; }
        public int Cancelado { get; set; }
    }
    public class SubEstadoDto
    {
        public int SubEstadoID { get; set; }
        public string Nombre { get; set; }
    }
    //public sealed class SoporteYoDto
    //{
    //    public bool EsAdmin { get; set; }
    //    public string Carnet { get; set; }    // necesario para comparar con Caso.SoporteID
    //    public string SoporteID { get; set; } // correo del soporte
    //    public string Email { get; set; }
    //    public string Nombre { get; set; }
    //}
    public class SoporteYoDto
    {
        public string SoporteID { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }

        public string Correo { get; set; }   // o Email según tu convención
        public string Area { get; set; }
        public bool Activo { get; set; }
        public bool EsAdmin { get; set; }
        public bool EsSuper { get; set; }   // NUEVO
        public string Carnet { get; set; }
        public bool EsJefe { get; set; }
    }
    public class SoporteYoDto2
    {
        public string SoporteID { get; set; }   // normalmente el correo
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public bool EsAdmin { get; set; }
        public string Carnet { get; set; }

        public bool EsJefe { get; set; }   // <-- NUEVO
    }
    public class SubtipoCasoDto
    {
        public int SubtipoCasoID { get; set; }
        public int TipoCasoID { get; set; }
        public string Nombre { get; set; }
    }
    public class KPIResumenDto
    {
        public int Total { get; set; }
        public int Pendiente { get; set; }
        public int EnProceso { get; set; }
        public int Cerrado { get; set; }
        public int Cancelado { get; set; }
    }
    public class KPIPorDiaDto
    {
        public DateTime Dia { get; set; }
        public int Creados { get; set; }
        public int Cerrados { get; set; }
    }
    public class KPIPorTipoDto
    {
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
        public int Total { get; set; }
    }
    public class KPIPorGerenciaDto
    {
        public string Gerencia { get; set; }
        public int Total { get; set; }
    }
    public class KPITiemposDto
    {
        public double? PromedioMinPrimeraAtencion { get; set; }
        public double? PromedioMinCierre { get; set; }
        public double? MedianaMinCierre { get; set; }
    }
    public class YoVM
    {
        public string Carnet { get; set; }
        public bool EsAdmin { get; set; }
        public bool EsJefe { get; set; }
        // puedes agregar más si quieres
    }

    public class CasoJefeVM
    {
        public int ID { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Prioridad { get; set; }
        public string Titulo { get; set; }

        public int? TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }

        public string SolicitanteCarnet { get; set; }
        public string SolicitanteNombre { get; set; }
        public string SolicitanteGerencia { get; set; }
        public string SolicitanteCorreo { get; set; }

        public string AfectadoCarnet { get; set; }
        public string AfectadoNombre { get; set; }
        public string AfectadoGerencia { get; set; }
        public string AfectadoCorreo { get; set; }

        public string RolEnMiEquipo { get; set; }

        public string Descripcion { get; set; }

        public string SoporteID { get; set; }
        public string Nombresoport { get; set; }

        // HS opcional
        public string Departamento { get; set; }
        public string Edificio { get; set; }
        public int? CantidadAfectados { get; set; }
        public int? DiasCondicion { get; set; }

        // Adjuntos primarios
        public string NombreArchivo { get; set; }
        public string data { get; set; } // base64 webp u otro
    }
    public class KPITopUsuarioDto
    {
        public string Usuario { get; set; }
        public int Total { get; set; }
    }


}