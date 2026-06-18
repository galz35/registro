using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class EmpleadoEnPrueba
    {
        public int IdHcm { get; set; }
        public string Carnet { get; set; }
        public string NombreCompleto { get; set; }
        public string Cargo { get; set; }
        public string Correo { get; set; }
        public string Jefe { get; set; }
        public string CorreoJefe { get; set; }
        public string Gerencia { get; set; }
        public string Area { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime? FechaBaja { get; set; } // Nullable as the field might not always have a value
        public DateTime FechaAsignacion { get; set; }
        public string ActionCode { get; set; }
        public int DiaPrueba { get; set; }
    }
}