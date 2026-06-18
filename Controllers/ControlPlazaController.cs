using Dapper;
using slnRhonline.Data;
using slnRhonline.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class ControlPlazaController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = ControPlaza; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos
        private readonly string connectionString2 = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIGHO1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos
        //ktSrW2n_4pR7
        // GET: ControlPlaza
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Control()
        {
            return View();
        }


        // GET: Cliente - Vista de Casos del Usuario
        public ActionResult List()
        {
            Entities.Employees eEmployee = null;

            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM ControlPlazas ";
                var empleados = db.Query<ControlPlaza>(query).ToList();
                Session["listempleadocaso"] = empleados;

                //return Json(empleados, JsonRequestBehavior.AllowGet);
            }
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var casos = db.Query<Caso>("SELECT * FROM Caso WHERE( UsuarioID = @UsuarioID  OR Correo=@Correo)", new { UsuarioID = eEmployee.EmployeeNumber, Correo = eEmployee.correo }).ToList();
                return View(casos);
            }
        }

        public ActionResult ObtenerPlazas()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT 
                        cp.plaza_id AS PlazaId,
                        cp.cod_activ AS CodActiv,
                        cp.nombre_actividad AS NombreActividad,
                        cp.gerencia AS Gerencia,
                        cp.subgerencia AS Subgerencia,
                        cp.coordinacion_superv AS CoordinacionSuperv,
                        cp.area_operativa AS AreaOperativa,
                        cp.organizacion AS Organizacion,
                        cp.puesto AS Puesto,
                        cp.fecha_creacion AS FechaCreacion,
                        cpe.Estado AS EstadoActual
                    FROM 
                        rrhh_control_plaza cp
                    INNER JOIN 
                        rrhh_control_plaza_estado cpe ON cp.plaza_id = cpe.plaza_id AND cpe.Activo = 'Y'
                ";

                var plazas = db.Query<RrhhControlPlazaViewModel>(query).ToList();

                return Json(new { data = plazas }, JsonRequestBehavior.AllowGet);
            }
        }

        // Obtener datos para KPIs
//        public ActionResult ObtenerDatosGrafico()
//        {
//            using (IDbConnection db = new SqlConnection(connectionString2))
//            {
//                string query = @"
//            SELECT
//                Categoria,
//                COUNT(*) AS Total
//            FROM (
                 
//                SELECT
//                    CASE
//                        WHEN estado IN ('ASCENSO', 'PROMOCION', 'PLAZA NUEVA', 'PASANTE', 'PUESTO NUEVO') THEN 'Ingresos'
//                        WHEN estado IN ('RENUNCIA', 'DESPIDO', 'FALLECIDO', 'JUBILACION', 'ABANDONO DE TRABAJO') THEN 'Egresos'
//                        WHEN estado IN ('TRASLADO', 'ASCENSO HORIZONTAL') THEN 'Movimientos Internos'
//                        ELSE 'Otros'
//                    END AS Categoria
//                FROM [dbo].[rrhh_control_plaza_estado]
            
//            ) AS sub
//            GROUP BY Categoria;
//        ";

//                string query2 = @"
//           WITH PositionData AS (
//    SELECT 
//        po.*, 
//        em.nombre_Completo,
//        em.carnet,
//        em.idhcm2 AS hcm,
//        em.cargo,
//        em.empresa,
//        vo.OGERENCIA AS GERENCIA, -- Obtener GERENCIA desde vw_organizacion
//        em.oSUBGERENCIA AS SUBGERENCIA,
//        em.primernivel AS area,
//        em.managerlevel,
//        te.termination_date,
//        te.Action_Code,
//        te.Termination_type,
//        em.fechaasignacion,
//        em.fechaingreso,
//        CASE 
//            WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE) -- Sustituye '0001-01-01' por '4712-12-31'
//            ELSE em.fechabaja
//        END AS fechabaja,
//        -- Asigna un número de fila basado en fechaasignacion y fechabaja ajustada
//        ROW_NUMBER() OVER (
//            PARTITION BY po.PositionId
//            ORDER BY 
//                CASE WHEN em.fechaasignacion IS NULL THEN 1 ELSE 0 END DESC, -- Prioriza registros con fechaasignacion NULL
//                CASE 
//                    WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE) -- Sustituye '0001-01-01' por '4712-12-31'
//                    ELSE em.fechabaja
//                END DESC, -- Ordena por fechabaja ajustada en orden descendente
//                em.fechaasignacion ASC -- Luego ordena por fechaasignacion en orden ascendente
//        ) AS rn
//    FROM  
//        Positions AS po 
//        INNER JOIN [dbo].[vw_organizacion] vo
//            ON po.DepartmentId = vo.idorg -- INNER JOIN para excluir posiciones sin GERENCIA
//        LEFT JOIN [dbo].[emp_control] AS em 
//            ON po.PositionId = em.PositionId  
//            AND em.cargo NOT IN ('PENSIONADO','JUBILADO','PASANTE')
//        LEFT JOIN [dbo].[TERMINATIONS] AS te 
//            ON em.carnet = te.EMPLEADO_ID 
//    WHERE 
//        po.BusinessUnitId IN (300000002793695, 300000002793636)
//        AND po.ActiveStatus = 'A'
//),
    
//ActivePositionData AS (
//    SELECT 
//        pd.*, 
//        CASE 
//            WHEN pd.rn = 1 THEN 'Y'
//            ELSE 'N'
//        END AS Activo
//    FROM 
//        PositionData pd 
//    WHERE 
//        pd.rn = 1
//),
    
//PositionStatus AS (
//    SELECT 
//        apd.GERENCIA,
        
//        -- Empleados Activos
//        SUM(CASE 
//                WHEN apd.Activo = 'Y' 
//                     AND apd.nombre_Completo IS NOT NULL 
//                     AND apd.fechabaja = '4712-12-31' 
//                     AND apd.cargo NOT IN ('PENSIONADO', 'JUBILADO')
//                THEN 1 ELSE 0 
//            END) AS EmpleadosActivos,
        
//        -- Vacantes por Renuncia
//        SUM(CASE 
//                WHEN apd.Activo = 'Y' 
//                     AND (apd.termination_date IS NOT NULL 
//                          OR apd.fechabaja <> '4712-12-31')
//                THEN 1 ELSE 0 
//            END) AS VacantesPorRenuncia,
        
//        -- Vacantes Nuevas
//        SUM(CASE 
//                WHEN apd.Activo = 'Y' 
//                     AND apd.nombre_Completo IS NULL 
//                THEN 1 ELSE 0 
//            END) AS VacantesNuevas,

//        -- Próxima Baja
//        SUM(CASE 
//                WHEN apd.Activo = 'Y' 
//                     AND apd.fechabaja > GETDATE() and apd.fechabaja <> '4712-12-31'
//                THEN 1 ELSE 0 
//            END) AS ProximaBaja,

//        -- Nuevo Ingreso
//        SUM(CASE 
//                WHEN apd.Activo = 'Y' 
//                     AND DATEDIFF(day, apd.fechaasignacion, GETDATE()) BETWEEN 1 AND 15 
//                THEN 1 ELSE 0 
//            END) AS NuevoIngreso
    
//    FROM 
//        ActivePositionData apd
//    GROUP BY 
//        apd.GERENCIA
//)
    
//-- Consulta Final: Calcula los agregados basados en PositionStatus por GERENCIA
//SELECT 
//    ps.GERENCIA,
    
//    -- Empleados Activos
//    ps.EmpleadosActivos,
    
//    -- Vacantes por Renuncia (solo si no hay empleados activos)
//    ps.VacantesPorRenuncia,
    
//    -- Vacantes Nuevas (solo si no hay empleados activos ni vacantes por renuncia)
//    ps.VacantesNuevas,
    
//    -- Próxima Baja (fecha sistema < fechabaja)
//    ps.ProximaBaja,
    
//    -- Nuevo Ingreso (fechaasignacion dentro de 1 a 15 días de fecha sistema)
//    ps.NuevoIngreso,
    
//    -- Vacante Total: Vacantes por Renuncia + Vacantes Nuevas (excluye Proxima_Baja)
//    (ps.VacantesPorRenuncia + ps.VacantesNuevas) AS Vacante_Total,
    
//    -- Total puestos por gerencia: EmpleadosActivos + Vacante_Total
//    (ps.EmpleadosActivos + ps.VacantesPorRenuncia + ps.VacantesNuevas) AS Total_Puestos,
    
//    -- Porcentaje de vacante ocupada: (EmpleadosActivos / Total_Puestos) * 100
//    CASE 
//        WHEN (ps.EmpleadosActivos + ps.VacantesPorRenuncia + ps.VacantesNuevas) = 0 
//        THEN 0
//        ELSE 
//            (ps.EmpleadosActivos * 100.0) / 
//            (ps.EmpleadosActivos + ps.VacantesPorRenuncia + ps.VacantesNuevas)
//    END AS [Porcentaje_Vacante_Ocupada]
//FROM 
//    PositionStatus ps

//UNION ALL

//-- Fila de Totalización
//SELECT 
//    'Total' AS GERENCIA,
//    SUM(ps.EmpleadosActivos) AS EmpleadosActivos,
//    SUM(ps.VacantesPorRenuncia) AS VacantesPorRenuncia,
//    SUM(ps.VacantesNuevas) AS VacantesNuevas,
//    SUM(ps.ProximaBaja) AS ProximaBaja,
//    SUM(ps.NuevoIngreso) AS NuevoIngreso,
//    (SUM(ps.VacantesPorRenuncia) + SUM(ps.VacantesNuevas)) AS Vacante_Total,
//    (SUM(ps.EmpleadosActivos) + SUM(ps.VacantesPorRenuncia) + SUM(ps.VacantesNuevas)) AS Total_Puestos,
//    CASE 
//        WHEN (SUM(ps.EmpleadosActivos) + SUM(ps.VacantesPorRenuncia) + SUM(ps.VacantesNuevas)) = 0 
//        THEN 0
//        ELSE 
//            (SUM(ps.EmpleadosActivos) * 100.0) / 
//            (SUM(ps.EmpleadosActivos) + SUM(ps.VacantesPorRenuncia) + SUM(ps.VacantesNuevas))
//    END AS [Porcentaje_Vacante_Ocupada]
//FROM 
//    PositionStatus ps
//        ";
//                List<PositionStatus> templista = new List<PositionStatus>();
//                templista = db.Query<PositionStatus>(query2).ToList();

//                //int ingresos = resultados.FirstOrDefault(r => r.Categoria == "Ingresos")?.Total ?? 0;
//                //int egresos = resultados.FirstOrDefault(r => r.Categoria == "Egresos")?.Total ?? 0;
//                //int movimientosInternos = resultados.FirstOrDefault(r => r.Categoria == "Movimientos Internos")?.Total ?? 0;
//                //int otros = resultados.FirstOrDefault(r => r.Categoria == "Otros")?.Total ?? 0;

//                //return Json(new
//                //{
//                //    ingresos,
//                //    egresos,
//                //    movimientosInternos,
//                //    otros
//                //}, JsonRequestBehavior.AllowGet);
//                var viewModel = new GraficoPlazasViewModel
//                {
//                    Categorias = templista.Select(r => r.GERENCIA).ToList(),
//                    EmpleadosActivos = templista.Select(r => r.EmpleadosActivos).ToList(),
//                    VacantesPorRenuncia = templista.Select(r => r.Vacante_Total).ToList(),
//                    NuevoIngreso = templista.Select(r => r.NuevoIngreso).ToList(),
//                    ProximaBaja = templista.Select(r => r.ProximaBaja).ToList(),
//                    Porcentaje = templista.Select(r => r.Porcentaje_Vacante_Ocupada).ToList() ,
//                    Total_Puestos = templista.Select(r => r.Total_Puestos).ToList()


//                };

//                return Json(viewModel, JsonRequestBehavior.AllowGet);
 
//            }
//        }
      
         public ActionResult ObtenerDatosGrafico()
        {
            using (IDbConnection db = new SqlConnection(connectionString2))
            {
                string query = @"
            SELECT
                Categoria,
                COUNT(*) AS Total
            FROM (
                 
                SELECT
                    CASE
                        WHEN estado IN ('ASCENSO', 'PROMOCION', 'PLAZA NUEVA', 'PASANTE', 'PUESTO NUEVO') THEN 'Ingresos'
                        WHEN estado IN ('RENUNCIA', 'DESPIDO', 'FALLECIDO', 'JUBILACION', 'ABANDONO DE TRABAJO') THEN 'Egresos'
                        WHEN estado IN ('TRASLADO', 'ASCENSO HORIZONTAL') THEN 'Movimientos Internos'
                        ELSE 'Otros'
                    END AS Categoria
                FROM [dbo].[rrhh_control_plaza_estado]
            
            ) AS sub
            GROUP BY Categoria;
        ";

                string query2 = @"
           WITH PositionData AS (
    SELECT 
        po.*, 
        em.nombre_Completo,
        em.carnet,
        em.idhcm2 AS hcm,
        em.cargo,
        em.empresa,
        vo.OGERENCIA AS GERENCIA, -- Obtener GERENCIA desde vw_organizacion
        em.oSUBGERENCIA AS SUBGERENCIA,
        em.primernivel AS area,
        em.managerlevel,
        te.termination_date,
        te.Action_Code,
        te.Termination_type,
        em.fechaasignacion,
        em.fechaingreso,
        CASE 
            WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE) -- Sustituye '0001-01-01' por '4712-12-31'
            ELSE em.fechabaja
        END AS fechabaja,
        -- Asigna un número de fila basado en fechaasignacion y fechabaja ajustada
        ROW_NUMBER() OVER (
            PARTITION BY po.PositionId
            ORDER BY 
                CASE WHEN em.fechaasignacion IS NULL THEN 1 ELSE 0 END DESC, -- Prioriza registros con fechaasignacion NULL
                CASE 
                    WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE) -- Sustituye '0001-01-01' por '4712-12-31'
                    ELSE em.fechabaja
                END DESC, -- Ordena por fechabaja ajustada en orden descendente
                em.fechaasignacion ASC -- Luego ordena por fechaasignacion en orden ascendente
        ) AS rn
    FROM  
        Positions AS po 
        INNER JOIN [dbo].[vw_organizacion] vo
            ON po.DepartmentId = vo.idorg -- INNER JOIN para excluir posiciones sin GERENCIA
        LEFT JOIN [dbo].[emp_control] AS em 
            ON po.PositionId = em.PositionId  
            AND em.cargo NOT IN ('PENSIONADO','JUBILADO','PASANTE')
        LEFT JOIN [dbo].[TERMINATIONS] AS te 
            ON em.carnet = te.EMPLEADO_ID 
    WHERE 
        po.BusinessUnitId IN (300000002793695, 300000002793636)
        AND po.ActiveStatus = 'A'
),
    
ActivePositionData AS (
    SELECT 
        pd.*, 
        CASE 
            WHEN pd.rn = 1 THEN 'Y'
            ELSE 'N'
        END AS Activo
    FROM 
        PositionData pd 
    WHERE 
        pd.rn = 1
),
    
PositionStatus AS (
    SELECT 
        apd.GERENCIA,
        
        -- Empleados Activos
        SUM(CASE 
                WHEN apd.Activo = 'Y' 
                     AND apd.nombre_Completo IS NOT NULL 
                     AND apd.fechabaja = '4712-12-31' 
                     AND apd.cargo NOT IN ('PENSIONADO', 'JUBILADO')
                THEN 1 ELSE 0 
            END) AS EmpleadosActivos,
        
        -- Vacantes por Renuncia
        SUM(CASE 
                WHEN apd.Activo = 'Y' 
                     AND (apd.termination_date IS NOT NULL 
                          OR apd.fechabaja <> '4712-12-31')
                THEN 1 ELSE 0 
            END) AS VacantesPorRenuncia,
        
        -- Vacantes Nuevas
        SUM(CASE 
                WHEN apd.Activo = 'Y' 
                     AND apd.nombre_Completo IS NULL 
                THEN 1 ELSE 0 
            END) AS VacantesNuevas,

        -- Próxima Baja
        SUM(CASE 
                WHEN apd.Activo = 'Y' 
                     AND apd.fechabaja > GETDATE() and apd.fechabaja <> '4712-12-31'
                THEN 1 ELSE 0 
            END) AS ProximaBaja,

        -- Nuevo Ingreso
        SUM(CASE 
                WHEN apd.Activo = 'Y' 
                     AND DATEDIFF(day, apd.fechaasignacion, GETDATE()) BETWEEN 1 AND 15 
                THEN 1 ELSE 0 
            END) AS NuevoIngreso
    
    FROM 
        ActivePositionData apd
    GROUP BY 
        apd.GERENCIA
)
    
-- Consulta Final: Calcula los agregados basados en PositionStatus por GERENCIA
SELECT 
    ps.GERENCIA,
    
    -- Empleados Activos
    ps.EmpleadosActivos,
    
    -- Vacantes por Renuncia (solo si no hay empleados activos)
    ps.VacantesPorRenuncia,
    
    -- Vacantes Nuevas (solo si no hay empleados activos ni vacantes por renuncia)
    ps.VacantesNuevas,
    
    -- Próxima Baja (fecha sistema < fechabaja)
    ps.ProximaBaja,
    
    -- Nuevo Ingreso (fechaasignacion dentro de 1 a 15 días de fecha sistema)
    ps.NuevoIngreso,
    
    -- Vacante Total: Vacantes por Renuncia + Vacantes Nuevas (excluye Proxima_Baja)
    (ps.VacantesPorRenuncia + ps.VacantesNuevas) AS Vacante_Total,
    
    -- Total puestos por gerencia: EmpleadosActivos + Vacante_Total
    (ps.EmpleadosActivos + ps.VacantesPorRenuncia + ps.VacantesNuevas) AS Total_Puestos,
    
    -- Porcentaje de vacante ocupada: (EmpleadosActivos / Total_Puestos) * 100
    CASE 
        WHEN (ps.EmpleadosActivos + ps.VacantesPorRenuncia + ps.VacantesNuevas) = 0 
        THEN 0
        ELSE 
            (ps.EmpleadosActivos * 100.0) / 
            (ps.EmpleadosActivos + ps.VacantesPorRenuncia + ps.VacantesNuevas)
    END AS [Porcentaje_Vacante_Ocupada]
FROM 
    PositionStatus ps

UNION ALL

-- Fila de Totalización
SELECT 
    'Total' AS GERENCIA,
    SUM(ps.EmpleadosActivos) AS EmpleadosActivos,
    SUM(ps.VacantesPorRenuncia) AS VacantesPorRenuncia,
    SUM(ps.VacantesNuevas) AS VacantesNuevas,
    SUM(ps.ProximaBaja) AS ProximaBaja,
    SUM(ps.NuevoIngreso) AS NuevoIngreso,
    (SUM(ps.VacantesPorRenuncia) + SUM(ps.VacantesNuevas)) AS Vacante_Total,
    (SUM(ps.EmpleadosActivos) + SUM(ps.VacantesPorRenuncia) + SUM(ps.VacantesNuevas)) AS Total_Puestos,
    CASE 
        WHEN (SUM(ps.EmpleadosActivos) + SUM(ps.VacantesPorRenuncia) + SUM(ps.VacantesNuevas)) = 0 
        THEN 0
        ELSE 
            (SUM(ps.EmpleadosActivos) * 100.0) / 
            (SUM(ps.EmpleadosActivos) + SUM(ps.VacantesPorRenuncia) + SUM(ps.VacantesNuevas))
    END AS [Porcentaje_Vacante_Ocupada]
FROM 
    PositionStatus ps
        ";
                List<PositionStatus> templista = new List<PositionStatus>();
                templista = db.Query<PositionStatus>(query2).ToList();

                //int ingresos = resultados.FirstOrDefault(r => r.Categoria == "Ingresos")?.Total ?? 0;
                //int egresos = resultados.FirstOrDefault(r => r.Categoria == "Egresos")?.Total ?? 0;
                //int movimientosInternos = resultados.FirstOrDefault(r => r.Categoria == "Movimientos Internos")?.Total ?? 0;
                //int otros = resultados.FirstOrDefault(r => r.Categoria == "Otros")?.Total ?? 0;

                //return Json(new
                //{
                //    ingresos,
                //    egresos,
                //    movimientosInternos,
                //    otros
                //}, JsonRequestBehavior.AllowGet);
                var viewModel = new GraficoPlazasViewModel
                {
                    Categorias = templista.Select(r => r.GERENCIA).ToList(),
                    EmpleadosActivos = templista.Select(r => r.EmpleadosActivos).ToList(),
                    VacantesPorRenuncia = templista.Select(r => r.Vacante_Total).ToList(),
                    NuevoIngreso = templista.Select(r => r.NuevoIngreso).ToList(),
                    ProximaBaja = templista.Select(r => r.ProximaBaja).ToList(),
                    Porcentaje = templista.Select(r => r.Porcentaje_Vacante_Ocupada).ToList() ,
                    Total_Puestos = templista.Select(r => r.Total_Puestos).ToList()


                };

                return Json(viewModel, JsonRequestBehavior.AllowGet);
 
            }
        }
        public ActionResult Obtenerplazas2()
        {
            using (IDbConnection db = new SqlConnection(connectionString2))
            {

                string query = @"
           WITH PositionData AS (
    SELECT 
        po.PositionCode,po.Name nombre_posicion, 
        em.nombre_Completo,
        em.carnet,
        em.idhcm2 AS hcm,
        em.cargo,
        em.empresa,
        vo.OGERENCIA AS GERENCIA, -- Obtener GERENCIA desde vw_organizacion
        em.oSUBGERENCIA AS SUBGERENCIA,
        em.primernivel AS area,
        em.managerlevel,
        te.termination_date,
        te.Action_Code,
        te.Termination_type,
        em.fechaasignacion,
        em.fechaingreso,em.correo,
        CASE 
            WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE) -- Sustituye '0001-01-01' por '4712-12-31'
            ELSE em.fechabaja
        END AS fechabaja,
        -- Asigna un número de fila basado en fechaasignacion y fechabaja ajustada
        ROW_NUMBER() OVER (
            PARTITION BY po.PositionId
            ORDER BY 
                CASE WHEN em.fechaasignacion IS NULL THEN 1 ELSE 0 END DESC, -- Prioriza registros con fechaasignacion NULL
                CASE 
                    WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE) -- Sustituye '0001-01-01' por '4712-12-31'
                    ELSE em.fechabaja
                END DESC, -- Ordena por fechabaja ajustada en orden descendente
                em.fechaasignacion ASC -- Luego ordena por fechaasignacion en orden ascendente
        ) AS rn
    FROM  
        Positions AS po 
        INNER JOIN [dbo].[vw_organizacion] vo
            ON po.DepartmentId = vo.idorg -- INNER JOIN para excluir posiciones sin GERENCIA
        LEFT JOIN [dbo].[emp_control] AS em 
            ON po.PositionId = em.PositionId  
            AND em.cargo NOT IN ('PENSIONADO','JUBILADO','PASANTE')
        LEFT JOIN [dbo].[TERMINATIONS] AS te 
            ON em.carnet = te.EMPLEADO_ID 
    WHERE 
        po.BusinessUnitId IN (300000002793695, 300000002793636)
        AND po.ActiveStatus = 'A'
),
    
ActivePositionData AS (
    SELECT 
        pd.*, 
        CASE 
            WHEN pd.rn = 1 THEN 'Y'
            ELSE 'N'
        END AS Activo,
		CASE 
    WHEN pd.rn = 1 AND pd.nombre_Completo IS NOT NULL AND pd.fechabaja = '4712-12-31' AND pd.cargo NOT IN ('PENSIONADO', 'JUBILADO') THEN 'Empleado Activo'
    WHEN pd.rn = 1 AND (pd.termination_date IS NOT NULL OR pd.fechabaja <> '4712-12-31') THEN 'Vacante por Renuncia'
    WHEN pd.rn = 1 AND pd.nombre_Completo IS NULL THEN 'Vacante Nueva'
    WHEN pd.rn = 1 AND pd.fechabaja > GETDATE() AND pd.fechabaja <> '4712-12-31' THEN 'Próxima Baja'
    WHEN  pd.rn = 1 AND DATEDIFF(day, pd.fechaasignacion, GETDATE()) BETWEEN 1 AND 15 THEN 'Nuevo Ingreso'
    ELSE 'Otro' -- Para cualquier otro caso no cubierto
END AS Categoria
    FROM 
        PositionData pd 
    WHERE 
        pd.rn = 1
)select * from ActivePositionData where Categoria IN ('Empleado Activo','Nuevo Ingreso')
        ";
                List<plazas> templista = new List<plazas>();
                templista = db.Query<plazas>(query).ToList();

             


                return Json(new { data = templista }, JsonRequestBehavior.AllowGet);

            }
        }
        public ActionResult Obtenerplazas3()
        {
            using (IDbConnection db = new SqlConnection(connectionString2))
            {

                string query = @"
       WITH PositionData AS (
    SELECT 
        po.PositionCode,
        po.Name AS nombre_posicion,
        em.nombre_Completo,
        em.carnet,
        em.idhcm2 AS hcm,
        em.cargo,
        em.empresa,
        vo.OGERENCIA AS GERENCIA,
        em.oSUBGERENCIA AS SUBGERENCIA,
        em.primernivel AS area,
        em.managerlevel,
        te.termination_date,
        te.Action_Code,
        te.Termination_type,
        em.fechaasignacion,
        em.fechaingreso,
        em.correo,
        CASE 
            WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE)
            ELSE em.fechabaja
        END AS fechabaja,
        ROW_NUMBER() OVER (
            PARTITION BY po.PositionId
            ORDER BY 
                CASE WHEN em.fechaasignacion IS NULL THEN 1 ELSE 0 END DESC,
                CASE 
                    WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE)
                    ELSE em.fechabaja
                END DESC,
                em.fechaasignacion ASC
        ) AS rn,
        COUNT(*) OVER (PARTITION BY po.PositionId) AS registros_historial -- Contar registros por PositionId
    FROM  
        Positions AS po 
        INNER JOIN [dbo].[vw_organizacion] vo
            ON po.DepartmentId = vo.idorg
        LEFT JOIN [dbo].[emp_control] AS em 
            ON po.PositionId = em.PositionId  
            AND em.cargo NOT IN ('PENSIONADO','JUBILADO','PASANTE')
        LEFT JOIN [dbo].[TERMINATIONS] AS te 
            ON em.carnet = te.EMPLEADO_ID 
    WHERE 
        po.BusinessUnitId IN (300000002793695, 300000002793636)
        AND po.ActiveStatus = 'A'
),
    
ActivePositionData AS (
    SELECT 
        pd.*, 
        CASE 
            WHEN pd.rn = 1 THEN 'Y'
            ELSE 'N'
        END AS Activo,
        CASE 
            WHEN pd.rn = 1 AND pd.nombre_Completo IS NOT NULL AND pd.fechabaja = '4712-12-31' AND pd.cargo NOT IN ('PENSIONADO', 'JUBILADO') THEN 'Empleado Activo'
            WHEN pd.rn = 1 AND (pd.termination_date IS NOT NULL OR pd.fechabaja <> '4712-12-31') THEN 'Vacante por Renuncia'
            WHEN pd.rn = 1 AND pd.nombre_Completo IS NULL THEN 'Vacante Nueva'
            WHEN pd.rn = 1 AND pd.fechabaja > GETDATE() AND pd.fechabaja <> '4712-12-31' THEN 'Próxima Baja'
            WHEN pd.rn = 1 AND DATEDIFF(day, pd.fechaasignacion, GETDATE()) BETWEEN 1 AND 15 THEN 'Nuevo Ingreso'
            ELSE 'Otro'
        END AS Categoria,
        CASE 
            WHEN pd.registros_historial > 1 THEN 'Y' -- Indica si tiene más de un registro
            ELSE 'N'
        END AS TieneHistorial -- Nuevo campo
    FROM 
        PositionData pd 
   WHERE 
        pd.rn = 1
)
SELECT * 
FROM ActivePositionData where Categoria IN ('Empleado Activo','Nuevo Ingreso')

        ";
                List<plazas> templista = new List<plazas>();
                templista = db.Query<plazas>(query).ToList();




                return Json(new { data = templista }, JsonRequestBehavior.AllowGet);

            }
        }
        public ActionResult Obtenerplazascodigo( string codigo)
        {
            using (IDbConnection db = new SqlConnection(connectionString2))
            {

                string query = @"
       WITH PositionData AS (
    SELECT 
        po.PositionCode,
        po.Name AS nombre_posicion,
        em.nombre_Completo,
        em.carnet,
        em.idhcm2 AS hcm,
        em.cargo,
        em.empresa,
        vo.OGERENCIA AS GERENCIA,
        em.oSUBGERENCIA AS SUBGERENCIA,
        em.primernivel AS area,
        em.managerlevel,
        te.termination_date,
        te.Action_Code,
        te.Termination_type,
        em.fechaasignacion,
        em.fechaingreso,
        em.correo,
        CASE 
            WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE)
            ELSE em.fechabaja
        END AS fechabaja,
        ROW_NUMBER() OVER (
            PARTITION BY po.PositionId
            ORDER BY 
                CASE WHEN em.fechaasignacion IS NULL THEN 1 ELSE 0 END DESC,
                CASE 
                    WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE)
                    ELSE em.fechabaja
                END DESC,
                em.fechaasignacion ASC
        ) AS rn,
        COUNT(*) OVER (PARTITION BY po.PositionId) AS registros_historial -- Contar registros por PositionId
    FROM  
        Positions AS po 
        INNER JOIN [dbo].[vw_organizacion] vo
            ON po.DepartmentId = vo.idorg
        LEFT JOIN [dbo].[emp_control] AS em 
            ON po.PositionId = em.PositionId  
            AND em.cargo NOT IN ('PENSIONADO','JUBILADO','PASANTE')
        LEFT JOIN [dbo].[TERMINATIONS] AS te 
            ON em.carnet = te.EMPLEADO_ID 
    WHERE 
        po.BusinessUnitId IN (300000002793695, 300000002793636)
        AND po.ActiveStatus = 'A'
),
    
ActivePositionData AS (
    SELECT 
        pd.*, 
        CASE 
            WHEN pd.rn = 1 THEN 'Y'
            ELSE 'N'
        END AS Activo,
        CASE 
            WHEN pd.rn = 1 AND pd.nombre_Completo IS NOT NULL AND pd.fechabaja = '4712-12-31' AND pd.cargo NOT IN ('PENSIONADO', 'JUBILADO') THEN 'Empleado Activo'
            WHEN pd.rn = 1 AND (pd.termination_date IS NOT NULL OR pd.fechabaja <> '4712-12-31') THEN 'Vacante por Renuncia'
            WHEN pd.rn = 1 AND pd.nombre_Completo IS NULL THEN 'Vacante Nueva'
            WHEN pd.rn = 1 AND pd.fechabaja > GETDATE() AND pd.fechabaja <> '4712-12-31' THEN 'Próxima Baja'
            WHEN pd.rn = 1 AND DATEDIFF(day, pd.fechaasignacion, GETDATE()) BETWEEN 1 AND 15 THEN 'Nuevo Ingreso'
            ELSE 'Otro'
        END AS Categoria,
        CASE 
            WHEN pd.registros_historial > 1 THEN 'Y' -- Indica si tiene más de un registro
            ELSE 'N'
        END AS TieneHistorial -- Nuevo campo
    FROM 
        PositionData pd 
   WHERE 
        pd.rn = 1
)
SELECT * 
FROM ActivePositionData where Categoria IN ('Empleado Activo','Nuevo Ingreso')

        ";
                List<plazas> templista = new List<plazas>();
                templista = db.Query<plazas>(query).ToList();




                return Json(new { data = templista }, JsonRequestBehavior.AllowGet);

            }
        }
        public ActionResult ObtenerHistorialPlaza(string codigo)
        {
            using (IDbConnection db = new SqlConnection(connectionString2))
            {
                string query = @"
            WITH PositionData AS (
                SELECT 
                    po.PositionCode,
                    po.Name AS nombre_posicion,
                    em.nombre_Completo,
                    em.carnet,
                    em.idhcm2 AS hcm,
                    em.cargo,
                    em.empresa,
                    vo.OGERENCIA AS GERENCIA,
                    em.oSUBGERENCIA AS SUBGERENCIA,
                    em.primernivel AS area,
                    em.managerlevel,
                    te.termination_date,
                    te.Action_Code,
                    te.Termination_type,
                    em.fechaasignacion,
                    em.fechaingreso,
                    em.correo,
                    CASE 
                        WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE)
                        ELSE em.fechabaja
                    END AS fechabaja,
                    ROW_NUMBER() OVER (
                        PARTITION BY po.PositionId
                        ORDER BY 
                            CASE WHEN em.fechaasignacion IS NULL THEN 1 ELSE 0 END DESC,
                            CASE 
                                WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE)
                                ELSE em.fechabaja
                            END DESC,
                            em.fechaasignacion ASC
                    ) AS rn,
                    COUNT(*) OVER (PARTITION BY po.PositionId) AS registros_historial
                FROM  
                    Positions AS po 
                    INNER JOIN [dbo].[vw_organizacion] vo
                        ON po.DepartmentId = vo.idorg
                    LEFT JOIN [dbo].[emp_control] AS em 
                        ON po.PositionId = em.PositionId  
                        AND em.cargo NOT IN ('PENSIONADO','JUBILADO','PASANTE')
                    LEFT JOIN [dbo].[TERMINATIONS] AS te 
                        ON em.carnet = te.EMPLEADO_ID 
                WHERE 
                    po.BusinessUnitId IN (300000002793695, 300000002793636)
                    AND po.ActiveStatus = 'A'
            ),
            ActivePositionData AS (
                SELECT 
                    pd.*, 
                    CASE 
                        WHEN pd.rn = 1 THEN 'Y'
                        ELSE 'N'
                    END AS Activo,
                    CASE 
                        WHEN pd.rn = 1 AND pd.nombre_Completo IS NOT NULL AND pd.fechabaja = '4712-12-31' AND pd.cargo NOT IN ('PENSIONADO', 'JUBILADO') THEN 'Empleado Activo'
                        WHEN pd.rn = 1 AND (pd.termination_date IS NOT NULL OR pd.fechabaja <> '4712-12-31') THEN 'Vacante por Renuncia'
                        WHEN pd.rn = 1 AND pd.nombre_Completo IS NULL THEN 'Vacante Nueva'
                        WHEN pd.rn = 1 AND pd.fechabaja > GETDATE() AND pd.fechabaja <> '4712-12-31' THEN 'Próxima Baja'
                        WHEN pd.rn = 1 AND DATEDIFF(day, pd.fechaasignacion, GETDATE()) BETWEEN 1 AND 15 THEN 'Nuevo Ingreso'
                        ELSE 'Otro'
                    END AS Categoria,
                    CASE 
                        WHEN pd.registros_historial > 1 THEN 'Y'
                        ELSE 'N'
                    END AS TieneHistorial
                FROM 
                    PositionData pd 
                WHERE 
                    pd.rn = 1
            )
            SELECT * 
            FROM ActivePositionData 
            WHERE ActivePositionData.PositionCode = @codigo;
        ";

                List<plazas> templista = db.Query<plazas>(query, new { codigo }).ToList();

                return Json(new { data = templista }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DashboardController()
        {List< PositionStatus > templista = new List<PositionStatus>();
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"-- [Inserta aquí tu consulta SQL completa]";

                // Asegúrate de reemplazar el comentario con tu consulta SQL.
                db.Query<PositionStatus>(sql);
            }
            // Obtén la cadena de conexión desde Web.config
            return Json(templista, JsonRequestBehavior.AllowGet);
        }

        // Obtener datos para el gráfico
     

        // Acción para ver detalles de una plaza
        public ActionResult Detalles(int id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT 
                        cp.*, 
                        ce.nombre_estado AS EstadoActual
                    FROM 
                        rrhh_control_plaza cp
                    INNER JOIN 
                        rrhh_control_plaza_estado cpe ON cp.plaza_id = cpe.plaza_id AND cpe.Activo = 'Y'
                    INNER JOIN 
                        rrhh_catalogo_estados ce ON cpe.estado_id = ce.id
                    WHERE 
                        cp.plaza_id = @PlazaId
                ";

                var plaza = db.QueryFirstOrDefault<RrhhControlPlaza>(query, new { plaza_id = id });

                if (plaza != null)
                {
                    return Json(plaza, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return HttpNotFound();
                }
            }
        }

        // Acción para cambiar el estado de una plaza
        [HttpPost]
        public ActionResult CambiarEstado(int plazaId, string nuevoEstado)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                // Obtener el estado_id del nuevo estado
                string getEstadoIdQuery = "SELECT id FROM rrhh_catalogo_estados WHERE nombre_estado = @nombre_estado";
                int estadoId = db.ExecuteScalar<int>(getEstadoIdQuery, new { nombre_estado = nuevoEstado });

                if (estadoId == 0)
                {
                    return Json(new { success = false, message = "Estado inválido." });
                }

                // Iniciar transacción
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        Entities.Employees eEmployee = null;

                        if (Session["User"] != null)
                        {
                            eEmployee = (Entities.Employees)Session["User"];
                        }
                        // Desactivar el estado actual
                        string updateEstadoActualQuery = @"
                            UPDATE rrhh_control_plaza_estado
                            SET Activo = 'N'
                            WHERE plaza_id = @plaza_id AND Activo = 'Y'
                        ";
                        db.Execute(updateEstadoActualQuery, new { plaza_id = plazaId }, transaction);

                        // Insertar el nuevo estado
                        string insertNuevoEstadoQuery = @"
                            INSERT INTO rrhh_control_plaza_estado (plaza_id, usuario_id, fecha, estado_id, Activo)
                            VALUES (@plaza_id, @usuario_id, GETDATE(), @estado_id, 'Y')
                        ";
                        db.Execute(insertNuevoEstadoQuery, new
                        {
                            plaza_id = plazaId,
                            usuario_id = eEmployee.EmployeeNumber,
                            estado_id = estadoId
                        }, transaction);

                        transaction.Commit();

                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }

        // Acción para editar una plaza (GET)

        public ActionResult Editar(int id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM rrhh_control_plaza WHERE plaza_id = @PlazaId";
                var plaza = db.QueryFirstOrDefault<RrhhControlPlaza>(query, new { PlazaId = id });

                if (plaza != null)
                {
                    return View(plaza);
                }
                else
                {
                    return HttpNotFound();
                }
            }
        }

        // Acción para editar una plaza (POST)
        [HttpPost]
        public ActionResult Editar(RrhhControlPlaza plaza)
        {
            if (ModelState.IsValid)
            {
                using (IDbConnection db = new SqlConnection(connectionString))
                {
                    string updateQuery = @"
                        UPDATE rrhh_control_plaza
                        SET 
                            cod_activ = @CodActiv,
                            nombre_actividad = @NombreActividad,
                            gerencia = @Gerencia,
                            subgerencia = @Subgerencia,
                            coordinacion_superv = @CoordinacionSuperv,
                            area_operativa = @AreaOperativa,
                            organizacion = @Organizacion,
                            edificio = @Edificio,
                            nomina_desvinculante = @NominaDesvinculante,
                            empresa_contratante = @EmpresaContratante,
                            puesto = @Puesto,
                            fecha_recibo_ps = @FechaReciboPs,
                            mes1 = @Mes1,
                            ano = @Ano,
                            f_recepcion_rq = @FRecepcionRq,
                            no_de_emp = @NoDeEmp,
                            sustituye_a_nombre = @SustituyeANombre,
                            posicion_sigho = @PosicionSigho,
                            solicitante_jefe_inmediato = @SolicitanteJefeInmediato,
                            salario = @Salario,
                            observaciones = @Observaciones,
                            fecha_envio_rs = @FechaEnvioRs,
                            autoriz = @Autoriz,
                            observaciones_compensacion = @ObservacionesCompensacion,
                            correo = @Correo,
                            estatus_sr = @EstatusSr,
                            estatus2_s_r = @Estatus2Sr,
                            persona_contratada = @PersonaContratada,
                            fecha_ingreso = @FechaIngreso,
                            espc_seleccion_reclut = @EspcSeleccionReclut,
                            observaciones_seleccion_reclutamiento = @ObservacionesSeleccionReclutamiento,
                            observaciones_generales = @ObservacionesGenerales,
                            est_compensacion = @EstCompensacion,
                            evaluacion_id = @EvaluacionId
                        WHERE 
                            plaza_id = @PlazaId
                    ";
                    db.Execute(updateQuery, plaza);
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return View(plaza);
            }
        }
        public ActionResult ObtenerDetallePlaza(int id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT 
                        cp.*, 
                        ce.nombre_estado AS EstadoActual
                    FROM 
                        rrhh_control_plaza cp
                    INNER JOIN 
                        rrhh_control_plaza_estado cpe ON cp.plaza_id = cpe.plaza_id AND cpe.Activo = 'Y'
                    INNER JOIN 
                        rrhh_catalogo_estados ce ON cpe.estado_id = ce.id
                    WHERE 
                        cp.plaza_id = @PlazaId
                ";

                var plaza = db.QueryFirstOrDefault<RrhhControlPlazaViewModel>(query, new { PlazaId = id });

                if (plaza != null)
                {
                    return Json(plaza, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return HttpNotFound();
                }
            }
        }

        // ============================================================
        // MODULO DE REQUISICIONES DE PERSONAL
        // ============================================================

        // Vista principal de Requisiciones
        public ActionResult Requisiciones()
        {
            return View();
        }

        // Obtener todas las requisiciones (para RRHH)
        public ActionResult ObtenerRequisiciones(string estado = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT * FROM Rrhh_Requisicion
                    WHERE (@Estado IS NULL OR Estado = @Estado)
                    ORDER BY FechaSolicitud DESC
                ";
                var requisiciones = db.Query<RrhhRequisicion>(query, new { Estado = estado }).ToList();
                return Json(new { data = requisiciones }, JsonRequestBehavior.AllowGet);
            }
        }

        // Obtener requisiciones del usuario logueado
        public ActionResult ObtenerMisRequisiciones()
        {
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT * FROM Rrhh_Requisicion
                    WHERE SolicitanteId = @SolicitanteId
                    ORDER BY FechaSolicitud DESC
                ";
                var requisiciones = db.Query<RrhhRequisicion>(query, new { SolicitanteId = eEmployee?.EmployeeNumber }).ToList();
                return Json(new { data = requisiciones }, JsonRequestBehavior.AllowGet);
            }
        }

        // Crear nueva requisicion
        [HttpPost]
        public ActionResult CrearRequisicion(RrhhRequisicion req)
        {
            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                req.SolicitanteId = eEmployee?.EmployeeNumber;
                req.SolicitanteNombre = eEmployee?.FullName;
                req.Estado = "Pendiente";

                using (IDbConnection db = new SqlConnection(connectionString))
                {
                    string query = @"
                        INSERT INTO Rrhh_Requisicion 
                            (PlazaId, PositionCode, NombrePuesto, Nueva, Ubicacion, Motivo, 
                             Reporta, Justificacion, SolicitanteId, SolicitanteNombre, 
                             FechaSolicitud, Estado, FechaCreacion)
                        VALUES 
                            (@PlazaId, @PositionCode, @NombrePuesto, @Nueva, @Ubicacion, @Motivo, 
                             @Reporta, @Justificacion, @SolicitanteId, @SolicitanteNombre, 
                             GETDATE(), 'Pendiente', GETDATE())
                    ";
                    db.Execute(query, req);
                    return Json(new { success = true, message = "Requisicion creada exitosamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Aprobar o Rechazar requisicion
        [HttpPost]
        public ActionResult ResolverRequisicion(int requisicionId, string estado, string comentario)
        {
            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                using (IDbConnection db = new SqlConnection(connectionString))
                {
                    string query = @"
                        UPDATE Rrhh_Requisicion 
                        SET Estado = @Estado,
                            ComentarioRRHH = @Comentario,
                            FechaResolucion = GETDATE(),
                            ResueltoPor = @ResueltoPor
                        WHERE RequisicionId = @RequisicionId
                    ";
                    db.Execute(query, new
                    {
                        RequisicionId = requisicionId,
                        Estado = estado,
                        Comentario = comentario,
                        ResueltoPor = eEmployee?.EmployeeNumber
                    });
                    return Json(new { success = true, message = "Requisicion " + estado.ToLower() + " exitosamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Obtener KPIs de requisiciones
        public ActionResult ObtenerKPIsRequisiciones()
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT 
                        COUNT(*) AS Total,
                        SUM(CASE WHEN Estado = 'Pendiente' THEN 1 ELSE 0 END) AS Pendientes,
                        SUM(CASE WHEN Estado = 'Aprobada' AND MONTH(FechaResolucion) = MONTH(GETDATE()) AND YEAR(FechaResolucion) = YEAR(GETDATE()) THEN 1 ELSE 0 END) AS AprobadasMes,
                        SUM(CASE WHEN Estado = 'Rechazada' AND MONTH(FechaResolucion) = MONTH(GETDATE()) AND YEAR(FechaResolucion) = YEAR(GETDATE()) THEN 1 ELSE 0 END) AS RechazadasMes
                    FROM Rrhh_Requisicion
                ";
                var kpis = db.QueryFirstOrDefault(query);
                return Json(kpis, JsonRequestBehavior.AllowGet);
            }
        }

        // Obtener vacantes disponibles para vincular a requisicion
        public ActionResult ObtenerVacantesParaRequisicion()
        {
            using (IDbConnection db = new SqlConnection(connectionString2))
            {
                string query = @"
                    WITH PositionData AS (
                        SELECT 
                            po.PositionCode,
                            po.Name AS nombre_posicion,
                            em.nombre_Completo,
                            vo.OGERENCIA AS GERENCIA,
                            em.oSUBGERENCIA AS SUBGERENCIA,
                            em.cargo,
                            te.termination_date,
                            em.fechaasignacion,
                            CASE 
                                WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE)
                                ELSE em.fechabaja
                            END AS fechabaja,
                            ROW_NUMBER() OVER (
                                PARTITION BY po.PositionId
                                ORDER BY 
                                    CASE WHEN em.fechaasignacion IS NULL THEN 1 ELSE 0 END DESC,
                                    CASE 
                                        WHEN em.fechabaja = '0001-01-01' THEN CAST('4712-12-31' AS DATE)
                                        ELSE em.fechabaja
                                    END DESC,
                                    em.fechaasignacion ASC
                            ) AS rn
                        FROM  
                            Positions AS po 
                            INNER JOIN [dbo].[vw_organizacion] vo
                                ON po.DepartmentId = vo.idorg
                            LEFT JOIN [dbo].[emp_control] AS em 
                                ON po.PositionId = em.PositionId  
                                AND em.cargo NOT IN ('PENSIONADO','JUBILADO','PASANTE')
                            LEFT JOIN [dbo].[TERMINATIONS] AS te 
                                ON em.carnet = te.EMPLEADO_ID 
                        WHERE 
                            po.BusinessUnitId IN (300000002793695, 300000002793636)
                            AND po.ActiveStatus = 'A'
                    ),
                    ActivePositionData AS (
                        SELECT 
                            pd.*,
                            CASE 
                                WHEN pd.rn = 1 AND (pd.termination_date IS NOT NULL OR pd.fechabaja <> '4712-12-31') THEN 'Vacante por Renuncia'
                                WHEN pd.rn = 1 AND pd.nombre_Completo IS NULL THEN 'Vacante Nueva'
                                ELSE 'Otro'
                            END AS Categoria
                        FROM PositionData pd 
                        WHERE pd.rn = 1
                    )
                    SELECT PositionCode, nombre_posicion AS NombrePosicion, GERENCIA, SUBGERENCIA, Categoria
                    FROM ActivePositionData 
                    WHERE Categoria IN ('Vacante por Renuncia', 'Vacante Nueva')
                    ORDER BY GERENCIA, nombre_posicion
                ";
                var vacantes = db.Query(query).ToList();
                return Json(new { data = vacantes }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}