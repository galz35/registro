using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class Proceso
    {
        public int ProcesoID { get; set; }
        public string NombreProceso { get; set; }
        public List<Formulario> Formularios { get; set; } = new List<Formulario>();
        public List<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();

        public string DescripcionProceso { get; set; }
        public ConfiguracionProceso ConfiguracionProceso { get; set; }
     }
}