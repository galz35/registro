using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;

namespace slnRhonline.Models
{
    public static class Menu
    {
        static HttpSessionState Session { get { return HttpContext.Current.Session; } }
        public static List<Entities.Menu> GetAllMenu()

        {
            //Recuperacion de parametros.

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            List<Entities.Menu> lstMenu = new List<Entities.Menu>();

            var resultMenu = Utils.ClaroWCF.GetAllMenu(eEmployee.EmailAddress);

            if (resultMenu != null)
            {
                lstMenu = resultMenu.ToList();
            }

            return lstMenu;
        }
    }
}