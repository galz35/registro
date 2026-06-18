using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static class Reasons
    {
        public static List<Entities.Reasons> GetReasons()
        {
            //try
            //{
                List<Entities.Reasons> temp = new List<Entities.Reasons>();
                temp.Add(new Entities.Reasons{ReasonId=1,ReasonName= "ADMINISTRATIVA" });
                temp.Add(new Entities.Reasons { ReasonId = 2, ReasonName = "CAPACITACION" });
                temp.Add(new Entities.Reasons { ReasonId = 3, ReasonName = "FLOTA VEHICULAR" });
                temp.Add(new Entities.Reasons { ReasonId = 4, ReasonName = "TRABAJO FUERA DE SEDE" });
                temp.Add(new Entities.Reasons { ReasonId = 5, ReasonName = "MANTENIMIENTO" });
                temp.Add(new Entities.Reasons { ReasonId = 6, ReasonName = "REPARACION" });
                temp.Add(new Entities.Reasons { ReasonId = 7, ReasonName = "CLIENTES VIP" });
                temp.Add(new Entities.Reasons { ReasonId = 8, ReasonName = "LINEA FIJA" });
                temp.Add(new Entities.Reasons { ReasonId = 9, ReasonName = "JORNADA EXTRAORDINARIA" });
                temp.Add(new Entities.Reasons { ReasonId = 10, ReasonName = "SUPERVISION" });
            return temp;
      

            //    var lstReasons = Utils.ClaroWCF.ReasonsGetReasonsList();
                //if (lstReasons != null)
                //{
                //    return lstReasons.ToList();
                //}
                //else
                //{
                //    return new List<Entities.Reasons>();
                //}
            //}
            //catch
            //{

            //    return null;
            //}

        }
    }
}