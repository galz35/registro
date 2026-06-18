using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class RegistrarPresupuestosRequest
    {
        public List<PresupuestoInput> Presupuestos { get; set; }
        public string Justificacion { get; set; }
        public string Periodo { get; set; }
    }
}