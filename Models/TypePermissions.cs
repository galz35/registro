using System;
using System.Collections.Generic;
using System.Linq;

namespace slnRhonline.Models
{
    public static class TypePermissions
    {
        public static List<Entities.TypePermissions> GetTypePermisionById(int typeId)
        {
            try
            {
                var result = Utils.ClaroWCF.GetAllTypePermissions().Where(x => x.AbsenceAttendanceTypeId == typeId);

                if(result != null)
                {
                    return result.ToList();
                } else
                {
                    return new List<Entities.TypePermissions>();
                }
            } catch
            {
                return null;
            }
        }

        public static List<Entities.TypePermissions> GetTypePermisions()
        {
            try
            {
                var result = Utils.ClaroWCF.GetAllTypePermissions();
                //string.IsNullOrEmpty(countryName) || c.Country.CountryName == countryName
                if(result != null)
                {
                    return result.ToList();
                } else
                {
                    return new List<Entities.TypePermissions>();
                }
            } catch
            {
                return null;
            }
        }
    }
}