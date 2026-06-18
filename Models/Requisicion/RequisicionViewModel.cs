namespace slnRhonline.Models.Requisicion
{
    public class RequisicionViewModel
    {
        public int RequisicionID { get; set; }
        public string NumeroRequisicion { get; set; }
        public string TipoRequisicion { get; set; }
        public string CarnetSolicitante { get; set; }
        public string NombreSolicitante { get; set; }
        public string CorreoSolicitante { get; set; }
        public string CarnetEmpleadoBaja { get; set; }
        public string NombreEmpleadoBaja { get; set; }
        public string MotivoBaja { get; set; }
        public System.DateTime? FechaBaja { get; set; }
        public string TerminationDate { get; set; }
        public string ActionCode { get; set; }
        public string ActionMeaning { get; set; }
        public string TerminationType { get; set; }
        public long? PositionId { get; set; }
        public string PositionCode { get; set; }
        public string NombrePosicion { get; set; }
        public string NombrePuesto { get; set; }
        public string NumeroPlaza { get; set; }
        public string CodigoPosicion { get; set; }
        public string Empresa { get; set; }
        public long? BusinessUnitId { get; set; }
        public string BusinessUnitName { get; set; }
        public string TipoNomina { get; set; }
        public decimal? Sueldo { get; set; }
        public decimal? Comisiones { get; set; }
        public string TipoContrato { get; set; }
        public string HorarioLaboral { get; set; }
        public string CostCenter { get; set; }
        public string CostCenterName { get; set; }
        public string Gerencia { get; set; }
        public string SubGerencia { get; set; }
        public string Coordinacion { get; set; }
        public string Supervision { get; set; }
        public string AreaOperativa { get; set; }
        public string Edificio { get; set; }
        public string CentroCostos { get; set; }
        public string CarnetJefeInmediato { get; set; }
        public string NombreJefeInmediato { get; set; }
        // Traslado
        public string GerenciaDestino { get; set; }
        public string SubGerenciaDestino { get; set; }
        public string EdificioDestino { get; set; }
        public string PuestoDestino { get; set; }
        public string CarnetJefeDestino { get; set; }
        public string NombreJefeDestino { get; set; }
        public string MotivoTraslado { get; set; }
        public string DuracionTraslado { get; set; }
        public System.DateTime? FechaTrasladoPropuesta { get; set; }
        public string JustificacionTraslado { get; set; }
        // Control
        public string Estado { get; set; }
        public System.DateTime FechaCreacion { get; set; }
        public System.DateTime? FechaEnvio { get; set; }
        public System.DateTime? FechaRevision { get; set; }
        public System.DateTime? FechaAprobacion { get; set; }
        public string CarnetAnalista { get; set; }
        public string NombreAnalista { get; set; }
        public string ObservacionesAnalista { get; set; }
        public string ObservacionesJefe { get; set; }
    }
}
