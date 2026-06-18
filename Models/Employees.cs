using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.SessionState;
using DevExpress.Data;
using DevExpress.Data.Filtering;
using DevExpress.Data.Linq;
using DevExpress.Data.Linq.Helpers;
using DevExpress.Web.Mvc;

namespace slnRhonline.Models
{
    public static class Employees
    {

        

        static HttpSessionState Session { get { return HttpContext.Current.Session; } }

     
        #region Binding Employees List to Licenses
        
        /// <summary>
        /// Obtiene toda la lista de empleados segun el user level real del usuario que inicia sesion
        /// </summary>
        /// <returns>List of Entities.Employees</returns>

        public static List<Entities.Employees> GetEmployeesByRealBossLicenses()
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            //Entities.Employees realEmployee = new Entities.Employees();
            //realEmployee = Utils.ClaroWCF.GetRealBoss(eEmployee.Id_HRMS);


            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();


            if ((eEmployee != null) && (Session["sRealEmployeesLicense"] == null))
            {
                if (eEmployee.RealUserLevel == 0)

                {
                    lstEmployees.Add(eEmployee);
                    Session["sRealEmployeesLicense"] = lstEmployees;
                }
                else
                {
                    var oEmployees = Utils.ClaroWCF.GetEmployeesByBossToLicense(eEmployee.Idhrms.ToString());
                    if (oEmployees != null)
                    {
                        lstEmployees = oEmployees.ToList();
                        Session["sRealEmployeesLicense"] = lstEmployees;
                    }
                    else
                    {

                        lstEmployees = new List<Entities.Employees>();
                    }
                }


            }
            else
            {
                lstEmployees = (List<Entities.Employees>)Session["sEmployeesLicense"];
            }

            return lstEmployees;
        }



        #endregion
       
        #region Datos generales del empleado
        /// <summary>
        /// Metodo para obetener la informacion de uin empleado ya sea de HMRS o SBM_NI
        /// </summary>
        /// <returns>Employee</returns>
        public static Entities.Employees GetEmployeeById()
        {
            Entities.Employees Employee = new Entities.Employees();

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            //Obtiene informacion del SBM
            var sbm = Utils.ClaroWCF.GetEmployeeSBM(eEmployee.Idhrms);

            if (sbm != null)
            {
                Employee = sbm;
            }
            else
            {
                //Obtiene informacion de HMRS
                var result = Utils.ClaroWCF.GetEmployeeHMRS(eEmployee.Idhrms);
                Employee = result;
            }


            return Employee;


        }

        public static List<Entities.Employees> GetEmployeeToReport(long personId)
        {
            List<Entities.Employees> Employee = new List<Entities.Employees>();

           

            //Obtiene informacion del SBM
            var sbm = Utils.ClaroWCF.GetEmployeeSBM(personId);
            if (sbm != null)
            {
                Employee.Add(sbm);
            }

            return Employee;


        }

        public static List<Entities.InsuranceBeneficiaries> GetAllBeneficiariesReport(long personId)
        {
            List<Entities.InsuranceBeneficiaries> lstBeneficiaries = new List<Entities.InsuranceBeneficiaries>();

           

            //Obtiene informacion del SBM
            //var result = Utils.ClaroWCF.GetAllBeneficiariesReport(personId);
            //if (result != null)
            //{
            //    lstBeneficiaries = result.ToList();
            //}

            return lstBeneficiaries;


        }
        public static List<Entities.LegalBeneficiaries> GetAllCompensationBeneficiariesReport(long personId)
        {
            List<Entities.LegalBeneficiaries> lstBeneficiaries = new List<Entities.LegalBeneficiaries>();



            ////Obtiene informacion del SBM
            //var result = Utils.ClaroWCF.GetAllCompensationReport(personId);
            //if (result != null)
            //{
            //    lstBeneficiaries = result.ToList();
            //}

            return lstBeneficiaries;


        }

        /// <summary>
        /// Metodo para insertar datos generales del empleado
        /// </summary>
        /// <param name="employee">Objeto de tipo Entities.Employees, que contiene los datos a insertar.</param>
        public static void InsertEmployee(Entities.Employees employee, long registerPersonId, String login)
        {
            string Result;

            try
            {

                Result = Utils.ClaroWCF.InsertEmployeeData(employee, registerPersonId, login);

            }
            catch (Exception e)
            {
                throw new Exception("error", e);
            }

            return;
        }



        /// <summary>
        /// Metodo para actualizar datos generales del empleado
        /// </summary>
        /// <param name="employee">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void UpdateEmployee(Entities.Employees employee, long registerPersonId, String login)
        {
            string Result;

            try
            {

                Result = Utils.ClaroWCF.UpdateEmployeeData(employee, registerPersonId, login);

            }
            catch (Exception e)
            {
                throw new Exception("error", e);
            }

            return;
        }

        /// <summary>
        /// Metodo para insertar datos generales del empleado
        /// </summary>
        /// <param name="employee">Objeto de tipo Entities.Employees, que contiene los datos a insertar.</param>
        public static string InsertAcceptance(Entities.Acceptance acceptance)
        {
            string Result;

            try
            {

                Result = Utils.ClaroWCF.InsertAcceptance(acceptance);

            }
            catch (Exception e)
            {
                throw new Exception("error", e);
            }

            return Result;
        }

        #endregion
        #region Datos familiares del empleado


        public static List<Entities.ViewModels.EmployeeFamilyView> GetAllEmployeeFamily()
        {
            //Recuperacion de parametros.

            List<Entities.ViewModels.EmployeeFamilyView> lstFamily = Session["sEmployeeFamily"] as List<Entities.ViewModels.EmployeeFamilyView>;

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            if (lstFamily == null)
            {

                var sbmFamily = Utils.ClaroWCF.GetAllEmployeeFamilySBM(eEmployee.Idhrms);
                if (sbmFamily != null)
                {

                    Session["sEmployeeFamily"] = lstFamily = sbmFamily.ToList();

                }
                else
                {
                    var hmrsFamily = Utils.ClaroWCF.GetAllEmployeeFamily(eEmployee.Idhrms);
                    if (hmrsFamily != null)
                    {

                        Session["sEmployeeFamily"] = lstFamily = hmrsFamily.ToList();
                    }
                    else
                    {

                        Session["sEmployeeFamily"] = lstFamily = new List<Entities.ViewModels.EmployeeFamilyView>();
                    }

                }
            }

            return lstFamily;

        }




        /// <summary>
        /// Metodo para insertar datos del familiar en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void AddFamily(Entities.ViewModels.EmployeeFamilyView item)
        {

            try
            {
                Entities.ViewModels.EmployeeFamilyView family = new Entities.ViewModels.EmployeeFamilyView();


                family.Id = GetNewFamilyId();

                family.DateOfBirth = item.DateOfBirth;
                family.Sex = item.Sex;
                family.RelationshipType = item.RelationshipType;
                family.DocumentId = item.DocumentId;
                family.FirstName = item.FirstName;
                family.LastName = item.LastName;
                family.MiddleName = item.MiddleName;

                if (family.DocumentId != null)
                {
                    family.DocumentId = family.DocumentId.ToUpper().ToString();
                }

                if (family.FirstName != null)
                {
                    family.FirstName = family.FirstName.ToUpper().Trim();
                }
                if (family.MiddleName != null)
                {
                    family.MiddleName = family.MiddleName.ToUpper().Trim();
                }
                if (family.LastName != null)
                {
                    family.LastName = family.LastName.ToUpper().Trim();
                }



                GetAllEmployeeFamily().Add(family);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos del familiar en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void EditFamily(Entities.ViewModels.EmployeeFamilyView item)
        {

            try
            {
                Entities.ViewModels.EmployeeFamilyView family = GetAllEmployeeFamily().FirstOrDefault(x => x.Id == item.Id);
                if (family != null)
                {

                    family.DateOfBirth = item.DateOfBirth;
                    family.Sex = item.Sex;
                    family.RelationshipType = item.RelationshipType;

                    family.DocumentId = item.DocumentId;
                    family.FirstName = item.FirstName;
                    family.LastName = item.LastName;
                    family.MiddleName = item.MiddleName;

                    if (family.DocumentId != null)
                    {
                        family.DocumentId = family.DocumentId.ToUpper().ToString();
                    }

                    if (family.FirstName != null)
                    {
                        family.FirstName = family.FirstName.ToUpper().Trim();
                    }
                    if (family.MiddleName != null)
                    {
                        family.MiddleName = family.MiddleName.ToUpper().Trim();
                    }
                    if (family.LastName != null)
                    {
                        family.LastName = family.LastName.ToUpper().Trim();
                    }
                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }
        /// <summary>
        /// Metodo para eliminar los datos del familiar en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void DeleteFamily(Entities.ViewModels.EmployeeFamilyView item)
        {
            string Result;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                var editableItem = GetAllEmployeeFamily().Where(et => et.Id == item.Id).FirstOrDefault();

                if (editableItem != null)
                {
                    var sbmFamily = Utils.ClaroWCF.GetFamilyById(editableItem.ContactPersonId);
                    if (sbmFamily != null)
                    {
                        Result = Utils.ClaroWCF.DeleteFamily(editableItem.ContactPersonId, eEmployee.Idhrms, eEmployee.EmailAddress);
                    }
                    GetAllEmployeeFamily().Remove(editableItem);
                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }


        public static int GetNewFamilyId()
        {
            List<Entities.ViewModels.EmployeeFamilyView> editableId = GetAllEmployeeFamily();
            return (editableId.Count() > 0) ? editableId.Last().Id + 1 : 0;
        }


        /// <summary>
        /// Metodo para insertar los datos del familiar en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void InsertFamily(Entities.ViewModels.EmployeeFamilyView item, long registerPersonId, String login)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.InsertFamily(item, registerPersonId, login);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos del familiar en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void UpdateFamily(Entities.ViewModels.EmployeeFamilyView item, long registerPersonId, String login)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.UpdateFamily(item, registerPersonId, login);

            }
            catch { throw new HttpException(404, "Error al actualizar el registro."); }

            return;
        }
        #endregion

        #region Datos academicos de empleado


        public static List<Entities.Qualifications> GetAllEmployeeQualification()
        {


            List<Entities.Qualifications> lstQualification = Session["sEmployeeQualification"] as List<Entities.Qualifications>;

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            if (lstQualification == null)
            {
                //Verifico si existe informacion en esquema SBM.
                var sbmQualifcation = Utils.ClaroWCF.GetAllEmployeeQualificationsSBM(eEmployee.Idhrms);
                if (sbmQualifcation != null)
                {
                    lstQualification = sbmQualifcation.ToList();
                    Session["sEmployeeQualification"] = lstQualification;

                }
                else
                {
                    //Verificar si existe informacion en esquema HMRS.
                    var hmrsQualification = Utils.ClaroWCF.GetAllEmployeeQualificationsHMRS(eEmployee.Idhrms);
                    if (hmrsQualification != null)
                    {
                        lstQualification = hmrsQualification.ToList();
                        Session["sEmployeeQualification"] = lstQualification;
                    }
                    else
                    {
                        lstQualification = new List<Entities.Qualifications>();
                        Session["sEmployeeQualification"] = lstQualification;
                    }

                }
            }

            return lstQualification;

        }


        /// <summary>
        /// Metodo para insertar datos academicos en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void AddQualification(Entities.Qualifications item)
        {

            try
            {


                item.QualificationId = GetNewQualificationId();
                if (item.Title != null)
                {
                    item.Title = item.Title.ToUpper().Trim();
                }

                if (item.StudyCenter != null)
                {
                    item.StudyCenter = item.StudyCenter.ToUpper().Trim();
                }



                GetAllEmployeeQualification().Add(item);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos academicos en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void EditQualification(Entities.Qualifications item)
        {

            try
            {
                Entities.Qualifications qualification = GetAllEmployeeQualification().FirstOrDefault(x => x.QualificationId == item.QualificationId);
                if (qualification != null)
                {
                    qualification.QualificationTypeId = item.QualificationTypeId;
                    qualification.Status = item.Status;

                    qualification.StartDate = item.StartDate;
                    qualification.EndDate = item.EndDate;
                    qualification.StudyCenter = item.StudyCenter;
                    qualification.Title = item.Title;
                    if (qualification.StudyCenter != null)
                    {
                        qualification.StudyCenter = qualification.StudyCenter.ToUpper().Trim();
                    }
                    if (qualification.Title != null)
                    {
                        qualification.Title = qualification.Title.ToUpper().Trim();
                    }
                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }
        /// <summary>
        /// Metodo para eliminar los datos academicos en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void DeleteQualification(Entities.Qualifications item)
        {
            string Result;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                var editableItem = GetAllEmployeeQualification().Where(et => et.QualificationId == item.QualificationId).FirstOrDefault();

                if (editableItem != null)
                {
                    var sbmFamily = Utils.ClaroWCF.GetQualificationById(editableItem.QualificationId, eEmployee.Idhrms);
                    if (sbmFamily != null)
                    {
                        Result = Utils.ClaroWCF.DeleteQualification(editableItem.QualificationId, eEmployee.Idhrms, eEmployee.EmailAddress);
                    }
                    GetAllEmployeeQualification().Remove(editableItem);
                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }


        public static int GetNewQualificationId()
        {
            List<Entities.Qualifications> editableId = GetAllEmployeeQualification();
            return (editableId.Count() > 0) ? editableId.Last().QualificationId + 1 : 0;
        }

        /// <summary>
        /// Metodo para insertar los datos academicos en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void InsertQualification(Entities.Qualifications item, long registerPersonId, String login)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.InsertQualification(item, registerPersonId, login);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos academicos en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void UpdateQualification(Entities.Qualifications item, long registerPersonId, String login)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.UpdateQualification(item, registerPersonId, login);

            }
            catch { throw new HttpException(404, "Error al actualizar el registro."); }

            return;
        }

        #endregion
        #region Datos de trabajos anteriores del empleado
        public static List<Entities.PreviousEmployers> GetAllPreviousEmployers()
        {


            List<Entities.PreviousEmployers> lstEmployers = Session["sPreviousEmployers"] as List<Entities.PreviousEmployers>;

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            if (lstEmployers == null)
            {
                //Verifico si existe informacion en esquema SBM.
                var sbmEmployers = Utils.ClaroWCF.GetAllPreviousEmployersSBM(eEmployee.Idhrms);
                if (sbmEmployers != null)
                {

                    Session["sPreviousEmployers"] = lstEmployers = sbmEmployers.ToList();

                }
                else
                {
                    //Verificar si existe informacion en esquema HMRS.
                    var hmrsEmployers = Utils.ClaroWCF.GetAllPreviousEmployersHMRS(eEmployee.Idhrms);
                    if (hmrsEmployers != null)
                    {

                        Session["sPreviousEmployers"] = lstEmployers = hmrsEmployers.ToList();
                    }
                    else
                    {

                        Session["sPreviousEmployers"] = lstEmployers = new List<Entities.PreviousEmployers>();
                    }

                }
            }
            return lstEmployers;

        }


        /// <summary>
        /// Metodo para insertar datos de un trabajo en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void AddPreviousEmployer(Entities.PreviousEmployers item)
        {

            try
            {



                item.PreviousEmployerId = GetNewPreviousEmployerId();
                if (item.EmployerName != null)
                {
                    item.EmployerName = item.EmployerName.ToUpper().Trim();
                }
                if (item.Description != null)
                {
                    item.Description = item.Description.ToUpper().Trim();
                }



                GetAllPreviousEmployers().Add(item);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos de un trabajo  en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void EditPreviousEmployer(Entities.PreviousEmployers item)
        {

            try
            {
                Entities.PreviousEmployers employer = GetAllPreviousEmployers().FirstOrDefault(x => x.PreviousEmployerId == item.PreviousEmployerId);
                if (employer != null)
                {


                    employer.StartDate = item.StartDate;
                    employer.EndDate = item.EndDate;
                    employer.JobTitleId = item.JobTitleId;
                    employer.ExitReasonId = item.ExitReasonId;
                    employer.EmployerName = item.EmployerName;
                    employer.Description = item.Description;
                    if (employer.EmployerName != null)
                    {
                        employer.EmployerName = employer.EmployerName.ToUpper().Trim();
                    }
                    if (employer.Description != null)
                    {
                        employer.Description = employer.Description.ToUpper().Trim();
                    }

                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }
        /// <summary>
        /// Metodo para eliminar los datos de un trabajo en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void DeletePreviousEmployer(Entities.PreviousEmployers item)
        {
            string Result;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                var editableItem = GetAllPreviousEmployers().Where(et => et.PreviousEmployerId == item.PreviousEmployerId).FirstOrDefault();

                if (editableItem != null)
                {
                    var sbmEmployer = Utils.ClaroWCF.GetPreviousEmployerById(editableItem.PreviousEmployerId, eEmployee.Idhrms);
                    if (sbmEmployer != null)
                    {
                        Result = Utils.ClaroWCF.DeletePreviousEmployer(editableItem.PreviousEmployerId, eEmployee.Idhrms, eEmployee.EmailAddress);
                    }
                    GetAllPreviousEmployers().Remove(editableItem);
                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }


        public static int GetNewPreviousEmployerId()
        {
            List<Entities.PreviousEmployers> editableId = GetAllPreviousEmployers();
            return (editableId.Count() > 0) ? editableId.Last().PreviousEmployerId + 1 : 0;
        }


        /// <summary>
        /// Metodo para insertar los datos de un trabajo en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void InsertPreviousEmployer(Entities.PreviousEmployers item, long updatePersonId, String login, long registerPersonId)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.InsertPreviousEmployer(item, updatePersonId, login, registerPersonId);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos de un trabajo en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void UpdatePreviousEmployer(Entities.PreviousEmployers item, long updatePersonId, String login)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.UpdatePreviousEmployer(item, updatePersonId, login);

            }
            catch { throw new HttpException(404, "Error al actualizar el registro."); }

            return;
        }


        #endregion

        #region Insurance Beneficiaries

        public static int GetNewBeneficiaryId()
        {
            List<Entities.InsuranceBeneficiaries> editableId = GetAllBeneficiaries();
            return (editableId.Count() > 0) ? editableId.Last().InsuranceBeneficiareId + 1 : 0;
        }

        public static List<Entities.InsuranceBeneficiaries> GetAllBeneficiaries()
        {
            //Recuperacion de parametros.

            List<Entities.InsuranceBeneficiaries> lstBeneficiaries = Session["sBeneficiaries"] as List<Entities.InsuranceBeneficiaries>;

            //Entities.Employees eEmployee = null;

            //if (Session["User"] != null)
            //{
            //    eEmployee = (Entities.Employees)Session["User"];
            //}

            //if (lstBeneficiaries == null)
            //{

            //    var beneficiary = Utils.ClaroWCF.GetAllBeneficiaries(eEmployee.Id_HRMS);
            //    if (beneficiary != null)
            //    {

            //        Session["sBeneficiaries"] = lstBeneficiaries = beneficiary.ToList();

            //    }
            //    else
            //    {
                    

            //            Session["sBeneficiaries"] = lstBeneficiaries = new List<Entities.InsuranceBeneficiaries>();
                    
            //    }
            //}

            return lstBeneficiaries;

        }

        /// <summary>
        /// Metodo para insertar datos del beneficiario en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void AddBeneficiary(Entities.InsuranceBeneficiaries item)
        {

            try
            {
                Entities.InsuranceBeneficiaries beneficiary = new Entities.InsuranceBeneficiaries();



                beneficiary.InsuranceBeneficiareId = GetNewBeneficiaryId();
                beneficiary.ContactPersonId = item.ContactPersonId;
                beneficiary.Percentage = item.Percentage;
                beneficiary.EditMode = "I";
                beneficiary.TutorName = item.TutorName;

                GetAllBeneficiaries().Add(beneficiary);

             

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos del beneficiario en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void EditBeneficiary(Entities.InsuranceBeneficiaries item)
        {
            try
            {
                Entities.InsuranceBeneficiaries beneficiary = GetAllBeneficiaries().FirstOrDefault(x => x.InsuranceBeneficiareId == item.InsuranceBeneficiareId);
                if (beneficiary != null)
                {
                    beneficiary.ContactPersonId = item.ContactPersonId;
                    beneficiary.Percentage = item.Percentage;
                    beneficiary.InsuranceBeneficiareId = item.InsuranceBeneficiareId;
                    beneficiary.TutorName = item.TutorName;

                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }
        /// <summary>
        /// Metodo para eliminar los datos del beneficiario en la sesion y en la base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void DeleteBeneficiary(Entities.InsuranceBeneficiaries item)
        {
            string result;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                var editableItem = GetAllBeneficiaries().Where(et => et.InsuranceBeneficiareId== item.InsuranceBeneficiareId).FirstOrDefault();

                if (editableItem != null)
                {
                    if (editableItem.EditMode == "U")
                    {
                        result = Utils.ClaroWCF.DeleteBeneficiary(item);
                        GetAllBeneficiaries().Remove(editableItem);
                    }
                    else
                    {
                        GetAllBeneficiaries().Remove(editableItem);
                    }

                    
                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }




        /// <summary>
        /// Metodo para insertar los datos del familiar en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void InsertBeneficiary(Entities.InsuranceBeneficiaries item)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.InsertBeneficiary(item);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos del familiar en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void UpdateBeneficiary(Entities.InsuranceBeneficiaries item)
        {
            string Result;
            try
            {
 
                Result = Utils.ClaroWCF.UpdateBeneficiary(item);

            }
            catch { throw new HttpException(404, "Error al actualizar el registro."); }

            return;
        }

        #endregion
        #region Legal Beneficiaries

        public static int GetNewLegalBeneficiaryId()
        {
            List<Entities.LegalBeneficiaries> editableId = GetLegalBeneficiariesByPerson();
            return (editableId.Count() > 0) ? editableId.Last().LegalBeneficiaryId + 1 : 0;
        }

        public static List<Entities.LegalBeneficiaries> GetLegalBeneficiariesByPerson()
        {
            //Recuperacion de parametros.

            List<Entities.LegalBeneficiaries> lstBeneficiaries = Session["sLegalBeneficiaries"] as List<Entities.LegalBeneficiaries>;

            //Entities.Employees eEmployee = null;

            //if (Session["User"] != null)
            //{
            //    eEmployee = (Entities.Employees)Session["User"];
            //}

            //if (lstBeneficiaries == null)
            //{

            //    var beneficiary = Utils.ClaroWCF.GetLegalBeneficiariesByPerson(eEmployee.Id_HRMS);
            //    if (beneficiary != null)
            //    {

            //        Session["sLegalBeneficiaries"] = lstBeneficiaries = beneficiary.ToList();

            //    }
            //    else
            //    {


            //        Session["sLegalBeneficiaries"] = lstBeneficiaries = new List<Entities.LegalBeneficiaries>();

            //    }
            //}

            return lstBeneficiaries;

        }

        /// <summary>
        /// Metodo para insertar datos del beneficiario en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void AddLegalBeneficiary(Entities.LegalBeneficiaries item)
        {

            try
            {
                Entities.LegalBeneficiaries beneficiary = new Entities.LegalBeneficiaries();



                beneficiary.LegalBeneficiaryId = GetNewBeneficiaryId();
                beneficiary.ContactPersonId = item.ContactPersonId;
                beneficiary.Percentage = item.Percentage;
                beneficiary.EditMode = "I";
                beneficiary.TutorName = item.TutorName;

                GetLegalBeneficiariesByPerson().Add(beneficiary);



            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos del beneficiario en la sesion
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void EditLegalBeneficiary(Entities.LegalBeneficiaries item)
        {
            try
            {
                Entities.LegalBeneficiaries beneficiary = GetLegalBeneficiariesByPerson().FirstOrDefault(x => x.LegalBeneficiaryId == item.LegalBeneficiaryId);
                if (beneficiary != null)
                {
                    beneficiary.ContactPersonId = item.ContactPersonId;
                    beneficiary.Percentage = item.Percentage;
                    beneficiary.LegalBeneficiaryId = item.LegalBeneficiaryId;
                    beneficiary.TutorName = item.TutorName;

                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }
        /// <summary>
        /// Metodo para eliminar los datos del beneficiario en la sesion y en la base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>
        public static void DeleteLegalBeneficiary(Entities.LegalBeneficiaries item)
        {
            string result;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                var editableItem = GetLegalBeneficiariesByPerson().Where(et => et.LegalBeneficiaryId == item.LegalBeneficiaryId).FirstOrDefault();

                if (editableItem != null)
                {
                    if (editableItem.EditMode == "U")
                    {
                        result = Utils.ClaroWCF.DeleteLegalBeneficiary(item);
                        GetLegalBeneficiariesByPerson().Remove(editableItem);
                    }
                    else
                    {
                        GetLegalBeneficiariesByPerson().Remove(editableItem);
                    }


                }

            }
            catch { throw new HttpException(404, "Error al actualizar la linea de detalle"); }

            return;
        }




        /// <summary>
        /// Metodo para insertar los datos del familiar en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void InsertLegalBeneficiary(Entities.LegalBeneficiaries item)
        {
            string Result;
            try
            {


                Result = Utils.ClaroWCF.InsertLegalBeneficiary(item);

            }
            catch { throw new HttpException(404, "Error al insertar el registro."); }

            return;
        }

        /// <summary>
        /// Metodo para actualizar los datos del familiar en la  base de datos
        /// </summary>
        /// <param name="item">Objeto de tipo Entities.Employees, que contiene los datos a actualizar.</param>

        public static void UpdateLegalBeneficiary(Entities.LegalBeneficiaries item)
        {
            string Result;
            try
            {

                Result = Utils.ClaroWCF.UpdateLegalBeneficiary(item);

            }
            catch { throw new HttpException(404, "Error al actualizar el registro."); }

            return;
        }

        #endregion





    }






}

public static class GridViewCustomOperationDataHelper
{

    static IQueryable ApplyExpression(this IQueryable query, Expression expression, ParameterExpression param)
    {
        var lambda = Expression.Lambda(expression, param);
        var callExpr = Expression.Call(typeof(Queryable), "Select", new Type[] { query.ElementType, lambda.Body.Type }, query.Expression, Expression.Quote(lambda));
        return query.Provider.CreateQuery(callExpr);
    }

    static IQueryable ApplyExpressions(this IQueryable query, IEnumerable<Expression> expressions, ParameterExpression param)
    {
        var combinedExpr = Expression.NewArrayInit(typeof(object), expressions.Select(expr => Expression.Convert(expr, typeof(object))).ToArray());
        return query.ApplyExpression(combinedExpr, param);
    }

    static IQueryable ApplyGroupInfoExpression(this IQueryable query, Type rowType)
    {
        var param = Expression.Parameter(query.ElementType, string.Empty);
        return query.ApplyExpressions(new Expression[] {
                Expression.Property(param, "Key"),
                Expression.Call(typeof(Enumerable), "Count", new Type[] { rowType }, param) },
        param);
    }

    static List<Expression> GetAggregateExpressions(Type elementType, List<GridViewSummaryItemState> summaryItems, ParameterExpression groupParam)
    {
        var list = new List<Expression>();
        var elementParam = Expression.Parameter(elementType, "elem");
        foreach (var item in summaryItems)
        {
            Expression e;
            LambdaExpression elementExpr = null;
            if (!string.IsNullOrEmpty(item.FieldName))
                elementExpr = Expression.Lambda(Converter.Convert(elementParam, new OperandProperty(item.FieldName)), elementParam);

            switch (item.SummaryType)
            {
                case SummaryItemType.Count:
                    e = Expression.Call(typeof(Enumerable), "Count", new Type[] { elementType }, groupParam);
                    break;
                case SummaryItemType.Sum:
                    e = Expression.Call(typeof(Enumerable), "Sum", new Type[] { elementType }, groupParam, elementExpr);
                    break;
                case SummaryItemType.Min:
                    e = Expression.Call(typeof(Enumerable), "Min", new Type[] { elementType }, groupParam, elementExpr);
                    break;
                case SummaryItemType.Max:
                    e = Expression.Call(typeof(Enumerable), "Max", new Type[] { elementType }, groupParam, elementExpr);
                    break;
                case SummaryItemType.Average:
                    e = Expression.Call(typeof(Enumerable), "Average", new Type[] { elementType }, groupParam, elementExpr);
                    break;
                default:
                    throw new NotSupportedException(item.SummaryType.ToString());
            }
            list.Add(e);
        }
        return list;
    }

    static object[] ToArray(this IQueryable query)
    {
        var list = new ArrayList();
        foreach (var item in query)
            list.Add(item);
        return list.ToArray();
    }

    static ICriteriaToExpressionConverter Converter { get { return new CriteriaToExpressionConverter(); } }

    public static IQueryable ApplyFilter(this IQueryable query, IList<GridViewGroupInfo> groupInfoList)
    {
        var criteria = GroupOperator.And(
            groupInfoList.Select(i => new BinaryOperator(i.FieldName, i.KeyValue, BinaryOperatorType.Equal))
        );
        return query.ApplyFilter(CriteriaOperator.ToString(criteria));
    }
    public static IQueryable ApplyFilter(this IQueryable query, string filterExpression)
    {

        if (filterExpression != null)
        {
            filterExpression.ToUpper();
        }

        return query.AppendWhere(Converter, CriteriaOperator.Parse(filterExpression));
    }

    public static IQueryable ApplySorting(this IQueryable query, IEnumerable<GridViewColumnState> sortedColumns)
    {
        ServerModeOrderDescriptor[] orderDescriptors = sortedColumns
            .Select(c => new ServerModeOrderDescriptor(new OperandProperty(c.FieldName), c.SortOrder == ColumnSortOrder.Descending))
            .ToArray();
        return query.MakeOrderBy(Converter, orderDescriptors);
    }

    public static object[] CalculateSummary(this IQueryable query, List<GridViewSummaryItemState> summaryItems)
    {
        var elementType = query.ElementType;

        query = query.MakeGroupBy(Converter, new OperandValue(0));
        var groupParam = Expression.Parameter(query.ElementType, string.Empty);

        var expressions = GetAggregateExpressions(elementType, summaryItems, groupParam);
        query = query.ApplyExpressions(expressions, groupParam);

        var groupValue = query.ToArray();
        return groupValue.Length > 0 ? groupValue[0] as object[] : new object[summaryItems.Count];
    }

    public static IEnumerable<GridViewGroupInfo> GetGroupInfo(this IQueryable query, string fieldName, ColumnSortOrder order)
    {
        var rowType = query.ElementType;
        query = query.MakeGroupBy(Converter, new OperandProperty(fieldName));
        query = query.MakeOrderBy(Converter, new ServerModeOrderDescriptor(new OperandProperty("Key"), order == ColumnSortOrder.Descending));
        query = query.ApplyGroupInfoExpression(rowType);

        var list = new List<GridViewGroupInfo>();
        foreach (var item in query)
        {
            var obj = (object[])item;
            list.Add(new GridViewGroupInfo() { KeyValue = obj[0], DataRowCount = (int)obj[1] });
        }
        return list;
    }

    public static IQueryable Select(this IQueryable query, string fieldName)
    {
        return query.MakeSelect(Converter, new OperandProperty(fieldName));
    }

    public static IQueryable UniqueValuesForField(this IQueryable query, string fieldName)
    {
        query = query.Select(fieldName);
        var expression = Expression.Call(typeof(Queryable), "Distinct", new Type[] { query.ElementType }, query.Expression);
        return query.Provider.CreateQuery(expression);
    }
}
