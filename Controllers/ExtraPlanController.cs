using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using ClosedXML.Excel;
using DevExpress.Data;
using DevExpress.Web;
using DevExpress.Web.Mvc;
using DevExpress.XtraCharts;
using DevExpress.XtraCharts.Printing;
using DevExpress.XtraPrinting;
using Entities.MyEntities;
using Newtonsoft.Json;
using RestSharp;
using slnRhonline.Models;


namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class ExtraPlanController : Controller
    {
        const string keyIdExtraPlan = "sIdExtraPlan";
        #region Lista de ExtraPlanes 
        [Authorize]

        public ActionResult List()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstExtraPlan = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                ////Obtener persona que se loguea en el sistema
                //Entities.Employees eEmployee = null;
                //if (Session["User"] != null)
                //{
                //    eEmployee = (Entities.Employees)Session["User"];
                //}
                //lstExtraPlan = Data.ExtraPlan.ObtenerExtraPlanesPorPersona(eEmployee.Idhrms.ToString());
                //return View("ExtraPlanList", lstExtraPlan);
                return View("ExtraPlanList" );
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

        }
        public ActionResult ExtraPlanList()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstExtraPlan = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                //lstExtraPlan = Data.ExtraPlan.ObtenerExtraPlanesPorPersona(eEmployee.Idhrms.ToString());
                //return View("ExtraPlanList", lstExtraPlan);
                return View();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

        }
        public JsonResult Listjson()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstExtraPlan = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                lstExtraPlan = Data.ExtraPlan.ObtenerExtraPlanesPorPersona(eEmployee.EmployeeNumber.ToString());
                return Json(new { data = lstExtraPlan }, JsonRequestBehavior.AllowGet);


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

        }
        public ActionResult EmployeeDetailM(long id)
        {


            Entities.Employees Employee = new Entities.Employees();
            Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.Idhrms == id);
            //  Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);
            // Se debera cambiar por la api de foto
            Session.Remove("sExpense");
            Session["fullName"] = Employee.FullName;
            Session["IDempleado"] = id;
            return View("RegisterDetail", Employee);
        }
        public ActionResult ListPartial()

        {
            List<Entities.ViewModels.VistaExtraPlan> lstExtraPlan = new List<Entities.ViewModels.VistaExtraPlan>();

           
            try
            {
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                lstExtraPlan = Data.ExtraPlan.ObtenerExtraPlanesPorPersona(eEmployee.EmployeeNumber.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("ExtraPlanListPartial", lstExtraPlan);
        }
        #endregion


        #region Acciones CRUD ExtraPlan
        /// <summary>
        /// Metodo para validar un extra plan
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>

        [HttpPost]
        public JsonResult ValidarExtraPlan(string idExtraPlan, string tipoEdicion)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string resultadoEstado = string.Empty;
            string ip = string.Empty;
            string usuario = String.Empty;

            if (tipoEdicion != "Nuevo")
            {
                var estado = Data.ExtraPlan.ObtenerExtraPlanById(idExtraPlan);

                if (estado.Count > 0)
                {
                    string estadoExtraPlan = estado.FirstOrDefault().Estado;
                    if (estadoExtraPlan != "1901")
                    {

                        return Json(new { status = "Error", message = "Solo se pueden editar extraplanes en estado de REGISTRADO" });
                    }
                }
            }

            return Json(new { status = "Exito", message = "" });

        }

        /// <summary>
        /// Accion que carga la informacion a la vista de encabezado del extra plan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        public ActionResult EditExtraPlan(string idExtraPlan = "-1")
        {
            if (idExtraPlan != null && idExtraPlan != "-1" && idExtraPlan != "")
            {

                if (idExtraPlan.Contains("EXP") == false)
                {
                    if (idExtraPlan.Length < 6)
                    {
                        // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                        idExtraPlan = idExtraPlan.PadLeft(6, '0');
                    }
                    idExtraPlan = "EXP-" + idExtraPlan;
                }
            }

            Session[keyIdExtraPlan] = idExtraPlan;

            Entities.ViewModels.VistaExtraPlan editExtraPlan = Data.ExtraPlan.ObtenerExtraPlanById(idExtraPlan).FirstOrDefault();
            if (editExtraPlan == null)
            {
                DateTime fechaActual = DateTime.Today;
                editExtraPlan = new Entities.ViewModels.VistaExtraPlan();
                editExtraPlan.IdCombustibleExtraPlan = "-1";
                editExtraPlan.FechaSolicitud = fechaActual;
                Session.Remove("sDetailExtraPlan");
            }
            //else
            //{
            //    var estado = Data.ExtraPlan.ObtenerExtraPlanById(idExtraPlan);

            //    if (estado.Count > 0)
            //    {
            //        string estadoExtraPlan = estado.FirstOrDefault().Estado;
            //        if (estadoExtraPlan != "1901")
            //        {
            //            ViewData["EditError"] = "Solo se pueden editar extraplanes en estado de REGISTRADO";
            //            editExtraPlan = new Entities.ViewModels.VistaExtraPlan();
            //            Session.Remove("sDetailExtraPlan");

            //        }
            //    }
            //}
            return View("Edit", editExtraPlan);
        }
        /// <summary>
        /// Accion que carga  el detalle del extra plan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        public ActionResult DetailPartial(string idExtraPlan = "-1")
        {
            idExtraPlan = (string)Session[keyIdExtraPlan];
            //Session.Remove("sDetailExtraPlan");
            List<Entities.ViewModels.ExtraPlanDetailView> lstDetailExtraPlan = new List<Entities.ViewModels.ExtraPlanDetailView>();

            try
            {

                lstDetailExtraPlan = Data.ExtraPlan.GetDetailExtraPlan(idExtraPlan);

            }



            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


            return PartialView("DetailPartial", lstDetailExtraPlan);
        }
        /// <summary>
        /// Metodo para guardar un extra plan
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>
        public JsonResult SaveExtraPlan(Entities.ViewModels.VistaExtraPlan extraPlan)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string resultadoEstado = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;
            string esFechaDetalleDuplicada = string.Empty;
            Validations.ExtraPlan vExtraPlan = new Validations.ExtraPlan();

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

            //Obtener periodo de transaccion.
            var periodos =
                Data.CombustiblePeriodo.ObtenerCombustiblePeirodosPorFecha(
                    extraPlan.FechaSolicitud.ToString("yyyy/MM/dd")).FirstOrDefault();
            if (periodos != null)
            {
                extraPlan.IdCombustiblePeriodo = periodos.IdCombustiblePeriodo;
            }

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            extraPlan.IdPersona = eEmployee.Idhrms.ToString();
            extraPlan.carnet = eEmployee.EmployeeNumber;
            //Validar total de filas
            int totalFilas = Data.ExtraPlan.GetDetailExtraPlan("-1").Count;

            if (totalFilas > 7)
            {
                
                return Json(new { status = "Error", message = "No se pueden guardar mas de una semana para un extra plan, por favor corrija." });
            }

            //Validar que las fechas no esten duplicadas.
           
            bool esFechaValida = false;
            esFechaValida = vExtraPlan.ValidarFechaDuplicada();
            if (esFechaValida == false)
            {
                return Json(new { status = "Error", message = "No se pueden grabar fechas duplicadas." });
            }
            if (string.IsNullOrEmpty(extraPlan.IdUnidad))
            {
                return Json(new { status = "Error", message = "La unidad es requerida." });
            }
            //Validar que la unidad tiene el porcentaje de consumo maximo para solicitar un extra plan.
            string respuestaPorcentajeMaximo = Data.ExtraPlan.ObtenerRespuestaPorcentajeMaximo(extraPlan.IdUnidad,
                extraPlan.IdCombustiblePeriodo);
            if (!string.IsNullOrEmpty(respuestaPorcentajeMaximo))
            {
                return Json(new { status = "Error", message = respuestaPorcentajeMaximo });
            }
            //Validar que la fecha de solicitud no se repita
            bool esFechaSolicitudValida = false;
            esFechaSolicitudValida = vExtraPlan.ValidarFechaSolicitud(extraPlan.IdUnidad,extraPlan.FechaSolicitud,extraPlan.IdCombustibleExtraPlan);
            if (esFechaSolicitudValida == false)
            {
                return Json(new { status = "Error", message = "No se pueden grabar fechas de solicitud duplicadas." });
            }

            //Validar que la fecha de actividad actual para la unidad no se repita en las fechas de actividades de la base de datos.
            esFechaDetalleDuplicada = vExtraPlan.ValidarFechaDuplicadaDetalle(extraPlan.IdUnidad);
            if (!string.IsNullOrEmpty(esFechaDetalleDuplicada))
            {
                return Json(new { status = "Error", message = esFechaDetalleDuplicada });
            }

            //if (extraPlan.IdCombustibleProveedor == 0)
            //{

            //    return Json(new { status = "Error", message = "Debe seleccionar el proveedor de combustible." });
            //}


            if (extraPlan.IdCombustibleExtraPlan == "-1")
            {
              
              
                result = Data.ExtraPlan.InsertExtraPlan(extraPlan);
                if (result.Length == 10)
                {
                    resultadoDetalle = Data.ExtraPlan.SaveBdDetailExtraPlan(result);
                    if (resultadoDetalle.Length == 10)
                    {
                        Session.Remove("sDetailExtraPlan");
                        Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                        extraPlanEstado.IdCombustibleExtraPlan = result;
                        extraPlanEstado.IdEstado = "1901";
                        extraPlanEstado.EsActivo = "Y";
                        extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                        extraPlanEstado.carnet = eEmployee.EmployeeNumber.ToString();
                        extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                        extraPlanEstado.IpLocal = ipLocal;
                        resultadoEstado = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);
                        if (resultadoEstado.Length == 10)
                        {
                            string resultadoCorreo = EnviarCorreo("ExtraPlanNuevo", result, extraPlan.FechaSolicitud);
                            return Json(new { status = "Exito", message = "Exito al guardar el extra plan" });
                        }
                       
                    }

                }
            }

            else
            {
               
                result = Data.ExtraPlan.UpdateExtraPlan(extraPlan);
                if (result.Length == 10)
                {
                    resultadoDetalle = Data.ExtraPlan.SaveBdDetailExtraPlan(result);
                    if (resultadoDetalle.Length == 10)
                    {
                        Session.Remove("sDetailExtraPlan");
                        return Json(new { status = "Exito", message = "Exito al actualizar el registro" });

                    }

                }

            }

            return Json(new { status = "Error", message = "Ocurrió un error en la transaccion, por favor verifique." });

        }

       
        /// <summary>
        /// Metodo para guardar un extraplan
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>

        public ActionResult EditDetailExtraPlan(MVCxGridViewBatchUpdateValues<Entities.ViewModels.ExtraPlanDetailView> updateValues)
        {
            
            try
            {
                //Insert en la sesion 
                foreach (var item in updateValues.Insert)
                {
                    if (updateValues.IsValid(item))
                       

                        Data.ExtraPlan.AddSessionDetail(item);
                }
                //Update en la sesión
                foreach (var item in updateValues.Update)
                {
                    if (updateValues.IsValid(item))
                        Data.ExtraPlan.EditSessionDetail(item);
                }

                // Delete en la sesion o en la Base de datos.
                foreach (var itemKey in updateValues.DeleteKeys)
                {
                    string result;
                    var editableItem = Data.ExtraPlan.GetDetailExtraPlan("-1")
                                .Where(x => x.IdCombustibleDetalleExtraPlan == int.Parse(itemKey))
                                .FirstOrDefault();

                    if (editableItem.IsBdRecord == 1)
                    {
                        result = Data.ExtraPlan.DeleteBdDetail(int.Parse(itemKey));
                        if (result != "EXITO")
                        {
                            return Content("Error al eliminar el detalle");
                        }
                    }
                    else
                    {

                        Data.ExtraPlan.DeleteSessionDetail(int.Parse(itemKey));
                    }
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            ////Actualizacion dedel detalle en la sesion
            //foreach (var item in updateValues.Update)
            //{

            //    if (updateValues.IsValid(item))
            //    {
            //        try
            //        {
            //            Entities.ViewModels.ExtraPlanDetailView detailItem = new Entities.ViewModels.ExtraPlanDetailView();


            //            Data.ExtraPlan.EditSessionDetail(item);



            //            ////Llamar al metodo EditGoal
            //            //SafeExecute(() => Data.EmployeeGoal.EditGoal(item));

            //        }
            //        catch (Exception e)
            //        {
            //            ViewData["EditError"] = e.Message;
            //        }
            //    }

            //}
            ////Actualizacion dedel detalle en la sesion
            //foreach (var item in updateValues.Insert)
            //{
            //    if (updateValues.IsValid(item))
            //    {
            //        try
            //        {


            //            Data.ExtraPlan.AddSessionDetail(item);



            //            ////Llamar al metodo EditGoal
            //            //SafeExecute(() => Data.EmployeeGoal.EditGoal(item));

            //        }
            //        catch (Exception e)
            //        {
            //            ViewData["EditError"] = e.Message;
            //        }
            //    }

            //}

            ////Elimiar linea de detalle de l sesion
            //foreach (var id in updateValues.DeleteKeys)
            //{
            //    string result;
            //    try
            //    {
            //        var editableItem = Data.ExtraPlan.GetDetailExtraPlan("-1")
            //            .Where(x => x.IdCombustibleDetalleExtraPlan == int.Parse(id))
            //            .FirstOrDefault();

            //        if (editableItem.IsBdRecord == 1)
            //        {
            //            result = Data.ExtraPlan.DeleteBdDetail(int.Parse(id));
            //            if (result != "EXITO")
            //            {
            //                return Content("Error al eliminar el detalle");
            //            }
            //        }
            //        else
            //        {

            //            Data.ExtraPlan.DeleteSessionDetail(int.Parse(id));
            //        }




            //    }
            //    catch (Exception e)
            //    {
            //        ViewData["EditError"] = e.Message;
            //    }



            //}



            return DetailPartial("-1");
        }


        /// <summary>
        /// Metodo para eliminar un extraplan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AnularExtraPlan(string idExtraPlan)
        {  if (idExtraPlan != null && idExtraPlan != "-1" && idExtraPlan != "")
            {

                if (idExtraPlan.Contains("EXP") == false)
                {
                    if (idExtraPlan.Length < 6)
                    {
                        // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                        idExtraPlan = idExtraPlan.PadLeft(6, '0');
                    }
                    idExtraPlan = "EXP-" + idExtraPlan;
                }
            }

            string result = String.Empty;
            
            string ip = string.Empty;
            string usuario = string.Empty;
            var estado = Data.ExtraPlan.ListarExtraPlanes(idExtraPlan);

            if (estado.Count > 0)
            {
                string estadoEx = estado.FirstOrDefault().Estado;
                if (estadoEx != "REGISTRADO")
                {
                    return Json(new { status = "Error", message = "Solo se pueden eliminar extra planes en estado REGISTRADO" });
                }
            }

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
            extraPlanEstado.IdCombustibleExtraPlan = idExtraPlan;
            extraPlanEstado.IdEstado = "1906";
            extraPlanEstado.EsActivo = "Y";
            extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
            extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
            extraPlanEstado.IpLocal = ipLocal;
            result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

       

            if (result.Length != 10)
            {


                return Json(new { status = "Error", message = "Error en la eliminación del extraplan" });


            }
            string resultadoCorreo = EnviarCorreo("ExtraPlanAnular", idExtraPlan, estado.FirstOrDefault().FechaSolicitud);
            return Json(new { status = "Exito", message = "El extra plan ha sido eliminado" });
        }

        #endregion


        public ActionResult GetSiafEmployees(string textField, string valueField)
        {

            return GridViewExtension.GetComboBoxCallbackResult(p => {
                                                                        p.TextField = textField;
                                                                        p.ValueField = valueField;
                                                                        p.BindList(slnRhonline.Data.ExtraPlan.GetSiafEmployees());
            });
        }

        public ActionResult GetAssigmnentCarsByManagement(string idPersona, string textField, string valueField)
        {

            return GridViewExtension.GetComboBoxCallbackResult(p => {
                                                                        p.TextField = textField;
                                                                        p.ValueField = valueField;
                                                                        p.BindList(slnRhonline.Data.ExtraPlan.GetCarsByManagement());
            });
        }

        #region Autorizaciones de jefe

        [Authorize]
        [HttpGet]

        public ActionResult AuthorizeBoss()
        {
             try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                //Entities.Employees eEmployee = null;
                //if (Session["User"] != null)
                //{
                //    eEmployee = (Entities.Employees)Session["User"];
                //}
                //var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                //if (managementResult != null)
                //{
                //    string gerencia = managementResult.FirstOrDefault().Gerencia;
                //    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1901",
                //        eEmployee.Idhrms.ToString());

                //}

                //else
                //{
                //    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                //}



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View( );
        }
        public JsonResult autorizarExtraPlanListJson()
        {
            try
            {
                // Verifica que el usuario esté autenticado
                if (Session["User"] == null)
                {
                    return Json(new { data = new List<Entities.ViewModels.VistaExtraPlan>(), error = "Usuario no autenticado" },
                                JsonRequestBehavior.AllowGet);
                }

                Entities.Employees eEmployee = (Entities.Employees)Session["User"];

                // Obtiene la gestión según el empleado
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();

                if (managementResult != null && managementResult.Any())
                {
                    string gerencia = managementResult.First().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1901", eEmployee.Idhrms.ToString());
                }

                // Retorna el JSON con la propiedad "data"
                return Json(new { data = lstDetail }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // En caso de error, devuelve también un mensaje de error en el JSON
                return Json(new { data = new List<Entities.ViewModels.VistaExtraPlan>(), error = ex.Message },
                            JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeBossPartial()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1901",
                        eEmployee.Idhrms.ToString());

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeBossPartial", lstDetail);

        }
        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes pero destruyendo la sesion.
        /// </summary>
        /// <returns></returns>
        public ActionResult RefreshPartial()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1901",
                        eEmployee.Idhrms.ToString());

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeBossPartial", lstDetail);

        }

        /// <summary>
        /// Metodo para autorizar un extraplan por el jefe inmediato
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeBoss(string ids)
        {
            string result = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    //Obtener IPlocal
                    string ipLocal = Utils.ObtenerIpLocal(ip);
                    //Obtener usuario de dominio
                    string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

                    Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                    extraPlanEstado.IdCombustibleExtraPlan = keyAuthorize;
                    extraPlanEstado.IdEstado = "1902";
                    extraPlanEstado.EsActivo = "Y";
                    extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                    extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                    extraPlanEstado.IpLocal = ipLocal;
                    result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

                    //Entities.ViewModels.VistaExtraPlan extraPlan = new VistaExtraPlan();
                    //extraPlan.IdCombustibleExtraPlan = keyAuthorize;
                    //extraPlan.Estado = "AUTORIZADO POR JEFE";
                    //result = Data.ExtraPlan.ChangeState(extraPlan);

                    if (result.Length != 10)
                    {


                        return Json(new { status = "Error", message = "Error en la autorizacion" });


                    }

                  
                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }


        /// <summary>
        /// Metodo para denegar un extra plan por el jefe inmediato
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedBoss(string ids)
        {
            string result = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    //Obtener IPlocal
                    string ipLocal = Utils.ObtenerIpLocal(ip);
                    //Obtener usuario de dominio
                    string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

                    Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                    extraPlanEstado.IdCombustibleExtraPlan = keyAuthorize;
                    extraPlanEstado.IdEstado = "1907";
                    extraPlanEstado.EsActivo = "Y";
                    extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                    extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                    extraPlanEstado.IpLocal = ipLocal;
                    result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

                    if (result.Length != 10)
                    {


                        return Json(new { status = "Error", message = "Error en denegar el extra plan" });


                    }

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }


        #endregion
        #region Autorizaciones de gerente

        [Authorize]
        [HttpGet]

        public ActionResult AuthorizeManagement()
        {
         
            return View( );
        }

        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeManagementPartial()
        {
            Session.Remove("sExtraPlanAuthorize");
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
               // var managementResult = Data.ConsumptionClaro.GetManagementByEmployee("48353");
               var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902",
                        eEmployee.Idhrms.ToString());
                    //lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902",
                    //    "48353");

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeManagementPartial", lstDetail);

        }
        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes pero destruyendo la sesion.
        /// </summary>
        /// <returns></returns>
        public ActionResult RefreshManagementPartial()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902",
                        eEmployee.Idhrms.ToString());

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeManagementPartial", lstDetail);

        }

        /// <summary>
        /// Metodo para autorizar un extraplan por el gerente
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeManagement(string ids)
        {
            string result = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    //Obtener IPlocal
                    string ipLocal = Utils.ObtenerIpLocal(ip);
                    //Obtener usuario de dominio
                    string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

                    Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                    extraPlanEstado.IdCombustibleExtraPlan = keyAuthorize;
                    extraPlanEstado.IdEstado = "1903";
                    extraPlanEstado.EsActivo = "Y";
                    extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                    extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                    extraPlanEstado.IpLocal = ipLocal;
                    result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

                    if (result.Length != 10)
                    {


                        return Json(new { status = "Error", message = result });


                    }


                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }
            else
            {
                return Json(new { status = "Error", message = "El id del extra plan esta vacio" });
            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }


        /// <summary>
        /// Metodo para denegar un extra plan por el jefe inmediato
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedMangagement(string ids)
        {
            string result = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    //Obtener IPlocal
                    string ipLocal = Utils.ObtenerIpLocal(ip);
                    //Obtener usuario de dominio
                    string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

                    Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                    extraPlanEstado.IdCombustibleExtraPlan = keyAuthorize;
                    extraPlanEstado.IdEstado = "1908";
                    extraPlanEstado.EsActivo = "Y";
                    extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                    extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                    extraPlanEstado.IpLocal = ipLocal;
                    result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

                    if (result.Length != 10)
                    {



                        return Json(new { status = "Error", message = "Error en denegar el extraplan" });


                    }

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }


        #endregion
        #region Consulta de Detalle Extra Plan de las autorizaciones

        public ActionResult AuthorizeConsult(string idExtraPlan)
        {

            List<Entities.ViewModels.ExtraPlanDetailView> lstExpenseDetail =
                new List<Entities.ViewModels.ExtraPlanDetailView>();

            lstExpenseDetail = Data.ExtraPlan.GetDetailExtraPlan(idExtraPlan);


     
            return View("AuthorizeConsult", lstExpenseDetail);
        }

        /// <summary>
        /// Accion que retorna la vista parcial AuthorizeConsultPartial
        /// </summary>
        /// <param name="_expenseId"></param>
        /// <returns></returns>
        public ActionResult AuthorizeConsultPartial(string idExtraPlan)
        {
            List<Entities.ViewModels.ExtraPlanDetailView> lstExpenseDetail =
                new List<Entities.ViewModels.ExtraPlanDetailView>();

            lstExpenseDetail = Data.ExtraPlan.GetDetailExtraPlan(idExtraPlan);


            return PartialView("AuthorizeConsultPartial", lstExpenseDetail);
        }

        #endregion

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
            //string destinatario = eEmployee.FullName;

            //string nombreDestinatario = eEmployee.FirstName;

            //string nombreDestinatarioMinuscula = nombreDestinatario.ToLower();
            //string nombreDestinatarioPrimeraMayuscula = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nombreDestinatarioMinuscula);




            if (tipoCorreo == "ExtraPlanNuevo")
            {
                titulo = "Nuevo Extra Plan";
                mensaje = "Estimada Yohana García:" +
                                "<br/>" +
                                "<br/>" + "Le informamos que el extra plan " + id + ", solicitado por el usuario +" + eEmployee.FullName + " para la fecha" +
                                " " + fecha.ToString("dd/MM/yyyy") + ", esta pendiente por autorizar. " +
                                "<br/>" +
                                "<br/>" +
                                "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                                "<br/>" +
                                "<br/>" + "Saludos.";
            }
            if (tipoCorreo == "ExtraPlanAnular")
            {
                titulo = "Anulación Extra Plan";
                mensaje = "Estimada Yohana García:" +
                          "<br/>" +
                          "<br/>" + "Le informamos que el extra plan " + id + ", solicitado por el usuario +" + eEmployee.FullName + " para la fecha" +
                          " " + fecha.ToString("dd/MM/yyyy") + ", ha sido anulado por el usuario. " +
                          "<br/>" +
                          "<br/>" +
                          "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                          "<br/>" +
                          "<br/>" + "Saludos.";
            }



            resultadoCorreo = Utils.EnviarCorreoUsuario("yohana.garcia@claro.com.ni", titulo, "candida.sanchez@claro.com.ni", mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transaccion se genero exitosamente, pero ocurrió un error al enviar el correo";

            }


            return resultadoCorreo;
        }




    }
}