using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult Error(int error = 0)
        {
            switch (error)
            {
                //HTTP 4xx: Errores del cliente
                
                case 401:
                    ViewBag.Title = "401-Autorización de acceso";
                    ViewBag.Description = "La petición http no ha sido ejecutada porque debe de iniciar sesión, favor contactar al administrador del sistema. ";
                    break;
                case 403:
                    ViewBag.Title = "403-Acceso denegado";
                    ViewBag.Description = "No tiene permiso para acceder al recurso solicitado, favor contactar al administrador del sistema.";
                    break;
                case 404:
                    ViewBag.Title = "404-Página no encontrada";
                    ViewBag.Description = "La url que está intentando ingresar no existe o el recurso ha sido borrado, favor contactar al administrador del sistema";
                    break;
                //Errores de Servidor 5xx: 500 y 502
                case 500:
                    ViewBag.Title = "500-Error interno de servidor";
                    ViewBag.Description = "Error de servidor web en la aplicacion, favor contactar con el administrador del sistema.";
                    break;
                case 502:
                    ViewBag.Title = "502-Error de comunicacion";
                    ViewBag.Description = "Ocurrió un error con la comunicación al servidor, favor contactar con el administrador del sistema.";
                    break;

                default:
                    ViewBag.Title = "RH-Online";
                    ViewBag.Description = "Ocurrió un error, favor reiniciar sesión.";
                    break;
            }

            return View();
        }
        public ActionResult PageNotFound()
        {
            //Response.TrySkipIisCustomErrors = true;
            //Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View();
        }
        public ActionResult AccessAuthorized()
        {
            //Response.TrySkipIisCustomErrors = true;
            //Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View();
        }
        public ActionResult AccessDenied()
        {
            //Response.TrySkipIisCustomErrors = true;
            //Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View();
        }
        public ActionResult InternalServer()
        {
            //Response.TrySkipIisCustomErrors = true;
            //Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View();
        }
        public ActionResult Comunication()
        {
            //Response.TrySkipIisCustomErrors = true;
            //Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View();
        }
    }
}