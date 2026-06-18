using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class agradecimientomodel
    {
        public int GratefulnessId { get; set; }
        public DateTime GratefulnessDate { get; set; }
        
       
        public string ReceiveEmployeeNumber { get; set; }
        public string SendPersonName { get; set; }
        
        public string DestinataryName { get; set; }
       
        public string GratefulnessTypeName { get; set; }
        public string Message { get; set; }
      
    }
}