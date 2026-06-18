using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class Act_ActivoFijo
    {
        public int Act_ID { get; set; }
        public string Act_Codigo { get; set; }                // Código único o número de serie
        public string Act_Nombre { get; set; }                // Nombre o descripción breve
        public string Act_Descripcion { get; set; }           // Descripción detallada
        public int? Act_CategoriaID { get; set; }             // ID de la categoría
        public string Act_Estado { get; set; }                // 'Disponible', 'Asignado', 'De Baja'
        public string Act_Ubicacion { get; set; }             // Ubicación física
        public DateTime? Act_FechaAdquisicion { get; set; }   // Fecha de adquisición
        public int? Act_VidaUtil { get; set; }                // Vida útil en años
        public string Act_UsuarioAsignadoID { get; set; }     // ID del usuario asignado
        public DateTime? Act_FechaAsignacion { get; set; }    // Fecha de asignación
        public byte[] Act_Imagen { get; set; }                // Imagen del activo
        public DateTime Act_FechaCreacion { get; set; }
        public DateTime? Act_FechaActualizacion { get; set; }
        public bool Act_Eliminado { get; set; }
 

        // Propiedad adicional para pasar la imagen en Base64 (si es necesario)
        public string Act_ImagenBase64 { get; set; }
    }
}