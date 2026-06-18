using Dapper;
using Newtonsoft.Json;
using slnRhonline.Models;
using slnRhonline.Models.Compensacion;
using slnRhonline;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;


namespace slnRhonline.Controllers
{
    public class CompensacionController : Controller
    {
        private string CadenaConexion = ConfigurationManager.ConnectionStrings["CompensacionConnection"].ConnectionString;

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Plantilla(int tipo = 1)
        {
            ViewBag.TipoPlantilla = tipo;
            return View();
        }

        public ActionResult Revision(int tipo = 1)
        {
            ViewBag.Tipo = tipo;
            return View();
        }

        public ActionResult RevisionComisiones()
        {
            ViewBag.Tipo = 1;
            return View("RevisionComisiones");
        }

        public ActionResult RevisionCertificacion()
        {
            ViewBag.Tipo = 2;
            return View("RevisionCertificacion");
        }
        
        [HttpGet]
        public ActionResult Comisiones()
        {
            ViewBag.TipoPlantilla = 1;
            return View("Plantilla");
        }
        [HttpGet]
        public ActionResult Personal()
        {
            ViewBag.TipoPlantilla = 2;
            return View("Certificacion");
        }
        [HttpGet]
        public ActionResult GestionMaestra()
        {
            return View();
        }

        [HttpGet]
        public JsonResult ObtenerUniversoMaestro()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var periodoCom = db.QueryFirstOrDefault<PeriodoViewModel>("SELECT TOP 1 * FROM dbo.Comp_Periodo WHERE Estado = 'Activo' AND TipoPlantillaID = 1 ORDER BY PeriodoID DESC");
                    var periodoCert = db.QueryFirstOrDefault<PeriodoViewModel>("SELECT TOP 1 * FROM dbo.Comp_Periodo WHERE Estado = 'Activo' AND TipoPlantillaID = 2 ORDER BY PeriodoID DESC");
                    
                    int pid = periodoCom?.PeriodoID ?? 0;

                    // Consulta robusta que trae el universo de SIGHO y cruza con configuraciones y plantillas actuales
                    string sql = @"
                        SELECT 
                            ISNULL(d.DetalleID, 0) as DetalleID,
                            e.CARNET as CarnetEmpleado, 
                            e.nombre_completo as NombreCompleto, 
                            e.OGERENCIA as OGERENCIA_SIGHO, 
                            ISNULL(e.oSUBGERENCIA, '') as OSUBGERENCIA_SIGHO, 
                            ISNULL(e.primernivel, '') as Area_SIGHO, 
                            ISNULL(e.cargo, '') as Cargo_SIGHO, 
                            ISNULL(e.Nombreubicacion, '') as Ubicacion_SIGHO, 
                            ISNULL(e.nom_jefe1, '') as Jefe_SIGHO,
                            ISNULL(cv.NombreValidador, 'POR ASIGNAR') as NombreGerente,
                            ISNULL(cv.CarnetValidador, '') as CarnetGerente,
                            ISNULL(d.AplicaEmpleado, 'S') as AplicaEmpleado,
                            ISNULL(d.AplicaComision, 'S') as AplicaComision
                        FROM SIGHO1.dbo.EMP2024 e WITH (NOLOCK)
                        LEFT JOIN dbo.Comp_Com_ConfiguracionValidador cv ON 
                            (cv.CarnetEmpleado = e.CARNET) -- Asignacion por empleado especifico
                            OR (cv.CarnetEmpleado IS NULL AND e.OGERENCIA = cv.Gerencia 
                                AND (cv.Subgerencia IS NULL OR e.oSUBGERENCIA = cv.Subgerencia) 
                                AND (cv.Area IS NULL OR e.primernivel = cv.Area))
                        LEFT JOIN dbo.Comp_Plantilla pl ON pl.PeriodoID = @pid AND pl.CarnetGerente = cv.CarnetValidador
                        LEFT JOIN dbo.Comp_PlantillaDetalle d ON d.PlantillaID = pl.PlantillaID AND d.CarnetEmpleado = e.CARNET
                        WHERE e.fechabaja IS NULL";

                    var universo = db.Query<DetallePlantillaViewModel>(sql, new { pid }).ToList();
                    
                    var jsonRes = Json(new { success = true, data = universo, periodoCom = periodoCom, periodoCert = periodoCert }, JsonRequestBehavior.AllowGet);
                    jsonRes.MaxJsonLength = int.MaxValue;
                    return jsonRes;
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult PeriodoActivo(int tipo = 1)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var p = db.QueryFirstOrDefault<PeriodoViewModel>("SP_Comp_Plantilla", new { Op = "PeriodoActivo", TipoPlantillaID = tipo }, commandType: CommandType.StoredProcedure);
                    return Json(p, JsonRequestBehavior.AllowGet);
                }
            }
            catch { return Json(null, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult ObtenerPeriodos(int tipo = 1)
        {
            try
            {
                List<PeriodoViewModel> lista = new List<PeriodoViewModel>();
                using (SqlConnection cn = new SqlConnection(CadenaConexion))
                {
                    string sql = "SELECT PeriodoID, NombrePeriodo, Estado FROM dbo.Comp_Periodo WHERE TipoPlantillaID = @tipo ORDER BY PeriodoID DESC";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@tipo", tipo);
                    cn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        PeriodoViewModel item = new PeriodoViewModel();
                        if (dr["PeriodoID"] != DBNull.Value) item.PeriodoID = Convert.ToInt32(dr["PeriodoID"]);
                        if (dr["NombrePeriodo"] != DBNull.Value) item.NombrePeriodo = dr["NombrePeriodo"].ToString();
                        if (dr["Estado"] != DBNull.Value) item.Estado = dr["Estado"].ToString();
                        
                        lista.Add(item);
                    }
                }

                var activo = lista.FirstOrDefault(x => x.Estado == "Activo") ?? lista.FirstOrDefault();
                return Json(new { success = true, data = lista, activoID = activo?.PeriodoID }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerKPI(int periodoId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var kpi = db.QueryFirstOrDefault("SP_Comp_Plantilla", new { Op = "ObtenerKPI", PeriodoID = periodoId }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true, kpi = kpi }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerGerenciasSigho()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query<string>(@"SELECT DISTINCT OGERENCIA FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) 
                                                WHERE OGERENCIA IS NOT NULL AND fechabaja IS NULL 
                                                ORDER BY 1").ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult ObtenerSubgerencias(string gerencia)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query<string>(@"SELECT DISTINCT OSUBGERENCIA FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) 
                                                WHERE OGERENCIA = @gerencia AND OSUBGERENCIA IS NOT NULL AND fechabaja IS NULL 
                                                UNION
                                                SELECT DISTINCT Subgerencia FROM dbo.Comp_Com_EstructurasManuales WITH (NOLOCK)
                                                WHERE Gerencia = @gerencia AND Subgerencia IS NOT NULL
                                                ORDER BY 1", new { gerencia }).ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult ObtenerAreas(string gerencia, string subgerencia)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query<string>(@"SELECT DISTINCT primernivel FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) 
                                                WHERE OGERENCIA = @gerencia AND OSUBGERENCIA = @subgerencia AND primernivel IS NOT NULL AND fechabaja IS NULL 
                                                UNION
                                                SELECT DISTINCT Area FROM dbo.Comp_Com_EstructurasManuales WITH (NOLOCK)
                                                WHERE Gerencia = @gerencia AND Subgerencia = @subgerencia AND Area IS NOT NULL
                                                ORDER BY 1", new { gerencia, subgerencia }).ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult ObtenerBitacora()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var logs = db.Query("SP_Comp_Plantilla", new { Op = "ObtenerBitacora" }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true, logs = logs }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult CargarMiPlantilla(int? periodoId, int tipo = 1)
        {
            try
            {
                Entities.Employees employees = (Entities.Employees)Session["User"];

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // 1. Periodo activo (consulta ligera)
                    PeriodoViewModel pActivo = null;
                    using (var cmd = new SqlCommand("SELECT TOP 1 PeriodoID, NombrePeriodo, Estado FROM dbo.Comp_Periodo WHERE Estado = 'Activo' AND TipoPlantillaID = @tipo ORDER BY Anio DESC, Mes DESC", db))
                    {
                        cmd.Parameters.AddWithValue("@tipo", tipo);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                pActivo = new PeriodoViewModel();
                                if (dr["PeriodoID"] != DBNull.Value) pActivo.PeriodoID = Convert.ToInt32(dr["PeriodoID"]);
                                if (dr["NombrePeriodo"] != DBNull.Value) pActivo.NombrePeriodo = dr["NombrePeriodo"].ToString();
                                if (dr["Estado"] != DBNull.Value) pActivo.Estado = dr["Estado"].ToString();
                            }
                        }
                    }

                    bool esActivo = false;
                    if (!periodoId.HasValue)
                    {
                        if (pActivo == null) return Json(new { success = false, message = "No hay periodo activo configurado en el sistema." }, JsonRequestBehavior.AllowGet);
                        periodoId = pActivo.PeriodoID;
                        esActivo = true;
                    }
                    else if (pActivo != null && pActivo.PeriodoID == periodoId.Value)
                    {
                        esActivo = true;
                    }

                    PlantillaViewModel plantilla = null;

                    if (esActivo)
                    {
                        plantilla = db.QueryFirstOrDefault<PlantillaViewModel>(
                            "SP_Comp_Plantilla",
                            new { Op = "IniciarPlantilla", PeriodoID = periodoId, CarnetGerente = employees.EmployeeNumber, TipoPlantillaID = tipo },
                            commandType: CommandType.StoredProcedure);

                        if (plantilla == null) return Json(new { success = false, message = "No tienes personal a cargo o segun tu perfil estas excluidos del proceso." }, JsonRequestBehavior.AllowGet);

                        // Sincronizar empleados (Reemplaza al SP para aplicar filtros de jerarquia correctamente)
                        string sqlSync = @"
                            IF EXISTS (SELECT 1 FROM dbo.Comp_Com_ConfiguracionValidador WHERE CarnetValidador = @carnet AND ISNULL(Activo, 1) = 1)
                            BEGIN
                                -- MODO CONFIGURACION: Respeta Gerencia, Subgerencia y Area
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
                                -- MODO JERARQUIA: Reportes directos de SIGHO
                                INSERT INTO dbo.Comp_PlantillaDetalle (PlantillaID, CarnetEmpleado, NombreCompleto, Cargo_SIGHO, OGERENCIA_SIGHO, OSUBGERENCIA_SIGHO, Area_SIGHO, Ubicacion_SIGHO, Jefe_SIGHO, Departamento_SIGHO, Comisiona)
                                SELECT @pid, e.CARNET, e.nombre_completo, e.cargo, e.OGERENCIA, e.oSUBGERENCIA, e.primernivel, e.Nombreubicacion, e.nom_jefe1, e.primernivel, 'S'
                                FROM SIGHO1.dbo.EMP2024 e WITH (NOLOCK)
                                WHERE e.fechabaja IS NULL AND e.carnet_jefe1 = @carnet
                                AND NOT EXISTS (SELECT 1 FROM dbo.Comp_PlantillaDetalle WHERE PlantillaID = @pid AND CarnetEmpleado = e.CARNET);
                            END";
                        db.Execute(sqlSync, new { pid = plantilla.PlantillaID, carnet = employees.EmployeeNumber });

                        // APLICAR CARRY-FORWARD: Si no hay cambios reportados aun, traer lo del mes aprobado anterior
                        AplicarCarryForward(db, plantilla.PlantillaID, null);
                    }
                    else
                    {
                        // Para historicos, solo leemos lo que ya existe sin tocar nada
                        plantilla = db.QueryFirstOrDefault<PlantillaViewModel>(
                            "SELECT * FROM dbo.Comp_Plantilla WHERE PeriodoID = @periodoId AND CarnetGerente = @carnet AND TipoPlantillaID = @tipo",
                            new { periodoId = periodoId.Value, carnet = employees.EmployeeNumber, tipo });

                        if (plantilla == null) return Json(new { success = false, message = "No se encontro registro de validacion para el periodo seleccionado." }, JsonRequestBehavior.AllowGet);
                    }

                    // 2. Detalles + Catalogos en UNA sola consulta multi-resultado
                    List<DetallePlantillaViewModel> detalles = new List<DetallePlantillaViewModel>();
                    List<string> cargosCat = new List<string>();
                    List<string> ubisCat = new List<string>();
                    List<string> jefesCat = new List<string>();
                    List<string> deptosCat = new List<string>();

                    string sqlMulti = @"
                        -- ResultSet 1: Detalles
                        SELECT DetalleID, PlantillaID, CarnetEmpleado, NombreCompleto, Cargo_SIGHO, 
                               OGERENCIA_SIGHO, OSUBGERENCIA_SIGHO, Area_SIGHO, Ubicacion_SIGHO, 
                               Jefe_SIGHO, Departamento_SIGHO, Comisiona, Observacion, 
                               Cargo_Reportado, Jefe_Reportado, Ubicacion_Reportada, Departamento_Reportado,
                               AplicaEmpleado, AplicaComision, EsPrueba_SIGHO,
                               JustMotivo, JustReposicion, JustTiempo,
                               CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = d.DetalleID) THEN 1 ELSE 0 END AS BIT) as HasEvidencia,
                               CAST(CASE WHEN (Cargo_Reportado IS NOT NULL AND Cargo_Reportado <> Cargo_SIGHO) 
                                         OR (Jefe_Reportado IS NOT NULL AND Jefe_Reportado <> Jefe_SIGHO) 
                                         OR (Comisiona = 'N') THEN 1 ELSE 0 END AS INT) as EsDiscrepancia
                        FROM dbo.Comp_PlantillaDetalle d WITH (NOLOCK)
                        WHERE PlantillaID = @pid
                        ORDER BY NombreCompleto;

                        -- ResultSet 2: Cargos
                        SELECT DISTINCT NombreCargo FROM dbo.Comp_CatalogoCargo WITH (NOLOCK) ORDER BY NombreCargo;

                        -- ResultSet 3: Ubicaciones
                        SELECT DISTINCT Nombreubicacion FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) WHERE Nombreubicacion IS NOT NULL AND fechabaja IS NULL ORDER BY Nombreubicacion;

                        -- ResultSet 4: Jefes
                        SELECT DISTINCT nombre_completo FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) WHERE nombre_completo IS NOT NULL AND fechabaja IS NULL ORDER BY nombre_completo;

                        -- ResultSet 5: Departamentos
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
                                if (dr["OSUBGERENCIA_SIGHO"] != DBNull.Value) item.OSUBGERENCIA_SIGHO = dr["OSUBGERENCIA_SIGHO"].ToString();
                                if (dr["Area_SIGHO"] != DBNull.Value) item.Area_SIGHO = dr["Area_SIGHO"].ToString();
                                if (dr["Ubicacion_SIGHO"] != DBNull.Value) item.Ubicacion_SIGHO = dr["Ubicacion_SIGHO"].ToString();
                                if (dr["Jefe_SIGHO"] != DBNull.Value) item.Jefe_SIGHO = dr["Jefe_SIGHO"].ToString();
                                if (dr["Departamento_SIGHO"] != DBNull.Value) item.Departamento_SIGHO = dr["Departamento_SIGHO"].ToString();
                                if (dr["Comisiona"] != DBNull.Value) item.Comisiona = dr["Comisiona"].ToString();
                                if (dr["Observacion"] != DBNull.Value) item.Observacion = dr["Observacion"].ToString();
                                item.Cargo_Reportado = dr["Cargo_Reportado"] != DBNull.Value ? dr["Cargo_Reportado"].ToString() : null;
                                item.Jefe_Reportado = dr["Jefe_Reportado"] != DBNull.Value ? dr["Jefe_Reportado"].ToString() : null;
                                item.Ubicacion_Reportada = dr["Ubicacion_Reportada"] != DBNull.Value ? dr["Ubicacion_Reportada"].ToString() : null;
                                item.Departamento_Reportado = dr["Departamento_Reportado"] != DBNull.Value ? dr["Departamento_Reportado"].ToString() : null;
                                if (dr["AplicaEmpleado"] != DBNull.Value) item.AplicaEmpleado = dr["AplicaEmpleado"].ToString();
                                if (dr["AplicaComision"] != DBNull.Value) item.AplicaComision = dr["AplicaComision"].ToString();
                                if (dr["EsPrueba_SIGHO"] != DBNull.Value) item.EsPrueba_SIGHO = dr["EsPrueba_SIGHO"].ToString();
                                if (dr["JustMotivo"] != DBNull.Value) item.JustMotivo = dr["JustMotivo"].ToString();
                                if (dr["JustReposicion"] != DBNull.Value) item.JustReposicion = dr["JustReposicion"].ToString();
                                if (dr["JustTiempo"] != DBNull.Value) item.JustTiempo = dr["JustTiempo"].ToString();
                                if (dr["HasEvidencia"] != DBNull.Value) item.HasEvidencia = Convert.ToBoolean(dr["HasEvidencia"]);
                                if (dr["EsDiscrepancia"] != DBNull.Value) item.EsDiscrepancia = Convert.ToInt32(dr["EsDiscrepancia"]);
                                detalles.Add(item);
                            }

                            // ResultSet 2: Cargos
                            if (dr.NextResult())
                                while (dr.Read()) { if (dr[0] != DBNull.Value) cargosCat.Add(dr[0].ToString()); }

                            // ResultSet 3: Ubicaciones
                            if (dr.NextResult())
                                while (dr.Read()) { if (dr[0] != DBNull.Value) ubisCat.Add(dr[0].ToString()); }

                            // ResultSet 4: Jefes
                            if (dr.NextResult())
                                while (dr.Read()) { if (dr[0] != DBNull.Value) jefesCat.Add(dr[0].ToString()); }

                            // ResultSet 5: Departamentos
                            if (dr.NextResult())
                                while (dr.Read()) { if (dr[0] != DBNull.Value) deptosCat.Add(dr[0].ToString()); }
                        }
                    }

                    var config = new {
                        PermitirComisiones = (tipo == 1),
                        MensajeAutoValidar = (tipo == 2) ? "Confirmar Certificacion Masiva? Los datos de plaza oficiales de SIGHO se usaran como base." : "Esta accion llenara todos los campos con la informacion actual de puestos y sedes de SIGHO1. Desea continuar?",
                        RequireConfirmacionReforzada = (tipo == 2)
                    };

                    var jsonRes = Json(new { success = true, periodo = pActivo, plantilla = plantilla, detalles = detalles, cargos = cargosCat, ubicaciones = ubisCat, jefes = jefesCat, departamentos = deptosCat, configUI = config }, JsonRequestBehavior.AllowGet);
                    jsonRes.MaxJsonLength = int.MaxValue;
                    return jsonRes;
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GuardarBorrador(List<DetallePlantillaViewModel> filas)
        {
            if (filas == null || !filas.Any()) return Json(new { success = true, message = "No hay cambios." });

            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    Entities.Employees employees = (Entities.Employees)Session["User"];
                    using (var tx = db.BeginTransaction())
                    {
                        foreach (var f in filas)
                        {
                            db.Execute("SP_Comp_Plantilla",
                                new { 
                                    Op = "GuardarDetalle", 
                                    DetalleID = f.DetalleID, 
                                    Cargo_Reportado = f.Cargo_Reportado, 
                                    Jefe_Reportado = f.Jefe_Reportado, 
                                    Ubicacion_Reportada = f.Ubicacion_Reportada, 
                                    Departamento_Reportado = f.Departamento_Reportado,
                                    Comisiona = f.Comisiona, 
                                    Observacion = f.Observacion, 
                                    UsuarioModifica = employees.EmployeeNumber,
                                    AplicaEmpleado = f.AplicaEmpleado,
                                    AplicaComision = f.AplicaComision,
                                    JustMotivo = f.JustMotivo,
                                    JustReposicion = f.JustReposicion,
                                    JustTiempo = f.JustTiempo
                                },
                                tx, commandType: CommandType.StoredProcedure);
                        }
                        tx.Commit();
                        return Json(new { success = true, message = "Borrador guardado exitosamente." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AgregarCargoRapido(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) 
                return Json(new { success = false, message = "El nombre del puesto no puede estar vacío." });

            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var existe = db.QueryFirstOrDefault<int?>(
                        "SELECT 1 FROM dbo.Comp_CatalogoCargo WHERE LTRIM(RTRIM(UPPER(NombreCargo))) = LTRIM(RTRIM(UPPER(@nombre)))", 
                        new { nombre });

                    if (existe.HasValue)
                        return Json(new { success = false, message = "El puesto ya existe en el catálogo." });

                    db.Execute("INSERT INTO dbo.Comp_CatalogoCargo (NombreCargo) VALUES (@nombre)", new { nombre = nombre.Trim().ToUpper() });
                    return Json(new { success = true, message = "Puesto agregado exitosamente.", nombre = nombre.Trim().ToUpper() });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AgregarDepartamentoRapido(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) 
                return Json(new { success = false, message = "El nombre del departamento no puede estar vacío." });

            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var existeSigho = db.QueryFirstOrDefault<int?>(
                        "SELECT 1 FROM SIGHO1.dbo.EMP2024 WHERE LTRIM(RTRIM(UPPER(primernivel))) = LTRIM(RTRIM(UPPER(@nombre))) AND fechabaja IS NULL",
                        new { nombre });

                    var existeManual = db.QueryFirstOrDefault<int?>(
                        "SELECT 1 FROM dbo.Comp_CatalogoDepartamento WHERE LTRIM(RTRIM(UPPER(NombreDepartamento))) = LTRIM(RTRIM(UPPER(@nombre)))",
                        new { nombre });

                    if (existeSigho.HasValue || existeManual.HasValue)
                        return Json(new { success = false, message = "El departamento ya existe en el catálogo." });

                    db.Execute("INSERT INTO dbo.Comp_CatalogoDepartamento (NombreDepartamento) VALUES (@nombre)", new { nombre = nombre.Trim().ToUpper() });
                    return Json(new { success = true, message = "Departamento agregado exitosamente.", nombre = nombre.Trim().ToUpper() });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SincronizarMaestro(int periodoId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("SP_Comp_Plantilla", new { Op = "SincronizarMaestro", PeriodoID = periodoId }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true, message = "Universo sincronizado correctamente desde SIGHO." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> EnviarPlantilla(int plantillaId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // 1. Obtener informacion previa al envio para el correo
                    var info = db.QueryFirstOrDefault<dynamic>(@"
                        SELECT p.CarnetGerente, e.nombre_completo as NombreGerente, p.OGERENCIA as Gerencia, pr.NombrePeriodo, p.TipoPlantillaID
                        FROM dbo.Comp_Plantilla p
                        INNER JOIN SIGHO1.dbo.EMP2024 e ON p.CarnetGerente = e.CARNET
                        INNER JOIN dbo.Comp_Periodo pr ON p.PeriodoID = pr.PeriodoID
                        WHERE p.PlantillaID = @plantillaId", new { plantillaId });

                    // 2. Ejecutar el proceso en base de datos (Cambio de estado)
                    db.Execute("SP_Comp_Plantilla", new { Op = "EnviarPlantilla", PlantillaID = plantillaId }, commandType: CommandType.StoredProcedure);

                    // 3. Despachar correo de confirmacion
                    if (info != null)
                    {
                        string tipoStr = (int)info.TipoPlantillaID == 1 ? "Comisiones" : "Certificacion Organizacional";
                        string subject = $"[RHOnline] Plantilla Finalizada: {info.NombreGerente} - {info.Gerencia} ({tipoStr})";

                        string body = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
                                <div style='max-width: 600px; margin: 20px auto; border: 1px solid #ddd; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.1);'>
                                    <div style='background-color: #B71C1C; color: white; padding: 20px; text-align: center;'>
                                        <h2 style='margin: 0;'>Confirmacion de Envio</h2>
                                        <p style='margin: 5px 0 0;'>Modulo de Compensacion</p>
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

                        // Enviar a RRHH y copia al validador (puedes ajustar el correo de destino)
                        await RegistrarYDespacharCorreo(db, "compensaciones@claro.com.ni", subject, body);
                    }

                    return Json(new { success = true, message = "Certificacion enviada con exito." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerTodasPlantillas(int periodoId, int tipoPlantilla)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query<PlantillaViewModel>("SP_Comp_Plantilla", new { Op = "ObtenerTodasPlantillas", PeriodoID = periodoId, TipoPlantillaID = tipoPlantilla }, commandType: CommandType.StoredProcedure).ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerDetallePlantilla(int plantillaId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var details = db.Query<DetallePlantillaViewModel>("SP_Comp_Plantilla", new { Op = "ObtenerDetalle", PlantillaID = plantillaId }, commandType: CommandType.StoredProcedure).ToList();
                    return Json(new { success = true, data = details }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult AprobarPlantilla(int plantillaId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    // ESTADO 'Aprobado' sirve como cierre por plantilla para RRHH
                    db.Execute("SP_Comp_Plantilla", new { Op = "AprobarPlantilla", PlantillaID = plantillaId }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true, message = "Plantilla aprobada y CERRADA para el validador." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> DevolverPlantilla(int plantillaId, string comentario)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("SP_Comp_Plantilla", new { Op = "DevolverPlantilla", PlantillaID = plantillaId, ComentarioDevolucion = comentario }, commandType: CommandType.StoredProcedure);
                    
                    // Notificar por correo al validador usando la API
                    var plantilla = db.QueryFirstOrDefault<PlantillaViewModel>("SELECT CarnetGerente, TipoPlantillaID FROM dbo.Comp_Plantilla WHERE PlantillaID = @id", new { id = plantillaId });
                    if (plantilla != null)
                    {
                        var validador = db.QueryFirstOrDefault("SELECT correo, nombre + ' ' + apellido1 as Nombre FROM SIGHO1.dbo.EMP2024 WHERE carnet = @carnet", new { carnet = plantilla.CarnetGerente });
                        if (validador != null && !string.IsNullOrEmpty(validador.correo))
                        {
                            string nombreProceso = plantilla.TipoPlantillaID == 1 ? "Comisiones" : "Certificacion de Personal";
                             string mensajeHtml = $@"
                                 <html><body>
                                 <h2 style='color:#d9534f;'>RH Online - Revisión de {nombreProceso}</h2>
                                 <p>Buenas tardes,</p>
                                 <p>Solicitando de su apoyo con completar en las <b>observaciones</b>, por diferencias encontradas al validar con sistema (Jefe, Ubicación, Edificio):</p>
                                 <blockquote style='border-left: 4px solid #d9534f; padding-left: 10px; color:#555;'>
                                     <b>Comentario de RRHH:</b> {comentario}<br><br>
                                     <b>Por favor responder en la plataforma:</b><br>
                                     - ¿Porque se realiza el cambio?<br>
                                     - ¿A quien va a reponer?<br>
                                     - ¿Por cuánto Tiempo? (Indicar meses / indefinido)
                                 </blockquote>
                                 <p>Atento para proceder.</p>
                                 <p><a href='http://10.200.5.24:81/Compensacion/Plantilla' style='display:inline-block; padding:10px 20px; background-color:#B71C1C; color:white; text-decoration:none; border-radius:5px;'>Ir a la Plantilla</a></p>
                                 </body></html>";

                            // MODO PRUEBA: Redirigido a Gustavo Lira
                            string correo = validador.correo;
                             await RegistrarYDespacharCorreo(db, correo, $"RH Online - Revisión de {nombreProceso}", mensajeHtml);
                        }
                    }

                    return Json(new { success = true, message = "Plantilla devuelta y validador notificado por correo." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> NotificarJefe(int periodoId, string carnetJefe, int tipoPlantilla)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var periodo = db.QueryFirstOrDefault<PeriodoViewModel>("SP_Comp_Plantilla", new { Op = "PeriodoByID", PeriodoID = periodoId }, commandType: CommandType.StoredProcedure);
                    var plantilla = db.QueryFirstOrDefault<PlantillaViewModel>("SP_Comp_Plantilla", new { Op = "IniciarPlantilla", PeriodoID = periodoId, CarnetGerente = carnetJefe, TipoPlantillaID = tipoPlantilla }, commandType: CommandType.StoredProcedure);
                    var detalles = db.Query<DetallePlantillaViewModel>("SP_Comp_Plantilla", new { Op = "ObtenerDetalle", PlantillaID = plantilla.PlantillaID }, commandType: CommandType.StoredProcedure).ToList();

                    // Buscar correo del jefe
                    var datosJefe = db.QueryFirstOrDefault("SELECT correo, nombre + ' ' + apellido1 as Nombre FROM SIGHO1.dbo.EMP2024 WHERE carnet = @carnet", new { carnet = carnetJefe });
                    if (datosJefe == null) return Json(new { success = false, message = "No se encontro correo para el carnet del jefe." });

                    // 1. Cabecera Premium
                    string htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 800px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                            <div style='background-color: #B71C1C; color: white; padding: 15px; text-align: center;'>
                                <h1 style='margin: 0;'>Certificacion de Plantilla RRHH</h1>
                                <p style='margin: 5px 0 0;'>Periodo: {periodo.NombrePeriodo}</p>
                            </div>
                            <div style='padding: 20px;'>
                                <p>Estimado(a) <strong>{datosJefe.Nombre}</strong>,</p>
                                <p>Se solicita su validacion periodica del personal a su cargo. A continuacion, un resumen de los colaboradores de su gerencia que requieren certificacion:</p>
                                
                                <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                                    <thead>
                                        <tr style='background-color: #f2f2f2; border-bottom: 2px solid #B71C1C;'>
                                            <th style='padding: 10px; text-align: left;'>Empleado</th>
                                            <th style='padding: 10px; text-align: left;'>Cargo Actual</th>
                                            <th style='padding: 10px; text-align: right;'>Comisiona</th>
                                        </tr>
                                    </thead>
                                    <tbody>";

                    // 2. Filas de empleados
                    foreach (var d in detalles)
                    {
                        htmlBody += $@"
                            <tr style='border-bottom: 1px solid #eee;'>
                                <td style='padding: 10px;'>{d.NombreCompleto}<br/><small style='color: #666;'>{d.CarnetEmpleado}</small></td>
                                <td style='padding: 10px;'>{d.Cargo_SIGHO}</td>
                                <td style='padding: 10px; text-align: right;'>{(d.Comisiona == "S" ? "SI" : "NO")}</td>
                            </tr>";
                    }

                    // 3. Cierre y Link
                    htmlBody += $@"
                                    </tbody>
                                </table>
                                <div style='margin-top: 30px; text-align: center;'>
                                    <p>Por favor, ingrese al portal RH Online para completar su reporte formal:</p>
                                    <a href='http://localhost:60992/Compensacion/Plantilla?tipo={tipoPlantilla}' 
                                       style='background-color: #B71C1C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                       Acceder a RH Online
                                    </a>
                                </div>
                                <p style='margin-top: 40px; font-size: 12px; color: #777;'>
                                    Este es un mensaje automatico generado por el sistema de Compensacion de RRHH.
                                </p>
                            </div>
                        </div>";

                    // 5. Preparar datos de prueba y despachar (Refactorizado)
                    string correo = datosJefe.correo;
                     string subject = $"RH Online: {(tipoPlantilla == 1 ? "Comisiones" : "Certificacion Personal")} - {periodo.NombrePeriodo} (Original: {datosJefe.correo})";

                    bool enviado = await RegistrarYDespacharCorreo(db, correo, subject, htmlBody);
                    
                    if (enviado)
                    {
                        return Json(new { success = true, message = $"Notificacion preparada y enviada correctamente (Modo Prueba: {correo})." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Se registro la notificacion pero hubo un problema al contactar al servidor de despacho." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> NotificarMasivo(int periodoId, int tipoPlantilla)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var pendientes = db.Query<PlantillaViewModel>("SELECT DISTINCT CarnetGerente FROM dbo.Comp_Plantilla WHERE PeriodoID = @periodoId AND TipoPlantillaID = @tipoPlantilla AND (Estado = 'Borrador' OR Estado = 'Devuelto')", new { periodoId, tipoPlantilla }).ToList();

                    int count = 0;
                    foreach (var p in pendientes)
                    {
                        await NotificarJefe(periodoId, p.CarnetGerente, tipoPlantilla);
                        count++;
                    }

                    return Json(new { success = true, message = $"Se han disparado {count} notificaciones a validadores pendientes." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerExclusiones()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var list = db.Query("SELECT * FROM dbo.Comp_Com_Exclusion").ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult AgregarExclusion(string tipo, string valor, string motivo)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("INSERT INTO dbo.Comp_Com_Exclusion (TipoExclusion, ValorExclusion, Motivo) VALUES (@tipo, @valor, @motivo)", new { tipo, valor, motivo });
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult QuitarExclusion(int exclusionId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("DELETE FROM dbo.Comp_Com_Exclusion WHERE ExclusionID = @exclusionId", new { exclusionId });
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerConfigValidadores()
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var data = db.Query<ConfigValidadorViewModel>("EXEC SP_Comp_Plantilla @Op='ObtenerConfigValidadores'").ToList();
                    return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerPersonalValidador(string carnetValidador)
        {
            try
            {
                if (string.IsNullOrEmpty(carnetValidador))
                    return Json(new { success = false, message = "Carnet de validador requerido." }, JsonRequestBehavior.AllowGet);

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // Intentar primero desde la plantilla activa (datos reales del periodo actual)
                    string sqlPlantilla = @"
                        SELECT DISTINCT
                            pd.CarnetEmpleado,
                            pd.NombreCompleto,
                            pd.OGERENCIA_SIGHO AS Gerencia,
                            pd.OSUBGERENCIA_SIGHO AS Subgerencia,
                            pd.Departamento_SIGHO AS Departamento,
                            pd.Cargo_SIGHO AS Cargo,
                            pd.Ubicacion_SIGHO AS Ubicacion,
                            pd.Comisiona
                        FROM Comp_PlantillaDetalle pd
                        INNER JOIN Comp_Plantilla p ON pd.PlantillaID = p.PlantillaID
                        INNER JOIN Comp_Periodo pe ON p.PeriodoID = pe.PeriodoID AND pe.Estado = 'Activo'
                        WHERE p.CarnetValidador = @carnet
                        ORDER BY pd.NombreCompleto";

                    var dataPlantilla = db.Query(sqlPlantilla, new { carnet = carnetValidador.Trim() })
                                          .Select(x => new {
                                              CarnetEmpleado = (string)x.CarnetEmpleado,
                                              NombreCompleto = (string)x.NombreCompleto,
                                              Gerencia = (string)x.Gerencia,
                                              Subgerencia = (string)x.Subgerencia,
                                              Departamento = (string)x.Departamento,
                                              Cargo = (string)x.Cargo,
                                              Ubicacion = (string)x.Ubicacion
                                          }).ToList();

                    if (dataPlantilla.Count > 0)
                    {
                        return Json(new { success = true, data = dataPlantilla, fuente = "Plantilla Activa" }, JsonRequestBehavior.AllowGet);
                    }

                    // Fallback: Buscar desde la configuracion + SIGHO1
                    string sqlConfig = @"
                        SELECT DISTINCT
                            e.CARNET AS CarnetEmpleado,
                            e.nombre_completo AS NombreCompleto,
                            e.OGERENCIA AS Gerencia,
                            e.oSUBGERENCIA AS Subgerencia,
                            e.Departamento AS Departamento,
                            e.cargo AS Cargo,
                            e.Nombreubicacion AS Ubicacion,
                            'S' AS Comisiona
                        FROM Comp_Com_ConfiguracionValidador cv
                        INNER JOIN SIGHO1.dbo.EMP2024 e WITH (NOLOCK) ON (
                            (cv.CarnetEmpleado IS NOT NULL AND cv.CarnetEmpleado <> '' AND cv.CarnetEmpleado = e.CARNET)
                            OR
                            (cv.CarnetEmpleado IS NULL OR cv.CarnetEmpleado = '') AND (
                                (cv.Area IS NOT NULL AND cv.Area <> '' AND e.Departamento = cv.Area AND e.OGERENCIA = cv.Gerencia)
                                OR
                                (cv.Area IS NULL OR cv.Area = '') AND cv.Subgerencia IS NOT NULL AND cv.Subgerencia <> '' AND e.oSUBGERENCIA = cv.Subgerencia AND e.OGERENCIA = cv.Gerencia
                                OR
                                (cv.Area IS NULL OR cv.Area = '') AND (cv.Subgerencia IS NULL OR cv.Subgerencia = '') AND e.OGERENCIA = cv.Gerencia
                            )
                        )
                        WHERE cv.CarnetValidador = @carnet
                          AND e.fechabaja IS NULL
                        ORDER BY e.nombre_completo";

                    var dataConfig = db.Query(sqlConfig, new { carnet = carnetValidador.Trim() })
                                       .Select(x => new {
                                           CarnetEmpleado = (string)x.CarnetEmpleado,
                                           NombreCompleto = (string)x.NombreCompleto,
                                           Gerencia = (string)x.Gerencia,
                                           Subgerencia = (string)x.Subgerencia,
                                           Departamento = (string)x.Departamento,
                                           Cargo = (string)x.Cargo,
                                           Ubicacion = (string)x.Ubicacion
                                       }).ToList();

                    return Json(new { success = true, data = dataConfig, fuente = "Configuracion SIGHO" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GuardarConfigValidador(string gerencia, string subgerencia, string area, string carnetValidador, string carnetEmpleado)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // Resolver carnet si se ingresó correo
                    string sqlResolve = @"SELECT TOP 1 CARNET FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK)
                        WHERE fechabaja IS NULL AND (CARNET = @filtro OR correo = @filtro OR correo LIKE @filtro + '@%')";
                   

                    var infoV = db.QueryFirstOrDefault(sqlResolve, new { filtro = carnetValidador.Trim() });
                    if (infoV == null) return Json(new { success = false, message = "El validador no fue encontrado en SIGHO1." });

                    string carnetReal = infoV.CARNET;
                    string nombre = infoV.nombre_completo;

                    db.Execute(@"INSERT INTO dbo.Comp_Com_ConfiguracionValidador 
                        (Gerencia, Subgerencia, Area, CarnetValidador, NombreValidador, CarnetEmpleado, FechaCreacion) 
                        VALUES (@gerencia, @subgerencia, @area, @carnetValidador, @nombre, @carnetEmpleado, GETDATE())",
                        new { gerencia, subgerencia = string.IsNullOrEmpty(subgerencia) ? (string)null : subgerencia, 
                              area = string.IsNullOrEmpty(area) ? (string)null : area, 
                              carnetValidador = carnetReal, nombre, 
                              carnetEmpleado = string.IsNullOrEmpty(carnetEmpleado) ? (string)null : carnetEmpleado });

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarEstructuraManual(string gerencia, string subgerencia, string area)
        {
            try
            {
                if (string.IsNullOrEmpty(gerencia))
                    return Json(new { success = false, message = "La gerencia es requerida." });

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // Verificar si ya existe en SIGHO
                    bool existeSigho = false;
                    if (!string.IsNullOrEmpty(area))
                    {
                        var res = db.QueryFirstOrDefault<string>(
                            @"SELECT TOP 1 primernivel FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) 
                              WHERE OGERENCIA = @gerencia AND OSUBGERENCIA = @subgerencia AND primernivel = @area AND fechabaja IS NULL",
                            new { gerencia, subgerencia, area }
                        );
                        if (res != null) existeSigho = true;
                    }
                    else if (!string.IsNullOrEmpty(subgerencia))
                    {
                        var res = db.QueryFirstOrDefault<string>(
                            @"SELECT TOP 1 OSUBGERENCIA FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) 
                              WHERE OGERENCIA = @gerencia AND OSUBGERENCIA = @subgerencia AND fechabaja IS NULL",
                            new { gerencia, subgerencia }
                        );
                        if (res != null) existeSigho = true;
                    }

                    if (existeSigho)
                        return Json(new { success = true, message = "La estructura ya existe en SIGHO." });

                    // Verificar si ya existe en la tabla de manuales
                    bool existeManual = false;
                    if (!string.IsNullOrEmpty(area))
                    {
                        var res = db.QueryFirstOrDefault<int?>(
                            @"SELECT 1 FROM dbo.Comp_Com_EstructurasManuales WITH (NOLOCK) 
                              WHERE Gerencia = @gerencia AND Subgerencia = @subgerencia AND Area = @area",
                            new { gerencia, subgerencia, area }
                        );
                        if (res != null) existeManual = true;
                    }
                    else if (!string.IsNullOrEmpty(subgerencia))
                    {
                        var res = db.QueryFirstOrDefault<int?>(
                            @"SELECT 1 FROM dbo.Comp_Com_EstructurasManuales WITH (NOLOCK) 
                              WHERE Gerencia = @gerencia AND Subgerencia = @subgerencia AND Area IS NULL",
                            new { gerencia, subgerencia }
                        );
                        if (res != null) existeManual = true;
                    }

                    if (existeManual)
                        return Json(new { success = true, message = "La estructura ya esta registrada." });

                    // Insertar
                    db.Execute(@"INSERT INTO dbo.Comp_Com_EstructurasManuales (Gerencia, Subgerencia, Area, FechaCreacion, UsuarioCreacion) 
                                 VALUES (@gerencia, @subgerencia, @area, GETDATE(), @usuario)",
                                 new { 
                                     gerencia, 
                                     subgerencia = string.IsNullOrEmpty(subgerencia) ? (string)null : subgerencia, 
                                     area = string.IsNullOrEmpty(area) ? (string)null : area, 
                                     usuario = User.Identity.Name ?? "SISTEMA" 
                                 });

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult GuardarConfigMasiva(string carnetValidador, string listaColaboradores)
        {
            try
            {
                if (string.IsNullOrEmpty(carnetValidador) || string.IsNullOrEmpty(listaColaboradores))
                    return Json(new { success = false, message = "Datos incompletos" });

                var carnets = listaColaboradores.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(c => c.Trim())
                                                .Where(c => !string.IsNullOrEmpty(c))
                                                .Distinct()
                                                .ToList();

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // Resolver validador
                    string sqlResolve = @"SELECT TOP 1 CARNET, nombre_completo FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK)
                        WHERE fechabaja IS NULL AND (CARNET = @filtro OR correo = @filtro OR correo LIKE @filtro + '@%')";
                    var infoV = db.QueryFirstOrDefault(sqlResolve, new { filtro = carnetValidador.Trim() });
                    if (infoV == null) return Json(new { success = false, message = "El validador no fue encontrado." });

                    string carnetRealV = infoV.CARNET;
                    string nombreV = infoV.nombre_completo;

                    int procesados = 0;
                    foreach (var carnetEmp in carnets)
                    {
                        // Resolver jerarquia del colaborador automaticamente
                        var infoE = db.QueryFirstOrDefault("SELECT OGERENCIA, oSUBGERENCIA, primernivel FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK) WHERE CARNET = @c", new { c = carnetEmp });
                        
                        db.Execute(@"INSERT INTO dbo.Comp_Com_ConfiguracionValidador 
                            (Gerencia, Subgerencia, Area, CarnetValidador, NombreValidador, CarnetEmpleado, FechaCreacion) 
                            VALUES (@gerencia, @subgerencia, @area, @carnetValidador, @nombre, @carnetEmpleado, GETDATE())",
                            new { 
                                gerencia = infoE?.OGERENCIA, 
                                subgerencia = infoE?.oSUBGERENCIA, 
                                area = infoE?.primernivel, 
                                carnetValidador = carnetRealV, 
                                nombre = nombreV, 
                                carnetEmpleado = carnetEmp 
                            });
                        procesados++;
                    }

                    return Json(new { success = true, message = "Carga exitosa. Se procesaron " + procesados + " colaboradores." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarConfigValidador(int? configId, string gerencia, string carnetValidador, string subgerencia, string area)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    string sql = "";
                    if (configId.HasValue && configId.Value > 0)
                    {
                        sql = "DELETE FROM dbo.Comp_Com_ConfiguracionValidador WHERE ConfigID = @configId";
                    }
                    else
                    {
                        sql = @"DELETE FROM dbo.Comp_Com_ConfiguracionValidador 
                                WHERE Gerencia = @gerencia AND CarnetValidador = @carnetValidador
                                AND (Subgerencia = @subgerencia OR (Subgerencia IS NULL AND @subgerencia IS NULL))
                                AND (Area = @area OR (Area IS NULL AND @area IS NULL))";
                    }

                    db.Execute(sql, new { configId, gerencia, carnetValidador, subgerencia, area });
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarPeriodo(int periodoId, string fechaInicio, string fechaFin, string estado, int tipo = 1)
        {
            try
            {
                // Parsear fechas para evitar errores de region/formato en SQL
                DateTime? dtInicio = string.IsNullOrEmpty(fechaInicio) ? (DateTime?)null : DateTime.Parse(fechaInicio);
                DateTime? dtFin = string.IsNullOrEmpty(fechaFin) ? (DateTime?)null : DateTime.Parse(fechaFin);

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("EXEC SP_Comp_Plantilla @Op='GuardarPeriodo', @PeriodoID=@periodoId, @FechaInicio=@inicio, @FechaFin=@fin, @Estado=@estado, @TipoPlantillaID=@tipo", 
                        new { periodoId, inicio = dtInicio, fin = dtFin, estado, tipo });
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = "Error al guardar periodo: " + ex.Message }); }
        }

        [HttpPost]
        public JsonResult ReabrirPeriodo(int periodoId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("EXEC SP_Comp_Plantilla @Op='ReabrirPeriodo', @PeriodoID=@periodoId", new { periodoId });
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
                    db.Execute("EXEC SP_Comp_Plantilla @Op='ReabrirPlantilla', @PlantillaID=@plantillaId", new { plantillaId });
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult ObtenerListaPeriodos(int tipo = 1)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    var list = db.Query<PeriodoViewModel>("SELECT PeriodoID, NombrePeriodo, Estado, FechaInicio, FechaFin FROM dbo.Comp_Periodo WHERE TipoPlantillaID = @tipo ORDER BY PeriodoID DESC", new { tipo }).ToList();
                    return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult ObtenerDetallePeriodo(int id)
        {
            try {
                using (var db = new SqlConnection(CadenaConexion)) {
                    var p = db.QueryFirstOrDefault<PeriodoViewModel>("SELECT * FROM dbo.Comp_Periodo WHERE PeriodoID = @id", new { id });
                    return Json(new { success = true, data = p }, JsonRequestBehavior.AllowGet);
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult ObtenerTodosDetallesRevision(int periodoId, int tipo, string carnetValidador = null)
        {
            try
            {
                var data = new List<DetallePlantillaViewModel>();
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    Utils.LogInfo("Consultando ObtenerTodosDetallesRevision SQL DIRECTO - Periodo: " + periodoId + ", Tipo: " + tipo + ", Validador: " + carnetValidador);

                    // Construimos el SQL para traer detalles de plantillas existentes O de validadores asignados
                    string sql = @"
                        -- 1. Empleados en plantillas ya iniciadas
                        SELECT 
                            d.DetalleID, d.PlantillaID, d.CarnetEmpleado, d.NombreCompleto, 
                            ISNULL(d.Cargo_SIGHO, '-') as Cargo_SIGHO, d.OGERENCIA_SIGHO, d.OSUBGERENCIA_SIGHO, d.Area_SIGHO, 
                            ISNULL(d.Ubicacion_SIGHO, '-') as Ubicacion_SIGHO, ISNULL(d.Jefe_SIGHO, '-') as Jefe_SIGHO, d.Departamento_SIGHO, 
                            d.Comisiona, d.Observacion, p.Estado, p.CarnetGerente, p.NombreGerente,
                            CAST(CASE WHEN (d.Cargo_Reportado IS NOT NULL AND d.Cargo_Reportado <> d.Cargo_SIGHO) 
                                      OR (d.Jefe_Reportado IS NOT NULL AND d.Jefe_Reportado <> d.Jefe_SIGHO) 
                                      OR (d.Comisiona = 'N') THEN 1 ELSE 0 END AS INT) as EsDiscrepancia,
                            CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = d.DetalleID) THEN 1 ELSE 0 END AS BIT) as HasEvidencia,
                            d.Cargo_Reportado, d.Jefe_Reportado, d.Ubicacion_Reportada, d.Departamento_Reportado
                        FROM dbo.Comp_PlantillaDetalle d
                        JOIN dbo.Comp_Plantilla p ON d.PlantillaID = p.PlantillaID
                        WHERE p.PeriodoID = @pid AND p.TipoPlantillaID = @tipo
                        " + (string.IsNullOrEmpty(carnetValidador) ? "" : " AND p.CarnetGerente = @carnet ") + @"

                        UNION ALL

                        -- 2. Empleados de validadores configurados que aun no han iniciado su plantilla
                        SELECT 
                            0 as DetalleID, 0 as PlantillaID, j.CarnetEmpleado, j.NombreEmpleado as NombreCompleto,
                            ISNULL(e.cargo, '-') as Cargo_SIGHO, j.GerenciaMaestra as OGERENCIA_SIGHO, '' as OSUBGERENCIA_SIGHO, 
                            j.AreaMaestra as Area_SIGHO, 
                            ISNULL(j.UbicacionMaestra, '-') as Ubicacion_SIGHO, ISNULL(j.NombreJefeMaestro, '-') as Jefe_SIGHO, 
                            j.AreaMaestra as Departamento_SIGHO, 
                            'S' as Comisiona, '' as Observacion, 'Pendiente' as Estado, cv.CarnetValidador as CarnetGerente, ISNULL(ev.nombre_completo, cv.CarnetValidador) as NombreGerente,
                            0 as EsDiscrepancia, CAST(0 AS BIT) as HasEvidencia,
                            NULL as Cargo_Reportado, NULL as Jefe_Reportado, NULL as Ubicacion_Reportada, NULL as Departamento_Reportado
                        FROM dbo.Comp_Com_ConfiguracionValidador cv
                        JOIN dbo.Comp_Com_JerarquiaMaestra j ON (j.GerenciaMaestra = cv.Gerencia OR j.GerenciaMaestra = REPLACE(cv.Gerencia, 'NI ', ''))
                        LEFT JOIN SIGHO1.dbo.EMP2024 e ON j.CarnetEmpleado = e.CARNET
                        LEFT JOIN SIGHO1.dbo.EMP2024 ev ON ev.CARNET = cv.CarnetValidador
                        LEFT JOIN dbo.Comp_Plantilla p ON p.CarnetGerente = cv.CarnetValidador AND p.PeriodoID = @pid AND p.TipoPlantillaID = @tipo
                        WHERE p.PlantillaID IS NULL AND j.Activo = 1
                        " + (string.IsNullOrEmpty(carnetValidador) ? "" : " AND cv.CarnetValidador = @carnet ") + @"
                        AND (cv.Subgerencia IS NULL OR cv.Subgerencia = e.oSUBGERENCIA)
                        AND (cv.Area IS NULL OR cv.Area = j.AreaMaestra)
                        AND (cv.CarnetEmpleado IS NULL OR cv.CarnetEmpleado = j.CarnetEmpleado)
                        ";

                    using (var cmd = new SqlCommand(sql, db))
                    {
                        cmd.Parameters.AddWithValue("@pid", periodoId);
                        cmd.Parameters.AddWithValue("@tipo", tipo);
                        if (!string.IsNullOrEmpty(carnetValidador)) cmd.Parameters.AddWithValue("@carnet", carnetValidador);
                        cmd.CommandTimeout = 180;

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                var item = new DetallePlantillaViewModel();
                                if (dr["DetalleID"] != DBNull.Value) item.DetalleID = Convert.ToInt32(dr["DetalleID"]);
                                if (dr["PlantillaID"] != DBNull.Value) item.PlantillaID = Convert.ToInt32(dr["PlantillaID"]);
                                if (dr["CarnetEmpleado"] != DBNull.Value) item.CarnetEmpleado = dr["CarnetEmpleado"].ToString();
                                if (dr["NombreCompleto"] != DBNull.Value) item.NombreCompleto = dr["NombreCompleto"].ToString();
                                if (dr["Cargo_SIGHO"] != DBNull.Value) item.Cargo_SIGHO = dr["Cargo_SIGHO"].ToString();
                                if (dr["OGERENCIA_SIGHO"] != DBNull.Value) item.OGERENCIA_SIGHO = dr["OGERENCIA_SIGHO"].ToString();
                                if (dr["OSUBGERENCIA_SIGHO"] != DBNull.Value) item.OSUBGERENCIA_SIGHO = dr["OSUBGERENCIA_SIGHO"].ToString();
                                if (dr["Area_SIGHO"] != DBNull.Value) item.Area_SIGHO = dr["Area_SIGHO"].ToString();
                                if (dr["Ubicacion_SIGHO"] != DBNull.Value) item.Ubicacion_SIGHO = dr["Ubicacion_SIGHO"].ToString();
                                if (dr["Jefe_SIGHO"] != DBNull.Value) item.Jefe_SIGHO = dr["Jefe_SIGHO"].ToString();
                                if (dr["Departamento_SIGHO"] != DBNull.Value) item.Departamento_SIGHO = dr["Departamento_SIGHO"].ToString();
                                if (dr["Comisiona"] != DBNull.Value) item.Comisiona = dr["Comisiona"].ToString();
                                if (dr["Observacion"] != DBNull.Value) item.Observacion = dr["Observacion"].ToString();
                                if (dr["Estado"] != DBNull.Value) item.Estado = dr["Estado"].ToString();
                                if (dr["CarnetGerente"] != DBNull.Value) item.CarnetGerente = dr["CarnetGerente"].ToString();
                                if (dr["NombreGerente"] != DBNull.Value) item.NombreGerente = dr["NombreGerente"].ToString();
                                if (dr["EsDiscrepancia"] != DBNull.Value) item.EsDiscrepancia = Convert.ToInt32(dr["EsDiscrepancia"]);
                                if (dr["HasEvidencia"] != DBNull.Value) item.HasEvidencia = Convert.ToBoolean(dr["HasEvidencia"]);

                                // Mapeo de reportados
                                item.Cargo_Reportado = dr["Cargo_Reportado"] != DBNull.Value ? dr["Cargo_Reportado"].ToString() : null;
                                item.Jefe_Reportado = dr["Jefe_Reportado"] != DBNull.Value ? dr["Jefe_Reportado"].ToString() : null;
                                item.Ubicacion_Reportada = dr["Ubicacion_Reportada"] != DBNull.Value ? dr["Ubicacion_Reportada"].ToString() : null;
                                item.Departamento_Reportado = dr["Departamento_Reportado"] != DBNull.Value ? dr["Departamento_Reportado"].ToString() : null;

                                data.Add(item);
                            }
                        }
                    }
                }
                var res = Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
                res.MaxJsonLength = int.MaxValue;
                return res;
            }
            catch (Exception ex)
            {
                Utils.LogError("Error en ObtenerTodosDetallesRevision SQL", ex);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerDashboardRevision(int periodoId, int tipo)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    if (periodoId <= 0) {
                        Utils.LogInfo("Intento de carga de Dashboard con PeriodoID <= 0");
                        return Json(new { success = false, message = "ID de Periodo no válido (0)." }, JsonRequestBehavior.AllowGet);
                    }
                    db.Open();
                    Utils.LogInfo("Cargando DashboardRevision - Periodo: " + periodoId + ", Tipo: " + tipo);
                    var data = new List<DashboardRevisionViewModel>();
                    string sql = @"
                        WITH Validadores AS (
                            SELECT cv.CarnetValidador as Carnet, 
                                   STUFF((SELECT DISTINCT ', ' + cv2.Gerencia 
                                          FROM dbo.Comp_Com_ConfiguracionValidador cv2 
                                          WHERE cv2.CarnetValidador = cv.CarnetValidador AND ISNULL(cv2.Activo, 1) = 1
                                          FOR XML PATH('')), 1, 2, '') as OGERENCIA
                            FROM dbo.Comp_Com_ConfiguracionValidador cv
                            WHERE ISNULL(cv.Activo, 1) = 1
                            GROUP BY cv.CarnetValidador
                        ),
                        ConteoUniverso AS (
                            SELECT cv.CarnetValidador, COUNT(DISTINCT e.CARNET) as Total
                            FROM dbo.Comp_Com_ConfiguracionValidador cv
                            JOIN SIGHO1.dbo.EMP2024 e ON 
                                (cv.CarnetEmpleado = e.CARNET) -- Asignacion directa
                                OR (cv.CarnetEmpleado IS NULL 
                                    AND (e.OGERENCIA = cv.Gerencia OR e.OGERENCIA = REPLACE(cv.Gerencia, 'NI ', ''))
                                    AND (cv.Subgerencia IS NULL OR e.oSUBGERENCIA = cv.Subgerencia)
                                    AND (cv.Area IS NULL OR e.primernivel = cv.Area))
                            WHERE e.fechabaja IS NULL AND ISNULL(cv.Activo, 1) = 1
                            GROUP BY cv.CarnetValidador
                        )
                        SELECT 
                            p.PlantillaID, 
                            v.Carnet as CarnetGerente, 
                            ISNULL(ev.nombre_completo, 'VAL- ' + v.Carnet) as NombreGerente, 
                            ISNULL(p.Estado, 'SIN INICIAR') as Estado,
                            v.OGERENCIA,
                            ISNULL(cu.Total, 0) as TotalColaboradores,
                            ISNULL((SELECT COUNT(*) FROM dbo.Comp_PlantillaDetalle WHERE PlantillaID = p.PlantillaID), 0) as TotalEnPlantilla,
                            ISNULL((SELECT COUNT(*) FROM dbo.Comp_PlantillaDetalle d WHERE d.PlantillaID = p.PlantillaID 
                                       AND (d.Cargo_Reportado IS NOT NULL OR d.Jefe_Reportado IS NOT NULL 
                                       OR d.Ubicacion_Reportada IS NOT NULL OR d.Departamento_Reportado IS NOT NULL
                                       OR d.Comisiona <> 'S' OR d.AplicaEmpleado = 'N')), 0) as TotalDiscrepancias
                        FROM Validadores v
                        LEFT JOIN SIGHO1.dbo.EMP2024 ev ON v.Carnet = ev.CARNET
                        LEFT JOIN ConteoUniverso cu ON v.Carnet = cu.CarnetValidador
                        LEFT JOIN dbo.Comp_Plantilla p ON p.CarnetGerente = v.Carnet AND p.PeriodoID = @pid AND p.TipoPlantillaID = @tipo
                        ORDER BY ISNULL(ev.nombre_completo, v.Carnet)";

                    using (var cmd = new SqlCommand(sql, db))
                    {
                        cmd.Parameters.AddWithValue("@pid", periodoId);
                        cmd.Parameters.AddWithValue("@tipo", tipo);

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                var item = new DashboardRevisionViewModel();
                                if (dr["PlantillaID"] != DBNull.Value) item.PlantillaID = Convert.ToInt32(dr["PlantillaID"]);
                                item.CarnetGerente = dr["CarnetGerente"].ToString();
                                item.NombreGerente = dr["NombreGerente"].ToString();
                                item.OGERENCIA = dr["OGERENCIA"].ToString();
                                item.Estado = dr["Estado"].ToString();
                                item.TotalColaboradores = dr["TotalColaboradores"] != DBNull.Value ? Convert.ToInt32(dr["TotalColaboradores"]) : 0;
                                item.TotalEnPlantilla = dr["TotalEnPlantilla"] != DBNull.Value ? Convert.ToInt32(dr["TotalEnPlantilla"]) : 0;
                                item.TotalDiscrepancias = dr["TotalDiscrepancias"] != DBNull.Value ? Convert.ToInt32(dr["TotalDiscrepancias"]) : 0;
                                data.Add(item);
                            }
                        }
                    }
                    return Json(new { success = true, data = data, debug_pid = periodoId, debug_tipo = tipo }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> NotificarValidadores(int periodoId, int tipo, string fechaLimite, string estructurasJson = null, string nivelCargo = "Todos", string gerencia = null, string subgerencia = null, string departamento = null)
        {
            try
            {
                var estructuras = new List<Dictionary<string, string>>();
                if (!string.IsNullOrEmpty(estructurasJson))
                {
                    estructuras = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(estructurasJson);
                }
                else if (!string.IsNullOrEmpty(gerencia))
                {
                    var gerenciasList = gerencia.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(g => g.Trim())
                                                .Where(g => !string.IsNullOrEmpty(g))
                                                .ToList();
                    foreach (var g in gerenciasList)
                    {
                        estructuras.Add(new Dictionary<string, string> {
                            { "Gerencia", g },
                            { "Subgerencia", subgerencia },
                            { "Area", departamento }
                        });
                    }
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    
                    var paramsObj = new DynamicParameters();
                    paramsObj.Add("periodoId", periodoId);
                    paramsObj.Add("tipo", tipo);

                    string filterCV = "1=1";
                    string filterP = "1=1";
                    string filterJ = "1=1";

                    if (estructuras != null && estructuras.Count > 0)
                    {
                        var filterCVList = new List<string>();
                        var filterPList = new List<string>();
                        var filterJList = new List<string>();

                        for (int i = 0; i < estructuras.Count; i++)
                        {
                            var est = estructuras[i];
                            string gVal = est.ContainsKey("Gerencia") ? est["Gerencia"] : "";
                            string sVal = est.ContainsKey("Subgerencia") ? est["Subgerencia"] : null;
                            string aVal = est.ContainsKey("Area") ? est["Area"] : null;

                            string gKey = $"g_{i}";
                            string sKey = $"s_{i}";
                            string aKey = $"a_{i}";

                            paramsObj.Add(gKey, gVal);
                            paramsObj.Add(sKey, string.IsNullOrEmpty(sVal) ? null : sVal);
                            paramsObj.Add(aKey, string.IsNullOrEmpty(aVal) ? null : aVal);

                            filterCVList.Add($"(cv.Gerencia = @{gKey} AND (@{sKey} IS NULL OR cv.Subgerencia = @{sKey} OR (cv.Subgerencia IS NULL AND @{sKey} = '')) AND (@{aKey} IS NULL OR cv.Area = @{aKey} OR (cv.Area IS NULL AND @{aKey} = '')))");
                            filterPList.Add($"(p.OGerencia = @{gKey} AND (@{sKey} IS NULL OR e.oSUBGERENCIA = @{sKey} OR (e.oSUBGERENCIA IS NULL AND @{sKey} = '')) AND (@{aKey} IS NULL OR e.primernivel = @{aKey} OR (e.primernivel IS NULL AND @{aKey} = '')))");
                            filterJList.Add($"(j.OGERENCIA = @{gKey} AND (@{sKey} IS NULL OR j.oSUBGERENCIA = @{sKey} OR (j.oSUBGERENCIA IS NULL AND @{sKey} = '')) AND (@{aKey} IS NULL OR j.primernivel = @{aKey} OR (j.primernivel IS NULL AND @{aKey} = '')))");
                        }

                        filterCV = string.Join(" OR ", filterCVList);
                        filterP = string.Join(" OR ", filterPList);
                        filterJ = string.Join(" OR ", filterJList);
                    }

                    string sql = "";
                    if (tipo == 1) // COMISIONES
                    {
                        sql = $@"
                            SELECT DISTINCT 
                                cv.CarnetValidador, 
                                ISNULL(e.correo, cv.CarnetValidador) as CorreoValidador,
                                ISNULL(cv.Gerencia, '') as Gerencia,
                                ISNULL(cv.Subgerencia, '') as Subgerencia,
                                ISNULL(cv.Area, '') as Area
                            FROM dbo.Comp_Com_ConfiguracionValidador cv
                            LEFT JOIN SIGHO1.dbo.EMP2024 e ON cv.CarnetValidador = e.CARNET
                            LEFT JOIN dbo.Comp_Plantilla p ON p.CarnetGerente = cv.CarnetValidador AND p.PeriodoID = @periodoId AND p.TipoPlantillaID = @tipo
                            WHERE p.PlantillaID IS NULL
                              AND ({filterCV})
                            UNION
                            SELECT DISTINCT 
                                p.CarnetGerente as CarnetValidador, 
                                ISNULL(e.correo, p.CarnetGerente) as CorreoValidador,
                                ISNULL(p.OGerencia, '') as Gerencia,
                                ISNULL(cv.Subgerencia, '') as Subgerencia,
                                ISNULL(cv.Area, '') as Area
                            FROM dbo.Comp_Plantilla p
                            LEFT JOIN SIGHO1.dbo.EMP2024 e ON p.CarnetGerente = e.CARNET
                            LEFT JOIN dbo.Comp_Com_ConfiguracionValidador cv ON p.CarnetGerente = cv.CarnetValidador AND p.OGERENCIA = cv.Gerencia
                            WHERE p.PeriodoID = @periodoId AND p.TipoPlantillaID = @tipo AND (p.Estado = 'Borrador' OR p.Estado = 'Devuelto')
                              AND ({filterP})";
                    }
                    else // CERTIFICACION
                    {
                        string filtroCargo = "";
                        if (!string.IsNullOrEmpty(nivelCargo) && nivelCargo != "Todos")
                        {
                            filtroCargo = $" AND j.cargo LIKE '%{nivelCargo}%' ";
                        }

                        sql = $@"
                            -- Jefes Inmediatos segun SIGHO (EMP2024) que no han iniciado
                            SELECT DISTINCT 
                                e.carnet_jefe1 as CarnetValidador, 
                                ISNULL(j.correo, e.carnet_jefe1) as CorreoValidador,
                                ISNULL(j.OGERENCIA, '') as Gerencia,
                                ISNULL(j.oSUBGERENCIA, '') as Subgerencia,
                                ISNULL(j.primernivel, '') as Area
                            FROM SIGHO1.dbo.EMP2024 e
                            INNER JOIN SIGHO1.dbo.EMP2024 j ON e.carnet_jefe1 = j.CARNET
                            LEFT JOIN dbo.Comp_Plantilla p ON p.CarnetGerente = e.carnet_jefe1 AND p.PeriodoID = @periodoId AND p.TipoPlantillaID = @tipo
                            WHERE e.fechabaja IS NULL 
                              AND e.carnet_jefe1 IS NOT NULL 
                              AND p.PlantillaID IS NULL
                              AND ({filterJ})
                              {filtroCargo}
                              -- Regla especial de mando: Debe tener al menos un reporte bajo su cargo directo
                              AND EXISTS (SELECT 1 FROM SIGHO1.dbo.EMP2024 rep WHERE rep.carnet_jefe1 = e.carnet_jefe1 AND rep.fechabaja IS NULL)
                            
                            UNION
                            
                            -- Jefes que ya tienen plantilla pero en borrador
                            SELECT DISTINCT 
                                p.CarnetGerente as CarnetValidador, 
                                ISNULL(e.correo, p.CarnetGerente) as CorreoValidador,
                                ISNULL(p.OGerencia, '') as Gerencia,
                                ISNULL(j.oSUBGERENCIA, '') as Subgerencia,
                                ISNULL(j.primernivel, '') as Area
                            FROM dbo.Comp_Plantilla p
                            LEFT JOIN SIGHO1.dbo.EMP2024 e ON p.CarnetGerente = e.CARNET
                            LEFT JOIN SIGHO1.dbo.EMP2024 j ON p.CarnetGerente = j.CARNET
                            WHERE p.PeriodoID = @periodoId AND p.TipoPlantillaID = @tipo AND (p.Estado = 'Borrador' OR p.Estado = 'Devuelto')
                              AND ({filterP})
                              -- Si se filtra por cargo en las plantillas existentes
                              { (string.IsNullOrEmpty(filtroCargo) ? "" : filtroCargo.Replace("j.", "e.")) }
                              -- Regla especial de mando
                              AND EXISTS (SELECT 1 FROM SIGHO1.dbo.EMP2024 rep WHERE rep.carnet_jefe1 = p.CarnetGerente AND rep.fechabaja IS NULL)";
                    }

                    var pendientes = db.Query<dynamic>(sql, paramsObj).ToList();
                    
                    if (pendientes.Count == 0) return Json(new { success = true, notificados = 0, message = "No hay validadores/jefes pendientes para notificar." });

                    string periodoNombre = db.QueryFirstOrDefault<string>("SELECT NombrePeriodo FROM dbo.Comp_Periodo WHERE PeriodoID = @id", new { id = periodoId });
                    
                    // Formatear fecha si viene del date picker (YYYY-MM-DD)
                    string fechaFormateada = fechaLimite;
                    DateTime dt;
                    if (DateTime.TryParse(fechaLimite, out dt)) {
                        fechaFormateada = dt.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
                    }

                    // Obtener plantilla personalizada de la BD si existe para este tipo
                    var plantillaPersonalizada = db.QueryFirstOrDefault("SELECT Asunto, Cuerpo FROM dbo.Comp_PlantillaCorreo WHERE TipoPlantillaID = @tipo", new { tipo });
                    string plantillaAsunto = plantillaPersonalizada?.Asunto;
                    string plantillaCuerpo = plantillaPersonalizada?.Cuerpo;

                    int enviados = 0;

                    foreach (var p in pendientes)
                    {
                        if (string.IsNullOrEmpty((string)p.CorreoValidador) || !((string)p.CorreoValidador).Contains("@")) continue;

                        string gerenciaNombre = (string)p.Gerencia ?? "";
                        string subgerenciaNombre = (string)p.Subgerencia ?? "";
                        string areaNombre = (string)p.Area ?? "";

                        // Determinar el nombre especifico de la estructura a notificar
                        string estructuraNombre = gerenciaNombre;
                        if (!string.IsNullOrEmpty(areaNombre)) {
                            estructuraNombre = areaNombre;
                        } else if (!string.IsNullOrEmpty(subgerenciaNombre)) {
                            estructuraNombre = subgerenciaNombre;
                        }
                        
                        string subject = "";
                        string body = "";
                        string correo = p.CorreoValidador;
                        
                        if (!string.IsNullOrEmpty(plantillaAsunto) && !string.IsNullOrEmpty(plantillaCuerpo))
                        {
                            subject = plantillaAsunto
                                .Replace("{estructuraNombre}", estructuraNombre)
                                .Replace("{periodoNombre}", periodoNombre)
                                .Replace("{fechaFormateada}", fechaFormateada)
                                .Replace("{fechaActual}", DateTime.Now.ToString("dd/MM/yyyy"));

                            body = plantillaCuerpo
                                .Replace("{estructuraNombre}", estructuraNombre)
                                .Replace("{periodoNombre}", periodoNombre)
                                .Replace("{fechaFormateada}", fechaFormateada)
                                .Replace("{fechaActual}", DateTime.Now.ToString("dd/MM/yyyy"));
                        }
                        else
                        {
                            // Fallbacks por defecto si no hay plantilla en BD
                            subject = $"Solicitud de Plantilla: {estructuraNombre} - {periodoNombre}";
                            if (tipo == 1) // COMISIONES
                            {
                                body = $@"<html><body><p>Buenas tardes.</p><p>Por este medio solicito su apoyo con enviarnos la plantilla actualizada, personal <b>{estructuraNombre}</b>, indicando si comisionara o no comisionara en el mes de <b>{periodoNombre}</b>. El cumplimiento de esta solicitud nos permitira:</p><ul><li>Actualizar sistema RH</li><li>Control del personal que comisiona.</li></ul><p><b>NOTA IMPORTANTE:</b> De acuerdo a orientaciones de Relaciones Laborales para los casos que sean trasladados a otro municipio aunque sea de la misma zona:</p><p>Cuando sea un traslado permanente el Colaborador debera presentarse a RH/Relaciones Laborales, para firma de acuerdo del traslado, caso contrario indicar tiempo en el cual estara apoyando en otro municipio.</p><p><b>FECHA DE ENTREGA: {fechaFormateada}</b></p><p>Puede acceder al portal para realizar su gestion en el siguiente enlace:</p><p><a href='https://recursoshumanosni/RHOnline' style='display:inline-block; padding:12px 25px; background-color:#B71C1C; color:white; text-decoration:none; border-radius:8px; font-weight:bold;'>ACCEDER AL PORTAL RHONLINE</a></p><p>Agradeciendo de antemano sus atenciones,</p><p>Saludos.</p></body></html>";
                            }
                            else // CERTIFICACION (TIPO 2)
                            {
                                body = $@"<html><body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
<p>Buen dia.</p>
<p>Favor revisar la plantilla del personal que le reporta directamente ({estructuraNombre}) ingresando al enlace adjunto, con corte al {DateTime.Now:dd/MM/yyyy}.</p>
<p>Revisar la plantilla. Si existen cambios que reportar, agradeceremos que los indique unicamente en la plataforma (cambio de jefe, cambio de ubicacion y cambio de departamento).</p>
<p>En caso de identificar cambios, favor detallar en la columna de Justificacion el motivo de los movimientos realizados.</p>
<p>Agradecemos su retroalimentacion a mas tardar el <b>{fechaFormateada}</b>.</p>
<p>Puede realizar la certificacion en el siguiente enlace:</p>
<p><a href='https://recursoshumanosni/RHOnline' style='display:inline-block; padding:12px 25px; background-color:#B71C1C; color:white; text-decoration:none; border-radius:8px; font-weight:bold;'>REALIZAR CERTIFICACION AHORA</a></p>
<p>Gracias de antemano por su atencion; quedamos atentos.</p>
</body></html>";
                            }
                        }

                        if (tipo == 1) // COMISIONES CC a Gerente
                        {
                            // Buscar el Gerente de esta gerencia del validador para mandarle copia (CC)
                            var carnetGerente = db.QueryFirstOrDefault<string>(@"
                                SELECT TOP 1 CARNET FROM SIGHO1.dbo.EMP2024 
                                WHERE fechabaja IS NULL AND cargo LIKE '%GERENTE%' AND OGERENCIA = @g", 
                                new { g = gerenciaNombre });

                            if (!string.IsNullOrEmpty(carnetGerente))
                            {
                                var correoGerente = db.QueryFirstOrDefault<string>(@"SELECT correo FROM SIGHO1.dbo.EMP2024 WHERE CARNET = @c", new { c = carnetGerente });
                                if (!string.IsNullOrEmpty(correoGerente) && correoGerente.Contains("@") && correoGerente.ToLower() != correo.ToLower())
                                {
                                    // Despachar copia al correo del Gerente de area
                                    await RegistrarYDespacharCorreo(db, correoGerente, $"(Copia) {subject}", body);
                                }
                            }
                        }

                        // MODO PRUEBA: Redirigido a Gustavo Lira
                        // correo = "gustavo.lira@claro.com.ni"; // REDIRECCIÓN DE PRUEBA DESACTIVADA
                        if (await RegistrarYDespacharCorreo(db, correo, subject, body))
                        {
                            enviados++;
                        }
                    }

                    return Json(new { success = true, notificados = enviados, message = "Se enviaron notificaciones a " + enviados + " validadores via API." });  
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerPlantillaCorreo(int tipo)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var plantilla = db.QueryFirstOrDefault("SELECT TipoPlantillaID, Asunto, Cuerpo FROM dbo.Comp_PlantillaCorreo WHERE TipoPlantillaID = @tipo", new { tipo });
                    if (plantilla != null)
                    {
                        string asunto = plantilla.Asunto;
                        string cuerpo = plantilla.Cuerpo;
                        int idPlantilla = plantilla.TipoPlantillaID;

                        var resObj = Json(new { success = true, data = new { TipoPlantillaID = idPlantilla, Asunto = asunto, Cuerpo = cuerpo } }, JsonRequestBehavior.AllowGet);
                        resObj.ContentEncoding = Encoding.UTF8;
                        return resObj;
                    }
                    
                    // Fallback si no existe en BD (cargar los hardcoded por defecto)
                    string asuntoDefecto = "Solicitud de Plantilla: {estructuraNombre} - {periodoNombre}";
                    string cuerpoDefecto = "";
                    if (tipo == 1)
                    {
                        cuerpoDefecto = @"<html><body><p>Buenas tardes.</p><p>Por este medio solicito su apoyo con enviarnos la plantilla actualizada, personal <b>{estructuraNombre}</b>, indicando si comisionara o no comisionara en el mes de <b>{periodoNombre}</b>. El cumplimiento de esta solicitud nos permitira:</p><ul><li>Actualizar sistema RH</li><li>Control del personal que comisiona.</li></ul><p><b>NOTA IMPORTANTE:</b> De acuerdo a orientaciones de Relaciones Laborales para los casos que sean trasladados a otro municipio aunque sea de la misma zona:</p><p>Cuando sea un traslado permanente el Colaborador debera presentarse a RH/Relaciones Laborales, para firma de acuerdo del traslado, caso contrario indicar tiempo en el cual estara apoyando en otro municipio.</p><p><b>FECHA DE ENTREGA: {fechaFormateada}</b></p><p>Puede acceder al portal para realizar su gestion en el siguiente enlace:</p><p><a href='https://recursoshumanosni/RHOnline' style='display:inline-block; padding:12px 25px; background-color:#B71C1C; color:white; text-decoration:none; border-radius:8px; font-weight:bold;'>ACCEDER AL PORTAL RHONLINE</a></p><p>Agradeciendo de antemano sus atenciones,</p><p>Saludos.</p></body></html>";
                    }
                    else
                    {
                        cuerpoDefecto = @"<html><body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
<p>Buen dia.</p>
<p>Favor revisar la plantilla del personal que le reporta directamente ({estructuraNombre}) ingresando al enlace adjunto, con corte al {fechaActual}.</p>
<p>Revisar la plantilla. Si existen cambios que reportar, agradeceremos que los indique unicamente en la plataforma (cambio de jefe, cambio de ubicacion y cambio de departamento).</p>
<p>En caso de identificar cambios, favor detallar en la columna de Justificacion el motivo de los movimientos realizados.</p>
<p>Agradecemos su retroalimentacion a mas tardar el <b>{fechaFormateada}</b>.</p>
<p>Puede realizar la certificacion en el siguiente enlace:</p>
<p><a href='https://recursoshumanosni/RHOnline' style='display:inline-block; padding:12px 25px; background-color:#B71C1C; color:white; text-decoration:none; border-radius:8px; font-weight:bold;'>REALIZAR CERTIFICACION AHORA</a></p>
<p>Gracias de antemano por su atencion; quedamos atentos.</p>
</body></html>";
                    }

                    return Json(new { success = true, data = new { TipoPlantillaID = tipo, Asunto = asuntoDefecto, Cuerpo = cuerpoDefecto } }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult GuardarPlantillaCorreo(int tipo, string asunto, string cuerpo)
        {
            try
            {
                if (string.IsNullOrEmpty(asunto) || string.IsNullOrEmpty(cuerpo))
                {
                    return Json(new { success = false, message = "El asunto y cuerpo no pueden estar vacios." });
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    string sql = @"
                        IF EXISTS (SELECT 1 FROM dbo.Comp_PlantillaCorreo WHERE TipoPlantillaID = @tipo)
                            UPDATE dbo.Comp_PlantillaCorreo SET Asunto = @asunto, Cuerpo = @cuerpo, FechaModificacion = GETDATE() WHERE TipoPlantillaID = @tipo
                        ELSE
                            INSERT INTO dbo.Comp_PlantillaCorreo (TipoPlantillaID, Asunto, Cuerpo, FechaModificacion) VALUES (@tipo, @asunto, @cuerpo, GETDATE())";
                    
                    db.Execute(sql, new { tipo, asunto, cuerpo });
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public async Task<JsonResult> EnviarCorreoPrueba(int tipo, string asunto, string cuerpo, string destinatario)
        {
            try
            {
                if (string.IsNullOrEmpty(destinatario) || !destinatario.Contains("@"))
                {
                    return Json(new { success = false, message = "Debe proporcionar un correo electronico valido." });
                }

                if (string.IsNullOrEmpty(asunto) || string.IsNullOrEmpty(cuerpo))
                {
                    return Json(new { success = false, message = "El asunto y cuerpo no pueden estar vacios." });
                }

                string subject = asunto
                    .Replace("{estructuraNombre}", "GERENCIA DE PRUEBA")
                    .Replace("{periodoNombre}", "PERIODO DE PRUEBA")
                    .Replace("{fechaFormateada}", DateTime.Now.AddDays(5).ToString("dd/MM/yyyy"))
                    .Replace("{fechaActual}", DateTime.Now.ToString("dd/MM/yyyy"));

                string body = cuerpo
                    .Replace("{estructuraNombre}", "GERENCIA DE PRUEBA")
                    .Replace("{periodoNombre}", "PERIODO DE PRUEBA")
                    .Replace("{fechaFormateada}", DateTime.Now.AddDays(5).ToString("dd/MM/yyyy"))
                    .Replace("{fechaActual}", DateTime.Now.ToString("dd/MM/yyyy"));

                if (body.Contains("</body>"))
                {
                    body = body.Replace("</body>", "<hr/><p style='color:red; font-size:11px;'><i>Este es un correo de prueba enviado desde la configuracion de plantillas de Compensacion.</i></p></body>");
                }
                else
                {
                    body += "<hr/><p style='color:red; font-size:11px;'><i>Este es un correo de prueba enviado desde la configuracion de plantillas de Compensacion.</i></p>";
                }

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    bool enviado = await RegistrarYDespacharCorreo(db, destinatario, "[PRUEBA] " + subject, body);
                    if (enviado)
                    {
                        return Json(new { success = true, message = "Correo de prueba enviado con exito a " + destinatario });
                    }
                    else
                    {
                        return Json(new { success = false, message = "No se pudo despachar el correo de prueba. Verifique logs o conexion del API de correos." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AgregarCargo(string nuevoCargo)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("IF NOT EXISTS (SELECT 1 FROM dbo.Comp_CatalogoCargo WHERE NombreCargo = @nuevoCargo) INSERT INTO dbo.Comp_CatalogoCargo (NombreCargo) VALUES (@nuevoCargo)", new { nuevoCargo });
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
                    // Busqueda hibrida: Carnet exacto o Correo (exacto o parcial antes del @)
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
        public JsonResult ReemplazarValidadorGlobal(string carnetAnterior, string carnetNuevo)
        {
            try
            {
                if (string.IsNullOrEmpty(carnetAnterior) || string.IsNullOrEmpty(carnetNuevo))
                    return Json(new { success = false, message = "Debe indicar ambos carnets." });

                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // Resolver correo a carnet si aplica
                    string sqlResolve = @"SELECT TOP 1 CARNET FROM SIGHO1.dbo.EMP2024 WITH (NOLOCK)
                        WHERE fechabaja IS NULL AND (CARNET = @filtro OR correo = @filtro OR correo LIKE @filtro + '@%')";

                    var carnetNuevoReal = db.QueryFirstOrDefault<string>(sqlResolve, new { filtro = carnetNuevo.Trim() });
                    if (carnetNuevoReal == null)
                        return Json(new { success = false, message = "El validador nuevo no fue encontrado en SIGHO1." });

                    var carnetAntReal = db.QueryFirstOrDefault<string>(sqlResolve, new { filtro = carnetAnterior.Trim() });
                    if (carnetAntReal == null) carnetAntReal = carnetAnterior.Trim();

                    int filas = db.Execute(@"UPDATE dbo.Comp_Com_ConfiguracionValidador 
                        SET CarnetValidador = @nuevo, FechaModificacion = GETDATE() 
                        WHERE CarnetValidador = @anterior",
                        new { nuevo = carnetNuevoReal, anterior = carnetAntReal });

                    return Json(new { success = true, message = filas + " asignacion(es) transferida(s) al nuevo validador." });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult AgregarEmpleadoManual(string carnet, int plantillaId, string gerencia = null)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("EXEC SP_Comp_Plantilla @Op='AgregarEmpleadoManual', @PlantillaID=@plantillaId, @CarnetUsuario=@carnet, @Gerencia=@gerencia", new { carnet, plantillaId, gerencia });
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
                    // 1. Eliminar evidencia asociada primero para evitar error de FK
                    db.Execute("DELETE FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = @detalleId", new { detalleId });

                    // 2. Eliminar el detalle
                    int eliminados = db.Execute("DELETE FROM dbo.Comp_PlantillaDetalle WHERE DetalleID = @detalleId", new { detalleId });
                    
                    if (eliminados > 0)
                        return Json(new { success = true, message = "Colaborador eliminado de la plantilla." });
                    else
                        return Json(new { success = false, message = "No se encontro el registro para eliminar." });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
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
                    string sql = @"SELECT DISTINCT Gerencia 
                                   FROM dbo.Comp_Com_ConfiguracionValidador 
                                   WHERE CarnetValidador = @carnet AND ISNULL(Activo, 1) = 1";

                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@carnet", employees.EmployeeNumber);
                    cn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        if (dr["Gerencia"] != DBNull.Value)
                        {
                            lista.Add(dr["Gerencia"].ToString());
                        }
                    }
                }

                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) 
            { 
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); 
            }
        }

        [HttpGet]
        public JsonResult ObtenerHistorial(string filtro, string modo)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();

                    // Resolver carnet si se pasa un correo
                    string carnet = filtro?.Trim();
                    if (!string.IsNullOrEmpty(carnet) && carnet.Contains("@"))
                    {
                        var resolved = db.QueryFirstOrDefault<string>("SELECT TOP 1 CARNET FROM SIGHO1.dbo.EMP2024 WHERE correo = @c OR correo LIKE @c + '%'", new { c = carnet });
                        if (resolved != null) carnet = resolved;
                    }

                    // Columnas base para todas las consultas
                    string selectBase = @"
                        SELECT per.NombrePeriodo, per.Mes, per.Anio,
                               CASE WHEN p.TipoPlantillaID = 1 THEN 'Comisiones' ELSE 'Certificacion' END as Modulo,
                               d.CarnetEmpleado, d.NombreCompleto,
                               d.Cargo_SIGHO, d.Cargo_Reportado,
                               d.Jefe_SIGHO, d.Jefe_Reportado,
                               d.Ubicacion_SIGHO, d.Ubicacion_Reportada,
                               d.Departamento_SIGHO, d.Departamento_Reportado,
                               d.Comisiona, d.Observacion,
                               d.JustMotivo, d.JustReposicion, d.JustTiempo,
                               d.FechaSnapshot,
                               p.NombreGerente as Validador, p.CarnetGerente as CarnetValidador,
                               p.Estado as EstadoPlantilla,
                               d.OGERENCIA_SIGHO as Gerencia
                        FROM dbo.Comp_PlantillaDetalle d
                        INNER JOIN dbo.Comp_Plantilla p ON d.PlantillaID = p.PlantillaID
                        INNER JOIN dbo.Comp_Periodo per ON p.PeriodoID = per.PeriodoID";

                    List<dynamic> rawData;
                    dynamic rawInfo = null;
                    string infoLabel = "";

                    switch (modo)
                    {
                        case "emp_com": // 1. Empleado en Comisiones
                            rawData = db.Query(selectBase + " WHERE d.CarnetEmpleado = @carnet AND p.TipoPlantillaID = 1 ORDER BY per.Anio DESC, per.Mes DESC", new { carnet }).ToList();
                            rawInfo = db.QueryFirstOrDefault("SELECT nombre_completo, cargo, OGERENCIA, Nombreubicacion FROM SIGHO1.dbo.EMP2024 WHERE CARNET = @carnet AND fechabaja IS NULL", new { carnet });
                            infoLabel = "Empleado";
                            break;

                        case "validador_com": // 2. Validador en Comisiones
                            rawData = db.Query(selectBase + " WHERE p.CarnetGerente = @carnet AND p.TipoPlantillaID = 1 ORDER BY per.Anio DESC, per.Mes DESC, d.NombreCompleto", new { carnet }).ToList();
                            rawInfo = db.QueryFirstOrDefault("SELECT nombre_completo, cargo, OGERENCIA FROM SIGHO1.dbo.EMP2024 WHERE CARNET = @carnet AND fechabaja IS NULL", new { carnet });
                            infoLabel = "Validador";
                            break;

                        case "emp_cert": // 3. Empleado en Certificacion
                            rawData = db.Query(selectBase + " WHERE d.CarnetEmpleado = @carnet AND p.TipoPlantillaID = 2 ORDER BY per.Anio DESC, per.Mes DESC", new { carnet }).ToList();
                            rawInfo = db.QueryFirstOrDefault("SELECT nombre_completo, cargo, OGERENCIA, Nombreubicacion FROM SIGHO1.dbo.EMP2024 WHERE CARNET = @carnet AND fechabaja IS NULL", new { carnet });
                            infoLabel = "Empleado";
                            break;

                        case "jefe_cert": // 4. Jefe Inmediato en Certificacion
                            rawData = db.Query(selectBase + " WHERE p.CarnetGerente = @carnet AND p.TipoPlantillaID = 2 ORDER BY per.Anio DESC, per.Mes DESC, d.NombreCompleto", new { carnet }).ToList();
                            rawInfo = db.QueryFirstOrDefault("SELECT nombre_completo, cargo, OGERENCIA FROM SIGHO1.dbo.EMP2024 WHERE CARNET = @carnet AND fechabaja IS NULL", new { carnet });
                            infoLabel = "Jefe Inmediato";
                            break;

                        case "gerencia": // 5. Por Gerencia / Subgerencia / Area
                            string filtroGer = "%" + (carnet ?? "") + "%";
                            rawData = db.Query(selectBase + @" WHERE (d.OGERENCIA_SIGHO LIKE @f 
                                OR d.OSUBGERENCIA_SIGHO LIKE @f 
                                OR d.Departamento_SIGHO LIKE @f 
                                OR d.NombreCompleto LIKE @f)
                                ORDER BY per.Anio DESC, per.Mes DESC, d.OGERENCIA_SIGHO, d.NombreCompleto", new { f = filtroGer }).ToList();
                            infoLabel = "Gerencia";
                            break;

                        default:
                            rawData = new List<dynamic>();
                            break;
                    }

                    // Convertir DapperRow a Dictionary para serializar
                    var data = rawData.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, k => k.Value)).ToList();
                    var info = rawInfo != null ? ((IDictionary<string, object>)rawInfo).ToDictionary(k => k.Key, k => k.Value) : null;

                    return new JsonResult
                    {
                        Data = new { success = true, data = data, info = info, carnet = carnet, infoLabel = infoLabel, totalRegistros = data.Count },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                        MaxJsonLength = int.MaxValue
                    };
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        private async Task<bool> RegistrarYDespacharCorreo(SqlConnection db, string para, string asunto, string cuerpo)
        {
            try
            {
                // Limpieza de Body para evitar caracteres de control de identacion y ahorrar espacio
                string compactBody = cuerpo.Replace("\r\n", "").Replace("    ", "");

                // 1. Registrar en Base de Datos
                int notifId = db.QueryFirstOrDefault<int>(@"
                    INSERT INTO dbo.Comp_Com_Notificacion (Para, Asunto, Cuerpo, Estado) 
                    VALUES (@para, @asunto, @cuerpo, 'Pendiente');
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { para, asunto, cuerpo = compactBody });

                // 2. Notificar al API remoto (.66)
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(60); // Timeout extendido para SMTP
                    var payload = new { id = notifId };
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://172.26.54.66/apihcm/api/values/correo/compensacion", content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void AplicarCarryForward(SqlConnection db, int plantillaId, SqlTransaction tx)
        {
            string sqlCarry = @"
                UPDATE det_actual
                SET det_actual.Cargo_Reportado = det_ant.Cargo_Reportado,
                    det_actual.Jefe_Reportado = det_ant.Jefe_Reportado,
                    det_actual.Ubicacion_Reportada = det_ant.Ubicacion_Reportada,
                    det_actual.Comisiona = det_ant.Comisiona,
                    det_actual.Observacion = '(Arrastre mes anterior) ' + ISNULL(det_ant.Observacion,'')
                FROM dbo.Comp_PlantillaDetalle det_actual
                INNER JOIN dbo.Comp_Plantilla p_actual ON det_actual.PlantillaID = p_actual.PlantillaID
                INNER JOIN dbo.Comp_PlantillaDetalle det_ant ON det_actual.CarnetEmpleado = det_ant.CarnetEmpleado
                INNER JOIN dbo.Comp_Plantilla p_ant ON det_ant.PlantillaID = p_ant.PlantillaID
                WHERE det_actual.PlantillaID = @plantillaId
                  AND p_ant.Estado = 'Aprobado'
                  AND p_ant.PeriodoID < p_actual.PeriodoID
                  AND det_ant.DetalleID = (
                      SELECT MAX(d2.DetalleID) 
                      FROM dbo.Comp_PlantillaDetalle d2 
                      INNER JOIN dbo.Comp_Plantilla p2 ON d2.PlantillaID = p2.PlantillaID
                      WHERE d2.CarnetEmpleado = det_actual.CarnetEmpleado 
                        AND p2.Estado = 'Aprobado' 
                        AND p2.PeriodoID < p_actual.PeriodoID
                  )";
            db.Execute(sqlCarry, new { plantillaId }, transaction: tx);
        }

        [HttpGet]
        public JsonResult ObtenerHistorialEvaluaciones()
        {
            using (var db = new SqlConnection(CadenaConexion))
            {
                var bajas = db.Query("SELECT 'BAJA' AS Tipo, * FROM dbo.Comp_EvaluacionBaja").ToList();
                var ingresos = db.Query("SELECT 'INGRESO' AS Tipo, * FROM dbo.Comp_EvaluacionIngreso").ToList();
                var final = bajas.Concat(ingresos).OrderByDescending(x => ((dynamic)x).FechaEvaluacion).ToList();
                return Json(new { data = final }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerAuditoriaDetalle(int detalleId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var audit = db.Query("SELECT * FROM dbo.Comp_PlantillaAuditoria WHERE DetalleID = @detalleId ORDER BY FechaCambio DESC", new { detalleId }).ToList();
                    return Json(new { success = true, data = audit }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

  

        [HttpPost]
        public JsonResult SincronizarSigho(int plantillaId)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    db.Execute("SP_Comp_Plantilla", new { Op = "SincronizarFinal", PlantillaID = plantillaId }, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true, message = "Datos sincronizados exitosamente con SIGHO1." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ExportarExcel(int periodoId, int tipo)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    string spName = tipo == 1 ? "SP_Comp_Plantilla" : "SP_Comp_Certificacion";
                    var details = tipo == 1 
                        ? db.Query<DetallePlantillaViewModel>($"EXEC {spName} @Op='ObtenerRevisionRRHH', @PeriodoID=@periodoId, @TipoPlantillaID=@tipo", new { periodoId, tipo }).ToList()
                        : db.Query<DetallePlantillaViewModel>($"EXEC {spName} @Op='ObtenerRevisionRRHH', @PeriodoID=@periodoId", new { periodoId }).ToList();
                    
                    string nombreProceso = tipo == 1 ? "Comisiones" : "Certificacion";
                    
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add($"Revision_{nombreProceso}");
                        
                        // Encabezados
                        int col = 1;
                        worksheet.Cell(1, col++).Value = "Estado Plantilla";
                        worksheet.Cell(1, col++).Value = "Validador (Gerente)";
                        worksheet.Cell(1, col++).Value = "Gerencia";
                        worksheet.Cell(1, col++).Value = "Carnet";
                        worksheet.Cell(1, col++).Value = "Nombre Empleado";
                        worksheet.Cell(1, col++).Value = "Area Oficial";
                        worksheet.Cell(1, col++).Value = "Area Reportada";
                        worksheet.Cell(1, col++).Value = "Cargo Oficial";
                        worksheet.Cell(1, col++).Value = "Cargo Reportado";
                        worksheet.Cell(1, col++).Value = "Jefe Oficial";
                        worksheet.Cell(1, col++).Value = "Jefe Reportado";
                        worksheet.Cell(1, col++).Value = "Ubicacion Oficial";
                        worksheet.Cell(1, col++).Value = "Ubicacion Reportada";
                        worksheet.Cell(1, col++).Value = "Comisiona";
                        worksheet.Cell(1, col++).Value = "Tiene Evidencia";
                        worksheet.Cell(1, col++).Value = "Motivo Cambio";
                        worksheet.Cell(1, col++).Value = "A Quien Repone";
                        worksheet.Cell(1, col++).Value = "Por Cuanto Tiempo";
                        worksheet.Cell(1, col++).Value = "Observacion General";

                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.DarkRed;
                        headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

                        // Datos
                        int row = 2;
                        foreach (var d in details)
                        {
                            col = 1;
                            worksheet.Cell(row, col++).Value = d.EstadoPlantilla ?? d.Estado ?? "---";
                            worksheet.Cell(row, col++).Value = d.NombreGerente;
                            worksheet.Cell(row, col++).Value = d.OGERENCIA_SIGHO;
                            worksheet.Cell(row, col++).Value = d.CarnetEmpleado;
                            worksheet.Cell(row, col++).Value = d.NombreCompleto;
                            worksheet.Cell(row, col++).Value = d.Departamento_SIGHO;
                            worksheet.Cell(row, col++).Value = d.Departamento_Reportado;
                            worksheet.Cell(row, col++).Value = d.Cargo_SIGHO;
                            worksheet.Cell(row, col++).Value = d.Cargo_Reportado;
                            worksheet.Cell(row, col++).Value = d.Jefe_SIGHO;
                            worksheet.Cell(row, col++).Value = d.Jefe_Reportado;
                            worksheet.Cell(row, col++).Value = d.Ubicacion_SIGHO;
                            worksheet.Cell(row, col++).Value = d.Ubicacion_Reportada;
                            worksheet.Cell(row, col++).Value = d.Comisiona == "S" ? "SI" : "NO";
                            worksheet.Cell(row, col++).Value = d.HasEvidencia ? "SI" : "NO";
                            worksheet.Cell(row, col++).Value = d.JustMotivo;
                            worksheet.Cell(row, col++).Value = d.JustReposicion;
                            worksheet.Cell(row, col++).Value = d.JustTiempo;
                            worksheet.Cell(row, col++).Value = d.Observacion;
                            
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();

                        using (var stream = new System.IO.MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();
                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Revision_{nombreProceso}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"Error al exportar: {ex.Message}");
            }
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
                    // Eliminar evidencia previa si existe
                    db.Execute("DELETE FROM dbo.Comp_PlantillaEvidencia WHERE DetalleID = @detalleId", new { detalleId });

                    string sql = @"INSERT INTO dbo.Comp_PlantillaEvidencia (DetalleID, NombreArchivo, RutaArchivo, Extension, Tamanio, UsuarioCarga, ArchivoBinario)
                                   VALUES (@detalleId, @fileName, 'DATABASE', @extension, @tamanio, @usuario, @fileData)";
                    db.Execute(sql, new { detalleId, fileName, extension, tamanio = file.ContentLength, usuario = user.EmployeeNumber, fileData });
                }

                return Json(new { success = true, message = "Archivo guardado en base de datos exitosamente." });
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
                    if (ev == null) return Content("No se encontro evidencia para este registro.");

                    byte[] fileBytes = (byte[])ev.ArchivoBinario;
                    if (fileBytes == null || fileBytes.Length == 0) return Content("El archivo no tiene contenido binario en la base de datos.");

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
                return Json(new { success = true, message = "Evidencia eliminada de la base de datos." });
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

        [HttpGet]
        public ActionResult ExportarComisiones(int periodoId) { return ExportarExcel(periodoId, 1); }
        
        [HttpGet]
        public ActionResult ExportarCertificacion(int periodoId) { return ExportarExcel(periodoId, 2); }

        [HttpGet]
        public ActionResult ExportarPDF(int periodoId, int tipo)
        {
            try
            {
                using (var db = new SqlConnection(CadenaConexion))
                {
                    db.Open();
                    var p = db.QueryFirstOrDefault<PeriodoViewModel>("SELECT * FROM dbo.Comp_Periodo WHERE PeriodoID = @periodoId", new { periodoId });
                    string spName = tipo == 1 ? "SP_Comp_Plantilla" : "SP_Comp_Certificacion";
                    var rawDetails = tipo == 1 
                        ? db.Query<DetallePlantillaViewModel>($"EXEC {spName} @Op='ObtenerRevisionRRHH', @PeriodoID=@periodoId, @TipoPlantillaID=@tipo", new { periodoId, tipo }).ToList()
                        : db.Query<DetallePlantillaViewModel>($"EXEC {spName} @Op='ObtenerRevisionRRHH', @PeriodoID=@periodoId", new { periodoId }).ToList();
                    
                    var details = rawDetails.Where(x => x.EsDiscrepancia == 1).ToList();

                    using (MemoryStream ms = new MemoryStream())
                    {
                        Document doc = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                        PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                        doc.Open();

                        var fontTitle = FontFactory.GetFont("Arial", 16, Font.BOLD, BaseColor.DARK_GRAY);
                        var fontSub = FontFactory.GetFont("Arial", 10, Font.NORMAL, BaseColor.GRAY);
                        var fontHead = FontFactory.GetFont("Arial", 9, Font.BOLD, BaseColor.WHITE);
                        var fontRow = FontFactory.GetFont("Arial", 8, Font.NORMAL, BaseColor.BLACK);

                        string titulo = tipo == 1 ? "REPORTE DE DISCREPANCIAS - COMISIONES" : "REPORTE DE DISCREPANCIAS - CERTIFICACION PERSONAL";
                        Paragraph pTitle = new Paragraph(titulo, fontTitle);
                        pTitle.Alignment = Element.ALIGN_CENTER;
                        doc.Add(pTitle);

                        Paragraph pPeriodo = new Paragraph("Periodo: " + (p?.NombrePeriodo ?? "-") + " | Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontSub);
                        pPeriodo.Alignment = Element.ALIGN_CENTER;
                        pPeriodo.SpacingAfter = 20;
                        doc.Add(pPeriodo);

                        PdfPTable table = new PdfPTable(10);
                        table.WidthPercentage = 100;
                        table.SetWidths(new float[] { 10f, 8f, 12f, 12f, 12f, 10f, 10f, 10f, 4f, 12f });

                        string[] heads = { "Gerencia", "Carnet", "Nombre Empleado", "Area (Rep)", "Cargo (Rep)", "Jefe (Rep)", "Ubicacion (Rep)", "Depto SIGHO", "Com", "Observacion" };
                        BaseColor headColor = tipo == 1 ? new BaseColor(66, 66, 66) : new BaseColor(183, 28, 28);

                        foreach (var h in heads)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(h, fontHead));
                            cell.BackgroundColor = headColor;
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.Padding = 5;
                            table.AddCell(cell);
                        }

                        foreach (var d in details)
                        {
                            table.AddCell(new PdfPCell(new Phrase(d.OGERENCIA_SIGHO, fontRow)));
                            table.AddCell(new PdfPCell(new Phrase(d.CarnetEmpleado, fontRow)));
                            table.AddCell(new PdfPCell(new Phrase(d.NombreCompleto, fontRow)));
                            table.AddCell(new PdfPCell(new Phrase(d.Departamento_Reportado ?? d.Departamento_SIGHO, fontRow)));
                            table.AddCell(new PdfPCell(new Phrase(d.Cargo_Reportado ?? d.Cargo_SIGHO, fontRow)));
                            table.AddCell(new PdfPCell(new Phrase(d.Jefe_Reportado ?? d.Jefe_SIGHO, fontRow)));
                            table.AddCell(new PdfPCell(new Phrase(d.Ubicacion_Reportada ?? d.Ubicacion_SIGHO, fontRow)));
                            table.AddCell(new PdfPCell(new Phrase(d.Departamento_SIGHO, fontRow)));
                            table.AddCell(new PdfPCell(new Phrase(d.Comisiona, fontRow)) { HorizontalAlignment = Element.ALIGN_CENTER });
                            table.AddCell(new PdfPCell(new Phrase(d.Observacion, fontRow)));
                        }

                        doc.Add(table);
                        doc.Close();

                        byte[] bytes = ms.ToArray();
                        string fileName = $"Reporte_Discrepancias_{tipo}_{(p?.NombrePeriodo ?? "Periodo").Replace(" ","_")}.pdf";
                        return File(bytes, "application/pdf", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("Error al generar PDF: " + ex.Message);
            }
        }
    }
}
