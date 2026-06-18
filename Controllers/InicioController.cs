using slnRhonline.Data;
using slnRhonline.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class InicioController : Controller
    {
        public ActionResult Index() => View();

        public ActionResult Registrar()
        {
            return this.Session["User"] == null ? (ActionResult)this.RedirectToAction("Index", "Login") : (ActionResult)this.View();
        }

        public ActionResult Details(int id) => View();

        public ActionResult Create() => View();

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public JsonResult Obtener()
        {
            List<ligaempleado> lista = new List<ligaempleado>();

            if (Session["lt"] != null)
                return Json(new { data = (List<ligaempleado>)Session["lt"] }, JsonRequestBehavior.AllowGet);

            using (SARHEntities sarhEntities = new SARHEntities())
            {
                try
                {
                    // Proyección de vep2 a ligaempleado
                    lista = sarhEntities.vep2.Select(x => new ligaempleado
                    {
                        // Comentario: Asumiendo que vep2 tiene las propiedades nombre, carnet, gerencia y area.
                        nombre = x.nombre_completo,
                        carnet = x.carnet,
                        gerente = x.OGERENCIA,
                        area = x.primernivel
                    }).ToList();

                    if (lista != null && lista.Count > 0)
                    {
                        Session["lt"] = lista;
                        return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Obtenerequipo()
        {
            string year = DateTime.Now.Year.ToString();
            List<ligaempleado> listaVacia = new List<ligaempleado>();

            using (SARHEntities sarhEntities = new SARHEntities())
            {
                try
                {
                    Entities.Employees empleado = (Entities.Employees)Session["User"];
                    var detalles = sarhEntities.ligadetalle
                        .Where(dt => dt.year == year &&
                                     dt.carnet == empleado.EmployeeNumber &&
                                     dt.estado == "Y");

                    if (detalles != null && detalles.Any())
                    {
                        int id = detalles.FirstOrDefault().idliga.Value;
                        var listaMaster = sarhEntities.ligamaster
                            .Where(dt2 => dt2.idliga == id)
                            .ToList();
                        Session["masterligar"] = listaMaster;

                        ligamodelo modelo = new ligamodelo
                        {
                            Nombre = listaMaster.FirstOrDefault().nombre,
                            Disciplina = listaMaster.FirstOrDefault().disciplina,
                            carnet = listaMaster.FirstOrDefault().carnet,
                            fullnombre = listaMaster.FirstOrDefault().creador,
                            estado = listaMaster.FirstOrDefault().estado
                        };
                        modelo.rg = empleado.EmployeeNumber == modelo.carnet || modelo.estado != "fin" ? "ok" : "";
                        return Json(new { data = modelo }, JsonRequestBehavior.AllowGet);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            return Json(new { data = listaVacia }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Obtenerequipo2()
        {
            string year = DateTime.Now.Year.ToString();
            List<ligadetallemodelo> listaDetalleModelo = new List<ligadetallemodelo>();

            using (SARHEntities sarhEntities = new SARHEntities())
            {
                try
                {
                    if (Session["masterligar"] != null)
                    {
                        var listaMaster = (List<ligamaster>)Session["masterligar"];
                        int id = listaMaster.FirstOrDefault().idliga;

                        if (sarhEntities.ligadetalle.Any(dt => dt.year == year && dt.idliga == (int?)id && dt.estado == "Y"))
                        {
                            listaDetalleModelo = sarhEntities.ligadetalle
                                .Where(x => x.year == year && x.idliga == (int?)id && x.estado == "Y")
                                .Select(x => new ligadetallemodelo
                                {
                                    // Comentario: Mapeo de propiedades desde ligadetalle a ligadetallemodelo.
                                    iddliga = x.iddliga,
                                    area = x.area,
                                    telefono = x.telefono
                                    // Agregar otras propiedades según sea necesario.
                                })
                                .ToList();
                            return Json(new { data = listaDetalleModelo }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            return Json(new { data = listaDetalleModelo }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Guardar(ligamodelo oPersona)
        {
            string resultado = "OK";
            try
            {
                string year = DateTime.Now.Year.ToString();
                Entities.Employees empleado = (Entities.Employees)Session["User"];

                ligamaster registrar = new ligamaster
                {
                    year = year,
                    nombre = oPersona.Nombre,
                    disciplina = oPersona.Disciplina,
                    fecha = DateTime.Now,
                    estado = "Y"
                };

                if (string.IsNullOrEmpty(empleado.EmployeeNumber))
                    return Json(new { resultado = "no es el capitan" }, JsonRequestBehavior.AllowGet);

                using (SARHEntities sarhEntities = new SARHEntities())
                {
                    bool equipoExiste = registrar.nombre != null &&
                                        registrar.disciplina != "Seleccione una opción" &&
                                        sarhEntities.ligamaster.Count(x => x.nombre == registrar.nombre) < 1;
                    if (!equipoExiste)
                        return Json(new { resultado = "usted ya ha creado un equipo" }, JsonRequestBehavior.AllowGet);

                    registrar.carnet = empleado.EmployeeNumber;
                    sarhEntities.ligamaster.Add(registrar);
                    sarhEntities.SaveChanges();

                    int idliga = registrar.idliga;
                    ligadetalle detalle = new ligadetalle
                    {
                        idliga = idliga,
                        tipo = "registro de equipo",
                        carnet = empleado.EmployeeNumber,
                        estado = "Y",
                        fecha = DateTime.Now,
                        telefono = oPersona.Telefono,
                        year = DateTime.Now.Year.ToString()
                    };
                    sarhEntities.ligadetalle.Add(detalle);
                    sarhEntities.SaveChanges();
                    sarhEntities.actualizarregistro();
                }
            }
            catch (Exception ex)
            {
                resultado = "error guardar";
            }
            return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Guardarf(ligamodelo oPersona)
        {
            string resultado = "ok";
            try
            {
                string year = DateTime.Now.Year.ToString();
                if (Session["masterligar"] != null)
                {
                    var listaMaster = (List<ligamaster>)Session["masterligar"];
                    Entities.Employees empleado = (Entities.Employees)Session["User"];

                    if (string.IsNullOrEmpty(empleado.EmployeeNumber))
                        return Json(new { resultado = "Solo el creador del equipo puede finalizar" }, JsonRequestBehavior.AllowGet);

                    using (SARHEntities sarhEntities = new SARHEntities())
                    {
                        int id = listaMaster.FirstOrDefault().idliga;
                        if (sarhEntities.ligamaster.Count(x => x.idliga == id) <= 0)
                            return Json(new { resultado = "no se pudo finalizar el equipo" }, JsonRequestBehavior.AllowGet);

                        int num = sarhEntities.ligadetalle.Count(x => x.year == year && x.estado == "Y" && x.idliga == (int?)id);
                        if (num > 0)
                        {
                            if (listaMaster.FirstOrDefault().disciplina == "Softball" && num < 14)
                            {
                                resultado = "Se necesita mínimo 14 personas en equipo de Softball para cerrar la inscripción";
                                return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
                            }
                            if (listaMaster.FirstOrDefault().disciplina == "Football Sala" && num < 8)
                            {
                                resultado = "Se necesita mínimo 8 personas en equipo de Football Sala para cerrar la inscripción";
                                return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
                            }
                            if (listaMaster.FirstOrDefault().disciplina == "Volibol" && num < 8)
                            {
                                resultado = "Se necesita mínimo 8 personas en equipo de Voleibol para cerrar la inscripción";
                                return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
                            }
                            if (listaMaster.FirstOrDefault().disciplina == "Tenis de Mesa" && num < 3)
                            {
                                resultado = "Se necesita mínimo 3 persona en equipo de Tenis de Mesa para cerrar la inscripción";
                                return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
                            }
                        }
                        var equipo = sarhEntities.ligamaster.FirstOrDefault(x => x.idliga == id);
                        if (equipo != null)
                        {
                            equipo.estado = "F";
                            sarhEntities.Entry(equipo).State = EntityState.Modified;
                            sarhEntities.SaveChanges();
                            resultado = "Se a finalizado el equipo";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resultado = "error:" + ex.Message;
            }
            return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Guardar2(ligamodelo oPersona)
        {
            string resultado = "ok";
            try
            {
                if (Session["masterligar"] != null)
                {
                    var listaMaster = (List<ligamaster>)Session["masterligar"];
                    string year = DateTime.Now.Year.ToString();

                    using (SARHEntities sarhEntities = new SARHEntities())
                    {
                        int idnew = listaMaster.FirstOrDefault().idliga;
                        int num = sarhEntities.ligadetalle.Count(x => x.year == year && x.estado == "Y" && x.idliga == (int?)idnew);
                        if (num > 0)
                        {
                            if (listaMaster.FirstOrDefault().disciplina == "Softball" && num > 17)
                            {
                                resultado = "Solamente se permiten 18 personas en equipo de Softball";
                                return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
                            }
                            if (listaMaster.FirstOrDefault().disciplina == "Football Sala" && num > 11)
                            {
                                resultado = "Solamente se permiten 12 personas en equipo de Football Sala";
                                return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
                            }
                            if (listaMaster.FirstOrDefault().disciplina == "Volibol" && num > 11)
                            {
                                resultado = "Solamente se permiten 12 personas en equipo de Voleibol";
                                return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
                            }
                            if (listaMaster.FirstOrDefault().disciplina == "Tenis de Mesa" && num > 4)
                            {
                                resultado = "Solamente se permiten 5 personas en equipo de Tenis de Mesa";
                                return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
                            }
                        }
                        if (sarhEntities.ligadetalle.Count(x => x.year == year && x.carnet == oPersona.Nombre && x.estado == "Y") < 1)
                        {
                            if (Session["lt"] != null)
                            {
                                var listaEmpleados = (IEnumerable<ligaempleado>)Session["lt"];
                                if (listaEmpleados.Any(q => q.carnet == oPersona.Nombre))
                                {
                                    ligadetalle detalle = new ligadetalle
                                    {
                                        idliga = idnew,
                                        tipo = "registro de jugador",
                                        carnet = oPersona.Nombre,
                                        telefono = oPersona.Disciplina,
                                        estado = "Y",
                                        fecha = DateTime.Now,
                                        gerencia = "",
                                        area = "",
                                        year = DateTime.Now.Year.ToString()
                                    };
                                    sarhEntities.ligadetalle.Add(detalle);
                                    sarhEntities.SaveChanges();
                                    sarhEntities.actualizarregistro();
                                }
                                else
                                    resultado = "El carnet no existe";
                            }
                            else
                                resultado = "sin registro de empleado";
                        }
                        else
                            resultado = "El carnet ingresado ya está registrado en un equipo";
                    }
                }
                else
                    resultado = "registro";
            }
            catch (Exception ex)
            {
                resultado = "error:" + ex.Message;
            }
            return Json(new { resultado = resultado }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Guardar3(string id)
        {
            int idd = int.Parse(id);
            bool flag = true;
            try
            {
                if (Session["masterligar"] != null)
                {
                    using (SARHEntities sarhEntities = new SARHEntities())
                    {
                        var detalle = sarhEntities.ligadetalle.FirstOrDefault(x => x.iddliga == idd);
                        if (detalle != null)
                        {
                            detalle.estado = "N";
                            sarhEntities.Entry(detalle).State = EntityState.Modified;
                            sarhEntities.SaveChanges();
                        }
                    }
                }
                else
                    flag = false;
            }
            catch (Exception ex)
            {
                flag = false;
            }
            return Json(new { resultado = flag }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Edit(int id) => View();

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Delete(int id) => View();

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public class emp
        {
            public string Nombre { get; set; }
            public string Gerencia { get; set; }
        }
    }
}
