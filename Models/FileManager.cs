using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class FileManager
    {
        public string RecuperarDatosArchivo(string rutaArchivo)
        {
            string contenido = string.Empty;

            try
            {
                if (File.Exists(rutaArchivo))
                {
                    contenido = File.ReadAllText(rutaArchivo);
                    Console.WriteLine("Datos recuperados exitosamente:");
                    Console.WriteLine(contenido);
                }
                else
                {
                    Console.WriteLine("El archivo no existe en la ruta especificada.");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"Ocurrió un error al intentar leer el archivo: {e.Message}");
            }

            return contenido;
        }

        public void CrearArchivo(string rutaArchivo, string contenido)
        {
            try
            {
                File.WriteAllText(rutaArchivo, contenido);
                Console.WriteLine("Archivo creado exitosamente.");
            }
            catch (IOException e)
            {
                Console.WriteLine($"Ocurrió un error al intentar crear el archivo: {e.Message}");
            }
        }

        public void EliminarArchivo(string rutaArchivo)
        {
            try
            {
                if (File.Exists(rutaArchivo))
                {
                    File.Delete(rutaArchivo);
                    Console.WriteLine("Archivo eliminado exitosamente.");
                }
                else
                {
                    Console.WriteLine("El archivo no existe en la ruta especificada.");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"Ocurrió un error al intentar eliminar el archivo: {e.Message}");
            }
        }
    }
}