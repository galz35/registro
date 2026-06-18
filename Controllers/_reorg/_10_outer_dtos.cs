    #region Outer DTOs
    public class KpiExportRow
    {
        public int ID { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaAtencion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
        public string SoporteNombre { get; set; }
        public string SoporteEmail { get; set; }
        public string GerenciaAfectado { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string NotasCierre { get; set; }
    }
    public class SoporteUpsertReq
    {
        public string SoporteID { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public List<PermisoLite> Permisos { get; set; } = new List<PermisoLite>();
    }
    public class PermisoLite
    {
        public int TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }
    }

    public class SoporteListadoDto
    {
        public string SoporteID { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaUpd { get; set; }
    }

    public class SoportePermisoDto
    {
        public int TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
    }
    public class CasoDetalle
    {
        public int ID { get; set; }
        public DateTime? FechaCreacion { get; set; }
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
        public DateTime? FechaAtencion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string NotasCierre { get; set; }
        public string SoporteID { get; set; }   // carnet
        public string Correo { get; set; }

        public string CorreoAutor { get; set; }
        public string NombreAutor { get; set; }
        public string CargoAutor { get; set; }
        public string AreaAutor { get; set; }
        public string TelefonoAutor { get; set; }
        public string GerenciaAutor { get; set; }

        public string NombreResponsable { get; set; }
        public string CargoResponsable { get; set; }
        public string AreaResponsable { get; set; }
        public string TelefonoResponsable { get; set; }

        public string Nombresoport { get; set; }
        public string Cargosoport { get; set; }
        public string Areasoport { get; set; }
        public string Telefonosoport { get; set; }
        public string CorreoSoport { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }

        // Campos para adjunto (se llenan en el controller)
        public byte[] DatosArchivo { get; set; }
        public string TipoArchivo { get; set; }
        public string NombreArchivo { get; set; }
        public string data { get; set; } // base64 para la vista
    }
    public class SoporteVm
    {
        public string SoporteID { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public List<SoportePermisoDto> Permisos { get; set; } = new List<SoportePermisoDto>();
    }
    // Request para guardar

    public class TipoItem { public int TipoCasoID { get; set; } public string Nombre { get; set; } }
    public class SubtipoItem { public int SubtipoCasoID { get; set; } public int TipoCasoID { get; set; } public string Nombre { get; set; } }
    public class EmpleadoItem { public string Correo { get; set; } public string Nombre { get; set; } public string Area { get; set; } }

    public class SoportePermisoItem { public int TipoCasoID { get; set; } public int? SubtipoCasoID { get; set; } }
    public class SoporteUpsertRequest
    {
        public string SoporteID { get; set; }   // sugerido: correo
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public List<SoportePermisoItem> Permisos { get; set; }
    }
    // DTOs (colócalos en carpeta Models o dentro del controller)
    public class TipoCasoDto
    {
        public int TipoCasoID { get; set; }
        public string Nombre { get; set; }
    }
    public sealed class SoporteResolucion
    {
        // ← Deben coincidir con los nombres de columnas que retorna el SP
        public string SoporteID { get; set; }  // carnet o id

        public string Correo { get; set; }        // correo del soporte (SoporteID lógico)
        public string Carnet { get; set; }        // identificador interno (para Caso.SoporteID)
        public bool EsAdmin { get; set; }       // 1/0 en SQL → bool en C#
        public bool Activo { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }

        // Opcional, por si lo devuelves también:
        // public string SoporteID { get; set; }  // si decides devolver además el mismo correo
    }
    public sealed class CasoListadoDto
    {
        // Caso
        public int ID { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string Prioridad { get; set; }
        public DateTime? FechaCreacion { get; set; }

        public string UsuarioID { get; set; }      // carnet solicitante
        public string Correo { get; set; }         // correo afectado
        public string SoporteID { get; set; }      // carnet soporte (si existe)

        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
        public int? TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }

        // ► Enriquecidos (joins a emp2024)
        public string NombreAutor { get; set; }
        public string CargoAutor { get; set; }
        public string AreaAutor { get; set; }
        public string TelefonoAutor { get; set; }

        public string NombreResponsable { get; set; }
        public string CargoResponsable { get; set; }
        public string AreaResponsable { get; set; }
        public string TelefonoResponsable { get; set; }

        public string Nombresoport { get; set; }
        public string Cargosoport { get; set; }
        public string Areasoport { get; set; }
        public string Telefonosoport { get; set; }
        public string CorreoSoport { get; set; }
    }
    public sealed class CasoRow
    {
        public int ID { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string Prioridad { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string UsuarioID { get; set; }
        public string Correo { get; set; }
        public string SUBGERENTE { get; set; }
        public string SoporteID { get; set; }         // CARNET del soporte asignado
        public int? TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
        public string NombreAutor { get; set; }
        public string NombreResponsable { get; set; }
        public string Nombresoport { get; set; }
        public string Descripcion { get; set; }
        public int? TiempoAtencionMinutos { get; set; }   // para métricas / promedio
        public string TiempoAtencion { get; set; }        // para mostrar "2h 13m"
        public DateTime? FechaFinalizacion { get; set; }  // ya existe en tu DB

    }

    public sealed class KpiDto
    {
        public int Total { get; set; }
        public int Pendiente { get; set; }
        public int Asignado { get; set; }
        public int EnProceso { get; set; }
        public int Cerrado { get; set; }
        public int Cancelado { get; set; }
    }
    public class SubEstadoDto
    {
        public int SubEstadoID { get; set; }
        public string Nombre { get; set; }
    }
    //public sealed class SoporteYoDto
    //{
    //    public bool EsAdmin { get; set; }
    //    public string Carnet { get; set; }    // necesario para comparar con Caso.SoporteID
    //    public string SoporteID { get; set; } // correo del soporte
    //    public string Email { get; set; }
    //    public string Nombre { get; set; }
    //}
    public class SoporteYoDto
    {
        public string SoporteID { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }

        public string Correo { get; set; }   // o Email según tu convención
        public string Area { get; set; }
        public bool Activo { get; set; }
        public bool EsAdmin { get; set; }
        public bool EsSuper { get; set; }   // NUEVO
        public string Carnet { get; set; }
        public bool EsJefe { get; set; }
    }
    public class SoporteYoDto2
    {
        public string SoporteID { get; set; }   // normalmente el correo
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Area { get; set; }
        public bool Activo { get; set; }
        public bool EsAdmin { get; set; }
        public string Carnet { get; set; }

        public bool EsJefe { get; set; }   // <-- NUEVO
    }
    public class SubtipoCasoDto
    {
        public int SubtipoCasoID { get; set; }
        public int TipoCasoID { get; set; }
        public string Nombre { get; set; }
    }
    public class KPIResumenDto
    {
        public int Total { get; set; }
        public int Pendiente { get; set; }
        public int EnProceso { get; set; }
        public int Cerrado { get; set; }
        public int Cancelado { get; set; }
    }
    public class KPIPorDiaDto
    {
        public DateTime Dia { get; set; }
        public int Creados { get; set; }
        public int Cerrados { get; set; }
    }
    public class KPIPorTipoDto
    {
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }
        public int Total { get; set; }
    }
    public class KPIPorGerenciaDto
    {
        public string Gerencia { get; set; }
        public int Total { get; set; }
    }
    public class KPITiemposDto
    {
        public double? PromedioMinPrimeraAtencion { get; set; }
        public double? PromedioMinCierre { get; set; }
        public double? MedianaMinCierre { get; set; }
    }
    public class YoVM
    {
        public string Carnet { get; set; }
        public bool EsAdmin { get; set; }
        public bool EsJefe { get; set; }
        // puedes agregar más si quieres
    }
    
    public class CasoJefeVM
    {
        public int ID { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Prioridad { get; set; }
        public string Titulo { get; set; }

        public int? TipoCasoID { get; set; }
        public int? SubtipoCasoID { get; set; }
        public string TipoNombre { get; set; }
        public string SubtipoNombre { get; set; }

        public string SolicitanteCarnet { get; set; }
        public string SolicitanteNombre { get; set; }
        public string SolicitanteGerencia { get; set; }
        public string SolicitanteCorreo { get; set; }

        public string AfectadoCarnet { get; set; }
        public string AfectadoNombre { get; set; }
        public string AfectadoGerencia { get; set; }
        public string AfectadoCorreo { get; set; }

        public string RolEnMiEquipo { get; set; }

        public string Descripcion { get; set; }

        public string SoporteID { get; set; }
        public string Nombresoport { get; set; }

        // HS opcional
        public string Departamento { get; set; }
        public string Edificio { get; set; }
        public int? CantidadAfectados { get; set; }
        public int? DiasCondicion { get; set; }

        // Adjuntos primarios
        public string NombreArchivo { get; set; }
        public string data { get; set; } // base64 webp u otro
    }
    public class KPITopUsuarioDto
    {
        public string Usuario { get; set; }
        public int Total { get; set; }
    }

    public class SoporteVisibilidadDto
    {
        public int ID { get; set; }
        public string ViewerID { get; set; } // quien ve
        public string TargetID { get; set; } // a quien ve
                                             // Podrías poner Nombre etc. con JOIN
    }
    public class SoporteVisibilidadDto2
    {
        public int Id { get; set; }
        public string ViewerID { get; set; }
        public string CorreoTarget { get; set; }
        public string NombreTarget { get; set; }
    }

    // DTO para recibir el form del modal
    public class RecategorizacionVM
    {
        public int CasoID { get; set; }
        public int TipoCasoID { get; set; }
        public int SubtipoCasoID { get; set; }
        public string Nota { get; set; }
    }

    // DTO para el resultado del SP (info para el correo)
    public class RecategorizacionDTO
    {
        public int ID { get; set; }
        public string CorreoAutor { get; set; }
        public string CorreoResponsable { get; set; } // afectado
        public string CorreoSoporte { get; set; }     // asignado
        public string TipoAnterior { get; set; }
        public string SubtipoAnterior { get; set; }
        public string TipoNuevo { get; set; }
        public string SubtipoNuevo { get; set; }
        public string DestinatariosCSV { get; set; }  // opcional si el SP ya decide a quiénes
    }
    #endregion
