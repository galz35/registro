using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class Formulario
    {
        public int FormularioID { get; set; }
        public string TituloFormulario { get; set; }
        public string DescripcionFormulario { get; set; }
        public DateTime FechaCreacionFormulario { get; set; }
        public bool? grafico { get; set; }
 	        public bool? estrella { get; set; }
         public List<Preguntas> Preguntas { get; set; } = new List<Preguntas>();
    }
}