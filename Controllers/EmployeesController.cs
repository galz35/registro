using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DevExpress.DashboardCommon.DataProcessing;
using DevExpress.Data;
using DevExpress.Web.Mvc;
using Entities.ViewModels;
using slnRhonline.Reports;
using DevExpress.XtraReports.Parameters;
using Entities;
using Employees = slnRhonline.Models.Employees;
using Habilities = slnRhonline.Models.Habilities;


namespace slnRhonline.Controllers

{
    [SessionExpire]
    public class EmployeesController : Controller
    {
       

        #region General Employee Data

        /// <summary>
        /// Accion que retorna la vista UpdateEmploye
        /// </summary>
        /// <returns></returns>
        public ActionResult UpdateEmployee()
        {
            Entities.Employees employee = new Entities.Employees();


            employee = Models.Employees.GetEmployeeById();


            return View("UpdateEmployee",employee);
        }


        /// <summary>
        /// Accion que manda a llamar a los metodos CRUD del modelo Employees y retorna la acción UpdateEmployee.
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult UpdateEmployeeData(Entities.Employees employee)
        {
            Entities.Employees eEmployee = null;

            try
            {
                //Validar si los datos vienen nulo



                if(Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                if (string.IsNullOrEmpty(employee.Address))
                {
                    return Json(new { status = "Error", message = "La dirección es requerida." });
                }

                if (string.IsNullOrEmpty(employee.EmergencyContact))
                {
                    return Json(new { status = "Error", message = "El contacto de emergencia es requerido." });
                }
                if (string.IsNullOrEmpty(employee.CellPhoneNumber))
                {
                    return Json(new { status = "Error", message = "El teléfono celular es requerido." });
                }
                if (string.IsNullOrEmpty(employee.EmergencyContactNumber))
                {
                    return Json(new { status = "Error", message = "El teléfono del contacto de emergencia es requerido." });
                }
                if (string.IsNullOrEmpty(employee.TelephoneNumber))
                {
                    return Json(new { status = "Error", message = "El teléfono de casa es requerido." });
                }
                if (string.IsNullOrEmpty(employee.IniserNumber))
                {
                    return Json(new { status = "Error", message = "El número de iniser es requerido." });
                }


                ///////////////////////////////////////////ACTUALIZACION DE DATOS GENERALES//////////////////////////////////////////////////////////////////

                //Obtiene informacion del SBM
                var sbm = Utils.ClaroWCF.GetEmployeeSBM(eEmployee.Idhrms);

                if(sbm != null)
                {
                    //Llama a método para actualizar la información del empleado
                    employee.Idhrms = eEmployee.Idhrms;
                    if(employee.Address != null)
                    {
                        employee.Address = employee.Address.ToUpper().Trim();
                    }
                    if(employee.EmergencyContact != null)
                    {
                        employee.EmergencyContact = employee.EmergencyContact.ToUpper();
                    }

                    Models.Employees.UpdateEmployee(employee, eEmployee.Idhrms, eEmployee.EmailAddress.Trim());
                } else
                {
                    var hmrs = Utils.ClaroWCF.GetEmployeeHMRS(eEmployee.Idhrms);
                    employee.Idhrms = eEmployee.Idhrms; 


                    employee.DateofBirth = employee.DateofBirth;

                    if(employee.FirstName != null)
                    {
                        employee.FirstName = employee.FirstName.ToUpper().Trim();
                    }
                    if(employee.MiddleName != null)
                    {
                        employee.MiddleName = employee.MiddleName.ToUpper().Trim();
                    }
                    if(employee.LastNames != null)
                    {
                        employee.LastNames = employee.LastNames.ToUpper();
                    }

                    if(employee.EmergencyContact != null)
                    {
                        employee.EmergencyContact = employee.EmergencyContact.ToUpper();
                    }

                    employee.PersonTypeId = hmrs.PersonTypeId;
                    if(employee.Address != null)
                    {
                        employee.Address = employee.Address.ToUpper().Trim();
                    }

                    employee.CompanyName = hmrs.CompanyName.ToUpper().Trim();
                    //Llama a metodo para insertar informacion de datos generales del empleado
                    Models.Employees.InsertEmployee(employee, eEmployee.Idhrms, eEmployee.EmailAddress.Trim());
                }

                ///////////////////////////////////////////ACTUALIZACION DE DATOS DE FAMILIARES//////////////////////////////////////////


                foreach(var item in Models.Employees.GetAllEmployeeFamily())
                {
                    Entities.ViewModels.EmployeeFamilyView familyView = new Entities.ViewModels.EmployeeFamilyView();

                    familyView.Id_HRMS = eEmployee.Id_HRMS;
                    familyView.FirstName = item.FirstName;
                    familyView.MiddleName = item.MiddleName;
                    familyView.LastName = item.LastName;
                    familyView.DateOfBirth = item.DateOfBirth;
                    familyView.RelationshipType = item.RelationshipType;
                    familyView.Sex = item.Sex;
                    familyView.ContactPersonId = item.ContactPersonId;
                    familyView.DocumentId = item.DocumentId;

                    var sbmFamily = Utils.ClaroWCF.GetFamilyById(item.ContactPersonId);
                    if(sbmFamily != null)
                    {
                        // es una actualizacion
                        Models.Employees.UpdateFamily(familyView, eEmployee.Idhrms, eEmployee.EmailAddress);
                    } else
                    {
                        //es una insercion
                        Models.Employees.InsertFamily(familyView, eEmployee.Idhrms, eEmployee.EmailAddress);
                    }
                }

                ///////////////////////////////////////////ACTUALIZACION DE DATOS ACADEMICOS//////////////////////////////////////////


                foreach(var item in Models.Employees.GetAllEmployeeQualification())
                {
                    Entities.Qualifications qualification = new Entities.Qualifications();

                    qualification.QualificationId = item.QualificationId;
                    qualification.PersonId = eEmployee.Idhrms;
                    qualification.QualificationTypeId = item.QualificationTypeId;
                    qualification.Title = item.Title;
                    qualification.Status = item.Status;
                    qualification.StudyCenter = item.StudyCenter;
                    qualification.StartDate = item.StartDate;
                    qualification.EndDate = item.EndDate;


                    var sbmQualification = Utils.ClaroWCF.GetQualificationById(item.QualificationId, eEmployee.Idhrms);
                    if(sbmQualification != null)
                    {
                        // es una actualizacion
                        Models.Employees.UpdateQualification(qualification, eEmployee.Idhrms, eEmployee.EmailAddress);
                    } else
                    {
                        //es una insercion
                        Models.Employees.InsertQualification(qualification, eEmployee.Idhrms, eEmployee.EmailAddress);
                    }
                }

                ///////////////////////////////////////////ACTUALIZACION DE DATOS LABORALES//////////////////////////////////////////


                foreach(var item in Models.Employees.GetAllPreviousEmployers())
                {
                    Entities.PreviousEmployers employer = new Entities.PreviousEmployers();

                    employer.PreviousEmployerId = item.PreviousEmployerId;
                    employer.PersonId = eEmployee.Idhrms;
                    employer.EmployerName = item.EmployerName;
                    employer.Description = item.Description;
                    employer.StartDate = item.StartDate;
                    employer.EndDate = item.EndDate;
                    employer.JobTitleId = item.JobTitleId;
                    employer.ExitReasonId = item.ExitReasonId;


                    var sbmEmployer = Utils.ClaroWCF.GetPreviousEmployerById(item.PreviousEmployerId, eEmployee.Idhrms);
                    if(sbmEmployer != null)
                    {
                        // es una actualizacion
                        Models.Employees.UpdatePreviousEmployer(employer, eEmployee.Idhrms, eEmployee.EmailAddress);
                    } else
                    {
                        //es una insercion
                        Models.Employees
                            .InsertPreviousEmployer(employer,
                                                    eEmployee.Idhrms,
                                                    eEmployee.EmailAddress,
                                                    eEmployee.Idhrms);
                    }
                }

                ///////////////////////////////////////////ACTUALIZACION DE CONOCIMIENTOS TECNICOS//////////////////////////////////////////
                foreach(var item in Models.Habilities.GetAllHabilities())
                {
                    Entities.Habilities hability = new Entities.Habilities();

                    hability.HabilityId = item.HabilityId;
                    hability.HabilityTypeId = item.HabilityTypeId;
                    hability.ApplicationTypeId = item.ApplicationTypeId;
                    hability.AcademicLevelId = item.AcademicLevelId;
                    hability.PersonId = eEmployee.Idhrms;


                    var result = Utils.ClaroWCF.GetHabilityById(item.HabilityId, eEmployee.Idhrms);
                    if(result != null)
                    {
                        // es una actualizacion
                        Models.Habilities.UpdateHability(hability, eEmployee.Idhrms, eEmployee.EmailAddress);
                    } else
                    {
                        //es una insercion
                        Models.Habilities.InsertHability(hability, eEmployee.Idhrms, eEmployee.EmailAddress);
                    }
                }

                ///////////////////////////////////////////ACTUALIZACION DE DATOS DE DATOS DE LOS BENEFICIARIOS//////////////////////////////////////////


                foreach (var item in Models.Employees.GetAllBeneficiaries())
                {
                    
 
                    Entities.InsuranceBeneficiaries beneficiary = new Entities.InsuranceBeneficiaries();

                    beneficiary.ContactPersonId = item.ContactPersonId;
                    beneficiary.PersonId = eEmployee.Idhrms;
                    beneficiary.Percentage = item.Percentage;
                    beneficiary.EditMode = item.EditMode;
                    beneficiary.InsuranceBeneficiareId = item.InsuranceBeneficiareId;
                    beneficiary.TutorName = item.TutorName;

                    if (beneficiary.EditMode == "U")
                    {
                        Models.Employees.UpdateBeneficiary(beneficiary);
                    }
                    else
                    {
                        Models.Employees.InsertBeneficiary(beneficiary);
                    }

                   


                }
                
                ///////////////////////////////////////////ACTUALIZACION DE DATOS DE DATOS DE LOS BENEFICIARIOS DE LA INDEMNIZACION//////////////////////////////////////////


                foreach (var item in Models.Employees.GetLegalBeneficiariesByPerson())
                {


                    Entities.LegalBeneficiaries beneficiary = new Entities.LegalBeneficiaries();

                    beneficiary.ContactPersonId = item.ContactPersonId;
                    beneficiary.PersonId = eEmployee.Idhrms;
                    beneficiary.Percentage = item.Percentage;
                    beneficiary.EditMode = item.EditMode;
                    beneficiary.LegalBeneficiaryId = item.LegalBeneficiaryId;
                    beneficiary.TutorName = item.TutorName;

                    if (beneficiary.EditMode == "U")
                    {
                        Models.Employees.UpdateLegalBeneficiary(beneficiary);
                    }
                    else
                    {
                        Models.Employees.InsertLegalBeneficiary(beneficiary);
                    }

                }
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }
            Session.Remove("sEmployeeFamily");
            Session.Remove("sEmployeeQualification");
            Session.Remove("sPreviousEmployers");
            Session.Remove("sEmployeeHabilities");
            Session.Remove("sBeneficiaries");
            Session.Remove("sLegalBeneficiaries");

            // return RedirectToAction("UpdateEmployee");
            return Json(new { status = "Exito", message = "Exito al actualizar la información." });
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ValidarTotalPorcentaje()
        {
            decimal totalSessionPercentage = Models.Employees.GetAllBeneficiaries().Sum(x => x.Percentage);
            if (totalSessionPercentage  <100)
            {
                return Json(new { status = "Error", message = "Solo se puede notificar a ININSER cuando ha completado el 100% de su seguro." });
            }
            

            decimal totalPercentageCompensation = Models.Employees.GetLegalBeneficiariesByPerson().Sum(x => x.Percentage);
            if (totalPercentageCompensation < 100)
            {
                return Json(new { status = "Error", message = "Solo se puede notificar a Nomina cuando ha completado el 100% de su seguro." });
            }
           


            return Json(new { status = "Exito", message = "A continuacion se mostrará el formulario de aceptacion de términos." });
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult InsertAcceptance()
        {
            Entities.Employees eEmployee = null;

           



                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                Entities.Acceptance  acceptance = new Acceptance();
            acceptance.PersonId = eEmployee.Idhrms;
            acceptance.AcceptanceDate = DateTime.Now;
             string resultado = Models.Employees.InsertAcceptance(acceptance);
            if (resultado.Trim() != "Exito al insertar el registro")
            {
                return Json(new { status = "Error", message = "Ocurrió un error al notificar la información" });
            }

           
             EnviarCorreo("BeneficiarioSeguro");
             EnviarCorreo("BeneficiarioCompensacion");
            return Json(new { status = "Exito", message = "Se ha notificado exitosamente a las entidades correspondientes, por favor revise su correo." });
        }

     

        public string EnviarCorreo(string tipoCorreo)
        {
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            string destinatario = string.Empty;
            string copia = string.Empty;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            if (eEmployee.EmailAddress == null)
            {
                return resultadoCorreo;
            }

           
            if (tipoCorreo == "BeneficiarioSeguro")
            {
                destinatario = eEmployee.EmailAddress;
                copia = "representante.iniser@claro.com.ni";
                titulo = "Actualizacion de beneficiarios de seguro de vida.";
                mensaje = "Estimada(o) colaborador:" +
                                  "<br/>" +
                                  "<br/>" + "Para ser efectiva la actualización de tus beneficiarios debes enviar adjunto la información siguiente:" +
                                    "<br/>" +
                                  " " + "1. Fotocopia de cedula de beneficiario." +
                                  "<br/>" +
                                  " " + "2. Fotocopia de cedula de beneficiario." +
                                  "<br/>" +
                                  " " + "3. Fotocopia de carnet de empleado." +
                                  "<br/>" +
                                  " " + "4. Fotocopia de carnet de  INISER." +
                                  "<br/>" +
                                  " " + "5. Partida de nacimiento del menor de edad." +
                                  "<br/>" +
                                  " " + "6. Cédula del tutor." +
                                  "<br/>" +
                                  " " + "7. Solicitud de Beneficiario debidamente firmado." +
                                  "<br/>" +
                                  "<br/>" +
                                  "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                                  "<br/>" +
                                  "<br/>" + "Saludos.";
            }
            if (tipoCorreo == "NuevoFamiliar")
            {
                destinatario = "solicitud.nomina@claro.com.ni";
                copia = "candida.sanchez@claro.com.ni";
                titulo = "Actualizacion de datos";
                mensaje = "Equipo de Nomina:" +
                          "<br/>" +
                          "<br/>" + "El colaborador: " + eEmployee.FullName + "acaba de actualizar sus datos personales, favor dar seguimiento" + 
                          "<br/>" +
                          "<br/>" +
                          "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                          "<br/>" +
                          "<br/>" + "Saludos.";
            }
            if (tipoCorreo == "BeneficiarioCompensacion")
            {
                destinatario = eEmployee.EmailAddress;
                copia = "solicitud.nomina@claro.com.ni";
                titulo = "Actualizacion de beneficiarios de prestaciones sociales e indemnización.";
                mensaje = "Estimada(o) colaborador:" +
                          "<br/>" +
                          "<br/>" + "Para ser efectiva la actulización de tus beneficiarios debes enviar al área de nómina la información siguiente:" +
                          "<br/>" +
                          " " + "1. Formato de beneficiarios para  prestaciones sociales e indemnización debidamente firmado." +
                          "<br/>" +
                          " " + "2. Fotocopia de cédula de beneficiarios." +
                          "<br/>" +
                          " " + "4. Partida de nacimiento del menor de edad." +
                          "<br/>" +
                          " " + "5. Cédula del tutor." +
                          "<br/>" +
                          "<br/>" +
                          "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                          "<br/>" +
                          "<br/>" + "Saludos.";
            }



            resultadoCorreo = Utils.EnviarCorreoUsuario(destinatario, titulo, copia, mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transaccion se genero exitosamente, pero ocurrió un error al enviar el correo";

            }


            return resultadoCorreo;
        }


        /// <summary>
        /// Metodo que retorna el resultado de la imagen una vez que se hace le callbackresult.
        /// </summary>
        /// <returns></returns>
        public ActionResult BinaryImageColumnPhotoUpdate()
        {
            return BinaryImageEditExtension.GetCallbackResult();
        }


        #endregion

        #region Family Employee Data


        /// <summary>
        /// Accion que retorna la vista parcial EmployeeFamilyPartial
        /// </summary>
        /// <returns></returns>
        public ActionResult EmployeeFamilyPartial()
        {
            List<Entities.ViewModels.EmployeeFamilyView> lstFamily = new List<Entities.ViewModels.EmployeeFamilyView>();

            lstFamily = Models.Employees.GetAllEmployeeFamily();

            return PartialView("EmployeeFamilyPartial", lstFamily);
        }

        /// <summary>
        /// Accion que llama al metodo AddFamily del modelo Employees para insertar un familiar
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddFamily(Entities.ViewModels.EmployeeFamilyView family)
        {
            List<Entities.ViewModels.EmployeeFamilyView> lstFamily = new List<Entities.ViewModels.EmployeeFamilyView>();
            try
            {    

                if (family.DateOfBirth == null)
                {
                    return Content("El campo fecha de nacimiento es requerido, favor corregir.");
                }
                Models.Employees.AddFamily(family);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeFamilyPartial", Employees.GetAllEmployeeFamily());
        }

        /// <summary>
        /// Accion que llama metodo EditFamily del modelo Employees para editar un familiar.
        /// </summary>
        /// <param name="eExpenseDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditFamily(Entities.ViewModels.EmployeeFamilyView family)
        {
            try
            {
                Models.Employees.EditFamily(family);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeFamilyPartial", Models.Employees.GetAllEmployeeFamily());
        }

        /// <summary>
        /// Accion que llama al metodo DeleteFamily del modelo Employees para eliminar un familiar de la sesión.
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteFamily(Entities.ViewModels.EmployeeFamilyView family)
        {
            try
            {
                Models.Employees.DeleteFamily(family);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeFamilyPartial", Models.Employees.GetAllEmployeeFamily());
        }
        #endregion
        #region Academic Employee Data.


        /// <summary>
        /// Acción que retorna la vista parcial EmployeeQualificationPartial.
        /// </summary>
        /// <returns></returns>
        public ActionResult EmployeeQualificationPartial()
        {
            List<Entities.Qualifications> lstQualification = new List<Entities.Qualifications>();

            lstQualification = Models.Employees.GetAllEmployeeQualification();

            return PartialView("EmployeeQualificationPartial", lstQualification);
        }


        /// <summary>
        /// Accion que llama al metodo AddQualification del metodo Employees para insertar datos academicos.
        /// </summary>
        /// <param name="qualification"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddQualification(Entities.Qualifications qualification)
        {
            try
            {
                if(qualification.StartDate == null)
                {
                    return Content("La fecha de inicio es requerida.");
                }
                if((qualification.EndDate == null) &&
                    ((qualification.Status == "00020") || (qualification.Status == "00050")))
                {
                    return Content("La fecha de fin es requerida cuando el estado sea titulado o completo.");
                }
                if(qualification.StartDate > qualification.EndDate)
                {
                    return Content("La fecha de inicio no puede ser mayor que la fecha de fin.");
                }


                Models.Employees.AddQualification(qualification);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeQualificationPartial", Employees.GetAllEmployeeQualification());
        }


        /// <summary>
        /// Accion que llama al metodo EditQualification del metodo Employees para editar datos academicos.
        /// </summary>
        /// <param name="qualification"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditQualification(Entities.Qualifications qualification)
        {
            try
            {
                if(qualification.StartDate == null)
                {
                    return Content("La fecha de inicio es requerida.");
                }
                if((qualification.EndDate == null) &&
                    ((qualification.Status == "00020") || (qualification.Status == "00050")))
                {
                    return Content("La fecha de fin es requerida cuando el estado sea titulado o completo.");
                }
                if(qualification.StartDate > qualification.EndDate)
                {
                    return Content("La fecha de inicio no puede ser mayor que la fecha de fin.");
                }

                Models.Employees.EditQualification(qualification);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeQualificationPartial", Models.Employees.GetAllEmployeeQualification());
        }


        /// <summary>
        /// Accion que llama al metodo DeleteQualification del metodo Employees para eliminar datos academicos.
        /// </summary>
        /// <param name="qualification"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteQualification(Entities.Qualifications qualification)
        {
            try
            {
                Models.Employees.DeleteQualification(qualification);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeQualificationPartial", Models.Employees.GetAllEmployeeQualification());
        }
        #endregion
        #region Previous Employee Data

        /// <summary>
        /// Accion que manda a llamar al metodo GetAllPreviousEmployers del modelo Employees y retorna la vista parcial 
        /// EmployeePreviousEmployersPartial
        /// </summary>
        /// <returns></returns>
        public ActionResult EmployeePreviousEmployersPartial()
        {
            List<Entities.PreviousEmployers> lstEmployers = new List<Entities.PreviousEmployers>();

            lstEmployers = Models.Employees.GetAllPreviousEmployers();

            return PartialView("EmployeePreviousEmployersPartial", lstEmployers);
        }

        /// <summary>
        /// Accion que llama al metodo AddPreviousEmployer del modelo Employees para insertar experiencia laboral del empleado.
        /// </summary>
        /// <param name="employer"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddPreviousEmployer(Entities.PreviousEmployers employer)
        {
            try
            {
                if(employer.StartDate > employer.EndDate)
                {
                    return Content("La fecha de inicio no puede ser mayor que la fecha de fin.");
                }

                Models.Employees.AddPreviousEmployer(employer);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeePreviousEmployersPartial", Employees.GetAllPreviousEmployers());
        }

        /// <summary>
        /// Accion que llama al metodo EditPreviousEmployer del modelo Employees para editar experiencia laboral del empleado.
        /// </summary>
        /// <param name="employer"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditPreviousEmployer(Entities.PreviousEmployers employer)
        {
            try
            {
                if(employer.StartDate > employer.EndDate)
                {
                    return Content("La fecha de inicio no puede ser mayor que la fecha de fin.");
                }
                Models.Employees.EditPreviousEmployer(employer);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeePreviousEmployersPartial", Models.Employees.GetAllPreviousEmployers());
        }

        /// <summary>
        /// Accion que llama al metodo DeletePreviousEmployer del modelo Employees para eliminar experiencia laboral del empleado.
        /// </summary>
        /// <param name="employer"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeletePreviousEmployer(Entities.PreviousEmployers employer)
        {
            try
            {
                Models.Employees.DeletePreviousEmployer(employer);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeePreviousEmployersPartial", Models.Employees.GetAllPreviousEmployers());
        }
        #endregion
        #region Technical Habilities

        /// <summary>
        /// Accion que llama al metodo GetAllHabilities del modelo Habilities y retorna la vista parcial 
        /// EmployeeHabilitiesPartial
        /// </summary>
        /// <returns></returns>
        public ActionResult EmployeeHabilitiesPartial()
        {
            List<Entities.Habilities> lstHability = new List<Entities.Habilities>();

            lstHability = Models.Habilities.GetAllHabilities();

            return PartialView("EmployeeHabilitiesPartial", lstHability);
        }

        /// <summary>
        /// Accion que llama al metodo AddHability del modelo Habilities para insertar habilidad tecnica del empleado.
        /// </summary>
        /// <param name="hability"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddHability(Entities.Habilities hability)
        {
            try
            {
                Models.Habilities.AddHability(hability);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeHabilitiesPartial", Habilities.GetAllHabilities());
        }

        /// <summary>
        ///Accion que llama al metodo EditHability del modelo Habilities para editar una  habilidad tecnica del empleado.
        /// </summary>
        /// <param name="eExpenseDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditHability(Entities.Habilities hability)
        {
            try
            {
                Models.Habilities.EditHability(hability);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeHabilitiesPartial", Habilities.GetAllHabilities());
        }

        /// <summary>
        /// Accion que llama al metodo DeleteHability del modelo Habilities para eliminar una habilidad tecnica del empleado.
        /// </summary>
        /// <param name="hability"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteHability(Entities.Habilities hability)
        {
            try
            {
                Models.Habilities.DeleteHability(hability);
            } catch(Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("EmployeeHabilitiesPartial", Habilities.GetAllHabilities());
        }
        #endregion
        #region Insurance Beneficiaries
        /// <summary>
        /// Accion que retorna la vista parcial EmployeeFamilyPartial
        /// </summary>
        /// <returns></returns>
        public ActionResult BeneficiariesPartial()
        {
            List<Entities.InsuranceBeneficiaries> lstBeneficiaries = new List<Entities.InsuranceBeneficiaries>();

            lstBeneficiaries = Models.Employees.GetAllBeneficiaries();

            return PartialView("BeneficiariesPartial", lstBeneficiaries);
        }

        /// <summary>
        /// Accion que llama al metodo AddFamily del modelo Employees para insertar un familiar
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddBeneficiary(Entities.InsuranceBeneficiaries beneficiary)
        {
            int edadAdulta = 18;
            try
            {
                Entities.ViewModels.EmployeeFamilyView familiar = new EmployeeFamilyView();
                familiar =
                    Models.Employees.GetAllEmployeeFamily().Where(x => x.ContactPersonId == beneficiary.ContactPersonId)
                        .FirstOrDefault();
                //int anioFechaNacimiento = familiar.DateOfBirth.Value.Year

                if (familiar.DateOfBirth == null)
                {
                    return Content("El familiar no tiene fecha de nacimiento, favor regresar a datos familiares y completar esa información.");
                }

                int edad = DateTime.Today.Year - familiar.DateOfBirth.Value.Year;

                //si el mes es menor restamos un año directamente
                if (DateTime.Today.Month < familiar.DateOfBirth.Value.Month)
                {

                    --edad;
                }
                //sino preguntamos si estamos en el mismo mes, si es el mismo preguntamos si el dia de hoy es menor al de la fecha de nacimiento
                else if (DateTime.Today.Month == familiar.DateOfBirth.Value.Month && DateTime.Today.Day < familiar.DateOfBirth.Value.Day)
                {
                    --edad;
                }

                if (edad < edadAdulta && string.IsNullOrEmpty(beneficiary.TutorName) )
                {
                    return Content("El beneficiario es menor de edad, debe escribir el nombre del tutor");
                }
                if (edad > edadAdulta && !string.IsNullOrEmpty(beneficiary.TutorName))
                {
                    return Content("El beneficiario es mayor de edad, no debe tener tutor.");
                }
                decimal totalSessionPercentage = Models.Employees.GetAllBeneficiaries().Sum(x => x.Percentage);
                decimal totalPercentage = totalSessionPercentage + beneficiary.Percentage;

                if (totalPercentage > Utils.ObtenerMaximoPorcentajeSeguro())
                {
                    return Content("El porcentaje del seguro no puede ser mayor del 100 porciento, por favor corrija.");
                }

                Models.Employees.AddBeneficiary(beneficiary);                
                
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("BeneficiariesPartial", Employees.GetAllBeneficiaries());
        }

        /// <summary>
        /// Accion que llama metodo EditFamily del modelo Employees para editar un familiar.
        /// </summary>
        /// <param name="eExpenseDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditBeneficiary(Entities.InsuranceBeneficiaries beneficiary)
        {
            try
            {
                decimal totalSessionPercentage = Models.Employees.GetAllBeneficiaries().Where(x => x.InsuranceBeneficiareId != beneficiary.InsuranceBeneficiareId).Sum(x => x.Percentage);
                decimal totalPercentage = totalSessionPercentage + beneficiary.Percentage;

                if (totalPercentage > Utils.ObtenerMaximoPorcentajeSeguro())
                {
                    return Content("El porcentaje del seguro no puede ser mayor del 100 porciento, por favor corrija.");
                }
                Models.Employees.EditBeneficiary(beneficiary);
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("BeneficiariesPartial", Employees.GetAllBeneficiaries());
        }

        /// <summary>
        /// Accion que llama al metodo DeleteFamily del modelo Employees para eliminar un familiar de la sesión.
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteBeneficiary(Entities.InsuranceBeneficiaries beneficiary)
        {
            try
            {
                Models.Employees.DeleteBeneficiary(beneficiary);
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("BeneficiariesPartial", Employees.GetAllBeneficiaries());
        }

        
   
        public ActionResult BeneficiariesReport()
        {
            List<Entities.InsuranceBeneficiaries> lstBeneficiaries = new List<Entities.InsuranceBeneficiaries>();
            List<Entities.LegalBeneficiaries> lstCompensationBeneficiaries = new List<Entities.LegalBeneficiaries>();
            List<Entities.InsuranceBeneficiaries> lstOlderBeneficiaries = new List<Entities.InsuranceBeneficiaries>();
            List<Entities.InsuranceBeneficiaries> lstYoungBeneficiaries = new List<Entities.InsuranceBeneficiaries>();
            List<Entities.Employees> lstEmployee = new List<Entities.Employees>();

            BeneficiariesReport masterReport = new BeneficiariesReport();
            OlderBeneficiaries olderReport = new OlderBeneficiaries();
            YoungBeneficiaries youngReport = new YoungBeneficiaries();
            BeneficiaryLetter beneficiaryLetterReport = new BeneficiaryLetter(); 
            AllBeneficiariesReport allBeneficiariesReport = new AllBeneficiariesReport();
            CompensationLetter compensationLetterReport = new CompensationLetter();
            AllCompensationBeneficiariesReport allCompensationBeneficiariesReport = new AllCompensationBeneficiariesReport();

            try 
            {

                    
                    Entities.Employees eEmployee = null;

                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                   
                        
                        masterReport.xrSubreport1.ReportSource = olderReport;
                        masterReport.xrSubreport2.ReportSource = youngReport;
                        masterReport.xrSubreport3.ReportSource = beneficiaryLetterReport;
                        beneficiaryLetterReport.xrSubreport1.ReportSource = allBeneficiariesReport;
                        beneficiaryLetterReport.srCompensationLetter.ReportSource = compensationLetterReport;
                      compensationLetterReport.srCompensationBeneficiaries.ReportSource = allCompensationBeneficiariesReport;


                        lstEmployee = Models.Employees.GetEmployeeToReport(eEmployee.Idhrms);
                        lstBeneficiaries = Models.Employees.GetAllBeneficiariesReport(eEmployee.Idhrms);
                        lstCompensationBeneficiaries = Models.Employees.GetAllCompensationBeneficiariesReport(eEmployee.Idhrms);
                         lstOlderBeneficiaries = lstBeneficiaries.Where(x => string.IsNullOrEmpty(x.TutorName)).ToList();
                        lstYoungBeneficiaries = lstBeneficiaries.Where(x => !string.IsNullOrEmpty(x.TutorName)).ToList();

                        olderReport.DataSource = lstBeneficiaries;
                        youngReport.DataSource = lstYoungBeneficiaries;
                        beneficiaryLetterReport.DataSource = lstEmployee;
                        allBeneficiariesReport.DataSource = lstBeneficiaries;
                        compensationLetterReport.DataSource = lstEmployee;
                         allCompensationBeneficiariesReport.DataSource = lstCompensationBeneficiaries;
                        masterReport.DataSource = lstEmployee;





            }
              catch (Exception e)
            {
                  ViewData["EditError"] = e.Message;
            }

            return View(masterReport);
            //return RedirectToAction("ParametersReport", "Expenses");// 
        }

      
        

        #endregion
        #region Legal Beneficiaries
        /// <summary>
        /// Accion que retorna la vista parcial EmployeeFamilyPartial
        /// </summary>
        /// <returns></returns>
        public ActionResult LegalBeneficiariesPartial()
        {
            List<Entities.LegalBeneficiaries> lstBeneficiaries = new List<Entities.LegalBeneficiaries>();

            lstBeneficiaries = Models.Employees.GetLegalBeneficiariesByPerson();

            return PartialView("LegalBeneficiariesPartial", lstBeneficiaries);
        }

        /// <summary>
        /// Accion que llama al metodo AddLegalBeneficiary del modelo Employees para insertar un ebeneficiario en la sesion.
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddLegalBeneficiary(Entities.LegalBeneficiaries beneficiary)
        {
            int edadAdulta = 18;
            try
            {
                Entities.ViewModels.EmployeeFamilyView familiar = new EmployeeFamilyView();
                familiar =
                    Models.Employees.GetAllEmployeeFamily().Where(x => x.ContactPersonId == beneficiary.ContactPersonId)
                        .FirstOrDefault();
                //int anioFechaNacimiento = familiar.DateOfBirth.Value.Year

                int edad = DateTime.Today.Year - familiar.DateOfBirth.Value.Year;

                //si el mes es menor restamos un año directamente
                if (DateTime.Today.Month < familiar.DateOfBirth.Value.Month)
                {

                    --edad;
                }
                //sino preguntamos si estamos en el mismo mes, si es el mismo preguntamos si el dia de hoy es menor al de la fecha de nacimiento
                else if (DateTime.Today.Month == familiar.DateOfBirth.Value.Month && DateTime.Today.Day < familiar.DateOfBirth.Value.Day)
                {
                    --edad;
                }

                if (edad < edadAdulta && string.IsNullOrEmpty(beneficiary.TutorName))
                {
                    return Content("Es un menor de edad debe escribir el nombre del tutor");
                }
                decimal totalSessionPercentage = Models.Employees.GetLegalBeneficiariesByPerson().Sum(x => x.Percentage);
                decimal totalPercentage = totalSessionPercentage + beneficiary.Percentage;

                if (totalPercentage > Utils.ObtenerMaximoPorcentajeSeguro())
                {
                    return Content("El porcentaje del seguro no puede ser mayor del 100 porciento, por favor corrija.");
                }
                Models.Employees.AddLegalBeneficiary(beneficiary);
                



            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("LegalBeneficiariesPartial", Employees.GetLegalBeneficiariesByPerson());
        }

        /// <summary>
        /// Accion que llama metodo EditFamily del modelo Employees para editar un familiar.
        /// </summary>
        /// <param name="eExpenseDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditLegalBeneficiary(Entities.LegalBeneficiaries beneficiary)
        {
            try
            {

                decimal totalSessionPercentage = Models.Employees.GetLegalBeneficiariesByPerson().Where(x=>x.LegalBeneficiaryId !=beneficiary.LegalBeneficiaryId).Sum(x => x.Percentage);
                decimal totalPercentage = totalSessionPercentage + beneficiary.Percentage;

                if (totalPercentage > Utils.ObtenerMaximoPorcentajeSeguro())
                {
                    return Content("El porcentaje del seguro no puede ser mayor del 100 porciento, por favor corrija.");
                }
                Models.Employees.EditLegalBeneficiary(beneficiary);
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("LegalBeneficiariesPartial", Employees.GetLegalBeneficiariesByPerson());
        }

        /// <summary>
        /// Accion que llama al metodo DeleteFamily del modelo Employees para eliminar un familiar de la sesión.
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteLegalBeneficiary(Entities.LegalBeneficiaries beneficiary)
        {
            try
            {
                Models.Employees.DeleteLegalBeneficiary(beneficiary);
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }


            return PartialView("LegalBeneficiariesPartial", Employees.GetLegalBeneficiariesByPerson());
        }

        #endregion
    }
}