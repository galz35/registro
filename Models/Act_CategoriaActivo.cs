using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class Act_CategoriaActivo
    {
        public int Act_CategoriaID { get; set; }
        public string Act_Nombre { get; set; }
        public string Act_Descripcion { get; set; }

        // Propiedad de navegación
        public List<Act_ActivoFijo> ActivosFijos { get; set; }
    }
}