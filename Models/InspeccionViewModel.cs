using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class InspeccionViewModel
    { // Id del vehículo al que se actualizarán los datos
      // Identificador del vehículo
        public string IdVehiculo { get; set; }

        // Datos del vehículo
        public string Kilometraje { get; set; }
        public string Ubicacion { get; set; }

        // Usuario asignado
        public string AsignadaA { get; set; }

        // Lista de conductores (por ejemplo, cédulas)
        public List<string> Conductores { get; set; }

        // Nombre completo (opcional)
        public string NombreCompleto { get; set; }

        // Datos adicionales
        public string Domicilio { get; set; }
        public string Telefono { get; set; }
        public string PlantelAsignado { get; set; }

        // Ahora se reciben dos fotos de la licencia: frontal y posterior
        public string FotoLicenciaFrontalBase64 { get; set; }
        public string FotoLicenciaPosteriorBase64 { get; set; }

        // Indica si se realizó la prueba de manejo
        public bool PruebaManejo { get; set; }

        // Colección de fotos de inspección (por zona)
        public Dictionary<string, string> Imagenes { get; set; }

        // Estado de la inspección (por ejemplo, "Activo")
        public string Estado { get; set; }

        // Identificador del usuario asignado (opcional)
        public string ReceivePersonId { get; set; }

        // Nuevos campos para Departamento y Edificio
        public string Departamento { get; set; }
        public string Edificio { get; set; }
    
        public int IdInspeccion { get; set; }
        public string Usuario { get; set; }
        public string Correo { get; set; }
        public List<string> NombreConductores { get; set; }

    }
}