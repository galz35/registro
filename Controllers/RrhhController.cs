using ClosedXML.Excel;
using Dapper;
using Datos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RestSharp;
using slnRhonline.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace slnRhonline.Controllers
{
    public class RrhhController : Controller
    {
        private readonly string _connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIAF; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos
        private readonly string connStr = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIGHO1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos
                                                                                                                                                                                            // ====== CONTROLLER: utilidades comunes ======
 
    private const string SessionKey = "ContactosExcel";
        private const string SessOk = "HCM_OK";
        private const string SessExist = "HCM_EXIST";
        private const string SessErr = "HCM_ERR";
        private static string Canon(string s) => (s ?? "").Trim().ToUpperInvariant();

        private async Task<List<ContactItem>> FetchContactItemsAsync(string relatedPersonNumber)
        {
            var uri = "https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/hcmContacts" +
                    "?q=contactRelationships.RelatedPersonNumber=" + relatedPersonNumber +
                    "&expand=names,contactRelationships&onlyData=true";

            const string user = "Claro_RhOnline_WS_SS";
            const string pwd = "HCM-RH0nl1ne@#3";

            string auth = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(user + ":" + pwd));
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers.Add("Authorization", "Basic " + auth);
            request.Headers.Add("REST-Framework-Version", "2");

            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string json = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(json))
                return new List<ContactItem>();

            ContactosHCM root = JsonConvert.DeserializeObject<ContactosHCM>(json);
            if (root != null && root.items != null)
                return root.items;

            return new List<ContactItem>();
        }
        
        private async Task<bool> ContactoExisteAsync(
    string relatedPersonNumber, string tipoRelacion,
    string first, string middle, string last, string secondLast)
        {
 
            var items = await FetchContactItemsAsync(relatedPersonNumber);

            foreach (var it in items)
            {
                // it.contactRelationships: List<ContactRelationship>
                if (it.contactRelationships == null) continue;

                var coincideRelacion = it.contactRelationships.Any(r =>
                    r.RelatedPersonNumber == relatedPersonNumber && // compara número
                    r.ContactType == tipoRelacion                   // compara tipo directo
                );
                if (!coincideRelacion) continue;

                var nm = it.names?.FirstOrDefault();
                if (nm != null && NombreMatch(nm, first, middle, last, secondLast))
                    return true;
            }
            return false;
        }
        private bool NombreMatch(
    Name nm,
    string first, string middle, string last, string secondLast)
        {
            // Compara nombre, segundo nombre, apellido y segundo apellido (case-insensitive)
            return string.Equals(nm.FirstName ?? "", first ?? "", StringComparison.OrdinalIgnoreCase)
                && string.Equals(nm.MiddleNames ?? "", middle ?? "", StringComparison.OrdinalIgnoreCase)
                && string.Equals(nm.LastName ?? "", last ?? "", StringComparison.OrdinalIgnoreCase)
                && string.Equals(nm.NameInformation1 ?? "", secondLast ?? "", StringComparison.OrdinalIgnoreCase);
        }


        private string BuildNombreKey(string first, string middle, string last, string secondLast)
            => string.Join("|", Canon(first), Canon(middle), Canon(last), Canon(secondLast));
        [HttpGet]
        public ActionResult CargaMasiva() { return View(); }
        private void EnsureResultLists()
        {
            if (Session[SessOk] == null) Session[SessOk] = new List<HcmResultadoVm>();
            if (Session[SessExist] == null) Session[SessExist] = new List<HcmResultadoVm>();
            if (Session[SessErr] == null) Session[SessErr] = new List<HcmResultadoVm>();
        }

        // Quita de la sesión del preview el registro ya procesado (por llave razonable)
        private void RemoveFromPreview(ContactoExcelVm x)
        {
            var list = Session[SessionKey] as List<ContactoExcelVm>;
            if (list == null) return;

            var idx = list.FindIndex(r =>
                Canon(r.Carnet) == Canon(x.Carnet) &&
                Canon(r.PrimerNombre) == Canon(x.PrimerNombre) &&
                Canon(r.PrimerApellido) == Canon(x.PrimerApellido) &&
                Canon(r.TipoRelacion) == Canon(x.TipoRelacion) &&
                Canon(r.SegundoNombre) == Canon(x.SegundoNombre) &&
                Canon(r.SegundoApellido) == Canon(x.SegundoApellido)
            );
            if (idx >= 0)
            {
                list.RemoveAt(idx);
                Session[SessionKey] = list;
            }
        }

        [HttpGet]
        public JsonResult Preview()
        {
            var data = Session[SessionKey] as List<ContactoExcelVm> ?? new List<ContactoExcelVm>();
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<JsonResult> EnviarHcm(ContactoExcelVm x)
        {
            if (x == null)
                return Json(new { status = "error", message = "Sin datos." });

            EnsureResultLists();

            // 1) ¿Ya existe?
            try
            {
                var yaExiste = await ContactoExisteAsync(
                    x.Carnet, x.TipoRelacion,
                    x.PrimerNombre, x.SegundoNombre, x.PrimerApellido, x.SegundoApellido);

                if (yaExiste)
                {
                    RemoveFromPreview(x);

                    var existe = new HcmResultadoVm
                    {
                        Carnet = x.Carnet,
                        TipoRelacion = x.TipoRelacion,
                        Nombre = $"{x.PrimerApellido} {x.SegundoApellido} {x.PrimerNombre} {x.SegundoNombre}".Replace("  ", " ").Trim(),
                        Estado = "EXISTE",
                        Mensaje = "El contacto ya existe en HCM."
                    };
                    (Session[SessExist] as List<HcmResultadoVm>).Add(existe);

                    return Json(new
                    {
                        status = "exist",
                        message = "Ya existía",
                        counters = Counters(),
                        item = existe
                    });
                }
            }
            catch (Exception exChk)
            {
                // No detengas el flujo por un fallo de consulta; lo marcamos como error y salimos
                RemoveFromPreview(x);
                var ei = new HcmResultadoVm
                {
                    Carnet = x.Carnet,
                    TipoRelacion = x.TipoRelacion,
                    Nombre = $"{x.PrimerApellido} {x.SegundoApellido} {x.PrimerNombre} {x.SegundoNombre}".Replace("  ", " ").Trim(),
                    Estado = "ERROR",
                    Mensaje = "Fallo validando existencia: " + exChk.Message
                };
                (Session[SessErr] as List<HcmResultadoVm>).Add(ei);

                return Json(new { status = "error", message = ei.Mensaje, counters = Counters(), item = ei });
            }

            // 2) Armar payload (con campos opcionales)
            var names = new List<Name>
        {
            new Name
            {
                LastName = x.PrimerApellido,
                FirstName = x.PrimerNombre,
                MiddleNames = string.IsNullOrWhiteSpace(x.SegundoNombre) ? null : x.SegundoNombre,
                LegislationCode = "NI",
                NameInformation1 = string.IsNullOrWhiteSpace(x.SegundoApellido) ? null : x.SegundoApellido
            }
        };

            List<LegislativeInfo> legis = null;
            if (!string.IsNullOrWhiteSpace(x.Genero))
            {
                legis = new List<LegislativeInfo>
            {
                new LegislativeInfo { Gender = x.Genero, LegislationCode = "NI" }
            };
            }

            var rel = new List<ContactRelationship>
        {
            new ContactRelationship
            {
                RelatedPersonNumber = x.Carnet,
                ContactType = x.TipoRelacion,
                LegislationCode = "NI",
                EmergencyContactFlag = false,
                PrimaryContactFlag = false
            }
        };

            var payload = new HcmContactRequest
            {
                names = names,
                contactRelationships = rel
            };
            if (!string.IsNullOrWhiteSpace(x.FechaNacimiento))
                payload.DateOfBirth = x.FechaNacimiento;
            if (legis != null)
                payload.legislativeInfo = legis;

            // 3) Llamar HCM
            try
            {
                var respuesta = await CrearContactoHcmAsync(payload);

                // ¿Oracle devolvió error en JSON?
                if (!string.IsNullOrEmpty(respuesta) &&
                    respuesta.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new Exception(respuesta);

                // Parsear y guardar en DB (mínimos)
                var root = JObject.Parse(respuesta);

                Entities.Employees eEmployee = Session["User"] as Entities.Employees;

                var dbModel = new HcmContactDb
                {
                    PersonNumber = root["PersonNumber"]?.ToString(),
                    FirstName = root["names"]?[0]?["FirstName"]?.ToString(),
                    MiddleNames = root["names"]?[0]?["MiddleNames"]?.ToString(),
                    LastName = root["names"]?[0]?["LastName"]?.ToString(),
                    SecondLastName = root["names"]?[0]?["NameInformation1"]?.ToString(),
                    DateOfBirth = root["DateOfBirth"]?.ToObject<DateTime?>(),
                    Gender = root["legislativeInfo"]?[0]?["Gender"]?.ToString(),
                    ContactType = root["contactRelationships"]?[0]?["ContactType"]?.ToString(),
                    RelatedPersonNumber = root["contactRelationships"]?[0]?["RelatedPersonNumber"]?.ToString(),
                    UploadedByCarnet = eEmployee?.EmployeeNumber
                };

                using (var con = new SqlConnection(connStr))
                {
                    con.Execute("dbo.usp_InsertHcmContact", dbModel, commandType: CommandType.StoredProcedure);
                }

                RemoveFromPreview(x);

                var ok = new HcmResultadoVm
                {
                    Carnet = x.Carnet,
                    TipoRelacion = x.TipoRelacion,
                    Nombre = $"{x.PrimerApellido} {x.SegundoApellido} {x.PrimerNombre} {x.SegundoNombre}".Replace("  ", " ").Trim(),
                    Estado = "OK",
                    Mensaje = "Creado en HCM",
                    PersonNumber = dbModel.PersonNumber
                };
                (Session[SessOk] as List<HcmResultadoVm>).Add(ok);

                return Json(new { status = "ok", message = ok.Mensaje, counters = Counters(), item = ok });
            }
            catch (Exception ex)
            {
                RemoveFromPreview(x);

                var ei = new HcmResultadoVm
                {
                    Carnet = x.Carnet,
                    TipoRelacion = x.TipoRelacion,
                    Nombre = $"{x.PrimerApellido} {x.SegundoApellido} {x.PrimerNombre} {x.SegundoNombre}".Replace("  ", " ").Trim(),
                    Estado = "ERROR",
                    Mensaje = ex.Message
                };
                (Session[SessErr] as List<HcmResultadoVm>).Add(ei);

                return Json(new { status = "error", message = ei.Mensaje, counters = Counters(), item = ei });
            }
        }


        private object Counters()
        {
            var list = Session[SessionKey] as List<ContactoExcelVm> ?? new List<ContactoExcelVm>();
            var ok = (Session[SessOk] as List<HcmResultadoVm>)?.Count ?? 0;
            var ex = (Session[SessExist] as List<HcmResultadoVm>)?.Count ?? 0;
            var er = (Session[SessErr] as List<HcmResultadoVm>)?.Count ?? 0;

            return new { ok, exist = ex, err = er, remaining = list.Count };
        }
        [HttpGet]
        public JsonResult Contadores() => Json(Counters(), JsonRequestBehavior.AllowGet);

        [HttpGet]
        public JsonResult Resultados()
        {
            EnsureResultLists();
            return Json(new
            {
                ok = (List<HcmResultadoVm>)Session[SessOk],
                exist = (List<HcmResultadoVm>)Session[SessExist],
                err = (List<HcmResultadoVm>)Session[SessErr]
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public FileResult DescargarResultados(string tipo)
        {
            EnsureResultLists();
            var data = tipo?.ToLowerInvariant() == "ok" ? (List<HcmResultadoVm>)Session[SessOk]
                     : tipo?.ToLowerInvariant() == "exist" ? (List<HcmResultadoVm>)Session[SessExist]
                     : (List<HcmResultadoVm>)Session[SessErr];

            using (var wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet("Resultados");
                ws.Cell(1, 1).Value = "Carnet";
                ws.Cell(1, 2).Value = "TipoRelacion";
                ws.Cell(1, 3).Value = "Nombre";
                ws.Cell(1, 4).Value = "Estado";
                ws.Cell(1, 5).Value = "Mensaje";
                ws.Cell(1, 6).Value = "PersonNumber";

                int r = 2;
                foreach (var i in data)
                {
                    ws.Cell(r, 1).Value = i.Carnet;
                    ws.Cell(r, 2).Value = i.TipoRelacion;
                    ws.Cell(r, 3).Value = i.Nombre;
                    ws.Cell(r, 4).Value = i.Estado;
                    ws.Cell(r, 5).Value = i.Mensaje;
                    ws.Cell(r, 6).Value = i.PersonNumber;
                    r++;
                }
                ws.Columns().AdjustToContents();

                using (var ms = new System.IO.MemoryStream())
                {
                    wb.SaveAs(ms);
                    var bytes = ms.ToArray();
                    var fileName = $"HCM_{tipo}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // MÉTODO QUE LLAMA HCM (usa tu versión; aquí un stub)
        private async Task<string> CrearContactoHcmAsync(HcmContactRequest contacto)
        {
            // Endpoint de creación de contactos HCM (11.13.18.05)
            var uri = "https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/hcmContacts";

            // ⚠️ Reemplaza con tus credenciales o toma de config
            const string user = "Claro_RhOnline_WS_SS";
            const string pwd = "HCM-RH0nl1ne@#3";

            // Auth Basic
            string auth = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(user + ":" + pwd));

            // TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

            // Serializa ignorando nulls (solo campos llenos)
            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(contacto, new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            });

            // Request HTTP
            var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Accept = "application/json";
            req.Headers.Add("Authorization", "Basic " + auth);
            req.Headers.Add("REST-Framework-Version", "2");
            req.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
            req.Timeout = 100000; // 100s

            // Body
            using (var sw = new System.IO.StreamWriter(await req.GetRequestStreamAsync()))
            {
                await sw.WriteAsync(jsonBody);
            }

            // Respuesta OK
            try
            {
                using (var res = (System.Net.HttpWebResponse)await req.GetResponseAsync())
                using (var rd = new System.IO.StreamReader(res.GetResponseStream()))
                {
                    var content = await rd.ReadToEndAsync();
                    return string.IsNullOrWhiteSpace(content) ? "{\"status\":\"ok\",\"message\":\"Empty response\"}" : content;
                }
            }
            // Errores 4xx/5xx: devuelve el JSON de error de Oracle si existe
            catch (System.Net.WebException ex)
            {
                var httpRes = ex.Response as System.Net.HttpWebResponse;
                if (httpRes != null && httpRes.GetResponseStream() != null)
                {
                    using (var rd = new System.IO.StreamReader(httpRes.GetResponseStream()))
                    {
                        var errorBody = await rd.ReadToEndAsync();
                        // Devuelve tal cual para que lo manejes en el controller
                        return string.IsNullOrWhiteSpace(errorBody)
                            ? "{\"status\":\"error\",\"code\":" + (int)httpRes.StatusCode + ",\"message\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}"
                            : errorBody;
                    }
                }
                // Sin cuerpo de error
                return "{\"status\":\"error\",\"message\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}";
            }
        }
      
        [HttpPost]
        public JsonResult ImportarExcel(HttpPostedFileBase archivo)
        {
            if (archivo == null || archivo.ContentLength == 0)
                return Json(new { ok = false, msg = "Seleccione un archivo." });

            var lista = new List<ContactoExcelVm>();

            try
            {
                using (var wb = new XLWorkbook(archivo.InputStream))
                {
                    var ws = wb.Worksheets.First();
                    var lastRow = ws.LastRowUsed().RowNumber();
                    var lastCol = ws.LastColumnUsed().ColumnNumber();

                    var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int c = 1; c <= lastCol; c++)
                    {
                        var key = ws.Cell(1, c).GetString().Trim();
                        if (!string.IsNullOrEmpty(key) && !headers.ContainsKey(key))
                            headers.Add(key, c);
                    }

                    for (int _r = 2; _r <= lastRow; _r++)
                    {
                        string GetCell(string col)
                        {
                            if (!headers.ContainsKey(col)) return "";
                            return ws.Cell(_r, headers[col]).GetString().Trim();
                        }

                        string fecha = "";
                        if (headers.ContainsKey("FechaNacimiento"))
                        {
                            var cell = ws.Cell(_r, headers["FechaNacimiento"]);
                            DateTime dt;
                            if (cell.TryGetValue<DateTime>(out dt)) fecha = dt.ToString("yyyy-MM-dd");
                            else fecha = cell.GetString().Trim();
                        }

                        var genero = GetCell("Genero");
                        if (!string.IsNullOrWhiteSpace(genero)) genero = genero.Substring(0, 1).ToUpper();

                        var tipoRel = GetCell("TipoRelacion");
                        if (!string.IsNullOrWhiteSpace(tipoRel)) tipoRel = tipoRel.ToUpper();

                        var item = new ContactoExcelVm
                        {
                            Carnet = GetCell("Carnet"),
                            PrimerNombre = GetCell("PrimerNombre"),
                            SegundoNombre = GetCell("SegundoNombre"),
                            PrimerApellido = GetCell("PrimerApellido"),
                            SegundoApellido = GetCell("SegundoApellido"),
                            FechaNacimiento = fecha,
                            Genero = genero,
                            TipoRelacion = tipoRel
                        };

                        if (!string.IsNullOrWhiteSpace(item.Carnet) &&
                            !string.IsNullOrWhiteSpace(item.PrimerNombre) &&
                            !string.IsNullOrWhiteSpace(item.PrimerApellido) &&
                            !string.IsNullOrWhiteSpace(item.TipoRelacion))
                        {
                            lista.Add(item);
                        }
                    }
                }

                Session[SessionKey] = lista;
                return Json(new { ok = true, count = lista.Count });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error leyendo archivo: " + ex.Message });
            }
        }

        public ActionResult HS()
        {
            return View();
        }
        public ActionResult Bajas()
        {
            return View();
        }
        [HttpPost]
        public FileResult Exportar()
        {

            //var result = Utils.ClaroWCF.hs()
            string apiUrl = $"http://172.26.54.66/apihcm/api/empleado/activo3?token=021092";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = -1;


            var resultExpenses = client.Execute(request);
            //Console.WriteLine(response.Content);
            //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

            if (resultExpenses != null)
            {

                var serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = 500000000;
                resultExpenses.Content = resultExpenses.Content.Replace(".0", "");
                //var deserializedObject = serializer.Deserialize<List<Entities.empleadoagradecimiento>>(resultExpenses.Content); //new JavaScriptSerializer().Deserialize<List<Entities.Employees>>(result);
                //DataTable dt1 = serializer.Deserialize<DataTable>(resultExpenses.Content); //new JavaScriptSerializer().Deserialize<List<Entities.Employees>>(result);
                DataTable dt1 = JsonConvert.DeserializeObject<DataTable>(resultExpenses.Content);
                if (string.IsNullOrEmpty(dt1.TableName))
                {
                    dt1.TableName = "Datos"; // Asigna un nombre no vacío
                }
                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dt1);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte Higiene y seguridad" + DateTime.Now.ToString() + ".xlsx");
                    }
                }

            }

            DataTable dt = null;

            dt = new DataTable();
            dt.TableName = "Datos";
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte reclutamiento " + DateTime.Now.ToString() + ".xlsx");
                }
            }
        }
        // GET: Rrhh

        public ActionResult Reclutamiento()
        {
            return View();
        }
        [HttpPost]
        public FileResult ExportarReclutamiento()
        {

            //var result = Utils.ClaroWCF.wEmployeeactivo();

            //DataTable dt = null;
            //if (result != null)
            //{
            //    List<Entities.reclutamiento> templ = new List<Entities.reclutamiento>();
            //    templ = result;

            //    dt = new DataTable("Grid");
            //    dt.Columns.AddRange(new DataColumn[33] { new DataColumn("NUMERO_DE_CANDIDATO"),
            //                                new DataColumn("PRIMERNOMBRE"),
            //                                new DataColumn("SEGUNDONOMBRE"),
            //                                 new DataColumn("PRIMERAPELLIDO"),
            //                                  new DataColumn("SEGUNDOAPELLIDO"),
            //                                   new DataColumn("CEDULA"),
            //                                    new DataColumn("FECHA_NACIMIENTO"),
            //                                     new DataColumn("EDAD"),
            //                                      new DataColumn("ESTADO_CIVIL"),
            //                                   new DataColumn("IDENTIFICACION"),
            //                                    new DataColumn("TITULO"),
            //                                     new DataColumn("LUGAR_DE_NACIMIENTO"),
            //                                      new DataColumn("ID_PERSONA"),
            //                                       new DataColumn("PUESTO"),
            //                                             new DataColumn("CATEGORIA"),
            //                                                   new DataColumn("GERENCIA"),
            //                                                         new DataColumn("SUBGERENCIA"),
            //                                                               new DataColumn("COORDINACION"),new DataColumn("SUPERVISION"),

            //        new DataColumn("AREA"),
            //  new DataColumn("NIVELES"),
            //         new DataColumn("EDIFICIO"),
            //               new DataColumn("REFERENCIA"),
            //                     new DataColumn("FECHA_INICIO"),
            //                           new DataColumn("EMPRESA"),
            //                                 new DataColumn("NOMBRE_JEFE"),
            //                                       new DataColumn("DEPARTAMENTO"),
            //                                                   new DataColumn("MUNICIPIO"),
            //                                                         new DataColumn("DIRECCION_DOMICILIO"),
            //                                                               new DataColumn("SALARIO"),
            //                                                         new DataColumn("BANCO"),
            //                                                             new DataColumn("TELEFONO1"),
            //                                             new DataColumn("TELEPHONE_NUMBER_2")
            //                                                         });



            //    foreach (var customer in templ)
            //    {
            //        dt.Rows.Add(customer.NUMERO_DE_CANDIDATO,
            //            customer.PRIMERNOMBRE,
            //            customer.SEGUNDONOMBRE,
            //            customer.PRIMERAPELLIDO,
            //            customer.SEGUNDOAPELLIDO,
            //            customer.CEDULA,
            //            customer.FECHA_NACIMIENTO,
            //            customer.EDAD,
            //            customer.ESTADO_CIVIL,
            //            customer.IDENTIFICACION,
            //            customer.TITULO,
            //            customer.LUGAR_DE_NACIMIENTO,
            //            customer.ID_PERSONA,
            //            customer.PUESTO,
            //            customer.CATEGORIA,
            //            customer.GERENCIA,
            //            customer.SUBGERENCIA,
            //            customer.COORDINACION,
            //            customer.SUPERVISION,
            //            customer.AREA,
            //            customer.NIVELES,
            //            customer.EDIFICIO,
            //            customer.REFERENCIA
            //            , customer.FECHA_INICIO,
            //            customer.EMPRESA
            //            , customer.NOMBRE_JEFE,
            //            customer.DEPARTAMENTO,
            //            customer.MUNICIPIO,
            //            customer.DIRECCION_DOMICILIO,
            //            customer.SALARIO,
            //            customer.BANCO, customer.TELEFONO1, customer.TELEPHONE_NUMBER_2);

            //    }

            //    //ListtoDataTableConverter converter = new ListtoDataTableConverter();
            //    //dt = converter.ToDataTable();

            //var result = Utils.ClaroWCF.hs()
            string apiUrl = $"http://172.26.54.66/apihcm/api/empleado/wEmployeeactivo?token=021092";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = -1;


            var resultExpenses = client.Execute(request);
            //Console.WriteLine(response.Content);
            //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

            if (resultExpenses != null)
            {

                var serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = 500000000;
                resultExpenses.Content = resultExpenses.Content.Replace(".0", "");
                //var deserializedObject = serializer.Deserialize<List<Entities.empleadoagradecimiento>>(resultExpenses.Content); //new JavaScriptSerializer().Deserialize<List<Entities.Employees>>(result);
                //DataTable dt1 = serializer.Deserialize<DataTable>(resultExpenses.Content); //new JavaScriptSerializer().Deserialize<List<Entities.Employees>>(result);
                DataTable dt1 = JsonConvert.DeserializeObject<DataTable>(resultExpenses.Content);
                if (string.IsNullOrEmpty(dt1.TableName))
                {
                    dt1.TableName = "Datos"; // Asigna un nombre no vacío
                }
                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dt1);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte reclutamiento" + DateTime.Now.ToString() + ".xlsx");
                    }
                }

            }

            DataTable dt = null;

            dt = new DataTable();
            dt.TableName = "Datos";
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte reclutamiento " + DateTime.Now.ToString() + ".xlsx");
                }
            }



        }
        
            public ActionResult EmpleadosVWEF()
        {
            return View();
        }
        public ActionResult Candidato()
        {
            return View();
        }
        [HttpGet]
        public JsonResult ListaCandidatosJsoncontratado(DateTime? desde = null, DateTime? hasta = null)
        {
            var minInicio = new DateTime(2024, 1, 1);
            var desdeEf = (desde ?? minInicio).Date;
            if (desdeEf < minInicio) desdeEf = minInicio;

            var hastaEf = (hasta ?? DateTime.Today).Date;
            if (hastaEf < desdeEf) hastaEf = desdeEf; // evita rango invertido

            const string sql = @"
SELECT
    v.carnet,
    v.nombre_completo,
    v.correo,
    v.cargo,
    v.empresa,
    v.cedula,
    v.Departamento,
    v.Nombreubicacion,
    v.fechaingreso,
    v.fechabaja,

    v.oDEPARTAMENTO,   -- Coordinación
    v.OGERENCIA,       -- Gerencia
    v.oSUBGERENCIA,    -- Subgerencia

    v.telefono,
    v.telefonojefe,
    v.nom_jefe1,
    v.correo_jefe1,
    v.cargo_jefe1,
    v.carnet_jefe1,

    v.SUBGERENTECORREO,
    v.SUBGERENTE,
    v.GERENTECORREO,
    v.GERENTE,
    v.GERENTECARNET,

    v.organizacion,
    v.primernivel,     -- Área

    e.AddressLine1  AS EmpAddressLine1, -- Dirección real
    e.City          AS EmpCity,         -- Ciudad
    e.Region2       AS EmpRegion2,      -- Departamento (dir)
    e.Gender        AS EmpGender,       -- Género
    e.MaritalStatus AS EmpMaritalStatus -- Estado civil
FROM EmpleadosVWEF AS v
INNER JOIN emp AS e ON e.PersonNumber = v.carnet
WHERE ( @Desde IS NULL OR TRY_CONVERT(date, v.fechaingreso) >= @Desde )
  AND ( @Hasta IS NULL OR TRY_CONVERT(date, v.fechaingreso) < DATEADD(DAY, 1, @Hasta) );";

            using (var cn = new SqlConnection(connStr))
            {
                // commandTimeout en segundos (ajústalo si la vista es pesada)
                var data = cn.Query<EmpleadoVwefDto>(
                                  sql,
                                  new { Desde = desdeEf, Hasta = hastaEf },
                                  commandTimeout: 60
                              ).ToList();

                var json = Json(new { data = data }, JsonRequestBehavior.AllowGet);
                json.MaxJsonLength = int.MaxValue;
                return json;
            }
        }
        public JsonResult ListaCandidatosJson()
        {
            string apiUrl = "http://172.26.54.66/apihcm/api/reclutamiento/Getcandidatos?token=021092";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = 10000;

            var response = client.Execute(request);

            if (response != null && response.Content != "")
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                string cleanJson = response.Content.Replace(".0", "");

                return Json(new { data = serializer.Deserialize<object>(cleanJson) }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { data = new List<object>() }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult horaextratodo(string bossId, string id)
        {
            string apiUrl = $"http://172.26.54.66/apihcm/api/horaextratodo/GetAlltodohora?token=021092&bossId={bossId}&id={id}";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = 10000;

            var response = client.Execute(request);

            if (response != null && !string.IsNullOrEmpty(response.Content))
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                string cleanJson = response.Content.Replace(".0", "");
                return Json(new { data = serializer.Deserialize<object>(cleanJson) }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { data = new List<object>() }, JsonRequestBehavior.AllowGet);
        }


        public FileResult Reporte(string fechaini, string fechafin)
        {

            List<slnRhonline.ServiceReference1.bajasBS> lstExpenseDetail = new List<slnRhonline.ServiceReference1.bajasBS>();
            DataTable dt = new DataTable("Grid");
            try
            {


                //var result = Utils.ClaroWCF.ReporteBaja(fechaini, fechafin);
                string apiUrl = $"http://172.26.54.66/apihcm/api/empleado/Reportebaja?token=021092&fechaini={fechaini}&fechafin={fechafin}";

                var client = new RestClient(apiUrl);
                var request = new RestRequest(Method.GET);
                request.Timeout = -1;


                var resultExpenses = client.Execute(request);
                //Console.WriteLine(response.Content);
                //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                if (resultExpenses != null)
                {

                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 500000000;
                    resultExpenses.Content = resultExpenses.Content.Replace(".0", "");
                    //var deserializedObject = serializer.Deserialize<List<Entities.empleadoagradecimiento>>(resultExpenses.Content); //new JavaScriptSerializer().Deserialize<List<Entities.Employees>>(result);
                    //DataTable dt1 = serializer.Deserialize<DataTable>(resultExpenses.Content); //new JavaScriptSerializer().Deserialize<List<Entities.Employees>>(result);
                    List<bajamodelo> temp2 = new List<bajamodelo>();
                    List<bajamodelo> temp3 = new List<bajamodelo>();
                    temp2 = JsonConvert.DeserializeObject<List<bajamodelo>>(resultExpenses.Content);
                    //lstExpenseDetail = result.ToList();
                    foreach (var item in temp2)
                    {
                        item.FECHANACIMIENTO = item.DATE_OF_BIRTH.Date;
                        int edad = DateTime.Today.Year - item.FECHANACIMIENTO.Year;
                        if (item.FECHANACIMIENTO > DateTime.Today.AddYears(-edad)) edad--;
                        item.EDAD = edad.ToString();
                    }



                    dt.Columns.AddRange(new DataColumn[] {
    new DataColumn("NOMBRE_COMPLETO"),
    new DataColumn("CODIGO"),
    new DataColumn("CODIGO JEFE"),
    new DataColumn("NOMBRE JEFE"),
    new DataColumn("CARGO JEFE"),
    new DataColumn("COREO JEFE"),
    new DataColumn("DEPART"),
    new DataColumn("MUNICIPIO"),
    new DataColumn("ESTUDIO"),
    new DataColumn("AREA"),
    new DataColumn("CARGO"),
    new DataColumn("SEX"),
    new DataColumn("EMPRESA"),
    new DataColumn("FECHA_INGRESO", typeof(DateTime)),
    new DataColumn("FECHA_BAJA", typeof(DateTime)),
    new DataColumn("GERENCIA"),
    new DataColumn("GERENCIARAIZ"),
    new DataColumn("EDIFICIO"),
    new DataColumn("MOTIVO_ABANDONO"),
    new DataColumn("TELEFONO1"),
    new DataColumn("TELEPHONE_NUMBER_2"),
    new DataColumn("FECHANACIMIENTO", typeof(DateTime)),
    new DataColumn("EDAD")
});

                    // Llenar filas desde temp2
                    foreach (var customer in temp2)
                    {
                        dt.Rows.Add(
                            customer.NOMBRE_COMPLETO,
                            customer.CODIGO,
                            customer.CODIGO2,
                            customer.NOMBRE2,
                            customer.CARGO2,
                            customer.CORREO,
                            customer.DEPART,
                            customer.MUNICIPIO,
                            customer.ACA,
                            customer.AREA,
                            customer.CARGO,
                            customer.SEX,
                            customer.EMPRESA,
                            customer.FECHA_INGRESO,
                            customer.FECHA_BAJA,
                            customer.GERENCIA,
                            customer.GERENCIARAIZ,
                            customer.Edificio,
                            customer.MOTIVO_ABANDONO,
                            customer.TELEFONO1,
                            customer.TELEPHONE_NUMBER_2,
                            customer.FECHANACIMIENTO,
                            customer.EDAD
                        );
                    }

                    // Exportar a Excel con formato de fecha
                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add(dt, "Reporte");

                        // Aplicar formato corto a columnas de fecha
                        ws.Column(dt.Columns["FECHA_INGRESO"].Ordinal + 1).Style.DateFormat.Format = "dd/MM/yyyy";
                        ws.Column(dt.Columns["FECHA_BAJA"].Ordinal + 1).Style.DateFormat.Format = "dd/MM/yyyy";
                        ws.Column(dt.Columns["FECHANACIMIENTO"].Ordinal + 1).Style.DateFormat.Format = "dd/MM/yyyy";

                        using (MemoryStream stream = new MemoryStream())
                        {
                            wb.SaveAs(stream);
                            return File(
                                stream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "Reporte de baja " + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx"
                            );
                        }
                    }


                }
                else
                {
                    lstExpenseDetail = new List<slnRhonline.ServiceReference1.bajasBS>();
                }





            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }





            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte de baja " + DateTime.Now.ToString() + ".xlsx");
                }
            }

        }
        public ActionResult Finanza()
        {
            return View();
        }
        [HttpPost]
        public FileResult Reportefinanza2(string fechafin)
        {

            List<Entities.viaticosreportes> lstExpenseDetail = new List<Entities.viaticosreportes>();

            try
            {

                //var result = Utils.ClaroWCF.GetExpenseDetailToRrhh(paidDate);

              

                var apiUrl = "http://172.26.54.66/apihcm/api/empleado/GetExpenseDetailToRrhhvscar?fecha=" + fechafin;
 
                    var client = new RestClient(apiUrl);
                    var request = new RestRequest(Method.GET) { Timeout = 30000 }; // 10s

                    var resp = client.Execute(request);
                if (resp == null || resp.Content == "" || string.IsNullOrWhiteSpace(resp.Content))
                {
                    lstExpenseDetail = new List<Entities.viaticosreportes>();
                }

                else { 
                // deserialización FUERTEMENTE TIPADA
                var settings = new JsonSerializerSettings
                    {
                        FloatParseHandling = FloatParseHandling.Decimal // respeta decimales en HOURS
                    };

                    var lista = JsonConvert.DeserializeObject<List<Entities.viaticosreportes>>(resp.Content, settings);
                    if (lista == null)
                    {
                        lstExpenseDetail = new List<Entities.viaticosreportes>();
                    }
                    else { lstExpenseDetail = lista.ToList(); }
                }
                //var result = Utils.ClaroWCF.GetExpenseDetailToRrhhvscar(fechafin);

                //if (result != null)
                //{
                //    lstExpenseDetail = result.ToList();


                //}
                //else
                //{
                //    lstExpenseDetail = new List<Entities.viaticosreportes>();
                //}
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            DataTable dt = new DataTable();
            ListtoDataTableConverter converter = new ListtoDataTableConverter();
            dt = converter.ToDataTable(lstExpenseDetail);



            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte  " + DateTime.Now.ToString() + ".xlsx");
                }
            }

        }

        //public JsonResult horaextratodo2(string id)
        //{
        //    Entities.Employees eEmployee = null;

        //    if (Session["User"] != null)
        //    {
        //        eEmployee = (Entities.Employees)Session["User"];
        //    }


        //    var apiUrl = "http://172.26.54.66/apihcm/api/empleado/gethoraextratodo?fecha=" + id;

        //    try
        //    {
        //        var client = new RestClient(apiUrl);
        //        var request = new RestRequest(Method.GET) { Timeout = 30000 }; // 10s

        //        var resp = client.Execute(request);
        //        if (resp == null || string.IsNullOrWhiteSpace(resp.Content))
        //            return Json(new { data = new List<Entities.Nominaexcel>() }, JsonRequestBehavior.AllowGet);

        //        // servidor puede retornar "SIN RESULTADO" literal
        //        var raw = resp.Content.Trim().Trim('"');
        //        if (string.Equals(raw, "SIN RESULTADO", StringComparison.OrdinalIgnoreCase))
        //            return Json(new { data = new List<Entities.Nominaexcel>() }, JsonRequestBehavior.AllowGet);

        //        // deserialización FUERTEMENTE TIPADA
        //        var settings = new JsonSerializerSettings
        //        {
        //            FloatParseHandling = FloatParseHandling.Decimal // respeta decimales en HOURS
        //        };

        //        var lista = JsonConvert.DeserializeObject<List<Entities.Nominaexcel>>(resp.Content, settings);
        //        if (lista == null)
        //            lista = new List<Entities.Nominaexcel>();

        //        // Opcional: normaliza ESTATUS a texto legible (si lo necesitas en el grid)
        //        for (int i = 0; i < lista.Count; i++)
        //        {
        //            var s = lista[i].STATUS;
        //            if (s == "1") lista[i].STATUS = "Registrado";
        //            else if (s == "2") lista[i].STATUS = "Autorizado por jefe inmediato";
        //            else if (s == "3") lista[i].STATUS = "Autorizado por Gerente";
        //            else if (s == "4") lista[i].STATUS = "Autorizado por Recursos Humanos";
        //        }
        //        string gerenciasPermitidas;

        //        if (eEmployee.EmployeeNumber == "401204")
        //        {
        //            // Las 4 gerencias sin el prefijo "NI "
        //            gerenciasPermitidas = string.Join(",",
        //                new[]
        //                {
        //    "GERENCIA OPERACIONES PLANTA INTERNA",
        //    "GERENCIA DE IMPLANTACION",
        //    "GERENCIA TECNICA",
        //    "GERENCIA OPERACIONES PLANTA EXTERNA"
        //                });
        //            lista = lista.Where(x =>
        //                    !string.IsNullOrEmpty(x.GERENCIA) &&
        //                    gerenciasPermitidas.Contains(x.GERENCIA.Replace("NI ", "").Trim())
        //                ).ToList();
        //        }
        //        else if (eEmployee.GERENCIA.Contains("RECURSOS") == true)
        //        {
        //            return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        //        }
        //        else
        //        {
        //            List<Entities.Employees> lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();
        //            var projectedEmployees = lstEmployees.Select(e => e.EmployeeNumber);
        //            lista = lista
        //                .Where(u => projectedEmployees.Contains(u.EMPLOYEE_NUMBER))
        //                .ToList();
        //        }

        //        // retorno estándar para DataTables: { data: [...] }
        //        return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch
        //    {
        //        // falla segura: lista vacía
        //        return Json(new { data = new List<Entities.Nominaexcel>() }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        [HttpPost]
        public JsonResult Reportefinanza22(string fechafin)
        {
            List<Entities.viaticosreportes> lstExpenseDetail = new List<Entities.viaticosreportes>();

            try
            {

                //var result = Utils.ClaroWCF.GetExpenseDetailToRrhh(paidDate);

                var result = Utils.ClaroWCF.GetExpenseDetailToRrhhvscar(fechafin);

                if (result != null)
                {
                    Entities.Employees eEmployee = new Entities.Employees();
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    if (eEmployee.oDEPARTAMENTO== "NI COORDINACION DE SOPORTE A LA OPERACION")
                    {// Gerencias a filtrar explícitamente
                        var gerenciasPermitidas = new List<string>
{
    "GERENCIA TECNICA",
    "IMPLANTACION",
    "PLANTA INTERNA",
    "PLANTA EXTERNA"
};

                        // Filtrar si NOTES contiene alguna de las gerencias permitidas

                        if (result.Any(x => gerenciasPermitidas.Any(g => x.NOTES.Contains(g))))
                        {
                            lstExpenseDetail = result
                                .Where(x => gerenciasPermitidas.Any(g => x.NOTES.Contains(g)))
                                .ToList();
                        }
                        else
                        {
                            lstExpenseDetail = new List<Entities.viaticosreportes>() ; // lista vacía
                        }


                    }
                    else { 
                    string gerencia = "";
                    gerencia = eEmployee.GERENCIA.Replace("NI ", "");
                    if (result.Count(x => x.NOTES.Contains(gerencia)) > 0)
                    {
                        lstExpenseDetail = result.Where(x => x.NOTES.Contains(gerencia)).ToList();

                    }
                    else { lstExpenseDetail = new List<Entities.viaticosreportes>(); }
                    }

                }
                else
                {
                    lstExpenseDetail = new List<Entities.viaticosreportes>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }
             return Json(lstExpenseDetail  , JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public FileResult Reportefinanza23(string fechafin)
        {

            List<Entities.viaticosreportes> lstExpenseDetail = new List<Entities.viaticosreportes>();

            try
            {

                //var result = Utils.ClaroWCF.GetExpenseDetailToRrhh(paidDate);

                var result = Utils.ClaroWCF.GetExpenseDetailToRrhhvscar(fechafin);

                if (result != null)
                {
                    Entities.Employees eEmployee = new Entities.Employees();
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    string gerencia = "";
                    gerencia = eEmployee.GERENCIA.Replace("NI ", "");
                    if (result.Count(x=>x.NOTES.Contains(gerencia))>0)
                    {
                        lstExpenseDetail = result.Where(x => x.NOTES.Contains(gerencia)).ToList();

                    }
                    else { lstExpenseDetail = new List<Entities.viaticosreportes>(); }


                }
                else
                {
                    lstExpenseDetail = new List<Entities.viaticosreportes>();
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            DataTable dt = new DataTable();
            ListtoDataTableConverter converter = new ListtoDataTableConverter();
            dt = converter.ToDataTable(lstExpenseDetail);



            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte  " + DateTime.Now.ToString() + ".xlsx");
                }
            }

        }
        public ActionResult analisis()
        {
            return View();
        }
        public ActionResult Nomina()
        {
            return View();
        }
        [HttpPost]
        public FileResult Nomina2(string fechafin)
        {

            List<slnRhonline.ServiceReference1.Nominaexcel> lstExpenseDetail = new List<slnRhonline.ServiceReference1.Nominaexcel>();

            List<slnRhonline.ServiceReference1.Nominaexcel> result = Utils.ClaroWCF.Nomina(fechafin);

            DataTable dt = new DataTable();
            ListtoDataTableConverter converter = new ListtoDataTableConverter();
            dt = converter.ToDataTable(lstExpenseDetail);
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte  " + DateTime.Now.ToString() + ".xlsx");
                }
            }


        }
        public ActionResult Index()
        {
            configdamper conex = new configdamper();
            using (IDbConnection db = new SqlConnection(conex.strConnection2))
            {
                const string sql =
                    "SELECT idhcm     AS IdHcm , nombre_completo AS NombreCompleto," +
                    "       correo     AS Correo, cargo          AS Cargo " +
                    "FROM   EMP2024 WHERE fechabaja = '0001-01-01' ORDER BY NombreCompleto";
                var lista = db.Query<Empleado>(sql).ToList();
                return View(lista);        // -->  /Views/RRhh/Index.cshtml
            }
        }
        [HttpPost]
        public async Task<ActionResult> Upload(ExcelFileModel model)
        {
            if (model.File != null && model.File.ContentLength > 0)
            {
                using (var package = new ExcelPackage(model.File.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var columnData = new Dictionary<string, List<string>>();
                    var totalRecords = worksheet.Dimension.End.Row - 1;

                    if (!string.IsNullOrEmpty(model.ColumnName))
                    {
                        var columnIndex = GetColumnIndex(worksheet, model.ColumnName);
                        if (columnIndex != -1)
                        {
                            columnData = GetGroupedData(worksheet, columnIndex);
                        }
                    }
                    else
                    {
                        columnData = GetAllData(worksheet);
                    }

                    var response = new List<string>();
                    int processedRecords = 0;
                    foreach (var group in columnData)
                    {
                        var jsonPayload = new
                        {
                            Query = model.UserQuery,
                            Data = group.Value
                        };

                        var apiResponse = await SendToApi(jsonPayload);
                        response.Add($"Grupo: {group.Key}, Respuesta: {apiResponse}");
                        processedRecords += group.Value.Count;
                        var remainingTime = CalculateRemainingTime(totalRecords - processedRecords);

                        // Actualizar progreso
                        Response.Write($"<script>updateProgress('Procesando grupo {group.Key}... Tiempo estimado restante: {remainingTime} minutos');</script>");
                        Response.Flush();
                    }

                    var resultFile = GenerateExcelResponse(response);

                    return File(resultFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Resultado.xlsx");
                }
            }

            return View("Upload");
        }
        private byte[] GenerateExcelResponse(List<string> responses)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Resultado");
                worksheet.Cells[1, 1].Value = "Grupo";
                worksheet.Cells[1, 2].Value = "Respuesta";

                for (int i = 0; i < responses.Count; i++)
                {
                    var parts = responses[i].Split(new[] { ", Respuesta: " }, StringSplitOptions.None);
                    worksheet.Cells[i + 2, 1].Value = parts[0].Replace("Grupo: ", "");
                    worksheet.Cells[i + 2, 2].Value = parts[1];
                }

                return package.GetAsByteArray();
            }
        }

        private string CalculateRemainingTime(int remainingRecords)
        {
            int timePerBatch = 5; // minutos por batch de 50 registros
            int batches = (int)Math.Ceiling((double)remainingRecords / 50);
            int remainingTime = batches * timePerBatch;
            return remainingTime.ToString();
        }
        private int GetColumnIndex(ExcelWorksheet worksheet, string columnName)
        {
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                if (worksheet.Cells[1, col].Text.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return col;
                }
            }
            return -1;
        }
        private async Task<string> SendToApi(object payload)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsJsonAsync("http://your-api-endpoint", payload);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
        private Dictionary<string, List<string>> GetGroupedData(ExcelWorksheet worksheet, int columnIndex)
        {
            var groupedData = new Dictionary<string, List<string>>();

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var key = worksheet.Cells[row, columnIndex].Text;
                if (!groupedData.ContainsKey(key))
                {
                    groupedData[key] = new List<string>();
                }

                var rowData = string.Join(",", Enumerable.Range(1, worksheet.Dimension.End.Column).Select(col => worksheet.Cells[row, col].Text));
                groupedData[key].Add(rowData);
            }

            return groupedData;
        }
        public ActionResult AsignarPaginas(int id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                var usuarios = db.Query<Empleado>("SELECT * FROM EMP2024 WHERE fechabaja IS NULL").ToList();
                return View(usuarios);
            }
        }

        // POST: Guarda o actualiza la asignación de páginas al perfil
        [HttpPost]
        public JsonResult GuardarPerfilMenu(int profileId, int menuId, int status)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ProfileId", profileId);
                parameters.Add("@MenuId", menuId);
                parameters.Add("@Status", status);
                db.Execute("sp_UpsertProfileMenu", parameters, commandType: CommandType.StoredProcedure);
            }
            return Json(new { success = true, message = "Asignación guardada correctamente" });
        }
        private Dictionary<string, List<string>> GetAllData(ExcelWorksheet worksheet)
        {
            var allData = new Dictionary<string, List<string>> { { "AllData", new List<string>() } };

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var rowData = string.Join(",", Enumerable.Range(1, worksheet.Dimension.End.Column).Select(col => worksheet.Cells[row, col].Text));
                allData["AllData"].Add(rowData);
            }

            return allData;
        }
        public ActionResult AsignarPaginaUsuario()
        {
            //using (IDbConnection db = new SqlConnection(_connectionString))
            //{
            //    string sql = "SELECT * FROM EMP2024 WHERE fechabaja = @fecha";
            //    var empleadosActivos = db.Query<Empleado>(sql, new { fecha = "0001-01-01" }).ToList();
            //    Session["empleadomenu"] = empleadosActivos;
            //    return View(empleadosActivos);
            //}
            return View();
        }

        // POST: Guarda o actualiza la asignación (o desasignación) de una página para el usuario

        // Crea un nuevo perfil personalizado

        public ActionResult ListaUsuariosActivos()
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                var usuarios = db.Query<Empleado>("SELECT * FROM EMP2024 WHERE fechabaja IS NULL").ToList();
                return View(usuarios);
            }
        }

        // Devuelve el formulario (parcial) de edición de menús para un usuario
        public ActionResult ObtenerMenusUsuario(int userId)
        {
            List<Empleado> temp = new List<Empleado>();
            temp = (List<Empleado>)Session["empleadomenu"];
            var temp1 = temp.FirstOrDefault(x => x.idhcm == userId);
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                // Se obtiene el perfil activo asignado al usuario, si existe

                var usuarioPerfil = db.QueryFirstOrDefault<UsuarioPerfil>(
                    @"SELECT pu.ProfileUserId, p.ProfileId, p.ProfileName ,UserName
                      FROM ProfileUsersRhOnline pu 
                      INNER JOIN ProfilesRhOnline p ON pu.ProfileId = p.ProfileId 
                      WHERE pu.UserName = @correo AND pu.Status = 1", new { correo = "" });

                // Se obtienen todos los menús
                var menus = db.Query<Menu>("SELECT Id AS ID, MenuText, MenuUrl FROM MenuRhOnline").ToList();
                HashSet<int> assignedMenus = new HashSet<int>();
                if (usuarioPerfil != null)
                {
                    var asignados = db.Query<int>("SELECT MenuId FROM ProfileMenuRhOnline WHERE ProfileId = @profileId",
                        new { profileId = usuarioPerfil.ProfileId });
                    assignedMenus = new HashSet<int>(asignados);
                }
                var model = new MenusUsuarioViewModel
                {
                    UserId = userId,
                    Menus = menus,
                    MenusAsignados = assignedMenus
                };
                return PartialView("_EditarMenusUsuario", model);
            }
        }
        public JsonResult MenusUsuario(int userId)
        {
            var config = new configdamper();
            using (var db = new SqlConnection(config.strConnectio3))
            {
                // 1) Leer todos los menús activos
                var lista = db.Query<MenuNode>(
                    "SELECT Id, MenuText, ParentId, OrderMenu FROM MenuRhOnline WHERE Estado='A'"
                ).ToList();

                // 2) Construir árbol
                var lookup = lista.ToDictionary(m => m.Id);
                foreach (var nodo in lista)
                {
                    if (nodo.ParentId.HasValue && nodo.ParentId.Value > 0
                        && lookup.ContainsKey(nodo.ParentId.Value))
                    {
                        lookup[nodo.ParentId.Value].Children.Add(nodo);
                    }
                }

                // 3) Tomar sólo raíces ordenadas
                var nodosRaiz = lista
                    .Where(m => !m.ParentId.HasValue || m.ParentId.Value == 0)
                    .OrderBy(m => m.OrderMenu)
                    .ToList();

                // 4) Recuperar asignados (usando tu lógica de carnet)
                string userCode = userId.ToString("D6");
                var asignados = db.Query<int>(
                    @"SELECT pm.MenuId
              FROM ProfileMenuRhOnline pm
              INNER JOIN ProfileUsersRhOnline pu ON pm.ProfileId = pu.ProfileId
              INNER JOIN SIGHO1.dbo.EMP2024 emp ON pu.UserName = emp.correo
              WHERE emp.carnet = @c AND pu.Status = 1",
                    new { c = userCode }
                ).ToList();

                return Json(new { nodes = nodosRaiz, asignados }, JsonRequestBehavior.AllowGet);
            }
        }
        private int CrearPerfil(IDbConnection db, IDbTransaction tx, string userId, string nombre)
        {
            return db.QuerySingle<int>(
                @"INSERT INTO ProfilesRhOnline(ProfileName,Descripcion,Status)
                  VALUES(@n,'Autogen',1); SELECT CAST(SCOPE_IDENTITY() AS int)",
                new { n = nombre }, tx);
        }

        private int ClonarPerfil(IDbConnection db, IDbTransaction tx, int perfilId)
        {
            var nuevoId = db.QuerySingle<int>(
                @"INSERT INTO ProfilesRhOnline(ProfileName,Descripcion,Status)
                  SELECT ProfileName+'-clon '+FORMAT(GETDATE(),'yyyyMMddHHmmss'),
                         'Clonado',1 FROM ProfilesRhOnline WHERE ProfileId=@id;
                  SELECT CAST(SCOPE_IDENTITY() AS int)",
                new { id = perfilId }, tx);

            db.Execute(
                @"INSERT INTO ProfileMenuRhOnline(ProfileId,MenuId,Status)
                  SELECT @nuevo,MenuId,Status
                  FROM   ProfileMenuRhOnline WHERE ProfileId=@old",
                new { nuevo = nuevoId, old = perfilId }, tx);

            return nuevoId;
        }

        private static void InsertarMenus(IDbConnection db, IDbTransaction tx, int perfilId, int[] menus)
        {
            db.Execute("DELETE FROM ProfileMenuRhOnline WHERE ProfileId=@p",
                       new { p = perfilId }, tx);

            foreach (var m in menus ?? Array.Empty<int>())
                db.Execute(@"INSERT INTO ProfileMenuRhOnline(ProfileId,MenuId,Status)
                             VALUES(@p,@m,1)", new { p = perfilId, m }, tx);
        }

        [HttpGet]
        public JsonResult GetUsuariosActivos()
        {
            List<Empleado> empleadosActivos = new List<Empleado>();
            if (Session["empleadomenu"] != null)
            {
                empleadosActivos = (List<Empleado>)Session["empleadomenu"];
                return Json(empleadosActivos, JsonRequestBehavior.AllowGet);
            }
            var config = new configdamper();
            string cs = config.strConnectio3;
            using (IDbConnection db = new SqlConnection(cs))
            {
                var list = db.Query<Empleado>(
                    "SELECT idhcm, nombre_completo, correo, cargo,carnet FROM EMP2024 WHERE fechabaja = '0001-01-01' or fechabaja is null  and correo is not null union all  SELECT idhcm, nombre_completo, correo, cargo,carnet FROM cr.dbo. EMP2024 WHERE fechabaja = '0001-01-01' or fechabaja is null  and correo is not null  "
                ).ToList();

                Session["empleadomenu"] = list;
                return Json(list, JsonRequestBehavior.AllowGet);
            }

        }
        [HttpGet]
        public JsonResult GetEmpsVisible(string bossId)
        {
            var rows = new List<dynamic>();
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.sp_Org2026_Tree", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 180 })
            {
                cmd.Parameters.AddWithValue("@Boss_Id", bossId ?? "");
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    // Devuelve EMP2024 “visible” para ese carnet según tu lógica
                    while (rd.Read())
                    {
                        rows.Add(new
                        {
                            carnet = rd["CARNET"]?.ToString(),
                            nombre_completo = rd["NOMBRE_COMPLETO"]?.ToString(),
                            cargo = rd["CARGO"]?.ToString(),
                            idorg = rd["IDORG_TRIM"]?.ToString()
                        });
                    }
                }
            }
            return Json(rows, JsonRequestBehavior.AllowGet);
        }
        public sealed class OrgFlat2
        {
            public string IdOrg { get; set; }
            public string Nombre { get; set; }
            public string PadreId { get; set; }
            public string Nivel { get; set; }
            public int ChildrenCount { get; set; }
        }
        public sealed class OrgNode2
        {
            public string Id { get; set; }
            public string Nombre { get; set; }
            public string Nivel { get; set; }
            public bool HasPerm { get; set; }
            public List<OrgNode2> Children { get; set; } = new List<OrgNode2>();
        }
        public sealed class UsuarioDto
        {
            public string idhcm { get; set; }
            public string nombre_completo { get; set; }
            public string correo { get; set; }
            public string cargo { get; set; }
        }
        public sealed class OrgFlatDto
        {
            public string IdOrg { get; set; }
            public string Nombre { get; set; }
            public string PadreId { get; set; }
            public string Nivel { get; set; }
            public int ChildrenCount { get; set; }
        }
        public class OrgNode { public string Id; public string Nombre; public string Nivel; public bool HasPerm; public List<OrgNode> Children = new List<OrgNode>(); }

        public sealed class PermisoDto
        {
            public string Carnet { get; set; }
            public string IDORG { get; set; }
            public long? ORGANIZATION_ID { get; set; }
            public string Estado { get; set; }
            public DateTime? FechaInsercion { get; set; }
            public string CarnetUsuarioInsert { get; set; }
        }
        public class OrgFlat
        {
            public string IdOrg { get; set; }
            public string PadreId { get; set; }
            public string Nombre { get; set; }
            public string Nivel { get; set; }
            public int ChildrenCount { get; set; }
            public long EmpCount { get; set; }
            public long EmpCountTotal { get; set; }
        }
        // Guarda la asignación de menús para el usuario (recibe la lista de menuIds seleccionados)
        public ActionResult PermisoOrg()
        {
            return View(); // /Views/Rrhh/PermisoOrg.cshtml  (tu página existente)
        }
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult GetOrgTree()
        {
            var flat = new List<OrgFlat>();

            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_Org_All", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 300;
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        flat.Add(new OrgFlat
                        {
                            IdOrg = rd["IdOrg"]?.ToString(),
                            PadreId = rd["PadreId"] == DBNull.Value ? null : rd["PadreId"].ToString(),
                            Nombre = rd["Nombre"]?.ToString(),
                            Nivel = rd["Nivel"]?.ToString(),
                            ChildrenCount = rd["ChildrenCount"] == DBNull.Value ? 0 : Convert.ToInt32(rd["ChildrenCount"]),
                            EmpCount = rd["EmpCount"] == DBNull.Value ? 0 : Convert.ToInt64(rd["EmpCount"]),
                            EmpCountTotal = rd["EmpCountTotal"] == DBNull.Value ? 0 : Convert.ToInt64(rd["EmpCountTotal"])
                        });
                    }
                }
            }

            // map → nodos
            var map = flat.ToDictionary(
              x => x.IdOrg,
              x => new OrgNodeDto
              {
                  Id = x.IdOrg,
                  Nombre = x.Nombre,
                  Nivel = x.Nivel,
                  ChildrenCount = x.ChildrenCount,
                  EmpCount = x.EmpCount,
                  EmpCountTotal = x.EmpCountTotal,
                  HasPerm = false,
                  IsDenied = false
              },
              StringComparer.OrdinalIgnoreCase
            );

            // construir jerarquía
            var roots = new List<OrgNodeDto>();
            foreach (var f in flat)
            {
                var n = map[f.IdOrg];
                if (string.IsNullOrWhiteSpace(f.PadreId) ||
                    f.PadreId.Equals(f.IdOrg, StringComparison.OrdinalIgnoreCase) ||
                    !map.ContainsKey(f.PadreId))
                    roots.Add(n);
                else
                    map[f.PadreId].Children.Add(n);
            }

            // ordenar por nivel+nombre
            Action<List<OrgNodeDto>> sortRec = null;
            sortRec = lst =>
            {
                lst.Sort((a, b) => string.Concat(a.Nivel ?? "", a.Nombre ?? "")
                                .CompareTo(string.Concat(b.Nivel ?? "", b.Nombre ?? "")));
                foreach (var c in lst) sortRec(c.Children);
            };
            sortRec(roots);

            return Json(roots, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetOrgEmpsDirect(string idorg)
        {
            var list = new List<EmpleadoRow>();
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_Org_EmpsDirect", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@IdOrg", idorg ?? "");
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new EmpleadoRow
                        {
                            carnet = rd["CARNET"]?.ToString(),
                             nombre_completo = rd["NOMBRE_COMPLETO"]?.ToString(),
                            cargo = rd["CARGO"]?.ToString(),
                             idorg = rd["IDORG_TRIM"]?.ToString()
                        });
                    }
                }
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetOrgEmpsSubtree(string idorg)
        {
            var list = new List<EmpleadoRow>();
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_Org_EmpsSubtree", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@IdOrg", idorg ?? "");
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new EmpleadoRow
                        {
                            carnet = rd["CARNET"]?.ToString(),
                            nombre_completo = rd["NOMBRE_COMPLETO"]?.ToString(),
                            cargo = rd["CARGO"]?.ToString(),
                             idorg = rd["IDORG_TRIM"]?.ToString()
                        });
                    }
                }
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public class EmpleadoRow
        {
            public string carnet { get; set; }
            public int idhcm { get; set; }
            public string nombre_completo { get; set; }
            public string cargo { get; set; }
            public string correo { get; set; }
            public string idorg { get; set; }
        }
        [HttpGet]
        public JsonResult GetUsuariosQuePuedeVer(string carnet)
        {
            var list = new List<EmpleadoRow>();
            using (var cn = new SqlConnection(connStr))
            {
                cn.Open();
                // ejecuta tu SP y lee empleados visibles
                using (var cmd = new SqlCommand("sp_Org2026_Tree", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@Boss_Id", carnet ?? "");
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new EmpleadoRow
                            {
                                carnet = rd["CARNET"]?.ToString(),
                                idhcm = rd["IDHCM"] == DBNull.Value ? 0 : Convert.ToInt32(rd["IDHCM"]),
                                nombre_completo = rd["NOMBRE_COMPLETO"]?.ToString(),
                                cargo = rd["CARGO"]?.ToString(),
                                correo = rd["CORREO"]?.ToString(),
                                idorg = rd["IDORG_TRIM"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult PermisoSaveSet(string Carnet, string Usuario, List<string> IdsAllow, List<string> IdsDeny)
        {
            if (string.IsNullOrWhiteSpace(Carnet))
                return Json(new { ok = false, msg = "Carnet vacío" });

            IdsAllow = IdsAllow ?? new List<string>();
            IdsDeny = IdsDeny ?? new List<string>();
            var all = IdsAllow.Concat(IdsDeny).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            using (var cn = new SqlConnection(connStr))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        foreach (var id in IdsAllow)
                        {
                            using (var cmd = new SqlCommand("EXEC dbo.usp_Perm_Upsert @Carnet,@IDORG,@Estado,@Usuario", cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Carnet", Carnet);
                                cmd.Parameters.AddWithValue("@IDORG", id);
                                cmd.Parameters.AddWithValue("@Estado", "A");
                                cmd.Parameters.AddWithValue("@Usuario", Usuario ?? Carnet);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        foreach (var id in IdsDeny)
                        {
                            using (var cmd = new SqlCommand("EXEC dbo.usp_Perm_Upsert @Carnet,@IDORG,@Estado,@Usuario", cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Carnet", Carnet);
                                cmd.Parameters.AddWithValue("@IDORG", id);
                                cmd.Parameters.AddWithValue("@Estado", "N");
                                cmd.Parameters.AddWithValue("@Usuario", Usuario ?? Carnet);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // limpiar los que no están en ninguno
                        var del = @"DELETE p FROM dbo.PermisosOrg p
                        WHERE p.Carnet=@c AND NOT EXISTS (
                          SELECT 1 WHERE p.IDORG IN (" + (all.Count == 0 ? "SELECT NULL WHERE 1=0" : string.Join(",", all.Select((_, i) => "@x" + i))) + @")
                        )";
                        using (var cmd = new SqlCommand(del, cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@c", Carnet);
                            for (int i = 0; i < all.Count; i++) cmd.Parameters.AddWithValue("@x" + i, all[i]);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return Json(new { ok = true });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return Json(new { ok = false, msg = ex.Message });
                    }
                }
            }
        }
        public class OrgNodeDto
        {
            public string Id { get; set; }
            public string Nombre { get; set; }
            public string Nivel { get; set; }
            public int ChildrenCount { get; set; }
            public long EmpCount { get; set; }
            public long EmpCountTotal { get; set; }
            public bool HasPerm { get; set; }     // A
            public bool IsDenied { get; set; }    // N
            public List<OrgNodeDto> Children { get; set; } = new List<OrgNodeDto>();
        }
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult GetOrgTreeUser(string carnet)
        {
            var baseTreeResult = GetOrgTree().Data as IEnumerable<OrgNodeDto>;
            var tree = baseTreeResult?.ToList() ?? new List<OrgNodeDto>();

            // 2) permisos del usuario (lee Estado si existe)
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_Perm_ListByUser", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@Carnet", carnet ?? "");
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    bool hasEstado = HasColumn(rd, "Estado");
                    while (rd.Read())
                    {
                        var id = rd["IDORG"]?.ToString() ?? "";
                        var st = hasEstado ? (rd["Estado"]?.ToString() ?? "A") : "A"; // por compatibilidad
                        if (!string.IsNullOrWhiteSpace(id))
                            dict[id] = st;  // 'A' o 'N'
                    }
                }
            }

            // 3) pintar flags sobre el árbol
            void Mark(OrgNodeDto n)
            {
                if (dict.TryGetValue(n.Id, out var st))
                {
                    n.HasPerm = (st == "A");
                    n.IsDenied = (st == "N");
                }
                foreach (var c in n.Children) Mark(c);
            }
            foreach (var r in tree) Mark(r);

            return Json(tree, JsonRequestBehavior.AllowGet);
        }
        private static bool HasColumn(IDataRecord r, string name)
        {
            try { return r.GetOrdinal(name) >= 0; }
            catch (IndexOutOfRangeException) { return false; }
        }
    

        private static List<OrgNode2> BuildTree(List<OrgFlat2> flat, HashSet<string> grantedOrNull)
        {
            // normaliza nulos/blancos/auto-referencias
            foreach (var r in flat)
                if (string.IsNullOrWhiteSpace(r.PadreId) || r.PadreId == r.IdOrg) r.PadreId = null;

            // índices
            var byId = flat.ToDictionary(x => x.IdOrg, x => x, StringComparer.OrdinalIgnoreCase);
            var childrenOf = new Dictionary<string, List<OrgFlat2>>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in flat)
            {
                var k = r.PadreId ?? "__ROOT__";
                if (!childrenOf.TryGetValue(k, out var list)) childrenOf[k] = list = new List<OrgFlat2>();
                list.Add(r);
            }

            // raíces reales
            var roots = childrenOf.ContainsKey("__ROOT__") ? childrenOf["__ROOT__"] : new List<OrgFlat2>();

            // fallback: si no hay raíces, usar los de menor NIVEL (top-level) o por patrón "NI DIRECCION PAIS"
            if (roots.Count == 0)
            {
                // 1) preferir los que contienen "NI DIRECCION PAIS"
                var top = flat.Where(f => (f.Nombre ?? "").ToUpper().Contains("NI DIRECCION PAIS")).ToList();
                if (top.Count == 0)
                {
                    // 2) mínimo por prefijo de 5 chars del NIVEL (típica jerarquía 5|10|15|…)
                    var minLen = flat.Min(f => (f.Nivel ?? "").Length);
                    top = flat.Where(f => (f.Nivel ?? "").Length == minLen).ToList();
                }
                roots = top;
            }

            // map OrgFlat -> OrgNode
            var nodeById = new Dictionary<string, OrgNode2>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in flat)
            {
                nodeById[r.IdOrg] = new OrgNode2
                {
                    Id = r.IdOrg,
                    Nombre = r.Nombre,
                    Nivel = r.Nivel,
                    HasPerm = grantedOrNull != null && grantedOrNull.Contains(r.IdOrg)
                };
            }

            // enlazar hijos
            foreach (var r in flat)
            {
                if (r.PadreId == null) continue;
                if (nodeById.TryGetValue(r.PadreId, out var p) && nodeById.TryGetValue(r.IdOrg, out var c))
                    p.Children.Add(c);
            }

            // ordenar hijos (Nivel, Nombre)
            void sortRec(OrgNode2 n)
            {
                n.Children = n.Children
                    .OrderBy(c => c.Nivel ?? "")
                    .ThenBy(c => c.Nombre ?? "", System.StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                foreach (var ch in n.Children) sortRec(ch);
            }

            // si había raíces reales
            if (childrenOf.ContainsKey("__ROOT__"))
            {
                var rootNodes = new List<OrgNode2>();
                foreach (var r in roots)
                {
                    var n = nodeById[r.IdOrg];
                    sortRec(n);
                    rootNodes.Add(n);
                }
                // si hay varias raíces, agregamos raíz sintética que las agrupa (no afecta IDs reales)
                if (rootNodes.Count > 1)
                {
                    var root = new OrgNode2 { Id = "_ROOT_", Nombre = "NI DIRECCION PAIS (Raíz)", Nivel = "", HasPerm = false, Children = rootNodes };
                    return new List<OrgNode2> { root };
                }
                return rootNodes;
            }
            else
            {
                // no había raíces → construimos raíz sintética con las top detectadas
                var root = new OrgNode2 { Id = "_ROOT_", Nombre = "NI DIRECCION PAIS (Raíz)", Nivel = "", HasPerm = false };
                foreach (var r in roots)
                {
                    var n = nodeById[r.IdOrg];
                    sortRec(n);
                    root.Children.Add(n);
                }
                return new List<OrgNode2> { root };
            }
        }
        // =====================================
        // 1) Empleados activos (DataTable UI)
        // GET: /Rrhh/GetUsuariosActivos
        // =====================================
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult GetUsuariosActivos3()
        {
            var lista = new List<UsuarioDto>();
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_UsuariosActivos", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        lista.Add(new UsuarioDto
                        {
                            idhcm = rd["idhcm"]?.ToString(),
                            nombre_completo = rd["nombre_completo"]?.ToString(),
                            correo = rd["correo"]?.ToString(),
                            cargo = rd["cargo"]?.ToString()
                        });
                    }
                }
            }
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        // ==========================================================
        // 2) Toda la organización (1 llamada – plano para armar tree)
        // GET: /Rrhh/GetOrgAll
        // ==========================================================
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult GetOrgAll()
        {
            var filas = new List<OrgFlatDto>();
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_Org_All", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;
                cn.Open();

                // sin SequentialAccess
                using (var rd = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (rd.Read())
                    {
                        filas.Add(new OrgFlatDto
                        {
                            IdOrg = rd["IdOrg"] as string,
                            Nombre = rd["Nombre"] as string,
                            PadreId = rd["PadreId"] as string,
                            Nivel = rd["Nivel"] as string,
                            ChildrenCount = rd.IsDBNull(rd.GetOrdinal("ChildrenCount")) ? 0 : rd.GetInt32(rd.GetOrdinal("ChildrenCount"))
                        });
                    }
                }
            }
            return Json(filas, JsonRequestBehavior.AllowGet);
        }

        // ==========================================================
        // 3) Permisos: listar (activos) por usuario
        // GET: /Rrhh/PermisosListar?carnet=500708
        // ==========================================================
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult PermisosListar(string carnet)
        {
            if (string.IsNullOrWhiteSpace(carnet))
                return Json(new { ok = false, msg = "carnet requerido" }, JsonRequestBehavior.AllowGet);

            var lista = new List<PermisoDto>();
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_Perm_List", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Carnet", SqlDbType.NVarChar, 50).Value = carnet;
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        lista.Add(new PermisoDto
                        {
                            Carnet = rd["Carnet"]?.ToString(),
                            IDORG = rd["IDORG"]?.ToString(),
                            ORGANIZATION_ID = rd["ORGANIZATION_ID"] == DBNull.Value ? (long?)null : Convert.ToInt64(rd["ORGANIZATION_ID"]),
                            Estado = rd["Estado"]?.ToString(),
                            FechaInsercion = rd["FechaInsercion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["FechaInsercion"]),
                            CarnetUsuarioInsert = rd["CarnetUsuarioInsert"]?.ToString()
                        });
                    }
                }
            }
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        // ==============================================================================
        // 4) Permisos: guardar/activar (upsert puntual)  – POST a /Rrhh/PermisoGuardar
        // body x-www-form-urlencoded o JSON: Carnet, IDORG (o ORGANIZATION_ID), Estado
        // ==============================================================================
        [HttpPost]
        public JsonResult PermisoGuardar(string Carnet, string IDORG, string Estado = "A", string Usuario = null)
        {
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_Perm_On", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@Carnet", Carnet ?? "");
                cmd.Parameters.AddWithValue("@IDORG", IDORG ?? "");
                cmd.Parameters.AddWithValue("@Usuario", Usuario ?? Carnet ?? "");
                cn.Open(); cmd.ExecuteNonQuery();
            }
            return Json(new { ok = true });
        }

        // ==============================================================================
        // 5) Permisos: deshabilitar puntual – POST a /Rrhh/PermisoAnular
        // body: Carnet + (IDORG o ORGANIZATION_ID)
        // ==============================================================================
        [HttpPost]
        public JsonResult PermisoAnular(string Carnet, string IDORG)
        {
            using (var cn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_Perm_Off", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@Carnet", Carnet ?? "");
                cmd.Parameters.AddWithValue("@IDORG", IDORG ?? "");
                cn.Open(); cmd.ExecuteNonQuery();
            }
            return Json(new { ok = true });
        }
        [HttpPost]
        public JsonResult GuardarMenusrr(int userId, int[] menuIds)
        {
            try
            {
                string userIdFormateado = userId.ToString("D6"); // Resultado: "000123"
                List<Empleado> empleadosActivos = new List<Empleado>();

                empleadosActivos = (List<Empleado>)Session["empleadomenu"];
                var emp = empleadosActivos.Where(x => x.idhcm == userId).FirstOrDefault();

                // Obtener cadena de conexión
                var config = new configdamper();
                using (IDbConnection db = new SqlConnection(config.strConnectio3))
                {
                    db.Open();
                    using (IDbTransaction tx = db.BeginTransaction())
                    {
                        // 1) Inactivar perfiles actuales del usuario
                        db.Execute(
                            "UPDATE ProfileUsersRhOnline SET Status = 0 WHERE UserName = @u AND Status = 1",
                            new { u = emp.correo }, tx);

                        // 2) Buscar perfil que coincida exactamente con estos menús
                        var perfilCoincidente = db.QueryFirstOrDefault<int?>(
                            @"SELECT pm.ProfileId
                              FROM ProfileMenuRhOnline pm
                              GROUP BY pm.ProfileId
                              HAVING COUNT(*) = @count
                                 AND SUM(CASE WHEN pm.MenuId IN @menuIds THEN 1 ELSE 0 END) = @count",
                            new { count = (menuIds ?? new int[0]).Length, menuIds }, tx);

                        int perfilAsignar;
                        if (perfilCoincidente.HasValue)
                        {
                            // Reutilizar perfil existente
                            perfilAsignar = perfilCoincidente.Value;
                        }
                        else
                        {
                            // Crear perfil nuevo
                            perfilAsignar = CreateProfile(db, tx, userId);
                            SetProfileMenus(db, tx, perfilAsignar, menuIds);
                        }

                        // 3) Asignar perfil al usuario
                        db.Execute(
                          @"INSERT INTO ProfileUsersRhOnline(ProfileId, UserName, UserId, Status)
                              VALUES(@p, @un, @u, 1)",
                          new { p = perfilAsignar, un = emp.correo, u = userId }, tx);

                        tx.Commit();
                    }
                }

                return Json(new { ok = true, msg = "Menús asignados correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult GuardarMenus(int userId, int[] menuIds)
        {
            try
            {
                // Formatear userId a 6 dígitos
                string userIdFormateado = userId.ToString("D6");
                // Recuperar correo de la sesión (o de la DB según tu lógica)
                var empleadosActivos = (List<Empleado>)Session["empleadomenu"];
                var emp = empleadosActivos.FirstOrDefault(x => x.idhcm == userId);
                string userName = emp?.correo;

                var config = new configdamper();
                using (var db = new SqlConnection(config.strConnectio3))
                {
                    db.Open();
                    using (var tx = db.BeginTransaction())
                    {
                        // 1) Inactivar cualquier perfil activo del usuario
                        db.Execute(
                            "UPDATE ProfileUsersRhOnline SET Status = 0 " +
                            "WHERE UserName = @u AND Status = 1",
                            new { u = userName }, tx);

                        // 2) Construir el conjunto completo de IDs (hijos + sus ancestros)
                        var allIds = new HashSet<int>(menuIds ?? new int[0]);
                        var queue = new Queue<int>(allIds);
                        while (queue.Count > 0)
                        {
                            int current = queue.Dequeue();
                            int? parent = db.QueryFirstOrDefault<int?>(
                                "SELECT ParentId FROM MenuRhOnline WHERE Id = @id",
                                new { id = current }, tx);
                            if (parent.HasValue && parent.Value != 0 && allIds.Add(parent.Value))
                                queue.Enqueue(parent.Value);
                        }

                        // 3) Buscar perfil existente con EXACTAMENTE este conjunto de menús
                        int count = allIds.Count;
                        var perfilCoincidente = db.QueryFirstOrDefault<int?>(
                            @"SELECT pm.ProfileId
                      FROM ProfileMenuRhOnline pm
                      GROUP BY pm.ProfileId
                      HAVING COUNT(*) = @count
                         AND SUM(CASE WHEN pm.MenuId IN @ids THEN 1 ELSE 0 END) = @count",
                            new { count = count, ids = allIds }, tx);

                        int perfilAsignar;
                        if (perfilCoincidente.HasValue)
                        {
                            // 3A) Reutilizar perfil existente
                            perfilAsignar = perfilCoincidente.Value;
                        }
                        else
                        {
                            int newId = db.QuerySingle<int>(
       "SELECT ISNULL(MAX(ProfileId), 0) + 1 FROM ProfilesRhOnline",
       transaction: tx);
                            // 3B) Crear un perfil nuevo
                            string nombre = string.Format("Perfil_{0}_{1}",
                                              userId,
                                              DateTime.Now.ToString("yyyyMMddHHmmss"));
                            db.Execute(
        @"INSERT INTO ProfilesRhOnline
             (ProfileId, ProfileName, Descripcion, Status)
          VALUES
             (@id, @name, 'Autogenerado', 1)",
        new { id = newId, name = nombre },
        tx);

                            // Asignar menús (incluyendo ancestros) al perfil nuevo
                            foreach (var m in allIds)
                            {
                                db.Execute(
                                    @"INSERT INTO ProfileMenuRhOnline(ProfileId,MenuId,Status)
                              VALUES(@p,@m,1)",
                                    new { p = newId, m }, tx);
                            }
                            perfilAsignar = newId;
                        }

                        // 4) Asignar el perfil (nuevo o coincidente) al usuario
                        db.Execute(
                            @"INSERT INTO ProfileUsersRhOnline(ProfileId,UserName,UserId,Status)
                      VALUES(@p,@un,@u,1)",
                            new { p = perfilAsignar, un = userName, u = userId }, tx);

                        tx.Commit();
                    }
                }

                return Json(new { ok = true, msg = "Menús asignados correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }

        #region Helpers

        private int CreateProfile(IDbConnection db, IDbTransaction tx, int userId)
        {
            // Generar nuevo ID manualmente si la columna no es identity
            int newId = db.QuerySingle<int>(
                "SELECT ISNULL(MAX(ProfileId), 0) + 1 FROM ProfilesRhOnline",
                transaction: tx);
            // Nombre único
            string nombre = string.Format("Perfil_{0}_{1}", userId, DateTime.Now.ToString("yyyyMMddHHmmss"));

            string sql = @"INSERT INTO ProfilesRhOnline(ProfileId, ProfileName, Descripcion, Status)
                           VALUES(@id, @n, 'Auto-generado', 1);";
            db.Execute(sql, new { id = newId, n = nombre }, tx);
            return newId;
        }

        private void SetProfileMenus(IDbConnection db, IDbTransaction tx, int perfilId, int[] menuIds)
        {
            // Eliminar asignaciones previas
            db.Execute(
                "DELETE FROM ProfileMenuRhOnline WHERE ProfileId = @p",
                new { p = perfilId }, tx);

            if (menuIds == null || menuIds.Length == 0)
                return;

            // Calcular lista completa de menús incluyendo ancestros
            var allIds = new HashSet<int>(menuIds);
            var queue = new Queue<int>(menuIds);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                // Obtener ParentId de este menú
                var parent = db.QueryFirstOrDefault<int?>(
                    "SELECT ParentId FROM MenuRhOnline WHERE Id = @id",
                    new { id = current }, tx);
                if (parent.HasValue && parent.Value != 0 && allIds.Add(parent.Value))
                {
                    queue.Enqueue(parent.Value);
                }
            }

            // Insertar nuevas asignaciones para todos los menús (hijos + padres)
            foreach (int m in allIds)
            {
                db.Execute(
                    @"INSERT INTO ProfileMenuRhOnline(ProfileId, MenuId, Status)
                      VALUES(@p, @m, 1)",
                    new { p = perfilId, m }, tx);
            }
        }

        #endregion
        // Modelos internos

        // Modelos internos
        public class UsuarioPerfil
        {
            public int ProfileUserId { get; set; }
            public int ProfileId { get; set; }
            public string ProfileName { get; set; }
        }
        public class MenusUsuarioViewModel
        {
            public int UserId { get; set; }
            public List<Menu> Menus { get; set; }
            public HashSet<int> MenusAsignados { get; set; }
        }
        public class Menu
        {
            public int ID { get; set; }
            public string MenuText { get; set; }
            public string MenuUrl { get; set; }
        }
        public class Perfil
        {
            public int ProfileId { get; set; }
            public string ProfileName { get; set; }
        }
        public class MenuDto
        {
            public int Id { get; set; }
            public string MenuText { get; set; }
            public int? ParentId { get; set; }
            public decimal OrderMenu { get; set; }
        }
        public class GrupoDto
        {
            public int PadreId { get; set; }
            public string PadreNombre { get; set; }
            public List<HijDto> Hijos { get; set; }
        }
        public class HijDto
        {
            public int Id { get; set; }
            public string MenuText { get; set; }
        }
     

        public class Empleado
        {
            public int idhcm { get; set; }
            public string correo { get; set; }
            public string nombre_completo { get; set; }
            public string cargo { get; set; }
            public string OGERENCIA { get; set; }
            public string carnet { get; set; }
        }
        //private static readonly string HcmBase = "https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/workers/";   // https://fa-exjp-...oraclecloud.com
        //private static readonly string HcmUser = ConfigurationManager.AppSettings["HcmUser"];
        //private static readonly string HcmPass = ConfigurationManager.AppSettings["HcmPass"];
      
        // GET: /Phones/Cargar
       


        private const string SESSION_PREVIEW = "TEL_PREVIEW";
        private const string SESSION_LOTE = "TEL_LOTEID";
 
    }
}
