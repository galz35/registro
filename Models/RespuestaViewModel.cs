using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class RespuestaViewModel
    {
        public int FormularioID { get; set; }
        public int PreguntaID { get; set; }
        public string UsuarioID { get; set; }
        public int? OpcionID { get; set; }
        public string TextoRespuesta { get; set; }
        public string PorcentajeAsignado { get; set; }
        public string PorcentajeReal { get; set; }
        public string Motivo { get; set; }
      
        public int ProcesoID { get; set; } // Necesario para asociar la evaluación
    }
}