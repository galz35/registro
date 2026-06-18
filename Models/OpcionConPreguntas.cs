using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class OpcionConPreguntas
    {
        public int OpcionID { get; set; }
        public string TextoOpcion { get; set; }
        public string ValorOpcion { get; set; }
        public List<int> PreguntaIDs { get; set; } = new List<int>();
    }
}