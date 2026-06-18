using Newtonsoft.Json;
using RestSharp;
using slnRhonline.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class AccidenteController : Controller
    {
        // GET: Accidente
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult Listjson()
        {

             try
            {
                Entities.Employees em = new Entities.Employees();
                   var client = new RestClient("http://172.26.54.66/hcmapiws/api/values/Accidente/hs");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/json");
                IRestResponse response = client.Execute(request);
             

                List< accidentejson> instancia = JsonConvert.DeserializeObject<List<accidentejson>>(response.Content);
                Entities.Employees cookie2 = (Entities.Employees)Session["User"];

                if (cookie2 != null)
                {
                    em = cookie2;
                  string gerencias = RemoverNI(em.GERENCIA);
                    //string gerencias = "GERENCIA CALL CENTER";
                    if (gerencias.Contains("RECURSOS")==true)
                    {

                    }
                    else
                    {
                        if (instancia.Count(accidente => RemoverNI(accidente.GERENCIA).ToLower().Equals(gerencias.ToLower())) > 0)
                            instancia = instancia
           .Where(accidente => RemoverNI(accidente.GERENCIA).ToLower().Equals(gerencias.ToLower()))
           .ToList();
                        //instancia = instancia.Where(x => x.GERENCIA.Contains(gerencias) == true).ToList();
                        else instancia = new List<accidentejson>();
                    }
                    // Usa las preferencias recuperadas
                }

                List<accidentejson> temp = new List<accidentejson>();
                foreach(var q in instancia)
                {
                    if (q.GRAVE=="X")
                    {
                        q.t1 = "GRAVE";
                    }
                    if (q.LEVE == "X")
                    {
                        q.t1 = "LEVE";

                    }
                    if (q.MORTAL == "X")
                    {
                        q.t1 = "MORTAL";

                    }
                    if (q.CTRABAJO=="X")
                    {
                        q.t2 = "TREYECTO TRABAJO";
                    }
                    else { q.t2 = "TREYECTO CASA"; }
                    temp.Add(q);
                }
                return Json(new { data = temp }, JsonRequestBehavior.AllowGet); ;

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            //  return Json(new { data = lstEmployees }, JsonRequestBehavior.AllowGet);
        }
        static string RemoverAcentos(string input)
        {
            string normalized = input.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
        static string RemoverNI(string input)
        {
            const string prefix = "NI ";
            if (input.StartsWith(prefix))
            {
                return input.Substring(prefix.Length);
            }
            return input;
        }
        public ActionResult List()
        {
            return View();
        }

        // GET: Accidente/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Accidente/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Accidente/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Accidente/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Accidente/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Accidente/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Accidente/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
