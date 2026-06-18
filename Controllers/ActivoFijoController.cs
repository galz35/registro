using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using slnRhonline.Models;
using System.Drawing.Imaging;
using System.Drawing;
//using SkiaSharp;
using System.IO;
using ImageMagick;
using System.Threading.Tasks;

namespace slnRhonline.Controllers
{
    public class ActivoFijoController : Controller
    {
        static ServiceReference1.ClaroAsemClient ClaroWCF = new ServiceReference1.ClaroAsemClient();

        private readonly string _connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIGHO1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos

        // GET: ActivoFijo
        public ActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public JsonResult GetAll()
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT * FROM Act_ActivoFijo WHERE Act_Eliminado = 0;
                ";
           var qr =       db.Query<Act_ActivoFijo>(query);
                return Json(new { success = true, data = qr }, JsonRequestBehavior.AllowGet);

            }
        }

        /// <summary>
        /// Obtener detalles de un activo fijo por ID.
        /// GET: /Act_ActivoFijo/GetDetails/{id}
        /// </summary>
        [HttpGet]
        public JsonResult GetDetails(int id)
        {
            //List<Act_ActivoFijo> activo = new List<Act_ActivoFijo>();
             using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT * FROM Act_ActivoFijo WHERE Act_ID = @Act_ID AND Act_Eliminado = 0;
                ";
                var activo = db.QueryFirstOrDefault<Act_ActivoFijo>(query, new { Act_ID = id });
                if (activo == null)
                {
                    return Json(new { success = false, message = "Activo fijo no encontrado." }, JsonRequestBehavior.AllowGet);
                }

                var historial = GetHistorialByActivoFijo(id);
                return Json(new { success = true, data = activo, historial = historial }, JsonRequestBehavior.AllowGet);
            }
         
        }
        // **5. Obtener historial de asignaciones por ID de activo fijo**
        public IEnumerable<Act_HistorialAsignacion> GetHistorialByActivoFijo(int activoFijoId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        h.Act_HistorialID,
                        h.Act_ActivoFijoID,
                        h.Act_UsuarioID,
                        h.Act_FechaAsignacion,
                        h.Act_FechaDevolucion,
                        h.Act_Comentarios,
                        u.Act_NombreCompleto AS Nombre_Completo
                    FROM 
                        Act_HistorialAsignacion h
                    LEFT JOIN 
                        emp2024 u ON h.Act_UsuarioID = u.carnet
                    WHERE 
                        h.Act_ActivoFijoID = @ActivoFijoID
                    ORDER BY 
                        h.Act_FechaAsignacion DESC;
                ";
                return db.Query<Act_HistorialAsignacion>(query, new { ActivoFijoID = activoFijoId });
            }
        }
        /// <summary>
        /// Registrar un nuevo activo fijo.
        /// POST: /Act_ActivoFijo/Create
        /// </summary>
        [HttpPost]
        public JsonResult Create(Act_ActivoFijo activo, HttpPostedFileBase Act_Imagen)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (Act_Imagen != null && Act_Imagen.ContentLength > 0)
                    {
                        using (var binaryReader = new System.IO.BinaryReader(Act_Imagen.InputStream))
                        {
                            activo.Act_Imagen = binaryReader.ReadBytes(Act_Imagen.ContentLength);
                        }
                    }

                    activo.Act_Estado = "Disponible"; // Estado inicial
                    CrearActivoFijo(activo);

                    return Json(new { success = true, message = "Activo fijo registrado exitosamente." });
                }
                catch (Exception ex)
                {
                    // Log the exception (omitted for brevity)
                    return Json(new { success = false, message = "Error al registrar el activo fijo.", error = ex.Message });
                }
            }

            // Si el modelo no es válido
            return Json(new { success = false, message = "Datos inválidos. Por favor verifica y vuelve a intentarlo." });
        }
        public void CrearActivoFijo(Act_ActivoFijo activo)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string insertQuery = @"
                    INSERT INTO Act_ActivoFijo 
                        (Act_Codigo, Act_Nombre, Act_Descripcion, Act_CategoriaID, Act_Estado, Act_Ubicacion, 
                        Act_FechaAdquisicion, Act_VidaUtil, Act_UsuarioAsignadoID, Act_FechaAsignacion, Act_Imagen, Act_FechaCreacion)
                    VALUES 
                        (@Act_Codigo, @Act_Nombre, @Act_Descripcion, @Act_CategoriaID, @Act_Estado, @Act_Ubicacion, 
                        @Act_FechaAdquisicion, @Act_VidaUtil, @Act_UsuarioAsignadoID, @Act_FechaAsignacion, @Act_Imagen, GETDATE());
                ";
                db.Execute(insertQuery, activo);
            }
        }
        public Act_ActivoFijo GetActivoFijoById(int id)
        {
            return ObtenerDetalles(id);
        }
        public Act_ActivoFijo ObtenerDetalles(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT * FROM Act_ActivoFijo WHERE Act_ID = @Act_ID AND Act_Eliminado = 0;
                ";
                return db.QueryFirstOrDefault<Act_ActivoFijo>(query, new { Act_ID = id });
            }
        }
        /// <summary>
        /// Solicitar baja de un activo fijo (cambiar estado a 'De Baja').
        /// POST: /Act_ActivoFijo/RequestDisposal
        /// </summary>
        [HttpPost]
        public JsonResult RequestDisposal(int id)
        {
            try
            {
                var activo =  GetActivoFijoById(id);
                if (activo == null)
                {
                    return Json(new { success = false, message = "Activo fijo no encontrado." });
                }

                if (activo.Act_Estado == "De Baja")
                {
                    return Json(new { success = false, message = "El activo ya está dado de baja." });
                }

                 ActualizarEstadoActivoFijo(id, "De Baja", null, null);

                return Json(new { success = true, message = "Solicitud de baja realizada correctamente." });
            }
            catch (Exception ex)
            {
                // Log the exception (omitted for brevity)
                return Json(new { success = false, message = "Error al solicitar la baja del activo fijo.", error = ex.Message });
            }
        }
        public void ActualizarEstadoActivoFijo(int id, string nuevoEstado, string usuarioAsignadoId, DateTime? fechaAsignacion)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string updateQuery = @"
                    EXEC Act_sp_UpdateActivoFijoEstado 
                        @Act_ID = @ID, 
                        @NuevoEstado = @Estado, 
                        @UsuarioAsignadoID = @UsuarioID, 
                        @FechaAsignacion = @FechaAsignacion;
                ";
                db.Execute(updateQuery, new
                {
                    ID = id,
                    Estado = nuevoEstado,
                    UsuarioID = usuarioAsignadoId,
                    FechaAsignacion = fechaAsignacion
                });
            }
            // El trigger Act_trg_ActivoFijo_EstadoChange insertará en Act_HistorialAsignacion
        }
        /// <summary>
        /// Obtener historial de asignaciones de un activo fijo.
        /// GET: /Act_ActivoFijo/GetHistory/{id}
        /// </summary>
        [HttpGet]
        public JsonResult GetHistory(int id)
        {
            var activo = GetActivoFijoById(id);
            if (activo == null)
            {
                return Json(new { success = false, message = "Activo fijo no encontrado." }, JsonRequestBehavior.AllowGet);
            }

            var historial = GetHistorialByActivoFijo(id);
            return Json(new { success = true, data = historial }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Obtener todos los activos disponibles (Estado = 'Disponible').
        /// GET: /Act_ActivoFijo/GetAvailable
        /// </summary>
        [HttpGet]
        public JsonResult GetAvailable()
        {
            var disponibles =  GetActivosDisponibles();
            return Json(new { success = true, data = disponibles }, JsonRequestBehavior.AllowGet);
        }
        public IEnumerable<Act_ActivoFijo> GetActivosDisponibles()
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT * FROM Act_ActivoFijo 
                    WHERE Act_Estado = 'Disponible' AND Act_Eliminado = 0;
                ";
                return db.Query<Act_ActivoFijo>(query);
            }
        }
        /// <summary>
        /// Asignar un activo fijo a un usuario.
        /// POST: /Act_ActivoFijo/Assign
        /// </summary>
        [HttpPost]
        public JsonResult Assign(int id, string usuarioAsignadoId, string fechaAsignacion)
        {
            if (string.IsNullOrEmpty(usuarioAsignadoId) || fechaAsignacion == null)
            {
                return Json(new { success = false, message = "Usuario asignado y fecha de asignación son requeridos." });
            }

            DateTime? fechaAsign = null;
            if (!DateTime.TryParse(fechaAsignacion, out DateTime parsedFecha))
            {
                return Json(new { success = false, message = "Fecha de asignación inválida." });
            }
            fechaAsign = parsedFecha;

            try
            {
                var activo = GetActivoFijoById(id);
                if (activo == null)
                {
                    return Json(new { success = false, message = "Activo fijo no encontrado." });
                }

                if (activo.Act_Estado != "Disponible")
                {
                    return Json(new { success = false, message = $"El activo no está disponible. Estado actual: {activo.Act_Estado}" });
                }

               ActualizarEstadoActivoFijo(id, "Asignado", usuarioAsignadoId, fechaAsign);

                return Json(new { success = true, message = "Activo asignado correctamente." });
            }
            catch (Exception ex)
            {
                // Log the exception (omitted for brevity)
                return Json(new { success = false, message = "Error al asignar el activo fijo.", error = ex.Message });
            }
        }

        /// <summary>
        /// Liberar un activo fijo (cambiar estado a 'Disponible').
        /// POST: /Act_ActivoFijo/Release
        /// </summary>
        [HttpPost]
        public JsonResult Release(int id)
        {
            try
            {
                var activo = GetActivoFijoById(id);
                if (activo == null)
                {
                    return Json(new { success = false, message = "Activo fijo no encontrado." });
                }

                if (activo.Act_Estado != "Asignado")
                {
                    return Json(new { success = false, message = $"El activo no está asignado. Estado actual: {activo.Act_Estado}" });
                }

                 ActualizarEstadoActivoFijo(id, "Disponible", null, null);

                return Json(new { success = true, message = "Activo liberado correctamente." });
            }
            catch (Exception ex)
            {
                // Log the exception (omitted for brevity)
                return Json(new { success = false, message = "Error al liberar el activo fijo.", error = ex.Message });
            }
        }

    }
}