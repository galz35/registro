using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class Respuesta
    {
        public int FormularioID { get; set; }
        public int FormularioPreguntaID { get; set; }
        public string UsuarioID { get; set; }
        public int? CicloEvaluacionID { get; set; } // Relacionado con la Evaluación
        public string TextoRespuesta { get; set; }
        public int? OpcionID { get; set; }
        public decimal? PorcentajeAsignado { get; set; }
        public decimal? PorcentajeReal { get; set; }
        public string Motivo { get; set; }
    }
}