using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class viaticodetallemodal
    {
        public int ExpensePeriodId { get; set; }
        public string Notes { get; set; }
        public int ExpenseId { get; set; }
        public string Carnet { get; set; }
        public string FullName { get; set; }
        public string JUSTIFY { get; set; }
        public string Jefe { get; set; }
        public string Gerencia { get; set; }
        public decimal Amount { get; set; }
        public DateTime? ExpenseDate { get; set; }
    }
}