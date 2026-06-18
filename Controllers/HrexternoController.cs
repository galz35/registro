using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class HrexternoController : Controller
    {
        // GET: Hrexterno
 
            // Conexion SQL
            private readonly string _cs = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog=CR;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;";

        // ============================
        // 1) LISTA DE EMPLEADOS (POR JEFE)
        // ============================

        // Página (razor mínimo, JS arma todo)
        public ActionResult Empleados() => View();

            // DataTable -> empleados visibles para el jefe actual (carnet)
            [HttpGet]
            public JsonResult EmpleadosJson()
            {
                var carnetActual = GetCarnetActual(); // <- resuelve el carnet del usuario conectado
                using (var cn = new SqlConnection(_cs))
                {
                    var filas = cn.Query<EmpleadoBasico>(
                        "cr.usp_EmpleadosPorJefe",
                        new { Carnet = carnetActual },
                        commandType: CommandType.StoredProcedure
                    );
                    return Json(new { data = filas }, JsonRequestBehavior.AllowGet);
                }
            }

            // ============================
            // 2) LISTA HE POR EMPLEADO + CRUD HE
            // ============================

            // Página detalle del empleado (carnet)
            public ActionResult Empleado(string id) // id = carnet
            {
                ViewBag.Carnet = id;
                return View();
            }

            // JSON: lista de horas extra por carnet
            [HttpGet]
            public JsonResult HE_Listar(string carnet)
            {
                using (var cn = new SqlConnection(_cs))
                {
                    var rows = cn.Query<HeItem>(
                        "cr.usp_HE_ListarPorEmpleado",            // Proc asumido fase previa
                        new { Carnet = carnet },
                        commandType: CommandType.StoredProcedure
                    );
                    return Json(new { data = rows }, JsonRequestBehavior.AllowGet);
                }
            }

            // Crear HE
            [HttpPost]
            public JsonResult HE_Crear(HeCreateDto dto)
            {
                // Validaciones rápidas en servidor
                if (dto == null || string.IsNullOrWhiteSpace(dto.Carnet))
                    return Json(new { status = "Error", message = "Datos inválidos." });

                using (var cn = new SqlConnection(_cs))
                {
                    var p = new DynamicParameters();
                    p.Add("@Carnet", dto.Carnet);
                    p.Add("@Fecha", dto.Fecha);
                    p.Add("@HoraInicio", dto.HoraInicio);
                    p.Add("@HoraFin", dto.HoraFin);
                    p.Add("@Servicio", dto.Servicio);
                    p.Add("@Razon", dto.Razon);
                    p.Add("@Notas", dto.Notas);
                    p.Add("@CarnetActor", GetCarnetActual());
                    p.Add("@IdHE", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    cn.Execute("cr.usp_HE_Crear", p, commandType: CommandType.StoredProcedure);
                    var id = p.Get<int>("@IdHE");

                    return Json(new { status = "OK", message = "Registrado.", id });
                }
            }

            // Editar HE (solo campos editables del solicitante mientras esté en estado permitido)
            [HttpPost]
            public JsonResult HE_Editar(HeEditDto dto)
            {
                if (dto == null || dto.IdHE <= 0)
                    return Json(new { status = "Error", message = "Datos inválidos." });

                using (var cn = new SqlConnection(_cs))
                {
                    var p = new DynamicParameters();
                    p.Add("@IdHE", dto.IdHE);
                    p.Add("@Fecha", dto.Fecha);
                    p.Add("@HoraInicio", dto.HoraInicio);
                    p.Add("@HoraFin", dto.HoraFin);
                    p.Add("@Servicio", dto.Servicio);
                    p.Add("@Razon", dto.Razon);
                    p.Add("@Notas", dto.Notas);
                    p.Add("@CarnetActor", GetCarnetActual());

                    cn.Execute("cr.usp_HE_Editar", p, commandType: CommandType.StoredProcedure);
                    return Json(new { status = "OK", message = "Actualizado." });
                }
            }

            // ============================
            // 3) APROBACIONES (Jefe/Gerente/RRHH) MASIVO
            //    Recibe CSV de Ids, usa TVP cr.IdList
            // ============================

            [HttpPost]
            public JsonResult HE_Aprobar(string ids, string nota = null)
            {
                return CambiarEstadoMasivo(ids, "AP", nota);
            }

            [HttpPost]
            public JsonResult HE_Denegar(string ids, string nota = null)
            {
                return CambiarEstadoMasivo(ids, "DN", nota);
            }

            // ============================
            // Helpers
            // ============================

            private JsonResult CambiarEstadoMasivo(string idsCsv, string estado, string nota)
            {
                if (string.IsNullOrWhiteSpace(idsCsv))
                    return Json(new { status = "Error", message = "Sin IDs." });

                var tvp = ToIdListTVP(idsCsv);
                using (var cn = new SqlConnection(_cs))
                using (var cmd = new SqlCommand("cr.usp_HE_CambiarEstado_Masivo", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // TVP: cr.IdList(Id INT NOT NULL)
                    var pIds = cmd.Parameters.AddWithValue("@Ids", tvp);
                    pIds.SqlDbType = SqlDbType.Structured;
                    pIds.TypeName = "cr.IdList";

                    cmd.Parameters.AddWithValue("@NuevoEstado", estado);         // 'AP' o 'DN'
                    cmd.Parameters.AddWithValue("@CarnetActor", GetCarnetActual());
                    cmd.Parameters.AddWithValue("@Notas", (object)nota ?? DBNull.Value);

                    cn.Open();
                    cmd.ExecuteNonQuery();
                    return Json(new { status = "OK", message = (estado == "AP" ? "Aprobado" : "Denegado") + " correctamente." });
                }
            }

            // Convierte "12,45,78" -> DataTable TVP
            private static DataTable ToIdListTVP(string csv)
            {
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                foreach (var s in csv.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
                {
                    if (int.TryParse(s, out var id))
                        dt.Rows.Add(id);
                }
                return dt;
            }

            // Obtiene el carnet del usuario actual (ajusta a tu realidad)
            private string GetCarnetActual()
            {
            // 1) Si lo guardas en sesión:
            var emp = Session?["User"] as Entities.Employees;
            if (emp != null && !string.IsNullOrWhiteSpace(emp.EmployeeNumber))
                return emp.EmployeeNumber.Trim(); // ← este es tu carnet

            // 2) (Opcional) Fallback por si la sesión se perdió: intenta por identidad
            var user = (User?.Identity?.Name ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(user))
            {
                using (var cn = new SqlConnection(_cs))
                {
                    // Busca por correo/usuario/carnet
                    var carnet = cn.Query<string>(
                        "SELECT TOP 1 carnet FROM dbo.EMP2024 WITH (NOLOCK) WHERE WorkEmail = @u OR correo = @u OR carnet = @u",
                        new { u = user }
                    ).FirstOrDefault();
                    if (!string.IsNullOrEmpty(carnet))
                        return carnet.Trim();
                }
            }

            // 3) Fallback de pruebas
            return "0";
            }
        }

        // ============================
        // Modelos simples para bind/JSON
        // ============================

        public class EmpleadoBasico
        {
            public string carnet { get; set; }
            public string nombre { get; set; }
            public string gerencia { get; set; }
            public string area { get; set; }
            public string telefono { get; set; }
        }

        public class HeItem
        {
            public int IdHE { get; set; }
            public string Carnet { get; set; }
            public DateTime Fecha { get; set; }
            public TimeSpan HoraInicio { get; set; }
            public TimeSpan HoraFin { get; set; }
            public string Servicio { get; set; }
            public string Razon { get; set; }
            public string Estado { get; set; }
            public string Notas { get; set; }
        }

        public class HeCreateDto
        {
            public string Carnet { get; set; }        // del empleado
            public DateTime Fecha { get; set; }
            public TimeSpan HoraInicio { get; set; }
            public TimeSpan HoraFin { get; set; }
            public string Servicio { get; set; }
            public string Razon { get; set; }
            public string Notas { get; set; }
        }

        public class HeEditDto : HeCreateDto
        {
            public int IdHE { get; set; }
        }
    }
 