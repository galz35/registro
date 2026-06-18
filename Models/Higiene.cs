using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace slnRhonline.Models
{
    [DataContract]
    public class Higiene
    {
        [DataMember]
        public String CODIGO { get; set; }
        [DataMember]
        public String CODIGO2 { get; set; }
        [DataMember]
        public String NOMBRE { get; set; }

        [DataMember]
        public String NOMBRE_COMPLETO { get; set; }
        [DataMember]
        public String APELLIDO { get; set; }
        [DataMember]
        public String CEDULA { get; set; }
        [DataMember]
        public String CORREO { get; set; }
        [DataMember]
        public String CORREOJEFE { get; set; }
        [DataMember]
        public String GERENCIARAIZ { get; set; }
        [DataMember]
        public String TIPO_EMPLEADO { get; set; }
        [DataMember]
        public String GERENCIA { get; set; }
        [DataMember]
        public String SUBGERENCIA { get; set; }
        [DataMember]
        public String AREA { get; set; }
        [DataMember]
        public String AREA2 { get; set; }
        [DataMember]
        public String PUESTO { get; set; }
        [DataMember]
        public String PUESTO2 { get; set; }

        [DataMember]
        public String SEXO { get; set; }
        [DataMember]
        public DateTime FECHA_NAC { get; set; }
        [DataMember]
        public DateTime FECHA_INGRESO { get; set; }
        [DataMember]
        public String EDAD { get; set; }

        [DataMember]
        public String ANTIGUEDAD { get; set; }
        [DataMember]
        public DateTime FECHAFIN { get; set; }
        [DataMember]
        public String ESTADO { get; set; }
        [DataMember]
        public String SIND_FED { get; set; }
        [DataMember]
        public String EMPRESA { get; set; }
        [DataMember]
        public String DEPARTAMENTO { get; set; }
        [DataMember]
        public String MUNICIPIO { get; set; }
        [DataMember]
        public String CIUDAD { get; set; }

        [DataMember]
        public String EDIFICIO { get; set; }
        [DataMember]
        public String NIVELES { get; set; }

        [DataMember]
        public String NOMBRE_JEFE { get; set; }

        [DataMember]
        public String INSS { get; set; }
    }
}