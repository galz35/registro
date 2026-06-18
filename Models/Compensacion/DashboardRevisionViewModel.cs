namespace slnRhonline.Models.Compensacion
{
    public class DashboardRevisionViewModel
    {
        public int? PlantillaID { get; set; }
        public string CarnetGerente { get; set; }
        public string NombreGerente { get; set; }
        public string OGERENCIA { get; set; }
        public string Estado { get; set; }
        public int? TotalColaboradores { get; set; }
        public int? TotalEnPlantilla { get; set; }
        public int? TotalDiscrepancias { get; set; }
    }
}
