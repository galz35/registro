using Dapper;
using Datos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class EdificiosController : Controller
    {
        public ActionResult Permisos()
        {
            return View();
        }
        // GET: Edificios
        public ActionResult Index()
        {
            return View();
        }
        public class EmpleadoDto
        {
            public int idhcm { get; set; }
            public string carnet { get; set; }
            public string nombre_completo { get; set; }
            public string correo { get; set; }
            public string cargo { get; set; }
            public string OGERENCIA { get; set; }
        }
        public class EdificioDto
        {
            public int id { get; set; }
            public string nombre { get; set; }
            public string direccion { get; set; }
            public decimal latitud { get; set; }
            public decimal longitud { get; set; }
            public string exactitud { get; set; }
            public string fuente { get; set; }
        }
        public class PermisoOut
        {
            public int id { get; set; }
            public string carnet { get; set; }
            public int edificio_id { get; set; }
            public string nombre { get; set; }
            public string direccion { get; set; }
            public bool es_default { get; set; }
            public int? radio_mts { get; set; }
            public DateTime? vigencia_ini { get; set; }
            public DateTime? vigencia_fin { get; set; }
            public bool? Activo { get; set; }
        }
        public class PermisoIn    // para guardar
        {
            public int edificio_id { get; set; }
            public bool es_default { get; set; }
            public int? radio_mts { get; set; }
            public DateTime? vigencia_ini { get; set; }
            public DateTime? vigencia_fin { get; set; }
            public bool activo { get; set; }
        }

        private static SqlConnection Conn()
        {
            var cs = new configdamper().strConnection2; // <- tu clase de config
            return new SqlConnection(cs);
        }


        /* =========================
           Empleados (con Gerencia)
           ========================= */
        [HttpGet]
        public JsonResult GetUsuariosActivos()
        {
            // cachea en sesión para resolver carnet/correo rápido
            if (Session["empleadomenu_edif"] is List<EmpleadoDto> cache)
                return Json(cache, JsonRequestBehavior.AllowGet);

            using (var db = Conn())
            {
                var list = db.Query<EmpleadoDto>(@"
                    SELECT idhcm, carnet, nombre_completo, correo, cargo, OGERENCIA
                    FROM EMP2024 WITH (NOLOCK)
                    WHERE fechabaja IS NULL OR fechabaja='0001-01-01'
                ").ToList();
                Session["empleadomenu_edif"] = list;
                return Json(list, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetGerencias()
        {
            using (var db = Conn())
            {
                var list = db.Query<string>("spr_EMP_Gerencias", commandType: CommandType.StoredProcedure).ToList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
        }

        /* =========================
           Catálogo Edificios
           ========================= */
        [HttpGet]
        public JsonResult Edificios(string q = null)
        {
            using (var db = Conn())
            {
                var list = db.Query<EdificioDto>("spr_Edif_Listar", new { q }, commandType: CommandType.StoredProcedure).ToList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult UpsertEdificio(EdificioDto e)
        {
            try
            {
                using (var db = Conn())
                {
                    var p = new DynamicParameters();
                    p.Add("@id", null, DbType.Int32, ParameterDirection.InputOutput);
                    p.Add("@nombre", e.nombre);
                    p.Add("@direccion", e.direccion);
                    p.Add("@lat", e.latitud);
                    p.Add("@lon", e.longitud);
                    p.Add("@exactitud", e.exactitud);
                    p.Add("@fuente", e.fuente);
                    db.Execute("spr_Edif_Upsert", p, commandType: CommandType.StoredProcedure);
                    var id = p.Get<int>("@id");
                    return Json(new { ok = true, id });
                }
            }
            catch (Exception ex) { return Json(new { ok = false, msg = ex.Message }); }
        }

        /* =========================
           Permisos por usuario
           ========================= */
        [HttpGet]
        public JsonResult PermisosUsuario(int userId)
        {
            // obtiene carnet a 6 dígitos desde caché
            var cache = Session["empleadomenu_edif"] as List<EmpleadoDto>;
            if (cache == null) GetUsuariosActivos();
            cache = Session["empleadomenu_edif"] as List<EmpleadoDto>;
            var emp = cache?.FirstOrDefault(x => x.idhcm == userId);
            var carnet = emp?.carnet ?? userId.ToString("D6");

            using (var db = Conn())
            {
                var list = db.Query<PermisoOut>("spr_EEP_ListarPorCarnet", new { carnet }, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { carnet, permisos = list }, JsonRequestBehavior.AllowGet);
            }
        }

        // helper: arma DataTable para TVP (dbo.TVP_EEP)
        private static DataTable ToPermisosTable(IEnumerable<PermisoIn> items)
        {
            var dt = new DataTable();
            dt.Columns.Add("edificio_id", typeof(int));
            dt.Columns.Add("es_default", typeof(bool));
            dt.Columns.Add("radio_mts", typeof(int));
            dt.Columns.Add("vigencia_ini", typeof(DateTime));
            dt.Columns.Add("vigencia_fin", typeof(DateTime));
            dt.Columns.Add("activo", typeof(bool));
            foreach (var x in items ?? new List<PermisoIn>())
            {
                var row = dt.NewRow();
                row["edificio_id"] = x.edificio_id;
                row["es_default"] = x.es_default;
                row["radio_mts"] = x.radio_mts.HasValue ? (object)x.radio_mts.Value : DBNull.Value;
                row["vigencia_ini"] = x.vigencia_ini.HasValue ? (object)x.vigencia_ini.Value : DBNull.Value;
                row["vigencia_fin"] = x.vigencia_fin.HasValue ? (object)x.vigencia_fin.Value : DBNull.Value;

                row["activo"] = x.activo;
                dt.Rows.Add(row);
            }
            return dt;
        }

        [HttpPost]
        public JsonResult GuardarPermisos(int userId, List<PermisoIn> data)
        {
            try
            {
                // resolve carnet y username para auditoría
                var cache = Session["empleadomenu_edif"] as List<EmpleadoDto>;
                if (cache == null) GetUsuariosActivos();
                cache = Session["empleadomenu_edif"] as List<EmpleadoDto>;
                var emp = cache?.FirstOrDefault(x => x.idhcm == userId);
                var carnet = emp?.carnet ?? userId.ToString("D6");
                var userName = emp?.correo;

                using (var db = Conn())
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandText = "spr_EEP_GuardarLote";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var p1 = new SqlParameter("@Carnet", SqlDbType.VarChar, 20) { Value = carnet };
                    var p2 = new SqlParameter("@UserName", SqlDbType.NVarChar, 64) { Value = (object)userName ?? DBNull.Value };
                    var tvp = new SqlParameter("@Permisos", SqlDbType.Structured) { TypeName = "dbo.TVP_EEP", Value = ToPermisosTable(data) };

                    cmd.Parameters.Add(p1); cmd.Parameters.Add(p2); cmd.Parameters.Add(tvp);
                    if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                }
                return Json(new { ok = true, msg = "Permisos actualizados." });
            }
            catch (Exception ex) { return Json(new { ok = false, msg = ex.Message }); }
        }
    }
}