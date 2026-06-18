namespace slnRhonline.Models.Requisicion
{
    public class RequisicionListaViewModel
    {
        public int RequisicionID { get; set; }
        public string NumeroRequisicion { get; set; }
        public string TipoRequisicion { get; set; }
        public string CarnetSolicitante { get; set; }
        public string NombreSolicitante { get; set; }
        public string NombreEmpleadoBaja { get; set; }
        public string CarnetEmpleadoBaja { get; set; }
        public string NombrePuesto { get; set; }
        public string PositionCode { get; set; }
        public string Gerencia { get; set; }
        public string SubGerencia { get; set; }
        public string Edificio { get; set; }
        public string Estado { get; set; }
        public System.DateTime FechaCreacion { get; set; }
        public System.DateTime? FechaEnvio { get; set; }
        public System.DateTime? FechaAprobacion { get; set; }
        public string MotivoBaja { get; set; }
        public string ObservacionesAnalista { get; set; }
        public string NombreAnalista { get; set; }
    }
}
