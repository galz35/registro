using Dapper;
using Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace slnRhonline.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIAF; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos

        // GET: Notifications
        public ActionResult Index()
        {
            return View();
        }

        // GET: Notifications/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Notifications/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Notifications/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Notifications/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Notifications/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Notifications/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Notifications/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        [HttpGet]
        public ActionResult GetSolicitudDetails(int id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT 
                        tcd.IdCitaDetalle, tcd.IdCita,      TipoOrden.Descripcion AS TipoOrden,
 tcd.Observacion,  tcd.Anexo,  tcd.Fecha,  tcd.fechaanexo, 
                         tcd.Aplica,  tcd.Aprobacion,  tcd.Usuario,  tcd.FechaAprobacion,  tcd.TipoRegistro,  tcd.Autorizacion,  tcd.FechaA,  tcd.Foto
                    FROM TallerCitasDetalle tcd
 INNER JOIN TallerTipoServicio tts ON tts.IdTipoServicio = tcd.IdTipoServicio
INNER JOIN GeneralCatalogos TipoOrden ON tts.IdTipoSolicitud = TipoOrden.Id_Catalogo
                    WHERE tcd.IdCita = @IdCita   and Anexo='SI' and TipoRegistro='A' ";
                var details = db.Query<SolicitudDetailsViewModel>(query, new { IdCita = id }).ToList();
                if (details != null && details.Count()>0)
                {

               var resultado = details
    .Select(s => new Solicitudsiaf
    {
        TipoOrden = s.TipoOrden,
        Observacion = s.Observacion,
        IdCita = s.IdCita
    })
    .ToList();
                    Session["solicitudlista"] = resultado; }
                // Convertir la imagen a Base64 para cada registro si existe
                foreach (var detail in details)
                {
                    if (detail.Foto != null && detail.Foto.Length > 0)
                    {
                        detail.FotoBase64 =   Convert.ToBase64String(detail.Foto);
                    }
                   ;

                }

                return Json(details, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult Approve(int id)
        {
            // Aquí se marca la solicitud como aprobada (actualiza la base de datos)
            // Ejemplo:
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                string query = @"
            UPDATE TallerCitasDetalle
            SET Aprobacion = @Aprobacion, 
                fechaanexo = GETDATE(), 
                FechaAprobacion = GETDATE(), 
                Autorizacion = @Autorizacion
            WHERE IdCita = @IdCita and Anexo='SI' and TipoRegistro='A'";
                int rowsAffected = db.Execute(query, new
                {
                    Aprobacion = "Aprobar",
                    Autorizacion = eEmployee.EmployeeNumber,
                    IdCita = id
                });
                //string query = @"
                //    UPDATE TallerCitasDetalle
                //    SET   FechaAprobacion = GETDATE(), Autorizacion = @Autorizacion
                //    WHERE IdCita = @IdCita";
                List<CitaSolicitud> solicitudList1 = new List<CitaSolicitud>();

                solicitudList1 = (List<CitaSolicitud>)Session["solicitudlista1"];
                CitaSolicitud solicitud1 = solicitudList1.Where(x => x.IdCita == id).FirstOrDefault();
                List<Solicitudsiaf> solicitudList = new List<Solicitudsiaf>();
                solicitudList = (List<Solicitudsiaf>)Session["solicitudlista"];
                Solicitudsiaf solicitud = solicitudList.Where(x => x.IdCita == id).FirstOrDefault();
                string respuesta = EnviarCorreoTransporte(solicitud1, "Aprobar", solicitudList,""  );
                return Json(new { success = rowsAffected > 0, message = rowsAffected > 0 ? "Solicitud aprobada." : "No se pudo aprobar." });
            }
        }

        // POST: Solicitud/Deny?id=...
        [HttpPost]
        public ActionResult Deny(int id,string motivo)
        {
            // Aquí se marca la solicitud como denegada (actualiza la base de datos)
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                string query = @"
            UPDATE TallerCitasDetalle
            SET Aprobacion = @Aprobacion, 
                FechaAprobacion = GETDATE(), 
                fechaanexo = GETDATE(), 
                Autorizacion = @Autorizacion
            WHERE IdCita = @IdCita and Anexo='SI' and TipoRegistro='A'";
                int rowsAffected = db.Execute(query, new
                {
                    Aprobacion = "Denegar",
                    Autorizacion = eEmployee.EmployeeNumber,
                    IdCita = id
                });
                List<CitaSolicitud> solicitudList1 = new List<CitaSolicitud>();

                solicitudList1= (List<CitaSolicitud>)Session["solicitudlista1"] ;
                CitaSolicitud solicitud1 = solicitudList1.Where(x => x.IdCita == id).FirstOrDefault();

                List<Solicitudsiaf> solicitudList = new List<Solicitudsiaf>();
                 solicitudList= (List<Solicitudsiaf>)Session["solicitudlista"] ;
                Solicitudsiaf solicitud = solicitudList.Where(x => x.IdCita == id).FirstOrDefault();
                string respuesta = EnviarCorreoTransporte(solicitud1, "Denegar", solicitudList,motivo);
                return Json(new { success = rowsAffected > 0, message = rowsAffected > 0 ? "Solicitud denegada." : "No se pudo denegar." });
            }
        }
        public string EnviarCorreoTransporte(CitaSolicitud appointment, string tipoCorreo, List<Solicitudsiaf> x1, string motivo = "")
        {
            // Variables para título, mensaje, resultado y destinatarios
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            string destinatario = "transporte@claro.com.ni";
            string copia = "transporte@claro.com.ni";

            // Obtiene el empleado de la sesión
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            // Genera la tabla HTML a partir de la lista x1 de solicitudes Siaf
            string tableHtml = "";
            if (x1 != null && x1.Count > 0)
            {
                tableHtml += "<table style='width:100%; border-collapse: collapse; border: 1px solid #ddd;'>";
                tableHtml += "<tr style='background-color: #007bff; color: white;'><th style='border: 1px solid #ddd; padding: 8px;'>Tipo de Orden</th><th style='border: 1px solid #ddd; padding: 8px;'>Observación</th></tr>";
                foreach (var item in x1)
                {
                    tableHtml += $"<tr style='border: 1px solid #ddd;'><td style='padding: 8px;'>{item.TipoOrden}</td><td style='padding: 8px;'>{item.Observacion}</td></tr>";
                }
                tableHtml += "</table>";
            }
            else
            {
                tableHtml = "<p>No se encontraron detalles adicionales.</p>";
            }

            // Construye el correo según el tipo (Aprobar o Denegar)
            if (tipoCorreo.Equals("Aprobar", StringComparison.OrdinalIgnoreCase))
            {
                titulo = "Aprobación de Solicitud de Transporte - Unidad "+ appointment.IdUnidad+ " - Cita #" + appointment.IdCita;
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
      background-color: #28a745;
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
    table {{
      width: 100%;
      border-collapse: collapse;
    }}
    th, td {{
      border: 1px solid #ddd;
      padding: 8px;
    }}
    th {{
      background-color: #007bff;
      color: white;
    }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>🚗 Aprobación de Solicitud de Transporte - Unidad:{appointment.IdUnidad} - Cita #{appointment.IdCita}</div>
    <div class='content'>
      <p>Estimado equipo de transporte,</p>
      <p>El usuario <strong>{eEmployee?.FullName}</strong> ha <strong>APROBADO</strong> la solicitud de transporte para la unidad <strong>{appointment.IdUnidad}.</p>
      <p>A continuación, se muestran los detalles de la solicitud:</p>
      {tableHtml}
    </div>
    <div class='footer'>
      Este es un mensaje generado automáticamente. Por favor, no responda a este correo.
    </div>
  </div>
</body>
</html>";
            }
            else if (tipoCorreo.Equals("Denegar", StringComparison.OrdinalIgnoreCase))
            {
                titulo = "Denegación de Solicitud de Transporte - Unidad " + appointment.IdUnidad + " - Cita #" + appointment.IdCita;
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
    table {{
      width: 100%;
      border-collapse: collapse;
    }}
    th, td {{
      border: 1px solid #ddd;
      padding: 8px;
    }}
    th {{
      background-color: #007bff;
      color: white;
    }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>🚗 Denegación de Solicitud de Transporte - Unidad:{appointment.IdUnidad} - Cita #{appointment.IdCita}</div>
    <div class='content'>
      <p>Estimado equipo de transporte,</p>
      <p>El usuario <strong>{eEmployee?.FullName}</strong> ha <strong>DENEGADO</strong> la solicitud de transporte para la unidad <strong>{appointment.IdUnidad}.</p>
      <p>Motivo: <strong>{motivo}</strong></p>
      <p>A continuación, se muestran los detalles de la solicitud:</p>
      {tableHtml}
    </div>
    <div class='footer'>
      Este es un mensaje generado automáticamente. Por favor, no responda a este correo.
    </div>
  </div>
</body>
</html>";
            }
            else
            {
                return "Tipo de correo no definido.";
            }

            // Se usa el correo del empleado, si existe, o se usa un correo por defecto
            string correo = eEmployee != null ? eEmployee.EmailAddress : "gustavo.lira@claro.com.ni";
            // Envía el correo usando el método utilitario
            resultadoCorreo = Utils.EnviarCorreoUsuario(correo, titulo, correo, mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transacción se generó exitosamente, pero ocurrió un error al enviar el correo.";
            }
            return resultadoCorreo;
        }

        [HttpGet]
        public async Task<JsonResult> GetNotifications()
        {
            try
            {
                // Obtén el empleado desde la sesión (si no existe, usa valores de prueba)
                //var employee = Session["User"] as Employees;
                //var carnet = employee != null ? employee.EmployeeNumber : "112568";
                //var personId = employee != null ? employee.GERENCIAIDHRMS : "0";
                var employee = Session["User"] as Entities.Employees;
                var carn = employee != null ? employee.EmployeeNumber : "0";
                var personId = employee != null ? employee.GERENCIAIDHRMS : "0";
                  if (employee != null)
                {

            
                // --- Consumir API 1: Citas Pendientes ---
                List<  Periodovt > citaPendienteList = new List<Periodovt>();
                var client1 = new RestClient("http://172.26.54.66/apihcm/api/values/cita/citapendiente?carnet=" + carn);
                var request1 = new RestRequest(Method.GET);
                request1.Timeout = -1;
                var tcs1 = new TaskCompletionSource<IRestResponse>();
                client1.ExecuteAsync(request1, (response, handle) =>
                {
                    tcs1.SetResult(response);
                });
                var response1 = await tcs1.Task;

                if (response1.Content!=null && response1.Content!=null)
                {
                    var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                    citaPendienteList = serializer.Deserialize<List<Periodovt>>(response1.Content);
                }

                // --- Consumir API 2: Solicitudes ---
                // Consumir API 2: Solicitudes
                List<CitaSolicitud> solicitudList = new List<CitaSolicitud>();
                var client2 = new RestClient("http://172.26.54.66/apihcm/api/values/cita/solicitud?carnet=" + carn);
                var request2 = new RestRequest(Method.GET);
                request2.Timeout = -1;

                var tcs2 = new TaskCompletionSource<IRestResponse>();
                client2.ExecuteAsync(request2, (response, handle) =>
                {
                    tcs2.SetResult(response);
                });
                var response2 = await tcs2.Task;

                if (response2.Content != null && response2.Content != null)
                {
                    var serializer2 = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                    solicitudList = serializer2.Deserialize<List<CitaSolicitud>>(response2.Content);
                    Session["solicitudlista1"] = solicitudList;
                }

                // Arma el ViewModel con ambas listas
                var viewModel = new NavNotificationsViewModel
                {
                    CitaPendienteCount = citaPendienteList.Count,
                    SolicitudCount = solicitudList.GroupBy(x=>x.IdCita).Count(),
                    CitaPendienteList = citaPendienteList,
                    SolicitudList = solicitudList
                };

                return Json(viewModel, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var viewModel = new NavNotificationsViewModel();
                    return Json(viewModel, JsonRequestBehavior.AllowGet);

                }
            }
            catch (Exception ex)
            {
                return Json(new { Error = "Error al cargar notificaciones: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
