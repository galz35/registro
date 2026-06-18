using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class Archivo
    {
        public int ID { get; set; }  // Identificador único del archivo
        public int CasoID { get; set; }  // ID del caso al que está relacionado el archivo
        public string NombreArchivo { get; set; }  // Nombre del archivo
        public string TipoArchivo { get; set; }  // Tipo de archivo (e.g., imagen/webp)
        public byte[] DatosArchivo { get; set; }  // Datos binarios del archivo
        public DateTime FechaSubida { get; set; }  // Fecha en la que se subió el archivo

    }
}