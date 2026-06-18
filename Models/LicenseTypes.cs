using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DevExpress.Web.Mvc;
using DevExpress.Data;
using DevExpress.Data.Linq.Helpers;
using System.Web.SessionState;
using System.Collections;
using System.Linq.Expressions;

namespace slnRhonline.Models
{
    public static class LicenseTypes
    {
        static HttpSessionState Session { get { return HttpContext.Current.Session; } }



        public static List<Entities.LicenseTypes> GetLicenseTypesCombo()
        {
            try
            {
               
                var result = Utils.ClaroWCF.GetAllLicenseTypes();

                if (result != null)
                {
                   
                    return result.ToList();

                }

                else
                {
                    return new List<Entities.LicenseTypes>();
                }
            }
            catch
            {

                return null;
            }

        }
        /// <summary>
        /// Metodo para cargar los tipos de licencia
        /// </summary>
        /// <returns></returns>
        public static List<Entities.LicenseTypes> GetLicenseTypes()
        {
            try
            {
                Entities.MyEntities.LicenseParameters eParameter = new Entities.MyEntities.LicenseParameters();
         
                Entities.ViewModels.LicenseDetailConsultView detailLicenseType = new Entities.ViewModels.LicenseDetailConsultView();
                eParameter = (Entities.MyEntities.LicenseParameters)Session["sParameterLicenseType"];
                var result = Utils.ClaroWCF.GetAllLicenseTypes();

                if (result != null)
                {
                    if (Session["sBalance"] == null)
                    {
                      
                        decimal balance = Utils.ClaroWCF.GetAllLicensesForDetailConsult(eParameter.PersonId).FirstOrDefault().Balance;
                        Session["sBalance"] = balance;
                    }
                    return result.ToList();

                }

                else
                {
                    return new List<Entities.LicenseTypes>();
                }
            }
            catch
            {

                return null;
            }

        }

        /// <summary>
        /// Metodo para cargar el detalle por tipo de licencia con el saldo
        /// </summary>
        /// <returns></returns>
        public static List<Entities.ViewModels.LicenseDetailConsultView> GetLicenseTypesDetail()
        {
            try
            {
                Entities.MyEntities.LicenseParameters eParameter = new Entities.MyEntities.LicenseParameters();
                eParameter = (Entities.MyEntities.LicenseParameters)Session["sParameterLicenseType"];
                var result = Utils.ClaroWCF.GetAllLicensesForHistoric(eParameter.PersonId,eParameter.StartDate.ToShortDateString(),eParameter.EndDate.ToShortDateString());
                if (result != null)
                {
                    return result.ToList();
                }
                else
                {
                    return new List<Entities.ViewModels.LicenseDetailConsultView>();
                }
            }
            catch
            {

                return null;
            }

        }


        
    }
}