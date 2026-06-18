using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class JobTitles
    {
        public static List<Entities.JobTitles> GetAllJobTitles()
        {
            List<Entities.JobTitles> lstJobTitle = new List<Entities.JobTitles>();
            try
            {
                var result = Utils.ClaroWCF.GetAllJobTitles();
                if (result != null)
                {
                    lstJobTitle = result.OrderBy(x=>x.JobTitleName).ToList();
                }

            }
            catch (Exception)
            {

                throw;
            }

            return lstJobTitle;
        }

    }
}