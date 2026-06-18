using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class ConfiguracionProceso
    {
        public int ConfiguracionProcesoID { get; set; }
        public bool EsCiclico { get; set; }
        public bool RequiereFirmaEvaluacion { get; set; }
        public bool RequiereFirmaCiclo { get; set; }
        public bool RequiereAprobacionFinal { get; set; }
        public bool EnviarRecordatorios { get; set; }
    }
}