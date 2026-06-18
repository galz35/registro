using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using ClosedXML.Excel;
using DevExpress.Data;
using DevExpress.Web;
using DevExpress.Web.Mvc;
using DevExpress.XtraCharts;
using DevExpress.XtraCharts.Printing;
using DevExpress.XtraPrinting;
 using Entities.MyEntities;
using Newtonsoft.Json;
using RestSharp;
using slnRhonline.Models;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class ExpensesController : Controller
    {
        /// <summary>
        /// Declaracion de constantes 
        /// </summary>
        //const string keyModelBoss = "gvAuthorizeBoss";
        const string keyModelConsult = "gvConsult";
        //const string keyModelCoordinator = "gvAuthorizeCoordinator";
        //const string keyModelManager = "gvAuthorizeManager";
        const string keyModelRrhh = "gvAuthorizeRrhh";
        const string keyModelYield = "gvAuthorizeYield";
        const string keyPerson = "sPersonId";
        const string keyViewModel = "gridView";
         #region Consulta de Viáticos de las autorizaciones

        /// <summary>
        /// Accion que retorna la vista parcial AuthorizeConsultPartial
        /// </summary>
        /// <param name="_expenseId"></param>
        /// <returns></returns>

        public PartialViewResult AuthorizeConsultPartial(string _expenseId)
        {
            List<Entities.ViewModels.ExpenseDetailView> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailView>();

            if (Session["_expenseId"] != null && (string)Session["_expenseId"] == _expenseId)
            {
                string _expenseidtemp = (string)Session["_expenseId"];
                lstExpenseDetail = (List<Entities.ViewModels.ExpenseDetailView>)Session["_expenseIdlist"];
            }
            else
            {


                var resultado = Expenses.GetAllExpenseDetailById(int.Parse(_expenseId));

                if (resultado.Count > 0)
                {

                    foreach (var item in resultado)
                    {
                        Entities.ViewModels.ExpenseDetailView nuevoDetalle = new Entities.ViewModels.ExpenseDetailView();
                        nuevoDetalle.ClasificationId = item.ClasificationId;
                        nuevoDetalle.CategoryId = item.CategoryId;
                        nuevoDetalle.SubCategoryId = item.SubCategoryId;
                        nuevoDetalle.TotalAmount = item.Amount;
                        nuevoDetalle.HourStart = item.HourStart;
                        nuevoDetalle.TotalYieldAmount = item.YieldAmount;
                        nuevoDetalle.Justify = item.ExpenseDetailNotes;
                        nuevoDetalle.NOTES = item.ExpenseDetailNotes;
                        lstExpenseDetail.Add(nuevoDetalle);

                    }
                    Session["_expenseIdlist"] = lstExpenseDetail;
                    Session["_expenseId"] = _expenseId;
                }
            }
            if (lstExpenseDetail.Count()>0)
            {
                string justificacion = lstExpenseDetail.FirstOrDefault().Justify;
                if (justificacion!=null && justificacion != "")
                {
                    ViewBag.PrimeraJustificacion = justificacion;
                }
                else
                {
                }
                {

                }
            }


            return PartialView("AuthorizeConsultPartial", lstExpenseDetail);
        }
        public JsonResult AuthorizeConsultPartialjson(string expenseId)
        {
            List<Entities.ViewModels.ExpenseDetailView> lstExpenseDetail = new List<Entities.ViewModels.ExpenseDetailView>();

            if (Session["_expenseId"] != null && (string)Session["_expenseId"] == expenseId)
            {
                string _expenseidtemp = (string)Session["_expenseId"];
                lstExpenseDetail = (List<Entities.ViewModels.ExpenseDetailView>)Session["_expenseIdlist"];
            }
            else
            {


                var resultado = Expenses.GetAllExpenseDetailById(int.Parse(expenseId));

                if (resultado.Count > 0)
                {

                    foreach (var item in resultado)
                    {
                        Entities.ViewModels.ExpenseDetailView nuevoDetalle = new Entities.ViewModels.ExpenseDetailView();
                        nuevoDetalle.ClasificationId = item.ClasificationId;
                        nuevoDetalle.CategoryId = item.CategoryId;
                        nuevoDetalle.SubCategoryId = item.SubCategoryId;
                        nuevoDetalle.TotalAmount = item.Amount;
                        nuevoDetalle.HourStart = item.HourStart;
                        nuevoDetalle.TotalYieldAmount = item.YieldAmount;
                        nuevoDetalle.Justify = item.ExpenseDetailNotes;

                        lstExpenseDetail.Add(nuevoDetalle);

                    }
                    Session["_expenseIdlist"] = lstExpenseDetail;
                    Session["_expenseId"] = expenseId;
                }
            }

            return Json(new { data = lstExpenseDetail }, JsonRequestBehavior.AllowGet);

         }

        #endregion
        #region Dashboards 
        /// <summary>
        /// Accion que retorna la vista parcial DashboardExecutedPlannedPartial, vista que muestra los viaticos ejecutados 
        /// y el disponible de los viaticos.
        /// </summary>
        /// <returns></returns>
        public ActionResult DashboardExecutedPlannedPartial()
        {
            List<Entities.ViewModels.ExpenseBudget> lstBugget = new List<Entities.ViewModels.ExpenseBudget>();

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if ((eEmployee.userlevel == 5) || (eEmployee.userlevel == 6))
                {
                    lstBugget = Data.Expense.GetChartExecutedAssignment(eEmployee.Idhrms);

                }
                else
                {
                    lstBugget = new List<Entities.ViewModels.ExpenseBudget>();
                }
            }


            catch
            {
                return PartialView(new List<Entities.ViewModels.ExpenseBudget>());
            }
            return PartialView(lstBugget);
        }


        /// <summary>
        /// Accion que retorna la vista parcial del Top Ten de Viaticos.
        /// </summary>
        /// <returns></returns>
        public ActionResult DashboardExpensesTopTenPartial()
        {

            List<Entities.ViewModels.ExpenseTopTenView> lstExpense = new List<Entities.ViewModels.ExpenseTopTenView>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if ((eEmployee.userlevel == 5) || (eEmployee.userlevel == 6))
                {
                    string respPeriodId = Utils.ClaroWCF.GetExtraTimePeriod(DateTime.Today.ToShortDateString());
                    lstExpense = Data.Expense.GetExpensesByTopTen(respPeriodId, eEmployee.Idhrms);
                }
                else
                {
                    lstExpense = new List<Entities.ViewModels.ExpenseTopTenView>();
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView(lstExpense);

        }

        /// <summary>
        /// Accion para exportar el dashboard asignacion vs ejecución a pdf.
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportChart()
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            ChartControlSettings settings = ChartSettings.GetChartExpensesSettings();

            using (MemoryStream stream = new MemoryStream())
            {
                settings.SaveToStream(stream);
                stream.Seek(0, SeekOrigin.Begin);
                ChartControl chartControl = new ChartControl();
                chartControl.LoadFromStream(stream);
                chartControl.Width = Convert.ToInt16(settings.Width.Value);
                chartControl.Height = Convert.ToInt16(settings.Height.Value);
                chartControl.OptionsPrint.SizeMode = PrintSizeMode.Zoom;
                chartControl.DataSource = new List<Entities.ViewModels.ExpenseBudget>(Data.Expense
                    .GetChartExecutedAssignment(eEmployee.Idhrms));
                var pcl = new PrintableComponentLink(new PrintingSystem());
                pcl.Component = chartControl;
                pcl.Landscape = true;
                pcl.CreateDocument();

                using (var exstream = new MemoryStream())
                {
                    pcl.PrintingSystem.ExportToPdf(exstream);

                    byte[] buf = new byte[(int)exstream.Length];
                    exstream.Seek(0, SeekOrigin.Begin);
                    exstream.Read(buf, 0, buf.Length);

                    return File(buf, "application/pdf", "Chart-Viaticos" + Guid.NewGuid().ToString() + ".pdf");
                }
            }
        }



        #endregion
        #region Reportes

        /// <summary>
        /// Accion que retorna la vista  ParametersReport
        /// </summary>
        /// <returns></returns>
        public ViewResult ParametersReport()
        {
            //Data.ExpensePeriod.GetAllPeriodsByClass(17);

            //          Session["PV"] = result;
            String managementId = (string)Session["managementId"];
            Session["V17"] = Data.ExpensePeriod.GetAllPeriodsByClass(17
                 , managementId);
           
            Session.Remove("sParameterReport");
            return View();
        }
        public JsonResult GetAllPeriodsByClassjson(int classId)
        {
            String managementId = (string)Session["managementId"];
            var periods = Data.ExpensePeriod.GetAllPeriodsByClass(classId, managementId);
            return Json(periods, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Accion que retorna la vista FinancialParameters
        /// </summary>
        /// <returns></returns>
        public ViewResult FinancialParameters()
        {
            Session.Remove("sParameterReport");
            return View();
        }


        #endregion
        #region Binding Lista de empleados para consulta


        /// <summary>
        /// Accion que retorna la vista EmployeesList
        /// </summary>
        /// <returns></returns>
        public ActionResult EmployeesList()
        {

            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();
            try
            {
                lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();
                                Session["listaemployee"] = lstEmployees;

            }
            catch (Exception e)
            {

                throw;
            }
            return View();

        }
        public ActionResult SaldoViatico()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ObtenerPresupuestoViaticos(string fecha) // dd/MM/yyyy
        {       
            
            ServiceReference1.ClaroAsemClient proxy = new ServiceReference1.ClaroAsemClient();

            try
            {
                // 1) Periodos: actual + próximo (formato yyyy-MM)
                var ci = new CultureInfo("es-NI");
                DateTime f;
                if (!DateTime.TryParseExact((fecha ?? "").Trim(), "dd/MM/yyyy", ci, DateTimeStyles.None, out f))
                    f = DateTime.Today;
                var periodoActual = f.ToString("yyyy-MM");
                var periodoSiguiente = f.AddMonths(1).ToString("yyyy-MM");
                var periodos = new[] { periodoActual, periodoSiguiente };

                // 2) Gerencias fijas (IDs “quemados”)
                var gerencias = new[]
                {
                    new GerenciaItem { Id = 6656, Nombre = "GERENCIA TECNICA" },
                    new GerenciaItem { Id = 6731, Nombre = "GERENCIA OPERACIONES PLANTA INTERNA" },
                    new GerenciaItem { Id = 6671, Nombre = "GERENCIA OPERACIONES PLANTA EXTERNA" },
                    new GerenciaItem { Id = 6817, Nombre = "GERENCIA DE IMPLANTACION" }
                };

                // 3) Consulta a proxy por periodo+gerencia
                var lista = new List<ResumenPresupuestoVm>();
                foreach (var per in periodos)
                {
                    foreach (var g in gerencias)
                    {
                        // Nota: segundo parámetro es el OrgId esperado por tu API (usa tu método real)
                        var rows = proxy.GetBudgetByPeriod(per, g.Id) ?? new List<Entities.ViewModels.ExpenseBudget>();
                        var r = rows.FirstOrDefault();

                        var presup = r != null ? r.AssignmentAmount : 0m;
                        var ejec = r != null ? r.ExecutedAmount : 0m;
                        var dispo = presup - ejec;

                        lista.Add(new ResumenPresupuestoVm
                        {
                            Periodo = per,
                            GerenciaId = g.Id,
                            Gerencia = g.Nombre,
                            Presupuestado = presup,
                            Ejecutado = ejec,
                            Disponible = dispo,
                            TienePresupuesto = presup > 0m
                        });
                    }
                }

                // 4) Totales por periodo (para KPI)
                var totales = lista
                    .GroupBy(x => x.Periodo)
                    .Select(s => new
                    {
                        Periodo = s.Key,
                        Presupuestado = s.Sum(x => x.Presupuestado),
                        Ejecutado = s.Sum(x => x.Ejecutado),
                        Disponible = s.Sum(x => x.Disponible)
                    })
                    .OrderBy(x => x.Periodo)
                    .ToList();

                return Json(new
                {
                    ok = true,
                    periodos = periodos,
                    data = lista,
                    resumen = totales
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult ObtenerPresupuestoActualProximo()
        {
            try
            {                               
                ServiceReference1.ClaroAsemClient proxy = new ServiceReference1.ClaroAsemClient();

                // Periodos actual y próximo (yyyy-MM)
                // Regla: si hoy es día 1–4, usar el mes ANTERIOR como "periodoActual";
                // caso contrario, usar el mes actual. "periodoProximo" = periodoActual + 1 mes.
                var hoy = DateTime.Now;
                var basePeriodo = (hoy.Day <= 4) ? hoy.AddMonths(-1) : hoy;  // si querés SOLO 3 o 4: (hoy.Day == 3 || hoy.Day == 4)

                var periodoActual = basePeriodo.ToString("yyyy-MM");
                var periodoProximo = basePeriodo.AddMonths(1).ToString("yyyy-MM");
                var periodos = new[] { periodoActual, periodoProximo };


                // Gerencias fijas (IDs quemados)
                var gerencias = new[]
                {
                    new GerenciaItem { Id = 6656, Nombre = "GERENCIA TECNICA" },
                    new GerenciaItem { Id = 6731, Nombre = "GERENCIA OPERACIONES PLANTA INTERNA" },
                    new GerenciaItem { Id = 6671, Nombre = "GERENCIA OPERACIONES PLANTA EXTERNA" },
                    new GerenciaItem { Id = 6817, Nombre = "GERENCIA DE IMPLANTACION" }
                };

                // Consulta por periodo/gerencia
                var lista = new List<ResumenPresupuestoVm>();
                foreach (var per in periodos)
                {
                    foreach (var g in gerencias)
                    {
                        var rows = proxy.GetBudgetByPeriod(per, g.Id) ?? new List<Entities.ViewModels.ExpenseBudget>();
                        var r = rows.FirstOrDefault();

                        var presup = r != null ? r.AssignmentAmount : 0m; // presupuestado
                        var ejec = r != null ? r.ExecutedAmount : 0m; // gastado
                        var dispo = presup - ejec;
                        var pct = presup > 0m ? Math.Round((ejec * 100m) / presup, 1) : 0m;

                        lista.Add(new ResumenPresupuestoVm
                        {
                            Periodo = per,
                            GerenciaId = g.Id,
                            Gerencia = g.Nombre,
                            Presupuestado = presup,
                            Ejecutado = ejec,
                            Disponible = dispo,
                            PorcentajeUso = pct,
                            TienePresupuesto = presup > 0m
                        });
                    }
                }

                return Json(new
                {
                    ok = true,
                    periodos = periodos,
                    data = lista
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // ====== DTOs locales ======
        public class GerenciaItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }

        public class ResumenPresupuestoVm
        {
            public string Periodo { get; set; }
            public int GerenciaId { get; set; }
            public string Gerencia { get; set; }
            public decimal Presupuestado { get; set; }   // presupuesto
            public decimal Ejecutado { get; set; }       // gastado
            public decimal Disponible { get; set; }      // presupuesto - gastado
            public decimal PorcentajeUso { get; set; }   // (gastado/presupuesto)*100
            public bool TienePresupuesto { get; set; }
        }
        public JsonResult EmployeesListjson()
        {

            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();
            try
            {
                lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();
                Session["listaemployee"] = lstEmployees;

                // Serializar la lista de empleados utilizando Newtonsoft.Json
                //  string jsonData = JsonConvert.SerializeObject(lstEmployees);
                var projectedEmployees = lstEmployees.Select(employee => new
                {
                    Idhrms = employee.Idhrms,
                   
                    idorganizacion=employee.GERENCIAIDHRMS,
                    EmployeeNumber = employee.EmployeeNumber,
                    FullName = employee.FullName,
                    Location= employee.Nombreubicacion
             }).ToList();
                return Json(new { data = projectedEmployees }, JsonRequestBehavior.AllowGet); ;
 
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
         }
        public JsonResult EmployeesListjsone()
        {

            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();
            try
            {
                lstEmployees = Data.Employee.GetEmployeesByBossToHoraextra();
                Session["listaemployee"] = lstEmployees;

                // Serializar la lista de empleados utilizando Newtonsoft.Json
                //  string jsonData = JsonConvert.SerializeObject(lstEmployees);
                var projectedEmployees = lstEmployees.Select(employee => new
                {
                    Idhrms = employee.Idhrms,

                    idorganizacion = employee.GERENCIAIDHRMS,
                    EmployeeNumber = employee.EmployeeNumber,
                    FullName = employee.FullName,
                    Location = employee.Nombreubicacion
                }).ToList();
                return Json(new { data = projectedEmployees }, JsonRequestBehavior.AllowGet); ;

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        
        public JsonResult AuthorizeCoordinatorPartialjson()
        {

            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {
                if (Session["Detalleviatico"] != null)
                {
                    lstDetail = (List<Entities.ViewModels.ExpenseDetailView>)Session["Detalleviatico"];


                }
                else
                {

                    var result = Data.Expense.GetAllExpensesAuthorizeCoordinator();
                    if (result != null)
                    {
                        lstDetail = result.ToList();
                        Session["Detalleviatico"] = lstDetail;
                    }

                    else
                    {
                        lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            var data2 = lstDetail.Select(x => new ExpenseGridDto
            {
                ExpenseId = x.ExpenseId,
                EmployeeNumber = x.EmployeeNumber,
                FullName = x.FullName,
                ExpenseDate = x.ExpenseDate == default(DateTime) ? (DateTime?)null : x.ExpenseDate,
                ClassName = x.ClassName,
                TotalAmount = x.TotalAmount,
                ExpenseStatus = x.ExpenseStatus,
                VehicleNumber = x.VehicleNumber
            }).OrderBy(x => x.EmployeeNumber)                           // ← Carnet
.ThenByDescending(x => x.ExpenseDate ?? DateTime.MinValue) // ← Fecha DESC
.ToList(); ;

            return Json(new { data = data2 }, JsonRequestBehavior.AllowGet);


        }
        public JsonResult AuthorizeManagerPartialjson()
        {

            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {
                if (Session["Detalleviaticog"] != null)
                {
                    lstDetail = (List<Entities.ViewModels.ExpenseDetailView>)Session["Detalleviaticog"];


                }
                else
                {

                    var result = Data.Expense.GetAllExpensesAuthorizeManager();
                    if (result != null)
                    {
                        lstDetail = result.ToList();
                        if (lstDetail.Count()>0)
                        {
                            Session["Detalleviaticog"] = lstDetail;
                        }
                        else
                        Session["Detalleviaticog"] = null;
                    }

                    else
                    {
                        lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }

             }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            var data2 = lstDetail.Select(x => new ExpenseGridDto
            {
                ExpenseId = x.ExpenseId,
                EmployeeNumber = x.EmployeeNumber,
                FullName = x.FullName,
                ExpenseDate = x.ExpenseDate == default(DateTime) ? (DateTime?)null : x.ExpenseDate,
                ClassName = x.ClassName,
                TotalAmount = x.TotalAmount,
                ExpenseStatus = x.ExpenseStatus,
                VehicleNumber = x.VehicleNumber
            }).OrderBy(x => x.EmployeeNumber)                           // ← Carnet
.ThenByDescending(x => x.ExpenseDate ?? DateTime.MinValue) // ← Fecha DESC
.ToList(); ;
            return Json(new { data = data2 }, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Accion que retorna el resultado de la accion EmployeesBindingCore
        /// </summary>
        /// <returns></returns>
        public ActionResult EmployeesListPartial()
        {
            List<Entities.Employees> lstEmployees = new  List<Entities.Employees>();
            try
            {
                lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();
                Session["listaemployee"] = lstEmployees;

            }
            catch (Exception)
            {

                throw;
            }
            return PartialView(lstEmployees);
        }

        
        #endregion
        #region Binding del listado de registro de viáticos.

        /// <summary>
        /// Accion que retorna la vista RegisterDetail
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult RegisterDetail(long id)
        {
          

            Entities.Employees Employee = new Entities.Employees();
            if (id >100000)
            {
                Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.Idhrms == id);
            }
            else
            Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.Idhrms == id);
          //  Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);
          // Se debera cambiar por la api de foto
            Session.Remove("sExpense");
            Session[keyPerson] = id;
            Session["fullName"] = Employee.FullName;
            Session["IDempleado"] = id;
            Session["empleado"] = Employee;
            return View("RegisterDetail", Employee);
        }
        public ActionResult RegisterDetail2(string carnet)
        {


            Entities.Employees Employee = new Entities.Employees();
            if (carnet!=null && carnet.Length>0 )
            {
                Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.EmployeeNumber == carnet);
            }
            else
                Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.EmployeeNumber == carnet);
            //  Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);
            // Se debera cambiar por la api de foto
            Session.Remove("sExpense");
            Session[keyPerson] = Employee.Idhrms  ;
            Session["fullName"] = Employee.FullName;
            Session["IDempleado"] = Employee.Idhrms;
            Session["empleado"] = Employee;
            return View("RegisterDetail", Employee);
        }

        public ActionResult RegisterDetailx( )
        {
            long id = (long)Session[keyPerson];

            Entities.Employees Employee = new Entities.Employees();
            if (id > 100000)
            {
                Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.Idhrms == id);
            }
            else
                Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.Idhrms == id);
            //  Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);
            // Se debera cambiar por la api de foto
            Session.Remove("sExpense");
            Session[keyPerson] = id;
            Session["fullName"] = Employee.FullName;
            Session["IDempleado"] = id;
            Session["empleado"] = Employee;
            return View("RegisterDetail", Employee);
        }

        /// <summary>
        /// Accion  que retorna la vista parcial RegisterDetailPartial.
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        public ActionResult RegisterDetailPartial(long personId)
        {
            Session[keyPerson] = personId;
            List<Entities.ViewModels.ExpenseDetailView> lstExpenseDetailView = new List<Entities.ViewModels.ExpenseDetailView>();
            string carnet = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.Idhrms == personId).EmployeeNumber;

            var result = Expenses.GetExpenseDetailViewByPersonId(carnet);

            if (result.Count > 0)
            {
                var oExpenseDetail = from item in result
                                     group item by new
                                     {
                                         item.ExpenseId,
                                         item.EmployeeNumber,
                                         item.PersonId,
                                         item.FullName,
                                         item.ExpenseDate,
                                         item.ClassId,
                                         item.ClassName,
                                         item.ReasonId,
                                         item.Justify,
                                         item.Route,
                                         item.ServiceNumber,
                                         item.ExpenseStatus
                                     } into g

                                     select new Entities.ViewModels.ExpenseDetailView
                                     {
                                         ExpenseId = g.Key.ExpenseId,
                                         EmployeeNumber = g.Key.EmployeeNumber,
                                         PersonId = g.Key.PersonId,
                                         FullName = g.Key.FullName,
                                         ExpenseDate = g.Key.ExpenseDate,
                                         ClassId = g.Key.ClassId,
                                         ClassName = g.Key.ClassName,
                                         ReasonId = g.Key.ReasonId,
                                         Justify = g.Key.Justify,
                                         Route = g.Key.Route,
                                         ServiceNumber = g.Key.ServiceNumber,
                                         ExpenseStatus = g.Key.ExpenseStatus,
                                         TotalAmount = g.Sum(y => y.TotalAmount)
                                     };
                lstExpenseDetailView = oExpenseDetail.OrderByDescending(o => o.ExpenseDate).ToList();
                Entities.ViewModels.ExpenseDetailView expenseDetailView = new Entities.ViewModels.ExpenseDetailView();
                expenseDetailView = lstExpenseDetailView.FirstOrDefault();


                return PartialView("RegisterDetailPartial", lstExpenseDetailView);
            }


            return PartialView("RegisterDetailPartial", lstExpenseDetailView);
        }
        public JsonResult RegisterDetailPartialjson(long personId)
        {
            Session[keyPerson] = personId;
            List<Entities.ViewModels.ExpenseDetailView> lstExpenseDetailView = new List<Entities.ViewModels.ExpenseDetailView>();
        string carnet=    Data.Employee.GetEmployeesByBossToExpenses().First(item => item.Idhrms == personId).EmployeeNumber;
            try
            {
                //var result = await Task.Run(() => Expenses.GetExpenseDetailViewByPersonId(personId)); // Se usa Task.Run para simular una operación asincrónica.
                var result = Expenses.GetExpenseDetailViewByPersonId(carnet);
                    // Se usa Task.Run para simular una operación asincrónica.
                if (result.Count > 0)
                {   
                    var oExpenseDetail = from item in result
                                         group item by new
                                         {
                                             item.ExpenseId,
                                             item.EmployeeNumber,
                                             item.PersonId,
                                             item.FullName,
                                             item.ExpenseDate,
                                             item.ClassId,
                                             item.ClassName,
                                             item.ReasonId,
                                             item.Justify,
                                             item.Route,
                                             item.ServiceNumber,
                                             item.ExpenseStatus
                                         } into g

                                         select new Entities.ViewModels.ExpenseDetailView
                                         {
                                             ExpenseId = g.Key.ExpenseId,
                                             EmployeeNumber = g.Key.EmployeeNumber,
                                             PersonId = g.Key.PersonId,
                                             FullName = g.Key.FullName,
                                             ExpenseDate = g.Key.ExpenseDate,
                                             ClassId = g.Key.ClassId,
                                             ClassName = g.Key.ClassName,
                                             ReasonId = g.Key.ReasonId,
                                             Justify = g.Key.Justify,
                                             Route = g.Key.Route,
                                             ServiceNumber = g.Key.ServiceNumber,
                                             ExpenseStatus = g.Key.ExpenseStatus,
                                             TotalAmount = g.Sum(y => y.TotalAmount)
                                         };
                    lstExpenseDetailView = oExpenseDetail.OrderByDescending(o => o.ExpenseDate).ToList();
 
                    Session["tempviaticovalidardoble"] = lstExpenseDetailView;
                }
            }
            catch (Exception)
            {
                 return Json(new { data = lstExpenseDetailView }, JsonRequestBehavior.AllowGet);

            }
            return Json(new { data = lstExpenseDetailView }, JsonRequestBehavior.AllowGet);

         }
        /// <summary>
        /// Accion que retorna el resultado de la accion RegisterDetail
        /// </summary>
        /// <returns></returns>
        public ActionResult BackToRegister()
        {
            long personId = (long)Session[keyPerson];
            Session.Remove("sExpenseDetail");

            return RegisterDetail(personId);
        }
        public ActionResult Consultageneral()
        {
            return View();
        }
        #endregion

        #region CRUD

        /// <summary>
        /// Metodo para validar viaticos al editar o eliminar
        /// </summary>
        /// <param name="expenseId"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ValidateExpense(int expenseId, string option)
        {
            Validations.Expenses vExpenses = new Validations.Expenses();


            try
            {
                if ((option == "editar") || (option == "eliminar"))
                {
                    bool resultAuthorization = vExpenses.ValidateAuthorization(expenseId);
                    if (resultAuthorization == false)
                    {
                        return Json(new { status = "Error", message = "Solo se pueden editar o eliminar viáticos en estado GRABADO" });
                    }
                }

                if (option == "rendir")
                {
                    string resultYieldAuthorization = vExpenses.ValidatYieldeAuthorization(expenseId);
                    bool resultYieldBeforeDate = vExpenses.ValidateYieldBeforeDate(expenseId);
                    if (resultYieldAuthorization != "  ")
                    {
                        return Json(new { status = "Error", message = resultYieldAuthorization });
                    }
                    if (resultYieldBeforeDate == false)
                    {
                        return Json(new { status = "Error", message = "No se puede rendir un viático si la fecha actual es menor que la fecha de ejecución" });
                    }
                }



            }
            catch (Exception ex)
            {

                return Json(new { status = "Error", message = ex.Message });
            }
            //Validar el estado del viático.

            return Json(new { status = "Exito", message = "Exito al actualizar el registro" });

        }
        [HttpPost]
        public JsonResult ValidateExpense1(int expenseId, string option,string url)
        {
            Validations.Expenses vExpenses = new Validations.Expenses();
            var base64EncodedBytes = Convert.FromBase64String(url);
            string decodedUrl = Encoding.UTF8.GetString(base64EncodedBytes);

            var returnUrlx = decodedUrl; // Obtén la URL desde la sesión
            Session["sediturl"] = returnUrlx;
 
            try
            {
                if ((option == "editar") || (option == "eliminar"))
                {
                    bool resultAuthorization = vExpenses.ValidateAuthorization(expenseId);
                    if (resultAuthorization == false)
                    {
                        return Json(new { status = "Error", message = "Solo se pueden editar o eliminar viáticos en estado GRABADO" });
                    }
                }

                if (option == "rendir")
                {
                    string resultYieldAuthorization = vExpenses.ValidatYieldeAuthorization(expenseId);
                    bool resultYieldBeforeDate = vExpenses.ValidateYieldBeforeDate(expenseId);
                    if (resultYieldAuthorization != "  ")
                    {
                        return Json(new { status = "Error", message = resultYieldAuthorization });
                    }
                    if (resultYieldBeforeDate == false)
                    {
                        return Json(new { status = "Error", message = "No se puede rendir un viático si la fecha actual es menor que la fecha de ejecución" });
                    }
                }



            }
            catch (Exception ex)
            {

                return Json(new { status = "Error", message = ex.Message });
            }
            //Validar el estado del viático.

            return Json(new { status = "Exito", message = "Exito al actualizar el registro" });

        }
        [HttpPost]
        public JsonResult ValidateExpense2(int expenseId, string option, string url)
        {
            Validations.Expenses vExpenses = new Validations.Expenses();
            var base64EncodedBytes = Convert.FromBase64String(url);
            string decodedUrl = Encoding.UTF8.GetString(base64EncodedBytes);

            var returnUrlx = decodedUrl; // Obtén la URL desde la sesión
            Session["sediturl"] = returnUrlx;
            Session["url"] = url;
          
            //Validar el estado del viático.

            return Json(new { status = "Exito", message = "Exito al actualizar el registro" });

        }

        /// <summary>
        /// Accion que retorna la vista MasterDetail que es la interfaz para agregar un viático.
        /// </summary>
        /// <param name="expenseId"></param>
        /// <returns></returns>

        public ActionResult AddExpense()
        {
            Session.Remove("sExpenseDetail");
            Session["sExpense"] = null;

            return View("MasterDetail");
        }
        public ActionResult Presupuesto()
        { return View(); }

        [HttpGet]
        public JsonResult GetExpenseGerenciaSession(string periodId, long gerencia)
        {
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
                eEmployee = (Entities.Employees)Session["User"];

            if (eEmployee == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            // Construcción del endpoint
            string apiUrl = $"http://172.26.54.66/apihcm/api/px/GetExpensegerencia?gerencia={gerencia}&periodo={periodId}";

            // Consumir la API con RestSharp
            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = -1;  // Sin límite de tiempo, según tu ejemplo
             List<viaticodetallemodal> expenses = new List<viaticodetallemodal>();
            try
            {
                var response = client.Execute(request);
            if (response == null)
            {
                // Manejo de error
                return Json(null, JsonRequestBehavior.AllowGet);
            }

                // Deserializar
              expenses = JsonConvert.DeserializeObject<List<viaticodetallemodal>>(response.Content);

             }
            catch(Exception e)
            {
                // Error de parseo
                return Json(null, JsonRequestBehavior.AllowGet);
            }
             return Json(new { data = expenses }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult ObtenerDetallesPresupuestoPorGerenciaSession(string periodId, long gerencia)
        {
            var organizaciones = Data.organizaciongerencia.ObtenerOrganizaciones();
 
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
                eEmployee = (Entities.Employees)Session["User"];

            if (eEmployee == null)
            {
                return Json(new { data = new List<BudgetDetail>() }, JsonRequestBehavior.AllowGet);
            }

            string gerenciax = organizaciones.Any(x => x.IdOrg == gerencia)
                ? organizaciones.First(x => x.IdOrg == gerencia).IdHrms
                : eEmployee.GERENCIAIDHRMS;

            string apiUrl = $"http://172.26.54.66/apihcm/api/px/ObtenerDetallesPresupuestoPorGerencia?gerencia={gerenciax}&periodo={periodId}";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = -1;
            bool esRRHH = eEmployee?.GERENCIAIDHRMS == "343";

            try
            {
                var response = client.Execute(request);
                if (response == null || response.Content == "null")
                {
                    return Json(new { data = new List<BudgetDetail>(), esRRHH }, JsonRequestBehavior.AllowGet);
                }

                var detalles = JsonConvert.DeserializeObject<List<BudgetDetail>>(response.Content);
                return Json(new { data = detalles , esRRHH }, JsonRequestBehavior.AllowGet);
            }
            catch(Exception e)
            {
                return Json(new { data = new List<BudgetDetail>() , esRRHH }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult ObtenerGerenciasSession()
        {
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
                eEmployee = (Entities.Employees)Session["User"];

            if (eEmployee == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            // IDs de Gerencia Técnica
            var gerenciasTecnica = new List<Gerencia>
            {
                new Gerencia { Id = 300000002902772, Nombre = "NI GERENCIA OPERACIONES PLANTA EXTERNA", Codigo = "6671" },
                new Gerencia { Id = 300000002902948, Nombre = "NI GERENCIA OPERACIONES PLANTA INTERNA", Codigo = "6731" },
                new Gerencia { Id = 300000002902619, Nombre = "NI GERENCIA DE IMPLANTACION", Codigo = "6817" },
                new Gerencia { Id = 300000002903006, Nombre = "NI GERENCIA TECNICA", Codigo = "6656" }
            };

            var gerencias = new List<Gerencia>();

            // Validar si es gerencia técnica (por Codigo) o no
            if (gerenciasTecnica.Any(g => g.Codigo == eEmployee.GERENCIAIDHRMS))
            {
                gerencias = gerenciasTecnica;
            }
            else
            {
                gerencias.Add(new Gerencia
                {
                    Id = long.Parse(eEmployee.GERENCIAID),
                    Nombre = eEmployee.GERENCIA,
                    Codigo = eEmployee.GERENCIAIDHRMS
                });
            }

            return Json(gerencias, JsonRequestBehavior.AllowGet);
        }

        // POST: Presupuesto/RegistrarPresupuestosSession
        [HttpPost]
        public async Task<JsonResult> RegistrarPresupuestosSessionx([System.Web.Http.FromBody] RegistrarPresupuestosRequest request)
        {
            Entities.Employees eEmployee = null;
            var gerenciasTecnica = new List<Gerencia>
            {
                new Gerencia { Id = 300000002902772, Nombre = "NI GERENCIA OPERACIONES PLANTA EXTERNA", Codigo = "6671" },
                new Gerencia { Id = 300000002902948, Nombre = "NI GERENCIA OPERACIONES PLANTA INTERNA", Codigo = "6731" },
                new Gerencia { Id = 300000002902619, Nombre = "NI GERENCIA DE IMPLANTACION", Codigo = "6817" },
                new Gerencia { Id = 300000002903006, Nombre = "NI GERENCIA TECNICA", Codigo = "6656" }
            };
            string g1 = "";

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            if (eEmployee == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            if (gerenciasTecnica.Count(x => x.Id == request.Presupuestos.FirstOrDefault().GerenciaId) > 0)
            {
                g1 = gerenciasTecnica.Where(x => x.Id == request.Presupuestos.FirstOrDefault().GerenciaId).FirstOrDefault().Codigo;
            }
            else
            {
                g1 = eEmployee.GERENCIAIDHRMS;
            }
            long usuarioRegistro = 0;
            long.TryParse(eEmployee.EmployeeNumber, out usuarioRegistro);

            bool exitoTotal = true;

            using (HttpClient client = new HttpClient())
            {
                foreach (var presupuesto in request.Presupuestos)
                {
                    string apiUrl = $"http://172.26.54.66/apihcm/api/px/insert?gerencia={g1}&montoAumentar={presupuesto.MontoAumentar}&montoDisminuir={presupuesto.MontoDisminuir}&justificacion={System.Net.WebUtility.UrlEncode(request.Justificacion)}&usuarioRegistro={usuarioRegistro}&periodo={System.Net.WebUtility.UrlEncode(request.Periodo)}";
                    var response = await client.GetAsync(apiUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        exitoTotal = false;
                        break;
                    }
                }
            }

            return Json(new { success = exitoTotal });
        }

        [HttpPost]
        public JsonResult RegistrarPresupuestosSession([System.Web.Http.FromBody] RegistrarPresupuestosRequest request)
        {
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
                eEmployee = (Entities.Employees)Session["User"];

            if (eEmployee == null)
            {
                return Json(new { success = false, message = "Usuario no autenticado." }, JsonRequestBehavior.AllowGet);
            }

            long usuarioRegistro = 0;
            long.TryParse(eEmployee.EmployeeNumber, out usuarioRegistro);

            bool exitoTotal = true;
            var gerenciasTecnica = new List<Gerencia>
            {
                new Gerencia { Id = 300000002902772, Nombre = "NI GER OPERACIONES PLANTA EXTERNA", Codigo = "6671" },
                new Gerencia { Id = 300000002902948, Nombre = "NI GER OPERACIONES PLANTA INTERNA", Codigo = "6731" },
                new Gerencia { Id = 300000002902619, Nombre = "NI GERENCIA DE IMPLANTACION", Codigo = "6817" },
                new Gerencia { Id = 300000002903006, Nombre = "NI GERENCIA TECNICA", Codigo = "6656" }
            };

            var errores = new List<string>();

            foreach (var presupuesto in request.Presupuestos)
            {
                // Determinar la gerencia: si es técnica, usar Id del request; si no, usar la del usuario
                string gerencia;
                if (gerenciasTecnica.Count(x => x.Id == presupuesto.GerenciaId)>0)
                {
                    gerencia  = gerenciasTecnica.Where(x => x.Id == presupuesto.GerenciaId).FirstOrDefault().Codigo;
                }
                else
                {
                    gerencia = eEmployee.GERENCIAIDHRMS; // O eEmployee.GERENCIAID (depende de la API)
                }

                // Construir la URL
                string apiUrl = $"http://172.26.54.66/apihcm/api/px/insert?gerencia={gerencia}" +
                                $"&montoAumentar={presupuesto.MontoAumentar}" +
                                $"&montoDisminuir={presupuesto.MontoDisminuir}" +
                                $"&justificacion={Uri.EscapeDataString(request.Justificacion ?? "")}" +
                                $"&usuarioRegistro={usuarioRegistro}" +
                                $"&periodo={Uri.EscapeDataString(request.Periodo ?? "")}";

                var client = new RestClient(apiUrl);
                var restRequest = new RestRequest(Method.GET);
                restRequest.Timeout = -1;

                try
                {
                    var response = client.Execute(restRequest);
                    if (response == null)
                    {
                        exitoTotal = false;
                        errores.Add($"Error al insertar gerencia {gerencia}: {response.StatusCode} - {response.StatusDescription}");
                        // break; // si deseas interrumpir
                    }
                }
                catch (Exception ex)
                {
                    exitoTotal = false;
                    errores.Add($"Excepción al insertar gerencia {gerencia}: {ex.Message}");
                    // break; // si deseas interrumpir
                }
            }

            if (exitoTotal)
            {
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = string.Join("; ", errores) }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult ObtenerGraficoEstado(string periodId, long gerencia)
        {
            var gerenciasTecnica = new List<Gerencia>
            {
                new Gerencia { Id = 300000002902772, Nombre = "NI GER OPERACIONES PLANTA EXTERNA", Codigo = "6671" },
                new Gerencia { Id = 300000002902948, Nombre = "NI GER OPERACIONES PLANTA INTERNA", Codigo = "6731" },
                new Gerencia { Id = 300000002902619, Nombre = "NI GERENCIA DE IMPLANTACION", Codigo = "6817" },
                new Gerencia { Id = 300000002903006, Nombre = "NI GERENCIA TECNICA", Codigo = "6656" }
            };
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
                eEmployee = (Entities.Employees)Session["User"];

            if (eEmployee == null)
            {
                return Json(new { labels = new List<string>(), data = new List<int>() }, JsonRequestBehavior.AllowGet);
            }

            string gerenciax;
            if (gerenciasTecnica.Any(x => x.Id == gerencia))
            {
                gerenciax = gerenciasTecnica.FirstOrDefault(x => x.Id == gerencia).Codigo;
            }
            else
            {
                gerenciax = eEmployee.GERENCIAIDHRMS;
            }

            // Incluir periodId en la URL
            string apiUrl = $"http://172.26.54.66/apihcm/api/px/ObtenerDetallesPresupuestoPorGerencia?gerencia={gerenciax}&periodo={periodId}";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = 30000; // 30 segundos

            List<BudgetDetail> detalles = new List<BudgetDetail>();

            try
            {
                var response = client.Execute(request);
                if (response == null ||  response.Content=="" || response.Content == "null")
                {
                    // Retornar algo vacío
                    return Json(new { labels = new List<string>(), data = new List<int>() }, JsonRequestBehavior.AllowGet);
                }

                detalles = JsonConvert.DeserializeObject<List<BudgetDetail>>(response.Content);
            }
            catch
            {
                return Json(new { labels = new List<string>(), data = new List<int>() }, JsonRequestBehavior.AllowGet);
            }

            var agrupado = detalles
               .GroupBy(d => d.EstadoPresupuesto ?? "SIN_ESTADO")
               .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
               .ToList();

            var labels = agrupado.Select(x => x.Estado).ToList();
            var data = agrupado.Select(x => x.Cantidad).ToList();

            return Json(new { labels, data }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Obtiene los datos para el gráfico de presupuesto (asignado vs consumido)
        /// </summary>
        [HttpGet]
        public JsonResult ObtenerGraficoPresupuesto(string periodId, long gerencia)
        {
            var gerenciasTecnica = new List<Gerencia>
            {
                new Gerencia { Id = 300000002902772, Nombre = "NI GER OPERACIONES PLANTA EXTERNA", Codigo = "6671" },
                new Gerencia { Id = 300000002902948, Nombre = "NI GER OPERACIONES PLANTA INTERNA", Codigo = "6731" },
                new Gerencia { Id = 300000002902619, Nombre = "NI GERENCIA DE IMPLANTACION", Codigo = "6817" },
                new Gerencia { Id = 300000002903006, Nombre = "NI GERENCIA TECNICA", Codigo = "6656" }
            };
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
                eEmployee = (Entities.Employees)Session["User"];

            if (eEmployee == null)
            {
                return Json(new { labels = new List<string>(), data = new List<int>() }, JsonRequestBehavior.AllowGet);
            }

            string gerenciax;
            if (gerenciasTecnica.Any(x => x.Id == gerencia))
            {
                gerenciax = gerenciasTecnica.FirstOrDefault(x => x.Id == gerencia).Id+"";
            }
            else
            {
                gerenciax = eEmployee.GERENCIAID;
            }
            string apiUrl = $"http://172.26.54.66/apihcm/api/px/getpresupuesto?periodId={periodId}&gerencia={gerenciax}";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = -1;   

            var response = client.Execute(request);
            if (response == null || response.Content=="null")
            {
                return Json(new { labels = new List<string>(), asignado = new List<decimal>(), consumido = new List<decimal>() }, JsonRequestBehavior.AllowGet);
            }

            List<PresupuestoResumen> lista = new List<PresupuestoResumen>();
            try
            {
                lista = JsonConvert.DeserializeObject<List<PresupuestoResumen>>(response.Content);
            }
            catch
            {
                return Json(new { labels = new List<string>(), asignado = new List<decimal>(), consumido = new List<decimal>() }, JsonRequestBehavior.AllowGet);
            }

            var labels = new List<string>();
            var asignado = new List<decimal>();
            var consumido = new List<decimal>();

            foreach (var item in lista)
            {
                labels.Add(item.PeriodId);
                asignado.Add(item.AssignmentAmount);
                consumido.Add(item.ExecutedAmount);
            }

            return Json(new { labels, asignado, consumido }, JsonRequestBehavior.AllowGet);
        }
           /// <summary>
            /// POST para aprobar un presupuesto vía RestSharp
            /// </summary>
            [HttpPost]
            public JsonResult AprobarPresupuestoSession(int idPresupuesto)
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                    eEmployee = (Entities.Employees)Session["User"];

                if (eEmployee == null)
                    return Json(new { success = false, message = "Usuario no autenticado." });

                long usuarioCambio = 0;
                long.TryParse(eEmployee.EmployeeNumber, out usuarioCambio);

                string apiUrl = $"http://172.26.54.66/apihcm/api/px/permiso?idPresupuesto={idPresupuesto}&usuarioCambio={usuarioCambio}";

                var client = new RestClient(apiUrl);
                var request = new RestRequest(Method.GET);
                request.Timeout = -1;

                bool exito = false;
                string mensaje = "";

                try
                {
                    var response = client.Execute(request);
                    if (response == null)
                    {
                        exito = true;
                    }
                    else
                    {
                        mensaje = $"Error: {response.StatusCode} - {response.StatusDescription}";
                    }
                }
                catch (Exception ex)
                {
                    mensaje = $"Excepción: {ex.Message}";
                }

                return Json(new { success = exito, message = mensaje });
            }
        [HttpPost]
        public JsonResult gestorPresupuestoSession(int idPresupuesto,string accion)
        {
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
                eEmployee = (Entities.Employees)Session["User"];

            if (eEmployee == null)
                return Json(new { success = false, message = "Usuario no autenticado." });

            long usuarioCambio = 0;
            long.TryParse(eEmployee.EmployeeNumber, out usuarioCambio);

            string apiUrl = $"http://172.26.54.66/apihcm/api/px/permiso2?idPresupuesto={idPresupuesto}&usuarioCambio={usuarioCambio}&accion={accion}";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = -1;

            bool exito = false;
            string mensaje = "";

            try
            {
                var response = client.Execute(request);
                if (response != null && response.Content.Contains("Presupuesto")==true)
                {
                    exito = true;
                }
                else
                {
                    mensaje = $"Error: {response.StatusCode} - {response.StatusDescription}";
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Excepción: {ex.Message}";
            }

            return Json(new { success = exito, message = mensaje });
        }

        public ActionResult AddExpensex(string returnUrl)//aqui hay que corregir no va con la arquitectura MVC
        {
            Session.Remove("sExpenseDetail");
            var base64EncodedBytes = Convert.FromBase64String(returnUrl);
            string decodedUrl = Encoding.UTF8.GetString(base64EncodedBytes);
 
            var returnUrlx = decodedUrl; // Obtén la URL desde la sesión
            Session["sExpense"] = null;
            ViewBag.ReturnUrl = returnUrlx;
            
            return View("MasterDetail");
        }
        

        [HttpPost]
        public ActionResult StoreCurrentUrl(string returnUrl)
        {
            Session["ReturnUrl"] = returnUrl; // Guarda la URL en la sesión
            return new HttpStatusCodeResult(200); // Devuelve un estado de éxito
        }
       
        /// <summary>
        /// Accion que retorna la vista MasterDetail para editar un viatico.
        /// </summary>
        /// <param name="expenseId"></param>
        /// <returns></returns>
        public ActionResult EditExpense(int expenseId)//aqui hay que corregir no va con la arquitectura MVC
        {
            Entities.Expenses expense = new Entities.Expenses();
            // Validations.Expenses vExpenses = new Validations.Expenses();
            //bool resultAuthorization = vExpenses.ValidateAuthorization(expenseId);
            try
            {
                //if (resultAuthorization == false)
                //{
                //  ViewData["EditError"] = "Solo se pueden editar viáticos en estado grabado.";
                // }
                //else
                //{
                expense = Utils.ClaroWCF.GetAllExpenses(expenseId).FirstOrDefault(); //.FirstOrDefault(e => e.ExpenseId == expenseId);
                Session["sExpense"] = expense;

                //}
            }
            catch (Exception)
            {
                throw;
            }
       
            return View("MasterDetail", expense);
            //return RegisterDetail((int)Session[keyPerson]);
        }
        public ActionResult EditExpense1(int expenseId )//aqui hay que corregir no va con la arquitectura MVC
        {
            Session["sExpenseDetail"] = null;
            Entities.Expenses expense = new Entities.Expenses();
            // Validations.Expenses vExpenses = new Validations.Expenses();
            //bool resultAuthorization = vExpenses.ValidateAuthorization(expenseId);
            try
            {
                //if (resultAuthorization == false)
                //{
                //  ViewData["EditError"] = "Solo se pueden editar viáticos en estado grabado.";
                // }
                //else
                //{

                string returnUrlx = "";
                returnUrlx = (string)Session["sediturl"];
          

                 Session["sExpense"] = null;
                ViewBag.ReturnUrl = returnUrlx;
                expense = Utils.ClaroWCF.GetAllExpenses(expenseId).FirstOrDefault(); //.FirstOrDefault(e => e.ExpenseId == expenseId);
                Session["sExpense"] = expense;
                Entities.Expenses eExpense = new Entities.Expenses();
                 List<Entities.ExpenseDetail> lstExpenseDetail = new List<Entities.ExpenseDetail>();


               
                    lstExpenseDetail = Expenses.GetAllEditableExpenseDetail(); //Expenses.GetAllExpenseDetails().Where(e => e.ExpenseId == eExpense.ExpenseId).ToList();
 
              
            }
            catch (Exception)
            {
                throw;
            }

            return View("Mastereditar", expense);
            //return RegisterDetail((int)Session[keyPerson]);
        }
        /// <summary>
        /// Acción para mandar a mostrar la vista parcial DetailPartial que es el detalle del viatico
        /// </summary>
        /// <param name="expense"></param>
        /// <returns></returns>
        public ActionResult EditExpense2(int expenseId)//aqui hay que corregir no va con la arquitectura MVC
        {
            string returnUrl = Session["sediturl"] as string ?? Url.Action("Index", "Home");

            // Pasar la URL a la vista usando ViewBag
            ViewBag.ReturnUrl = returnUrl;
            Entities.Expenses expense = new Entities.Expenses();
            // Validations.Expenses vExpenses = new Validations.Expenses();
            //bool resultAuthorization = vExpenses.ValidateAuthorization(expenseId);
            try
            {
                //if (resultAuthorization == false)
                //{
                //  ViewData["EditError"] = "Solo se pueden editar viáticos en estado grabado.";
                // }
                //else
                //{
                Session["sExpenseDetail"] = null;
                string returnUrlx = "";
                returnUrlx = (string)Session["sediturl"];


                Session["sExpense"] = null;
                ViewBag.ReturnUrl = returnUrlx;
                expense = Utils.ClaroWCF.GetAllExpenses(expenseId).FirstOrDefault(); //.FirstOrDefault(e => e.ExpenseId == expenseId);
                Session["sExpense"] = expense;
                Entities.Expenses eExpense = new Entities.Expenses();
                List<Entities.ExpenseDetail> lstExpenseDetail = new List<Entities.ExpenseDetail>();



                lstExpenseDetail = Expenses.GetAllEditableExpenseDetail(); //Expenses.GetAllExpenseDetails().Where(e => e.ExpenseId == eExpense.ExpenseId).ToList();


            }
            catch (Exception)
            {
                throw;
            }

            return View("Mastereditar1", expense);
            //return RegisterDetail((int)Session[keyPerson]);
        }

        public ActionResult DetailPartial()
        {
            Entities.Expenses eExpense = new Entities.Expenses();
            eExpense = (Entities.Expenses)Session["sExpense"];
            List<Entities.ExpenseDetail> lstExpenseDetail = new List<Entities.ExpenseDetail>();


            if (eExpense != null)
            {
                lstExpenseDetail = Expenses.GetAllEditableExpenseDetail(); //Expenses.GetAllExpenseDetails().Where(e => e.ExpenseId == eExpense.ExpenseId).ToList();
                return PartialView("DetailPartial", lstExpenseDetail);
            }
            else
            {
                return PartialView("DetailPartial", Expenses.GetAllEditableExpenseDetail());
            }
        }

        /// <summary>
        /// Accion para eliminar un viatico.
        /// </summary>
        /// <param name="expenseId"></param>
        /// <returns></returns>
        public ActionResult DeleteExpense(int expenseId)
        {
            Entities.Expenses expense = new Entities.Expenses();
            //Validations.Expenses vExpenses = new Validations.Expenses();
            //bool resultAuthorization = vExpenses.ValidateAuthorization(expenseId);
            try
            {
                //if (resultAuthorization == false)
                //{
                //    ViewData["EditError"] = "El viático no se puede eliminar porque ya fue autorizado o rendido";
                //}
                //else
                //{
                Expenses.DeleteExpense(expenseId);
                //}
            }
            catch (Exception)
            {
                throw;
            }

            return RegisterDetail((long)Session[keyPerson]);
        }


        /// <summary>
        /// Accion que llama al metodo AddDetailLine para insertar una linea de detalle de viático.
        /// </summary>
        /// <param name="eExpenseDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddDetailLine(Entities.ExpenseDetail eExpenseDetail)
        {
            List<Entities.ExpenseDetail> lstExpenseDetail = new List<Entities.ExpenseDetail>();
            Validations.Expenses vExpenses = new Validations.Expenses();

            if (ModelState.IsValid)
            {
                try
                {
                    Expenses.AddDetailLine(eExpenseDetail);
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Ha ocurrido un error en la transaccion, favor verificar los datos.";
            }
            return PartialView("DetailPartial", Expenses.GetAllEditableExpenseDetail());
        }
        [HttpPost]
public JsonResult AddDetailLinejson([System.Web.Http.FromBody] Entities.ExpenseDetail eExpenseDetail)
{
    List<Entities.ExpenseDetail> lstExpenseDetail = (List<Entities.ExpenseDetail>)Session["sExpenseDetail"] ?? new List<Entities.ExpenseDetail>();
    Validations.Expenses vExpenses = new Validations.Expenses();
            var concepto = Concepts.GetAllCategories();
    if (ModelState.IsValid)
    {
        try
        {List<int> validCategories = new List<int> 
{ 
    9, 18, 19, 20, 95, 172
    // Puedes agregar más valores de tu lista si es necesario
};

 
                    if (lstExpenseDetail.Count()>0)
                    {
                        if (validCategories.Contains(eExpenseDetail.CategoryId))
                        {
                            lstExpenseDetail.Add(eExpenseDetail);
                            Session["sExpenseDetail"] = lstExpenseDetail;
                            return Json(new { success = true });
                        }
                        else  if (lstExpenseDetail.Count(x=>x.CategoryId==eExpenseDetail.CategoryId)==0)
                        {
                            lstExpenseDetail.Add(eExpenseDetail);
                            Session["sExpenseDetail"] = lstExpenseDetail;
                            return Json(new { success = true });
                        }
                        else {
                            
                            Session["sExpenseDetail"] = lstExpenseDetail;
                            return Json(new { success = false, message = "Ya existe este tipo de linea:"+ concepto.Where(z=>z.CategoryId==eExpenseDetail.CategoryId).FirstOrDefault().CategoryName });
                        }
                    }
                    else
                    {
                        lstExpenseDetail.Add(eExpenseDetail);
                        Session["sExpenseDetail"] = lstExpenseDetail;
                        return Json(new { success = true });
                    }
        
        }
        catch (Exception e)
        {
            return Json(new { success = false, message = e.Message });
        }
    }
    else
    {
        return Json(new { success = false, message = "Ha ocurrido un error en la transacción, favor verificar los datos." });
    }
}

        /// <summary>
        /// Accion que llama al metodo UpdateDetailLine para editar una linea de detalle de viático.
        /// </summary>
        /// <param name="eExpenseDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult UpdateDetailLine(Entities.ExpenseDetail eExpenseDetail)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Expenses.UpdateDetailLine(eExpenseDetail);
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Ha ocurrido un error en la transaccion, favor verificar los datos.";
            }
            return PartialView("DetailPartial", Expenses.GetAllEditableExpenseDetail());
        }

        /// <summary>
        /// Accion que llama al metodo DeleteDetailLine para eliminar una linea de detalle de viático
        /// </summary>
        /// <param name="eExpenseDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteDetailLine(Entities.ExpenseDetail eExpenseDetail)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Expenses.DeleteDetailLine(eExpenseDetail);
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Ha ocurrido un error en la transaccion, favor verificar los datos.";
            }
            return PartialView("DetailPartial", Expenses.GetAllEditableExpenseDetail());
        }

        /// <summary>
        /// Accion que llama al metodo SaveExpense para insertar o editar el viatico.
        /// </summary>
        /// <param name="eExpense"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveExpense(Entities.Expenses eExpense)
        {
            Entities.Expenses eExpenseBeforeUpdate = new Entities.Expenses();
            Validations.Expenses vExpenses = new Validations.Expenses();
            long personId = (long)Session[keyPerson];
            Entities.Employees persona = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.Idhrms == personId);

            eExpense.carnet = persona.EmployeeNumber;
                string resultPeriodsClosed = "";
            if (ModelState.IsValid)
            {
                try
                {
                      resultPeriodsClosed = vExpenses.ValidatePeriods(eExpense);
                    if (resultPeriodsClosed.Contains("Error")==false)
                    {
                        resultPeriodsClosed = Expenses.GetAllEditableExpenseDetails();
                        if (resultPeriodsClosed== "1")
                        {
                              resultPeriodsClosed = vExpenses.ValidateBudgets(eExpense);
                            if (resultPeriodsClosed == "1")
                            {
                                resultPeriodsClosed = vExpenses.ValidatePendingYieldsx(eExpense);
                                if (resultPeriodsClosed == "1")
                                {
                                    //bool resultDuplicateDates = vExpenses.ValidateDuplicateDates(eExpense);
                                    //if (resultDuplicateDates == true)
                                    //{
                                        bool resultDuplicateConcepts = vExpenses.ValidateDuplicateConceptsbyCategory(eExpense);
                                        if (resultDuplicateConcepts == true)
                                        {
                                            bool resultDuplicateByHours = vExpenses.ValidateDuplicateConceptsbyHours(eExpense);
                                            if (resultDuplicateByHours == true)
                                            {
                                                if (Session["sExpense"] != null)
                                                {
                                                    eExpenseBeforeUpdate = (Entities.Expenses)Session["sExpense"];
                                                    eExpense.ExpenseId = eExpenseBeforeUpdate.ExpenseId;
                                                }
                                                if (eExpense.ExpenseId > 0)//es una actualizacion
                                                {
                                                    Expenses.SaveExpense(eExpense);
                                                }
                                                else
                                                {
 
                                                    eExpense.PersonId = personId;
                                                    Expenses.SaveExpense(eExpense);  //es una inserción.
                                                }
                                            }
                                            else
                                            {
                                                ViewData["EditError"] = "No se pueden guardar categorias con la misma hora.";
                                                return View("MasterDetail", eExpense);
                                            }
                                        }
                                        else
                                        {
                                            ViewData["EditError"] = "No se pueden guardar conceptos tipo cena,desayuno,almuerzo 2 veces.";
                                            return View("MasterDetail", eExpense);
                                        }
                                    //}
                                    //else
                                    //{
                                    //    ViewData["EditError"] = "No se pueden guardar fechas duplicadas.";
                                    //    return View("MasterDetail", eExpense);
                                    //}
                                }
                                else
                                {
                                    ViewData["EditError"] = "No se puede guardar el viático, porque tiene viáticos planificados pendientes del periodo anterior por rendir.";
                                    return View("MasterDetail", eExpense);
                                }
                            }
                            else
                            {
                                //ViewData["EditError"] = resultValidateBudget;
                                return View("MasterDetail", eExpense);
                            }
                        }
                        else
                        {
                            ViewData["EditError"] = "No se puede guardar el viatico, porque no lleva detalle.";
                            return View("MasterDetail", eExpense);
                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "No se puede guardar el viatico, porque el periodo para esa fecha de solicitud esta cerrado.";
                        return View("MasterDetail", eExpense);
                    }
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;

                }
            }
            else
            {
                //$"persona: {persona.EmployeeNumber}\n" +
                //  $"correo: {persona.correo}\n" +
                //  $"nombre: {persona.FirstName}\n" +
                //  $"apeliido: {persona.LastNames}\n" +
                //  $"gerenca: {persona.GERENCIA}\n" +
              ViewData["EditError"] = "Favor verificar los datos introducidos...";
            }

            Session.Remove("sExpenseDetail");
            return RegisterDetail((long)Session[keyPerson]);
        }
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveExpensejson(Entities.Expenses eExpense)
        {
            Entities.Expenses eExpenseBeforeUpdate = new Entities.Expenses();
            Validations.Expenses vExpenses = new Validations.Expenses();

            if (ModelState.IsValid)
            {
                try
                {
                    bool resultPeriodsClosed = vExpenses.ValidatePeriod(eExpense);
                    if (resultPeriodsClosed)
                    {
                        var expenseDetail = Expenses.GetAllEditableExpenseDetail();
                        if (expenseDetail.Count > 0)
                        {
                            string resultValidateBudget = vExpenses.ValidateBudget(eExpense);
                            if (string.IsNullOrEmpty(resultValidateBudget))
                            {
                                bool resultDuplicateDates2 = vExpenses.ValidateDuplicateDates2(eExpense);

                                //bool resultDuplicateDates = vExpenses.validarduplicadoviatico(eExpense);
                                if (resultDuplicateDates2 == true)
                                {
                                    bool resultPendingYields = vExpenses.ValidatePendingYields(eExpense);
                                if (resultPendingYields)
                                {
                                    bool resultDuplicateConcepts = vExpenses.ValidateDuplicateConceptsbyCategory(eExpense);
                                    if (resultDuplicateConcepts)
                                    {
                                        bool resultDuplicateByHours = vExpenses.ValidateDuplicateConceptsbyHours(eExpense);
                                            if (resultDuplicateByHours)
                                        {
                                            if (Session["sExpense"] != null)
                                            {
                                                eExpenseBeforeUpdate = (Entities.Expenses)Session["sExpense"];
                                                eExpense.ExpenseId = eExpenseBeforeUpdate.ExpenseId;
                                            }
                                            if (eExpense.ExpenseId > 0) // es una actualización
                                            {
                                                Expenses.SaveExpense(eExpense);
                                            }
                                            else
                                            {
                                                long personId = (long)Session[keyPerson];
                                                    Entities.Employees Employee = new Entities.Employees();
                                                    Employee = (Entities.Employees)Session["empleado"];
                                                    eExpense.carnet = Employee.EmployeeNumber;
                                                    eExpense.PersonId = personId;
                                                Expenses.SaveExpense(eExpense); // es una inserción
                                            }

                                            return Json(new { success = true, message = "El registro ha sido guardado exitosamente." });
                                        }
                                        else
                                        {
                                            return Json(new { success = false, message = "No se pueden guardar categorías con la misma hora." });
                                        }
                                    }
                                    else
                                    {
                                        return Json(new { success = false, message = "No se pueden guardar conceptos tipo cena, desayuno, almuerzo dos veces." });
                                    }
                                }
                                else
                                {
                                    return Json(new { success = false, message = "No se puede guardar el viático porque tiene viáticos planificados pendientes del periodo anterior por rendir." });
                                    }
                                } //else
                                {
                                    return Json(new { success = false, message = "No se pueden guardar fechas duplicadas." });


                                }
                            }
                            else
                            {
                                return Json(new { success = false, message = resultValidateBudget });
                            }
                        }
                        else
                        {
                            return Json(new { success = false, message = "No se puede guardar el viático porque no lleva detalle." });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "No se puede guardar el viático porque el periodo para esa fecha de solicitud está cerrado." });
                    }
                }
                catch (Exception e)
                {
                    return Json(new { success = false, message = e.Message });
                }
            }
            else
            {
                return Json(new { success = false, message = "Favor verificar los datos introducidos." });
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveExpensejson1(Entities.Expenses eExpense)
        {
            Entities.Expenses eExpenseBeforeUpdate = new Entities.Expenses();
            Validations.Expenses vExpenses = new Validations.Expenses();
            eExpenseBeforeUpdate = (Entities.Expenses)Session["sExpense"];
            eExpense  = eExpenseBeforeUpdate ;
            try
                {
                    bool resultPeriodsClosed = vExpenses.ValidatePeriod(eExpense);
                    if (resultPeriodsClosed)
                    {
                        var expenseDetail = Expenses.GetAllEditableExpenseDetail();
                        if (expenseDetail.Count > 0)
                        {
                            string resultValidateBudget = vExpenses.ValidateBudget(eExpense);
                            if (string.IsNullOrEmpty(resultValidateBudget))
                        {
                          
                                bool resultPendingYields = vExpenses.ValidatePendingYields(eExpense);
                                if (resultPendingYields)
                                {
                                    bool resultDuplicateConcepts = vExpenses.ValidateDuplicateConceptsbyCategory(eExpense);
                                    if (resultDuplicateConcepts)
                                    {
                                        bool resultDuplicateByHours = vExpenses.ValidateDuplicateConceptsbyHours(eExpense);
                                        if (resultDuplicateByHours)
                                        {
                                            if (Session["sExpense"] != null)
                                            {
                                                eExpenseBeforeUpdate = (Entities.Expenses)Session["sExpense"];
                                                eExpense.ExpenseId = eExpenseBeforeUpdate.ExpenseId;
                                            }
                                            if (eExpense.ExpenseId > 0) // es una actualización
                                            {
                                                Expenses.SaveExpense(eExpense);
                                            }
                                            else
                                            {
                                                long personId = (long)Session[keyPerson];
                                                eExpense.PersonId = personId;
                                                Expenses.SaveExpense(eExpense); // es una inserción
                                            }

                                            return Json(new { success = true, message = "El registro ha sido guardado exitosamente." });
                                        }
                                        else
                                        {
                                            return Json(new { success = false, message = "No se pueden guardar categorías con la misma hora." });
                                        }
                                    }
                                    else
                                    {
                                        return Json(new { success = false, message = "No se pueden guardar conceptos tipo cena, desayuno, almuerzo dos veces." });
                                    }
                                }
                                else
                                {
                                    return Json(new { success = false, message = "No se puede guardar el viático porque tiene viáticos planificados pendientes del periodo anterior por rendir." });
                                }
                          
                        }
                        else
                            {
                                return Json(new { success = false, message = resultValidateBudget });
                            }
                        }
                        else
                        {
                            return Json(new { success = false, message = "No se puede guardar el viático porque no lleva detalle." });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "No se puede guardar el viático porque el periodo para esa fecha de solicitud está cerrado." });
                    }
                }
                catch (Exception e)
                {
                    return Json(new { success = false, message = e.Message });
                }
            
        }

        [HttpGet]
        public async Task<JsonResult> GetExpenseDetails(int personId)
        {
           
            try
            {
                var client = new RestClient("http://172.26.54.66/apihcm/api/viatico/GetExpenseDetailViewByPersonId?personId=" + personId);
                var request = new RestRequest(Method.GET);
                request.Timeout = -1;


                var resultExpensesx = client.Execute(request);
                //Console.WriteLine(response.Content);
                //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                if (resultExpensesx != null)
                {
                    return Json(resultExpensesx.Content, JsonRequestBehavior.AllowGet);
                   
                }
               
            }
            catch (Exception e)
            {


            }
            return Json("", JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetViaticosSinRendir()
        {
            try
            {     string UrlApi = "http://172.26.54.66/apihcm/api/viatico/rendirviaticopendiente";

                  var client = new RestClient(UrlApi);
                var request = new RestRequest(Method.GET) { Timeout = -1 }; // timeout sin límite
                var resp = client.Execute(request);

                // si responde, deserializa a lista fuerte y retorna JSON
                if (resp != null && !string.IsNullOrWhiteSpace(resp.Content))
                {
                    var data = JsonConvert.DeserializeObject<List<Entities.ViaticoSinRendir>>(resp.Content);
                    return Json(data, JsonRequestBehavior.AllowGet);
                }
            }
            catch { /* log opcional */ }

            return Json(new List<Entities.ViaticoSinRendir>(), JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetViaticosSinRendirPorCarnet( )
        {

            Entities.Employees Employee = new Entities.Employees();
            Employee = (Entities.Employees)Session["empleado"];
             //  Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);
            // Se debera cambiar por la api de foto
           
            try
                {
                   
                    string UrlApi = "http://172.26.54.66/apihcm/api/viatico/ObtenerTodosfaltarendircarnet?carnet="+ Employee.EmployeeNumber;
                var client = new RestClient(UrlApi);
                var request = new RestRequest(Method.GET) { Timeout = -1 };
                var resp = client.Execute(request);

                if (resp != null && !string.IsNullOrWhiteSpace(resp.Content))
                {
                    var data = JsonConvert.DeserializeObject<List<Entities.ViaticoSinRendir>>(resp.Content);
                        // filtro en cliente por Carnet
                      

                        // Filtrar solo los que coinciden con el listado
                       

                        return Json(data, JsonRequestBehavior.AllowGet);
                    }
            }
            catch { /* log opcional */ }

            return Json(new List<Entities.ViaticoSinRendir>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetExpenseDetails1()
        {
            var details = Models.Expenses.GetAllEditableExpenseDetailjson();
            //return Json(details, JsonRequestBehavior.AllowGet);
            return Json(new { data = details }, JsonRequestBehavior.AllowGet); 

        }
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeleteDetailLinex(int id)
        {
            var details = Session["sExpenseDetail"] as List<Entities.ExpenseDetail>;
            var detail = details?.FirstOrDefault(d => d.ExpenseDetailId == id);
            if (detail != null)
            {
                details.Remove(detail);
                Session["sExpenseDetail"] = details;
            }
            return Json(new { success = true });
        }
   
        public JsonResult UpdateDetailLinejson(Entities.ExpenseDetail updatedDetail)
        {
            var details = Session["sExpenseDetail"] as List<Entities.ExpenseDetail>;
            if (details != null)
            {
                var detail = details.FirstOrDefault(d => d.ExpenseDetailId == updatedDetail.ExpenseDetailId);
                if (detail != null)
                {
                    // Actualiza el detalle específico
                    detail.ClasificationId = updatedDetail.ClasificationId;
                    detail.CategoryId = updatedDetail.CategoryId;
                    detail.SubCategoryId = updatedDetail.SubCategoryId;
                    detail.DepartmentId = updatedDetail.DepartmentId;
                    detail.ExpenseDetailNotes = updatedDetail.ExpenseDetailNotes;
                    detail.Amount = updatedDetail.Amount;
                    detail.HourStart = updatedDetail.HourStart;
                    detail.HourEnd = updatedDetail.HourEnd;
                    detail.YieldAmount = updatedDetail.YieldAmount;
                    detail.ReturnAmount = updatedDetail.ReturnAmount;

                    // Guarda la lista actualizada en la sesión
                    Session["sExpenseDetail"] = details;
                }
            }
            return Json(new { success = true });
        }
        [HttpGet]
        public JsonResult GetAllSubCategories(int categoryId)
        {
            List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
            try
            {
                var result = Utils.ClaroWCF.GetAllConceptsSetting();
                if (result != null)
                {
                    lstConcepts = (from item in result
                                   where item.CategoryId== categoryId
                                   select new Entities.ViewModels.ConceptsSettingView
                                   {
                                       SubCategoryId = item.SubCategoryId,
                                       SubCategoryName = item.SubCategoryName,
                                       Amount = item.Amount
                                   }).ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return Json(lstConcepts, JsonRequestBehavior.AllowGet);
        }
        

        [HttpPost]
        public JsonResult StoreSubCategoryId(int subCategoryId)
        {
            Session["SelectedSubCategoryId"] = subCategoryId;
            return Json(new { success = true });
        }
        
        #endregion
        #region Autorizaciones Jefe Inmediato



        public ActionResult AuthorizeBoss()
        {
            Session.Remove("sExpenseAuthorizeManager");
            Session.Remove("sExpenseAuthorizeCoordinator");
            Session.Remove("sExpenseAuthorizeRrhh");
             Session.Remove("sExpenseAuthorizeYield");
            Session.Remove("listautorizarboss");
            Session.Remove("sExpenseAuthorizeBoss");

            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            //try
            //{
               

            //    var result = Data.Expense.GetAllExpensesAuthorizeBoss();
            //    if (result != null)
            //    {
            //        lstDetail = result.ToList();
            //        Session["listautorizarboss"] = lstDetail;
            //    }

            //    else
            //    {
            //        lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            //    }



            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("Se ha producido el siguiente error ", ex);
            //}
            return View(lstDetail);
        }
        public ActionResult AuthorizeBossPartial()
        {
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {

                if (Session["listautorizarboss"] != null) {
                    lstDetail = (List<Entities.ViewModels.ExpenseDetailView>)Session["listautorizarboss"];
                }
                else
                {
                    var result = Data.Expense.GetAllExpensesAuthorizeBossx();
                    if (result != null)
                    {
                        lstDetail = result.ToList();
                        Session["listautorizarboss"] = lstDetail;
                    }

                    else
                    {
                        lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeBossPartial", lstDetail);



        }
        public JsonResult ExpensesListJson()
        {
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {

                if (Session["listautorizarboss"] != null)
                {
                    lstDetail = (List<Entities.ViewModels.ExpenseDetailView>)Session["listautorizarboss"];
                }
                else
                {
                    var result = Data.Expense.GetAllExpensesAuthorizeBoss();
                    if (result != null)
                    {
                        lstDetail = result.ToList();
                        Session["listautorizarboss"] = lstDetail;
                    }
                    else
                    {
                        lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

     
            var data2 = lstDetail.Select(x => new ExpenseGridDto
            {
                ExpenseId = x.ExpenseId,
                EmployeeNumber = x.EmployeeNumber,
                FullName = x.FullName,
                ExpenseDate = x.ExpenseDate == default(DateTime) ? (DateTime?)null : x.ExpenseDate,
                ClassName = x.ClassName,
                TotalAmount = x.TotalAmount,
                ExpenseStatus = x.ExpenseStatus,
                VehicleNumber = x.VehicleNumber
            }).ToList().OrderBy(x => x.EmployeeNumber)                           // ← Carnet
.ThenByDescending(x => x.ExpenseDate ?? DateTime.MinValue) // ← Fecha DESC
.ToList(); ;
            return Json(new { data = data2 }, JsonRequestBehavior.AllowGet);

 

        }
        public sealed class ExpenseGridDto
        {
            public int ExpenseId { get; set; }
            public string EmployeeNumber { get; set; }
            public string FullName { get; set; }
            public DateTime? ExpenseDate { get; set; }
            public string ClassName { get; set; }
            public decimal? TotalAmount { get; set; }
            public string ExpenseStatus { get; set; }
            public string VehicleNumber { get; set; }
        }
        public ActionResult AuthorizeBossPartialload()
        {
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {


                var result = Data.Expense.GetAllExpensesAuthorizeBoss();
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeBossPartial", lstDetail);



        }

        public ActionResult RefreshPartial()
        {            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {
                Session.Remove("sExpenseAuthorizeBoss");

                var result = Data.Expense.GetAllExpensesAuthorizeBoss();
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("AuthorizeBossPartial", lstDetail);

        }
        

        /// <summary>
        /// Accion que llama a metodo para autorizacion de jefe inmediato del viatico de un colaborador
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeBoss(string ids)
        {
            string result = string.Empty;

          //  string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (ids != "")
            {
                //while (keysET.Trim().Length > 0)
                //{
                //    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                //    Data.Expense.AuthorizeBoss(int.Parse(keyAuthorize));
                //    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                //}
                Data.Expense.AuthorizeBoss(ids);
            }
            Session["listautorizarboss"] = null;
            RefreshPartial();
            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedBoss(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.Expense.DeniedBoss(int.Parse(keyAuthorize));

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }


     
        #endregion

        #region Autorizacion Coordinador


      
        public ActionResult AuthorizeCoordinator()
        {
            Session.Remove("sExpenseAuthorizeManager");
            Session.Remove("sExpenseAuthorizeBoss");
            Session.Remove("sExpenseAuthorizeRrhh");
            Session.Remove("sExpenseAuthorizeYield");
            Session.Remove("sExpenseAuthorizeCoordinator");


            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {
                Session["Detalleviatico"] = null;

                var result = Data.Expense.GetAllExpensesAuthorizeCoordinator();
                if (result != null)
                {
                    lstDetail = result.ToList();
                    Session["Detalleviatico"] = lstDetail;
                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                }

                return View(lstDetail);

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

        }
        
        /// <summary>
        /// Accion que retorna resultado de la accion ListCoordinatorBindingCore
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeCoordinatorPartial()
        {

            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {
                if (Session["Detalleviatico"] != null)
                {
                    lstDetail = (List<Entities.ViewModels.ExpenseDetailView>)Session["Detalleviatico"];


                }
                else
                {

                    var result = Data.Expense.GetAllExpensesAuthorizeCoordinator();
                    if (result != null)
                    {
                        lstDetail = result.ToList();
                        Session["Detalleviatico"] = lstDetail;
                    }

                    else
                    {
                        lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                    }

                }
                    return PartialView("AuthorizeCoordinatorPartial", lstDetail);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            

        }

  
        /// <summary>
        /// Accion que llama a metodo AuthorizeCoordinator para autorizacion de coordiandor.
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        /// 
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeCoordinator(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
           // if (keysET != ",")
                if (ids!="") 
            {
                //while (keysET.Trim().Length > 0)
                //{
                //    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                //    Data.Expense.AuthorizeCoordinator(int.Parse(keyAuthorize));
                //    keysET = keysET.Substring(keysET.IndexOf(",") + 1);

                //}

                Data.Expense.AuthorizeCoordinator2(ids);
                Session["Detalleviatico"]=null;
                Session.Remove("sExpenseAuthorizeCoordinator");

                //AuthorizeCoordinatorPartial();
                return Json(new { status = "Exito", message = "Exito en la autorización" });
            }
            else { return Json(new { status = "Error", message = "No se a logrado realizar la autorización" }); }
          
        }
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedCoordinator(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.Expense.DeniedCoordinator(int.Parse(keyAuthorize));

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }


        #endregion
        #region Autorizacion Gerencial

        /// <summary>
        /// Accion que retorna la vista AuthorizeManager
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeManager()
        {
            Session.Remove("sExpenseAuthorizeBoss");
            Session.Remove("sExpenseAuthorizeCoordinator");
            Session.Remove("sExpenseAuthorizeRrhh");
            Session.Remove("sExpenseAuthorizeYield");
            Session.Remove("sExpenseAuthorizeManager");
            Session.Remove("Detalleviaticog");
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {

                if (Session["Detalleviaticog"] != null)
                { }
                else
                {
                    var result = Data.Expense.GetAllExpensesAuthorizeManager();
                    if (result != null)
                    {
                        lstDetail = result.ToList();
                        Session["Detalleviaticog"] = lstDetail;
                    }

                    else
                    {
                        lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }
                return View(lstDetail);

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

        }

        /// <summary>
        /// Accion que retorna resultado de la accion ListManagerBindingCore
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeManagerPartial()
        {
            
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {
                if (Session["Detalleviaticog"] != null)
                {
                    lstDetail = (List<Entities.ViewModels.ExpenseDetailView>)Session["Detalleviaticog"];


                }
                else
                {

                    var result = Data.Expense.GetAllExpensesAuthorizeManager();
                    if (result != null)
                    {
                        lstDetail = result.ToList();
                        Session["Detalleviaticog"] = lstDetail;
                    }

                    else
                    {
                        lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }

                return PartialView("AuthorizeManagerPartial", lstDetail);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }

        /// <summary>
        /// Accion que retorna vista parcial AuthorizeManagerPartial
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        //PartialViewResult ListManagerBindingCore(GridViewModel viewModel)
        //{
        //    viewModel.ProcessCustomBinding(Expenses.GetListManagerCountAdvanced,
        //                                   Expenses.GetListManager,
        //                                   Expenses.GetSummaryValuesManager);
        //    return PartialView("AuthorizeManagerPartial", viewModel);
        //}


        ///// <summary>
        ///// Accion para paginacion de registros de autorizaciones gerenciales.
        ///// </summary>
        ///// <param name="pager"></param>
        ///// <returns></returns>
        //public ActionResult GridListManagerPagingAction(GridViewPagerState pager)
        //{
        //    GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelManager);
        //    viewModel.ApplyPagingState(pager);
        //    return ListManagerBindingCore(viewModel);
        //}


        ///// <summary>
        ///// Accion para el filtrado de registros de autorizaciones gerenciales.
        ///// </summary>
        ///// <param name="filter"></param>
        ///// <returns></returns>
        //public ActionResult GridListManagerFilteringAction(GridViewFilteringState filter)
        //{
        //    GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelManager);
        //    viewModel.ApplyFilteringState(filter);
        //    return ListManagerBindingCore(viewModel);
        //}

        ///// <summary>
        ///// Metodo que retorna el viewModel de las autorizaciones gerenciales.
        ///// </summary>
        ///// <returns></returns>
        //private static GridViewModel CreateManagerModelWithSummary()
        //{
        //    GridViewModel viewModel = new GridViewModel();
        //    viewModel.KeyFieldName = "ExpenseId";
        //    viewModel.Columns.Add("EmployeeNumber");
        //    viewModel.Columns.Add("FullName");
        //    viewModel.Columns.Add("ExpenseDate");
        //    viewModel.Columns.Add("TotalAmount");
        //    viewModel.Columns.Add("ExpenseStatus");
        //    viewModel.Columns.Add("VehicleNumber");


        //    viewModel.TotalSummary
        //        .Add(new GridViewSummaryItemState() { FieldName = "TotalAmount", SummaryType = SummaryItemType.Sum });
        //    return viewModel;
        //}


        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeManager (string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (ids != "")
            {
                //while (keysET.Trim().Length > 0)
                //{
                //    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.Expense.AuthorizeManager2(ids);
                //    keysET = keysET.Substring(keysET.IndexOf(",") + 1);

                //}
                Session.Remove("sExpenseAuthorizeManager");
                Session["Detalleviaticog"] = null;
                AuthorizeCoordinatorPartial();
            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedManager(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.Expense.DeniedManager(int.Parse(keyAuthorize));

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }
        
       
        #endregion
        
        #region Autorizacion Rrhh

        /// <summary>
        /// Accion para retornar la vista AuthorizeRrhh 
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeRrhh()
        {
            Session.Remove("sExpenseAuthorizeManager");
            Session.Remove("sExpenseAuthorizeCoordinator");
            Session.Remove("sExpenseAuthorizeBoss");
            Session.Remove("sExpenseAuthorizeYield");
            return View();
        }

        /// <summary>
        /// Accion que retorna el resultado de la accion ListRrhhBindingCore
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeRrhhPartial()
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelRrhh);
            if (viewModel == null)
            {
                viewModel = CreateRrhhModelWithSummary();
            }
            return ListRrhhBindingCore(viewModel);
        }

        /// <summary>
        /// Accion que retorna vista parcial AuthorizeRrhhPartial
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        PartialViewResult ListRrhhBindingCore(GridViewModel viewModel)
        {
            viewModel.ProcessCustomBinding(Expenses.GetListRrhhCountAdvanced,
                                           Expenses.GetListRrhh,
                                           Expenses.GetSummaryValuesRrhh);
            return PartialView("AuthorizeRrhhPartial", viewModel);
        }


        /// <summary>
        /// Accion para paginación de registros de autorizaciones de corrdinador.
        /// </summary>
        /// <param name="pager"></param>
        /// <returns></returns>
        public ActionResult GridListRrhhPagingAction(GridViewPagerState pager)
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelRrhh);
            viewModel.ApplyPagingState(pager);
            return ListRrhhBindingCore(viewModel);
        }


        /// <summary>
        /// Accion de filtrado de registros de autorizaciones de coordiandor
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public ActionResult GridListRrhhFilteringAction(GridViewFilteringState filter)
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelRrhh);
            viewModel.ApplyFilteringState(filter);
            return ListRrhhBindingCore(viewModel);
        }

        /// <summary>
        /// Metodo para crear el viewModel 
        /// </summary>
        /// <returns></returns>
        private static GridViewModel CreateRrhhModelWithSummary()
        {
            GridViewModel viewModel = new GridViewModel();
            viewModel.KeyFieldName = "ExpenseId";
            viewModel.Columns.Add("EmployeeNumber");
            viewModel.Columns.Add("FullName");
            viewModel.Columns.Add("ExpenseDate");
            viewModel.Columns.Add("TotalAmount");
            viewModel.Columns.Add("ExpenseStatus");
            viewModel.Columns.Add("VehicleNumber");


            viewModel.TotalSummary
                .Add(new GridViewSummaryItemState() { FieldName = "TotalAmount", SummaryType = SummaryItemType.Sum });
            return viewModel;
        }

        /// <summary>
        /// Accion que llama a metodo AuthorizeRrhh para autorizacion de RRHH
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AuthorizeRrhh(string selectedIdsAP)
        {
            string keysET = selectedIdsAP + ",";

            try
            {
                //Validar que no venga vacia Keyset
                if (keysET != ",")
                {
                    while (keysET.Trim().Length > 0)
                    {
                        string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                        Expenses.AuthorizeRrhh(int.Parse(keyAuthorize));
                        keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error al autorizar el registro", e);
            }

            return View("AuthorizeRrhh");
        }

        /// <summary>
        /// Accion que llama a metodo DeniedRrhh para denegacion de RRHH.
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeniedRrhh(string selectedIdsDN)
        {
            string keysET = selectedIdsDN + ",";
            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Expenses.DeniedRrhh(int.Parse(keyAuthorize));

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }
            }
            return View("AuthorizeRrhh");
        }
        #endregion
        #region Autorizacion Yield

        /// <summary>
        /// Accion que retorna vista AuthorizeYield
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeYield()
        {
            Session.Remove("sExpenseAuthorizeManager");
            Session.Remove("sExpenseAuthorizeCoordinator");
            Session.Remove("sExpenseAuthorizeBoss");
            Session.Remove("sExpenseAuthorizeRrhh");
            return View();
        }

        /// <summary>
        /// Accion que retorna resultado de la accion ListYieldBindingCore
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeYieldPartial()
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelYield);
            if (viewModel == null)
            {
                viewModel = CreateYieldModelWithSummary();
            }
            return ListYieldBindingCore(viewModel);
        }

        /// <summary>
        /// Accion que retorna vista parcial AuthorizeYieldPartial
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        PartialViewResult ListYieldBindingCore(GridViewModel viewModel)
        {
            viewModel.ProcessCustomBinding(Expenses.GetListYieldCountAdvanced,
                                           Expenses.GetListYield,
                                           Expenses.GetSummaryValuesYield);
            return PartialView("AuthorizeYieldPartial", viewModel);
        }


        /// <summary>
        /// Accion de paginacion para los registros de las autorizaciones de rendiciones.
        /// </summary>
        /// <param name="pager"></param>
        /// <returns></returns>
        public ActionResult GridListYieldPagingAction(GridViewPagerState pager)
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelYield);
            viewModel.ApplyPagingState(pager);
            return ListYieldBindingCore(viewModel);
        }


        /// <summary>
        /// Accion de filtrado para los registros de las autorizaciones de rendicion.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public ActionResult GridListYieldFilteringAction(GridViewFilteringState filter)
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelYield);
            viewModel.ApplyFilteringState(filter);
            return ListYieldBindingCore(viewModel);
        }

        /// <summary>
        /// Metodo que retorna el viewModel de las autorizaciones de rendiciones.
        /// </summary>
        /// <returns></returns>
        private static GridViewModel CreateYieldModelWithSummary()
        {
            GridViewModel viewModel = new GridViewModel();
            viewModel.KeyFieldName = "ExpenseId";
            viewModel.Columns.Add("EmployeeNumber");
            viewModel.Columns.Add("FullName");
            viewModel.Columns.Add("ExpenseDate");
            viewModel.Columns.Add("TotalAmount");
            viewModel.Columns.Add("ExpenseStatus");
            viewModel.Columns.Add("VehicleNumber");


            viewModel.TotalSummary
                .Add(new GridViewSummaryItemState() { FieldName = "TotalAmount", SummaryType = SummaryItemType.Sum });
            return viewModel;
        }

        /// <summary>
        /// Accion que llama a metodo AuthorizeYield para autorizaciones de rendiciones.
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AuthorizeYield(string selectedIdsAP)
        {
            string keysET = selectedIdsAP + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Expenses.AuthorizeYield(int.Parse(keyAuthorize));
                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }
            }
            return View("AuthorizeYield");
        }

        /// <summary>
        /// Accion que llama a metodo DeniedYield para denegar autorizaciones de rendiciones.
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeniedYield(string selectedIdsDN)
        {
            string keysET = selectedIdsDN + ",";
            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Expenses.DeniedYield(int.Parse(keyAuthorize));

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }
            }
            return View("AuthorizeYield");
        }
        #endregion
        
        #region Rendicion de Viaticos
        /// <summary>
        /// Accion que retorna la vista YieldExpense, interfaz para subir rendicion o minuta de un viatico
        /// </summary>
        /// <param name="expenseId"></param>
        /// <returns></returns>
        public ActionResult YieldExpense(int expenseId)//aqui hay que corregir no va con la arquitectura MVC
        {
            Entities.Expenses expense = new Entities.Expenses();
            //Validations.Expenses vExpenses = new Validations.Expenses();
            //string resultYieldAuthorization = vExpenses.ValidatYieldeAuthorization(expenseId);
            //bool resultYieldBeforeDate = vExpenses.ValidateYieldBeforeDate(expenseId);
            try
            {
               // if (resultYieldAuthorization == "  ")
                //{
                  //  if (resultYieldBeforeDate == true)
                    //{
                        expense = Data.Expense.GetExpenseById(expenseId); //Utils.ClaroWCF.GetAllExpenses(expenseId).FirstOrDefault();
                        Session["sExpense"] = expense;
                        
                    //}
                    //else
                    //{
                      //  ViewData["EditError"] = "No se puede rendir un viático si la fecha actual es menor que la fecha de ejecución";
                    //}
                //}
                //else
                //{
                 //   ViewData["EditError"] = resultYieldAuthorization;
               // }
            }
            catch (Exception)
            {
                throw;
            }

            return View("YieldExpense", expense);
            //return RegisterDetail((int)Session[keyPerson]);
        }

        /// <summary>
        /// Accion para mandar a mostrar la vista parcial YieldDetailPartial para la rendicion.
        /// </summary>
        /// <param name="expense"></param>
        /// <returns></returns>

        public ActionResult YieldDetailPartial(Entities.Expenses expense)
        {
            Entities.Expenses eExpense = new Entities.Expenses();
            eExpense = (Entities.Expenses)Session["sExpense"];

            List<Entities.ExpenseDetail> lstExpenseDetail = new List<Entities.ExpenseDetail>();

            if (eExpense != null)
            {
                lstExpenseDetail = Expenses.GetAllExpenseDetailById(eExpense.ExpenseId);
                    //.Where(e => e.ExpenseId == eExpense.ExpenseId)
                    //.ToList();
            }
            Session["lstExpenseDetailrendir"] = lstExpenseDetail;

            return PartialView("YieldDetailPartial", lstExpenseDetail);
        }


        /// <summary>
        /// Accion que llama al metodo UpdateYieldAmount del modelo Expenses para actualizar el monto a rendir 
        /// en el detalle del viatico.
        /// </summary>
        /// <param name="eExpenseDetail"></param>
        /// <returns></returns>
        /// [HttpPost]
        public ActionResult UpdateYieldAmount2(int ExpenseDetailId, decimal YieldAmount)
        {
            //var expenseDetail = dbContext.ExpenseDetails.Find(ExpenseDetailId);
            //if (expenseDetail != null)
            //{
            //    expenseDetail.YieldAmount = YieldAmount;
            //    dbContext.SaveChanges();
            //}
            return Json(new { success = true });
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult UpdateYieldAmount(Entities.ExpenseDetail eExpenseDetail) // esto se debe de modificar debe de ser hasta que le de en el botono rendir en un ciclo.
        {

            List<Entities.ExpenseDetail> lstExpenseDetail = new List<Entities.ExpenseDetail>();
            lstExpenseDetail=
            (List<Entities.ExpenseDetail>)Session["lstExpenseDetailrendir"]  ;
            var qr = lstExpenseDetail.Where(x => x.ExpenseDetailId == eExpenseDetail.ExpenseDetailId).FirstOrDefault();
            if (qr!=null && qr.Amount>0)
            {
                eExpenseDetail.Amount = qr.Amount;
            }
            Entities.Expenses eExpense = new Entities.Expenses();
            if (Session["sExpense"] != null)
            {
                eExpense = (Entities.Expenses)Session["sExpense"];
            }
            Validations.Expenses vExpenses = new Validations.Expenses();
            bool resultValidateQuantity = vExpenses.ValidateYieldAmount(eExpenseDetail);
            if (ModelState.IsValid)
            {
                try
                {
                    if (resultValidateQuantity == true)
                    {
                        if (eExpense.ClassId == 16)
                        {
                            eExpenseDetail.ReturnAmount = eExpenseDetail.Amount - eExpenseDetail.YieldAmount;
                        }
                        else
                        {
                            eExpenseDetail.YieldAmount = eExpenseDetail.Amount;
                            eExpenseDetail.ReturnAmount = eExpenseDetail.Amount - eExpenseDetail.YieldAmount;
                        }

                        Expenses.UpdateYieldAmount(eExpenseDetail);
                    }
                    else
                    {
                        ViewData["EditError"] = "Aviso: El viático no se puede rendir porque el monto a rendir no es un valor válido o es mayor que el monto solicitado.";
                    }
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Ha ocurrido un error en la transaccion, favor verificar los datos.";
            }

            eExpense = (Entities.Expenses)Session["sExpense"];
            return YieldDetailPartial(eExpense); //View ("YieldExpense",eExpense);
        }

        /// <summary>
        /// Accion que llama a los metodos UpdateDepositFile,UpdateYieldFile,UpdateReturnAmount
        /// ChangeStateExpense del modelo Expenses para actualizar el archivo adjunto, actualizar montos del detalle 
        /// y cambiar el estado del viatico.
        /// </summary>
        /// <param name="eExpense"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ChangeStateYield(Entities.Expenses eExpense)
        {
            Validations.Expenses vExpenses = new Validations.Expenses();
            Entities.Expenses eExpenseBefore = new Entities.Expenses();
            if (Session["sExpense"] != null)
            {
                eExpenseBefore = (Entities.Expenses)Session["sExpense"];
                eExpense.ExpenseId = eExpenseBefore.ExpenseId;
            }
            bool resultYieldFile = vExpenses.ValidateYieldFile(eExpense);
            bool resultDepositFile = vExpenses.ValidateDepositFile(eExpense);
            //if (ModelState.IsValid)
            //{
                string cadena = null;
                string depositSession = null;
                try
                {
                    if (resultYieldFile == true)
                    {
                        if (resultDepositFile == true)
                        {
                            if (eExpense.ClassId == 16)
                            {
                                if (Session["UploadedFileBytes"] != null)
                                {
                                    //Actualizando el valor de YieldFile en la base de datos
                                    eExpense.YieldFile = (byte[])Session["UploadedFileBytes"]; //UploadedFileName
                                    cadena = (string)Session["UploadedFileName"];
                                    string extension = cadena.Split('.').Last();
                                    eExpense.YieldFileExtension = extension;

                                    //Expenses.UpdateYieldFile(eExpense);
                                }
                                if (Session["UploadedDepositBytes"] != null)
                                {
                                    //Actualizando el valor de DepositFile en la base de datos
                                    eExpense.DepositFile = (byte[])Session["UploadedDepositBytes"];
                                    depositSession = (string)Session["UploadedDepositName"];
                                    string depositExtension = depositSession.Split('.').Last();
                                    eExpense.DepositFileExtension = depositExtension;

                                    //Expenses.UpdateDepositFile(eExpense);
                                }

                             
                                string result = Data.Expense.ChangeStateExpense(eExpense.ExpenseId);
                                if (result.Contains("Exito") )
                                {
                                string resultYield = Data.Expense.UpdateYield(eExpense);
                                }
                                else
                            {
                                return Content(result);


                            }
                            // Expenses.ChangeStateExpense(eExpense.ExpenseId);
                        }
                           //aqui actualizo la rendicion cuando es reembolso
                            else
                            {
                                if (Session["UploadedFileBytes"] != null)
                                {
                                    //Actualizando el valor de YieldFile en la base de datos
                                    eExpense.YieldFile = (byte[])Session["UploadedFileBytes"];
                                    cadena = (string)Session["UploadedFileName"];
                                    string extension = cadena.Split('.').Last();
                                    eExpense.YieldFileExtension = extension;
                                    //Expenses.UpdateYieldFile(eExpense);
                                }

                                if (Session["UploadedDepositBytes"] != null)
                                {
                                    //Actualizando el valor de DepositFile en la base de datos
                                    eExpense.DepositFile = (byte[])Session["UploadedDepositBytes"];
                                    depositSession = (string)Session["UploadedDepositName"];
                                    string depositExtension = depositSession.Split('.').Last();
                                    eExpense.DepositFileExtension = depositExtension;
                                    //Expenses.UpdateDepositFile(eExpense);
                                }
                                string resultYield = Data.Expense.UpdateYield(eExpense);
                                if (resultYield != "Exito al actualizar la rendición")
                                {
                                    return Content(resultYield);
                                }
                            }
                        }
                        else
                        {
                            Session.Remove("UploadedDepositBytes");
                            Session.Remove("UploadedFileBytes");
                            Session.Remove("UploadedFileName");
                            Session.Remove("UploadedDepositName");
                            ViewData["EditError"] = "Hay una linea de detalle con monto a rendir cero, debe subir la minuta de deposito.";
                            return View("YieldExpense", eExpense);
                        }
                    }
                    else
                    {
                        Session.Remove("UploadedDepositBytes");
                        Session.Remove("UploadedFileBytes");
                        Session.Remove("UploadedFileName");
                        Session.Remove("UploadedDepositName");
                        ViewData["EditError"] = "Hay una linea de detalle con monto a rendir mayor a cero, debe subir el formato de rendicion.";
                        return View("YieldExpense", eExpense);
                    }
                }
                catch (Exception e)
                {
                    Session.Remove("UploadedDepositBytes");
                    Session.Remove("UploadedFileBytes");
                    Session.Remove("UploadedFileName");
                    Session.Remove("UploadedDepositName");
                    ViewData["EditError"] = e.Message;
                    return View("YieldExpense", eExpense);
                }
            //}
            //else
            //{
            //    Session.Remove("UploadedDepositBytes");
            //    Session.Remove("UploadedFileBytes");
            //    Session.Remove("UploadedFileName");
            //    Session.Remove("UploadedDepositName");
            //    ViewData["EditError"] = "Ha ocurrido un error en la transaccion, favor verificar los datos.";
            //    return View("YieldExpense", eExpense);
            //}

            Session.Remove("UploadedDepositBytes");
            Session.Remove("UploadedFileBytes");
            Session.Remove("UploadedFileName");
            Session.Remove("UploadedDepositName");

            return RegisterDetail((long)Session[keyPerson]);
        }
        
        
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult ChangeStateYieldjson(Entities.Expenses eExpense)
        {
            Validations.Expenses vExpenses = new Validations.Expenses();
            Entities.Expenses eExpenseBefore = new Entities.Expenses();
            if (Session["sExpense"] != null)
            {
                eExpenseBefore = (Entities.Expenses)Session["sExpense"];
                eExpense= eExpenseBefore;
            }

            bool resultYieldFile = vExpenses.ValidateYieldFile(eExpense);
            bool resultDepositFile = vExpenses.ValidateDepositFile(eExpense);
            string depositSession = null;
            string cadena = null;

            try
            {
                if (resultYieldFile)
                {
                    if (resultDepositFile)
                    {
                        // Procesa archivos y otros lógicos
                        // Llamar a los métodos de actualización de la base de datos como en tu lógica original
                        if (eExpense.ClassId == 16)
                        {
                            if (Session["UploadedFileBytes"] != null)
                            {
                                //Actualizando el valor de YieldFile en la base de datos
                                eExpense.YieldFile = (byte[])Session["UploadedFileBytes"]; //UploadedFileName
                                cadena = (string)Session["UploadedFileName"];
                                string extension = cadena.Split('.').Last();
                                eExpense.YieldFileExtension = extension;

                                //Expenses.UpdateYieldFile(eExpense);
                            }
                            if (Session["UploadedDepositBytes"] != null)
                            {
                                //Actualizando el valor de DepositFile en la base de datos
                                eExpense.DepositFile = (byte[])Session["UploadedDepositBytes"];
                                depositSession = (string)Session["UploadedDepositName"];
                                string depositExtension = depositSession.Split('.').Last();
                                eExpense.DepositFileExtension = depositExtension;

                                //Expenses.UpdateDepositFile(eExpense);
                            }

                            //falta prueba
                            var detail = Utils.ClaroWCF.GetAllExpensesDetailById(eExpense.ExpenseId);

                            if (detail != null)
                            {
                                foreach (var item in detail)
                                {
                                    Entities.ExpenseDetail newDetail = new Entities.ExpenseDetail();

                                    newDetail.ExpenseDetailId = item.ExpenseDetailId;
                                    newDetail.ReturnAmount = item.TotalAmount - item.TotalYieldAmount;

                                    //aqui voy a llamar al procedimiento a crear UpdateYieldsAmount
                                    Expenses.UpdateReturnAmount(newDetail);
                                }
                            }
                            //Llamar al metodo InsertEvent
                            string resultx = Data.Expense.ChangeStateExpense(eExpense.ExpenseId);
                            if (resultx.Contains("Exito") )
                            {
                               
                                  string resultYield = Data.Expense.UpdateYield(eExpense);
                             }
                            else
                            {
                                return Json(new { success = false, message = resultx });
                              
                            }
                            // Expenses.ChangeStateExpense(eExpense.ExpenseId);
                        }
                        //aqui actualizo la rendicion cuando es reembolso
                        else
                        {
                            if (Session["UploadedFileBytes"] != null)
                            {
                                //Actualizando el valor de YieldFile en la base de datos
                                eExpense.YieldFile = (byte[])Session["UploadedFileBytes"];
                                cadena = (string)Session["UploadedFileName"];
                                string extension = cadena.Split('.').Last();
                                eExpense.YieldFileExtension = extension;
                                //Expenses.UpdateYieldFile(eExpense);
                            }

                            if (Session["UploadedDepositBytes"] != null)
                            {
                                //Actualizando el valor de DepositFile en la base de datos
                                eExpense.DepositFile = (byte[])Session["UploadedDepositBytes"];
                                depositSession = (string)Session["UploadedDepositName"];
                                string depositExtension = depositSession.Split('.').Last();
                                eExpense.DepositFileExtension = depositExtension;
                                //Expenses.UpdateDepositFile(eExpense);
                            }
                            eExpense.YieldNotes = "RENDIDO";
                            string resultYield = Data.Expense.UpdateYield(eExpense);
                            if (resultYield != "Exito al actualizar la rendición")
                            {
                                return Json(new { success = false, message = resultYield });

                             }
                        }
                       
                        //string result = Data.Expense.ChangeStateExpense(eExpense.ExpenseId);
                        //if (result != "Exito al cambiar estado")
                        //{
                        //    Session.Remove("UploadedDepositBytes");
                        //    Session.Remove("UploadedFileBytes");
                        //    Session.Remove("UploadedFileName");
                        //    Session.Remove("UploadedDepositName");
                        //    return Json(new { success = false, message = result });
                        //}
                        //else
                        //{
                        //    string resultYield = Data.Expense.UpdateYield(eExpense);
                        //    if (resultYield != "Exito al actualizar la rendición")
                        //    {
                        //        Session.Remove("UploadedDepositBytes");
                        //        Session.Remove("UploadedFileBytes");
                        //        Session.Remove("UploadedFileName");
                        //        Session.Remove("UploadedDepositName");
                        //        return Json(new { success = false, message = resultYield });
                        //    }
                        //}
                    }
                    else
                    {
                        Session.Remove("UploadedDepositBytes");
                        Session.Remove("UploadedFileBytes");
                        Session.Remove("UploadedFileName");
                        Session.Remove("UploadedDepositName");
                        // Manejando el caso de error
                        return Json(new { success = false, message = "Hay una linea de detalle con monto a rendir cero, debe subir la minuta de deposito." });
                    }
                }
                else
                {
                    Session.Remove("UploadedDepositBytes");
                    Session.Remove("UploadedFileBytes");
                    Session.Remove("UploadedFileName");
                    Session.Remove("UploadedDepositName");
                    return Json(new { success = false, message = "Hay una linea de detalle con monto a rendir mayor a cero, debe subir el formato de rendicion." });
                }
            }
            catch (Exception e)
            {
                Session.Remove("UploadedDepositBytes");
                Session.Remove("UploadedFileBytes");
                Session.Remove("UploadedFileName");
                Session.Remove("UploadedDepositName");
                return Json(new { success = false, message = "Ocurrió un error: " + e.Message });
            }
            Session.Remove("UploadedDepositBytes");
            Session.Remove("UploadedFileBytes");
            Session.Remove("UploadedFileName");
            Session.Remove("UploadedDepositName");
            return Json(new { success = true, message = "exito subir la rendicion." });

        }

        /// <summary>
        /// Accion que retorna la vista parcial YieldFilePartial en caso de que el adjunto sea pdf o la vista parcial 
        /// ImageYieldFilePartial en caso de que el adjunto sea una imagen.
        /// </summary>
        /// <param name="expenseId"></param>
        /// <returns></returns>
        public ActionResult LoadYield(int expenseId)
        {
            try
            {
                Entities.Expenses eExpense = new Entities.Expenses();
                eExpense = Utils.ClaroWCF.GetAllExpenses(expenseId).FirstOrDefault();
                if (eExpense.YieldFileExtension == "pdf")
                {
                    byte[] byteArray = Utils.ClaroWCF.GetAllExpenseValidateYieldView(expenseId).FirstOrDefault()
                        .YieldFile;
                    if (byteArray == null)
                    {
                        return null;
                    }

                    var strBase64 = Convert.ToBase64String(byteArray);
                    Entities.Expenses Expense = new Entities.Expenses();
                    Expense.ExpenseNotes = string.Format("data:application/pdf;base64,{0}", strBase64);
                    return PartialView("YieldFilePartial", Expense);
                }
                else
                {
                    Entities.Expenses Expense = new Entities.Expenses();
                    Expense.YieldFile = Utils.ClaroWCF.GetYieldFile(expenseId);
                    if (Expense.YieldFile == null)
                    {
                        return null;
                    }

                    return PartialView("ImageYieldFilePartial", Expense);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error al cargar el objeto", e);
            }
        }
        #endregion
        #region Subir Rendición

        /// <summary>
        /// Accion para subir el archivo de rendicion que llama a los metodos YieldFileUploadValidationSettings y  YieldFileUploadComplete
        /// </summary>
        /// <returns></returns>
        public ActionResult YieldFileUpload()
        {
            UploadControlExtension.GetUploadedFiles("ucYieldFile", 
               slnRhonline.Data.Expense.YieldFileUploadValidationSettings,
               slnRhonline.Data.Expense.YieldFileUploadComplete);
            return null;
        }
        /// <summary>
        /// Accion para subir la minuta de deposito que llama a los metodos DepositFileUploadValidationSettings y  DepositFileUploadComplete
        /// </summary>
        /// <returns></returns>
        public ActionResult DepositFileUpload()
        {
            UploadControlExtension.GetUploadedFiles("ucDepositFile",
               slnRhonline.Data.Expense.DepositFileUploadValidationSettings,
               slnRhonline.Data.Expense.DepositFileUploadComplete);
            return null;
        }

       

        #endregion
        #region Consulta de viaticos

        /// <summary>
        /// Accion que retorna la vista ParametersConsult.
        /// </summary>
        /// <returns></returns>
        public ViewResult ParametersConsult()
        {
            Session.Remove("sExpenseAuthorizeConsult");
            Session.Remove("sParameter");

            Session["MenuArea"] = slnRhonline.Models.ExtraTime.GetAreas();


       

            return View();
        }
 
        /// <summary>
        /// Accion que retorna el resultado de la accion Consult en caso de que el tipo de vista sea Consolidado, en
        /// caso de que el modelo no sea valido retorna la vista ParametersConsult.
        /// </summary>
        /// <param name="eParameter"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ExpensesConsult(Parameters eParameter)
        {
            //Validations.ExtraTime vExtraTime = new Validations.ExtraTime();
            //bool resUserActive = vExtraTime.ValidateUserActive();

            Session["sParameter"] = eParameter;

            if (ModelState.IsValid)
            {
                try
                {
                    //if (resUserActive == true)
                    //{
                    if (eParameter.ViewType == "CONSOLIDADO")
                    {
                        return RedirectToAction("Consult");
                    }
                    //}
                    //else
                    //{
                    //    ViewData["EditError"] = "Usuario sin autorizacion de visualizar la consulta";
                    //}
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
            return View("ParametersConsult");
        }


        /// <summary>
        /// Accion que retorna la vista Consult
        /// </summary>
        /// <returns></returns>
        public ActionResult Consult()
        {
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();


 
            
            return View( );
        }
        public JsonResult Consultjson()
        { 
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();

            Session["Consultjson"] = null;

            var result = Data.Expense.GetAllExpensesAuthorizeConsult();
            if (result != null)
            {
                lstDetail = result.ToList();

            }

            else
            {
                lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            }

            Session["Consultjson"] = lstDetail;

            return Json(new { data = lstDetail }, JsonRequestBehavior.AllowGet);


        }
        public JsonResult Consultjson2()
        {
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();


            if (Session["Consultjson"] == null)
            {

                lstDetail= (List<Entities.ViewModels.ExpenseDetailView>)Session["Consultjson"]   ;
            }
            else { 
                var result = Data.Expense.GetAllExpensesAuthorizeConsult();
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                }
            }

            return Json(new { data = lstDetail }, JsonRequestBehavior.AllowGet);


        }
        /// <summary>
        /// Accion que retorna resultado de la accion ListConsultBindingCore
        /// </summary>
        /// <returns></returns>
        public ActionResult ConsultPartial()
        {
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {


                var result = Data.Expense.GetAllExpensesAuthorizeConsult();
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
                }


                return PartialView("ConsultPartial", lstDetail);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            //GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelConsult);
            //if (viewModel == null)
            //{
            //    viewModel = CreateConsultModelWithSummary();
            //}
            //return ListConsultBindingCore(viewModel);
        }

        /// <summary>
        /// Accion que retorna la vista parcial ConsultPartial.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        //PartialViewResult ListConsultBindingCore(GridViewModel viewModel)
        //{
        //    viewModel.ProcessCustomBinding(Expenses.GetListConsultCountAdvanced,
        //                                   Expenses.GetListConsult,
        //                                   Expenses.GetSummaryValues);
        //    return PartialView("ConsultPartial", viewModel);
        //}


        /// <summary>
        /// Accion para la paginacion del vieModel de la consulta de viaticos.
        /// </summary>
        /// <param name="pager"></param>
        /// <returns></returns>
        //public ActionResult GridListConsultPagingAction(GridViewPagerState pager)
        //{
        //    GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelConsult);
        //    viewModel.ApplyPagingState(pager);
        //    return ListConsultBindingCore(viewModel);
        //}


        /// <summary>
        /// Accion para el filtrado de registros del viewModel de la consulta de viaticos.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        //public ActionResult GridListConsultFilteringAction(GridViewFilteringState filter)
        //{
        //    GridViewModel viewModel = GridViewExtension.GetViewModel(keyModelConsult);
        //    viewModel.ApplyFilteringState(filter);
        //    return ListConsultBindingCore(viewModel);
        //}

        /// <summary>
        /// Metoddo que retorna el viewModel de la consulta de viaticos.
        /// </summary>
        /// <returns></returns>
        private static GridViewModel CreateConsultModelWithSummary()
        {
            GridViewModel viewModel = new GridViewModel();
            viewModel.KeyFieldName = "ExpenseId";
            viewModel.Columns.Add("ClassName");
            viewModel.Columns.Add("EmployeeNumber");
            viewModel.Columns.Add("FullName");
            viewModel.Columns.Add("ExpenseDate");
            viewModel.Columns.Add("TotalAmount");
            viewModel.Columns.Add("ExpenseStatus");


            viewModel.TotalSummary
                .Add(new GridViewSummaryItemState() { FieldName = "TotalAmount", SummaryType = SummaryItemType.Sum });
            viewModel.Pager.PageSize = 10;
            return viewModel;
        }


        /// <summary>
        /// Accion para exportar a excel la consulta de viaticos.
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportConsultToXls()// no va con la arquitectura MVC
        {
            List<Entities.ViewModels.ExpenseDetailView> lstReport = new List<Entities.ViewModels.ExpenseDetailView>();
            Parameters eParameter = new Parameters();
            eParameter = (Parameters)Session["sParameter"];

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            lstReport = Data.ExpenseDetail.GetAllExpensesConsult(eEmployee.Idhrms, 0, eParameter.StartDate, eParameter.EndDate);

            return GridViewExtension.ExportToXls(PersonalizedClasses.ExpenseDetail.ExportConsultSettings, lstReport);
        }
        [HttpGet]
        public FileResult Cuentareporte()
        {
            List<SimpleExpenseDetailReport> lstReport = new List<SimpleExpenseDetailReport>();

            try
            {



                Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            //eParameterx = (Entities.ViewModels.FinanceParametersView)Session["sParameterReport"];

            var result = Data.ExpenseDetail.GetAllFinanceExpensesByManagementClassPeriodCompanyx(int.Parse(eEmployee.GERENCIAIDHRMS))


                .ToList();
               
            DataTable dt = null;
            if (result != null)
            {
                    lstReport = result.Select(expense => new SimpleExpenseDetailReport
                    {
                        EmployeeNumber = expense.EmployeeNumber,
                        FullName = expense.FullName,
                        ManagementName = expense.ManagementName,
                        SubManagementName = expense.SubManagementName,
                        AreaName = expense.AreaName,
                        CostCenterName = expense.CostCenterName,
                        EconomicActivity = expense.EconomicActivity,
                        BussinessName = expense.BussinessName
                    }).ToList();


                    ListtoDataTableConverter converter = new ListtoDataTableConverter();
                dt = converter.ToDataTable(lstReport);



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

         
            }
            catch (Exception e)
            {
                return null;
            }

           

        }
        #endregion



    }

}
