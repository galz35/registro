using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities;
using DevExpress.DashboardWeb.Mvc;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using slnRhonline.basededatos;
 using System.Web.Script.Serialization;
using slnRhonline.Models;
using System.Data.SqlClient;
using Dapper;
using System.Data;
using RestSharp;

namespace slnRhonline.Controllers
{

    public class HomeController : Controller
    {
        public string strConnection1 = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIAF; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;";


        //Metodo nuevo para dash con char.js
        public ActionResult Index()
        {
            Console.WriteLine("This is C#");
            System.Diagnostics.Debug.WriteLine("This is text");
            Console.WriteLine("World");
            Entities.Employees temp = new Entities.Employees();
             temp = (Entities.Employees)Session["User"];
            Utils.Employee = temp;
            //var subManagementId = Utils.ClaroWCF.ExtraTimeGetOrganizationId(Utils.Employee.Id_HRMS);
            //string managementId =  Utils.ClaroWCF.GetManagementId(int.Parse(subManagementId));
            Session["subManagementId"] = temp.SUBGERENCIAID;
            Session["managementId"] = temp.GERENCIAID;
            
            if (Utils.Employee.RealUserLevel == 1 || Utils.Employee.EmployeeNumber == "772"  )
            {
                return View();
                //return View("Dash");
            }
            else if (temp.EmailAddress.Contains("com.ni")==true)
            {

            
                return View("MessaggeWelcome", Utils.Employee);
            }
            else
            {
                return View("MessaggeWelcomeNI", Utils.Employee);
            }




        }
        
             [HttpGet]
        public async Task<ActionResult> GetempleadolistAsync()
        {
            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();
            try
            {
                lstEmployees = await Data.Employee.ObtenerEmpleadosPorJefeAsync();
 
            }
            catch (Exception)
            {

                throw;
            }
            return Json(new { data = "" }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public async Task<ActionResult> GetManagementIdAsync()
        {//pasaria por empleado esto no es necesario
            Entities.Employees eEmployee = (Entities.Employees)Session["User"];
            if (eEmployee != null)
            {
                var subManagementId = Utils.ClaroWCF.ExtraTimeGetOrganizationId(eEmployee.Idhrms);
                Session["subManagementId"] = subManagementId;
                string managementId = await Utils.ClaroWCF.GetManagementIdAsync(int.Parse(subManagementId));
                Session["managementId"] = managementId;

                return Content(managementId);
            }
            return Content(string.Empty);
        }
        [HttpGet]
        public async Task<ActionResult> GetsolicitudpendienteAsync()
        {//pasaria por empleado esto no es necesario
            Entities.Employees eEmployee = (Entities.Employees)Session["User"];
            if (eEmployee != null)
            {
                 //return Content(managementId);
            }
            return Content(string.Empty);
        }
        private void _doback()
        {
            Thread.Sleep(500);
        }
        public JsonResult ObtenerFechasViaticosx()
        {
            List<Models.periodovt> lstEmployees = new List<Models.periodovt>();
            Entities.Employees eEmployee = (Entities.Employees)Session["User"];
       
            try
            {
                var client = new RestClient("http://172.26.54.66/apihcm/api/viatico/GetExpenseDetailViewByPersonId?personId=" + eEmployee.GERENCIAIDHRMS);
                var request = new RestRequest(Method.GET);
                request.Timeout = -1;


                var resultExpensesx = client.Execute(request);
                //Console.WriteLine(response.Content);
                //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                if (resultExpensesx != null)
                {
                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 500000000;

                    lstEmployees = serializer.Deserialize<List<Models.periodovt>>(resultExpensesx.Content);
                    return Json(resultExpensesx.Content, JsonRequestBehavior.AllowGet);

                }

            }
            catch (Exception e)
            {


            }
            return Json(lstEmployees, JsonRequestBehavior.AllowGet);
           
         }

        public JsonResult obtenercita()
        {
            List<CitaResult> lstEmployees = new List<CitaResult>();
            Entities.Employees eEmployee = (Entities.Employees)Session["User"];
            List<CitaResult> tem = new List<CitaResult>();
            if (eEmployee!=null)
            {

       
            using (var connection = new SqlConnection(strConnection1))
            {
                try
                {
                    int person = int.Parse(eEmployee.Idhrms.ToString());
                    DynamicParameters param = new DynamicParameters();
                    param.Add("@TipoConsulta", "pruebacita");
                    param.Add("@IdPais", "NI");
                    param.Add("@IdPersona", person);

                    // Abrir la conexión y comenzar la transacción
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var listaOrdenes = connection.Query<CitaResult>(
                                "sprCombustibleConsumoClaro",
                                param,
                                transaction: transaction,
                                commandType: CommandType.StoredProcedure
                            ).ToList();

                            // Confirmar la transacción si la consulta tuvo éxito
                            transaction.Commit();

                            if (listaOrdenes.Count > 0)
                            {
                                return Json(listaOrdenes, JsonRequestBehavior.AllowGet);

                            }
                            else
                            {
                             }
                        }
                        catch (Exception)
                        {
                            // Deshacer la transacción en caso de error
                            transaction.Rollback();
                            return Json(lstEmployees, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                catch (Exception)
                {

                }
            }

            }
            return Json(tem, JsonRequestBehavior.AllowGet);

        }

        public JsonResult ObtenerFechasViaticos()
        {
            try
            {
                List<organizacionviatico> t1 =   new List<organizacionviatico>
        {
            new organizacionviatico { Id = "300000002978259", Descripcion = "NI DIRECCION PAIS", Padre = "3E+14", Desp = "DIRECCION REGIONAL", Hrms = 7515 },
            new organizacionviatico { Id = "300000002955989", Descripcion = "NI AREA COMERCIAL", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 7595 },
            new organizacionviatico { Id = "300000002978260", Descripcion = "NI GERENCIA DE AUDITORIA", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 338 },
            new organizacionviatico { Id = "300000002903006", Descripcion = "NI GERENCIA TECNICA", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 6656 },
            new organizacionviatico { Id = "300000002979149", Descripcion = "NI GERENCIA OPERACIONES COMERCIALES", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 6337 },
            new organizacionviatico { Id = "300000002954196", Descripcion = "NI GERENCIA CALL CENTER", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 6320 },
            new organizacionviatico { Id = "300000002982263", Descripcion = "NI GERENCIA DE PLAZA", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 7216 },
            new organizacionviatico { Id = "300000002984517", Descripcion = "NI GERENCIA CUENTAS CORPORATIVAS", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 5506 },
            new organizacionviatico { Id = "300000002979944", Descripcion = "NI GERENCIA MARKETING", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 5490 },
            new organizacionviatico { Id = "300000002982516", Descripcion = "NI GERENCIA JURIDICA", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 348 },
            new organizacionviatico { Id = "300000002977816", Descripcion = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 345 },
            new organizacionviatico { Id = "300000002980211", Descripcion = "NI GERENCIA ADMINISTRACION Y FINANZAS", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 335 },
            new organizacionviatico { Id = "300000002980876", Descripcion = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 341 },
            new organizacionviatico { Id = "300000002982193", Descripcion = "NI GERENCIA DE RECURSOS HUMANOS", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 343 },
            new organizacionviatico { Id = "300000002954381", Descripcion = "NI GERENCIA DE COMPRAS", Padre = "3E+14", Desp = "NI DIRECCION PAIS", Hrms = 339 }
        };
                List<organizacionviatico> t2 = new List<organizacionviatico>
                      {
            new organizacionviatico { Id = "300000002869772", Descripcion = "NI SUBGCIA COMUNICACION CORPORATIVA", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 7516 },
            new organizacionviatico { Id = "300000002902619", Descripcion = "NI GERENCIA DE IMPLANTACION", Padre = "300000002903006", Desp = "NI GERENCIA TECNICA", Hrms = 6817 },
            new organizacionviatico { Id = "300000002979366", Descripcion = "NI SUBGERENCIA DE ALMACENES", Padre = "300000002980211", Desp = "NI GERENCIA ADMINISTRACION Y FINANZAS", Hrms = 7735 },
            new organizacionviatico { Id = "300000002902439", Descripcion = "NI SUBGCIA INGENIERIA Y CALIDAD MOVIL", Padre = "300000002903006", Desp = "NI GERENCIA TECNICA", Hrms = 6658 },
            new organizacionviatico { Id = "300000002902361", Descripcion = "NI BI", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 6347 },
            new organizacionviatico { Id = "300000002955989", Descripcion = "NI AREA COMERCIAL", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 7595 },
            new organizacionviatico { Id = "300000002978884", Descripcion = "NI COORDINACION DE CALIDAD Y SOPORTE COMERCIAL", Padre = "300000002954196", Desp = "NI GERENCIA CALL CENTER", Hrms = 6575 },
            new organizacionviatico { Id = "300000002978973", Descripcion = "NI SUBGERENCIA DE COMPENSACIONES", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 404 },
            new organizacionviatico { Id = "300000002979450", Descripcion = "NI DEPARTAMENTO MASAYA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 3002 },
            new organizacionviatico { Id = "300000002979452", Descripcion = "NI COORD. OPERACION BACKEND", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7697 },
            new organizacionviatico { Id = "300000002979635", Descripcion = "NI DEPARTAMENTO GRANADA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2960 },
            new organizacionviatico { Id = "300000002977708", Descripcion = "NI SUPERV. DE ADQUISICION SITIOS CELULARES", Padre = "300000002954381", Desp = "NI GERENCIA DE COMPRAS", Hrms = 7210 },
            new organizacionviatico { Id = "300000002977779", Descripcion = "NI R-INFRAESTRUCTURA Y PUESTOS DE TRABAJO TI CENAM", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7760 },
            new organizacionviatico { Id = "300000002980001", Descripcion = "NI COORD. ASEGURAMIENTO DE INGRESOS", Padre = "300000002978260", Desp = "NI GERENCIA DE AUDITORIA", Hrms = 7058 },
            new organizacionviatico { Id = "300000002980022", Descripcion = "NI COORD. PROCESOS Y CALIDAD", Padre = "300000002978260", Desp = "NI GERENCIA DE AUDITORIA", Hrms = 7060 },
            new organizacionviatico { Id = "300000002955196", Descripcion = "NI SUBGCIA. MARKETING MOVIL Y VAS", Padre = "300000002979944", Desp = "NI GERENCIA MARKETING", Hrms = 6225 },
            new organizacionviatico { Id = "300000002955875", Descripcion = "NI SUBGERENCIA DE RECURSOS HUMANOS", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 7212 },
            new organizacionviatico { Id = "300000002956319", Descripcion = "NI COORDINACION CECOR", Padre = "300000002984517", Desp = "NI GERENCIA CUENTAS CORPORATIVAS", Hrms = 7455 },
            new organizacionviatico { Id = "300000002957000", Descripcion = "NI SUBGCIA. INTELIGENCIA DE MERCADOS", Padre = "300000002979944", Desp = "NI GERENCIA MARKETING", Hrms = 5500 },
            new organizacionviatico { Id = "300000002958825", Descripcion = "NI COORD. MICROINFORMATICA", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7699 },
            new organizacionviatico { Id = "300000002978025", Descripcion = "NI COORD. INFORMACION E INDICADORES", Padre = "300000002979149", Desp = "NI GERENCIA OPERACIONES COMERCIALES", Hrms = 6365 },
            new organizacionviatico { Id = "300000002977124", Descripcion = "NI DEPARTAMENTO BOACO", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2704 },
            new organizacionviatico { Id = "300000002978260", Descripcion = "NI GERENCIA DE AUDITORIA", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 338 },
            new organizacionviatico { Id = "300000002903006", Descripcion = "NI GERENCIA TECNICA", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 6656 },
            new organizacionviatico { Id = "300000002978328", Descripcion = "NI SUBGCIA. TIENDA VIRTUAL", Padre = "300000002977816", Desp = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Hrms = 7956 },
            new organizacionviatico { Id = "300000002978710", Descripcion = "NI SUBGCIA. REGIONAL DE MARKETING CORPORATIVO", Padre = "300000002984517", Desp = "NI GERENCIA CUENTAS CORPORATIVAS", Hrms = 7935 },
            new organizacionviatico { Id = "300000002979061", Descripcion = "NI COORD. PROCESOS FACTURACION", Padre = "300000002980211", Desp = "NI GERENCIA ADMINISTRACION Y FINANZAS", Hrms = 5682 },
            new organizacionviatico { Id = "300000002979149", Descripcion = "NI GERENCIA OPERACIONES COMERCIALES", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 6337 },
            new organizacionviatico { Id = "300000002903227", Descripcion = "NI SUBGERENCIA INGENIERIA FIJA", Padre = "300000002903006", Desp = "NI GERENCIA TECNICA", Hrms = 6666 },
            new organizacionviatico { Id = "300000002978480", Descripcion = "NI R-SERVICIOS DE VALOR AGREGADO TI CENAM", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7763 },
            new organizacionviatico { Id = "300000002978763", Descripcion = "NI R-SERV.CANALES DIG. Y DITRIBUCION TI CENAM", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7761 },
            new organizacionviatico { Id = "300000002902948", Descripcion = "NI GERENCIA OPERACIONES PLANTA INTERNA", Padre = "300000002903006", Desp = "NI GERENCIA TECNICA", Hrms = 6731 },
            new organizacionviatico { Id = "300000002977705", Descripcion = "NI DEPARTAMENTO ESTELI", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2951 },
            new organizacionviatico { Id = "300000002902772", Descripcion = "NI GERENCIA OPERACIONES PLANTA EXTERNA", Padre = "300000002903006", Desp = "NI GERENCIA TECNICA", Hrms = 6671 },
            new organizacionviatico { Id = "300000002954136", Descripcion = "NI TRADE MARKETING", Padre = "300000002982263", Desp = "NI GERENCIA DE PLAZA", Hrms = 7636 },
            new organizacionviatico { Id = "300000002954166", Descripcion = "NI R-AUTOMATIZACIONES Y RPA TI CENAM", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7762 },
            new organizacionviatico { Id = "300000002954196", Descripcion = "NI GERENCIA CALL CENTER", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 6320 },
            new organizacionviatico { Id = "300000002903062", Descripcion = "NI SOX", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 6348 },
            new organizacionviatico { Id = "300000002954543", Descripcion = "NI COORDINACIÓN MARKETING OPERATIVO", Padre = "300000002982263", Desp = "NI GERENCIA DE PLAZA", Hrms = 7218 },
            new organizacionviatico { Id = "300000002954573", Descripcion = "NI R-APLICACIONES CORPORATIVAS TI CENAM", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7764 },
            new organizacionviatico { Id = "300000002958960", Descripcion = "NI COORDINACION CONFIGURACIONES", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7696 },
            new organizacionviatico { Id = "300000002978246", Descripcion = "NI SUBGERENCIA OPERACIONES COMERCIALES", Padre = "300000002979149", Desp = "NI GERENCIA OPERACIONES COMERCIALES", Hrms = 6342 },
            new organizacionviatico { Id = "300000002979001", Descripcion = "NI DEPARTAMENTO JINOTEGA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2966 },
            new organizacionviatico { Id = "300000002977451", Descripcion = "NI INFRAESTRUCTURAS", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7700 },
            new organizacionviatico { Id = "300000002979503", Descripcion = "NI SUBGERENCIA PLANIFICACION FINANCIERA", Padre = "300000002980211", Desp = "NI GERENCIA ADMINISTRACION Y FINANZAS", Hrms = 6149 },
            new organizacionviatico { Id = "300000002982052", Descripcion = "NI LITIGIOS", Padre = "300000002982516", Desp = "NI GERENCIA JURIDICA", Hrms = 7077 },
            new organizacionviatico { Id = "300000002982134", Descripcion = "NI DEPARTAMENTO RIVAS", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 3069 },
            new organizacionviatico { Id = "300000002982263", Descripcion = "NI GERENCIA DE PLAZA", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 7216 },
            new organizacionviatico { Id = "300000002984517", Descripcion = "NI GERENCIA CUENTAS CORPORATIVAS", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 5506 },
            new organizacionviatico { Id = "300000002984589", Descripcion = "NI SUBGCIA. PUBLICIDAD Y MARKETING OPERATIVO", Padre = "300000002979944", Desp = "NI GERENCIA MARKETING", Hrms = 5492 },
            new organizacionviatico { Id = "300000066033796", Descripcion = "NI GERENCIA COMERCIAL MOVIL Y SERVICIOS FIJOS_DUPLICADA", Padre = "300000002955989", Desp = "NI AREA COMERCIAL", Hrms = 7595 },
            new organizacionviatico { Id = "300000002978643", Descripcion = "NI CAC ZONA MANAGUA I", Padre = "300000002977816", Desp = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Hrms = 7235 },
            new organizacionviatico { Id = "300000002980014", Descripcion = "NI SUBGERENCIA DE COMPRAS", Padre = "300000002954381", Desp = "NI GERENCIA DE COMPRAS", Hrms = 7195 },
            new organizacionviatico { Id = "300000002954758", Descripcion = "GERENCIA COMERCIAL SERVICIOS FIJOS", Padre = "300000002955989", Desp = "NI AREA COMERCIAL", Hrms = 7622 },
            new organizacionviatico { Id = "300000002980581", Descripcion = "NI GERENCIA COMERCIAL MOVIL Y SERVICIOS FIJOS", Padre = "300000002955989", Desp = "NI AREA COMERCIAL", Hrms = 7596 },
            new organizacionviatico { Id = "300000002979944", Descripcion = "NI GERENCIA MARKETING", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 5490 },
            new organizacionviatico { Id = "300000002980238", Descripcion = "NI DEPARTAMENTO MATAGALPA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 3008 },
            new organizacionviatico { Id = "300000002982420", Descripcion = "NI COORD. ANALISIS DE RIESGOS", Padre = "300000002978260", Desp = "NI GERENCIA DE AUDITORIA", Hrms = 7059 },
            new organizacionviatico { Id = "300000002982516", Descripcion = "NI GERENCIA JURIDICA", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 348 },
            new organizacionviatico { Id = "300000002982723", Descripcion = "NI DEPARTAMENTO MADRIZ", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2984 },
            new organizacionviatico { Id = "300000002982801", Descripcion = "NI DEPARTAMENTO NUEVA SEGOVIA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 3047 },
            new organizacionviatico { Id = "300000002977952", Descripcion = "NI SUBGCIA DE RELACIONES LABORALES", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 393 },
            new organizacionviatico { Id = "300000002981171", Descripcion = "NI GERENCIA DE SEGURIDAD Y EMERGENCIA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 344 },
            new organizacionviatico { Id = "300000002984355", Descripcion = "NI GERENCIA COMERCIAL CADENAS Y MULTIMARCAS", Padre = "300000002955989", Desp = "NI AREA COMERCIAL", Hrms = 7618 },
            new organizacionviatico { Id = "300000002980302", Descripcion = "NI COORD. DE RECLAMOS", Padre = "300000002954196", Desp = "NI GERENCIA CALL CENTER", Hrms = 6635 },
            new organizacionviatico { Id = "300000002982564", Descripcion = "NI SUBGCIA DE CONTRATOS Y CONVENIOS", Padre = "300000002982516", Desp = "NI GERENCIA JURIDICA", Hrms = 379 },
            new organizacionviatico { Id = "300000002982703", Descripcion = "NI R-DISEÑO Y TRANSICION TI CENAM", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7703 },
            new organizacionviatico { Id = "300000002982799", Descripcion = "NI COORD. ASEGURAMIENTO DE CALIDAD (QA)", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7698 },
            new organizacionviatico { Id = "300000002982907", Descripcion = "NI GERENCIA REGULACION E INTERCONEXION", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 351 },
            new organizacionviatico { Id = "300000002984535", Descripcion = "NI COORD. DE AUDITORIA TEC. INFORMATIC", Padre = "300000002978260", Desp = "NI GERENCIA DE AUDITORIA", Hrms = 7056 },
            new organizacionviatico { Id = "300000002984602", Descripcion = "NI DEPARTAMENTO JUIGALPA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2969 },
            new organizacionviatico { Id = "300000002977816", Descripcion = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 345 },
            new organizacionviatico { Id = "300000002980211", Descripcion = "NI GERENCIA ADMINISTRACION Y FINANZAS", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 335 },
            new organizacionviatico { Id = "300000002980245", Descripcion = "NI DEPARTAMENTO ATLANTICO SUR", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2698 },
            new organizacionviatico { Id = "300000002980258", Descripcion = "NI SUBGCIA DE CONTABILIDAD", Padre = "300000002980211", Desp = "NI GERENCIA ADMINISTRACION Y FINANZAS", Hrms = 378 },
            new organizacionviatico { Id = "300000002980307", Descripcion = "NI SUBGERENCIA SOPORTE FRONTEND", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7695 },
            new organizacionviatico { Id = "300000002980349", Descripcion = "NI COORD. CALL CENTER MOVIL", Padre = "300000002954196", Desp = "NI GERENCIA CALL CENTER", Hrms = 7424 },
            new organizacionviatico { Id = "300000002980409", Descripcion = "NI DEPARTAMENTO CHINANDEGA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2717 },
            new organizacionviatico { Id = "300000002980400", Descripcion = "NI COORD. POSPAGO TELEVENTAS", Padre = "300000002954196", Desp = "NI GERENCIA CALL CENTER", Hrms = 7426 },
            new organizacionviatico { Id = "300000002980539", Descripcion = "NI R-SEGURIDAD TI CENAM", Padre = "300000002980876", Desp = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Hrms = 7759 },
            new organizacionviatico { Id = "300000002980628", Descripcion = "NI SUBGCIA DE FIDELIZACION Y RETENCIONES", Padre = "300000002977816", Desp = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Hrms = 373 },
            new organizacionviatico { Id = "300000002981092", Descripcion = "NI DEPARTAMENTO CARAZO", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2712 },
            new organizacionviatico { Id = "300000002980876", Descripcion = "NI GERENCIA DE INFORMATICA Y TECNOLOGIA", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 341 },
            new organizacionviatico { Id = "300000002981133", Descripcion = "NI DEPARTAMENTO MANAGUA", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2993 },
            new organizacionviatico { Id = "300000002981156", Descripcion = "NI SUBGCIA CAPACITACION Y DESARROLLO", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 371 },
            new organizacionviatico { Id = "300000002982062", Descripcion = "NI COORD. CALL CENTER MULTIMEDIA Y DESPACHO", Padre = "300000002954196", Desp = "NI GERENCIA CALL CENTER", Hrms = 7416 },
            new organizacionviatico { Id = "300000002982193", Descripcion = "NI GERENCIA DE RECURSOS HUMANOS", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 343 },
            new organizacionviatico { Id = "300000002982326", Descripcion = "NI DEPARTAMENTO LEON", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 2975 },
            new organizacionviatico { Id = "300000002982590", Descripcion = "NI COORDINACION DE TESORERIA", Padre = "300000002980211", Desp = "NI GERENCIA ADMINISTRACION Y FINANZAS", Hrms = 6359 },
            new organizacionviatico { Id = "300000002981469", Descripcion = "NI SUBGERENCIA CREDITO Y COBRO", Padre = "300000002979149", Desp = "NI GERENCIA OPERACIONES COMERCIALES", Hrms = 6338 },
            new organizacionviatico { Id = "300000002954241", Descripcion = "NI COORD. DE AUDITORIA DE NEGOCIOS", Padre = "300000002978260", Desp = "NI GERENCIA DE AUDITORIA", Hrms = 7121 },
            new organizacionviatico { Id = "300000002902514", Descripcion = "NI SUBGERENCIA DE SOPORTE Y GESTION", Padre = "300000002903006", Desp = "NI GERENCIA TECNICA", Hrms = 6835 },
            new organizacionviatico { Id = "300000002954381", Descripcion = "NI GERENCIA DE COMPRAS", Padre = "300000002978259", Desp = "NI DIRECCION PAIS", Hrms = 339 },
            new organizacionviatico { Id = "300000002956374", Descripcion = "NI SUBGCIA. SOPORTE COMERCIAL Y VIP", Padre = "300000002954196", Desp = "NI GERENCIA CALL CENTER", Hrms = 8016 },
            new organizacionviatico { Id = "300000069707389", Descripcion = "NI SUPERVISION VENTAS CLIENTES ESTRATEGICOS Y MULTINACIONALES", Padre = "300000002984517", Desp = "NI GERENCIA CUENTAS CORPORATIVAS", Hrms = 8015 },
            new organizacionviatico { Id = "300000002979252", Descripcion = "NI SUBGERENCIA VENTAS PYME", Padre = "300000002984517", Desp = "NI GERENCIA CUENTAS CORPORATIVAS", Hrms = 5512 },
            new organizacionviatico { Id = "300000002978886", Descripcion = "NI SUBGCIA. MARKETING CORPORATIVO Y CONSULTORIA PREVENTA", Padre = "300000002984517", Desp = "NI GERENCIA CUENTAS CORPORATIVAS", Hrms = 7135 },
            new organizacionviatico { Id = "300000002981263", Descripcion = "NI SUBGERENCIA DE HIGIENE Y SEGURIDAD", Padre = "300000002982193", Desp = "NI GERENCIA DE RECURSOS HUMANOS", Hrms = 7555 },
            new organizacionviatico { Id = "300000002955527", Descripcion = "NI SUBGCIA. DE PLANEAMIENTO COMERCIAL", Padre = "300000002979944", Desp = "NI GERENCIA MARKETING", Hrms = 7975 },
            new organizacionviatico { Id = "300000002980518", Descripcion = "NI CAC ZONA CENTRO - ESPECIALES", Padre = "300000002977816", Desp = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Hrms = 7319 },
            new organizacionviatico { Id = "300000002979250", Descripcion = "NI CAC ZONA MANAGUA II", Padre = "300000002977816", Desp = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Hrms = 7244 },
            new organizacionviatico { Id = "300000002979816", Descripcion = "NI SUBGCIA VENTAS PUBLICIDAD", Padre = "300000002984517", Desp = "NI GERENCIA CUENTAS CORPORATIVAS", Hrms = 7079 },
            new organizacionviatico { Id = "300000002979708", Descripcion = "NI SUBGCIA. VENTAS CORPORATIVAS", Padre = "300000002984517", Desp = "NI GERENCIA CUENTAS CORPORATIVAS", Hrms = 5515 },
            new organizacionviatico { Id = "300000002954151", Descripcion = "NI CAC ZONA CENTRO - NORTE", Padre = "300000002977816", Desp = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Hrms = 7278 },
            new organizacionviatico { Id = "300000002980677", Descripcion = "NI CAC ZONA SURORIENTE", Padre = "300000002977816", Desp = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Hrms = 7300 },
            new organizacionviatico { Id = "300000002980671", Descripcion = "NI CAC ZONA NOROCCIDENTE", Padre = "300000002977816", Desp = "NI GERENCIA CENTRO DE ATENCION AL CLIENTE", Hrms = 7255 },
            new organizacionviatico { Id = "300000131488222", Descripcion = "NI SUBGCIA. PRODUCTOS  FIJOS", Padre = "300000002979944", Desp = "NI GERENCIA MARKETING", Hrms = 5490 }
        };
              
                Entities.Employees eEmployee = (Entities.Employees)Session["User"];
                string org = "";
                if (t1.Count(x => x.Hrms.ToString() == eEmployee.GERENCIAIDHRMS) > 0)
                {
                    org = eEmployee.GERENCIAIDHRMS;
                }
                else if (t2.Count(x => x.Hrms.ToString() == eEmployee.GERENCIAIDHRMS)>0)
                {
                    string organizacion = t2.Where(x => x.Hrms.ToString() == eEmployee.GERENCIAIDHRMS).FirstOrDefault().Padre;
                    org = t1.Where(x => x.Id  == organizacion).FirstOrDefault().Hrms.ToString();

                }
                if (org!="")
                {

                
                var client = new RestClient("http://172.26.54.66/apihcm/api/viatico/GetAllExpensesx?managementId=" + org);
                var request = new RestRequest(Method.GET);
                request.Timeout = -1;

                var resultExpensesx = client.Execute(request);

                if (resultExpensesx != null)
                {
                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 500000000;

                    var lstEmployees = serializer.Deserialize<List<Models.periodovt>>(resultExpensesx.Content);
                    string genrencia = "";
                    genrencia = eEmployee.GERENCIA;
                    genrencia = genrencia.Replace("NI ", "");

                    // Inicializar las variables para los diferentes periodos
                    string reembolsoInicio = null;
                    string complemento = null;
                    string anticipoInicio = null;
                    string emergenciaClienteInicio = null;
                    string fechaMaximaRegistro = null;
                    if (lstEmployees!=null && lstEmployees.Count()>0 )
                    {

                    foreach (var item in lstEmployees)
                    {
                        switch (item.ClassId)
                        {
                            case 17:
                                if (item.Notes.Contains("COMPLEMENTO"))
                                {
                                            // Categorizar como "Complemento" en lugar de "Reembolso"
                                            complemento = "Complemento: " + item.Notes;
                                }
                                else
                                {
                                    reembolsoInicio = item.Notes;
                                }
                                fechaMaximaRegistro = item.LastDate.ToString("dd 'de' MMMM yyyy");
                                break;

                            case 16:
                                anticipoInicio = item.Notes;
                                fechaMaximaRegistro = item.LastDate.ToString("dd 'de' MMMM yyyy");
                                break;

                            case 94:
                                        if (org == "343")
                                        {
                                            if (item.Notes.Contains(eEmployee.GERENCIA.Replace("NI ",""))==true)
                                            {
                                                emergenciaClienteInicio = item.Notes;
                                                fechaMaximaRegistro = item.LastDate.ToString("dd 'de' MMMM yyyy");
                                            }
                                        }
                                        else
                                        {
                                            emergenciaClienteInicio = item.Notes;
                                            fechaMaximaRegistro = item.LastDate.ToString("dd 'de' MMMM yyyy");
                                        }
                                break;
                        }
                    }
                    }
                    var datos = new
                    {
                        ReembolsoInicio = reembolsoInicio,
                        AnticipoInicio = anticipoInicio,
                        EmergenciaClienteInicio = emergenciaClienteInicio,
                        FechaMaximaRegistro = fechaMaximaRegistro,
                        Complemento= complemento
                    };

                    return Json(datos, JsonRequestBehavior.AllowGet);
                }
                }
            }
            catch (Exception e)
            {
                // Manejo de errores (puedes loguear el error o manejarlo de acuerdo a tus necesidades)
            }

            return Json(new { Error = "No se pudo obtener los datos." }, JsonRequestBehavior.AllowGet);
        }
 
    [HttpPost]
        public JsonResult GetChartExtraTimeExecutedAssignment()
        {


            List<Entities.ViewModels.DashboardExtraTimeBudgetExecuted> lstHours = new List<Entities.ViewModels.DashboardExtraTimeBudgetExecuted>();

            Entities.Employees eEmployee = null;

            ////if (Session["User"] != null)
            ////{
            eEmployee = (Entities.Employees)Session["User"];
            ////}

            ////    if ((eEmployee.UserLevel == 5) || (eEmployee.UserLevel == 6))
            ////    {
            var iData = Data.ExtraTime.GetChartExecutedAssignment(eEmployee.Idhrms);
            if (iData != null)
            {
                foreach (var item in iData)
                {
                    Entities.ViewModels.DashboardExtraTimeBudgetExecuted dash = new Entities.ViewModels.DashboardExtraTimeBudgetExecuted();
                    dash.PeriodId = item.Period;
                    dash.Type = item.Type;
                    dash.Hours = item.Hours;

                    lstHours.Add(dash);

                }
            }
            //var period = iData.Select(x => x.PeriodId).Distinct();


            return Json(lstHours, JsonRequestBehavior.AllowGet);




        }
        /// <summary>
        /// Accion que llama al metodo GetChartExecutedAssignment para cargar informacion de Viaticos Ejecutadas vs Asignadas.
        /// </summary>
        /// <param name="bossId"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetChartExpensesExecutedAssignment()
        {


            List<Entities.ViewModels.DashboardExpenseBudgetExecuted> lstDash = new List<Entities.ViewModels.DashboardExpenseBudgetExecuted>();

            Entities.Employees eEmployee = null;

            ////if (Session["User"] != null)
            ////{
            eEmployee = (Entities.Employees)Session["User"];
            ////}

            ////    if ((eEmployee.UserLevel == 5) || (eEmployee.UserLevel == 6))
            ////    {
            var iData = Data.Expense.GetChartExecutedAssignment(eEmployee.Idhrms);
            if (iData != null)
            {
                foreach (var item in iData)
                {
                    Entities.ViewModels.DashboardExpenseBudgetExecuted dash = new Entities.ViewModels.DashboardExpenseBudgetExecuted();
                    dash.PeriodId = item.PeriodId;
                    dash.Type = item.Type;
                    dash.Amount = item.Amount;

                    lstDash.Add(dash);

                }
            }



            return Json(lstDash, JsonRequestBehavior.AllowGet);




        }


        [HttpGet]
        public async Task<JsonResult> GetNotifications()
        {
            try
            {
                // Obtén el empleado desde la sesión (si no existe, usa valores de prueba)
                var employee = Session["User"] as Entities.Employees;
                var carn = employee != null ? employee.EmployeeNumber : "0";
                var personId = employee != null ? employee.GERENCIAIDHRMS : "0";
                

                // --- Consumir API 1: Citas Pendientes ---
                List<Periodovt> citaPendienteList = new List<Periodovt>();
                var client1 = new RestClient("http://172.26.54.66/apihcm/api/values/cita/citapendiente?carnet=" + carn);
                var request1 = new RestRequest(Method.GET);
                request1.Timeout = -1;
                var tcs1 = new TaskCompletionSource<IRestResponse>();
                client1.ExecuteAsync(request1, (response, handle) =>
                {
                    tcs1.SetResult(response);
                });
                var response1 = await tcs1.Task;

                if (response1.Content != null && response1.Content != null)
                {
                    var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                    citaPendienteList = serializer.Deserialize<List<Periodovt>>(response1.Content);
                }

                // --- Consumir API 2: Solicitudes ---
                // Consumir API 2: Solicitudes
                List<CitaSolicitud> solicitudList = new List<CitaSolicitud>();
                var client2 = new RestClient("http://172.26.54.66/apihcm/api/values/cita/solicitud?personId=" + carn);
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
                }

                // Arma el ViewModel con ambas listas
                var viewModel = new NavNotificationsViewModel
                {
                    CitaPendienteCount = citaPendienteList.Count,
                    SolicitudCount = solicitudList.Count,
                    CitaPendienteList = citaPendienteList,
                    SolicitudList = solicitudList
                };

                return Json(viewModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Error = "Error al cargar notificaciones: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }


}

public enum HeaderViewRenderMode { Full, Title }