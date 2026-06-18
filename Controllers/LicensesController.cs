using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Dapper;
using DevExpress.Data;
using DevExpress.Web;
using DevExpress.Web.Mvc;
using OfficeOpenXml;
using slnRhonline.Models;
using slnRhonline.Reports;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using Newtonsoft.Json;
using System.Net.Http;
using RestSharp;
using System.Net;
using Newtonsoft.Json.Linq;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class LicensesController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIGHO1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos
        private const string UsuarioHcm = "Claro_Jira_Emp_WS_SS";
        private const string PasswordHcm = "HCM-J1r@EmP_Int.#$";

        
        const string keyModelBoss = "gvAuthorizeBossLicense";
        const string keyModelConsult = "gvConsult";
        const string keyModelRh = "gvAuthorizeRh";
        const string keyPersonLicense = "sPersonIdLicense";

        //Declaracion de constantes
        const string keyViewModel = "gvEmployees";




        #region Adjunto del Archivo de esquela

        public ActionResult ModificarExcel()
        {
            string originalFilePath = Server.MapPath("~/App_Data/ProgramacionVacaciones.xlsx");

            // Verificar si el archivo original existe
            if (!System.IO.File.Exists(originalFilePath))
            {
                return new HttpNotFoundResult("El archivo original no se encontró.");
            }

            // Generar un nombre único (código aleatorio de 8-10 caracteres)
            string uniqueCode = GenerateUniqueCode(8); // Cambia el 8 si necesitas un código más largo

            // Abrir el archivo original y trabajar directamente en un flujo en memoria
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(originalFilePath)))
            {
                // Seleccionar la hoja donde se desea trabajar
                var worksheet = package.Workbook.Worksheets["FORMATO"];

                // Modificar contenido
                worksheet.Cells["B1"].Value = "GERENCIA"; // gerencia
                worksheet.Cells["B3"].Value = "SUBGERENCIA"; // subgerencia
                worksheet.Cells["B9"].Value = "CODIGO2"; // codigo o carnet
                worksheet.Cells["C9"].Value = "NOMBRE"; // nombre
                worksheet.Cells["D9"].Value = "AREA"; // area
                worksheet.Cells["E9"].Value = 22; // saldo
                worksheet.Cells["F9"].Value = "23/12/2024"; // fecha inicio
                worksheet.Cells["G9"].Value = "5/1/2025"; // fecha fin
                worksheet.Cells["H9"].Value = "15"; // "Días Propuestos"
                worksheet.Cells["I9"].Value = "7"; //Saldo
                worksheet.Cells["J9"].Value = "0"; // % COMPLETADO



                // Guardar los cambios en memoria
                using (var memoryStream = new MemoryStream())
                {
                    package.SaveAs(memoryStream);
                    memoryStream.Position = 0; // Reiniciar la posición del flujo

                    // Devolver el archivo como descarga
                    return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PlanificacionVacaciones_{uniqueCode}.xlsx");
                }
            }

            // Retornar un mensaje o redirigir después de guardar
        }

        public ActionResult ModificarExcelvacaciones(DateTime fechaInicio, DateTime fechaFin)
        {
            // Validar que las fechas sean correctas
            if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
            {
                return new HttpStatusCodeResult(400, "Rango de fechas inválido");
            }

            string originalFilePath = Server.MapPath("~/App_Data/ProgramacionVacaciones.xlsx");

            // Verificar si el archivo original existe
            if (!System.IO.File.Exists(originalFilePath))
            {
                return new HttpNotFoundResult("El archivo original no se encontró.");
            }

            // Consulta SQL con filtro por fechas
            string query = @"
        SELECT
            em.nombre_completo AS NombreCompleto,
            em.carnet AS Carnet,
            em.OGERENCIA AS Gerencia,
            em.primernivel AS PrimerNivel,
            em.cargo AS Cargo,
            ab.Comments AS Comentario,
            ab.CreatedBy AS Creador,
            NULLIF(ab.SubmittedDate, '') AS FechaEnvio,
            ab.StartDate AS FechaInicio,
            ab.StartTime AS HoraInicio,
            ab.EndDate AS FechaFin,
            ab.EndTime AS HoraFin,
            ab.UnitOfMeasureMeaning AS UnidadDeMedida,
            ab.Duration AS Duracion,
            ab.FormattedDuration AS DuracionFormateada,
            ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
            ab.ApprovalDatetime AS FechaDeAprobacion,
            lastUpdatedBy AS usuarioactualizo,
            lastUpdateDate AS fecha_Actualizo
        FROM emp2024 em
        INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
        WHERE
            (ab.AbsenceType in ('Vacaciones','Enfermedad grave de un miembro del núcleo familiar que viva bajo mismo techo','Licencia por adopción')) and    LegislationCode='NI' 
            AND ((ab.StartDate >= @fechaInicio AND ab.StartDate <= @fechaFin)
                 OR (ab.EndDate >= @fechaInicio AND ab.EndDate <= @fechaFin)) and AbsenceDispStatusMeaning='Programado' and AbsenceType='Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
        ORDER BY ab.SubmittedDate desc, em.carnet, ab.StartDate";

            try
            {
                List<EmpleadoDTO> temp = new List<EmpleadoDTO>();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query1 = @"
                SELECT 
                    em.carnet,
                    em.nombre_completo,
                    em.cargo,
                    em.primernivel AS Area,   em.OGERENCIA AS Gerencia,  em.OSUBGERENCIA AS SUBGerencia, em.primernivel AS area,
                    pl.BalanceAsOfBalanceCalculationDate AS Acumulado,telefonojefe,telefono,nom_jefe1,cargo_jefe1
                FROM [dbo].[EMP2024] em
                INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
                 ORDER BY pl.BalanceAsOfBalanceCalculationDate DESC";

                    temp= connection.Query<EmpleadoDTO>(query1).ToList();
                }
                using (var connection = new SqlConnection(connectionString))
                {
                    // Ejecutar la consulta y obtener los datos
                    var vacaciones = connection.Query<VacacionesPersona>(query, new { fechaInicio, fechaFin }).ToList();

                    // Validar si hay datos
                    if (vacaciones == null || !vacaciones.Any())
                    {
                        return new HttpStatusCodeResult(204, "No se encontraron datos para el rango de fechas seleccionado.");
                    }
                    string uniqueCode = GenerateUniqueCode(8);
                    // Generar un archivo Excel con los datos obtenidos
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(new FileInfo(originalFilePath)))
                    {
                        var worksheet = package.Workbook.Worksheets["FORMATO"];
                        int startRow = 9; // Comenzar desde la fila 9

                        foreach (var vacacion in vacaciones)
                        {
                            worksheet.Cells[$"B{startRow}"].Value = vacacion.Gerencia; // GERENCIA
                            worksheet.Cells[$"C{startRow}"].Value = vacacion.NombreCompleto; // NOMBRE
                            worksheet.Cells[$"D{startRow}"].Value = vacacion.PrimerNivel; // AREA
                            worksheet.Cells[$"E{startRow}"].Value = vacacion.Duracion; // SALDO
                            //worksheet.Cells[$"F{startRow}"].Value = vacacion.FechaInicio.ToString("dd/MM/yyyy"); // FECHA INICIO
                            //worksheet.Cells[$"G{startRow}"].Value = vacacion.FechaFin.ToString("dd/MM/yyyy"); // FECHA FIN
                            worksheet.Cells[$"H{startRow}"].Value = vacacion.DuracionFormateada; // DÍAS PROPUESTOS
                            worksheet.Cells[$"I{startRow}"].Value = vacacion.Comentario; // COMENTARIO
                            worksheet.Cells[$"J{startRow}"].Value = $"{vacacion.EstadoDeDisponibilidad}"; // ESTADO

                            startRow++; // Avanzar a la siguiente fila
                        }

                        // Guardar los cambios en memoria
                        using (var memoryStream = new MemoryStream())
                        {
                            package.SaveAs(memoryStream);
                            memoryStream.Position = 0;

                            // Descargar el archivo modificado
                            return File(memoryStream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                $"PlanificacionVacaciones_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}_{uniqueCode}.xlsx");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar errores
                return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
            }
        }

        public ActionResult ModificarExcelvacaciones2(DateTime fechaInicio, DateTime fechaFin)
        {
            // Validar que las fechas sean correctas
            if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
            {
                return new HttpStatusCodeResult(400, "Rango de fechas inválido");
            }

            string originalFilePath = Server.MapPath("~/App_Data/ProgramacionVacaciones.xlsx");

            // Verificar si el archivo original existe
            if (!System.IO.File.Exists(originalFilePath))
            {
                return new HttpNotFoundResult("El archivo original no se encontró.");
            }

            // Consulta SQL con filtro por fechas
            string query = @"
        SELECT
            em.nombre_completo AS NombreCompleto,
            em.carnet AS Carnet,
            em.OGERENCIA AS Gerencia,
            em.primernivel AS PrimerNivel,
            em.cargo AS Cargo,
            ab.Comments AS Comentario,
            ab.CreatedBy AS Creador,
           NULLIF(ab.SubmittedDate, '') AS FechaEnvio,
            ab.StartDate AS FechaInicio,
            ab.StartTime AS HoraInicio,
            ab.EndDate AS FechaFin,
            ab.EndTime AS HoraFin,
            ab.UnitOfMeasureMeaning AS UnidadDeMedida,
            ab.Duration AS Duracion,
            ab.FormattedDuration AS DuracionFormateada,
            ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
            ab.ApprovalDatetime AS FechaDeAprobacion,
            lastUpdatedBy AS usuarioactualizo,
            lastUpdateDate AS fecha_Actualizo
        FROM emp2024 em
        INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
        WHERE
              (ab.AbsenceType in ('Vacaciones','Enfermedad grave de un miembro del núcleo familiar que viva bajo mismo techo','Licencia por adopción')) and LegislationCode='NI' 
            AND ((ab.StartDate >= @fechaInicio AND ab.StartDate <= @fechaFin)
                 OR (ab.EndDate >= @fechaInicio AND ab.EndDate <= @fechaFin))  AND YEAR(ab.SubmittedDate) > 2023
        ORDER BY ab.SubmittedDate desc, em.carnet, ab.StartDate";

            try
            {
                List<EmpleadoDTO> temp = new List<EmpleadoDTO>();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query1 = @"
                SELECT 
                    em.carnet,
                    em.nombre_completo,
                    em.cargo,
                    em.primernivel AS Area,   em.OGERENCIA AS Gerencia,  em.OSUBGERENCIA AS SUBGerencia, em.primernivel AS area,
                    pl.BalanceAsOfBalanceCalculationDate AS Acumulado,telefonojefe,telefono,nom_jefe1,cargo_jefe1
                FROM [dbo].[EMP2024] em
                INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
                 ORDER BY pl.BalanceAsOfBalanceCalculationDate DESC";

                    temp = connection.Query<EmpleadoDTO>(query1).ToList();
                }
                using (var connection = new SqlConnection(connectionString))
                {
                    // Ejecutar la consulta y obtener los datos
                    var vacaciones = connection.Query<VacacionesPersona>(query, new { fechaInicio, fechaFin }).ToList();

                    // Validar si hay datos
                    if (vacaciones == null || !vacaciones.Any())
                    {
                        return new HttpStatusCodeResult(204, "No se encontraron datos para el rango de fechas seleccionado.");
                    }
                    string uniqueCode = GenerateUniqueCode(8);
                    // Generar un archivo Excel con los datos obtenidos
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(new FileInfo(originalFilePath)))
                    {
                        var worksheet = package.Workbook.Worksheets["FORMATO"];
                        int startRow = 9; // Comenzar desde la fila 9

                        foreach (var vacacion in vacaciones)
                        {
                            worksheet.Cells[$"B{startRow}"].Value = vacacion.Gerencia; // GERENCIA
                            worksheet.Cells[$"C{startRow}"].Value = vacacion.NombreCompleto; // NOMBRE
                            worksheet.Cells[$"D{startRow}"].Value = vacacion.PrimerNivel; // AREA
                            worksheet.Cells[$"E{startRow}"].Value = vacacion.Duracion; // SALDO
                            //worksheet.Cells[$"F{startRow}"].Value = vacacion.FechaInicio.ToString("dd/MM/yyyy"); // FECHA INICIO
                            //worksheet.Cells[$"G{startRow}"].Value = vacacion.FechaFin.ToString("dd/MM/yyyy"); // FECHA FIN
                            worksheet.Cells[$"H{startRow}"].Value = vacacion.DuracionFormateada; // DÍAS PROPUESTOS
                            worksheet.Cells[$"I{startRow}"].Value = vacacion.Comentario; // COMENTARIO
                            worksheet.Cells[$"J{startRow}"].Value = $"{vacacion.EstadoDeDisponibilidad}"; // ESTADO

                            startRow++; // Avanzar a la siguiente fila
                        }

                        // Guardar los cambios en memoria
                        using (var memoryStream = new MemoryStream())
                        {
                            package.SaveAs(memoryStream);
                            memoryStream.Position = 0;

                            // Descargar el archivo modificado
                            return File(memoryStream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                $"PlanificacionVacaciones_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}_{uniqueCode}.xlsx");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar errores
                return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
            }
        }
        public ActionResult ObtenerPorcentajeSinProgramacion()
        {
            string query = @"
  WITH CarnetsProgramados AS (
    SELECT DISTINCT
        em.carnet
    FROM emp2024 em
    INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
    WHERE ab.AbsenceType = 'Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
           AND ((ab.StartDate >= @fechaInicio AND ab.StartDate <= @fechaFin)
                   OR (ab.EndDate >= @fechaInicio AND ab.EndDate <= @fechaFin))
),
TotalesPorGerencia AS (
    SELECT 
        em.OGERENCIA AS Gerencia,
        COUNT(DISTINCT em.carnet) AS TotalEmpleados
    FROM emp2024 em
    GROUP BY em.OGERENCIA
),
ConProgramacion AS (
    SELECT 
        em.OGERENCIA AS Gerencia,
        COUNT(DISTINCT em.carnet) AS TotalSinProgramacion
    FROM emp2024 em
    WHERE em.carnet IN (SELECT carnet FROM CarnetsProgramados)
    GROUP BY em.OGERENCIA
)
SELECT 
    tpg.Gerencia,
    tpg.TotalEmpleados,
    COALESCE(cp.TotalSinProgramacion, 0) AS TotalSinProgramacion,
    ROUND((COALESCE(cp.TotalSinProgramacion, 0) * 100.0) / tpg.TotalEmpleados, 2) AS PorcentajeSinProgramacion
FROM TotalesPorGerencia tpg
LEFT JOIN ConProgramacion cp ON tpg.Gerencia = cp.Gerencia
ORDER BY tpg.Gerencia;

    ";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var datos = connection.Query<PorcentajeDTO>(query).ToList();

                    if (!datos.Any())
                    {
                        return new HttpStatusCodeResult(204, "No se encontraron datos.");
                    }

                    return Json(new { data = datos }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"Error al obtener datos: {ex.Message}");
            }
        }
        public ActionResult ObtenerPorcentajeSinProgramacionx(DateTime fechaInicio, DateTime fechaFin)
        {
            if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
            {
                return new HttpStatusCodeResult(400, "Rango de fechas inválido");
            }

            string query = @"
    WITH CarnetsProgramados AS (
        SELECT DISTINCT
            em.carnet
        FROM emp2024 em
        INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
        WHERE ab.AbsenceType = 'Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
           
              AND ((ab.StartDate >= @fechaInicio AND ab.StartDate <= @fechaFin)
                   OR (ab.EndDate >= @fechaInicio AND ab.EndDate <= @fechaFin))
    ),
    TotalesPorGerencia AS (
        SELECT 
            em.OGERENCIA AS Gerencia,
            COUNT(DISTINCT em.carnet) AS TotalEmpleados
        FROM emp2024 em
        GROUP BY em.OGERENCIA
    ),
    ConProgramacion AS (
        SELECT 
            em.OGERENCIA AS Gerencia,
            COUNT(DISTINCT em.carnet) AS TotalSinProgramacion
        FROM emp2024 em
        WHERE em.carnet IN (SELECT carnet FROM CarnetsProgramados)
        GROUP BY em.OGERENCIA
    )
    SELECT 
        tpg.Gerencia,
        tpg.TotalEmpleados,
        COALESCE(cp.TotalSinProgramacion, 0) AS TotalSinProgramacion,
        ROUND((COALESCE(cp.TotalSinProgramacion, 0) * 100.0) / tpg.TotalEmpleados, 2) AS PorcentajeSinProgramacion
    FROM TotalesPorGerencia tpg
    LEFT JOIN ConProgramacion cp ON tpg.Gerencia = cp.Gerencia
    ORDER BY tpg.Gerencia;
    ";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var datos = connection.Query<PorcentajeDTO>(query, new { fechaInicio, fechaFin }).ToList();

                    if (!datos.Any())
                    {
                        return new HttpStatusCodeResult(204, "No se encontraron datos.");
                    }

                    return Json(new { data = datos }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"Error al obtener datos: {ex.Message}");
            }
        }

        public ActionResult ObtenerProgramacion(DateTime fechaInicio, DateTime fechaFin)
        {
            // Validar fechas
            if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
            {
                return new HttpStatusCodeResult(400, "Rango de fechas inválido");
            }

            string query = @"
    DECLARE @Hoy DATE = GETDATE();
    DECLARE @Manana DATE = DATEADD(DAY, 1, GETDATE());

    SELECT 
        em.carnet AS Carnet,
        em.nombre_completo AS NombreCompleto,
        em.cargo AS Cargo,
        em.primernivel AS Area,
        em.OGERENCIA AS Gerencia,
        em.OSUBGERENCIA AS SubGerencia,
        ab.StartDate AS FechaInicio,
        ab.EndDate AS FechaFin,
        ab.AbsenceDispStatusMeaning AS Estado,
        ab.Comments AS Comentario,
        pl.BalanceAsOfBalanceCalculationDate AS Acumulado,
        @Hoy AS FechaActual,
        DATEDIFF(DAY, @Hoy, ab.StartDate) AS DiasParaInicio,
        DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1 AS DiasVacaciones,
        ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5) AS AcumuladaPasiva,
        pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5) AS TotalAcumulado,
        (pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5)) - (DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1) AS SaldoFinal,
        CASE 
            WHEN pl.BalanceAsOfBalanceCalculationDate = 0 THEN 0
            ELSE ROUND(
                ((pl.BalanceAsOfBalanceCalculationDate - 
                ((pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5)) - 
                (DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1))) / 
                pl.BalanceAsOfBalanceCalculationDate) * 100, 2)
        END AS PorcentajeReduccion, CASE 
        WHEN @Hoy < ab.StartDate THEN 0 -- No ha iniciado aún
        WHEN @Hoy > ab.EndDate THEN 100 -- Ya terminó
        ELSE ROUND(
            ((DATEDIFF(DAY, ab.StartDate, @Hoy) + 1) * 100.0) / 
            (DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1), 2)
    END AS PorcentajeCumplido
    FROM emp2024 em
    INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
    INNER JOIN PlanBalances pl ON em.carnet = pl.carnet
    WHERE 
        ab.AbsenceType = 'Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
      
        AND ((ab.StartDate BETWEEN @FechaInicio AND @FechaFin)or(ab.EndDate BETWEEN @FechaInicio AND @FechaFin))
    ORDER BY ab.StartDate;";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    // Ejecutar la consulta
                    var vacaciones = connection.Query<vacacionprogramada>(query, new { fechaInicio, fechaFin }).ToList();

                    if (vacaciones == null || !vacaciones.Any())
                    {
                        return new HttpStatusCodeResult(204, "No se encontraron datos de vacaciones programadas.");
                    }
                    DateTime hora = DateTime.Now;

                    // Generar archivo Excel
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(new FileInfo(Server.MapPath("~/App_Data/ProgramacionVacaciones.xlsx"))))
                    {
                        var worksheet = package.Workbook.Worksheets["FORMATO"];
                        int startRow = 9; // Fila inicial

                        worksheet.Cells["B6"].Value = "Planificación de Vacaciones:" + fechaInicio.ToShortDateString() + " AL " + fechaFin.ToShortDateString(); ; // subgerencia

                        foreach (var vacacion in vacaciones)
                        {
                            worksheet.Cells[$"B{startRow}"].Value = vacacion.Carnet;
                            worksheet.Cells[$"C{startRow}"].Value = vacacion.NombreCompleto;
                            worksheet.Cells[$"D{startRow}"].Value = vacacion.Gerencia;
                            worksheet.Cells[$"E{startRow}"].Value = vacacion.SUBGerencia;
                            worksheet.Cells[$"F{startRow}"].Value = vacacion.Area;
                            worksheet.Cells[$"G{startRow}"].Value = vacacion.Acumulado;
                            worksheet.Cells[$"G{startRow}"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            worksheet.Cells[$"G{startRow}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                            worksheet.Cells[$"H{startRow}"].Value = vacacion.FechaInicio?.ToString("yyyy-MM-dd");
                            worksheet.Cells[$"I{startRow}"].Value = vacacion.FechaFin?.ToString("yyyy-MM-dd");
                            worksheet.Cells[$"J{startRow}"].Value = vacacion.DiasParaInicio;
                            worksheet.Cells[$"J{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                            worksheet.Cells[$"K{startRow}"].Value = vacacion.DiasVacaciones;
                            worksheet.Cells[$"K{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                            worksheet.Cells[$"L{startRow}"].Value = vacacion.AcumuladaPasiva;
                            worksheet.Cells[$"L{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                            worksheet.Cells[$"M{startRow}"].Value = vacacion.TotalAcumulado;
                            worksheet.Cells[$"M{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                            worksheet.Cells[$"N{startRow}"].Value = vacacion.SaldoFinal;
                            worksheet.Cells[$"M{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                            worksheet.Cells[$"O{startRow}"].Value = vacacion.PorcentajeReduccion;
                            worksheet.Cells[$"P{startRow}"].Value = vacacion.PorcentajeCumplido;
                            // Configurar formato condicional para la columna O (PorcentajeReduccion)
                            var reglaPorcentajeReduccion = worksheet.ConditionalFormatting.AddTwoColorScale(worksheet.Cells[$"O{startRow} "]);
                            reglaPorcentajeReduccion.LowValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                            reglaPorcentajeReduccion.LowValue.Value = 0;
                            reglaPorcentajeReduccion.LowValue.Color = System.Drawing.Color.LightGreen; // Verde claro para valores bajos
                            reglaPorcentajeReduccion.HighValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                            reglaPorcentajeReduccion.HighValue.Value = 100;
                            reglaPorcentajeReduccion.HighValue.Color = System.Drawing.Color.Red; // Rojo para valores altos

                            // Configurar formato condicional para la columna P (PorcentajeCumplido)
                            var reglaPorcentajeCumplido = worksheet.ConditionalFormatting.AddThreeColorScale(worksheet.Cells[$"P{startRow} "]);
                            reglaPorcentajeCumplido.LowValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                            reglaPorcentajeCumplido.LowValue.Value = 0;
                            reglaPorcentajeCumplido.LowValue.Color = System.Drawing.Color.LightGray; // Gris claro para valores bajos
                            reglaPorcentajeCumplido.MiddleValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                            reglaPorcentajeCumplido.MiddleValue.Value = 50;
                            reglaPorcentajeCumplido.MiddleValue.Color = System.Drawing.Color.Yellow; // Amarillo para valores intermedios
                            reglaPorcentajeCumplido.HighValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                            reglaPorcentajeCumplido.HighValue.Value = 100;
                            reglaPorcentajeCumplido.HighValue.Color = System.Drawing.Color.Green; // Verde para valores altos

                            startRow++; // Avanzar a la siguiente fila
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            package.SaveAs(memoryStream);
                            memoryStream.Position = 0;

                            // Descargar el archivo
                            return File(memoryStream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                $"VacacionesProgramadas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}_Generacion:{hora}.xlsx");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
            }
        }
        public ActionResult ObtenerProgramacion2(DateTime fechaInicio, DateTime fechaFin,string Gerencia,int origen)
        {
            if (origen == 1)
            {


                // Validar fechas
                if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
                {
                    return new HttpStatusCodeResult(400, "Rango de fechas inválido");
                }

                string query = @"
    DECLARE @Hoy DATE = GETDATE();
    DECLARE @Manana DATE = DATEADD(DAY, 1, GETDATE());

    SELECT distinct
        em.carnet AS Carnet,
        em.nombre_completo AS NombreCompleto,
        em.cargo AS Cargo,
        em.primernivel AS Area,
        em.OGERENCIA AS Gerencia,
        em.OSUBGERENCIA AS SubGerencia,
        ab.StartDate AS FechaInicio,
        ab.EndDate AS FechaFin,
        ab.AbsenceDispStatusMeaning AS Estado,
        ab.Comments AS Comentario,
        pl.BalanceAsOfBalanceCalculationDate AS Acumulado,
        @Hoy AS FechaActual,
        DATEDIFF(DAY, @Hoy, ab.StartDate) AS DiasParaInicio,
        DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1 AS DiasVacaciones,
        ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5) AS AcumuladaPasiva,
        pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5) AS TotalAcumulado,
        (pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5)) - (DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1) AS SaldoFinal,
        CASE 
            WHEN pl.BalanceAsOfBalanceCalculationDate = 0 THEN 0
            ELSE ROUND(
                ((pl.BalanceAsOfBalanceCalculationDate - 
                ((pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5)) - 
                (DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1))) / 
                pl.BalanceAsOfBalanceCalculationDate) * 100, 2)
        END AS PorcentajeReduccion,
    CASE 
        WHEN @Hoy < ab.StartDate THEN 0 -- No ha iniciado aún
        WHEN @Hoy > ab.EndDate THEN 100 -- Ya terminó
        ELSE ROUND(
            ((DATEDIFF(DAY, ab.StartDate, @Hoy) + 1) * 100.0) / 
            (DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1), 2)
    END AS PorcentajeCumplido
    FROM emp2024 em
    INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
    INNER JOIN PlanBalances pl ON em.carnet = pl.carnet
    WHERE 
        ab.AbsenceType = 'Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
       
        AND ((ab.StartDate BETWEEN @FechaInicio AND @FechaFin)or(ab.EndDate BETWEEN @FechaInicio AND @FechaFin)) and    (em.OGERENCIA = @Gerencia)
    ORDER BY ab.StartDate;";

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        // Ejecutar la consulta
                        var vacaciones = connection.Query<vacacionprogramada>(query, new { fechaInicio, fechaFin, Gerencia }).ToList();

                        if (vacaciones == null || !vacaciones.Any())
                        {
                            return new HttpStatusCodeResult(204, "No se encontraron datos de vacaciones programadas.");
                        }

                        // Generar archivo Excel
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                        using (var package = new ExcelPackage(new FileInfo(Server.MapPath("~/App_Data/ProgramacionVacaciones.xlsx"))))
                        {
                            var worksheet = package.Workbook.Worksheets["FORMATO"];
                            int startRow = 9; // Fila inicial
                            DateTime hora = DateTime.Now ;
                            worksheet.Cells["B6"].Value = "Planificación de Vacaciones:" + fechaInicio.ToShortDateString() + " AL " + fechaFin.ToShortDateString(); ; // subgerencia
                            if (true)
                            {

                            }
                            foreach (var vacacion in vacaciones)
                            {
                                worksheet.Cells[$"B{startRow}"].Value = vacacion.Carnet;
                                worksheet.Cells[$"B{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Black); // Letra en negro
 
                                worksheet.Cells[$"C{startRow}"].Value = vacacion.NombreCompleto;
                                worksheet.Cells[$"C{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Black); // Letra en negro

                                worksheet.Cells[$"D{startRow}"].Value = vacacion.Gerencia;
                                worksheet.Cells[$"D{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Black); // Letra en negro

                                worksheet.Cells[$"E{startRow}"].Value = vacacion.SUBGerencia;
                                worksheet.Cells[$"E{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Black); // Letra en negro

                                worksheet.Cells[$"F{startRow}"].Value = vacacion.Area;
                                worksheet.Cells[$"G{startRow}"].Value = vacacion.Acumulado;
                                if (vacacion.PorcentajeCumplido == 100)
                                {
                                    worksheet.Cells[$"J{startRow}"].Value = 0;
                                    worksheet.Cells[$"L{startRow}"].Value = 0;
                                    worksheet.Cells[$"M{startRow}"].Value = vacacion.Acumulado;
                                    worksheet.Cells[$"N{startRow}"].Value = vacacion.Acumulado;


                                }
                                else
                                {
                                    worksheet.Cells[$"J{startRow}"].Value = vacacion.DiasParaInicio;
                                    worksheet.Cells[$"L{startRow}"].Value = vacacion.AcumuladaPasiva;
                                    worksheet.Cells[$"M{startRow}"].Value = vacacion.TotalAcumulado;
                                    worksheet.Cells[$"N{startRow}"].Value = vacacion.SaldoFinal;



                                }

                                worksheet.Cells[$"G{startRow}"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                worksheet.Cells[$"G{startRow}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                                worksheet.Cells[$"H{startRow}"].Value = vacacion.FechaInicio?.ToString("yyyy-MM-dd");
                                worksheet.Cells[$"I{startRow}"].Value = vacacion.FechaFin?.ToString("yyyy-MM-dd");
                             
                             
                                worksheet.Cells[$"J{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                                worksheet.Cells[$"K{startRow}"].Value = vacacion.DiasVacaciones;
                                worksheet.Cells[$"K{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                               
                                worksheet.Cells[$"L{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                                
                                worksheet.Cells[$"M{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                                worksheet.Cells[$"M{startRow}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                                worksheet.Cells[$"O{startRow}"].Value = vacacion.PorcentajeReduccion;
                                worksheet.Cells[$"P{startRow}"].Value = vacacion.PorcentajeCumplido;
                                // Configurar formato condicional para la columna O (PorcentajeReduccion)
                                var reglaPorcentajeReduccion = worksheet.ConditionalFormatting.AddTwoColorScale(worksheet.Cells[$"O{startRow} "]);
                                reglaPorcentajeReduccion.LowValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                                reglaPorcentajeReduccion.LowValue.Value = 0;
                                reglaPorcentajeReduccion.LowValue.Color = System.Drawing.Color.LightGreen; // Verde claro para valores bajos
                                reglaPorcentajeReduccion.HighValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                                reglaPorcentajeReduccion.HighValue.Value = 100;
                                reglaPorcentajeReduccion.HighValue.Color = System.Drawing.Color.Red; // Rojo para valores altos

                                // Configurar formato condicional para la columna P (PorcentajeCumplido)
                                var reglaPorcentajeCumplido = worksheet.ConditionalFormatting.AddThreeColorScale(worksheet.Cells[$"P{startRow} "]);
                                reglaPorcentajeCumplido.LowValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                                reglaPorcentajeCumplido.LowValue.Value = 0;
                                reglaPorcentajeCumplido.LowValue.Color = System.Drawing.Color.LightGray; // Gris claro para valores bajos
                                reglaPorcentajeCumplido.MiddleValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                                reglaPorcentajeCumplido.MiddleValue.Value = 50;
                                reglaPorcentajeCumplido.MiddleValue.Color = System.Drawing.Color.Yellow; // Amarillo para valores intermedios
                                reglaPorcentajeCumplido.HighValue.Type = OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingValueObjectType.Num;
                                reglaPorcentajeCumplido.HighValue.Value = 100;
                                reglaPorcentajeCumplido.HighValue.Color = System.Drawing.Color.Green; // Verde para valores altos

                                // Formato de columna "Porcentaje Reduccion" con formato de porcentaje
                                //worksheet.Cells[$"O{startRow}"].Style.Numberformat.Format = "0.00%";
                                startRow++; // Avanzar a la siguiente fila
                            }

                            using (var memoryStream = new MemoryStream())
                            {
                                package.SaveAs(memoryStream);
                                memoryStream.Position = 0;

                                // Descargar el archivo
                                return File(memoryStream.ToArray(),
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    $"VacacionesProgramadas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}_Generacion:{hora}.xlsx");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
                }
            }
            else
            {
                string query = @"
        WITH CarnetsProgramados AS (
            SELECT DISTINCT
                em.carnet
            FROM emp2024 em
            INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
            WHERE
                ab.AbsenceType = 'Vacaciones'   AND YEAR(ab.SubmittedDate) > 2023
                
                AND ((ab.StartDate >= @fechaInicio AND ab.StartDate <= @fechaFin)
                     OR (ab.EndDate >= @fechaInicio AND ab.EndDate <= @fechaFin))   and    (em.OGERENCIA = @Gerencia)
        ),
        Pendientes AS (
            SELECT 
                em.carnet,
                em.nombre_completo as NombreCompleto,
                em.cargo,
                em.primernivel AS Area,
                em.OGERENCIA AS Gerencia,
                em.OSUBGERENCIA AS SubGerencia,
                pl.BalanceAsOfBalanceCalculationDate AS Acumulado,
                em.telefonojefe,
                em.telefono,
                em.nom_jefe1,
                em.cargo_jefe1
            FROM [dbo].[EMP2024] em
            INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet 
            WHERE em.carnet NOT IN (SELECT carnet FROM CarnetsProgramados)  and    (em.OGERENCIA = @Gerencia)
        )
        SELECT * FROM Pendientes
        ORDER BY Gerencia,SubGerencia,Area,nom_jefe1 DESC";

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        // Ejecutar la consulta
                        var pendientes = connection.Query<EmpleadoDTO>(query, new { fechaInicio, fechaFin, Gerencia }).ToList();

                        if (pendientes == null || !pendientes.Any())
                        {
                            return new HttpStatusCodeResult(204, "No se encontraron datos pendientes.");
                        }

                        // Generar archivo Excel
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                        using (var package = new ExcelPackage(new FileInfo(Server.MapPath("~/App_Data/Programacionpendiente.xlsx"))))
                        {
                            var worksheet = package.Workbook.Worksheets["FORMATO"];
                            int startRow = 6; // Fila inicial
                            worksheet.Cells["B3"].Value ="" + fechaInicio.ToShortDateString() + " A " + fechaFin.ToShortDateString(); ; // subgerencia

                            foreach (var pendiente in pendientes)
                            {
                                worksheet.Cells[$"B{startRow}"].Value = pendiente.Carnet;
                                worksheet.Cells[$"C{startRow}"].Value = pendiente.NombreCompleto;
                                worksheet.Cells[$"D{startRow}"].Value = pendiente.Gerencia;
                                worksheet.Cells[$"E{startRow}"].Value = pendiente.SUBGerencia;
                                worksheet.Cells[$"F{startRow}"].Value = pendiente.Area;
                                worksheet.Cells[$"G{startRow}"].Value = pendiente.telefono;
                                worksheet.Cells[$"H{startRow}"].Value = pendiente.nom_jefe1;
                                worksheet.Cells[$"I{startRow}"].Value = pendiente.telefonojefe;
                                worksheet.Cells[$"J{startRow}"].Value = pendiente.Acumulado;

                                startRow++; // Avanzar a la siguiente fila
                            }

                            using (var memoryStream = new MemoryStream())
                            {
                                package.SaveAs(memoryStream);
                                memoryStream.Position = 0;

                                // Descargar el archivo
                                return File(memoryStream.ToArray(),
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    $"PendientesVacaciones_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
                }

            }
            }
        public ActionResult ObtenerProgramacion3(DateTime fechaInicio, DateTime fechaFin, string Gerencia, int origen)
        {
            if (origen == 1)
            {


                // Validar fechas
                if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
                {
                    return new HttpStatusCodeResult(400, "Rango de fechas inválido");
                }

                string query = @"
    DECLARE @Hoy DATE = GETDATE();
    DECLARE @Manana DATE = DATEADD(DAY, 1, GETDATE());

    SELECT 
        em.carnet AS Carnet,
        em.nombre_completo AS NombreCompleto,
        em.cargo AS Cargo,
        em.primernivel AS Area,
        em.OGERENCIA AS Gerencia,
        em.OSUBGERENCIA AS SubGerencia,
        ab.StartDate AS FechaInicio,
        ab.EndDate AS FechaFin,
        ab.AbsenceDispStatusMeaning AS Estado,
        ab.Comments AS Comentario,
        pl.BalanceAsOfBalanceCalculationDate AS Acumulado,
        @Hoy AS FechaActual,
        DATEDIFF(DAY, @Hoy, ab.StartDate) AS DiasParaInicio,
        DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1 AS DiasVacaciones,
        ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5) AS AcumuladaPasiva,
        pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5) AS TotalAcumulado,
        (pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5)) - (DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1) AS SaldoFinal,
        CASE 
            WHEN pl.BalanceAsOfBalanceCalculationDate = 0 THEN 0
            ELSE ROUND(
                ((pl.BalanceAsOfBalanceCalculationDate - 
                ((pl.BalanceAsOfBalanceCalculationDate + ROUND((DATEDIFF(DAY, @Manana, ab.EndDate) + 1) * 0.083333, 5)) - 
                (DATEDIFF(DAY, ab.StartDate, ab.EndDate) + 1))) / 
                pl.BalanceAsOfBalanceCalculationDate) * 100, 2)
        END AS PorcentajeReduccion
    FROM emp2024 em
    INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
    INNER JOIN PlanBalances pl ON em.carnet = pl.carnet
    WHERE 
        ab.AbsenceType = 'Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
      
        AND ab.StartDate BETWEEN @FechaInicio AND @FechaFin and    (em.OGERENCIA = @Gerencia)
    ORDER BY ab.StartDate;";

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        // Ejecutar la consulta
                        var vacaciones = connection.Query<vacacionprogramada>(query, new { fechaInicio, fechaFin, Gerencia }).ToList();

                        if (vacaciones == null || !vacaciones.Any())
                        {
                            return new HttpStatusCodeResult(204, "No se encontraron datos de vacaciones programadas.");
                        }

                        // Generar archivo Excel
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                        using (var package = new ExcelPackage(new FileInfo(Server.MapPath("~/App_Data/ProgramacionVacaciones.xlsx"))))
                        {
                            var worksheet = package.Workbook.Worksheets["FORMATO"];
                            int startRow = 9; // Fila inicial

                            worksheet.Cells["B6"].Value = "Planificación de Vacaciones:" + fechaInicio.ToShortDateString() + " A " + fechaFin.ToShortDateString(); ; // subgerencia

                            foreach (var vacacion in vacaciones)
                            {
                                worksheet.Cells[$"B{startRow}"].Value = vacacion.Carnet;
                                worksheet.Cells[$"C{startRow}"].Value = vacacion.NombreCompleto;
                                worksheet.Cells[$"D{startRow}"].Value = vacacion.Gerencia;
                                worksheet.Cells[$"E{startRow}"].Value = vacacion.SUBGerencia;
                                worksheet.Cells[$"F{startRow}"].Value = vacacion.Area;
                                worksheet.Cells[$"G{startRow}"].Value = vacacion.Acumulado;
                                worksheet.Cells[$"H{startRow}"].Value = vacacion.FechaInicio?.ToString("yyyy-MM-dd");
                                worksheet.Cells[$"I{startRow}"].Value = vacacion.FechaFin?.ToString("yyyy-MM-dd");
                                worksheet.Cells[$"J{startRow}"].Value = vacacion.DiasParaInicio;
                                worksheet.Cells[$"K{startRow}"].Value = vacacion.DiasVacaciones;
                                worksheet.Cells[$"L{startRow}"].Value = vacacion.AcumuladaPasiva;
                                worksheet.Cells[$"M{startRow}"].Value = vacacion.TotalAcumulado;
                                worksheet.Cells[$"N{startRow}"].Value = vacacion.SaldoFinal;
                                worksheet.Cells[$"O{startRow}"].Value = vacacion.PorcentajeReduccion;

                                // Formato de columna "Porcentaje Reduccion" con formato de porcentaje
                                //worksheet.Cells[$"O{startRow}"].Style.Numberformat.Format = "0.00%";
                                startRow++; // Avanzar a la siguiente fila
                            }

                            using (var memoryStream = new MemoryStream())
                            {
                                package.SaveAs(memoryStream);
                                memoryStream.Position = 0;

                                // Descargar el archivo
                                return File(memoryStream.ToArray(),
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    $"VacacionesProgramadas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
                }
            }
            else
            {
                string query = @"
        WITH CarnetsProgramados AS (
            SELECT DISTINCT
                em.carnet
            FROM emp2024 em
            INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
            WHERE
                ab.AbsenceType = 'Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
              
                AND ((ab.StartDate >= @fechaInicio AND ab.StartDate <= @fechaFin)
                     OR (ab.EndDate >= @fechaInicio AND ab.EndDate <= @fechaFin))   and    (em.OGERENCIA = @Gerencia)
        ),
        Pendientes AS (
            SELECT 
                em.carnet,
                em.nombre_completo as NombreCompleto,
                em.cargo,
                em.primernivel AS Area,
                em.OGERENCIA AS Gerencia,
                em.OSUBGERENCIA AS SubGerencia,
                pl.BalanceAsOfBalanceCalculationDate AS Acumulado,
                em.telefonojefe,
                em.telefono,
                em.nom_jefe1,
                em.cargo_jefe1
            FROM [dbo].[EMP2024] em
            INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
            WHERE em.carnet NOT IN (SELECT carnet FROM CarnetsProgramados)
        )
        SELECT * FROM Pendientes
        ORDER BY Gerencia,SubGerencia,Area,nom_jefe1 DESC";

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        // Ejecutar la consulta
                        var pendientes = connection.Query<EmpleadoDTO>(query, new { fechaInicio, fechaFin, Gerencia }).ToList();

                        if (pendientes == null || !pendientes.Any())
                        {
                            return new HttpStatusCodeResult(204, "No se encontraron datos pendientes.");
                        }

                        // Generar archivo Excel
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                        using (var package = new ExcelPackage(new FileInfo(Server.MapPath("~/App_Data/Programacionpendiente.xlsx"))))
                        {
                            var worksheet = package.Workbook.Worksheets["FORMATO"];
                            int startRow = 6; // Fila inicial
                            worksheet.Cells["B3"].Value = "" + fechaInicio.ToShortDateString() + " A " + fechaFin.ToShortDateString(); ; // subgerencia

                            foreach (var pendiente in pendientes)
                            {
                                worksheet.Cells[$"B{startRow}"].Value = pendiente.Carnet;
                                worksheet.Cells[$"C{startRow}"].Value = pendiente.NombreCompleto;
                                worksheet.Cells[$"D{startRow}"].Value = pendiente.Gerencia;
                                worksheet.Cells[$"E{startRow}"].Value = pendiente.SUBGerencia;
                                worksheet.Cells[$"F{startRow}"].Value = pendiente.Area;
                                worksheet.Cells[$"G{startRow}"].Value = pendiente.telefono;
                                worksheet.Cells[$"H{startRow}"].Value = pendiente.nom_jefe1;
                                worksheet.Cells[$"I{startRow}"].Value = pendiente.telefonojefe;
                                worksheet.Cells[$"J{startRow}"].Value = pendiente.Acumulado;

                                startRow++; // Avanzar a la siguiente fila
                            }

                            using (var memoryStream = new MemoryStream())
                            {
                                package.SaveAs(memoryStream);
                                memoryStream.Position = 0;

                                // Descargar el archivo
                                return File(memoryStream.ToArray(),
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    $"PendientesVacaciones_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
                }

            }
        }

        public ActionResult ObtenerPendientesYGenerarExcel(DateTime fechaInicio, DateTime fechaFin)
        {
            // Validar fechas
            if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
            {
                return new HttpStatusCodeResult(400, "Rango de fechas inválido");
            }

            string query = @"
        WITH CarnetsProgramados AS (
            SELECT DISTINCT
                em.carnet
            FROM emp2024 em
            INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
            WHERE
                ab.AbsenceType = 'Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
             
                AND ((ab.StartDate >= @fechaInicio AND ab.StartDate <= @fechaFin)
                     OR (ab.EndDate >= @fechaInicio AND ab.EndDate <= @fechaFin))
        ),
        Pendientes AS (
            SELECT 
                em.carnet,
                em.nombre_completo as NombreCompleto,
                em.cargo,
                em.primernivel AS Area,
                em.OGERENCIA AS Gerencia,
                em.OSUBGERENCIA AS SubGerencia,
                pl.BalanceAsOfBalanceCalculationDate AS Acumulado,
                em.telefonojefe,
                em.telefono,
                em.nom_jefe1,
                em.cargo_jefe1
            FROM [dbo].[EMP2024] em
            INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
            WHERE em.carnet NOT IN (SELECT carnet FROM CarnetsProgramados)
        )
        SELECT * FROM Pendientes
        ORDER BY Gerencia,SubGerencia,Area,nom_jefe1 DESC";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    // Ejecutar la consulta
                    var pendientes = connection.Query<EmpleadoDTO>(query, new { fechaInicio, fechaFin }).ToList();

                    if (pendientes == null || !pendientes.Any())
                    {
                        return new HttpStatusCodeResult(204, "No se encontraron datos pendientes.");
                    }

                    // Generar archivo Excel
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(new FileInfo(Server.MapPath("~/App_Data/Programacionpendiente.xlsx"))))
                    {
                        var worksheet = package.Workbook.Worksheets["FORMATO"];
                        int startRow =6; // Fila inicial
                        worksheet.Cells["B3"].Value = "" + fechaInicio.ToShortDateString() + " A " + fechaFin.ToShortDateString(); ; // subgerencia

                        foreach (var pendiente in pendientes)
                        {
                            worksheet.Cells[$"B{startRow}"].Value = pendiente.Carnet;
                            worksheet.Cells[$"C{startRow}"].Value = pendiente.NombreCompleto;
                            worksheet.Cells[$"D{startRow}"].Value = pendiente.Gerencia;
                            worksheet.Cells[$"E{startRow}"].Value = pendiente.SUBGerencia;
                            worksheet.Cells[$"F{startRow}"].Value = pendiente.Area;
                            worksheet.Cells[$"G{startRow}"].Value = pendiente.telefono;
                            worksheet.Cells[$"H{startRow}"].Value = pendiente.nom_jefe1;
                            worksheet.Cells[$"I{startRow}"].Value = pendiente.telefonojefe;
                            worksheet.Cells[$"J{startRow}"].Value = pendiente.Acumulado;

                            startRow++; // Avanzar a la siguiente fila
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            package.SaveAs(memoryStream);
                            memoryStream.Position = 0;

                            // Descargar el archivo
                            return File(memoryStream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                $"PendientesVacaciones_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
            }
        }
        public ActionResult ObtenerConRegistrosYGenerarExcel(DateTime fechaInicio, DateTime fechaFin)
        {
            // Validar fechas
            if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
            {
                return new HttpStatusCodeResult(400, "Rango de fechas inválido");
            }

            string query = @"
        WITH CarnetsProgramados AS (
            SELECT DISTINCT
                em.carnet,
            ab.StartDate AS FechaInicio,
            ab.EndDate AS FechaFin,
            ab.Duration AS Duracion,
            ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
            ab.ApprovalDatetime AS FechaDeAprobacion,
            lastUpdatedBy AS usuarioactualizo,
            lastUpdateDate AS fecha_Actualizo
            FROM emp2024 em
            INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
            WHERE
                ab.AbsenceType = 'Vacaciones'  AND YEAR(ab.SubmittedDate) > 2023
  AND ((ab.StartDate >= @fechaInicio AND ab.StartDate <= @fechaFin)
                     OR (ab.EndDate >= @fechaInicio AND ab.EndDate <= @fechaFin))
        ),
        ConRegistros AS (
            SELECT 
                em.carnet,
                em.nombre_completo,
                em.cargo,
                em.primernivel AS Area,
                em.OGERENCIA AS Gerencia,
                em.OSUBGERENCIA AS SubGerencia,
                pl.BalanceAsOfBalanceCalculationDate AS Acumulado,
                em.telefonojefe,
                em.telefono,
                em.nom_jefe1,
                em.cargo_jefe1, 
				CarnetsProgramados.FechaInicio,
          CarnetsProgramados.FechaFin,
           CarnetsProgramados.Duracion,
           CarnetsProgramados.EstadoDeDisponibilidad,
            CarnetsProgramados.FechaDeAprobacion,
            CarnetsProgramados.usuarioactualizo,
           CarnetsProgramados.fecha_Actualizo
            FROM [dbo].[EMP2024] em
            INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
			inner join CarnetsProgramados on CarnetsProgramados.carnet=pl.carnet
           
        )
        SELECT * FROM ConRegistros where duracion>6
        ORDER BY FechaInicio,Gerencia,SubGerencia,Acumulado DESC";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    // Ejecutar la consulta
                    var conRegistros = connection.Query<VacacionesPersona>(query, new { fechaInicio, fechaFin }).ToList();

                    if (conRegistros == null || !conRegistros.Any())
                    {
                        return new HttpStatusCodeResult(204, "No se encontraron datos con registros programados.");
                    }

                    // Generar archivo Excel
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(new FileInfo(Server.MapPath("~/App_Data/ProgramacionVacaciones.xlsx"))))
                    {
                        var worksheet = package.Workbook.Worksheets["FORMATO"];
                        int startRow = 9; // Fila inicial

                        foreach (var registro in conRegistros)
                        {
                            //worksheet.Cells[$"B{startRow}"].Value = registro.carnet; // Carnet
                            //worksheet.Cells[$"C{startRow}"].Value = registro.nombre_completo; // Nombre Completo
                            //worksheet.Cells[$"D{startRow}"].Value = registro.Area; // Área
                            //worksheet.Cells[$"E{startRow}"].Value = registro.Acumulado; // Acumulado
                            //worksheet.Cells[$"F{startRow}"].Value = registro.Gerencia; // Gerencia
                            //worksheet.Cells[$"G{startRow}"].Value = registro.SubGerencia; // SubGerencia
                            //worksheet.Cells[$"H{startRow}"].Value = registro.telefonojefe; // Teléfono Jefe
                            //worksheet.Cells[$"I{startRow}"].Value = registro.nom_jefe1; // Nombre Jefe
                            //worksheet.Cells[$"J{startRow}"].Value = registro.cargo_jefe1; // Cargo Jefe

                            startRow++; // Avanzar a la siguiente fila
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            package.SaveAs(memoryStream);
                            memoryStream.Position = 0;

                            // Descargar el archivo
                            return File(memoryStream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                $"ConRegistrosVacaciones_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"Ocurrió un error al generar el archivo: {ex.Message}");
            }
        }

        private string GenerateUniqueCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }
        // Acción para descargar el archivo modificado
        public ActionResult DescargarExcel()
        {
            // Ruta al archivo original (modificado)
            string filePath = Server.MapPath("~/App_Data/PlanificacionVacaciones.xlsx");

            if (!System.IO.File.Exists(filePath))
            {
                return new HttpNotFoundResult("El archivo no se encontró.");
            }

            // Devolver el archivo para descarga
            return File(filePath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PlanificacionVacaciones_Modificado.xlsx");
        }
        public ActionResult FileUpload()
        {
            int personId = (int)Session[keyPersonLicense];
            UploadedFile[] files = UploadControlExtension.GetUploadedFiles("uploadLicense",
                                                                           null,
                                                                           (s, e) =>
            {
                var name = e.UploadedFile.FileName;
                Session["UploadedLicenseName"] = name;
                Session["UploadedLicenseBytes"] = e.UploadedFile.FileBytes;
                e.CallbackData = name;
            });
            return RegisterDetail(personId);
        }


        #endregion
        #region Cargar archivo de rendición
        public ActionResult LoadLicense(int id)
        {
            try
            {
                Entities.Licenses license = new Entities.Licenses();
                license = Data.License.GetLicenseById(id).FirstOrDefault();
                if (license.LicenseFileExtension == "pdf")
                {
                    byte[] byteArray = Data.License.GetLicenseById(id).FirstOrDefault().LicenseFile;
                    if (byteArray == null)
                    {
                        return null;
                    }

                    var strBase64 = Convert.ToBase64String(byteArray);
                    Entities.Licenses licenses = new Entities.Licenses();
                    licenses.Notes = string.Format("data:application/pdf;base64,{0}", strBase64);
                    return PartialView("PdfLicenseFilePartial", licenses);
                }
                else
                {
                    Entities.Licenses licenses = new Entities.Licenses();
                    licenses.LicenseFile = Utils.ClaroWCF.GetLicenseFile(id);
                    if (licenses.LicenseFile == null)
                    {
                        return null;
                    }

                    return PartialView("ImageLicenseFilePartial", licenses);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error al cargar el objeto", e);
            }
        }
        #endregion
        #region CRUD

        /// <summary>
        /// Accion que llama  a metodo para insertar una licencia
        /// </summary>
        /// <param name="license"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddLicense(Entities.Licenses license)
        {

            int personId = (int)Session[keyPersonLicense];
            license.PersonId = personId;
            Validations.Licenses vLicenses = new Validations.Licenses();
            string cadena = null;
            string extension = null;

            if (ModelState.IsValid)
            {
                try
                {
                    bool resultNull = vLicenses.ValidateNullFields(license);
                    if (resultNull == true)
                    {
                        bool resultEndDate = vLicenses.ValidateEndDateGreaterThanStartDate(license);
                        if (resultEndDate == true)
                        {
                            bool resultEndHour = vLicenses.ValidateEndHourGreaterThanStartHour(license);
                            if (resultEndHour == true)
                            {
                                bool resultDuplicateLicensesDates = vLicenses.ValidateDuplicateDates(license);
                                if (resultDuplicateLicensesDates == true)
                                {
                                    bool resultDuplicateLicensesHours = vLicenses.ValidateDuplicateHours(license);
                                    if (resultDuplicateLicensesHours == true)
                                    {
                                        bool resultTotalPartialHours = vLicenses.ValidateTotalPartialHours(license);
                                        if (resultTotalPartialHours == true)
                                        {
                                            if (Session["UploadedLicenseBytes"] != null)
                                            {
                                                license.LicenseFile = (byte[])Session["UploadedLicenseBytes"];
                                                cadena = (string)Session["UploadedLicenseName"];
                                                extension = cadena.Split('.').Last();
                                                license.LicenseFileExtension = extension;
                                            }

                                            Data.License.InsertLicense(license);
                                        }
                                        else
                                        {
                                            Session.Remove("UploadedLicenseName");
                                            Session.Remove("UploadedLicenseBytes");

                                            return Content("El total de horas es mayor o igual que 8 por favor cambie de parcial a total.");
                                        }
                                    }
                                    else
                                    {
                                        Session.Remove("UploadedLicenseName");
                                        Session.Remove("UploadedLicenseBytes");

                                        return Content("Ya existe una hora igual dentro del rango de horas digitado.");
                                    }
                                }
                                else
                                {
                                    Session.Remove("UploadedLicenseName");
                                    Session.Remove("UploadedLicenseBytes");

                                    return Content("Ya existe una fecha igual dentro del rango de fechas digitado.");
                                }
                            }
                            else
                            {
                                Session.Remove("UploadedLicenseName");
                                Session.Remove("UploadedLicenseBytes");

                                return Content("Cuando el periodo es parcial la hora de fin no puede ser menor o igual que la hora de inicio");
                            }
                        }
                        else
                        {
                            Session.Remove("UploadedLicenseName");
                            Session.Remove("UploadedLicenseBytes");

                            return Content("La fecha de fin no puede ser menor que la fecha de inicio.");
                        }
                    }
                    else
                    {
                        Session.Remove("UploadedLicenseName");
                        Session.Remove("UploadedLicenseBytes");

                        return Content("El tipo o el motivo son campos requeridos.");
                    }
                }
                catch (Exception e)
                {
                    Session.Remove("UploadedLicenseName");
                    Session.Remove("UploadedLicenseBytes");
                    //ViewData["vwLicense"] = license;
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                Session.Remove("UploadedLicenseName");
                Session.Remove("UploadedLicenseBytes");
                //ViewData["vwLicense"] = license;
                // ViewData["EditError"] = "Por favor, llene todos los campos requeridos.";
                return Content("Por favor, llene todos los campos requeridos.");
            }
            Session.Remove("UploadedLicenseName");
            Session.Remove("UploadedLicenseBytes");
            //ViewData["vwLicense"] = null;
            return RegisterDetailPartial(license.PersonId);
        }

        /// <summary>
        /// Accion que llama a metodo UpdateLicenseRRHH del modelo Licenses para actualizar una licencia.
        /// </summary>
        /// <param name="license"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult UpdateLicenseRRHH(Entities.Licenses license)
        {
            Entities.Licenses uLicense = new Entities.Licenses();
            List<Entities.Licenses> lstLicenses = new List<Entities.Licenses>();
            if (Session["sLicensesAuthorizeRh"] != null)
            {
                lstLicenses = (List<Entities.Licenses>)Session["sLicensesAuthorizeRh"];
            }
            try
            {
                if (license.NotesRh != null)
                {
                    //var result = Utils.ClaroWCF.GetLicenseById(license.LicenseId);
                    var result = lstLicenses.FirstOrDefault(x => x.LicenseId == license.LicenseId);
                    if (result != null)
                    {
                        uLicense = result;
                        uLicense.StartDate = license.StartDate;
                        uLicense.EndDate = license.EndDate;
                        uLicense.DaysQuantity = license.DaysQuantity;
                        uLicense.StartHour = DateTime.Parse(DateTime.Now.ToShortDateString() +
                            " " +
                        license.StartHour.ToString("hh:mm:ss tt"));
                        uLicense.EndHour = DateTime.Parse(DateTime.Now.ToShortDateString() +
                            " " +
                        license.EndHour.ToString("hh:mm:ss tt"));
                        uLicense.HoursQuantity = license.HoursQuantity;
                       
                        uLicense.NotesRh = license.NotesRh.ToUpper();
                        Data.License.UpdateLicenseRRHH(uLicense);
                    }
                }
                else
                {
                    return Content("El campo NotasRH es requerido");
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return RedirectToAction("AuthorizeRh");
        }

        /// <summary>
        /// Accion que llama a metodo UpdateLicense del modelo Licenses para actualizar una licencia
        /// </summary>
        /// <param name="license"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult UpdateLicense(Entities.Licenses license)
        {
            Validations.Licenses vLicenses = new Validations.Licenses();
            int personId = (int)Session[keyPersonLicense];
            license.PersonId = personId;
            string cadena = null;
            string extension = null;


            if (ModelState.IsValid)
            {
                try
                {
                    bool resultNull = vLicenses.ValidateNullFields(license);
                    if (resultNull == true)
                    {
                        bool resultEndDate = vLicenses.ValidateEndDateGreaterThanStartDate(license);
                        if (resultEndDate == true)
                        {
                            bool resultEndHour = vLicenses.ValidateEndHourGreaterThanStartHour(license);
                            if (resultEndHour == true)
                            {
                                bool resultDuplicateLicensesDates = vLicenses.ValidateDuplicateDates(license);
                                if (resultDuplicateLicensesDates == true)
                                {
                                    bool resultDuplicateLicensesHours = vLicenses.ValidateDuplicateHours(license);
                                    if (resultDuplicateLicensesHours == true)
                                    {
                                        if (Session["UploadedLicenseBytes"] != null)
                                        {
                                            license.LicenseFile = (byte[])Session["UploadedLicenseBytes"];
                                            cadena = (string)Session["UploadedLicenseName"];
                                            extension = cadena.Split('.').Last();
                                            license.LicenseFileExtension = extension;
                                        }
                                        Data.License.UpdateLicense(license);
                                    }
                                    else
                                    {
                                        Session.Remove("UploadedLicenseName");
                                        Session.Remove("UploadedLicenseBytes");

                                        return Content("Ya existe una fecha igual o el rango de horas ya existe.");
                                    }
                                }
                                else
                                {
                                    Session.Remove("UploadedLicenseName");
                                    Session.Remove("UploadedLicenseBytes");

                                    return Content("Ya existe una fecha igual dentro del rango de fechas digitado.");
                                }
                            }
                            else
                            {
                                Session.Remove("UploadedLicenseName");
                                Session.Remove("UploadedLicenseBytes");
                                return Content("Cuando el periodo es parcial la hora de fin no puede ser menor o igual que la hora de inicio");
                            }
                        }
                        else
                        {
                            Session.Remove("UploadedLicenseName");
                            Session.Remove("UploadedLicenseBytes");

                            return Content("La fecha de fin no puede ser menor que la fecha de inicio.");
                        }
                    }
                    else
                    {
                        Session.Remove("UploadedLicenseName");
                        Session.Remove("UploadedLicenseBytes");

                        return Content("El tipo o el motivo son campos requeridos.");
                    }
                }
                catch (Exception e)
                {
                    Session.Remove("UploadedLicenseName");
                    Session.Remove("UploadedLicenseBytes");
                    ViewData["vwLicense"] = license;
                    //Session["sLicense"] = license;
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                Session.Remove("UploadedLicenseName");
                Session.Remove("UploadedLicenseBytes");
                // ViewData["vwLicense"] = license;
                ViewData["EditError"] = "Por favor, llene todos los campos requeridos.";
            }

            Session.Remove("UploadedLicenseName");
            Session.Remove("UploadedLicenseBytes");
            //ViewData["vwLicense"] = null;

            return RegisterDetailPartial(license.PersonId);
        }

        ///// <summary>
        ///// Metodo para elimianar una licencia
        ///// </summary>
        ///// <param name="expenseId"></param>
        ///// <returns></returns>
        public ActionResult DeleteLicense(int licenseId)
        {
            int personId = (int)Session[keyPersonLicense];
            Validations.Licenses vLicenses = new Validations.Licenses();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {//gustavo revisar esto id porque esta si 
                if ((eEmployee.Idhrms != 48912) && (eEmployee.Idhrms != 40389) && (eEmployee.Idhrms != 1847))
                {
                    bool resultAuthorization = vLicenses.ValidateAuthorization(licenseId);
                    if (resultAuthorization == true)
                    {
                        Data.License.DeleteLicense(licenseId);
                    }
                    else
                    {
                        return Content("Solo se pueden eliminar licencias en estado de GRABADO");
                    }
                }
                else
                {
                    Data.License.DeleteLicense(licenseId);
                }

               
            }
            catch (Exception)
            {
                throw;
            }

            return RegisterDetailPartial(personId);
        }


        #endregion
        #region Autorizaciones de Jefe inmediato
        [Authorize]
        public ActionResult RefreshAuthorizeBoss()
        {

            Session.Remove("sLicensesAuthorizeBoss");
            return AuthorizeBossPartial();
        }

        [Authorize]
        public ActionResult AuthorizeBoss()
        {
            List<Entities.Licenses> lstLicense = new List<Entities.Licenses>();
            lstLicense = Data.License.GetAllLicensesAuthorizeBoss();
            return View(lstLicense);
        }

        public ActionResult AuthorizeBossPartial()
        {
            List<Entities.Licenses> lstLicense = new List<Entities.Licenses>();
            try
            {

                lstLicense = Data.License.GetAllLicensesAuthorizeBoss();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeBossPartial", lstLicense);
           
        }

       


        /// <summary>
        /// Accion que llama a metodo para autorizacion de jefe inmediato de la licencia de un colaborador
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult AuthorizeBoss(string ids)//selectedIdsAP
        {
            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.License.AuthorizeBoss(int.Parse(keyAuthorize));
                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }
            }
            return AuthorizeBossPartial(); //View("AuthorizeBoss");
        }


        /// <summary>
        /// Accion que llama a metodo para denegar licencia de un colaboador por el jefe inmediato.
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DeniedBoss(string ids) //selectedIdsDN
        {
            string keysET = ids + ",";
            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.License.DeniedBoss(int.Parse(keyAuthorize));

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }
            }
         return AuthorizeBossPartial();
        }

        #endregion
        #region Binding Lista de empleados para consulta
        public ActionResult UserAgreement()
        {
            return View();
        }

        [Authorize]

        public ActionResult EmployeesList()
        {
            return View();
        }

        public ActionResult EmployeesListPartial()
        {
            List<Entities.Employees> lstEmployees = new List<Entities.Employees>();
            try
            {
                lstEmployees = Data.Employee.GetEmployeesByBossToLicense();
            }
            catch (Exception)
            {

                throw;
            }
            return PartialView("EmployeesListPartial", lstEmployees);
            //GridViewModel viewModel = GridViewExtension.GetViewModel(keyViewModel);
            //if (viewModel == null)
            //{
            //    viewModel = CreateGridViewModelWithSummary();
            //}

            //return EmployeesBindingCore(viewModel);
        }

        //[Authorize]
        //PartialViewResult EmployeesBindingCore(GridViewModel viewModel)
        //{
        //    viewModel.ProcessCustomBinding(Employees.GetDataRowCountLicense,
        //                                   Employees.GetDataLicense,
        //                                   Employees.GetSummaryValuesLicense);
        //    return PartialView("EmployeesListPartial", viewModel);
        //}

        ////Paginación gridview registrar
        //public ActionResult GridPagingAction(GridViewPagerState pager)
        //{
        //    GridViewModel viewModel = GridViewExtension.GetViewModel(keyViewModel);
        //    viewModel.ApplyPagingState(pager);
        //    return EmployeesBindingCore(viewModel);
        //}

        ////Filtro
        //public ActionResult GridFilteringAction(GridViewFilteringState filter)
        //{
        //    GridViewModel viewModel = GridViewExtension.GetViewModel(keyViewModel);
        //    viewModel.ApplyFilteringState(filter);
        //    return EmployeesBindingCore(viewModel);
        //}

        //static GridViewModel CreateGridViewModelWithSummary()
        //{
        //    GridViewModel viewModel = new GridViewModel();
        //    viewModel.KeyFieldName = "Id_HRMS";
        //    viewModel.Columns.Add("EmployeeNumber");
        //    viewModel.Columns.Add("Names");
        //    viewModel.Columns.Add("LastNames");
        //    viewModel.Columns.Add("FullName");
        //    viewModel.Columns.Add("Location");

        //    viewModel.TotalSummary
        //        .Add(new GridViewSummaryItemState()
        //        { FieldName = "EmployeeNumber", SummaryType = SummaryItemType.Count });


        //    return viewModel;
        //}
        #endregion
        #region Binding del listado de registro de licencias.

        [Authorize]

        public ActionResult RegisterDetail(int id)
        {
            try
            {
                Entities.Employees Employee = new Entities.Employees();
                Employee = Data.Employee.GetEmployeesByBossToLicense().First(item => item.Idhrms == id);
                Employee.Picture = Utils.ClaroWCF.GetEmployeePicture(id);

                Session["fullName"] = Employee.FullName;

                return View("RegisterDetail", Employee);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
        }
        public ActionResult Acumulado()
        { return View(); }
        public JsonResult Acumuladox()
        {
            List<EmpleadoDTO> temp = new List<EmpleadoDTO>();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                SELECT 
                    em.carnet,
                    em.nombre_completo,
                    em.cargo,
                    em.primernivel AS Area,
                    pl.BalanceAsOfBalanceCalculationDate AS Acumulado
                FROM [dbo].[EMP2024] em
                INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
                 ORDER BY pl.BalanceAsOfBalanceCalculationDate DESC";
                
                  connection.Query<EmpleadoDTO>(query ).ToList();
            }
            return Json(new { data = temp }, JsonRequestBehavior.AllowGet);

        }
        [HttpGet]
        public JsonResult ObtenerGerentes()
        {
            try
            {
                Entities.Employees employee = (Entities.Employees)Session["User"];
                if(employee.GERENCIA.Contains("RECURSOS HUMANOS") == true || employee.GERENCIA.Contains("DIRECCION PAIS") == true)
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        string query = @"
                    WITH AcumuladoPorGerencia AS (
                            SELECT 
                                em2.OGERENCIA, 
                                SUM(pl2.BalanceAsOfBalanceCalculationDate) AS AcumuladoGerencia
                            FROM 
                                [dbo].[EMP2024] em2
                            INNER JOIN 
                                [dbo].[PlanBalances] pl2 ON em2.carnet = pl2.carnet
                            GROUP BY 
                                em2.OGERENCIA
                        )
                        SELECT 
                            em.OGERENCIA AS Gerencia,
                            em.nombre_completo AS Nombre,em.Carnet,
							em.cargo,pl.BalanceAsOfBalanceCalculationDate AcumuladoGerente,
                            g.AcumuladoGerencia
                        FROM 
                            [dbo].[EMP2024] em
                        INNER JOIN 
                            [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
                        INNER JOIN 
                            AcumuladoPorGerencia g ON em.OGERENCIA = g.OGERENCIA
                        WHERE 
                            em.cargo LIKE 'GERENTE%'
                        ORDER BY 
                            g.AcumuladoGerencia DESC ;";

                        var gerentes = connection.Query<GerenteDTO>(query).ToList();

                        return Json(new { data = gerentes }, JsonRequestBehavior.AllowGet);
                    }
                    }else
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        string query = @"
                    WITH AcumuladoPorGerencia AS (
                            SELECT 
                                em2.OGERENCIA, 
                                SUM(pl2.BalanceAsOfBalanceCalculationDate) AS AcumuladoGerencia
                            FROM 
                                [dbo].[EMP2024] em2
                            INNER JOIN 
                                [dbo].[PlanBalances] pl2 ON em2.carnet = pl2.carnet
                            GROUP BY 
                                em2.OGERENCIA
                        )
                        SELECT 
                            em.OGERENCIA AS Gerencia,
                            em.nombre_completo AS Nombre,em.Carnet,
							em.cargo,pl.BalanceAsOfBalanceCalculationDate AcumuladoGerente,
                            g.AcumuladoGerencia
                        FROM 
                            [dbo].[EMP2024] em
                        INNER JOIN 
                            [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
                        INNER JOIN 
                            AcumuladoPorGerencia g ON em.OGERENCIA = g.OGERENCIA
                        WHERE 
                            em.cargo LIKE 'GERENTE%'
                        ORDER BY 
                            g.AcumuladoGerencia DESC ;";

                        var gerentes = connection.Query<GerenteDTO>(query).ToList();
                        gerentes = gerentes.Where(o => o.Gerencia == employee.GERENCIA).ToList();
                        return Json(new { data = gerentes }, JsonRequestBehavior.AllowGet);
                    }
                    }
                }
       
            catch (Exception ex)
            {
                // Retornar un objeto JSON con un mensaje de error para facilitar la depuración
                return Json(new { data = new List<GerenteDTO>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult ObtenerVacacionesPorPersona(string carnet)
        {
            if (string.IsNullOrEmpty(carnet))
            {
                return Json(new { error = "Carnet es requerido." }, JsonRequestBehavior.AllowGet);
            }

            //string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; // Reemplaza con tu cadena de conexión
             //(ab.CreationDate > '2024-01-01')
            string query = @"
            SELECT
                em.nombre_completo AS NombreCompleto,
                em.carnet AS Carnet,
                em.OGERENCIA AS Gerencia,
                em.primernivel AS PrimerNivel,
                em.cargo AS Cargo,
                ab.Comments AS Comentario,
                ab.CreatedBy AS Creador,
               NULLIF(ab.SubmittedDate, '') AS FechaEnvio,
                ab.StartDate AS FechaInicio,
                ab.StartTime AS HoraInicio,
                ab.EndDate AS FechaFin,
                ab.EndTime AS HoraFin,
                ab.UnitOfMeasureMeaning AS UnidadDeMedida,
                ab.Duration AS Duracion,
                ab.FormattedDuration AS DuracionFormateada,
                ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
                ab.ApprovalDatetime AS FechaDeAprobacion, lastUpdatedBy usuarioactualizo, lastUpdateDate fecha_Actualizo
            FROM emp2024 em
            INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
            WHERE
                   (ab.AbsenceType in ('Vacaciones','Enfermedad grave de un miembro del núcleo familiar que viva bajo mismo techo','Licencia por adopción','Días compensatorios')) and LegislationCode='NI' 
              AND (em.carnet = @Carnet)   AND YEAR(ab.SubmittedDate) > 2023
            ORDER BY ab.SubmittedDate desc, em.carnet, ab.StartDate";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                
                    var vacaciones = connection.Query<VacacionesPersona>(query, new { Carnet = carnet });

                    return Json(new { data = vacaciones }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Manejar el error según tus necesidades
                return Json(new { error = "Ocurrió un error al obtener las vacaciones.", detalles = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateAbsenceedit(
    long absenceId,
    string actionType,
    DateTime newStartDate,
    DateTime newEndDate,
    string newStartTime,
    string newEndTime)  
        {
            string comment = "";
            string userMode = "ADMIN";
            try
            {
                // Recuperar el registro de Session (si lo necesitas)
                List<VacacionesPersona> vacaciones = (List<VacacionesPersona>)Session["Ausenciatoda"];
                VacacionesPersona temp = vacaciones.FirstOrDefault(x => x.PersonAbsenceEntryId == absenceId);
                comment = temp?.Comentario;
                if (string.IsNullOrEmpty(comment) || comment.ToUpper() == "NULL")
                    comment = ".";
                else
                    comment += ".";

                // Si se aprueba, usamos "SUBMITTED"; si se deniega, "ORA_WITHDRAWN"
                string absenceStatusCd = (actionType.ToLower() == "approve") ? "SUBMITTED" : "ORA_WITHDRAWN";
                string body = "";
                if (absenceStatusCd == "ORA_WITHDRAWN")
                {
                    body = string.Format(@"{{ ""absenceStatusCd"": ""{0}"" }}", absenceStatusCd);
                }
                else
                {
                    body = string.Format(@"{{ 
    ""absenceStatusCd"": ""{0}"", 
    ""userMode"": ""{1}"", 
    ""startDate"": ""{2}"", 
    ""endDate"": ""{3}"", 
    ""startTime"": ""{4}"", 
    ""endTime"": ""{5}"", 
    ""comments"": ""{6}"" 
}}", absenceStatusCd, userMode, newStartDate.ToString("yyyy-MM-dd"), newEndDate.ToString("yyyy-MM-dd"), newStartTime, newEndTime, comment);
                }

                // Configurar RestClient (v104) con la URL base
                var client = new RestClient("https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com");
                // Crear la solicitud PATCH para el absenceId
                var request = new RestRequest($"/hcmRestApi/resources/latest/absences/{absenceId}", Method.PATCH);

                // Agregar cabeceras requeridas
                request.AddHeader("REST-Framework-Version", "2");
                request.AddHeader("Content-Type", "application/json");
                // Asegúrate de reemplazar "••••••" con tu valor correcto (por ejemplo, "Basic <base64Credentials>")
                request.AddHeader("Authorization", "Basic Q2xhcm9fUmhPbmxpbmVfV1NfU1M6SENNLVJIMG5sMW5lQCMz");

                // Agregar el cuerpo como parámetro de tipo RequestBody
                request.AddParameter("application/json", body, ParameterType.RequestBody);

                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

                // Esperar la respuesta usando TaskCompletionSource (RestSharp v104)
                var tcs = new TaskCompletionSource<IRestResponse>();
                client.ExecuteAsync(request, (response, handle) =>
                {
                    tcs.SetResult(response);
                });
                var responseResult = await tcs.Task;

                return Json(new { status = "OK", message = "Actualización exitosa", response = responseResult.Content }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public async Task<JsonResult> UpdateAbsenceedit2(
   long absenceId,
   string actionType,
   DateTime newStartDate,
   DateTime newEndDate,
   string newStartTime,
   string newEndTime)
        {
            int v = 0;
            string absenceStatusCd = (actionType.ToLower() == "approve") ? "SUBMITTED" : "ORA_WITHDRAWN";
            // En este ejemplo, "comment" se fija según la acción
            string comment= "";
            string userMode = "ADMIN";
            List<VacacionesPersona> vacaciones = (List<VacacionesPersona>)Session["Ausenciatoda"];
            VacacionesPersona temp = vacaciones.FirstOrDefault(x => x.PersonAbsenceEntryId == absenceId);
            comment = temp?.Comentario;
            if (string.IsNullOrEmpty(comment) || comment.ToUpper() == "NULL")
                comment = ".";
            else
                comment += ".";
            try
            {
                // Construir el payload usando formato JSON
                string payload = "";
                if (absenceStatusCd == "ORA_WITHDRAWN")
                {
                    // Para denegación se envía solo absenceStatusCd
                    payload = "{\"absenceStatusCd\":\"" + absenceStatusCd + "\"}";
                }else if (temp.FechaInicio.HasValue && temp.FechaFin.HasValue &&
    temp.FechaInicio.Value.ToString("yyyy-MM-dd") == newStartDate.ToString("yyyy-MM-dd") &&
    temp.FechaFin.Value.ToString("yyyy-MM-dd") == newEndDate.ToString("yyyy-MM-dd") &&
    temp.HoraInicio == newStartTime &&
    temp.HoraFin == newEndTime)
                {
                    payload = "{" +
                             "\"absenceStatusCd\":\"" + absenceStatusCd + "\"," +
                             "\"userMode\":\"" + userMode + "\"," +     
                             "\"comments\":\"" + comment + "\"" +
                             "}";
                }
                else
                {
                    v = 1;
                    payload = "{" +
                              //"\"absenceStatusCd\":\"" + absenceStatusCd + "\"," +
                              //"\"userMode\":\"" + userMode + "\"," +
                              "\"startDate\":\"" + newStartDate.ToString("yyyy-MM-dd") + "\"," +
                              "\"endDate\":\"" + newEndDate.ToString("yyyy-MM-dd") + "\"," +
                              "\"startTime\":\"" + newStartTime + "\"," +
                              "\"endTime\":\"" + newEndTime + "\"," +
                              "\"comments\":\"" + comment + "\"" +
                              "}";
                }

                // Crear un HttpClient y definir el método PATCH
                using (var client = new HttpClient())
                {
                    // Crear un HttpRequestMessage con método PATCH
                    var method = new HttpMethod("PATCH");
                    var request = new HttpRequestMessage(method,
                        $"https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/absences/{absenceId}");
                    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

                    // Agregar cabeceras
                    request.Headers.Add("REST-Framework-Version", "2");
                    request.Headers.Add("Authorization", "Basic Q2xhcm9fUmhPbmxpbmVfV1NfU1M6SENNLVJIMG5sMW5lQCMz"); // Reemplaza con tu cadena de autenticación
                    // Asignar el contenido
                    request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                    // Enviar la solicitud y obtener la respuesta
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    if (content!=null && content.Contains(absenceStatusCd)==true)
                    {
                        content = content.Replace("\t\t", "").Replace("\t", "");
                        var result = JsonConvert.DeserializeObject<Entities.AbsenceRecords>(content);
                        if (result == null || result.PersonAbsenceEntryId == 0)
                        {
                            return Json(new { status = "Error", message = "El registro obtenido es inválido." }, JsonRequestBehavior.AllowGet);
                        }

                        // Actualizar la base de datos usando Dapper
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string query = @"
                        UPDATE AbsenceRecords SET 
                            absenceStatusCd = @absenceStatusCd,
                            comments = @comments,
                            startDate = @startDate,
                            endDate = @endDate,
                            startTime = @startTime,
                            endTime = @endTime,
                            lastUpdateDate = @lastUpdateDate,
                            lastUpdatedBy = @lastUpdatedBy,
                            AbsenceDispStatus=@AbsenceDispStatus
                        WHERE PersonAbsenceEntryId = @PersonAbsenceEntryId;

                        IF @@ROWCOUNT = 0
                        BEGIN
                            INSERT INTO AbsenceRecords
                            (
                                PersonAbsenceEntryId,
                                absenceStatusCd,
                                comments,
                                startDate,
                                endDate,
                                startTime,
                                endTime,
                                lastUpdateDate,
                                lastUpdatedBy,AbsenceDispStatus
                            )
                            VALUES
                            (
                                @PersonAbsenceEntryId,
                                @absenceStatusCd,
                                @comments,
                                @startDate,
                                @endDate,
                                @startTime,
                                @endTime,
                                @lastUpdateDate,
                                @lastUpdatedBy,@AbsenceDispStatus
                            );
                        END";

                            int affected = await connection.ExecuteAsync(query, result);
                        }

                    }
                    return Json(new { status = "OK", message = "Actualización exitosa", response = content }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet] // /Licenses/VerAdjuntoPdf/300000449180099
        public async Task<ActionResult> VerAdjuntoPdf2(long id)
        {
            const string baseUrl =
                "https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05";

            string auth = Convert.ToBase64String(
              Encoding.GetEncoding("ISO-8859-1").GetBytes(UsuarioHcm + ":" + PasswordHcm));
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2

            try
            {
                /* 1) LISTAR ADJUNTOS --------------------------------------- */
                string listUrl = baseUrl + "/absences/" + id + "/child/absenceAttachments";
                string listJson;

                using (var client = new HttpClient())
                {
                    var listReq = new HttpRequestMessage(HttpMethod.Get, listUrl);
                     //listReq.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

                    // Agregar cabeceras
                     listReq.Headers.Add("Authorization", "Basic Q2xhcm9fUmhPbmxpbmVfV1NfU1M6SENNLVJIMG5sMW5lQCMz"); // Reemplaza con tu cadena de autenticación
                    // Asignar el contenido
                    //listReq.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                    var listResp = await client.SendAsync(listReq);
                    if (!listResp.IsSuccessStatusCode)
                        return Content("Error al consultar adjuntos: " + listResp.StatusCode, "text/plain");

                    listJson = await listResp.Content.ReadAsStringAsync();
                }

                var items = JObject.Parse(listJson)["items"];
                if (items == null)
                    return Content("La ausencia no tiene adjuntos.", "text/plain");

                /* 2) BUSCAR EL PRIMER PDF ---------------------------------- */
                string fileUrl = null;
                string fileName = null;

                foreach (var item in items)
                {
                    string mime = (string)item["UploadedFileContentType"];
                    if (!string.Equals(mime, "application/pdf", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var link = item["links"]
                               .First(l => (string)l["name"] == "FileContents")?["href"]?.ToString();
                    if (link != null)
                    {
                        fileUrl = link;
                        fileName = (string)item["FileName"];
                        break;
                    }
                }

                if (fileUrl == null)
                    return Content("La ausencia no tiene adjunto PDF.", "text/plain");

                /* 3) DESCARGAR EL PDF -------------------------------------- */
                byte[] pdfBytes;
                using (var client = new HttpClient())
                {
                    var fileReq = new HttpRequestMessage(HttpMethod.Get, fileUrl);
                    fileReq.Headers.Add("REST-Framework-Version", "2");
                     fileReq.Headers.Add("Authorization", "Basic Q2xhcm9fUmhPbmxpbmVfV1NfU1M6SENNLVJIMG5sMW5lQCMz"); // Reemplaza con tu cadena de autenticación

                    var fileResp = await client.SendAsync(fileReq);
                    if (!fileResp.IsSuccessStatusCode)
                        return Content("Error al descargar PDF: " + fileResp.StatusCode, "text/plain");

                    pdfBytes = await fileResp.Content.ReadAsByteArrayAsync();
                }

                if (pdfBytes == null || pdfBytes.Length == 0)
                    return Content("El PDF está vacío o no se pudo leer.", "text/plain");

                Response.Headers.Add("Content-Disposition", "inline; filename=" + fileName);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return Content("Excepción: " + ex.Message, "text/plain");
            }
        }

        [HttpGet]
        public async Task<ActionResult> VerAdjuntoPdf(long id)
        {
            const string baseUrl =
                 "https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05";

            string auth = Convert.ToBase64String(
              Encoding.GetEncoding("ISO-8859-1").GetBytes(UsuarioHcm + ":" + PasswordHcm));
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2

            try
            {
                /* 1) Lista los adjuntos */
                string listUrl = baseUrl + "/absences/" + id + "/child/absenceAttachments";
                string listJson;
                using (var cli = new HttpClient())
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, listUrl);
                    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

                    // Agregar cabeceras
                    req.Headers.Add("Authorization", "Basic Q2xhcm9fUmhPbmxpbmVfV1NfU1M6SENNLVJIMG5sMW5lQCMz"); // Reemplaza con tu cadena de autenticación

                    var resp = await cli.SendAsync(req);
                    if (!resp.IsSuccessStatusCode)
                        return Content("Error al consultar adjuntos.", "text/plain");
                    listJson = await resp.Content.ReadAsStringAsync();
                }

                var items = JObject.Parse(listJson)["items"];
                if (items == null)
                    return Content("La ausencia no tiene adjuntos.", "text/plain");

                /* 2) Elige el primer archivo soportado (PDF o imagen) */
                string[] mimesSoportados = {
                "application/pdf",
                "image/png",
                "image/jpeg",
                "image/jpg",
                "image/gif",
                "image/bmp",
                "image/webp",
                "image/tiff"
            };

                string fileUrl = null;
                string fileName = null;
                string mimeType = null;

                foreach (var item in items)
                {
                    string mime = (string)item["UploadedFileContentType"];
                    if (!mimesSoportados.Contains(mime, StringComparer.OrdinalIgnoreCase))
                        continue;

                    var link = item["links"]
                               .First(l => (string)l["name"] == "FileContents")?["href"]?.ToString();
                    if (link != null)
                    {
                        fileUrl = link;
                        fileName = (string)item["FileName"];
                        mimeType = mime;
                        break;
                    }
                }

                if (fileUrl == null)
                    return Content("No hay PDF ni imagen adjunta.", "text/plain");

                /* 3) Descarga el archivo */
                byte[] bytes;
                using (var cli = new HttpClient())
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, fileUrl);
                    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

                    // Agregar cabeceras
                    req.Headers.Add("Authorization", "Basic Q2xhcm9fUmhPbmxpbmVfV1NfU1M6SENNLVJIMG5sMW5lQCMz"); // Reemplaza con tu cadena de autenticación

                    var resp = await cli.SendAsync(req);
                    if (!resp.IsSuccessStatusCode)
                        return Content("Error al descargar archivo.", "text/plain");
                    bytes = await resp.Content.ReadAsByteArrayAsync();
                }

                if (bytes == null || bytes.Length == 0)
                    return Content("El archivo está vacío o no se pudo leer.", "text/plain");

                /* 4) Devuélvelo inline (PDF o imagen) */
                Response.Headers.Add("Content-Disposition", "inline; filename=" + fileName);
                return File(bytes, mimeType);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message, "text/plain");
            }
        }
 

    [HttpPost]
        public async Task<JsonResult> UpdateAbsenceedit3(
    long absenceId,
    string actionType,
    DateTime newStartDate,
    DateTime newEndDate,
    string newStartTime,
    string newEndTime)
        {
            //string user = "Claro_RhOnline_WS_SS";
            string user = "Claro_Jira_Emp_WS_SS";
            string pwd = "HCM-J1r@EmP_Int.#$";
          //  string pwd = "HCM-RH0nl1ne@#3";
            string authHeader = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(user + ":" + pwd));
 
            int v = 0;
            string absenceStatusCd = (actionType.ToLower() == "approve") ? "SUBMITTED" : "ORA_WITHDRAWN";
            // Comentario se obtiene de la sesión y se fija
            string comment = "";
            string userMode = "ADMIN";
            List<VacacionesPersona> vacaciones = (List<VacacionesPersona>)Session["Ausenciatoda"];
            VacacionesPersona temp = vacaciones.FirstOrDefault(x => x.PersonAbsenceEntryId == absenceId);
            comment = temp?.Comentario;
            if (string.IsNullOrEmpty(comment) || comment.ToUpper() == "NULL")
                comment = ".";
            else
                comment += ".";
            try
            {
                string payload = "";
                // Si se deniega, solo se envía absenceStatusCd
                if (absenceStatusCd == "ORA_WITHDRAWN")
                {
                    payload = "{\"absenceStatusCd\":\"" + absenceStatusCd + "\"}";
                }
                else if (temp.FechaInicio.HasValue && temp.FechaFin.HasValue &&
                         temp.FechaInicio.Value.ToString("yyyy-MM-dd") == newStartDate.ToString("yyyy-MM-dd") &&
                         temp.FechaFin.Value.ToString("yyyy-MM-dd") == newEndDate.ToString("yyyy-MM-dd") &&
                         temp.HoraInicio == newStartTime &&
                         temp.HoraFin == newEndTime)
                {
                    // Si las fechas y horas son iguales, se envía un payload con absenceStatusCd, userMode y comments
                    payload = "{" +
                              "\"absenceStatusCd\":\"" + absenceStatusCd + "\"," +
                              "\"userMode\":\"" + userMode + "\"" +
                               "}";
                }
                else
                {
                    // Si hay cambios en las fechas/horas, se setea v = 1 para ejecutar dos llamados:
                    v = 1;
                }
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

                // Configurar HttpClient
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = null;
                    // Si v != 1 o si se deniega o no hay cambio en fecha/hora, se realiza un único llamado
                    if (v == 0)
                    {
                        // Construir el HttpRequestMessage con método PATCH
                        var method = new HttpMethod("PATCH");
                        var request = new HttpRequestMessage(method,
                            $"https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/absences/{absenceId}");
                         request.Headers.Add("Authorization", "Basic " + authHeader);

                         request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                        response = await client.SendAsync(request);
                    }
                    else
                    {
                        // v == 1: Primero, enviar payload1 (solo absenceStatusCd, userMode y comments)
                        string payload1 = "{" +
                                          "\"absenceStatusCd\":\"" + absenceStatusCd + "\"," +
                                          "\"userMode\":\"" + userMode + "\"," +
                                          "\"startDate\":\"" + newStartDate.ToString("yyyy-MM-dd") + "\"," +
                                          "\"endDate\":\"" + newEndDate.ToString("yyyy-MM-dd") + "\"," +
                                          "\"startTime\":\"" + newStartTime + "\"," +
                                          "\"endTime\":\"" + newEndTime + "\"," +
                                          "\"comments\":\"" + comment + "\"" +
                                          "}";
                        var method = new HttpMethod("PATCH");
                        var request1 = new HttpRequestMessage(method,
                            $"https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/absences/{absenceId}");
                        request1.Headers.Add("Authorization", "Basic " + authHeader);
                         request1.Content = new StringContent(payload1, Encoding.UTF8, "application/json");

                        response = await client.SendAsync(request1);
                        // Opcional: verificar response1

                        
                    }

                    //response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string errorInsertQuery = @"
            INSERT INTO dbo.AbsenceErrors (PersonAbsenceEntryId, Content, ErrorDate)
            VALUES (@PersonAbsenceEntryId, @Content, @ErrorDate);
        ";

                            var errorParams = new
                            {
                                PersonAbsenceEntryId = absenceId,
                                Content = content, // Aquí el contenido que recibes del API (descomprimido)
                                ErrorDate = DateTime.Now
                            };

                            await connection.ExecuteAsync(errorInsertQuery, errorParams);
                        }
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string query = @"
                UPDATE AbsenceRecords SET 
                
                    SentToApi =1 
                WHERE PersonAbsenceEntryId = @PersonAbsenceEntryId;
            ";

                            var updateParams = new
                            {

                                PersonAbsenceEntryId = absenceId
                            };

                            int affected = await connection.ExecuteAsync(query, updateParams);
                        }

                        // Insertar en AbsenceApprovals
                        Entities.Employees employee = (Entities.Employees)Session["User"];
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string insertQuery = @"
                INSERT INTO dbo.AbsenceApprovals (PersonAbsenceEntryId, EmployeeNumber, ApprovalDate, ApprovalTime)
                VALUES (@PersonAbsenceEntryId, @EmployeeNumber, @ApprovalDate, @ApprovalTime)
            ";

                            var parameters = new
                            {
                                PersonAbsenceEntryId = absenceId,
                                EmployeeNumber = employee.EmployeeNumber,
                                ApprovalDate = DateTime.Now.Date,
                                ApprovalTime = DateTime.Now.TimeOfDay
                            };

                            await connection.ExecuteAsync(insertQuery, parameters);
                        }
                        string titulo = "Error en la solicitud de ausencia";
                        // Armar el mensaje, incluyendo el Carnet y el texto de caso especial.
                        string mensaje = "" +
                      "Se ha detectado un error "+ content+" en la solicitud de ausencia correspondiente al colaborador:<br/>" +
                      "<b>" + temp.NombreCompleto + " (Carnet: " + temp.Carnet + ")</b><br/><br/>" +
                      "Detalles de la solicitud:<br/>" +
                      "Fecha de solicitud: " + (temp.FechaEnvio.HasValue
                                                 ? temp.FechaEnvio.Value.ToString("dd/MM/yyyy")
                                                 : "No definida") + "<br/>" +
                      "Tipo de ausencia: " + temp.AbsenceType + "<br/><br/>" +
                      "Por favor, revise la información y proceda con las correcciones necesarias.<br/><br/>" +
                      "Saludos cordiales.";

                        // Enviar el correo llamando al método getcorreo previamente definido
                        SendNotificationEmail(titulo, mensaje);
                      
                            return Json(new { status = "Error", message = content }, JsonRequestBehavior.AllowGet);
                    }

                    if (content != null && content.Contains(absenceStatusCd))
                    {
                        content = content.Replace("\t\t", "").Replace("\t", "");
                        // Se asume que se retorna un JSON que se puede mapear a Entities.AbsenceRecords
                        var result = JsonConvert.DeserializeObject<Entities.AbsenceRecords>(content);
                        if (result == null || result.PersonAbsenceEntryId == 0)
                        {
                            return Json(new { status = "Error", message = "El registro obtenido es inválido." }, JsonRequestBehavior.AllowGet);
                        }

                        // Actualizar la base de datos local usando Dapper (consulta de ejemplo)
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string query = @"
                        UPDATE AbsenceRecords SET 
                            absenceStatusCd = @absenceStatusCd,
                            comments = @comments,
                            startDate = @startDate,
                            endDate = @endDate,
                            startTime = @startTime,
                            endTime = @endTime,
                            lastUpdateDate = @lastUpdateDate,
                            lastUpdatedBy = @lastUpdatedBy,
                            AbsenceDispStatus = @AbsenceDispStatus,
  SentToApi=1
                        WHERE PersonAbsenceEntryId = @PersonAbsenceEntryId;
 ";

                            int affected = await connection.ExecuteAsync(query, result);
                        }
                        Entities.Employees employee = (Entities.Employees)Session["User"];


                        using (var connection = new SqlConnection(connectionString))
                        {
                            string insertQuery = @"
        INSERT INTO dbo.AbsenceApprovals (PersonAbsenceEntryId, EmployeeNumber, ApprovalDate, ApprovalTime)
        VALUES (@PersonAbsenceEntryId, @EmployeeNumber, @ApprovalDate, @ApprovalTime)";
                            var parameters = new
                            {
                                PersonAbsenceEntryId = absenceId,
                                EmployeeNumber = employee.EmployeeNumber,
                                ApprovalDate = DateTime.Now.Date,
                                ApprovalTime = DateTime.Now.TimeOfDay
                            };
                            await connection.ExecuteAsync(insertQuery, parameters);

                            if (absenceStatusCd == "ORA_WITHDRAWN")
                            {

                                if (!string.IsNullOrWhiteSpace(temp.correo))
                                {
                                    string titulo = "Solicitud de ausencia denegada – " + temp.AbsenceType + " (" +
                  (temp.FechaEnvio.HasValue
                      ? temp.FechaEnvio.Value.ToString("dd/MM/yyyy")
                      : "Fecha no definida") +
                  ", estado: " + temp.EstadoDeDisponibilidad + ")";


                                    string mensaje = "Estimado/a <b>" + temp.NombreCompleto + "</b>,<br/><br/>" +

                                        "Le informamos que su solicitud de ausencia ha sido <b>denegada</b> tras el proceso de revisión.<br/><br/>" +

                                        "<u><b>Detalles de la solicitud:</b></u><br/>" +
                                        "- Fecha de solicitud: " + (temp.FechaEnvio.HasValue
                                            ? temp.FechaEnvio.Value.ToString("dd/MM/yyyy")
                                            : "No definida") + "<br/>" +
                                        "- Tipo de ausencia: " + temp.AbsenceType + "<br/>" +
                                        "- Estado anterior: " + temp.EstadoDeDisponibilidad + "<br/><br/>" +

                                        "Si tiene dudas o desea más información, puede comunicarse con el área de <b>Relaciones Laborales</b>.<br/><br/>" +

                                        "Gracias por su atención.<br/><br/>" +
                                        "Atentamente,<br/>" +
                                        "<b>Gerencia de Recursos Humanos</b>";



                                    SendNotificationEmailanulada(titulo, mensaje, temp.correo); // Asumiendo que este método recibe destinatario
                                }
                            }
                        }
                    }
                    return Json(new { status = "OK", message = "Actualización exitosa", response = content }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("400 (Bad Request)"))
                {
                    // Actualizar AbsenceRecords con SentToApi = 2
                    using (var connection = new SqlConnection(connectionString))
                    {
                        string query = @"
                UPDATE AbsenceRecords SET 
                
                    SentToApi =1 
                WHERE PersonAbsenceEntryId = @PersonAbsenceEntryId;
            ";

                        var updateParams = new
                        {
                           
                            PersonAbsenceEntryId = absenceId
                        };

                        int affected = await connection.ExecuteAsync(query, updateParams);
                    }

                    // Insertar en AbsenceApprovals
                    Entities.Employees employee = (Entities.Employees)Session["User"];
                    using (var connection = new SqlConnection(connectionString))
                    {
                        string insertQuery = @"
                INSERT INTO dbo.AbsenceApprovals (PersonAbsenceEntryId, EmployeeNumber, ApprovalDate, ApprovalTime)
                VALUES (@PersonAbsenceEntryId, @EmployeeNumber, @ApprovalDate, @ApprovalTime)
            ";

                        var parameters = new
                        {
                            PersonAbsenceEntryId = absenceId,
                            EmployeeNumber = employee.EmployeeNumber,
                            ApprovalDate = DateTime.Now.Date,
                            ApprovalTime = DateTime.Now.TimeOfDay
                        };

                        await connection.ExecuteAsync(insertQuery, parameters);
                    }
                    string titulo = "Error en la solicitud de ausencia";
                    // Armar el mensaje, incluyendo el Carnet y el texto de caso especial.
                    string mensaje = "" +
                  "Se ha detectado un error en la solicitud de ausencia correspondiente al colaborador:<br/>" +
                  "<b>" + temp.NombreCompleto + " (Carnet: " + temp.Carnet + ")</b><br/><br/>" +
                  "Detalles de la solicitud:<br/>" +
                  "Fecha de solicitud: " + (temp.FechaEnvio.HasValue
                                             ? temp.FechaEnvio.Value.ToString("dd/MM/yyyy")
                                             : "No definida") + "<br/>" +
                  "Tipo de ausencia: " + temp.AbsenceType + "<br/><br/>" +
                  "Por favor, revise la información y proceda con las correcciones necesarias.<br/><br/>" +
                  "Saludos cordiales.";

                    // Enviar el correo llamando al método getcorreo previamente definido
                    SendNotificationEmail(titulo, mensaje);
                    return Json(new { status = "Advertencia", message = "problema con el registro", response = ""}, JsonRequestBehavior.AllowGet);

                }
                else 
                return Json(new { status = "Error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    

        [HttpPost]
        public async Task<JsonResult> UpdateAbsenceedit4(
  long absenceId,
  string actionType )
        {
            string user = "Claro_Jira_Emp_WS_SS";
            string pwd = "HCM-J1r@EmP_Int.#$";
            string authHeader = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(user + ":" + pwd));

             string absenceStatusCd = (actionType.ToLower() == "approve") ? "SUBMITTED" : "ORA_WITHDRAWN";
            // Comentario se obtiene de la sesión y se fija
            string comment = "";
            string userMode = "ADMIN";
            List<VacacionesPersona> vacaciones = (List<VacacionesPersona>)Session["Ausenciatoda"];
            VacacionesPersona temp = vacaciones.FirstOrDefault(x => x.PersonAbsenceEntryId == absenceId);
            comment = temp?.Comentario;
            if (string.IsNullOrEmpty(comment) || comment.ToUpper() == "NULL")
                comment = ".";
            else
                comment += ".";
            try
            {
                string payload = "";
                // Si se deniega, solo se envía absenceStatusCd
                if (absenceStatusCd == "ORA_WITHDRAWN")
                {
                    payload = "{\"absenceStatusCd\":\"" + absenceStatusCd + "\"}";
                }
                else { 
                    // Si las fechas y horas son iguales, se envía un payload con absenceStatusCd, userMode y comments
                    payload = "{" +
                              "\"absenceStatusCd\":\"" + absenceStatusCd + "\"," +
                              "\"userMode\":\"" + userMode + "\"" +
                               "}";
                }
                 

                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                // Configurar HttpClient
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = null;
                    // Si v != 1 o si se deniega o no hay cambio en fecha/hora, se realiza un único llamado
                  
                        // Construir el HttpRequestMessage con método PATCH
                        var method = new HttpMethod("PATCH");
                        var request = new HttpRequestMessage(method,
                            $"https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/absences/{absenceId}");
                        request.Headers.Add("Authorization", "Basic " + authHeader);

                        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                        response = await client.SendAsync(request);
                
                  
                    
                   var content = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string errorInsertQuery = @"
            INSERT INTO dbo.AbsenceErrors (PersonAbsenceEntryId, Content, ErrorDate)
            VALUES (@PersonAbsenceEntryId, @Content, @ErrorDate);
        ";

                            var errorParams = new
                            {
                                PersonAbsenceEntryId = absenceId,
                                Content = content, // Aquí el contenido que recibes del API (descomprimido)
                                ErrorDate = DateTime.Now
                            };

                            await connection.ExecuteAsync(errorInsertQuery, errorParams);
                        }
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string query = @"
                            UPDATE AbsenceRecords SET 

                                SentToApi =1 
                            WHERE PersonAbsenceEntryId = @PersonAbsenceEntryId;
                        ";

                            var updateParams = new
                            {

                                PersonAbsenceEntryId = absenceId
                            };

                            int affected = await connection.ExecuteAsync(query, updateParams);
                        }

                        // Insertar en AbsenceApprovals
                        Entities.Employees employee = (Entities.Employees)Session["User"];
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string insertQuery = @"
                            INSERT INTO dbo.AbsenceApprovals (PersonAbsenceEntryId, EmployeeNumber, ApprovalDate, ApprovalTime)
                            VALUES (@PersonAbsenceEntryId, @EmployeeNumber, @ApprovalDate, @ApprovalTime)
                        ";

                            var parameters = new
                            {
                                PersonAbsenceEntryId = absenceId,
                                EmployeeNumber = employee.EmployeeNumber,
                                ApprovalDate = DateTime.Now.Date,
                                ApprovalTime = DateTime.Now.TimeOfDay
                            };

                            await connection.ExecuteAsync(insertQuery, parameters);
                        }
                        string titulo = "Error en la solicitud de ausencia";
                        // Armar el mensaje, incluyendo el Carnet y el texto de caso especial.
                        string mensaje = "" +
                      "Se ha detectado un error "+ content+" en la solicitud de ausencia correspondiente al colaborador:<br/>" +
                      "<b>" + temp.NombreCompleto + " (Carnet: " + temp.Carnet + ")</b><br/><br/>" +
                      "Detalles de la solicitud:<br/>" +
                      "Fecha de solicitud: " + (temp.FechaEnvio.HasValue
                                                 ? temp.FechaEnvio.Value.ToString("dd/MM/yyyy")
                                                 : "No definida") + "<br/>" +
                      "Tipo de ausencia: " + temp.AbsenceType + "<br/><br/>" +
                      "Por favor, revise la información y proceda con las correcciones necesarias.<br/><br/>" +
                      "Saludos cordiales.";

                        // Enviar el correo llamando al método getcorreo previamente definido
                        SendNotificationEmail(titulo, mensaje);
 
                        return Json(new { status = "Error", message = content, response = content }, JsonRequestBehavior.AllowGet);
                    }
                    if (content != null && content.Contains(absenceStatusCd))
                    {
                        content = content.Replace("\t\t", "").Replace("\t", "");
                        // Se asume que se retorna un JSON que se puede mapear a Entities.AbsenceRecords
                        var result = JsonConvert.DeserializeObject<Entities.AbsenceRecords>(content);
                        if (result == null || result.PersonAbsenceEntryId == 0)
                        {
                            return Json(new { status = "Error", message = "El registro obtenido es inválido." }, JsonRequestBehavior.AllowGet);
                        }

                        // Actualizar la base de datos local usando Dapper (consulta de ejemplo)
                        using (var connection = new SqlConnection(connectionString))
                        {
                            string query = @"
                        UPDATE AbsenceRecords SET 
                            absenceStatusCd = @absenceStatusCd,
                            comments = @comments,
                            startDate = @startDate,
                            endDate = @endDate,
                            startTime = @startTime,
                            endTime = @endTime,
                            lastUpdateDate = @lastUpdateDate,
                            lastUpdatedBy = @lastUpdatedBy,
                            AbsenceDispStatus = @AbsenceDispStatus,
                            SentToApi=1
                        WHERE PersonAbsenceEntryId = @PersonAbsenceEntryId;

                        ";

                            int affected = await connection.ExecuteAsync(query, result);
                        }
                        Entities.Employees employee = (Entities.Employees)Session["User"];


                        using (var connection = new SqlConnection(connectionString))
                        {
                            string insertQuery = @"
        INSERT INTO dbo.AbsenceApprovals (PersonAbsenceEntryId, EmployeeNumber, ApprovalDate, ApprovalTime)
        VALUES (@PersonAbsenceEntryId, @EmployeeNumber, @ApprovalDate, @ApprovalTime)";
                            var parameters = new
                            {
                                PersonAbsenceEntryId = absenceId,
                                EmployeeNumber = employee.EmployeeNumber,
                                ApprovalDate = DateTime.Now.Date,
                                ApprovalTime = DateTime.Now.TimeOfDay
                            };
                            await connection.ExecuteAsync(insertQuery, parameters);
                        }
                        if (absenceStatusCd == "ORA_WITHDRAWN")
                        {

                            if (!string.IsNullOrWhiteSpace(temp.correo))
                            {
                                string titulo = "Solicitud de ausencia denegada – " + temp.AbsenceType + " (" +
              (temp.FechaEnvio.HasValue
                  ? temp.FechaEnvio.Value.ToString("dd/MM/yyyy")
                  : "Fecha no definida") +
              ", estado: " + temp.EstadoDeDisponibilidad + ")";


                                string mensaje = "Estimado/a <b>" + temp.NombreCompleto + "</b>,<br/><br/>" +

                                    "Le informamos que su solicitud de ausencia ha sido <b>denegada</b> tras el proceso de revisión.<br/><br/>" +

                                    "<u><b>Detalles de la solicitud:</b></u><br/>" +
                                    "- Fecha de solicitud: " + (temp.FechaEnvio.HasValue
                                        ? temp.FechaEnvio.Value.ToString("dd/MM/yyyy")
                                        : "No definida") + "<br/>" +
                                    "- Tipo de ausencia: " + temp.AbsenceType + "<br/>" +
                                    "- Estado anterior: " + temp.EstadoDeDisponibilidad + "<br/><br/>" +

                                    "Si tiene dudas o desea más información, puede comunicarse con el área de <b>Relaciones Laborales</b>.<br/><br/>" +

                                    "Gracias por su atención.<br/><br/>" +
                                    "Atentamente,<br/>" +
                                    "<b>Gerencia de Recursos Humanos</b>";



                                SendNotificationEmailanulada(titulo, mensaje, temp.correo); // Asumiendo que este método recibe destinatario
                            }
                        }
                    }
                    return Json(new { status = "OK", message = "Actualización exitosa", response = content }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("400 (Bad Request)"))
                {
                    // Actualizar AbsenceRecords con SentToApi = 2
                    using (var connection = new SqlConnection(connectionString))
                    {
                        string query = @"
                UPDATE AbsenceRecords SET 
               
                    SentToApi = 1 
                WHERE PersonAbsenceEntryId = @PersonAbsenceEntryId;
            ";

                        var updateParams = new
                        {
                         
                            PersonAbsenceEntryId = absenceId
                        };

                        int affected = await connection.ExecuteAsync(query, updateParams);
                    }

                    // Insertar en AbsenceApprovals
                    Entities.Employees employee = (Entities.Employees)Session["User"];
                    using (var connection = new SqlConnection(connectionString))
                    {
                        string insertQuery = @"
                INSERT INTO dbo.AbsenceApprovals (PersonAbsenceEntryId, EmployeeNumber, ApprovalDate, ApprovalTime)
                VALUES (@PersonAbsenceEntryId, @EmployeeNumber, @ApprovalDate, @ApprovalTime)
            ";

                        var parameters = new
                        {
                            PersonAbsenceEntryId = absenceId,
                            EmployeeNumber = employee.EmployeeNumber,
                            ApprovalDate = DateTime.Now.Date,
                            ApprovalTime = DateTime.Now.TimeOfDay
                        };

                        await connection.ExecuteAsync(insertQuery, parameters);
                    }
                    string titulo = "Error en la solicitud de ausencia";
                    // Armar el mensaje, incluyendo el Carnet y el texto de caso especial.
                    string mensaje = "" +
                    "Se ha detectado un error en la solicitud de ausencia correspondiente al colaborador:<br/>" +
                    "<b>" + temp.NombreCompleto + " (Carnet: " + temp.Carnet + ")</b><br/><br/>" +
                    "Detalles de la solicitud:<br/>" +
                    "Fecha de solicitud: " + (temp.FechaEnvio.HasValue
                                               ? temp.FechaEnvio.Value.ToString("dd/MM/yyyy")
                                               : "No definida") + "<br/>" +
                    "Tipo de ausencia: " + temp.AbsenceType + "<br/><br/>" +
                    "Por favor, revise la información y proceda con las correcciones necesarias.<br/><br/>" +
                    "Saludos cordiales.";

                    // Enviar el correo llamando al método getcorreo previamente definido
                    SendNotificationEmail(  titulo, mensaje);
                    return Json(new { status = "Advertencia", message = "problema con el registro", response = "" }, JsonRequestBehavior.AllowGet);

                }
                else
                    return Json(new { status = "Error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult ObtenerVacacionesPorGerencia(string gerencia)
        {
            if (string.IsNullOrEmpty(gerencia))
            {
                return Json(new { error = "gerencia es requerido." }, JsonRequestBehavior.AllowGet);
            }
            //gerencia = "NI GERENCIA " + gerencia;
            //string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; // Reemplaza con tu cadena de conexión
             //(ab.CreationDate > '2024-01-01')
            string query = @"
            SELECT
                em.nombre_completo AS NombreCompleto,
                em.carnet AS Carnet,
                em.OGERENCIA AS Gerencia,
                em.primernivel AS PrimerNivel,
                em.cargo AS Cargo,
                ab.Comments AS Comentario,
                ab.CreatedBy AS Creador,
                NULLIF(ab.SubmittedDate, '') AS FechaEnvio,
                ab.StartDate AS FechaInicio,
                ab.StartTime AS HoraInicio,
                ab.EndDate AS FechaFin,
                ab.EndTime AS HoraFin,
                ab.UnitOfMeasureMeaning AS UnidadDeMedida,
                ab.Duration AS Duracion,
                ab.FormattedDuration AS DuracionFormateada,
                ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
                ab.ApprovalDatetime AS FechaDeAprobacion, lastUpdatedBy usuarioactualizo, lastUpdateDate fecha_Actualizo
            FROM emp2024 em
            INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
            WHERE
         
                             (ab.AbsenceType IN (
    'Vacaciones',
    'Enfermedad grave de un miembro del núcleo familiar que viva bajo mismo techo',
    'Licencia por adopción',
    'Descanso durante la jornada por condición de gravidez',
    'Permiso para renovar licencia',
    'Comparecencia a procesos administrativos o judiciales',
    'Lactancia materna',
    'Justificación',
    'Permiso para atención de hijos con discapacidad',
    'Permiso especial',
    'Fallecimiento de un miembro del núcleo familiar',
    'Consulta médica hijos menores',
    'Consulta médica hijos discapacitados de cualquier edad',
    'Permiso para el hombre por nacimiento de hijo',
    'Fallecimiento de un miembro del núcleo familiar - ENITEL',
    'Consulta médica colaborador',
    'Matrimonio'
)) and LegislationCode='NI' 

              AND (em.OGERENCIA = @gerencia)  AND YEAR(ab.SubmittedDate) >= 2025
            ORDER BY ab.SubmittedDate desc, em.carnet, ab.StartDate";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {

                    var vacaciones = connection.Query<VacacionesPersona>(query, new { gerencia = gerencia });

                    return Json(new { data = vacaciones }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Manejar el error según tus necesidades
                return Json(new { error = "Ocurrió un error al obtener las vacaciones.", detalles = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult HistorialPorCarnet(string carnet)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var sql = @"
        SELECT
            em.nombre_completo AS NombreCompleto,
            em.carnet AS Carnet,
            em.OGERENCIA AS Gerencia, em.correo,
            em.primernivel AS PrimerNivel,
            em.cargo AS Cargo,
            ab.Comments AS Comentario,
            ab.CreatedBy AS Creador,
            NULLIF(ab.SubmittedDate, '') AS FechaEnvio,
            ab.StartDate AS FechaInicio,
            ab.StartTime AS HoraInicio,
            ab.EndDate AS FechaFin,
            ab.EndTime AS HoraFin,
            ab.UnitOfMeasureMeaning AS UnidadDeMedida,
            ab.Duration AS Duracion,
            ab.FormattedDuration AS DuracionFormateada,
            ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
            ab.ApprovalDatetime AS FechaDeAprobacion, 
            ab.lastUpdatedBy AS UsuarioActualizo, 
            ab.lastUpdateDate AS Fecha_Actualizo, 
            ab.PersonAbsenceEntryId, 
            ab.AbsenceType
        FROM emp2024 em
        INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
        WHERE em.carnet = @carnet
        ORDER BY ab.SubmittedDate DESC, ab.StartDate, ab.EndDate";

                var vacaciones = db.Query<VacacionesPersona>(sql, new { carnet }).ToList();
                return Json(vacaciones, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult ObtenerVacacionesErrores()
        {
            string sql = @"
SELECT
    em.nombre_completo AS NombreCompleto,
    em.carnet          AS Carnet,
    ab.AbsenceType,
    ab.StartDate       AS FechaInicio,
    ab.EndDate         AS FechaFin,
    ab.FormattedDuration AS DuracionFormateada,
    ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
    NULLIF(ab.SubmittedDate,'') AS FechaEnvio,
    err.Content        AS ErrorContenido,
    err.ErrorDate      AS FechaError,
    ab.PersonAbsenceEntryId
FROM EmpleadosVWEF em
JOIN AbsenceRecords ab
  ON em.carnet = ab.PersonNumber
LEFT JOIN AbsenceErrors err
  ON err.PersonAbsenceEntryId = ab.PersonAbsenceEntryId
WHERE
  ab.LegislationCode = 'NI'
  AND ab.AbsenceType IN (
    'Vacaciones',
    'Enfermedad grave de un miembro del núcleo familiar que viva bajo mismo techo',
    'Licencia por adopción',
    'Descanso durante la jornada por condición de gravidez',
    'Permiso para renovar licencia',
    'Comparecencia a procesos administrativos o judiciales',
    'Lactancia materna',
    'Justificación',
    'Permiso para atención de hijos con discapacidad',
    'Permiso especial',
    'Fallecimiento de un miembro del núcleo familiar',
    'Consulta médica hijos menores',
    'Consulta médica hijos discapacitados de cualquier edad',
    'Permiso para el hombre por nacimiento de hijo',
    'Fallecimiento de un miembro del núcleo familiar - ENITEL',
    'Consulta médica colaborador',
    'Matrimonio'
,'Días compensatorios'
  )
  AND ab.AbsenceDispStatusMeaning IN ('En espera de aprobación','Guardado') -- no aprobadas
  AND (
      2025 >  YEAR(ab.SubmittedDate)
       OR EXISTS (SELECT 1 FROM AbsenceErrors e WHERE e.PersonAbsenceEntryId = ab.PersonAbsenceEntryId)
  )
ORDER BY
  CASE WHEN err.ErrorDate IS NULL THEN 1 ELSE 0 END,  -- primero con error
  ISNULL(err.ErrorDate, '1900-01-01') DESC,
  TRY_CONVERT(date, NULLIF(ab.SubmittedDate,''), 126) DESC,
  em.carnet, ab.StartDate;
";

            using (var cn = new SqlConnection(
               connectionString))
            {
                var lista = cn.Query<AusenciaPopupDto>(sql).ToList();
                return Json(new { items = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerVacacionesPorGerenciatodo()
        {
            int anioMinimo = 2024; bool soloNoEnviadas = true;
            //gerencia = "NI GERENCIA " + gerencia;
            //string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; // Reemplaza con tu cadena de conexión
            //(ab.CreationDate > '2024-01-01')
            string query = @"
            SELECT
                em.nombre_completo AS NombreCompleto,
                em.carnet AS Carnet,
                em.OGERENCIA AS Gerencia,em.correo,
                em.primernivel AS PrimerNivel,
                em.cargo AS Cargo,
                ab.Comments AS Comentario,
                ab.CreatedBy AS Creador,
 NULLIF(ab.SubmittedDate, '') AS FechaEnvio,    
ab.StartDate AS FechaInicio,
                ab.StartTime AS HoraInicio,
                ab.EndDate AS FechaFin,
                ab.EndTime AS HoraFin,
                ab.UnitOfMeasureMeaning AS UnidadDeMedida,
                ab.Duration AS Duracion,
                ab.FormattedDuration AS DuracionFormateada,
                ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
                ab.ApprovalDatetime AS FechaDeAprobacion, lastUpdatedBy usuarioactualizo, lastUpdateDate fecha_Actualizo, ab.PersonAbsenceEntryId,ab.AbsenceType
            FROM EmpleadosVWEF em
            INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
            WHERE
         
                                               (ab.AbsenceType IN (
    'Vacaciones',
    'Enfermedad grave de un miembro del núcleo familiar que viva bajo mismo techo',
    'Licencia por adopción',
    'Descanso durante la jornada por condición de gravidez',
    'Permiso para renovar licencia',
    'Comparecencia a procesos administrativos o judiciales',
    'Lactancia materna',
    'Justificación',
    'Permiso para atención de hijos con discapacidad',
    'Permiso especial',
    'Fallecimiento de un miembro del núcleo familiar',
    'Consulta médica hijos menores',
    'Consulta médica hijos discapacitados de cualquier edad',
    'Permiso para el hombre por nacimiento de hijo',
    'Fallecimiento de un miembro del núcleo familiar - ENITEL',
    'Consulta médica colaborador',
    'Matrimonio','Días compensatorios'
)) and SentToApi = 0 
               AND (AbsenceDispStatusMeaning IN ('En espera de aprobación',  'Guardado')) AND YEAR(ab.SubmittedDate) > 2024
            ORDER BY   ab.SubmittedDate desc, em.carnet, ab.StartDate ";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {

                    //var vacaciones = connection.Query<VacacionesPersona>(query);

                    var vacaciones = connection.Query<VacacionesPersona>(
                "dbo.usp_Vacaciones_PendientesPorGerencia_Todo",
                new { AnioMinimo = anioMinimo, SoloNoEnviadas = soloNoEnviadas ? 1 : 0 },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 90
            ).ToList();
                    Session["Ausenciatoda"] = vacaciones.ToList();
                    return new JsonResult
                    {
                        Data = new { data = vacaciones },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                        MaxJsonLength = int.MaxValue
                    };
                 }
            }
            catch (Exception ex)
            {
                // Manejar el error según tus necesidades
                return Json(new { error = "Ocurrió un error al obtener las vacaciones.", detalles = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateAbsencesMassive(string absenceIds, string actionType)
        {
            string userMode = "ADMIN";
            try
            {

                List<VacacionesPersona> vacaciones = new List<VacacionesPersona>();
                vacaciones= (List<VacacionesPersona>)Session["Ausenciatoda"];
                 var ids = absenceIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> responses = new List<string>();

                foreach (var idStr in ids)
                { long id = 0;
                    id = long.Parse(idStr);
                    string comment = "";
                    VacacionesPersona temp = vacaciones.Where(x => x.PersonAbsenceEntryId == id).FirstOrDefault();
                    comment = temp.Comentario + ".";
                    if (long.TryParse(idStr, out long absenceId))
                    {
                        // Crear el cliente RestSharp con la URL base
                        var client = new RestClient("https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com");

                        // Crear la solicitud PATCH para la ausencia específica
                        var request = new RestRequest("/hcmRestApi/resources/latest/absences/" + absenceId, Method.PATCH);

                        // Agregar cabeceras necesarias
                        request.AddHeader("REST-Framework-Version", "2");
                        request.AddHeader("Content-Type", "application/json");
                        // Reemplaza "••••••" con tu cadena de autorización (por ejemplo, "Basic <base64credentials>")
                        request.AddHeader("Authorization", "••••••");

                        // Determinar el estado según la acción:
                        // Si actionType es "approve" se usa "SUBMITTED", de lo contrario "ORA_WITHDRAWN"
                        string absenceStatusCd = (actionType.ToLower() == "approve") ? "SUBMITTED" : "ORA_WITHDRAWN";

                        // Construir el cuerpo JSON de la solicitud, agregando también el comentario
                        var body = @"{
    ""absenceStatusCd"": """ + absenceStatusCd + @""",
    ""userMode"": """ + userMode + @""",
    ""comments"": """ + comment + @"""
}";
                        // Agregar el cuerpo como parámetro de tipo RequestBody (RestSharp v104)
                        request.AddParameter("application/json", body, ParameterType.RequestBody);

                        // Usar TaskCompletionSource para esperar la respuesta de ExecuteAsync
                        var tcs = new TaskCompletionSource<IRestResponse>();
                        client.ExecuteAsync(request, (response, handle) =>
                        {
                            tcs.SetResult(response);
                        });
                        var responseResult = await tcs.Task;

                        responses.Add($"ID: {absenceId}, Response: {responseResult.Content}");
                    }
                }

                return Json(new { status = "OK", message = "Actualización masiva completada", responses = responses }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private void SendNotificationEmailanulada(string titulo, string mensaje,string correo)
        {

            string output = null;
            //MailMessage email = new MailMessage();


            //email.From = new MailAddress("Recursoshumanos@claro.com.ni");
            //email.Subject = titulo;
            //email.SubjectEncoding = System.Text.Encoding.UTF8;

            //email.To.Add(correo);
            //email.CC.Add("nelson.perez@claro.com.ni");
            //email.CC.Add("marlene.rosales@claro.com.ni");
            //email.CC.Add("mariav.sequeira@claro.com.ni");
            //email.Bcc.Add("gustavo.lira@claro.com.ni");



            //email.Body = mensaje;
            //email.BodyEncoding = System.Text.Encoding.UTF8;
            //email.IsBodyHtml = true;
            //email.Priority = MailPriority.Normal;
            //email.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.OnSuccess |
            //            DeliveryNotificationOptions.Delay;
            //System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;


            //ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
            //SmtpClient cliente = new SmtpClient("10.200.5.23", 587); // IP y puerto de FortiMail
            // cliente.Credentials = new NetworkCredential("recursoshumanos@claro.com.ni", "Enero&272025"); // Eliminar antes de producción
            //cliente.Credentials = new NetworkCredential("transporte@claro.com.ni", "Enero&r546"); // Eliminar antes de producción                                                            //cliente.Credentials = new NetworkCredential("transporte@claro.com.ni", "Enero&r546"); // Eliminar antes de producción
            //cliente.EnableSsl = true;


            try
            {
                //cliente.Send(email);
                //email.Dispose();
                output = "EXITO";
            }
            catch (Exception ex)
            {
                output = ex.InnerException.Message;
            }

        }

        private void SendNotificationEmail(  string titulo, string mensaje)
        {
 
                string output = null;
                MailMessage email = new MailMessage();
 
 
                email.From = new MailAddress("Recursoshumanos@claro.com.ni");
                email.Subject = titulo;
                email.SubjectEncoding = System.Text.Encoding.UTF8;
                
              email.To.Add("nelson.perez@claro.com.ni");
              email.To.Add("marlene.rosales@claro.com.ni");
              email.CC.Add("mariav.sequeira@claro.com.ni");
             email.Bcc.Add("gustavo.lira@claro.com.ni");


 
                email.Body = mensaje;
                email.BodyEncoding = System.Text.Encoding.UTF8;
                email.IsBodyHtml = true;
                email.Priority = MailPriority.Normal;
                email.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.OnSuccess |
                            DeliveryNotificationOptions.Delay;


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
             
            }
        //[HttpGet]
        public ActionResult ObtenerVacacionesPorGerencia(DateTime fechaInicio, DateTime fechaFin)
        {
            // Validar fechas
            if (fechaInicio == default || fechaFin == default || fechaInicio > fechaFin)
            {
                return Json(new { error = "El rango de fechas es inválido." }, JsonRequestBehavior.AllowGet);
            }

            // Consulta SQL con filtro por fechas
            string query = @"
        SELECT
            em.nombre_completo AS NombreCompleto,
            em.carnet AS Carnet,
            em.OGERENCIA AS Gerencia,
            em.primernivel AS PrimerNivel,
            em.cargo AS Cargo,
            ab.Comments AS Comentario,
            ab.CreatedBy AS Creador,
            NULLIF(ab.SubmittedDate, '') AS FechaEnvio,
            ab.StartDate AS FechaInicio,
            ab.StartTime AS HoraInicio,
            ab.EndDate AS FechaFin,
            ab.EndTime AS HoraFin,
            ab.UnitOfMeasureMeaning AS UnidadDeMedida,
            ab.Duration AS Duracion,
            ab.FormattedDuration AS DuracionFormateada,
            ab.AbsenceDispStatusMeaning AS EstadoDeDisponibilidad,
            ab.ApprovalDatetime AS FechaDeAprobacion,
            lastUpdatedBy AS usuarioactualizo,
            lastUpdateDate AS fecha_Actualizo
        FROM emp2024 em
        INNER JOIN AbsenceRecords ab ON em.carnet = ab.PersonNumber
        WHERE
             (ab.AbsenceType in ('Vacaciones','Enfermedad grave de un miembro del núcleo familiar que viva bajo mismo techo','Licencia por adopción','Días compensatorios')) and    LegislationCode='NI' 
            AND ((ab.StartDate >= @fechaInicio and ab.StartDate<=fechaFin)or 
            (ab.EndDate >= @fechaInicio ab.EndDate <= @fechaFin))  AND YEAR(ab.SubmittedDate) > 2023
                              
             ORDER BY   ab.SubmittedDate desc, em.carnet, ab.StartDate";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var vacaciones = connection.Query<VacacionesPersona>(query, new { fechaInicio, fechaFin });
                    return Json(new { data = vacaciones }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Manejar errores
                return Json(new { error = "Ocurrió un error al obtener las vacaciones.", detalles = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerEmpleadosPorGerencia(string gerencia)
        {
            try
            {
                if (gerencia=="nada")
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        string query = @"
                    SELECT 
                        em.nombre_completo AS NombreCompleto,em.carnet,
                        em.cargo AS Cargo,
                        em.primernivel AS Area,
                        pl.BalanceAsOfBalanceCalculationDate AS Acumulado
                    FROM [dbo].[EMP2024] em
                    INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
 
                    ORDER BY pl.BalanceAsOfBalanceCalculationDate DESC";

                        var empleados = connection.Query<EmpleadoDTO>(query ).ToList();

                        return Json(new { data = empleados }, JsonRequestBehavior.AllowGet);
                    }
                }
                else {
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = @"
                    SELECT 
                        em.nombre_completo AS NombreCompleto,em.carnet,
                        em.cargo AS Cargo,
                        em.primernivel AS Area,
                        pl.BalanceAsOfBalanceCalculationDate AS Acumulado
                    FROM [dbo].[EMP2024] em
                    INNER JOIN [dbo].[PlanBalances] pl ON em.carnet = pl.carnet
                    WHERE em.OGERENCIA = @Gerencia
                    ORDER BY pl.BalanceAsOfBalanceCalculationDate DESC";

                    var empleados = connection.Query<EmpleadoDTO>(query, new { Gerencia = gerencia }).ToList();

                    return Json(new { data = empleados }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<EmpleadoDTO>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult RegisterDetailPartial(long personId)
        {
            Session[keyPersonLicense] = personId;


            List<Entities.Licenses> lstLicenses = new List<Entities.Licenses>();
            try
            {
                var result = Data.License.GetAllLicensesByPersonId(personId);

                if (result != null)
                {
                    lstLicenses = result.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("RegisterDetailPartial", lstLicenses);
        }


        #endregion
        #region Autorizaciones Rh
        [Authorize]
        public ActionResult RefreshAuthorizeRh()
        {

            Session.Remove("sLicensesAuthorizeRh");
            return AuthorizeRhPartial();
        }

        [Authorize]
        public ActionResult AuthorizeRh()
        {
            List<Entities.Licenses> lstLicense = new List<Entities.Licenses>();
            try
            {

                lstLicense = Data.License.GetAllLicensesAuthorizeRh();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstLicense);
        }

        public ActionResult AuthorizeRhPartial()
        {
            List<Entities.Licenses> lstLicense = new List<Entities.Licenses>();
            try
            {

                lstLicense = Data.License.GetAllLicensesAuthorizeRh();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
             return PartialView("AuthorizeRhPartial", lstLicense);
            
        }



        /// <summary>
        /// Accion que llama a metodo para autorizacion de jefe inmediato de la licencia de un colaborador
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult AuthorizeRh(string ids)
        {
            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.License.AuthorizeRh(int.Parse(keyAuthorize));
                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }
            }
            return AuthorizeRhPartial();
        }

        /// <summary>
        /// Accion que llama a metodo para denegar licencia de un colaboador por el jefe inmediato.
        /// </summary>
        /// <param name="selectedIdsDN"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DeniedRh(string ids)//selectedIdsDN
        {
            string keysET = ids + ",";
            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Data.License.DeniedRh(int.Parse(keyAuthorize));

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }
            }
            return AuthorizeRhPartial();
        }
        #endregion
        #region Consulta detalle de licencias
        /*****************************************************************************************/
        /*Parameters of date*/
        /*****************************************************************************************/
        [Authorize]
        public ViewResult ConsultDetailParameters()
        {
            Session.Remove("sLicensesConsultParameter");
            Session.Remove("sLicenseHistoric");
            Session.Remove("sRealEmployeesLicense");
            ViewData["startPeriod"] = "01/01/2017";
            return View();
        }

        [HttpPost]
        public ActionResult LicensesConsult(Entities.MyEntities.LicenseParameters eParameter)
        {
            Session["sLicensesConsultParameter"] = eParameter;

            if (ModelState.IsValid)
            {
                try
                {
                    return RedirectToAction("ConsultDetail");
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Error en la operacion";
            }

            return View("ConsultDetailParameters");
        }

        public ActionResult ConsultDetail()
        {
            return View();
        }

        public ActionResult ConsultDetailPartial()
        {
            List<Entities.ViewModels.LicenseDetailConsultView> lstLicense = new List<Entities.ViewModels.LicenseDetailConsultView>();
            try
            {

                lstLicense = Data.License.GetAllLicensesHistoric();
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("ConsultDetailPartial", lstLicense);
           
        }

     

        #endregion
        #region Acumulado de vacaciones
        public ViewResult EmployeesBalanceParameters()
        {
            Session.Remove("sEmployeesBalanceParameter");
            Session.Remove("sRealEmployeesLicense");
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EmployeesBalanceReport(Entities.MyEntities.LicenseParameters eParameter)
        {
            Session["sEmployeesBalanceParameter"] = eParameter;

            if (ModelState.IsValid)
            {
                try
                {
                    if ((eParameter.PersonId == 0) && string.IsNullOrEmpty(eParameter.CompanyId))
                    {
                        ViewData["EditError"] = "Debe de seleccionar una empresa cuando seleccione el valor TODOS LOS COLABORADORES";
                        return View("EmployeesBalanceParameters");
                    }

                    return View("EmployeesBalanceReport");
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Error en la operacion";
            }

            return View("EmployeesBalanceParameters");
        }

        public ActionResult EmployeesBalanceReportExport()
        {
            Entities.MyEntities.LicenseParameters licenseParameter = new Entities.MyEntities.LicenseParameters();
            licenseParameter = (Entities.MyEntities.LicenseParameters)Session["sEmployeesBalanceParameter"];

            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }


            EmployeesBalance detailReport = new EmployeesBalance();

            try
            {
                if (eEmployee != null)
                {
                    if (licenseParameter.PersonId == 0)
                    {
                        detailReport.DataSource = Data.License.GetAllEmployeesBalance().ToList();
                    }
                    else
                    {
                        // falta enviar en el web servide el person_id
                        detailReport.DataSource = Data.License
                            .GetAllEmployeesBalance2()
                            .Where(x => x.Idhrms == licenseParameter.PersonId)
                            .ToList();
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return ReportViewerExtension.ExportTo(detailReport);
        }
        public ActionResult EmployeesBalanceReportPartial2()
        {
            Entities.MyEntities.LicenseParameters licenseParameter = new Entities.MyEntities.LicenseParameters();
            licenseParameter = (Entities.MyEntities.LicenseParameters)Session["sEmployeesBalanceParameter"];
            List<Entities.Employees> model = new List<Entities.Employees>();
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                if (eEmployee != null)
                {
                    if (licenseParameter.PersonId == 0)
                    {
                        model = Data.License
                            .GetAllEmployeesBalance()
                            .Where(x => x.CompanyId == licenseParameter.CompanyId)
                            .ToList();
                    }
                    else
                    {
                        // falta enviar en el web servide el person_id
                        model = Data.License
                            .GetAllEmployeesBalance2()
                            .ToList();
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("EmployeesBalanceReportPartial", model);
        }

        public ActionResult EmployeesBalanceReportPartial()
        {
            Entities.MyEntities.LicenseParameters licenseParameter = new Entities.MyEntities.LicenseParameters();
            licenseParameter = (Entities.MyEntities.LicenseParameters)Session["sEmployeesBalanceParameter"];
            List<Entities.Employees> model = new List<Entities.Employees>();
            Entities.Employees eEmployee = null;
           

            return PartialView("EmployeesBalanceReportPartial", model);
        }
        #endregion
        #region Estado de Cuenta
        public ViewResult StatementAccountParameters()
        {
            Session.Remove("sLicensesConsultParameter");
            Session.Remove("sLicenseType");
            Session.Remove("sRealEmployeesLicense");
            ViewData["startPeriod"] = "01/01/2017";
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult StatementAccountReport(Entities.MyEntities.LicenseParameters eParameter)
        {
            Session["sLicensesConsultParameter"] = eParameter;


            if (ModelState.IsValid)
            {
                try
                {
                    return View("StatementAccountReport");
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Error en la operacion";
            }

            return View("StatementAccountParameters");
        }

        public ActionResult StatementAccountReportPartial()
        {
            Entities.MyEntities.LicenseParameters eParameter = new Entities.MyEntities.LicenseParameters();
            eParameter = (Entities.MyEntities.LicenseParameters)Session["sLicensesConsultParameter"];


            List<Entities.ViewModels.LicenseDetailConsultView> model = new List<Entities.ViewModels.LicenseDetailConsultView>();

            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

      
            try
            {
                if (eEmployee != null)
                {
                    var result = Data.License.GetStatementAccount();

                    if (result != null)
                    {
                        foreach (var item in result)
                        {
                            Entities.ViewModels.LicenseDetailConsultView license = new Entities.ViewModels.LicenseDetailConsultView();

                            license.AreaName = item.AreaName;
                            license.ManagementName = item.ManagementName;
                            license.CompanyName = item.CompanyName;
                            license.PersonId = item.PersonId;
                            license.EmployeeNumber = item.EmployeeNumber;
                            license.FullName = item.FullName;
                            license.Type = item.Type;
                            license.StartDate = item.StartDate;
                            license.EndDate = item.EndDate;
                            license.Balance = item.Balance;

                            license.DaysQuantity = item.DaysQuantity;

                            license.StartDateReport = eParameter.StartDate;
                            license.EndDateReport = eParameter.EndDate;

                            model.Add(license);
                        }
                    }
                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("StatementAccountReportPartial", model);
        }


        #endregion
        
        #region Tableros
        public ActionResult DashboardTopTenPartial()
        {
            List<Entities.ViewModels.LicenseDetailConsultView> lstLicense = new List<Entities.ViewModels.LicenseDetailConsultView>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            try
            {
                if ((eEmployee.userlevel == 5) || (eEmployee.userlevel == 6))
                {
                    DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    DateTime endDate = startDate.AddMonths(1).AddDays(-1);

                    lstLicense = Data.License.GetLicensesTopTen(eEmployee.Idhrms, startDate.ToShortDateString(), endDate.ToShortDateString());
                }
                else
                {
                    lstLicense = new List<Entities.ViewModels.LicenseDetailConsultView>();
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView(lstLicense);
        }


        //Metodo para el BalanceDashboard
        public ActionResult BalanceDashboardPartial()
        {
            List<Entities.ViewModels.BalanceDashboardView> lstBalance = new List<Entities.ViewModels.BalanceDashboardView>();
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            try
            {
                //if (eEmployee.UserLevel >= 5)
                //{
                var result = Data.License.GetBalanceDashboardByBoss(eEmployee.Idhrms);
                if (result != null)
                {
                    lstBalance = result.ToList();
                }
                else
                {
                    lstBalance = new List<Entities.ViewModels.BalanceDashboardView>();
                }
            }
            catch (Exception)
            {

                return PartialView(new List<Entities.ViewModels.BalanceDashboardView>());
            }

            return PartialView(lstBalance);

        }

        #endregion
    }
    public sealed class AusenciaPopupDto
    {
        public string NombreCompleto { get; set; }
        public string Carnet { get; set; }
        public string AbsenceType { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string DuracionFormateada { get; set; }
        public string EstadoDeDisponibilidad { get; set; }
        public string FechaEnvio { get; set; }
        public string ErrorContenido { get; set; }
        public DateTime? FechaError { get; set; }
        public long PersonAbsenceEntryId { get; set; }
    }
    public class GerenteDTO
    {
        public string Gerencia { get; set; }
        public string Nombre { get; set; }
        public string Cargo { get; set; }
        public string Carnet { get; set; }
        public string Area { get; set; }
        public decimal AcumuladoGerente { get; set; }
        public decimal AcumuladoGerencia { get; set; }
    }

}