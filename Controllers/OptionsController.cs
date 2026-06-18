using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class OptionsController : Controller
    {
        #region Lista de Usuarios

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
            List<Entities.Options> lstOption = new List<Entities.Options>();
            try
            {
                lstOption = Data.Option.GetAllOptions();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("ListPartial", lstOption);
        }
        #endregion
        #region CRUD

        /// <summary>
        /// Accion que llama  a metodo para insertar una opción
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddOption(Entities.Options option)
        {


            if (ModelState.IsValid)
            {
                try
                {


                    if (string.IsNullOrEmpty(option.OptionName))
                    {
                        return Content("El campo Opcion es requerido. Por favor ingrese el dato");
                    }

                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }

                    option.OptionName = option.OptionName.Trim();
                    option.UserCreate = eEmployee.Idhrms;


                    //Llamar al metodo InsertOption 
                    string result = Data.Option.InsertOption(option);
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
        /// Accion que llama  a metodo para editar un usuario
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditOption(Entities.Options option)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(option.OptionName))
                    {
                        return Content("El campo Opcion es requerido. Por favor ingrese el dato");
                    }

                    option.OptionName = option.OptionName.ToUpper().Trim();

                    //Llamar al metodo UpdateOption
                    string result = Data.Option.UpdateOption(option);
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
        public ActionResult DeleteOption(int optionId)
        {


            if (ModelState.IsValid)
            {
                try
                {
                    //Validar si el evento ya posee inscripcion

                    //Llamar al metodo DeleteOption
                    string result = Data.Option.DeleteOption(optionId);
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
    }
}