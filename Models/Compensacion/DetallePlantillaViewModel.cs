namespace slnRhonline.Models.Compensacion
{
    public class DetallePlantillaViewModel
    {
        public int DetalleID { get; set; }
        public int PlantillaID { get; set; }
        
        public string CarnetEmpleado { get; set; }
        public string NombreCompleto { get; set; }
        public string Cargo_SIGHO { get; set; }
        public string OGERENCIA_SIGHO { get; set; }
        public string Ubicacion_SIGHO { get; set; }
        public string CarnetJefe_SIGHO { get; set; }
        public string Jefe_SIGHO { get; set; }
        public string Empresa_SIGHO { get; set; }
        public string Departamento_SIGHO { get; set; }
        public System.DateTime? FechaIngreso_SIGHO { get; set; }
        public string EsPrueba_SIGHO { get; set; }
        
        public string Cargo_Actual { get; set; }
        public string Ubicacion_Actual { get; set; }
        public string Jefe_Actual { get; set; }

        public string OSUBGERENCIA_SIGHO { get; set; }
        public string Area_SIGHO { get; set; }
        public string AplicaEmpleado { get; set; }
        public string AplicaComision { get; set; }
        public string Comisiona { get; set; }
        public string Cargo_Reportado { get; set; }
        public string Ubicacion_Reportada { get; set; }
        public string Jefe_Reportado { get; set; }
        public string Departamento_Reportado { get; set; }
        public string Observacion { get; set; }
        
        public System.DateTime? FechaModificacion { get; set; }
        public string CarnetModifica { get; set; }

        public bool DifiereCargo { get; set; }
        public bool DiefereUbicacion { get; set; }
        public bool DifiereJefe { get; set; }
        public bool DifiereDepartamento { get; set; }
        
        // Campos de cabecera (Join con Plantilla)
        public string OGERENCIA { get; set; }
        public string Estado { get; set; }
        public string EstadoPlantilla { get; set; }
        public string NombreGerente { get; set; }
        public string CarnetGerente { get; set; }
        public string Comisiona_Original { get; set; }
        public int EsDiscrepancia { get; set; }
        
        // Propiedades de Justificacion de Cambio
        public string JustMotivo { get; set; }
        public string JustReposicion { get; set; }
        public string JustTiempo { get; set; }
        
        // Propiedades de Evidencia
        public bool HasEvidencia { get; set; }
        public int TotalEvidencias { get; set; }
    }
}
