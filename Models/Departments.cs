using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class Departments
    {
        public static List<Entities.Departments> GetAllDepartments()
        {

            List<Entities.Departments> lstDepartments = new List<Entities.Departments>()
             {
                new Entities.Departments { DepartmentId = 80098, DepartmentName = "BOACO" },
                new Entities.Departments { DepartmentId = 80099, DepartmentName = "CARAZO" },
                new Entities.Departments { DepartmentId = 80100, DepartmentName = "CHINANDEGA" },
                new Entities.Departments { DepartmentId = 80102, DepartmentName = "CHONTALES" },
                new Entities.Departments { DepartmentId = 80108, DepartmentName = "ESTELI" },
                new Entities.Departments { DepartmentId = 80111, DepartmentName = "GRANADA" },
                new Entities.Departments { DepartmentId = 80114, DepartmentName = "JINOTEGA" },
                new Entities.Departments { DepartmentId = 80117, DepartmentName = "LEON" },
                new Entities.Departments { DepartmentId = 80118, DepartmentName = "MADRIZ" },
                new Entities.Departments { DepartmentId = 80119, DepartmentName = "MANAGUA" },
                new Entities.Departments { DepartmentId = 80120, DepartmentName = "MASAYA" },
                new Entities.Departments { DepartmentId = 80121, DepartmentName = "MATAGALPA" },
                new Entities.Departments { DepartmentId = 80122, DepartmentName = "NUEVA SEGOVIA" },
                new Entities.Departments { DepartmentId = 80125, DepartmentName = "RAAN" },
                new Entities.Departments { DepartmentId = 80126, DepartmentName = "RAAS" },
                new Entities.Departments { DepartmentId = 80127, DepartmentName = "RIO SAN JUAN" },
                new Entities.Departments { DepartmentId = 80128, DepartmentName = "RIVAS" },
                new Entities.Departments { DepartmentId = 124412, DepartmentName = "SIUNA" },
                new Entities.Departments { DepartmentId = 124414, DepartmentName = "ROSITA" },
                new Entities.Departments { DepartmentId = 124416, DepartmentName = "PTO. CABEZA" },
                new Entities.Departments { DepartmentId = 124418, DepartmentName = "BLUEFIEDLS" },
                new Entities.Departments { DepartmentId = 124420, DepartmentName = "SAN CARLOS" },
                new Entities.Departments { DepartmentId = 124422, DepartmentName = "RAMA" }
            };

            //List<Entities.Departments> lstDepartments = new List<Entities.Departments>();
            //try
            //{
            //    var result = Utils.ClaroWCF.GetAllDepartments();
            //    if (result != null)
            //    {
            //        lstDepartments = result.ToList();
            //    }

            //}
            //catch (Exception)
            //{

            //    throw;
            //}

            return lstDepartments;
         }

    }
}