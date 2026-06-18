using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class Expensejson
    {

        public double EXPENSE_ID { get; set; }
        public double PERSON_ID { get; set; }
        public string CARNET { get; set; }
        public string FULLNAME { get; set; }
        public DateTime EXPENSE_DATE { get; set; }
        public double CLASS_ID { get; set; }
        public string JUSTIFY { get; set; }
        public double REASON_ID { get; set; }
        public string ROUTE { get; set; }
        public object SERVICE_NUMBER { get; set; }
        public double EXPENSE_DETAIL_ID { get; set; }
        public double CLASIFICATIONID { get; set; }
        public double CATEGORYID { get; set; }
        public double SUBCATEGORYID { get; set; }
        public string CLASIFICATION { get; set; }
        public string CATEGORY { get; set; }
        public string SUBCATEGORY { get; set; }
        public string CLASSNAME { get; set; }
        public double AMOUNT { get; set; }
        public string STATUS { get; set; }
        public string CARNET1 { get; set; }
        public DateTime HOUR_START { get; set; }
    }
}