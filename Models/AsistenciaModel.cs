using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class AsistenciaModel
    {
        public string Empleado { get; set; }
        public DateTime Fecha { get; set; }
        public string Gerencia { get; set; }
        public string Dpto { get; set; }
        public int Adulto { get; set; }
        public int Niño { get; set; }
    }
}