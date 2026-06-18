using DevExpress.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class EmployeeGoalsController : Controller
    {
        const string keyPeriodId = "sPeriodId";
        const string keyAreaId = "sAreaId";
        const string keyIndicatorId = "sIndicatorId";
        const string keyProductId = "sProductId";
        const string keyManagementId = "sManagementId";

        //By changing the return type from ActionResult to void - this method does not return anything
        public void ChangePeriod(string periodId)
        {
            Session[keyPeriodId] = periodId;
        }
        
        /// <summary>
        /// Metodo que actualiza las metas en la base de datos
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="periodId"></param>
        [HttpPost]
        [ValidateInput(false)]
        public void UpdateDatabase(int areaId, string periodId)
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            Data.EmployeeGoal.UpdateGoal(areaId, eEmployee.Idhrms, periodId);
            Session.Remove("sGoalsEdit");
            //return RedirectToAction PivotPartial(7213, "2019-03");
        }



        /// <summary>
        /// Accion para abrir vista primaria Commission
        /// </summary>
        /// <returns></returns>
        public ActionResult Commission()

        {
            return View();
        }
        public ActionResult CommissionRosterPartial(int? managementId)
        {
            Session.Remove("sCommissionEmployee");

            if (managementId == null)
            {
                managementId = (int)Session[keyManagementId];
            }
            else
            {
                Session[keyManagementId] = managementId;
            }
            List<Entities.ViewModels.CommissionRosterView> lstEmployee = new List<Entities.ViewModels.CommissionRosterView>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >= 1 && eEmployee.userlevel != 6)
                    {

                        var subManagementId = Session["subManagementId"]; //Utils.ClaroWCF.ExtraTimeGetOrganizationId(eEmployee.Idhrms);
                        string management = (string)Session["managementId"]; //Utils.ClaroWCF.GetManagementId(int.Parse(subManagementId));
                        lstEmployee = Data.EmployeeGoal.GetOrganizationsByManagement(int.Parse(management));

                    }
                    else
                    {
                        lstEmployee = Data.EmployeeGoal.GetOrganizationsByManagement(managementId.GetValueOrDefault());
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("CommissionRosterPartial", lstEmployee);
        }
        /// <summary>
        /// Accion para abrir vista primaria Edit
        /// </summary>
        /// <returns></returns>
        public ActionResult Edit()
        {
            Session.Remove("sGoalsEdit");
            
            return View();
        }
        
        /// <summary>
        /// Accion para abrir vista parcial PivotPartial
        /// </summary>
        /// <returns></returns>
        public ActionResult PivotPartial(int? areaId,string periodId)
        {
            Session[keyAreaId] = areaId;
            Session[keyPeriodId] = periodId;

            List<Entities.ViewModels.CommissionRosterView> lstEmployee = new List<Entities.ViewModels.CommissionRosterView>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >= 1)
                    {

                        lstEmployee = Data.EmployeeGoal.GetEmployeesGoalsByArea(areaId.GetValueOrDefault(), eEmployee.Idhrms,periodId);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView(lstEmployee);
        }

        /// <summary>
        /// Accion para abrir vista parcial EditPartial para editar las metas por empleado segun el indicador
        /// </summary>
        /// <returns></returns>
        public ActionResult EditPartial(string indicatorId, string productId)
        {

           
            List<Entities.ViewModels.CommissionRosterView> lstEmployee = new List<Entities.ViewModels.CommissionRosterView>();
         

            string positionType = Data.Indicator.GetPositionTypeByIndicator(indicatorId);

            int areaId = (int)Session[keyAreaId];
            string periodId = (string)Session[keyPeriodId];

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

          

         
            if (indicatorId == null)
            {
                indicatorId = (string)Session[keyIndicatorId];
            }
            else
            {
                Session[keyIndicatorId] = indicatorId;
            }

            if (productId == null)
            {
                productId = (string)Session[keyProductId];
            }
            else
            {
                Session[keyProductId] = productId;
            }
           
           
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >= 1)
                    {

                        // lstEmployee = Data.EmployeeGoal.GetEmployeesGoalsByIndicator(areaId, eEmployee.Idhrms, periodId,indicatorId, productId).Where(x=> x.PositionTypeId==positionType).ToList();
                        lstEmployee = Data.EmployeeGoal.GetAllEditableEmployeeGoals(areaId, eEmployee.Idhrms, periodId).Where(x=> x.IndicatorId== indicatorId && x.ProductId==productId && x.PositionTypeId == positionType).ToList();
                        //return Json(lstEmployee, JsonRequestBehavior.AllowGet);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

           
            return PartialView("EditPartial", lstEmployee);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditGoal(MVCxGridViewBatchUpdateValues<Entities.ViewModels.CommissionRosterView> updateValues)
        {
          

            foreach (var item in updateValues.Update)
            {
                if (updateValues.IsValid(item))
                {
                    try
                    {
                        Entities.Employees eEmployee = null;
                        //Entities.ViewModels.CommissionRosterView commission = new Entities.ViewModels.CommissionRosterView();
                        item.PeriodId = (string)Session[keyPeriodId];
                        item.ProductId = (string)Session[keyProductId];
                        item.IndicatorId = (string)Session[keyIndicatorId];
                        item.OrganizationId = (int)Session[keyAreaId];
                        //commission.PeriodId = (string)Session[keyPeriodId];
                        //commission.ProductId = (string)Session[keyProductId];
                        //commission.IndicatorId = (string)Session[keyIndicatorId];
                        //commission.OrganizationId = (int)Session[keyAreaId];
                        
                        if (Session["User"] != null)
                        {
                            eEmployee = (Entities.Employees)Session["User"];
                        }
                        //commission.BossId = eEmployee.Idhrms;
                        item.BossId = eEmployee.Idhrms;

                        //Llamar al metodo EditGoal
                        SafeExecute(() => Data.EmployeeGoal.EditGoal(item));

                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                }

            }
           
            //if (ModelState.IsValid)
            //{
            //    try
            //    {

            //        commissionRosterView.PeriodId = (string)Session[keyPeriodId];
            //        commissionRosterView.ProductId= (string)Session[keyProductId];
            //        commissionRosterView.IndicatorId = (string)Session[keyIndicatorId];
            //        commissionRosterView.OrganizationId = (int)Session[keyAreaId];
            //        Entities.Employees eEmployee = null;
            //        if (Session["User"] != null)
            //        {
            //            eEmployee = (Entities.Employees)Session["User"];
            //        }
            //        commissionRosterView.BossId = eEmployee.Idhrms;


            //        //Llamar al metodo EditGoal
            //        SafeExecute(() => Data.EmployeeGoal.EditGoal(commissionRosterView));

            //    }
            //    catch (Exception e)
            //    {
            //        ViewData["EditError"] = e.Message;
            //    }
            //}
            //else
            //{

            // return Content("Ocurrió un error al actualizar la información, por favor verifique los datos y vuelva a intentarlo.");
            //}

            return EditPartial((string)Session[keyIndicatorId], (string)Session[keyProductId]);
        }

        public void SafeExecute(Action method)
        {
            try
            {
                method();
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }
        }


    }
}