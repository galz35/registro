namespace slnRhonline.Models.Compensacion
{
    public class AuditoriaViewModel
    {
        public int AuditoriaID { get; set; }
        public string Campo { get; set; }
        public string ValorAnterior { get; set; }
        public string ValorNuevo { get; set; }
        public System.DateTime FechaCambio { get; set; }
        public string CarnetUsuario { get; set; }
        public string NombreGeneraCambio { get; set; }
    }
}
