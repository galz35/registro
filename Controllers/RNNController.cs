using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class RNNController : Controller
    {
        // GET: RNN
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Muestra()
        {
            return View();
        }

        // GET: RNN/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }
        [HttpPost]
        public JsonResult GuardarFrames(string etiqueta, string[] frames)
        {
            if (string.IsNullOrEmpty(etiqueta) || frames == null || frames.Length == 0)
                return Json(new { success = false, message = "Datos inválidos." });

            string rutaCarpeta = Server.MapPath($"~/Frames/{etiqueta}");
            Directory.CreateDirectory(rutaCarpeta);

            for (int i = 0; i < frames.Length; i++)
            {
                string base64 = frames[i].Split(',')[1]; // Eliminar encabezado 'data:image/jpeg;base64,'
                byte[] imagenBytes = Convert.FromBase64String(base64);
                System.IO.File.WriteAllBytes(Path.Combine(rutaCarpeta, $"frame_{i + 1}.jpg"), imagenBytes);
            }

            return Json(new { success = true, message = "Frames guardados correctamente." });
        }
        // GET: RNN/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RNN/Create
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

        // GET: RNN/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: RNN/Edit/5
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

        // GET: RNN/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: RNN/Delete/5
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
