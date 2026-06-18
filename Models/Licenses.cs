using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using DevExpress.Data.Linq.Helpers;
using DevExpress.Web.Mvc;

namespace slnRhonline.Models
{
    public static class Licenses
    {
        static HttpSessionState Session { get { return HttpContext.Current.Session; } }

        //#region Reporte de balance por empleado
        //public static List<Entities.Employees> GetAllEmployeesBalance()
        //{
        //    List<Entities.Employees> lstLicenses = new List<Entities.Employees>();
        //    Entities.Employees eEmployee = null;
        //    if (Session["User"] != null)
        //    {
        //        eEmployee = (Entities.Employees)Session["User"];
        //    }

        //    //Entities.Employees realEmployee = new Entities.Employees();
        //    //realEmployee = Utils.ClaroWCF.GetRealBoss(eEmployee.Id_HRMS);

        //    Entities.MyEntities.LicenseParameters licenseParameter = new Entities.MyEntities.LicenseParameters();
        //    licenseParameter = (Entities.MyEntities.LicenseParameters)Session["sEmployeesBalanceParameter"];



        //    if (eEmployee.RealUserLevel > 0)
        //    {
        //        var result = Utils.ClaroWCF.GetAllEmployeesBalance(eEmployee.Id_HRMS);

        //        if (licenseParameter.PersonId > 0)
        //        {
        //            if (result != null)
        //            {

        //                lstLicenses = result.Where(x => x.Id_HRMS == licenseParameter.PersonId).ToList();
        //            }

        //        }
        //        else
        //        {
        //            lstLicenses = result.ToList();
        //        }
        //    }
        //    else
        //    {
        //        var result = Utils.ClaroWCF.GetBalanceByEmployee(eEmployee.Id_HRMS);
        //        lstLicenses = result.ToList();
        //    }

        //    return lstLicenses;

        //}
        //#endregion
        //#region Binding del listado de registro de licencias.
        //public static List<Entities.Licenses> GetAllLicensesByPersonId(int personId)
        //{

        //    List<Entities.Licenses> lstLicenses = new List<Entities.Licenses>();

        //    try
        //    {

        //        var result = Utils.ClaroWCF.GetAllLicenses(personId);//.ToList();
        //        if (result != null)
        //        {
        //            lstLicenses = result.ToList();

        //        }

        //    }
        //    catch (Exception)
        //    {

        //        return null;
        //    }

        //    return lstLicenses;

        //}
        //#endregion
        //#region Visualizar esquela
        //public static List<Entities.Licenses> GetLicenseById(int licenseId)
        //{

        //    List<Entities.Licenses> lstLicenses = new List<Entities.Licenses>();



        //    var result = Utils.ClaroWCF.GetLicenseById(licenseId);


        //    if (result != null)
        //    {

        //        lstLicenses = result.ToList();
        //    }


        //    return lstLicenses;

        //}

        //#endregion
        //#region Impresión de esquela
        //public static List<Entities.ViewModels.LicensePrintView> GetPrintLicense(int licenseId)
        //{

        //    List<Entities.ViewModels.LicensePrintView> lstLicenses = new List<Entities.ViewModels.LicensePrintView>();




        //    var result = Utils.ClaroWCF.PrintLicense(licenseId);


        //    if (result != null)
        //    {

        //        lstLicenses = result.ToList();
        //    }


        //    return lstLicenses;

        //}
        //#endregion
        //#region Estado de Cuenta
        //public static List<Entities.ViewModels.LicenseDetailConsultView> GetStatementAccount()
        //{

        //    List<Entities.ViewModels.LicenseDetailConsultView> lstLicenses = new List<Entities.ViewModels.LicenseDetailConsultView>();

        //    Entities.MyEntities.LicenseParameters licenseParameter = new Entities.MyEntities.LicenseParameters();
        //    licenseParameter = (Entities.MyEntities.LicenseParameters)Session["sLicensesConsultParameter"];
        //    //licenseParameter.StartDate = licenseParameter.StartDate.AddDays(-1);

        //    var result = Utils.ClaroWCF.GetStatementAccount(licenseParameter.PersonId, licenseParameter.StartDate.ToShortDateString(), licenseParameter.EndDate.ToShortDateString());



        //    if (result != null)
        //    {


        //        lstLicenses = result.ToList();
        //    }





        //    return lstLicenses;

        //}

        //#endregion
        //#region Adjunto de archivo de licencia
        //public static void UpdateLicenseAttached(Entities.Licenses license)
        //{
        //    string Result;
        //    try
        //    {
        //        Result = Utils.ClaroWCF.UpdateLicenseAttached(license);

        //    }
        //    //catch { throw new HttpException(404, "Error al actualizar el archivo de rendición."); }
        //    catch { throw new HttpException(404, "Error al adjuntar licencia."); }
        //    return;

        //}
        //#endregion

        //#region CRUD
        ///// <summary>
        ///// Inserta una licencia en la base de datos.
        ///// </summary>
        ///// <param name="license">Objeto de tipo Entities.Extratime, que contiene los datos a insertar.</param>
        //public static void InsertLicense(Entities.Licenses license)
        //{
        //    string Result;

        //    try
        //    {
        //        Entities.Employees eEmployee = null;

        //        if (Session["User"] != null)
        //        {
        //            eEmployee = (Entities.Employees)Session["User"];
        //        }

        //        license.StartHour = DateTime.Parse(license.StartDate.ToShortDateString() + " " + license.StartHour.ToString("hh:mm:ss tt"));
        //        license.EndHour = DateTime.Parse(license.EndDate.ToShortDateString() + " " + license.EndHour.ToString("hh:mm:ss tt"));
        //        license.HoursQuantity = license.HoursQuantity;
        //        license.StartDate = DateTime.Parse(license.StartDate.ToShortDateString());
        //        license.EndDate = DateTime.Parse(license.EndDate.ToShortDateString());
        //        license.DaysQuantity = license.DaysQuantity;
        //        license.Notes = license.Notes.ToUpper();


        //        Result = Utils.ClaroWCF.InsertLicense(license, eEmployee.Id_HRMS);

        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception("error", e);// throw new HttpException(404, "Error al insertar el registro.");
        //    }

        //    return;
        //}
        ///// <summary>
        ///// Actualiza una licencia en la base de datos.
        ///// </summary>
        ///// <param name="license">Objeto de tipo Entities.Extratime, que contiene los datos a insertar.</param>
        //public static void UpdateLicense(Entities.Licenses license)

        //{
        //    string Result;

        //    try
        //    {
        //        Entities.Employees eEmployee = null;

        //        if (Session["User"] != null)
        //        {
        //            eEmployee = (Entities.Employees)Session["User"];
        //        }

        //        license.StartHour = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + license.StartHour.ToString("hh:mm:ss tt"));
        //        license.EndHour = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + license.EndHour.ToString("hh:mm:ss tt"));
        //        license.HoursQuantity = license.HoursQuantity;
        //        license.StartDate = DateTime.Parse(license.StartDate.ToShortDateString());
        //        license.EndDate = DateTime.Parse(license.EndDate.ToShortDateString());
        //        license.DaysQuantity = license.DaysQuantity;
        //        license.Notes = license.Notes.ToUpper();




        //        Result = Utils.ClaroWCF.UpdateLicense(license, eEmployee.Id_HRMS);

        //    }

        //    catch (Exception e)
        //    {
        //        throw new Exception("Error", e);
        //    }//catch { throw new HttpException(404, "Error al actualizar el registro."); }

        //    return;
        //}

        //public static void UpdateLicenseRRHH(Entities.Licenses license)
        //{
        //    string Result;

        //    try
        //    {
        //        Entities.Employees eEmployee = null;

        //        if (Session["User"] != null)
        //        {
        //            eEmployee = (Entities.Employees)Session["User"];
        //        }

        //        Result = Utils.ClaroWCF.UpdateLicense(license, eEmployee.Id_HRMS);

        //        if (Result == "Exito al actualizar la licencia ")
        //        {
        //            var editableItem = GetAllLicensesAuthorizeRh().Where(x => x.LicenseId == license.LicenseId).FirstOrDefault();
        //            if (editableItem == null)
        //                return;

        //            editableItem.StartDate = license.StartDate;
        //            editableItem.EndDate = license.EndDate;
        //            editableItem.DaysQuantity = license.DaysQuantity;
        //            editableItem.NotesRh = license.NotesRh;
        //            editableItem.StartHour = license.StartHour;
        //            editableItem.EndHour = license.EndHour;
        //            editableItem.HoursQuantity = license.HoursQuantity;

        //        }

        //    }
        //    catch { throw new HttpException(404, "Error al actualizar el registro."); }

        //    return;
        //}
        //public static void DeleteLicense(int licenseId)
        //{
        //    string Result;
        //    try
        //    {
        //        Entities.Employees eEmployee = null;

        //        if (Session["User"] != null)
        //        {
        //            eEmployee = (Entities.Employees)Session["User"];
        //        }

        //        Result = Utils.ClaroWCF.DeleteLicense(licenseId, eEmployee.Id_HRMS);

        //    }
        //    catch { throw new HttpException(404, "Error al eliminar el registro."); }

        //    return;
        //}

        //#endregion
        //#region Autorizaciones de jefe inmediato
        //static IQueryable<Entities.Licenses> ModelBoss { get { return GetAllLicensesAuthorizeBoss().AsQueryable(); } }

        //// Conteo de registros totales
        //public static void GetListBossCount(GridViewCustomBindingGetDataRowCountArgs e)
        //{
        //    e.DataRowCount = ModelBoss.Count();
        //}

        //// Conteo de registros cuando hay filtros activdados
        //public static void GetListBossCountAdvanced(GridViewCustomBindingGetDataRowCountArgs e)
        //{
        //    int rowCount;
        //    if (GridViewCustomBindingSummaryCache.TryGetCount(e.FilterExpression, out rowCount))
        //        e.DataRowCount = rowCount;
        //    else
        //        e.DataRowCount = ModelBoss.ApplyFilter(e.FilterExpression).Count();
        //}

        //// Obtiene la lista de horas extras, filtradas y ordenadas con el custombinding
        //public static void GetListBoss(GridViewCustomBindingGetDataArgs e)
        //{
        //    e.Data = ModelBoss
        //      .ApplyFilter(e.FilterExpression)
        //      .Skip(e.StartDataRowIndex)
        //      .Take(e.DataRowCount);
        //}

        //public static List<Entities.Licenses> GetAllLicensesAuthorizeBoss()
        //{
        //    //Recuperacion de parametros.

        //    List<Entities.Licenses> lstLicenses = Session["sLicensesAuthorizeBoss"] as List<Entities.Licenses>;
        //    Entities.Employees eEmployee = null;

        //    if (Session["User"] != null)
        //    {
        //        eEmployee = (Entities.Employees)Session["User"];
        //    }

        //    if (eEmployee.UserLevel >= 2)
        //    {
        //        if (lstLicenses == null)
        //        {
        //            var result = Utils.ClaroWCF.GetAllLicensesForAuthorize(eEmployee.Id_HRMS, 1);

        //            if (result != null)
        //            {
        //                Session["sLicensesAuthorizeBoss"] = lstLicenses = result.Where(l => l.PersonId != eEmployee.Id_HRMS).ToList();

        //            }

        //            else
        //            {
        //                return new List<Entities.Licenses>();
        //            }
        //        }
        //        else
        //        {
        //            return lstLicenses;
        //        }


        //    }
        //    else
        //    {
        //        return new List<Entities.Licenses>();
        //    }

        //    return lstLicenses;

        //}
        ///// <summary>
        ///// Metodo para autorizar un registro de licencia por el jefe inmediato.
        ///// </summary>
        ///// <param name="licenseId">Objeto de tipo Entities.Extratime, que contiene los datos a insertar.</param>
        //public static void AuthorizeBoss(int licenseId)
        //{
        //    string Result;
        //    try
        //    {
        //        Entities.Employees eEmployee = null;

        //        if (Session["User"] != null)
        //        {
        //            eEmployee = (Entities.Employees)Session["User"];
        //        }

        //        Result = Utils.ClaroWCF.ChangeStateLicense(licenseId, 2, eEmployee.Id_HRMS);
        //        if (Result != null)
        //        {
        //            var editableItem = GetAllLicensesAuthorizeBoss().Where(et => et.LicenseId == licenseId).FirstOrDefault();
        //            GetAllLicensesAuthorizeBoss().Remove(editableItem);

        //        }
        //    }
        //    catch { throw new HttpException(404, "Error al autorizar el registro."); }

        //    return;
        //}
        ///// <summary>
        ///// Metdodo para denegar un registro de licencia por el jefe inmediato.
        ///// </summary>
        ///// <param name="licenseId">Objeto de tipo Entities.Extratime, que contiene los datos a insertar.</param>
        //public static void DeniedBoss(int licenseId)
        //{
        //    string Result;
        //    try
        //    {
        //        Result = Utils.ClaroWCF.ChangeStateLicense(licenseId, -2, Utils.Employee.Id_HRMS);
        //        if (Result != null)
        //        {
        //            var editableItem = GetAllLicensesAuthorizeBoss().Where(et => et.LicenseId == licenseId).FirstOrDefault();
        //            GetAllLicensesAuthorizeBoss().Remove(editableItem);
        //        }
        //    }
        //    catch { throw new HttpException(404, "Error al autorizar el registro."); }

        //    return;
        //}
        //#endregion
        #region Autorizaciones de Rh
        static IQueryable<Entities.Licenses> ModelRh { get { return GetAllLicensesAuthorizeRh().AsQueryable(); } }

        // Conteo de registros totales
        public static void GetListhCount(GridViewCustomBindingGetDataRowCountArgs e)
        {
            e.DataRowCount = ModelRh.Count();
        }

        // Conteo de registros cuando hay filtros activdados
        public static void GetListRhCountAdvanced(GridViewCustomBindingGetDataRowCountArgs e)
        {
            int rowCount;
            if (GridViewCustomBindingSummaryCache.TryGetCount(e.FilterExpression, out rowCount))
                e.DataRowCount = rowCount;
            else
                e.DataRowCount = ModelRh.ApplyFilter(e.FilterExpression).Count();
        }

        // Obtiene la lista de horas extras, filtradas y ordenadas con el custombinding
        public static void GetListRh(GridViewCustomBindingGetDataArgs e)
        {
            e.Data = ModelRh
              .ApplyFilter(e.FilterExpression)
              .Skip(e.StartDataRowIndex)
              .Take(e.DataRowCount);
        }

        public static List<Entities.Licenses> GetAllLicensesAuthorizeRh()
        {
            //Recuperacion de parametros.
            const string SessionKey = "sLicensesAuthorizeRh";
            List<Entities.Licenses> lstLicenses = HttpContext.Current.Session[SessionKey] as List<Entities.Licenses>;
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            if (eEmployee.userlevel == 6)
            {
                if (lstLicenses == null)
                {
                    var result = Utils.ClaroWCF.GetAllLicensesForAuthorize(eEmployee.Idhrms, 2);

                    if (result != null)
                    {
                        HttpContext.Current.Session[SessionKey] = lstLicenses = result.Where(x => x.PersonId != eEmployee.Idhrms).ToList();

                    }

                    else
                    {
                        return new List<Entities.Licenses>();
                    }
                }
                else
                {
                    return lstLicenses;
                }


            }
            else
            {
                return new List<Entities.Licenses>();
            }

            return lstLicenses;

        }
        /// <summary>
        /// Metodo para autorizar un registro de licencia por el jefe inmediato.
        /// </summary>
        /// <param name="licenseId">Objeto de tipo Entities.Extratime, que contiene los datos a insertar.</param>
        public static void AuthorizeRh(int licenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }


                Result = Utils.ClaroWCF.ChangeStateLicense(licenseId, 4, eEmployee.Idhrms);
                if (Result != null)
                {
                    var editableItem = GetAllLicensesAuthorizeRh().Where(et => et.LicenseId == licenseId).FirstOrDefault();
                    GetAllLicensesAuthorizeRh().Remove(editableItem);

                }
            }
            catch { throw new HttpException(404, "Error al autorizar el registro."); }

            return;
        }
        /// <summary>
        /// Metdodo para denegar un registro de licencia por el jefe inmediato.
        /// </summary>
        /// <param name="licenseId">Objeto de tipo Entities.Extratime, que contiene los datos a insertar.</param>
        public static void DeniedRh(int licenseId)
        {
            string Result;
            try
            {
                Entities.Employees eEmployee = null;

                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }


                Result = Utils.ClaroWCF.ChangeStateLicense(licenseId, -4, eEmployee.Idhrms);
                if (Result != null)
                {
                    var editableItem = GetAllLicensesAuthorizeRh().Where(et => et.LicenseId == licenseId).FirstOrDefault();
                    GetAllLicensesAuthorizeRh().Remove(editableItem);
                }
            }
            catch { throw new HttpException(404, "Error al autorizar el registro."); }

            return;
        }
        #endregion
        //#region Consult de detalle de licencias


        //static IQueryable<Entities.ViewModels.LicenseDetailConsultView> ModelConsult { get { return GetAllLicensesHistoric().AsQueryable(); } }

        //// Conteo de registros totales
        //public static void GetListConsultCount(GridViewCustomBindingGetDataRowCountArgs e)
        //{
        //    e.DataRowCount = ModelConsult.Count();
        //}

        //// Conteo de registros cuando hay filtros activdados
        //public static void GetListConsultCountAdvanced(GridViewCustomBindingGetDataRowCountArgs e)
        //{
        //    int rowCount;
        //    if (GridViewCustomBindingSummaryCache.TryGetCount(e.FilterExpression, out rowCount))
        //        e.DataRowCount = rowCount;
        //    else
        //        e.DataRowCount = ModelConsult.ApplyFilter(e.FilterExpression).Count();
        //}

        //// Obtiene la lista de horas extras, filtradas y ordenadas con el custombinding
        //public static void GetListConsult(GridViewCustomBindingGetDataArgs e)
        //{
        //    e.Data = ModelConsult
        //      .ApplyFilter(e.FilterExpression)
        //      .Skip(e.StartDataRowIndex)
        //      .Take(e.DataRowCount);
        //}

        //public static List<Entities.ViewModels.LicenseDetailConsultView> GetAllLicensesHistoric()
        //{

        //    List<Entities.ViewModels.LicenseDetailConsultView> lstLicenses = Session["sLicenseHistoric"] as List<Entities.ViewModels.LicenseDetailConsultView>;
        //    List<Entities.ViewModels.LicenseDetailConsultView> lstNewLicense = new List<Entities.ViewModels.LicenseDetailConsultView>();
        //    Entities.Employees eEmployee = null;

        //    if (Session["User"] != null)
        //    {
        //        eEmployee = (Entities.Employees)Session["User"];
        //    }
        //    Entities.Employees realEmployee = new Entities.Employees();
        //    realEmployee = Utils.ClaroWCF.GetRealBoss(eEmployee.Id_HRMS);

        //    Entities.MyEntities.LicenseParameters licenseParameter = new Entities.MyEntities.LicenseParameters();
        //    licenseParameter = (Entities.MyEntities.LicenseParameters)Session["sLicensesConsultParameter"];

        //    if (lstLicenses == null)
        //    {
        //        var result = Utils.ClaroWCF.GetAllLicensesForHistoric(eEmployee.Id_HRMS, licenseParameter.StartDate.ToShortDateString(), licenseParameter.EndDate.ToShortDateString());
        //        if (realEmployee.UserLevel > 0)
        //        {

        //            if (result != null)

        //            {
        //                int count = 0;
        //                foreach (var item in result)
        //                {
        //                    count++;
        //                    Entities.ViewModels.LicenseDetailConsultView license = new Entities.ViewModels.LicenseDetailConsultView();
        //                    license.LicenseId = count;
        //                    license.CompanyName = item.CompanyName;
        //                    license.PersonId = item.PersonId;
        //                    license.EmployeeNumber = item.EmployeeNumber;
        //                    license.FullName = item.FullName;
        //                    license.ManagementName = item.ManagementName;
        //                    license.AreaName = item.AreaName;
        //                    license.Type = item.Type;
        //                    license.StartDate = item.StartDate;
        //                    license.EndDate = item.EndDate;
        //                    license.DaysQuantity = item.DaysQuantity;
        //                    license.LicenseStatus = item.LicenseStatus;

        //                    lstNewLicense.Add(item);
        //                }

        //                if (licenseParameter.PersonId > 0)
        //                {
        //                    Session["sLicenseHistoric"] = lstLicenses = lstNewLicense.Where(x => x.PersonId == licenseParameter.PersonId).ToList();
        //                }
        //                else
        //                {
        //                    Session["sLicenseHistoric"] = lstLicenses = lstNewLicense;
        //                }


        //            }
        //            else
        //            {
        //                return new List<Entities.ViewModels.LicenseDetailConsultView>();
        //            }
        //        }
        //        else
        //        {
        //            Session["sLicenseHistoric"] = lstLicenses = result.Where(x => x.PersonId == eEmployee.Id_HRMS).ToList();
        //        }
        //    }
        //    else
        //    {
        //        return lstLicenses;
        //    }

        //    return lstLicenses;

        //}
        //#endregion
        

    }
}