namespace slnRhonline.Controllers
{
    internal class EmpleadoVwefDto
    {
    
        public string carnet { get; set; }
         public string nombre_completo { get; set; }
        public string correo { get; set; }
        public string cargo { get; set; }
        public string empresa { get; set; }
        public string cedula { get; set; }
        public string Departamento { get; set; }
         public string Nombreubicacion { get; set; }
         public string fechaingreso { get; set; }
        public string fechabaja { get; set; }
         public string oDEPARTAMENTO { get; set; }//coorinacion
        public string OGERENCIA { get; set; } //gerencia
        public string oSUBGERENCIA { get; set; } 
         public string telefono { get; set; }
        public string telefonojefe { get; set; }
        public string nom_jefe1 { get; set; }
        public string correo_jefe1 { get; set; }
        public string cargo_jefe1 { get; set; }
         public string carnet_jefe1 { get; set; }
        
        public string SUBGERENTECORREO { get; set; }
        public string SUBGERENTE { get; set; }
        public string GERENTECORREO { get; set; }
        public string GERENTE { get; set; }
        public string GERENTECARNET { get; set; }
        public string organizacion { get; set; }
        public string primernivel { get; set; } //Area
 
        // --- columnas de emp (aliased) ---
        public string EmpAddressLine1 { get; set; } //direccion real
        public string EmpCity { get; set; } //ciudad
        public string EmpRegion2 { get; set; } //departamento
        public string EmpGender { get; set; }  // genero 
        public string EmpMaritalStatus { get; set; }// estado civil
    }
}