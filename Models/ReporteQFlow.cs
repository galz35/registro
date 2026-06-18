using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class ReporteQFlow
    {
        public int Numero { get; set; }
        public string Carnet { get; set; }
        public string CarnetJefe { get; set; }
        public string PuestoActual { get; set; }
        public string GerenciaOrigen { get; set; }
        public DateTime FechaInicioPeriodoPrueba { get; set; }
        public string PuestoEnPeriodoPrueba { get; set; }
        public string GerenciaNueva { get; set; }
        public int Meses { get; set; }
    }
}