using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using DevExpress.XtraPrinting;
using RestSharp;
using slnRhonline.Models;
using slnRhonline.Reports;

namespace slnRhonline.Controllers
{
    public class GratefulnessController : Controller
    {
        public const string UploadDirectory = "~/Content/Images/";
  

        // <summary>
        /// Metodo que devuelve la lista de consumo por persona a la vista primaria
        /// </summary>
        /// <returns></returns>
        [Authorize]

        public ActionResult List()
        {
            List<Entities.ViewModels.GratefulnessView> lstGratefulness = new List<Entities.ViewModels.GratefulnessView>();
           
            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                if (eEmployee.EmailAddress.Contains("claro.com.ni")==true)
                {
                    lstGratefulness = Data.Gratefulness.GetAllGratefulnessByPerson(eEmployee.Domain, eEmployee.Idhrms+"", eEmployee.RealUserLevel);

                }
                else
                lstGratefulness = Data.Gratefulness.GetAllGratefulnessByPerson(eEmployee.Domain,eEmployee.Id_HRMS+"",eEmployee.RealUserLevel);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


            return View("List", lstGratefulness);

        }
        public ActionResult EmpleadosV2025VJson()
        {
            var empleados = slnRhonline.Data.Employee.GetAllEmpleadosV2025V();
            return Json(empleados, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Mantenimiento()
        {
            var empleados = slnRhonline.Data.Employee.GetAllEmpleadosV2025V();
            return View(empleados); // Pasas la lista al modelo de la vista
        }
        public JsonResult Listjson()
        {
            List<Entities.ViewModels.GratefulnessView> lstGratefulness = new List<Entities.ViewModels.GratefulnessView>();

            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }


                if (eEmployee.EmailAddress.Contains("claro.com.ni") == true)
                {
                
                    lstGratefulness = Data.Gratefulness.GetAllGratefulnessByPerson(eEmployee.Domain, eEmployee.Idhrms+"", eEmployee.RealUserLevel);

                }
                else
                    lstGratefulness = Data.Gratefulness.GetAllGratefulnessByPerson(eEmployee.Domain, eEmployee.Id_HRMS + "", eEmployee.RealUserLevel);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return Json(new { data = lstGratefulness }, JsonRequestBehavior.AllowGet);

 
        }
        public ActionResult ListPartial()

        {

            List<Entities.ViewModels.GratefulnessView> lstGratefulness = new List<Entities.ViewModels.GratefulnessView>();

            try
            {
              
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }


                if (eEmployee.EmailAddress.Contains("claro.com.ni") == true)
                {
                    lstGratefulness = Data.Gratefulness.GetAllGratefulnessByPerson(eEmployee.Domain, eEmployee.Idhrms+"", eEmployee.RealUserLevel);

                }
                else
                    lstGratefulness = Data.Gratefulness.GetAllGratefulnessByPerson(eEmployee.Domain, eEmployee.Id_HRMS + "", eEmployee.RealUserLevel);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


            return PartialView("ListPartial", lstGratefulness);
        }

        [HttpPost]
        public JsonResult ValidateGratefulness(string gratefulnessId, string tipoEdicion)
        {

            //string result = String.Empty;

            //if (tipoEdicion != "Nuevo")
            //{
            //    var estado = Data.Consumo.ObtenerConsumoPorId(int.Parse(idConsumo));

            //    if (estado != null)
            //    {
            //        string estadoTraslado = estado.Estado;
            //        if (estadoTraslado != "1501")
            //        {

            //            return Json(new { status = "Error", message = "Solo se pueden editar o eliminar consumos en estado REGISTRADO" });
            //        }
            //    }
            //}

            return Json(new { status = "Exito", message = "El extra plan ha sido eliminado" });
        }
        /// <summary>
        /// Accion que carga la vista de edicion de agradeciento por empleado
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        public ActionResult EditGratefulness(int gratefulnessId = -1)
        {

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Entities.Agradecimiento gratefulness =
                Data.Gratefulness.GetGratefulnessById(gratefulnessId);
            if (gratefulness.GratefulnessId == 0)
            {
                DateTime fechaActual = DateTime.Today;
                gratefulness = new Entities.Agradecimiento();
                gratefulness.GratefulnessId = -1;
                gratefulness.SendPersonId = eEmployee.Id_HRMS;
                gratefulness.GratefulnessDate = fechaActual;
                gratefulness.GratefulnessTypeId = 1;

            }

            return View("Edit", gratefulness);
        }

        /// <summary>
        /// Accion que guarda el agradecimiento de un empleado
        /// </summary>
        /// <param name="consumo"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveGratefulness(Entities.Agradecimiento gratefulness)
        {
            try
            {
                if (Session["User"] == null)
                {
                    return Json(new { status = "Error", message = "Usuario no identificado." });
                }
                Entities.Employees eEmployee = (Entities.Employees)Session["User"];

                // Validaciones previas
                if (gratefulness.FormatTypeId == 0)
                    return Json(new { status = "Error", message = "El tipo de formato es requerido." });

                if (string.IsNullOrEmpty(gratefulness.Message) || gratefulness.Message.Length > 400)
                    return Json(new { status = "Error", message = "El mensaje es obligatorio y debe tener menos de 400 caracteres." });

                if (gratefulness.ReceivePersonId == eEmployee.Id_HRMS)
                    return Json(new { status = "Error", message = "No puedes enviarte un agradecimiento a ti mismo." });

                if (gratefulness.ReceivePersonId == 0)
                    return Json(new { status = "Error", message = "El destinatario es requerido." });

                if (gratefulness.GratefulnessTypeId == 0)
                    return Json(new { status = "Error", message = "El tipo de mensaje es requerido." });

                gratefulness.GratefulnessDate = DateTime.Now;

                // Obtener nombre del destinatario si no está asignado
                if (string.IsNullOrEmpty(gratefulness.DestinataryName) && Session["sEmployeestodoempleado"] != null)
                {
                    var empleados = (List<Entities.empleadoagradecimiento>)Session["sEmployeestodoempleado"];
                    var destinatario = empleados.FirstOrDefault(x => x.Id_HRMS == gratefulness.ReceivePersonId);
                    if (destinatario != null)
                        gratefulness.DestinataryName = destinatario.FullName;
                }

                // **Generar imagen antes de insertar el agradecimiento**
                MemoryStream ms = new MemoryStream();
                try
                {
                    ImageExportOptions imageOptions = new ImageExportOptions
                    {
                        ExportMode = ImageExportMode.SingleFile,
                        Format = ImageFormat.Jpeg,
                        Resolution = 100,
                        PageRange = "1"
                    };

                    Entities.ViewModels.GratefulnessView gratefulnessData = new Entities.ViewModels.GratefulnessView
                    {
                        DestinataryName = gratefulness.DestinataryName,
                        GratefulnessTypeName = gratefulness.GratefulnessTypeId == 1 ? "EXCELENTE ATENCIÓN" :
                                               gratefulness.GratefulnessTypeId == 2 ? "EXCELENTE SERVICIO" : "EXCELENTE TRABAJO",
                        Message = gratefulness.Message,
                        SendPersonName = eEmployee.FullName,
                        GratefulnessDate = gratefulness.GratefulnessDate
                    };

                    List<Entities.ViewModels.GratefulnessView> gratefulnessReport = new List<Entities.ViewModels.GratefulnessView> { gratefulnessData };

                    if (gratefulness.FormatTypeId == 1)
                    {
                        Agradecimiento reporte = new Agradecimiento { DataSource = gratefulnessReport };
                        reporte.ExportToImage(ms, imageOptions);
                        reporte.Dispose();
                    }
                    else if (gratefulness.FormatTypeId == 2)
                    {
                        Agradecimiento2 reporte2 = new Agradecimiento2 { DataSource = gratefulnessReport };
                        reporte2.ExportToImage(ms, imageOptions);
                        reporte2.Dispose();
                    }

                    gratefulness.Image = ms.ToArray();
                    ms.Dispose();
                }
                catch (Exception ex)
                {
                    return Json(new { status = "Error", message = "Error al generar la imagen: " + ex.Message });
                }

                // **Insertar agradecimiento después de haber generado la imagen**
                gratefulness.StatusId = 2;
                if (eEmployee.Domain=="CLARONI")
                    gratefulness.SendPersonId = eEmployee.Idhrms;

                else
                    gratefulness.SendPersonId = eEmployee.Id_HRMS;
                gratefulness.Message = gratefulness.Message.Trim();
                gratefulness.DestinataryName = gratefulness.DestinataryName.Trim();

                string result = Data.Gratefulness.InsertGratefulness(gratefulness);
                if (!Data.Appointment.isNumeric(result))
                {
                    return Json(new { status = "Error", message = "Error al registrar el agradecimiento." });
                }

                // Asignar el ID insertado
                gratefulness.GratefulnessId = int.Parse(result);

                // **Actualizar agradecimiento con la imagen**
                try
                {
                    Data.Gratefulness.UpdateGratefulness(gratefulness);
                }
                catch (Exception ex)
                {
                    return Json(new { status = "Error", message = "Error al guardar la imagen en el agradecimiento: " + ex.Message });
                }

                // **Enviar notificación por correo**
                try
                {
                    var empleados = (List<Entities.empleadoagradecimiento>)Session["sEmployeestodoempleado"];
                    var destinatario = empleados.FirstOrDefault(x => x.Id_HRMS == gratefulness.ReceivePersonId);

                    string destinatarioCorreo = destinatario?.Email_Address;
                    string copiaCorreo = eEmployee.EmailAddress;

                    string titulo = gratefulness.GratefulnessTypeId == 1 ? "¡Excelente Atención!" :
                                    gratefulness.GratefulnessTypeId == 2 ? "¡Excelente Servicio!" : "¡Excelente Trabajo!";

                    string fileName = $"Formato{gratefulness.FormatTypeId}-{gratefulness.GratefulnessId}.JPEG";

                    Utils.ClaroWCF.EnviarCorreootros(destinatarioCorreo, titulo, copiaCorreo, fileName, gratefulness.Image);
                }
                catch (Exception ex)
                {
                    return Json(new { status = "Error", message = "Error al enviar el correo: " + ex.Message });
                }

                return Json(new { status = "Éxito", message = "El agradecimiento ha sido guardado y enviado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = "Ocurrió un error en la transacción: " + ex.Message });
            }

        }
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveGratefulnessxx(Entities.Agradecimiento gratefulness)
        {
            try
            {
                if (Session["User"] == null)
                {
                    return Json(new { status = "Error", message = "Usuario no identificado." });
                }
                Entities.Employees eEmployee = (Entities.Employees)Session["User"];

                // Validaciones previas
                if (gratefulness.FormatTypeId == 0)
                    return Json(new { status = "Error", message = "El tipo de formato es requerido." });

                if (string.IsNullOrEmpty(gratefulness.Message) || gratefulness.Message.Length > 400)
                    return Json(new { status = "Error", message = "El mensaje es obligatorio y debe tener menos de 400 caracteres." });

                if (gratefulness.ReceivePersonId == eEmployee.Id_HRMS)
                    return Json(new { status = "Error", message = "No puedes enviarte un agradecimiento a ti mismo." });

                if (gratefulness.ReceivePersonId == 0)
                    return Json(new { status = "Error", message = "El destinatario es requerido." });

                if (gratefulness.GratefulnessTypeId == 0)
                    return Json(new { status = "Error", message = "El tipo de mensaje es requerido." });

                gratefulness.GratefulnessDate = DateTime.Now;

                // Obtener nombre del destinatario si no está asignado
                if (string.IsNullOrEmpty(gratefulness.DestinataryName) && Session["sEmployeestodoempleado"] != null)
                {
                    var empleados = (List<Entities.empleadoagradecimiento>)Session["sEmployeestodoempleado"];
                    var destinatario = empleados.FirstOrDefault(x => x.Id_HRMS == gratefulness.ReceivePersonId);
                    if (destinatario != null)
                        gratefulness.DestinataryName = destinatario.FullName;
                }

                // **Generar imagen antes de insertar el agradecimiento**
                MemoryStream ms = new MemoryStream();
                try
                {
                    ImageExportOptions imageOptions = new ImageExportOptions
                    {
                        ExportMode = ImageExportMode.SingleFile,
                        Format = ImageFormat.Jpeg,
                        Resolution = 100,
                        PageRange = "1"
                    };

                    Entities.ViewModels.GratefulnessView gratefulnessData = new Entities.ViewModels.GratefulnessView
                    {
                        DestinataryName = gratefulness.DestinataryName,
                        GratefulnessTypeName = gratefulness.GratefulnessTypeId == 1 ? "EXCELENTE ATENCIÓN" :
                                               gratefulness.GratefulnessTypeId == 2 ? "EXCELENTE SERVICIO" : "EXCELENTE TRABAJO",
                        Message = gratefulness.Message,
                        SendPersonName = eEmployee.FullName,
                        GratefulnessDate = gratefulness.GratefulnessDate
                    };

                    List<Entities.ViewModels.GratefulnessView> gratefulnessReport = new List<Entities.ViewModels.GratefulnessView> { gratefulnessData };

                    if (gratefulness.FormatTypeId == 1)
                    {
                        Agradecimiento reporte = new Agradecimiento { DataSource = gratefulnessReport };
                        reporte.ExportToImage(ms, imageOptions);
                        reporte.Dispose();
                    }
                    else if (gratefulness.FormatTypeId == 2)
                    {
                        Agradecimiento2 reporte2 = new Agradecimiento2 { DataSource = gratefulnessReport };
                        reporte2.ExportToImage(ms, imageOptions);
                        reporte2.Dispose();
                    }

                    gratefulness.Image = ms.ToArray();
                    ms.Dispose();
                }
                catch (Exception ex)
                {
                    return Json(new { status = "Error", message = "Error al generar la imagen: " + ex.Message });
                }

                // **Insertar agradecimiento después de haber generado la imagen**
                gratefulness.StatusId = 2;
                gratefulness.SendPersonId = eEmployee.Id_HRMS;
                gratefulness.Message = gratefulness.Message.Trim();
                gratefulness.DestinataryName = gratefulness.DestinataryName.Trim();

                string result = Data.Gratefulness.InsertGratefulness(gratefulness);
                if (!Data.Appointment.isNumeric(result))
                {
                    return Json(new { status = "Error", message = "Error al registrar el agradecimiento." });
                }

                // Asignar el ID insertado
                gratefulness.GratefulnessId = int.Parse(result);

                // **Actualizar agradecimiento con la imagen**
                try
                {
                    Data.Gratefulness.UpdateGratefulness(gratefulness);
                }
                catch (Exception ex)
                {
                    return Json(new { status = "Error", message = "Error al guardar la imagen en el agradecimiento: " + ex.Message });
                }

                // **Enviar notificación por correo**
                try
                {
                    var empleados = (List<Entities.empleadoagradecimiento>)Session["sEmployeestodoempleado"];
                    var destinatario = empleados.FirstOrDefault(x => x.Id_HRMS == gratefulness.ReceivePersonId);

                    string destinatarioCorreo = destinatario?.Email_Address;
                    string copiaCorreo = eEmployee.EmailAddress;

                    string titulo = gratefulness.GratefulnessTypeId == 1 ? "¡Excelente Atención!" :
                                    gratefulness.GratefulnessTypeId == 2 ? "¡Excelente Servicio!" : "¡Excelente Trabajo!";

                    string fileName = $"Formato{gratefulness.FormatTypeId}-{gratefulness.GratefulnessId}.JPEG";

                    Utils.ClaroWCF.EnviarCorreootros(destinatarioCorreo, titulo, copiaCorreo, fileName, gratefulness.Image);
                }
                catch (Exception ex)
                {
                    return Json(new { status = "Error", message = "Error al enviar el correo: " + ex.Message });
                }

                return Json(new { status = "Éxito", message = "El agradecimiento ha sido guardado y enviado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = "Ocurrió un error en la transacción: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveGratefulnessx(Entities.Agradecimiento gratefulness)
        {
            string result = string.Empty;

            string resultSaveCard = string.Empty;

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                if (gratefulness.FormatTypeId == 0)
                {
                    return Json(new { status = "Error", message = "El tipo de formato es requerido, por favor corrija." });
                }
                gratefulness.GratefulnessDate = DateTime.Now;
                if ((gratefulness.GratefulnessDate == null) || (gratefulness.GratefulnessDate == default(DateTime)))
                {
                    return Json(new { status = "Error", message = "La fecha  es requerida,por favor corrija." });
                }

                if (string.IsNullOrEmpty(gratefulness.Message))
                {
                    return Json(new { status = "Error", message = "El mensaje es requerido, por favor corrija." });
                }
                if (gratefulness.Message.Length > 400)
                {
                    return Json(new { status = "Error", message = "El máximo de caracteres permitidos en el mensaje es de 200, por favor corrija." });
                }
                if (gratefulness.ReceivePersonId == eEmployee.Id_HRMS)
                {
                    return Json(new { status = "Error", message = "El remitente es igual al destinatario, por favor corrija." });
                }


                if (string.IsNullOrEmpty(gratefulness.DestinataryName))
                {
                    List<Entities.empleadoagradecimiento> lstEmpleado = new List<Entities.empleadoagradecimiento>();
                    if (Session["sEmployeestodoempleado"] != null)
                    {
                        lstEmpleado = (List<Entities.empleadoagradecimiento>)Session["sEmployeestodoempleado"];
                        gratefulness.DestinataryName = lstEmpleado.Where(x => x.Id_HRMS == gratefulness.ReceivePersonId).FirstOrDefault().FullName;
                    }
                    //return Json(new { status = "Error", message = "El nombre del destinatario es requerido,por favor corrija" });
                }

                if (gratefulness.ReceivePersonId == 0)
                {
                    return Json(new { status = "Error", message = "La persona que recibe el mensaje es requerida,por favor corrij." });
                }

                if (gratefulness.GratefulnessTypeId == 0)
                {
                    return Json(new { status = "Error", message = "El tipo de mensaje es requerido, por favor corrij" });
                }



                //Verificar ssi es insercion o actualización.

                if (gratefulness.GratefulnessId == 0)
                {

                    gratefulness.StatusId = 2;
                    gratefulness.SendPersonId = eEmployee.Id_HRMS;
                    gratefulness.Message = gratefulness.Message.Trim();
                    gratefulness.DestinataryName = gratefulness.DestinataryName.Trim();
                    result = Data.Gratefulness.InsertGratefulness(gratefulness);
                    if (!Data.Appointment.isNumeric(result))
                    {

                        return Json(new { status = "Error", message = "Error al regitrar el agradecimiento" });
                    }
                    else
                    {
                        List<Entities.empleadoagradecimiento> lstEmpleadox = new List<Entities.empleadoagradecimiento>();
                        if (Session["sEmployeestodoempleado"] != null)
                        {
                            lstEmpleadox = (List<Entities.empleadoagradecimiento>)Session["sEmployeestodoempleado"];
                        }
                        //var destinatary = Data.Employee.GetEmployeeById(gratefulness.ReceivePersonId).FirstOrDefault();
                        var destinatary = lstEmpleadox.Where(x => x.Id_HRMS == gratefulness.ReceivePersonId).FirstOrDefault();
                        if (!string.IsNullOrEmpty(destinatary.Domain))
                        {
                            if ((destinatary.Domain != "CLARONI") && (destinatary.Domain != "TELECOM1"))
                            {
                                string usuarioCodigoRecibe = destinatary.NoEmpleado.Split('-').Last();
                                string resultadoGuate = Data.Gratefulness.InsertGratefulnessGuate(gratefulness.GratefulnessTypeId.ToString(), int.Parse(usuarioCodigoRecibe), gratefulness.GratefulnessDate.ToShortDateString(), "PROGRAMA DE RECONOCIMIENTO", result, gratefulness.SendPersonId.ToString());
                            }




                        }

                        //var Destinatary = Data.Employee.GetAllEmployees().Where(x => x.Id_HRMS == gratefulness.ReceivePersonId).FirstOrDefault();



                        gratefulness.GratefulnessId = int.Parse(result.ToString());
                        string resultado = string.Empty;
                        MemoryStream ms = new MemoryStream();
                        List<Entities.ViewModels.GratefulnessView> gratefulnessReport = new List<Entities.ViewModels.GratefulnessView>();
                        string fileName = string.Empty;

                        //Parametros de exportacion dle reporte.
                        ImageExportOptions imageOptions = new ImageExportOptions();
                        imageOptions.ExportMode = ImageExportMode.SingleFile;
                        imageOptions.Format = ImageFormat.Jpeg;
                        imageOptions.Resolution = 100;
                        imageOptions.PageRange = "1";



                        try
                        {
                            if (gratefulness.FormatTypeId == 1)
                            {
                                Agradecimiento reporte = new Agradecimiento();
                                Entities.ViewModels.GratefulnessView temo = new Entities.ViewModels.GratefulnessView();
                                temo.DestinataryName = gratefulness.DestinataryName;
                                //                                1   EXCELENTE ATENCION
                                //2   EXCELENTE SERVICIO
                                //3   EXCELENTE TRABAJO
                                if (gratefulness.GratefulnessTypeId == 1)
                                {
                                    temo.GratefulnessTypeName = "EXCELENTE ATENCION";
                                }
                                if (gratefulness.GratefulnessTypeId == 2)
                                {
                                    temo.GratefulnessTypeName = "EXCELENTE SERVICIO";
                                }
                                if (gratefulness.GratefulnessTypeId == 3)
                                {
                                    temo.GratefulnessTypeName = "EXCELENTE TRABAJO";
                                }
                                temo.Message = gratefulness.Message;
                                temo.SendPersonName = eEmployee.FullName;
                                temo.GratefulnessDate = gratefulness.GratefulnessDate;
                                gratefulnessReport.Add(temo);
                                //  gratefulnessReport = Data.Gratefulness.GetGratefulnessByPersonAndId(eEmployee.Domain, eEmployee.Id_HRMS, eEmployee.RealUserLevel, gratefulness.GratefulnessId);
                                reporte.DataSource = gratefulnessReport;

                                //fileName = "Formato1-" + gratefulness.GratefulnessId.ToString()+".JPEG";
                                //string newFilePath = @"\172.26.54.66\foto\" + fileName; // Nueva ruta de guardado

                                reporte.ExportToImage(ms, imageOptions);
                                //guardar la imagend le reporte en la base de datos


                                //Image imageIn = Image.FromFile(Server.MapPath(newFilePath));

                                //imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                gratefulness.Image = ms.ToArray();

                                ms.Dispose();
                                reporte.Dispose();

                            }
                            if (gratefulness.FormatTypeId == 2)
                            {
                                Agradecimiento2 formato2 = new Agradecimiento2();
                                resultado = "crear1";
                                Entities.ViewModels.GratefulnessView temo = new Entities.ViewModels.GratefulnessView();
                                temo.DestinataryName = gratefulness.DestinataryName;
                                //                                1   EXCELENTE ATENCION
                                //2   EXCELENTE SERVICIO
                                //3   EXCELENTE TRABAJO
                                if (gratefulness.GratefulnessTypeId == 1)
                                {
                                    temo.GratefulnessTypeName = "EXCELENTE ATENCION";
                                }
                                if (gratefulness.GratefulnessTypeId == 2)
                                {
                                    temo.GratefulnessTypeName = "EXCELENTE SERVICIO";
                                }
                                if (gratefulness.GratefulnessTypeId == 3)
                                {
                                    temo.GratefulnessTypeName = "EXCELENTE TRABAJO";
                                }
                                temo.Message = gratefulness.Message;
                                temo.SendPersonName = eEmployee.FullName;
                                temo.GratefulnessDate = gratefulness.GratefulnessDate;
                                gratefulnessReport.Add(temo);
                                //  gratefulnessReport = Data.Gratefulness.GetGratefulnessByPersonAndId(eEmployee.Domain, eEmployee.Id_HRMS, eEmployee.RealUserLevel, gratefulness.GratefulnessId);
                                formato2.DataSource = gratefulnessReport;
                                resultado = "crear2";


                                //fileName = "Formato2-" + gratefulness.GratefulnessId.ToString() + ".JPEG";
                                //string newFilePath = @"\172.26.54.66\foto\" + fileName; // Nueva ruta de guardado

                                formato2.ExportToImage(ms, imageOptions);
                                resultado = "crear3";

                                //Guardar la imagend le reporte en la base de datos


                                //Image imageIn = Image.FromFile(Server.MapPath(newFilePath));

                                gratefulness.Image = ms.ToArray();
                                resultado = "crear5";

                                //imageIn = null;
                                ms.Dispose();
                                formato2.Dispose();
                                resultado = "crear5";

                            }

                        }
                        catch (Exception ex)
                        {
                            resultado = resultado + " " + ex.Message + "1";
                            return Json(new { status = "Error", message = "error a crear el reporte   :" + gratefulness.GratefulnessId + " " + resultado });


                        }
                        try
                        {
                            string titulo = string.Empty;
                            string mensaje = string.Empty;
                            string resultadoCorreo = string.Empty;
                            string destinatario = string.Empty;
                            string copia = string.Empty;


                            Entities.Employees employee = new Entities.Employees();
                            employee = (Entities.Employees)Session["User"];


                            var Destinatary = lstEmpleadox.Where(x => x.Id_HRMS == gratefulness.ReceivePersonId).FirstOrDefault();
                            var senderPersonName = employee.FullName;


                            if (!string.IsNullOrEmpty(Destinatary.Email_Address))
                            {
                                destinatario = Destinatary.Email_Address;
                            }

                            copia = employee.EmailAddress;


                            titulo = "TIENES UN AGRADECIMIENTO DE: " + employee.FullName;




                            //  copia = "candida.sanchez@claro.com.ni";
                            if (gratefulness.GratefulnessTypeId == 1)
                            {
                                titulo = "¡Excelente Atención!";
                            }
                            else if (gratefulness.GratefulnessTypeId == 2)
                            {
                                titulo = "¡Excelente Servicio!";

                            }
                            else
                            { titulo = "¡Excelente Trabajo!"; }
                            if (gratefulness.FormatTypeId == 1)
                            {
                                fileName = "Formato1-" + gratefulness.GratefulnessId.ToString() + ".JPEG";


                            }
                            else
                            {
                                fileName = "Formato2-" + gratefulness.GratefulnessId.ToString() + ".JPEG";


                            }
                            Utils.ClaroWCF.EnviarCorreootros(destinatario, titulo, copia, fileName, gratefulness.Image);



                        }
                        catch (Exception et)
                        {
                            resultado = et.Message + "2";
                            return Json(new { status = "Error", message = "enviar correo :" + gratefulness.GratefulnessId + " " + resultado });

                        }
                        if (Session["User"] != null)
                        {
                            eEmployee = (Entities.Employees)Session["User"];
                        }
                        try
                        {
                            resultado = Data.Gratefulness.UpdateGratefulness(gratefulness);
                        }
                        catch (Exception ex)
                        {
                            //    resultado = ex.Message + "3";
                            //   return Json(new { status = "Error", message = "error guardar el agradecimiento :" + gratefulness.GratefulnessId + " " + resultado });

                        }



                    }
                }

            }

            catch (Exception ex)
            {
                return Json(new { status = "Error", message = "Ocurrió un error en la transacción, por favor verifique los datos o consulte con el administrador de la plataforma." + ex.Message });
            }


            return Json(new { status = "Exito", message = "El agradecimiento ha sido guardado y enviado al destinatario. Gracias por utilizar RHOnline." });

        }


        public string SaveGratefulnessCard(Entities.Agradecimiento gratefulness)
        {
            string resultado = string.Empty;
            MemoryStream ms = new MemoryStream();
            List<agradecimientomodel> gratefulnessReport = new List<agradecimientomodel>();
            string fileName = string.Empty;

            //Parametros de exportacion dle reporte.
            ImageExportOptions imageOptions = new ImageExportOptions();
            imageOptions.ExportMode = ImageExportMode.SingleFile;
            imageOptions.Format = ImageFormat.Jpeg;
            imageOptions.Resolution = 100;
            imageOptions.PageRange = "1";


            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                if (gratefulness.FormatTypeId == 1)
                {
                    Agradecimiento reporte = new Agradecimiento();
                    
                    gratefulnessReport = Data.Gratefulness.GetGratefulnessByPersonAndId(eEmployee.Domain, eEmployee.Idhrms, eEmployee.RealUserLevel, gratefulness.GratefulnessId);
                    reporte.DataSource = gratefulnessReport;

                    reporte.ExportToImage(ms, imageOptions);
                    //guardar la imagend le reporte en la base de datos


                    //Image imageIn = Image.FromFile(Server.MapPath(newFilePath));

                    //imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    gratefulness.Image = ms.ToArray();

                    ms.Dispose();
                    reporte.Dispose();

                }
                if (gratefulness.FormatTypeId == 2)
                {
                    Agradecimiento2 formato2 = new Agradecimiento2();

                    gratefulnessReport = Data.Gratefulness.GetGratefulnessByPersonAndId(eEmployee.Domain, eEmployee.Idhrms, eEmployee.RealUserLevel, gratefulness.GratefulnessId);
                    formato2.DataSource = gratefulnessReport;


                    //fileName = "Formato2-" + gratefulness.GratefulnessId.ToString() + ".JPEG";
                    //string newFilePath = @"\172.26.54.66\foto\" + fileName; // Nueva ruta de guardado

                    formato2.ExportToImage(ms, imageOptions);

                    //Guardar la imagend le reporte en la base de datos


                    //Image imageIn = Image.FromFile(Server.MapPath(newFilePath));

                    //imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    gratefulness.Image = ms.ToArray();
                    //imageIn = null;
                    ms.Dispose();
                    formato2.Dispose();
                }

                resultado = Data.Gratefulness.UpdateGratefulness(gratefulness);
            }
            catch (Exception ex)
            {
                resultado = ex.Message;
                throw new Exception(ex.Message);
            }
            try
            {
                resultado = EnviarCorreo(gratefulness);

            }
            catch (Exception et)
            {
                resultado = et.Message;
                throw new Exception(et.Message);
            }


            return resultado;
           
        }

        public string EnviarCorreo(Entities.Agradecimiento gratefulness)
        {
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            string destinatario = string.Empty;
            string copia = string.Empty;
            string fileName = string.Empty;





            try
            {
                List<Entities.empleadoagradecimiento> lstEmpleadox = new List<Entities.empleadoagradecimiento>();
                if (Session["sEmployeestodoempleado"] != null)
                {
                    lstEmpleadox = (List<Entities.empleadoagradecimiento>)Session["sEmployeestodoempleado"];
                }
                //var destinatary = Data.Employee.GetEmployeeById(gratefulness.ReceivePersonId).FirstOrDefault();
                var Destinatary = lstEmpleadox.Where(x => x.Id_HRMS == gratefulness.ReceivePersonId).FirstOrDefault();
                //var senderPersonName = lstEmpleadox.Where(x => x.Id_HRMS == gratefulness.SendPersonId).FirstOrDefault();
                Entities.Employees employee = new Entities.Employees();
                employee = (Entities.Employees)Session["User"];



                if (!string.IsNullOrEmpty(Destinatary.Email_Address))
                {
                    destinatario = Destinatary.Email_Address;
                }
                  copia = employee.EmailAddress;
                    //titulo = "TIENES UN AGRADECIMIENTO DE: " + senderPersonName.FullName;

                    titulo = "TIENES UN AGRADECIMIENTO DE: " + employee.FullName;
           



                //  copia = "candida.sanchez@claro.com.ni";
                if (gratefulness.GratefulnessTypeId == 1)
                {
                    titulo = "¡Excelente Atención!";
                }
                else if (gratefulness.GratefulnessTypeId == 2)
                {
                    titulo = "¡Excelente Servicio!";

                }
                else
                { titulo = "¡Excelente Trabajo!"; }
                if (gratefulness.FormatTypeId == 1)
                {
                    fileName = "Formato1-" + gratefulness.GratefulnessId.ToString() + ".JPEG";

                    resultadoCorreo =  EnviarCorreootros(destinatario, titulo, copia, fileName, gratefulness.Image);
                }
                else
                {
                    fileName = "Formato2-" + gratefulness.GratefulnessId.ToString() + ".JPEG";

                    resultadoCorreo =  EnviarCorreootros(destinatario, titulo, copia, fileName, gratefulness.Image);
                }

                if (resultadoCorreo != "EXITO")
                {
                    resultadoCorreo = "Ocurrió un error al enviar el correo a: " + destinatario;

                }
                else
                {
                    resultadoCorreo = "EXITO";
                }
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }


            return resultadoCorreo;
        }

        public string EnviarCorreootros(string correoDestinatario, string titulo, string destinatarioCopia, string fileName, byte[] Image)
        {

            string output = null;
            string htmlBody = "<html><body><img src=cid:Pic1></body></html>";
            AlternateView Body = AlternateView.CreateAlternateViewFromString(htmlBody, null, System.Net.Mime.MediaTypeNames.Text.Html);
            //LinkedResource Felicitaciones = new LinkedResource(HttpContext.Current.Server.MapPath("~/Content/Images/" + fileName), System.Net.Mime.MediaTypeNames.Image.Jpeg);
            LinkedResource Felicitaciones = new LinkedResource(new MemoryStream(Image), System.Net.Mime.MediaTypeNames.Image.Jpeg);
            string newLocalPart = "recursoshumanos";
            string domainPart = destinatarioCopia.Split('@')[1];
            string newEmail = newLocalPart + "@" + domainPart;
            Felicitaciones.ContentId = "Pic1";
            Body.LinkedResources.Add(Felicitaciones);
            MailMessage email = new MailMessage();
            // email.To.Add("gustavo.lira@claro.com.ni");
            email.To.Add(correoDestinatario);
            email.From = new MailAddress(newEmail, "Recursos Humanos");
            email.Subject = titulo;
            email.SubjectEncoding = System.Text.Encoding.UTF8;
            email.Bcc.Add(destinatarioCopia);
            //email.Body = mensaje;
            email.BodyEncoding = System.Text.Encoding.UTF8;
            email.IsBodyHtml = false;
            email.AlternateViews.Add(Body);
            email.Priority = MailPriority.Normal;


            email.Bcc.Add("gustavo.lira@claro.com.ni");
            email.Bcc.Add("candida.sanchez@claro.com.ni");

            //// Usamos el nombre del archivo como ContentId
            //string contentId = Path.GetFileName(rutaImagen);
            //imagen.ContentId = contentId;
            //mensaje += $"<br/><br/><img src='cid:{imagen.ContentId}'/>";


            System.Net.Mail.SmtpClient cliente = new SmtpClient();
            cliente.Host = ("10.200.5.23");
            //cliente.Host = ("192.168.8.250");
            cliente.UseDefaultCredentials = false;
            //cliente.Credentials = new NetworkCredential("recursoshumanos@claroni.americamovil.ca1", "Claro2014");
            cliente.Port = 25;
            cliente.EnableSsl = false;


            try
            {
                cliente.Send(email);
                email.Dispose();
                output = "EXITO";
            }
            catch (Exception ex)
            {
                output = ex.InnerException.Message;
            }

            return output;


        }

        public ActionResult LoadGratefulness2024(int gratefulnessId)
        {
            try
            {
                Entities.Agradecimiento gratefulness = new Entities.Agradecimiento();
                gratefulness.Image = Utils.ClaroWCF.GetGratefulnessImage(gratefulnessId);
                if (gratefulness.Image == null)
                {
                    return Json(new { status = "Error", message = "Imagen no encontrada" }, JsonRequestBehavior.AllowGet);
                }

                string base64Image = Convert.ToBase64String(gratefulness.Image);
                string imageUrl = string.Format("data:image/png;base64,{0}", base64Image);
                return Json(new { status = "Success", imageUrl = imageUrl }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { status = "Error", message = "Error al cargar la imagen" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult LoadGratefulness(int gratefulnessId)
        {
            try
            {
                Entities.Agradecimiento gratefulness = new Entities.Agradecimiento();
                //gratefulness = Data.Gratefulness.GetGratefulnessById(gratefulnessId);
                
                
                
                gratefulness.Image = Utils.ClaroWCF.GetGratefulnessImage(gratefulnessId);
                    if (gratefulness.Image == null)
                    {
                        return null;
                    }

                    return PartialView("ImageGratefulnessPartial", gratefulness);
                
            }
            catch (Exception e)
            {
                throw new Exception("Error al cargar el objeto", e);
            }
        }
        public static List<Entities.EmpleadoV2025V> GetAllEmpleadosV2025V()
        {
            List<Entities.EmpleadoV2025V> lista = new List<Entities.EmpleadoV2025V>();
            if (System.Web.HttpContext.Current.Session["V2025V_Empleados"] != null)
            {
                lista = (List<Entities.EmpleadoV2025V>)System.Web.HttpContext.Current.Session["V2025V_Empleados"];
            }
            else
            {
                string apiUrl = "http://TU_SERVIDOR/api/v2025v/listar";
                var client = new RestClient(apiUrl);
                var request = new RestRequest(Method.GET);
                request.Timeout = 8000;
                var result = client.Execute(request);

                if (result != null && !string.IsNullOrWhiteSpace(result.Content))
                {
                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 50000000;
                    var cleanJson = result.Content.Replace(".0", "");
                    // La API puede devolver string serializado, ajustar si es necesario
                    if (cleanJson.StartsWith("\"") && cleanJson.EndsWith("\""))
                        cleanJson = serializer.Deserialize<string>(cleanJson);

                    lista = serializer.Deserialize<List<Entities.EmpleadoV2025V>>(cleanJson);
                    System.Web.HttpContext.Current.Session["V2025V_Empleados"] = lista;
                }
            }
            return lista;
        }

        // Devuelve empleados para vista (JSON)
        public JsonResult ObtenerEmpleadosV2025V()
        {
            var listaEmpleados = slnRhonline.Data.Employee.GetAllEmpleadosV2025V();
            // RETORNA SOLO LA LISTA
            return Json(listaEmpleados, JsonRequestBehavior.AllowGet);
        }


        // Agregar empleado (por AJAX POST)
        [HttpPost]
        public JsonResult AgregarEmpleadoV2025V(Entities.EmpleadoV2025V model)
        {
            string resultado = "";
             slnRhonline.Data.Employee.AgregarEmpleadoV2025V(model,out resultado);
                            return Json(resultado, JsonRequestBehavior.AllowGet);

        }

        // Editar nombre
        [HttpPost]
        public JsonResult EditarNombreEmpleadoV2025V(int ID_HRMS, string FULLNAME)
        {
            string resultado = "";
            slnRhonline.Data.Employee.EditarFullNameV2025V(ID_HRMS, FULLNAME, out resultado);
            return Json(resultado, JsonRequestBehavior.AllowGet);
         }
        public ViewResult ReportParameters()
        {
          //  Session.Remove("sConsumptionParameter");

            //ViewData["startPeriod"] = "01/01/2017";
            return View();
        }


        public ActionResult GratefulnessReport(Entities.MyEntities.GratefulnessParameters parameters)
        {
            xrptGratefulnessList reporte = new xrptGratefulnessList();
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {

               
                List<Entities.ViewModels.GratefulnessView> gratefulnessReport = new List<Entities.ViewModels.GratefulnessView>();
                gratefulnessReport = Data.Gratefulness.GetGratefulnessReport(eEmployee.Domain, eEmployee.Id_HRMS, eEmployee.RealUserLevel,parameters.StartDate.ToShortDateString(),parameters.EndDate.ToShortDateString()).ToList();
                reporte.DataSource = gratefulnessReport;
               
                return View(reporte);
               
            }
            catch (Exception ex)
            {
                reporte.Dispose();
                throw new Exception(ex.Message);
            }
        }

        //public ActionResult DevolverString()
        //{
        //    Entities.Gratefulness gratefulness = new Entities.Gratefulness();
        //    gratefulness.Message = "E-401691"; //"estas es una prueba con mas dos caracteres para enviar al web service de guatemala, el web service de guatemala solo puede recibir cien caracteres noo";


        //    string resultado = Data.Gratefulness.InsertGratefulnessGuate(gratefulness);
        //           return Json(new { status = "Exito", message = resultado, JsonRequestBehavior.AllowGet });
        //}
    }
}