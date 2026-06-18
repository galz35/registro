using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.SessionState;

namespace slnRhonline.Models
{
    public static class ExpensePeriods
    {
        static HttpSessionState Session { get { return HttpContext.Current.Session; } }

        public static List<Entities.ExpensesPeriods> GetAllPeriods()
        {
            List<Entities.ExpensesPeriods> lstPeriods = new List<Entities.ExpensesPeriods>();
            Entities.Employees eEmployee = null;

            if(Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                DateTime dTotay = DateTime.Parse(DateTime.Now.ToShortDateString());
                var result = Utils.ClaroWCF.GetAllExpensesPeriods();
                if(result != null)
                {
                    if(eEmployee.userlevel != 6)
                    {
                       
                        var subManagementId = Session["subManagementId"]; //Utils.ClaroWCF.ExtraTimeGetOrganizationId(eEmployee.Id_HRMS);
                        string managementId = (string)Session["managementId"];//  Utils.ClaroWCF.GetManagementId(int.Parse(subManagementId));

                        lstPeriods = result.Where(x => (x.PaidDate > dTotay) &&
                            (x.ManagementId == int.Parse(managementId)))
                            .ToList();
                    } else
                    {
                        lstPeriods = result.Where(x => x.PaidDate > dTotay)
                            .ToList();
                    }
                }
            } catch(Exception)
            {
                throw;
            }

            return lstPeriods;
        }
    }
}