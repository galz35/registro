using System;

namespace slnRhonline.Models.Compensacion
{
    public class ConfigValidadorViewModel
    {
        public int ConfigID { get; set; }
        public string Gerencia { get; set; }
        public string Subgerencia { get; set; }
        public string Area { get; set; }
        public string CarnetValidador { get; set; }
        public string NombreValidador { get; set; }
        public string CarnetEmpleado { get; set; }
        public bool? Activo { get; set; }
    }
}
