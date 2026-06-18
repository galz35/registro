using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace slnRhonline.Models
{
    [DataContract]
    public class bajasBS
    {
        [DataMember]
        public String CODIGO { get; set; }
        [DataMember]

        public String CODIGO2 { get; set; }

        [DataMember]
        public String NOMBRE2 { get; set; }
        [DataMember]
        public String DEPART { get; set; }
        [DataMember]
        public String MUNICIPIO { get; set; }
        [DataMember]
        public String ACA { get; set; }
        [DataMember]
        public String CARGO2 { get; set; }
        [DataMember]
        public String CORREO { get; set; }
        [DataMember]
        public String NOMBRE_COMPLETO { get; set; }
        [DataMember]
        public DateTime DATE_OF_BIRTH { get; set; }
        [DataMember]
        public String SEX { get; set; }
        [DataMember]
        public String GERENCIARAIZ { get; set; }
        [DataMember]
        public String GERENCIA { get; set; }
        [DataMember]
        public String CARGO { get; set; }
        [DataMember]
        public int ACTIVIDAD { get; set; }
        [DataMember]
        public String AREA { get; set; }

        [DataMember]
        public String EMPRESA { get; set; }
        [DataMember]
        public DateTime FECHA_BAJA { get; set; }
        [DataMember]
        public DateTime FECHA_INGRESO { get; set; }
        [DataMember]
        public String MOTIVO_ABANDONO { get; set; }
        [DataMember]
        public String TELEFONO1 { get; set; }
        [DataMember]
        public String TELEPHONE_NUMBER_2 { get; set; }

        [DataMember]
        public int DIAS { get; set; }
        [DataMember]
        public decimal MES { get; set; }
        [DataMember]
        public decimal ANO { get; set; }
    }
}