using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class Preguntas
    {
        public int PreguntaID { get; set; }

        public string TextoPregunta { get; set; }

        public string TipoPregunta { get; set; }

        public bool? RequierePorcentaje { get; set; }
        public bool? RequierePorcentajeReal { get; set; }
        public bool? RequiereMotivo { get; set; }
        public bool? Requerido { get; set; }
     }
}