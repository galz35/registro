using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class CasoView
    { // Campos de la tabla Caso
        public int ID { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaEliminacion { get; set; }
        public bool Eliminado { get; set; }
        public string UsuarioID { get; set; }
        public DateTime? FechaCreacionCaso { get; set; }
        public string Descripcion { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string Prioridad { get; set; }
        public string TipoCaso { get; set; }

        public int TipoCasoID { get; set; }        // ← agregado (ya lo devuelve el SP)
        public int SubtipoCasoID { get; set; }     // ← agregado (ya lo devuelve el SP)

        public DateTime? FechaAtencion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string NotasCierre { get; set; }
        public string SoporteID { get; set; }
        public string Correo { get; set; }
        public string CorreoResponsable { get; set; }
        public string ParaQuien { get; set; }
        public string NombreArchivo { get; set; }
        public string TipoArchivo { get; set; }
        public string data { get; set; }

        // === NUEVOS CAMPOS OPCIONALES (Higiene y Seguridad - Inconveniente en edificio) ===
        public string Departamento { get; set; }        // ← nuevo
        public string Edificio { get; set; }            // ← nuevo (NombreUbicacion)
        public int? CantidadAfectados { get; set; }     // ← nuevo
        public int? DiasCondicion { get; set; }         // ← nuevo

        // Campos del autor del caso
        public string CorreoAutor { get; set; }
        public string NombreAutor { get; set; }
        public string CargoAutor { get; set; }
        public string AreaAutor { get; set; }
        public string TelefonoAutor { get; set; }
        public string GerenciaAutor { get; set; }

        // Campos del responsable del caso
        public string NombreResponsable { get; set; }
        public string CargoResponsable { get; set; }
        public string AreaResponsable { get; set; }
        public string TelefonoResponsable { get; set; }
        public string carnetResponsable { get; set; }

        public string Nombresoport { get; set; }
        public string Cargosoport { get; set; }
        public string Areasoport { get; set; }
        public string Telefonosoport { get; set; }
        public string CorreoSoport { get; set; }        // ← agregado (tu SP ya lo selecciona)

        // Campos calculados
        public int? DiasAbierto { get; set; }
        public int DiasDesdeUltimaActualizacion { get; set; }
    }
}