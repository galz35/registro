using System;

namespace slnRhonline.Controllers
{
    internal class MarcaGeoV2Dto
    {
        public DateTime Fecha { get; set; }
        public string TipoMarca { get; set; }
        public string carnet { get; set; }
        public long PersonId { get; set; }
        public string nombre_completo { get; set; }
        public string cargo { get; set; }
        public string oDEPARTAMENTO { get; set; }
        public string OGERENCIA { get; set; }
        public string NombreUbicacion { get; set; }
        public string Edificio { get; set; }
        public decimal? LatEdif { get; set; }
        public decimal? LonEdif { get; set; }
        public TimeSpan? Hora { get; set; }                 // si tu SP retorna time
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string deviceType { get; set; }
        public string geolocationexception { get; set; }
        public string SinMarca { get; set; }
        public string SinCoordenada { get; set; }
        public int? DistanciaMetros { get; set; }
        public string Categoria { get; set; }
    }
}