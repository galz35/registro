using Dapper;
using Datos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RestSharp;
using slnRhonline.Data;
using slnRhonline.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebApi.Models;

namespace slnRhonline.Controllers
{
    public class EvaluacionController : Controller
    {
        static ServiceReference1.ClaroAsemClient ClaroWCF = new ServiceReference1.ClaroAsemClient();
        public string strConnection1 = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = x1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;";

        // GET: Evaluacion
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Encurso()
        {
            return View();
        }
        // GET: Evaluacion/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Evaluacion/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Evaluacion/Create
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

        // GET: Evaluacion/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Evaluacion/Edit/5
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

        // GET: Evaluacion/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }
        public ActionResult EnviarEvaluacionobjetivo()
        {
            return View();

        }
        // POST: Evaluacion/Delete/5
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
        [HttpPost]
        public JsonResult GuardarEncuestax(List<RespuestaViewModel> respuestas) // Baja-evaluacion
        {
            try
            {
                if (respuestas == null || respuestas.Count == 0)
                {
                    return Json(new { success = false, message = "No se recibieron respuestas." });
                }

                // Obtener la información del proceso y usuario desde la sesión
                datas.Root lstEmployees = (datas.Root)Session["procesotodo"];
                var proceso = lstEmployees?.Procesos.FirstOrDefault();

                if (proceso == null)
                {
                    return Json(new { success = false, message = "Proceso no encontrado en sesión." });
                }

                // Obtener todas las preguntas y crear un diccionario
                var todasLasPreguntas = proceso.Formularios.SelectMany(f => f.Preguntas).ToList();
                var diccionarioPreguntas = todasLasPreguntas.ToDictionary(x => x.PreguntaID, x => x.TextoPregunta);

                var datosAnalisis = new
                {
                    respuestas = respuestas.Select(r => new
                    {
                        PreguntaID = r.PreguntaID,
                        TextoPregunta = diccionarioPreguntas.ContainsKey(r.PreguntaID) ? diccionarioPreguntas[r.PreguntaID] : "Texto de pregunta no encontrado",

                        Respuesta = r.TextoRespuesta
                    }).ToList()
                };
                string analisis = "";
                string analisis2 = "";
                string analisis3 = "";
             //   analisis = ObtenerAnalisisSincrono(datosAnalisis);
                analisis = ObtenerAnalisisSincrono4(datosAnalisis);
           //     analisis2 = ObtenerAnalisisSincrono3(datosAnalisis);
                if (analisis == null)
                {
                }
                 List<emp2024> listaEmpleados = (List<emp2024>)Session["listabaja"];
                string idEmpleado = (string)Session["EmpleadoID"];
                var empleado = listaEmpleados?.FirstOrDefault(x => x.idhcm == idEmpleado);

                if (empleado == null)
                {
                    return Json(new { success = false, message = "Empleado no encontrado en sesión." });
                }

                using (var connection = new SqlConnection(strConnection1))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Paso 0: Obtener EstadoID para 'Baja' y 'Finalizado'
                            var estadoBajaID = connection.QuerySingle<int>(
                                "SELECT EstadoID FROM [dbo].[CatalogoEstado] WHERE Descripcion = @Desc",
                                new { Desc = "Baja" },
                                transaction);

                            var estadoFinalizadoID = connection.QuerySingle<int>(
                                "SELECT EstadoID FROM [dbo].[CatalogoEstado] WHERE Descripcion = @Desc",
                                new { Desc = "Finalizado" },
                                transaction);

                            // Paso 1: Insertar Evaluación
                            var evaluacionParams = new
                            {
                                UsuarioID = empleado.carnet,
                                ProcesoID = proceso.ProcesoID,
                                TipoDeConsulta = "InsertarEvaluacion",
                                FechaAsignacion = DateTime.Now.Date, // Usar solo la fecha
                                EstadoID = estadoBajaID, // 'Baja'
                                FechaFinalizacion = (DateTime?)null, // No finalizar ahora
                                CargoAnterior = empleado.cargo,
                                NuevoCargo = empleado.cargo,
                                JefeID = empleado.carnet_jefe1,
                                GerenciaAnterior = empleado.OGERENCIA,
                                NuevaGerencia = empleado.OGERENCIA,
                                CantidadCiclosPlaneados = 1,
                                IA = analisis
                            };

                            int evaluacionID = connection.QuerySingle<int>(
                                "SP_G_Evaluacio",
                                evaluacionParams,
                                transaction,
                                commandType: CommandType.StoredProcedure);

                            // Paso 2: Insertar Ciclo
                            var cicloParams = new
                            {
                                UsuarioID = empleado.carnet, // Necesario para el procedimiento
                                ProcesoID = proceso.ProcesoID, // Aunque no se usa en InsertarCiclo, se incluye por parámetros generales
                                TipoDeConsulta = "InsertarCiclo",
                                EvaluacionID = evaluacionID,
                                NombreCiclo = "Ciclo 1",
                                FechaInicio = DateTime.Now.Date,
                                FechaFin = DateTime.Now.AddMonths(1).Date, // Suponiendo ciclo de un mes
                                EstadoCicloID = estadoFinalizadoID // 'Finalizado'
                                                                   // Otros parámetros opcionales permanecen NULL
                            };

                            int cicloEvaluacionID = connection.QuerySingle<int>(
                                "SP_G_Evaluacio",
                                cicloParams,
                                transaction,
                                commandType: CommandType.StoredProcedure);

                            // Paso 3: Insertar Respuestas
                            foreach (var respuesta in respuestas)
                            {
                                var respuestaParams = new
                                {
                                    UsuarioID = empleado.carnet, // Necesario para el procedimiento
                                    ProcesoID = proceso.ProcesoID, // Aunque no se usa en InsertarRespuestas, se incluye por parámetros generales
                                    TipoDeConsulta = "InsertarRespuestas",
                                    FormularioID = respuesta.FormularioID,
                                    PreguntaID = respuesta.PreguntaID,
                                    TextoRespuesta = respuesta.TextoRespuesta,
                                    OpcionID = respuesta.OpcionID,
                                    PorcentajeAsignado = (decimal?)null, // Si es necesario
                                    PorcentajeReal = (decimal?)null,     // Si es necesario
                                    Motivo = (string)null,               // Si es necesario
                                    CicloEvaluacionID = cicloEvaluacionID
                                    // Otros parámetros opcionales permanecen NULL
                                };

                                connection.Execute(
                                    "SP_G_Evaluacio",
                                    respuestaParams,
                                    transaction,
                                    commandType: CommandType.StoredProcedure);
                            }

                            // Commit de la transacción
                            transaction.Commit();

                            return Json(new { success = true, message = "Encuesta guardada exitosamente: analisis IA:."+analisis });
                        }
                        catch (Exception ex)
                        {
                            // Rollback en caso de error
                            transaction.Rollback();
                            return Json(new { success = false, message = "Error al guardar encuesta: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult GuardarEncuestaxx(List<RespuestaViewModel> respuestas)//Baja-evaluacion
        {
            try
            {
                if (respuestas == null || respuestas.Count == 0)
                {
                    return Json(new { success = false, message = "No se recibieron respuestas." });
                }

                // Obtener la información del proceso y usuario desde la sesión
                 datas.Root lstEmployees = new datas.Root();
                lstEmployees = (datas.Root)Session["procesotodo"];
                var p = lstEmployees.Procesos.FirstOrDefault();

                var todasLasPreguntas = p.Formularios.SelectMany(f => f.Preguntas).ToList();
                var diccionarioPreguntas = todasLasPreguntas.ToDictionary(x => x.PreguntaID, x => x.TextoPregunta);

                var datosAnalisis = new
                {
                    respuestas = respuestas.Select(r => new
                    {
                        PreguntaID = r.PreguntaID,
                        TextoPregunta = diccionarioPreguntas.ContainsKey(r.PreguntaID) ? diccionarioPreguntas[r.PreguntaID] : "Texto de pregunta no encontrado",

                        Respuesta = r.TextoRespuesta
                    }).ToList()
                };
                string analisis = "";
                //  analisis = ObtenerAnalisisSincrono(datosAnalisis);
                //if (analisis == null)
                // {
                // }
                 List<emp2024> tem = (List<emp2024>)Session["listabaja"];
                string idbaja = (string)Session["EmpleadoID"];
                var empleado = tem?.FirstOrDefault(x => x.idhcm == idbaja);

                if (empleado == null || p == null)
                {
                    return Json(new { success = false, message = "No se pudo encontrar el empleado o proceso en sesión." });
                }

                using (var connection = new SqlConnection(strConnection1))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Paso 1: Insertar Evaluación
                            var evaluacionParams = new
                            {
                                UsuarioID = empleado.carnet,
                                ProcesoID = p.ProcesoID,
                                TipoDeConsulta = "InsertarEvaluacion",
                                FechaAsignacion = DateTime.Now,
                                Estado = "Finalizado",
                                CargoAnterior = empleado.cargo,
                                NuevoCargo = empleado.cargo,
                                JefeID = empleado.carnet_jefe1,
                                GerenciaAnterior = empleado.OGERENCIA,
                                NuevaGerencia = empleado.OGERENCIA,
                                EstadoEvaluacion = "Baja",
                                CantidadCiclosPlaneados = 1,
                                IA = "Análisis IA"
                            };

                            int evaluacionID = connection.QuerySingle<int>("SP_GestionarEvaluacionCicloFirmaRespuestas", evaluacionParams, transaction, commandType: CommandType.StoredProcedure);

                            // Paso 2: Insertar Ciclo
                            var cicloParams = new
                            {
                                EvaluacionID = evaluacionID,
                                NombreCiclo = "Ciclo 1",
                                FechaInicio = DateTime.Now,
                                EstadoCiclo = "Finalizado",
                                Estado = "Finalizado",
                                TipoDeConsulta = "InsertarCiclo"
                            };

                            int cicloEvaluacionID = connection.QuerySingle<int>("SP_GestionarEvaluacionCicloFirmaRespuestas", cicloParams, transaction, commandType: CommandType.StoredProcedure);

                            // Paso 3: Insertar Respuestas
                            foreach (var respuesta in respuestas)
                            {
                                var respuestaParams = new
                                {
                                    FormularioID = respuesta.FormularioID,
                                    PreguntaID = respuesta.PreguntaID,
                                    UsuarioID = empleado.carnet,
                                    CicloEvaluacionID = cicloEvaluacionID,
                                    TextoRespuesta = respuesta.TextoRespuesta,
                                    OpcionID = respuesta.OpcionID,
                                    PorcentajeAsignado = (decimal?)null,  // Si es necesario
                                    PorcentajeReal = (decimal?)null,      // Si es necesario
                                    Motivo = (string)null,                // Si es necesario
                                    TipoDeConsulta = "InsertarRespuestas"
                                };

                                connection.Execute("SP_GestionarEvaluacionCicloFirmaRespuestas", respuestaParams, transaction, commandType: CommandType.StoredProcedure);
                            }

                            // Si se necesita una firma para el ciclo/evaluación
                            //if (p.RequiereFirma)
                            //{
                            //    var firmaParams = new
                            //    {
                            //        CicloEvaluacionID = cicloEvaluacionID,
                            //        UsuarioID = empleado.carnet,
                            //        Firma = ObtenerFirmaDelUsuario(empleado), // Función que obtiene la firma en binario
                            //        TipoDeConsulta = "InsertarFirma",
                            //        RequiereFirma = true
                            //    };

                            //    connection.Execute("SP_GestionarEvaluacionCicloFirmaRespuestas", firmaParams, transaction, commandType: CommandType.StoredProcedure);
                            //}

                            transaction.Commit();

                            return Json(new { success = true, message = "Encuesta guardada exitosamente." });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { success = false, message = "Error al guardar encuesta: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarRespuestas(List<RespuestaEncuesta> respuestas)
        {
            if (respuestas == null || !respuestas.Any())
            {
                return Json(new { success = false, message = "No se recibieron respuestas." });
            }

            try
            {
                // Aquí puedes procesar las respuestas. Ejemplo: guardar en base de datos o realizar alguna lógica adicional.
                foreach (var respuesta in respuestas)
                {
                    // Procesar cada respuesta. Por ejemplo, podrías guardarlas en la base de datos.
                    Console.WriteLine($"Procesando respuesta de PreguntaID: {respuesta.PreguntaID}, TextoRespuesta: {respuesta.TextoRespuesta}");
                }

                // Retornar una respuesta indicando que todo fue exitoso
                return Json(new { success = true, message = "Respuestas guardadas exitosamente.", totalRespuestas = respuestas.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar las respuestas: " + ex.Message });
            }
        }
        public int InsertarEvaluacion(Evaluacion2 evaluacion)
        {
            using (var connection = new SqlConnection(strConnection1))
            {
                var query = @"
            INSERT INTO [dbo].[Evaluacion] 
            (UsuarioID, FechaAsignacion, Estado, FechaFinalizacion, UsuarioID,CargoAnterior, NuevoCargo, JefeID, GerenciaAnterior, NuevaGerencia, ProcesoID, EstadoEvaluacion, CantidadCiclosPlaneados,IA)
            VALUES 
            (@UsuarioID, @FechaAsignacion, @Estado, @FechaFinalizacion,@UsuarioID, @CargoAnterior, @NuevoCargo, @JefeID, @GerenciaAnterior, @NuevaGerencia, @ProcesoID, @EstadoEvaluacion, @CantidadCiclosPlaneados,@IA);
            SELECT CAST(SCOPE_IDENTITY() as int);";

                var evaluacionID = connection.QuerySingle<int>(query, evaluacion);
                return evaluacionID; // Devolver el ID de la evaluación insertada
            }
        }
        private void InsertarRespuestas(List<RespuestaViewModel> respuestas, int cicloEvaluacionID)
        {
            using (var connection = new SqlConnection(strConnection1))
            {
                var query = @"
                INSERT INTO [dbo].[Respuesta] 
                (FormularioID, PreguntaID, UsuarioID, CicloEvaluacionID, TextoRespuesta, OpcionID, FechaRespuesta, MesCumplido, Mes, Año)
                VALUES 
                (@FormularioID, @PreguntaID, @UsuarioID, @CicloEvaluacionID, @TextoRespuesta, @OpcionID, @FechaRespuesta, @MesCumplido, @Mes, @Año);";

                foreach (var respuesta in respuestas)
                {
                    var now = DateTime.Now;

                    var parametros = new
                    {
                        respuesta.FormularioID,
                        respuesta.PreguntaID,
                        respuesta.UsuarioID,
                        CicloEvaluacionID = cicloEvaluacionID,
                        respuesta.TextoRespuesta,
                        respuesta.OpcionID,
                        FechaRespuesta = now,
                        MesCumplido = now.ToString("MMMM"),  // Mes completo
                        Mes = now.Month.ToString(),  // Número de mes
                        Año = now.Year.ToString()   // Año
                    };

                    connection.Execute(query, parametros);
                }
            }
        }
            public int InsertarCicloEvaluacion(CicloEvaluacion ciclo)
        {
            using (var connection = new SqlConnection(strConnection1))
            {
                var query = @"
            INSERT INTO [dbo].[CicloEvaluacion] 
            (EvaluacionID, NombreCiclo, FechaInicio, FechaFin, Estado, EstadoCiclo)
            VALUES 
            (@EvaluacionID, @NombreCiclo, @FechaInicio, @FechaFin, @Estado, @EstadoCiclo);
            SELECT CAST(SCOPE_IDENTITY() as int);";

                var cicloEvaluacionID = connection.QuerySingle<int>(query, ciclo);
                return cicloEvaluacionID; // Devuelve el ID del ciclo de evaluación
            }
        }
        public int InsertarEvaluacion2(Evaluacion2 evaluacion)
        {
            using (var connection = new SqlConnection(strConnection1))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insertar nueva evaluación
                        var insertEvaluacionQuery = @"
                    INSERT INTO [dbo].[Evaluacion] 
                    (UsuarioID, FechaAsignacion, FechaFinalizacion, CargoAnterior, NuevoCargo, JefeID, GerenciaAnterior, NuevaGerencia, FechaCreacion, ProcesoID, EstadoID, CantidadCiclosPlaneados, IA)
                    VALUES 
                    (@UsuarioID, @FechaAsignacion, @FechaFinalizacion, @CargoAnterior, @NuevoCargo, @JefeID, @GerenciaAnterior, @NuevaGerencia, GETDATE(), @ProcesoID, @EstadoID, @CantidadCiclosPlaneados, @IA);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                        var evaluacionID = connection.QuerySingle<int>(insertEvaluacionQuery, evaluacion, transaction);

                        // Actualizar estados anteriores a 'N' en EstadoEvaluacion
                        var updateEstadoEvaluacionQuery = @"
                    UPDATE [dbo].[EstadoEvaluacion]
                    SET Activo = 'N'
                    WHERE EvaluacionID = @EvaluacionID
                    AND Activo = 'Y';";

                        connection.Execute(updateEstadoEvaluacionQuery, new { EvaluacionID = evaluacionID }, transaction);

                        // Insertar nuevo EstadoEvaluacion con Activo = 'Y'
                        var insertEstadoEvaluacionQuery = @"
                    INSERT INTO [dbo].[EstadoEvaluacion] 
                    (EvaluacionID, UsuarioID, Estado, Fecha, Activo)
                    VALUES 
                    (@EvaluacionID, @UsuarioID, @EstadoID, GETDATE(), 'Y');";

                        connection.Execute(insertEstadoEvaluacionQuery, new { EvaluacionID = evaluacionID, UsuarioID= evaluacion.UsuarioID, eEstadoID="8" }, transaction);

                        transaction.Commit();
                        return evaluacionID;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void InsertarRespuestas2(List<RespuestaViewModel> respuestas, int cicloEvaluacionID)
        {
            using (var connection = new SqlConnection(strConnection1))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var insertRespuestaQuery = @"
                    INSERT INTO [dbo].[Respuesta] 
                    (FormularioID, PreguntaID, UsuarioID, CicloEvaluacionID, TextoRespuesta, OpcionID, FechaRespuesta, MesCumplido, Mes, Año)
                    VALUES 
                    (@FormularioID, @PreguntaID, @UsuarioID, @CicloEvaluacionID, @TextoRespuesta, @OpcionID, @FechaRespuesta, @MesCumplido, @Mes, @Año);";

                        foreach (var respuesta in respuestas)
                        {
                            var now = DateTime.Now;
                            var parametros = new
                            {
                                respuesta.FormularioID,
                                respuesta.PreguntaID,
                                respuesta.UsuarioID,
                                CicloEvaluacionID = cicloEvaluacionID,
                                respuesta.TextoRespuesta,
                                respuesta.OpcionID,
                                FechaRespuesta = now,
                                MesCumplido = now.ToString("MMMM"),
                                Mes = now.Month.ToString(),
                                Año = now.Year.ToString()
                            };

                            connection.Execute(insertRespuestaQuery, parametros, transaction);
                        }

                        // Actualizar estados anteriores a 'N' en EstadoCicloEvaluacion
                        var updateEstadoCicloEvaluacionQuery = @"
                    UPDATE [dbo].[EstadoCicloEvaluacion]
                    SET Activo = 'N'
                    WHERE CicloEvaluacionID = @CicloEvaluacionID
                    AND Activo = 'Y';";

                        connection.Execute(updateEstadoCicloEvaluacionQuery, new { CicloEvaluacionID = cicloEvaluacionID }, transaction);

                        // Insertar nuevo EstadoCicloEvaluacion con Activo = 'Y'
                        var insertEstadoCicloEvaluacionQuery = @"
                    INSERT INTO [dbo].[EstadoCicloEvaluacion] 
                    (CicloEvaluacionID, UsuarioID, Estado, Fecha, Activo)
                    VALUES 
                    (@CicloEvaluacionID, @UsuarioID, @EstadoID, GETDATE(), 'Y');";

                        // Obtener EstadoID para 'Inicio' o 'Progreso' según corresponda
                        foreach (var respuesta in respuestas)
                        {
                            var estadoDescripcion = "Baja"; // Ajusta según tu lógica
                            var estadoID = connection.QuerySingle<int>(
                                "SELECT EstadoID FROM [dbo].[CatalogoEstado] WHERE Descripcion = @Descripcion",
                                new { Descripcion = estadoDescripcion }, transaction);

                            connection.Execute(insertEstadoCicloEvaluacionQuery, new { CicloEvaluacionID = cicloEvaluacionID, UsuarioID = respuesta.UsuarioID, EstadoID = estadoID }, transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public JsonResult EmployeesListjson()
        {

            List<Models.EmpleadoEnPrueba> lstEmployees = new List<Models.EmpleadoEnPrueba>();
            try
            {
                try {
                    var client = new RestClient("http://172.26.54.66/apihcm/api/evaluacion/obtenerTodas");
                    var request = new RestRequest(Method.GET);
                    request.Timeout = -1;


                    var resultExpensesx = client.Execute(request);
                    //Console.WriteLine(response.Content);
                    //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                    if (resultExpensesx != null)
                    {
                        var serializer = new JavaScriptSerializer();
                        serializer.MaxJsonLength = 500000000;

                        lstEmployees = serializer.Deserialize<List<Models.EmpleadoEnPrueba>>(resultExpensesx.Content);
                      }
                } catch (Exception e) { }
             
                return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet); ;

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        public   JsonResult ObtenerDatosProceso()
        {
            string apiUrl = "http://172.26.54.66/apihcm/api/formulariomaster/getall?procesoID=3";

            datas.Root lstEmployees = new datas.Root();
            try
            {
                 
                    var client = new RestClient(apiUrl);
                    var request = new RestRequest(Method.GET);
                    request.Timeout = -1;


                    var resultExpensesx = client.Execute(request);
                    //Console.WriteLine(response.Content);
                    //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                    if (resultExpensesx != null)
                    {
                        var serializer = new JavaScriptSerializer();
                        serializer.MaxJsonLength = 500000000;
                    lstEmployees = serializer.Deserialize<datas.Root>(resultExpensesx.Content);
                    Session["procesotodo"] = lstEmployees;
  
                    // Deserializar el JSON a objetos C#
                //    var procesos = JsonConvert.DeserializeObject<List<Proceso>>(jsonResult);

                    // Puedes manipular los datos aquí si es necesario

                    // Retornar los datos como JSON
                    return Json(lstEmployees, JsonRequestBehavior.AllowGet);

                }
                }
                catch (Exception e) { }

                return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet); ;
 
            
        }
        public JsonResult ObtenerDatosProceso2()
        {
            string apiUrl = "http://172.26.54.66/apihcm/api/formulariomaster/getall?procesoID=5";

            datas.Root lstEmployees = new datas.Root();
            try
            {

                var client = new RestClient(apiUrl);
                var request = new RestRequest(Method.GET);
                request.Timeout = -1;


                var resultExpensesx = client.Execute(request);
                //Console.WriteLine(response.Content);
                //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                if (resultExpensesx != null)
                {
                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 500000000;
                    lstEmployees = serializer.Deserialize<datas.Root>(resultExpensesx.Content);
                    Session["procesotodo"] = lstEmployees;

                    // Deserializar el JSON a objetos C#
                    //    var procesos = JsonConvert.DeserializeObject<List<Proceso>>(jsonResult);

                    // Puedes manipular los datos aquí si es necesario

                    // Retornar los datos como JSON
                    return Json(lstEmployees, JsonRequestBehavior.AllowGet);

                }
            }
            catch (Exception e) { }

            return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet); ;


        }
        [HttpPost]
        public JsonResult GuardarEncuesta(List<RespuestaViewModel> respuestas)
        {
            try
            {
                if (respuestas == null || respuestas.Count == 0)
                {
                    return Json(new { success = false, message = "No se recibieron respuestas." });
                }
                datas.Root lstEmployees = new datas.Root();
                lstEmployees = (datas.Root)Session["procesotodo"];
                    var p = lstEmployees.Procesos.FirstOrDefault();
                 
                var todasLasPreguntas = p.Formularios.SelectMany(f => f.Preguntas).ToList();
                var diccionarioPreguntas = todasLasPreguntas.ToDictionary(x => x.PreguntaID, x => x.TextoPregunta);

                var datosAnalisis = new
                {
                    respuestas = respuestas.Select(r => new
                    {
                        PreguntaID = r.PreguntaID,
                        TextoPregunta = diccionarioPreguntas.ContainsKey(r.PreguntaID) ? diccionarioPreguntas[r.PreguntaID] : "Texto de pregunta no encontrado",
                    
                        Respuesta = r.TextoRespuesta
                    }).ToList()
                };  
                string analisis = "";
                 //analisis = ObtenerAnalisisSincrono(datosAnalisis);
                 //if (analisis == null)
                 //{
                 // }
                List<emp2024> tem = new List<emp2024>();
                tem = (List<emp2024>)Session["listabaja"];
                string idbaja = "0";
                idbaja = (string)Session["EmpleadoID"];
                var t = tem.Where(x => x.idhcm == idbaja).FirstOrDefault();
                int carnet = int.Parse(t.carnet);
                // 1. Insertar Evaluación
                var evaluacion = new Evaluacion2
                {
                    UsuarioID = t.carnet,
                    FechaAsignacion = DateTime.Now,
                  
                    ProcesoID = 3,
                
                    CantidadCiclosPlaneados = 1,
                    NuevoCargo = t.cargo,
                    NuevaGerencia = t.OGERENCIA,
                    JefeID = t.carnet_jefe1,
                    IA = analisis
                };

                int evaluacionID = InsertarEvaluacion2(evaluacion);

                // 2. Insertar Ciclo de Evaluación
                var cicloEvaluacion = new CicloEvaluacion
                {
                    EvaluacionID = evaluacionID,
                    NombreCiclo = "Ciclo 1",
                    FechaInicio = DateTime.Now 
                };

                int cicloEvaluacionID = InsertarCicloEvaluacion(cicloEvaluacion);

                // 3. Insertar las respuestas

                // 3. Insertar las respuestas
                InsertarRespuestas2(respuestas, cicloEvaluacionID);

                return Json(new { success = true, message = "Encuesta guardada exitosamente."+analisis });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult GuardarEncuesta2(List<RespuestaViewModel> respuestas)
        {
            try
            {
                if (respuestas == null || respuestas.Count == 0)
                {
                    return Json(new { success = false, message = "No se recibieron respuestas." });
                }
                datas.Root lstEmployees = new datas.Root();
                lstEmployees = (datas.Root)Session["procesotodo"];
                var p = lstEmployees.Procesos.FirstOrDefault();

                var todasLasPreguntas = p.Formularios.SelectMany(f => f.Preguntas).ToList();
                var diccionarioPreguntas = todasLasPreguntas.ToDictionary(x => x.PreguntaID, x => x.TextoPregunta);

                var datosAnalisis = new
                {
                    respuestas = respuestas.Select(r => new
                    {
                        PreguntaID = r.PreguntaID,
                        TextoPregunta = diccionarioPreguntas.ContainsKey(r.PreguntaID) ? diccionarioPreguntas[r.PreguntaID] : "Texto de pregunta no encontrado",

                        Respuesta = r.TextoRespuesta
                    }).ToList()
                };
                string analisis = "";
             //   analisis = ObtenerAnalisisSincrono2(datosAnalisis);
                //if (analisis == null)
                // {
                // }
                //List<emp2024> tem = new List<emp2024>();
                //tem = (List<emp2024>)Session["listabaja"];
                //int idbaja = 0;
                //idbaja = (int)Session["EmpleadoID"];
                //var t = tem.Where(x => x.idhcm == idbaja).FirstOrDefault();
                //int carnet =int.Parse( t.carnet);
                //// 1. Insertar Evaluación
                //var evaluacion = new Evaluacion2
                //{
                //    UsuarioID = t.carnet,
                //    FechaAsignacion = DateTime.Now,
                //    Estado = "Finalizado",
                //    ProcesoID = 3,
                //    EstadoEvaluacion = "baja",
                //    CantidadCiclosPlaneados = 1,
                //    NuevoCargo = t.cargo,
                //    NuevaGerencia = t.OGERENCIA,
                //    JefeID = t.carnet_jefe1,
                //  IA  = analisis 
                //};

                //int evaluacionID = InsertarEvaluacion(evaluacion);

                //// 2. Insertar Ciclo de Evaluación
                //var cicloEvaluacion = new CicloEvaluacion
                //{
                //    EvaluacionID = evaluacionID,
                //    NombreCiclo = "Ciclo 1",
                //    FechaInicio = DateTime.Now,
                //    Estado = "Finaliazado",
                //    EstadoCiclo = "baja",
                //};

                //int cicloEvaluacionID = InsertarCicloEvaluacion(cicloEvaluacion);

                //// 3. Insertar las respuestas

                //// 3. Insertar las respuestas
                //InsertarRespuestas(respuestas, cicloEvaluacionID);

                return Json(new { success = true, message = "Encuesta guardada exitosamente." + analisis });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }   
        private string ObtenerAnalisisSincrono(object datosAnalisis)
{
            try
            {
                var client = new RestClient("http://172.26.54.111:5000");

                //var url = "http://172.26.54.111:5000/api/call_groq_api32q"; // Asegúrate de que la URL sea correcta
                var request = new RestRequest("/api/call_groq_api32", Method.POST)
                {
                    Timeout = -1 // Sin límite de tiempo
                };

                // Serializar el objeto datosAnalisis a JSON
                var jsonBody = JsonConvert.SerializeObject(datosAnalisis);

                // Agregar encabezados y cuerpo a la solicitud
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

                // Ejecutar la solicitud de forma síncrona
                var response = client.Execute(request);

                // Verificar si la respuesta es exitosa
                if (response.Content != null && response.Content != "")
                {
                    // Deserializar el contenido de la respuesta
                    var jsonRespuesta = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    // Extraer el valor del campo 'response'
                    return jsonRespuesta?.response?.ToString() ?? "Respuesta no contiene el campo 'response'.";
                }
                else
                {
                    // Manejar el error de la solicitud HTTP
                    return $"Error en la solicitud: {response.StatusCode} - {response.StatusDescription}";
                }


            }
            catch (Exception e)
            {
                string a = e.Message;
                // Manejar excepciones
                return null;
            }
        }
        private string ObtenerAnalisisSincrono3(object datosAnalisis)
        {
            try
            {
                var client = new RestClient("http://172.26.54.111:5000");

                //var url = "http://172.26.54.111:5000/api/call_groq_api32q"; // Asegúrate de que la URL sea correcta
                var request = new RestRequest("/api/call_groq_api32q", Method.POST)
                {
                    Timeout = -1 // Sin límite de tiempo
                };

                // Serializar el objeto datosAnalisis a JSON
                var jsonBody = JsonConvert.SerializeObject(datosAnalisis);

                // Agregar encabezados y cuerpo a la solicitud
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

                // Ejecutar la solicitud de forma síncrona
                var response = client.Execute(request);

                // Verificar si la respuesta es exitosa
                if (response.Content != null && response.Content != "")
                {
                    // Deserializar el contenido de la respuesta
                    var jsonRespuesta = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    // Extraer el valor del campo 'response'
                    return jsonRespuesta?.response?.ToString() ?? "Respuesta no contiene el campo 'response'.";
                }
                else
                {
                    // Manejar el error de la solicitud HTTP
                    return $"Error en la solicitud: {response.StatusCode} - {response.StatusDescription}";
                }


            }
            catch (Exception e)
            {
                string a = e.Message;
                // Manejar excepciones
                return null;
            }
        }

        private string ObtenerAnalisisSincrono4(object datosAnalisis)
        {
            try
            {
                var client = new RestClient("http://172.26.54.111:5000");

                //var url = "http://172.26.54.111:5000/api/call_grok"; // Asegúrate de que la URL sea correcta
                var request = new RestRequest("/api/call_grok_api", Method.POST)
                {
                    Timeout = -1 // Sin límite de tiempo
                };

                // Serializar el objeto datosAnalisis a JSON
                var jsonBody = JsonConvert.SerializeObject(datosAnalisis);

                // Agregar encabezados y cuerpo a la solicitud
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

                // Ejecutar la solicitud de forma síncrona
                var response = client.Execute(request);

                // Verificar si la respuesta es exitosa
                if (response.Content != null && response.Content != "")
                {
                    // Deserializar el contenido de la respuesta
                    var jsonRespuesta = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    // Extraer el valor del campo 'response'
                    return jsonRespuesta?.response?.ToString() ?? "Respuesta no contiene el campo 'response'.";
                }
                else
                {
                    // Manejar el error de la solicitud HTTP
                    return $"Error en la solicitud: {response.StatusCode} - {response.StatusDescription}";
                }


            }
            catch (Exception e)
            {
                string a = e.Message;
                // Manejar excepciones
                return null;
            }
        }
        private string ObtenerAnalisisSincrono2(object datosAnalisis)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var url = "http://172.26.54.113:5000/api/call_groq_api322"; // Asegúrate de que la URL sea correcta

                    var jsonContent = JsonConvert.SerializeObject(datosAnalisis);
                    var contentString = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Realizar la solicitud de forma síncrona
                    var response = client.PostAsync(url, contentString).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var respuesta = response.Content.ReadAsStringAsync().Result;
                        var jsonRespuesta = JObject.Parse(respuesta);

                        return jsonRespuesta["response"].Value<string>();

                    }
                    else
                    {
                        // Manejar el error de la solicitud HTTP
                        return null;
                    }
                }
            }
            catch
            {
                // Manejar excepciones
                return null;
            }
        }
        public JsonResult EmployeesListjson2(string tipo)
        {

            List<Models.EmpleadoEnPrueba> lstEmployees = new List<Models.EmpleadoEnPrueba>();
            try
            {
                try
                {
                    var client = new RestClient("http://172.26.54.66/apihcm/api/evaluacion/obtenerTodas");
                    var request = new RestRequest(Method.GET);
                    request.Timeout = -1;


                    var resultExpensesx = client.Execute(request);
                    //Console.WriteLine(response.Content);
                    //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                    if (resultExpensesx != null)
                    {
                        var serializer = new JavaScriptSerializer();
                        serializer.MaxJsonLength = 500000000;

                        lstEmployees = serializer.Deserialize<List<Models.EmpleadoEnPrueba>>(resultExpensesx.Content);
                    }
                }
                catch (Exception e) { }

                return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet); ;

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult EmployeesListjson1(string tipo)
        {

            List<Models.EmpleadoEnPrueba> lstEmployees = new List<Models.EmpleadoEnPrueba>();
            try
            {
                try
                {
                    var client = new RestClient("http://172.26.54.66/apihcm/api/evaluacion/obtenerTodas");
                    var request = new RestRequest(Method.GET);
                    request.Timeout = -1;


                    var resultExpensesx = client.Execute(request);
                    //Console.WriteLine(response.Content);
                    //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                    if (resultExpensesx != null)
                    {
                        var serializer = new JavaScriptSerializer();
                        serializer.MaxJsonLength = 500000000;

                        lstEmployees = serializer.Deserialize<List<Models.EmpleadoEnPrueba>>(resultExpensesx.Content);
                    }
                }
                catch (Exception e) { }

                return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet); ;

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult EmployeesListjsonBaja(string tipo)
        {
            List<emp2024> lstEmployees = new List<emp2024>();

            try
            {
                // Crear cliente para la API
                var client = new RestClient("http://172.26.54.66/apihcm/api/formulariomaster/Employee?Tipoconsulta=BajasSinEvaluacion");
                var request = new RestRequest(Method.GET);
                request.Timeout = -1; // Sin límite de tiempo, ajusta si es necesario

                // Ejecutar la solicitud
                var response = client.Execute(request);

                // Si se obtuvo respuesta
                if (response != null && response.Content!=null)
                {
                    // Deserializar el JSON obtenido de la API
                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = int.MaxValue; // Ajustar longitud máxima si es necesario

                    lstEmployees = serializer.Deserialize<List<emp2024>>(response.Content);
                    Session["listabaja"] = lstEmployees;
                }
                else
                {
                    return Json(new { success = false, message = "Error en la solicitud a la API." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            // Retornar el resultado en formato JSON
            return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult EmployeesListjsonnuevo(string tipo)
        {
            List<emp2024> lstEmployees = new List<emp2024>();

            try
            {
                // Crear cliente para la API
                var client = new RestClient("http://172.26.54.66/apihcm/api/formulariomaster/Employee?Tipoconsulta=nuevosin");
                var request = new RestRequest(Method.GET);
                request.Timeout = -1; // Sin límite de tiempo, ajusta si es necesario

                // Ejecutar la solicitud
                var response = client.Execute(request);

                // Si se obtuvo respuesta
                if (response != null && response.Content != null)
                {
                    // Deserializar el JSON obtenido de la API
                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = int.MaxValue; // Ajustar longitud máxima si es necesario

                    lstEmployees = serializer.Deserialize<List<emp2024>>(response.Content);
                    Session["listanuevo"] = lstEmployees;
                }
                else
                {
                    return Json(new { success = false, message = "Error en la solicitud a la API." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            // Retornar el resultado en formato JSON
            return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Encuestabaja(int idEmpleado)
        {
            // Guardar el ID del empleado en sesión
            Session["EmpleadoID"] = idEmpleado;

            // Redirigir a la página donde se cargará la encuesta de baja
            return RedirectToAction("Baja");
        }
        public ActionResult Encuestanuevo(int idEmpleado)
        {
            // Guardar el ID del empleado en sesión
            Session["EmpleadoID"] = idEmpleado;

            // Redirigir a la página donde se cargará la encuesta de baja
            return RedirectToAction("Nuevo");
        }
        public ActionResult empleadobaja()
        { return View(); }
        public ActionResult empleadonuevo()
        { return View(); }
        // Acción para cargar la vista de baja del empleado, donde se mostrará la encuesta
        public ActionResult Baja()
        {
            // Obtener el ID del empleado desde la sesión (si se requiere usar en la vista)
            int? empleadoID = Session["EmpleadoID"] as int?;

            if (empleadoID != null)
            {
                // Aquí puedes cargar datos adicionales del empleado, si es necesario
                // Ejemplo: ViewBag.Empleado = Servicio.ObtenerEmpleadoPorID(empleadoID.Value);
            }

            // Cargar la vista de baja del empleado
            return View();
        }
        public ActionResult Nuevo()
        {
            // Obtener el ID del empleado desde la sesión (si se requiere usar en la vista)
            int? empleadoID = Session["EmpleadoID"] as int?;

            if (empleadoID != null)
            {
                // Aquí puedes cargar datos adicionales del empleado, si es necesario
                // Ejemplo: ViewBag.Empleado = Servicio.ObtenerEmpleadoPorID(empleadoID.Value);
            }

            // Cargar la vista de baja del empleado
            return View();
        }
        public JsonResult EmployeesListjsontodo()
        {

            List<Models.EMPGENERALX> lstEmployees = new List<Models.EMPGENERALX>();
            try
            {
                try
                {
                    var client = new RestClient("http://172.26.54.66/apihcm/api/values/Colaborador/Employeeshcmx");
                    var request = new RestRequest(Method.GET);
                    request.Timeout = -1;


                    var resultExpensesx = client.Execute(request);
                    //Console.WriteLine(response.Content);
                    //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                    if (resultExpensesx != null)
                    {
                        var serializer = new JavaScriptSerializer();
                        serializer.MaxJsonLength = 500000000;

                        lstEmployees = serializer.Deserialize<List<Models.EMPGENERALX>>(resultExpensesx.Content);
                    }
                }
                catch (Exception e) { }

                return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet); ;

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult EmployeesListjsontodox()
        {

            List<EvaluacionCicloViewModel> lstEmployees = new List<EvaluacionCicloViewModel>();
            try
            {
                try
                {
                    var client = new RestClient("http://172.26.54.66/apihcm/api/evaluacion/obtenerTodascarnet");
                    var request = new RestRequest(Method.GET);
                    request.Timeout = -1;


                    var resultExpensesx = client.Execute(request);
                    //Console.WriteLine(response.Content);
                    //var resultExpenses = await ClaroWCF.GetEmployeesByBossToExpensesAsync(eEmployee.Id_HRMS.ToString());

                    if (resultExpensesx != null)
                    {
                        var serializer = new JavaScriptSerializer();
                        serializer.MaxJsonLength = 500000000;

                        lstEmployees = serializer.Deserialize<List<EvaluacionCicloViewModel>>(resultExpensesx.Content);
                        Session["levaluaciontotal"] = lstEmployees;

                    }
                }
                catch (Exception e) { }

                return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet); ;

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            var listaReportes = new List<ReporteQFlow>();

            if (file != null && file.ContentLength > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(file.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var reporte = new ReporteQFlow
                        {
                            Numero = int.Parse(worksheet.Cells[row, 1].Text),
                            Carnet = worksheet.Cells[row, 2].Text,
                            CarnetJefe = worksheet.Cells[row, 3].Text,
                            PuestoActual = worksheet.Cells[row, 4].Text,
                            GerenciaOrigen = worksheet.Cells[row, 5].Text,
                            FechaInicioPeriodoPrueba = DateTime.Parse(worksheet.Cells[row, 6].Text),
                            PuestoEnPeriodoPrueba = worksheet.Cells[row, 7].Text,
                            GerenciaNueva = worksheet.Cells[row, 8].Text,
                            Meses = int.Parse(worksheet.Cells[row, 9].Text)
                        };

                        listaReportes.Add(reporte);
                    }
                }
            }
            Session["Levalua"] = listaReportes;

            return Json(new { success = true, data = listaReportes });
        }
        [HttpPost]
        public ActionResult CrearEvaluaciones2()
        {
            string falla = "";
            var listaReportes = Session["Levalua"] as List<ReporteQFlow>;
            if (listaReportes == null || listaReportes.Count == 0)
            {
                return Json(new { success = false, message = "No hay evaluaciones para procesar." });
            }

            foreach (var q in listaReportes)
            {
                var evaluacion = new
                {
                    Numero = 0,
                    Carnet = q.Carnet,
                    CarnetJefe = q.CarnetJefe,
                    PuestoActual = q.PuestoActual,
                    GerenciaOrigen = q.GerenciaOrigen, // No necesario, lo dejamos vacío
                    FechaInicioPeriodoPrueba = q.FechaInicioPeriodoPrueba, // No necesario, lo dejamos null
                    PuestoEnPeriodoPrueba = q.PuestoEnPeriodoPrueba, // No necesario, lo dejamos vacío
                    GerenciaNueva = q.GerenciaNueva, // No necesario, lo dejamos vacío
                    Meses = q.Meses // No necesario, lo dejamos vacío
 
                };
                var jsonBody = JsonConvert.SerializeObject(evaluacion);

                // Aquí realizas la llamada a la API para cada evaluación.
                var client = new RestClient("http://172.26.54.66/apihcm/api/evaluacion/crear2");
                var request = new RestRequest(Method.POST);
                request.Timeout = -1;
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);
                var response = client.Execute(request);
                if (response.Content != "\"Evaluación creada exitosamente.\"")
                {
                    if (falla=="")
                    {
                        falla = "registro fallado:" + q.Carnet;
                    }
                    else
                    {
                        falla = falla+ "," + q.Carnet;

                    }
                 }
                else { }
            }

            // Limpiar la sesión después de procesar
            Session.Remove("ListaReportes");

            return Json(new { success = true, message = "Evaluaciones creadas correctamente y registro con falla:"+ falla });
        }
        [HttpPost]
        public JsonResult Crearevaluacionz(string Carnet, DateTime FechaInicio, int CantidadMeses)
        {
            try
            {
                // Crear el objeto que se enviará en el cuerpo de la solicitud (body)
                var evaluacion = new
                {
                    IdEvaluacion = 0,
                    CodigoEmpleado = Carnet,
                    FechaInicio = FechaInicio,
                    CantidadMeses = CantidadMeses,
                    Estado = string.Empty, // No necesario, lo dejamos vacío
                    FechaFinalizacion = (DateTime?)null, // No necesario, lo dejamos null
                    PDFComprobante = string.Empty // No necesario, lo dejamos vacío
                };

                // Serializar el objeto a JSON
                var jsonBody = JsonConvert.SerializeObject(evaluacion);

                // Configurar el cliente y la solicitud REST
                var client = new RestClient("http://172.26.54.66/apihcm/api/evaluacion/crear");
                var request = new RestRequest(Method.POST);
                request.Timeout = -1;
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

                // Ejecutar la solicitud y obtener la respuesta
                var response = client.Execute(request);

                if (response.Content == "Evaluación creada exitosamente")
                {
                    return Json(new { success = true, message = "Evaluación creada exitosamente." }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Error al crear la evaluación: " + response.ErrorMessage }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult CrearEvaluaciones(List<ReporteQFlow> evaluaciones)
        {
            // Aquí puedes realizar la lógica para crear las evaluaciones con los datos recibidos.
            // Por ejemplo, podrías iterar sobre la lista y enviar cada evaluación a una API o guardarlas en la base de datos.

            return Json(new { success = true, message = "Evaluaciones creadas correctamente." });
        }
        public ActionResult EnviarEvaluacion(int idCiclo)
        {
            List<EvaluacionCicloViewModel> lstEmployees = Session["levaluaciontotal"] as List<EvaluacionCicloViewModel>;
            List<EvaluacionCicloViewModel> lstEmployees2 = new List<EvaluacionCicloViewModel>() ;
              lstEmployees2 = lstEmployees.Where(e => e.IdCiclo == idCiclo).ToList();
            Session["levaluaciontotalx"] = lstEmployees2;
            // Lógica para enviar la evaluación
            // Puedes realizar operaciones en la base de datos o cualquier otra lógica de negocio aquí

            // Redirigir a una página específica después de enviar
            return RedirectToAction("ProcesoAscenso");
        }
        [HttpPost]
        public JsonResult EnviarEvaluacion(  List<ObjetivoEvaluacion> objetivos)

        {
            try
            {

                List<EvaluacionCicloViewModel> lstEmployees = Session["levaluaciontotalx"] as List<EvaluacionCicloViewModel>;
                EvaluacionCicloViewModel evaluacion = new EvaluacionCicloViewModel();
                evaluacion = lstEmployees.FirstOrDefault();

                string resultadoInventario = string.Empty;

                foreach (var objetivo in objetivos)
                {
                    using (var dapperDatos = new DapperDatos())
                    {
                        var parameters = new DynamicParameters();
                            parameters.Add("@TipoConsulta", "INSERTAR_OBJETIVO");
                            parameters.Add("@IdCiclo", evaluacion.IdCiclo);
                            parameters.Add("@DescripcionObjetivo", objetivo.DescripcionObjetivo);
                            parameters.Add("@PorcentajeCumplimientoEsperado", objetivo.PorcentajeCumplimientoEsperado);

                             parameters.Add("@Response", string.Empty, DbType.String, direction: ParameterDirection.Output);
                            dapperDatos.IniciarTransaccion();
                            resultadoInventario = dapperDatos.GuardarTransaccion("sp_Comp_GestionarEvaluacion", parameters);

                            
                      
                        if (resultadoInventario != "ok")
                        {
                            dapperDatos.DeshacerTransaccion();
                        }
                        else
                        {
                            dapperDatos.ConfirmarTransaccion();
                        }
                    }
                }
                return Json(new { success = true });
           
             }
            catch (Exception ex)
            {
                // Manejo de errores
                return Json(new { success = false, message = ex.Message });
            }
        }
        public ActionResult EYR()
        {
          //List<ObjetivoEvaluacion> objetivos = Session["objetivosEvaluacion"] as List<ObjetivoEvaluacion> 
        
            return View();
        }
        [HttpPost]
        public ActionResult GuardarEvaluacionRetro(FormCollection form)
        {
            try
            {
                // Obtener el IdCiclo de la sesión
                EvaluacionCicloViewModel evaluacion = Session["evaluacionActual"] as EvaluacionCicloViewModel;
                int idCiclo = evaluacion.IdCiclo;

                // Iterar sobre los objetivos
                var objetivos = new List<ObjetivoEvaluacion>();
                for (int i = 0; i < 6; i++)
                {
                    if (!string.IsNullOrWhiteSpace(form[$"descripcionObjetivo{i}"]) && !string.IsNullOrWhiteSpace(form[$"cumplimientoReal{i}"]))
                    {
                        objetivos.Add(new ObjetivoEvaluacion
                        {
                            IdCiclo = idCiclo,
                            DescripcionObjetivo = form[$"descripcionObjetivo{i}"],
                            PorcentajeCumplimientoReal = int.Parse(form[$"cumplimientoReal{i}"])
                        });
                    }
                }

                // Guardar los objetivos en la base de datos usando Dapper
                //using (DapperDatos dapperDatos = new DapperDatos())
                //{
                //    foreach (var objetivo in objetivos)
                //    {
                //        DynamicParameters param = new DynamicParameters();
                //        param.Add("@TipoConsulta", "INSERTAR_OBJETIVO");
                //        param.Add("@IdCiclo", objetivo.IdCiclo);
                //        param.Add("@DescripcionObjetivo", objetivo.DescripcionObjetivo);
                //        param.Add("@PorcentajeCumplimientoReal", objetivo.PorcentajeCumplimientoReal);

                //        dapperDatos.EjecutarComando("sp_Comp_GestionarEvaluacion", param);
                //    }
                //}

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public ActionResult EnviarEvaluacion2(int idCiclo)
        {
            List<EvaluacionCicloViewModel> lstEmployees = Session["levaluaciontotal"] as List<EvaluacionCicloViewModel>;
            List<EvaluacionCicloViewModel> lstEmployees2 = new List<EvaluacionCicloViewModel>();
            lstEmployees2 = lstEmployees.Where(e => e.IdCiclo == idCiclo).ToList();
            Session["levaluaciontotalx"] = lstEmployees2;
            // Lógica para enviar la evaluación
            // Puedes realizar operaciones en la base de datos o cualquier otra lógica de negocio aquí

            // Redirigir a una página específica después de enviar
            return RedirectToAction("EnviarEvaluacionobjetivo");
        }
        [HttpPost]
        public ActionResult TuAccion([System.Web.Http.FromBody] EvaluacionYRetroalimentacionModel evaluacion)
        {
            try
            {
                List<EvaluacionCicloViewModel> lstEmployees = Session["levaluaciontotalx"] as List<EvaluacionCicloViewModel>;
                EvaluacionCicloViewModel t1 = new EvaluacionCicloViewModel();
                t1 = lstEmployees.FirstOrDefault();

                 List<ObjetivoEvaluacion> objetivos = Session["objetivosEvaluacion"] as  List<ObjetivoEvaluacion> ;
                 List<Comp_Habilidades> habilidad = Session["Heva"] as List<Comp_Habilidades> ;
                string resultadoInventario = string.Empty;

                foreach (var objetivo in evaluacion.Objetivos)
                {
                    ObjetivoEvaluacion temp1 = new ObjetivoEvaluacion();
                    temp1 = objetivos.Where(x => x.DescripcionObjetivo == objetivo.DescripcionObjetivo).FirstOrDefault();
                    temp1.PorcentajeCumplimientoReal = objetivo.PorcentajeCumplimientoReal;


                    using (var dapperDatos = new DapperDatos())
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@TipoConsulta", "ACTUALIZAR_OBJETIVO");
                        parameters.Add("@IdObjetivo", temp1.IdObjetivo);
                         parameters.Add("@PorcentajeCumplimientoReal", objetivo.PorcentajeCumplimientoReal );

                        parameters.Add("@Response", string.Empty, DbType.String, direction: ParameterDirection.Output);
                        dapperDatos.IniciarTransaccion();
                        resultadoInventario = dapperDatos.GuardarTransaccion("sp_Comp_GestionarEvaluacion", parameters);



                        if (resultadoInventario != "ok")
                        {
                            dapperDatos.DeshacerTransaccion();
                        }
                        else
                        {
                            dapperDatos.ConfirmarTransaccion();
                        }
                    }
                }

                foreach (var hab in evaluacion.Habilidades)
                {
                    Comp_Habilidades temp1 = new Comp_Habilidades();
                    temp1 = habilidad.Where(x => x.Habilidad == hab.Habilidad).FirstOrDefault();
                    temp1.Puntuacion = hab.Puntuacion;


                    using (var dapperDatos = new DapperDatos())
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@TipoConsulta", "ACTUALIZAR_RESULTADO");
                        parameters.Add("@IdResultadoHabilidad", temp1.IdResultadoHabilidad);
                        parameters.Add("@Puntuacion", temp1.Puntuacion);

                        parameters.Add("@Response", string.Empty, DbType.String, direction: ParameterDirection.Output);
                        dapperDatos.IniciarTransaccion();
                        resultadoInventario = dapperDatos.GuardarTransaccion("sp_Comp_GestionarEvaluacion", parameters);



                        if (resultadoInventario != "ok")
                        {
                            dapperDatos.DeshacerTransaccion();
                        }
                        else
                        {
                            dapperDatos.ConfirmarTransaccion();
                        }
                    }
                }
                using (var dapperDatos = new DapperDatos())
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@TipoConsulta", "INSERTAR_EVALUACION_GLOBAL");
                    parameters.Add("@IdCiclo", t1.IdCiclo);
                    parameters.Add("@EvaluacionGlobal", evaluacion.EvaluacionGlobal);
                    parameters.Add("@Fortalezas", evaluacion.Fortalezas);
                    parameters.Add("@Debilidades", evaluacion.Debilidades);
                    parameters.Add("@Acciones", evaluacion.AccionesASeguir);
                   

                    parameters.Add("@Response", string.Empty, DbType.String, direction: ParameterDirection.Output);
                    dapperDatos.IniciarTransaccion();
                    resultadoInventario = dapperDatos.GuardarTransaccion("sp_Comp_GestionarEvaluacion", parameters);



                    if (resultadoInventario != "ok")
                    {
                        dapperDatos.DeshacerTransaccion();
                    }
                    else
                    {
                        dapperDatos.ConfirmarTransaccion();
                    }
                }
                string correo = GenerarCorreo(evaluacion, t1);


                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Manejar errores
                return Json(new { success = false, message = ex.Message });
            }
        }

      
        public string GenerarCorreo(EvaluacionYRetroalimentacionModel evaluacion, EvaluacionCicloViewModel ciclo)
        {
            var puntuacionHabilidadesMap = new Dictionary<string, string>
{
    { "A", "Por debajo de lo requerido" },
    { "B", "Ligeramente por debajo de lo requerido" },
    { "C", "Igual que lo requerido" },
    { "D", "Mejor que lo requerido" },
    { "E", "Mucho mejor que lo requerido" }
};
            // Mapeo de puntuaciones a descripciones
            var puntuacionMap = new Dictionary<string, string>
    {
        {"A", "No cumple con lo requerido en el puesto"},
        {"B", "Requiere de un mayor esfuerzo para cumplir con las responsabilidades de su puesto"},
        {"C", "Cumple con lo requerido en el puesto de manera adecuada"},
        {"D", "Cumple con lo requerido en el puesto de manera adecuada y propone mejoras que enriquecen los procesos de su área"},
        {"E", "Supera ampliamente las expectativas en su puesto"}
    };

            // Información general del ciclo y el colaborador
            string correo = $@"
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; }}
            table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
            table, th, td {{ border: 1px solid #dddddd; padding: 8px; text-align: left; }}
            th {{ background-color: #f2f2f2; }}
            h2, h3 {{ color: #333333; }}
        </style>
    </head>
    <body>
        <p>Estimado <strong>{ciclo.colaborador}</strong> con el cargo {ciclo.Cargo2},</p>
        <p>A continuación se detalla la evaluación de su desempeño correspondiente al periodo <strong>{ciclo.MesCiclo}-{ciclo.FechaInicio.Year}</strong>:</p>

        <h2>Evaluación de Habilidades</h2>
        <table>
            <tr>
                <th>Habilidad</th>
                <th>Evaluación</th>
            </tr>";

            foreach (var hab in evaluacion.Habilidades)
            {
                if (puntuacionHabilidadesMap.TryGetValue(hab.Puntuacion, out string descripcionPuntuacion))
                {
                    correo += $"<tr><td>{hab.Habilidad}</td><td>{descripcionPuntuacion}</td></tr>";
                }
                else
                {
                    correo += $"<tr><td>{hab.Habilidad}</td><td>{hab.Puntuacion}</td></tr>";
                }
            }

            correo += $@"
        </table>

        <h2>Evaluación Global</h2>
        <p><strong>{puntuacionMap[evaluacion.EvaluacionGlobal]}</strong></p>

        <h2>Cumplimiento de Objetivos</h2>
        <table>
            <tr>
                <th>Objetivo</th>
                <th>% de Cumplimiento Esperado</th>
                <th>% de Cumplimiento Real</th>
            </tr>";

            foreach (var obj in evaluacion.Objetivos)
            {
                correo += $"<tr><td>{obj.DescripcionObjetivo}</td><td>{obj.PorcentajeCumplimientoEsperado}%</td><td>{obj.PorcentajeCumplimientoReal}%</td></tr>";
            }

            correo += $@"
        </table>

        <h2>Retroalimentación</h2>
        <p><strong>Fortalezas:</strong> {evaluacion.Fortalezas}</p>
        <p><strong>Debilidades:</strong> {evaluacion.Debilidades}</p>
        <p><strong>Acciones a Seguir:</strong> {evaluacion.AccionesASeguir}</p>

        <p>Agradecemos su dedicación y esfuerzo. Por favor, revise esta evaluación detenidamente y no dude en contactar a su superior inmediato si tiene alguna duda o comentario.</p>
        <p>Saludos cordiales,<br>Equipo de Recursos Humanos</p>
    </body>
    </html>";
            //var emailService = new EmailService("10.200.5.23", 25, "compensacion@claro.com.ni");

            //string resultado = emailService.SendEmail(
            //    toAddress: "gustavo.lira@claro.com.ni",
            //    subject: "Evaluación Enviada",
            //    body: correo,
 
            //    ccAddress: "gustavo.lira@claro.com.ni"
            //);
            string respuesta = ClaroWCF.getcorreoenviar("nathalia.quintana@claro.com.ni", "Evaluación Enviada", correo);
            return respuesta;
        }

        [HttpPost]
        public JsonResult ConfirmarAscenso(string empleadoNo, string nombre)
        {
            try
            {
                // Aquí puedes agregar la lógica para registrar la confirmación en la base de datos
                // Por ejemplo, puedes llamar a un servicio que guarde esta información

                // Simulación de guardado exitoso
                bool guardadoExitoso = true;

                if (guardadoExitoso)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return Json(new { success = false, message = ex.Message });
            }
        }
        public ActionResult ProcesoAscenso  ()
        { return View();
        }
        // 2. Evaluar
        public ActionResult Evaluar(int idCiclo)
        {
            // Lógica para evaluar
            // Puedes realizar operaciones en la base de datos o cualquier otra lógica de negocio aquí
            List<EvaluacionCicloViewModel> lstEmployees = Session["levaluaciontotal"] as List<EvaluacionCicloViewModel>;
            List<EvaluacionCicloViewModel> lstEmployees2 = new List<EvaluacionCicloViewModel>();
            List<Comp_Habilidades> habilidad = new List<Comp_Habilidades>();
            lstEmployees2 = lstEmployees.Where(e => e.IdCiclo == idCiclo).ToList();
            Session["levaluaciontotalx"] = lstEmployees2;
            List<ObjetivoEvaluacion> objetivos = new List<ObjetivoEvaluacion>();
            EvaluacionCicloViewModel evaluacion = Session["evaluacionActual"] as EvaluacionCicloViewModel;

            using (DapperDatos dapperDatos = new DapperDatos())
            {
                try
                {
                    DynamicParameters param = new DynamicParameters();
                    param.Add("@TipoConsulta", "SELECT_OBJETIVO");
                    param.Add("@IdCiclo", idCiclo);
                    
                    dapperDatos.IniciarTransaccion();
                    objetivos = dapperDatos.CargarDatosTransaccion<ObjetivoEvaluacion>("sp_Comp_GestionarEvaluacion", param);
                    dapperDatos.ConfirmarTransaccion();

                }
                catch (Exception e)
                {

                    dapperDatos.DeshacerTransaccion();
                    throw new Exception(e.Message);
                }
            }
            using (DapperDatos dapperDatos = new DapperDatos())
            {
                try
                {
                    DynamicParameters param = new DynamicParameters();
                    param.Add("@TipoConsulta", "SELECT_HAB");
                    param.Add("@IdCiclo", idCiclo);

                    dapperDatos.IniciarTransaccion();
                    habilidad = dapperDatos.CargarDatosTransaccion<Comp_Habilidades>("sp_Comp_GestionarEvaluacion", param);
                    dapperDatos.ConfirmarTransaccion();

                }
                catch (Exception e)
                {

                    dapperDatos.DeshacerTransaccion();
                    throw new Exception(e.Message);
                }
            }
            
            
            Session["objetivosEvaluacion"] = objetivos;
            Session["Heva"] = habilidad;
            // Redirigir a una página específica para realizar la evaluación
            return RedirectToAction("EYR");
        }

        // 3. Descargar PDF
        public FileResult DescargarPDF(int idCiclo)
        {
            // Lógica para generar o obtener el PDF
            // Aquí deberías obtener el archivo PDF desde el servidor o generarlo dinámicamente

            string filePath = Server.MapPath("~/path/to/pdf/file.pdf"); // Reemplaza con la ruta del archivo PDF
            string fileName = "Evaluacion_" + idCiclo + ".pdf"; // Nombre del archivo para la descarga

            return File(filePath, "application/pdf", fileName);
        }

        // 4. Subir PDF
        [HttpPost]
        public ActionResult SubirPDF(int idCiclo, HttpPostedFileBase pdfFile)
        {
            List<EvaluacionCicloViewModel> lstEmployees = Session["levaluaciontotal"] as List<EvaluacionCicloViewModel>;
            List<EvaluacionCicloViewModel> lstEmployees2 = new List<EvaluacionCicloViewModel>();
            lstEmployees2 = lstEmployees.Where(e => e.IdCiclo == idCiclo).ToList();
            Session["levaluaciontotalx"] = lstEmployees2;
            if (pdfFile != null && pdfFile.ContentLength > 0)
            {
                // Lógica para guardar el PDF en el servidor
                // Puedes guardar el archivo en una carpeta específica y registrar la información en la base de datos

                string path = Server.MapPath("~/path/to/upload/folder/");
                string fileName = "Evaluacion_" + idCiclo + ".pdf";
                string fullPath = System.IO.Path.Combine(path, fileName);

                pdfFile.SaveAs(fullPath);

                // Redirigir o mostrar un mensaje de éxito después de subir el archivo
                ViewBag.Message = "PDF subido exitosamente.";
            }

            return RedirectToAction("Retroalimentacion" );
        }
        #region insertar evaluacion baja y estado
        public int InsertarEvaluacionbaja(Evaluacion2 evaluacion)
        {
            using (var connection = new SqlConnection(strConnection1))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insertar Evaluacion
                        var insertEvaluacionQuery = @"
                    INSERT INTO [dbo].[Evaluacion] 
                    (UsuarioID, FechaAsignacion, Estado, FechaFinalizacion, CargoAnterior, NuevoCargo, JefeID, GerenciaAnterior, NuevaGerencia, ProcesoID, EstadoEvaluacion, CantidadCiclosPlaneados, IA)
                    VALUES 
                    (@UsuarioID, @FechaAsignacion, @Estado, @FechaFinalizacion, @CargoAnterior, @NuevoCargo, @JefeID, @GerenciaAnterior, @NuevaGerencia, @ProcesoID, @EstadoEvaluacion, @CantidadCiclosPlaneados, @IA);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                        var evaluacionID = connection.QuerySingle<int>(insertEvaluacionQuery, evaluacion, transaction);

                        // Actualizar estados anteriores a 'N' en EstadoEvaluacion
                        var updateEstadoEvaluacionQuery = @"
                    UPDATE [dbo].[EstadoEvaluacion]
                    SET Activo = 'N'
                    WHERE EvaluacionID = @EvaluacionID
                    AND Activo = 'Y';";

                        connection.Execute(updateEstadoEvaluacionQuery, new { EvaluacionID = evaluacionID }, transaction);

                        // Insertar nuevo EstadoEvaluacion con Activo = 'Y'
                        var insertEstadoEvaluacionQuery = @"
                    INSERT INTO [dbo].[EstadoEvaluacion] 
                    (EvaluacionID, UsuarioID, Estado, Fecha, Activo)
                    VALUES 
                    (@EvaluacionID, @UsuarioID, @Estado, @Fecha, 'Y');";

                        var newEstadoEvaluacion = new
                        {
                            EvaluacionID = evaluacionID,
                            UsuarioID = evaluacion.UsuarioID,
                            Estado =8, // Asegúrate de que este campo corresponda al ID correcto de CatalogoEstado
                            Fecha = DateTime.Now
                        };

                        connection.Execute(insertEstadoEvaluacionQuery, newEstadoEvaluacion, transaction);

                        transaction.Commit();
                        return evaluacionID;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void InsertarRespuestasrespuestabaja(List<RespuestaViewModel> respuestas, int cicloEvaluacionID)
        {
            using (var connection = new SqlConnection(strConnection1))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Actualizar estados anteriores a 'N' en EstadoCicloEvaluacion
                        var updateEstadoCicloEvaluacionQuery = @"
                    UPDATE [dbo].[EstadoCicloEvaluacion]
                    SET Activo = 'N'
                    WHERE CicloEvaluacionID = @CicloEvaluacionID
                    AND Activo = 'Y';";

                        connection.Execute(updateEstadoCicloEvaluacionQuery, new { CicloEvaluacionID = cicloEvaluacionID }, transaction);

                        // Insertar respuestas en la tabla Respuesta
                        var insertRespuestaQuery = @"
                    INSERT INTO [dbo].[Respuesta] 
                    (FormularioID, PreguntaID, UsuarioID, CicloEvaluacionID, TextoRespuesta, OpcionID, FechaRespuesta, MesCumplido, Mes, Año)
                    VALUES 
                    (@FormularioID, @PreguntaID, @UsuarioID, @CicloEvaluacionID, @TextoRespuesta, @OpcionID, @FechaRespuesta, @MesCumplido, @Mes, @Año);";

                        foreach (var respuesta in respuestas)
                        {
                            var now = DateTime.Now;

                            var parametros = new
                            {
                                respuesta.FormularioID,
                                respuesta.PreguntaID,
                                respuesta.UsuarioID,
                                CicloEvaluacionID = cicloEvaluacionID,
                                respuesta.TextoRespuesta,
                                respuesta.OpcionID,
                                FechaRespuesta = now,
                                MesCumplido = now.ToString("MMMM"),  // Mes completo en español
                                Mes = now.Month.ToString(),          // Número de mes
                                Año = now.Year.ToString()            // Año
                            };

                            connection.Execute(insertRespuestaQuery, parametros, transaction);
                        }

                        // Insertar nuevo EstadoCicloEvaluacion con Activo = 'Y'
                        var insertEstadoCicloEvaluacionQuery = @"
                    INSERT INTO [dbo].[EstadoCicloEvaluacion] 
                    (CicloEvaluacionID, UsuarioID, Estado, Fecha, Activo)
                    VALUES 
                    (@CicloEvaluacionID, @UsuarioID, @Estado, @Fecha, 'Y');";

                        // Asumiendo que todas las respuestas tienen el mismo UsuarioID y Estado
                        var firstRespuesta = respuestas.FirstOrDefault();
                        if (firstRespuesta != null)
                        {
                            var newEstadoCicloEvaluacion = new
                            {
                                CicloEvaluacionID = cicloEvaluacionID,
                                UsuarioID = firstRespuesta.UsuarioID,
                                Estado = 8, // Asegúrate de que este campo corresponda al ID correcto de CatalogoEstado
                                Fecha = DateTime.Now
                            };

                            connection.Execute(insertEstadoCicloEvaluacionQuery, newEstadoCicloEvaluacion, transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        [HttpGet]
        public JsonResult ObtenerRespuestas(int? evaluacion_id = null, string carnet = null, int? proceso = null)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(strConnection1))
                {
                    string sql = "SELECT * FROM view_Fresp WHERE 1=1";

                    // Dinamizar los parámetros según los filtros proporcionados
                    if (evaluacion_id.HasValue)
                    {
                        sql += " AND EvaluacionID = @EvaluacionID";
                    }

                    if (!string.IsNullOrEmpty(carnet))
                    {
                        sql += " AND carnet = @Carnet";
                    }

                    if (proceso.HasValue)
                    {
                        sql += " AND ProcesoID = @ProcesoID";
                    }

                    var parametros = new DynamicParameters();

                    if (evaluacion_id.HasValue)
                    {
                        parametros.Add("@EvaluacionID", evaluacion_id.Value, DbType.Int32);
                    }

                    if (!string.IsNullOrEmpty(carnet))
                    {
                        parametros.Add("@Carnet", carnet, DbType.String);
                    }

                    if (proceso.HasValue)
                    {
                        parametros.Add("@ProcesoID", proceso.Value, DbType.Int32);
                    }

                    var respuestas = db.Query<Respuestavm>(sql, parametros).AsList();

                    return Json(new { success = true, data = respuestas }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Loguea el error según tu mecanismo de logging preferido
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }

}
