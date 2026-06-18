using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class IndicatorProductsController : Controller
    {
        #region Lista de Indicadores y Productos
        [Authorize]

        public ActionResult List()
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



        public ActionResult ListPartial()
        {
            List<Entities.IndicatorProducts> lstIndicator = new List<Entities.IndicatorProducts>();
            try
            {

                lstIndicator = Data.IndicatorProductcs.GetIndicatorProductsByManagement(343);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("ListPartial", lstIndicator);
        }
        #endregion

        #region CRUD

        /// <summary>
        /// Accion que llama  a metodo para insertar un indicador producto
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddIndicatorProduct(Entities.IndicatorProducts indicatorProduct)
        {


            if (ModelState.IsValid)
            {
                try
                {


                    //if (string.IsNullOrEmpty(events.EventName))
                    //{
                    //    return Content("El campo Evento es requerido. Por favor ingrese el dato");
                    //}

                    //if (string.IsNullOrEmpty(events.CoordinatorEmail))
                    //{
                    //    return Content("El campo Evento es requerido. Por favor ingrese el dato");
                    //}

                    //events.EventName = events.EventName.Trim();
                    //events.Description = events.Description.Trim();
                    //events.CoordinatorEmail = events.CoordinatorEmail.Trim();

                    //Llamar al metodo InsertIndicatorProduct
                    string result = Data.IndicatorProductcs.InsertIndicatorProduct(indicatorProduct);
                    if (result != "Exito al insertar el registro")
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
        /// Accion que llama  a metodo para editar un indicador producto
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditindicatorProduct(Entities.IndicatorProducts indicatorProduct)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //if (string.IsNullOrEmpty(events.EventName))
                    //{
                    //    return Content("El campo Evento es requerido. Por favor ingrese el dato");
                    //}

                    //if (string.IsNullOrEmpty(events.CoordinatorEmail))
                    //{
                    //    return Content("El correo del responsable del evento es requerido. Por favor ingrese el dato");
                    //}

                    //events.EventName = events.EventName.ToUpper().Trim();

                    //Llamar al metodo UpdateIndicatorProduct
                    string result = Data.IndicatorProductcs.UpdateIndicatorProduct(indicatorProduct);
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






        #endregion
    }
}