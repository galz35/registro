using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;


namespace slnRhonline.Models
{
    public static class Company
    {


        static HttpSessionState Session { get { return HttpContext.Current.Session; } }


        public static List<Entities.Companies> GetCompanyByUser()
        {
            List<Entities.Companies> lstCompany = new List<Entities.Companies>();

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            var result = Utils.ClaroWCF.GetCompanyByUser(eEmployee.Idhrms);


            if (result != null)
            {

                lstCompany = result.ToList();
            }
            else
            {
                lstCompany = new List<Entities.Companies>();
            }

            return lstCompany;
        }


    }
}