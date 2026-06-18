namespace slnRhonline.Models.Requisicion
{
    public class VacanteViewModel
    {
        public int VacanteID { get; set; }
        public string CarnetEmpleado { get; set; }
        public string NombreEmpleado { get; set; }
        public string PuestoEmpleado { get; set; }
        public string GerenciaEmpleado { get; set; }
        public string SubGerenciaEmpleado { get; set; }
        public string EdificioEmpleado { get; set; }
        public string EmpresaEmpleado { get; set; }
        public System.DateTime FechaBaja { get; set; }
        public string MotivoBaja { get; set; }
        public long? PositionId { get; set; }
        public string PositionCode { get; set; }
        public string NombrePosicion { get; set; }
        public string CostCenter { get; set; }
        public string CostCenterName { get; set; }
        public string TerminationDate { get; set; }
        public string ActionCode { get; set; }
        public string ActionMeaning { get; set; }
        public string TerminationType { get; set; }
        public string CarnetJefe { get; set; }
        public string NombreJefe { get; set; }
        public string CorreoJefe { get; set; }
        public bool CorreoEnviado { get; set; }
        public System.DateTime? FechaCorreoEnviado { get; set; }
        public int CantidadRecordatorios { get; set; }
        public int? RequisicionID { get; set; }
        public bool TieneRequisicion { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }
        // Calculados
        public int DiasSinRequisicion { get; set; }
        public string Semaforo { get; set; }

        // Nuevos campos de estado de documentos
        public int? ReqAsociadaID { get; set; }
        public string ReqEstado { get; set; }
        public string NumeroRequisicion { get; set; }
        public int TieneDocPerfil { get; set; }
        public int TieneDocDescriptor { get; set; }
    }
}
