using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Models
{
    public class Caso
    {
        public int ID { get; set; }  // Identificador único del caso
        public int TipoCasoID { get; set; }  // Identificador único del caso
        public int SubtipoCasoID { get; set; }  // Identificador único del caso
        public DateTime FechaCreacion { get; set; }  // Fecha de creación del caso
        public DateTime? FechaActualizacion { get; set; }  // Fecha de la última actualización del caso
        public DateTime? FechaEliminacion { get; set; }  // Fecha en la que se eliminó el caso
        public bool Eliminado { get; set; }  // Indicador de si el caso está eliminado
        public string UsuarioID { get; set; }  // ID del usuario que creó el caso
        public string Correo { get; set; }  // ID del usuario que creó el caso
         public string Descripcion { get; set; }  // Descripción detallada del caso
        public string Titulo { get; set; }  // Título del caso
        public string Estado { get; set; }  // Estado del caso (e.g., Abierto, En Proceso, Cerrado)
        public string Prioridad { get; set; }  // Prioridad del caso (e.g., Alta, Media, Baja)
        public string TipoCaso { get; set; }  // Tipo de caso (e.g., RHOnline, Sigho, Otros)
        public DateTime? FechaAtencion { get; set; }  // Fecha en la que el soporte comenzó a atender el caso
        public DateTime? FechaFinalizacion { get; set; }  // Fecha en la que se finalizó el caso
        public string NotasCierre { get; set; }  // Notas de cierre para el caso
        public int? SoporteID { get; set; }  // ID del técnico de soporte asignado
        public byte[] Archivos { get; set; }  // Datos binarios del archivo
        public string ParaQuien { get; set; }
        public string NombreArchivo { get; set; }  // Nombre del archivo
        public string TipoArchivo { get; set; }  // Tipo de archivo (e.g., imagen/webp)
        public string data { get; set; }  // Tipo de archivo (e.g., imagen/webp)
        public byte[] DatosArchivo { get; set; }  // Datos binarios del archivo
        public List<Archivo> Archivo { get; set; }
        public int? TiempoAtencionMinutos { get; set; }
        public string TiempoAtencion { get; set; }

    }
}