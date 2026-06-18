using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class CitaResult
    {
        public string IdUnidad { get; set; }

        public DateTime HoraCita { get; set; }
        public int KilometrajeActual { get; set; }
        public int Dia { get; set; }
        public string NombreEmpleado { get; set; }
    }
}