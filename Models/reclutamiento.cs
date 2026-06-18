using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class reclutamiento
    {
        public int NUMERO_DE_CANDIDATO { get; set; }
        public string PRIMERNOMBRE { get; set; }
        public string SEGUNDONOMBRE { get; set; }
        public string PRIMERAPELLIDO { get; set; }
        public string SEGUNDOAPELLIDO { get; set; }
        public string CEDULA { get; set; }

        public string FECHA_NACIMIENTO { get; set; }
        public int EDAD { get; set; }
        public string ESTADO_CIVIL { get; set; }
        public string IDENTIFICACION { get; set; }
        public string TITULO { get; set; }
        public string LUGAR_DE_NACIMIENTO { get; set; }
        public int ID_PERSONA { get; set; }
        public string PUESTO { get; set; }
        public string CATEGORIA { get; set; }
        public string GERENCIA { get; set; }
        public string SUBGERENCIA { get; set; }
        public string COORDINACION { get; set; }
        public string SUPERVISION { get; set; }
        public string AREA { get; set; }
        public string NIVELES { get; set; }
        public string EDIFICIO { get; set; }
        public string REFERENCIA { get; set; }
        // [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string FECHA_INICIO { get; set; }
        public string EMPRESA { get; set; }
        public string NOMBRE_JEFE { get; set; }
        public string DEPARTAMENTO { get; set; }
        public string MUNICIPIO { get; set; }
        public string DIRECCION_DOMICILIO { get; set; }
        public float SALARIO { get; set; }
        public string BANCO { get; set; }
        public string TELEFONO1 { get; set; }
        public string TELEPHONE_NUMBER_2 { get; set; }
        public string CORREO2 { get; set; }
    }
}