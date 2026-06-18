using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using ClosedXML.Excel;
using Dapper;
using DevExpress.Data;
using DevExpress.Data.Filtering;
using DevExpress.Web.Mvc;
using DevExpress.XtraCharts;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using Newtonsoft.Json;
using RestSharp;
using slnRhonline.Models;
using slnRhonline.Reports;


namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class ExtraTimeController : Controller
    {
        const string detailViewModel = "gvConsultDetail";

        const string keyEmployee = "Id_Employee";

   
        const string KeyPeriodEnd = "PeriodEndDate";
        const string KeyPeriodStart = "PeriodStarDate";
        const string summaryViewModel = "gvSummary";
 

        #region Lista de empleados
        [Authorize]

        public ActionResult EmployeesList()
        {
            return View();
        }
        public ActionResult EmployeesListM()
        {
            return View();
        }
        public ActionResult EmployeeDetailM(string id)
        {
            Entities.Employees Employee = new Entities.Employees();
            Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.EmployeeNumber == id);
            //  Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);
            // Se debera cambiar por la api de foto
            Session.Remove("sExpense");
            Session["fullName"] = Employee.FullName;
            Session["IDempleado"] = id;
            return View("EmployeeDetailM", Employee);
            
        }


        public JsonResult EmployeesListjsone()
        {

            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();
            try
    
            {
                //lstEmployees = Data.Employee.GetEmployeesByBossToHoraextra();
                lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();
                Session["listaemployee"] = lstEmployees;

                // Serializar la lista de empleados utilizando Newtonsoft.Json
                //  string jsonData = JsonConvert.SerializeObject(lstEmployees);
                var projectedEmployees = lstEmployees.Select(employee => new
                {
                    Idhrms = employee.Idhrms,

                    idorganizacion = employee.GERENCIAIDHRMS,
                    EmployeeNumber = employee.EmployeeNumber,
                    FullName = employee.FullName,
                    Location = employee.Nombreubicacion
                }).ToList();
                return Json(new { data = projectedEmployees }, JsonRequestBehavior.AllowGet); ;
                
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult EmployeesListjson()
        {

            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();
            try
            {
                lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();
                Session["listaemployee"] = lstEmployees;

                // Serializar la lista de empleados utilizando Newtonsoft.Json
                //  string jsonData = JsonConvert.SerializeObject(lstEmployees);
                var projectedEmployees = lstEmployees.Select(employee => new
                {
                    Idhrms = employee.Idhrms,

                    idorganizacion = employee.GERENCIAIDHRMS,
                    EmployeeNumber = employee.EmployeeNumber,
                    FullName = employee.FullName,
                    Location = employee.Nombreubicacion
                }).ToList();
                return Json(new { data = projectedEmployees }, JsonRequestBehavior.AllowGet); ;

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AuthorizationDashboard()
        {
            int ID_PERMISO_JEFE = 7;
            int ID_PERMISO_GERENTE = 8;

            bool tienePermisoJefe = false;
            bool tienePermisoGerente = false;

            if (Session["Menulista"] != null)
            {
                try
                {
                    var menuList = (List<Entities.MenuRhOnline>)Session["Menulista"];

                    // Usamos la búsqueda recursiva para encontrar el ID en cualquier nivel (Padre o Hijo)
                    tienePermisoJefe = BuscarPermisoRecursivo(menuList, ID_PERMISO_JEFE);
                    tienePermisoGerente = BuscarPermisoRecursivo(menuList, ID_PERMISO_GERENTE);
                }
                catch
                {
                    // Si falla la conversión o la sesión está corrupta, asumimos falso por seguridad
                    tienePermisoJefe = false;
                    tienePermisoGerente = false;
                }
            }

            // Pasamos los permisos a la Vista
            ViewBag.CanAuthBoss = tienePermisoJefe;
            ViewBag.CanAuthManager = tienePermisoGerente;
            ViewBag.CanAuthManager = tienePermisoGerente;

            return View();
        }
        // Método recursivo para buscar un ID en cualquier nivel del menú
        private bool BuscarPermisoRecursivo(List<Entities.MenuRhOnline> menuList, int targetId)
        {
            if (menuList == null) return false;

            foreach (var item in menuList)
            {
                // 1. Validar si el item actual es el que buscamos
                if (item.ID == targetId)
                {
                    return true;
                }

                // 2. Si tiene hijos, buscar dentro de la lista de hijos (Recursividad)
                if (item.MenuList != null && item.MenuList.Count > 0)
                {
                    bool encontradoEnHijos = BuscarPermisoRecursivo(item.MenuList, targetId);
                    if (encontradoEnHijos)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private List<Entities.Nominaexcel> ObtenerDataApiFiltrada()
        {
            Entities.Employees eEmployee = (Entities.Employees)Session["User"];
            if (eEmployee == null) return new List<Entities.Nominaexcel>();

            var apiUrl = "http://172.26.54.66/apihcm/api/empleado/gethoraextratodo3";

            try
            {
                var client = new RestClient(apiUrl);
                var request = new RestRequest(Method.GET) { Timeout = 30000 };
                var resp = client.Execute(request);

                if (resp == null || string.IsNullOrWhiteSpace(resp.Content))
                    return new List<Entities.Nominaexcel>();

                var raw = resp.Content.Trim().Trim('"');
                if (string.Equals(raw, "SIN RESULTADO", StringComparison.OrdinalIgnoreCase))
                    return new List<Entities.Nominaexcel>();

                var settings = new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal };
                var lista = JsonConvert.DeserializeObject<List<Entities.Nominaexcel>>(resp.Content, settings);

                if (lista == null) lista = new List<Entities.Nominaexcel>();

                // --- LOGICA DE FILTRADO POR EMPLEADOS DEL USUARIO ---
                // Obtenemos la lista de subordinados desde tu base local
                List<Entities.Employees> lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();

                // Creamos lista de Carnets para comparar
                var projectedEmployees = lstEmployees.Select(e => e.EmployeeNumber).ToList();

                // Filtramos lo que viene de la API
                lista = lista.Where(u => projectedEmployees.Contains(u.EMPLOYEE_NUMBER)).ToList();
 var empleadosPorCarnet = lstEmployees
    .Where(e => !string.IsNullOrEmpty(e.EmployeeNumber))
    .GroupBy(e => e.EmployeeNumber.Trim())
    .ToDictionary(g => g.Key, g => g.First());

                // 2) Filtra + mapea campos a la lista (u = lo que viene de la API)
                lista = lista
                    .Where(u => u != null && !string.IsNullOrEmpty(u.EMPLOYEE_NUMBER))
                    .Select(u =>
                    {
                        Entities.Employees emp;
                        if (empleadosPorCarnet.TryGetValue(u.EMPLOYEE_NUMBER.Trim(), out emp))
                        {
            // llenar los 3 campos en el objeto de la API (si existen ahí), o en tu modelo destino
            u.AREA = emp.area;               // <-- asegúrate que u tenga estas props
            u.GERENCIA = emp.GERENCIA;
                            u.FULL_NAME = emp.FullName;
                            return u;
                        }
                        return null;
                    })
                    .Where(u => u != null)
                    .ToList();
                return lista;
            }
            catch
            {
                return new List<Entities.Nominaexcel>();
            }
        }

        [HttpGet]
        public JsonResult GetBoardData2025(int stage)
        {
            try
            {
                // Obtenemos TODA la data filtrada una sola vez
                var listaCompleta = ObtenerDataApiFiltrada();
                List<Entities.Nominaexcel> resultado = new List<Entities.Nominaexcel>();

                // PESTAÑA 1: Solo Registrados (Pendiente Jefe)
                if (stage == 1)
                {
                    resultado = listaCompleta.Where(x => x.STATUS.Contains("GRABADO")).ToList();
                }
                // PESTAÑA 2: Solo Autorizado Jefe (Pendiente Gerente)
                else if (stage == 2)
                {
                    resultado = listaCompleta.Where(x => x.STATUS.Contains("APROBADO") == true).ToList();
                }
                else
                { return Json(new { data = listaCompleta }, JsonRequestBehavior.AllowGet); }
                // PESTAÑA 3: Histórico (Lo que ya pasó o está en RRHH)
                

                return Json(new { data = resultado }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<object>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult RegisterDetailPartialjson(string personId)
        {
            hcmmarcas.Root templ =new  hcmmarcas.Root();
            hcmmarcas.Root templ2 = new hcmmarcas.Root();
            try
            {
                string result = $"{int.Parse(personId):D6}";
                string url = "https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com:443/hcmRestApi/resources/latest/timeRecords?onlyData=true&q=personNumber=" + result + "; earnedDate>=2023-11-30;recordType=RANGE&fields=startTime,stopTime,measure,comment,earnedDate,personNumber&limit=100";
                var client = new RestClient(url);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", "Basic Q2xhcm9fUmhPbmxpbmVfV1NfU1M6SENNLVJIMG5sMW5lQCMz");
                request.AddHeader("Cookie", "_WL_AUTHCOOKIE_JSESSIONID=Xcv7uSOk-RzE7FeLUgTa");
                IRestResponse response = client.Execute(request);
                Console.WriteLine(response.Content);
                var result2 = response.Content;

                if (result2 != null)
                {
                    //lstExpenseDetail = serializer.Deserialize<List<RHOnlineWCF.bajasBS>>(result);
                    templ = JsonConvert.DeserializeObject<hcmmarcas.Root>(result2);
                    templ2.items = new List<hcmmarcas.Item>();
                    foreach (var q in templ.items)
                    {
                        if (q.measure=="1"&& (q.comment==""|| q.comment==null )&& q.startTime.Contains("T08:00:00+00:00")==true)
                        {
                            if (q.stopTime!=null && q.stopTime.Contains("T18:00:00+00:00")==true)
                            {
                                continue;
                            }
                        }
                        if (q.startTime!=null && q.startTime != "")
                        {
                            DateTime fechaHora = DateTime.ParseExact(q.startTime, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                            DateTime horaLocal = fechaHora.ToLocalTime();
                            string horaMinutoSegundoString = q.startTime.Split('T')[1].Split('+')[0];

                            // Dividir la cadena en partes: hora, minuto y segundo
                            string[] partes = horaMinutoSegundoString.Split(':');

                            // Convertir cada parte a entero
                            int hora = int.Parse(partes[0]);
                            int minuto = int.Parse(partes[1]);
                            int segundo = int.Parse(partes[2]);

                            // Obtener solo la parte de la hora
                            q.startTime = horaMinutoSegundoString;
                        }
                        if (q.stopTime != null && q.stopTime != "")
                        {
                            DateTime fechaHorax = DateTime.Parse(q.stopTime);
                            DateTime fechaHora = DateTime.ParseExact(q.stopTime, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                            DateTime horaLocal = fechaHora.ToLocalTime();
                            string horaMinutoSegundoString = q.stopTime.Split('T')[1].Split('+')[0];

                            // Dividir la cadena en partes: hora, minuto y segundo
                            string[] partes = horaMinutoSegundoString.Split(':');

                            // Convertir cada parte a entero
                            int hora = int.Parse(partes[0]);
                            int minuto = int.Parse(partes[1]);
                            int segundo = int.Parse(partes[2]);
                            // Obtener solo la parte de la hora
                            q.stopTime = horaMinutoSegundoString;
                            if (q.comment != null && q.comment != "" && q.comment.Contains("API") == true)
                            {
                                q.comment = "BIOMETRICO";
                            }
                            else { q.comment = "RELOJ WEB"; }
                        }
                        else { q.comment = "Marca incompleta"; }
                        templ2.items.Add(q);
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { data = templ2.items }, JsonRequestBehavior.AllowGet);

            }
            templ2.items = templ2.items.OrderBy(x => DateTime.Parse(x.earnedDate)).ToList();
            return Json(new { data = templ2.items }, JsonRequestBehavior.AllowGet);

        }


        /// <summary>
        /// Accion que retorna el metodo EmployeesBindingCore().
        /// </summary>
        /// <returns></returns>
        public ActionResult EmployeesListPartial()
        {
            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();
            try
            {
                lstEmployees = Data.Employee.GetEmployeesByBossToExtraTime();
            }
            catch (Exception)
            {

                throw;
            }
            return PartialView(lstEmployees);
        }

        //Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.EmployeeNumber == id);
        //        if (Session["User"] != null)
        //        {
        //            eEmployee = (Entities.Employees) Session["User"];
        //}
        public JsonResult ReporteMarcasJson(int carnet)
        { //Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.EmployeeNumber == id);

            Entities.Employees eEmployee = new Entities.Employees();
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            var data = Data.Employee.GetReporteMarcasByCarnet(eEmployee.EmployeeNumber);
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }
            public JsonResult GetMarcaWebClockDetalle(string carnet, DateTime fecha)
            { //Employee = Data.Employee.GetEmployeesByBossToExpenses().First(item => item.EmployeeNumber == id);

                Entities.Employees eEmployee = new Entities.Employees();
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                string jsont = JsonConvert.SerializeObject(eEmployee);
                var data = Data.Employee.GetMarcaWebClockDetalle(carnet, fecha);
                return Json(new { data }, JsonRequestBehavior.AllowGet);
            }
        private static string HoraToStr(object v)
        {
            if (v == null) return null;
            if (v is TimeSpan ts) return ts.ToString(@"hh\:mm\:ss");
            var s = v.ToString();
            if (TimeSpan.TryParse(s, out var t2)) return t2.ToString(@"hh\:mm\:ss");
            return s.Length >= 8 ? s.Substring(0, 8) : s;
        }

        // Semana pasada (lun-dom) por defecto
        private static void RangoPorDefecto(ref DateTime? fi, ref DateTime? ff)
        {
            if (fi != null && ff != null) return;
            var hoy = DateTime.Today;
            var dow = (int)hoy.DayOfWeek; if (dow == 0) dow = 7;
            var lunesActual = hoy.AddDays(-(dow - 1));
            fi = lunesActual.AddDays(-7);
            ff = fi.Value.AddDays(6);
        }

        // --------- VISTA PRO ----------
        [HttpGet]
        public ActionResult MarcasWebPro() => View(); // Views/ExtraTime/MarcasWebPro.cshtml

        // JSON para la vista PRO (nombra igual que en la página pro)
        [HttpGet]
        public ActionResult MarcasWebV3Json(string fechaIni, string fechaFin, int radio = 150, string categoria = null)
        {
            DateTime? fi = DateTime.TryParse(fechaIni, out var a) ? a : (DateTime?)null;
            DateTime? ff = DateTime.TryParse(fechaFin, out var b) ? b : (DateTime?)null;
            RangoPorDefecto(ref fi, ref ff);

            var data = BuildMarcasUnificadas(fi, ff, radio, categoria);
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        // Alias para compatibilidad con tu página actual (usa mismo motor)
        [HttpGet]
        public ActionResult V2Json(string fechaIni, string fechaFin, int radio = 150, string categoria = null)
            => MarcasWebV3Json(fechaIni, fechaFin, radio, categoria);

        // --------- core: une IN/OUT por día/persona y normaliza horas ----------
        private List<MarcaGeoDiaDto> BuildMarcasUnificadas(DateTime? fi, DateTime? ff, int radio, string categoria)
        {
            List<dynamic> filas;
            using (var cn = Conn())
            {
                filas = cn.Query(
                    "dbo.usp_MarcasGeo_V2",                    // SP que ya tienes
                    new { FechaIni = fi, FechaFin = ff, RadioMts = radio },
                    commandType: CommandType.StoredProcedure
                ).ToList();
            }

            var unidos = filas
                .GroupBy(x => new
                {
                    PersonId = (int)x.PersonId,
                    Dia = ((DateTime)x.Fecha).Date,
                    carnet = (string)x.carnet,
                    nombre = (string)x.nombre_completo,
                    ger = (string)x.OGERENCIA,
                    dep = (string)x.oDEPARTAMENTO,
                    edif = (string)x.Edificio,
                    latE = (decimal?)x.LatEdif,
                    lonE = (decimal?)x.LonEdif
                })
                .Select(g =>
                {
                    var ent = g.FirstOrDefault(r => (string)r.TipoMarca == "ORA_HWM_IN");
                    var sal = g.FirstOrDefault(r => (string)r.TipoMarca == "ORA_HWM_OUT");

                    return new MarcaGeoDiaDto
                    {
                        Fecha = g.Key.Dia,
                        PersonId = g.Key.PersonId,
                        carnet = g.Key.carnet,
                        nombre_completo = g.Key.nombre,
                        OGERENCIA = g.Key.ger,
                        oDEPARTAMENTO = g.Key.dep,
                        Edificio = g.Key.edif,
                        LatEdif = g.Key.latE,
                        LonEdif = g.Key.lonE,

                    // horas como string (para DataTable)
                    HoraEntrada = HoraToStr(ent?.Hora),
                        LatEntrada = ent?.Latitud,
                        LonEntrada = ent?.Longitud,
                        DistanciaEntrada = ent?.DistanciaMetros,
                        CategoriaEntrada = ent?.Categoria ?? "SIN MARCA",

                        HoraSalida = HoraToStr(sal?.Hora),
                        LatSalida = sal?.Latitud,
                        LonSalida = sal?.Longitud,
                        DistanciaSalida = sal?.DistanciaMetros,
                        CategoriaSalida = sal?.Categoria ?? "SIN MARCA"
                    };
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                unidos = unidos.Where(u =>
                       (categoria.Equals("SIN MARCA", StringComparison.OrdinalIgnoreCase)
                            && u.CategoriaEntrada == "SIN MARCA" && u.CategoriaSalida == "SIN MARCA")
                    || string.Equals(u.CategoriaEntrada, categoria, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(u.CategoriaSalida, categoria, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            return unidos;
        }
        public ActionResult V2() => View();
        public ActionResult V3() => View();

        // DataTables JSON (usa usp_MarcasGeo_V2)
        //[HttpGet]
        //public ActionResult V2Json(string fechaIni, string fechaFin, int radio = 100, string categoria = null)
        //{

        //    DateTime? fi = DateTime.TryParse(fechaIni, out var a) ? a : (DateTime?)null;
        //    DateTime? ff = DateTime.TryParse(fechaFin, out var b) ? b : (DateTime?)null;

        //    if (fi == null || ff == null) // semana pasada (lun-dom)
        //    {
        //        var hoy = DateTime.Today;
        //        var dow = (int)hoy.DayOfWeek; if (dow == 0) dow = 7;
        //        var lunesActual = hoy.AddDays(-(dow - 1));
        //        fi = lunesActual.AddDays(-7);
        //        ff = fi.Value.AddDays(6);
        //    }
        //    // 1) Trae IN/OUT separados (SP v2)
        //    List<dynamic> filas;
        //    using (var cn = Conn())
        //    {
        //        filas = cn.Query(
        //            "dbo.usp_MarcasGeo_V2",
        //            new { FechaIni = fi, FechaFin = ff, RadioMts = radio },
        //            commandType: CommandType.StoredProcedure
        //        ).ToList();
        //    }

        //    // 2) Une por día/persona y formatea horas a STRING
        //    var unidos = filas
        //        .GroupBy(x => new
        //        {
        //            PersonId = (int)x.PersonId,
        //            Dia = ((DateTime)x.Fecha).Date,
        //            carnet = (string)x.carnet,
        //            nombre = (string)x.nombre_completo,
        //            ger = (string)x.OGERENCIA,
        //            dep = (string)x.oDEPARTAMENTO,
        //            edif = (string)x.Edificio,
        //            latE = (decimal?)x.LatEdif,
        //            lonE = (decimal?)x.LonEdif
        //        })
        //        .Select(g =>
        //        {
        //            var ent = g.FirstOrDefault(r => (string)r.TipoMarca == "ORA_HWM_IN");
        //            var sal = g.FirstOrDefault(r => (string)r.TipoMarca == "ORA_HWM_OUT");

        //            return new MarcaGeoDiaDto
        //            {
        //                Fecha = g.Key.Dia,
        //                PersonId = g.Key.PersonId,
        //                carnet = g.Key.carnet,
        //                nombre_completo = g.Key.nombre,
        //                OGERENCIA = g.Key.ger,
        //                oDEPARTAMENTO = g.Key.dep,
        //                Edificio = g.Key.edif,
        //                LatEdif = g.Key.latE,
        //                LonEdif = g.Key.lonE,

        //            // ⬇️ horas como string
        //            HoraEntrada = HoraToStr(ent?.Hora),
        //                LatEntrada = ent?.Latitud,
        //                LonEntrada = ent?.Longitud,
        //                DistanciaEntrada = ent?.DistanciaMetros,
        //                CategoriaEntrada = ent?.Categoria ?? "SIN MARCA",

        //                HoraSalida = HoraToStr(sal?.Hora),
        //                LatSalida = sal?.Latitud,
        //                LonSalida = sal?.Longitud,
        //                DistanciaSalida = sal?.DistanciaMetros,
        //                CategoriaSalida = sal?.Categoria ?? "SIN MARCA"
        //            };
        //        })
        //        .ToList();

        //    // 3) Filtro opcional por categoría (match si IN o OUT coincide)
        //    if (!string.IsNullOrWhiteSpace(categoria))
        //    {
        //        unidos = unidos.Where(u =>
        //               (categoria.Equals("SIN MARCA", StringComparison.OrdinalIgnoreCase)
        //                    && u.CategoriaEntrada == "SIN MARCA" && u.CategoriaSalida == "SIN MARCA")
        //            || string.Equals(u.CategoriaEntrada, categoria, StringComparison.OrdinalIgnoreCase)
        //            || string.Equals(u.CategoriaSalida, categoria, StringComparison.OrdinalIgnoreCase)
        //        ).ToList();
        //    }

        //    return Json(new { data = unidos }, JsonRequestBehavior.AllowGet);
        //}
        //private static string HoraToStr(object v)
        //{
        //    if (v == null) return null;
        //    if (v is TimeSpan ts) return ts.ToString(@"hh\:mm\:ss");
        //    var s = v.ToString();
        //    if (TimeSpan.TryParse(s, out var t2)) return t2.ToString(@"hh\:mm\:ss");
        //    return (s.Length >= 8 ? s.Substring(0, 8) : s);
        ////}
        public class MarcaGeoDiaV4Dto
        {
            public DateTime Fecha { get; set; }
            public long PersonId { get; set; }
            public string carnet { get; set; }
            public string nombre_completo { get; set; }
            public string OGERENCIA { get; set; }
            public string oDEPARTAMENTO { get; set; }
            public string DefaultUbicacion { get; set; }
            public string DeviceIn { get; set; }
            public string DeviceOut { get; set; }
            public string DispositivoIn { get; set; }
            public string DispositivoOut { get; set; }
            // IN
            public string HoraEntrada { get; set; }
            public decimal? LatEntrada { get; set; }
            public decimal? LonEntrada { get; set; }
            public int? DistanciaEntrada { get; set; }
            public string CategoriaEntrada { get; set; }
            public string PermEdifIn { get; set; }
            public decimal? PermLatIn { get; set; }
            public decimal? PermLonIn { get; set; }
            public string PermEdificioIn { get; set; }
            public decimal? PrecisionIn { get; set; }
            // OUT
            public string HoraSalida { get; set; }
            public decimal? LatSalida { get; set; }
            public decimal? LonSalida { get; set; }
            public int? DistanciaSalida { get; set; }
            public string CategoriaSalida { get; set; }
            public string PermEdifOut { get; set; }
            public decimal? PermLatOut { get; set; }
            public decimal? PermLonOut { get; set; }
            public string PermEdificioOut { get; set; }
            public decimal? PrecisionOut { get; set; }

        }

        public class PermisoEdificioDto
        {
            public string Edificio { get; set; }
            public decimal? LatEdif { get; set; }
            public decimal? LonEdif { get; set; }
            public bool IsDefault { get; set; }
        }

        private static DateTime? ToDateOrNull(string s)
            => DateTime.TryParse(s, out var d) ? (DateTime?)d : null; 
        // ================== V4 JSON ==================
        [HttpGet]
        public ActionResult MarcasWebV4Json(string fechaIni, string fechaFin, int radio = 150)
        {
            var fi = ToDateOrNull(fechaIni);
            var ff = ToDateOrNull(fechaFin);

            string HoraToStr(TimeSpan? t) => t.HasValue ? t.Value.ToString(@"hh\:mm\:ss") : null;
            string DeviceToLabel(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var up = raw.ToUpperInvariant();
                if (up.Contains("MOBILE")) return "Móvil";
                if (up.Contains("DESKTOP")) return "Computador";
                return raw;
            }
            Entities.Employees eEmployee = new Entities.Employees();
            
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            
            using (var cn = Conn())
            {
                string boss_id = eEmployee.EmployeeNumber;
                var rows = cn.Query(
  @"EXEC dbo.usp_MarcasGeo_V8 
         @Desde=@fi, 
         @Hasta=@ff, 
         @RadioMts=@radio, 
         @Boss_Id=@boss",
  new { fi, ff, radio, boss = boss_id },      // ← parámetros
  commandTimeout: 120
).ToList();

                var unidos = rows
                  .GroupBy(r => new {
                      PersonId = (long)r.PersonId,
                      Fecha = ((DateTime)r.Fecha).Date,
                      carnet = (string)r.carnet,
                      nombre = (string)r.nombre_completo,
                      ger = (string)r.OGERENCIA,
                      dep = (string)r.oDEPARTAMENTO,
                      defUbic = (string)r.DefaultUbicacion
                  })
                  .Select(g => {
                      var ent = g.FirstOrDefault(x => (string)x.TipoMarca == "ORA_HWM_IN");
                      var sal = g.FirstOrDefault(x => (string)x.TipoMarca == "ORA_HWM_OUT");

                      TimeSpan? tIn = (TimeSpan?)ent?.Hora;
                      TimeSpan? tOut = (TimeSpan?)sal?.Hora;

                      return new MarcaGeoDiaV4Dto
                      {
                          Fecha = g.Key.Fecha,
                          PersonId = g.Key.PersonId,
                          carnet = g.Key.carnet,
                          nombre_completo = g.Key.nombre,
                          OGERENCIA = g.Key.ger,
                          oDEPARTAMENTO = g.Key.dep,
                          DefaultUbicacion = g.Key.defUbic,

                          HoraEntrada = HoraToStr(tIn),
                          LatEntrada = (decimal?)ent?.LatMarca,
                          LonEntrada = (decimal?)ent?.LonMarca,
                          DistanciaEntrada = (int?)ent?.DistanciaMetros,
                          CategoriaEntrada = (string)ent?.Categoria ?? "SIN MARCA",
                          PermEdificioIn = (string)ent?.PermittedEdificio,
                          PermLatIn = (decimal?)ent?.PermLat,
                          PermLonIn = (decimal?)ent?.PermLon,
                          PrecisionIn = (decimal?)ent?.PrecisionMetros,
                          DispositivoIn = DeviceToLabel((string)ent?.deviceType),

                          HoraSalida = HoraToStr(tOut),
                          LatSalida = (decimal?)sal?.LatMarca,
                          LonSalida = (decimal?)sal?.LonMarca,
                          DistanciaSalida = (int?)sal?.DistanciaMetros,
                          CategoriaSalida = (string)sal?.Categoria ?? "SIN MARCA",
                          PermEdificioOut = (string)sal?.PermittedEdificio,
                          PermLatOut = (decimal?)sal?.PermLat,
                          PermLonOut = (decimal?)sal?.PermLon,
                          PrecisionOut = (decimal?)sal?.PrecisionMetros,
                          DispositivoOut = DeviceToLabel((string)sal?.deviceType)
                      };
                  })
                  .OrderBy(x => x.carnet).ThenBy(x => x.Fecha)
                  .ToList();

                // (opcional) filtro por lista de empleados
                //try
                //{
                //    //var lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();
                //    //var set = lstEmployees.Select(e => e.EmployeeNumber);
                //    //unidos = unidos.Where(u => set.Contains(u.carnet)).ToList();
                //}
                //catch { }

                return Json(new { data = unidos }, JsonRequestBehavior.AllowGet);
            }
        }
        string MapDevice(string dev)
        {
            if (string.IsNullOrEmpty(dev)) return "N/A";
            if (dev.Contains("ORA_HWM_WC_DT_MOBILE_ON")) return "Móvil";
            if (dev.Contains("ORA_HWM_WC_DT_DESKTOP")) return "Computador";
            return dev; // por si viene algo distinto
        }

        [HttpGet]
        public ActionResult PermisosEmpleadoV4(long? personId, string carnet)
        {
            if (personId == null && string.IsNullOrWhiteSpace(carnet))
                return Json(new { ok = false, msg = "Parámetros insuficientes" }, JsonRequestBehavior.AllowGet);

            try
            {
                using (var cn = Conn())
                {
                    var data = cn.Query<PermisoEdificioDto>(
                        "dbo.usp_PermisosEdificio_Empleado",
                        new { PersonId = personId, Carnet = carnet },
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 60
                    ).ToList();

                    return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // (opcional) loguear ex
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ================== V4 Permisos (todos los edificios permitidos) ==================
        public class MarcaGeoDiaDto
        {
            public DateTime Fecha { get; set; }
            public int PersonId { get; set; }
            public string carnet { get; set; }
            public string nombre_completo { get; set; }
            public string OGERENCIA { get; set; }
            public string oDEPARTAMENTO { get; set; }
            public string Edificio { get; set; }
            public decimal? LatEdif { get; set; }
            public decimal? LonEdif { get; set; }

            public string HoraEntrada { get; set; }
            public decimal? LatEntrada { get; set; }
            public decimal? LonEntrada { get; set; }
            public int? DistanciaEntrada { get; set; }
            public string CategoriaEntrada { get; set; }

            public string HoraSalida { get; set; }
            public decimal? LatSalida { get; set; }
            public decimal? LonSalida { get; set; }
            public int? DistanciaSalida { get; set; }
            public string CategoriaSalida { get; set; }

        }
        public class PuntoDto { public TimeSpan? Hora { get; set; } public decimal? Latitud { get; set; } public decimal? Longitud { get; set; } }
        public class EsperadoDto { public string Edificio { get; set; } public string NombreUbicacion { get; set; } public decimal? LatEdif { get; set; } public decimal? LonEdif { get; set; } }

        // Detalle para mapa (esperado vs IN/OUT del día)
        [HttpGet]
        public ActionResult V2DetalleMapa(long personId, string fecha)
        {
            if (!DateTime.TryParse(fecha, out var f))
                return Json(new { ok = false, msg = "Fecha inválida" }, JsonRequestBehavior.AllowGet);

            const string sql = @"
SELECT TOP 1 ed.Nombre AS Edificio, e.NombreUbicacion, ed.Latitud AS LatEdif, ed.Longitud AS LonEdif
FROM EMP2024 e
JOIN emp b ON b.PersonNumber = e.carnet
LEFT JOIN Edificios ed ON ed.Nombre = e.NombreUbicacion
WHERE b.PersonId = @PersonId;

SELECT TOP 1 Hora, Latitud, Longitud
FROM MarcasWebClock
WHERE PersonId = @PersonId AND CAST(Fecha AS date) = @F AND TipoMarca = 'ORA_HWM_IN'
ORDER BY Hora ASC;

SELECT TOP 1 Hora, Latitud, Longitud
FROM MarcasWebClock
WHERE PersonId = @PersonId AND CAST(Fecha AS date) = @F AND TipoMarca = 'ORA_HWM_OUT'
ORDER BY Hora DESC;";

            using (var cn = Conn())
            using (var multi = cn.QueryMultiple(sql, new { PersonId = personId, F = f }))
            {
                var esperado = multi.ReadFirstOrDefault<EsperadoDto>();
                var entrada = multi.ReadFirstOrDefault<PuntoDto>();
                var salida = multi.ReadFirstOrDefault<PuntoDto>();

                return Json(new { ok = true, data = new { Esperado = esperado, Entrada = entrada, Salida = salida } },
                            JsonRequestBehavior.AllowGet);
            }
        }
        private static SqlConnection Conn()
        {
            var cn = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIGHO1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;";

            var conn = new SqlConnection(cn);
            conn.Open();
            return conn;
        }
        #endregion
        #region Lista de horas extras por empleado
        public ActionResult marcaje()
        {
            Entities.Employees eEmployee = new Entities.Employees();
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
             
            ViewBag.Carnet = eEmployee.EmployeeNumber;
            return View();
        }
        /// <summary>  
        ///  Accion para cargar los registros del empleado seleccioando
        /// </summary>  

        public ActionResult EmployeeDetail(long id)
        {
           Session["empleadoextratime"]= id;

            Entities.Employees Employee = new Entities.Employees();
            Employee = Data.Employee.GetEmployeesByBossToExtraTime().First(item => item.Idhrms == id);
        //    Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);

            return View("EmployeeDetail", Employee);

        }
        public ActionResult EmployeeDetailx( )
        {
            long id = (long)Session["empleadoextratime"];

 
            Entities.Employees Employee = new Entities.Employees();
            Employee = Data.Employee.GetEmployeesByBossToExtraTime().First(item => item.Idhrms == id);
            //    Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);

            return View("EmployeeDetail", Employee);

        }
        public ActionResult EmployeeDetailPartial(long personId)
        {
            List<Entities.ExtraTime> ListET = new List<Entities.ExtraTime>();

            //Sacamos la lista de periodos abiertos
            if (Session[KeyPeriodStart] == null || Convert.ToString(Session[KeyPeriodStart]) == string.Empty)
            {
                String Periods = Utils.ClaroWCF.ExtraTimePeriods();
                Session[KeyPeriodStart] = Periods.Substring(1, Periods.IndexOf(";") - 1).ToString().Trim();
                Session[KeyPeriodEnd] = Periods.Substring(Periods.IndexOf(";") + 1, Periods.Length - (Periods.IndexOf(";") + 1));
            }

            ViewData[KeyPeriodStart] = Session[KeyPeriodStart]; 
            ViewData[KeyPeriodEnd] = Session[KeyPeriodEnd];

            ListET = Data.ExtraTime.GetExtraTimeByEmployee(personId, Session[KeyPeriodStart].ToString(), Session[KeyPeriodEnd].ToString());




            return PartialView("EmployeeDetailPartial", ListET);
        }
      
        public JsonResult EmployeeDetailPartialjson(long personId)
        {
            List<Entities.ExtraTime> ListET = new List<Entities.ExtraTime>();

            //Sacamos la lista de periodos abiertos
            if (Session[KeyPeriodStart] == null || Convert.ToString(Session[KeyPeriodStart]) == string.Empty)
            {
                String Periods = Utils.ClaroWCF.ExtraTimePeriods();
                Session[KeyPeriodStart] = Periods.Substring(1, Periods.IndexOf(";") - 1).ToString().Trim();
                Session[KeyPeriodEnd] = Periods.Substring(Periods.IndexOf(";") + 1, Periods.Length - (Periods.IndexOf(";") + 1));
            }

            ViewData[KeyPeriodStart] = Session[KeyPeriodStart];
            ViewData[KeyPeriodEnd] = Session[KeyPeriodEnd];

            ListET = Data.ExtraTime.GetExtraTimeByEmployee(personId, Session[KeyPeriodStart].ToString(), Session[KeyPeriodEnd].ToString());



            return Json(new { data = ListET }, JsonRequestBehavior.AllowGet);

         }
        #endregion
        #region DashBoard ExtraTime Asignacion vs Ejecución


        public ActionResult DashboardExecutedPlannedPartial()
        {
            List<Entities.ExtratimeManagnment> lstBudget = new List<Entities.ExtratimeManagnment>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                if ((eEmployee.userlevel == 5) || (eEmployee.userlevel == 6))
                {
                    lstBudget = Data.ExtraTime.GetChartExecutedAssignment(eEmployee.Idhrms);

                }

                else
                {
                    lstBudget = new List<Entities.ExtratimeManagnment>();
                }

            }
            catch (Exception)
            {

                return PartialView(new List<Entities.ExtratimeManagnment>());
            }

            return PartialView(lstBudget);
        }



        #endregion
        #region DashBoard ExtraTime Top Ten


        public ActionResult DashboardExtraTimeTopTenPartial()
        {
            List<Entities.Employees> lstExtraTime = new List<Entities.Employees>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if ((eEmployee.userlevel == 5) || (eEmployee.userlevel == 6))
                {
                    string respPeriodId = Utils.ClaroWCF.GetPeriodDashboard();
                    lstExtraTime = Data.ExtraTime.GetExtraTimeTopTen(respPeriodId, eEmployee.Idhrms.ToString());
                }
                else
                {
                    lstExtraTime = new List<Entities.Employees>();
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView(lstExtraTime);

        }
        #region Export Chart Executed vs Assignment
        public ActionResult ExportChart()
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            ChartControlSettings settings = Models.ChartSettings.GetChartExtratimeSettings();

            using (MemoryStream stream = new MemoryStream())
            {
                settings.SaveToStream(stream);
                stream.Seek(0, SeekOrigin.Begin);
                ChartControl chartControl = new ChartControl();
                chartControl.LoadFromStream(stream);
                chartControl.Width = Convert.ToInt16(settings.Width.Value);
                chartControl.Height = Convert.ToInt16(settings.Height.Value);
                chartControl.OptionsPrint.SizeMode = DevExpress.XtraCharts.Printing.PrintSizeMode.Zoom;
                chartControl.DataSource = new List<Entities.ExtratimeManagnment>(Data.ExtraTime.GetChartExecutedAssignment(eEmployee.Idhrms));
                var pcl = new PrintableComponentLink(new PrintingSystem());
                pcl.Component = chartControl;
                pcl.Landscape = true;
                pcl.CreateDocument();

                using (var exstream = new MemoryStream())
                {
                    pcl.PrintingSystem.ExportToPdf(exstream);

                    byte[] buf = new byte[(int)exstream.Length];
                    exstream.Seek(0, SeekOrigin.Begin);
                    exstream.Read(buf, 0, buf.Length);

                    return File(buf, "application/pdf", "Chart-Horas Extras" + Guid.NewGuid().ToString() + ".pdf");
                }

            }
        }
        #endregion

        #endregion       
        #region CRUD

        /// <summary>
        /// Accion que llama  a metodo para insertar una inscripcion
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddExtraTime(Entities.ExtraTime extratime)
        {
 
 
            long Id_Employee = (long)Session["empleadoextratime"];
            extratime.Person_Id = Id_Employee;
            Validations.ExtraTime vExtratTime = new Validations.ExtraTime();
            bool resNegativeHours = vExtratTime.ValidateNegativeHours(extratime);
            bool resHoursRange = vExtratTime.ValidateHoursRange(extratime);
            string resHoursAssigment = vExtratTime.ValidateHousAssigment(extratime);
            bool resHoursAfterCurrentDate = vExtratTime.ValidateHoursAfterCurrentDate(extratime);

            if (ModelState.IsValid)
            {
                try
                {

                    if (resNegativeHours == false)
                    {
                        return Content("No se pueden guardar horas extras negativas, favor corregir.");
                    }
                    if (resHoursRange == false)
                    {
                        return Content("Ya existe ese rango de horas extras en la base de datos, favor corregir.");
                    }
                    if (resHoursAssigment != string.Empty )
                    {
                        return Content(resHoursAssigment);
                    }
                    if (resHoursAfterCurrentDate == false)
                    {
                        return Content("No se puede guardar un registro de horas extras posterior a la fecha actual, favor corregir.");
                    }
                    if (!extratime.ReasonId.HasValue)
                    {
                        return Content("El campo motivo es requerido. Por favor ingrese el dato");
                    }


                    if (Session["SupportFileBytes"] != null)
                    {
                        //Actualizando el valor de DepositFile en la base de datos
                        extratime.SupportFile = (byte[])Session["SupportFileBytes"];
                        
                    }

                   
                    //Llamar al metodo InsertExtraTime
                    string result = Data.ExtraTime.InsertExtraTime(extratime, Session[KeyPeriodStart].ToString(), Session[KeyPeriodEnd].ToString());
                    if (result != "Exito al insertar el registro")
                    {
                        return Content(result);
                    }

                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {

                return Content("Ocurrió un error al actualizar la información, por favor verifique los datos y vuelva a intentarlo.");
            }

            return EmployeeDetailPartial(extratime.Person_Id);
        }

        /// <summary>
        /// Accion que llama  a metodo para editar una hora extra
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditExtraTime(Entities.ExtraTime extratime)
        {
            long Id_Employee = (long)Session["empleadoextratime"];

       //     int Id_Employee = (int)Session[keyEmployee];
            extratime.Person_Id = Id_Employee;
            Validations.ExtraTime vExtratTime = new Validations.ExtraTime();
            bool resNegativeHours = vExtratTime.ValidateNegativeHours(extratime);
            bool resHoursRange = vExtratTime.ValidateHoursRange(extratime);
            string resHoursAssigment = vExtratTime.ValidateHousAssigment(extratime);
            bool resHoursAfterCurrentDate = vExtratTime.ValidateHoursAfterCurrentDate(extratime);
            bool resValidateStatus = vExtratTime.ValidateStatus(extratime.Id);
         

            if (ModelState.IsValid)
            {
                try
                {

                    if (resNegativeHours == false)
                    {
                        return Content("No se pueden guardar horas extras negativas, favor corregir.");
                    }
                    if (resHoursRange == false)
                    {
                        return Content("Ya existe ese rango de horas extras en la base de datos, favor corregir.");
                    }
                    if (resHoursAssigment != string.Empty)
                    {
                        return Content(resHoursAssigment);
                    }
                    if (resHoursAfterCurrentDate == false)
                    {
                        return Content("No se puede guardar un registro de horas extras posterior a la fecha actual, favor corregir.");
                    }
                    if (resValidateStatus == false)
                    {
                        return Content("No se puede editar el registro ya fue autorizado");
                    }
                    if (!extratime.ReasonId.HasValue)
                    {
                        return Content("El campo motivo es requerido. Por favor ingrese el dato");
                    }


                    if (Session["SupportFileBytes"] != null)
                    {
                        //Actualizando el valor de DepositFile en la base de datos
                        extratime.SupportFile = (byte[])Session["SupportFileBytes"];
                     
                    }

                   
                    //Llamar al metodo InsertExtraTime
                    string result = Data.ExtraTime.EditExtraTime(extratime);
                    if (result != "Exito al actualizar el registro")
                    {
                        return Content(result);
                    }

                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {

                return Content("Ocurrió un error al actualizar la información, por favor verifique los datos y vuelva a intentarlo.");
            }

            return EmployeeDetailPartial(extratime.Person_Id);
        }

        /// <summary>
        /// Accion que llama  a metodo para eliminar una hora extra
        /// </summary>
        /// <param name="systemUser"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteExtraTime(int id)
        {
            long Id_Employee = (long)Session["empleadoextratime"];

            //  int Id_Employee = (int)Session[keyEmployee];

            Validations.ExtraTime vExtratTime = new Validations.ExtraTime();
           
            bool resValidateStatus = vExtratTime.ValidateStatus(id);


            if (ModelState.IsValid)
            {
                try
                {

                   
                    if (resValidateStatus == false)
                    {
                        return Content("No se puede editar el registro porque ya fue autorizado");
                    }
                    


                    //Llamar al metodo InsertExtraTime
                    string result = Data.ExtraTime.DeleteExtraTime(id);
                    if (result != "Exito al actualizar el registro")
                    {
                        return Content(result);
                    }

                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {

                return Content("Ocurrió un error al actualizar la información, por favor verifique los datos y vuelva a intentarlo.");
            }

            return EmployeeDetailPartial(Id_Employee);
        }

      
        #endregion
        #region Autorizaciones Jefe Inmediato

        public ActionResult AuthorizeBoss()
        {
            List<Entities.ExtraTime> lstDetail = new List<Entities.ExtraTime>();
            try
            {


                var result = Data.ExtraTime.GetAllHoursAuthorizeBoss();
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ExtraTime>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstDetail);
        }
        public ActionResult AuthorizeBossPartial()
        {
            List<Entities.ExtraTime> lstDetail = new List<Entities.ExtraTime>();
            try
            {


                var result = Data.ExtraTime.GetAllHoursAuthorizeBoss();
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ExtraTime>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeBossPartial", lstDetail);



        }

        public JsonResult AuthorizeBossPartialjson()
        {
            List<Entities.ExtraTime> lstDetail = new List<Entities.ExtraTime>();
            try
            {
               
                
                var result = Data.ExtraTime.GetAllHoursAuthorizeBoss();
                if (result != null)
                {
                    lstDetail = result.ToList();
                 }

                else
                {
                    lstDetail = new List<Entities.ExtraTime>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return Json(new { data = lstDetail }, JsonRequestBehavior.AllowGet);



        }
        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes pero destruyendo la sesion.
        /// </summary>
        /// <returns></returns>
            public ActionResult RefreshPartial()
            {
                List<Entities.ExtraTime> lstDetail = new List<Entities.ExtraTime>();
                try
                {
                    Session.Remove("sAuthorizeBoss");

                    var result = Data.ExtraTime.GetAllHoursAuthorizeBoss();
                    if (result != null)
                    {
                        lstDetail = result.ToList();

                    }

                    else
                    {
                        lstDetail = new List<Entities.ExtraTime>();
                    }



                }
                catch (Exception ex)
                {
                    throw new Exception("Se ha producido el siguiente error ", ex);
                }
                return PartialView("AuthorizeBossPartial", lstDetail);

            }
        /// <summary>
        /// Metodo para autorizar un extraplan por el jefe inmediato
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeBoss(string ids)
        {
            string result = string.Empty;
            
            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.ExtraTime.ChangeStateBoss(int.Parse(keyAuthorize), 2, string.Empty);


                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }
        /// <summary>
        /// Metodo para denegar un extra plan por el jefe inmediato
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedBoss(string ids)
        {
            string result = string.Empty;
            
            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));

                   Data.ExtraTime.ChangeStateBoss(int.Parse(keyAuthorize), -2, string.Empty);
                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }

        #endregion
        #region Autorizaciones Gerente
        public ActionResult AuthorizeManager()
        {
            List<Entities.ExtraTime> lstDetail = new List<Entities.ExtraTime>();
            try
            {


                var result = Data.ExtraTime.GetAllHoursAuthorizeManager("2");
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ExtraTime>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstDetail);
        }

        public ActionResult AuthorizeManagerPartial()
        {
            List<Entities.ExtraTime> lstDetail = new List<Entities.ExtraTime>();
            try
            {


                var result = Data.ExtraTime.GetAllHoursAuthorizeManager("2");
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ExtraTime>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeManagerPartial", lstDetail);



        }
        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes pero destruyendo la sesion.
        /// </summary>
        /// <returns></returns>
        public ActionResult RefreshPartialManager()
        {
            List<Entities.ExtraTime> lstDetail = new List<Entities.ExtraTime>();
            try
            {
                Session.Remove("sAuthorizeManager");

                var result = Data.ExtraTime.GetAllHoursAuthorizeBoss();
                if (result != null)
                {
                    lstDetail = result.ToList();

                }

                else
                {
                    lstDetail = new List<Entities.ExtraTime>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeManagerPartial", lstDetail);

        }
        /// <summary>
        /// Metodo para autorizar horas extras por el gerente
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeManagement(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.ExtraTime.ChangeStateManager(int.Parse(keyAuthorize), 3, string.Empty);


                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }
        /// <summary>
        /// Metodo para denegar horas extras por el gerente
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedManagement(string ids)
        {
            string result = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));

                    Data.ExtraTime.ChangeStateManager(int.Parse(keyAuthorize), -3, string.Empty);
                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }

       
        #endregion
        #region Chart

        public ViewResult ParametersChart()
        {

            return View();
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult ExtraTimeChart(Entities.MyEntities.ChartParameters eParameter)
        {
            Validations.ExtraTime vExtraTime = new Validations.ExtraTime();
            bool resUserActive = vExtraTime.ValidateUserActive();
            Session.Remove("sParameter");
            Session["sParameter"] = eParameter;

            if (ModelState.IsValid)
            {
                try
                {
                    if (resUserActive == true)
                    {

                        return RedirectToAction("Chart");

                    }
                    else
                    {
                        ViewData["EditError"] = "Usuario sin autorizacion de visualizar el reporte";
                    }
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
                ViewData["EditError"] = "Error en la operacion";

            return View("ParametersChart");
        }

        public ActionResult Chart()
        {

            Validations.ExtraTime vExtraTime = new Validations.ExtraTime();
            bool resUserActive = vExtraTime.ValidateUserActive();
            List<Entities.ExtratimeManagnment> lstExtratimeHistoric = new List<Entities.ExtratimeManagnment>();
            if (resUserActive == true)
            {

                lstExtratimeHistoric = Models.ExtraTime.GetExtraTimeHistoric();

            }
            else
            {
                return View("ExtraTimeChart", lstExtratimeHistoric);
            }



            return View("ExtraTimeChart", lstExtratimeHistoric);
        }
        #endregion
        #region Reports
        public ActionResult Consoliadohorasextratodo() => View();   // Consolidado

        public ActionResult ConsultaHorasExtras() => View();   // Consolidado
        public ActionResult DetalleHorasExtras() => View();   // Detalle
        [HttpGet]
        public JsonResult ObtenerResumenHorasconsolidado()
        {
            var lista = ExtraTime.GetEmployeesConsult()
                .Select(e => new { e.AreaName, e.TotalHours })
                .ToList();
            return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult ObtenerResumenHoras( )
        {
            var lista = ExtraTime.GetEmployeesConsult()
            .Select(e => new {
                e.AreaName,
                e.TotalHours,
                e.TotalHoursBoss,
                e.TotalHoursManager,
                e.TotalHoursRrhh
            }).ToList();

            return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerDetalleHoras()                       // sin parámetro
        {
            var detalle = ExtraTime.GetAllDetailExtraTime();    // ya filtra por Session
            return Json(new { data = detalle }, JsonRequestBehavior.AllowGet);
        }

        // ==== EXPORTS ========================================================
        public FileResult ExportarPdf() =>
            GenerarArchivo("pdf", "application/pdf", "ResumenHoras.pdf");

        public FileResult ExportarXls() =>
            GenerarArchivo("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                           "ResumenHoras.xlsx");

        // util privado
        private FileResult GenerarArchivo(string ext, string mime, string nombre)
        {
            byte[] bytes = System.IO.File.ReadAllBytes($@"C:\Temp\ResumenHoras.{ext}");
            return File(bytes, mime, nombre);
        }
        public ViewResult ParametersReport()
        {

            Session.Remove("sParameter");
            return View();
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult ExtraTimeReport(Entities.MyEntities.Parameters eParameter)
        {
            Validations.ExtraTime vExtraTime = new Validations.ExtraTime();
            bool resUserActive = vExtraTime.ValidateUserActive();

            Session["sParameter"] = eParameter;
          var  startDate = eParameter.StartDate.ToShortDateString();
        var    endDate = eParameter.EndDate.ToShortDateString();
            if (ModelState.IsValid)
            {
                try
                { 
                    if (resUserActive == true)
                    {
                        if (eParameter.ViewType == "CONSOLIDADO")
                        {
                            //return RedirectToAction("Consoliadohorasextratodo");
                           return RedirectToAction("SummaryReport");

                        }
                        else
                        {
                            //return RedirectToAction("DetalleHorasExtras");
                          return RedirectToAction("DetailReport");
                        }


                    }
                    else
                    {
                        ViewData["EditError"] = "Usuario sin autorizacion de visualizar el reporte";
                    }
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
                ViewData["EditError"] = "Error en la operacion";

            return View("ParametersReport");
        }
        // V2: en lugar de ir a SummaryReport/DetailReport, llama directo a HoraExtraTodo3 / HoraExtraTodo4 (PDF)
        [HttpPost, ValidateInput(false)]
        public ActionResult ExtraTimeReport2(Entities.MyEntities.Parameters eParameter)
        {
            // Validar entrada
            if (eParameter == null)
            {
                ViewData["EditError"] = "Parámetros requeridos.";
                return View("ParametersReport");
            }

            // Guardar parámetros en sesión (por si se necesitan en otra lógica)
            Session["sParameter"] = eParameter;

            if (!ModelState.IsValid)
            {
                ViewData["EditError"] = "Error en la operación.";
                return View("ParametersReport");
            }

            try
            {
                // Validar usuario autorizado
                var vExtraTime = new Validations.ExtraTime();
                var usuarioActivo = vExtraTime.ValidateUserActive();
                if (!usuarioActivo)
                {
                    ViewData["EditError"] = "Usuario sin autorización de visualizar el reporte.";
                    return View("ParametersReport");
                }

                // Validar que StartDate y EndDate estén en el mismo año-mes (un solo período)
                var periodoInicio = eParameter.StartDate.ToString("yyyy-MM");
                var periodoFin = eParameter.EndDate.ToString("yyyy-MM");

                if (!string.Equals(periodoInicio, periodoFin, StringComparison.Ordinal))
                {
                    ViewData["EditError"] = "Debe seleccionar un solo período (mismo año y mes).";
                    return View("ParametersReport");
                }

                var periodId = periodoInicio; // "YYYY-MM" que consumen horaextratodo3/4
                var viewType = (eParameter.ViewType ?? string.Empty).Trim().ToUpperInvariant();

                // CONSOLIDADO → usa acción que genera PDF consolidado
                if (viewType == "CONSOLIDADO")
                {
                    // HoraExtraTodo3(id) ya retorna File(PDF) o mensaje de texto
                    return horaextratodo3(periodId);
                }

                // DETALLE (cualquier otro valor) → usa acción que genera PDF detalle
                return horaextratodo4(periodId);
            }
            catch (Exception ex)
            {
                ViewData["EditError"] = ex.Message;
                return View("ParametersReport");
            }
        }

        //Accion que llama a la vista de consolidado de reporte de horas extras
        public ActionResult SummaryReport()
        {

            return View();
        }
        //Accion que llama a la vista de detalle de reporte de horas extras
        public ActionResult DetailReport()
        {

            return View();
        }
        //Accion que llama a la vista parcial de consolidado de reporte de horas extras
        public ActionResult SummaryReportPartial()
        {
            string startDate, endDate;
            Entities.MyEntities.Parameters eParameter = new Entities.MyEntities.Parameters();
            eParameter = (Entities.MyEntities.Parameters)Session["sParameter"];

            SummaryReport reportes = new SummaryReport();

            startDate = eParameter.StartDate.ToShortDateString();
            endDate = eParameter.EndDate.ToShortDateString();


            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            List<Entities.MyEntities.ExtraTimeReport> model = new List<Entities.MyEntities.ExtraTimeReport>();

            try
            {
                //if (eParameter.AreaId == 0)
                //{
                //    model = Models.ExtraTime.GetListExtraTimeReport(eEmployee.Idhrms, startDate, endDate);
                //}
                //else
                //{
                //    model = Models.ExtraTime.GetListExtraTimeReport(eEmployee.Idhrms, startDate, endDate).Where(X => X.AreaId == eParameter.AreaId).ToList();

                //}

                //if (model.Count==0)
                //{

                 
                    if (eParameter.AreaId == 0)
                    {
                        model = Models.ExtraTime.GetDetailExtraTimeReport(eEmployee.Idhrms, startDate, endDate);
                    }
                    else
                    {
                        model = Models.ExtraTime.GetDetailExtraTimeReport(eEmployee.Idhrms, startDate, endDate).Where(X => X.AreaId == eParameter.AreaId).ToList();

                    }
                //}
                if (model.Count>0)
                {
                    // Agrupa por los campos indicados y suma las horas
                    model = model
         .GroupBy(r => new
         {
             r.OrganizationId,
             r.OrganizationName,
             r.PeriodId,
             r.EmployeeNumber,
             r.FullName,
             r.AreaId,
             r.Location
         })
         .Select(g => new Entities.MyEntities.ExtraTimeReport
         {
             OrganizationId = g.Key.OrganizationId,
             OrganizationName = g.Key.OrganizationName,
             PeriodId = g.Key.PeriodId,
             EmployeeNumber = g.Key.EmployeeNumber,
             FullName = g.Key.FullName,
             AreaId = g.Key.AreaId,
             Location = g.Key.Location,
             TotalHours = g.Sum(x => x.TotalHours) // ← esta propiedad debe existir en tu clase
    })
         .ToList();


                }
                //var modelo = ObtenerModeloDesdeTuLógica(); // <-- ya lo tienes como 'Model' en la vista
                reportes.DataSource = model;


                reportes.CreateDocument();                  // fuerza la generación del documento
                Session["SummaryReportDoc"] = reportes;

                return PartialView("SummaryReportPartial", model);

            }
            catch (Exception e)
            {

                ViewData["EditError"] = e.Message;
            }
          
            return PartialView("SummaryReportPartial", model);
        }

        //Accion que llama a la vista parcial de detalle de reporte de horas extras
        public ActionResult DetailReportPartial()
        {
            string startDate, endDate;
            Entities.MyEntities.Parameters eParameter = new Entities.MyEntities.Parameters();
            eParameter = (Entities.MyEntities.Parameters)Session["sParameter"];


            startDate = eParameter.StartDate.ToShortDateString();
            endDate = eParameter.EndDate.ToShortDateString();


            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            List<Entities.MyEntities.ExtraTimeReport> model = new List<Entities.MyEntities.ExtraTimeReport>();

            try
            {
                if (eParameter.AreaId == 0)
                {
                    model = Models.ExtraTime.GetDetailExtraTimeReport(eEmployee.Idhrms, startDate, endDate);
                }
                else
                {
                    model = Models.ExtraTime.GetDetailExtraTimeReport(eEmployee.Idhrms, startDate, endDate).Where(X => X.AreaId == eParameter.AreaId).ToList();

                }


                return PartialView("DetailReportPartial", model);

            }
            catch (Exception e)
            {

                ViewData["EditError"] = e.Message;
            }

            return PartialView("DetailReportPartial", model);
        }

        //Acción para exportar reporte de consolidado de horas extras
        public ActionResult SummaryReportExport()
        {
            string startDate, endDate;
            Entities.MyEntities.Parameters eParameter = new Entities.MyEntities.Parameters();
            eParameter = (Entities.MyEntities.Parameters)Session["sParameter"];


            startDate = eParameter.StartDate.ToShortDateString();
            endDate = eParameter.EndDate.ToShortDateString();


            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            SummaryReport reporte = new SummaryReport();//Objeto de instancia del reporte

            //if (eParameter.AreaId == 0)
            //{
            //    report.DataSource = Models.ExtraTime.GetListExtraTimeReport(eEmployee.Idhrms, startDate, endDate);
            //}
            //else
            //{
            //    report.DataSource = Models.ExtraTime.GetListExtraTimeReport(eEmployee.Idhrms, startDate, endDate).Where(X => X.AreaId == eParameter.AreaId).ToList();

            //}
            reporte = (SummaryReport)Session["SummaryReportDoc"];

            if (reporte == null)
            {
                // Fallback por si expiró la sesión: reconstruir desde el mismo origen en memoria
                //var modelo = ObtenerModeloDesdeTuLógica();
                //reporte = new SummaryReport { DataSource = modelo };
                //reporte.CreateDocument();
                //Session["SummaryReportDoc"] = reporte;
            }

            // Exporta exactamente lo que el usuario ve (mismo documento ya generado)
            //return DocumentViewerExtension.ExportTo(reporte);
            return ReportViewerExtension.ExportTo(reporte);



        }

        //Acción para exportar reporte de detalle de horas extras
        public ActionResult DetailReportExport()
        {
            string startDate, endDate;
            Entities.MyEntities.Parameters eParameter = new Entities.MyEntities.Parameters();
            eParameter = (Entities.MyEntities.Parameters)Session["sParameter"];


            startDate = eParameter.StartDate.ToShortDateString();
            endDate = eParameter.EndDate.ToShortDateString();


            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            ExtraTimeDetail detailReport = new ExtraTimeDetail();//Objeto de instancia del reporte

            if (eParameter.AreaId == 0)
            {
                detailReport.DataSource = Models.ExtraTime.GetDetailExtraTimeReport(eEmployee.Idhrms, startDate, endDate);
            }
            else
            {
                detailReport.DataSource = Models.ExtraTime.GetDetailExtraTimeReport(eEmployee.Idhrms, startDate, endDate).Where(X => X.AreaId == eParameter.AreaId).ToList();

            }
            return ReportViewerExtension.ExportTo(detailReport);

        }



        #endregion
        #region Consult

        /*****************************************************************************************/
        /*Parameters of date*/
        /*****************************************************************************************/
        //[Authorize]
        public ViewResult ParametersConsult()
        {

            return View();
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult ExtraTimeConsult(Entities.MyEntities.Parameters eParameter)
        {

            Validations.ExtraTime vExtraTime = new Validations.ExtraTime();
            bool resUserActive = vExtraTime.ValidateUserActive();
            Session.Remove("sParameter");
            Session["sParameter"] = eParameter;

            if (ModelState.IsValid)
            {
                try
                {
                    if (resUserActive == true)
                    {
                        if (eParameter.ViewType == "CONSOLIDADO")
                        {
                            return RedirectToAction("ExtraHoursSummary");

                        }
                        else
                        {
                            return RedirectToAction("ExtraHoursConsultDetail");
                        }
                    }
                    else
                    {
                        ViewData["EditError"] = "Usuario sin autorizacion de visualizar la consulta";
                    }
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
                ViewData["EditError"] = "Error en la operacion";

            return View("ParametersConsult");

        }
        /*****************************************************************************************/
        /*ExtraTime Summary Binding*/
        /*****************************************************************************************/
        public ActionResult ExtraHoursSummary()
        {
            return View();
        }

        public ActionResult ExtraHoursSummaryPartial()
        {

            GridViewModel viewModel = GridViewExtension.GetViewModel(summaryViewModel);
            if (viewModel == null)
                viewModel = CreateGridViewModelWithSummary();


            return SummaryBindingCore(viewModel);
        }

        PartialViewResult SummaryBindingCore(GridViewModel viewModel)
        {
            viewModel.ProcessCustomBinding
                (
                Models.ExtraTime.GetDataRowCount,
                Models.ExtraTime.GetData,
                Models.ExtraTime.GetSummaryValues
                );
            return PartialView("ExtraHoursSummaryPartial", viewModel);
        }
        //Paginación gridview registrar
        public ActionResult GridPagingAction(GridViewPagerState pager)
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(summaryViewModel);
            viewModel.ApplyPagingState(pager);
            return SummaryBindingCore(viewModel);
        }
        //Filtro
        public ActionResult GridFilteringAction(GridViewFilteringState filter)
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(summaryViewModel);
            viewModel.ApplyFilteringState(filter);
            return SummaryBindingCore(viewModel);
        }

        static GridViewModel CreateGridViewModelWithSummary()
        {
            GridViewModel viewModel = new GridViewModel();
            viewModel.KeyFieldName = "Idhrms";
            viewModel.Columns.Add("EmployeeNumber");
            viewModel.Columns.Add("Names");
            viewModel.Columns.Add("LastNames");
            viewModel.Columns.Add("FullName");
            viewModel.Columns.Add("Location");
            viewModel.Columns.Add("TotalHours");
            viewModel.Columns.Add("TotalHoursBoss");
            viewModel.Columns.Add("TotalHoursManager");
            viewModel.Columns.Add("TotalHoursRrhh ");

            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "EmployeeNumber", SummaryType = SummaryItemType.Count });
            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "TotalHours", SummaryType = SummaryItemType.Sum });
            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "TotalHoursBoss", SummaryType = SummaryItemType.Sum });
            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "TotalHoursManager", SummaryType = SummaryItemType.Sum });
            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "TotalHoursRrhh", SummaryType = SummaryItemType.Sum });

            viewModel.Pager.PageSize = 10;
            return viewModel;
        }
        /*****************************************************************************************/
        /*ExtraTime  Detail Summary Binding*/
        /*****************************************************************************************/

        public ActionResult ExtraHoursDetailPartial(int customerID)
        {
            var viewModel = GridViewExtension.GetViewModel("gvDetail" + customerID);
            if (viewModel == null)
                viewModel = CreateDetailGridViewModel(customerID);
            return DetailBindingCore(viewModel, customerID);
        }


        //Binding del gridview de detalle de consulta de horas extras

        public ActionResult DetailBindingCore(GridViewModel gridViewModel, int customerID)
        {
            try
            {
                Session.Remove("PersonId");
                Session["PersonId"] = customerID;
                gridViewModel.ProcessCustomBinding(
               Models.ExtraTime.DetailGetDataRowCount,
               Models.ExtraTime.DetailGetData,
               Models.ExtraTime.DetailGetSummaryValues
           );
                ViewData["CustomerID"] = customerID;
                return PartialView("ExtraHoursDetailPartial", gridViewModel);
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }
            return PartialView("ExtraHoursDetailPartial", gridViewModel);
        }

        //Paginacion gridview detalle de consulta de horas extras
        public ActionResult DetailGridViewPagingAction(GridViewPagerState pager, int customerID)
        {   
            var viewModel = GridViewExtension.GetViewModel("gvDetail" + customerID);
            viewModel.Pager.Assign(pager);
            return DetailBindingCore(viewModel, customerID);
        }

        //Ordenamiento gridview detalle de consulta de horas extras
        public ActionResult DetailGridViewSortingAction(GridViewColumnState column, bool reset, int customerID)
        {
            var viewModel = GridViewExtension.GetViewModel("gvDetail" + customerID);
            viewModel.SortBy(column, reset);
            return DetailBindingCore(viewModel, customerID);
        }
        public JsonResult horaextratodo2( string id)
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            
             
            var apiUrl = "http://172.26.54.66/apihcm/api/empleado/gethoraextratodo?fecha=" + id;

            try
            {
                var client = new RestClient(apiUrl);
                var request = new RestRequest(Method.GET) { Timeout = 30000 }; // 10s

                var resp = client.Execute(request);
                if (resp == null || string.IsNullOrWhiteSpace(resp.Content))
                    return Json(new { data = new List<Entities.Nominaexcel>() }, JsonRequestBehavior.AllowGet);

                // servidor puede retornar "SIN RESULTADO" literal
                var raw = resp.Content.Trim().Trim('"');
                if (string.Equals(raw, "SIN RESULTADO", StringComparison.OrdinalIgnoreCase))
                    return Json(new { data = new List<Entities.Nominaexcel>() }, JsonRequestBehavior.AllowGet);

                // deserialización FUERTEMENTE TIPADA
                var settings = new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal // respeta decimales en HOURS
                };

                var lista = JsonConvert.DeserializeObject<List<Entities.Nominaexcel>>(resp.Content, settings);
                if (lista == null)
                    lista = new List<Entities.Nominaexcel>();

                // Opcional: normaliza ESTATUS a texto legible (si lo necesitas en el grid)
                for (int i = 0; i < lista.Count; i++)
                {
                    var s = lista[i].STATUS;
                    if (s == "1") lista[i].STATUS = "Registrado";
                    else if (s == "2") lista[i].STATUS = "Autorizado por jefe inmediato";
                    else if (s == "3") lista[i].STATUS = "Autorizado por Gerente";
                    else if (s == "4") lista[i].STATUS = "Autorizado por Recursos Humanos";
                }
                string gerenciasPermitidas;

                if (eEmployee.EmployeeNumber == "401204")
                {
                    // Las 4 gerencias sin el prefijo "NI "
                    gerenciasPermitidas = string.Join(",",
                        new[]
                        {
            "GERENCIA OPERACIONES PLANTA INTERNA",
            "GERENCIA DE IMPLANTACION",
            "GERENCIA TECNICA",
            "GERENCIA OPERACIONES PLANTA EXTERNA"
                        });
                    lista = lista.Where(x =>
                            !string.IsNullOrEmpty(x.GERENCIA) &&
                            gerenciasPermitidas.Contains(x.GERENCIA.Replace("NI ", "").Trim())
                        ).ToList();
                }
                else if (eEmployee.GERENCIA.Contains("RECURSOS")==true)
                {
                    return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    List<Entities.Employees> lstEmployees = Data.Employee.GetEmployeesByBossToExpenses();
   var projectedEmployees = lstEmployees  .Select(e => e.EmployeeNumber);
                    lista = lista
                        .Where(u => projectedEmployees.Contains(u.EMPLOYEE_NUMBER))
                        .ToList();
                }

                // retorno estándar para DataTables: { data: [...] }
                return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                // falla segura: lista vacía
                return Json(new { data = new List<Entities.Nominaexcel>() }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult horaextratodo(string id)
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetBoardStats2025()
        {
            try
            {
                var lista = ObtenerDataApiFiltrada();

                // Contamos basándonos en el STATUS de la API
                int countJefe = lista.Count(x => x.STATUS.Contains("GRABADO"));
                int countGerente = lista.Count(x => x.STATUS.Contains("APROBADO") == true); 
               
                return Json(new { status = "OK", jefe = countJefe, gerente = countGerente }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "Error", jefe = 0, gerente = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
 
        public ActionResult HoraExtraTodo5Excel(string id)
        {
            var eEmployee = Session["User"] as Entities.Employees;
            var apiUrl = "http://172.26.54.66/apihcm/api/empleado/gethoraextratodo2?fecha=" + id;

            try
            {
                var client = new RestClient(apiUrl);
                var request = new RestRequest(Method.GET) { Timeout = 30000 };
                var resp = client.Execute(request);

                // si no hay datos → Excel vacío con headers mínimos
                if (resp == null || string.IsNullOrWhiteSpace(resp.Content) ||
                    string.Equals(resp.Content.Trim().Trim('"'), "SIN RESULTADO", StringComparison.OrdinalIgnoreCase))
                {
                    using (var wb = new XLWorkbook())
                    {
                        wb.Worksheets.Add("HorasExtra_" + id).Cell(1, 1).Value = "SIN DATOS";
                        using (var ms = new MemoryStream())
                        {
                            wb.SaveAs(ms);
                            return File(ms.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "HorasExtra_" + id + ".xlsx");
                        }
                    }
                }

                var settings = new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal };
                var lista = JsonConvert.DeserializeObject<List<Entities.Nominaexcel2>>(resp.Content, settings)
                           ?? new List<Entities.Nominaexcel2>();

                // normaliza STATUS
                for (int i = 0; i < lista.Count; i++)
                {
                    var s = lista[i].STATUS;
                    if (s == "1") lista[i].STATUS = "Registrado";
                    else if (s == "2") lista[i].STATUS = "Autorizado por jefe inmediato";
                    else if (s == "3") lista[i].STATUS = "Autorizado por Gerente";
                    else if (s == "4") lista[i].STATUS = "Autorizado por Recursos Humanos";
                }

                // filtros (⚠️ NO retornar JSON aquí; solo ajustar lista y seguir a Excel)
                if (eEmployee != null && eEmployee.EmployeeNumber == "401204")
                {
                    var permitidas = new[]
                    {
                "GERENCIA OPERACIONES PLANTA INTERNA",
                "GERENCIA DE IMPLANTACION",
                "GERENCIA TECNICA",
                "GERENCIA OPERACIONES PLANTA EXTERNA"
            };
                    lista = lista.Where(x =>
                        !string.IsNullOrEmpty(x.Gerencia) &&
                        permitidas.Contains(x.Gerencia.Replace("NI ", "").Trim())
                    ).ToList();
                }
                else if (eEmployee != null && !string.IsNullOrEmpty(eEmployee.GERENCIA) &&
                         eEmployee.GERENCIA.Contains("RECURSOS"))
                {
                    // RH ve todo → no filtrar
                }
                else
                {
                    var lst = Data.Employee.GetEmployeesByBossToExpenses();
                    var set = new HashSet<string>(lst.Select(e => e.EmployeeNumber));
                    lista = lista.Where(u => set.Contains(u.NoEmpleado)).ToList();
                }

                // Excel
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("HorasExtra_" + id);
                    int c = 1;
                    ws.Cell(1, c++).Value = "Nombre";

                    ws.Cell(1, c++).Value = "NoEmpleado";
                    ws.Cell(1, c++).Value = "Gerencia";

                    ws.Cell(1, c++).Value = "Area";

                    ws.Cell(1, c++).Value = "Fecha";
                    ws.Cell(1, c++).Value = "Estatus";
                    ws.Cell(1, c++).Value = "Hora_Ini_Reg";
                    ws.Cell(1, c++).Value = "Hora_Fin_Reg";
                    ws.Cell(1, c++).Value = "Hora Registrada";
                    ws.Cell(1, c++).Value = "Hora_Ini_Aut";
                    ws.Cell(1, c++).Value = "Hora_Fin_Aut";
                    ws.Cell(1, c++).Value = "Hora autorizada";

                    int r = 2;
                    foreach (var x in lista)
                    {
                        c = 1;
                         ws.Cell(r, c++).Value = x.NombreCompleto;
                        ws.Cell(r, c++).Value = x.NoEmpleado ;
                        ws.Cell(r, c++).Value = (x.Gerencia ?? "").Replace("NI ", "").Trim();
                        ws.Cell(r, c++).Value = x.AREA;

                        ws.Cell(r, c++).Value = x.DATE_EXTRATIME;
                        ws.Cell(r, c++).Value = x.STATUS;
                        ws.Cell(r, c++).Value = x.Hora_Ini_Reg;
                        ws.Cell(r, c++).Value = x.Hora_Fin_Reg;
                        ws.Cell(r, c++).Value = x.HoursReg;
                        ws.Cell(r, c++).Value = x.Hora_Ini_Aut;
                        ws.Cell(r, c++).Value = x.Hora_Fin_Aut;
                        ws.Cell(r, c++).Value = x.HoursAutoEntero;
                        r++;
                    }

                    ws.Range(1, 1, 1, 13).Style.Font.Bold = true;
                    ws.Columns().AdjustToContents();

                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        return File(ms.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "HorasExtra_" + id + ".xlsx");
                    }
                }
            }
            catch
            {
                using (var wb = new XLWorkbook())
                {
                    wb.Worksheets.Add("HorasExtra_" + id).Cell(1, 1).Value = "ERROR";
                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        return File(ms.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "HorasExtra_" + id + ".xlsx");
                    }
                }
            }
        }

        public ActionResult horaextratodo3(string id)
    {
        Entities.Employees eEmployee = null;

        if (Session["User"] != null)
        {
            eEmployee = (Entities.Employees)Session["User"];
        }

        string apiUrl = $"http://172.26.54.66/apihcm/api/horaextratodo/GetAlltodohora?token=021092&bossId={eEmployee.EmployeeNumber}&id={id}";
                          //http://172.26.54.66/apihcm/api/horaextratodo/GetAlltodohora?token=021092&bossId=400913&id=2025-08

            var client = new RestClient(apiUrl);
        var request = new RestRequest(Method.GET);
        request.Timeout = 60000;

        var response = client.Execute(request);

        if (response != null && !string.IsNullOrEmpty(response.Content))
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            string cleanJson = response.Content.Replace(".0", "");
            if (cleanJson.Trim().ToUpper() == "SIN RESULTADO")
                return Content("No hay datos para exportar.", "text/plain");

            var items = serializer.Deserialize<List<Dictionary<string, object>>>(response.Content);
            var period = id;
            
                var list = items.Select(item => new Entities.MyEntities.ExtraTimeReport
                {
                    OrganizationId = 0,
                    OrganizationName = item.ContainsKey("GERENCIA") ? item["GERENCIA"]?.ToString() : "",
                    PeriodId = period,
                    EmployeeNumber = item.ContainsKey("EMPLOYEE_NUMBER") ? item["EMPLOYEE_NUMBER"]?.ToString() : "",
                    FullName = item.ContainsKey("FULLNAME") ? item["FULLNAME"]?.ToString() : "",
                    AreaId = 0,
                    Location = item.ContainsKey("AREA") ? item["AREA"]?.ToString() : "",
                    TotalHours = item.ContainsKey("HOURS") ? Convert.ToDouble(item["HOURS"]) : 0,
                    ExecutionDate = item.ContainsKey("DATE_EXTRATIME") && item["DATE_EXTRATIME"] != null
                        ? Convert.ToDateTime(item["DATE_EXTRATIME"]) : DateTime.MinValue,
                    StartHour = item.ContainsKey("HOUR_START") ? item["HOUR_START"]?.ToString() : "",
                    EndHour = item.ContainsKey("HOUR_END") ? item["HOUR_END"]?.ToString() : "",
                    StatusName = item.ContainsKey("STATUS") ? item["STATUS"]?.ToString() : ""
                }).ToList();
                list = list
     .GroupBy(r => new
     {
         r.OrganizationId,
         r.OrganizationName,
         r.PeriodId,
         r.EmployeeNumber,
         r.FullName,
         r.AreaId,
         r.Location
     })
     .Select(g => new Entities.MyEntities.ExtraTimeReport
     {
         OrganizationId = g.Key.OrganizationId,
         OrganizationName = g.Key.OrganizationName,
         PeriodId = g.Key.PeriodId,
         EmployeeNumber = g.Key.EmployeeNumber,
         FullName = g.Key.FullName,
         AreaId = g.Key.AreaId,
         Location = g.Key.Location,
         TotalHours = g.Sum(x => x.TotalHours) // ← esta propiedad debe existir en tu clase
         })
     .ToList();
                string gerencia = "";
                gerencia = eEmployee.GERENCIA.Replace("NI GERENCIA", "GERENCIA");
                list = list.Where(x => x.OrganizationName.Contains(gerencia)).ToList();
                var report = new SummaryReport(); // Tu clase de reporte, cambia el namespace si aplica
            report.DataSource = list;
            report.CreateDocument();

            // Exporta a PDF
            using (var ms = new MemoryStream())
            {
                report.ExportToPdf(ms); // Usa ExportToXlsx para Excel si lo prefieres
                ms.Position = 0;
                return File(ms.ToArray(), "application/pdf", "HorasExtra_" + period + ".pdf");
            }

          
        }
        return Content("No hay datos para exportar.", "text/plain");
    }

        public ActionResult horaextratodo4(string id)
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            string apiUrl = $"http://172.26.54.66/apihcm/api/horaextratodo/GetAlltodohora?token=021092&bossId={eEmployee.EmployeeNumber}&id={id}";

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = 60000;

            var response = client.Execute(request);

            if (response != null && !string.IsNullOrEmpty(response.Content))
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                string cleanJson = response.Content.Replace(".0", "");
                if (cleanJson.Trim().ToUpper() == "SIN RESULTADO")
                    return Content("No hay datos para exportar.", "text/plain");
                //"NI GERENCIA OPERACIONES PLANTA EXTERNA"
                var items = serializer.Deserialize<List<Dictionary<string, object>>>(response.Content);
                var period = id;

                var list = items.Select(item => new Entities.MyEntities.ExtraTimeReport
                {
                    OrganizationId = 0,
                    OrganizationName = item.ContainsKey("GERENCIA") ? item["GERENCIA"]?.ToString() : "",
                    PeriodId = period,
                    EmployeeNumber = item.ContainsKey("EMPLOYEE_NUMBER") ? item["EMPLOYEE_NUMBER"]?.ToString() : "",
                    FullName = item.ContainsKey("FULLNAME") ? item["FULLNAME"]?.ToString() : "",
                    AreaId = 0,
                    Location = item.ContainsKey("AREA") ? item["AREA"]?.ToString() : "",
                    TotalHours = item.ContainsKey("HOURS") ? Convert.ToDouble(item["HOURS"]) : 0,
                    TotalHours2 = item.ContainsKey("HOURS") ? Convert.ToDecimal(item["HOURS"]) : 0,
                    TotalHours3 = item.ContainsKey("HOURS") ?  (item["HOURS"]).ToString() : "0",
                    ExecutionDate = item.ContainsKey("DATE_EXTRATIME") && item["DATE_EXTRATIME"] != null
                        ? Convert.ToDateTime(item["DATE_EXTRATIME"]) : DateTime.MinValue,
                    StartHour = item.ContainsKey("HOUR_START") && item["HOUR_START"] != null
    ? Convert.ToDateTime(item["HOUR_START"]).ToString("HH:mm")
    : "",
                    EndHour = item.ContainsKey("HOUR_END") && item["HOUR_END"] != null
    ? Convert.ToDateTime(item["HOUR_END"]).ToString("HH:mm")
    : "",

                    StatusName = item.ContainsKey("STATUS") ? item["STATUS"]?.ToString() : ""
                }).ToList();
                string gerencia = "";
                gerencia = eEmployee.GERENCIA.Replace("NI GERENCIA", "GERENCIA");
                list = list.Where(x => x.OrganizationName.Contains(gerencia)).ToList();
                //var lista2= list.Where(x => x.EmployeeNumber.Contains("001213")).ToList();
                var report = new ExtraTimeDetail(); // Tu clase de reporte, cambia el namespace si aplica
                report.DataSource = list;
                report.CreateDocument();

                // Exporta a PDF
                using (var ms = new MemoryStream())
                {
                    report.ExportToPdf(ms); // Usa ExportToXlsx para Excel si lo prefieres
                    ms.Position = 0;
                    return File(ms.ToArray(), "application/pdf", "HorasExtra_" + period + ".pdf");
                }


            }
            return Content("No hay datos para exportar.", "text/plain");
        }
        //Binding de los campos del gridview de detalle de consultas
        static GridViewModel CreateDetailGridViewModel(int customerID)
        {
            var viewModel = new GridViewModel();

            viewModel.KeyFieldName = "Person_Id";
            viewModel.Columns.Add("ExecutionDate");
            viewModel.Columns.Add("HourStart");
            viewModel.Columns.Add("HourEnd");
            viewModel.Columns.Add("Hours");
            viewModel.Columns.Add("Reasons");
            viewModel.FilterExpression = (new BinaryOperator("Person_Id", customerID)).ToString();

            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "Hours", SummaryType = SummaryItemType.Sum });
            //viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "EmployeeID", SummaryType = SummaryItemType.Count });
            //viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "Freight", SummaryType = SummaryItemType.Average });
            //viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "ShipCity", SummaryType = SummaryItemType.Min });
            //viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "OrderDate", SummaryType = SummaryItemType.Min });
            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "ExecutionDate", SummaryType = SummaryItemType.Count });
            return viewModel;
        }
        /*****************************************************************************************/
        /*ExtraTime AllConsultDetail Binding*/
        /*****************************************************************************************/
        public ActionResult ExtraHoursConsultDetail()
        {
            return View();
        }

        public ActionResult ExtraHoursConsultDetailPartial()
        {

            GridViewModel viewModel = GridViewExtension.GetViewModel(detailViewModel);
            if (viewModel == null)
                viewModel = CreateExtraTimeConsultModelWithSummary();


            return SummaryExtraTimeConsultBindingCore(viewModel);
        }

        PartialViewResult SummaryExtraTimeConsultBindingCore(GridViewModel viewModel)
        {
            viewModel.ProcessCustomBinding
                (
                Models.ExtraTime.GetConsultDetailDataRowCount,
                Models.ExtraTime.GetConsultDetailData,
                Models.ExtraTime.GetConsultDetailValues
                );
            return PartialView("ExtraHoursConsultDetailPartial", viewModel);
        }
        //Paginación gridview registrar
        public ActionResult GridExtraTimeConsultPagingAction(GridViewPagerState pager)
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(detailViewModel);
            viewModel.ApplyPagingState(pager);
            return SummaryExtraTimeConsultBindingCore(viewModel);
        }
        //Filtro
        public ActionResult GridExtraTimeConsultFilteringAction(GridViewFilteringState filter)
        {
            GridViewModel viewModel = GridViewExtension.GetViewModel(detailViewModel);
            viewModel.ApplyFilteringState(filter);
            return SummaryExtraTimeConsultBindingCore(viewModel);
        }

        static GridViewModel CreateExtraTimeConsultModelWithSummary()
        {
            GridViewModel viewModel = new GridViewModel();
            viewModel.KeyFieldName = "PersonId";
            viewModel.Columns.Add("ExecutionDate");
            viewModel.Columns.Add("EmployeeNumber");
            viewModel.Columns.Add("EmployeeName");
            viewModel.Columns.Add("AreaName");
            viewModel.Columns.Add("Hours");
            viewModel.Columns.Add("Status");



            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "EmployeeNumber", SummaryType = SummaryItemType.Count });
            viewModel.TotalSummary.Add(new GridViewSummaryItemState() { FieldName = "Hours", SummaryType = SummaryItemType.Sum });


            viewModel.Pager.PageSize = 10;
            return viewModel;
        }

        //Exportacion a pdf del consolidado de la consulta
        public ActionResult ExportTo()
        {
            var model = ExtraTime.GetEmployeesConsult();

            return GridViewExtension.ExportToPdf(GridViewHelper.ExportGridViewSettings, model.ToList());
        }
        //Exportacion a xls del consolidado de la consulta
        public ActionResult ExportToXls()
        {
            var model = ExtraTime.GetEmployeesConsult();

            return GridViewExtension.ExportToXls(GridViewHelper.ExportGridViewSettings, model.ToList());
        }

        //Exportacion a xls del detalle de la consulta
        public ActionResult ExportDetailToXls()
        {
            var model = ExtraTime.GetAllDetailExtraTime();

            return GridViewExtension.ExportToXls(GridViewDetailHelper.ExportGridViewSettings, model.ToList());
        }


        public static class GridViewHelper
        {
            private static GridViewSettings exportGridViewSettings;

            private static GridViewSettings CreateExportGridViewSettings()
            {
                GridViewSettings settings = new GridViewSettings();
                settings.Name = "GridView";

                settings.CallbackRouteValues = new { Controller = "ExtraTime", Action = "ExtraHoursSummaryPartial" };

                settings.KeyFieldName = "Idhrms";

                settings.Columns.Add(col =>
                {
                    col.FieldName = "EmployeeNumber";
                    col.Caption = "# Carnet";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "FullName";
                    col.Caption = "Colaborador";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "Location";
                    col.Caption = "Area";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "TotalHours";
                    col.Caption = "Horas Registradas";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "TotalHoursBoss";
                    col.Caption = "Aut.Jefe";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "TotalHoursManager";
                    col.Caption = "Aut.Gerente";
                });

                settings.Columns.Add(col =>
                {
                    col.FieldName = "TotalHoursRrhh";
                    col.Caption = "Aut.RRHH";
                });
                settings.SettingsExport.FileName = "Consolidado";
                settings.SettingsExport.PaperKind = System.Drawing.Printing.PaperKind.Letter;
                settings.SettingsExport.Landscape = true;
                settings.SettingsExport.LeftMargin = 25;
                settings.SettingsExport.TopMargin = 25;
                settings.SettingsExport.RightMargin = 25;
                settings.SettingsExport.BottomMargin = 25;
                settings.SettingsExport.Styles.Cell.Font.Name = "Arial";
                settings.SettingsExport.Styles.Cell.Font.Size = 8;
                settings.SettingsExport.Styles.Header.Font.Name = "Arial";
                settings.SettingsExport.Styles.Title.Font.Name = "Arial";
                settings.SettingsExport.Styles.Default.Font.Name = "Arial";
                settings.SettingsPager.Visible = true;
                settings.Settings.ShowGroupPanel = true;

                return settings;
            }

            public static GridViewSettings ExportGridViewSettings
            {
                get
                {
                    if (exportGridViewSettings == null)
                        exportGridViewSettings = CreateExportGridViewSettings();
                    return exportGridViewSettings;
                }
            }
        }
        public static class GridViewDetailHelper
        {
            private static GridViewSettings exportGridViewSettings;

            private static GridViewSettings CreateExportGridViewSettings()
            {
                GridViewSettings settings = new GridViewSettings();
                settings.Name = "GridViewDetail";

                settings.CallbackRouteValues = new { Controller = "ExtraTime", Action = "ExtraHoursConsultDetailPartial" };

                settings.KeyFieldName = "PersonId";

                settings.Columns.Add(col =>
                {
                    col.FieldName = "EmployeeNumber";
                    col.Caption = "# Carnet";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "EmployeeName";
                    col.Caption = "Colaborador";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "AreaName";
                    col.Caption = "Area";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "ExecutionDate";
                    col.Caption = "Fecha";
                });
                settings.Columns.Add(col =>
                {
                    col.FieldName = "Hours";
                    col.Caption = "Total de Horas";
                });

                settings.SettingsExport.FileName = "Detalle de Horas Extras";
                settings.SettingsExport.PaperKind = System.Drawing.Printing.PaperKind.Letter;
                settings.SettingsExport.Landscape = true;
                settings.SettingsExport.LeftMargin = 25;
                settings.SettingsExport.TopMargin = 25;
                settings.SettingsExport.RightMargin = 25;
                settings.SettingsExport.BottomMargin = 25;
                settings.SettingsExport.Styles.Cell.Font.Name = "Arial";
                settings.SettingsExport.Styles.Cell.Font.Size = 8;
                settings.SettingsExport.Styles.Header.Font.Name = "Arial";
                settings.SettingsExport.Styles.Title.Font.Name = "Arial";
                settings.SettingsExport.Styles.Default.Font.Name = "Arial";
                settings.SettingsPager.Visible = true;
                settings.Settings.ShowGroupPanel = true;

                return settings;
            }

            public static GridViewSettings ExportGridViewSettings
            {
                get
                {
                    if (exportGridViewSettings == null)
                        exportGridViewSettings = CreateExportGridViewSettings();
                    return exportGridViewSettings;
                }
            }
        }

        #endregion
        #region  Soporte de Horas Extras
        /// <summary>
        /// Accion para subir el soporte de horas extras que llama a los metodos YieldFileUploadValidationSettings y  YieldFileUploadComplete
        /// </summary>
        /// <returns></returns>
        public ActionResult SupportUpload()
        {
            UploadControlExtension.GetUploadedFiles("ucExtraTimeSupport",
               slnRhonline.Data.ExtraTime.SupportUploadValidationSettings,
               slnRhonline.Data.ExtraTime.SupportUploadComplete);
            return null;
        }

        /// <summary>
        /// Metodo para cargar el soporte de horas extras
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult LoadSupport(int id)
        {
            try
            {
                
                    byte[] byteArray = Data.ExtraTime.GetExtraTimeById(id).FirstOrDefault().SupportFile;
                    if (byteArray == null)
                    {
                        return null;
                    }

                    var strBase64 = Convert.ToBase64String(byteArray);
                    Entities.ExtraTime extratime = new Entities.ExtraTime();
                    extratime.Notes = string.Format("data:application/pdf;base64,{0}", strBase64);
                    return PartialView("SupportPartial", extratime);
 
            }
            catch (Exception e)
            {
                throw new Exception("Error al cargar el objeto", e);
            }
        }
        #endregion





        // 3. PROCESAR ACCIONES (ProcessBatch2025)
        [HttpPost]
        public JsonResult ProcessBatch2025(string ids, int stage, string action)
        {
            try
            {
                string keysET = ids + ",";
                if (keysET == ",") return Json(new { status = "Error", message = "No hay selección" });

                while (keysET.Trim().Length > 0)
                {
                    string keyStr = keysET.Substring(0, keysET.IndexOf(","));
                    if (int.TryParse(keyStr, out int id))
                    {
                        // Stage 1 = Acciones de Jefe (Menu 43)
                        if (stage == 1)
                        {
                            if (action == "APROBAR") Data.ExtraTime.ChangeStateBoss(id, 2, string.Empty);
                            if (action == "DENEGAR") Data.ExtraTime.ChangeStateBoss(id, -2, string.Empty);
                        }
                        // Stage 2 = Acciones de Gerente (Menu 44)
                        else if (stage == 2)
                        {
                            if (action == "APROBAR") Data.ExtraTime.ChangeStateManager(id, 3, string.Empty);
                            if (action == "DENEGAR") Data.ExtraTime.ChangeStateManager(id, -3, string.Empty);
                        }
                    }
                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

                return Json(new { status = "OK", message = "Procesado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult ReporteHorasExtraPorArea()
        {
            return View(); // Views/ExtraTime/ReporteHorasExtraPorArea2026.cshtml
        }

        // =========================
        // 2) Cargar datos + tabla (AJAX)
        // =========================
        [HttpGet]
        public JsonResult CargarHorasExtraPeriodo2026(string id) // "yyyy-MM"
        {
            var eEmployee = GetEmpleadoSesion2026();
            if (eEmployee == null)
                return Json(new { ok = false, msg = "Sesión expirada." }, JsonRequestBehavior.AllowGet);

            id = (id ?? "").Trim();
            if (!EsPeriodoValido2026(id))
                return Json(new { ok = false, msg = "Período inválido. Use yyyy-MM." }, JsonRequestBehavior.AllowGet);

            // Trae detalle desde API + filtra por gerencia
            var detalle = ObtenerDetalleDesdeApi2026(eEmployee, id);
            //detalle = FiltrarPorGerencia2026(detalle, eEmployee);

            // Cache por período (para luego generar PDF sin volver a llamar API)
            Session[GetCacheKey2026(id)] = detalle;

            // Lista de áreas para el select (sale de los mismos resultados)
            var areas = detalle
                .Select(x => (x.Location ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            // Retorna datos + áreas
            return Json(new
            {
                ok = true,
                periodId = id,
                areas = areas,
                rows = detalle.Select(x => new
                {
                    x.OrganizationName,
                    x.EmployeeNumber,
                    x.FullName,
                    x.Location,
                    TotalHours = x.TotalHours,
                    ExecutionDate = (x.ExecutionDate == DateTime.MinValue) ? "" : x.ExecutionDate.ToString("yyyy-MM-dd"),
                    x.StartHour,
                    x.EndHour,
                    x.StatusName
                }).ToList()
            }, JsonRequestBehavior.AllowGet);
        }

        // =========================
        // 3) Generar PDF usando lo cargado (Session)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerarPdfHorasExtra2026(string id, string viewType, string area)
        {
            var eEmployee = GetEmpleadoSesion2026();
            if (eEmployee == null) return Content("Sesión expirada.", "text/plain");

            id = (id ?? "").Trim();
            if (!EsPeriodoValido2026(id)) return Content("Período inválido. Use yyyy-MM.", "text/plain");

            viewType = (viewType ?? "").Trim().ToUpperInvariant();
            area = (area ?? "").Trim(); // "" = Todas

            // 1) Toma de Session (lo que ya cargaste en la tabla)
            var cacheKey = GetCacheKey2026(id);
            var detalle = Session[cacheKey] as List<Entities.MyEntities.ExtraTimeReport>;

            // 2) Fallback: si no está en sesión, vuelve a consultar (por si el usuario recarga o expira sesión parcial)
            if (detalle == null)
            {
                detalle = ObtenerDetalleDesdeApi2026(eEmployee, id);
                //detalle = FiltrarPorGerencia2026(detalle, eEmployee);
                Session[cacheKey] = detalle;
            }

            if (detalle == null || detalle.Count == 0) return Content("No hay datos para exportar.", "text/plain");

            // 3) Filtra por área si aplica
            if (!string.IsNullOrWhiteSpace(area))
                detalle = detalle.Where(x => string.Equals((x.Location ?? "").Trim(), area, StringComparison.OrdinalIgnoreCase)).ToList();

            if (detalle.Count == 0) return Content("No hay datos para exportar.", "text/plain");

            // 4) Consolidado o detalle
            if (viewType == "CONSOLIDADO")
            {
                var consolidado = detalle
                    .GroupBy(r => new
                    {
                        r.OrganizationId,
                        r.OrganizationName,
                        r.PeriodId,
                        r.EmployeeNumber,
                        r.FullName,
                        r.AreaId,
                        r.Location
                    })
                    .Select(g => new Entities.MyEntities.ExtraTimeReport
                    {
                        OrganizationId = g.Key.OrganizationId,
                        OrganizationName = g.Key.OrganizationName,
                        PeriodId = g.Key.PeriodId,
                        EmployeeNumber = g.Key.EmployeeNumber,
                        FullName = g.Key.FullName,
                        AreaId = g.Key.AreaId,
                        Location = g.Key.Location,
                        TotalHours = g.Sum(x => x.TotalHours)
                    })
                    .ToList();

                var report = new SummaryReport();
                report.DataSource = consolidado;
                report.CreateDocument();
                return ExportarPdf2026(report, NombreArchivo2026(id, viewType, area));
            }
            else
            {
                var report = new ExtraTimeDetail();
                report.DataSource = detalle;
                report.CreateDocument();
                return ExportarPdf2026(report, NombreArchivo2026(id, viewType, area));
            }
        }

        // ===============================
        // Helpers 2026
        // ===============================
        private string GetCacheKey2026(string periodId)
        {
            return "HoraExtra2026_" + periodId; // clave por período
        }

        private Entities.Employees GetEmpleadoSesion2026()
        {
            return (Session["User"] as Entities.Employees);
        }

        private bool EsPeriodoValido2026(string periodo)
        {
            DateTime dt;
            return DateTime.TryParseExact(periodo + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
        }

        private List<Entities.MyEntities.ExtraTimeReport> ObtenerDetalleDesdeApi2026(Entities.Employees eEmployee, string periodId)
        {
            var apiUrl = string.Format(
                "http://172.26.54.66/apihcm/api/horaextratodo/GetAlltodohora?token=021092&bossId={0}&id={1}",
                eEmployee.EmployeeNumber,
                periodId
            );

            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = 60000;

            var response = client.Execute(request);
            if (response == null || string.IsNullOrWhiteSpace(response.Content))
                return new List<Entities.MyEntities.ExtraTimeReport>();

            var cleanJson = response.Content.Replace(".0", "");
            if (cleanJson.Trim().ToUpperInvariant() == "SIN RESULTADO")
                return new List<Entities.MyEntities.ExtraTimeReport>();

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var items = serializer.Deserialize<List<Dictionary<string, object>>>(cleanJson);

            var list = items.Select(item => new Entities.MyEntities.ExtraTimeReport
            {
                OrganizationId = 0,
                OrganizationName = item.ContainsKey("GERENCIA") ? (item["GERENCIA"] == null ? "" : item["GERENCIA"].ToString()) : "",
                PeriodId = periodId,
                EmployeeNumber = item.ContainsKey("EMPLOYEE_NUMBER") ? (item["EMPLOYEE_NUMBER"] == null ? "" : item["EMPLOYEE_NUMBER"].ToString()) : "",
                FullName = item.ContainsKey("FULLNAME") ? (item["FULLNAME"] == null ? "" : item["FULLNAME"].ToString()) : "",
                AreaId = 0,
                Location = item.ContainsKey("AREA") ? (item["AREA"] == null ? "" : item["AREA"].ToString()) : "",
                TotalHours = item.ContainsKey("HOURS") && item["HOURS"] != null ? Convert.ToDouble(item["HOURS"]) : 0,

                ExecutionDate = item.ContainsKey("DATE_EXTRATIME") && item["DATE_EXTRATIME"] != null
                    ? Convert.ToDateTime(item["DATE_EXTRATIME"])
                    : DateTime.MinValue,

                StartHour = item.ContainsKey("HOUR_START") && item["HOUR_START"] != null
                    ? Convert.ToDateTime(item["HOUR_START"]).ToString("HH:mm")
                    : "",

                EndHour = item.ContainsKey("HOUR_END") && item["HOUR_END"] != null
                    ? Convert.ToDateTime(item["HOUR_END"]).ToString("HH:mm")
                    : "",

                StatusName = item.ContainsKey("STATUS") ? (item["STATUS"] == null ? "" : item["STATUS"].ToString()) : ""
            }).ToList();

            return list;
        }
        public static string QuitarPrefijoNI(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            texto = texto.Trim();

            // Solo si empieza con "NI " (no reemplaza en medio)
            return texto.StartsWith("NI ", StringComparison.OrdinalIgnoreCase)
                ? texto.Substring(3).Trim()
                : texto;
        }

        private List<Entities.MyEntities.ExtraTimeReport> FiltrarPorGerencia2026(List<Entities.MyEntities.ExtraTimeReport> list, Entities.Employees eEmployee)
        {
            var area = (eEmployee.area ?? "").Trim();

            // "Toda" / vacío => NO filtra
            if (string.IsNullOrWhiteSpace(area) ||
                area.Equals("TODAS", StringComparison.OrdinalIgnoreCase) ||
                area.Equals("TODA", StringComparison.OrdinalIgnoreCase) ||
                area.Equals("TODO", StringComparison.OrdinalIgnoreCase))
                return list;

            // Filtra por Área (recomendado usar Location, no OrganizationName)
            return list.Where(x =>
                (x.Location ?? "").IndexOf(area, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }
        private ActionResult ExportarPdf2026(XtraReport report, string fileName)
        {
            using (var ms = new MemoryStream())
            {
                report.ExportToPdf(ms);   // <- como tu versión viejita
                ms.Position = 0;
                return File(ms.ToArray(), "application/pdf", fileName);
            }
        }

        //private ActionResult ExportarPdf2026(object reportDevExpress, string fileName)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        var mi = reportDevExpress.GetType().GetMethod("ExportToPdf", new[] { typeof(Stream) });
        //        if (mi == null) return Content("El reporte no soporta ExportToPdf.", "text/plain");

        //        mi.Invoke(reportDevExpress, new object[] { ms });
        //        ms.Position = 0;
        //        return File(ms.ToArray(), "application/pdf", fileName);
        //    }
        //}

        private string NombreArchivo2026(string periodId, string viewType, string area)
        {
            var areaTag = string.IsNullOrWhiteSpace(area) ? "Todas" : area.Replace(" ", "_");
            var tipo = (viewType == "CONSOLIDADO") ? "Consolidado" : "Detalle";
            return string.Format("HorasExtra_{0}_{1}_{2}_2026.pdf", periodId, tipo, areaTag);
        }

        #region Migración frmNominaHEAsignacionesMaestro
        
        public ActionResult AsignacionesMaestro()
        {
            return View();
        }

        //[HttpGet]
        //public JsonResult HE_AsignacionesPorPeriodos(string periodo)
        //{
        //    string apiUrl = $"http://172.26.54.66/apiclaroasemn/api/viatico/HE_AsignacionesPorPeriodos?periodo={periodo}";
        //    var client = new RestClient(apiUrl);
        //    var request = new RestRequest(Method.GET);
        //    var response = client.Execute(request);
        //    if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
        //    {
        //        return Content(response.Content, "application/json");
        //    }
        //    return Json(new { data = new List<object>() }, JsonRequestBehavior.AllowGet);
        //}

        //[HttpGet]
        //public JsonResult HE_AsignacionDetalleGerencia(string periodo, string gerencia)
        //{
        //    string apiUrl = $"http://172.26.54.66/apiclaroasemn/api/viatico/HE_AsignacionDetalleGerencia?Periodo={periodo}&Gerencia={gerencia}";
        //    var client = new RestClient(apiUrl);
        //    var request = new RestRequest(Method.GET);
        //    var response = client.Execute(request);
        //    if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
        //    {
        //        return Content(response.Content, "application/json");
        //    }
        //    return Json(new { data = new List<object>() }, JsonRequestBehavior.AllowGet);
        //}

        //[HttpGet]
        //public JsonResult HE_DetalleConsumoGerencia(string periodo, string gerencia)
        //{
        //    string apiUrl = $"http://172.26.54.66/apiclaroasemn/api/viatico/HE_DetalleConsumoGerencia?Periodo={periodo}&Gerencia={gerencia}";
        //    var client = new RestClient(apiUrl);
        //    var request = new RestRequest(Method.GET);
        //    var response = client.Execute(request);
        //    if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
        //    {
        //        return Content(response.Content, "application/json");
        //    }
        //    return Json(new { data = new List<object>() }, JsonRequestBehavior.AllowGet);
        //}

        #endregion
    }
}