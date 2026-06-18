using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class viaticojsonmodel
    {
        public class Datum
        {
            public int ExpenseDetailId { get; set; }
            public int ExpenseId { get; set; }
            public object Carnet { get; set; }
            public long PersonId { get; set; }
            public int ClassId { get; set; }
            public int ReasonId { get; set; }
            public string Justify { get; set; }
            public string Route { get; set; }
            public object ServiceNumber { get; set; }
            public string EmployeeNumber { get; set; }
            public string FullName { get; set; }
            public DateTime ExpenseDate { get; set; }
            public string ClassName { get; set; }
            public int ClasificationId { get; set; }
            public int CategoryId { get; set; }
            public int SubCategoryId { get; set; }
            public object Clasification { get; set; }
            public object Category { get; set; }
            public object SubCategory { get; set; }
            public string ExpenseStatus { get; set; }
            public double TotalAmount { get; set; }
            public double TotalYieldAmount { get; set; }
            public double TotalReturnAmount { get; set; }
            public DateTime HourStart { get; set; }
            public int StatusId { get; set; }
            public object YieldFile { get; set; }
            public object DepositFile { get; set; }
            public DateTime StartPeriodDate { get; set; }
            public DateTime EndPeriodDate { get; set; }
            public DateTime PaidDate { get; set; }
            public DateTime LastDate { get; set; }
            public DateTime YieldDate { get; set; }
            public object VehicleNumber { get; set; }
            public object AreaName { get; set; }
        }

        public class Root
        {
            public List<Datum> data { get; set; }
        }
    }
}