using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class ApplicationTypes
    {
        public static List<Entities.ApplicationTypes> GetAllApplicationTypes()
        {
            try
            {

                var result = Utils.ClaroWCF.GetAllApplicationTypes();
             
                if (result != null)
                {

                    return result.ToList();

                }

                else
                {
                    return new List<Entities.ApplicationTypes>();
                }
            }
            catch
            {

                return null;
            }

        }

        /// <summary>
        /// Metodo para obtener las aplicaicon segun el id del conocimiento tecnico.
        /// </summary>
        /// <param name="habilityTypeId"></param>
        /// <returns></returns>
        public static List<Entities.ApplicationTypes> GetApplicationTypesById(int habilityTypeId)
        {
            try
            {

                var result = Utils.ClaroWCF.GetAllApplicationTypes().Where(x => x.HabilityTypeId == habilityTypeId);
               
                if (result != null)
                {

                    return result.ToList();

                }

                else
                {
                    return new List<Entities.ApplicationTypes>();
                }
            }
            catch
            {

                return null;
            }

        }
    }
}