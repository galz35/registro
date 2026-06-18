using ClosedXML.Excel;
 
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class ReportRangoController : Controller
    {
        // GET: ReportRango
        public ActionResult Index()
        {
            return View();
        }

        // GET: ReportRango/Details/5
     

        public FileResult Fecharango(DateTime f1,DateTime f2)
        {
            try
            {
                //  DataTable dt = ReporteLogica.Instancia.Productos(f1, f2);
                DataTable dt = null;
                dt.TableName = "Datos";
                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dt);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);

                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte Productos " + DateTime.Now.ToString() + ".xlsx");
                    }
                }
            }
            catch (Exception e) { return null; }
             
        }

     
 
    }
}
