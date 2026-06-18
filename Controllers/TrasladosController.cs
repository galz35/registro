using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities;

using  Entities.ViewModels;
using DevExpress.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class TrasladosController : Controller
    {
        const string keyIdTraslado = "sIdTraslado";
        static ServiceReference1.ClaroAsemClient ClaroWCF = new ServiceReference1.ClaroAsemClient();


        #region Lista de Traslados
        [Authorize]

        public ActionResult List()
        {
            List<VistaTraslados> lstTraslados = new List<VistaTraslados>();
            try
            {
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                //lstTraslados = Data.Traslado.ObtenerTrasladosPorPersona(eEmployee.Id_HRMS.ToString());
               
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstTraslados);
        }

        public ActionResult ListPartial()

        {
            List<VistaTraslados> lstTraslados = new List<VistaTraslados>();
            try
            {
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                lstTraslados = Data.Traslado.ObtenerTrasladosPorPersona(eEmployee.Idhrms.ToString());

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("ListPartial", lstTraslados);
        }
 

        public JsonResult ListPartialjson()

        {
            List<VistaTraslados> lstTraslados = new List<VistaTraslados>();
            try
            {
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                lstTraslados = Data.Traslado.ObtenerTrasladosPorPersona(eEmployee.Idhrms.ToString());

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return Json(new { data = lstTraslados }, JsonRequestBehavior.AllowGet);
        }

 
        #endregion

        #region Acciones del CRUD de Traslados
        //[HttpGet]
        public ActionResult ObtenerSaldoUnidad(string idUnidad)
            {
            List<Entities.ViewModels.AssignmentCarsView> lstCars = new List<Entities.ViewModels.AssignmentCarsView>();
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            lstCars = Data.ExtraPlan.GetCarsByManagement();
            var unidad = lstCars.Where(x => x.IdUnidad == idUnidad).FirstOrDefault();
            if (unidad != null)
            {
                string saldoUnidad = unidad.SaldoCombustible.ToString();  // lstCars.Where(x => x.IdUnidad == idUnidad).FirstOrDefault().SaldoCombustible;
                return Json(saldoUnidad, JsonRequestBehavior.AllowGet);
            }
            else
            {
                decimal saldoUnidad = 0;
                return Json(saldoUnidad, JsonRequestBehavior.AllowGet);
            }



        }

        /// <summary>
        /// Metodo para validar el traslado
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        [HttpPost]

        public JsonResult ValidarTraslado(string idTraslado, string tipoEdicion)
        {

            string result = String.Empty;

            if (tipoEdicion != "Nuevo")
            {
                var estado = Data.Traslado.ObtenerTrasladoPorId(idTraslado);

                if (estado.Count > 0)
                {
                    string estadoTraslado = estado.FirstOrDefault().Estado;
                    if (estadoTraslado != "REGISTRADO")
                    {
                        EditarTraslado(idTraslado);
                        return Json(new { status = "Error", message = "Solo se pueden editar traslados en estado REGISTRADO" });
                    }
                }
            }

           return Json(new { status = "Exito", message = "El extra plan ha sido eliminado" });
        }
        /// <summary>
        /// Accion que carga la informacion a la vista de encabezado del extra plan
        /// </summary>
        /// <param name="idTraslado"></param>
        /// <returns></returns>
        public ActionResult EditarTraslado(string idTraslado = "-1")
        {
            if (idTraslado != "-1" && idTraslado.Contains("TRS") == false)
            {
                if (idTraslado.Length < 6)
                {
                    // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                    idTraslado = idTraslado.PadLeft(6, '0');
                }
                idTraslado = "TRS-" + idTraslado;
            }   

            Session[keyIdTraslado] = idTraslado;
            Session.Remove("sDetalleTraslado");

            Entities.ViewModels.VistaTraslados editarTraslado = Data.Traslado.ObtenerTrasladoPorId(idTraslado).FirstOrDefault();
            if (editarTraslado == null)
            {
                DateTime fechaActual = DateTime.Today;
                editarTraslado = new Entities.ViewModels.VistaTraslados();
                editarTraslado.IdCombustibleTraslado = "-1";
                editarTraslado.FechaTraslado = fechaActual;
                Session.Remove("sDetalleTraslado");
            }
         
            return View("Edit", editarTraslado);
        }
        /// <summary>
        /// Accion que carga  el detalle del traslado
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        public ActionResult DetailPartial()
        {
            string idTraslado = (string)Session[keyIdTraslado];
          
            List<Entities.ViewModels.VistaTrasladosDetalle> lstDetail = new List<Entities.ViewModels.VistaTrasladosDetalle>();

            try
            {

                lstDetail = Data.Traslado.ObtenerDetalleTrasladosPorId(idTraslado);

            }



            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


            return PartialView("DetailPartial", lstDetail);
        }

        /// <summary>
        /// Metodo para guardar un traslado
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>
        public JsonResult SaveTraslado(Entities.ViewModels.VistaTraslados vTraslado)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string resultadoEstado = string.Empty;
            string ip=string.Empty;
            string usuario = String.Empty;
            Validations.Traslado valTraslado = new Validations.Traslado();
            bool esUnidadIgual, esNegativoSaldoOrigen;
           
            


            if (Data.Traslado.ObtenerDetalleTrasladosPorId("-1").Count == 0)
            {
                return Json(new { status = "Error", message = "Debe guardar primero el detalle del traslado" });
            }
            //Validar unidades de origen y destino iguales.
            esUnidadIgual = valTraslado.ValidarUnidadesIguales();
            if (esUnidadIgual == false)
            {
                return Json(new { status = "Error", message = "La unidad Origen y la unidad Destino no pueden ser iguales." });
            }
            //Validar unidades de origen con saldo negativo.
            esNegativoSaldoOrigen = valTraslado.ValidarSaldoNegativaOrigen();
            if (esNegativoSaldoOrigen == false)
            {
                return Json(new { status = "Error", message = "La unidad de origen no puede tener saldo negativo o cero." });
            }

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

            //Obtener periodo de transaccion.
            var periodos =
                Data.CombustiblePeriodo.ObtenerCombustiblePeirodosPorFecha(
                    vTraslado.FechaTraslado.ToString("yyyy/MM/dd")).FirstOrDefault();
            if (periodos != null)
            {
                vTraslado.IdCombustiblePeriodo = periodos.IdCombustiblePeriodo;
            }

            //if (vTraslado.IdCombustibleProveedor == 0)
            //{
            //    return Json(new { status = "Error", message = "Debe seleccionar el proveedor de combustible" });
            //}

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }



            //Asignar los datos del modelo a la entidad de dominio.
            Entities.Traslados traslado = new Traslados();

            traslado.IdCombustibleTraslado = vTraslado.IdCombustibleTraslado;
            traslado.IdPersona = eEmployee.Idhrms.ToString();
            traslado.FechaTraslado = vTraslado.FechaTraslado;
 
            traslado.FechaAnulacion = default(DateTime);
            traslado.IdCombustiblePeriodo = vTraslado.IdCombustiblePeriodo;
            //traslado.IdCombustibleProveedor = vTraslado.IdCombustibleProveedor;

            if (vTraslado.IdCombustibleTraslado == "-1")
            {
 
                result = Data.Traslado.InsertarTraslado(traslado);
                if (result.Length == 10)
                {
                    resultadoDetalle = Data.Traslado.GuardarDetalle(result);
                    if (resultadoDetalle.Length == 10)
                    {
                        Session.Remove("sDetalleTraslado");
                        Entities.TrasladosEstados trasladoEstado = new TrasladosEstados();
                        trasladoEstado.IdCombustibleTraslado = result;
                        trasladoEstado.IdEstado = "1801";
                        trasladoEstado.EsActivo = "Y";
                        trasladoEstado.IdPersona = eEmployee.Idhrms.ToString();
                        trasladoEstado.carnet = eEmployee.EmployeeNumber.ToString();
                        trasladoEstado.UsuarioDominioInserto = usuarioDominio;
                        trasladoEstado.IpLocal = ipLocal;
                        resultadoEstado = Data.TrasladoEstado.CambiarEstado(trasladoEstado);
                        if (resultadoEstado.Length == 10)
                        {
                            string resultadoCorreo = EnviarCorreo("TrasladoNuevo", result, traslado.FechaTraslado);
                            return Json(new { status = "Exito", message = "Exito al guardar el traslado" });
                        }
                       
                       
                    }

                }
            }

            else
            {
                result = Data.Traslado.ActualizarTraslado(traslado);
                if (result.Length == 10)
                {
                    resultadoDetalle = Data.Traslado.GuardarDetalle(result);
                    if (resultadoDetalle.Length == 10)
                    {
                        Session.Remove("sDetalleTraslado");
                        return Json(new { status = "Exito", message = "Exito al actualizar el traslado" });

                    }

                }

            }

            return Json(new { status = "Error", message = "Error en la transaccion" });

        }


        /// <summary>
        /// Accion para guardar el detalle del traslado en la sesion.
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>

        public ActionResult EditarDetalleTraslado(MVCxGridViewBatchUpdateValues<Entities.ViewModels.VistaTrasladosDetalle> updateValues)
        {

            //Actualizacion dedel detalle en la sesion
            foreach (var item in updateValues.Update)
            {
                if (updateValues.IsValid(item))
                {
                    try
                    {
                    

                        Data.Traslado.EditSessionDetail(item);
                    



                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                }

            }
            //Actualizacion dedel detalle en la sesion
            foreach (var item in updateValues.Insert)
            {
                if (updateValues.IsValid(item))
                {
                    try
                    {


                        Data.Traslado.AddSessionDetail(item);



                        ////Llamar al metodo EditGoal
                        //SafeExecute(() => Data.EmployeeGoal.EditGoal(item));

                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                }

            }

            //Elimiar linea de detalle de l sesion
            foreach (var id in updateValues.DeleteKeys)
            {
                string result;
                try
                {
                    var editableItem = Data.Traslado.ObtenerDetalleTrasladosPorId("-1")
                        .Where(x => x.IdCombustibleDetalleTraslado == int.Parse(id))
                        .FirstOrDefault();

                    if (editableItem.IsBdRecord == 1)
                    {
                        result = Data.Traslado.DeleteBdDetail(int.Parse(id));
                        if (result != "EXITO")
                        {
                            return Content("Error al eliminar el detalle");
                        }
                    }
                    else
                    {

                        Data.Traslado.DeleteBdDetail(int.Parse(id));
                    }




                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }



            }



            return DetailPartial();
        }

        /// <summary>
        /// Metodo para eliminar un extraplan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AnularTraslado(string idTraslado)
        {
            if (idTraslado.Contains("TRS") == false)
            {
                if (idTraslado.Length < 6)
                {
                    // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                    idTraslado = idTraslado.PadLeft(6, '0');
                }
                idTraslado = "TRS-" + idTraslado;
            }
            string result = String.Empty;

            var estado = Data.Traslado.ObtenerTrasladoPorId(idTraslado);

            if (estado.Count > 0)
            {
                string estadoTraslado = estado.FirstOrDefault().Estado;
                if (estadoTraslado != "REGISTRADO")
                {
                    return Json(new { status = "Error", message = "Solo se pueden anular traslados en estado REGISTRADO" });
                }
            }
            string ip = string.Empty;
            string usuario = String.Empty;

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Entities.TrasladosEstados trasladoEstado = new TrasladosEstados();
            trasladoEstado.IdCombustibleTraslado = idTraslado;
            trasladoEstado.IdEstado = "1803";
            trasladoEstado.EsActivo = "Y";
            trasladoEstado.IdPersona = eEmployee.Idhrms.ToString();
            trasladoEstado.UsuarioDominioInserto = usuarioDominio;
            trasladoEstado.IpLocal = ipLocal;
            result = Data.TrasladoEstado.CambiarEstado(trasladoEstado);

            if (result.Length != 10)
            {


                return Json(new { status = "Error", message = "Error en la anulación del extraplan" });


            }
            string resultadoCorreo = EnviarCorreo("TrasladoNuevo", idTraslado, estado.FirstOrDefault().FechaTraslado);
            return Json(new { status = "Exito", message = "El traslado ha sido anulado" });
        }



        /// <summary>
        /// Metodo para enviar correo
        /// </summary>
        /// <param name="tipoCorreo"></param>
        /// <param name="id"></param>
        /// <param name="fecha"></param>
        /// <returns></returns>
        public string EnviarCorreo(string tipoCorreo, string id, DateTime fecha)
        {
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            // Envio del mensaje de correo al usuario
         



            if (tipoCorreo == "TrasladoNuevo")
            {
                titulo = "Nuevo Traslado";
                mensaje = "Estimada Transporte:" +
                                  "<br/>" +
                                  "<br/>" + "Le informamos que el traslado " + id + ", solicitado por el usuario " + eEmployee.FullName + " para la fecha" +
                                  " " + fecha.ToString("dd/MM/yyyy") + ", esta pendiente de autorizar. " +
                                  "<br/>" +
                                  "<br/>" +
                                  "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                                  "<br/>" +
                                  "<br/>" + "Saludos.";
            }
            if (tipoCorreo == "TrasladoAnular")
            {
                titulo = "Anulación Traslado";
                mensaje = "Estimada  Transporte:" +
                          "<br/>" +
                          "<br/>" + "Le informamos que el traslado " + id + ", solicitado por el usuario +" + eEmployee.FullName + " para la fecha" +
                          " " + fecha.ToString("dd/MM/yyyy") + ", ha sido anulado por el usuario. " +
                          "<br/>" +
                          "<br/>" +
                          "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                          "<br/>" +
                          "<br/>" + "Saludos.";
            }


            ClaroWCF = new ServiceReference1.ClaroAsemClient();

                       resultadoCorreo = ClaroWCF.getcorreoenviar("yohana.garcia@claro.com.ni", titulo,   mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transaccion se genero exitosamente, pero ocurrió un error al enviar el correo";

            }


            return resultadoCorreo;
        }
        #endregion

    }
}