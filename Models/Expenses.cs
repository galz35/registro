using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using System.Xml.Serialization;
using DevExpress.Data;
using DevExpress.Data.Linq.Helpers;
using DevExpress.Web.Mvc;
using Newtonsoft.Json;
using RestSharp;

namespace slnRhonline.Models
{
    public static class Expenses
    {
        const string keyExpenseId = "sExpenseId";
        const string keyPerson = "sPersonId";

        static HttpSessionState Session { get { return HttpContext.Current.Session; } }


        #region Dashboard Executed vs Assignment
        public static List<Entities.ViewModels.ExpenseBudget> GetChartExecutedAssignment(int bossId)
        {

            try
            {
                var result = Utils.ClaroWCF.GetExpenseBudgetVsExecuted(bossId);
                if (result != null)
                {
                    return result.ToList();
                }
                else
                {
                    return new List<Entities.ViewModels.ExpenseBudget>();
                }

            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region Registro de Viaticos
        /// <summary>
        /// Metodo que llama al metodo GetExpenseStatusById para obtener el estado del viatico.
        /// </summary>
        /// <returns></returns>
        public static Entities.ExpensesStatus GetExpenseStatusById(int expenseId)
        {
            Entities.ExpensesStatus expense = new Entities.ExpensesStatus();



            var result = Utils.ClaroWCF.GetExpenseStatusById(expenseId);


            if (result != null)
            {
                var deserializedObject = new JavaScriptSerializer().Deserialize<Entities.ExpensesStatus>(result);
                expense = deserializedObject;
            }
            else
            {
                expense = new Entities.ExpensesStatus();
            }

            return expense;
        }
        //Este metodo ya no se usa

      
        public static List<Entities.ExpensesStatus> GetAllExpensesStatus()
        {
            List<Entities.ExpensesStatus> lstExpenses = new List<Entities.ExpensesStatus>();
            try
            {
                var result = Utils.ClaroWCF.GetAllExpensesStatus();
                if (result != null)
                {
                    lstExpenses = result.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return lstExpenses;
        }

        public static List<Entities.ViewModels.ExpenseDetailView> GetAllExpenseValidateYieldView(int expenseId)
        {
            List<Entities.ViewModels.ExpenseDetailView> lstDetail = new List<Entities.ViewModels.ExpenseDetailView>();
            try
            {
                var result = Utils.ClaroWCF.GetAllExpenseValidateYieldView(expenseId);
                if (result != null)
                {
                    lstDetail = result.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return lstDetail;
        }


        public static List<Entities.ViewModels.ExpenseDetailView> GetExpenseDetailViewByPersonId(string personId)
        {
            List<Entities.ViewModels.ExpenseDetailView> lstExpenseDetailView = new List<Entities.ViewModels.ExpenseDetailView>();

            try 
            { 
                    var client = new RestClient("http://172.26.54.66/hcmapiws/api/viatico/GetExpenseDetailViewByPersonIdx?personId=" + personId);
                    var request = new RestRequest(Method.GET);
                    request.Timeout = -1;


                    var resultExpensesx = client.Execute(request);
                    //Console.WriteLine(response.Content);
                    //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                    if (resultExpensesx != null)
                    {
                        var serializer = new JavaScriptSerializer();
                        serializer.MaxJsonLength = 500000000;
                     var result = JsonConvert.DeserializeObject<Dictionary<string, List<Entities.ViewModels.ExpenseDetailView>>>(resultExpensesx.Content)["data"];
                    if (result!=null)
                    {
                        lstExpenseDetailView = result;
                    }
                    //lstExpenseDetailView = serializer.Deserialize<List<Entities.ViewModels.ExpenseDetailView>>(resultExpensesx.Content);
                      
                    }
                     
              
          
                //var result = Utils.ClaroWCF.GetExpenseDetailViewByPersonId(personId+"");

                //if (result != null && result.Count()>0)
                //{
                //    lstExpenseDetailView = result.ToList();

                    //List<Expensejson> expensesjson = JsonConvert.DeserializeObject<List<Expensejson>>(result);
                    //List<Entities.ViewModels.ExpenseDetailView> expenseRegisterDetails = new List<Entities.ViewModels.ExpenseDetailView>();

                    //foreach (Expensejson expense in expensesjson)
                    //{
                    //    Entities.ViewModels.ExpenseDetailView expenseRegisterDetail = new Entities.ViewModels.ExpenseDetailView
                    //    {
                    //        ExpenseId = (int)expense.EXPENSE_ID,
                    //        Carnet = expense.CARNET,
                    //        PersonId = (int)expense.PERSON_ID,
                    //        ExpenseDate = expense.EXPENSE_DATE,
                    //        ClassId = (int)expense.CLASS_ID,
                    //        Justify = expense.JUSTIFY,
                    //        ReasonId = (int)expense.REASON_ID,
                    //        Route = expense.ROUTE,
                    //        ServiceNumber = expense.SERVICE_NUMBER?.ToString(),
                    //        ExpenseDetailId = (int)expense.EXPENSE_DETAIL_ID,
                    //        EmployeeNumber = expense.CARNET1,
                    //        ClasificationId = (int)expense.CLASIFICATIONID,
                    //        CategoryId = (int)expense.CATEGORYID,
                    //        SubCategoryId = (int)expense.SUBCATEGORYID,
                    //        Clasification = expense.CLASIFICATION,
                    //        Category = expense.CATEGORY,
                    //        SubCategory = expense.SUBCATEGORY,
                    //        ClassName = expense.CLASSNAME,
                    //        FullName = expense.FULLNAME,
                    //        TotalAmount = (decimal)expense.AMOUNT,
                    //        ExpenseStatus = expense.STATUS,
                    //        HourStart = expense.HOUR_START
                    //    };

                    //    lstExpenseDetailView.Add(expenseRegisterDetail);
                    //}
                //}
            }
            catch (Exception)
            {
                return null;
            }

            return lstExpenseDetailView;
        }

        public static List<Entities.ExpenseDetail> GetAllExpenseDetails()
        {
            List<Entities.ExpenseDetail> lstExpenseDetail = new List<Entities.ExpenseDetail>();
            try
            {
                var result = Utils.ClaroWCF.GetAllExpenseDetails();
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return lstExpenseDetail;
        }
        public static List<Entities.ExpenseDetail> GetAllExpenseDetailById(int expenseId)
        {
            List<Entities.ExpenseDetail> lstExpenseDetail = new List<Entities.ExpenseDetail>();
            try
            {
                var result = Utils.ClaroWCF.GetAllExpenseDetailById(expenseId);
                if (result != null)
                {
                    lstExpenseDetail = result.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return lstExpenseDetail;
        }
        #endregion
        #region Métodos CRUD
        public static List<Entities.ExpenseDetail> GetAllEditableExpenseDetail()
        {
            string respuesta = "0";
            //Recuperacion de parametros.
            Entities.Expenses eExpense = new Entities.Expenses();

            List<Entities.ExpenseDetail> lstDetail = Session["sExpenseDetail"] as List<Entities.ExpenseDetail>;
            if (Session["sExpense"] != null)
            {
                eExpense = (Entities.Expenses)Session["sExpense"];
            }


            if (lstDetail == null && eExpense!=null && eExpense.ExpenseId>0)
            {
                
                var result = Expenses.GetAllExpenseDetailById(eExpense.ExpenseId);

                if (result != null)
                {
                    lstDetail = result.ToList();
                    Session["sExpenseDetail"] = lstDetail;
                   respuesta= "1";
                }
                else {
                    //respuesta = $"Error:\n" +
                    //  $"persona: {persona.EmployeeNumber}\n" +
                    //  $"correo: {persona.correo}\n" +
                    //  $"nombre: {persona.FirstName}\n" +
                    //  $"apeliido: {persona.LastNames}\n" +
                    //  $"gerenca: {persona.GERENCIA}\n" +
                    //  $"subManagementId: {subManagementId}\n" +
                    //  $"managementId: {managementId}\n" +
                    //  $"idgerencia: {idgerencia}\n" +
                    //  $"periods: {periods}";
                }
            }


            return lstDetail;
        }
        public static string  GetAllEditableExpenseDetails()
        {
            string respuesta = "0";
            //Recuperacion de parametros.
            Entities.Expenses eExpense = new Entities.Expenses();

            List<Entities.ExpenseDetail> lstDetail = Session["sExpenseDetail"] as List<Entities.ExpenseDetail>;
            if (Session["sExpense"] != null)
            {
                eExpense = (Entities.Expenses)Session["sExpense"];
            }


            if (lstDetail == null && eExpense != null && eExpense.ExpenseId > 0)
            {
                try
                {
                    var result = Expenses.GetAllExpenseDetailById(eExpense.ExpenseId);

                    if (result != null)
                    {
                        lstDetail = result.ToList();
                        Session["sExpenseDetail"] = lstDetail;
                        respuesta = "1";
                    }
                    else
                    {
                        respuesta = $"Error:GetAllEditableExpenseDetails \n" +

                          $"ExpenseId: {eExpense.ExpenseId}\n" +
                          $"resultado: {result}\n";
                         
                    }
                } catch (Exception ex) {
                    respuesta = $"Error:GetAllEditableExpenseDetails:{ex.Message} \n" +

                             $"ExpenseId: {eExpense.ExpenseId}\n" ;

                }
            }


            return respuesta;
        }
        public static List<Entities.ExpenseDetail> GetAllEditableExpenseDetailjson()
        {
            //Recuperacion de parametros.
            Entities.Expenses eExpense = new Entities.Expenses();
            List<Entities.ExpenseDetail> lstDetail = new List<Entities.ExpenseDetail>();

            //List<Entities.ExpenseDetail> lstDetail = Session["sExpenseDetail"] as List<Entities.ExpenseDetail>;

            if (Session["sExpenseDetail"]!=null)
            {
             lstDetail = Session["sExpenseDetail"] as List<Entities.ExpenseDetail>;

            }
            if (Session["sExpense"] != null)
            {
                eExpense = (Entities.Expenses)Session["sExpense"];
            }


            if ( lstDetail == null  && eExpense != null && eExpense.ExpenseId > 0)
            {
                var result = Expenses.GetAllExpenseDetailById(eExpense.ExpenseId);

                if (result != null)
                {
                    lstDetail = result.ToList();
                    Session["sExpenseDetail"] = lstDetail;
                }
            }
            else if (lstDetail.Count()==0)
            {
                var result = Expenses.GetAllExpenseDetailById(eExpense.ExpenseId);

                if (result != null)
                {
                    lstDetail = result.ToList();
                    Session["sExpenseDetail"] = lstDetail;
                }
            }

            if (lstDetail==null)
            {
                lstDetail = new List<Entities.ExpenseDetail>();
            }
            return lstDetail;
        }

        public static void SaveExpense(Entities.Expenses item)
        {
            try
            {
                //Si es una actualizacion

                if (item.ExpenseId > 0)
                {
                    //Actualizar el encabezado si hubo cambios
                    UpdateExpense(item);

                    //Eliminar de la base de datos el detalle anterior
                    string Result = Utils.ClaroWCF.DeleteAllExpenseDetail(item.ExpenseId);

                    //Insertar Detalle
                    foreach (var val in GetAllEditableExpenseDetail())
                    {
                        Entities.ExpenseDetail eExpenseDetail = new Entities.ExpenseDetail();


                        eExpenseDetail.ExpenseId = item.ExpenseId;
                        eExpenseDetail.ClasificationId = val.ClasificationId;
                        eExpenseDetail.CategoryId = val.CategoryId;
                        eExpenseDetail.SubCategoryId = val.SubCategoryId;
                        eExpenseDetail.HourStart = DateTime.Parse(DateTime.Now.ToShortDateString() +
                            " " +
                            val.HourStart.ToString("hh:mm:ss tt"));
                        eExpenseDetail.Amount = val.Amount;
                        eExpenseDetail.ExpenseDetailNotes = val.ExpenseDetailNotes;
                        eExpenseDetail.DepartmentId = val.DepartmentId;

                        if (item.ClassId != 16)
                        {
                            eExpenseDetail.YieldAmount = val.Amount;
                            eExpenseDetail.ReturnAmount = eExpenseDetail.Amount - eExpenseDetail.YieldAmount;
                        }

                        InsertExpenseDetail(eExpenseDetail);
                    }
                }
                else // Si es una insercion
                {
                    //Insertar encabezado
                    //List<Entities.ExpenseDetail> tlist = new List<Entities.ExpenseDetail>();
                    ////Insertar Detalle
                    //List<Entities.ExpenseDetail> lstDetail = Session["sExpenseDetail"] as List<Entities.ExpenseDetail>;
                    InsertExpense(item);
                    int expenseId = (int)Session[keyExpenseId];
                    foreach (var val in GetAllEditableExpenseDetail())
                    {
                        Entities.ExpenseDetail eExpenseDetail = new Entities.ExpenseDetail();


                        //eExpenseDetail.ExpenseId = 0;
                        eExpenseDetail.ExpenseId = expenseId;
                        eExpenseDetail.ClasificationId = val.ClasificationId;
                        eExpenseDetail.CategoryId = val.CategoryId;
                        eExpenseDetail.SubCategoryId = val.SubCategoryId;
                        eExpenseDetail.HourStart = DateTime.Parse(DateTime.Now.ToShortDateString() +
                            " " +
                            val.HourStart.ToString("hh:mm:ss tt"));
                        eExpenseDetail.Amount = val.Amount;
                        eExpenseDetail.ExpenseDetailNotes = val.ExpenseDetailNotes;
                        eExpenseDetail.DepartmentId = val.DepartmentId;
                        if (item.ClassId != 16)
                        {
                            eExpenseDetail.YieldAmount = val.Amount;
                            eExpenseDetail.ReturnAmount = eExpenseDetail.Amount - eExpenseDetail.YieldAmount;
                        }

                        InsertExpenseDetail(eExpenseDetail);
                    }
                    //InsertExpense(item, lstDetail);
                    //int expenseId = (int)Session[keyExpenseId];
                }
            }
            catch(Exception ex)
            {
                throw new HttpException(404, "Error al insertar el registro.");

            }

            return;
        }
        public static viatico ConvertirExpensesAViatico(Entities.Expenses item)
        {
            return new viatico
            {
                ExpenseId = item.ExpenseId,
                ExpenseDate = item.ExpenseDate,
                PersonId = item.PersonId,
                ExpenseNotes = item.ExpenseNotes,
                Route = item.Route,
                Justify = item.Justify,
                ClassId = item.ClassId,
                ServiceNumber = item.ServiceNumber,
                ReasonId = item.ReasonId,
                ExpensePeriodId = item.ExpensePeriodId,
                VehicleNumber = item.VehicleNumber,
                YieldFileExtension = item.YieldFileExtension,
                DepositFileExtension = item.DepositFileExtension,
                YieldNotes = item.YieldNotes,
                carnet = item.carnet 
                
            };
        }

        public static void InsertExpense(Entities.Expenses item)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;
                eEmployee = (Entities.Employees)Session["User"];
                Entities.Employees Employee = new Entities.Employees();
                Employee= (Entities.Employees)Session["empleado"] ;

                var subManagementId = Utils.ClaroWCF.ExtraTimeGetOrganizationId(item.PersonId);
                //string subManagementId = (string)Session["subManagementId"];
                string managementId = Utils.ClaroWCF.GetManagementId(int.Parse(subManagementId));
               // string managementId = (string)Session["managementId"];
                var respPeriodId = Utils.ClaroWCF
                    .GetExpenseInsertPeriod(int.Parse(managementId),
                                            item.ClassId,
                                            item.ExpenseDate.ToShortDateString());
                item.ExpensePeriodId = int.Parse(respPeriodId);
                //Result = Utils.ClaroWCF.InsertExpense(item, eEmployee.Idhrms);
                viatico temp = new viatico();

                string endpoint = "http://172.26.54.66/apihcm/api/viatico/InsertExpense";

              
                var client = new RestClient(endpoint);
                var request = new RestRequest(  Method.POST);
                var model1 = ConvertirExpensesAViatico(item);
                    model1.RegisterPersonId = Employee.Idhrms;
                model1.PersonId = Employee.Idhrms;
                model1.carnet = Employee.EmployeeNumber;
                 string jsonBody = JsonConvert.SerializeObject(model1);

                 request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

                 IRestResponse response = client.Execute(request);
                if (response != null)
                {
                    string valorCrudo = response.Content;
                    string limpio = valorCrudo.Replace("\"", "");  // quita las comillas
                    int resultado = int.Parse(limpio);
                    Session[keyExpenseId] = resultado;
                }
            }
            catch(Exception ex)
            {
                throw new HttpException(404, "Error al insertar el registro.");
            }

            return;
        }
        public static void InsertExpense(Entities.Expenses item, List<Entities.ExpenseDetail> lstDetail)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;
                eEmployee = (Entities.Employees)Session["User"];
                //Obtener periodo actual


                //var subManagementId = Utils.ClaroWCF.ExtraTimeGetOrganizationId(item.PersonId);
                //string subManagementId = (string)Session["subManagementId"];
                //string managementId = Utils.ClaroWCF.GetManagementId(int.Parse(subManagementId));
                // string managementId = (string)Session["managementId"];
                var respPeriodId = Utils.ClaroWCF
                    .GetExpenseInsertPeriod(long.Parse(eEmployee.GERENCIAID),
                                            item.ClassId,
                                            item.ExpenseDate.ToShortDateString());
                item.ExpensePeriodId = int.Parse(respPeriodId);
                string xmlregistro = "";
                XmlSerializer serializer = new XmlSerializer(typeof(List<Entities.ExpenseDetail>));
                using (StringWriter stringWriter = new StringWriter())
                {
                    serializer.Serialize(stringWriter, lstDetail);
                  xmlregistro=   stringWriter.ToString();
                }

                Result = Utils.ClaroWCF.InsertExpense3(item, eEmployee.Id_HRMS, xmlregistro);

                if (Result != null)
                {
                    Session[keyExpenseId] = int.Parse(Result);
                }
            }
            catch
            {
                throw new HttpException(404, "Error al insertar el registro.");
            }

            return;
        }

        public static void UpdateExpense(Entities.Expenses item)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;
                eEmployee = (Entities.Employees)Session["User"];
                //Obtener periodo actual
                long personId = (long)Session[keyPerson];

                var subManagementId = Utils.ClaroWCF.ExtraTimeGetOrganizationId(personId);
                string managementId = Utils.ClaroWCF.GetManagementId(int.Parse(subManagementId));
                var respPeriodId = Utils.ClaroWCF
                    .GetExpenseInsertPeriod(int.Parse(managementId),
                                            item.ClassId,
                                            item.ExpenseDate.ToShortDateString());
                item.ExpensePeriodId = int.Parse(respPeriodId);

                Result = Utils.ClaroWCF.UpdateExpense(item);
            }
            catch(Exception ex)
            {
                throw new HttpException(404, "Error al insertar el registro.");
            }

            return;
        }

        public static void DeleteExpense(int expenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;
                eEmployee = (Entities.Employees)Session["User"];
                Result = Utils.ClaroWCF.DeleteExpense(expenseId, eEmployee.Idhrms);
            }
            catch
            {
                throw new HttpException(404, "Error al eliminar el registro.");
            }

            return;
        }


        public static int GetNewLineDetailId()
        {
            List<Entities.ExpenseDetail> editableId = GetAllEditableExpenseDetail();
            return (editableId.Count() > 0) ? (editableId.Last().ExpenseDetailId + 1) : 0;
        }

        public static void AddDetailLine(Entities.ExpenseDetail item)
        {
            try
            {
                Entities.ExpenseDetail eExpenseDetail = new Entities.ExpenseDetail();


                eExpenseDetail.ExpenseDetailId = GetNewLineDetailId();
                eExpenseDetail.ClasificationId = item.ClasificationId;
                eExpenseDetail.CategoryId = item.CategoryId;
                eExpenseDetail.SubCategoryId = item.SubCategoryId;
                eExpenseDetail.HourStart = DateTime.Parse(DateTime.Now.ToShortDateString() +
                    " " +
                    item.HourStart.ToString("hh:mm:ss tt"));
                eExpenseDetail.Amount = item.Amount;

                eExpenseDetail.ExpenseDetailNotes = item.ExpenseDetailNotes;
                eExpenseDetail.DepartmentId = item.DepartmentId;

                GetAllEditableExpenseDetail().Add(eExpenseDetail);//retorna listado que este listado tiene la referencia de la seesion por lo tanto hacer add indirectamente guarda el registro en la session actualiza.

            }
            catch
            {
                throw new HttpException(404, "Error al insertar el registro.");
            }

            return;
        }


        public static void UpdateDetailLine(Entities.ExpenseDetail item)
        {
            try
            {
                Entities.ExpenseDetail eExpenseDetail = GetAllEditableExpenseDetail()
                    .FirstOrDefault(x => x.ExpenseDetailId == item.ExpenseDetailId);
                if (eExpenseDetail != null)
                {
                    eExpenseDetail.ClasificationId = item.ClasificationId;
                    eExpenseDetail.CategoryId = item.CategoryId;
                    eExpenseDetail.SubCategoryId = item.SubCategoryId;
                    eExpenseDetail.HourStart = DateTime.Parse(DateTime.Now.ToShortDateString() +
                        " " +
                        item.HourStart.ToString("hh:mm:ss tt"));
                    eExpenseDetail.Amount = item.Amount;
                    eExpenseDetail.ExpenseDetailNotes = item.ExpenseDetailNotes;
                    eExpenseDetail.DepartmentId = item.DepartmentId;
                }
            }
            catch
            {
                throw new HttpException(404, "Error al actualizar la linea de detalle");
            }

            return;
        }


        public static void DeleteDetailLine(Entities.ExpenseDetail item)
        {
            try
            {
                var editableItem = GetAllEditableExpenseDetail()
                    .Where(et => et.ExpenseDetailId == item.ExpenseDetailId)
                    .FirstOrDefault();

                if (editableItem != null)
                {
                    GetAllEditableExpenseDetail().Remove(editableItem);
                }
            }
            catch
            {
                throw new HttpException(404, "Error al actualizar la linea de detalle");
            }

            return;
        }

        //Metodos sin sesion
        public static void InsertExpenseDetail(Entities.ExpenseDetail item)
        {
            string Result;
            try
            {
                Result = Utils.ClaroWCF.InsertExpenseDetail(item);
            }
            catch(Exception ex)
            {
                throw new HttpException(404, "Error al insertar el registro.");
            }

            return;
        }
        public static void InsertExpenseDetail(Entities.ExpenseDetail item, List<Entities.ExpenseDetail> lstDetail)
        {
            string Result;
            try
            {
                Result = Utils.ClaroWCF.InsertExpenseDetail(item);
            }
            catch
            {
                throw new HttpException(404, "Error al insertar el registro.");
            }

            return;
        }

        #endregion

        #region Binding AuthorizeManager

        static IQueryable<Entities.ViewModels.ExpenseDetailView> ModelManager
        {
            get { return GetAllExpensesAuthorizeManager().AsQueryable(); }
        }

        // Conteo de registros totales
        public static void GetListManagerCount(GridViewCustomBindingGetDataRowCountArgs e)
        {
            e.DataRowCount = ModelManager.Count();
        }

        // Conteo de registros cuando hay filtros activdados
        public static void GetListManagerCountAdvanced(GridViewCustomBindingGetDataRowCountArgs e)
        {
            int rowCount;
            if (GridViewCustomBindingSummaryCache.TryGetCount(e.FilterExpression, out rowCount))
            {
                e.DataRowCount = rowCount;
            }
            else
            {
                e.DataRowCount = ModelManager.ApplyFilter(e.FilterExpression).Count();
            }
        }

        // Obtiene la lista de horas extras, filtradas y ordenadas con el custombinding
        public static void GetListManager(GridViewCustomBindingGetDataArgs e)
        {
            e.Data = ModelManager
              .ApplyFilter(e.FilterExpression)
                .Skip(e.StartDataRowIndex)
                .Take(e.DataRowCount);
        }

        public static void GetSummaryValuesManager(GridViewCustomBindingGetSummaryValuesArgs e)
        {
            var query = ModelManager
                .ApplyFilter(e.State.AppliedFilterExpression)
                .ApplyFilter(e.GroupInfoList);
            var list = new ArrayList();
            foreach (var item in e.SummaryItems)
            {
                switch (item.SummaryType)
                {
                    case SummaryItemType.Count:
                        list.Add(query.Count());
                        break;
                    default:
                        string summaryString = Enum.GetName(typeof(SummaryItemType), item.SummaryType);
                        list.Add(query.CountSummaryManager(item.FieldName, summaryString));
                        break;
                }
            }
            e.Data = list;
        }

        public static object CountSummaryManager(this IQueryable query, string fieldName, string summaryType)
        {
            if (query.Count() == 0)
            {
                return 0;
            }

            var parameter = Expression.Parameter(query.ElementType, string.Empty);
            var propertyInfo = query.ElementType.GetProperty(fieldName);
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);
            var propertyAccessExpression = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression expression = null;
            if ((summaryType == "Min") || (summaryType == "Max"))
            {
                expression = Expression.Call(typeof(Queryable),
                                             summaryType,
                                             new Type[]
                    { query.ElementType, propertyAccessExpression.Body.Type },
                                             query.Expression,
                                             propertyAccessExpression);
            }
            else
            {
                expression = Expression.Call(typeof(Queryable),
                                             summaryType,
                                             new Type[]
                    { query.ElementType },
                                             query.Expression,
                                             Expression.Quote(propertyAccessExpression));
            }

            return query.Provider.Execute(expression);
        }

        public static List<Entities.ViewModels.ExpenseDetailView> GetAllExpensesAuthorizeManager()
        {
            //Recuperacion de parametros.

            List<Entities.ViewModels.ExpenseDetailView> lstExtraTime = Session["sExpenseAuthorizeManager"] as List<Entities.ViewModels.ExpenseDetailView>;
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }


            if ((eEmployee.userlevel == 5) || (eEmployee.userlevel == 6))
            {
                if (lstExtraTime == null)
                {
                    var resultExpenses = Utils.ClaroWCF.GetAllExpensesForAuthorize(eEmployee.Idhrms, 5);

                    if (resultExpenses != null)
                    {
                        //Agrupando el resultado obtenido del webservice
                        var oExpenseDetail = from item in resultExpenses.Where(x => (x.StatusId == 5) &&
                            (x.PersonId != eEmployee.Idhrms))
                                             group item by new
                                             {
                                                 item.ExpenseId,
                                                 item.EmployeeNumber,
                                                 item.PersonId,
                                                 item.FullName,
                                                 item.ExpenseDate,
                                                 item.ClassName,
                                                 item.Justify,
                                                 item.ExpenseStatus,
                                                 item.VehicleNumber
                                             } into g

                                             select new Entities.ViewModels.ExpenseDetailView
                                             {
                                                 ExpenseId = g.Key.ExpenseId,
                                                 EmployeeNumber = g.Key.EmployeeNumber,
                                                 PersonId = g.Key.PersonId,
                                                 FullName = g.Key.FullName,
                                                 ExpenseDate = g.Key.ExpenseDate,
                                                 ClassName = g.Key.ClassName,
                                                 Justify = g.Key.Justify,
                                                 ExpenseStatus = g.Key.ExpenseStatus,
                                                 VehicleNumber = g.Key.VehicleNumber,
                                                 TotalAmount = g.Sum(y => y.TotalAmount)
                                             };
                        lstExtraTime = oExpenseDetail.OrderByDescending(o => o.ExpenseDate).ToList();
                        //Asignando la lista a la sesión.
                        Session["sExpenseAuthorizeManager"] = lstExtraTime;
                        //Session["sExpenseAuthorizeManager"] = lstExtraTime = resultExpenses.ToList();
                        return lstExtraTime;
                    }
                    else
                    {
                        return new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }
                else
                {
                    return lstExtraTime;
                }
            }
            else
            {
                return new List<Entities.ViewModels.ExpenseDetailView>();
            }
        }

        public static void AuthorizeManager(int expenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                Result = Utils.ClaroWCF.ChangeStateExpense(expenseId, 3, eEmployee.Idhrms);
                if (Result != null)
                {
                    var editableItem = GetAllExpensesAuthorizeManager()
                        .Where(et => et.ExpenseId == expenseId)
                        .FirstOrDefault();
                    GetAllExpensesAuthorizeManager().Remove(editableItem);
                }
            }
            catch
            {
                throw new HttpException(404, "Error al autorizar el registro.");
            }

            return;
        }

        public static void DeniedManager(int expenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                Result = Utils.ClaroWCF.ChangeStateExpense(expenseId, -3, eEmployee.Idhrms);
                if (Result != null)
                {
                    var editableItem = GetAllExpensesAuthorizeManager()
                        .Where(et => et.ExpenseId == expenseId)
                        .FirstOrDefault();
                    GetAllExpensesAuthorizeManager().Remove(editableItem);
                }
            }
            catch
            {
                throw new HttpException(404, "Error al autorizar el registro.");
            }

            return;
        }

        #endregion
        #region Binding AuthorizeRrhh

        static IQueryable<Entities.ViewModels.ExpenseDetailView> ModelRrhh
        {
            get { return GetAllExpensesAuthorizeRrhh().AsQueryable(); }
        }

        // Conteo de registros totales
        public static void GetListRrhhCount(GridViewCustomBindingGetDataRowCountArgs e)
        {
            e.DataRowCount = ModelRrhh.Count();
        }

        // Conteo de registros cuando hay filtros activdados
        public static void GetListRrhhCountAdvanced(GridViewCustomBindingGetDataRowCountArgs e)
        {
            int rowCount;
            if (GridViewCustomBindingSummaryCache.TryGetCount(e.FilterExpression, out rowCount))
            {
                e.DataRowCount = rowCount;
            }
            else
            {
                e.DataRowCount = ModelRrhh.ApplyFilter(e.FilterExpression).Count();
            }
        }

        // Obtiene la lista de horas extras, filtradas y ordenadas con el custombinding
        public static void GetListRrhh(GridViewCustomBindingGetDataArgs e)
        {
            e.Data = ModelRrhh
              .ApplyFilter(e.FilterExpression)
                .Skip(e.StartDataRowIndex)
                .Take(e.DataRowCount);
        }

        public static void GetSummaryValuesRrhh(GridViewCustomBindingGetSummaryValuesArgs e)
        {
            var query = ModelRrhh
                .ApplyFilter(e.State.AppliedFilterExpression)
                .ApplyFilter(e.GroupInfoList);
            var list = new ArrayList();
            foreach (var item in e.SummaryItems)
            {
                switch (item.SummaryType)
                {
                    case SummaryItemType.Count:
                        list.Add(query.Count());
                        break;
                    default:
                        string summaryString = Enum.GetName(typeof(SummaryItemType), item.SummaryType);
                        list.Add(query.CountSummaryRrhh(item.FieldName, summaryString));
                        break;
                }
            }
            e.Data = list;
        }

        public static object CountSummaryRrhh(this IQueryable query, string fieldName, string summaryType)
        {
            if (query.Count() == 0)
            {
                return 0;
            }

            var parameter = Expression.Parameter(query.ElementType, string.Empty);
            var propertyInfo = query.ElementType.GetProperty(fieldName);
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);
            var propertyAccessExpression = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression expression = null;
            if ((summaryType == "Min") || (summaryType == "Max"))
            {
                expression = Expression.Call(typeof(Queryable),
                                             summaryType,
                                             new Type[]
                    { query.ElementType, propertyAccessExpression.Body.Type },
                                             query.Expression,
                                             propertyAccessExpression);
            }
            else
            {
                expression = Expression.Call(typeof(Queryable),
                                             summaryType,
                                             new Type[]
                    { query.ElementType },
                                             query.Expression,
                                             Expression.Quote(propertyAccessExpression));
            }

            return query.Provider.Execute(expression);
        }

        public static List<Entities.ViewModels.ExpenseDetailView> GetAllExpensesAuthorizeRrhh()
        {
            //Recuperacion de parametros.

            List<Entities.ViewModels.ExpenseDetailView> lstExtraTime = Session["sExpenseAuthorizeRrhh"] as List<Entities.ViewModels.ExpenseDetailView>;
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }


            if (eEmployee.userlevel == 6)
            {
                if (lstExtraTime == null)
                {
                    var resultExpenses = Utils.ClaroWCF.GetAllExpensesForAuthorize(eEmployee.Idhrms, 3);
                    if (resultExpenses != null)
                    {
                        //Agrupando el resultado obtenido del webservice
                        var oExpenseDetail = from item in resultExpenses.Where(x => x.StatusId == 3)
                                             group item by new
                                             {
                                                 item.ExpenseId,
                                                 item.EmployeeNumber,
                                                 item.PersonId,
                                                 item.FullName,
                                                 item.ExpenseDate,
                                                 item.ClassName,
                                                 item.Justify,
                                                 item.ExpenseStatus,
                                                 item.VehicleNumber
                                             } into g

                                             select new Entities.ViewModels.ExpenseDetailView
                                             {
                                                 ExpenseId = g.Key.ExpenseId,
                                                 EmployeeNumber = g.Key.EmployeeNumber,
                                                 PersonId = g.Key.PersonId,
                                                 FullName = g.Key.FullName,
                                                 ExpenseDate = g.Key.ExpenseDate,
                                                 ClassName = g.Key.ClassName,
                                                 Justify = g.Key.Justify,
                                                 ExpenseStatus = g.Key.ExpenseStatus,
                                                 VehicleNumber = g.Key.VehicleNumber,
                                                 TotalAmount = g.Sum(y => y.TotalAmount)
                                             };
                        lstExtraTime = oExpenseDetail.OrderByDescending(o => o.ExpenseDate).ToList();

                        //Asignando la lista a la sesión.
                        Session["sExpenseAuthorizeRrhh"] = lstExtraTime;

                        return lstExtraTime;
                    }
                    else
                    {
                        return new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }
                else
                {
                    return lstExtraTime;
                }
            }
            else
            {
                return new List<Entities.ViewModels.ExpenseDetailView>();
            }
        }

        /// <summary>
        /// Método para autorizacion de Recursos Humanos.
        /// </summary>
        /// <param name="expenseId"></param>
        public static void AuthorizeRrhh(int expenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                var expense = Utils.ClaroWCF.GetAllExpenses(expenseId).FirstOrDefault();
                if (expense != null)
                {
                    if (expense.ClassId == 16)
                    {
                        Result = Utils.ClaroWCF.ChangeStateExpense(expenseId, 4, eEmployee.Idhrms);
                        if (Result != null)
                        {
                            var editableItem = GetAllExpensesAuthorizeRrhh()
                                .Where(et => et.ExpenseId == expenseId)
                                .FirstOrDefault();
                            GetAllExpensesAuthorizeRrhh().Remove(editableItem);
                        }
                    }
                    else
                    {
                        Result = Utils.ClaroWCF.ChangeStateExpense(expenseId, 7, eEmployee.Idhrms);
                        if (Result != null)
                        {
                            var editableItem = GetAllExpensesAuthorizeRrhh()
                                .Where(et => et.ExpenseId == expenseId)
                                .FirstOrDefault();
                            GetAllExpensesAuthorizeRrhh().Remove(editableItem);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error al autorizar el registro", e);
            }
            //catch { throw new HttpException(404, "Error al autorizar el registro."); }

            return;
        }

        public static void DeniedRrhh(int expenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                Result = Utils.ClaroWCF.ChangeStateExpense(expenseId, -4, eEmployee.Idhrms);
                if (Result != null)
                {
                    var editableItem = GetAllExpensesAuthorizeRrhh()
                        .Where(et => et.ExpenseId == expenseId)
                        .FirstOrDefault();
                    GetAllExpensesAuthorizeRrhh().Remove(editableItem);
                }
            }
            catch
            {
                throw new HttpException(404, "Error al autorizar el registro.");
            }

            return;
        }

        #endregion
        #region Binding AuthorizeYield

        static IQueryable<Entities.ViewModels.ExpenseDetailView> ModelYield
        {
            get { return GetAllExpensesAuthorizeYield().AsQueryable(); }
        }

        // Conteo de registros totales
        public static void GetListYieldCount(GridViewCustomBindingGetDataRowCountArgs e)
        {
            e.DataRowCount = ModelYield.Count();
        }

        // Conteo de registros cuando hay filtros activdados
        public static void GetListYieldCountAdvanced(GridViewCustomBindingGetDataRowCountArgs e)
        {
            int rowCount;
            if (GridViewCustomBindingSummaryCache.TryGetCount(e.FilterExpression, out rowCount))
            {
                e.DataRowCount = rowCount;
            }
            else
            {
                e.DataRowCount = ModelYield.ApplyFilter(e.FilterExpression).Count();
            }
        }

        // Obtiene la lista de horas extras, filtradas y ordenadas con el custombinding
        public static void GetListYield(GridViewCustomBindingGetDataArgs e)
        {
            e.Data = ModelYield
              .ApplyFilter(e.FilterExpression)
                .Skip(e.StartDataRowIndex)
                .Take(e.DataRowCount);
        }

        public static void GetSummaryValuesYield(GridViewCustomBindingGetSummaryValuesArgs e)
        {
            var query = ModelYield
                .ApplyFilter(e.State.AppliedFilterExpression)
                .ApplyFilter(e.GroupInfoList);
            var list = new ArrayList();
            foreach (var item in e.SummaryItems)
            {
                switch (item.SummaryType)
                {
                    case SummaryItemType.Count:
                        list.Add(query.Count());
                        break;
                    default:
                        string summaryString = Enum.GetName(typeof(SummaryItemType), item.SummaryType);
                        list.Add(query.CountSummaryYield(item.FieldName, summaryString));
                        break;
                }
            }
            e.Data = list;
        }

        public static object CountSummaryYield(this IQueryable query, string fieldName, string summaryType)
        {
            if (query.Count() == 0)
            {
                return 0;
            }

            var parameter = Expression.Parameter(query.ElementType, string.Empty);
            var propertyInfo = query.ElementType.GetProperty(fieldName);
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);
            var propertyAccessExpression = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression expression = null;
            if ((summaryType == "Min") || (summaryType == "Max"))
            {
                expression = Expression.Call(typeof(Queryable),
                                             summaryType,
                                             new Type[]
                    { query.ElementType, propertyAccessExpression.Body.Type },
                                             query.Expression,
                                             propertyAccessExpression);
            }
            else
            {
                expression = Expression.Call(typeof(Queryable),
                                             summaryType,
                                             new Type[]
                    { query.ElementType },
                                             query.Expression,
                                             Expression.Quote(propertyAccessExpression));
            }

            return query.Provider.Execute(expression);
        }

        public static List<Entities.ViewModels.ExpenseDetailView> GetAllExpensesAuthorizeYield()
        {
            //Recuperacion de parametros.

            List<Entities.ViewModels.ExpenseDetailView> lstExtraTime = Session["sExpenseAuthorizeYield"] as List<Entities.ViewModels.ExpenseDetailView>;
            Entities.Employees eEmployee = null;
            eEmployee = (Entities.Employees)Session["User"];


            if ((eEmployee.userlevel == 7) || (eEmployee.userlevel == 4))
            {
                if (lstExtraTime == null)
                {
                    var resultExpenses = Utils.ClaroWCF.GetAllExpensesForAuthorize(eEmployee.Idhrms, 0);
                    if (resultExpenses != null)
                    {
                        //Agrupando el resultado obtenido del webservice
                        var oExpenseDetail = from item in resultExpenses.Where(x => ((x.StatusId == 6) &&
                                (x.ClassId == 16)) ||
                            ((x.StatusId == 4) && (x.ClassId == 17)))
                                             group item by new
                                             {
                                                 item.ExpenseId,
                                                 item.EmployeeNumber,
                                                 item.PersonId,
                                                 item.FullName,
                                                 item.ExpenseDate,
                                                 item.ClassName,
                                                 item.ExpenseStatus,
                                                 item.VehicleNumber
                                             } into g

                                             select new Entities.ViewModels.ExpenseDetailView
                                             {
                                                 ExpenseId = g.Key.ExpenseId,
                                                 EmployeeNumber = g.Key.EmployeeNumber,
                                                 PersonId = g.Key.PersonId,
                                                 FullName = g.Key.FullName,
                                                 ExpenseDate = g.Key.ExpenseDate,
                                                 ClassName = g.Key.ClassName,
                                                 ExpenseStatus = g.Key.ExpenseStatus,
                                                 VehicleNumber = g.Key.VehicleNumber,
                                                 TotalAmount = g.Sum(y => y.TotalAmount)
                                             };
                        lstExtraTime = oExpenseDetail.OrderByDescending(o => o.ExpenseDate).ToList();

                        //Asignando la lista a la sesión.
                        Session["sExpenseAuthorizeYield"] = lstExtraTime;

                        return lstExtraTime;
                    }
                    else
                    {
                        return new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }
                else
                {
                    return lstExtraTime;
                }
            }
            else
            {
                return new List<Entities.ViewModels.ExpenseDetailView>();
            }
        }

        public static void AuthorizeYield(int expenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                Result = Utils.ClaroWCF.ChangeStateExpense(expenseId, 7, eEmployee.Idhrms);
                if (Result != null)
                {
                    var editableItem = GetAllExpensesAuthorizeYield()
                        .Where(et => et.ExpenseId == expenseId)
                        .FirstOrDefault();
                    GetAllExpensesAuthorizeYield().Remove(editableItem);
                }
            }
            catch
            {
                throw new HttpException(404, "Error al autorizar el registro.");
            }

            return;
        }

        public static void DeniedYield(int expenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                Result = Utils.ClaroWCF.ChangeStateExpense(expenseId, -7, eEmployee.Idhrms);
                if (Result != null)
                {
                    var editableItem = GetAllExpensesAuthorizeYield()
                        .Where(et => et.ExpenseId == expenseId)
                        .FirstOrDefault();
                    GetAllExpensesAuthorizeYield().Remove(editableItem);
                }
            }
            catch
            {
                throw new HttpException(404, "Error al autorizar el registro.");
            }

            return;
        }

        #endregion
        #region Consult


        static IQueryable<Entities.ViewModels.ExpenseDetailView> ModelConsult
        {
            get { return GetAllExpensesAuthorizeConsult().AsQueryable(); }
        }

        // Conteo de registros totales
        public static void GetListConsultCount(GridViewCustomBindingGetDataRowCountArgs e)
        {
            e.DataRowCount = ModelConsult.Count();
        }

        // Conteo de registros cuando hay filtros activdados
        public static void GetListConsultCountAdvanced(GridViewCustomBindingGetDataRowCountArgs e)
        {
            int rowCount;
            if (GridViewCustomBindingSummaryCache.TryGetCount(e.FilterExpression, out rowCount))
            {
                e.DataRowCount = rowCount;
            }
            else
            {
                e.DataRowCount = ModelConsult.ApplyFilter(e.FilterExpression).Count();
            }
        }

        // Obtiene la lista de horas extras, filtradas y ordenadas con el custombinding
        public static void GetListConsult(GridViewCustomBindingGetDataArgs e)
        {
            e.Data = ModelConsult
              .ApplyFilter(e.FilterExpression)
                .Skip(e.StartDataRowIndex)
                .Take(e.DataRowCount);
        }

        public static void GetSummaryValues(GridViewCustomBindingGetSummaryValuesArgs e)
        {
            var query = ModelConsult
                .ApplyFilter(e.State.AppliedFilterExpression)
                .ApplyFilter(e.GroupInfoList);
            var list = new ArrayList();
            foreach (var item in e.SummaryItems)
            {
                switch (item.SummaryType)
                {
                    case SummaryItemType.Count:
                        list.Add(query.Count());
                        break;
                    default:
                        string summaryString = Enum.GetName(typeof(SummaryItemType), item.SummaryType);
                        list.Add(query.ConsultCountSummary(item.FieldName, summaryString));
                        break;
                }
            }
            e.Data = list;
        }

        public static object ConsultCountSummary(this IQueryable query, string fieldName, string summaryType)
        {
            if (query.Count() == 0)
            {
                return 0;
            }

            var parameter = Expression.Parameter(query.ElementType, string.Empty);
            var propertyInfo = query.ElementType.GetProperty(fieldName);
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);
            var propertyAccessExpression = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression expression = null;
            if ((summaryType == "Min") || (summaryType == "Max"))
            {
                expression = Expression.Call(typeof(Queryable),
                                             summaryType,
                                             new Type[]
                    { query.ElementType, propertyAccessExpression.Body.Type },
                                             query.Expression,
                                             propertyAccessExpression);
            }
            else
            {
                expression = Expression.Call(typeof(Queryable),
                                             summaryType,
                                             new Type[]
                    { query.ElementType },
                                             query.Expression,
                                             Expression.Quote(propertyAccessExpression));
            }

            return query.Provider.Execute(expression);
        }

        public static List<Entities.ViewModels.ExpenseDetailView> GetAllExpensesAuthorizeConsult()
        {
            List<Entities.ViewModels.ExpenseDetailView> lstConsult = Session["sExpenseAuthorizeConsult"] as List<Entities.ViewModels.ExpenseDetailView>;
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Entities.MyEntities.Parameters eParameter = null;
            eParameter = (Entities.MyEntities.Parameters)Session["sParameter"];

            if (eEmployee.userlevel >= 0)
            {
                if (lstConsult == null)
                {
                    var resultExpenses = Utils.ClaroWCF
                        .GetAllExpensesForConsult(eEmployee.Idhrms, eParameter.StartDate.ToShortDateString(),
                            eParameter.EndDate.ToShortDateString());

                    if (resultExpenses != null)
                    {
                        //Agrupando el resultado obtenido del webservice
                        var oExpenseDetail = from item in resultExpenses
                                             group item by new
                                             {
                                                 item.ExpenseId,
                                                 item.ClassName,
                                                 item.EmployeeNumber,
                                                 item.PersonId,
                                                 item.FullName,
                                                 item.AreaName,
                                                 item.ExpenseDate,
                                                 item.ExpenseStatus
                                             } into g

                                             select new Entities.ViewModels.ExpenseDetailView
                                             {
                                                 ExpenseId = g.Key.ExpenseId,
                                                 ClassName = g.Key.ClassName,
                                                 EmployeeNumber = g.Key.EmployeeNumber,
                                                 PersonId = g.Key.PersonId,
                                                 FullName = g.Key.FullName,
                                                 AreaName = g.Key.AreaName,
                                                 ExpenseDate = g.Key.ExpenseDate,
                                                 ExpenseStatus = g.Key.ExpenseStatus,
                                                 TotalAmount = g.Sum(y => y.TotalAmount)
                                             };
                        lstConsult = oExpenseDetail.Where(l => l.TotalAmount > 0)
                            .OrderByDescending(o => o.ExpenseDate)
                            .ToList();
                        //Asignando la lista a la sesión.

                        Session["sExpenseAuthorizeConsult"] = lstConsult;

                        return lstConsult;
                    }
                    else
                    {
                        return new List<Entities.ViewModels.ExpenseDetailView>();
                    }
                }
                else
                {
                    return lstConsult;
                }
            }
            else
            {
                return new List<Entities.ViewModels.ExpenseDetailView>();
            }

            //return lstConsult;
        }

        #endregion
        #region Yield

        public static void ChangeStateExpense(int expenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                Result = Utils.ClaroWCF.ChangeStateExpense(expenseId, 6, eEmployee.Idhrms);
            }
            catch
            {
                throw new HttpException(404, "Error al cambiar el estado del registro.");
            }

            return;
        }

        /// <summary>
        /// Metodo que llama al metodo utilizado en el web service para actualizar el monto de retorno en el detalle.
        /// </summary>
        /// <param name="expenseId"></param>
        public static void UpdateReturnAmount(Entities.ExpenseDetail expenseDetail)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                Result = Utils.ClaroWCF.UpdateReturnAmount(expenseDetail);
            }
            catch
            {
                throw new HttpException(404, "Error al cambiar el estado del registro.");
            }

            return;
        }

        public static void UpdateYieldFile(Entities.Expenses eExpense)
        {
            string Result;
            try
            {
                Result = Utils.ClaroWCF.UpdateYieldFile(eExpense);
            }
            catch
            {
                throw new HttpException(404, "Error al actualizar el archivo de rendición.");
            }

            return;
        }

        public static void UpdateDepositFile(Entities.Expenses eExpense)
        {
            string Result;
            try
            {
                Result = Utils.ClaroWCF.UpdateDepositFile(eExpense);
            }
            catch
            {
                throw new HttpException(404, "Error al actualizar el archivo de deposito.");
            }

            return;
        }

        public static void UpdateYieldAmount(Entities.ExpenseDetail item)
        {
            string   Result;
            try
            {
                Result = Utils.ClaroWCF.UpdateExpenseDetail(item);
            }
            catch
            {
                throw new HttpException(404, "Error al actualizar el registro.");
            }

            return;
        }

        #endregion


    }
}