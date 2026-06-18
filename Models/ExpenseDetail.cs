using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class ExpenseDetail
    {
        public int ExpensePeriodId { get; set; }
        public string Notes { get; set; }
        public int ExpenseId { get; set; }
        public string Carnet { get; set; }
        public string FullName { get; set; }
        public string Jefe { get; set; }
        public string Gerencia { get; set; }
        public System.DateTime ExpenseDate { get; set; }

        // Propiedades adicionales para la tabla de viáticos
        public decimal AumentarViaticos { get; set; }
        public decimal DisminuirViaticos { get; set; }
        public string Estado { get; set; }
    }
}