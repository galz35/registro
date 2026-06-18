using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class EMPGENERALX
    {
        public string carnet2 { get; set; }
        public string nombre_completo { get; set; }
        public string correo { get; set; }
        public string cargo { get; set; }
        public string empresa { get; set; }
        public string cedula { get; set; }
        public string Departamento { get; set; }
        public string Direccion { get; set; }
        public string Nombreubicacion { get; set; }
        public string datos { get; set; }
        public Nullable<System.DateTime> fechaingreso { get; set; }
        public Nullable<System.DateTime> fechabaja { get; set; }
        public Nullable<System.DateTime> fechaasignacion { get; set; }
        public string ActionCode { get; set; }
        public int diaprueba { get; set; }
        public string nom_jefe1 { get; set; }
        public string correo_jefe1 { get; set; }
        public string cargo_jefe1 { get; set; }
        public string carnet_jefe1 { get; set; }
    }
}