using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class ligamodelo
    {
        public string Nombre { get; set; }

        public string Disciplina { get; set; }

        public string fullnombre { get; set; }

        public string carnet { get; set; }

        public string rg { get; set; }

        public string sexo { get; set; }

        public string estado { get; set; }

        public string Telefono { get; set; }
        // NUEVOS CAMPOS
        public string TallaCamisa { get; set; }
        public string TallaPantalon { get; set; }
        public string TallaZapato { get; set; }
    }
}