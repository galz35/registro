using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class RespuestaEncuesta
    {
        public string Proceso { get; set; }
        public int FormularioID { get; set; }
        public int PreguntaID { get; set; }
        public string TipoPregunta { get; set; }
        public string TextoRespuesta { get; set; }
        public int? OpcionID { get; set; }
        public decimal? Porcentaje { get; set; }
        public decimal? PorcentajeReal { get; set; }
        public string Motivo { get; set; }
    }
}