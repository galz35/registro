using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class QualificationValues
    {
        public static List<Entities.QualificationValues> GetAllQualificationValues()
        {
            List<Entities.QualificationValues> lstQualificationValues = new List<Entities.QualificationValues>();
            try
            {
                var result = Utils.ClaroWCF.GetAllQualificationValues();
                if (result != null)
                {
                    lstQualificationValues = result.ToList();
                }

            }
            catch (Exception )
            {

                throw;
            }

            return lstQualificationValues;
        }
    }
}