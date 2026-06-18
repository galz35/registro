namespace slnRhonline.Models.Requisicion
{
    public class DashboardRequisicionViewModel
    {
        public int Total { get; set; }
        public int Borradores { get; set; }
        public int Pendientes { get; set; }
        public int Aprobadas { get; set; }
        public int Devueltas { get; set; }
        public int Canceladas { get; set; }
    }

    public class DashboardVacantesViewModel
    {
        public int Pendientes { get; set; }
        public int Notificadas { get; set; }
        public int EnProceso { get; set; }
        public int Resueltas { get; set; }
        public int Descartadas { get; set; }
        public int Total { get; set; }
    }

    public class EmpleadoBusquedaViewModel
    {
        public string carnet { get; set; }
        public string nombre_completo { get; set; }
        public string correo { get; set; }
        public string cargo { get; set; }
        public string empresa { get; set; }
        public string OGERENCIA { get; set; }
        public string oSUBGERENCIA { get; set; }
        public string Nombreubicacion { get; set; }
        public string carnet_jefe1 { get; set; }
        public string nom_jefe1 { get; set; }
        public string correo_jefe1 { get; set; }
        public long? PositionId { get; set; }
        public long? BusinessUnitId { get; set; }
        public string BusinessUnitName { get; set; }
        public System.DateTime? fechabaja { get; set; }
        
        // Campos de Positions y TERMINATIONS
        public string PositionCode { get; set; }
        public string NombrePosicion { get; set; }
        public string CostCenter { get; set; }
        public string CostCenterName { get; set; }
        public string TERMINATION_DATE { get; set; }
        public string ACTION_CODE { get; set; }
        public string ACTION_MEANING { get; set; }
        public string TERMINATION_TYPE { get; set; }
    }

    public class CatalogoViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }
}
