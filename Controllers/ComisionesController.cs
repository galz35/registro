using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Dapper;
using System.Configuration;
using System.Threading.Tasks;
using slnRhonline.Models.Compensacion;
using System.Net.Http;

namespace slnRhonline.Controllers
{
    public class ComisionesController : Controller
    {
        private string CadenaConexion = ConfigurationManager.ConnectionStrings["CompensacionConnection"].ConnectionString;

        public ActionResult Index()
        {
            return View("~/Views/Compensacion/Plantilla.cshtml");
        }

        public ActionResult Revision()
        {
            return View("~/Views/Compensacion/RevisionComisiones.cshtml");
        }

        [HttpGet]
        public JsonResult CargarMiPlantilla(int? periodoId)
        {
            try
            {
                Entities.Employees employees = (Entities.Employees)Session["User"];
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var pActivo = db.QueryFirstOrDefault<PeriodoViewModel>("SP_Comp_Comisiones", new { Op = "PeriodoActivo" }, commandType: CommandType.StoredProcedure);
                    if (!periodoId.HasValue && pActivo != null) periodoId = pActivo.PeriodoID;

                    using (var tx = db.BeginTransaction())
                    {
                        var plantilla = db.QueryFirstOrDefault<PlantillaViewModel>(
                            "SP_Comp_Comisiones",
                            new { Op = "IniciarPlantilla", PeriodoID = periodoId, CarnetGerente = employees.EmployeeNumber },
                            tx, commandType: CommandType.StoredProcedure);

                        if (plantilla == null) return Json(new { success = false, message = "No tienes personal asignado para validar comisiones." }, JsonRequestBehavior.AllowGet);

                        db.Execute("SP_Comp_Comisiones",
                            new { Op = "Sincronizar", PlantillaID = plantilla.PlantillaID, CarnetGerente = employees.EmployeeNumber },
                            tx, commandType: CommandType.StoredProcedure);

                        var detalles = db.Query<DetallePlantillaViewModel>(
                            "SP_Comp_Comisiones",
                            new { Op = "ObtenerDetalle", PlantillaID = plantilla.PlantillaID },
                            tx, commandType: CommandType.StoredProcedure).ToList();

                        tx.Commit();

                        var cargosCat = db.Query<string>("SELECT DISTINCT NombreCargo FROM dbo.Comp_CatalogoCargo ORDER BY NombreCargo").ToList();
                        var ubisCat = db.Query<string>("SELECT DISTINCT Nombreubicacion FROM SIGHO1.dbo.EMP2024 WHERE fechabaja IS NULL ORDER BY Nombreubicacion").ToList();
                        var jefesCat = db.Query<string>("SELECT DISTINCT nom_jefe1 FROM SIGHO1.dbo.EMP2024 WHERE fechabaja IS NULL ORDER BY nom_jefe1").ToList();

                        return Json(new { success = true, plantilla = plantilla, detalles = detalles, cargos = cargosCat, ubicaciones = ubisCat, jefes = jefesCat }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpPost]
        public JsonResult GuardarBorrador(List<DetallePlantillaViewModel> filas)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    foreach (var f in filas)
                    {
                        db.Execute("SP_Comp_Comisiones",
                            new { Op = "GuardarDetalle", DetalleID = f.DetalleID, Cargo_Reportado = f.Cargo_Reportado, Jefe_Reportado = f.Jefe_Reportado, Ubicacion_Reportada = f.Ubicacion_Reportada, Comisiona = f.Comisiona, Observacion = f.Observacion },
                            commandType: CommandType.StoredProcedure);
                    }
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EnviarPlantilla(int plantillaId)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    db.Execute("SP_Comp_Comisiones", new { Op = "Enviar", PlantillaID = plantillaId }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true });
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult ObtenerTodosDetallesRevision(int periodoId)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    var data = db.Query<DetallePlantillaViewModel>("EXEC SP_Comp_Comisiones @Op='ObtenerRevisionRRHH', @PeriodoID=@periodoId", new { periodoId }).ToList();
                    return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult ObtenerDashboardRevision(int periodoId)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    var data = db.Query<DashboardRevisionViewModel>("EXEC SP_Comp_Comisiones @Op='DashboardRevision', @PeriodoID=@periodoId", new { periodoId }).ToList();
                    return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpPost]
        public JsonResult AprobarPlantilla(int plantillaId) {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    db.Execute("SP_Comp_Comisiones", new { Op = "Aprobar", PlantillaID = plantillaId }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true });
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult DevolverPlantilla(int plantillaId, string comentario) {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    db.Execute("SP_Comp_Comisiones", new { Op = "Devolver", PlantillaID = plantillaId, ComentarioDevolucion = comentario }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true });
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> NotificarValidadores(int periodoId)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    db.Open();
                    var pendientes = db.Query<string>("SELECT DISTINCT CarnetGerente FROM dbo.Comp_Plantilla WHERE PeriodoID = @periodoId AND TipoPlantillaID = 1 AND (Estado = 'Borrador' OR Estado = 'Devuelto')", new { periodoId }).ToList();
                    int count = 0;
                    foreach (var carnet in pendientes) {
                        await NotificarJefe(periodoId, carnet);
                        count++;
                    }
                    return Json(new { success = true, notificados = count });
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        private async Task NotificarJefe(int periodoId, string carnet) {
            using (var db = new SqlConnection(CadenaConexion)) {
                var per = db.QueryFirstOrDefault<PeriodoViewModel>("SELECT * FROM dbo.Comp_Periodo WHERE PeriodoID = @periodoId", new { periodoId });
                var jefe = db.QueryFirstOrDefault("SELECT nombre_completo, correo FROM SIGHO1.dbo.EMP2024 WHERE CARNET = @carnet", new { carnet });
                var detalles = db.Query<DetallePlantillaViewModel>("EXEC SP_Comp_Comisiones @Op='ObtenerDetalle', @CarnetGerente=@carnet, @PeriodoID=@periodoId", new { carnet, periodoId }).ToList();

                string body = "<h3>Portal RH Online - Comisiones</h3><p>Estimado validador, tiene pendiente la revisión de comisiones para el periodo: " + per.NombrePeriodo + "</p>";
                // Enviar usando el nuevo sistema Outbox/API
                await RegistrarYDespacharCorreo(db, "gustavo.lira@claro.com.ni", "[PRUEBA] RH Comisiones: " + per.NombrePeriodo, body);
            }
        }

        private async Task<bool> RegistrarYDespacharCorreo(SqlConnection db, string to, string subject, string body) {
            try {
                int id = db.QuerySingle<int>("INSERT INTO dbo.Comp_Com_Notificacion (EmailTo, Asunto, Cuerpo, Estado) OUTPUT INSERTED.NotificacionID VALUES (@to, @subject, @body, 'Pendiente')", new { to, subject, body });
                using (var client = new HttpClient()) {
                    var resp = await client.GetAsync("http://localhost:60992/Api/DespacharCorreo?notificacionId=" + id);
                    return resp.IsSuccessStatusCode;
                }
            } catch { return false; }
        }
    }
}
