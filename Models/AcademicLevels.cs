using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class AcademicLevels
    {
        public static List<Entities.AcademicLevels> GetAllAcademicLevels()
        {
            try
            {

                var result = Utils.ClaroWCF.GetAllAcademicLevels();

                if (result != null)
                {

                    return result.ToList();

                }

                else
                {
                    return new List<Entities.AcademicLevels>();
                }
            }
            catch
            {

                return null;
            }

        }
    }
}