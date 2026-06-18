using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.SessionState;
using DevExpress.Data;
using DevExpress.Data.Linq.Helpers;
using DevExpress.Web.Mvc;


namespace slnRhonline.Models
{
    public static class ExtraTime
    {

        //const string keyEmployee = "Id_Employee";
        static HttpSessionState Session { get { return HttpContext.Current.Session; } }
        #region Lista de Areas
        public static List<Entities.MyEntities.Areas> GetAreas()
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }


            List<Entities.MyEntities.Areas> olstAreas = new List<Entities.MyEntities.Areas>();

            try
            {
                var oAreas = Utils.ClaroWCF.GetAreasList(eEmployee.Idhrms.ToString());
                if (oAreas != null)
                {
                    olstAreas = oAreas.ToList();
                    return olstAreas;


                }
                else
                {
                    return new List<Entities.MyEntities.Areas>();
                }
            }
            catch
            {

                return null;
            }

        }
        #endregion
        #region chart
        public static List<Entities.ExtratimeManagnment> GetExtraTimeHistoric()
        {

            try
            {


                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }


                var Result = Utils.ClaroWCF.GetExtraTimeExecuted(eEmployee.Idhrms);
                if (Result != null)

                    return Result.ToList();
                else
                    return new List<Entities.ExtratimeManagnment>();
            }
            catch
            {
                return null;
            }


        }
        #endregion
        #region AuthorizeBoss

        /// <summary>
        /// Cambia el estado de las horas extras
        /// </summary>
        /// <param name="Id">Identificador de la Hora Extra</param>
        /// <param name="State">Estado nuevo que será agregado</param>
        /// <param name="Justify">Justificación del Cambio de Estado</param>
        public static void ChangeState(int Id, int State, string Justify)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                Result = Utils.ClaroWCF.ExtraTimeChangeState(Id, State, eEmployee.Idhrms, Justify);
                if (Result != null)
                {
                    var editableItem = Data.ExtraTime.GetAllHoursAuthorizeBoss().Where(et => et.Id == Id).SingleOrDefault();
                    Data.ExtraTime.GetAllHoursAuthorizeBoss().Remove(editableItem);
                }
            }
            catch { throw new HttpException(404, "Error al modificar el registro."); }

            return;
        }

        /// <summary>
        /// Obtiene la lista de registros de horas extras de acuerdo al estado
        /// </summary>
        /// <param name="DateStart">Fecha de Inicio</param>
        /// <param name="DateEnd">Fecha de Fin</param>
        /// <param name="State">Id Estado que se quiere obtener los datos [-10 todos los registros menos los borrados]</param>
        /// <returns></returns>
        public static List<Entities.ExtraTime> GetListExtraTimeByState(string DateStart, string DateEnd, int State)
        {


            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                var Result = Utils.ClaroWCF.ExtraTimeGetListByState(DateStart, DateEnd, eEmployee.Idhrms.ToString(), State);
                if (Result != null)
                    return Result.OrderByDescending(x => x.ExecutionDate).ToList();
                else
                    return new List<Entities.ExtraTime>();
            }
            catch { return null; }
        }
        /// <summary>
        /// Obtiene la lista de registros de horas extras sin importar el estado
        /// </summary>
        /// <param name="DateStart">Fecha de Inicio</param>
        /// <param name="DateEnd">Fecha de Fin</param>
        /// <param name="State">Id Estado que se quiere obtener los datos [-10 todos los registros menos los borrados]</param>
        /// <returns></returns>
        public static List<Entities.ExtraTime> GetListExtraTimeAllState(string DateStart, string DateEnd)
        {
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                var Result = Utils.ClaroWCF.ExtraTimeGetListAllState(DateStart, DateEnd, eEmployee.Idhrms.ToString());
                if (Result != null)
                    return Result.ToList();
                else
                    return new List<Entities.ExtraTime>();
            }
            catch { return null; }
        }


        #endregion
        
        #region Consult

        public static List<Entities.ViewModels.ExtraTimeDetailView> GetAllDetailExtraTime()
        {
            string startDate, endDate;
            Entities.MyEntities.Parameters eParameter = new Entities.MyEntities.Parameters();
            eParameter = (Entities.MyEntities.Parameters)Session["sParameter"];
            startDate = eParameter.StartDate.ToShortDateString();
            endDate = eParameter.EndDate.ToShortDateString();

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            List<Entities.ViewModels.ExtraTimeDetailView> lstExtraTime = new List<Entities.ViewModels.ExtraTimeDetailView>();



            var result = Utils.ClaroWCF.GetAllDetailExtraTime(eEmployee.Idhrms.ToString(), startDate, endDate);
            if (result != null)
            {
                if (eParameter.AreaId == 0)
                {
                    lstExtraTime = result.ToList();
                }
                else
                {
                    lstExtraTime = result.Where(x => x.AreaId == eParameter.AreaId).ToList();
                }
            }

            else
            {
                lstExtraTime = new List<Entities.ViewModels.ExtraTimeDetailView>();
            }

            return lstExtraTime;
        }
        public static List<Entities.Employees> GetEmployeesConsult()
        {
            string startDate, endDate;
            Entities.MyEntities.Parameters eParameter = new Entities.MyEntities.Parameters();
            eParameter = (Entities.MyEntities.Parameters)Session["sParameter"];
            startDate = eParameter.StartDate.ToShortDateString();
            endDate = eParameter.EndDate.ToShortDateString();

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();



            var oEmployees = Utils.ClaroWCF.GetEmployeesConsult(eEmployee.Idhrms.ToString(), startDate, endDate);
            if (oEmployees != null)
            {
                if (eParameter.AreaId == 0)
                {
                    lstEmployees = oEmployees.ToList();
                }
                else
                {
                    lstEmployees = oEmployees.Where(X => X.AREAID == eParameter.AreaId).ToList();
                }
            }

            else
            {
                lstEmployees.Add(eEmployee);
            }

            return lstEmployees;
        }

        static IQueryable<Entities.Employees> SummaryModel { get { return GetEmployeesConsult().AsQueryable(); } }

        public static void GetDataRowCount(GridViewCustomBindingGetDataRowCountArgs e)
        {
            e.DataRowCount = SummaryModel
                .ApplyFilter(e.State.FilterExpression)
                .Count();
        }
        public static void GetData(GridViewCustomBindingGetDataArgs e)
        {
            e.Data = SummaryModel
                .ApplyFilter(e.State.FilterExpression)
                .ApplySorting(e.State.SortedColumns)
                .Skip(e.StartDataRowIndex)
                .Take(e.DataRowCount);
        }

        public static void GetSummaryValues(GridViewCustomBindingGetSummaryValuesArgs e)
        {
            var query = SummaryModel
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
                        list.Add(query.SummaryCountSummary(item.FieldName, summaryString));
                        break;
                }
            }
            e.Data = list;
        }

        public static object SummaryCountSummary(this IQueryable query, string fieldName, string summaryType)
        {
            if (query.Count() == 0)
                return 0;

            var parameter = Expression.Parameter(query.ElementType, string.Empty);
            var propertyInfo = query.ElementType.GetProperty(fieldName);
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);
            var propertyAccessExpression = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression expression = null;
            if (summaryType == "Min" || summaryType == "Max")
                expression = Expression.Call(typeof(Queryable), summaryType,
                    new Type[] { query.ElementType, propertyAccessExpression.Body.Type },
                    query.Expression,
                    propertyAccessExpression);
            else
                expression = Expression.Call(
                   typeof(Queryable),
                   summaryType,
                   new Type[] { query.ElementType },
                   query.Expression,
                   Expression.Quote(propertyAccessExpression)
               );
            return query.Provider.Execute(expression);
        }

        public static List<Entities.ExtraTime> GetListExtraTimeConsult()
        {
            try
            {
                Entities.MyEntities.Parameters eParameter = new Entities.MyEntities.Parameters();
                eParameter = (Entities.MyEntities.Parameters)Session["sParameter"];
                int PersonId = (int)Session["PersonId"];
                var Result = Utils.ClaroWCF.ExtraTimeGetListConsult(PersonId, eParameter.StartDate.ToShortDateString(), eParameter.EndDate.ToShortDateString());

                if (Result != null)
                    return Result.ToList();

                else
                    return new List<Entities.ExtraTime>();

            }

            catch { return null; }
        }

        static IQueryable<Entities.ExtraTime> DetailModel { get { return ExtraTime.GetListExtraTimeConsult().AsQueryable(); } }

        public static void DetailGetDataRowCount(GridViewCustomBindingGetDataRowCountArgs e)
        {
            e.DataRowCount = DetailModel
                .ApplyFilter(e.State.FilterExpression)
                .Count();
        }
        public static void DetailGetData(GridViewCustomBindingGetDataArgs e)
        {
            e.Data = DetailModel
                .ApplyFilter(e.State.FilterExpression)
                .ApplySorting(e.State.SortedColumns)
                .Skip(e.StartDataRowIndex)
                .Take(e.DataRowCount);
        }

        public static void DetailGetSummaryValues(GridViewCustomBindingGetSummaryValuesArgs e)
        {
            var query = DetailModel
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
                        list.Add(query.DetailCountSummary(item.FieldName, summaryString));
                        break;
                }
            }
            e.Data = list;
        }

        public static object DetailCountSummary(this IQueryable query, string fieldName, string summaryType)
        {
            if (query.Count() == 0)
                return 0;

            var parameter = Expression.Parameter(query.ElementType, string.Empty);
            var propertyInfo = query.ElementType.GetProperty(fieldName);
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);
            var propertyAccessExpression = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression expression = null;
            if (summaryType == "Min" || summaryType == "Max")
                expression = Expression.Call(typeof(Queryable), summaryType,
                    new Type[] { query.ElementType, propertyAccessExpression.Body.Type },
                    query.Expression,
                    propertyAccessExpression);
            else
                expression = Expression.Call(
                   typeof(Queryable),
                   summaryType,
                   new Type[] { query.ElementType },
                   query.Expression,
                   Expression.Quote(propertyAccessExpression)
               );
            return query.Provider.Execute(expression);
        }


        /*****************************************************************************************/
        /*Consult Detail Binding*/
        /*****************************************************************************************/
        // Variable para hacer consultas sobre el modelo de datos en consultar  horas extras
        static IQueryable<Entities.ViewModels.ExtraTimeDetailView> ExtraTimeDetailConsultModel { get { return GetAllDetailExtraTime().AsQueryable(); } }

        public static void GetConsultDetailDataRowCount(GridViewCustomBindingGetDataRowCountArgs e)
        {
            e.DataRowCount = ExtraTimeDetailConsultModel
                .ApplyFilter(e.State.FilterExpression)
                .Count();
        }
        public static void GetConsultDetailData(GridViewCustomBindingGetDataArgs e)
        {
            e.Data = ExtraTimeDetailConsultModel
                .ApplyFilter(e.State.FilterExpression)
                .ApplySorting(e.State.SortedColumns)
                .Skip(e.StartDataRowIndex)
                .Take(e.DataRowCount);
        }

        public static void GetConsultDetailValues(GridViewCustomBindingGetSummaryValuesArgs e)
        {
            var query = ExtraTimeDetailConsultModel
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
                        list.Add(query.ConsultDetailCountSummary(item.FieldName, summaryString));
                        break;
                }
            }
            e.Data = list;
        }

        public static object ConsultDetailCountSummary(this IQueryable query, string fieldName, string summaryType)
        {
            if (query.Count() == 0)
                return 0;

            var parameter = Expression.Parameter(query.ElementType, string.Empty);
            var propertyInfo = query.ElementType.GetProperty(fieldName);
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);
            var propertyAccessExpression = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression expression = null;
            if (summaryType == "Min" || summaryType == "Max")
                expression = Expression.Call(typeof(Queryable), summaryType,
                    new Type[] { query.ElementType, propertyAccessExpression.Body.Type },
                    query.Expression,
                    propertyAccessExpression);
            else
                expression = Expression.Call(
                   typeof(Queryable),
                   summaryType,
                   new Type[] { query.ElementType },
                   query.Expression,
                   Expression.Quote(propertyAccessExpression)
               );
            return query.Provider.Execute(expression);
        }

        #endregion
        #region Report
        // <summary>
        /// Método para cargar la lista de colaboradores con su total de horas extras para reporte
        /// </summary>
        /// <param name="OrganizationId">Id de la gerencia</param>
        /// <returns></returns>k
        public static List<Entities.MyEntities.ExtraTimeReport> GetListExtraTimeReport(long PersonId, string StartDate, string EndDate)
        {
            try
            {

                var Result = Utils.ClaroWCF.ExtraTimeGetListReport(PersonId, StartDate, EndDate);
                if (Result != null)
                {

                    return Result.ToList();
                }
                else
                {
                    return new List<Entities.MyEntities.ExtraTimeReport>();
                }
            }
            catch { return null; }

        }

        // <summary>
        /// Método para cargar detalle de hora extras
        /// </summary>
        /// <param name="OrganizationId">Id de la gerencia</param>
        /// <returns></returns>k
        public static List<Entities.MyEntities.ExtraTimeReport> GetDetailExtraTimeReport(long PersonId, string StartDate, string EndDate)
        {
            try
            {

                var Result = Utils.ClaroWCF.GetDetailReport(PersonId, StartDate, EndDate);
                if (Result != null)
                {

                    return Result.ToList();
                }
                else
                {
                    return new List<Entities.MyEntities.ExtraTimeReport>();
                }

                 
            }
            catch { return null; }


        }
        #endregion

    }

    public static class GridViewCustomBindingSummaryCache
    {
        const string CacheKey = "B08E5DF5-4D10-45C7-B4F1-C95EB2FE69C8";

        static Dictionary<string, int> Cache
        {
            get
            {
                if (Context.Items[CacheKey] == null)
                    Context.Items[CacheKey] = new Dictionary<string, int>();
                return (Dictionary<string, int>)Context.Items[CacheKey];
            }
        }
        static HttpContext Context { get { return HttpContext.Current; } }

        public static void SaveCount(string key, int count)
        {
            Cache[key] = count;
        }
        public static bool TryGetCount(string key, out int count)
        {
            count = 0;
            if (!Cache.ContainsKey(key))
                return false;
            count = Cache[key];
            return true;
        }
    }
}