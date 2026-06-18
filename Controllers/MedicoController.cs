using ClosedXML.Excel;
using Dapper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
 
namespace slnRhonline.Controllers
{
    public class MedicoController : Controller
    {
        private IDbConnection Conn()
        {
            var cs = Cadena;
            return new SqlConnection(cs);
        }
        public ActionResult Historial()
        {
            return View();
        }
     

        // GET: Medico
        public ActionResult Index()
        {
            return View();
        }
         public readonly string Cadena = "Data Source=192.168.8.234;Initial Catalog=Medico;User ID=sarh;Password=ktSrW2n_4pR7;MultipleActiveResultSets=True;Connection Timeout=60;";
        public class EncuestaPost
        {
            public string Ruta { get; set; }
            public string Modalidad { get; set; }
            public string AptoLaboral { get; set; }          // "true"/"false"
            public string AlergiasActivas { get; set; }      // "true"/"false"
            public string AlergiasDescripcion { get; set; }
            public string Triage { get; set; }
            public string DatosExtraJSON { get; set; }
            public string Comentario { get; set; }
        }
        public class DetalleJsonDto
        {
            // Encabezado
            public int IdEncuestaRegistro { get; set; }
            public DateTime FechaRegistro { get; set; }
            public string Carnet { get; set; }
            public string Nombre { get; set; }
            public string Gerencia { get; set; }
            public string Area { get; set; }
            public string Ruta { get; set; }
            public string Modalidad { get; set; }
            public bool? AptoLaboral { get; set; }
            public string Triage { get; set; }
            public string Comentario { get; set; }

            // Extras “bonitos”
            public List<string> Categorias { get; set; }
            public List<string> Sintomas { get; set; }
            public List<string> Insumos { get; set; }
            public List<DetallePlano> Detalles { get; set; }

            // Raw pretty
            public string DatosExtraPretty { get; set; }
        }

        // ========
        public class DetallePlano
        {
            public string SintomaKey { get; set; }
             public int? Intensidad { get; set; }
            public string Duracion { get; set; }
            public string Frecuencia { get; set; }
             public string Notas { get; set; }
        }
        public class EncuestaRegistro
        {
            public int IdEncuestaRegistro { get; set; }
            public string Carnet { get; set; }
            public string Nombre { get; set; }
            public string Gerencia { get; set; }
            public string Area { get; set; }
            public DateTime FechaRegistro { get; set; }
            public DateTime FechaDia { get; set; }
            public string Ruta { get; set; }
            public string Modalidad { get; set; }
            public bool? AptoLaboral { get; set; }
            public bool? AlergiasActivas { get; set; }
            public string AlergiasDescripcion { get; set; }
            public string Triage { get; set; }
            public string DatosExtraJSON { get; set; }
            public string Comentario { get; set; }
        }
        private static List<DetallePlano> ParseDetalles(JObject o)
        {
            var list = new List<DetallePlano>();
            if (o == null) return list;

            var detalles = o["Detalles"] as JObject;
            if (detalles == null) return list;

            foreach (var kv in detalles)
            {
                var k = kv.Key;
                var v = kv.Value as JObject;
                if (v == null) continue;

                var fila = new DetallePlano
                {
                    SintomaKey = k,
                     Intensidad = v["Intensidad"] != null ? (int?)v["Intensidad"] : null,
                    Duracion = v["Duracion"] != null ? (string)v["Duracion"] : null,
                    Frecuencia = v["Frecuencia"] != null ? (string)v["Frecuencia"] : null,
                  
                    Notas = v["Notas"] != null ? (string)v["Notas"] : null
                };
                list.Add(fila);
            }
            return list;
        }
        private static DetalleJsonDto MapToDetalleDto(EncuestaRegistro m)
        {
            var dto = new DetalleJsonDto
            {
                IdEncuestaRegistro = m.IdEncuestaRegistro,
                FechaRegistro = m.FechaRegistro,
                Carnet = m.Carnet,
                Nombre = m.Nombre,
                Gerencia = m.Gerencia,
                Area = m.Area,
                Ruta = m.Ruta,
                Modalidad = m.Modalidad,
                AptoLaboral = m.AptoLaboral,
                Triage = m.Triage,
                Comentario = m.Comentario,
                Categorias = new List<string>(),
                Sintomas = new List<string>(),
                Insumos = new List<string>(),
                Detalles = new List<DetallePlano>(),
                DatosExtraPretty = null
            };

            if (!string.IsNullOrWhiteSpace(m.DatosExtraJSON))
            {
                try
                {
                    var o = JObject.Parse(m.DatosExtraJSON);

                    var cat = o["Categorias"] as JArray;
                    if (cat != null) dto.Categorias = cat.Select(x => (string)x).ToList();

                    var sin = o["Sintomas"] as JArray;
                    if (sin != null) dto.Sintomas = sin.Select(x => (string)x).ToList();

                    var ins = o["Insumos"] as JArray;
                    if (ins != null) dto.Insumos = ins.Select(x => (string)x).ToList();

                    dto.Detalles = ParseDetalles(o);
                    dto.DatosExtraPretty = o.ToString(Formatting.Indented);
                }
                catch { /* ignora errores de parseo */ }
            }
            return dto;
        }

        // ==== API SIMULADA DE EMPLEADO ====
        [HttpGet]
        public JsonResult ApiEmpleadoActual()
        {
            var u = Session["User"] as Entities.Employees;

            // Toma datos de sesión real; aquí fijo para demo
            var empCarnet = u.EmployeeNumber;
            var empNombre = u.FullName;
            var empGerencia = u.GERENCIA;
            var empArea = u.area;
            // Reemplaza con tu sesión real
            var emp = new
            {
                Carnet = empCarnet,
                Nombre = empNombre,
                Gerencia = empGerencia,
                Area = empArea
            };
            return Json(new { ok = true, data = emp }, JsonRequestBehavior.AllowGet);
        }

        // ==== VERIFICAR HOY ====
        [HttpGet]
        public JsonResult VerificarEncuestaHoy(string carnet)
        {
            using (var cn = Conn())
            {
                var row = cn.QueryFirstOrDefault(
                    "dbo.usp_Encuesta_VerificarHoy",
                    new { Carnet = carnet },
                    commandType: CommandType.StoredProcedure);

                return Json(row ?? new { Id = 0, YaRespondio = false }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==== GUARDAR (POST) ====
        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult Guardar(EncuestaPost m)
        {
            try
            {
                var u = Session["User"] as Entities.Employees;

                // Toma datos de sesión real; aquí fijo para demo
                var empCarnet = u.EmployeeNumber;
                var empNombre = u.FullName;
                var empGerencia = u.GERENCIA;
                var empArea =u.area;

                bool? apto = ParseBool(m.AptoLaboral);
                bool? alerg = ParseBool(m.AlergiasActivas);

                int id = 0;
                using (var cn = Conn())
                {
                    var p = new DynamicParameters();
                    p.Add("@Carnet", empCarnet);
                    p.Add("@Nombre", empNombre);
                    p.Add("@Gerencia", empGerencia);
                    p.Add("@Area", empArea);
                    p.Add("@Ruta", m.Ruta);
                    p.Add("@Modalidad", m.Modalidad);
                    p.Add("@AptoLaboral", apto);
                    p.Add("@AlergiasActivas", alerg);
                    p.Add("@AlergiasDescripcion", m.AlergiasDescripcion);
                    p.Add("@Triage", m.Triage);
                    p.Add("@DatosExtraJSON", m.DatosExtraJSON);
                    p.Add("@Comentario", m.Comentario);
                    p.Add("@Ip", Request.UserHostAddress);
                    p.Add("@UserAgent", Request.UserAgent);
                    p.Add("@IdEncuesta", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    cn.Execute("dbo.usp_Encuesta_Guardar", p, commandType: CommandType.StoredProcedure);
                    id = p.Get<int>("@IdEncuesta");
                }

                return Json(new { ok = true, id = id, mensaje = "Registro guardado correctamente." });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500; // importante para que jQuery entre a error
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }

        private bool? ParseBool(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }
        [HttpGet]
         public ActionResult DetalleExcel(int id)
        {
            using (var cn = Conn())
            {
                var m = cn.QueryFirstOrDefault<EncuestaRegistro>(
                    "dbo.usp_Encuesta_Obtener",
                    new { IdEncuesta = id },
                    commandType: CommandType.StoredProcedure);

                if (m == null) return HttpNotFound();

                var dto = MapToDetalleDto(m);

                using (var wb = new XLWorkbook())
                {
                    // Hoja 1: Encabezado
                    var sh = wb.AddWorksheet("Encabezado");
                    int r = 1;
                    Put(sh, r++, "Fecha", dto.FechaRegistro.ToString("dd/MM/yyyy HH:mm"));
                    Put(sh, r++, "Carnet", dto.Carnet);
                    Put(sh, r++, "Nombre", dto.Nombre);
                    Put(sh, r++, "Gerencia", dto.Gerencia);
                    Put(sh, r++, "Área", dto.Area);
                     Put(sh, r++, "Modalidad", dto.Modalidad);
                    Put(sh, r++, "Apto laboral", dto.AptoLaboral.HasValue && dto.AptoLaboral.Value ? "Sí" : "No");
                     Put(sh, r++, "Comentario", dto.Comentario);

                    // Hoja 2: Listas
                    var sh2 = wb.AddWorksheet("Listas");
                    sh2.Cell(1, 1).Value = "Categorías";
                    sh2.Cell(1, 2).Value = "Síntomas";
                    sh2.Cell(1, 3).Value = "Insumos";
                    int n = Math.Max(Math.Max(dto.Categorias != null ? dto.Categorias.Count : 0, dto.Sintomas != null ? dto.Sintomas.Count : 0), dto.Insumos != null ? dto.Insumos.Count : 0);
                    for (int i = 0; i < n; i++)
                    {
                        sh2.Cell(i + 2, 1).Value = SafeList(dto.Categorias, i);
                        sh2.Cell(i + 2, 2).Value = SafeList(dto.Sintomas, i);
                        sh2.Cell(i + 2, 3).Value = SafeList(dto.Insumos, i);
                    }

                    // Hoja 3: Detalles
                    var sh3 = wb.AddWorksheet("Detalles");
                    sh3.Cell(1, 1).Value = "Síntoma (key)";
                     sh3.Cell(1, 3).Value = "Intensidad";
                    sh3.Cell(1, 4).Value = "Duración";
                    sh3.Cell(1, 5).Value = "Frecuencia";
                     sh3.Cell(1, 7).Value = "Notas";

                    int rr = 2;
                    if (dto.Detalles != null)
                    {
                        foreach (var x in dto.Detalles)
                        {
                            sh3.Cell(rr, 1).Value = x.SintomaKey;
                             sh3.Cell(rr, 3).Value = x.Intensidad.HasValue ? (object)x.Intensidad.Value : "";
                            sh3.Cell(rr, 4).Value = x.Duracion;
                            sh3.Cell(rr, 5).Value = x.Frecuencia;
                             sh3.Cell(rr, 7).Value = x.Notas;
                            rr++;
                        }
                    }

                    using (var ms = new System.IO.MemoryStream())
                    {
                        wb.SaveAs(ms);
                        var bytes = ms.ToArray();
                        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "Detalle_Chequeo.xlsx");
                    }
                }
            }
        }
        [HttpGet]
        public ActionResult RangoDetalleExcel(DateTime? desde, DateTime? hasta)
        {
            var carnet = "1010426"; // TODO: Session
            DateTime? d1 = desde.HasValue ? (DateTime?)desde.Value.Date : null;
            DateTime? d2 = hasta.HasValue ? (DateTime?)hasta.Value.Date : null;

            using (var cn = Conn())
            {
                var p = new DynamicParameters();
                p.Add("@Carnet", carnet, DbType.String);
                p.Add("@Desde", d1, DbType.Date);
                p.Add("@Hasta", d2, DbType.Date);

                var list = cn.Query<EncuestaRegistro>(
                    "dbo.usp_Encuesta_ListarPorCarnet",
                    p,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                using (var wb = new XLWorkbook())
                {
                    // Hoja: Detalle plano por síntoma (una fila por síntoma)
                    var sh = wb.AddWorksheet("DetalleRango");
                    // Encabezados (sin Id)
                    sh.Cell(1, 1).Value = "Fecha";
                    sh.Cell(1, 2).Value = "Carnet";
                    sh.Cell(1, 3).Value = "Nombre";
                    sh.Cell(1, 4).Value = "Gerencia";
                    sh.Cell(1, 5).Value = "Área";
                     sh.Cell(1, 7).Value = "Modalidad";
                    sh.Cell(1, 8).Value = "Apto laboral";
                     sh.Cell(1, 10).Value = "Comentario";
                    sh.Cell(1, 11).Value = "Síntoma (key)";
                     sh.Cell(1, 13).Value = "Intensidad";
                    sh.Cell(1, 14).Value = "Duración";
                    sh.Cell(1, 15).Value = "Frecuencia";
                     sh.Cell(1, 17).Value = "Notas";

                    int r = 2;
                    foreach (var m in list)
                    {
                        var dto = MapToDetalleDto(m);
                        // Si no hay detalles, igualmente una fila sin detalle.
                        var detalles = (dto.Detalles != null && dto.Detalles.Count > 0)
                            ? dto.Detalles
                            : new List<DetallePlano> { new DetallePlano() };

                        foreach (var x in detalles)
                        {
                            sh.Cell(r, 1).Value = dto.FechaRegistro; sh.Cell(r, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                            sh.Cell(r, 2).Value = dto.Carnet;
                            sh.Cell(r, 3).Value = dto.Nombre;
                            sh.Cell(r, 4).Value = dto.Gerencia;
                            sh.Cell(r, 5).Value = dto.Area;
                             sh.Cell(r, 7).Value = dto.Modalidad;
                            sh.Cell(r, 8).Value = dto.AptoLaboral.HasValue && dto.AptoLaboral.Value ? "Sí" : "No";
                             sh.Cell(r, 10).Value = dto.Comentario;
                            sh.Cell(r, 11).Value = x.SintomaKey;
                             sh.Cell(r, 13).Value = x.Intensidad.HasValue ? (object)x.Intensidad.Value : "";
                            sh.Cell(r, 14).Value = x.Duracion;
                            sh.Cell(r, 15).Value = x.Frecuencia;
                             sh.Cell(r, 17).Value = x.Notas;
                            r++;
                        }
                    }

                    sh.Columns().AdjustToContents();

                    using (var ms = new System.IO.MemoryStream())
                    {
                        wb.SaveAs(ms);
                        var bytes = ms.ToArray();
                        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "Detalle_Chequeo_Rango.xlsx");
                    }
                }
            }
        }

        // ================== UTILS EXCEL ==================
        private static void Put(IXLWorksheet sh, int row, string label, string value)
        {
            sh.Cell(row, 1).Value = label;
            sh.Cell(row, 2).Value = value ?? "";
            sh.Cell(row, 1).Style.Font.Bold = true;
        }

        private static string SafeList(List<string> list, int idx)
        {
            if (list == null) return "";
            if (idx < 0 || idx >= list.Count) return "";
            return list[idx] ?? "";
        }        // ===== Historial (solo endpoints si los usas en otra vista) =====
        [HttpGet]
        public JsonResult ObtenerHistorial(DateTime? desde, DateTime? hasta)
        {
            var carnet = "1010426"; // TODO: tomar de Session

            // Normaliza a solo fecha (evita tiempos)
            DateTime? d1 = desde?.Date;
            DateTime? d2 = hasta?.Date;

            using (var cn = Conn())
            {
                var p = new DynamicParameters();
                p.Add("@Carnet", carnet, dbType: DbType.String);
                p.Add("@Desde", d1, dbType: DbType.Date, direction: ParameterDirection.Input);
                p.Add("@Hasta", d2, dbType: DbType.Date, direction: ParameterDirection.Input);

                var list = cn.Query<EncuestaRegistro>(
                    "dbo.usp_Encuesta_ListarPorCarnet",
                    p,
                    commandType: CommandType.StoredProcedure
                );

                return Json(new { data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult DetalleJson(int id)
        {
            using (var cn = Conn())
            {
                var m = cn.QueryFirstOrDefault<EncuestaRegistro>(
                    "dbo.usp_Encuesta_Obtener",
                    new { IdEncuesta = id },
                    commandType: CommandType.StoredProcedure);

                if (m == null)
                    return Json(new { ok = false, mensaje = "No encontrado." }, JsonRequestBehavior.AllowGet);

                var dto = MapToDetalleDto(m);
                return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
            }
        }

        // ========= PDF bonito (HTML) =========
        // Controllers/MedicoController.cs — PDF más elegante, sin Ruta/Triage ni Lado/Desencadenantes
        [HttpGet]
        public ActionResult Pdf(int id)
        {
            using (var cn = Conn())
            {
                var m = cn.QueryFirstOrDefault<EncuestaRegistro>(
                    "dbo.usp_Encuesta_Obtener",
                    new { IdEncuesta = id },
                    commandType: CommandType.StoredProcedure);

                if (m == null) return HttpNotFound();

                // Map DTO (usa tu existente)
                var dto = MapToDetalleDto(m);

                // Stats simples para el “Resumen”
                var totalSintomas = dto.Sintomas != null ? dto.Sintomas.Count : 0;
                var maxSev = (dto.Detalles ?? new List<DetallePlano>())
                             .Select(x => x.Intensidad ?? 0).DefaultIfEmpty(0).Max();
                var estadoTxt = dto.AptoLaboral == true ? "APTO" : "REPOSO";
                var estadoClass = dto.AptoLaboral == true ? "ok" : "warn";

                var sb = new StringBuilder();
                sb.Append("<!doctype html><html><head><meta charset='utf-8'>");
                sb.Append(@"<meta name='viewport' content='width=device-width,initial-scale=1'/>
<style>
:root{
  --rojo:#b30000; --rojo2:#ff4444; --negro:#1a1a1a; --gris:#f5f5f5; --gris2:#ececec; --blanco:#fff; --verde:#60d394;
}
*{box-sizing:border-box}
html,body{background:#f3f4f6;color:var(--negro);font-family:Arial,Helvetica,sans-serif;line-height:1.35;margin:0;padding:0}
.page{max-width:900px;margin:24px auto;padding:0 12px}
.header{
  position:relative;border-radius:18px;overflow:hidden;color:#fff;margin-bottom:16px;
  background:linear-gradient(135deg,var(--rojo),var(--rojo2));
  box-shadow:0 8px 24px rgba(179,0,0,.18);
}
.header-inner{padding:22px 22px 18px 22px;display:flex;align-items:center;justify-content:space-between}
.brand{display:flex;align-items:center;gap:12px}
.logo{
  width:44px;height:44px;border-radius:12px;background:rgba(255,255,255,.12);
  display:flex;align-items:center;justify-content:center;font-weight:900;font-size:18px;letter-spacing:.5px
}
.title{font-size:22px;font-weight:900;letter-spacing:.3px}
.subtitle{opacity:.95;font-size:12px;margin-top:2px}
.ribbon{
  position:absolute;bottom:-1px;left:0;right:0;height:38px;
  background:linear-gradient(0deg, rgba(0,0,0,.06), rgba(0,0,0,0));
  backdrop-filter: blur(1px);
}
.summary{
  display:grid;grid-template-columns:repeat(4,1fr);gap:10px;padding:10px 14px 14px 14px
}
.sum-card{
  background:rgba(255,255,255,.08);border:1px solid rgba(255,255,255,.18);
  border-radius:12px;padding:10px 12px
}
.sum-k{font-size:11px;opacity:.9}
.sum-v{font-size:18px;font-weight:900}
.badge{display:inline-block;border-radius:999px;padding:3px 10px;font-weight:800;font-size:12px}
.badge.ok{background:#d6f5e5;color:#0c5132;border:1px solid #c5efd9}
.badge.warn{background:#fde2e1;color:#7a1f1a;border:1px solid #fbd0ce}

/* Tarjetas base */
.card{
  background:var(--blanco); border:1px solid var(--gris2); border-radius:16px; margin:14px 0;
  box-shadow:0 6px 18px rgba(0,0,0,.06);
}
.card-h{padding:14px 16px;border-bottom:1px solid var(--gris2);display:flex;align-items:center;justify-content:space-between}
.card-h .h{font-weight:900;color:var(--rojo);letter-spacing:.3px}
.card-b{padding:14px 16px}

/* Grilla de info */
.row{display:flex;flex-wrap:wrap;margin:-6px}
.col{flex:0 0 33.33%;padding:6px}
.k{font-weight:800;color:#444;font-size:12px;letter-spacing:.2px}
.v{margin-top:3px}

/* Pills */
.pills{margin-top:6px}
.pill{
  display:inline-block;margin:3px 4px 0 0;padding:5px 10px;border-radius:999px;
  background:#f7f7f7;border:1px solid #e6e6e6;font-size:12px;color:#111;font-weight:700
}

/* Tabla Detalles */
.table{width:100%;border-collapse:separate;border-spacing:0 6px}
.table thead th{
  text-align:left;font-size:12px;color:#555;font-weight:800;padding:8px 10px;
}
.table tbody tr{
  background:#fff;border:1px solid #eee;border-radius:10px;box-shadow:0 3px 12px rgba(0,0,0,.04)
}
.table td{
  padding:10px;border-top:1px solid #f2f2f2;border-bottom:1px solid #f2f2f2;font-size:13px
}
.table td:first-child{border-left:1px solid #f2f2f2;border-top-left-radius:10px;border-bottom-left-radius:10px}
.table td:last-child{border-right:1px solid #f2f2f2;border-top-right-radius:10px;border-bottom-right-radius:10px}

/* Comentario */
.note{white-space:pre-wrap;background:#fcfcfc;border:1px dashed #e5e7eb;border-radius:10px;padding:10px}

/* Footer */
.footer{margin:18px 4px 10px 4px;text-align:right;color:#888;font-size:11px}

/* Print */
@media print{
  body{background:#fff}
  .page{max-width:100%;margin:0}
  .card{box-shadow:none}
}
</style>");
                sb.Append("</head><body><div class='page'>");

                // HEADER
                sb.Append("<div class='header'>");
                sb.Append("<div class='header-inner'>");
                sb.Append("<div class='brand'><div class='logo'>CB</div>");
                sb.Append("<div><div class='title'>Chequeo de Bienestar</div>");
                sb.Append("<div class='subtitle'>").Append(HttpUtility.HtmlEncode(dto.Nombre ?? "")).Append("</div></div></div>");
                sb.Append("<div style='text-align:right'>");
                sb.Append("<div class='subtitle'>").Append(dto.FechaRegistro.ToString("dd/MM/yyyy HH:mm")).Append("</div>");
                sb.Append("<div class='badge ").Append(estadoClass).Append("'>").Append(estadoTxt).Append("</div>");
                sb.Append("</div></div>");
                sb.Append("<div class='summary'>");
                sb.Append(Sum("Modalidad", HttpUtility.HtmlEncode(dto.Modalidad ?? "-")));
                sb.Append(Sum("Síntomas", totalSintomas.ToString()));
                sb.Append(Sum("Intensidad máx.", maxSev.ToString()));
                sb.Append(Sum("Apto laboral", dto.AptoLaboral == true ? "Sí" : "No"));
                sb.Append("</div><div class='Ribbon'></div><div class='ribbon'></div></div>");

                // IDENTIDAD (sin Ruta/Triage)
                sb.Append("<div class='card'><div class='card-h'><div class='h'>Información del colaborador</div></div><div class='card-b'>");
                sb.Append("<div class='row'>");
                sb.Append(Block("Carnet", dto.Carnet));
                sb.Append(Block("Nombre", dto.Nombre));
                sb.Append(Block("Gerencia", dto.Gerencia));
                sb.Append(Block("Área", dto.Area));
                sb.Append(Block("Modalidad", dto.Modalidad));
                sb.Append(Block("Apto laboral", dto.AptoLaboral == true ? "Sí" : "No"));
                sb.Append("</div>");
                sb.Append("<div style='margin-top:8px'><div class='k'>Comentario</div><div class='note'>")
                  .Append(HttpUtility.HtmlEncode(dto.Comentario ?? ""))
                  .Append("</div></div>");
                sb.Append("</div></div>");

                // PASTILLAS: Síntomas / Insumos / Categorías
                sb.Append("<div class='card'><div class='card-h'><div class='h'>Resumen rápido</div></div><div class='card-b'>");
                if (dto.Sintomas != null && dto.Sintomas.Count > 0)
                {
                    sb.Append("<div class='k'>Síntomas</div><div class='pills'>")
                      .Append(PillList(dto.Sintomas)).Append("</div><br/>");
                }
                if (dto.Insumos != null && dto.Insumos.Count > 0)
                {
                    sb.Append("<div class='k'>Insumos</div><div class='pills'>")
                      .Append(PillList(dto.Insumos)).Append("</div><br/>");
                }
                if (dto.Categorias != null && dto.Categorias.Count > 0)
                {
                    sb.Append("<div class='k'>Categorías</div><div class='pills'>")
                      .Append(PillList(dto.Categorias)).Append("</div>");
                }
                if ((dto.Sintomas == null || dto.Sintomas.Count == 0) &&
                    (dto.Insumos == null || dto.Insumos.Count == 0) &&
                    (dto.Categorias == null || dto.Categorias.Count == 0))
                {
                    sb.Append("<div class='v' style='color:#666'>Sin elementos para mostrar.</div>");
                }
                sb.Append("</div></div>");

                // DETALLES (sin Lado/Desencadenantes)
                sb.Append("<div class='card'><div class='card-h'><div class='h'>Detalles de síntomas</div></div><div class='card-b'>");
                if (dto.Detalles != null && dto.Detalles.Count > 0)
                {
                    sb.Append("<table class='table'><thead><tr>")
                      .Append("<th>Síntoma (key)</th><th>Intensidad</th><th>Duración</th><th>Frecuencia</th><th>Notas</th>")
                      .Append("</tr></thead><tbody>");
                    foreach (var x in dto.Detalles)
                    {
                        sb.Append("<tr>")
                          .Append(Td(x.SintomaKey))
                          .Append(Td(x.Intensidad.HasValue ? x.Intensidad.Value.ToString() : ""))
                          .Append(Td(x.Duracion))
                          .Append(Td(x.Frecuencia))
                          .Append(Td(x.Notas))
                          .Append("</tr>");
                    }
                    sb.Append("</tbody></table>");
                }
                else
                {
                    sb.Append("<div class='v' style='color:#666'>Sin detalles de síntomas.</div>");
                }
                sb.Append("</div></div>");

                sb.Append("<div class='footer'>Documento generado automáticamente</div>");
                sb.Append("</div></body></html>");

                return Content(sb.ToString(), "text/html", Encoding.UTF8);
            }
        }
        string Sum(string k, string v)
        {
            return "<div class='sum-card'><div class='sum-k'>" + HttpUtility.HtmlEncode(k) + "</div><div class='sum-v'>" +
                   HttpUtility.HtmlEncode(v ?? "-") + "</div></div>";
        }
        // Lista de pills
        string PillList(IEnumerable<string> arr)
        {
            if (arr == null) return "";
            var sbp = new StringBuilder();
            foreach (var x in arr) sbp.Append("<span class='pill'>").Append(HttpUtility.HtmlEncode(x ?? "")).Append("</span>");
            return sbp.ToString();
        }
        string Td(string v)
        {
            return "<td>" + HttpUtility.HtmlEncode(v ?? "") + "</td>";
        }

        string Block(string label, string value)
        {
            return "<div class='col'><div class='k'>" + HttpUtility.HtmlEncode(label) +
                   "</div><div class='v'>" + HttpUtility.HtmlEncode(value ?? "") + "</div></div>";
        }
       
        private string Safe(string s) { return s ?? ""; }
        private string BuildResumenPlano(EncuestaRegistro r)
        {
            var sb = new StringBuilder();
            try
            {
                if (string.IsNullOrWhiteSpace(r.DatosExtraJSON)) return "(Sin detalles)";

                var jo = JObject.Parse(r.DatosExtraJSON);

                var categorias = jo["Categorias"] != null ? string.Join(", ", jo["Categorias"].ToObject<string[]>()) : "";
                var sintomas = jo["Sintomas"] != null ? string.Join(", ", jo["Sintomas"].ToObject<string[]>()) : "";

                if (!string.IsNullOrEmpty(categorias))
                    sb.AppendLine("Categorías: " + categorias);
                if (!string.IsNullOrEmpty(sintomas))
                    sb.AppendLine("Síntomas: " + sintomas);

                var det = jo["Detalles"] as JObject;
                if (det != null)
                {
                    sb.AppendLine("\nDetalles por síntoma:");
                    foreach (var p in det)
                    {
                        var line = new StringBuilder(" - " + p.Key + ": ");
                        var d = p.Value as JObject;
                        if (d != null)
                        {
                            if (d["Lado"] != null) line.Append("Lado=" + d["Lado"] + " ");
                            if (d["Intensidad"] != null) line.Append("Intensidad=" + d["Intensidad"] + "/10 ");
                            if (d["Duracion"] != null) line.Append("Duración=" + d["Duracion"] + " ");
                            if (d["Frecuencia"] != null) line.Append("Frecuencia=" + d["Frecuencia"] + " ");
                            if (d["Desencadenantes"] != null) line.Append("Desencadenantes=" + string.Join(",", d["Desencadenantes"].ToObject<string[]>()) + " ");
                            if (d["Notas"] != null) line.Append("Notas=\"" + d["Notas"] + "\" ");
                        }
                        sb.AppendLine(line.ToString().Trim());
                    }
                }

                var al = jo["Alergia"] as JObject;
                if (al != null)
                {
                    var act = al["Activa"] != null && al["Activa"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
                    sb.AppendLine("\nAlergia activa: " + (act ? "Sí" : "No"));
                    if (al["Descripcion"] != null) sb.AppendLine("Alergia detalle: " + al["Descripcion"]);
                }

                var hab = jo["Habitos"] as JObject;
                if (hab != null)
                {
                    if (hab["Sueno"] != null) sb.AppendLine("Sueño: " + hab["Sueno"]);
                    if (hab["Hidratacion"] != null) sb.AppendLine("Hidratación: " + hab["Hidratacion"]);
                }

                var ins = jo["Insumos"] != null ? string.Join(", ", jo["Insumos"].ToObject<string[]>()) : "";
                if (!string.IsNullOrEmpty(ins)) sb.AppendLine("Insumos en área: " + ins);
            }
            catch
            {
                sb.AppendLine("(No fue posible analizar el detalle JSON)");
            }
            return sb.ToString();
        }
        [HttpGet]
        public ActionResult Detalle(int id)
        {
            using (var cn = Conn())
            {
                var m = cn.QueryFirstOrDefault<EncuestaRegistro>(
                    "dbo.usp_Encuesta_Obtener",
                    new { IdEncuesta = id },
                    commandType: CommandType.StoredProcedure);
                if (m == null) return HttpNotFound();
                return View(m);
            }
        }
    }
}