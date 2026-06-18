using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class vacacionprogramada
    {
        public string Carnet { get; set; }
        public string NombreCompleto { get; set; }
        public string Gerencia { get; set; }
        public string SUBGerencia { get; set; }
        public string Area { get; set; }
 
 
 
        public decimal Acumulado { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
         public int DiasParaInicio { get; set; }
        public int DiasVacaciones { get; set; }
         public decimal? AcumuladaPasiva { get; set; }
         public decimal? TotalAcumulado { get; set; }
         public decimal? SaldoFinal { get; set; }
         public decimal? PorcentajeReduccion { get; set; }
         public decimal? PorcentajeCumplido { get; set; }
        
    }
}