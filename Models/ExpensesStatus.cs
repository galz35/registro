using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class ExpensesStatus
    {
        public static List<Entities.ExpensesStatus> GetAllStatus()
        {
            List<Entities.ExpensesStatus> lstStatus = new List<Entities.ExpensesStatus>();
            try
            {
                var result = Utils.ClaroWCF.GetAllExpensesStatus();
                if (result != null)
                {
                    lstStatus = lstStatus.ToList();
                }

            }
            catch (Exception)
            {

                throw;
            }

            return lstStatus;
        }
    }
}