using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class ligadetallemodelo
    {
        public int iddliga { get; set; }

        public int? idliga { get; set; }

        public string carnet { get; set; }

        public string sexo { get; set; }

        public string gerencia { get; set; }

        public string cargo { get; set; }

        public DateTime? fecha { get; set; }

        public string estado { get; set; }

        public string year { get; set; }

        public string area { get; set; }

        public string tipo { get; set; }

        public string nombre { get; set; }

        public string telefono { get; set; }
        public string TallaCamisa { get; set; }
        public string TallaPantalon { get; set; }
        public string TallaZapato { get; set; }
    }
}