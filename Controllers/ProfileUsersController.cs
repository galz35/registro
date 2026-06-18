using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class ProfileUsersController : Controller
    {
        const string keyUserId = "sUserId";

        #region CRUD

        /// <summary>
        /// Accion que llama  a metodo para insertar un usuario
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddProfileUser(Entities.ViewModels.ProfileUsersView profileView)
        {
            int userId = (int)Session[keyUserId];
            if (ModelState.IsValid)
            {
                try
                {
                    if (profileView.ProfileId == 0)
                    {
                        return Content("El campo Perfil es requerido. Por favor ingrese el dato");
                    }



                    Entities.ProfileUsers profileUser = new Entities.ProfileUsers();

                    profileUser.UserId = userId;
                    profileUser.ProfileId = profileView.ProfileId;

                    //Llamar al metodo InsertProfile
                    string result = Data.ProfileUser.InsertProfileUser(profileUser);
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

            return ListPartial(userId);
        }

        /////// <summary>
        /////// Metodo para eliminar un perfil de usuario
        /////// </summary>
        /////// <param name="expenseId"></param>
        /////// <returns></returns>
        public ActionResult DeleteProfileUser(int profileUserId)
        {
            int userId = (int)Session[keyUserId];
            try
            {
                string result = Data.ProfileUser.DeleteProfileUser(profileUserId);
                if (result != "Exito al eliminar el registro")
                {
                    return Content(result);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return ListPartial(userId);
        }

        /////// <summary>
        /////// Metodo para eliminar un perfil de usuario
        /////// </summary>
        /////// <param name="expenseId"></param>
        /////// <returns></returns>
        public ActionResult ActivateProfileUser(int id)
        {
            int userId = (int)Session[keyUserId];
            try
            {
                string result = Data.ProfileUser.ActivateProfileUser(id);
                if (result != "Exito al activar el registro")
                {
                    return Content(result);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return RedirectToAction("ListPartial", new { userId }); //ListPartial(userId);
        }



        #endregion#region Lista de Perfiles





        #region Lista de perfiles
        [Authorize]

        public ActionResult List(int id)
        {
            try
            {
                Entities.SystemUsers systemUser = new Entities.SystemUsers();
                systemUser = Data.SystemUser.GetAllSystemUsers().FirstOrDefault(x => x.UserId == id);
                return View("List", systemUser);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }


        public ActionResult ListPartial(int userId)
        {
            Session[keyUserId] = userId;
            List<Entities.ViewModels.ProfileUsersView> lstProfileUser = new List<Entities.ViewModels.ProfileUsersView>();
            try
            {
                lstProfileUser = Data.ProfileUser.GetAllProfilesUser().Where(x => x.UserId == userId).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("ListPartial", lstProfileUser);
        }


        #endregion

    }
}