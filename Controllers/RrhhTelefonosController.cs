// ====== Controller mínimo (solo 2 endpoints): ImportarExcel y Enviar ======
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ClosedXML.Excel;
using Dapper;
using Newtonsoft.Json.Linq;


namespace slnRhonline.Controllers
{
    public class RrhhTelefonosController : Controller
    {
       

        private const string SESSION_PREVIEW = "TEL_PREVIEW";

         private readonly string CadenaSql = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIGHO1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos
 
        // HCM hardcoded (ajusta pass real)
        private const string HcmBase = "https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com";
        private const string HcmUser = "Claro_RhOnline_WS_SS";
        private const string HcmPass = "HCM-RH0nl1ne@#3"; // ejemplo

        public ActionResult Telefono()
        {
            return View();
        }
        static RrhhTelefonosController()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            ServicePointManager.Expect100Continue = false;
        }

        // VM usado en preview y en JS
        public class TelefonoExcelVm
        {
            public string Carnet { get; set; }
            public string NombreApellidos { get; set; }   // solo display
            public string Gerencia { get; set; }          // solo display
            public string PhoneType { get; set; }          // solo display
            public string Telefono { get; set; }          // 8 dígitos
            public bool HacerPrincipal { get; set; }
        }

        // Resultado HTTP HCM (sin Tuple)
        public class HcmResult
        {
            public int StatusCode { get; set; }
            public string Content { get; set; }
        }
        [HttpPost]
        public JsonResult ImportarExcel(HttpPostedFileBase archivo)
        {
            if (archivo == null || archivo.ContentLength == 0)
                return Json(new { ok = false, msg = "Seleccione un archivo." });

            var lista = new List<TelefonoExcelVm>();

            try
            {
                using (var wb = new XLWorkbook(archivo.InputStream))
                {
                    // ================= HOJA MOVIL =================
                    IXLWorksheet wsMovil = wb.Worksheets
                        .FirstOrDefault(x => x.Name.Trim().Equals("MOVIL", StringComparison.OrdinalIgnoreCase));

                    if (wsMovil == null && wb.Worksheets.Count >= 1)
                        wsMovil = wb.Worksheet(1); // fallback: primera hoja

                    if (wsMovil != null)
                    {
                        var lastRow = wsMovil.LastRowUsed().RowNumber();
                        var lastCol = wsMovil.LastColumnUsed().ColumnNumber();

                        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 1; c <= lastCol; c++)
                        {
                            var key = wsMovil.Cell(1, c).GetString().Trim();
                            if (!string.IsNullOrEmpty(key) && !headers.ContainsKey(key))
                                headers.Add(key, c);
                        }

                        string GetCellM(int row, string colName)
                        {
                            if (!headers.ContainsKey(colName)) return "";
                            return wsMovil.Cell(row, headers[colName]).GetString().Trim();
                        }

                        for (int r = 2; r <= lastRow; r++)
                        {
                            var carnet = GetCellM(r, "NO. EMPLEADO");
                            if (string.IsNullOrWhiteSpace(carnet))
                                carnet = GetCellM(r, "PersonNumber");

                            var tel = GetCellM(r, "Telefono");
                            if (string.IsNullOrWhiteSpace(tel))
                                tel = GetCellM(r, "Teléfono");

                            var nombre = GetCellM(r, "NOMBRE Y APELLIDOS");
                            var gerencia = GetCellM(r, "GERENCIA");

                            var hacerPrincipalTxt = GetCellM(r, "HacerPrincipal");
                            bool hacerPrincipal = true;
                            if (!string.IsNullOrWhiteSpace(hacerPrincipalTxt))
                            {
                                var s = hacerPrincipalTxt.Trim().ToLowerInvariant();
                                if (s == "no" || s == "0" || s == "false" || s == "f")
                                    hacerPrincipal = false;
                            }

                            if (string.IsNullOrWhiteSpace(carnet) || string.IsNullOrWhiteSpace(tel))
                                continue;

                            carnet = carnet.Trim();
                            if (carnet.All(char.IsDigit) && carnet.Length < 6)
                                carnet = carnet.PadLeft(6, '0');

                            var digits = new string(tel.Where(char.IsDigit).ToArray());
                            if (digits.StartsWith("505")) digits = digits.Substring(3);
                            if (digits.Length != 8)
                                continue;

                            lista.Add(new TelefonoExcelVm
                            {
                                Carnet = carnet,
                                NombreApellidos = nombre,
                                Gerencia = gerencia,
                                Telefono = digits,
                                PhoneType = "WM",      // MOVIL
                                HacerPrincipal = hacerPrincipal
                            });
                        }
                    }

                    // ================= HOJA FIJO =================
                    IXLWorksheet wsFijo = wb.Worksheets
                        .FirstOrDefault(x =>
                            x.Name.Trim().Equals("FIJO", StringComparison.OrdinalIgnoreCase) ||
                            x.Name.Trim().IndexOf("FIJO", StringComparison.OrdinalIgnoreCase) >= 0);

                    // si no encontró por nombre pero hay 2+ hojas, tomamos la 2da como FIJO
                    if (wsFijo == null && wb.Worksheets.Count >= 2)
                    {
                        var candidate = wb.Worksheet(2);
                        // evitamos reutilizar MOVIL si es la misma
                        if (wsMovil == null || !ReferenceEquals(candidate, wsMovil))
                            wsFijo = candidate;
                    }

                    if (wsFijo != null)
                    {
                        var lastRowF = wsFijo.LastRowUsed().RowNumber();
                        var lastColF = wsFijo.LastColumnUsed().ColumnNumber();

                        var headersF = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 1; c <= lastColF; c++)
                        {
                            var key = wsFijo.Cell(1, c).GetString().Trim();
                            if (!string.IsNullOrEmpty(key) && !headersF.ContainsKey(key))
                                headersF.Add(key, c);
                        }

                        string GetCellF(int row, string colName)
                        {
                            if (!headersF.ContainsKey(colName)) return "";
                            return wsFijo.Cell(row, headersF[colName]).GetString().Trim();
                        }

                        for (int r = 2; r <= lastRowF; r++)
                        {
                            var carnet = GetCellF(r, "# EMPLEADO");
                            if (string.IsNullOrWhiteSpace(carnet))
                                carnet = GetCellF(r, "NO. EMPLEADO"); // por si acaso

                            var tel = GetCellF(r, "LINEA ASIGNADA");

                            if (string.IsNullOrWhiteSpace(carnet) || string.IsNullOrWhiteSpace(tel))
                                continue;

                            carnet = carnet.Trim();
                            if (carnet.All(char.IsDigit) && carnet.Length < 4)
                                carnet = carnet.PadLeft(6, '0');

                            var digits = new string(tel.Where(char.IsDigit).ToArray());
                            if (digits.StartsWith("505")) digits = digits.Substring(3);
                            if (digits.Length != 8)
                                continue;

                            var nombre = GetCellF(r, "NOMBRE DE ASIGNADO");
                            var gerencia = GetCellF(r, "GERENCIA");

                            lista.Add(new TelefonoExcelVm
                            {
                                Carnet = carnet,
                                NombreApellidos = nombre,
                                Gerencia = gerencia,
                                Telefono = digits,
                                PhoneType = "H1",     // FIJO
                                HacerPrincipal = false // fijo no maneja principal
                            });
                        }
                    }
                }

                return Json(new
                {
                    ok = true,
                    count = lista.Count,
                    items = lista
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error leyendo archivo: " + ex.Message });
            }
        }


        // ========== 1) IMPORTAR EXCEL (usa ClosedXML, estilo que ya usas) ==========
        //[HttpPost]
        //public JsonResult ImportarExcel(HttpPostedFileBase archivo)
        //{
        //    if (archivo == null || archivo.ContentLength == 0)
        //        return Json(new { ok = false, msg = "Seleccione un archivo." });

        //    var lista = new List<TelefonoExcelVm>();

        //    try
        //    {
        //        using (var wb = new XLWorkbook(archivo.InputStream))
        //        {
        //            var ws = wb.Worksheets.First();
        //            var lastRow = ws.LastRowUsed().RowNumber();
        //            var lastCol = ws.LastColumnUsed().ColumnNumber();

        //            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        //            for (int c = 1; c <= lastCol; c++)
        //            {
        //                var key = ws.Cell(1, c).GetString().Trim();
        //                if (!string.IsNullOrEmpty(key) && !headers.ContainsKey(key))
        //                    headers.Add(key, c);
        //            }

        //            string GetCell(int row, string colName)
        //            {
        //                if (!headers.ContainsKey(colName)) return "";
        //                return ws.Cell(row, headers[colName]).GetString().Trim();
        //            }

        //            for (int r = 2; r <= lastRow; r++)
        //            {
        //                // NO. EMPLEADO o PersonNumber
        //                var carnet = GetCell(r, "NO. EMPLEADO");
        //                if (string.IsNullOrWhiteSpace(carnet))
        //                    carnet = GetCell(r, "PersonNumber");

        //                var tel = GetCell(r, "Telefono");
        //                if (string.IsNullOrWhiteSpace(tel))
        //                    tel = GetCell(r, "Teléfono");

        //                // nuevos campos solo display
        //                var nombre = GetCell(r, "NOMBRE Y APELLIDOS");
        //                var gerencia = GetCell(r, "GERENCIA");

        //                var hacerPrincipalTxt = GetCell(r, "HacerPrincipal");
        //                bool hacerPrincipal = true;


        //                if (string.IsNullOrWhiteSpace(carnet) || string.IsNullOrWhiteSpace(tel))
        //                    continue;

        //                // normalizar carnet: si solo dígitos y < 6 → pad left
        //                carnet = carnet.Trim();
        //                if (carnet.All(char.IsDigit) && carnet.Length < 6)
        //                    carnet = carnet.PadLeft(6, '0');

        //                // normalizar teléfono NI
        //                var digits = new string(tel.Where(char.IsDigit).ToArray());
        //                if (digits.StartsWith("505")) digits = digits.Substring(3);
        //                if (digits.Length != 8)
        //                    continue;

        //                lista.Add(new TelefonoExcelVm
        //                {
        //                    Carnet = carnet,
        //                    NombreApellidos = nombre,
        //                    Gerencia = gerencia,
        //                    Telefono = digits,
        //                    HacerPrincipal = hacerPrincipal
        //                });
        //            }
        //        }

        //        return Json(new { ok = true, count = lista.Count, items = lista });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { ok = false, msg = "Error leyendo archivo: " + ex.Message });
        //    }
        //}
        //// ========== 2) ENVIAR UNA FILA (se invoca en loop JS) ==========
        [HttpPost]
        public async Task<JsonResult> Enviar(string carnet, string telefono, bool hacerPrincipal = true)
        {
            // normaliza de nuevo por seguridad
            var digits = new string((telefono ?? "").Where(char.IsDigit).ToArray());
            if (digits.StartsWith("505")) digits = digits.Substring(3);
            if (digits.Length != 8)
                return Json(new { accion = "ERROR", estado = "ERROR", msg = "Teléfono inválido (NI 8 dígitos)." });

            // 1) evitar reenvío si ya hubo OK con ese número
            bool existeOk;
            using (var cn = new SqlConnection(CadenaSql))
            {
                existeOk = await cn.ExecuteScalarAsync<bool>(
                    "dbo.usp_HcmTel_ExisteOK",
                    new { person_number = carnet, numero = digits },
                    commandType: CommandType.StoredProcedure
                );
            }
            if (existeOk)
            {
                await LogAsync(carnet, digits, "SKIP_DB", "SKIP", 200, "Omitido por historial OK");
                return Json(new { accion = "SKIP_DB", estado = "SKIP", http = 200, msg = "Omitido por historial OK" });
            }

            string workersUniqId = null;
            long? phoneId = null;

            try
            {
                // 2) Worker uniq id
                workersUniqId = await GetWorkersUniqIdAsync(carnet);
                if (string.IsNullOrEmpty(workersUniqId))
                {
                    await LogAsync(carnet, digits, "ERROR", "ERROR", 404, "No se encontró worker.");
                    return Json(new { accion = "ERROR", estado = "ERROR", http = 404, msg = "No se encontró worker." });
                }

                // 3) Phones existentes
                var phones = await GetPhonesAsync(workersUniqId);
                var wms = phones.Where(p => (string)p["PhoneType"] == "WM").ToList();
                var mismo = wms.FirstOrDefault(p => (string)p["PhoneNumber"] == digits);
                var principal = wms.FirstOrDefault(p => (bool?)p["PrimaryFlag"] == true);

                if (mismo != null)
                {
                    phoneId = (long)mismo["PhoneId"];

                    if (hacerPrincipal && ((bool?)mismo["PrimaryFlag"] != true))
                    {
                        // promover este
                        await PatchPrimaryAsync(workersUniqId, phoneId.Value, true);
                        // bajar anterior
                        if (principal != null)
                            await PatchPrimaryAsync(workersUniqId, (long)principal["PhoneId"], false);

                        await LogAsync(carnet, digits, "PATCH_PROMOTE", "OK", 200, "Promovido a principal.", workersUniqId, phoneId);
                        return Json(new { accion = "PATCH_PROMOTE", estado = "OK", http = 200, msg = "Promovido a principal." });
                    }

                    var msg = ((bool?)mismo["PrimaryFlag"] == true)
                        ? "Ya es principal."
                        : "Ya existe WM con ese número.";
                    var acc = ((bool?)mismo["PrimaryFlag"] == true) ? "SKIP_PRINCIPAL" : "SKIP";

                    await LogAsync(carnet, digits, acc, "SKIP", 200, msg, workersUniqId, phoneId);
                    return Json(new { accion = acc, estado = "SKIP", http = 200, msg = msg });
                }

                // 4) Crear nuevo WM
                var body = new JObject
                {
                    ["PhoneType"] = "WM",
                    ["LegislationCode"] = "NI",
                    ["CountryCodeNumber"] = "505",
                    ["PhoneNumber"] = digits
                };
                if (hacerPrincipal)
                    body["PrimaryFlag"] = true;

                var createRes = await PostPhoneAsync(workersUniqId, body.ToString());

                if (createRes.StatusCode >= 200 && createRes.StatusCode < 300)
                {
                    var creado = JObject.Parse(createRes.Content);
                    phoneId = (long)creado["PhoneId"];
                    await LogAsync(carnet, digits, "POST", "OK", createRes.StatusCode, "Creado.", workersUniqId, phoneId);
                    return Json(new { accion = "POST", estado = "OK", http = createRes.StatusCode, msg = "Creado." });
                }

                // si 409 y se intentó como principal → reintenta sin principal
                if (createRes.StatusCode == 409 && hacerPrincipal)
                {
                    body["PrimaryFlag"] = false;
                    var retryRes = await PostPhoneAsync(workersUniqId, body.ToString());

                    if (retryRes.StatusCode >= 200 && retryRes.StatusCode < 300)
                    {
                        var creado = JObject.Parse(retryRes.Content);
                        phoneId = (long)creado["PhoneId"];
                        await LogAsync(carnet, digits, "POST", "OK", retryRes.StatusCode, "Creado sin principal (409).", workersUniqId, phoneId);
                        return Json(new { accion = "POST", estado = "OK", http = retryRes.StatusCode, msg = "Creado sin principal (409)." });
                    }

                    await LogAsync(carnet, digits, "ERROR", "ERROR", retryRes.StatusCode, retryRes.Content, workersUniqId, null);
                    return Json(new { accion = "ERROR", estado = "ERROR", http = retryRes.StatusCode, msg = retryRes.Content });
                }

                // error directo
                await LogAsync(carnet, digits, "ERROR", "ERROR", createRes.StatusCode, createRes.Content, workersUniqId, null);
                return Json(new { accion = "ERROR", estado = "ERROR", http = createRes.StatusCode, msg = createRes.Content });
            }
            catch (Exception ex)
            {
                await LogAsync(carnet, digits, "ERROR", "ERROR", null, ex.Message, workersUniqId, phoneId);
                return Json(new { accion = "ERROR", estado = "ERROR", http = (int?)null, msg = ex.Message });
            }
        }

        // ========== Helpers HCM (HttpWebRequest, estilo tu FetchContactItemsAsync) ==========

        private string BuildAuthHeader()
        {
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(HcmUser + ":" + HcmPass);
            return "Basic " + Convert.ToBase64String(bytes);
        }

        private async Task<string> HcmGetAsync(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Headers.Add("Authorization", BuildAuthHeader());
            req.Headers.Add("REST-Framework-Version", "2");
            req.Accept = "application/json";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (var resp = (HttpWebResponse)await req.GetResponseAsync())
            using (var rs = resp.GetResponseStream())
            using (var rd = new StreamReader(rs))
                return await rd.ReadToEndAsync();
        }

        private async Task<HcmResult> HcmSendAsync(string url, string method, string body)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.Headers.Add("Authorization", BuildAuthHeader());
            req.Headers.Add("REST-Framework-Version", "2");
            req.Accept = "application/json";
            req.ContentType = "application/vnd.oracle.adf.resourceitem+json";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var bytes = Encoding.UTF8.GetBytes(body ?? "{}");
            using (var rs = await req.GetRequestStreamAsync())
            {
                await rs.WriteAsync(bytes, 0, bytes.Length);
            }

            try
            {
                using (var resp = (HttpWebResponse)await req.GetResponseAsync())
                using (var s = resp.GetResponseStream())
                using (var r = new StreamReader(s))
                {
                    return new HcmResult
                    {
                        StatusCode = (int)resp.StatusCode,
                        Content = await r.ReadToEndAsync()
                    };
                }
            }
            catch (WebException wex)
            {
                var res = wex.Response as HttpWebResponse;
                var code = res != null ? (int)res.StatusCode : 0;
                string content = wex.Message;

                if (res != null)
                {
                    try
                    {
                        using (var s = res.GetResponseStream())
                        using (var r = new StreamReader(s))
                            content = await r.ReadToEndAsync();
                    }
                    catch { }
                }

                return new HcmResult { StatusCode = code, Content = content };
            }
        }

        private async Task<string> GetWorkersUniqIdAsync(string personNumber)
        {
            var url = $"{HcmBase}/hcmRestApi/resources/11.13.18.05/workers?q=PersonNumber='{personNumber}'";
            var json = await HcmGetAsync(url);
            if (string.IsNullOrWhiteSpace(json)) return null;

            var j = JObject.Parse(json);
            var item = j["items"]?.First;
            if (item == null) return null;

            var self = item["links"]?.FirstOrDefault(l => (string)l["rel"] == "self");
            if (self == null) return null;

            var href = (string)self["href"];
            var parts = href.Split(new[] { "/workers/" }, StringSplitOptions.None);
            return parts.Length == 2 ? parts[1] : null;
        }

        private async Task<JArray> GetPhonesAsync(string workersUniqId)
        {
            var url = $"{HcmBase}/hcmRestApi/resources/11.13.18.05/workers/{workersUniqId}/child/phones?onlyData=true";
            var json = await HcmGetAsync(url);
            var j = JObject.Parse(json);
            return (JArray)(j["items"] ?? new JArray());
        }

        private Task<HcmResult> PostPhoneAsync(string workersUniqId, string body)
        {
            var url = $"{HcmBase}/hcmRestApi/resources/11.13.18.05/workers/{workersUniqId}/child/phones";
            return HcmSendAsync(url, "POST", body);
        }

        private async Task PatchPrimaryAsync(string workersUniqId, long phoneId, bool value)
        {
            var url = $"{HcmBase}/hcmRestApi/resources/11.13.18.05/workers/{workersUniqId}/child/phones/{phoneId}";
            var body = new JObject { ["PrimaryFlag"] = value }.ToString();
            var res = await HcmSendAsync(url, "PATCH", body);
            if (res.StatusCode < 200 || res.StatusCode >= 300)
                throw new Exception($"PATCH PrimaryFlag: {res.StatusCode} - {res.Content}");
        }

        // ========== Helpers LOG ==========
        private async Task LogAsync(
            string carnet,
            string numero,
            string accion,
            string estado,
            int? http,
            string msg,
            string workersUniqId = null,
            long? phoneId = null)
        {
            using (var cn = new SqlConnection(CadenaSql))
            {
                await cn.ExecuteAsync(
                    "dbo.usp_HcmTel_InsertLog",
                    new
                    {
                        person_number = carnet,
                        numero = numero,
                        accion = accion,
                        estado = estado,
                        http_code = http,
                        mensaje = msg,
                        workers_uniq_id = workersUniqId,
                        phone_id = phoneId
                    },
                    commandType: CommandType.StoredProcedure
                );
            }
        }
    }

}

 