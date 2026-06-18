using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class Users
    {
        public static List<Entities.ViewModels.ManagementUsersView> GetAllManagementUsers()
        {
            try
            {
                var result = Utils.ClaroWCF.GetAllManagementUsers();
                if (result != null)
                {
                    return result.ToList();
                }
                else
                {
                    return new List<Entities.ViewModels.ManagementUsersView>();
                }
            }
            catch
            {

                return null;
            }

        }
    }
}