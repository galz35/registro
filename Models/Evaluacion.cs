using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class Evaluacion
    {
        public int EvaluacionID { get; set; }
        public string UsuarioID { get; set; }
        public int FormularioID { get; set; }
        public DateTime FechaAsignacion { get; set; }
        public string Estado { get; set; }
        public string CargoAnterior { get; set; }
        public string NuevoCargo { get; set; }
        public string JefeID { get; set; }
        public string GerenciaAnterior { get; set; }
        public string NuevaGerencia { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int ProcesoID { get; set; }
        public string EstadoEvaluacion { get; set; }
      //  public List<CicloEvaluacion> CiclosEvaluacion { get; set; } = new List<CicloEvaluacion>();

    }
}