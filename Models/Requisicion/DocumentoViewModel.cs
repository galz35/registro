namespace slnRhonline.Models.Requisicion
{
    public class DocumentoViewModel
    {
        public int DocumentoID { get; set; }
        public int RequisicionID { get; set; }
        public string TipoDocumento { get; set; }
        public string NombreArchivo { get; set; }
        public string RutaArchivo { get; set; }
        public string Extension { get; set; }
        public long? TamanioBytes { get; set; }
        public System.DateTime FechaSubida { get; set; }
        public string SubidoPor { get; set; }
        public int Version { get; set; }
        public bool Activo { get; set; }
    }
}
