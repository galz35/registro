using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class viaticosreportes
    {
        public int EXPENSE_PERIOD_ID { get; set; }
        public string NOTES { get; set; }
        public int EXPENSE_ID { get; set; }
        public int CLASS_ID { get; set; }
        public int PERSON_TYPE_ID { get; set; }
        public int PERSON_ID { get; set; }
        public String FULLNAME { get; set; }
        public String GERENCIA { get; set; }
        public String EMPLOYEE_NUMBER { get; set; }
        public DateTime EXPENSE_DATE { get; set; }
        public int EXPENSE_DETAIL_ID { get; set; }

        public String CLASSNAME { get; set; }
        public int CLASIFICATIONID { get; set; }
        public String CLASIFICATION { get; set; }
        public String CATEGORY { get; set; }
        public String SUBCATEGORY { get; set; }
        public int DEPARTMENT_ID { get; set; }
        public Decimal AMOUNT { get; set; }
        public Decimal YIELD_AMOUNT { get; set; }
        public String STATUS { get; set; }
        public String COD_JEFE { get; set; }
        public String NOMBRE_JEFE { get; set; }
        public int STATUS_ID { get; set; }
        public DateTime EXPENSE_STATUS_DATE { get; set; }
        public String COMPANY { get; set; }
        public String BANCO { get; set; }
        public String DEPARTAMENTO { get; set; }
        public String EDIFICIO { get; set; }
        public String RUTA { get; set; }
    }
}