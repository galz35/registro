using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class PresupuestoInput
    {
        public long GerenciaId { get; set; }
        public decimal MontoAumentar { get; set; }
        public decimal MontoDisminuir { get; set; }
    }
}