namespace slnRhonline.Models.Compensacion
{
    public class PlantillaViewModel
    {
        public int PlantillaID { get; set; }
        public int PeriodoID { get; set; }
        public int TipoPlantillaID { get; set; }
        public string NombrePeriodo { get; set; }
        public byte Mes { get; set; }
        public short Anio { get; set; }
        public string CarnetGerente { get; set; }
        public string NombreGerente { get; set; }
        public string OGERENCIA { get; set; }
        public string Estado { get; set; }
        public System.DateTime FechaCreacion { get; set; }
        public System.DateTime? FechaEnvio { get; set; }
        public System.DateTime? FechaAprobacion { get; set; }
        public string ComentarioDevolucion { get; set; }
        public string CarnetValidador { get; set; }

        public int TotalEmpleados { get; set; }
        public int TotalComisiona { get; set; }
        public int TotalNoComisiona { get; set; }
        public int TotalSinDefinir { get; set; }
        public int TotalEnPrueba { get; set; }
        public int TotalConDiferencias { get; set; }
    }
}