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
    public class IncidenteController : Controller
    {
        // GET: Incidente
        public ActionResult Index()
        {
            return View();
        }

        // GET: Incidente/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Incidente/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Incidente/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Incidente/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Incidente/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Incidente/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Incidente/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        public class EmpleadoMin
        {
            public int carnet { get; set; }
            public string nombre_completo { get; set; }
        }
        public class EmpleadoMinVM
        {
            public int Carnet { get; set; }
            public string NombreCompleto { get; set; }
        }
        public class ResponsableMin
        {
            public int IdResponsable { get; set; }
            public string Nombre { get; set; }
        }

        public class IncidenteFilaVM
        {
            public int IdIncidente { get; set; }
            public DateTime? Fecha { get; set; }
            // Hora viene TIME(0) => lo traemos como string "HH:mm:ss" y lo formateamos en la vista
            public string Hora { get; set; }
            public int Carnet { get; set; }
            public string NombreEmpleado { get; set; }
            public string Genero { get; set; }
            public string Departamento_Accidente { get; set; }
            public string Edificio { get; set; }
            public string Lugar { get; set; }
            public string Sub_Tipo { get; set; }
            public decimal? Dias_Baja { get; set; }
            public string Gravedad { get; set; }
            public string Parte_Cuerpo { get; set; }
            public string Agente_Material { get; set; }
            public string Acto_Condicion { get; set; }
            public string Causa_Raiz { get; set; }
            public string Descripcion { get; set; }
            public int EstadoDocs { get; set; }

            // (opcionales devueltos por SP Listar; no usados por la tabla, Dapper los ignora si no existen)
            public int? IdResponsable { get; set; }
            public string ResponsableNombre { get; set; }
        }

        public class IncidenteEditVM
        {
            public int? IdIncidente { get; set; }
            public int Carnet { get; set; }
            public DateTime? Fecha { get; set; }
            public string Hora { get; set; }               // "HH:mm"
            public string Departamento_Accidente { get; set; }
            public string Edificio { get; set; }
            public string Lugar { get; set; }
            public string Sub_Tipo { get; set; }
            public decimal? Dias_Baja { get; set; }
            public string Gravedad { get; set; }
            public string Parte_Cuerpo { get; set; }
            public string Agente_Material { get; set; }
            public string Acto_Condicion { get; set; }
            public string Causa_Raiz { get; set; }
            public string Descripcion { get; set; }

            public int? IdResponsable { get; set; }        // << nuevo
        }

        // ===== Helpers =====
        private static TimeSpan? TimeSpanParse(string hhmm)
        {
            if (string.IsNullOrWhiteSpace(hhmm)) return null;
            if (TimeSpan.TryParse(hhmm, out var t)) return t;
            return null;
        }

        private readonly string cn = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog=Incidente;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;";


        /* ------- LISTAR (para DataTable) ------- */
        // ===== Listar (SP ya creado por ti) =====
        [HttpGet]
        public JsonResult Listar()
        {
            using (var db = new SqlConnection(cn))
            {
                // Forzamos Hora -> string para evitar conversion issues
                var rows = db.Query(
                    "dbo.usp_Incidente_Listar",
                    commandType: CommandType.StoredProcedure
                ).Select(r => new IncidenteFilaVM
                {
                    IdIncidente = r.IdIncidente,
                    Fecha = r.Fecha,
                    Hora = r.Hora is TimeSpan ts ? ts.ToString(@"hh\:mm") : (r.Hora?.ToString().Length >= 5 ? r.Hora.ToString().Substring(0, 5) : (string)null),
                    Carnet = r.Carnet,
                    NombreEmpleado = r.NombreEmpleado,
                    Genero = r.Genero,
                    Departamento_Accidente = r.Departamento_Accidente,
                    Edificio = r.Edificio,
                    Lugar = r.Lugar,
                    Sub_Tipo = r.Sub_Tipo,
                    Dias_Baja = r.Dias_Baja,
                    Gravedad = r.Gravedad,
                    Parte_Cuerpo = r.Parte_Cuerpo,
                    Agente_Material = r.Agente_Material,
                    Acto_Condicion = r.Acto_Condicion,
                    Causa_Raiz = r.Causa_Raiz,
                    Descripcion = r.Descripcion,
                    EstadoDocs = r.EstadoDocs,
                    IdResponsable = (int?)r.IdResponsable,
                    ResponsableNombre = r.ResponsableNombre
                }).ToList();

                return Json(new { data = rows }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===== Obtener por id =====
        [HttpGet]
        public JsonResult Obtener(int id)
        {
            using (var db = new SqlConnection(cn))
            {
                var m = db.QueryFirstOrDefault<IncidenteEditVM>(
                    "dbo.usp_Incidente_Obtener",
                    new { IdIncidente = id },
                    commandType: CommandType.StoredProcedure
                );

                return Json(new { success = m != null, data = m }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===== Guardar (insert) =====
        [HttpPost]
        public JsonResult Guardar(IncidenteEditVM m)
        {
            try
            {
                var gravedad = (m.Dias_Baja.HasValue && m.Dias_Baja.Value >= 8m) ? "Grave" : "Leve";
                using (var db = new SqlConnection(cn))
                {
                    var p = new DynamicParameters();
                    p.Add("@Carnet", m.Carnet);
                    p.Add("@Fecha", m.Fecha);
                    p.Add("@Hora", TimeSpanParse(m.Hora));
                    p.Add("@Departamento_Accidente", m.Departamento_Accidente);
                    p.Add("@Edificio", m.Edificio);
                    p.Add("@Lugar", m.Lugar);
                    p.Add("@Sub_Tipo", m.Sub_Tipo);
                    p.Add("@Dias_Baja", m.Dias_Baja);
                    p.Add("@Gravedad", gravedad);
                    p.Add("@Parte_Cuerpo", m.Parte_Cuerpo);
                    p.Add("@Agente_Material", m.Agente_Material);
                    p.Add("@Acto_Condicion", m.Acto_Condicion);
                    p.Add("@Causa_Raiz", m.Causa_Raiz);
                    p.Add("@Descripcion", m.Descripcion);
                    p.Add("@IdResponsable", m.IdResponsable);             // << nuevo
                    p.Add("@IdNuevo", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    db.Execute("dbo.usp_Incidente_Guardar", p, commandType: CommandType.StoredProcedure);
                    var idNew = p.Get<int>("@IdNuevo");
                    return Json(new { success = true, id = idNew });
                }
            }
            catch (Exception ex) { return Json(new { success = false, msg = ex.Message }); }
        }

        // ===== Editar (update) =====
        [HttpPost]
        public JsonResult Editar(IncidenteEditVM m)
        {
            try
            {
                if (!m.IdIncidente.HasValue) return Json(new { success = false, msg = "Id requerido" });

                var gravedad = (m.Dias_Baja.HasValue && m.Dias_Baja.Value >= 8m) ? "Grave" : "Leve";
                using (var db = new SqlConnection(cn))
                {
                    var p = new DynamicParameters();
                    p.Add("@IdIncidente", m.IdIncidente.Value);
                    p.Add("@Carnet", m.Carnet);
                    p.Add("@Fecha", m.Fecha);
                    p.Add("@Hora", TimeSpanParse(m.Hora));
                    p.Add("@Departamento_Accidente", m.Departamento_Accidente);
                    p.Add("@Edificio", m.Edificio);
                    p.Add("@Lugar", m.Lugar);
                    p.Add("@Sub_Tipo", m.Sub_Tipo);
                    p.Add("@Dias_Baja", m.Dias_Baja);
                    p.Add("@Gravedad", gravedad);
                    p.Add("@Parte_Cuerpo", m.Parte_Cuerpo);
                    p.Add("@Agente_Material", m.Agente_Material);
                    p.Add("@Acto_Condicion", m.Acto_Condicion);
                    p.Add("@Causa_Raiz", m.Causa_Raiz);
                    p.Add("@Descripcion", m.Descripcion);
                    p.Add("@IdResponsable", m.IdResponsable);            // << nuevo

                    db.Execute("dbo.usp_Incidente_Editar", p, commandType: CommandType.StoredProcedure);
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, msg = ex.Message }); }
        }

        // ===== Desactivar =====
        [HttpPost]
        public JsonResult Desactivar(int id)
        {
            try
            {
                using (var db = new SqlConnection(cn))
                {
                    db.Execute("UPDATE dbo.Incidente SET Activo = 0 WHERE IdIncidente = @id", new { id });
                    return Json(new { success = true });
                }
            }
            catch (Exception ex) { return Json(new { success = false, msg = ex.Message }); }
        }

        // ===== Empleados activos (para datalist) =====
        [HttpGet]
        public JsonResult EmpleadoActivosDatalist()
        {
            using (var db = new SqlConnection(cn))
            {
                var lista = db.Query<EmpleadoMin>(@"
SELECT CAST(carnet AS int) AS carnet, nombre_completo
FROM SIGHO1.dbo.EmpleadosVWEF WITH (NOLOCK)
WHERE fechabaja IS NULL
ORDER BY nombre_completo;")
                .Select(x => new EmpleadoMinVM { Carnet = x.carnet, NombreCompleto = x.nombre_completo })
                .ToList();

                return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===== Responsables (para select) =====
        [HttpGet]
        public JsonResult Responsables()
        {
            using (var db = new SqlConnection(cn))
            {
                var lista = db.Query<ResponsableMin>("dbo.usp_Responsable_ListarActivos", commandType: CommandType.StoredProcedure).ToList();
                return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===== Departamentos (estáticos) =====
        [HttpGet]
        public JsonResult Departamentos()
        {
            var deps = new[]
            {
                "MANAGUA","LEON","CHINANDEGA","BOACO","CARAZO","CHONTALES","COSTA CARIBE NORTE",
                "COSTA CARIBE SUR","ESTELI","GRANADA","JINOTEGA","MADRIZ","MASAYA","MATAGALPA","NUEVA SEGOVIA",
                "RIO SAN JUAN","RIVAS"
            };
            return Json(deps, JsonRequestBehavior.AllowGet);
        }

        // ===== Edificios (catálogo fijo que ya agregamos) =====
        [HttpGet]
        public JsonResult Edificios()
        {
            var lista = Catalogos.EdificiosENITEL(); // Usa el helper que definimos antes
            return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        }

        // ===== Documentos (sin cambios relevantes) =====
        [HttpGet]
        public JsonResult Documentos(int idIncidente)
        {
            using (var db = new SqlConnection(cn))
            {
                var rows = db.Query(@"
SELECT IdDocumento, IdIncidente, TipoDocumento, NombreArchivo, MimeType, TamanoBytes, FechaCreacion
FROM dbo.IncidenteDocumento WITH (NOLOCK)
WHERE IdIncidente = @id", new { id = idIncidente }).ToList();
                return Json(new { data = rows }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SubirDocumento(int idIncidente, int tipo, HttpPostedFileBase archivo)
        {
            if (archivo == null || archivo.ContentLength <= 0)
                return Json(new { success = false, msg = "Archivo requerido" });

            try
            {
                byte[] bin;
                using (var ms = new System.IO.MemoryStream())
                {
                    archivo.InputStream.CopyTo(ms);
                    bin = ms.ToArray();
                }

                using (var db = new SqlConnection(cn))
                {
                    var p = new DynamicParameters();
                    p.Add("@IdIncidente", idIncidente);
                    p.Add("@TipoDocumento", tipo.ToString()); // en tabla es NVARCHAR(100)
                    p.Add("@NombreArchivo", archivo.FileName);
                    p.Add("@MimeType", archivo.ContentType);
                    p.Add("@TamanoBytes", (long)bin.Length);
                    p.Add("@Contenido", bin, DbType.Binary);

                    db.Execute(@"
MERGE dbo.IncidenteDocumento AS T
USING (SELECT @IdIncidente AS IdIncidente, @TipoDocumento AS TipoDocumento) AS S
   ON T.IdIncidente = S.IdIncidente AND T.TipoDocumento = S.TipoDocumento
WHEN MATCHED THEN
   UPDATE SET NombreArchivo=@NombreArchivo, MimeType=@MimeType, TamanoBytes=@TamanoBytes, Contenido=@Contenido, FechaCreacion=SYSUTCDATETIME()
WHEN NOT MATCHED THEN
   INSERT(IdIncidente,TipoDocumento,NombreArchivo,MimeType,TamanoBytes,Contenido,FechaCreacion)
   VALUES(@IdIncidente,@TipoDocumento,@NombreArchivo,@MimeType,@TamanoBytes,@Contenido,SYSUTCDATETIME());", p);
                }

                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, msg = ex.Message }); }
        }

        [HttpGet]
        public FileResult VerDocumento(int idIncidente, string tipo)
        {
            using (var db = new SqlConnection(cn))
            {
                var row = db.QueryFirstOrDefault(@"
SELECT TOP(1) NombreArchivo, MimeType, Contenido
FROM dbo.IncidenteDocumento WITH (NOLOCK)
WHERE IdIncidente=@id AND TipoDocumento=@tipo",
                    new { id = idIncidente, tipo = tipo });

                if (row == null) return null;
                string filename = row.NombreArchivo ?? $"doc_{idIncidente}_{tipo}.bin";
                string mime = row.MimeType ?? "application/octet-stream";
                byte[] bin = row.Contenido as byte[];
                return File(bin, mime, filename);
            }
        }

        // ===== Helper catálogo Edificios =====
        static class Catalogos
        {
            public static List<string> EdificiosENITEL() => new List<string>{
"ENITEL METROCENTRO","ENITEL SAN JORGE","ENITEL CAMOAPA","ENITEL CHINANDEGA","ENITEL RIVAS",
"ENITEL MASATEPE","ENITEL SAN ISIDRO","ENITEL MATAGALPA 2","ENITEL ALTAGRACIA","ENITEL PUEBLO NUEVO",
"ENITEL EL RAMA","ENITEL CORN ISLANDS","ENITEL SANTO DOMINGO","ENITEL MUY MUY","ENITEL SEBACO",
"ENITEL SIUNA 2","ENITEL MULTICENTRO ESTELI","ENITEL NAGAROTE","ENITEL SAN CARLOS","ENITEL SABANA GRANDE",
"ENITEL SAN PEDRO DE LOVAGO","ENITEL MATIGUAS","ENITEL ESTACION TERRENA","ENITEL SANTA LUCIA","ENITEL PALACAGUINA",
"ENITEL TEUSTEPE","ENITEL NANDAIME","ENITEL CHICHIGALPA","ENITEL VERACRUZ/P.OFINOVA","ENITEL SIUNA",
"ESTESA EDIFICIO CENTRAL","ENITEL CARRETERA NORTE","ENITEL CETEL","ENITEL LA CONQUISTA","ENITEL LA TRINIDAD",
"ENITEL BOACO","ENITEL TELICA","ENITEL PLAZA ESPAÑA","ENITEL LA LIBERTAD","ENITEL MULTICENTRO LAS AMERICAS",
"ENITEL CARRETERA MASAYA ALMACEN","ENITEL CATARINA","ENITEL CASA PROTOCOLO(STO.DGO MGA)","ENITEL ACOYAPA","ENITEL JUIGALPA",
"ENITEL GRANADA","ENITEL BLUEFIELDS","ENITEL PLAZA LA LIGA","ENITEL BELEN","ENITEL PUERTO CABEZAS",
"ENITEL LA SABANA","ENITEL MASAYA","ENITEL VILLA FONTANA","ENITEL MASAYA MOVIL","ENITEL OFIBODEGAS SAN JOSE",
"ENITEL MUELLE DE LOS BUEYES","ENITEL POTOSI","ENITEL CIUDAD JARDIN","ENITEL ALTAMIRA","ENITEL LAS PIEDRECITAS",
"ENITEL NUEVA GUINEA","ENITEL TIPITAPA","ENITEL DIRIAMBA","ENITEL CONDEGA","ENITEL LAS COLINAS",
"ENITEL CORINTO","ENITEL TOLA","ENITEL CRISTIAN PEREZ","ENITEL ESTELI","ENITEL PATIO CABLE",
"ENITEL QUILALI","ENITEL CIUDAD SANDINO","ENITEL CARR.MASAYA /COBIRSA","ENITEL GALERIA SANTO DOMINGO","ENITEL 14 DE SEPTIEMBRE",
"ENITEL CARAZO","ENITEL SOMOTO","ENITEL EL JICARO","ENITEL BELLO HORIZONTE","ENITEL SAN JUAN DEL SUR",
"ENITEL EL VIEJO","ENITEL DIRIOMO","ENITEL JINOTEGA","ENITEL LA PAZ CENTRO","ENITEL OCOTAL",
"ENITEL SOMOTILLO","ENITEL JINOTEPE","ENITEL SAN RAFAEL DEL NORTE","ENITEL PLAZA MAYOR","ENITEL SAN MARCOS",
"ENITEL JALAPA","ENITEL CHINANDEGA 2","ENITEL LAS PALMAS","ENITEL PLAZA REAL","ENITEL LEON",
"ENITEL WASPAN","ENITEL RIO BLANCO","ENITEL SANTO TOMAS","ESTESA BASE II","ENITEL ESTELI / BODEGA TECNICA",
"ENITEL ACHUAPA","ENITEL NUEVA SEGOVIA","ENITEL EL SAUCE","ENITEL BILWI","ENITEL LAGUNA DE PERLAS",
"ENITEL PUERTO SANDINO","ENITEL YALI","ENITEL MALPAISILLO","ENITEL MONSEÑOR LEZCANO","ENITEL ROSITA",
"ENITEL BONANZAS","ENITEL MATAGALPA","ENITEL MOYOGALPA","ENITEL DARIO","ENITEL TICOMO"
            };
        }

    }
}
