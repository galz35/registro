using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Script.Serialization;
using Dapper;
using Entities;
using Entities.ViewModels;
using Newtonsoft.Json;
using WebApi.Models;
using slnRhonline.Models;

namespace Entities.Class
{
    /// <summary>
    /// Capa de datos Viáticos (Expenses) - SQL Server (reemplaza dbExpenses Oracle)
    /// </summary>
    public class dbExpensesSql
    {
        private static readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["SqlServerXET"]?.ConnectionString
            ?? "Server=TU_SERVIDOR;Database=TU_BD;User Id=sa;Password=TU_PASSWORD;";

        #region ========== CRUD (Stored Procedures) ==========

        public string InsertExpense(viatico record)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@PersonId",         record.PersonId);
                    p.Add("@ExpenseDate",      record.ExpenseDate);
                    p.Add("@Notes",            record.ExpenseNotes);
                    p.Add("@Route",            record.Route);
                    p.Add("@Justify",          record.Justify);
                    p.Add("@ClassId",          record.ClassId);
                    p.Add("@ServiceNumber",    record.ServiceNumber);
                    p.Add("@RegisterPersonId", record.RegisterPersonId);
                    p.Add("@ReasonId",         record.ReasonId);
                    p.Add("@ExpensePeriodId",  record.ExpensePeriodId);
                    p.Add("@VehicleNumber",    record.VehicleNumber);
                    p.Add("@Response",         dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_InsertExpense", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string InsertExpense2(Expenses record, long registerPersonId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@PersonId",         record.PersonId);
                    p.Add("@ExpenseDate",      record.ExpenseDate);
                    p.Add("@Notes",            record.ExpenseNotes);
                    p.Add("@Route",            record.Route);
                    p.Add("@Justify",          record.Justify);
                    p.Add("@ClassId",          record.ClassId);
                    p.Add("@ServiceNumber",    record.ServiceNumber);
                    p.Add("@RegisterPersonId", registerPersonId);
                    p.Add("@ReasonId",         record.ReasonId);
                    p.Add("@ExpensePeriodId",  record.ExpensePeriodId);
                    p.Add("@VehicleNumber",    record.VehicleNumber);
                    p.Add("@Response",         dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_InsertExpense2", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string InsertExpenseHCM(Expenses record, long registerPersonId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@PersonId",         record.PersonId);
                    p.Add("@ExpenseDate",      record.ExpenseDate);
                    p.Add("@Notes",            record.ExpenseNotes);
                    p.Add("@Route",            record.Route);
                    p.Add("@Justify",          record.Justify);
                    p.Add("@ClassId",          record.ClassId);
                    p.Add("@ServiceNumber",    record.ServiceNumber);
                    p.Add("@RegisterPersonId", registerPersonId);
                    p.Add("@ReasonId",         record.ReasonId);
                    p.Add("@ExpensePeriodId",  record.ExpensePeriodId);
                    p.Add("@VehicleNumber",    record.VehicleNumber);
                    p.Add("@Response",         dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_CExpensehcm", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string UpdateExpense(Expenses record)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseId",     record.ExpenseId);
                    p.Add("@ExpenseDate",   record.ExpenseDate);
                    p.Add("@ExpenseRoute",  record.Route);
                    p.Add("@ExpenseJustify", record.Justify);
                    p.Add("@ClassId",       record.ClassId);
                    p.Add("@ServiceNumber", record.ServiceNumber);
                    p.Add("@ReasonId",      record.ReasonId);
                    p.Add("@VehicleNumber", record.VehicleNumber);
                    p.Add("@Response",      dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_UpdateExpense", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string DeleteExpense(int expenseId, long registerPersonId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseId",        expenseId);
                    p.Add("@RegisterPersonId", registerPersonId);
                    p.Add("@Response",         dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_DeleteExpense", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string ChangeStateExpense(int expenseId, int statusId, long registerPersonId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseId",        expenseId);
                    p.Add("@StatusId",         statusId);
                    p.Add("@RegisterPersonId", registerPersonId);
                    p.Add("@Response",         dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_ChangeStateExpense", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string ChangeStateExpense2(string keysET, int statusId, long registerPersonId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseIdList",    keysET);
                    p.Add("@StatusId",         statusId);
                    p.Add("@RegisterPersonId", registerPersonId);
                    p.Add("@Response",         dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_ChangeStateExpenselist", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string UpdateYieldFile(Expenses record)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseId",          record.ExpenseId);
                    p.Add("@YieldFile",          record.YieldFile, DbType.Binary);
                    p.Add("@YieldFileExtension", record.YieldFileExtension);
                    p.Add("@Response",           dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_UpdateYieldFile", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string UpdateDepositFile(Expenses record)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseId",   record.ExpenseId);
                    p.Add("@DepositFile", record.DepositFile, DbType.Binary);
                    p.Add("@Response",    dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_UpdateDepositFile", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string UpdateYield(Expenses record)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseId",            record.ExpenseId);
                    p.Add("@YieldFile",            record.YieldFile, DbType.Binary);
                    p.Add("@YieldFileExtension",   record.YieldFileExtension);
                    p.Add("@DepositFile",          record.DepositFile, DbType.Binary);
                    p.Add("@DepositFileExtension", record.DepositFileExtension);
                    p.Add("@YieldNotes",           record.YieldNotes);
                    p.Add("@Response",             dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_UpdateYield", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string InsertExpenseDetail(ExpenseDetail record)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseId",       record.ExpenseId);
                    p.Add("@ClasificationId", record.ClasificationId);
                    p.Add("@CategoryId",      record.CategoryId);
                    p.Add("@SubCategoryId",   record.SubCategoryId);
                    p.Add("@DepartmentId",    record.DepartmentId);
                    p.Add("@NotesDetail",     record.ExpenseDetailNotes);
                    p.Add("@Amount",          record.Amount);
                    p.Add("@YieldAmount",     record.YieldAmount);
                    p.Add("@ReturnAmount",    record.ReturnAmount);
                    p.Add("@HourStart",       record.HourStart);
                    p.Add("@Response",        dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_InsertExpenseDetail", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string UpdateExpenseDetail(ExpenseDetail record)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseDetailId", record.ExpenseDetailId);
                    p.Add("@ClasificationId", record.ClasificationId);
                    p.Add("@CategoryId",      record.CategoryId);
                    p.Add("@SubCategoryId",   record.SubCategoryId);
                    p.Add("@DepartmentId",    record.DepartmentId);
                    p.Add("@DetailNotes",     record.ExpenseDetailNotes);
                    p.Add("@DetailAmount",    record.Amount);
                    p.Add("@YieldAmount",     record.YieldAmount);
                    p.Add("@ReturnAmount",    record.ReturnAmount);
                    p.Add("@Response",        dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_UpdateExpenseDetail", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string UpdateReturnAmount(ExpenseDetail record)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseDetailId", record.ExpenseDetailId);
                    p.Add("@ReturnAmount",    record.ReturnAmount);
                    p.Add("@Response",        dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_UpdateReturnAmount", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string DeleteExpenseDetail(int expenseDetailId, string deleteNotes)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseDetailId", expenseDetailId);
                    p.Add("@DeleteNotes",     deleteNotes);
                    p.Add("@Response",        dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_DeleteExpenseDetail", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string DeleteAllExpenseDetail(int expenseId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpenseId", expenseId);
                    p.Add("@Response",  dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                    cn.Execute("SBM_NI.spr_xet_DeleteAllExpenseDetail", p, commandType: CommandType.StoredProcedure);
                    return p.Get<string>("@Response");
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        #endregion

        #region ========== CONSULTAS (SELECT) ==========

        public List<ExpenseDetail> GetAllExpenseDetails()
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.Query<ExpenseDetail>(@"
                        SELECT EXPENSE_DETAIL_ID AS ExpenseDetailId, EXPENSE_ID AS ExpenseId,
                                CLASIFICATION_ID AS ClasificationId, CATEGORY_ID AS CategoryId,
                                SUBCATEGORY_ID AS SubCategoryId, NOTES AS ExpenseDetailNotes,
                                AMOUNT, HOUR_START AS HourStart, YIELD_AMOUNT AS YieldAmount,
                                DEPARTMENT_ID AS DepartmentId
                        FROM SBM_NI.XET_EXPENSE_DETAIL").ToList();
                }
            }
            catch { return null; }
        }

        public List<ExpenseDetail> GetAllExpenseDetailById(int expenseId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.Query<ExpenseDetail>(@"
                        SELECT EXPENSE_DETAIL_ID AS ExpenseDetailId, EXPENSE_ID AS ExpenseId,
                                CLASIFICATION_ID AS ClasificationId, CATEGORY_ID AS CategoryId,
                                SUBCATEGORY_ID AS SubCategoryId, NOTES AS ExpenseDetailNotes,
                                AMOUNT, HOUR_START AS HourStart, YIELD_AMOUNT AS YieldAmount,
                                DEPARTMENT_ID AS DepartmentId
                        FROM SBM_NI.XET_EXPENSE_DETAIL WHERE EXPENSE_ID = @Id",
                        new { Id = expenseId }).ToList();
                }
            }
            catch { return null; }
        }

        public List<Expenses> GetAllExpenses(int expenseId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.Query<Expenses>(@"
                        SELECT EX.EXPENSE_ID AS ExpenseId, EX.PERSON_ID AS PersonId,
                                EX.EXPENSE_DATE AS ExpenseDate, EX.CLASS_ID AS ClassId,
                                EX.NOTES AS ExpenseNotes, EX.JUSTIFY, EX.ROUTE,
                                EX.SERVICE_NUMBER AS ServiceNumber, EX.REASON_ID AS ReasonId,
                                EX.EXPENSE_PERIOD_ID AS ExpensePeriodId,
                                EX.YIELD_FILE AS YieldFile, EX.DEPOSIT_FILE AS DepositFile,
                                EX.YIELD_FILE_EXTENSION AS YieldFileExtension,
                                EX.VEHICLE_NUMBER AS VehicleNumber, EX.YIELD_NOTES AS YieldNotes
                        FROM SBM_NI.XET_EXPENSES EX
                        INNER JOIN SBM_NI.EMP2024 Emp ON EX.CODIGO = Emp.CARNET
                        WHERE EX.EXPENSE_ID = @Id",
                        new { Id = expenseId }).ToList();
                }
            }
            catch { return null; }
        }

        public string ObtenerTodosfaltarendir()
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var data = cn.Query(@"
                        SELECT DISTINCT CARNET, NOMBRE_COMPLETO, CARGO, GERENCIA,
                                START_DATE, DATENAME(WEEKDAY, START_DATE) AS DIA_INICIO,
                                END_DATE, DATENAME(WEEKDAY, END_DATE) AS DIA_FIN,
                                MONTO_TOTAL
                        FROM SBM_NI.VIATICOSINRENDIR ORDER BY START_DATE DESC").ToList();
                    return JsonConvert.SerializeObject(data);
                }
            }
            catch (Exception e) { return e.Message; }
        }

        public string ObtenerTodosfaltarendirCarnet(string carnet)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var data = cn.Query(@"
                        SELECT DISTINCT CARNET, NOMBRE_COMPLETO, CARGO, GERENCIA,
                                START_DATE, DATENAME(WEEKDAY, START_DATE) AS DIA_INICIO,
                                END_DATE, DATENAME(WEEKDAY, END_DATE) AS DIA_FIN,
                                MONTO_TOTAL
                        FROM SBM_NI.VIATICOSINRENDIR WHERE CARNET = @Carnet ORDER BY START_DATE DESC",
                        new { Carnet = carnet }).ToList();
                    return JsonConvert.SerializeObject(data);
                }
            }
            catch (Exception e) { return e.Message; }
        }

        public List<periodovt> GetAllExpensesx(long expenseId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.Query<periodovt>(@"
                        SELECT STATUS, LAST_DATE AS LastDate, CLASS_ID AS ClassId, NOTES
                        FROM SBM_NI.XET_EXPENSE_PERIODS
                        WHERE (MANAGEMENT_ID = @Id OR IDORGHCM = @Id)
                          AND STATUS = 'NO PAGADO' AND CAST(LAST_DATE AS DATE) >= CAST(GETDATE() AS DATE)",
                        new { Id = expenseId }).ToList();
                }
            }
            catch { return null; }
        }

        public string GetExpenseStatusById(int expenseId)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var row = cn.QueryFirstOrDefault<ExpensesStatus>(@"
                        SELECT EXPENSE_ID AS ExpenseId, STATUS_ID AS StatusId, IS_ACTIVE AS IsActive
                        FROM SBM_NI.XET_EXPENSES_STATUS
                        WHERE EXPENSE_ID = @Id AND IS_ACTIVE = 'Y' AND STATUS_ID NOT IN (1)",
                        new { Id = expenseId });
                    return row != null ? JsonConvert.SerializeObject(row) : null;
                }
            }
            catch { return null; }
        }

        public List<ExpenseDetailView> GetExpenseDetailViewByPersonId(string carnet)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT EX.EXPENSE_ID, Emp.IDPERSON AS PERSON_ID, Emp.CARNET,
                               Emp.NOMBRE_COMPLETO AS FullName,
                               EX.EXPENSE_DATE, EX.CLASS_ID, EX.JUSTIFY, EX.REASON_ID,
                               EX.ROUTE, EX.SERVICE_NUMBER,
                               ISNULL(ED.EXPENSE_DETAIL_ID, 0) AS EXPENSE_DETAIL_ID,
                               ISNULL(C.CONCEPT_ID, 0) AS ClasificationId,
                               ISNULL(ED.CATEGORY_ID, 0) AS CategoryId,
                               ISNULL(ED.SUBCATEGORY_ID, 0) AS SubCategoryId,
                               C.CONCEPT_NAME AS Clasification,
                               C2.CONCEPT_NAME AS Category,
                               C3.CONCEPT_NAME AS SubCategory,
                               C4.CONCEPT_NAME AS ClassName,
                               ISNULL(ED.AMOUNT, 0) AS AMOUNT,
                               XS.STATUS, Emp.CARNET AS CARNET1,
                               ISNULL(ED.HOUR_START, GETDATE()) AS HOUR_START
                        FROM SBM_NI.XET_EXPENSES EX
                        INNER JOIN SBM_NI.XET_EXPENSE_DETAIL ED ON EX.EXPENSE_ID = ED.EXPENSE_ID
                        INNER JOIN SBM_NI.XET_CONCEPTS C4 ON EX.CLASS_ID = C4.CONCEPT_ID
                        INNER JOIN SBM_NI.XET_CONCEPTS C ON ED.CLASIFICATION_ID = C.CONCEPT_ID
                        INNER JOIN SBM_NI.XET_CONCEPTS C2 ON ED.CATEGORY_ID = C2.CONCEPT_ID
                        INNER JOIN SBM_NI.XET_CONCEPTS C3 ON ED.SUBCATEGORY_ID = C3.CONCEPT_ID
                        INNER JOIN SBM_NI.XET_EXPENSES_STATUS ES ON EX.EXPENSE_ID = ES.EXPENSE_ID
                        INNER JOIN SBM_NI.XET_STATUS XS ON ES.STATUS_ID = XS.STATUS_ID
                        INNER JOIN SBM_NI.EMP2024 Emp ON EX.CODIGO = Emp.CARNET
                        WHERE ES.IS_ACTIVE = 'Y' AND EX.CODIGO = @Carnet";

                    var dt = new DataTable();
                    using (var da = new SqlDataAdapter(sql, cn))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@Carnet", carnet);
                        da.Fill(dt);
                    }

                    if (dt.Rows.Count == 0) return null;

                    string jsonresult = JsonConvert.SerializeObject(dt);
                    var expensesjson = JsonConvert.DeserializeObject<List<Expensejson>>(jsonresult);

                    return (from item in expensesjson
                            group item by new
                            {
                                item.EXPENSE_ID, item.CARNET1, item.PERSON_ID, item.FULLNAME,
                                item.EXPENSE_DATE, item.CLASS_ID, item.CLASSNAME, item.REASON_ID,
                                item.JUSTIFY, item.ROUTE, item.SERVICE_NUMBER, item.STATUS
                            } into g
                            select new ExpenseDetailView
                            {
                                ExpenseId = (int)g.Key.EXPENSE_ID,
                                EmployeeNumber = g.Key.CARNET1,
                                PersonId = (long)g.Key.PERSON_ID,
                                FullName = g.Key.FULLNAME,
                                ExpenseDate = g.Key.EXPENSE_DATE,
                                ClassId = (int)g.Key.CLASS_ID,
                                ClassName = g.Key.CLASSNAME,
                                ReasonId = (int)g.Key.REASON_ID,
                                Justify = g.Key.JUSTIFY,
                                Route = g.Key.ROUTE,
                                ServiceNumber = g.Key.SERVICE_NUMBER?.ToString(),
                                ExpenseStatus = g.Key.STATUS,
                                TotalAmount = (decimal)g.Sum(y => y.AMOUNT)
                            }).OrderByDescending(o => o.ExpenseDate).ToList();
                }
            }
            catch { return null; }
        }

        public List<ExpenseDetailView> GetExpenseDetailViewByPersonIdx(string carnet)
        {
            return GetExpenseDetailViewByPersonId(carnet);
        }

        public string GetAllExpensesForAuthorize(long bossId, int status)
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
                        SELECT EX.EXPENSE_ID, EX.PERSON_ID, EX.EXPENSE_DATE, EX.CLASS_ID,
                               EX.NOTES, EX.JUSTIFY, EX.ROUTE, EX.SERVICE_NUMBER, EX.CODIGO,
                               EX.VEHICLE_NUMBER,
                               ISNULL(ED.EXPENSE_DETAIL_ID, 0) AS EXPENSE_DETAIL_ID,
                               ISNULL(C.CONCEPT_ID, 0) AS ClasificationId,
                               C.CONCEPT_NAME AS Clasification,
                               C2.CONCEPT_NAME AS Category,
                               C3.CONCEPT_NAME AS SubCategory,
                               C4.CONCEPT_NAME AS ClassName,
                               ISNULL(ED.AMOUNT, 0) AS AMOUNT,
                               XS.STATUS, ES.STATUS_ID,
                               Emp.NOMBRE_COMPLETO AS FullName
                        FROM SBM_NI.XET_EXPENSES EX
                        INNER JOIN SBM_NI.XET_EXPENSE_DETAIL ED ON EX.EXPENSE_ID = ED.EXPENSE_ID
                        INNER JOIN SBM_NI.XET_CONCEPTS C4 ON EX.CLASS_ID = C4.CONCEPT_ID
                        INNER JOIN SBM_NI.XET_CONCEPTS C ON ED.CLASIFICATION_ID = C.CONCEPT_ID
                        INNER JOIN SBM_NI.XET_CONCEPTS C2 ON ED.CATEGORY_ID = C2.CONCEPT_ID
                        INNER JOIN SBM_NI.XET_CONCEPTS C3 ON ED.SUBCATEGORY_ID = C3.CONCEPT_ID
                        INNER JOIN SBM_NI.XET_EXPENSES_STATUS ES ON EX.EXPENSE_ID = ES.EXPENSE_ID
                        INNER JOIN SBM_NI.XET_STATUS XS ON ES.STATUS_ID = XS.STATUS_ID
                        INNER JOIN SBM_NI.EMP2024 Emp ON EX.CODIGO = Emp.CARNET
                        WHERE ES.IS_ACTIVE = 'Y' AND ES.STATUS_ID = @Status
                          AND EX.ORGRANIZACION_OD IN (SELECT IDORG FROM OrgTree)
                        OPTION (MAXRECURSION 100)";

                    var results = cn.Query<ExpenseDetailView>(sql, new { BossId = bossId, Status = status }, commandTimeout: 120).ToList();
                    return results.Count > 0 ? JsonConvert.SerializeObject(results) : "SIN RESULTADO";
                }
            }
            catch (Exception e) { return e.Message; }
        }

        public string GetAllExpensesForAuthorize2(long bossId, int status)
        {
            return GetAllExpensesForAuthorize(bossId, status);
        }

        public List<ExpensesStatus> GetAllExpensesStatus()
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    return cn.Query<ExpensesStatus>(@"
                        SELECT EXPENSE_ID AS ExpenseId, EXPENSE_STATUS_DATE AS ExpenseStatusDate,
                                STATUS_ID AS StatusId, IS_ACTIVE AS IsActive
                        FROM SBM_NI.XET_EXPENSES_STATUS").ToList();
                }
            }
            catch { return null; }
        }

        #endregion

        #region ========== PERÍODOS DE VIÁTICOS ==========

        public bool experiodInsert(int expensePeriodId, DateTime? startDate, DateTime? endDate,
                                    DateTime? paidDate, string notes, int? classId, string status,
                                    DateTime? lastDate, string periodId, DateTime? yieldDate, int? managementId)
                                    
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpensePeriodId", expensePeriodId);
                    p.Add("@StartDate",       startDate);
                    p.Add("@EndDate",         endDate);
                    p.Add("@PaidDate",        paidDate);
                    p.Add("@Notes",           notes ?? "");
                    p.Add("@ClassId",         classId);
                    p.Add("@Status",          status ?? "");
                    p.Add("@LastDate",        lastDate);
                    p.Add("@PeriodId",        periodId ?? "");
                    p.Add("@YieldDate",       yieldDate);
                    p.Add("@ManagementId",    managementId);

                    cn.Execute("SBM_NI.spr_xet_InsertExpensePeriod", p, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch { return false; }
        }

        public DataRow experioGetById(int expensePeriodId)
        {
            try
            {
                var dt = new DataTable();
                using (var cn = new SqlConnection(_connectionString))
                using (var da = new SqlDataAdapter("SELECT * FROM SBM_NI.XET_EXPENSE_PERIODS WHERE EXPENSE_PERIOD_ID = @Id", cn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@Id", expensePeriodId);
                    da.Fill(dt);
                }
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
            catch { return null; }
        }

        public DataTable experiodGetAll()
        {
            var dt = new DataTable();
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                using (var da = new SqlDataAdapter("SELECT TOP 25 * FROM SBM_NI.XET_EXPENSE_PERIODS ORDER BY EXPENSE_PERIOD_ID DESC", cn))
                {
                    da.Fill(dt);
                }
            }
            catch { }
            return dt;
        }

        public bool experiodUpdate(int expensePeriodId, DateTime? startDate, DateTime? endDate,
                                    DateTime? paidDate, string notes, int? classId, string status,
                                    DateTime? lastDate, string periodId, DateTime? yieldDate)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@ExpensePeriodId", expensePeriodId);
                    p.Add("@StartDate",       startDate);
                    p.Add("@EndDate",         endDate);
                    p.Add("@PaidDate",        paidDate);
                    p.Add("@Notes",           notes ?? "");
                    p.Add("@ClassId",         classId);
                    p.Add("@Status",          status ?? "");
                    p.Add("@LastDate",        lastDate);
                    p.Add("@PeriodId",        periodId ?? "");
                    p.Add("@YieldDate",       yieldDate);

                    cn.Execute("SBM_NI.spr_xet_UpdateExpensePeriod", p, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch { return false; }
        }

        #endregion

        #region ========== REPORTES NÓMINA ==========

        public List<Nominaexcel> Nomina(string fecha)
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT e.GERENCIA, et.PERIOD_ID, e.CARNET AS EMPLOYEE_NUMBER,
                               e.NOMBRE_COMPLETO AS FULL_NAME, et.PERSON_ID,
                               xs.STATUS, e.AREA,
                               ROUND(SUM(CAST(DATEDIFF(MINUTE, et.HOUR_START, et.HOUR_END) / 60.0 AS DECIMAL(10,2))), 2) AS HOURS
                        FROM SBM_NI.XET_EXTRATIME et
                        INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON et.EXTRATIME_ID = es.EXTRATIME_ID
                        INNER JOIN SBM_NI.XET_STATUS xs ON xs.Status_Id = es.Status
                        INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                        WHERE es.Is_Active = 'Y' AND es.STATUS >= 1
                          AND et.PERIOD_ID = @Fecha
                        GROUP BY e.GERENCIA, et.PERIOD_ID, e.CARNET, e.NOMBRE_COMPLETO,
                                 et.PERSON_ID, xs.STATUS, e.AREA";

                    return cn.Query<Nominaexcel>(sql, new { Fecha = fecha }).ToList();
                }
            }
            catch { return new List<Nominaexcel>(); }
        }

        public DataTable HE_AsignacionDetalleGerencia(string periodo, string gerencia)
        {
            var dt = new DataTable();
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                using (var da = new SqlDataAdapter(@"
                    SELECT a.DATE_ADD, a.QUANTITY, a.AMOUNT, a.JUSTIFY
                    FROM SBM_NI.XET_ASSIGNMENTS a
                    INNER JOIN SBM_NI.VW_ORGANIZACION o ON o.ORGANIZATION_ID = a.ORGANIZATION_ID
                    WHERE a.PERIOD_ID = @Periodo AND o.GERENCIA_ID = @Gerencia", cn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@Periodo", periodo);
                    da.SelectCommand.Parameters.AddWithValue("@Gerencia", gerencia);
                    da.Fill(dt);
                }
            }
            catch { }
            return dt;
        }

        public DataTable HE_DetalleConsumoGerencia(string periodo, string gerencia)
        {
            var dt = new DataTable();
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                using (var da = new SqlDataAdapter(@"
                    SELECT et.PERIOD_ID, et.Person_Id,
                           e.NOMBRE_COMPLETO AS FullName, e.CARNET AS Employee_Number,
                           ROUND(SUM(CAST(DATEDIFF(MINUTE,
                               CASE WHEN es.STATUS >= 2 THEN et.HOUR_START ELSE et.HOUR_END END,
                               et.HOUR_END) / 60.0 AS DECIMAL(10,2))), 2) AS Hours
                    FROM SBM_NI.XET_EXTRATIME et
                    INNER JOIN SBM_NI.XET_EXTRATIME_STATUS es ON es.EXTRATIME_ID = et.EXTRATIME_ID
                    INNER JOIN SBM_NI.EMP2024 e ON e.IDPERSON = et.Person_Id
                    WHERE es.Is_Active = 'Y' AND et.PERIOD_ID = @Periodo
                      AND e.GERENCIAID = @Gerencia
                    GROUP BY et.PERIOD_ID, et.Person_Id, e.NOMBRE_COMPLETO, e.CARNET", cn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@Periodo", periodo);
                    da.SelectCommand.Parameters.AddWithValue("@Gerencia", gerencia);
                    da.Fill(dt);
                }
            }
            catch { }
            return dt;
        }

        #endregion

        #region ========== Helpers auxiliares (migrados de Oracle) ==========

        private static string GetString(IDataRecord r, string name)
        {
            int i = r.GetOrdinal(name);
            return r.IsDBNull(i) ? string.Empty : Convert.ToString(r.GetValue(i));
        }

        private static DateTime GetDateTime(IDataRecord r, string name)
        {
            int i = r.GetOrdinal(name);
            return r.IsDBNull(i) ? DateTime.MinValue : Convert.ToDateTime(r.GetValue(i));
        }

        private static decimal GetDecimal(IDataRecord r, string name)
        {
            int i = r.GetOrdinal(name);
            return r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
        }

        #endregion
    }
}
