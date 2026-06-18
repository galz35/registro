using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class LookupTitles
    {
        public static List<Entities.LookupTitles> GetAllTitles()
        {
            List<Entities.LookupTitles> lstTitles = new List<Entities.LookupTitles>();
            try
            {
                var result = Utils.ClaroWCF.GetAllTitles();
                if (result != null)
                {
                    lstTitles = result.ToList();
                }

            }
            catch (Exception)
            {

                throw;
            }

            return lstTitles;
        }
    }
}