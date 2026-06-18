using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class viatico
    {
        public int ExpenseId { get; set; }

        public DateTime ExpenseDate { get; set; }

        public long PersonId { get; set; }

        public string ExpenseNotes { get; set; }

        public string Route { get; set; }

        public string Justify { get; set; }

        public int ClassId { get; set; }
        public string ServiceNumber { get; set; }
        public int ReasonId { get; set; }
        public int ExpensePeriodId { get; set; }

        public String VehicleNumber { get; set; }
        public String YieldFileExtension { get; set; }
        public String DepositFileExtension { get; set; }
        public String YieldNotes { get; set; }
        public String carnet { get; set; }
        public int RegisterPersonId { get; set; }
    }
}