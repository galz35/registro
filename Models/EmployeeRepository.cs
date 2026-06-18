using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class EmployeeRepository
    {
 
        public static Entities.Employees GetEmployeeByEmail(string domain, string email)
        {
            string _cs = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog=CR;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;";
            if (string.IsNullOrWhiteSpace(email)) return null;

            using (var cn = new SqlConnection(_cs))
            {
                // Mapea a un DTO intermedio (nombres = alias del SP)
                var row = cn.QueryFirstOrDefault<EmpRow>(
                    "cr.usp_EmpleadoPorCorreo",
                    new { Correo = email.Trim() },
                    commandType: CommandType.StoredProcedure);

                if (row == null) return null;

                // Construye tu Entities.Employees
                var emp = new Entities.Employees
                {
                    Domain = domain,
                    Id_HRMS = row.Id_HRMS,
                    EmployeeNumber = row.EmployeeNumber,
                    FullName = row.FullName,
                    EmailAddress = row.EmailAddress ?? row.correo,
                    correo = row.correo ?? row.EmailAddress,

                    CompanyName = row.CompanyName,
                    ManagementName = row.ManagementName,
                    Location = row.Location,
                    TelephoneNumber = row.TelephoneNumber,
                    WorkCellNumber = row.WorkCellNumber,
                    PositionName = row.PositionName,

                    // niveles
                    RealUserLevel = SafeToInt(row.userlevel),
                     idorg = (long)row.idorg,

                    // ubicaciones / dirección
                    Departmet = row.Departmet,
                    Municipio = row.Municipio,
                    Address = row.Address,
                    Country = row.Country,

                    // fechas
                    fechaingreso = row.fechaingreso ?? DateTime.MinValue,
                    fechabaja = row.fechabaja ?? DateTime.MinValue,
                    DateofBirth = row.DateofBirth ?? DateTime.MinValue,

                    // jerarquía
                    segundo_nivel = row.segundo_nivel,
                    tercer_nivel = row.tercer_nivel,
                    cuarto_nivel = row.cuarto_nivel,
                    quinto_nivel = row.quinto_nivel,
                    sexto_nivel = row.sexto_nivel,

                    // jefaturas
                    nom_jefe1 = row.nom_jefe1,
                    correo_jefe1 = row.correo_jefe1,
                    cargo_jefe1 = row.cargo_jefe1,
                     carnet_jefe1 = row.carnet_jefe1,
                    nom_jefe2 = row.nom_jefe2,
                    correo_jefe2 = row.correo_jefe2,
                    cargo_jefe2 = row.cargo_jefe2,
                     carnet_jefe2 = row.carnet_jefe2,
                    nom_jefe3 = row.nom_jefe3,
                    correo_jefe3 = row.correo_jefe3,
                    cargo_jefe3 = row.cargo_jefe3,
                     carnet_jefe3 = row.carnet_jefe3,
                    nom_jefe4 = row.nom_jefe4,
                    correo_jefe4 = row.correo_jefe4,
                    cargo_jefe4 = row.cargo_jefe4,
                     carnet_jefe4 = row.carnet_jefe4,

                    // requeridos para tu modelo (sin foto)
                    EmergencyContact = "",
                    EmergencyContactNumber = ""
                };

                return emp;
            }
        }

        private static int SafeToInt(long? v) => v.HasValue
            ? (v.Value > int.MaxValue ? int.MaxValue : (v.Value < int.MinValue ? int.MinValue : (int)v.Value))
            : 0;

        // DTO interno para mapear el SP (coincide con alias del PROC)
        private class EmpRow
        {
            public long Id_HRMS { get; set; }
            public string EmployeeNumber { get; set; }
            public string FullName { get; set; }
            public string correo { get; set; }
            public string EmailAddress { get; set; }
            public string CompanyName { get; set; }
            public string ManagementName { get; set; }
            public string Location { get; set; }
            public string TelephoneNumber { get; set; }
            public string WorkCellNumber { get; set; }
            public string PositionName { get; set; }
            public long? userlevel { get; set; }
            public long? LVL { get; set; }
            public long? idorg { get; set; }
            public string Departmet { get; set; }
            public string Municipio { get; set; }
            public string Address { get; set; }
            public string Country { get; set; }
            public DateTime? DateofBirth { get; set; }
            public DateTime? fechaingreso { get; set; }
            public DateTime? fechabaja { get; set; }

            public string segundo_nivel { get; set; }
            public string tercer_nivel { get; set; }
            public string cuarto_nivel { get; set; }
            public string quinto_nivel { get; set; }
            public string sexto_nivel { get; set; }

            public string nom_jefe1 { get; set; }
            public string correo_jefe1 { get; set; }
            public string cargo_jefe1 { get; set; }
            public long? idhcm_jefe1 { get; set; }
            public string carnet_jefe1 { get; set; }
            public string nom_jefe2 { get; set; }
            public string correo_jefe2 { get; set; }
            public string cargo_jefe2 { get; set; }
            public long? idhcm_jefe2 { get; set; }
            public string carnet_jefe2 { get; set; }
            public string nom_jefe3 { get; set; }
            public string correo_jefe3 { get; set; }
            public string cargo_jefe3 { get; set; }
            public long? idhcm_jefe3 { get; set; }
            public string carnet_jefe3 { get; set; }
            public string nom_jefe4 { get; set; }
            public string correo_jefe4 { get; set; }
            public string cargo_jefe4 { get; set; }
            public long? idhcm_jefe4 { get; set; }
            public string carnet_jefe4 { get; set; }

            public long? o1 { get; set; }
            public long? o2 { get; set; }
            public long? o3 { get; set; }
            public long? o4 { get; set; }
            public long? o5 { get; set; }
            public long? o6 { get; set; }
        }
    }
}