using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Dapper;
using DevExpress.Web.Mvc;
using Entities;
using Newtonsoft.Json;
using OfficeOpenXml;
using RestSharp;
using slnRhonline.Models;
using slnRhonline.Reports;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class EventsController : Controller
    {
        private readonly string _connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SARH; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos

        public ActionResult GetEventDescription(int eventId)
        {
            string description = Data.Event.GetAllEvents().FirstOrDefault(x => x.EventId == eventId).Description; // ProductList.GetProductPrice(productID);
            return Json(description, JsonRequestBehavior.AllowGet);
        }

        #region Reportes

        public ActionResult EventListReport()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }
        /// <summary>
        /// Accion que llama a metodo GetAllEvents de la capa de datos.
        /// ExpensesDetailReportPartial.
        /// </summary>
        /// <returns></returns>
        public ActionResult EventListReportPartial()
        {
            List<Entities.Events> lstEvent = new List<Entities.Events>();

            Entities.ViewModels.EventParametersView eParameter = new Entities.ViewModels.EventParametersView();
            eParameter = (Entities.ViewModels.EventParametersView)Session["sEventParameters"];


            try
            {


                var rEvent = Data.Event.GetAllEvents();

                if (rEvent != null)
                {

                    lstEvent = rEvent.ToList();


                }
                else
                {
                    lstEvent = new List<Entities.Events>();
                }


            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("EventListReportPartial", lstEvent);
        }


        /// <summary>
        /// Accion que retorna la exportacion de la lista de eventos
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportEventListReport()
        {
            Entities.ViewModels.EventParametersView eventParameters = new Entities.ViewModels.EventParametersView();
            eventParameters = (Entities.ViewModels.EventParametersView)Session["sEventParameters"];


            EventList detailReport = new EventList();


            var rEvent = Data.Event.GetAllEvents();

            if (rEvent != null)
            {

                detailReport.DataSource = rEvent.ToList();

            }
            else
            {
                detailReport.DataSource = new List<Entities.ViewModels.EventReportsView>();
            }


            return ReportViewerExtension.ExportTo(detailReport);
        }

        #endregion

        #region Lista de Eventos
        [Authorize]

        public ActionResult List()
        {
            List<Entities.Events> lstEvent = new List<Entities.Events>();
            try
            {
                lstEvent = Data.Event.GetAllEvents();
                return View("EventList", lstEvent);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

        }
        [Authorize]

        public ActionResult EventList()
        {
            List<Entities.Events> lstEvent = new List<Entities.Events>();
            try
            {

                return View();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

        }

        public ActionResult EventListPartial()

        {
            List<Entities.Events> lstEvent = new List<Entities.Events>();
            try
            {

                lstEvent = Data.Event.GetAllEvents();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("EventListPartial", lstEvent);
        }

        public ActionResult ListPartial()

        {
            List<Entities.Events> lstEvent = new List<Entities.Events>();
            try
            {

                lstEvent = Data.Event.GetAllEvents();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("EventListPartial", lstEvent);
        }
        #endregion
        #region CRUD
        [HttpPost, ValidateInput(false)]
        public JsonResult SaveEvent(Entities.Events events)
        {
            string result = string.Empty;
            if (events.EventId == -1)
            {

                result = Data.Event.InsertEvent(events);
                if (result == "Exito al insertar el registro")
                {
                    return Json(new { status = "Exito", message = "Exito al insertar el registro" });
                }
            }

            else
            {
                result = Data.Event.UpdateEvent(events);
                if (result == "Exito al actualizar el registro")
                {
                    return Json(new { status = "Exito", message = "Exito al actualizar el registro" });
                }
            }

            return Json(new { status = "Error", message = "campo vacio" });
        }


        public ActionResult ExternalEditFormEdit(int eventId = -1)
        {
            Entities.Events editEvent = Data.Event.GetAllEvents().Where(x => x.EventId == eventId).FirstOrDefault();
            if (editEvent == null)
            {
                editEvent = new Entities.Events();
                editEvent.EventId = -1;
            }
            return View("EditingForm", editEvent);
        }
        //[HttpPost]
        //[ValidateInput(false)]
        //public ActionResult SaveEvent(Entities.Events events)
        //{

        //    if (ModelState.IsValid)
        //    {
        //       try
        //    {
        //        //if (string.IsNullOrEmpty(events.EventName))
        //        //    {
        //        //        //var script = @"alert(""El nombre del evento es requerido"");"; // @"swal(""Aviso!"", ""Hay campos requeridos, por favor corrija!"", ""error"");"; 
        //        //        //return JavaScript(script);

        //        //        //        // return Content("El campo Evento es requerido. Por favor ingrese el dato");
        //        //            return Json(new { status = "EmptyField", message = "campo vacio" });


        //        //    }

        //        //if (string.IsNullOrEmpty(events.CoordinatorE
        // ))
        //        //{
        //        //        var script = @"alert(""El correo del coordinador  es requerido"");"; // @"swal(""Aviso!"", ""Hay campos requeridos, por favor corrija!"", ""error"");"; 
        //        //        return JavaScript(script);
        //        //    }

        //            events.EventName = events.EventName.ToUpper().Trim();
        //            if (events.EventId > 0)
        //            {

        //                    //Llamar al metodo UpdateEvent
        //                    string result = Data.Event.UpdateEvent(events);

        //                    if (result != "Exito al actualizar el registro")
        //                    {
        //                        return Content(result);
        //                    }


        //            }
        //            else
        //            {

        //                    //Llamar al metodo InsertEvent
        //                    string result = Data.Event.InsertEvent(events);
        //                    if (result != "Exito al insertar el registro")
        //                    {
        //                        return Content(result);
        //                    }


        //            }


        //}
        //        catch (Exception e)
        //        {
        //            ViewData["EditError"] = e.Message;
        //        }
        //    }
        //    else
        //    {
        //        return Json(new { status = "EmptyField", message = "campo vacio" });
        //        //return Content("Ocurrió un error al actualizar la información, por favor verifique los datos y vuelva a intentarlo.");
        //    }



        //    return ListPartial();
        //    //return PartialView("ListPartial");
        //}

        /// <summary>
        /// Accion que llama  a metodo para insertar una inscripcion
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddEvent(Entities.Events events)
        {


            if (ModelState.IsValid)
            {
                try
                {

                    if (string.IsNullOrEmpty(events.EventName))
                    {
                        return Content("El campo Evento es requerido. Por favor ingrese el dato");

                    }

                    if (string.IsNullOrEmpty(events.CoordinatorEmail))
                    {
                        return Content("El campo Evento es requerido. Por favor ingrese el dato");
                    }

                    events.EventName = events.EventName.Trim();
                    events.Description = events.Description.Trim();
                    events.CoordinatorEmail = events.CoordinatorEmail.Trim();

                    //Llamar al metodo InsertEvent
                    string result = Data.Event.InsertEvent(events);
                    if (result != "Exito al insertar el registro")
                    {
                        return Content(result);
                    }
                    else
                    {
                        var script = @"swal(""Aviso!"", ""Hay campos requeridos, por favor corrija!"", ""error"");";   //@"alert(""El nombre del evento es requerido"");"; 
                        return JavaScript(script);
                    }


                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {

                return Content("Ocurrió un error al actualizar la información, por favor verifique los datos y vuelva a intentarlo.");
            }

            return ListPartial();
        }

        /// <summary>
        /// Accion que llama  a metodo para editar un usuario
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditEvent(Entities.Events events)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(events.EventName))
                    {
                        return Content("El campo Evento es requerido. Por favor ingrese el dato");
                    }

                    if (string.IsNullOrEmpty(events.CoordinatorEmail))
                    {
                        return Content("El correo del responsable del evento es requerido. Por favor ingrese el dato");
                    }

                    events.EventName = events.EventName.ToUpper().Trim();

                    //Llamar al metodo UpdateEvent
                    string result = Data.Event.UpdateEvent(events);
                    if (result != "Exito al actualizar el registro")
                    {
                        return Content(result);
                    }

                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                return Content("Ocurrió un error al actualizar la información, por favor verifique los datos y vuelva a intentarlo.");
            }

            return ListPartial();
        }

        /// <summary>
        /// Accion que llama  a metodo para insertar una inscripcion
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteEvent(int eventId)
        {


            if (ModelState.IsValid)
            {
                try
                {
                    //Validar si el evento ya posee inscripcion

                    //Llamar al metodo DeleteEvent
                    string result = Data.Event.DeleteEvent(eventId);
                    if (result != "Exito al eliminar el registro")
                    {
                        return Content(result);
                    }

                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {

                return Content("Ocurrió un error al actualizar la información, por favor verifique los datos y vuelva a intentarlo.");
            }

            return ListPartial();
        }




        #endregion

        public ActionResult All()
        {
            return View();
        }
        public ActionResult Registrar()
        {
            return this.Session["User"] == null ? (ActionResult)this.RedirectToAction("Index", "Login") : (ActionResult)this.View();
        }

        public JsonResult GetEventsJson2()
        {
            var eventos = Data.Event.GetAllEvents()
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.EventDate,
                    e.RegistrationClosing,
                    e.EventEmployeesMax,
                    e.EventCompanionsMax,
                    e.StatusName
                }).ToList();

            return Json(new { data = eventos }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult GetEventsJson()
        {
            var result = (from eventos in Data.Event.GetAllEvents()
                          select new Entities.Events
                          {
                              CoordinatorEmail = eventos.CoordinatorEmail,
                              EventId = eventos.EventId,
                              EventDate = DateTime.Parse(eventos.EventDate.ToShortDateString()),
                              EventName = eventos.EventName,

                          }
                          );

            return Json(new { data = result }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetEventById(int eventId)
        {
            var evento = Data.Event.GetAllEvents().FirstOrDefault(e => e.EventId == eventId);
            if (evento == null)
            {
                return Json(new { success = false, message = "Evento no encontrado." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    evento.EventId,
                    evento.EventName,
                    evento.EventDate,
                    evento.RegistrationClosing,
                    evento.EventEmployeesMax,
                    evento.EventCompanionsMax,
                    evento.StatusName,
                    evento.CoordinatorEmail,
                    evento.Description
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddEventx(Entities.Events events)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { status = "Error", message = "Datos inválidos. Por favor verifica los campos." });
            }

            try
            {
                // Validaciones adicionales si es necesario
                if (string.IsNullOrEmpty(events.EventName))
                {
                    return Json(new { status = "Error", message = "El campo 'Nombre de Evento' es requerido." });
                }

                if (string.IsNullOrEmpty(events.CoordinatorEmail))
                {
                    return Json(new { status = "Error", message = "El campo 'Correo del Coordinador' es requerido." });
                }

                // Llamar al método de inserción en la capa de datos
                string result = Data.Event.InsertEvent(events);
                if (result == "Exito al insertar el registro")
                {
                    return Json(new { status = "Exito", message = "Evento agregado exitosamente." });
                }
                else
                {
                    return Json(new { status = "Error", message = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = $"Se produjo un error: {ex.Message}" });
            }
        }

        // Acción para editar un evento existente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditEventx(Entities.Events events)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { status = "Error", message = "Datos inválidos. Por favor verifica los campos." });
            }

            try
            {
                // Validaciones adicionales si es necesario
                if (string.IsNullOrEmpty(events.EventName))
                {
                    return Json(new { status = "Error", message = "El campo 'Nombre de Evento' es requerido." });
                }

                if (string.IsNullOrEmpty(events.CoordinatorEmail))
                {
                    return Json(new { status = "Error", message = "El campo 'Correo del Coordinador' es requerido." });
                }

                // Llamar al método de actualización en la capa de datos
                string result = Data.Event.UpdateEvent(events);
                if (result == "Exito al actualizar el registro")
                {
                    return Json(new { status = "Exito", message = "Evento actualizado exitosamente." });
                }
                else
                {
                    return Json(new { status = "Error", message = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = $"Se produjo un error: {ex.Message}" });
            }
        }
        [HttpPost]
        public JsonResult Guardarf( )
        {
            string respuesta = "Error al intentar finalizar el equipo.";

            try
            {
                string cadenaConexion = _connectionString;
                var listaMaster = Session["masterligar"] as IEnumerable<dynamic>;
                if (listaMaster == null || !listaMaster.Any())
                    return Json(new { resultado = "Sesión expirada o equipo no encontrado" });
                int idEquipo = listaMaster.First().idliga;
                using (SqlConnection conexion = new SqlConnection(cadenaConexion))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_FinalizarEquipo", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Le pasamos únicamente el ID al Procedimiento Almacenado
                        cmd.Parameters.AddWithValue("@IdLiga", idEquipo);

                        conexion.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                respuesta = dr["resultado"].ToString(); // Capturamos el mensaje del SP
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                respuesta = "Error de base de datos: " + ex.Message;
            }

            return Json(new { resultado = respuesta });
        }
        // Acción para eliminar un evento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteEventx(int eventId)
        {
            if (eventId <= 0)
            {
                return Json(new { status = "Error", message = "ID de evento inválido." });
            }

            try
            {
                // Validar si el evento ya posee inscripciones (implementa esta lógica según tu negocio)
                // bool tieneInscripciones = Data.Event.HasRegistrations(eventId);
                // if (tieneInscripciones)
                // {
                //     return Json(new { status = "Error", message = "No se puede eliminar el evento porque tiene inscripciones." });
                // }

                // Llamar al método de eliminación en la capa de datos
                string result = Data.Event.DeleteEvent(eventId);
                if (result == "Exito al eliminar el registro")
                {
                    return Json(new { status = "Exito", message = "Evento eliminado exitosamente." });
                }
                else
                {
                    return Json(new { status = "Error", message = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = $"Se produjo un error: {ex.Message}" });
            }
        }
        public ActionResult Details(int id) => View();

        public ActionResult Create() => View();

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }


        public ActionResult Edit(int id) => View();

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Delete(int id) => View();

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        #region liga
        public JsonResult Obtener()
        {
            try
            {
                // Si ya se cargó en sesión, se devuelve el resultado cacheado
                if (Session["lt"] != null)
                    return Json(new { data = (List<ligaempleado>)Session["lt"] }, JsonRequestBehavior.AllowGet);

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    // Consulta a la vista vep2; se mapean las columnas a las propiedades de ligaempleado
                    var query = @"SELECT nombre_completo AS nombre, carnet, OGERENCIA AS gerente, primernivel AS area FROM vep2";
                    var lista = conn.Query<ligaempleado>(query).ToList();
                    if (lista != null && lista.Count > 0)
                    {
                        Session["lt"] = lista; // Guardamos en sesión para futuras peticiones
                    }
                    return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<ligaempleado>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Método para obtener el equipo del usuario activo
        public JsonResult Obtenerequipo()
        {
            string year = DateTime.Now.Year.ToString();
            try
            {
                if (Session["User"] == null) return Json(new { data = new { }, error = "Sesión expirada" }, JsonRequestBehavior.AllowGet);
                var empleado = (dynamic)Session["User"];

                using (var conn = new SqlConnection(_connectionString))
                {
                    var detalles = conn.Query<dynamic>("SELECT * FROM ligadetalle WHERE year = @year AND carnet = @carnet AND estado = 'Y'",
                                    new { year, carnet = empleado.EmployeeNumber }).ToList();

                    if (detalles.Any())
                    {
                        int id = detalles.First().idliga;
                        var listaMaster = conn.Query<dynamic>("SELECT * FROM ligamaster WHERE idliga = @id", new { id }).ToList();
                        Session["masterligar"] = listaMaster;
                        var master = listaMaster.FirstOrDefault();

                        if (master != null)
                        {
                           
                            return Json(new
                            {
                                data = new
                                {
                                    idliga = master.idliga, // <--- ¡ESTA ES LA LÍNEA MÁGICA QUE FALTABA!
                                    Nombre = master.nombre,
                                    Disciplina = master.disciplina,
                                    fullnombre = master.creador,
                                    estado = master.estado,
                                    rg = (empleado.EmployeeNumber == master.carnet || master.estado != "F") ? "ok" : ""
                                }
                            }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    return Json(new { data = new { } }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); }
        }
        // Método para obtener los detalles del equipo
        public JsonResult Obtenerequipo2()
        {
            string year = DateTime.Now.Year.ToString();
            try
            {
                if (Session["masterligar"] != null)
                {
                    // TRUCO INFALIBLE: Convertimos el objeto de la sesión a JSON y de vuelta a tu lista estricta.
                    // Esto elimina el error de "List<System.Object> a List<ligamaster>"
                    string jsonSession = Newtonsoft.Json.JsonConvert.SerializeObject(Session["masterligar"]);
                    var listaMaster = Newtonsoft.Json.JsonConvert.DeserializeObject<List<slnRhonline.Models.ligamaster>>(jsonSession);

                    // Verificamos que no esté vacía por seguridad
                    if (listaMaster == null || !listaMaster.Any())
                    {
                        return Json(new { data = new List<ligadetallemodelo>() }, JsonRequestBehavior.AllowGet);
                    }

                    int id = listaMaster.FirstOrDefault().idliga;

                    using (var conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();

                        // Agregamos las tallas al SELECT y usamos alias para que mapeen con 'ligadetallemodelo'
                        string query = @"
                    SELECT distinct iddliga, area, telefono, nombre, carnet, gerencia, sexo,
                           talla_camisa as TallaCamisa, 
                           talla_pantalon as TallaPantalon, 
                           talla_zapato as TallaZapato
                    FROM ligadetalle 
                    WHERE year = @year AND idliga = @id AND estado = 'Y'";

                        var listaDetalleModelo = conn.Query<ligadetallemodelo>(query, new { year, id }).ToList();

                        return Json(new { data = listaDetalleModelo }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { data = new List<ligadetallemodelo>() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<ligadetallemodelo>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // Guarda el registro del equipo (ligamaster) y su detalle inicial (ligadetalle)

        // Finaliza el registro del equipo actual
        [HttpPost]
        public JsonResult Guardar(ligamodelo oPersona)
        {
            try
            {
                if (Session["User"] == null) return Json(new { resultado = "Sesión expirada." });
                Entities.Employees empleado = (Entities.Employees)Session["User"];

                string camisa = string.IsNullOrEmpty(oPersona.TallaCamisa) ? "N/A" : oPersona.TallaCamisa;
                string pantalon = string.IsNullOrEmpty(oPersona.TallaPantalon) ? "N/A" : oPersona.TallaPantalon;
                string zapatos = string.IsNullOrEmpty(oPersona.TallaZapato) ? "N/A" : oPersona.TallaZapato;

                string apiUrl = $"http://172.26.54.66/apihcm/api/values/Guardarliga1?Nombre={oPersona.Nombre}&Disciplina={oPersona.Disciplina}&carnet={empleado.EmployeeNumber}&Telefono={oPersona.Telefono}&camisa={camisa}&pantalon={pantalon}&zapatos={zapatos}";

                var client = new RestClient(apiUrl);
                var result = client.Execute(new RestRequest(Method.GET));

                var contenidoLimpio = JsonConvert.DeserializeObject<dynamic>(result.Content);

                // CORRECCIÓN VITAL: Castear a string puro
                string mensajeSalida = (string)contenidoLimpio.resultado;

                return Json(new { resultado = mensajeSalida }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { return Json(new { resultado = ex.Message }); }
        }
        // Agrega un jugador al equipo
        [HttpPost]
        public JsonResult Guardar2(ligamodelo oPersona)
        {
            try
            {
                if (Session["masterligar"] == null)
                    return Json(new { resultado = "registro" });

                // CORRECCIÓN VITAL: El puente JSON para evitar el error de System.Object a List<ligamaster>
                string jsonSession = JsonConvert.SerializeObject(Session["masterligar"]);
                var listaMaster = JsonConvert.DeserializeObject<List<slnRhonline.Models.ligamaster>>(jsonSession); // ¡Asegúrate que la ruta del modelo sea correcta!

                if (listaMaster == null || !listaMaster.Any())
                    return Json(new { resultado = "Error al leer el equipo de la memoria." });

                int idnew = listaMaster.FirstOrDefault().idliga;

                // Validamos tallas (por si acaso)
                string camisa = string.IsNullOrEmpty(oPersona.TallaCamisa) ? "N/A" : oPersona.TallaCamisa;
                string pantalon = string.IsNullOrEmpty(oPersona.TallaPantalon) ? "N/A" : oPersona.TallaPantalon;
                string zapatos = string.IsNullOrEmpty(oPersona.TallaZapato) ? "N/A" : oPersona.TallaZapato;
                string telefono = string.IsNullOrEmpty(oPersona.Disciplina) ? "N/A" : oPersona.Disciplina;

                // Armamos URL
                string apiUrl = $"http://172.26.54.66/apihcm/api/values/Guardarliga2?idliga={idnew}&carnet={oPersona.Nombre}&telefono={telefono}&camisa={camisa}&pantalon={pantalon}&zapatos={zapatos}";

                var client = new RestClient(apiUrl);
                var result = client.Execute(new RestRequest(Method.POST));

                if (result != null && !string.IsNullOrEmpty(result.Content))
                {
                    dynamic resultGuardar = JsonConvert.DeserializeObject(result.Content);
                    string mensajeGuardar = (string)resultGuardar.resultado; // Casteo seguro a string
                    return Json(new { resultado = mensajeGuardar }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { resultado = "Error de comunicación con la API externa" });
            }
            catch (Exception ex)
            {
                return Json(new { resultado = "error: " + ex.Message });
            }
        }
        // Desactiva (elimina) un registro de jugador
        public JsonResult Guardar3(string id)
        {
            bool flag = true;
            try
            {
                 
                int idd = int.Parse(id);
                if (Session["masterligar"] != null)
                {
                    string apiUrl = $"http://172.26.54.66/apihcm/api/values/Guardarliga9?iddliga={idd}";
 
                    var client = new RestClient(apiUrl);
                    var request = new RestRequest(Method.POST);

                    request.Timeout = -1;


                    var resultExpenses = client.Execute(request);
                    //Console.WriteLine(response.Content);
                    //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                    if (resultExpenses != null)
                    {

                        dynamic resultGuardar = JsonConvert.DeserializeObject(resultExpenses.Content);
                        string mensajeGuardar = resultGuardar.resultado;

                        return Json(new { resultado = mensajeGuardar }, JsonRequestBehavior.AllowGet);
                    }
                    //using (var conn = new SqlConnection(_connectionString))
                    //{
                    //    conn.Open();
                    //    // Actualiza el estado del detalle a 'N'
                    //    int rowsAffected = conn.Execute("UPDATE ligadetalle SET estado = 'N' WHERE iddliga = @idd", new { idd });
                    //    if (rowsAffected == 0)
                    //        flag = false;
                    //}
                }
                else
                {
                    flag = false;
                }
            }
            catch (Exception)
            {
                flag = false;
            }   
            return Json(new { resultado = flag }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Supervision()
        {
            return View();
        }

        // Llama a la API externa pasándole el año seleccionado
        [HttpGet]
        public ContentResult GetListaSupervision(string year = null)
        {
            // Si no mandan el año desde la web, tomamos el año en curso automáticamente
            if (string.IsNullOrEmpty(year))
            {
                year = DateTime.Now.Year.ToString();
            }

            string apiUrl = $"http://172.26.54.66/apihcm/api/values/supervision/lista?year={year}";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);

            // Devolvemos el JSON crudo a DataTables
            return Content(response.Content, "application/json");
        }
        public class emp
        {
            public string Nombre { get; set; }
            public string Gerencia { get; set; }
        }
        #endregion


        #region curso carso
        [HttpPost]
        public JsonResult UploadExcel(HttpPostedFileBase file)
        {
            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    return Json(new { success = false, message = "No se seleccionó ningún archivo." });
                }

                // Guarda el archivo temporalmente en App_Data/Uploads
                string uploadPath = Server.MapPath("~/App_Data/Uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);
                string filePath = Path.Combine(uploadPath, Path.GetFileName(file.FileName));
                file.SaveAs(filePath);

                // Procesa el Excel usando EPPlus y crea la lista de registros
                List<cursopendiente> registros = new List<cursopendiente>();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int row = 2; // Se asume encabezados en la fila 1
                    while (!string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Text))
                    {
                        var reg = new cursopendiente
                        {
                            Carnet = worksheet.Cells[row, 1].Text.Trim(),
                            Cursos = worksheet.Cells[row, 2].Text.Trim(),
                            Plataforma = worksheet.Cells[row, 3].Text.Trim()
                        };
                        registros.Add(reg);
                        row++;
                    }
                }

                // Guarda la lista en Session para uso posterior
                Session["RegistrosCursosPendientes"] = registros;

                // Actualiza la base de datos: inserta/actualiza cursos pendientes
                ActualizarCursosPendientes(registros);

                // Obtiene la lista única de carnets para procesamiento individual
                var carnets = registros.Select(r => r.Carnet).Distinct().ToList();
                int count = carnets.Count;

                return Json(new { success = true, message = "Archivo procesado y base de datos actualizada.", carnets = carnets, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Método que usa Dapper para actualizar/inserir cursos pendientes mediante el procedure
        private void ActualizarCursosPendientes(List<cursopendiente> registros)
        {
            // Convierte la lista a DataTable para el TVP
            DataTable dt = new DataTable();
            dt.Columns.Add("Carnet", typeof(string));
            dt.Columns.Add("Curso", typeof(string));
            dt.Columns.Add("Plataforma", typeof(string));

            foreach (var reg in registros)
            {
                dt.Rows.Add(reg.Carnet, reg.Cursos, reg.Plataforma);
            }

            var param = new DynamicParameters();
            param.Add("@Courses", dt.AsTableValuedParameter("dbo.CourseUploadType"));

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                connection.Execute("usp_UpdateCursosPendientes", param, commandType: CommandType.StoredProcedure);
            }
        }

        // POST: CursosPendientes/ProcessCarnet
        [HttpPost]
        public JsonResult ProcessCarnet(string carnet)
        {
            emp2024 empleado = null;
            string sql = "SELECT * FROM EMP2024 WHERE carnet = @carnet";
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                empleado = db.QueryFirstOrDefault<emp2024>(sql, new { carnet });
            }



            try
            {
                var registros = Session["RegistrosCursosPendientes"] as List<cursopendiente>;
                if (registros == null)
                    return Json(new { success = false, message = "No hay registros en sesión." });

                // Simula retardo de procesamiento (por ejemplo, 500 ms)
                Thread.Sleep(500);
              

                if (empleado != null)
                {
                    string cursop = "<table style='border-collapse: collapse; width: 40%; border: 2px solid black;'>" +
                        "<tr style='background-color: #9d0904; color: white;'><th style='border: 1px solid black;'>Cursos Pendientes</th>" +
                        "<th style='border: 1px solid black;'>Plataforma</th></tr>";
                    foreach (var reg in registros.Where(r => r.Carnet == carnet))
                    {
                        cursop += $"<tr><td style='border: 1px solid black;'>{reg.Cursos}</td>" +
                                  $"<td style='border: 1px solid black;'>{reg.Plataforma}</td></tr>";
                    }
                    cursop += "</table><br/>";
                    string formattedDateTime = DateTime.Now.ToString("dd/MM/yyyy");
                    cursop += $"<b>Fecha de corte: {formattedDateTime} 8:00 a.m.</b><br/>";
                    string mensaje = "Estimado <b>" + empleado.nombre_completo + "</b>, es un gusto saludarle.<br/><br/>" +
                        "La subgerencia de Capacitación y Desarrollo le invita a realizar los cursos pendientes a través de las plataformas CARSO & EDUCLARO.<br/><br/>" +
                        "<b>Cursos Pendientes:</b><br/><br/>" + cursop;

                    // Envío del correo mediante el servicio WCF
                    //Utils.ClaroAsemClient proxys = new wsportal.ClaroAsemClient();
                    //Utils.ClaroWCF.EnviarCorreoUsuariocapacitacionAsync("PENDIENTES: FORMACIÓN VIRTUAL - CARSO & EDUCLARO", mensaje, empleado.correo);
                }

                return Json(new { success = true, carnet = carnet });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion
    }
}