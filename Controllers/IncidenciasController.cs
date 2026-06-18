using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using slnRhonline.Validations;

namespace slnRhonline.Controllers
{
    public class IncidenciasController : Controller
    {
        const string keyIdUnidad = "sIdUnidad";
        const string keyIdIncidecia = "sIdIncidencia";
        
        // GET: Incidencias

        /// <summary>
        /// Metodo que devuelve la lista de unidades asignadas al area.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult CarsList()
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
        /// Metodo que devuelve la lista de unidades asignadas al area  a la vista parcial.
        /// </summary>
        /// <returns></returns>
        public ActionResult CarsListPartial()
        {
            List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();

            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                lstCars = Data.Consumo.ObtenerListaUnidades(eEmployee.Idhrms.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("CarsListPartial", lstCars);
        }


        public ActionResult List(string id)
        {
            List<Entities.ViewModels.VistaIncidencias> lstIncidencia = new List<Entities.ViewModels.VistaIncidencias>();
            try
            {
                Session[keyIdUnidad] = id;
                lstIncidencia = Data.Incidencia.ObtenerListaIncidencias(id);
                ViewBag.Unidad = id;
                return View("List", lstIncidencia);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }

        public ActionResult ListPartial()
        {
            List<Entities.ViewModels.VistaIncidencias> lstIncidencia = new List<Entities.ViewModels.VistaIncidencias>();
            try
            {
                string idIncidencia = (string)Session[keyIdUnidad];
                lstIncidencia = lstIncidencia = Data.Incidencia.ObtenerListaIncidencias(idIncidencia);



                return PartialView("ListPartial", lstIncidencia);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }

        #region CRUD

     
   

  
        public ActionResult EditarIncidencia(int idIncidencia)
        {
            string mensajeValidacion = string.Empty;
            Entities.Incidencias incidencia = new Entities.Incidencias();
            incidencia.IdIncidencia = idIncidencia;

            //Instancia de la clase de validacion de incidencias
            Incidencia vIncidencia = new Incidencia();
            mensajeValidacion = vIncidencia.ValidarIncidencia(incidencia);
            try
            {

                if (mensajeValidacion == "ok")
                {
                    var resultado =
                  Data.Incidencia.ObtenerIncidenciaPorId(idIncidencia).FirstOrDefault();
                    if (resultado != null)
                    {

                        incidencia.IdIncidencia = resultado.IdIncidencia;
                        incidencia.FechaIncidencia = DateTime.Parse(resultado.FechaIncidencia.ToShortDateString());
                        incidencia.IdTipoIncidencia = resultado.IdTipoIncidencia;
                        incidencia.ReportadoPor = resultado.ReportadoPor;
                        incidencia.IdTipoServicio = resultado.IdTipoServicio;
                        incidencia.DescripcionDano = resultado.DescripcionDano;
                        //incidencia.ReportoSeguro = resultado.ReportoSeguro;
                        //incidencia.NumeroReclamo = resultado.NumeroReclamo;
                        //incidencia.IdEstadoGestion = resultado.IdEstadoGestion;
                        //incidencia.ObservacionesTransporte = resultado.ObservacionesTransporte;

                    }

                    return Json(new { status = "Exito", data = incidencia }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { status = "Error", data = mensajeValidacion }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                return Json(new { status = "Error", data = "Ha ocurrido un error en la transaccion." }, JsonRequestBehavior.AllowGet);
            }

                
        }
      
     


        [HttpPost]
        [ValidateInput(false)]
        public JsonResult GuardarIncidencia(Entities.Incidencias model)
        {
            string result = string.Empty;
          
            string resultadoEstado = string.Empty;
            string validar = string.Empty;
            string idUnidad = (string)Session[keyIdUnidad];

            //Instancia de la clase de validacion de incidencias
            Incidencia vIncidencia = new Incidencia();

            //if (model.IdIncidencia == 0)
            //{
            //    model.IdIncidencia = -1;
            //}

          
            if (!string.IsNullOrEmpty(model.DescripcionDano))
            {
                model.DescripcionDano = model.DescripcionDano.ToUpper();
            }
            
            //incidencia.Odometro = model.Odometro;

            //Llamar al método de insertar cita
            try
            {
                validar = vIncidencia.ValidarIncidencia(model);

                if (validar == "ok")
                {
                    model.IdUnidad = idUnidad;
                    result = Data.Incidencia.GuardarIncidencia(model);
                    if (Data.Appointment.isNumeric(result))
                    {
                        Entities.IncidenciasEstados incidenciaEstado = new Entities.IncidenciasEstados();
                        Entities.Employees eEmployee = null;
                        if (Session["User"] != null)
                        {
                            eEmployee = (Entities.Employees)Session["User"];
                        }

                        incidenciaEstado.IdIncidencia = int.Parse(result);
                        incidenciaEstado.IdEstado = "2101";
                        incidenciaEstado.IdPersona = eEmployee.Idhrms.ToString();
                        incidenciaEstado.EsActivo = "Y";
                        incidenciaEstado.IpLocal = Utils.ObtenerIpLocal(string.Empty);
                        incidenciaEstado.UsuarioDominioInserto = Utils.ObtenerUsuarioDominio(string.Empty);

                        if (model.IdIncidencia == -1)
                        {
                            resultadoEstado = Data.Incidencia.InsertarEstadoIncidencia(incidenciaEstado);

                            if (resultadoEstado != "EXITO")
                            {
                               
                                return Json(new { status = "Error", message = "Error al insertar el estado de la incidencia" });
                            }
                            EnviarCorreo(model, "Incidencia");
                        }

                    }
                    else
                    {
                        return Json(new { status = "Error", message = "Error al insertar la incidencia" });
                    }
                    
                }
                else
                {
                    return Json(new { status = "Error", message = validar });
                }


            }
            catch (Exception ex)
            {

                return Json(new { status = "Error", message = "Ha ocurrido un error en la transaccion." });
            }



            return Json(new { status = "Exito", message = "Exito al registrar la incidecia" });

        }

        public string EnviarCorreo(Entities.Incidencias incidencia, string tipoCorreo)
        {
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            string destinatario = string.Empty;
            string copia = string.Empty;

            destinatario = "transporte@claro.com.ni";
            copia = "candida.sanchez@claro.com.ni";

            if (tipoCorreo == "Incidencia")
            {
                titulo = "Registro de Incidencia";
                mensaje = "Estimado equipo de transporte:" +
                             "<br/>" +
                             "<br/>" + "Tiene un registro de incidencia guardado de la unidad:" +
                               "<br/>" +
                             " " + incidencia.IdUnidad + " en la fecha " + incidencia.FechaIncidencia.ToString("D", new CultureInfo("es-ES")) + ".";

            }
            if (tipoCorreo == "Anular")
            {
                titulo = "Anulación de Incidencia";
                mensaje = "Estimado equipo de transporte:" +
                             "<br/>" +
                             "<br/>" + "Se ha anulado la incidencia generada para la unidad:" +
                               "<br/>" +
                             " " + incidencia.IdUnidad + " en la fecha " + incidencia.FechaIncidencia.ToString("D", new CultureInfo("es-ES")) + ".";

            }


            resultadoCorreo = Utils.EnviarCorreoUsuario(destinatario, titulo, copia, mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transaccion se genero exitosamente, pero ocurrió un error al enviar el correo";

            }


            return resultadoCorreo;
        }

        /// <summary>
        /// Metodo para anular una cita
        /// </summary>
        /// <param name = "idCita" ></ param >
        /// < returns ></ returns >
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AnularIncidencia(int idIncidencia)
        {

            string resultado = String.Empty;

            var estado = Data.Incidencia.ObtenerEstadoPorId(idIncidencia);

            if (estado != null)
            {
                string estadoIncidencia = estado.IdEstado;
                if (estadoIncidencia != "2101")
                {
                    return Json(new { status = "Error", message = "Solo se pueden anular incidencias en estado REGISTRADO" });
                }
            }

            Entities.IncidenciasEstados incidenciaEstado = new Entities.IncidenciasEstados();
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            incidenciaEstado.IdIncidencia = idIncidencia;
            incidenciaEstado.IdEstado = "2102";
            incidenciaEstado.IdPersona = eEmployee.Idhrms.ToString();
            incidenciaEstado.EsActivo = "Y";
            incidenciaEstado.IpLocal = Utils.ObtenerIpLocal(string.Empty);
            incidenciaEstado.UsuarioDominioInserto = Utils.ObtenerUsuarioDominio(string.Empty);

            resultado = Data.Incidencia.InsertarEstadoIncidencia(incidenciaEstado);

            if (resultado != "EXITO")
            {
                return Json(new { status = "Error", message = "Error al anular la incidencia" });
            }

            var incidencia = Data.Incidencia.ObtenerIncidenciaPorId(idIncidencia);
           // Entities.Incidencias incidencia = new Entities.Incidencias();
          
            EnviarCorreo(incidencia.FirstOrDefault(), "Anular");
            return Json(new { status = "Exito", message = "Exito al anular la incidencia" });

        }


        

        #endregion
    }
}