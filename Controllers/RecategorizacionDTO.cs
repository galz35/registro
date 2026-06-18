using System;

namespace slnRhonline.Controllers
{
    public class RecategorizacionDTO
    {
        public int ID { get; set; }

        // antes
        public int TipoAnteriorID { get; set; }
        public int SubtipoAnteriorID { get; set; }
        public string TipoAnterior { get; set; }
        public string SubtipoAnterior { get; set; }

        // después
        public int TipoNuevoID { get; set; }
        public int SubtipoNuevoID { get; set; }
        public string TipoNuevo { get; set; }
        public string SubtipoNuevo { get; set; }

        // datos del caso
        public string Titulo { get; set; }
        public string Prioridad { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string NombreAutor { get; set; }
        public string CorreoAutor { get; set; }
        public string NombreResponsable { get; set; }
        public string CorreoResponsable { get; set; }
        public string Nombresoport { get; set; }   // soporte (nombre)
        public string CorreoSoporte { get; set; }  // soporte (correo)
        public string Subgerencia { get; set; }
        public string Area { get; set; }

        // utilidad correo
        public string DestinatariosCSV { get; set; } // si tu SP arma to/cc
    }
}