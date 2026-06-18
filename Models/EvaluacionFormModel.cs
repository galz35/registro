using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class EvaluacionFormModel
    {
        public string NombreColaborador { get; set; }
        public string PeriodoEvaluacion { get; set; }
        public string PuestoColaborador { get; set; }
        public List<string> Objetivos { get; set; }
        public List<int> Cumplimientos { get; set; }
    }
}