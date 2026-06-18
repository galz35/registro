using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DevExpress.Web.Mvc;
using slnRhonline.Reports;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class EventRegistrationDetailController : Controller
    {
        #region Lista de Usuarios
        [Authorize]

        public ActionResult EventRegistrationDetail(int eventId)
        {
            List<Entities.EventRegistrationDetail> lstEventRegistrationDetail = new List<Entities.EventRegistrationDetail>();
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var eventRegistration = Data.EventRegistration.GetAllEventRegistrationByUser(eEmployee.Idhrms).Where(x => x.EventId == eventId).FirstOrDefault();

                lstEventRegistrationDetail = Data.EventRegistrationDetail.GetAllEventRegistrationDetail(eventRegistration.EventRegistrationId);

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstEventRegistrationDetail);
        }



        public ActionResult ListPartial(List<Entities.EventRegistrationDetail> lstDetail)
        {
            
            try
            {
                
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("EventRegistrationDetailPartial", lstDetail);
        }
        #endregion
        #region CRUD

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult InsertEventRegistrationDetail(MVCxGridViewBatchUpdateValues<Entities.EventRegistrationDetail> updateValues)
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Validations.EventRegistrationDetail vEventRegistrationDetail = new Validations.EventRegistrationDetail();
            Validations.EventRegistration vEventRegistration = new Validations.EventRegistration();
            List<Entities.EventRegistrationDetail> lstEventRegistrationDetail = new List<Entities.EventRegistrationDetail>();
            int eventRegistrationId = (int)Session["sEventRegistrationId"];
            foreach (var item in updateValues.Insert)
            {
                if (updateValues.IsValid(item))
                {
                    try
                    {
                        //validar total por colaborador y evento
                        bool validateQuotas = vEventRegistrationDetail.ValidateTotalAvailableQuotas(eventRegistrationId);
                        if (validateQuotas == false)
                        {
                           
                            return Content("No hay cupos disponibles para mas acompañantes");
                        }

                        //validar evento cerrado
                        Entities.ViewModels.EventRegistrationView eventView = Data.EventRegistration.GetAllEventRegistrationByUser(eEmployee.Idhrms).SingleOrDefault(x => x.EventRegistrationId == eventRegistrationId);
                        bool eventClosed = vEventRegistration.ValidateEventClosed(eventView.EventId.GetValueOrDefault());
                        if (eventClosed == false)
                        {

                            return Content("El evento ya esta cerrado");
                        }

                        string result = Data.EventRegistrationDetail.AddEventRegistrationDetail(item);
                        if (result != "Exito al insertar el registro")
                        {
                            return Content("Error en la insercion");
                        }
                        ////Llamar al metodo EditGoal
                        //SafeExecute(() => Data.EventRegistrationDetail.AddEventRegistrationDetail(item));


                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                }


            }
            foreach (var eventRegistrationDetailId in updateValues.DeleteKeys)
            {
               
                    try
                    {
                        string result = Data.EventRegistrationDetail.DeleteEventRegistrationDetail(int.Parse(eventRegistrationDetailId));
                        if (result != "Exito al eliminar el registro")
                        {
                        
                        return Content("Error en la eliminacion");
                    }
                       


                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                


            }
            lstEventRegistrationDetail = Data.EventRegistrationDetail.GetAllEventRegistrationDetail(eventRegistrationId);
            return ListPartial(lstEventRegistrationDetail);
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

  

        #endregion
    }
}