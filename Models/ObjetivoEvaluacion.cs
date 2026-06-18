using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class ObjetivoEvaluacion
    {
        public string DescripcionObjetivo { get; set; }
        public int PorcentajeCumplimientoEsperado { get; set; }
      
        public int? PorcentajeCumplimientoReal { get; set; }
        public int IdObjetivo { get; set; }
        public int IdCiclo { get; set; }

    }
}