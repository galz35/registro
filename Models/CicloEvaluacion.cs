using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class CicloEvaluacion
    {
        public int EvaluacionID { get; set; } // Relacionado con la evaluación
        public string NombreCiclo { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; }
        public string EstadoCiclo { get; set; }
    }
}