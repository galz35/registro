using Entities;
using Newtonsoft.Json;
 using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class CACController : Controller
    {
       
      // // private List<CacDepartamento> mode2;
      //  private List<CacUsuario> model;
      //  public CACController()
      //  {
           
      //      model = new List<CacUsuario>();
      //  }
      //  // GET: CAC
      //  public ActionResult Index()
      //  {
      //      return View();
      //  }
      //  public ActionResult List()
      //  {
      //      model = Utils.ClaroWCF.CACEmpleado();
      //      Session["listempleado"] = model;  
          
      //      return View(model);
      //  }
      //  // GET: CAC/Details/5

      //  public ActionResult Autorizacion()
      //  {
      //      return View();
      //  }
      //  public ActionResult Asignacion()
      //  { return View(); }
      //  public ActionResult Periodo()
      //  { return View(); }
      //  public ActionResult Administrador()
      //  { return View(); }
      //  // GET: CAC/Details/5
      //  public ActionResult Tienda()
      //  { return View(); }
      //  public ActionResult MisPunto()
      //  { return View(); }
      //  public ActionResult Inventario()
      //  { return View(); }
      //  [HttpPost]
      //  public ActionResult Registrar(string paramss, HttpPostedFileBase imagenArchivo)
      //  {
      //      slnRhonline.RHOnlineWCF.CacSolicitud temp = JsonConvert.DeserializeObject<slnRhonline.RHOnlineWCF.CacSolicitud>(paramss);
   

      //      string GuardarEnRuta = "~/Content/CAC/";
      //      string physicalPath = Server.MapPath("~/Content/CAC/");
      //      model = (List<CacUsuario>)Session["listempleado"];
      //      CacUsuario t1 = new CacUsuario();
      //      t1 = model.Where(x => x.CORREO == temp.Colaborador).FirstOrDefault();
      //      slnRhonline.RHOnlineWCF.CacSolicitudDato nuevo = new CacSolicitudDato
      //      {
      //          Colaborador = t1.NOMBRECOMPLETO,
      //          Colaborador_id = t1.ID_HRMS,
      //          Cargo = t1.CARGO,
      //          Correo = t1.CORREO,
      //          Destinatario = temp.Destinatario,
      //          Formato = temp.Formato,
      //          IdOrganizacion = t1.ORGANIZATION_ID,
      //          IdUsuarioReg = 1,
      //          Jefe = t1.NOMBRE_JEFE,
      //          Jefe_id = t1.JEFEID,
      //          Observacion = temp.Observacion,
      //          Pais = "NI",
      //          Punto = temp.Punto,
      //          Razon = temp.Razon,
      //          UsuarioReg = ""
      //      };
      //      var result = Data.CAC.GuardarSolicitud(nuevo);
      //      if (result != null)
      //      {
      //          int numero = Convert.ToInt32(result);
      //          if (!Directory.Exists(physicalPath))
      //              Directory.CreateDirectory(physicalPath);
      //          //if (File.Exists(ruta + "\\" + archivo))
      //          //{
      //          //    Console.WriteLine("Existe el archivo: " + archivo);
      //          //}



      //          if (imagenArchivo != null)
      //          {
      //              string extension = Path.GetExtension(imagenArchivo.FileName);
      //              GuardarEnRuta = GuardarEnRuta + numero.ToString() + extension;
      //              imagenArchivo.SaveAs(physicalPath + "/" + numero.ToString() + extension);
      //              nuevo.Archivo = numero + extension;

                

      //              result = Data.CAC.Updatei(nuevo);
      //              if (result != null)
      //              {
      //                  return Json(new { dt = "ok" });
      //              }
      //          }

                
      //      }
      //      return Json(new { dt = "ok" });
      //  }
       
      //  public JsonResult Listar ()
      //  {

      //      List<slnRhonline.RHOnlineWCF.CacSolicitudDato> oLista = new List<slnRhonline.RHOnlineWCF.CacSolicitudDato>();
      //      List<slnRhonline.RHOnlineWCF.CacSolicitudDato> oLista2 = new List< slnRhonline.RHOnlineWCF.CacSolicitudDato >();
           
      //oLista = Data.CAC.listarsolicitud( );

      //      foreach (var q in oLista)
      //      {
      //          q.Fecha2 = q.Fecha.ToShortDateString();
      //          oLista2.Add(q);
      //      }
      //          return Json (oLista2.ToList()  , JsonRequestBehavior.AllowGet);
      //  }



       

 
        
      //  [HttpPost]
      //  public JsonResult EliminarMarca(int id)
      //  {
      //      bool respuesta = false;
      //   //  respuesta = MarcaLogica.Instancia.Eliminar(id);
      //      return Json(new { resultado = respuesta }, JsonRequestBehavior.AllowGet);
      //  }



      //  [HttpGet]
      //  public JsonResult ListarProducto()
      //  {
      //      List< Products> oLista = new List<Products>();

            
      //      return Json(new { data = oLista }, JsonRequestBehavior.AllowGet);
      //  }

      //  [HttpPost]
      //  public JsonResult GuardarProducts(string objeto, HttpPostedFileBase imagenArchivo)
      //  {

      //      Response oresponse = new Response() { resultado = true, mensaje = "" };

      //      try
      //      {
      //          Products oProducts = new Products();
      //          oProducts = JsonConvert.DeserializeObject<Products>(objeto);

      //          //string GuardarEnRuta = "~/Imagenes/Productss/";
      //          //string physicalPath = Server.MapPath("~/Imagenes/Productss");

      //          //if (!Directory.Exists(physicalPath))
      //          //    Directory.CreateDirectory(physicalPath);

      //         // if (oProducts.ProductId == 0)
      //         // {
      //         ////    int id = ProductsLogica.Instancia.Registrar(oProducts);
      //         //     oProducts.ProductId = id;
      //         //     oresponse.resultado = oProducts.IdProducts == 0 ? false : true;

      //         // }
      //         // else
      //         // {
      //         //     oresponse.resultado = ProductsLogica.Instancia.Modificar(oProducts);
      //         // }


      //         // if (imagenArchivo != null && oProducts.IdProducts != 0)
      //         // {
      //         //     string extension = Path.GetExtension(imagenArchivo.FileName);
      //         //     GuardarEnRuta = GuardarEnRuta + oProducts.IdProducts.ToString() + extension;
      //         //     oProducts.RutaImagen = GuardarEnRuta;

      //         //     imagenArchivo.SaveAs(physicalPath + "/" + oProducts.IdProducts.ToString() + extension);

      //         //     oresponse.resultado = ProductsLogica.Instancia.ActualizarRutaImagen(oProducts);
      //         // }

      //      }
      //      catch (Exception e)
      //      {
      //          oresponse.resultado = false;
      //          oresponse.mensaje = e.Message;
      //      }

      //      return Json(oresponse, JsonRequestBehavior.AllowGet);
      //  } 

      //  [HttpPost]
      //  public JsonResult EliminarProducts(int id)
      //  {
      //      bool respuesta = false;
      //     // respuesta = ProductsLogica.Instancia.Eliminar(id);
      //      return Json(new { resultado = respuesta }, JsonRequestBehavior.AllowGet);
      //  }
    }

    public class Response
    {

        public bool resultado { get; set; }
        public string mensaje { get; set; }
    }
}

