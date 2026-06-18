using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class VacacionesPersona
    {
        public string NombreCompleto { get; set; }
        public string Carnet { get; set; }
        public string Gerencia { get; set; }
        public string correo { get; set; }
        public string PrimerNivel { get; set; }
        public string Cargo { get; set; }
        public string Comentario { get; set; }
        public string Creador { get; set; }
        public string usuarioactualizo { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaInicio { get; set; }
        public string HoraInicio { get; set; }
        public long PersonAbsenceEntryId { get; set; }
        public DateTime? FechaFin { get; set; }
        public string HoraFin { get; set; }
        public string UnidadDeMedida { get; set; }
        public double? Duracion { get; set; }
        public string DuracionFormateada { get; set; }
        public string AbsenceType { get; set; }
        public string EstadoDeDisponibilidad { get; set; }
        public DateTime? FechaDeAprobacion { get; set; }
        public DateTime? fecha_Actualizo { get; set; }
     }
}