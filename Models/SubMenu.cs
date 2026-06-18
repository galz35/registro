using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;

using System.Web.Script.Serialization;

namespace slnRhonline.Models
{
    public static class SubMenu
    {
        static HttpSessionState Session { get { return HttpContext.Current.Session; } }

        public static List<Entities.ViewModels.UserOptionsView> GetAllSubMenu()
        {
            //Recuperacion de parametros.
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            List<Entities.ViewModels.UserOptionsView> lstSubMenu = new List<Entities.ViewModels.UserOptionsView>();

            var resultSubMenu = Utils.ClaroWCF.GetAllSubMenu(eEmployee.EmailAddress);
           

            if (resultSubMenu != null)
            {
                var deserializedObject = new JavaScriptSerializer().Deserialize<List<Entities.ViewModels.UserOptionsView>>(resultSubMenu);
                lstSubMenu = deserializedObject;
            }

            return lstSubMenu;
        }
    }
}