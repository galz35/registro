using System;
using System.Collections.Generic;
using System.Linq;

using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class SystemUsersController : Controller
    {
        #region CRUD

        /// <summary>
        /// Accion que llama  a metodo para insertar un usuario
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddSystemUser(Entities.SystemUsers systemUser)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(systemUser.UserName))
                    {
                        return Content("El campo Usuario es requerido. Por favor ingrese el dato");
                    }

                    if (string.IsNullOrEmpty(systemUser.FirstName))
                    {
                        return Content("El campo Primer nombre es requerido. Por favor ingrese el dato");
                    }
                    if (string.IsNullOrEmpty(systemUser.LastName))
                    {
                        return Content("El campo Apellidos es requerido. Por favor ingrese el dato");
                    }

                    //Setear atributos a mayuscula
                    systemUser.FirstName = systemUser.FirstName.ToUpper().Trim();
                    systemUser.LastName = systemUser.LastName.ToUpper().Trim();
                    //Llamar al metodoInsertSystemUser
                    string result = Data.SystemUser.InsertSystemUser(systemUser);
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
        public ActionResult EditSystemUser(Entities.SystemUsers systemUser)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(systemUser.UserName))
                    {
                        return Content("El campo Usuario es requerido, por favor ingrese el dato");
                    }
                    if (string.IsNullOrEmpty(systemUser.FirstName))
                    {
                        return Content("El campo Primer nombre es requerido, por favor ingrese el dato");
                    }
                    if (string.IsNullOrEmpty(systemUser.LastName))
                    {
                        return Content("El campo Apellidos es requerido, por favor ingrese el dato");
                    }

                    //Setear atributos a mayuscula
                    systemUser.FirstName = systemUser.FirstName.ToUpper().Trim();
                    systemUser.LastName = systemUser.LastName.ToUpper().Trim();
                    //Llamar al metodoUpdateSystemUser
                    string result = Data.SystemUser.UpdateSystemUser(systemUser);
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
                return Content("Los datos son incorrectos, por favor verificar la informacion.");
            }

            return ListPartial();
        }


        ///// <summary>
        ///// Metodo para elimianar una usuario
        ///// </summary>
        ///// <param name="expenseId"></param>
        ///// <returns></returns>
        public ActionResult DeleteSystemUser(int userId)
        {
            try
            {
                string result = Data.SystemUser.DeleteSystemUser(userId);
                if (result == "Error al eliminar el registro")
                {
                    return Content(result);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return ListPartial();
        }
        #endregion

        #region Lista de Usuarios
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
            List<Entities.SystemUsers> lstSystemUser = new List<Entities.SystemUsers>();
            try
            {
                lstSystemUser = Data.SystemUser.GetAllSystemUsers();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("ListPartial", lstSystemUser);
        }
        #endregion
    }
}