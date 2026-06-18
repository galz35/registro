using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class EvaluacionCicloViewModel
    {
        public int IdEvaluacion { get; set; }
        public int CodigoEmpleado { get; set; }
        public DateTime FechaInicio { get; set; }
        public int CantidadMeses { get; set; }
        public string EstadoEvaluacion { get; set; }
        public string colaborador { get; set; }
        public string nombrejefe { get; set; }
        public byte[] PDFComprobante { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaFinalizacion { get; set; } // Nullable, por si no tiene valor
        public string Jefe { get; set; }
        public string Cargo { get; set; }
        public string Cargo2 { get; set; }
        public string Cargo3 { get; set; }
        public string Gerencia { get; set; }
        public string Gerencia2 { get; set; }

        public int IdCiclo { get; set; }
        public string MesCiclo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string EstadoCiclo { get; set; }
        //public decimal? PorcentajeCumplimientoEsperado { get; set; } // Nullable, por si no tiene valor
        //public decimal? PorcentajeCumplimientoReal { get; set; } // Nullable, por si no tiene valor

    }
}