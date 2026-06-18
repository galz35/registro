using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Dapper;
using Entities;

namespace slnRhonline.Controllers
{
    public class RLaboralesController : Controller
    {
        private readonly string _cs = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SARH; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;";

        // ───── DTOs ─────────────────────────────────────────────────
        public class CatalogoItem { public int Id { get; set; } public string Descripcion { get; set; } }
        public class EmpleadoSearch { public string carnet { get; set; } public string nombre_completo { get; set; } public string cargo { get; set; } public string Area { get; set; } public string Gerencia { get; set; } }

        public class VwSancionItem
        {
            public int Id { get; set; }
            public string nombre_completo { get; set; }
            public string carnet { get; set; }
            public string Cargo { get; set; }
            public string Gerencia { get; set; }
            public string Area { get; set; }
            public string Descripcion { get; set; }
            public string Grado { get; set; }
            public string SancionAplicar { get; set; }
            public string Reincidencia { get; set; }
            public string Observacion { get; set; }
            public string Minutos { get; set; }
            public string Categorias { get; set; }
            public double? Costo { get; set; }
            public DateTime? Fechainicio { get; set; }
            public DateTime? fechafin { get; set; }
            public string Jefe { get; set; }
            public string Archivo { get; set; }
        }

        public class VwActaItem
        {
            public int Id { get; set; }
            public string nombre_completo { get; set; }
            public string carnet { get; set; }
            public string Cargo { get; set; }
            public string Gerencia { get; set; }
            public string Jefe { get; set; }
            public string Categoria { get; set; }
            public string Observacion { get; set; }
            public double? Monto { get; set; }
            public string Archivo { get; set; }
            public DateTime? Fechaini { get; set; }
            public DateTime? Fechafin { get; set; }
        }

        // ───── VISTA ────────────────────────────────────────────────
        public ActionResult Index()
        {
            return View();
        }

        // ───── CATALOGOS ────────────────────────────────────────────
        [HttpGet]
        public JsonResult ObtenerCatalogos()
        {
            using (var db = new SqlConnection(_cs))
            {
                var all = db.Query<CatalogoItem>("SELECT Id, Descripcion FROM TblSanciones ORDER BY Descripcion").ToList();
                var codigos = db.Query("SELECT Id, Descripcion, Codigo FROM TblSanciones ORDER BY Descripcion").ToList();

                var tiposFalta = codigos.Where(c => (int)c.Codigo == 2).Select(c => new CatalogoItem { Id = (int)c.Id, Descripcion = (string)c.Descripcion }).ToList();
                var gravedad = codigos.Where(c => (int)c.Codigo == 1).Select(c => new CatalogoItem { Id = (int)c.Id, Descripcion = (string)c.Descripcion }).ToList();
                var reincidencia = codigos.Where(c => (int)c.Codigo == 3).Select(c => new CatalogoItem { Id = (int)c.Id, Descripcion = (string)c.Descripcion }).ToList();

                return Json(new { tiposFalta, gravedad, reincidencia }, JsonRequestBehavior.AllowGet);
            }
        }

        // ───── BUSCAR EMPLEADO ──────────────────────────────────────
        [HttpGet]
        public JsonResult BuscarEmpleado(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<EmpleadoSearch>(), JsonRequestBehavior.AllowGet);
            using (var db = new SqlConnection(_cs))
            {
                var sql = @"SELECT DISTINCT TOP 20 carnet, nombre_completo, cargo, primernivel AS Area, OGERENCIA AS Gerencia
                            FROM EmpleadoHCM
                            WHERE nombre_completo LIKE '%' + @term + '%' OR carnet LIKE '%' + @term + '%'";
                var data = db.Query<EmpleadoSearch>(sql, new { term }).ToList();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
        }

        // ───── LISTAR ───────────────────────────────────────────────
        [HttpGet]
        public JsonResult ListarSanciones()
        {
            using (var db = new SqlConnection(_cs))
            {
                var data = db.Query<VwSancionItem>("SELECT * FROM vwEmpS ORDER BY Fechainicio DESC").ToList();
                return Json(new { data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ListarActas()
        {
            using (var db = new SqlConnection(_cs))
            {
                var data = db.Query<VwActaItem>("SELECT * FROM vwEmpA ORDER BY Fechaini DESC").ToList();
                return Json(new { data }, JsonRequestBehavior.AllowGet);
            }
        }

        // ───── CREAR SANCION ────────────────────────────────────────
        [HttpPost]
        public JsonResult CrearSancion(string Carnet, string TipoDeFata, string Grado, string SancionAplicar,
            string Reincidencia, string Categorias, string Minutos, double? Costo, string Observacion,
            DateTime? FechaInicio, DateTime? FechaFin)
        {
            try
            {
                var u = Session["User"] as Employees;
                var usuario = u != null ? u.FullName : "Sistema";

                using (var db = new SqlConnection(_cs))
                {
                    // Obtener datos del empleado
                    var emp = db.QueryFirstOrDefault<EmpleadoSearch>(
                        "SELECT DISTINCT TOP 1 carnet, nombre_completo, cargo, primernivel AS Area, OGERENCIA AS Gerencia FROM EmpleadoHCM WHERE carnet = @Carnet",
                        new { Carnet });

                    var sql = @"INSERT INTO TbSancionEmpleado 
                        (Carnet, TipoDeFata, Grado, SancionAplicar, Reincidencia, Categorias, Minutos, Costo, Observacion, Fechainicio, fechafin, Fecharegistro, Usuario, Cargo, Area, Gerencia)
                        VALUES (@Carnet, @TipoDeFata, @Grado, @SancionAplicar, @Reincidencia, @Categorias, @Minutos, @Costo, @Observacion, @FechaInicio, @FechaFin, GETDATE(), @Usuario, @Cargo, @Area, @Gerencia);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var id = db.QuerySingle<int>(sql, new
                    {
                        Carnet,
                        TipoDeFata = TipoDeFata ?? "",
                        Grado = Grado ?? "",
                        SancionAplicar = SancionAplicar ?? "",
                        Reincidencia = Reincidencia ?? "",
                        Categorias = Categorias ?? "",
                        Minutos = Minutos ?? "",
                        Costo = Costo ?? 0,
                        Observacion = Observacion ?? "",
                        FechaInicio,
                        FechaFin,
                        Usuario = usuario,
                        Cargo = emp?.cargo ?? "",
                        Area = emp?.Area ?? "",
                        Gerencia = emp?.Gerencia ?? ""
                    });

                    return Json(new { success = true, id, message = "Sancion registrada exitosamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ───── CREAR ACTA ───────────────────────────────────────────
        [HttpPost]
        public JsonResult CrearActa(string Empleado, string Categoria, double? Monto, string Observacion,
            DateTime? FechaIni, DateTime? FechaFin)
        {
            try
            {
                var u = Session["User"] as Employees;
                var usuario = u != null ? u.FullName : "Sistema";

                using (var db = new SqlConnection(_cs))
                {
                    var emp = db.QueryFirstOrDefault<EmpleadoSearch>(
                        "SELECT DISTINCT TOP 1 carnet, nombre_completo, cargo, primernivel AS Area, OGERENCIA AS Gerencia FROM EmpleadoHCM WHERE carnet = @Empleado",
                        new { Empleado });

                    var sql = @"INSERT INTO TblActa 
                        (Empleado, Categoria, Monto, Observacion, Fechaini, Fechafin, Fecharegistro, Usuariog, Cargo, Gerencia)
                        VALUES (@Empleado, @Categoria, @Monto, @Observacion, @FechaIni, @FechaFin, GETDATE(), @Usuario, @Cargo, @Gerencia);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var id = db.QuerySingle<int>(sql, new
                    {
                        Empleado,
                        Categoria = Categoria ?? "",
                        Monto = Monto ?? 0,
                        Observacion = Observacion ?? "",
                        FechaIni,
                        FechaFin,
                        Usuario = usuario,
                        Cargo = emp?.cargo ?? "",
                        Gerencia = emp?.Gerencia ?? ""
                    });

                    return Json(new { success = true, id, message = "Acta registrada exitosamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ───── ELIMINAR ─────────────────────────────────────────────
        [HttpPost]
        public JsonResult EliminarSancion(int id)
        {
            try
            {
                using (var db = new SqlConnection(_cs))
                {
                    db.Execute("DELETE FROM tblSacionImagen WHERE IdSancion = @id AND Tipo = 1", new { id });
                    db.Execute("DELETE FROM TbSancionEmpleado WHERE Id = @id", new { id });
                    return Json(new { success = true, message = "Sancion eliminada." });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EliminarActa(int id)
        {
            try
            {
                using (var db = new SqlConnection(_cs))
                {
                    db.Execute("DELETE FROM tblSacionImagen WHERE IdSancion = @id AND Tipo = 2", new { id });
                    db.Execute("DELETE FROM TblActa WHERE Id = @id", new { id });
                    return Json(new { success = true, message = "Acta eliminada." });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ───── DOCUMENTOS (SUBIR) ───────────────────────────────────
        [HttpPost]
        public JsonResult SubirDocumento(int id, int tipo, string carnet)
        {
            try
            {
                if (Request.Files.Count == 0) return Json(new { success = false, message = "No se envio ningun archivo." });
                var file = Request.Files[0];

                byte[] pdfBytes;
                string fileName = file.FileName;

                // Si es imagen, redimensionar y convertir a JPEG optimizado
                string ext = Path.GetExtension(fileName).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                {
                    using (var img = Image.FromStream(file.InputStream))
                    {
                        int maxLado = 1600;
                        double factor = Math.Min(1.0, Math.Min((double)maxLado / img.Width, (double)maxLado / img.Height));
                        int newW = (int)(img.Width * factor);
                        int newH = (int)(img.Height * factor);

                        using (var bmp = new Bitmap(img, new Size(newW, newH)))
                        {
                            var jpgEncoder = ImageCodecInfo.GetImageEncoders().First(c => c.MimeType == "image/jpeg");
                            var encParams = new EncoderParameters(1);
                            encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);

                            using (var ms = new MemoryStream())
                            {
                                bmp.Save(ms, jpgEncoder, encParams);
                                pdfBytes = ms.ToArray();
                            }
                        }
                    }
                }
                else
                {
                    // PDF u otro archivo: leer directo
                    using (var ms = new MemoryStream())
                    {
                        file.InputStream.CopyTo(ms);
                        pdfBytes = ms.ToArray();
                    }
                }

                // Comprimir con Brotli
                byte[] compressed = CompressBytes(pdfBytes);

                string tamOrig = FormatSize(pdfBytes.Length);
                string tamComp = FormatSize(compressed.Length);
                string porcComp = pdfBytes.Length > 0 ? ((double)(pdfBytes.Length - compressed.Length) / pdfBytes.Length * 100).ToString("N2") : "0";

                using (var db = new SqlConnection(_cs))
                {
                    var exists = db.QueryFirstOrDefault<int?>("SELECT Id FROM tblSacionImagen WHERE IdSancion = @id AND Tipo = @tipo", new { id, tipo });

                    if (exists.HasValue)
                    {
                        db.Execute(@"UPDATE tblSacionImagen SET ImageData = @ImageData, ImageName = @ImageName, 
                            TamañoOriginal = @TamOrig, TamañoComprimido = @TamComp, PorcentajeCompresion = @PorcComp, fecha = GETDATE()
                            WHERE IdSancion = @id AND Tipo = @tipo",
                            new { ImageData = compressed, ImageName = fileName, TamOrig = tamOrig, TamComp = tamComp, PorcComp = porcComp, id, tipo });
                    }
                    else
                    {
                        db.Execute(@"INSERT INTO tblSacionImagen (IdSancion, Tipo, idempleado, ImageData, ImageName, TamañoOriginal, TamañoComprimido, PorcentajeCompresion, fecha)
                            VALUES (@id, @tipo, @carnet, @ImageData, @ImageName, @TamOrig, @TamComp, @PorcComp, GETDATE())",
                            new { id, tipo, carnet, ImageData = compressed, ImageName = fileName, TamOrig = tamOrig, TamComp = tamComp, PorcComp = porcComp });
                    }
                }

                return Json(new { success = true, message = "Documento guardado. Original: " + tamOrig + " → Comprimido: " + tamComp + " (" + porcComp + "% reduccion)" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Error SubirDocumento: " + ex.Message);
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ───── DOCUMENTOS (OBTENER) ─────────────────────────────────
        [HttpGet]
        public JsonResult ObtenerDocumento(int id, int tipo)
        {
            try
            {
                using (var db = new SqlConnection(_cs))
                {
                    var row = db.QueryFirstOrDefault("SELECT ImageData, ImageName FROM tblSacionImagen WHERE IdSancion = @id AND Tipo = @tipo", new { id, tipo });
                    if (row == null) return Json(new { success = false, message = "No se encontro documento." }, JsonRequestBehavior.AllowGet);

                    byte[] compressed = (byte[])row.ImageData;
                    byte[] decompressed = DecompressBytes(compressed);
                    string base64 = Convert.ToBase64String(decompressed);
                    string name = (string)row.ImageName;

                    return Json(new { success = true, base64, fileName = name }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        // ───── DOCUMENTOS (ELIMINAR) ────────────────────────────────
        [HttpPost]
        public JsonResult EliminarDocumento(int id, int tipo)
        {
            try
            {
                using (var db = new SqlConnection(_cs))
                {
                    db.Execute("DELETE FROM tblSacionImagen WHERE IdSancion = @id AND Tipo = @tipo", new { id, tipo });
                    return Json(new { success = true, message = "Documento eliminado." });
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ───── HELPERS ──────────────────────────────────────────────
        private static byte[] CompressBytes(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var brotli = new BrotliStream(output, CompressionLevel.Optimal))
                {
                    brotli.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        private static byte[] DecompressBytes(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var brotli = new BrotliStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                brotli.CopyTo(output);
                return output.ToArray();
            }
        }

        private static string FormatSize(long size)
        {
            if (size >= 1024 * 1024) return ((double)size / (1024 * 1024)).ToString("N2") + " MB";
            return ((double)size / 1024).ToString("N2") + " KB";
        }
    }
}
