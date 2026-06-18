using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class DenegarInspeccionModel
    {
        public int idInspeccion { get; set; }
        public string comentario { get; set; }
        public string IdUnidad { get; set; }
        public string Correo { get; set; }
        public string Usuario { get; set; }
    }
}