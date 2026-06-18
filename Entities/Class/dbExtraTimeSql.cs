using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Entities;
using Entities.ViewModels;
using Entities.MyEntities;
using Newtonsoft.Json;

namespace Entities.Class
{
    /// <summary>
    /// Capa de datos Horas Extras - SQL Server (reemplaza dbExtraTime Oracle)
    /// </summary>
    public class dbExtraTimeSql
    {
        private static readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["SqlServerXET"]?.ConnectionString
            ?? "Server=TU_SERVIDOR;Database=TU_BD;User Id=sa;Password=TU_PASSWORD;";

        #region ========== CRUD (Stored Procedures) ==========

        public string Add(ExtraTime record, long userId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@PersonId",      record.Person_Id);
                    p.Add("@DateExtraTime",  record.ExecutionDate);
                    p.Add("@HourStart",      record.HourStart);
                    p.Add("@HourEnd",        record.HourEnd);
                    p.Add("@ServiceNumber",  record.Service);
                    p.Add("@Notes",          record.Notes);
                    p.Add("@PeriodId",       record.Period_Id);
                    p.Add("@ReasonId",       record.ReasonId);
                    p.Add("@SupportFile",    record.SupportFile, DbType.Binary);
                    p.Add("@UserId",         userId);
                    p.Add("@Response",       dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xET_ExtraTimeNew", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string Edit(ExtraTime record, long userId, string justify)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@IdET",          record.Id);
                    p.Add("@DateExtraTime", record.ExecutionDate);
                    p.Add("@HourStart",     record.HourStart);
                    p.Add("@HourEnd",       record.HourEnd);
                    p.Add("@ServiceNumber", record.Service);
                    p.Add("@Notes",         record.Notes);
                    p.Add("@State",         record.Status_Id);
                    p.Add("@ReasonId",      record.ReasonId);
                    p.Add("@SupportFile",   record.SupportFile, DbType.Binary);
                    p.Add("@UserId",        userId);
                    p.Add("@Justifies",     justify);
                    p.Add("@Response",      dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xET_ExtraTimeEdit", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string ChangeState(int id, int state, long userId, string justify)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@IdET",      id);
                    p.Add("@State",     state);
                    p.Add("@UserId",    userId);
                    p.Add("@Justifies", justify);
                    p.Add("@Response",  dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xET_ExtraTimeEstatus", p, commandType: CommandType.StoredProcedure);
                    string resp = p.Get<string>("@Response");
                    return resp == "Exito al actualizar el registro" ? "OK" : resp;
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string HE_Autoriza(int idHoraExtra, DateTime horaInicio, DateTime horaFin,
                                   int idEstado, long usuario, string justificacion, string tipo)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@IdET",      idHoraExtra);
                    p.Add("@HourStart", horaInicio);
                    p.Add("@HourEnd",   horaFin);
                    p.Add("@State",     idEstado);
                    p.Add("@UserId",    usuario);
                    p.Add("@Justifies", justificacion);
                    p.Add("@AutType",   tipo);
                    p.Add("@Response",  dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xET_ExtraTimeAuthorize", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        #endregion

        #region ========== CONSULTAS (SELECT directo) ==========

        public List<ExtraTime> GetListExtraTime(string dateStart, string dateEnd, string employeeId, int state)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT et.Extratime_Id, et.Date_Extratime, et.Hour_Start,
                               CAST(DATEDIFF(MINUTE, et.Hour_Start, et.Hour_End) / 60.0 AS DECIMAL(10,2)) AS Hours,
                               et.Hour_End, r.Reason_Name, r.Reason_Id, et.Service_Number, et.Notes,
                               es.STATUS AS Status_Id, xs.STATUS AS Status, et.Person_Id,
                               e.CARNET AS Employee_Number,
                               e.NOMBRE_COMPLETO AS FullName
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON es.Extratime_Id = et.Extratime_Id
                        INNER JOIN SBM_NI.XET_STATUS xs ON xs.Status_Id = es.Status
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        INNER JOIN SBM_NI.XET_REASONS r ON et.Reason_Id = r.Reason_Id
                        WHERE es.Is_Active = 'Y' AND es.STATUS > 0
                          AND et.Date_ExtraTime BETWEEN @DateStart AND @DateEnd
                          AND (@EmployeeId IS NULL OR @EmployeeId = '' OR et.Person_Id = @EmployeeId)
                          AND (@State = -10 OR xs.Status_Id = @State)
                        ORDER BY et.Date_Extratime DESC";

                    return cn.Query<ExtraTime>(sql, new
                    {
                        DateStart = DateTime.Parse(dateStart),
                        DateEnd = DateTime.Parse(dateEnd),
                        EmployeeId = string.IsNullOrWhiteSpace(employeeId) ? (long?)null : long.Parse(employeeId),
                        State = state
                    }, commandTimeout: 60).Select(MapExtraTime).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExtraTime> GetExtraTimeById(int extratimeId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT et.Extratime_Id, et.Date_Extratime, et.Hour_Start,
                               CAST(DATEDIFF(MINUTE, et.Hour_Start, et.Hour_End) / 60.0 AS DECIMAL(10,2)) AS Hours,
                               et.Hour_End, r.Reason_Name, r.Reason_Id, et.Service_Number, et.Notes,
                               es.STATUS AS Status_Id, xs.STATUS AS Status, et.Person_Id,
                               e.CARNET AS Employee_Number,
                               e.NOMBRE_COMPLETO AS FullName,
                               et.SUPPORT_FILE
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON es.Extratime_Id = et.Extratime_Id
                        INNER JOIN SBM_NI.XET_STATUS xs ON xs.Status_Id = es.Status
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        INNER JOIN SBM_NI.XET_REASONS r ON et.Reason_Id = r.Reason_Id
                        WHERE es.Is_Active = 'Y' AND xs.Status_Id <> -1
                          AND et.Extratime_Id = @Id";

                    return cn.Query<ExtraTime>(sql, new { Id = extratimeId }).Select(MapExtraTime).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExtraTime> GetListExtraTimeConsult(long personId, string dateStart, string dateEnd)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT et.Extratime_Id, et.Date_Extratime, et.Hour_Start, et.Hour_End,
                               CAST(DATEDIFF(MINUTE, et.Hour_Start, et.Hour_End) / 60.0 AS DECIMAL(10,2)) AS Hours,
                               r.Reason_Name, et.Person_Id, xs.STATUS
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON es.Extratime_Id = et.Extratime_Id
                        INNER JOIN SBM_NI.XET_STATUS xs ON xs.Status_Id = es.Status
                        INNER JOIN SBM_NI.XET_REASONS r ON et.Reason_Id = r.Reason_Id
                        WHERE es.Is_Active = 'Y' AND xs.Status_Id >= 1
                          AND et.Date_ExtraTime BETWEEN @DateStart AND @DateEnd
                          AND et.Person_Id = @PersonId";

                    return cn.Query<ExtraTime>(sql, new
                    {
                        PersonId = personId,
                        DateStart = DateTime.Parse(dateStart),
                        DateEnd = DateTime.Parse(dateEnd)
                    }).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExtraTime> GetListExtraTimeByBoss(string dateStart, string dateEnd, string bossId, int state)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        ;WITH BossOrgs AS (
                            SELECT IDORG FROM SBM_NI.EMP2024 WHERE CARNET = @BossId
                            UNION
                            SELECT CAST(HCM AS BIGINT) FROM SBM_NI.SIGO_USER_ORGANIZATION_HRMS WHERE CARNET = @BossId
                        ),
                        OrgTree AS (
                            SELECT o.IDORG FROM SBM_NI.SIGO_ORGANIZACION o WHERE o.IDORG IN (SELECT IDORG FROM BossOrgs)
                            UNION ALL
                            SELECT o2.IDORG FROM SBM_NI.SIGO_ORGANIZACION o2
                            INNER JOIN OrgTree ot ON o2.PADRE = ot.IDORG
                        )
                        SELECT DISTINCT et.Extratime_Id, et.Date_Extratime, et.Hour_Start, et.Hour_End,
                               CAST(DATEDIFF(MINUTE, et.Hour_Start, et.Hour_End) / 60.0 AS DECIMAL(10,2)) AS Hours,
                               r.Reason_Name, et.Service_Number, et.Notes,
                               es.STATUS AS Status_Id, xs.STATUS AS Status, et.Person_Id,
                               e.CARNET AS Employee_Number, e.NOMBRE_COMPLETO AS FullName
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON es.Extratime_Id = et.Extratime_Id
                        INNER JOIN SBM_NI.XET_STATUS xs ON xs.Status_Id = es.Status
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        INNER JOIN SBM_NI.XET_REASONS r ON et.Reason_Id = r.Reason_Id
                        WHERE es.Is_Active = 'Y' AND es.STATUS > 0
                          AND et.IDORG IN (SELECT IDORG FROM OrgTree)
                          AND et.Date_ExtraTime BETWEEN @DateStart AND @DateEnd
                          AND (@State = -10 OR xs.Status_Id = @State)
                        ORDER BY et.Date_Extratime DESC
                        OPTION (MAXRECURSION 100)";

                    return cn.Query<ExtraTime>(sql, new
                    {
                        BossId = bossId,
                        DateStart = DateTime.Parse(dateStart),
                        DateEnd = DateTime.Parse(dateEnd),
                        State = state
                    }, commandTimeout: 120).ToList();
                }
            }
            catch { return new List<ExtraTime>(); }
        }

        public List<ExtraTime> GetListExtraTimeAllState(string dateStart, string dateEnd, string bossId)
        {
            return GetListExtraTimeByBoss(dateStart, dateEnd, bossId, -10);
        }

        public string gettodohoraextra(string carnet, string periodo)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT et.Extratime_Id, et.Date_Extratime, et.Hour_Start, et.Hour_End,
                               CAST(DATEDIFF(MINUTE, et.Hour_Start, et.Hour_End) / 60.0 AS DECIMAL(10,2)) AS Hours,
                               r.Reason_Name, et.Service_Number, et.Notes,
                               es.STATUS AS Status_Id, xs.STATUS AS StatusName,
                               e.NOMBRE_COMPLETO AS FullName, e.CARNET AS Employee_Number
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON es.Extratime_Id = et.Extratime_Id
                        INNER JOIN SBM_NI.XET_STATUS xs ON xs.Status_Id = es.Status
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        INNER JOIN SBM_NI.XET_REASONS r ON et.Reason_Id = r.Reason_Id
                        WHERE es.Is_Active = 'Y' AND es.STATUS > 0
                          AND e.CARNET = @Carnet AND et.Period_Id = @PeriodId";

                    var dt = new DataTable();
                    using (var da = new SqlDataAdapter(sql, cn))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@Carnet", carnet);
                        da.SelectCommand.Parameters.AddWithValue("@PeriodId", periodo);
                        da.Fill(dt);
                    }
                    return dt.Rows.Count > 0 ? JsonConvert.SerializeObject(dt) : "SIN RESULTADO";
                }
            }
            catch (Exception e) { return e.Message; }
        }

        #endregion

        #region ========== REPORTES ==========

        public List<ExtraTimeExecuted> GetListExtraTimeExecuted(long personId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT e.GERENCIAID AS Organization_Id, e.GERENCIA AS Gerencia,
                               et.PERIOD_ID,
                               SUM(CAST(DATEDIFF(MINUTE, et.HOUR_START, et.HOUR_END) / 60.0 AS DECIMAL(10,2))) AS Hours
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        WHERE et.Person_Id = @PersonId
                        GROUP BY e.GERENCIAID, e.GERENCIA, et.PERIOD_ID";

                    return cn.Query<ExtraTimeExecuted>(sql, new { PersonId = personId }).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExtraTimeReport> GetListExtraTimeReport(long personId, string startDate, string endDate)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        ;WITH BossOrgs AS (
                            SELECT ORGANIZACION AS IDORG FROM SBM_NI.EMP2024 WHERE IDPERSON = @PersonId
                            UNION
                            SELECT CAST(HCM AS BIGINT) FROM SBM_NI.SIGO_USER_ORGANIZATION_HRMS WHERE IDPERSON = @PersonId
                        ),
                        OrgTree AS (
                            SELECT o.IDORG FROM SBM_NI.SIGO_ORGANIZACION o WHERE o.IDORG IN (SELECT IDORG FROM BossOrgs)
                            UNION ALL
                            SELECT o2.IDORG FROM SBM_NI.SIGO_ORGANIZACION o2
                            INNER JOIN OrgTree ot ON o2.PADRE = ot.IDORG
                        )
                        SELECT e.GERENCIAID AS ID_GERENCIARAIZ, e.GERENCIA, et.PERIOD_ID,
                               e.CARNET AS Employee_Number, e.NOMBRE_COMPLETO AS Full_Name,
                               e.IDORG AS ORGANIZATION_ID, e.AREA,
                               ROUND(SUM(CAST(DATEDIFF(MINUTE, et.HOUR_START, et.HOUR_END) / 60.0 AS DECIMAL(10,2))), 2) AS Hours
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON et.EXTRATIME_ID = es.EXTRATIME_ID
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        WHERE es.Is_Active = 'Y' AND es.STATUS >= 3
                          AND et.Date_ExtraTime BETWEEN @StartDate AND @EndDate
                          AND et.IDORG IN (SELECT IDORG FROM OrgTree)
                        GROUP BY e.GERENCIAID, e.GERENCIA, et.PERIOD_ID,
                                 e.CARNET, e.NOMBRE_COMPLETO, e.IDORG, e.AREA
                        OPTION (MAXRECURSION 100)";

                    return cn.Query<ExtraTimeReport>(sql, new
                    {
                        PersonId = personId,
                        StartDate = DateTime.Parse(startDate),
                        EndDate = DateTime.Parse(endDate)
                    }).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExtraTimeReport> GetDetailExtraTimeReport(long personId, string startDate, string endDate)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        ;WITH BossOrgs AS (
                            SELECT ORGANIZACION AS IDORG FROM SBM_NI.EMP2024 WHERE IDPERSON = @PersonId
                            UNION
                            SELECT CAST(HCM AS BIGINT) FROM SBM_NI.SIGO_USER_ORGANIZATION_HRMS WHERE IDPERSON = @PersonId
                        ),
                        OrgTree AS (
                            SELECT o.IDORG FROM SBM_NI.SIGO_ORGANIZACION o WHERE o.IDORG IN (SELECT IDORG FROM BossOrgs)
                            UNION ALL
                            SELECT o2.IDORG FROM SBM_NI.SIGO_ORGANIZACION o2
                            INNER JOIN OrgTree ot ON o2.PADRE = ot.IDORG
                        )
                        SELECT e.GERENCIAID AS ID_GERENCIARAIZ, e.GERENCIA, et.PERIOD_ID,
                               et.DATE_EXTRATIME,
                               CONVERT(VARCHAR(5), et.HOUR_START, 108) AS HOUR_START,
                               CONVERT(VARCHAR(5), et.HOUR_END, 108) AS HOUR_END,
                               e.CARNET AS Employee_Number, e.NOMBRE_COMPLETO AS Full_Name,
                               e.IDORG AS ORGANIZATION_ID, e.AREA,
                               ROUND(CAST(DATEDIFF(MINUTE, et.HOUR_START, et.HOUR_END) / 60.0 AS DECIMAL(10,2)), 2) AS Hours,
                               xs.STATUS AS StatusName
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON et.EXTRATIME_ID = es.EXTRATIME_ID
                        INNER JOIN SBM_NI.XET_STATUS xs ON xs.Status_Id = es.Status
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        WHERE es.Is_Active = 'Y' AND es.STATUS >= 1
                          AND et.Date_ExtraTime BETWEEN @StartDate AND @EndDate
                          AND et.IDORG IN (SELECT IDORG FROM OrgTree)
                        OPTION (MAXRECURSION 100)";

                    return cn.Query<ExtraTimeReport>(sql, new
                    {
                        PersonId = personId,
                        StartDate = DateTime.Parse(startDate),
                        EndDate = DateTime.Parse(endDate)
                    }).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExtraTimeHistoric> GetListExtraTimeHistoric(long organizationId, string startDate, string endDate)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT YEAR(et.DATE_EXTRATIME) AS xyear,
                               DATENAME(MONTH, et.DATE_EXTRATIME) AS xmonth,
                               SUM(CAST(DATEDIFF(MINUTE, et.HOUR_START, et.HOUR_END) / 60.0 AS DECIMAL(10,2))) AS Hours
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON et.EXTRATIME_ID = es.EXTRATIME_ID
                        WHERE es.Is_Active = 'Y' AND es.STATUS >= 3
                          AND et.GERENCIAHCM = @OrgId
                          AND et.Date_ExtraTime BETWEEN @StartDate AND @EndDate
                        GROUP BY YEAR(et.DATE_EXTRATIME), DATENAME(MONTH, et.DATE_EXTRATIME)";

                    return cn.Query<ExtraTimeHistoric>(sql, new
                    {
                        OrgId = organizationId,
                        StartDate = DateTime.Parse(startDate),
                        EndDate = DateTime.Parse(endDate)
                    }).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExtraTimeDetailView> GetAllDetailExtraTime(string bossId, string startDate, string endDate)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        ;WITH BossOrgs AS (
                            SELECT ORGANIZACION AS IDORG FROM SBM_NI.EMP2024 WHERE CARNET = @BossId
                            UNION
                            SELECT CAST(HCM AS BIGINT) FROM SBM_NI.SIGO_USER_ORGANIZATION_HRMS WHERE CARNET = @BossId
                        ),
                        OrgTree AS (
                            SELECT o.IDORG FROM SBM_NI.SIGO_ORGANIZACION o WHERE o.IDORG IN (SELECT IDORG FROM BossOrgs)
                            UNION ALL
                            SELECT o2.IDORG FROM SBM_NI.SIGO_ORGANIZACION o2
                            INNER JOIN OrgTree ot ON o2.PADRE = ot.IDORG
                        )
                        SELECT et.PERSON_ID, e.IDORG AS AreaId, e.AREA AS AreaName,
                               e.CARNET AS Employee_Number,
                               e.NOMBRE_COMPLETO AS EmployeeName,
                               et.DATE_EXTRATIME,
                               CAST(DATEDIFF(MINUTE, et.HOUR_START, et.HOUR_END) / 60.0 AS DECIMAL(10,2)) AS Hours,
                               xs.STATUS
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON et.EXTRATIME_ID = es.EXTRATIME_ID
                        INNER JOIN SBM_NI.XET_STATUS xs ON xs.Status_Id = es.Status
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        WHERE es.Is_Active = 'Y' AND es.STATUS > 0
                          AND et.Date_ExtraTime BETWEEN @StartDate AND @EndDate
                          AND et.IDORG IN (SELECT IDORG FROM OrgTree)
                        OPTION (MAXRECURSION 100)";

                    return cn.Query<ExtraTimeDetailView>(sql, new
                    {
                        BossId = bossId,
                        StartDate = DateTime.Parse(startDate),
                        EndDate = DateTime.Parse(endDate)
                    }).ToList();
                }
            }
            catch { return null; }
        }

        #endregion

        #region ========== PERÍODOS y AUXILIARES ==========

        public string ActiveDate()
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"SELECT MIN(Date_Start) AS Date_Start, MAX(Date_End) AS Date_End
                                   FROM SBM_NI.XET_PERIODS WHERE Date_Close IS NULL AND Last_Date >= GETDATE()";
                    var row = cn.QueryFirstOrDefault(sql);
                    if (row == null || row.Date_Start == null) return "";
                    return ((DateTime)row.Date_Start).ToString("dd/MM/yyyy") + ";" + ((DateTime)row.Date_End).ToString("dd/MM/yyyy");
                }
            }
            catch { return ""; }
        }

        public string GetActivePeriod(string executionDate)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"SELECT Period_Id FROM SBM_NI.XET_PERIODS
                                   WHERE @ExecDate BETWEEN Date_Start AND Date_End";
                    return cn.QueryFirstOrDefault<string>(sql, new { ExecDate = DateTime.Parse(executionDate) }) ?? "";
                }
            }
            catch { return ""; }
        }

        public string GetPeriodDashboard()
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"SELECT TOP 1 PERIOD_ID FROM SBM_NI.XET_PERIODS
                                   WHERE Date_Close IS NULL AND Last_Date >= GETDATE()
                                   GROUP BY PERIOD_ID";
                    return cn.QueryFirstOrDefault<string>(sql) ?? "";
                }
            }
            catch { return ""; }
        }

        public string GetPersonId(int extraTimeId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.QueryFirstOrDefault<string>(
                        "SELECT CAST(Person_Id AS VARCHAR) FROM SBM_NI.XET_EXTRATIME WHERE EXTRATIME_ID = @Id",
                        new { Id = extraTimeId }) ?? "";
                }
            }
            catch { return ""; }
        }

        public string GetPeriodId(int extraTimeId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.QueryFirstOrDefault<string>(
                        "SELECT Period_Id FROM SBM_NI.XET_EXTRATIME WHERE EXTRATIME_ID = @Id",
                        new { Id = extraTimeId }) ?? "";
                }
            }
            catch { return ""; }
        }

        public string GetOrganizationId(long personId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.QueryFirstOrDefault<string>(
                        "SELECT CAST(GERENCIAID AS VARCHAR) FROM SBM_NI.EMP2024 WHERE IDPERSON = @Id",
                        new { Id = personId }) ?? "";
                }
            }
            catch { return ""; }
        }

        public string GetOrganizationId2(string carnet)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.QueryFirstOrDefault<string>(
                        "SELECT CAST(GERENCIAID AS VARCHAR) FROM SBM_NI.EMP2024 WHERE CARNET = @Carnet",
                        new { Carnet = carnet }) ?? "";
                }
            }
            catch { return ""; }
        }

        public List<Reasons> GetReasonsList()
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.Query<Reasons>("SELECT Reason_Id AS ReasonId, Reason_Name AS ReasonName FROM SBM_NI.XET_REASONS").ToList();
                }
            }
            catch { return null; }
        }

        public List<Areas> GetAreasList(string bossId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        ;WITH EmpTree AS (
                            SELECT IDPERSON, IDORG FROM SBM_NI.EMP2024 WHERE CARNET = @BossId
                            UNION ALL
                            SELECT e2.IDPERSON, e2.IDORG FROM SBM_NI.EMP2024 e2
                            INNER JOIN EmpTree t ON e2.BOSS_ID = t.IDPERSON
                        )
                        SELECT DISTINCT IDORG AS AreaId, AREA AS AreaName
                        FROM SBM_NI.EMP2024 WHERE IDORG IN (SELECT DISTINCT IDORG FROM EmpTree)
                        OPTION (MAXRECURSION 100)";

                    return cn.Query<Areas>(sql, new { BossId = bossId }).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExtratimeManagnment> GetAssignmentByPeriod(string period, long organizationId)
        {
            return null; // Método deprecado en Oracle
        }

        public List<ExtratimeManagnment> GetAssignmentByPeriod2(string period, long organizationId)
        {
            return null; // Método deprecado en Oracle
        }

        public List<ExtratimeManagnment> GetExtraTimeBudgetVsExecuted(long bossId)
        {
            return null; // Método deprecado en Oracle
        }

        public List<ExtratimeManagnment> GetExtraTimeExecuted(long bossId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        ;WITH BossOrgs AS (
                            SELECT ORGANIZACION AS IDORG FROM SBM_NI.EMP2024 WHERE IDPERSON = @BossId
                            UNION
                            SELECT CAST(HCM AS BIGINT) FROM SBM_NI.SIGO_USER_ORGANIZATION_HRMS WHERE IDPERSON = @BossId
                        ),
                        OrgTree AS (
                            SELECT o.IDORG FROM SBM_NI.SIGO_ORGANIZACION o WHERE o.IDORG IN (SELECT IDORG FROM BossOrgs)
                            UNION ALL
                            SELECT o2.IDORG FROM SBM_NI.SIGO_ORGANIZACION o2
                            INNER JOIN OrgTree ot ON o2.PADRE = ot.IDORG
                        )
                        SELECT e.IDORG AS Organization_Id, e.AREA, et.PERIOD_ID,
                               ROUND(SUM(CAST(DATEDIFF(MINUTE, et.HOUR_START, et.HOUR_END) / 60.0 AS DECIMAL(10,2))), 2) AS Executed_Hours
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON et.EXTRATIME_ID = es.EXTRATIME_ID
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        INNER JOIN SBM_NI.XET_PERIODS pe ON pe.PERIOD_ID = et.PERIOD_ID
                        WHERE es.Is_Active = 'Y' AND es.STATUS >= 1
                          AND pe.DATE_END >= DATEADD(MONTH, -5, GETDATE())
                          AND et.IDORG IN (SELECT IDORG FROM OrgTree)
                        GROUP BY e.IDORG, e.AREA, et.PERIOD_ID
                        OPTION (MAXRECURSION 100)";

                    return cn.Query<ExtratimeManagnment>(sql, new { BossId = bossId }).ToList();
                }
            }
            catch { return null; }
        }

        #endregion

        #region ========== Mapeo auxiliar ==========

        private ExtraTime MapExtraTime(dynamic row)
        {
            return new ExtraTime
            {
                Id = (int)(row.Extratime_Id ?? 0),
                Person_Id = (int)(row.Person_Id ?? 0),
                Employee = row.FullName?.ToString() ?? "",
                EmployeeNumber = row.Employee_Number?.ToString() ?? "",
                ExecutionDate = row.Date_Extratime ?? DateTime.MinValue,
                HourStart = row.Hour_Start ?? DateTime.MinValue,
                HourEnd = row.Hour_End ?? DateTime.MinValue,
                Hours = (double)(row.Hours ?? 0),
                Reasons = row.Reason_Name?.ToString() ?? "",
                ReasonId = (int)(row.Reason_Id ?? 0),
                Service = row.Service_Number?.ToString() ?? "",
                Notes = row.Notes?.ToString() ?? "",
                Status_Id = (int)(row.Status_Id ?? 0),
                Status = row.Status?.ToString() ?? ""
            };
        }

        #endregion
    }
}
