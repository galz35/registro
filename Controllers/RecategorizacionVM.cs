namespace slnRhonline.Controllers
{
    public class RecategorizacionVM
    {
        public int CasoID { get; set; }
        public int TipoCasoID { get; set; }
        public int SubtipoCasoID { get; set; }
        public string Nota { get; set; } // opcional: por qué se recategorizó
    }
}