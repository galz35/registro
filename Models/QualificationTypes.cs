using System;
using System.Collections.Generic;
using System.Linq;


namespace slnRhonline.Models
{
    public static class QualificationTypes
    {
        public static List<Entities.QualificationTypes> GetAllQualificationTypes()
        {
            List<Entities.QualificationTypes> lstQualificationTypes = new List<Entities.QualificationTypes>();
            try
            {
                var result = Utils.ClaroWCF.GetAllQualificationTypes();
                if (result != null)
                {
                    lstQualificationTypes = result.ToList();
                }

            }
            catch (Exception)
            {

                throw;
            }

            return lstQualificationTypes;
        }

    }
}