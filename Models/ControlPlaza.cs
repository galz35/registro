using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class ControlPlaza
    {
        public int IdPlaza { get; set; }
        public string COD_ACTIV { get; set; }
        public string NOMBRE_ACTIVIDAD { get; set; }
        public string GERENCIA { get; set; }
        public string SUBGERENCIA { get; set; }
        public string COORDINACION_SUPERV { get; set; }
        public string AREA_OPERATIVA { get; set; }
        public string ORGANIZACION { get; set; }
        public string EDIFICIO { get; set; }
        public string NOMINA_DESVINCULANTE { get; set; }
        public string EMPRESA_CONTRATANTE { get; set; }
        public string PUESTO { get; set; }
        public DateTime? FECHA_RECIBO_PS { get; set; }
        public int? MES1 { get; set; }
        public int? AÑO { get; set; }
        public DateTime? F_RECEPCION_RQ { get; set; }
        public string NO_DE_EMP { get; set; }
        public string SUSTITUYE_A_NOMBRE { get; set; }
        public string POSICION_SIGHO { get; set; }
        public string SOLICITANTE_JEFE_INMEDIATO { get; set; }
        public decimal? SALARIO { get; set; }
        public string OBSERVACIONES { get; set; }
        public DateTime? FECHA_ENVIO_RS { get; set; }
        public string AUTORIZ { get; set; }
        public int? MES2 { get; set; }
        public string CORREO { get; set; }
        public string ESTATUS_SR { get; set; }
        public string ESTATUS2_S_R { get; set; }
        public string PERSONA_CONTRATADA { get; set; }
        public DateTime? FECHA_INGRESO { get; set; }
        public string ESPC_SELECCION_RECLUT { get; set; }
        public string OBSERVACIONES_SELECCION_RECLUTAMIENTO { get; set; }
        public string OBSERVACIONES_GENERALES { get; set; }
        public DateTime FECHA_CREACION { get; set; }
    }
}