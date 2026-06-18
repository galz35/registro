using Dapper;
using Newtonsoft.Json;
using slnRhonline.Models.Requisicion;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class RequisicionController : Controller
    {
        private readonly string CadenaConexion = ConfigurationManager.ConnectionStrings["CompensacionConnection"].ConnectionString;

        // ==========================================
        // VISTAS (GET)
        // ==========================================

        public ActionResult Index()
        {
            var user = Session["User"] as Entities.Employees;
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }
            
            ViewBag.EsAnalista = false;
            ViewBag.UsuarioNombre = user.FullName;
            ViewBag.UsuarioCarnet = user.EmployeeNumber;
            return View(); // Carga Views/Requisicion/Index.cshtml (Solo Jefe)
        }

        public ActionResult Bandeja()
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null)
                {
                    return RedirectToAction("Index", "Login");
                }
                
                // Permitir acceso siempre en localhost para pruebas de desarrollo, o si cumple la gerencia en prod
                bool esLocal = Request.Url.Host.ToLower().Contains("localhost") || Request.Url.Host.Contains("127.0.0.1");
                
                bool esAnalista = esLocal || (user.GERENCIA != null && 
                                  (user.GERENCIA.ToUpper().Contains("RECURSOS") || 
                                   user.GERENCIA.ToUpper().Contains("COMPENSACION")));
                                   
                if (!esAnalista)
                {
                    return RedirectToAction("Index", "Inicio");
                }
                
                ViewBag.EsAnalista = true;
                ViewBag.UsuarioNombre = user.FullName;
                ViewBag.UsuarioCarnet = user.EmployeeNumber;
                return View(); // Carga Views/Requisicion/Bandeja.cshtml (Solo Analista RRHH)
            }
            catch (Exception ex)
            {
                return Content("<h2>Error al cargar la bandeja de RRHH</h2><pre>" + ex.ToString() + "</pre>");
            }
        }

        public ActionResult Crear(int id = 0)
        {
            ViewBag.Id = id;
            return View();
        }

        public ActionResult Revisar(int id = 0)
        {
            ViewBag.Id = id;
            return View();
        }

        public ActionResult Vacantes()
        {
            return View();
        }

        // ==========================================
        // ENDPOINTS AJAX (JSON)
        // ==========================================

        [HttpGet]
        public JsonResult BuscarEmpleado(string carnet)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(carnet))
                {
                    return Json(new { success = false, message = "Debe proporcionar un carnet válido." }, JsonRequestBehavior.AllowGet);
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    string sql = @"
                        SELECT TOP 1
                            e.carnet, e.nombre_completo, e.correo, e.cargo, e.empresa,
                            e.OGERENCIA, e.oSUBGERENCIA, e.Nombreubicacion,
                            e.carnet_jefe1, e.nom_jefe1, e.correo_jefe1,
                            e.PositionId, e.BusinessUnitId, e.BusinessUnitName,
                            e.fechabaja,
                            p.PositionCode, p.Name AS NombrePosicion,
                            p.CostCenter, p.CostCenterName,
                            t.TERMINATION_DATE, t.ACTION_CODE, t.ACTION_MEANING, t.TERMINATION_TYPE
                        FROM SIGHO1.dbo.EmpleadosVWEF e
                        LEFT JOIN SIGHO1.dbo.Positions p ON e.PositionId = p.PositionId
                        LEFT JOIN SIGHO1.dbo.TERMINATIONS t ON e.carnet = t.EMPLEADO_ID
                        WHERE e.carnet = @carnet";

                    var emp = db.QueryFirstOrDefault<EmpleadoBusquedaViewModel>(sql, new { carnet });

                    if (emp == null)
                    {
                        return Json(new { success = false, message = "No se encontró el colaborador con carnet " + carnet + " en SIGHO." }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new { success = true, data = emp }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al buscar empleado: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult Guardar(RequisicionViewModel model)
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null)
                {
                    return Json(new { success = false, message = "Sesión expirada. Por favor vuelva a iniciar sesión." });
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var p = new DynamicParameters();
                    p.Add("@TipoRequisicion", model.TipoRequisicion);
                    p.Add("@CarnetSolicitante", user.EmployeeNumber);
                    p.Add("@NombreSolicitante", user.FullName);
                    p.Add("@CorreoSolicitante", user.EmailAddress);
                    p.Add("@CarnetEmpleadoBaja", model.CarnetEmpleadoBaja);
                    p.Add("@NombreEmpleadoBaja", model.NombreEmpleadoBaja);
                    p.Add("@MotivoBaja", model.MotivoBaja);
                    p.Add("@FechaBaja", model.FechaBaja);
                    p.Add("@TerminationDate", model.TerminationDate);
                    p.Add("@ActionCode", model.ActionCode);
                    p.Add("@ActionMeaning", model.ActionMeaning);
                    p.Add("@TerminationType", model.TerminationType);
                    p.Add("@PositionId", model.PositionId);
                    p.Add("@PositionCode", model.PositionCode);
                    p.Add("@NombrePosicion", model.NombrePosicion);
                    p.Add("@NombrePuesto", model.NombrePuesto);
                    p.Add("@NumeroPlaza", model.NumeroPlaza);
                    p.Add("@CodigoPosicion", model.CodigoPosicion);
                    p.Add("@Empresa", model.Empresa);
                    p.Add("@BusinessUnitId", model.BusinessUnitId);
                    p.Add("@BusinessUnitName", model.BusinessUnitName);
                    p.Add("@TipoNomina", model.TipoNomina);
                    p.Add("@Sueldo", model.Sueldo);
                    p.Add("@Comisiones", model.Comisiones ?? 0);
                    p.Add("@TipoContrato", model.TipoContrato);
                    p.Add("@HorarioLaboral", model.HorarioLaboral);
                    p.Add("@CostCenter", model.CostCenter);
                    p.Add("@CostCenterName", model.CostCenterName);
                    p.Add("@Gerencia", model.Gerencia);
                    p.Add("@SubGerencia", model.SubGerencia);
                    p.Add("@Coordinacion", model.Coordinacion);
                    p.Add("@Supervision", model.Supervision);
                    p.Add("@AreaOperativa", model.AreaOperativa);
                    p.Add("@Edificio", model.Edificio);
                    p.Add("@CentroCostos", model.CentroCostos);
                    p.Add("@CarnetJefeInmediato", model.CarnetJefeInmediato);
                    p.Add("@NombreJefeInmediato", model.NombreJefeInmediato);

                    // Traslado
                    p.Add("@GerenciaDestino", model.GerenciaDestino);
                    p.Add("@SubGerenciaDestino", model.SubGerenciaDestino);
                    p.Add("@EdificioDestino", model.EdificioDestino);
                    p.Add("@PuestoDestino", model.PuestoDestino);
                    p.Add("@CarnetJefeDestino", model.CarnetJefeDestino);
                    p.Add("@NombreJefeDestino", model.NombreJefeDestino);
                    p.Add("@MotivoTraslado", model.MotivoTraslado);
                    p.Add("@DuracionTraslado", model.DuracionTraslado);
                    p.Add("@FechaTrasladoPropuesta", model.FechaTrasladoPropuesta);
                    p.Add("@JustificacionTraslado", model.JustificacionTraslado);

                    p.Add("@CarnetUsuario", user.EmployeeNumber);
                    p.Add("@NombreUsuario", user.FullName);
                    p.Add("@Observaciones", model.ObservacionesJefe);

                    if (model.RequisicionID > 0)
                    {
                        p.Add("@Op", "Actualizar");
                        p.Add("@RequisicionID", model.RequisicionID);
                        db.Execute("dbo.SP_Comp_Requisicion", p, commandType: CommandType.StoredProcedure);
                        
                        ProcesarArchivosRequest(model.RequisicionID, user.EmployeeNumber);
                        
                        return Json(new { success = true, requisicionId = model.RequisicionID, message = "Requisición guardada exitosamente." });
                    }
                    else
                    {
                        p.Add("@Op", "Crear");
                        var res = db.QueryFirstOrDefault<dynamic>("dbo.SP_Comp_Requisicion", p, commandType: CommandType.StoredProcedure);
                        int newId = res.RequisicionID;
                        string numRQ = res.NumeroRequisicion;
                        
                        ProcesarArchivosRequest(newId, user.EmployeeNumber);
                        
                        return Json(new { success = true, requisicionId = newId, numeroRequisicion = numRQ, message = "Requisición creada exitosamente en borrador." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar requisición: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Enviar(int requisicionId)
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null) return Json(new { success = false, message = "Sesión expirada." });

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    
                    // Verificar si tiene los documentos mínimos necesarios antes de enviar
                    var docs = db.Query<DocumentoViewModel>("dbo.SP_Comp_Requisicion_Documento", 
                        new { Op = "Obtener", RequisicionID = requisicionId }, 
                        commandType: CommandType.StoredProcedure).ToList();

                    var req = db.QueryFirstOrDefault<RequisicionViewModel>("dbo.SP_Comp_Requisicion", 
                        new { Op = "ObtenerPorID", RequisicionID = requisicionId }, 
                        commandType: CommandType.StoredProcedure);

                    if (req == null) return Json(new { success = false, message = "No se encontró la requisición." });

                    bool tieneRequisicionPerfil = docs.Any(d => d.TipoDocumento == "REQUISICION_PERFIL" && d.Activo);
                    bool tieneDescriptorPuesto = docs.Any(d => d.TipoDocumento == "DESCRIPTOR_PUESTO" && d.Activo);
                    bool tieneTraslado = docs.Any(d => d.TipoDocumento == "SOLICITUD_TRASLADO" && d.Activo);

                    if (!tieneRequisicionPerfil || !tieneDescriptorPuesto)
                    {
                        return Json(new { success = false, message = "Debe subir los dos documentos requeridos: Requisición y Perfil de Personal, y el Descriptor de Puesto antes de enviar." });
                    }

                    if (req.TipoRequisicion == "TRASLADO" && !tieneTraslado)
                    {
                        return Json(new { success = false, message = "Debe subir la Solicitud de Traslado Horizontal firmada para este traslado." });
                    }

                    db.Execute("dbo.SP_Comp_Requisicion", new {
                        Op = "Enviar",
                        RequisicionID = requisicionId,
                        CarnetUsuario = user.EmployeeNumber,
                        NombreUsuario = user.FullName
                    }, commandType: CommandType.StoredProcedure);

                    // Intentar enviar correo de notificación a Compensaciones
                    EnviarNotificacionAnalista(requisicionId, req);

                    return Json(new { success = true, message = "La requisición ha sido enviada a revisión de Compensaciones." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al enviar: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Aprobar(int requisicionId, string observaciones)
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null) return Json(new { success = false, message = "Sesión expirada." });

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("dbo.SP_Comp_Requisicion", new {
                        Op = "Aprobar",
                        RequisicionID = requisicionId,
                        CarnetUsuario = user.EmployeeNumber,
                        NombreUsuario = user.FullName,
                        Observaciones = observaciones
                    }, commandType: CommandType.StoredProcedure);

                    var req = db.QueryFirstOrDefault<RequisicionViewModel>("dbo.SP_Comp_Requisicion", 
                        new { Op = "ObtenerPorID", RequisicionID = requisicionId }, 
                        commandType: CommandType.StoredProcedure);

                    // Notificar a Reclutamiento y al Jefe
                    EnviarNotificacionAprobada(requisicionId, req, observaciones);

                    return Json(new { success = true, message = "Requisición aprobada de forma exitosa." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al aprobar: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Devolver(int requisicionId, string observaciones)
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null) return Json(new { success = false, message = "Sesión expirada." });

                if (string.IsNullOrWhiteSpace(observaciones))
                {
                    return Json(new { success = false, message = "Debe detallar el motivo de la devolución en las observaciones." });
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("dbo.SP_Comp_Requisicion", new {
                        Op = "Devolver",
                        RequisicionID = requisicionId,
                        CarnetUsuario = user.EmployeeNumber,
                        NombreUsuario = user.FullName,
                        Observaciones = observaciones
                    }, commandType: CommandType.StoredProcedure);

                    var req = db.QueryFirstOrDefault<RequisicionViewModel>("dbo.SP_Comp_Requisicion", 
                        new { Op = "ObtenerPorID", RequisicionID = requisicionId }, 
                        commandType: CommandType.StoredProcedure);

                    // Notificar al Jefe solicitante sobre la devolución
                    EnviarNotificacionDevuelta(requisicionId, req, observaciones);

                    return Json(new { success = true, message = "La requisición ha sido devuelta al solicitante." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al devolver: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Cancelar(int requisicionId, string observaciones)
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null) return Json(new { success = false, message = "Sesión expirada." });

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("dbo.SP_Comp_Requisicion", new {
                        Op = "Cancelar",
                        RequisicionID = requisicionId,
                        CarnetUsuario = user.EmployeeNumber,
                        NombreUsuario = user.FullName,
                        Observaciones = observaciones
                    }, commandType: CommandType.StoredProcedure);

                    return Json(new { success = true, message = "Requisición cancelada exitosamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al cancelar: " + ex.Message });
            }
        }

        // ==========================================
        // CARGA DE DOCUMENTOS
        // ==========================================

        [HttpPost]
        public JsonResult SubirDocumento(int requisicionId, string tipoDocumento)
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null) return Json(new { success = false, message = "Sesión expirada." });

                if (Request.Files.Count == 0)
                {
                    return Json(new { success = false, message = "No se recibió ningún archivo." });
                }

                HttpPostedFileBase file = Request.Files[0];
                string res = GuardarArchivoFisicoYBD(requisicionId, tipoDocumento, file, user.EmployeeNumber);
                if (res == "EXITO")
                {
                    return Json(new { success = true, message = "Archivo cargado exitosamente." });
                }
                else
                {
                    return Json(new { success = false, message = res });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al subir documento: " + ex.Message });
            }
        }

        private string GuardarArchivoFisicoYBD(int requisicionId, string tipoDocumento, HttpPostedFileBase file, string carnetUsuario)
        {
            if (file == null || file.ContentLength == 0) return "El archivo está vacío.";

            // Validar tamaño máximo (10MB por defecto)
            int maxMb = 10;
            using (var db = new SqlConnection(CadenaConexion))
            {
                db.Open();
                var configMb = db.QueryFirstOrDefault<string>("SELECT Valor FROM dbo.Comp_Req_Configuracion WHERE Clave = 'TAMANO_MAX_ARCHIVO_MB'");
                if (configMb != null) int.TryParse(configMb, out maxMb);
            }

            if (file.ContentLength > (maxMb * 1024 * 1024))
            {
                return $"El tamaño del archivo supera el límite permitido de {maxMb}MB.";
            }

            string extension = Path.GetExtension(file.FileName).ToLower();

            // Validar extensiones
            if (tipoDocumento == "REQUISICION_PERFIL")
            {
                if (extension != ".xls" && extension != ".xlsx" && extension != ".doc" && extension != ".docx" && extension != ".pdf")
                    return "El archivo de Requisición y Perfil de Personal debe ser Excel (.xls, .xlsx), Word (.doc, .docx) o PDF (.pdf).";
            }
            else if (tipoDocumento == "DESCRIPTOR_PUESTO")
            {
                if (extension != ".doc" && extension != ".docx" && extension != ".pdf")
                    return "El Descriptor de Puesto debe ser Word (.doc, .docx) o PDF (.pdf).";
            }
            else if (tipoDocumento == "SOLICITUD_TRASLADO")
            {
                if (extension != ".pdf")
                    return "La Solicitud de Traslado debe ser de formato PDF (.pdf).";
            }

            // Crear directorio físico
            string subPath = $"~/Uploads/Requisicion/{requisicionId}/";
            string physicalPath = Server.MapPath(subPath);
            if (!Directory.Exists(physicalPath))
            {
                Directory.CreateDirectory(physicalPath);
            }

            // Nombre normalizado para evitar caracteres extraños
            string safeFileName = $"{tipoDocumento}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            string savePath = Path.Combine(physicalPath, safeFileName);
            file.SaveAs(savePath);

            // Registrar en BD
            using (var db = new SqlConnection(CadenaConexion))
            {
                db.Open();
                db.QueryFirstOrDefault<int>("dbo.SP_Comp_Requisicion_Documento", new {
                    Op = "Reemplazar",
                    RequisicionID = requisicionId,
                    TipoDocumento = tipoDocumento,
                    NombreArchivo = file.FileName,
                    RutaArchivo = Path.Combine(subPath, safeFileName).Replace("\\", "/"),
                    Extension = extension,
                    TamanioBytes = file.ContentLength,
                    SubidoPor = carnetUsuario
                }, commandType: CommandType.StoredProcedure);
            }

            return "EXITO";
        }

        private void ProcesarArchivosRequest(int requisicionId, string carnetUsuario)
        {
            foreach (string fileKey in Request.Files)
            {
                HttpPostedFileBase file = Request.Files[fileKey];
                if (file != null && file.ContentLength > 0)
                {
                    GuardarArchivoFisicoYBD(requisicionId, fileKey, file, carnetUsuario);
                }
            }
        }

        [HttpGet]
        public ActionResult DescargarDocumento(int id)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var doc = db.QueryFirstOrDefault<DocumentoViewModel>("SELECT * FROM dbo.Comp_Req_Documento WHERE DocumentoID = @id", new { id });
                    if (doc == null)
                    {
                        return HttpNotFound("El archivo no existe.");
                    }

                    string path = Server.MapPath(doc.RutaArchivo);
                    if (!System.IO.File.Exists(path))
                    {
                        return HttpNotFound("El archivo físico no existe en el servidor.");
                    }

                    string contentType = MimeMapping.GetMimeMapping(path);
                    return File(path, contentType, doc.NombreArchivo);
                }
            }
            catch (Exception ex)
            {
                return Content("Error al descargar: " + ex.Message);
            }
        }

        // ==========================================
        // CONSULTAS JSON
        // ==========================================

        [HttpGet]
        public JsonResult ObtenerRequisicion(int id)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var req = db.QueryFirstOrDefault<RequisicionViewModel>("dbo.SP_Comp_Requisicion", new { Op = "ObtenerPorID", RequisicionID = id }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true, data = req }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ListarMisRequisiciones()
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null) return Json(new { success = false, message = "Sesión expirada." }, JsonRequestBehavior.AllowGet);

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query<RequisicionListaViewModel>("dbo.SP_Comp_Requisicion", new { Op = "ListarPorJefe", CarnetSolicitante = user.EmployeeNumber }, commandType: CommandType.StoredProcedure).ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ListarPendientes()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query<RequisicionListaViewModel>("dbo.SP_Comp_Requisicion", new { Op = "ListarPendientes" }, commandType: CommandType.StoredProcedure).ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ListarTodas(string estado, string gerencia, string fechaDesde, string fechaHasta)
        {
            try
            {
                DateTime? desde = null;
                DateTime? hasta = null;
                if (!string.IsNullOrEmpty(fechaDesde)) desde = Convert.ToDateTime(fechaDesde);
                if (!string.IsNullOrEmpty(fechaHasta)) hasta = Convert.ToDateTime(fechaHasta);

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query<RequisicionListaViewModel>("dbo.SP_Comp_Requisicion", new {
                        Op = "ListarTodas",
                        EstadoFiltro = string.IsNullOrEmpty(estado) ? null : estado,
                        GerenciaFiltro = string.IsNullOrEmpty(gerencia) ? null : gerencia,
                        FechaDesde = desde,
                        FechaHasta = hasta
                    }, commandType: CommandType.StoredProcedure).ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerDocumentos(int id)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var docs = db.Query<DocumentoViewModel>("dbo.SP_Comp_Requisicion_Documento", new { Op = "Obtener", RequisicionID = id }, commandType: CommandType.StoredProcedure).ToList();
                    return Json(new { success = true, data = docs }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerHistorial(int id)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var hist = db.Query<HistorialViewModel>("dbo.SP_Comp_Requisicion", new { Op = "ObtenerHistorial", RequisicionID = id }, commandType: CommandType.StoredProcedure).ToList();
                    return Json(new { success = true, data = hist }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult Dashboard()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var res = db.QueryFirstOrDefault<DashboardRequisicionViewModel>("dbo.SP_Comp_Requisicion", new { Op = "Dashboard" }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true, data = res }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerCatalogos()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var bajas = db.Query<CatalogoViewModel>("SELECT MotivoID AS Id, Nombre FROM dbo.Comp_Req_Catalogo_MotivoBaja WHERE Activo = 1 ORDER BY Nombre").ToList();
                    var traslados = db.Query<CatalogoViewModel>("SELECT MotivoTrasladoID AS Id, Nombre FROM dbo.Comp_Req_Catalogo_MotivoTraslado WHERE Activo = 1 ORDER BY Nombre").ToList();
                    
                    // Buscar gerencias de SIGHO
                    var gerencias = db.Query<string>("SELECT DISTINCT OGERENCIA FROM SIGHO1.dbo.EMP2024 WHERE OGERENCIA IS NOT NULL AND OGERENCIA <> '' ORDER BY OGERENCIA").ToList();

                    return Json(new { success = true, bajas = bajas, traslados = traslados, gerencias = gerencias }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerArchivosAnteriores(string positionCode, long? positionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(positionCode) && !positionId.HasValue)
                {
                    return Json(new { success = false, message = "Debe proporcionar código o ID de la posición." }, JsonRequestBehavior.AllowGet);
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    // Buscar la última requisición aprobada para esta posición
                    string sqlReq = @"
                        SELECT TOP 1 RequisicionID 
                        FROM dbo.Comp_Req_Requisicion 
                        WHERE Activo = 1 AND Estado = 'APROBADA'
                          AND (
                            (@positionId IS NOT NULL AND PositionId = @positionId) OR
                            (@positionCode IS NOT NULL AND PositionCode = @positionCode)
                          )
                        ORDER BY FechaAprobacion DESC, RequisicionID DESC";

                    var reqId = db.QueryFirstOrDefault<int?>(sqlReq, new { positionCode, positionId });

                    if (!reqId.HasValue)
                    {
                        return Json(new { success = false, message = "No se encontraron requisiciones anteriores aprobadas para esta plaza." }, JsonRequestBehavior.AllowGet);
                    }

                    // Obtener los documentos asociados a esa requisición
                    string sqlDocs = @"
                        SELECT DocumentoID, TipoDocumento, NombreArchivo, RutaArchivo, Extension, TamanioBytes
                        FROM dbo.Comp_Req_Documento
                        WHERE RequisicionID = @reqId AND Activo = 1";

                    var docs = db.Query<DocumentoViewModel>(sqlDocs, new { reqId = reqId.Value }).ToList();

                    if (!docs.Any())
                    {
                        return Json(new { success = false, message = "No se encontraron archivos en la requisición previa." }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new { success = true, requisicionAnteriorId = reqId.Value, documentos = docs }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al buscar archivos anteriores: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ReutilizarDocumentosAnteriores(int nuevaRequisicionId, int requisicionAnteriorId)
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null) return Json(new { success = false, message = "Sesión expirada." });

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // Buscar los documentos activos de la requisición anterior
                    string sqlDocs = @"
                        SELECT TipoDocumento, NombreArchivo, RutaArchivo, Extension, TamanioBytes
                        FROM dbo.Comp_Req_Documento
                        WHERE RequisicionID = @requisicionAnteriorId AND Activo = 1";

                    var docsAnteriores = db.Query<DocumentoViewModel>(sqlDocs, new { requisicionAnteriorId }).ToList();

                    if (!docsAnteriores.Any())
                    {
                        return Json(new { success = false, message = "No hay documentos para reutilizar." });
                    }

                    // Crear directorio para la nueva requisición
                    string subPathNueva = $"~/Uploads/Requisicion/{nuevaRequisicionId}/";
                    string physicalPathNueva = Server.MapPath(subPathNueva);
                    if (!Directory.Exists(physicalPathNueva))
                    {
                        Directory.CreateDirectory(physicalPathNueva);
                    }

                    foreach (var doc in docsAnteriores)
                    {
                        string sourcePathFisico = Server.MapPath(doc.RutaArchivo);
                        if (!System.IO.File.Exists(sourcePathFisico))
                        {
                            continue;
                        }

                        // Nombre normalizado para evitar colisiones
                        string safeFileName = $"{doc.TipoDocumento}_{DateTime.Now:yyyyMMddHHmmss}{doc.Extension}";
                        string destPathFisico = Path.Combine(physicalPathNueva, safeFileName);

                        // Copiar el archivo físico
                        System.IO.File.Copy(sourcePathFisico, destPathFisico, true);

                        // Registrar en la base de datos
                        db.QueryFirstOrDefault<int>("dbo.SP_Comp_Requisicion_Documento", new {
                            Op = "Reemplazar",
                            RequisicionID = nuevaRequisicionId,
                            TipoDocumento = doc.TipoDocumento,
                            NombreArchivo = doc.NombreArchivo,
                            RutaArchivo = Path.Combine(subPathNueva, safeFileName).Replace("\\", "/"),
                            Extension = doc.Extension,
                            TamanioBytes = doc.TamanioBytes,
                            SubidoPor = user.EmployeeNumber
                        }, commandType: CommandType.StoredProcedure);
                    }

                    return Json(new { success = true, message = "Documentos reutilizados con éxito." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al reutilizar documentos: " + ex.Message });
            }
        }

        // ==========================================
        // GESTIÓN DE VACANTES PENDIENTES
        // ==========================================

        [HttpGet]
        public JsonResult ListarVacantes(string estado, string gerencia)
        {
            try
            {
                var user = Session["User"] as Entities.Employees;
                if (user == null)
                {
                    return Json(new { success = false, message = "Sesión expirada." }, JsonRequestBehavior.AllowGet);
                }

                bool esAnalista = user.GERENCIA != null && 
                                  (user.GERENCIA.ToUpper().Contains("RECURSOS") || 
                                   user.GERENCIA.ToUpper().Contains("COMPENSACION"));

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query<VacanteViewModel>("dbo.SP_Comp_Requisicion_Vacante", new {
                        Op = "ListarPendientes",
                        EstadoFiltro = string.IsNullOrEmpty(estado) ? null : estado,
                        GerenciaFiltro = string.IsNullOrEmpty(gerencia) ? null : gerencia,
                        CarnetUsuario = esAnalista ? null : user.EmployeeNumber
                    }, commandType: CommandType.StoredProcedure).ToList();

                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult SincronizarVacantes()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var res = db.QueryFirstOrDefault<dynamic>("dbo.SP_Comp_Requisicion_Vacante", new { Op = "Sincronizar" }, commandType: CommandType.StoredProcedure);
                    int count = res.NuevasVacantes;
                    return Json(new { success = true, count = count, message = $"Sincronización finalizada. Se detectaron {count} nuevas vacantes." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DescartarVacante(int vacanteId, string observaciones)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(observaciones))
                {
                    return Json(new { success = false, message = "Debe proporcionar una justificación para descartar la vacante." });
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("dbo.SP_Comp_Requisicion_Vacante", new {
                        Op = "Descartar",
                        VacanteID = vacanteId,
                        Observaciones = observaciones
                    }, commandType: CommandType.StoredProcedure);

                    return Json(new { success = true, message = "La vacante ha sido descartada." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult DashboardVacantes()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var res = db.QueryFirstOrDefault<DashboardVacantesViewModel>("dbo.SP_Comp_Requisicion_Vacante", new { Op = "Dashboard" }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true, data = res }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult NotificarJefeVacante(int vacanteId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var vac = db.QueryFirstOrDefault<VacanteViewModel>("SELECT * FROM dbo.Comp_Req_VacantePendiente WHERE VacanteID = @vacanteId", new { vacanteId });

                    if (vac == null)
                    {
                        return Json(new { success = false, message = "No se encontró el registro de la vacante." });
                    }

                    if (string.IsNullOrEmpty(vac.CorreoJefe))
                    {
                        return Json(new { success = false, message = "El jefe inmediato no tiene un correo electrónico configurado en SIGHO." });
                    }

                    // Enviar correo electrónico con la imagen del proceso incrustada
                    string htmlBody = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: 'Outfit', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; background-color: #f7f9fa; padding: 20px; }}
                                .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); border-top: 5px solid #d32f2f; overflow: hidden; }}
                                .header {{ background-color: #d32f2f; padding: 25px; text-align: center; color: #ffffff; }}
                                .header h2 {{ margin: 0; font-size: 24px; font-weight: 600; }}
                                .content {{ padding: 30px; }}
                                .info-box {{ background-color: #f1f3f4; border-left: 4px solid #1a73e8; padding: 15px; border-radius: 4px; margin-bottom: 20px; }}
                                .info-box p {{ margin: 5px 0; font-size: 14px; }}
                                .info-box strong {{ color: #1a73e8; }}
                                .btn {{ display: inline-block; padding: 12px 24px; background-color: #d32f2f; color: #ffffff !important; text-decoration: none; border-radius: 4px; font-weight: bold; text-align: center; margin-top: 15px; }}
                                .footer {{ background-color: #f1f3f4; padding: 15px; text-align: center; font-size: 12px; color: #777; }}
                                .img-container {{ text-align: center; margin: 25px 0; }}
                                .img-container img {{ max-width: 100%; height: auto; border-radius: 4px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h2>REPOSICIÓN DE VACANTE PENDIENTE</h2>
                                </div>
                                <div class='content'>
                                    <p>Estimado(a) <strong>{vac.NombreJefe}</strong>,</p>
                                    <p>Se ha detectado una baja reciente en su equipo de trabajo. Conforme a las normativas de la organización, es necesario registrar la requisición de personal para iniciar el proceso de selección y reclutamiento.</p>
                                    
                                    <div class='info-box'>
                                        <p><strong>Colaborador Saliente:</strong> {vac.CarnetEmpleado} - {vac.NombreEmpleado}</p>
                                        <p><strong>Posición / Cargo:</strong> {vac.PositionCode} - {vac.PuestoEmpleado}</p>
                                        <p><strong>Gerencia:</strong> {vac.GerenciaEmpleado}</p>
                                        <p><strong>Fecha de Baja:</strong> {vac.FechaBaja:dd/MM/yyyy}</p>
                                    </div>

                                    <p>Para su comodidad, los datos de la plaza ya han sido sincronizados en la plataforma. Solo debe ingresar al sistema, confirmar los datos mínimos e iniciar su trámite.</p>
                                    
                                    <div class='img-container'>
                                        <img src='cid:infografia_proceso' alt='Proceso de Requisición de Personal' />
                                    </div>

                                    <div style='text-align: center;'>
                                        <a href='http://172.26.54.66/rhonline/Requisicion/Vacantes' class='btn'>Tramitar Requisición en RHOnline</a>
                                    </div>
                                </div>
                                <div class='footer'>
                                    <p>Este es un correo automático, por favor no responda directamente a este mensaje.</p>
                                    <p>&copy; {DateTime.Now.Year} Gerencia de Recursos Humanos - Compensaciones</p>
                                </div>
                            </div>
                        </body>
                        </html>";

                    string imagePath = Server.MapPath("~/Content/images/requesicion/f06f3712-36ea-4a99-b158-be03c1a02fba.jpg");
                    
                    // Si no existe la carpeta, la creamos y copiamos el archivo original si está en la ruta física
                    if (!System.IO.File.Exists(imagePath))
                    {
                        string dir = Path.GetDirectoryName(imagePath);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        
                        string sourceImg = @"C:\rhgerencia\RHOnlineProduccion\compensacion\requesicion\f06f3712-36ea-4a99-b158-be03c1a02fba.jpg";
                        if (System.IO.File.Exists(sourceImg))
                        {
                            System.IO.File.Copy(sourceImg, imagePath, true);
                        }
                    }

                    string result = EnviarCorreoConImagen(vac.CorreoJefe, null, $"[RHOnline] Requisición de Vacante Pendiente: {vac.PuestoEmpleado}", htmlBody, imagePath, "infografia_proceso");

                    if (result == "EXITO")
                    {
                        // Marcar vacante como notificada en BD
                        db.Execute("dbo.SP_Comp_Requisicion_Vacante", new { Op = "MarcarNotificada", VacanteID = vacanteId }, commandType: CommandType.StoredProcedure);

                        // Registrar log en BD
                        db.Execute(@"
                            INSERT INTO dbo.Comp_Req_LogCorreo (TipoCorreo, VacanteID, DestinatarioPara, Asunto, EnviadoExitosamente, FechaEnvio, EnviadoPor)
                            VALUES ('NOTIFICACION_VACANTE_JEFE', @vacanteId, @destinatario, @asunto, 1, GETDATE(), 'SISTEMA')",
                            new { vacanteId, destinatario = vac.CorreoJefe, asunto = $"Requisición de Vacante Pendiente: {vac.PuestoEmpleado}" });

                        return Json(new { success = true, message = "Notificación enviada al correo del jefe inmediato con éxito." });
                    }
                    else
                    {
                        // Registrar log de error
                        db.Execute(@"
                            INSERT INTO dbo.Comp_Req_LogCorreo (TipoCorreo, VacanteID, DestinatarioPara, Asunto, EnviadoExitosamente, MensajeError, FechaEnvio, EnviadoPor)
                            VALUES ('NOTIFICACION_VACANTE_JEFE', @vacanteId, @destinatario, @asunto, 0, LEFT(@error, 499), GETDATE(), 'SISTEMA')",
                            new { vacanteId, destinatario = vac.CorreoJefe, asunto = $"Requisición de Vacante Pendiente: {vac.PuestoEmpleado}", error = result });

                        return Json(new { success = false, message = "No se pudo enviar el correo: " + result });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // HELPERS DE ENVÍO DE CORREOS
        // ==========================================

        private string EnviarCorreoConImagen(string destinatario, string copia, string asunto, string cuerpoHtml, string rutaImagenFisica, string contentIdImagen, List<string> adjuntos = null)
        {
            string resultado = "EXITO";
            try
            {
                MailMessage email = new MailMessage();
                email.To.Add(destinatario);
                if (!string.IsNullOrEmpty(copia))
                {
                    email.CC.Add(copia);
                }
                email.From = new MailAddress("recursoshumanos@claro.com.ni", "RHOnline - Requisiciones");
                email.Subject = asunto;
                email.SubjectEncoding = System.Text.Encoding.UTF8;
                email.Bcc.Add("gustavo.lira@claro.com.ni"); // Copia oculta de control

                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(cuerpoHtml, null, "text/html");

                if (!string.IsNullOrEmpty(rutaImagenFisica) && System.IO.File.Exists(rutaImagenFisica))
                {
                    LinkedResource imageResource = new LinkedResource(rutaImagenFisica, "image/jpeg");
                    imageResource.ContentId = contentIdImagen;
                    htmlView.LinkedResources.Add(imageResource);
                }

                email.AlternateViews.Add(htmlView);
                email.BodyEncoding = System.Text.Encoding.UTF8;
                email.IsBodyHtml = true;
                email.Priority = MailPriority.Normal;

                if (adjuntos != null && adjuntos.Any())
                {
                    foreach (var adjunto in adjuntos)
                    {
                        if (System.IO.File.Exists(adjunto))
                        {
                            email.Attachments.Add(new Attachment(adjunto));
                        }
                    }
                }

                ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;

                using (SmtpClient cliente = new SmtpClient("10.200.5.23", 587))
                {
                    cliente.Credentials = new NetworkCredential("recursoshumanos@claro.com.ni", "Enero&272025");
                    cliente.EnableSsl = true;
                    cliente.Send(email);
                }
            }
            catch (Exception ex)
            {
                resultado = "ERROR: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
            return resultado;
        }

        private void EnviarNotificacionAnalista(int requisicionId, RequisicionViewModel req)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var configDest = db.QueryFirstOrDefault<string>("SELECT Valor FROM dbo.Comp_Req_Configuracion WHERE Clave = 'CORREO_DESTINO_ANALISTA'");
                    string dest = configDest ?? "compensacion@claro.com.ni";

                    // Obtener los adjuntos activos de la requisición
                    var docs = db.Query<DocumentoViewModel>("dbo.SP_Comp_Requisicion_Documento", 
                        new { Op = "Obtener", RequisicionID = requisicionId }, 
                        commandType: CommandType.StoredProcedure).ToList();

                    List<string> adjuntosPaths = new List<string>();
                    foreach (var doc in docs)
                    {
                        if (doc.Activo)
                        {
                            string pathFisico = Server.MapPath(doc.RutaArchivo);
                            if (System.IO.File.Exists(pathFisico))
                            {
                                adjuntosPaths.Add(pathFisico);
                            }
                        }
                    }

                    string htmlBody = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; color: #333; }}
                                .container {{ max-width: 600px; padding: 20px; border: 1px solid #ccc; border-radius: 5px; }}
                                .header {{ background-color: #1a73e8; color: white; padding: 10px; text-align: center; }}
                                .content {{ margin-top: 15px; }}
                                .btn {{ display: inline-block; padding: 10px 20px; background-color: #1a73e8; color: white !important; text-decoration: none; border-radius: 4px; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h3>NUEVA SOLICITUD DE REQUISICIÓN</h3>
                                </div>
                                <div class='content'>
                                    <p>Se ha recibido una nueva requisición para revisión:</p>
                                    <p><strong>Folio:</strong> {req.NumeroRequisicion}</p>
                                    <p><strong>Tipo:</strong> {req.TipoRequisicion}</p>
                                    <p><strong>Jefe Solicitante:</strong> {req.NombreSolicitante}</p>
                                    <p><strong>Puesto Solicitado:</strong> {req.NombrePuesto}</p>
                                    <p><strong>Gerencia:</strong> {req.Gerencia}</p>
                                    <p><strong>Colaborador Saliente:</strong> {req.NombreEmpleadoBaja}</p>
                                    <p>Los archivos requeridos se adjuntan a este correo. También puede ingresar al sistema para revisar los documentos adjuntos y aprobar o devolver la solicitud.</p>
                                    <br>
                                    <div style='text-align: center;'>
                                        <a href='http://172.26.54.66/rhonline/Requisicion/Revisar/{requisicionId}' class='btn'>Revisar Requisición</a>
                                    </div>
                                </div>
                            </div>
                        </body>
                        </html>";

                    string result = EnviarCorreoConImagen(dest, null, $"[RHOnline] Nueva Requisición - {req.NumeroRequisicion} - {req.NombrePuesto}", htmlBody, null, null, adjuntosPaths);

                    db.Execute(@"
                        INSERT INTO dbo.Comp_Req_LogCorreo (TipoCorreo, RequisicionID, DestinatarioPara, Asunto, EnviadoExitosamente, MensajeError, FechaEnvio, EnviadoPor)
                        VALUES ('NOTIFICACION_ANALISTA', @requisicionId, @dest, @asunto, @success, @error, GETDATE(), 'SISTEMA')",
                        new {
                            requisicionId,
                            dest,
                            asunto = $"Nueva Requisición - {req.NumeroRequisicion} - {req.NombrePuesto}",
                            success = (result == "EXITO" ? 1 : 0),
                            error = (result == "EXITO" ? null : result)
                        });
                }
            }
            catch { /* Silenciar errores para evitar bloquear el flujo web */ }
        }

        private void EnviarNotificacionDevuelta(int requisicionId, RequisicionViewModel req, string observaciones)
        {
            try
            {
                if (string.IsNullOrEmpty(req.CorreoSolicitante)) return;

                string htmlBody = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; color: #333; }}
                            .container {{ max-width: 600px; padding: 20px; border: 1px solid #ff9800; border-radius: 5px; }}
                            .header {{ background-color: #ff9800; color: white; padding: 10px; text-align: center; }}
                            .content {{ margin-top: 15px; }}
                            .obs {{ background-color: #fff3e0; border-left: 4px solid #ff9800; padding: 10px; margin: 15px 0; font-style: italic; }}
                            .btn {{ display: inline-block; padding: 10px 20px; background-color: #ff9800; color: white !important; text-decoration: none; border-radius: 4px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h3>SOLICITUD DEVUELTA / CON OBSERVACIONES</h3>
                            </div>
                            <div class='content'>
                                <p>Estimado(a) <strong>{req.NombreSolicitante}</strong>,</p>
                                <p>Su solicitud de requisición <strong>{req.NumeroRequisicion}</strong> para el puesto de <strong>{req.NombrePuesto}</strong> ha sido devuelta por el equipo de Compensaciones debido a la siguiente observación:</p>
                                <div class='obs'>
                                    ""{observaciones}""
                                </div>
                                <p>Por favor, ingrese al sistema para corregir los datos o reemplazar los documentos solicitados.</p>
                                <br>
                                <div style='text-align: center;'>
                                    <a href='http://172.26.54.66/rhonline/Requisicion/Crear/{requisicionId}' class='btn'>Editar Requisición</a>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                string result = EnviarCorreoConImagen(req.CorreoSolicitante, null, $"[RHOnline] Solicitud Devuelta: {req.NumeroRequisicion}", htmlBody, null, null);

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute(@"
                        INSERT INTO dbo.Comp_Req_LogCorreo (TipoCorreo, RequisicionID, DestinatarioPara, Asunto, EnviadoExitosamente, MensajeError, FechaEnvio, EnviadoPor)
                        VALUES ('NOTIFICACION_DEVUELTA_JEFE', @requisicionId, @dest, @asunto, @success, @error, GETDATE(), 'SISTEMA')",
                        new {
                            requisicionId,
                            dest = req.CorreoSolicitante,
                            asunto = $"Solicitud Devuelta: {req.NumeroRequisicion}",
                            success = (result == "EXITO" ? 1 : 0),
                            error = (result == "EXITO" ? null : result)
                        });
                }
            }
            catch { }
        }

        private void EnviarNotificacionAprobada(int requisicionId, RequisicionViewModel req, string observaciones)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // Obtener los adjuntos activos de la requisición
                    var docs = db.Query<DocumentoViewModel>("dbo.SP_Comp_Requisicion_Documento", 
                        new { Op = "Obtener", RequisicionID = requisicionId }, 
                        commandType: CommandType.StoredProcedure).ToList();

                    List<string> adjuntosPaths = new List<string>();
                    foreach (var doc in docs)
                    {
                        if (doc.Activo)
                        {
                            string pathFisico = Server.MapPath(doc.RutaArchivo);
                            if (System.IO.File.Exists(pathFisico))
                            {
                                adjuntosPaths.Add(pathFisico);
                            }
                        }
                    }
                    
                    // 1. Notificar al Jefe
                    if (!string.IsNullOrEmpty(req.CorreoSolicitante))
                    {
                        string htmlJefe = $@"
                            <html>
                            <head>
                                <style>
                                    body {{ font-family: Arial, sans-serif; color: #333; }}
                                    .container {{ max-width: 600px; padding: 20px; border: 1px solid #4caf50; border-radius: 5px; }}
                                    .header {{ background-color: #4caf50; color: white; padding: 10px; text-align: center; }}
                                    .content {{ margin-top: 15px; }}
                                </style>
                            </head>
                            <body>
                                <div class='container'>
                                    <div class='header'>
                                        <h3>SOLICITUD APROBADA</h3>
                                    </div>
                                    <div class='content'>
                                        <p>Estimado(a) <strong>{req.NombreSolicitante}</strong>,</p>
                                        <p>Nos complace informarle que su solicitud de requisición <strong>{req.NumeroRequisicion}</strong> para el puesto de <strong>{req.NombrePuesto}</strong> ha sido <strong>APROBADA</strong> por el equipo de Compensaciones.</p>
                                        <p>El trámite ha sido derivado al equipo de Reclutamiento para dar inicio formal a la búsqueda del candidato. Adjuntamos los documentos oficiales de su solicitud.</p>
                                    </div>
                                </div>
                            </body>
                            </html>";

                        EnviarCorreoConImagen(req.CorreoSolicitante, null, $"[RHOnline] Solicitud Aprobada: {req.NumeroRequisicion}", htmlJefe, null, null, adjuntosPaths);
                    }

                    // 2. Notificar a Reclutamiento
                    var configRec = db.QueryFirstOrDefault<string>("SELECT Valor FROM dbo.Comp_Req_Configuracion WHERE Clave = 'CORREO_DESTINO_RECLUTAMIENTO'");
                    string destRec = configRec ?? "reclutamiento@claro.com.ni";

                    string htmlRec = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; color: #333; }}
                                .container {{ max-width: 650px; padding: 20px; border: 1px solid #d32f2f; border-radius: 5px; }}
                                .header {{ background-color: #d32f2f; color: white; padding: 10px; text-align: center; }}
                                .content {{ margin-top: 15px; }}
                                .table-rec {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                                .table-rec td, .table-rec th {{ border: 1px solid #ddd; padding: 8px; }}
                                .table-rec th {{ background-color: #f2f2f2; text-align: left; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h3>AUTORIZACIÓN DE PLAZA VACANTE - RECLUTAMIENTO</h3>
                                </div>
                                <div class='content'>
                                    <p>Conforme autorización de Gerencia de GRH / Compensaciones, se procede a autorizar la siguiente plaza con sus descriptores oficiales:</p>
                                    <table class='table-rec'>
                                        <tr><th>Folio Requisición</th><td>{req.NumeroRequisicion}</td></tr>
                                        <tr><th>Tipo Solicitud</th><td>{req.TipoRequisicion}</td></tr>
                                        <tr><th>Puesto</th><td>{req.NombrePuesto}</td></tr>
                                        <tr><th>Sueldo Autorizado</th><td>C$ {req.Sueldo:N2}</td></tr>
                                        <tr><th>Comisiones</th><td>C$ {req.Comisiones:N2}</td></tr>
                                        <tr><th>Tipo Nómina</th><td>{req.TipoNomina}</td></tr>
                                        <tr><th>Centro de Costos</th><td>{req.CentroCostos} - {req.CostCenterName}</td></tr>
                                        <tr><th>Gerencia</th><td>{req.Gerencia}</td></tr>
                                        <tr><th>Subgerencia</th><td>{req.SubGerencia}</td></tr>
                                        <tr><th>Edificio / Ubicación</th><td>{req.Edificio}</td></tr>
                                        <tr><th>Jefe Inmediato</th><td>{req.NombreJefeInmediato}</td></tr>
                                        <tr><th>Colaborador a Reemplazar</th><td>{req.CarnetEmpleadoBaja} - {req.NombreEmpleadoBaja}</td></tr>
                                        <tr><th>Motivo</th><td>{req.MotivoBaja}</td></tr>
                                    </table>
                                    
                                    {(req.TipoRequisicion == "TRASLADO" ? $@"
                                        <h4 style='color: #d32f2f; margin-top:20px;'>DATOS DE DESTINO DEL TRASLADO</h4>
                                        <table class='table-rec'>
                                            <tr><th>Nueva Gerencia</th><td>{req.GerenciaDestino}</td></tr>
                                            <tr><th>Nueva Subgerencia</th><td>{req.SubGerenciaDestino}</td></tr>
                                            <tr><th>Nuevo Edificio</th><td>{req.EdificioDestino}</td></tr>
                                            <tr><th>Nuevo Jefe Destino</th><td>{req.NombreJefeDestino}</td></tr>
                                            <tr><th>Fecha Traslado Propuesta</th><td>{req.FechaTrasladoPropuesta:dd/MM/yyyy}</td></tr>
                                        </table>" : "")}
                                        
                                    <p>Los descriptores del puesto autorizados en Word y PDF firmado están adjuntos a este correo para su descarga inmediata, y también disponibles en la plataforma RHOnline.</p>
                                    <br>
                                    <div style='text-align: center;'>
                                        <a href='http://172.26.54.66/rhonline/Requisicion/Revisar/{requisicionId}' style='display:inline-block; padding:10px 20px; background-color:#d32f2f; color:white !important; text-decoration:none; border-radius:4px;'>Ver Requisición y Descargar Adjuntos</a>
                                    </div>
                                </div>
                            </div>
                        </body>
                        </html>";

                    string result = EnviarCorreoConImagen(destRec, "compensacion@claro.com.ni", $"AUTORIZACION DE REPOSICION DE PLAZA - {req.NombrePuesto} : {req.CarnetEmpleadoBaja} - {req.NombreEmpleadoBaja}", htmlRec, null, null, adjuntosPaths);

                    db.Execute(@"
                        INSERT INTO dbo.Comp_Req_LogCorreo (TipoCorreo, RequisicionID, DestinatarioPara, Asunto, EnviadoExitosamente, MensajeError, FechaEnvio, EnviadoPor)
                        VALUES ('AUTORIZACION_RECLUTAMIENTO', @requisicionId, @destRec, @asunto, @success, @error, GETDATE(), 'SISTEMA')",
                        new {
                            requisicionId,
                            destRec,
                            asunto = $"AUTORIZACION DE REPOSICION DE PLAZA - {req.NombrePuesto}",
                            success = (result == "EXITO" ? 1 : 0),
                            error = (result == "EXITO" ? null : result)
                        });
                }
            }
            catch { }
        }
    }
}
