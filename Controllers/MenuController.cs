using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class MenuController : Controller
    {
        #region Lista de Opciones de menu
        [Authorize]


        /// <summary>
        /// Accion oficial que manda a llamar a 
        /// </summary>
        /// <returns></returns>
        public ActionResult GenerateMenu()
        {

            try
            {
                Data.Menu menu = new Data.Menu();
                var oMenuRhOnline = (List<MenuRhOnline>)Session["Menulista"];
                if (oMenuRhOnline == null)
                {
                    var menuList = menu.GetAllMenu();
                    var menuForDisplay = menu.GetAllMenuById(menuList, null);
                    Session["Menulista"] = menuForDisplay;
                    return PartialView("MenuPartial", menuForDisplay);
                }
                else {
                    var menuForDisplay= (List<MenuRhOnline>)Session["Menulista"];
                    return PartialView("MenuPartial", menuForDisplay);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


        }

       


        #endregion

    }
}