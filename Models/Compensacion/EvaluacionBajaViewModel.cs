using System;

namespace slnRhonline.Models.Compensacion
{
    public class EvaluacionBajaViewModel
    {
        public int EvaluacionBajaID { get; set; }
        public string CarnetEmpleado { get; set; }
        public string NombreEmpleado { get; set; }
        public DateTime? FechaBaja { get; set; }
        public string CarnetJefe { get; set; }
        
        // Formulario
        public string MotivoBaja { get; set; }
        public int CalificacionDesempeno { get; set; }
        public bool RecomendableReingreso { get; set; }
        public string ObservacionesJefe { get; set; }
        
        public DateTime FechaEvaluacion { get; set; }
        public string CarnetEvaluador { get; set; }
        public string Estado { get; set; }

        // Datos adicionales para la UI
        public string Cargo { get; set; }
        public string Gerencia { get; set; }
    }
}
