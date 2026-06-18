using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class datas
    {
        public class Formulario
        {
            public int FormularioID { get; set; }
            public string TituloFormulario { get; set; }
            public string DescripcionFormulario { get; set; }
            public DateTime FechaCreacionFormulario { get; set; }
            public bool grafico { get; set; }
            public bool estrella { get; set; }
            public List<Pregunta> Preguntas { get; set; }
        }

        public class Opcione
        {
            public int OpcionID { get; set; }
            public string TextoOpcion { get; set; }
            public string ValorOpcion { get; set; }
            public List<int> PreguntaIDs { get; set; }
        }

        public class Pregunta
        {
            public int PreguntaID { get; set; }
            public string TextoPregunta { get; set; }
            public string TipoPregunta { get; set; }
            public bool? RequierePorcentaje { get; set; }
            public bool? RequierePorcentajeReal { get; set; }
            public bool? RequiereMotivo { get; set; }
            public bool Requerido { get; set; }
        }

        public class Proceso
        {
            public int ProcesoID { get; set; }
            public string NombreProceso { get; set; }
            public List<Formulario> Formularios { get; set; }
            public List<object> Evaluaciones { get; set; }
            public string DescripcionProceso { get; set; }
            public object ConfiguracionProceso { get; set; }
        }

        public class Root
        {
            public List<Proceso> Procesos { get; set; }
            public List<Opcione> Opciones { get; set; }
        }

    }
}