using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class Vehiculo
    {
        public string IdUnidad { get; set; }    // Ejemplo: BOMBA1
        public string Modelo { get; set; }      // Ejemplo: SR440
       

        public Vehiculo(string idUnidad, string modelo )
        {
            IdUnidad = idUnidad;
            Modelo = modelo;
       
         }
    }
}