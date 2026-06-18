using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class Act_HistorialAsignacion
    {
        public int Act_HistorialID { get; set; }
        public int Act_ActivoFijoID { get; set; }
        public string Act_UsuarioID { get; set; }
        public DateTime Act_FechaAsignacion { get; set; }
        public DateTime? Act_FechaDevolucion { get; set; }
        public string Act_Comentarios { get; set; }

        // Propiedades de navegación
        public Act_ActivoFijo Act_ActivoFijo { get; set; }
 
        // Campo adicional para mostrar el nombre del usuario en el historial
        public string Act_UsuarioNombre { get; set; }
    }
}