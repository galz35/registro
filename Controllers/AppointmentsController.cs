using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Dapper;
using DevExpress.Web.Mvc;
using Entities.ViewModels;
using Newtonsoft.Json;
using RestSharp;
using slnRhonline.Validations;

namespace slnRhonline.Controllers
{
    public class AppointmentsController : Controller
    {
        const string keyIdUnidad = "sIdUnidad";
        const string keyTipoSolicitud = "sTipoSolicitud";
        const string keyCita = "sIdCita";
        const string keyKilometraje = "sKilometraje";
        private readonly string connectionString = "Data Source=192.168.8.234;Connection Timeout=60;Initial Catalog=SIAF;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;";

        /// <summary>
        /// Metodo que devuelve la lista de unidades asignadas al area.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult CarsList()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }
        [HttpGet]
        public JsonResult EnviarCodigoConfirmacionx(string celular)
        {
            var codigoAleatorio = GenerarCodigoAleatorio(); // Método para generar el código

            string mensaje = $"Tu código de confirmación es: {codigoAleatorio}";
            string url = $"http://172.26.54.66/apihcm/api/general/ssm?mensaje={mensaje}&numero={celular}";

            using (var client = new WebClient())
            {
                try
                {
                    string response = client.DownloadString(url);
                    // Devuelves el código generado al cliente
                    return Json(new { success = true, codigo = codigoAleatorio }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    // Manejar el error y devolverlo como respuesta
                    return Json(new { success = false, message = "Error al enviar el código de confirmación." }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        // Método para generar el código aleatorio
        private string GenerarCodigoAleatorio()
        {
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString(); // Ejemplo: código de 6 dígitos
        }

        /// <summary>
        /// Metodo que devuelve la lista de unidades asignadas al area  a la vista parcial.
        /// </summary>
        /// <returns></returns>
        public ActionResult CarsListPartial()
        {
            List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();

            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                lstCars = Data.Appointment.ObtenerListaUnidades(eEmployee.Idhrms.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("CarsListPartial", lstCars);
        }
        public JsonResult CarsListPartialjson()
        {
            List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();

            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                lstCars = Data.Appointment.ObtenerListaUnidades(eEmployee.Idhrms.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return Json(new { data = lstCars }, JsonRequestBehavior.AllowGet);

         }

        [HttpPost]
        public JsonResult ValidarCita(int idCita, string tipoEdicion)
        {
          
            if (tipoEdicion != "Nuevo")
            {
                var estado = Data.Appointment.GetAppointmentsByIdCita(idCita).FirstOrDefault();

                if (estado!= null)
                {
                    string estadoCita = estado.IdEstadoCita;
                    if (estadoCita != "1701")
                    {
                        return Json(new { status = "Error", message = "Solo se pueden editar citas en estado de REGISTRADO" });
                    }
                }
            }
            return Json(new { status = "Exito", message = "" });
        }

   
        public ActionResult Edit(int appointmentId = -1)
        {
            Session[keyCita] = appointmentId;
            Session.Remove("sAppointmentDetail");
            Entities.ViewModels.AppointmentsView editAppointment =
                Data.Appointment.GetAppointmentsByIdCita(appointmentId).FirstOrDefault();
            Session[keyKilometraje]  = Data.Appointment.kilometroanterio((string)Session[keyIdUnidad]);
            if (editAppointment == null)
            {
                editAppointment = new Entities.ViewModels.AppointmentsView();
               
                editAppointment.FechaCita = DateTime.Today;
                editAppointment.IdCita = -1;
            }
            return View("Edit", editAppointment);
        }
        public ActionResult Edit2(int appointmentId = -1)
        {
            Session[keyCita] = appointmentId;
            Session.Remove("sAppointmentDetail");
            Entities.ViewModels.AppointmentsView editAppointment =
                Data.Appointment.GetAppointmentsByIdCita(appointmentId).FirstOrDefault();
            Session[keyKilometraje] = Data.Appointment.kilometroanterio((string)Session[keyIdUnidad]);
            if (editAppointment == null)
            {
                editAppointment = new Entities.ViewModels.AppointmentsView();

                editAppointment.FechaCita = DateTime.Today;
                editAppointment.IdCita = -1;
            }
            return View("Edit2", editAppointment);
        }

        public ActionResult EditPartial(int idCita =-1)
        {
                      List<Entities.AppointmentsDetail> lstDetail = new List<Entities.AppointmentsDetail>();
            try
            {
                idCita= (int)Session[keyCita];

             
                    lstDetail = Data.Appointment
                        .GetAllEditableAppointmentDetail(idCita).ToList();
              
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


            return PartialView("EditPartial", lstDetail);
        }
        public ActionResult ConsultAppointment(int appointmentId = -1)
        {
            Session[keyCita] = appointmentId;
            Session.Remove("sAppointmentDetail");
            Entities.ViewModels.AppointmentsView editAppointment =          
                Data.Appointment.GetAppointmentsByIdCita(appointmentId).FirstOrDefault();
            if (editAppointment == null)
            {
                editAppointment = new Entities.ViewModels.AppointmentsView();

                editAppointment.FechaCita = DateTime.Today;
                editAppointment.IdCita = -1;
            }
            return View("ConsultAppointment", editAppointment);
        }
        [HttpGet]
        public ActionResult GetAppointment(int id)
        {
            if (id <= 0)
                return Json(new { }, JsonRequestBehavior.AllowGet);

            try
            {
                using (var conn = new SqlConnection(connectionString))

                 {
                    conn.Open();

                    // 1) Cabecera
                    var cab = conn.QueryFirstOrDefault(
                        "dbo.sp_TallerCitas_Seleccion",
                        new
                        {
                            TipoConsulta = "SeleccionCabecera",
                            IdCita = id,
                            CampoBusqueda = (string)null,
                            IdPais = (int?)null   // pásalo si aplica
                    },
                        commandType: CommandType.StoredProcedure
                    );

                    if (cab == null)
                        return Json(new { }, JsonRequestBehavior.AllowGet);

                    // normalizar número: solo dígitos
                    string numero = ((string)cab.WhatsApp ?? (string)cab.Telefono ?? string.Empty);
                    numero = new string(numero.Where(char.IsDigit).ToArray());

                    // 2) Detalle
                    var det = conn.Query(
                        "dbo.sp_TallerCitasDetalle_Seleccion",
                        new
                        {
                            TipoConsulta = "SeleccionDetalle",
                            IdCita = id,
                            CampoBusqueda = (string)null
                        },
                        commandType: CommandType.StoredProcedure
                    ).Select(d => new
                    {
                        IdTipoServicio = (string)d.IdTipoServicio,
                        Observacion = (string)(d.Observacion ?? string.Empty),
                        IdCitaDetalle = (int?)d.IdCitaDetalle,
                        EsRegistroBd = (int?)d.EsRegistroBd ?? 1
                    }).ToList();

                    // 3) Armar payload para el front
                    var payload = new
                    {
                        IdCita = (int)cab.IdCita,
                        IdUnidad = (string)cab.IdUnidad,
                        KilometrajeActual = (int?)cab.KilometrajeActual,
                        FechaCita = (DateTime?)cab.FechaCita,
                        HoraCita = cab.HoraCita, // ajusta tipo si fuese TimeSpan/string
                        Observacion = (string)(cab.Observacion ?? string.Empty),
                        EstadoCita = (string)cab.EstadoCita,
                        Telefono = numero,  // compat con tu front
                        WhatsApp = numero,  // idem
                        Detalle = det       // lista
                    };

                    return Json(payload, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return Json(new { error = true, message = "No se pudo cargar la cita." }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult ConsultAppointmentPartial(int idCita = -1)
        {

            List<Entities.AppointmentsDetail> lstDetail = new List<Entities.AppointmentsDetail>();
            try
            {
                idCita = (int)Session[keyCita];


                lstDetail = Data.Appointment
                    .GetAllEditableAppointmentDetail(idCita).ToList();

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


            return PartialView("ConsultAppointmentPartial", lstDetail);
        }
        [HttpPost]
        public JsonResult GuardarDetalleCitaanexo(int idCita, List<Entities.AppointmentsDetail> nuevosDetalles)
        {
            try
            { 
                // Guardar los nuevos detalles usando tu lógica existente
           string    resultadoDetalle = Data.Appointment.GuardarDetalleCitaanexo(idCita, nuevosDetalles);
                EnviarCorreo3(nuevosDetalles, idCita);
                return Json(new { success = true, message = resultadoDetalle });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult ObtenerTipoServicios()
        {
            try
            {
               
                var servicios = slnRhonline.Data.TipoServicio.ObtenerTipoServicios();// Lógica existente en tu capa de datos
                return Json(servicios, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult obtenerdetallecita(int idCita)
        {
            try
            {
                var detalles = Data.Appointment
                    .GetAllEditableAppointmentDetail2(idCita).ToList();   // Lógica existente en tu capa de datos
                return Json(detalles, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // <summary>
        /// Metodo que devuelve la lista de citas por unidad a la vista primaria
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult BackToList()
        {
            //string sIdUnidad;
            //sIdUnidad = (string)Session[keyUnidadConsumo];


            return CarsList();
        }
        public ActionResult List(string id)
        {
            Session[keyIdUnidad] =id;
          ViewBag.Unidad = id;

            return View();
            //List<Entities.ViewModels.AppointmentsView> lstAppointments = new List<Entities.ViewModels.AppointmentsView>();
            //try
            //{
            //    Session[keyIdUnidad] = id;
            //    lstAppointments = Data.Appointment.GetAppointmentsByCar(id);
            //    ViewBag.Unidad = id;
            //    return View("List", lstAppointments);
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("Se ha producido el siguiente error ", ex);
            //}
        }
        private const string SessionKeyDetail = "sAppointmentDetail";

        public class SyncDetailSessionDto
        {
            public List<Entities.AppointmentsDetail> Items { get; set; }
        }
        public class SyncDetailItemDto
        {
            public string IdTipoServicio { get; set; }
            public string Observacion { get; set; }
        }
        public class SyncDetailRequestDto
        {
            public List<SyncDetailItemDto> Items { get; set; }
        }

        // ====== Helpers de sesión ======
        private void ClearDetailSession()
        {
            System.Web.HttpContext.Current.Session[SessionKeyDetail] =
                new List<Entities.AppointmentsDetail>();
        }

        private List<Entities.AppointmentsDetail> GetDetailSession()
        {
            var list = System.Web.HttpContext.Current.Session[SessionKeyDetail]
                as List<Entities.AppointmentsDetail>;
            if (list == null)
            {
                list = new List<Entities.AppointmentsDetail>();
                System.Web.HttpContext.Current.Session[SessionKeyDetail] = list;
            }
            return list;
        }

        // ====== Reusa tus métodos existentes (como los compartiste) ======
        // public static void AddSessionDetail(Entities.AppointmentsDetail detail) { ... }
        // public static List<Entities.AppointmentsDetail> GetAllEditableAppointmentDetail(int idCita) { ... }
        // public static void EditSessiontDetail(Entities.AppointmentsDetail item) { ... }
        // ↑ Se asume que están en alguna clase (por ejemplo AppointmentSession). 
        //   Cambia "AppointmentSession." por la clase real donde viven.

        // POST: /Appointments/SyncDetailSession
        [HttpPost]
        public ActionResult SyncDetailSession(SyncDetailRequestDto request)
        {
            try
            {
                // 1) limpiar SIEMPRE para evitar residuos
                ClearDetailSession();

                // 2) si no hay items, devolvemos success=false para que el front bloqueé el Guardar
                if (request == null || request.Items == null || !request.Items.Any())
                {
                    return Json(new { success = false, count = 0, message = "Debes agregar al menos una fila de detalle." });
                }

                // 3) re-llenar usando tu AddSessionDetail
                foreach (var i in request.Items)
                {
                    var detail = new Entities.AppointmentsDetail
                    {
                        IdCita = -1, // en modo edición de popup
                        IdTipoServicio = i.IdTipoServicio.ToString(),
                        Observacion = i.Observacion
                    };

                    Data.Appointment.AddSessionDetail(detail);
                    // Reutiliza tu método tal cual:
                }

                var count = GetDetailSession().Count;
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, count = 0, message = ex.Message });
            }
        }

        public JsonResult Listjson( )
        {
            List<Entities.ViewModels.AppointmentsView> lstAppointments = new List<Entities.ViewModels.AppointmentsView>();
            try
            {
string id=         (string)       Session[keyIdUnidad] ;
                lstAppointments = Data.Appointment.GetAppointmentsByCar(id);
                ViewBag.Unidad = id;
                return Json(new { data = lstAppointments }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }
        public ActionResult ListPartial()
        {
            List<Entities.ViewModels.AppointmentsView> lstAppointments = new List<Entities.ViewModels.AppointmentsView>();
            try
            {
                string idUnidad = (string)Session[keyIdUnidad];
                lstAppointments = Data.Appointment.GetAppointmentsByCar(idUnidad);
               


                return PartialView("ListPartial", lstAppointments);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }

        #region CRUD
        [HttpPost]
        public ActionResult EnviarCodigoConfirmacion(string celular)
        {
            // Aquí generas el código aleatorio
            var codigoAleatorio = new Random().Next(100000, 999999).ToString();

            // Lógica para enviar el código por SMS usando la API correspondiente
            bool enviado = EnviarSms(celular, codigoAleatorio); // Implementa esta función según tu lógica

            if (enviado)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "No se pudo enviar el código de confirmación. Inténtelo de nuevo más tarde." });
            }
        }
 
       

        // POST: Appointments/RequestSmsCode
        [HttpPost]
        public async Task<JsonResult> RequestSmsCode(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return Json(new { success = false, message = "Número inválido." });
            var soloNumeros = new string(numero.Where(char.IsDigit).ToArray());

            // Genera código y guarda en Session con expiración
            var code = new Random().Next(100000, 999999).ToString();
            var info = new OtpInfo
            {
                Numero =  soloNumeros,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(20),
                Attempts = 0
            };
            Session[OTP_SESSION_KEY] = info;

            // Envía SMS desde el servidor (ocultando la API al cliente)
            var ok =  EnviarSmsAsync(info.Numero,  code.ToString());
            if (!ok) return Json(new { success = false, message = "No se pudo enviar el SMS." });

            return Json(new { success = true });
        }
        private static readonly HttpClient Httpx = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
      

        private  bool  EnviarSmsAsync(string numero, string mensaje)
        {
            // limpiar a solo dígitos
            var num = new string((numero ?? "").Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(num) || string.IsNullOrWhiteSpace(mensaje))
                return false;
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // Construye URL con parámetros ya escapados
            var url = $"https://recursoshumanosni/apihcmsw/api/general/ssm?mensaje={Uri.EscapeDataString(mensaje)}&numero={Uri.EscapeDataString(num)}";

            var client = new RestClient(url) { Timeout = 30000 }; // 30s
            var req = new RestRequest(Method.GET);

            IRestResponse resp = null;
            try
            {
                resp = client.Execute(req);
            }
            catch
            {
                return false;
            }
 
            if (resp == null || string.IsNullOrWhiteSpace(resp.Content))
                return false;

            try
            {
                var obj = JsonConvert.DeserializeObject<SmsRespuesta>(resp.Content);
                if (obj?.sms_message != null && obj.sms_message.Any())
                { 
                    string idUnidad = (string)Session[keyIdUnidad];
                    Entities.Employees eEmployee = null;
                    eEmployee = (Entities.Employees)Session["User"];
            int id=         LogVerificacionCodigoAsync(
               num, mensaje, mensaje, false, eEmployee.EmployeeNumber, null, "creacion codigo-" + idUnidad+"-telefono"+ num, idUnidad);
                    if (id==0)
                    {
                        return false;

                    }
                    GenerarCorreoCodigoActivacion(num, mensaje, idUnidad,id+"");

                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        // Genera correo con código de activación (incluye número de teléfono y código).
        public string GenerarCorreoCodigoActivacion(  string numeroTelefono, string codigo,string unidad,string id )
        {
            Entities.Employees eEmployee = null;
            eEmployee = (Entities.Employees)Session["User"];
            // Asunto claro para verificación.
            // Asunto claro para Registro de Cita + unidad.
//            string titulo = "Verificación Registro de Cita: Código de Activación - Unidad " + unidad;

//            // HTML con instrucción RHOnline y 1 solo uso.
//            string mensajex = $@"<html><head><style>
//    body{{font-family:Arial;color:#333;background:#f4f4f9;margin:0;padding:20px}}
//    .container{{background:#fff;padding:20px;border-radius:8px;box-shadow:0 2px 10px rgba(0,0,0,.08);width:600px;margin:auto}}
//    h2{{color:#d32f2f;text-align:center;margin-top:0}}
//    .dato{{background:#f7f7f7;border:1px dashed #ccc;border-radius:8px;padding:12px;margin:14px 0;text-align:center}}
//    .codigo{{font-size:28px;font-weight:700;letter-spacing:4px}}
//    .footer{{margin-top:20px;text-align:center;font-size:.9em;color:#777}}
//    .signature{{border-top:1px solid #e0e0e0;padding-top:12px;margin-top:16px;text-align:center;color:#333}}
//    .signature p{{margin:2px 0;font-size:.85em}}
//</style></head>
//<body>
//  <div class='container'>
//    <h2>Código de Activación — Registro de Cita</h2>
//    <p>Estimado(a) <strong>{eEmployee.FullName}</strong>,</p>

//    <!-- Instrucción explícita: RHOnline + 1 solo uso -->
//    <p>Ingrese este código en la pantalla de <strong>RHOnline</strong> donde se está solicitando el código para continuar con el <strong>Registro de Cita</strong>. 
//    Este código es de <strong>un solo uso</strong>.</p>

//    <div class='dato'><strong>Teléfono:</strong> {numeroTelefono}</div>
//    <div class='dato'><strong>Codigo:</strong> {codigo}</div>

 
//    <p>Si usted no solicitó este código, ignore este mensaje.</p>

//    <div class='signature'>
//      <p><strong>Soporte a la operación</strong></p>
//      <p>Tel: <strong>22745505</strong> / <strong>2274510</strong></p>
//      <p>Correo: <strong>soporte.operacion@claro.com.ni</strong></p>
//    </div>
//    <div class='footer'><p>Mensaje automático, no responder.</p></div>
//  </div>
//</body></html>";

            // Envío reutilizando tu helper. (ID no aplica para verificación)
            string output = null;
            try
            {
 
                string apiUrl = "http://172.26.54.66/apihcm/api/values/correo/transporteespecial3?correo=" + eEmployee.EmailAddress + "&titulo=" + unidad + "&destinatarioCopia=" + "transporte@claro.com.ni" + "&mensaje=" + id;
                string mensaje3 = "";
                var client = new RestClient(apiUrl);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                Console.WriteLine(response.Content);
              string  mensaje = response.Content;
                if (mensaje != null && mensaje != "")
                { }
                output = mensaje;
            }
            catch (Exception e) { output = "no se envio :" + e.Message; }
            if (output.Contains("EXITO") == true)
            { return "EXITO"; }
            return output;
            // Reutiliza el método existente de envío.
            //return getcorreohelp(eEmployee.EmailAddress, eEmployee.EmailAddress, titulo, mensaje, 0); // 0: no aplica ID de caso.
        }
        public static string EncodeHtmlToNumeric(string html)
        {
            // Convertir el HTML a bytes
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            // Comprimir los bytes para reducir tamaño
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                byte[] compressed = ms.ToArray();
                // Para asegurar que BigInteger sea positivo, agregamos un byte nulo
                byte[] positive = compressed.Concat(new byte[] { 0 }).ToArray();
                BigInteger bigInt = new BigInteger(positive);
                return bigInt.ToString(); // Representación decimal: solo dígitos
            }
        }
        public string getcorreohelp(string correo, string copia, string titulo, string mensaje, int id)
        {

            string output = null;
            MailMessage email = new MailMessage();
            //// email.To.Add("gustavo.lira@claro.com.ni");s

             email.To.Add(correo);


            //if (id > 0)
            //{
            //    List<CasoView> casosx = new List<CasoView>();
            //    casosx = (List<CasoView>)Session["casos"];
            //    var a = casosx.Where(x => x.ID == id).FirstOrDefault();
            //    if (correo != a.CorreoAutor)
            //    {
            //        email.To.Add(a.CorreoAutor);
            //    }

            //}
            email.From = new MailAddress("Rhonline.transporte@claro.com.ni");
            email.Subject = titulo;
            email.SubjectEncoding = System.Text.Encoding.UTF8;
            //email.Bcc.Add(destinatarioCopia);
            email.Bcc.Add("gustavo.lira@claro.com.ni");
            email.Bcc.Add("transporte@claro.com.ni");
            email.Bcc.Add("candida.sanchez@claro.com.ni");



            email.Body = mensaje;
            email.BodyEncoding = System.Text.Encoding.UTF8;
            email.IsBodyHtml = true;
            email.Priority = MailPriority.Normal;




            ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
            SmtpClient cliente = new SmtpClient("10.200.5.23", 587); // IP y puerto de FortiMail
            cliente.Credentials = new NetworkCredential("recursoshumanos@claro.com.ni", "Enero&272025"); // Eliminar antes de producción
                                                                                                         //cliente.Credentials = new NetworkCredential("transporte@claro.com.ni", "Enero&r546"); // Eliminar antes de producción
            cliente.EnableSsl = true;


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

        public class SmsRespuesta
        {
             public int sms_sent { get; set; }

             public string sms_message { get; set; }
        }
        private bool EnviarSms(string numero, string mensaje)
        {
            // Implementa la lógica para enviar el SMS aquí
            // Por ejemplo, usando una API externa
                return true; // Retorna true si el mensaje fue enviado correctamente, de lo contrario false
        }
        public class SyncDetailPayload
        {
            public List<DetailItem> items { get; set; }
            public class DetailItem
            {
                public string IdTipoServicio { get; set; }
                public string Observacion { get; set; }
            }
        }
        private const string OTP_SESSION_KEY = "OTP_INFO";

        // Asegúrate de tener esta clase (ya la habíamos usado antes)
        public class OtpInfo
        {
            public string Numero { get; set; }     // número que se envió a SMS (tal como lo capturaste)
            public string Code { get; set; }       // OTP generado/enviado
            public DateTime ExpiresAt { get; set; }
            public int Attempts { get; set; }
        }

        // Sanea un teléfono a solo dígitos (ej. "8621-9104" -> "86219104")
        private static string CleanPhoneDigits(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var arr = new System.Text.StringBuilder(raw.Length);
            foreach (var ch in raw)
                if (char.IsDigit(ch)) arr.Append(ch);
            return arr.ToString();
        }

        // Registrar auditoría en SQL (usa el SP)
        private int LogVerificacionCodigoAsync(
            string numero, string codigoIngresado, string codigoServidor,
            bool resultado, string carnet, int? idCita, string mensaje,string unidad )
        {
            Entities.Employees eEmployee = null;
            eEmployee = (Entities.Employees)Session["User"];
            // Asunto claro para verificación.
            // Asunto claro para Registro de Cita + unidad.
            string titulo = "Verificación Registro de Cita: Código de Activación - Unidad " + unidad;

            // HTML con instrucción RHOnline y 1 solo uso.
            string mensajex = $@"<html><head><style>
    body{{font-family:Arial;color:#333;background:#f4f4f9;margin:0;padding:20px}}
    .container{{background:#fff;padding:20px;border-radius:8px;box-shadow:0 2px 10px rgba(0,0,0,.08);width:600px;margin:auto}}
    h2{{color:#d32f2f;text-align:center;margin-top:0}}
    .dato{{background:#f7f7f7;border:1px dashed #ccc;border-radius:8px;padding:12px;margin:14px 0;text-align:center}}
    .codigo{{font-size:28px;font-weight:700;letter-spacing:4px}}
    .footer{{margin-top:20px;text-align:center;font-size:.9em;color:#777}}
    .signature{{border-top:1px solid #e0e0e0;padding-top:12px;margin-top:16px;text-align:center;color:#333}}
    .signature p{{margin:2px 0;font-size:.85em}}
</style></head>
<body>
  <div class='container'>
    <h2>Código de Activación — Registro de Cita</h2>
    <p>Estimado(a) <strong>{eEmployee.FullName}</strong>,</p>

    <!-- Instrucción explícita: RHOnline + 1 solo uso -->
    <p>Ingrese este código en la pantalla de <strong>RHOnline</strong> donde se está solicitando el código para continuar con el <strong>Registro de Cita</strong>. 
    Este código es de <strong>un solo uso</strong>.</p>

    <div class='dato'><strong>Teléfono:</strong> {numero}</div>
    <div class='dato'><strong>Codigo:</strong> {codigoServidor}</div>

 
    <p>Si usted no solicitó este código, ignore este mensaje.</p>

    <div class='signature'>
      <p><strong>Soporte a la operación</strong></p>
      <p>Tel: <strong>22745505</strong> / <strong>2274510</strong></p>
      <p>Correo: <strong>soporte.operacion@claro.com.ni</strong></p>
    </div>
    <div class='footer'><p>Mensaje automático, no responder.</p></div>
  </div>
</body></html>";

            // Envío reutilizando tu helper. (ID no aplica para verificación)
           
                string m = EncodeHtmlToNumeric(mensajex);
                var cs =   "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog=SIAF;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;"; ;
            // ADO.NET: captura el ID insertado vía parámetro OUTPUT.
            try
            {
                using (var cn = new SqlConnection(cs))
                using (var cmd = new SqlCommand("dbo.VerificarCodigo_Insert", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Numero", (object)numero ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CodigoIngresado", (object)codigoIngresado ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CodigoServidor", (object)codigoServidor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Resultado", resultado);
                    cmd.Parameters.AddWithValue("@Carnet", (object)carnet ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdCita", (object)idCita ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Mensaje", (object)mensaje ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NumeroMensaje", (object)m ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Unidad", (object)unidad ?? DBNull.Value);

                    var ip = Request?.UserHostAddress;
                    var ua = Request?.UserAgent;
                    cmd.Parameters.AddWithValue("@IP", (object)ip ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserAgent", (object)ua ?? DBNull.Value);

                    // ✅ parámetro OUTPUT para recibir el ID
                    var pOut = new SqlParameter("@NuevoId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(pOut);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    int nuevoId = (pOut.Value == DBNull.Value) ? 0 : (int)pOut.Value; // ← ID insertado
                    return nuevoId;
                }
            }
            catch (Exception er)
            {
                return 0;

            }
            return 0;

        }

        // Parseo mínimo del form plano "a=1&b=2..."
        private static NameValueCollection ParseFormPlano(string formDataPlano)
        {
            return HttpUtility.ParseQueryString(formDataPlano ?? string.Empty);
        }

        // Mapea lo mínimo a tu entidad de cita; ajusta a tu modelo real
        
        [HttpPost]
        public JsonResult VerifySmsAndSave(Entities.ViewModels.AppointmentsView appointment, string OtpCode)
        {
            string idUnidad = (string)Session[keyIdUnidad];
            Entities.Employees eEmployee = null;
            eEmployee = (Entities.Employees)Session["User"];
            appointment.carnet = eEmployee.EmployeeNumber;
            // ---------- 1) OTP en Session ----------
            var info = Session[OTP_SESSION_KEY] as OtpInfo;
            var numeroCita = new string((appointment?.Telefono ?? "").Where(char.IsDigit).ToArray());
            var otpIngresado = (OtpCode ?? string.Empty).Trim();
         
            var codigoIngresado = (OtpCode ?? string.Empty).Trim();
            appointment.Telefono = info.Numero;
            if (info == null)
            {
                  LogVerificacionCodigoAsync(
                   appointment.Telefono, codigoIngresado, null, false, eEmployee.EmployeeNumber, null, "OTP inexistente en sesión-"+ idUnidad, idUnidad);

                return Json(new { status = "Error", message = "No se ha solicitado el código o ya expiró. Vuelva a solicitarlo." });
            }
            if (DateTime.UtcNow > info.ExpiresAt)
            {
                Session.Remove(OTP_SESSION_KEY);
                LogVerificacionCodigoAsync(
                  appointment.Telefono,  codigoIngresado, info.Code, false, eEmployee.EmployeeNumber, null, "El código ha expirado. Solicite uno nuevo.-" + idUnidad, idUnidad);

                return Json(new { status = "Error", message = "El código ha expirado. Solicite uno nuevo." });
            }

            info.Attempts++;
            if (info.Attempts > 5)
            {
                Session.Remove(OTP_SESSION_KEY);
                LogVerificacionCodigoAsync(
             appointment.Telefono, codigoIngresado, info.Code, false, eEmployee.EmployeeNumber, null, "Demasiados intentos fallidos. Solicite un nuevo código.-" + idUnidad, idUnidad);

                return Json(new { status = "Error", message = "Demasiados intentos fallidos. Solicite un nuevo código." });
            }

            if (!string.Equals(info.Code, otpIngresado, StringComparison.Ordinal))
            {
                Session[OTP_SESSION_KEY] = info; // persistir intento incrementado
                LogVerificacionCodigoAsync(
                   appointment.Telefono, codigoIngresado, info.Code, false, eEmployee.EmployeeNumber, null, "Código incorrecto.-" + idUnidad, idUnidad);

                return Json(new { status = "Error", message = "Código incorrecto." });
            }

            // Para un solo uso, invalidamos el OTP si deseas:

            // Asegurar que el número verificado coincide con el de la cita
                
            // if (!string.Equals(numeroCita, (info.Numero ?? string.Empty).Trim(), StringComparison.Ordinal))
            //{
            //    LogVerificacionCodigoAsync(
            //        numeroCita, codigoIngresado, info.Code, false, eEmployee.EmployeeNumber, null, "El número de WhatsApp/SMS no coincide con el verificado por SMS.-" + idUnidad);

            //    return Json(new { status = "Error", message = "El número de WhatsApp/SMS no coincide con el verificado por SMS." });
            //}

             

            if (appointment.FechaCita == default(DateTime))
                return Json(new { status = "Error", message = "La fecha de cita es obligatoria." });

            if (appointment.KilometrajeActual < 0)
                return Json(new { status = "Error", message = "El Kilometraje Actual no puede ser negativo." });

            // ---------- 3) Detalle en Session ----------
            const string keyDetail = "sAppointmentDetail";
            var lstDetail = Session[keyDetail] as List<Entities.AppointmentsDetail>;
            if (lstDetail == null || lstDetail.Count == 0)
                return Json(new { status = "Error", message = "Debe agregar al menos un detalle de servicio antes de guardar." });

            // Validar cada línea (mismo criterio que aplicas en UI)
            foreach (var d in lstDetail)
            {
                if (string.IsNullOrWhiteSpace(d.IdTipoServicio))
                    return Json(new { status = "Error", message = "Todas las filas deben tener un Tipo de Servicio." });
                if (string.IsNullOrWhiteSpace(d.Observacion))
                    return Json(new { status = "Error", message = "Todas las filas deben tener Observación." });
            }

            // ---------- 4) Validación de negocio existente ----------
            try
            {
                // Si tu lógica original usaba el km de Session, respétalo:
                int kmSesion = 0;
                if (Session["Kilometraje"] != null)
                {
                    int.TryParse(Session["Kilometraje"].ToString(), out kmSesion);
                }

                appointment.IdUnidad = idUnidad;
                appointment.FechaConfirmacionCita = appointment.FechaCita;

             
                var valMsg = new Appointment().ValidarSolicitudCita(appointment, kmSesion);
                if (!string.Equals(valMsg, "ok", StringComparison.OrdinalIgnoreCase))
                    return Json(new { status = "Error", message = valMsg });
            }
            catch (Exception exVal)
            {
                return Json(new { status = "Error", message = "Error en la validación de la solicitud: " + exVal.Message });
            }
            if (appointment.IdCita == 0  )
            {
                appointment.IdCita = -1;

            }
            // ---------- 5) Persistencia: Guardar cita ----------
            string idCitaStr = null;
            try
            {
                idCitaStr = Data.Appointment.GuardarCita(appointment);
                if (!Data.Appointment.isNumeric(idCitaStr))
                    return Json(new { status = "Error", message = "Ha ocurrido un error al guardar la cita." });
            }
            catch (Exception exSave)
            {
                return Json(new { status = "Error", message = "Error al guardar la cita: " + exSave.Message });
            }

            var idCita = int.Parse(idCitaStr);

            // ---------- 6) Guardar detalle usando la Session ----------
            try
            {
                // Tu método ya inserta el detalle tomando los ítems de Session y asocia a la cita
                var idDetalleStr = Data.Appointment.GuardarDetalleCita(idCita);
                if (!Data.Appointment.isNumeric(idDetalleStr))
                {
                    // rollback
                    Data.Appointment.DeleteAppointmentDetail(idCitaStr);
                    Data.Appointment.DeleteAppointment(idCitaStr);
                    return Json(new { status = "Error", message = "Error al insertar el detalle de la cita." });
                }

                // ---------- 7) Estado inicial (tal como lo tenías) ----------
                if (appointment.IdCita == -1 || appointment.IdCita == 0)
                {
                     //var appointmentStatus = new Entities.AppointmentsStatus
                    //{
                    //    IdCita = int.Parse(idDetalleStr),
                    //    IdEstado = "1701",
                    //    IdPersona = eEmployee.Idhrms.ToString(),
                    //    EsActivo = "Y",
                    //    IpLocal = Utils.ObtenerIpLocal(string.Empty),
                    //    UsuarioDominioInserto = Utils.ObtenerUsuarioDominio(string.Empty),
                    //    MotivoAnulacion = string.Empty,
                    //    idUbicacion = string.Empty,
                    //    FechaInserto = DateTime.Now,
                    //    carnet = eEmployee?.EmployeeNumber
                    //};
                    Entities.AppointmentsStatus appointmentStatus = new Entities.AppointmentsStatus();


                    appointmentStatus.IdCita = int.Parse(idDetalleStr);
                    appointmentStatus.IdEstado = "1701";
                    appointmentStatus.IdPersona = eEmployee.Idhrms.ToString();
                    appointmentStatus.EsActivo = "Y";
                    appointmentStatus.IpLocal = Utils.ObtenerIpLocal(string.Empty);
                    appointmentStatus.UsuarioDominioInserto = Utils.ObtenerUsuarioDominio(string.Empty);
                    appointmentStatus.MotivoAnulacion = string.Empty;
                    appointmentStatus.idUbicacion = string.Empty;
                    appointmentStatus.FechaInserto = DateTime.Now;
                    appointmentStatus.carnet = eEmployee.EmployeeNumber;
                    var resultadoEstado = Data.Appointment.InsertAppointmentStatus(appointmentStatus);
                    if (!string.Equals(resultadoEstado, idDetalleStr, StringComparison.Ordinal))
                    {
                        // rollback completo
                        Data.Appointment.DeleteAppointmentDetail(idCitaStr);
                        Data.Appointment.DeleteAppointment(idCitaStr);
                        return Json(new { status = "Error", message = "Error al insertar el estado de la cita." });
                    }

                    // (Opcional) EnviarCorreo(appointment, "Solicitar");
                }

                // (Opcional) limpiar detalles en sesión tras guardado exitoso
                Session[keyDetail] = new List<Entities.AppointmentsDetail>();
            }
            catch (Exception exDet)
            {
                // rollback completo en cualquier error de detalle/estado
                try { Data.Appointment.DeleteAppointmentDetail(idCitaStr); } catch { }
                try { Data.Appointment.DeleteAppointment(idCitaStr); } catch { }
                return Json(new { status = "Error", message = "Error al procesar el detalle/estado: " + exDet.Message });
            }
            Session.Remove(OTP_SESSION_KEY);

            // ---------- 8) Éxito ----------
            return Json(new { status = "Exito", message = "Éxito al registrar la cita.", idCita = idCita });
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult GuardarCita(Entities.ViewModels.AppointmentsView appointment)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string resultadoEstado = string.Empty;
            string resultadoBorrarCita = string.Empty;
            string resultadoBorrarDetalleCita = string.Empty;
            string validar = string.Empty;
            string idUnidad = (string)Session[keyIdUnidad];
            if (Session[keyKilometraje]==null)
            {
                Session[keyKilometraje] = Data.Appointment.kilometroanterio((string)Session[keyIdUnidad]);

            }
            if (appointment.Telefono!=null )
            {
                appointment.Telefono = appointment.Telefono.Replace("-", ""); 
            }
            int idkilometrajes = (int)Session[keyKilometraje];
            Appointment vAppointment = new Appointment(); 
            appointment.IdUnidad = idUnidad;
            appointment.FechaConfirmacionCita = appointment.FechaCita;
            
            Entities.Employees eEmployee = null;
            eEmployee = (Entities.Employees)Session["User"];
            appointment.carnet = eEmployee.EmployeeNumber;
            //Llamar al método de insertar cita
            try
            {
                validar = vAppointment.ValidarSolicitudCita(appointment, idkilometrajes);

                if (validar == "ok")
                {
                    if (appointment.IdCita==0 || appointment.IdCita ==null)
                    {
                        appointment.IdCita = -1;

                    }
                    result = Data.Appointment.GuardarCita(appointment);
                    if (Data.Appointment.isNumeric(result))
                    {
                        

                        resultadoDetalle = Data.Appointment.GuardarDetalleCita(int.Parse(result));

                        if (Data.Appointment.isNumeric(resultadoDetalle))
                        {
                            Entities.AppointmentsStatus appointmentStatus = new Entities.AppointmentsStatus();
                          

                            appointmentStatus.IdCita = int.Parse(resultadoDetalle);
                            appointmentStatus.IdEstado = "1701";
                            appointmentStatus.IdPersona = eEmployee.Idhrms.ToString();
                            appointmentStatus.EsActivo = "Y";
                            appointmentStatus.IpLocal = Utils.ObtenerIpLocal(string.Empty);
                            appointmentStatus.UsuarioDominioInserto = Utils.ObtenerUsuarioDominio(string.Empty);
                            appointmentStatus.MotivoAnulacion = string.Empty;
                            appointmentStatus.idUbicacion = string.Empty;
                            appointmentStatus.FechaInserto = DateTime.Now;
                            appointmentStatus.carnet = eEmployee.EmployeeNumber;
                            if (appointment.IdCita == -1)
                            {

                                resultadoEstado = Data.Appointment.InsertAppointmentStatus(appointmentStatus);

                                if (resultadoEstado != resultadoDetalle)
                                {
                                    resultadoBorrarDetalleCita = Data.Appointment.DeleteAppointmentDetail(result);
                                    resultadoBorrarCita = Data.Appointment.DeleteAppointment(result);
                                    return Json(new { status = "Error", message = "Error al insertar el estado de la cita" });
                                }
                                EnviarCorreo(appointment, "Solicitar");
                            }

                        }
                        else
                        {
                            resultadoBorrarDetalleCita = Data.Appointment.DeleteAppointmentDetail(result);

                            resultadoBorrarCita = Data.Appointment.DeleteAppointment(result);

                            return Json(new { status = "Error", message = "Error al insertar el detalle de la cita" });
                        }


                    }
                    else
                    {
                        return Json(new { status = "Error", message = "Ha ocurrido un error al guardar la cita." });
                    }
                }
                else
                {
                    return Json(new { status = "Error", message = validar});
                }

               
            }
            catch (Exception ex)
            {

                return Json(new { status = "Error", message = "Ha ocurrido un error en la transaccion."+ex.Message });
            }


            //ClearDetailSession();
            return Json(new { status = "Exito", message = "Exito al registrar la cita" });

        }

        /// <summary>
        /// Metodo para actualizar el detalle de la cita
        /// </summary>
        /// <param name="updateValues"></param>
        /// <returns></returns>
        public ActionResult UpdateAppointmentDetail(MVCxGridViewBatchUpdateValues<Entities.AppointmentsDetail> updateValues)
        {
            int idCita = (int)Session[keyCita];
            foreach (var item in updateValues.Update)
            {
                if (updateValues.IsValid(item))
                {
                    try
                    {
                        // Data.EventRegistrationDetail.GetAllEventRegistrationDetail(eventRegistrationId);
                        if (!string.IsNullOrEmpty(item.Observacion))
                        {
                            item.Observacion = item.Observacion.ToUpper();
                        }
                        Data.Appointment.EditSessiontDetail(item);
                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                }
            }
            //Actualizacion dedel detalle en la sesion
            foreach (var item in updateValues.Insert)
            {
                if (updateValues.IsValid(item))
                {
                    try
                    {

                        if (!string.IsNullOrEmpty(item.Observacion))
                        {
                            item.Observacion = item.Observacion.ToUpper();
                        }
                        Data.Appointment.AddSessionDetail(item);



                        ////Llamar al metodo EditGoal
                        //SafeExecute(() => Data.EmployeeGoal.EditGoal(item));

                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                }

            }


            return EditPartial(idCita);
        }

        /// <summary>
        /// Metodo para anular una cita
        /// </summary>
        /// <param name="idCita"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AnularCita(int idCita,string estado2)
        {
            if (estado2.Contains("Regist") || estado2.Contains("Guard"))
            {
            }
            else
            {
                return Json(new { status = "Cita", message = "Solo se pueden anular citas en estado REGISTRADO" });
            }
            string resultado = String.Empty;

            var estado = Data.Appointment.GetAppointmentsByIdCita(idCita);

            if (estado.Count > 0)
            {
                string estadoCita = estado.FirstOrDefault().IdEstadoCita;
                if (estadoCita != "1701")
                {
                    return Json(new { status = "Error", message = "Solo se pueden anular citas en estado REGISTRADO" });
                }
            }
          
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }



            Entities.AppointmentsStatus appointmentStatus = new Entities.AppointmentsStatus();
           

            appointmentStatus.IdCita = idCita;
            appointmentStatus.IdEstado = "1703";
            appointmentStatus.IdPersona = eEmployee.Idhrms.ToString();
            appointmentStatus.EsActivo = "Y";
            appointmentStatus.IpLocal = Utils.ObtenerIpLocal(string.Empty);
            appointmentStatus.UsuarioDominioInserto = Utils.ObtenerUsuarioDominio(string.Empty);
            appointmentStatus.MotivoAnulacion = "ANULADA POR USUARIO";
            resultado = Data.Appointment.InsertAppointmentStatus(appointmentStatus);

            if (resultado != "EXITO")
            {
                return Json(new { status = "Error", message = "Error al anular la cita" });
            }
            Entities.ViewModels.AppointmentsView appointment = new AppointmentsView();
            appointment.IdUnidad = estado.FirstOrDefault().IdUnidad;
            appointment.FechaCita = estado.FirstOrDefault().FechaCita;
            EnviarCorreo(appointment, "Anular");
            return Json(new { status = "Exito", message = "Exito al anular la cita" });

        }

        public string EnviarCorreo2(List<Entities.AppointmentsDetail> nuevosDetalles, int idCita)
        {
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            string destinatario = "transporte@claro.com.ni";
            string copia = "transporte@claro.com.ni";

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            List<Entities.TipoServicios> servicios = new List<Entities.TipoServicios>();
            servicios=slnRhonline.Data.TipoServicio.ObtenerTipoServicios();
          
                titulo = "📌 Nueva Solicitud de la Cita:"+idCita;

                // Generar la tabla de detalles en HTML
                string detallesHtml = "<table style='width:100%; border-collapse: collapse; border: 1px solid #ddd;'>";
                detallesHtml += "<tr style='background-color: #dc3545; color: white;'><th style='border: 1px solid #ddd; padding: 8px;'>Tipo de Servicio</th><th style='border: 1px solid #ddd; padding: 8px;'>Observación</th></tr>";

                foreach (var detalle in nuevosDetalles)
                {
                    detallesHtml += $"<tr style='border: 1px solid #ddd;'><td style='padding: 8px;'>{detalle.IdTipoServicio}</td><td style='padding: 8px;'>{detalle.Observacion}</td></tr>";
                }
                detallesHtml += "</table>";

                mensaje = $@"
            <html>
            <head>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        color: #333;
                    }}
                    .container {{
                        padding: 15px;
                        border: 1px solid #ddd;
                        border-radius: 8px;
                        background-color: #f8f9fa;
                    }}
                    .header {{
                        background-color: #dc3545;
                        color: white;
                        padding: 10px;
                        text-align: center;
                        font-size: 20px;
                        font-weight: bold;
                        border-radius: 8px 8px 0 0;
                    }}
                    .content {{
                        padding: 15px;
                        line-height: 1.5;
                    }}
                    .footer {{
                        font-size: 12px;
                        color: #777;
                        padding: 10px;
                        text-align: center;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>🚗 Nueva Solicitud de la Cita:{idCita}</div>
                    <div class='content'>
                        <p>Estimado equipo de transporte,</p>
                        <p>El usuario <strong>{eEmployee.FullName}</strong> ha anexado los siguientes servicios a una cita existente:</p>
                        {detallesHtml}
                        <p>Por favor, revise y gestione la nueva solicitud en el sistema.</p>
                    </div>
                    <div class='footer'>
                        Este es un mensaje generado automáticamente. Por favor, no responda a este correo.
                    </div>
                </div>
            </body>
            </html>";
      

            resultadoCorreo = Utils.EnviarCorreoUsuario(destinatario, titulo, copia, mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transacción se generó exitosamente, pero ocurrió un error al enviar el correo.";
            }

            return resultadoCorreo;
        }
        public string EnviarCorreo3(List<Entities.AppointmentsDetail> nuevosDetalles, int id)
        {
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            string destinatario = "gustavo.lira@claro.com.ni";
            string copia = "gustavo.lira@claro.com.ni";

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            List<Entities.TipoServicios> servicios = new List<Entities.TipoServicios>();
            servicios = slnRhonline.Data.TipoServicio.ObtenerTipoServicios();
            // Obtener lista de tipos de servicios para convertir IdTipoServicio a NombreTipoServicio
             var servicioDiccionario = servicios.ToDictionary(s => s.IdTipoServicio, s => s.NombreTipoServicio);

            

                // Generar la tabla de detalles en HTML con nombres en lugar de IDs
                string detallesHtml = "<table style='width:100%; border-collapse: collapse; border: 1px solid #ddd;'>";
                detallesHtml += "<tr style='background-color: #dc3545; color: white;'><th style='border: 1px solid #ddd; padding: 8px;'>Tipo de Servicio</th><th style='border: 1px solid #ddd; padding: 8px;'>Observación</th></tr>";
                 foreach (var detalle in nuevosDetalles)
                {
                     string nombreServicio = servicioDiccionario.ContainsKey(detalle.IdTipoServicio) ? servicioDiccionario[detalle.IdTipoServicio] : "Desconocido";
                    detallesHtml += $"<tr style='border: 1px solid #ddd;'><td style='padding: 8px;'>{nombreServicio}</td><td style='padding: 8px;'>{detalle.Observacion}</td></tr>";
                }
                titulo = "📌 Nueva modificacion en la Cita "+id;

                detallesHtml += "</table>";

                mensaje = $@"
            <html>
            <head>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        color: #333;
                    }}
                    .container {{
                        padding: 15px;
                        border: 1px solid #ddd;
                        border-radius: 8px;
                        background-color: #f8f9fa;
                    }}
                    .header {{
                        background-color: #dc3545;
                        color: white;
                        padding: 10px;
                        text-align: center;
                        font-size: 20px;
                        font-weight: bold;
                        border-radius: 8px 8px 0 0;
                    }}
                    .content {{
                        padding: 15px;
                        line-height: 1.5;
                    }}
                    .footer {{
                        font-size: 12px;
                        color: #777;
                        padding: 10px;
                        text-align: center;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>🚗 Nueva modificacion en la Cita {id}</div>
                    <div class='content'>
                        <p>Estimado equipo de transporte,</p>
                        <p>El usuario <strong>{eEmployee.FullName}</strong> ha anexado los siguientes servicios a una cita existente:</p>
                        {detallesHtml}
                        <p>Por favor, revise y gestione la nueva solicitud en el sistema SIAF en el modulo de cita.</p>
                    </div>
                    <div class='footer'>
                        Este es un mensaje generado automáticamente. Por favor, no responda a este correo.
                    </div>
                </div>
            </body>
            </html>";
           

            resultadoCorreo = Utils.EnviarCorreoUsuario(destinatario, titulo, copia, mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transacción se generó exitosamente, pero ocurrió un error al enviar el correo.";
            }

            return resultadoCorreo;
        }

        public string EnviarCorreo(Entities.ViewModels.AppointmentsView appointment,string tipoCorreo)
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


            destinatario = "transporte@claro.com.ni";
                copia = "transporte@claro.com.ni";

            if (tipoCorreo == "Solicitar")
            {
                titulo = "Solicitud de cita pendiente";
                mensaje = "Estimado equipo de transporte:" +
                             "<br/>" +
                             "<br/>" + "El usuario +" + eEmployee.FullName + "+ a generado una solicitud de cita pendiente de revisar de la unidad:" +
                               "<br/>" +
                             " " + appointment.IdUnidad + " para la fecha " + appointment.FechaCita.ToString("D", new CultureInfo("es-ES")) + ".";

            }
            if (tipoCorreo == "Anular")
            {
                titulo = "Anulación de cita";
                mensaje = "Estimado equipo de transporte:" +
                             "<br/>" +
                             "<br/>" + "El usuario +" + eEmployee.FullName + "+ ha anulado la cita para la unidad:" +
                               "<br/>" +
                             " " + appointment.IdUnidad + " para la fecha " + appointment.FechaCita.ToString("D",new CultureInfo("es-ES")) + ".";
               

            }


            resultadoCorreo = Utils.EnviarCorreoUsuario(destinatario, titulo, copia, mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transaccion se genero exitosamente, pero ocurrió un error al enviar el correo";

            }


            return resultadoCorreo;
        }
        private static readonly string _baseUrl = "http://localhost:3000"; // <- cambia host/puerto

        // Valida formato NI local (rápido, evita llamada si es basura)
        public static bool EsMovilNica(string numero)
        {
            var s = (numero ?? "").Trim();
            var solo = Regex.Replace(s, @"\D", "");
            if (solo.Length == 8) solo = "505" + solo;
            return Regex.IsMatch(solo, @"^505[578]\d{7}$");
        }

        // Verificar 1 número usando GET /verificar?numero=
        public static bool VerificarUno(string numero  )
        {
            

            var url = "http://localhost:3000/verificar?numero=" + numero;
            var client = new RestClient(url) { Timeout = 30000 }; // 15s
            var req = new RestRequest(Method.GET);
 
            IRestResponse resp = null;
            try { resp = client.Execute(req); }
            catch (Exception ex) {   return false; }

            if (resp == null || string.IsNullOrWhiteSpace(resp.Content))
            {   return false; }

            WaRespuesta obj = null;
            try {
                obj = JsonConvert.DeserializeObject<WaRespuesta>(resp.Content);
                if (obj.data.Count()!=0)
                {
                    if (obj.data.FirstOrDefault().registrado==true)
                    {
                        return true;
                    }
                    else { return false; }

                }
                return false;
            }
            catch (Exception ex) {
                return false;
            }

            if (obj == null) {  return false; }
            if (!obj.ok)
            {
                // puede venir msg: "WhatsApp no listo. Escanee el QR."
                
                return false;
            }

            if (obj.data == null || obj.data.Count == 0)
            { return false; }

            
            return true;
        }

        #endregion
    }
    public class WaRespuesta
    {
        public bool ok { get; set; }
        public int total { get; set; }       // en POST puede venir
        public List<WaItem> data { get; set; }
        public string msg { get; set; }      // en errores (503, 400, 429)
    }
    public class WaItem
    {
        public string numero { get; set; }
        public string normalizado { get; set; }
        public bool valido { get; set; }
        public bool registrado { get; set; }
        public string motivo { get; set; }   // opcional si viene "formato_invalido"
        public string error { get; set; }    // opcional si el API manda "error"
    }
}