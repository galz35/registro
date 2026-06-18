using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class PorcentajeDTO
    {
        public string Gerencia { get; set; }
        public int TotalEmpleados { get; set; }
        public int TotalSinProgramacion { get; set; }
        public double PorcentajeSinProgramacion { get; set; }
    }
}