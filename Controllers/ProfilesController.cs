using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class ProfilesController : Controller
    {
        #region Lista de perfiles
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
            List<Entities.Profiles> lstProfile = new List<Entities.Profiles>();
            try
            {
                lstProfile = Data.Profile.GetAllProfiles();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("ListPartial", lstProfile);
        }

        #endregion

        #region CRUD

        /// <summary>
        /// Accion que llama  a metodo para insertar un usuario
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddProfile(Entities.Profiles profile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(profile.ProfileName))
                    {
                        return Content("El campo Perfil es requerido. Por favor ingrese el dato");
                    }



                    // Seteando el atributo a maysucula
                    profile.ProfileName = profile.ProfileName.ToUpper().Trim();
                    //Llamar al metodo InsertProfile
                    string result = Data.Profile.InsertProfile(profile);
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
        /// Accion que llama  a metodo para editar un perfil
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditProfile(Entities.Profiles profile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(profile.ProfileName))
                    {
                        return Content("El campo Perfil es requerido, por favor ingrese el dato");
                    }

                    // Seteando el atributo a maysucula
                    profile.ProfileName = profile.ProfileName.ToUpper().Trim();
                    //Llamar al metodo UpdateProfile
                    string result = Data.Profile.UpdateProfile(profile);

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
        ///// Metodo para eliminar un perfil
        ///// </summary>
        ///// <param name="expenseId"></param>
        ///// <returns></returns>
        public ActionResult DeleteProfile(int profileId)
        {
            try
            {
                string result = Data.Profile.DeleteProfile(profileId);
                if (result != "Exito al eliminar el registro")
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
        #endregion#region Lista de Perfiles

    }
}