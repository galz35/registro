using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DevExpress.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class ExpensePeriodsController : Controller
    {

        public ActionResult GetAllPeriodsByClass(
                                                              int? classId,
                                                              string textField,
                                                              string valueField)
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            //var subManagementId = Utils.ClaroWCF.ExtraTimeGetOrganizationId(eEmployee.Id_HRMS);
            //string managementId = Utils.ClaroWCF.GetManagementId(int.Parse(subManagementId));
            // string managementId = "7758";
            //string managementId = "7756";
            //string managementId = "7755";Session["V17"]
            if (classId.GetValueOrDefault().ToString()=="17")
            {
                List<Entities.ExpensesPeriods> temp= (List<Entities.ExpensesPeriods>)Session["V17"];
                if (temp.Count()>0)
                {
                    temp= temp.Where(x => x.ClassId == 17).ToList();
                }
                return GridViewExtension.GetComboBoxCallbackResult(p =>
                {
                    p.TextField = textField;
                    p.ValueField = valueField;
                    p.BindList(temp);
                });
            }
            else
            if (classId.GetValueOrDefault().ToString() == "16")
            {
                List<Entities.ExpensesPeriods> temp = (List<Entities.ExpensesPeriods>)Session["V17"];
                if (temp.Count() > 0)
                {
                    temp = temp.Where(x => x.ClassId == 16).ToList();
                }
                return GridViewExtension.GetComboBoxCallbackResult(p =>
                {
                    p.TextField = textField;
                    p.ValueField = valueField;
                    p.BindList(temp);
                });
            } else
            if (classId.GetValueOrDefault().ToString() == "94")
            {
                List<Entities.ExpensesPeriods> temp = (List<Entities.ExpensesPeriods>)Session["V17"];
                if (temp.Count() > 0)
                {
                    temp = temp.Where(x => x.ClassId == 94).ToList();
                }
                return GridViewExtension.GetComboBoxCallbackResult(p =>
                {
                    p.TextField = textField;
                    p.ValueField = valueField;
                    p.BindList(temp);
                });
            }
            else
            return GridViewExtension.GetComboBoxCallbackResult(p =>
            {
                p.TextField = textField;
                p.ValueField = valueField;
                p.BindList(Data.ExpensePeriod.GetAllPeriodsByClass(classId.GetValueOrDefault()
                    , "0"));
            });
        }
        public ActionResult GetAllPeriodsByManagementAndClass(int? managementId,
                                                              int? classId,
                                                              string textField,
                                                              string valueField)
        {
            return GridViewExtension.GetComboBoxCallbackResult(p =>
            {
                p.TextField = textField;
                p.ValueField = valueField;
                p.BindList(Data.ExpensePeriod
                    .GetAllPeriodsByManagementAndClass(managementId.GetValueOrDefault(), classId.GetValueOrDefault()));
                    //.GetAllPeriodsByManagementAndClass(6656, classId.GetValueOrDefault()));
                
            });
        }
    }
}