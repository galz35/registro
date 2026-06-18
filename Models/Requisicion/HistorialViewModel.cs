namespace slnRhonline.Models.Requisicion
{
    public class HistorialViewModel
    {
        public int HistorialID { get; set; }
        public int RequisicionID { get; set; }
        public string EstadoAnterior { get; set; }
        public string EstadoNuevo { get; set; }
        public string Observaciones { get; set; }
        public string CarnetUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public System.DateTime FechaCambio { get; set; }
    }
}
