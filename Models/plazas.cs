using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class plazas
    {
        public string PositionCode { get; set; }
        public string Nombre_Posicion { get; set; }
        public string Nombre_Completo { get; set; }
        public string Carnet { get; set; }
        public int Hcm { get; set; }
        public string Cargo { get; set; }
        public string Empresa { get; set; }
        public string Gerencia { get; set; }
        public string Subgerencia { get; set; }
        public string Area { get; set; }
        public string Managerlevel { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string Action_Code { get; set; }
        public string Termination_Type { get; set; }
        public DateTime? Fechaasignacion { get; set; }
        public DateTime? Fechaingreso { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Nombreubicacion { get; set; }
        public string Direccion { get; set; }
        public DateTime Fechabaja { get; set; }
        public int Rn { get; set; }
        public string Activo { get; set; }
        public string Categoria { get; set; }
        public string TieneHistorial { get; set; }
    }
}