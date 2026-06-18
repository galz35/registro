using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ClosedXML.Excel;
using DevExpress.Web.Mvc;
using slnRhonline.Reports;
 
namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class ExpenseDetailController : Controller
    {
        #region Reportes
        public ActionResult RrhhReport2()
        { return View(); }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult FinancialReports(Entities.ViewModels.FinanceParametersView eParameter)
        {
            Session["sParameterReport"] = eParameter;

            if (ModelState.IsValid)
            {
                try

                {
                    if (eParameter.ReportType == "DETALLE")
                    {
                        return View("FinancialDetailReport");
                    }
                    if (eParameter.ReportType == "CUENTA")
                    {
                        return View("cuentaclaro1");
                    }
                    if (eParameter.ReportType == "CONSOLIDADO")
                    {
                        return View("FinancialSummaryReport");
                    }

                    if (eParameter.ReportType == "FORMATO RRHH")
                    {

                        if (eParameter.PaidDate.HasValue)
                        {
                            return View("RrhhReport");


                        }
                        ViewData["EditError"] = "La fecha es requerida para el reporte de FORMATO RRHH";
                        return View("FinancialParameters");
                    }
                    if (eParameter.ReportType == "FORMATO TECNICA")
                    {

                        if (eParameter.PaidDate.HasValue )
                        {
                            if (eParameter.ManagementId == 6656 || eParameter.ManagementId == 6671 || eParameter.ManagementId == 6731 || eParameter.ManagementId == 6817)
                            {
                                return View("TechnicalReport");
                            }
                            else
                            {
                                ViewData["EditError"] = "No tiene permiso para ver el detalle de esta gerencia";
                                return RedirectToAction("FinancialParameters", "Expenses");
                            }
                           

                        }
                        ViewData["EditError"] = "La fecha es requerida para el reporte de FORMATO TECNICA";
                        return RedirectToAction("FinancialParameters", "Expenses");
                    }
                    return View();
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Error en la operacion";
            }
            return RedirectToAction("FinancialParameters", "Expenses");//return View("FinancialParameters");
        }

        /// <summary>
        /// Accion que manda a retornar una vista en dependencia del tipo de reporte seleccionado, en caso de que el modelo
        /// no sea valido retorna la vista ParametersReport.
        /// </summary>
        /// <param name="eParameter"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ExpensesDetailReport(Entities.ViewModels.ExpenseParametersView eParameter)
        {
            Entities.ViewModels.FinanceParametersView eParameterx = new Entities.ViewModels.FinanceParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
            Session["sParameterReport"] = eParameter;

            if (ModelState.IsValid)
            {
                try
                {
                    
                    
                    if (eParameter.ReportType == "DETALLE")
                    {
                        if (String.IsNullOrEmpty(eParameter.Banco) == false && eParameter.Banco!="0")
                        {
                            return View("DetailReportv");
                        }
                        else
                        {
                            return View("DetailReport");//afectar
                        }
                    }
                    if (eParameter.ReportType == "CUENTA")
                    {
                        Entities.Employees eEmployee = null;

                        if (Session["User"] != null)
                        {
                            eEmployee = (Entities.Employees)Session["User"];
                        }
                        //eParameterx = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];

                        var result = Data.ExpenseDetail.GetAllFinanceExpensesByManagementClassPeriodCompanyx(int.Parse( eEmployee.GERENCIAID))
                       
                                                                                                               
                            .ToList();
                       
                        DataTable dt = null;
                        if (result != null)
                        {
                         


                            ListtoDataTableConverter converter = new ListtoDataTableConverter();
                            dt = converter.ToDataTable(result);



                            using (XLWorkbook wb = new XLWorkbook())
                            {
                                wb.Worksheets.Add(dt);
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    wb.SaveAs(stream);
                                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cuenta" + DateTime.Now.ToString() + ".xlsx");
                                }
                            }



                        }
                        dt = new DataTable();
                        dt.TableName = "Datos";
                        using (XLWorkbook wb = new XLWorkbook())
                        {
                            wb.Worksheets.Add(dt);
                            using (MemoryStream stream = new MemoryStream())
                            {
                                wb.SaveAs(stream);
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cuenta" + DateTime.Now.ToString() + ".xlsx");
                            }
                        }
                        //return View("cuentaclaro1");//afectar
                       
                    }
                    if (eParameter.ReportType == "CONSOLIDADO")

                    {
                        if (String.IsNullOrEmpty(eParameter.Banco)==false && eParameter.Banco != "0")
                        {
                            return View("SummaryReportv");
                        }
                        else { return View("SummaryReport"); }
                    }
                    if (eParameter.ReportType == "PENDIENTES DE RENDICION")
                    {
                        return View("PendingsYieldsReport");
                    }
                    if (eParameter.ReportType == "MATRIZ RH")
                    {
                        return View("CrossTab");
                    }
                    if (eParameter.ReportType == "DESEMBOLSO")
                    {
                        if (String.IsNullOrEmpty(eParameter.Banco) == false && eParameter.Banco != "0")
                        {
                            return View("DisbursementReportv");
                        }
                        else
                        {
                            return View("DisbursementReport");// afectar
                        }
                    }
                    if (eParameter.ReportType == "RENDICIÓN")
                    {
                        return View("YieldSummaryReport");
                    }
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Error en la operacion";
            }
            return RedirectToAction("ParametersReport", "Expenses");// 
        }

        /// <summary>
        /// Accion que llama a metodo GetAllExpenseDetailByClassPeriodCompanyBoss de la capade datos.
        /// ExpensesDetailReportPartial.
        /// </summary>
        /// <returns></returns>
         public ActionResult DetailReportPartial1()
        {
            List<Entities.ViewModels.ExpenseDetailReport> lstDetail = new List<Entities.ViewModels.ExpenseDetailReport>();

        Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
        eParameter = (Entities.ViewModels.ExpenseParametersView) Session["sParameterReport"];

        Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees) Session["User"];
    }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {

                        var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBoss
                            (eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

                        if (detail != null)
                        {
                            lstDetail = detail.ToList();

                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("DetailReportPartial", lstDetail);
        }
        public ActionResult DetailReportPartial()
        {
            
            return PartialView("DetailReportPartial");
        }
        public ActionResult DetailReportPartialv()
        {
         

            return PartialView("DetailReportPartialv");
        }
        /// <summary>
        /// Accion que retorna la exportacion del detalle de viaticos.
        /// </summary>
        /// <returns></returns>
        /// 
        public ActionResult DetailReportPartialv2()
        {
            List<Entities.ViewModels.ExpenseDetailReport> lstDetail = new List<Entities.ViewModels.ExpenseDetailReport>();

            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {

                        var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBossv(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

                        if (detail != null)
                        {
                            lstDetail = detail.ToList();
                            // Session["lstDetail"] = lstDetail;

                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("DetailReportPartialv", lstDetail);
        }
        public ActionResult DetailReportPartialvv()
        {
            List<Entities.ViewModels.ExpenseDetailReport> lstDetail = new List<Entities.ViewModels.ExpenseDetailReport>();

            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {

                        var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBossv(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

                        if (detail != null)
                        {
                            lstDetail = detail.ToList();
                            // Session["lstDetail"] = lstDetail;

                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("DetailReportPartialv", lstDetail);
        }
        public ActionResult ExportDetailReport()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            ExpenseDetailReport detailReport = new ExpenseDetailReport();

            var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBoss(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

            if (detail != null)
            {
                detailReport.DataSource = detail.ToList();

            }


            return ReportViewerExtension.ExportTo(detailReport);
        }
        public ActionResult ExportDetailReportv()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            ExpenseDetailReport detailReport = new ExpenseDetailReport();

            var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBossv(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

            if (detail != null)
            {
                detailReport.DataSource = detail.ToList();

            }


            return ReportViewerExtension.ExportTo(detailReport);
        }
        /// <summary>
        /// Accion que llama al metodo GetAllExpenseDetailByClassPeriodCompanyBoss, el cual carga el consolidado de viaticos.
        /// </summary>
        /// <returns></returns>
        /// 
        public ActionResult SummaryReportPartial()
        { 
            

            return PartialView("SummaryReportPartial");
        }
        public ActionResult SummaryReportPartialv()
        {
            

            return PartialView("SummaryReportPartialv");
        }
        public ActionResult SummaryReportPartial2()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> lstSummary = new List<Entities.ViewModels.ExpenseDetailReport>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {
                        eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];


                        var summary = Data.ExpenseDetail.GetAllExpenseSummaryByClassPeriodCompanyBoss(eParameter.ClassId,
                                                                                                        eParameter.PeriodId,
                                                                                                        eParameter.Company,
                                                                                                        eEmployee.Idhrms);

                        if (summary != null)
                        {
                            lstSummary = summary.ToList();

                        }
                        else
                        {
                            lstSummary = new List<Entities.ViewModels.ExpenseDetailReport>();
                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("SummaryReportPartial", lstSummary);
        }
        
        /// <summary>
        public ActionResult SummaryReportPartialv2()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> lstSummary = new List<Entities.ViewModels.ExpenseDetailReport>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
               //     if (eEmployee.UserLevel >=0)
                 //   {
                        eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];
                     

                       var summary = Data.ExpenseDetail.GetAllExpenseSummaryByClassPeriodCompanyBossv(eParameter.ClassId,
                                                                                                        eParameter.PeriodId,
                                                                                                        eParameter.Company,
                                                                                                        eEmployee.Idhrms);


                    if (summary != null)
                        {
                            lstSummary = summary.ToList();

                        }
                        else
                        {
                            lstSummary = new List<Entities.ViewModels.ExpenseDetailReport>();
                        }
                    //}
                    //else
                    //{
                    //    ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    //}
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("SummaryReportPartialv", lstSummary);
        }
        /// <summary>
        /// Accion que llama al metodo GetAllExpenseDetailByClassPeriodCompanyBoss, para exportar consolidado de viaticos.
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportSummaryReport()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }


            ExpensesReimbursement detailReport = new ExpensesReimbursement();
            var summary = Data.ExpenseDetail.GetAllExpenseSummaryByClassPeriodCompanyBoss(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

            if (summary != null)
            {
                detailReport.DataSource = summary.ToList();
            }

            return ReportViewerExtension.ExportTo(detailReport);
        }
        public ActionResult ExportSummaryReportv()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }


            ExpensesReimbursement detailReport = new ExpensesReimbursement();
            var summary = Data.ExpenseDetail.GetAllExpenseSummaryByClassPeriodCompanyBossv(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

            if (summary != null)
            {
                detailReport.DataSource = summary.ToList();
            }

            return ReportViewerExtension.ExportTo(detailReport);
        }


        /// <summary>
        /// Médto que muestra la vista parcial del reporte de desembolso de viaticos planificados y de reembolso
        /// </summary>
        /// <returns></returns>
        public ActionResult DisbursementReportPartial()
        {
            List<Entities.ViewModels.ExpenseDetailReport> lstDisbursement = new List<Entities.ViewModels.ExpenseDetailReport>();

            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0 )
                    {


                        var disbursement = Data.ExpenseDetail.GetAllExpenseDisbursementByClassPeriodCompanyBoss(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);
                        if (disbursement != null)
                        {
                            lstDisbursement = disbursement.ToList();
                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("DisbursementReportPartial", lstDisbursement);
        }
        public ActionResult DisbursementReportPartialv()
        {
             

            return PartialView("DisbursementReportPartialv");
        }
        /// <summary>
        /// Método para exportar el  desembolso de viaticos planificados y reembolso
        /// </summary>
        /// <returns></returns>
          public ActionResult DisbursementReportPartialv2()
        {
            List<Entities.ViewModels.ExpenseDetailReport> lstDisbursement = new List<Entities.ViewModels.ExpenseDetailReport>();

        Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
        eParameter = (Entities.ViewModels.ExpenseParametersView) Session["sParameterReport"];

        Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees) Session["User"];
    }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {


                        var disbursement = Data.ExpenseDetail.GetAllExpenseDisbursementByClassPeriodCompanyBossv(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);
                        if (disbursement != null)
                        {
                            lstDisbursement = disbursement.ToList();
                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("DisbursementReportPartialv", lstDisbursement);
        }
        public ActionResult ExportDisbursementReport()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Disbursement detailReport = new Disbursement();//Objeto de instancia del reporte

            var disbursement = Data.ExpenseDetail.GetAllExpenseDisbursementByClassPeriodCompanyBoss(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

            if (disbursement != null)
            {
                detailReport.DataSource = disbursement.ToList();
            }


            return ReportViewerExtension.ExportTo(detailReport);
        }

        public ActionResult ExportDisbursementReportv()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Disbursement detailReport = new Disbursement();//Objeto de instancia del reporte

            var disbursement = Data.ExpenseDetail.GetAllExpenseDisbursementByClassPeriodCompanyBossv(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

            if (disbursement != null)
            {
                detailReport.DataSource = disbursement.ToList();
            }


            return ReportViewerExtension.ExportTo(detailReport);
        }

        /// <summary>
        /// Médto que muestra el reporte de viaticos pendientes de rendición
        /// </summary>
        /// <returns></returns>
        public ActionResult PendingsYieldReportPartial()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];
            List<Entities.ViewModels.ExpenseDetailReport> lstDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {


                        var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBoss(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

                        if (detail != null)
                        {
                            lstDetail = detail.ToList();

                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("PendingsYieldReportPartial", lstDetail);
        }

        /// <summary>
        /// Método para exportar el reporte de rendiciones pendientes
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportPendingsYieldReport()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            PendingsExpensesYields detailReport = new PendingsExpensesYields();


            var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBoss(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

            if (detail != null)
            {
                detailReport.DataSource = detail.ToList();

            }


            return ReportViewerExtension.ExportTo(detailReport);
        }

        /// <summary>
        /// Accion que manda a llamar al metodo GetExpenseCrossTab y retorna el resultado en la vista parcial CrossTabPartial.
        /// </summary>
        /// <returns></returns>
        public ActionResult CrossTabPartial()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> model = new List<Entities.ViewModels.ExpenseDetailReport>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel == 6)
                    {
                        model = Data.ExpenseDetail.GetAllExpenseRrhhByPaidDate(eParameter.PaidDate
                            .GetValueOrDefault()
                            .ToShortDateString())
                            .ToList();
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("CrossTabPartial", model);
        }

        /// <summary>
        /// Método para exportar la matriz de viaticos de recursos Humanos
        /// </summary>
        /// <returns></returns>
        public ActionResult CrossTabExport()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];


            CrossTabExpenses crossTabReport = new CrossTabExpenses();


            crossTabReport.DataSource = Data.ExpenseDetail.GetAllExpenseRrhhByPaidDate(eParameter.PaidDate
                .GetValueOrDefault()
                .ToShortDateString())
                .ToList();


            return ReportViewerExtension.ExportTo(crossTabReport);
        }

        /// <summary>
        /// Método para exportacion personalizada a xls del detalle de viaticos
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportDetailAllUsers()

        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];
            List<Entities.ViewModels.ExpenseDetailReport> lstDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {


                        var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBoss(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms);

                        if (detail != null)
                        {
                            lstDetail = detail.ToList();

                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return GridViewExtension.ExportToXls(PersonalizedClasses.ExpenseDetail.ExportDetailSettings, lstDetail);
        }

        public ActionResult ExportDetailAllUsersv()

        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];
            List<Entities.ViewModels.ExpenseDetailReport> lstDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {


                        var detail = Data.ExpenseDetail.GetAllExpenseDetailByClassPeriodCompanyBossv(eParameter.ClassId, eParameter.PeriodId, eParameter.Company, eEmployee.Idhrms );

                        if (detail != null)
                        {
                            lstDetail = detail.ToList();

                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return GridViewExtension.ExportToXls(PersonalizedClasses.ExpenseDetail.ExportDetailSettings, lstDetail);
        }
        //public ActionResult ExportDetailAllUsersv2()

        //{

         
        // Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
        //    eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];
        //    // List<Entities.ViewModels.exportarrhh> lstExpenseDetail = new List<Entities.ViewModels.exportarrhh>();
        //    Entities.Employees eEmployee = null;

        //    if (Session["User"] != null)
        //    {
        //        eEmployee = (Entities.Employees)Session["User"];
        //    }
        //    try
        //    {
                 
        //            var result = Data.ExpenseDetail.GetAllExpenseRrhhByPaidDatejson(eParameter.PaidDate.GetValueOrDefault().ToShortDateString(), eEmployee.correo);
        //            if (result != null)
        //            {
        //                return   result.ToList(); 
        //            }
        //            else
        //            {
        //            return "no";
        //            }
        //     }
        //    catch (Exception e)
        //    {
        //        ViewData["EditError"] = e.Message;
        //    }


        //  //  return GridViewExtension.ExportToXls(PersonalizedClasses.ExpenseDetail.ExportRrhhSettings, lstExpenseDetail);
        //}
        public ActionResult ExportDetailAllUsersv2()
        {
            var eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];
            var eEmployee = (Entities.Employees)Session["User"];

            try
            {
                var result = Data.ExpenseDetail.GetAllExpenseRrhhByPaidDatejson(
                    eParameter.PaidDate.GetValueOrDefault().ToShortDateString(),
                    eEmployee.EmailAddress
                );
                if (result != null && result.Contains("\"EXITO\""))
                    return Content("exito");
                else
                    return Content("no");
            }
            catch (Exception ex)
            {
                return Content("error");
            }
        }

        public List<Entities.ViewModels.exportarrhh> MapExpenseDetailToExport(List<Entities.ViewModels.ExpenseDetailReport> detalles)
        {
            return detalles.Select(d => new Entities.ViewModels.exportarrhh
            {
                ExpenseId = d.ExpenseId,
                tipo = d.ClassName, // ClassName -> tipo
                fecha_pago = d.PaidDate, // PaidDate -> fecha_pago
                periodo = d.PeriodNotes, // PeriodNotes -> periodo
                empresa = d.Company, // Company -> empresa
                ubicacion = d.Location, // Location -> ubicacion
                gerencia = d.ManagementName, // ManagementName -> gerencia
                subgerencia = d.SubManagementName, // SubManagementName -> subgerencia
                areaName = d.AreaName, // AreaName -> areaName
                numeroEmpleado = d.EmployeeNumber, // EmployeeNumber -> numeroEmpleado
                colaborador = d.FullName, // FullName -> colaborador
                fecha_pagado = d.ExpenseDate, // ExpenseDate -> fecha_pagado
                ORG_PAYMENT_METHOD_NAME = d.ORG_PAYMENT_METHOD_NAME, // ORG_PAYMENT_METHOD_NAME
                cuentacontable = d.Accounting, // Accounting -> cuentacontable
                cc = d.CostCenterName, // CostCenterName -> cc
                act = d.EconomicActivity, // EconomicActivity -> act
                negocio = d.BussinessName, // BussinessName -> negocio
                justificacion = d.Justify, // Justify -> justificacion
                orden_servicio = d.ServiceNumber, // ServiceNumber -> orden_servicio
                Clasification = d.Clasification, // Clasification
                Category = d.Category, // Category
                SubCategory = d.SubCategory, // SubCategory
                monto = d.Amount, // Amount -> monto
                monto_rendir = d.YieldAmount, // YieldAmount -> monto_rendir
                monto_retornar = d.ReturnAmount, // ReturnAmount -> monto_retornar
                razon = d.ReasonName, // ReasonName -> razon
                Month = d.Month, // Month
                Banco = d.Banco, // Banco
                Ruta = d.Ruta, // Ruta
                Departamentotraslado = d.Departamentotraslado, // Departamentotraslado
                Departamento = d.Departamento, // Departamento
                cuenta = d.cuenta, // cuenta
                estado = d.StatusName, // StatusName -> estado
                nombre = d.FullName // FullName -> nombre
            }).ToList();
        }

        /// <summary>
        /// Accion que llama a metodo GetAllExpenseRrhhByPaidDate de la capade datos ExpenseDetail.
        /// ExpensesDetailReportPartial.
        /// </summary>
        /// <returns></returns>
        public ActionResult RrhhReportPartial()
        {
          
            return PartialView("RrhhReportPartial");
        }
        [HttpPost]
        public JsonResult GetReportDataJson(string fechafin)
        {
            try
            {
                // Llamamos al método que ya tienes construido
                var result = Data.ExpenseDetail.GetAllExpenseRrhhByPaidDatexxx(fechafin);

                // Verificamos si hay datos, de lo contrario devolvemos una lista vacía
                var lstExpenseDetail = result != null ? result.ToList() : new List<Entities.ViewModels.ExpenseDetailReportxxx>();

                return new JsonResult()
                {
                    Data = new { data = lstExpenseDetail },
                    MaxJsonLength = Int32.MaxValue, // <--- Esto soluciona el error de los 4mil registros
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            catch (Exception ex)
            {
                // En caso de error, devolvemos el código 500 para que AJAX lo capture
                Response.StatusCode = 500;
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult RrhhReportPartialv()
        {
            

            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];
            List<Entities.ViewModels.ExpenseDetailReport> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();

            try
            {

                var result = Data.ExpenseDetail
                    .GetAllExpenseRrhhByPaidDate(eParameter.PaidDate.GetValueOrDefault().ToShortDateString());
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                    Session["lstExpenseDetail2"] = lstExpenseDetail;
                }
                else
                {
                    lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("RrhhReportPartial", lstExpenseDetail);
        }
        /// <summary>
        /// Método para exportacion formato xls del detalle de viaticos con formato para Recursos Humanos.
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportRrhhReport()

        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];
            List<Entities.ViewModels.ExpenseDetailReport> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();

            try
            {

                var result = Data.ExpenseDetail
                    .GetAllExpenseRrhhByPaidDate(eParameter.PaidDate.GetValueOrDefault().ToShortDateString());
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                }
                else
                {
                    lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return GridViewExtension.ExportToXls(PersonalizedClasses.ExpenseDetail.ExportRrhhSettings, lstExpenseDetail.ToList());
        }
    
        public ActionResult ExportRrhhReportv()

        {
     
         var result = (List<Entities.ViewModels.exportarrhh>)Session["lstExpenseDetail2"];
            List<Entities.ViewModels.exportarrhh> lstExpenseDetail = new List<Entities.ViewModels.exportarrhh>();

            try
            {
 
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                

                }
                else
                {
                    lstExpenseDetail = new List<Entities.ViewModels.exportarrhh>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return GridViewExtension.ExportToXls(PersonalizedClasses.ExpenseDetail.ExportRrhhSettings, lstExpenseDetail);
        }




        /// <summary>
        /// Accion que llama a metodo GetAllExpenseTechnicalByPaidDate de la capade datos ExpenseDetail.
        /// </summary>
        /// <returns></returns>
        public ActionResult TechnicalReportPartial()
        {
          

            return PartialView("TechnicalReportPartial");
        }
        public ActionResult TechnicalReportPartialv()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
         
            try
            {
                eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];
                var result = Data.ExpenseDetail
                    .GetAllExpenseTechnicalByPaidDate(eParameter.PaidDate.GetValueOrDefault().ToShortDateString(), eParameter.ManagementId.GetValueOrDefault());
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                    Session["lstExpenseDetail"] = lstExpenseDetail;
                }
                else
                {
                    lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("TechnicalReportPartial", lstExpenseDetail);
        }


        /// <summary>
        /// Método para exportacion formato xls del detalle de viaticos con formato para Recursos Humanos.
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportTechnicalReport()

        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];
            List<Entities.ViewModels.ExpenseDetailReport> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();

            try
            {
         var       result = (List<Entities.ViewModels.ExpenseDetailReport>)Session["lstExpenseDetail"];
                //  var result = Data.ExpenseDetail
                //    .GetAllExpenseTechnicalByPaidDate(eParameter.PaidDate.GetValueOrDefault().ToShortDateString(),eParameter.ManagementId.GetValueOrDefault());
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                }
                else
                {
                    lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return GridViewExtension.ExportToXls(PersonalizedClasses.ExpenseDetail.ExportTechnicalSettings, lstExpenseDetail.ToList());
        }


      

        /// <summary>
        /// Accion que llama al metodoGetAllFinanceExpensesSummaryByManagementClassPeriodCompanysi y
        /// luego retorna la vista parcial FinancialSummaryReportPartial. 
        /// </summary>
        /// <returns></returns>
        public ActionResult FinancialSummaryReportPartial()
        {
           
            return PartialView("FinancialSummaryReportPartial");
        }
        public ActionResult FinancialSummaryReportPartialv()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> lstSummary = new List<Entities.ViewModels.ExpenseDetailReport>();

            try
            {
                eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];


                var result = Data.ExpenseDetail
                    .GetAllFinanceExpensesSummaryByManagementClassPeriodCompany(eParameter.ManagementId
                    .GetValueOrDefault(),

                                                                                eParameter.ClassId.GetValueOrDefault(),
                                                                                eParameter.PeriodId.GetValueOrDefault(),
                                                                                eParameter.Company);


                if (result != null)
                {
                    lstSummary = result.ToList();
                }
                else
                {
                    lstSummary = new List<Entities.ViewModels.ExpenseDetailReport>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("FinancialSummaryReportPartial", lstSummary);
        }
        /// <summary>
        /// Accion que llama a los metodos  para exportar consolidado de viaticos.
        /// </summary>
        /// <returns></returns>
        public ActionResult FinancialSummaryExport()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];


            ExpensesReimbursement detailReport = new ExpensesReimbursement();//Objeto de instancia del reporte

            detailReport.DataSource = Data.ExpenseDetail.GetAllFinanceExpensesSummaryByManagementClassPeriodCompany(eParameter.ManagementId
                .GetValueOrDefault(),
                                                                                                          eParameter.ClassId
                .GetValueOrDefault(),
                                                                                                          eParameter.PeriodId
                .GetValueOrDefault(),
                                                                                                          eParameter.Company)
                .ToList();

            return ReportViewerExtension.ExportTo(detailReport);
        }


        /// <summary>
        /// Accion que llama a metodo GetAllFinanceExpensesByManagementClassPeriodCompany del modelo Expenses que retorna la vista parcial
        /// FinancialDetailReportPartial.
        /// </summary>
        /// <returns></returns>
        public ActionResult FinancialDetailReportPartial()
        {
            

            return PartialView("FinancialDetailReportPartial");
        }
        
        public ActionResult cuentaclaro()
        {


            return PartialView("cuentaclaro");
        }
        public ActionResult FinancialDetailReportPartialv()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();

            try
            {
                eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];
                var result = Data.ExpenseDetail
                    .GetAllFinanceExpensesByManagementClassPeriodCompany(eParameter.ManagementId.GetValueOrDefault(),
                                                                         eParameter.ClassId.GetValueOrDefault(),
                                                                         eParameter.PeriodId.GetValueOrDefault(),
                                                                         eParameter.Company);
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                    Session["lstExpenseDetailf"]= lstExpenseDetail; 
                }
                else
                {
                    lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("FinancialDetailReportPartial", lstExpenseDetail);
        }

        public ActionResult FinancialDetailReportPartialvx()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();

            try
            {
                eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];
              
                var result = Data.ExpenseDetail.GetAllFinanceExpensesByManagementClassPeriodCompanyx(eParameter.ManagementId
                    .GetValueOrDefault()
                                                                                                       )
                    .ToList();
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                    Session["lstExpenseDetailf"] = lstExpenseDetail;
                }
                else
                {
                    lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailReport>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("FinancialDetailReportPartial", lstExpenseDetail);
        }
        /// <summary>
        /// Accion que llama a los metodos  para exportar detalle de viaticoa.
        /// </summary>
        /// <returns></returns>
        public ActionResult FinancialDetailReportExportx()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];


            ExpenseDetailReport detailReport = new ExpenseDetailReport();//Objeto de instancia del reporte

            detailReport.DataSource = Data.ExpenseDetail.GetAllFinanceExpensesByManagementClassPeriodCompanyx(eParameter.ManagementId
                .GetValueOrDefault() 
                                                                                                   )
                .ToList();

            return ReportViewerExtension.ExportTo(detailReport);
        }
        public ActionResult FinancialDetailReportExport()
        {
            Entities.ViewModels.FinanceParametersView eParameter = new Entities.ViewModels.FinanceParametersView();
            eParameter = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];


            ExpenseDetailReport detailReport = new ExpenseDetailReport();//Objeto de instancia del reporte

            detailReport.DataSource = Data.ExpenseDetail.GetAllFinanceExpensesByManagementClassPeriodCompany(eParameter.ManagementId
                .GetValueOrDefault(),
                                                                                                   eParameter.ClassId
                .GetValueOrDefault(),
                                                                                                   eParameter.PeriodId
                .GetValueOrDefault(),
                                                                                                   eParameter.Company)
                .ToList();

            return ReportViewerExtension.ExportTo(detailReport);
        }
        /// <summary>
        /// Accion que llama al metodo GetAllYieldSummaryByClassPeriodBoss, el cual carga el consolidado de rendicion de viaticos.
        /// </summary>
        /// <returns></returns>
        public ActionResult YieldSummaryReportPartial()
        {
            

            return PartialView("YieldSummaryReportPartial");
        }

        public ActionResult YieldSummaryReportPartialv()
        {
            Entities.ViewModels.ExpenseParametersView eParameter = new Entities.ViewModels.ExpenseParametersView();
            List<Entities.ViewModels.ExpenseDetailReport> lstSummary = new List<Entities.ViewModels.ExpenseDetailReport>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if (eEmployee != null)
                {
                    if (eEmployee.userlevel >=0)
                    {
                        eParameter = (Entities.ViewModels.ExpenseParametersView)Session["sParameterReport"];


                        var summary = Data.ExpenseDetail.GetAllYieldSummaryByClassPeriodBoss(eParameter.ClassId,
                                                                                                        eParameter.PeriodId,
                                                                                                        eEmployee.Idhrms);

                        if (summary != null)
                        {
                            lstSummary = summary.ToList();

                        }
                        else
                        {
                            lstSummary = new List<Entities.ViewModels.ExpenseDetailReport>();
                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "El usuario no tiene permiso para visualizar el reporte";
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("YieldSummaryReportPartial", lstSummary);
        }


        #endregion

    }
}