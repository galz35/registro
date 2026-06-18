using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class HabilityTypes
    {
        public static List<Entities.HabilityTypes> GetAllHabilityTypes()
        {
            List<Entities.HabilityTypes> lstHability = new List<Entities.HabilityTypes>();
            try
            {
                var result = Utils.ClaroWCF.GetAllHabilityTypes();
                if (result != null)
                {
                    lstHability = result.ToList();
                }

            }
            catch (Exception)
            {

                throw;
            }

            return lstHability;
        }
    }
}