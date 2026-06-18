using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public static  class Logica
    {
        public static SqlConnection Abrir()
        {
            // Obtiene la cadena de conexión del archivo Web.config
            var cn = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = RRHH_Inventario; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;";

            var conn = new SqlConnection(cn);
            conn.Open();
            return conn;
        }
    }
}