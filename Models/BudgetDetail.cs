using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class BudgetDetail
    {
        public int IdPresupuesto { get; set; }
        public long Gerencia { get; set; }
        [JsonProperty("GERENCIAS")]

        public string Gerencias { get; set; }
        public string Ger { get; set; }
        public decimal MontoAumentar { get; set; }
        public decimal MontoDisminuir { get; set; }
        public string Justificacion { get; set; }
        public string Estado { get; set; }
        public string Periodo { get; set; }
        public string EstadoPresupuesto { get; set; }
        public System.DateTime? FechaRegistro { get; set; }
        public System.DateTime? FechaEstado { get; set; }
    }
}