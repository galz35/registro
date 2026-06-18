using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class ReceiptData
    {
        public string Cliente { get; set; }
        public string Estacion { get; set; } // E/S LOZELSA
        public string Control { get; set; } // SMART CONTROL
        public string Unidad { get; set; } // SMART CONTROL
        public string NumeroCompra { get; set; } // Número debajo de "COMPRA" o el propio "COMPRA"
        public string Referencia { get; set; } // Referencia
        public string Producto { get; set; } // PetruSuper o Diesel
        public string Cedula { get; set; }
        public DateTime FechaHora { get; set; } // Fecha y Hora
        public double VolumenLitros { get; set; } // Volumen en litros
        public double PrecioTotal { get; set; } // Precio total
        public string Odometro { get;  set; }
      
        public double PrecioUnitario { get;  set; }
        public string Descripcion
        {
            get
            {
                // Se formatea la fecha y se muestra el precio en formato moneda
                return $"Unidad: {Unidad}, Fecha: {FechaHora:dd/MM/yyyy HH:mm}, Voucher: {Referencia}, Precio Total: {PrecioTotal:C}";
            }
        }
    }

}