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
    public class CertificacionController : Controller
    {
        private string CadenaConexion = ConfigurationManager.ConnectionStrings["CompensacionConnection"].ConnectionString;

        public ActionResult Index()
        {
            return View("~/Views/Compensacion/Certificacion.cshtml");
        }

        public ActionResult Revision()
        {
            return View("~/Views/Compensacion/RevisionCertificacion.cshtml");
        }

        [HttpGet]
        public JsonResult ObtenerPeriodos()
        {
            try
            {
                List<PeriodoViewModel> lista = new List<PeriodoViewModel>();
                using (SqlConnection cn = new SqlConnection(CadenaConexion))
                {
                    string sql = "SELECT PeriodoID, NombrePeriodo, Estado FROM dbo.Comp_Periodo WHERE TipoPlantillaID = 2 ORDER BY PeriodoID DESC";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            PeriodoViewModel item = new PeriodoViewModel();
                            if (dr["PeriodoID"] != DBNull.Value) item.PeriodoID = Convert.ToInt32(dr["PeriodoID"]);
                            if (dr["NombrePeriodo"] != DBNull.Value) item.NombrePeriodo = dr["NombrePeriodo"].ToString();
                            if (dr["Estado"] != DBNull.Value) item.Estado = dr["Estado"].ToString();
                            lista.Add(item);
                        }
                    }
                }
                var activo = lista.FirstOrDefault(x => x.Estado == "Activo") ?? lista.FirstOrDefault();
                return Json(new { success = true, data = lista, activoID = activo?.PeriodoID }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
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

                    // 1. Periodo Activo
                    PeriodoViewModel pActivo = null;
                    using (var cmd = new SqlCommand("SELECT TOP 1 PeriodoID, NombrePeriodo, Estado FROM dbo.Comp_Periodo WHERE Estado = 'Activo' AND TipoPlantillaID = 2 ORDER BY PeriodoID DESC", db))
                    {
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                pActivo = new PeriodoViewModel();
                                if (dr["PeriodoID"] != DBNull.Value) pActivo.PeriodoID = Convert.ToInt32(dr["PeriodoID"]);
                                if (dr["NombrePeriodo"] != DBNull.Value) pActivo.NombrePeriodo = dr["NombrePeriodo"].ToString();
                            }
                        }
                    }

                    if (!periodoId.HasValue && pActivo != null) periodoId = pActivo.PeriodoID;

                    // 2. Iniciar y Sincronizar
                    PlantillaViewModel plantilla = db.QueryFirstOrDefault<PlantillaViewModel>(
                        "SP_Comp_Certificacion",
                        new { Op = "IniciarPlantilla", PeriodoID = periodoId, CarnetGerente = employees.EmployeeNumber },
                        commandType: CommandType.StoredProcedure);

                    if (plantilla == null) return Json(new { success = false, message = "No tienes personal asignado para certificacion organizacional." }, JsonRequestBehavior.AllowGet);

                    // Sincronizar empleados (Logica avanzada consistente con Comisiones)
                    string sqlSync = @"
                        IF EXISTS (SELECT 1 FROM dbo.Comp_Com_ConfiguracionValidador WHERE CarnetValidador = @carnet AND ISNULL(Activo, 1) = 1)
                        BEGIN
                            INSERT INTO dbo.Comp_PlantillaDetalle (PlantillaID, CarnetEmpleado, NombreCompleto, Cargo_SIGHO, OGERENCIA_SIGHO, OSUBGERENCIA_SIGHO, Area_SIGHO, Ubicacion_SIGHO, Jefe_SIGHO, Departamento_SIGHO, Comisiona)
                            SELECT DISTINCT @pid, e.CARNET, e.nombre_completo, e.cargo, e.OGERENCIA, e.oSUBGERENCIA, e.primernivel, e.Nombreubicacion, e.nom_jefe1, e.primernivel, 'S'
                            FROM SIGHO1.dbo.EMP2024 e WITH (NOLOCK)
                            JOIN dbo.Comp_Com_ConfiguracionValidador cv ON 
                                (cv.CarnetEmpleado = e.CARNET)
                                OR (cv.CarnetEmpleado IS NULL AND e.OGERENCIA = cv.Gerencia 
                                    AND (cv.Subgerencia IS NULL OR e.oSUBGERENCIA = cv.Subgerencia)
                                    AND (cv.Area IS NULL OR e.primernivel = cv.Area))
                            WHERE e.fechabaja IS NULL AND cv.CarnetValidador = @carnet AND ISNULL(cv.Activo, 1) = 1
                            AND NOT EXISTS (SELECT 1 FROM dbo.Comp_PlantillaDetalle WHERE PlantillaID = @pid AND CarnetEmpleado = e.CARNET);
                        END
                        ELSE
                        BEGIN
                            INSERT INTO dbo.Comp_PlantillaDetalle (PlantillaID, CarnetEmpleado, NombreCompleto, Cargo_SIGHO, OGERENCIA_SIGHO, OSUBGERENCIA_SIGHO, Area_SIGHO, Ubicacion_SIGHO, Jefe_SIGHO, Departamento_SIGHO, Comisiona)
                            SELECT @pid, e.CARNET, e.nombre_completo, e.cargo, e.OGERENCIA, e.oSUBGERENCIA, e.primernivel, e.Nombreubicacion, e.nom_jefe1, e.primernivel, 'S'
                            FROM SIGHO1.dbo.EMP2024 e WITH (NOLOCK)
                            WHERE e.fechabaja IS NULL AND e.carnet_jefe1 = @carnet
                            AND NOT EXISTS (SELECT 1 FROM dbo.Comp_PlantillaDetalle WHERE PlantillaID = @pid AND CarnetEmpleado = e.CARNET);
                        END";
                    db.Execute(sqlSync, new { pid = plantilla.PlantillaID, carnet = employees.EmployeeNumber });

                    // 3. Obtener Todo (Detalles + Catalogos) en un solo viaje
                    List<DetallePlantillaViewModel> detalles = new List<DetallePlantillaViewModel>();
                    List<string> cargosCat = new List<string>();
                    List<string> ubisCat = new List<string>();
                    List<string> jefesCat = new List<string>();
                    List<string> deptosCat = new List<string>();

                    string sqlMulti = @"
                        -- Detalles
                        SELECT 
                            d.DetalleID, d.PlantillaID, d.CarnetEmpleado, d.NombreCompleto,
                            d.Cargo_SIGHO, d.OGERENCIA_SIGHO, d.OSUBGERENCIA_SIGHO, d.Area_SIGHO,
                            d.Ubicacion_SIGHO, d.Jefe_SIGHO, d.Departamento_SIGHO,
                            d.Cargo_Reportado, d.Jefe_Reportado, d.Ubicacion_Reportada, d.Departamento_Reportado,
                            d.Observacion, d.JustMotivo, d.JustReposicion, d.JustTiempo,
                            CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = d.DetalleID) THEN 1 ELSE 0 END AS BIT) as HasEvidencia,
                            CAST(CASE WHEN (ISNULL(d.Cargo_Reportado, d.Cargo_SIGHO) <> d.Cargo_SIGHO OR 
                                           ISNULL(d.Jefe_Reportado, d.Jefe_SIGHO) <> d.Jefe_SIGHO OR 
                                           ISNULL(d.Ubicacion_Reportada, d.Ubicacion_SIGHO) <> d.Ubicacion_SIGHO OR
                                           ISNULL(d.Departamento_Reportado, d.Departamento_SIGHO) <> d.Departamento_SIGHO
                                      ) THEN 1 ELSE 0 END AS INT) as EsDiscrepancia
                        FROM dbo.Comp_PlantillaDetalle d WITH (NOLOCK)
                        WHERE d.PlantillaID = @pid
                        ORDER BY d.NombreCompleto;

                        -- Catalogo Cargos
                        SELECT DISTINCT NombreCargo FROM dbo.Comp_CatalogoCargo WITH (NOLOCK) ORDER BY NombreCargo;

                        -- Catalogo Ubicaciones
                        SELECT DISTINCT Nombreubicacion FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) WHERE fechabaja IS NULL ORDER BY Nombreubicacion;

                        -- Catalogo Jefes
                        SELECT DISTINCT nombre_completo FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) WHERE nombre_completo IS NOT NULL AND fechabaja IS NULL ORDER BY nombre_completo;

                        -- Catalogo Departamentos
                        SELECT DISTINCT primernivel FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) WHERE primernivel IS NOT NULL AND fechabaja IS NULL
                        UNION
                        SELECT NombreDepartamento FROM dbo.Comp_CatalogoDepartamento WITH (NOLOCK)
                        ORDER BY primernivel;";

                    using (var cmd = new SqlCommand(sqlMulti, db))
                    {
                        cmd.Parameters.AddWithValue("@pid", plantilla.PlantillaID);
                        cmd.CommandTimeout = 120;
                        using (var dr = cmd.ExecuteReader())
                        {
                            // ResultSet 1: Detalles
                            while (dr.Read())
                            {
                                var item = new DetallePlantillaViewModel();
                                if (dr["DetalleID"] != DBNull.Value) item.DetalleID = Convert.ToInt32(dr["DetalleID"]);
                                if (dr["PlantillaID"] != DBNull.Value) item.PlantillaID = Convert.ToInt32(dr["PlantillaID"]);
                                if (dr["CarnetEmpleado"] != DBNull.Value) item.CarnetEmpleado = dr["CarnetEmpleado"].ToString();
                                if (dr["NombreCompleto"] != DBNull.Value) item.NombreCompleto = dr["NombreCompleto"].ToString();
                                if (dr["Cargo_SIGHO"] != DBNull.Value) item.Cargo_SIGHO = dr["Cargo_SIGHO"].ToString();
                                if (dr["OGERENCIA_SIGHO"] != DBNull.Value) item.OGERENCIA_SIGHO = dr["OGERENCIA_SIGHO"].ToString();
                                if (dr["Ubicacion_SIGHO"] != DBNull.Value) item.Ubicacion_SIGHO = dr["Ubicacion_SIGHO"].ToString();
                                if (dr["Jefe_SIGHO"] != DBNull.Value) item.Jefe_SIGHO = dr["Jefe_SIGHO"].ToString();
                                if (dr["Departamento_SIGHO"] != DBNull.Value) item.Departamento_SIGHO = dr["Departamento_SIGHO"].ToString();
                                item.Cargo_Reportado = dr["Cargo_Reportado"] != DBNull.Value ? dr["Cargo_Reportado"].ToString() : null;
                                item.Jefe_Reportado = dr["Jefe_Reportado"] != DBNull.Value ? dr["Jefe_Reportado"].ToString() : null;
                                item.Ubicacion_Reportada = dr["Ubicacion_Reportada"] != DBNull.Value ? dr["Ubicacion_Reportada"].ToString() : null;
                                item.Departamento_Reportado = dr["Departamento_Reportado"] != DBNull.Value ? dr["Departamento_Reportado"].ToString() : null;
                                if (dr["Observacion"] != DBNull.Value) item.Observacion = dr["Observacion"].ToString();
                                if (dr["HasEvidencia"] != DBNull.Value) item.HasEvidencia = Convert.ToBoolean(dr["HasEvidencia"]);
                                if (dr["EsDiscrepancia"] != DBNull.Value) item.EsDiscrepancia = Convert.ToInt32(dr["EsDiscrepancia"]);
                                detalles.Add(item);
                            }

                            // ResultSet 2: Cargos
                            if (dr.NextResult()) while (dr.Read()) { if (dr[0] != DBNull.Value) cargosCat.Add(dr[0].ToString()); }
                            // ResultSet 3: Ubicaciones
                            if (dr.NextResult()) while (dr.Read()) { if (dr[0] != DBNull.Value) ubisCat.Add(dr[0].ToString()); }
                            // ResultSet 4: Jefes
                            if (dr.NextResult()) while (dr.Read()) { if (dr[0] != DBNull.Value) jefesCat.Add(dr[0].ToString()); }
                            // ResultSet 5: Departamentos
                            if (dr.NextResult()) while (dr.Read()) { if (dr[0] != DBNull.Value) deptosCat.Add(dr[0].ToString()); }
                        }
                    }

                    bool isMaster = employees.EmployeeNumber == "500708";
                    var result = new { success = true, isMaster = isMaster, plantilla = plantilla, detalles = detalles, cargos = cargosCat, ubicaciones = ubisCat, jefes = jefesCat, departamentos = deptosCat };
                    return new JsonResult { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = int.MaxValue };
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
                        db.Execute("SP_Comp_Certificacion",
                            new { Op = "GuardarDetalle", DetalleID = f.DetalleID, Cargo_Reportado = f.Cargo_Reportado, Jefe_Reportado = f.Jefe_Reportado, Ubicacion_Reportada = f.Ubicacion_Reportada, Departamento_Reportado = f.Departamento_Reportado, Observacion = f.Observacion, JustMotivo = f.JustMotivo, JustReposicion = f.JustReposicion, JustTiempo = f.JustTiempo },
                            commandType: CommandType.StoredProcedure);
                    }
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EnviarPlantilla(int plantillaId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // 1. Obtener info para el correo
                    var info = db.QueryFirstOrDefault<dynamic>(@"
                        SELECT p.CarnetGerente, e.nombre_completo as NombreGerente, p.OGerencia as Gerencia, pr.NombrePeriodo, p.TipoPlantillaID
                        FROM dbo.Comp_Plantilla p
                        INNER JOIN SIGHO1.dbo.EMP2024 e ON p.CarnetGerente = e.CARNET
                        INNER JOIN dbo.Comp_Periodo pr ON p.PeriodoID = pr.PeriodoID
                        WHERE p.PlantillaID = @plantillaId", new { plantillaId });

                    // 2. Ejecutar SP
                    db.Execute("SP_Comp_Certificacion", new { Op = "Enviar", PlantillaID = plantillaId }, commandType: CommandType.StoredProcedure);

                    // 3. Notificacion
                    if (info != null)
                    {
                        string tipoStr = "Certificacion Organizacional";
                        string subject = $"[RHOnline] Certificacion Finalizada: {info.NombreGerente} - {info.Gerencia}";

                        string body = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
                                <div style='max-width: 600px; margin: 20px auto; border: 1px solid #ddd; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.1);'>
                                    <div style='background-color: #B71C1C; color: white; padding: 20px; text-align: center;'>
                                        <h2 style='margin: 0;'>Confirmacion de Envio</h2>
                                        <p style='margin: 5px 0 0;'>Modulo de Certificacion de Personal</p>
                                    </div>
                                    <div style='padding: 30px;'>
                                        <p>Se ha recibido exitosamente la <b>{tipoStr}</b> del periodo <b>{info.NombrePeriodo}</b>.</p>
                                        <div style='background-color: #f9f9f9; padding: 20px; border-radius: 8px; border-left: 4px solid #B71C1C;'>
                                            <table style='width: 100%; border-collapse: collapse;'>
                                                <tr><td style='padding: 5px 0;'><strong>Validador:</strong></td><td>{info.NombreGerente} ({info.CarnetGerente})</td></tr>
                                                <tr><td style='padding: 5px 0;'><strong>Gerencia:</strong></td><td>{info.Gerencia}</td></tr>
                                                <tr><td style='padding: 5px 0;'><strong>Fecha:</strong></td><td>{DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>
                                            </table>
                                        </div>
                                        <p style='margin-top: 25px;'>La informacion ya esta disponible para la revision del equipo de RRHH.</p>
                                    </div>
                                    <div style='background-color: #f1f1f1; color: #777; padding: 15px; text-align: center; font-size: 12px;'>
                                        Este es un mensaje automatico generado por RHOnline. No es necesario responder.
                                    </div>
                                </div>
                            </body>
                            </html>";

                        await RegistrarYDespacharCorreo(db, "compensacion@claro.com.ni", subject, body);
                    }

                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> AprobarPlantilla(int plantillaId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("SP_Comp_Certificacion", new { Op = "AprobarPlantilla", PlantillaID = plantillaId }, commandType: CommandType.StoredProcedure);

                    // Notificar por correo al Jefe Inmediato
                    var plantilla = db.QueryFirstOrDefault<PlantillaViewModel>("SELECT CarnetGerente FROM dbo.Comp_Plantilla WHERE PlantillaID = @id", new { id = plantillaId });
                    if (plantilla != null)
                    {
                        var validador = db.QueryFirstOrDefault("SELECT correo, nombre_completo as Nombre FROM SIGHO1.dbo.EMP2024 WHERE carnet = @carnet", new { carnet = plantilla.CarnetGerente });
                        if (validador != null && !string.IsNullOrEmpty(validador.correo))
                        {
                            string mensajeHtml = $@"
                                <html><body>
                                <h2 style='color:#2e7d32;'>Plantilla de Certificación Aprobada</h2>
                                <p>Estimado/a <b>{validador.Nombre}</b>,</p>
                                <p>Recursos Humanos ha <b>APROBADO</b> exitosamente su estructura en el módulo de Certificación de Personal.</p>
                                <p>Gracias por su gestión.</p>
                                </body></html>";

                            // Enviado directamente al validador
                            string correo = validador.correo;
                            await RegistrarYDespacharCorreo(db, correo, "APROBADA: Certificación de Personal", mensajeHtml);
                        }
                    }

                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> DevolverPlantilla(int plantillaId, string comentario)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("SP_Comp_Certificacion", new { Op = "DevolverPlantilla", PlantillaID = plantillaId, ComentarioDevolucion = comentario }, commandType: CommandType.StoredProcedure);

                    // Notificar por correo al Jefe Inmediato
                    var plantilla = db.QueryFirstOrDefault<PlantillaViewModel>("SELECT CarnetGerente FROM dbo.Comp_Plantilla WHERE PlantillaID = @id", new { id = plantillaId });
                    if (plantilla != null)
                    {
                        var validador = db.QueryFirstOrDefault("SELECT correo, nombre_completo as Nombre FROM SIGHO1.dbo.EMP2024 WHERE carnet = @carnet", new { carnet = plantilla.CarnetGerente });
                        if (validador != null && !string.IsNullOrEmpty(validador.correo))
                        {
                            string mensajeHtml = $@"
                                <html><body>
                                <h2 style='color:#d9534f;'>Plantilla de Certificación Devuelta</h2>
                                <p>Estimado/a <b>{validador.Nombre}</b>,</p>
                                <p>Recursos Humanos ha <b>devuelto</b> su estructura de Certificación de Personal para corrección.</p>
                                <p><b>Observaciones de RRHH:</b></p>
                                <blockquote style='border-left: 4px solid #d9534f; padding-left: 10px; color:#555;'>{comentario}</blockquote>
                                <p>Por favor, ingrese al Portal de Recursos Humanos y realice los ajustes solicitados.</p>
                                <p><a href='http://10.200.5.24:81/Compensacion/Certificacion' style='display:inline-block; padding:10px 20px; background-color:#007bff; color:white; text-decoration:none; border-radius:5px;'>Ir al Portal RH</a></p>
                                </body></html>";

                            // Enviado directamente al validador
                            string correo = validador.correo;
                            await RegistrarYDespacharCorreo(db, correo, "URGENTE: Certificación de Personal Devuelta por RRHH", mensajeHtml);
                        }
                    }

                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult ReabrirPlantilla(int plantillaId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("EXEC SP_Comp_Certificacion @Op='ReabrirPlantilla', @PlantillaID=@plantillaId", new { plantillaId });
                    return Json(new { success = true, message = "Plantilla reabierta exitosamente." });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult ObtenerTodosDetallesRevision(int periodoId)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    var data = db.Query<DetallePlantillaViewModel>("EXEC SP_Comp_Certificacion @Op='ObtenerRevisionRRHH', @PeriodoID=@periodoId", new { periodoId }).ToList();
                    return new JsonResult {
                        Data = new { success = true, data = data },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                        MaxJsonLength = int.MaxValue
                    };
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult ObtenerDashboardRevision(int periodoId)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    var data = db.Query<DashboardRevisionViewModel>("EXEC SP_Comp_Certificacion @Op='DashboardRevision', @PeriodoID=@periodoId", new { periodoId }).Where(x => x.TotalColaboradores > 0).ToList();
                    return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpPost]
        public async Task<JsonResult> NotificarValidadores(int periodoId)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    db.Open();
                    var pendientes = db.Query<string>("SELECT DISTINCT CarnetGerente FROM dbo.Comp_Plantilla WHERE PeriodoID = @periodoId AND TipoPlantillaID = 2 AND (Estado = 'Borrador' OR Estado = 'Devuelto')", new { periodoId }).ToList();
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
                var jefe = db.QueryFirstOrDefault("SELECT nombre_completo FROM SIGHO1.dbo.EMP2024 WHERE CARNET = @carnet", new { carnet });
                string body = "<h3>RH Online - Certificación Organizacional</h3><p>Estimado jefe, tiene pendiente la certificación de su personal para el periodo: " + per.NombrePeriodo + "</p>";
                string correo = db.QueryFirstOrDefault<string>("SELECT correo FROM SIGHO1.dbo.EMP2024 WHERE CARNET = @carnet", new { carnet });
                if (!string.IsNullOrEmpty(correo)) {
                    await RegistrarYDespacharCorreo(db, correo, "Certificación Personal: " + per.NombrePeriodo, body);
                }
            }
        }

        private async Task<bool> RegistrarYDespacharCorreo(SqlConnection db, string to, string subject, string body) {
            try {
                string compactBody = body.Replace("\r\n", "").Replace("    ", "");

                int id = db.QueryFirstOrDefault<int>(@"
                    INSERT INTO dbo.Comp_Com_Notificacion (Para, Asunto, Cuerpo, Estado) 
                    VALUES (@to, @subject, @body, 'Pendiente');
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", 
                    new { to, subject, body = compactBody });

                using (var client = new HttpClient()) {
                    client.Timeout = TimeSpan.FromSeconds(60);
                    var payload = new { id = id };
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://172.26.54.66/apihcm/api/values/correo/compensacion", content);
                    return response.IsSuccessStatusCode;
                }
            } catch { return false; }
        }

        [HttpGet]
        public JsonResult ObtenerGerenciasValidador()
        {
            try
            {
                Entities.Employees employees = (Entities.Employees)Session["User"];
                List<string> lista = new List<string>();
                using (SqlConnection cn = new SqlConnection(CadenaConexion))
                {
                    string sql = "SELECT DISTINCT OGERENCIA FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) WHERE carnet_jefe1 = @carnet OR @carnet = '500708'";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@carnet", employees.EmployeeNumber);
                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (dr[0] != DBNull.Value) lista.Add(dr[0].ToString());
                        }
                    }
                }
                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult BuscarEmpleadoSighoo(string carnet)
        {
            try
            {
                if (string.IsNullOrEmpty(carnet)) return Json(new { success = false, message = "Ingrese un carnet o correo." }, JsonRequestBehavior.AllowGet);

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    string sql = @"
                        SELECT TOP 1 
                            CARNET as CARNET, 
                            nombre_completo as NombreCompleto, 
                            correo as Correo, 
                            OGERENCIA as OGERENCIA_SIGHO, 
                            oSUBGERENCIA as OSUBGERENCIA_SIGHO, 
                            primernivel as Area_SIGHO,
                            cargo as Cargo_SIGHO,
                            Nombreubicacion as Ubicacion_SIGHO,
                            nom_jefe1 as Jefe_SIGHO
                        FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK)
                        WHERE fechabaja IS NULL 
                          AND (CARNET = @filtro OR correo = @filtro OR correo LIKE @filtro + '@%')";

                    var result = db.QueryFirstOrDefault(sql, new { filtro = carnet.Trim() }) as IDictionary<string, object>;

                    if (result == null) return Json(new { success = false, message = "Empleado no encontrado en SIGHO1." }, JsonRequestBehavior.AllowGet);
                    
                    return Json(new { 
                        success = true, 
                        data = new {
                            CARNET = result.ContainsKey("CARNET") ? result["CARNET"] : "",
                            NombreCompleto = result.ContainsKey("NombreCompleto") ? result["NombreCompleto"] : "",
                            Cargo_SIGHO = result.ContainsKey("Cargo_SIGHO") ? result["Cargo_SIGHO"] : ""
                        }
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpPost]
        public JsonResult AgregarEmpleadoManual(string carnet, int plantillaId, string gerencia = null)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("EXEC SP_Comp_Certificacion @Op='AgregarEmpleadoManual', @PlantillaID=@plantillaId, @CarnetUsuario=@carnet, @Gerencia=@gerencia", new { carnet, plantillaId, gerencia });
                    return Json(new { success = true, message = "Empleado agregado a la lista correctamente." });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EliminarEmpleadoManual(int detalleId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    // 1. Eliminar evidencia primero
                    db.Execute("DELETE FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = @detalleId", new { detalleId });

                    // 2. Eliminar el detalle
                    int eliminados = db.Execute("DELETE FROM dbo.Comp_PlantillaDetalle WHERE DetalleID = @detalleId", new { detalleId });
                    
                    if (eliminados > 0)
                        return Json(new { success = true, message = "Colaborador eliminado." });
                    else
                        return Json(new { success = false, message = "No se encontro el registro." });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [HttpPost]
        public JsonResult SubirEvidencia(int detalleId)
        {
            try
            {
                if (Request.Files.Count == 0) return Json(new { success = false, message = "No se recibio ningun archivo." });
                var file = Request.Files[0];
                if (file == null || file.ContentLength == 0) return Json(new { success = false, message = "Archivo vacio o invalido." });

                Entities.Employees user = (Entities.Employees)Session["User"];
                string fileName = System.IO.Path.GetFileName(file.FileName);
                string extension = System.IO.Path.GetExtension(fileName);
                
                byte[] fileData;
                using (var binaryReader = new System.IO.BinaryReader(file.InputStream))
                {
                    fileData = binaryReader.ReadBytes(file.ContentLength);
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("DELETE FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = @detalleId", new { detalleId });
                    string sql = @"INSERT INTO dbo.Comp_PlantillaEvidencia (DetalleID, NombreArchivo, RutaArchivo, Extension, Tamanio, UsuarioCarga, ArchivoBinario)
                                   VALUES (@detalleId, @fileName, 'DATABASE', @extension, @tamanio, @usuario, @fileData)";
                    db.Execute(sql, new { detalleId, fileName, extension, tamanio = file.ContentLength, usuario = user.EmployeeNumber, fileData });
                }
                return Json(new { success = true, message = "Archivo guardado exitosamente." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public ActionResult DescargarEvidencia(int detalleId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var ev = db.QueryFirstOrDefault("SELECT TOP 1 NombreArchivo, ArchivoBinario, Extension FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = @detalleId ORDER BY EvidenciaID DESC", new { detalleId });
                    if (ev == null) return Content("No se encontro evidencia.");
                    byte[] fileBytes = (byte[])ev.ArchivoBinario;
                    return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, (string)ev.NombreArchivo);
                }
            }
            catch (Exception ex) { return Content("Error: " + ex.Message); }
        }

        [HttpPost]
        public JsonResult EliminarEvidencia(int detalleId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("DELETE FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = @detalleId", new { detalleId });
                }
                return Json(new { success = true, message = "Evidencia eliminada." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult ObtenerEvidencia(int detalleId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var ev = db.QueryFirstOrDefault("SELECT * FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = @detalleId", new { detalleId });
                    return Json(new { success = true, data = ev }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }
    }
}
