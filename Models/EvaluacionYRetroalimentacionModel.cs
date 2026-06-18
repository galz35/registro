using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class EvaluacionYRetroalimentacionModel
    {
        public string Periodo { get; set; }
        public string NombreColaborador { get; set; }
        public DateTime FechaEvaluacion { get; set; }
        public List<HabilidadEvaluacion> Habilidades { get; set; }
        public string EvaluacionGlobal { get; set; }
        public List<ObjetivoEvaluacion> Objetivos { get; set; }
        public string Fortalezas { get; set; }
        public string Debilidades { get; set; }
        public string AccionesASeguir { get; set; }
    }
}