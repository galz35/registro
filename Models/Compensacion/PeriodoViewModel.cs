namespace slnRhonline.Models.Compensacion
{
    public class PeriodoViewModel
    {
        public int PeriodoID { get; set; }
        public string NombrePeriodo { get; set; }
        public byte Mes { get; set; }
        public short Anio { get; set; }
        public string Estado { get; set; } 
        public System.DateTime FechaCreacion { get; set; }
        public System.DateTime? FechaCierre { get; set; }
        public System.DateTime? FechaInicio { get; set; }
        public System.DateTime? FechaFin { get; set; }
        
        public int TotalGerencias { get; set; }
        public int Enviadas { get; set; }
        public int Aprobadas { get; set; }
        public int Borradores { get; set; }
        public int Devueltas { get; set; }
        public int TotalEmpleados { get; set; }
        public int Comisionan { get; set; }
        public int NoComisionan { get; set; }
        public int SinDefinir { get; set; }
    }
}