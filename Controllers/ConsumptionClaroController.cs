using DevExpress.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities;
using slnRhonline.Reports;

namespace slnRhonline.Controllers
{
    public class ConsumptionClaroController : Controller
    {
        #region Lista de Consumo


        // <summary>
        /// Metodo que devuelve la lista de consumo por persona a la vista primaria
        /// </summary>
        /// <returns></returns>
        [Authorize]

        public ActionResult List()
        {
            List<Entities.ConsumptionClaro> lstConsumption = new List<Entities.ConsumptionClaro>();
            try
            {
                lstConsumption = Data.ConsumptionClaro.GetConsumptionByPerson();
                return View("List", lstConsumption);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

        }

        /// <summary>
        /// Metodo que devuelve la lista de consumo por persona a la vista parcial
        /// </summary>
        /// <returns></returns>
        public ActionResult ListPartial()

        {
            List<Entities.ConsumptionClaro> lstConsumption = new List<Entities.ConsumptionClaro>();


            try
            {

                lstConsumption = Data.ConsumptionClaro.GetConsumptionByPerson();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("ListPartial", lstConsumption);
        }

        public JsonResult SaveConsumption(Entities.ConsumptionClaro consumption)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;

            if (Session["SupportFileBytes"] != null)
            {

                consumption.SupportFile = (byte[]) Session["SupportFileBytes"];

            }
            if (consumption.FechaRegistro == default(DateTime))
            {
                return Json(new { status = "Error", message = "La fecha es requerida" });
            }
            if (consumption.IdVoucher == null)
            {
                return Json(new {status = "Error", message = "La referencia es requerida"});
            }
            if ((consumption.IdVoucher != null) && (consumption.IdCombustibleConsumoClaro == -1))
            {
                List<Entities.ConsumptionClaro> lstConsumo = new List<Entities.ConsumptionClaro>();
                lstConsumo = Data.ConsumptionClaro.GetConsumptionByVoucher(consumption.IdVoucher);
                if (lstConsumo.Count > 0 && lstConsumo.Any(x => x.FechaRegistro.Year == consumption.FechaRegistro.Year))
                {
                    return Json(new {status = "Error", message = "Ya existe ese numero de referencia"});
                }

            }
            if (consumption.IdUnidad == null)
            {
                return Json(new {status = "Error", message = "La unidad es requerida"});
            }
         
            if (consumption.CantidadLitros == 0)
            {
                return Json(new {status = "Error", message = "La cantidad es requerida"});
            }

            if (consumption.ValorCordobas == 0)
            {
                return Json(new {status = "Error", message = "El total de la factura es requerido"});
            }
            if (consumption.OdometroInicial == 0)
            {
                return Json(new {status = "Error", message = "El odómetro Inicial es requerido"});
            }
            if (consumption.IdTipoCombustible == null)
            {
                return Json(new {status = "Error", message = "El tipo de combustible es requerido"});
            }
            if (consumption.Cedula == null)
            {
                return Json(new {status = "Error", message = "La cédula es requerida"});
            }
            if (consumption.Estacion == null)
            {
                return Json(new {status = "Error", message = "La estación es requerida"});
            }
            if (consumption.Municipio == null)
            {
                return Json(new {status = "Error", message = "El municipio es requerido"});
            }
            if (consumption.IdDepartamento == null)
            {
                return Json(new {status = "Error", message = "El departamento es requerido"});
            }
            if (consumption.SupportFile == null)
            {
                return Json(new {status = "Error", message = "El voucher es requerido"});
            }


            if (consumption.IdCombustibleConsumoClaro == -1)
            {

                //return Json(new { status = "Exito", message = "Exito al insertar el registro" });
                result = Data.ConsumptionClaro.InsertConsumption(consumption);
                if (result == "EXITO")
                {


                    return Json(new {status = "Exito", message = "Exito al registrar el consumo"});


                }
            }

            else
            {
                result = Data.ConsumptionClaro.UpdateConsumption(consumption);
                if (result == "EXITO")
                {

                    return Json(new {status = "Exito", message = "Exito al actualizar el registro"});


                }

            }

            return Json(new {status = "Error", message = "campo vacio"});
        }

        /// <summary>
        /// Metodo para eliminar un consumo
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedByUser(int consumptionId)
        {

            string result = String.Empty;

            var estadoConsumo = Data.ConsumptionClaro.GetConsumptionById(consumptionId);

            if (estadoConsumo.Count > 0)
            {
                string estado = estadoConsumo.FirstOrDefault().Estado;
                if (estado != "1501")
                {
                    return Json(new { status = "Error", message = "Solo se pueden anular consumos en estado REGISTRADO" });
                }
            }

            result = Data.ConsumptionClaro.DeniedByUser(consumptionId);

            if (result != "EXITO")
            {


                return Json(new { status = "Error", message = "Error en la autorizacion" });


            }
            return Json(new { status = "Exito", message = "El registro ha sido denegado" });
        }

        public ActionResult ExternalEditFormEdit(int consumptionId = -1)
        {

            Entities.ConsumptionClaro editConsumption =
                Data.ConsumptionClaro.GetConsumptionByPerson().Where(x => x.IdCombustibleConsumoClaro == consumptionId)
                    .FirstOrDefault();
            if (editConsumption == null)
            {
                editConsumption = new Entities.ConsumptionClaro();
                editConsumption.IdCombustibleConsumoClaro = -1;
            }
            return View("Edit", editConsumption);
        }

        public ActionResult GetEmployeeName(string idUnidad)
        {
            List<Entities.ViewModels.AssignmentCarsView> lstCars = new List<Entities.ViewModels.AssignmentCarsView>();
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees) Session["User"];
            }
            lstCars = Data.ExtraPlan.GetCarsByManagement();
            var unidad = lstCars.Where(x => x.IdUnidad == idUnidad).FirstOrDefault();
            if (unidad != null)
            {
                string employeeName = lstCars.Where(x => x.IdUnidad == idUnidad).FirstOrDefault().NombreEmpleado;
                return Json(employeeName, JsonRequestBehavior.AllowGet);
            }
            else
            {
                string employeeName = string.Empty;
                return Json(employeeName, JsonRequestBehavior.AllowGet);
            }



        }

        public ActionResult GetAssigmnentCarsByManagement(string idPersona, string textField, string valueField)
        {
            //Entities.Employees eEmployee = null;
            //if (Session["User"] != null)
            //{
            //    eEmployee = (Entities.Employees) Session["User"];
            //}
            return GridViewExtension.GetComboBoxCallbackResult(p =>
            {
                p.TextField = textField;
                p.ValueField = valueField;
                p.BindList(slnRhonline.Data.ExtraPlan.GetCarsByManagement());
            });
        }

        #endregion

        #region  Soporte de Consumo

        /// <summary>
        /// Accion para subir el soporte de horas extras que llama a los metodos YieldFileUploadValidationSettings y  YieldFileUploadComplete
        /// </summary>
        /// <returns></returns>
        public ActionResult SupportUpload()
        {
            UploadControlExtension.GetUploadedFiles("ucVoucherSupport",
                slnRhonline.Data.ConsumptionClaro.SupportUploadValidationSettings,
                slnRhonline.Data.ConsumptionClaro.SupportUploadComplete);
            return null;
        }

        

        /// <summary>
        /// Metodo para cargar archivo de consumo
        /// </summary>
        /// <param name="idConsumo"></param>
        /// <returns></returns>
        public ActionResult LoadSupport(int idConsumo)
        {
            try
            {
                Entities.ConsumptionClaro consumo = new ConsumptionClaro();
                //Entities.Expenses eExpense = new Entities.Expenses();
                consumo =
                    Data.ConsumptionClaro.GetConsumptionById(idConsumo)
                        .FirstOrDefault();
                //eExpense = Utils.ClaroWCF.GetAllExpenses(expenseId).FirstOrDefault();
                //if (eExpense.YieldFileExtension == "pdf")
                //{
                byte[] byteArray = consumo.SupportFile;
                if (byteArray == null)
                {
                    return null;
                }

                var strBase64 = Convert.ToBase64String(byteArray);
                Entities.ConsumptionClaro newConsumo = new ConsumptionClaro();
                newConsumo.Municipio = string.Format("data:application/pdf;base64,{0}", strBase64);
                return PartialView("SupportPartial", newConsumo);
                
            }
            catch (Exception e)
            {
                throw new Exception("Error al cargar el objeto", e);
            }
        }

        public ViewResult ReportParameters()
        {
            Session.Remove("sConsumptionParameter");

            //ViewData["startPeriod"] = "01/01/2017";
            return View();
        }

        public ActionResult ConsumptionReport(Entities.MyEntities.ConsumptionParameters eParameter)
        {
            Session["sConsumptionParameters"] = eParameter;

            if (ModelState.IsValid)
            {
                try
                {
                    return View("ConsumptionByDate");
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Error en la operacion";
            }

            return View("ReportParameters");
        }

        //public ActionResult ConsumptionByDatePartial()
        //{
        //    List<Entities.ViewModels.ConsumptionView> model = new List<Entities.ViewModels.ConsumptionView>();
        //    try
        //    {
        //        Entities.Employees eEmployee = null;
        //        if (Session["User"] != null)
        //        {
        //            eEmployee = (Entities.Employees) Session["User"];
        //        }

        //        if (eEmployee.Id_HRMS != 64922 && eEmployee.Id_HRMS.ToString() != "42382" &&
        //            eEmployee.Id_HRMS.ToString() != "68606" && eEmployee.Id_HRMS.ToString() != "402035" &&
        //            eEmployee.Id_HRMS.ToString() != "9403")
        //        {
        //            var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Id_HRMS.ToString());
        //            if (managementResult != null)
        //            {

        //                Entities.MyEntities.ConsumptionParameters consumptionParameter =
        //                    new Entities.MyEntities.ConsumptionParameters();
        //                consumptionParameter =
        //                    (Entities.MyEntities.ConsumptionParameters) Session["sConsumptionParameters"];


        //                var result = Data.ConsumptionClaro.GetAllConsumption(managementResult.FirstOrDefault().Gerencia,
        //                    consumptionParameter.StartDate, consumptionParameter.EndDate,consumptionParameter.StatusId);

        //                if (result != null)
        //                {
        //                    foreach (var item in result)
        //                    {
        //                        Entities.ViewModels.ConsumptionView consumo = new Entities.ViewModels.ConsumptionView();
        //                        consumo.IdCombustibleConsumoClaro = item.IdCombustibleConsumoClaro;
        //                        consumo.IdVoucher = item.IdVoucher;
        //                        consumo.IdPersona = item.IdPersona;
        //                        consumo.IdUnidad = item.IdUnidad;
        //                        consumo.FechaRegistro = item.FechaRegistro;
        //                        consumo.CantidadLitros = item.CantidadLitros;
        //                        consumo.PrecioLitros = item.PrecioLitros;
        //                        consumo.ValorCordobas = item.ValorCordobas;
        //                        consumo.Estacion = item.Estacion;
        //                        consumo.IdTipoCombustible = item.IdTarjeta;
        //                        consumo.Municipio = item.Municipio;
        //                        consumo.IdDepartamento = item.IdDepartamento;
        //                        consumo.OdometroInicial = item.OdometroInicial;
        //                        consumo.NombreEmpleado = item.NombreEmpleado;
        //                        consumo.Cedula = item.Cedula;
        //                        consumo.Gerencia = item.Gerencia;
        //                        consumo.SubGerencia = item.SubGerencia;
        //                        consumo.FechaFin = consumptionParameter.EndDate;
        //                        consumo.FechaInicio = consumptionParameter.StartDate;

        //                        model.Add(consumo);
        //                    }

        //                }
        //                else
        //                {
        //                    ViewData["EditError"] = "Sesión nula";
        //                }
        //            }




        //        }
        //        else
        //        {
        //            Entities.MyEntities.ConsumptionParameters consumptionParameter =
        //                new Entities.MyEntities.ConsumptionParameters();
        //            consumptionParameter = (Entities.MyEntities.ConsumptionParameters) Session["sConsumptionParameters"];
        //            var result = Data.ConsumptionClaro.GetAllConsumption(null,
        //                consumptionParameter.StartDate, consumptionParameter.EndDate,consumptionParameter.StatusId);

        //            if (result != null)
        //            {
        //                foreach (var item in result)
        //                {
        //                    Entities.ViewModels.ConsumptionView consumo = new Entities.ViewModels.ConsumptionView();
        //                    consumo.IdCombustibleConsumoClaro = item.IdCombustibleConsumoClaro;
        //                    consumo.IdVoucher = item.IdVoucher;
        //                    consumo.IdPersona = item.IdPersona;
        //                    consumo.IdUnidad = item.IdUnidad;
        //                    consumo.FechaRegistro = item.FechaRegistro;
        //                    consumo.CantidadLitros = item.CantidadLitros;
        //                    consumo.PrecioLitros = item.PrecioLitros;
        //                    consumo.ValorCordobas = item.ValorCordobas;
        //                    consumo.Estacion = item.Estacion;
        //                    consumo.IdTipoCombustible = item.IdTarjeta;
        //                    consumo.Municipio = item.Municipio;
        //                    consumo.IdDepartamento = item.IdDepartamento;
        //                    consumo.OdometroInicial = item.OdometroInicial;
        //                    consumo.NombreEmpleado = item.NombreEmpleado;
        //                    consumo.Cedula = item.Cedula;
        //                    consumo.Gerencia = item.Gerencia;
        //                    consumo.SubGerencia = item.SubGerencia;
        //                    consumo.FechaFin = consumptionParameter.EndDate;
        //                    consumo.FechaInicio = consumptionParameter.StartDate;

        //                    model.Add(consumo);
        //                }
        //            }

        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ViewData["EditError"] = e.Message;
        //    }

        //    return PartialView("ConsumptionByDatePartial", model);
        //}

        //public ActionResult ConsumptionByDateExport()
        //{
        //    ConsumptionByDate report = new ConsumptionByDate();
        //    List<Entities.ViewModels.ConsumptionView> model = new List<Entities.ViewModels.ConsumptionView>();
        //    try
        //    {
        //        Entities.Employees eEmployee = null;
        //        if (Session["User"] != null)
        //        {
        //            eEmployee = (Entities.Employees)Session["User"];
        //        }
        //        if (eEmployee.Id_HRMS != 64922 && eEmployee.Id_HRMS.ToString() != "42382" &&
        //            eEmployee.Id_HRMS.ToString() != "68606" && eEmployee.Id_HRMS.ToString() != "402035" &&
        //            eEmployee.Id_HRMS.ToString() != "9403")
        //        {
        //            var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Id_HRMS.ToString());
        //            if (managementResult != null)
        //            {

        //                Entities.MyEntities.ConsumptionParameters consumptionParameter =
        //                    new Entities.MyEntities.ConsumptionParameters();
        //                consumptionParameter = (Entities.MyEntities.ConsumptionParameters)Session["sConsumptionParameters"];


        //                var result = Data.ConsumptionClaro.GetAllConsumption(managementResult.FirstOrDefault().Gerencia,
        //                    consumptionParameter.StartDate, consumptionParameter.EndDate,consumptionParameter.StatusId);

        //                if (result != null)
        //                {
        //                    foreach (var item in result)
        //                    {
        //                        Entities.ViewModels.ConsumptionView consumo = new Entities.ViewModels.ConsumptionView();
        //                        consumo.IdCombustibleConsumoClaro = item.IdCombustibleConsumoClaro;
        //                        consumo.IdVoucher = item.IdVoucher;
        //                        consumo.IdPersona = item.IdPersona;
        //                        consumo.IdUnidad = item.IdUnidad;
        //                        consumo.FechaRegistro = item.FechaRegistro;
        //                        consumo.CantidadLitros = item.CantidadLitros;
        //                        consumo.PrecioLitros = item.PrecioLitros;
        //                        consumo.ValorCordobas = item.ValorCordobas;
        //                        consumo.Estacion = item.Estacion;
        //                        consumo.IdTipoCombustible = item.IdTarjeta;
        //                        consumo.Municipio = item.Municipio;
        //                        consumo.IdDepartamento = item.IdDepartamento;
        //                        consumo.OdometroInicial = item.OdometroInicial;
        //                        consumo.NombreEmpleado = item.NombreEmpleado;
        //                        consumo.Cedula = item.Cedula;
        //                        consumo.Gerencia = item.Gerencia;
        //                        consumo.SubGerencia = item.SubGerencia;
        //                        consumo.FechaFin = consumptionParameter.EndDate;
        //                        consumo.FechaInicio = consumptionParameter.StartDate;

        //                        model.Add(consumo);
        //                    }

        //                }

        //                else
        //                {
        //                    ViewData["EditError"] = "Sesión nula";
        //                }
        //            }
        //        }
        //        else
        //        {
                    
        //                Entities.MyEntities.ConsumptionParameters consumptionParameter =
        //                    new Entities.MyEntities.ConsumptionParameters();
        //                consumptionParameter = (Entities.MyEntities.ConsumptionParameters)Session["sConsumptionParameters"];
        //                var result = Data.ConsumptionClaro.GetAllConsumption(null,
        //                    consumptionParameter.StartDate, consumptionParameter.EndDate,consumptionParameter.StatusId);

        //                if (result != null)
        //                {
        //                    foreach (var item in result)
        //                    {
        //                        Entities.ViewModels.ConsumptionView consumo = new Entities.ViewModels.ConsumptionView();
        //                        consumo.IdCombustibleConsumoClaro = item.IdCombustibleConsumoClaro;
        //                        consumo.IdVoucher = item.IdVoucher;
        //                        consumo.IdPersona = item.IdPersona;
        //                        consumo.IdUnidad = item.IdUnidad;
        //                        consumo.FechaRegistro = item.FechaRegistro;
        //                        consumo.CantidadLitros = item.CantidadLitros;
        //                        consumo.PrecioLitros = item.PrecioLitros;
        //                        consumo.ValorCordobas = item.ValorCordobas;
        //                        consumo.Estacion = item.Estacion;
        //                        consumo.IdTipoCombustible = item.IdTarjeta;
        //                        consumo.Municipio = item.Municipio;
        //                        consumo.IdDepartamento = item.IdDepartamento;
        //                        consumo.OdometroInicial = item.OdometroInicial;
        //                        consumo.NombreEmpleado = item.NombreEmpleado;
        //                        consumo.Cedula = item.Cedula;
        //                        consumo.Gerencia = item.Gerencia;
        //                        consumo.SubGerencia = item.SubGerencia;
        //                        consumo.FechaFin = consumptionParameter.EndDate;
        //                        consumo.FechaInicio = consumptionParameter.StartDate;

        //                        model.Add(consumo);
        //                    }
        //                }

                    

        //        }

        //    report.DataSource = model;
                
        //    }
        //    catch (Exception e)
        //    {
        //        ViewData["EditError"] = e.Message;
        //    }



        //    return ReportViewerExtension.ExportTo(report);


        //}
        #endregion
        #region Revision de gerencia

        [Authorize]
        public ActionResult Check()
        {
            List<Entities.ViewModels.ConsumptionView> lstConsumption = new List<Entities.ViewModels.ConsumptionView>();
            try
            {

                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                if (eEmployee.Idhrms != 64922 && eEmployee.Idhrms.ToString() != "42382" &&
                    eEmployee.Idhrms.ToString() != "68606" && eEmployee.Idhrms.ToString() != "402035" &&
                    eEmployee.Idhrms.ToString() != "9403")
                {
                    var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                    if (managementResult != null)
                    {
                        lstConsumption = Data.ConsumptionClaro.GetConsumptionByStateManagement("1501",
                            managementResult.FirstOrDefault().Gerencia);
                    }
                }
                else
                {
                    lstConsumption = Data.ConsumptionClaro.GetConsumptionByStateManagement("1501",
                        null);
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstConsumption);
        }

        public ActionResult CheckPartial()
        {
            List<Entities.ViewModels.ConsumptionView> lstConsumption = new List<Entities.ViewModels.ConsumptionView>();
            try
            {

                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                if (eEmployee.Idhrms != 64922 && eEmployee.Idhrms.ToString() != "42382" &&
                    eEmployee.Idhrms.ToString() != "68606" && eEmployee.Idhrms.ToString() != "402035" &&
                    eEmployee.Idhrms.ToString() != "9403")
                {
                    var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                    if (managementResult != null)
                    {
                        lstConsumption = Data.ConsumptionClaro.GetConsumptionByStateManagement("1501",
                            managementResult.FirstOrDefault().Gerencia);
                    }
                }
                else
                {
                    lstConsumption = Data.ConsumptionClaro.GetConsumptionByStateManagement("1501",
                        null);
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("CheckPartial", lstConsumption);

        }

        public JsonResult AuthorizeManagement(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    result = Data.ConsumptionClaro.AuthorizeManagement(int.Parse(keyAuthorize));

                    if (result != "EXITO")
                    {


                        return Json(new { status = "Error", message = "Error en la autorizacion" });


                    }

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }
        public JsonResult DeniedManagement(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    result = Data.ConsumptionClaro.DeniedManagement(int.Parse(keyAuthorize));

                    if (result != "EXITO")
                    {


                        return Json(new { status = "Error", message = "Error en la autorizacion" });


                    }

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }
        
        #endregion
        #region Revision de Recursos Humanos

        [Authorize]
        public ActionResult CheckRh()
        {
            List<Entities.ViewModels.ConsumptionView> lstConsumption = new List<Entities.ViewModels.ConsumptionView>();
            try
            {

                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                if (eEmployee.Idhrms != 64922 && eEmployee.Idhrms.ToString() != "42382" &&
                    eEmployee.Idhrms.ToString() != "68606" && eEmployee.Idhrms.ToString() != "402035" &&
                    eEmployee.Idhrms.ToString() != "9403")
                {
                    var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                    if (managementResult != null)
                    {
                        lstConsumption = Data.ConsumptionClaro.GetConsumptionByStateManagement("1502",
                            managementResult.FirstOrDefault().Gerencia);
                    }
                }
                else
                {
                    lstConsumption = Data.ConsumptionClaro.GetConsumptionByStateManagement("1502",
                        null);
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstConsumption);
        }

        public ActionResult CheckRhPartial()
        {
            List<Entities.ViewModels.ConsumptionView> lstConsumption = new List<Entities.ViewModels.ConsumptionView>();
            try
            {

                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                if (eEmployee.Idhrms != 64922 && eEmployee.Idhrms.ToString() != "42382" &&
                    eEmployee.Idhrms.ToString() != "68606" && eEmployee.Idhrms.ToString() != "402035" &&
                    eEmployee.Idhrms.ToString() != "9403")
                {
                    var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                    if (managementResult != null)
                    {
                        lstConsumption = Data.ConsumptionClaro.GetConsumptionByStateManagement("1502",
                            managementResult.FirstOrDefault().Gerencia);
                    }
                }
                else
                {
                    lstConsumption = Data.ConsumptionClaro.GetConsumptionByStateManagement("1502",
                        null);
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("CheckRhPartial", lstConsumption);

        }

        public JsonResult AuthorizeRh(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    result = Data.ConsumptionClaro.AuthorizeRh(int.Parse(keyAuthorize));

                    if (result != "EXITO")
                    {


                        return Json(new { status = "Error", message = "Error en la autorizacion" });


                    }

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }
        public JsonResult DeniedRh(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    result = Data.ConsumptionClaro.DeniedRh(int.Parse(keyAuthorize));

                    if (result != "EXITO")
                    {


                        return Json(new { status = "Error", message = "Error en la autorizacion" });


                    }

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar regitros" });
        }

        #endregion

    }


}