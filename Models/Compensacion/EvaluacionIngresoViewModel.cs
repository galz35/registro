using System;

namespace slnRhonline.Models.Compensacion
{
    public class EvaluacionIngresoViewModel
    {
        public int EvaluacionIngresoID { get; set; }
        public string CarnetEmpleado { get; set; }
        public string NombreEmpleado { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public string CarnetJefe { get; set; }
        
        // Formulario
        public int CalificacionAdaptacion { get; set; }
        public int CalificacionCompetencias { get; set; }
        public string ResultadoFinal { get; set; }
        public string Observaciones { get; set; }
        
        public DateTime FechaEvaluacion { get; set; }
        public string CarnetEvaluador { get; set; }
        public string Estado { get; set; }

        // Datos adicionales UI
        public string Cargo { get; set; }
        public string Gerencia { get; set; }
    }
}
