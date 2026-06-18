        #region Inner DTOs

        public class CasoDetalleDto
        {
            public int ID { get; set; }
            public string Titulo { get; set; }
            public string Estado { get; set; }
            public string Prioridad { get; set; }
            public DateTime? FechaCreacion { get; set; }
            public DateTime? FechaFinalizacion { get; set; }
            public string Descripcion { get; set; }
            public string NotasCierre { get; set; }

            // Solicitante
            public string NombreAutor { get; set; }
            public string CorreoAutor { get; set; }
            public string AreaAutor { get; set; }
            public string TelefonoAutor { get; set; }
            public string GerenciaAutor { get; set; }    // Nuevo: Gerencia desde Emp2024

            // Afectado
            public string NombreResponsable { get; set; }
            public string Correo { get; set; }           // Correo afectado
            public string AreaResponsable { get; set; }
            public string TelefonoResponsable { get; set; }
            public string GerenciaResponsable { get; set; } // Nuevo

            // Soporte
            public string Nombresoport { get; set; }
            public string CorreoSoport { get; set; }

            // Clasificación
            public string TipoNombre { get; set; }
            public string SubtipoNombre { get; set; }
            public int? TipoCasoID { get; set; }
            public int? SubtipoCasoID { get; set; }

            // Adjuntos (lista)
            public List<ArchivoDto> Adjuntos { get; set; }

            // Compatibilidad legacy (si usas un solo adjunto en vista)
            public string data { get; set; }
            public string NombreArchivo { get; set; }
            public string TipoArchivo { get; set; }
            public byte[] DatosArchivo { get; set; }
        }

        public class ArchivoDto
        {
            public int Id { get; set; }
            public int CasoID { get; set; }
            public string NombreArchivo { get; set; }
            public string TipoArchivo { get; set; }
            public byte[] DatosArchivo { get; set; }
            public DateTime FechaSubida { get; set; }
            public string data { get; set; } // base64 para vista
            public string UrlDescarga { get; set; } // Enlace para descargar si es PDF
        }

        public class NotaDto
        {
            public int NotaID { get; set; }
            public int CasoID { get; set; }
            public string Nota { get; set; }
            public bool EsPrivada { get; set; }
            public DateTime Fecha { get; set; }
            public string UsuarioNombre { get; set; }
            public string UsuarioID { get; set; } // carnet
            public string Role { get; set; } // 'Soporte', 'Usuario', 'Sistema'
            public List<ArchivoDto> Adjuntos { get; set; }
        }

        public class CasoView
        {
            public int ID { get; set; }
            public string Titulo { get; set; }
            public string Estado { get; set; }
            public string Prioridad { get; set; }
            public DateTime? FechaCreacion { get; set; }
            public DateTime? FechaFinalizacion { get; set; }
            public string Descripcion { get; set; }
            public string NotasCierre { get; set; }

            // Solicitante
            public string NombreAutor { get; set; }
            public string CorreoAutor { get; set; }
            public string AreaAutor { get; set; }
            public string TelefonoAutor { get; set; }
            public string GerenciaAutor { get; set; }

            // Afectado
            public string NombreResponsable { get; set; }
            public string Correo { get; set; } // El afectado
            public string AreaResponsable { get; set; }
            public string TelefonoResponsable { get; set; }
            public string GerenciaResponsable { get; set; }

            // Soporte asignado
            public string Nombresoport { get; set; }
            public string CorreoSoport { get; set; }

            // Clasificación
            public string TipoNombre { get; set; }
            public string SubtipoNombre { get; set; }
            public int? TipoCasoID { get; set; }
            public int? SubtipoCasoID { get; set; }
        }

        // DTOs auxiliares
        public class PersonaDto
        {
            public string EMPLEADO { get; set; }  // carnet
            public string NOMBRE { get; set; }
            public string EMAIL { get; set; }
            public string AREA { get; set; }
            public string CARGO { get; set; }
            public string TELEFONO { get; set; }
            public string GERENCIA { get; set; } // OGERENCIA
            public string Url { get; set; }      // Foto URL
        }

        public class NotificacionCasoDto
        {
            public int CasoID { get; set; }
            public string Titulo { get; set; }
            public string Estado { get; set; }
            public string CorreoSolicitante { get; set; }
        }

        // Estructuras para el Arbol de tipos
        public class TipoNode
        {
            public int TipoCasoID { get; set; }
            public string Nombre { get; set; }
            public List<SubNode> Subtipos { get; set; }
        }
        public class SubNode
        {
            public int SubtipoCasoID { get; set; }
            public int TipoCasoID { get; set; }
            public string Nombre { get; set; }
        }

        public class SoporteLite
        {
            public string SoporteID { get; set; }
            public string Nombre { get; set; }
            public string Email { get; set; }
        }

        // Request para asignar soporte
        public class AsignarReq
        {
            public int CasoID { get; set; }
            public string SoporteID { get; set; } // carnet o email (según tu lógica)
            public string Nota { get; set; }
        }

        public class SoporteAdminItem
        {
            public string SoporteID { get; set; }
            public string Nombre { get; set; }
            public string Email { get; set; }
            public string Area { get; set; }
            public bool Activo { get; set; }
            // Podrías devolver lista de tipos concatenada
            public string TiposAsignados { get; set; }
        }

        // Clases para JSON responses genéricos
        public class RespuestaJson<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }
        public class TipoDto
        {
            public int TipoCasoID { get; set; }
            public string Nombre { get; set; }
            public List<SubtipoDto> Subtipos { get; set; }
        }

        public class SubtipoDto
        {
            public int SubtipoCasoID { get; set; }
            public string Nombre { get; set; }
        }
        #endregion
