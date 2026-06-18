using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Entities;
using Entities.Class;
using Entities.ViewModels;
using Newtonsoft.Json;
using slnRhonline.Models;
using ExtraTime = Entities.ExtraTime;
using Expenses = Entities.Expenses;
using ExpenseDetail = Entities.ExpenseDetail;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller API para Horas Extras y Viáticos - SQL Server
    /// Reemplaza las llamadas WCF/Oracle por acceso directo a SQL Server
    /// </summary>
    [RoutePrefix("api/v2")]
    public class ExtraTimeExpensesSqlController : ApiController
    {
        private readonly dbExtraTimeSql _extraTime = new dbExtraTimeSql();
        private readonly dbExpensesSql _expenses = new dbExpensesSql();

        // ================================================================
        // HORAS EXTRAS
        // ================================================================

        #region ExtraTime - CRUD

        [HttpPost, Route("extratime/add")]
        public IHttpActionResult ExtraTimeAdd([FromBody] ExtraTime record, [FromUri] long userId)
        {
            var result = _extraTime.Add(record, userId);
            return Ok(new { response = result });
        }

        [HttpPost, Route("extratime/edit")]
        public IHttpActionResult ExtraTimeEdit([FromBody] ExtraTime record, [FromUri] long userId, [FromUri] string justify = "")
        {
            var result = _extraTime.Edit(record, userId, justify);
            return Ok(new { response = result });
        }

        [HttpPost, Route("extratime/changestate")]
        public IHttpActionResult ExtraTimeChangeState([FromUri] int id, [FromUri] int state, [FromUri] long userId, [FromUri] string justify = "")
        {
            var result = _extraTime.ChangeState(id, state, userId, justify);
            return Ok(new { response = result });
        }

        [HttpPost, Route("extratime/authorize")]
        public IHttpActionResult ExtraTimeAuthorize([FromUri] int idHoraExtra, [FromBody] DateTime horaInicio, [FromBody] DateTime horaFin,
                                                     [FromUri] int idEstado, [FromUri] long usuario, [FromUri] string justificacion = "", [FromUri] string tipo = "E")
        {
            var result = _extraTime.HE_Autoriza(idHoraExtra, horaInicio, horaFin, idEstado, usuario, justificacion, tipo);
            return Ok(new { response = result });
        }

        #endregion

        #region ExtraTime - Consultas

        [HttpGet, Route("extratime/byemployee")]
        public IHttpActionResult ExtraTimeGetByEmployee([FromUri] string employeeId, [FromUri] string dateStart, [FromUri] string dateEnd)
        {
            var list = _extraTime.GetListExtraTime(dateStart, dateEnd, employeeId, -10);
            return Ok(list);
        }

        [HttpGet, Route("extratime/{id:int}")]
        public IHttpActionResult GetExtraTimeById(int id)
        {
            var list = _extraTime.GetExtraTimeById(id);
            return Ok(list);
        }

        [HttpGet, Route("extratime/consult")]
        public IHttpActionResult ExtraTimeConsult([FromUri] long personId, [FromUri] string dateStart, [FromUri] string dateEnd)
        {
            var list = _extraTime.GetListExtraTimeConsult(personId, dateStart, dateEnd);
            return Ok(list);
        }

        [HttpGet, Route("extratime/byboss")]
        public IHttpActionResult ExtraTimeGetByBoss([FromUri] string bossId, [FromUri] string dateStart, [FromUri] string dateEnd, [FromUri] int state)
        {
            var list = _extraTime.GetListExtraTimeByBoss(dateStart, dateEnd, bossId, state);
            return Ok(list);
        }

        [HttpGet, Route("extratime/allstates")]
        public IHttpActionResult ExtraTimeGetAllStates([FromUri] string bossId, [FromUri] string dateStart, [FromUri] string dateEnd)
        {
            var list = _extraTime.GetListExtraTimeAllState(dateStart, dateEnd, bossId);
            return Ok(list);
        }

        [HttpGet, Route("extratime/todo")]
        public IHttpActionResult GetTodoHoraExtra([FromUri] string carnet, [FromUri] string periodo)
        {
            var result = _extraTime.gettodohoraextra(carnet, periodo);
            return Ok(result);
        }

        #endregion

        #region ExtraTime - Períodos y Auxiliares

        [HttpGet, Route("extratime/periods/active")]
        public IHttpActionResult ExtraTimePeriods()
        {
            return Ok(_extraTime.ActiveDate());
        }

        [HttpGet, Route("extratime/periods/bydate")]
        public IHttpActionResult GetExtraTimePeriod([FromUri] string executionDate)
        {
            return Ok(_extraTime.GetActivePeriod(executionDate));
        }

        [HttpGet, Route("extratime/periods/dashboard")]
        public IHttpActionResult GetPeriodDashboard()
        {
            return Ok(_extraTime.GetPeriodDashboard());
        }

        [HttpGet, Route("extratime/personid/{id:int}")]
        public IHttpActionResult GetPersonId(int id)
        {
            return Ok(_extraTime.GetPersonId(id));
        }

        [HttpGet, Route("extratime/periodid/{id:int}")]
        public IHttpActionResult GetPeriodId(int id)
        {
            return Ok(_extraTime.GetPeriodId(id));
        }

        [HttpGet, Route("extratime/organizationid/{personId:long}")]
        public IHttpActionResult GetOrganizationId(long personId)
        {
            return Ok(_extraTime.GetOrganizationId(personId));
        }

        [HttpGet, Route("extratime/organizationid2/{carnet}")]
        public IHttpActionResult GetOrganizationId2(string carnet)
        {
            return Ok(_extraTime.GetOrganizationId2(carnet));
        }

        [HttpGet, Route("extratime/reasons")]
        public IHttpActionResult GetReasons()
        {
            return Ok(_extraTime.GetReasonsList());
        }

        [HttpGet, Route("extratime/areas/{bossId}")]
        public IHttpActionResult GetAreas(string bossId)
        {
            return Ok(_extraTime.GetAreasList(bossId));
        }

        #endregion

        #region ExtraTime - Reportes

        [HttpGet, Route("extratime/executed/{personId:long}")]
        public IHttpActionResult GetExecuted(long personId)
        {
            return Ok(_extraTime.GetListExtraTimeExecuted(personId));
        }

        [HttpGet, Route("extratime/report")]
        public IHttpActionResult GetReport([FromUri] long personId, [FromUri] string startDate, [FromUri] string endDate)
        {
            return Ok(_extraTime.GetListExtraTimeReport(personId, startDate, endDate));
        }

        [HttpGet, Route("extratime/report/detail")]
        public IHttpActionResult GetDetailReport([FromUri] long personId, [FromUri] string startDate, [FromUri] string endDate)
        {
            return Ok(_extraTime.GetDetailExtraTimeReport(personId, startDate, endDate));
        }

        [HttpGet, Route("extratime/historic")]
        public IHttpActionResult GetHistoric([FromUri] long organizationId, [FromUri] string startDate, [FromUri] string endDate)
        {
            return Ok(_extraTime.GetListExtraTimeHistoric(organizationId, startDate, endDate));
        }

        [HttpGet, Route("extratime/detail/all")]
        public IHttpActionResult GetAllDetail([FromUri] string bossId, [FromUri] string startDate, [FromUri] string endDate)
        {
            return Ok(_extraTime.GetAllDetailExtraTime(bossId, startDate, endDate));
        }

        [HttpGet, Route("extratime/budget")]
        public IHttpActionResult GetBudgetVsExecuted([FromUri] long bossId)
        {
            return Ok(_extraTime.GetExtraTimeBudgetVsExecuted(bossId));
        }

        [HttpGet, Route("extratime/executed/boss/{bossId:long}")]
        public IHttpActionResult GetExtraTimeExecuted(long bossId)
        {
            return Ok(_extraTime.GetExtraTimeExecuted(bossId));
        }

        [HttpGet, Route("extratime/nomina/{fecha}")]
        public IHttpActionResult Nomina(string fecha)
        {
            return Ok(_expenses.Nomina(fecha));
        }

        #endregion

        // ================================================================
        // VIÁTICOS (EXPENSES)
        // ================================================================

        #region Expenses - CRUD

        [HttpPost, Route("expenses/insert")]
        public IHttpActionResult InsertExpense([FromBody] viatico record)
        {
            return Ok(new { response = _expenses.InsertExpense(record) });
        }

        [HttpPost, Route("expenses/insert2")]
        public IHttpActionResult InsertExpense2([FromBody] Expenses record, [FromUri] long registerPersonId)
        {
            return Ok(new { response = _expenses.InsertExpense2(record, registerPersonId) });
        }

        [HttpPost, Route("expenses/inserthcm")]
        public IHttpActionResult InsertExpenseHCM([FromBody] Expenses record, [FromUri] long registerPersonId)
        {
            return Ok(new { response = _expenses.InsertExpenseHCM(record, registerPersonId) });
        }

        [HttpPut, Route("expenses/update")]
        public IHttpActionResult UpdateExpense([FromBody] Expenses record)
        {
            return Ok(new { response = _expenses.UpdateExpense(record) });
        }

        [HttpDelete, Route("expenses/delete/{expenseId:int}")]
        public IHttpActionResult DeleteExpense(int expenseId, [FromUri] long registerPersonId)
        {
            return Ok(new { response = _expenses.DeleteExpense(expenseId, registerPersonId) });
        }

        [HttpPost, Route("expenses/changestate")]
        public IHttpActionResult ChangeStateExpense([FromUri] int expenseId, [FromUri] int statusId, [FromUri] long registerPersonId)
        {
            return Ok(new { response = _expenses.ChangeStateExpense(expenseId, statusId, registerPersonId) });
        }

        [HttpPost, Route("expenses/changestate/list")]
        public IHttpActionResult ChangeStateExpenseList([FromUri] string keys, [FromUri] int statusId, [FromUri] long registerPersonId)
        {
            return Ok(new { response = _expenses.ChangeStateExpense2(keys, statusId, registerPersonId) });
        }

        #endregion

        #region Expenses - Archivos y Rendición

        [HttpPost, Route("expenses/yieldfile")]
        public IHttpActionResult UpdateYieldFile([FromBody] Expenses record)
        {
            return Ok(new { response = _expenses.UpdateYieldFile(record) });
        }

        [HttpPost, Route("expenses/depositfile")]
        public IHttpActionResult UpdateDepositFile([FromBody] Expenses record)
        {
            return Ok(new { response = _expenses.UpdateDepositFile(record) });
        }

        [HttpPost, Route("expenses/yield")]
        public IHttpActionResult UpdateYield([FromBody] Expenses record)
        {
            return Ok(new { response = _expenses.UpdateYield(record) });
        }

        #endregion

        #region Expenses - Detalle

        [HttpPost, Route("expenses/detail/insert")]
        public IHttpActionResult InsertExpenseDetail([FromBody] ExpenseDetail record)
        {
            return Ok(new { response = _expenses.InsertExpenseDetail(record) });
        }

        [HttpPut, Route("expenses/detail/update")]
        public IHttpActionResult UpdateExpenseDetail([FromBody] ExpenseDetail record)
        {
            return Ok(new { response = _expenses.UpdateExpenseDetail(record) });
        }

        [HttpPut, Route("expenses/detail/returnamount")]
        public IHttpActionResult UpdateReturnAmount([FromBody] ExpenseDetail record)
        {
            return Ok(new { response = _expenses.UpdateReturnAmount(record) });
        }

        [HttpDelete, Route("expenses/detail/delete/{id:int}")]
        public IHttpActionResult DeleteExpenseDetail(int id, [FromUri] string deleteNotes = "")
        {
            return Ok(new { response = _expenses.DeleteExpenseDetail(id, deleteNotes) });
        }

        [HttpDelete, Route("expenses/detail/deleteall/{expenseId:int}")]
        public IHttpActionResult DeleteAllExpenseDetail(int expenseId)
        {
            return Ok(new { response = _expenses.DeleteAllExpenseDetail(expenseId) });
        }

        #endregion

        #region Expenses - Consultas

        [HttpGet, Route("expenses/{id:int}")]
        public IHttpActionResult GetExpense(int id)
        {
            return Ok(_expenses.GetAllExpenses(id));
        }

        [HttpGet, Route("expenses/details")]
        public IHttpActionResult GetAllExpenseDetails()
        {
            return Ok(_expenses.GetAllExpenseDetails());
        }

        [HttpGet, Route("expenses/details/{expenseId:int}")]
        public IHttpActionResult GetExpenseDetails(int expenseId)
        {
            return Ok(_expenses.GetAllExpenseDetailById(expenseId));
        }

        [HttpGet, Route("expenses/bycarnet/{carnet}")]
        public IHttpActionResult GetExpenseByCarnet(string carnet)
        {
            return Ok(_expenses.GetExpenseDetailViewByPersonId(carnet));
        }

        [HttpGet, Route("expenses/status/{expenseId:int}")]
        public IHttpActionResult GetExpenseStatus(int expenseId)
        {
            return Ok(_expenses.GetExpenseStatusById(expenseId));
        }

        [HttpGet, Route("expenses/authorize")]
        public IHttpActionResult GetExpensesForAuthorize([FromUri] long bossId, [FromUri] int status)
        {
            return Ok(_expenses.GetAllExpensesForAuthorize(bossId, status));
        }

        [HttpGet, Route("expenses/faltarendir")]
        public IHttpActionResult GetFaltaRendir()
        {
            return Ok(_expenses.ObtenerTodosfaltarendir());
        }

        [HttpGet, Route("expenses/faltarendir/{carnet}")]
        public IHttpActionResult GetFaltaRendirCarnet(string carnet)
        {
            return Ok(_expenses.ObtenerTodosfaltarendirCarnet(carnet));
        }

        #endregion

        #region Expenses - Períodos

        [HttpPost, Route("expenses/periods/insert")]
        public IHttpActionResult PeriodInsert([FromUri] int expensePeriodId, [FromUri] DateTime? startDate, [FromUri] DateTime? endDate,
                                              [FromUri] DateTime? paidDate, [FromUri] string notes, [FromUri] int? classId,
                                              [FromUri] string status, [FromUri] DateTime? lastDate, [FromUri] string periodId,
                                              [FromUri] DateTime? yieldDate, [FromUri] int? managementId)
        {
            var ok = _expenses.experiodInsert(expensePeriodId, startDate, endDate, paidDate, notes, classId, status, lastDate, periodId, yieldDate, managementId);
            return Ok(new { success = ok });
        }

        [HttpPut, Route("expenses/periods/update")]
        public IHttpActionResult PeriodUpdate([FromUri] int expensePeriodId, [FromUri] DateTime? startDate, [FromUri] DateTime? endDate,
                                              [FromUri] DateTime? paidDate, [FromUri] string notes, [FromUri] int? classId,
                                              [FromUri] string status, [FromUri] DateTime? lastDate, [FromUri] string periodId,
                                              [FromUri] DateTime? yieldDate)
        {
            var ok = _expenses.experiodUpdate(expensePeriodId, startDate, endDate, paidDate, notes, classId, status, lastDate, periodId, yieldDate);
            return Ok(new { success = ok });
        }

        [HttpGet, Route("expenses/periods")]
        public IHttpActionResult PeriodGetAll()
        {
            var dt = _expenses.experiodGetAll();
            return Ok(JsonConvert.SerializeObject(dt));
        }

        [HttpGet, Route("expenses/periodsx/{id:long}")]
        public IHttpActionResult GetExpensesx(long id)
        {
            return Ok(_expenses.GetAllExpensesx(id));
        }

        #endregion

        #region Reportes HE

        [HttpGet, Route("expenses/asignacion")]
        public IHttpActionResult HE_AsignacionDetalleGerencia([FromUri] string periodo, [FromUri] string gerencia)
        {
            var dt = _expenses.HE_AsignacionDetalleGerencia(periodo, gerencia);
            return Ok(JsonConvert.SerializeObject(dt));
        }

        [HttpGet, Route("expenses/consumo")]
        public IHttpActionResult HE_DetalleConsumoGerencia([FromUri] string periodo, [FromUri] string gerencia)
        {
            var dt = _expenses.HE_DetalleConsumoGerencia(periodo, gerencia);
            return Ok(JsonConvert.SerializeObject(dt));
        }

        #endregion
    }
}
