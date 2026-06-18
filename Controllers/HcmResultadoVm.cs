namespace slnRhonline.Controllers
{
    internal class HcmResultadoVm
    {
        public string Carnet { get; set; }
        public string TipoRelacion { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }   // OK / EXISTE / ERROR
        public string Mensaje { get; set; }
        public string PersonNumber { get; set; }
    }
}