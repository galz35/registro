using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class PresupuestoResumen
    {
        public string PeriodId { get; set; }
        public long OrganizationId { get; set; }
        public decimal AssignmentAmount { get; set; }
        public decimal ExecutedAmount { get; set; }
    }
}