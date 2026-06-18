using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class Evaluacion2
    {
        public string UsuarioID { get; set; }
        public DateTime FechaAsignacion { get; set; }
        public string Estado { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string CargoAnterior { get; set; }
        public string NuevoCargo { get; set; }
        public string JefeID { get; set; }
        public string GerenciaAnterior { get; set; }
        public string NuevaGerencia { get; set; }
        public string IA { get; set; }
        public int ProcesoID { get; set; }
        public int EstadoID { get; set; }

        public string EstadoEvaluacion { get; set; }
          public int CantidadCiclosPlaneados { get; set; }
    }
}