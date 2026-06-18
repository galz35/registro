using System;
using System.Collections.Generic;
using System.Linq;

namespace slnRhonline.Models
{
    public static class ExitReasons
    {
        public static List<Entities.ExitReasons> GetAllExitReasons()
        {
            List<Entities.ExitReasons> lstExitReason = new List<Entities.ExitReasons>();
            try
            {
                var result = Utils.ClaroWCF.GetAllExitReasons();
                if (result != null)
                {
                    lstExitReason = result.ToList();
                }

            }
            catch (Exception)
            {

                throw;
            }

            return lstExitReason;
        }
    }
}