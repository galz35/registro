using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using DevExpress.Data.Linq.Helpers;

namespace slnRhonline.Models
{
    public static class Habilities
    {
        static HttpSessionState Session { get { return HttpContext.Current.Session; } }

        /// <summary>
        /// Metodo para insertar datos de un conocimmiento en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void AddHability(Entities.Habilities item)
        {

            try
            {



                item.HabilityId = GetNewHabilityId();



                GetAllHabilities().Add(item);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }
        /// <summary>
        /// Metodo para eliminar los datos de un conocimiento en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void DeleteHability(Entities.Habilities item)
        {
            string Result;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                var editableItem = GetAllHabilities().Where(et => et.HabilityId == item.HabilityId).FirstOrDefault();

                if (editableItem != null)
                {
                    var sbmEmployer = Utils.ClaroWCF.GetHabilityById(editableItem.HabilityId, eEmployee.Idhrms);
                    if (sbmEmployer != null)
                    {
                        Result = Utils.ClaroWCF.DeleteHability(editableItem.HabilityId, eEmployee.Idhrms, eEmployee.EmailAddress);
                    }
                    GetAllHabilities().Remove(editableItem);
                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos de un conocimiento  en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void EditHability(Entities.Habilities item)
        {

            try
            {
                Entities.Habilities hability = GetAllHabilities().FirstOrDefault(x => x.HabilityId == item.HabilityId);
                if (hability != null)
                {
                    hability.HabilityTypeId = item.HabilityTypeId;
                    hability.ApplicationTypeId = item.ApplicationTypeId;
                    hability.AcademicLevelId = item.AcademicLevelId;

                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }

        /// <summary>
        /// Metodo para obtener la lista de habilidades técnicas
        /// </summary>
        /// <returns></returns>
        public static List<Entities.Habilities> GetAllHabilities()
        {


            List<Entities.Habilities> lstHability = Session["sEmployeeHabilities"] as List<Entities.Habilities>;

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                if (lstHability == null)
                {

                    var result = Utils.ClaroWCF.GetAllHabilities(eEmployee.Idhrms);
                    if (result != null)
                    {

                        Session["sEmployeeHabilities"] = lstHability = result.ToList();

                    }
                    else
                    {
                        Session["sEmployeeHabilities"] = lstHability = new List<Entities.Habilities>();
                    }


                }

            }
            catch (Exception e)
            {

                throw new Exception("error", e);
            }

            return lstHability;

        }


        /// <summary>
        /// Metodo para incrementar el Id
        /// </summary>
        /// <returns></returns>
        public static int GetNewHabilityId()
        {
            List<Entities.Habilities> editableId = GetAllHabilities();
            return (editableId.Count() > 0) ? editableId.Last().HabilityId + 1 : 0;
        }


        /// <summary>
        /// Metodo para insertar los datos de un conocimiento en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void InsertHability(Entities.Habilities item, long registerPersonId, String login)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.InsertHability(item, registerPersonId, login);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos de un conocimiento en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void UpdateHability(Entities.Habilities item, long updatePersonId, String login)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.UpdateHability(item, updatePersonId, login);

            }
            catch { throw new HttpException(404, "Error al actualizar el registro."); }

            return;
        }
    }
}