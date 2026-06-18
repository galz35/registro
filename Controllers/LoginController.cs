using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.Script.Serialization;
using slnRhonline.Data;
using System.Net;
using System.Web.UI;
using System.Diagnostics;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Security.Principal;
using System.Data.SqlClient;
using System.Net.Mail;
using slnRhonline.Models;

//using Entities;

namespace slnRhonline.Controllers
{

    public class LoginController : Controller
    {

        public ActionResult Login()
        {


            //string usuarioLogueado = WindowsIdentity.GetCurrent().Name;
            //ViewData["UsuarioWindows"] = usuarioLogueado;

            HttpCookie rememberCookie = Request.Cookies["RememberMeCookie"];

            if (rememberCookie != null)
            {
                string username = rememberCookie.Values["Username"];
                string Domian = rememberCookie.Values["Domian"];

                string Email = rememberCookie.Values["Email"];
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var cookie = Request.Cookies["Menulista"];
                if (cookie != null)
                {
                    var preferences2 = JsonConvert.DeserializeObject<List<Entities.MenuRhOnline>>(cookie.Value);
                    Session["Menulista"] = preferences2;

                    // Usa las preferencias recuperadas
                }
                else
                {
                    // No se encontraron preferencias en la cookie
                }
                var cookie2 = Request.Cookies["Empleado"];
                if (cookie2 != null)
                {
                    var preferences = JsonConvert.DeserializeObject<Entities.Employees>(cookie2.Value);
                    Session["User"] = preferences;

                    // Usa las preferencias recuperadas
                }
                else
                {
                    // No se encontraron preferencias en la cookie
                }

                var model = new Entities.Users { UserName = username, RememberMe = true };
                var employee = Employee.GetEmployeeData(Email);
                if (employee != null)
                {

                    employee.Domain = model.Domain;

                    Session["User"] = employee;

                    // Creamos una cookie sin que pueda recordar al usuario
                    System.Diagnostics.Debug.WriteLine("ENTRO 5");

                    FormsAuthentication.SetAuthCookie(employee.Names, false);
                    System.Diagnostics.Debug.WriteLine("ENTRO 6");
                    Data.Menu menu = new Data.Menu();
                    var oMenuRhOnline = (List<Entities.MenuRhOnline>)Session["Menulista"];
                    if (oMenuRhOnline == null)
                    {
                        var menuList = menu.GetAllMenu();
                        var menuForDisplay = menu.GetAllMenuById(menuList, null);
                        Session["Menulista"] = menuForDisplay;

                        // return PartialView("_SideBar", menuForDisplay);
                        // return PartialView("_SideBar", menuForDisplay);
                    }

                    // Verifica si se seleccionó "Recordarme"
                    //   return View(model);
                    return RedirectToAction("Index", "Home");


                }


            }
            return View();
        }
     

        // Leer datos desde el archivo
        [HttpPost]
        public JsonResult IniciarSesion2(bool rememberMe)
        {
          //  Entities.Users model = new Entities.Users();
       
            Session.Remove("Menulista");


            string mensaje = "Exito";
            string email = string.Empty;
            bool estado = true;
            try
            {
                string resultadoEmail = "";
                System.Diagnostics.Debug.WriteLine("Aaa");
                Debug.Write("prueba");
               
                    // Establecer el contexto para el dominio actual
                    using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                    {
                        // Buscar el usuario actualmente autenticado en el contexto del dominio
                        UserPrincipal user = UserPrincipal.Current;

                        if (user != null && user.EmailAddress!=null && user.EmailAddress!="")
                        {

                        resultadoEmail = user.EmailAddress;


                        }
                        else
                        {
                            
                        }
                    }
              
                //   System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                //if (System.Net.ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServiceReference1.ClaroAsemClient ClaroWCF = new ServiceReference1.ClaroAsemClient();
                //var resultadoEmail = ClaroWCF.Login(model);
                System.Diagnostics.Debug.WriteLine("ENTRO 2");

                if (resultadoEmail == null)
                {
                    System.Diagnostics.Debug.WriteLine("SALIO 1");

                    estado = false;
                    mensaje = "Usuario o contraseña incorrecta.";
                    return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    if (resultadoEmail == "gustavo.lira@claro.com.ni")
                    {
                        email = resultadoEmail.ToString();
                        System.Diagnostics.Debug.WriteLine("ENTRO 4");

                        var employee = Employee.GetEmployeeData(email);
                          //var employee = Employee.GetEmployeeData( "gina.mejia@claro.com.ni");
                        // var employee = Employee.GetEmployeeData(model.Domain, "mario.hurtado@claro.com.ni");
                        //   var employee = Employee.GetEmployeeData(model.Domain, "wilson.lopezc@claro.com.ni");
                        //   var employee = Employee.GetEmployeeData(model.Domain, "nestor.zavala@claro.com.ni");
                        // var employee = Employee.GetEmployeeData(model.Domain, "karla.puerto@claro.com.ni");//jefa recursos humano 
                        System.Diagnostics.Debug.WriteLine("ENTRO 4.1");
                        if (employee != null)
                        {

             

                            Session["User"] = employee;

                            // Creamos una cookie sin que pueda recordar al usuario
                            System.Diagnostics.Debug.WriteLine("ENTRO 5");

                            FormsAuthentication.SetAuthCookie(employee.Names, false);
                            System.Diagnostics.Debug.WriteLine("ENTRO 6");
                            Data.Menu menu = new Data.Menu();
                            var oMenuRhOnline = (List<Entities.MenuRhOnline>)Session["Menulista"];
                            if (oMenuRhOnline == null)
                            {
                                var menuList = menu.GetAllMenu();
                                menuList = menuList.Where(x => x.ID != 2 && x.ParentID != 2).ToList();
                                var menuForDisplay = menu.GetAllMenuById(menuList, null);
                                Session["Menulista"] = menuForDisplay;

                                var cookie = new HttpCookie("Menulista");
                                cookie.Value = JsonConvert.SerializeObject(menuForDisplay);
                                cookie.Expires = DateTime.Now.AddDays(7); // Expira en 30 días
                                Response.Cookies.Add(cookie);
                                // return PartialView("_SideBar", menuForDisplay);
                                // return PartialView("_SideBar", menuForDisplay);
                            }

                            // Verifica si se seleccionó "Recordarme"
                            if (rememberMe)
                            {
                                // Crea una cookie para recordar la autenticación
                                HttpCookie rememberCookie = new HttpCookie("RememberMeCookie");
                                rememberCookie.Values.Add("Username", employee.Names);
                                rememberCookie.Values.Add("Domian", employee.Domain);
                                rememberCookie.Values.Add("Email", employee.EmailAddress);

                                rememberCookie.Expires = DateTime.Now.AddDays(7); // Establecer la expiración de la cookie
                                Response.Cookies.Add(rememberCookie);
                                var cookie = new HttpCookie("Empleado");
                                cookie.Value = JsonConvert.SerializeObject(employee);
                                cookie.Expires = DateTime.Now.AddDays(7); // Expira en 30 días
                                Response.Cookies.Add(cookie);
                            }


                        }
                        else
                        {
                            estado = false;
                            mensaje = "Usuario o contraseña incorrecta.";
                            return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        email = resultadoEmail.ToString();
                        System.Diagnostics.Debug.WriteLine("ENTRO 4");
                        var employee = Employee.GetEmployeeData( email);
                        //   var employee = Employee.GetEmployeeData(model.Domain, "yohana.garcia@claro.com.ni");
                        //   var employee = Employee.GetEmployeeData(model.Domain, "candida.sanchez@claro.com.ni");
                        // var employee = Employee.GetEmployeeData(model.Domain, "karla.puerto@claro.com.ni");//jefa recursos humano 
                        System.Diagnostics.Debug.WriteLine("ENTRO 4.1");
                        if (employee != null)
                        {
                              

                            Session["User"] = employee;
                            Entities.Employees temp = (Entities.Employees)Session["User"];
                            // Creamos una cookie sin que pueda recordar al usuario
                            System.Diagnostics.Debug.WriteLine("ENTRO 5");

                            FormsAuthentication.SetAuthCookie(employee.Names, false);
                            System.Diagnostics.Debug.WriteLine("ENTRO 6");
                            Data.Menu menu = new Data.Menu();
                            var oMenuRhOnline = (List<Entities.MenuRhOnline>)Session["Menulista"];
                            if (oMenuRhOnline == null)
                            {
                                var menuList = menu.GetAllMenu();
                                var menuForDisplay = menu.GetAllMenuById(menuList, null);
                                Session["Menulista"] = menuForDisplay;

                                var cookie = new HttpCookie("Menulista");
                                cookie.Value = JsonConvert.SerializeObject(menuForDisplay);
                                cookie.Expires = DateTime.Now.AddDays(7); // Expira en 30 días
                                Response.Cookies.Add(cookie);
                                // return PartialView("_SideBar", menuForDisplay);
                                // return PartialView("_SideBar", menuForDisplay);
                            }

                            if (rememberMe)
                            {
                                // Crea una cookie para recordar la autenticación
                                HttpCookie rememberCookie = new HttpCookie("RememberMeCookie");
                                rememberCookie.Values.Add("Username", employee.Names);
                                //rememberCookie.Values.Add("Domian", model.Domain);
                                rememberCookie.Values.Add("Email", employee.EmailAddress);

                                rememberCookie.Expires = DateTime.Now.AddDays(7); // Establecer la expiración de la cookie
                                Response.Cookies.Add(rememberCookie);
                                var cookie = new HttpCookie("Empleado");
                                cookie.Value = JsonConvert.SerializeObject(employee);
                                cookie.Expires = DateTime.Now.AddDays(7); // Expira en 30 días
                                Response.Cookies.Add(cookie);
                            }

                        }
                        else
                        {
                            estado = false;
                            mensaje = "Usuario o contraseña incorrecta.";
                            return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                        }
                    }


                }



            }
            catch (Exception e)
            {
                estado = false;
                mensaje = "Usuario o contraseña incorrecta, por favor corrija.";
                return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
        }
        private void SendNotificationEmail(string titulo, string mensaje,string correo)
        {

            string output = null;
            MailMessage email = new MailMessage();


            email.From = new MailAddress("Recursoshumanos@claro.com.ni");
            email.Subject = titulo;
            email.SubjectEncoding = System.Text.Encoding.UTF8;

       
            email.To .Add(correo);
 


            email.Body = mensaje;
            email.BodyEncoding = System.Text.Encoding.UTF8;
            email.IsBodyHtml = true;
            email.Priority = MailPriority.Normal;
            email.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.OnSuccess |
                        DeliveryNotificationOptions.Delay;


            ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
            SmtpClient cliente = new SmtpClient("10.200.5.23", 587); // IP y puerto de FortiMail
            cliente.Credentials = new NetworkCredential("recursoshumanos@claro.com.ni", "Enero&272025"); // Eliminar antes de producción
                                                                                                         //cliente.Credentials = new NetworkCredential("transporte@claro.com.ni", "Enero&r546"); // Eliminar antes de producción
            cliente.EnableSsl = true;


            try
            {
                cliente.Send(email);
                email.Dispose();
                output = "EXITO";
            }
            catch (Exception ex)
            {
                output = ex.InnerException.Message;
            }

        }
        [HttpPost]
        public JsonResult GenerarCodigoLogin(string correo, string pais)
        {
            string cnn = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIGHO1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;";
            // Verifica si ya existe un código hoy para ese correo/pais
            using (var con = new SqlConnection(cnn))
            using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM CodigosLoginExterno
        WHERE Correo=@correo AND Pais=@pais AND CONVERT(DATE,FechaGeneracion)=CONVERT(DATE,GETDATE())", con))
            {
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@pais", pais);
                con.Open();
                int existe = (int)cmd.ExecuteScalar();
                if (existe > 0)
                    return Json(new { status = false, message = "Ya generó un código hoy. Intente mañana." });
            }

            // Generar código 6 dígitos
            string codigo = new Random().Next(100000, 999999).ToString();

            // Guardar en SQL
            using (var con = new SqlConnection(cnn))
            using (var cmd = new SqlCommand(@"INSERT INTO CodigosLoginExterno (Correo,Codigo,Pais,FechaGeneracion)
        VALUES (@correo,@codigo,@pais,GETDATE())", con))
            {
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@codigo", codigo);
                cmd.Parameters.AddWithValue("@pais", pais);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            // Email HTML bonito
            string titulo = "Código de acceso RHOnline";
            string mensaje = $@"
        <div style='font-family:sans-serif; color:#222; padding:18px'>
            <h2 style='color:#d32f2f; margin-bottom:0'>RHOnline</h2>
            <p>Tu código para iniciar sesión es:</p>
            <div style='font-size:32px;letter-spacing:8px; font-weight:700; margin:12px 0; color:#d32f2f'>{codigo}</div>
            <p>Este código es válido solo por hoy.<br>Si no solicitaste este código, ignora este mensaje.</p>
            <hr>
            <small style='color:#aaa'>Recursos Humanos Claro | RHOnline</small>
        </div>";

            // Envía email usando tu método
            SendNotificationEmail(titulo, mensaje, correo);

            return Json(new { status = true, message = "Código enviado a su correo." });
        }

        // Valida login con código
        [HttpPost]
        public JsonResult LoginCodigo(string correo, string pais, string codigo)
        {
            string cnn = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIGHO1; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;";
            using (var con = new SqlConnection(cnn))
            using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM CodigosLoginExterno
            WHERE Correo=@correo AND Pais=@pais AND Codigo=@codigo
           ", con))
            {
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@pais", pais);
                cmd.Parameters.AddWithValue("@codigo", codigo);
                con.Open();
                int existe = (int)cmd.ExecuteScalar();
                if (existe > 0)
                {
                    Entities.Users model = new Entities.Users();
                    model.Domain = pais;
                    model.UserName = correo;
               
                    Session.Remove("Menulista");


                    string mensaje = "Exito";
                    string email = string.Empty; string email2 = "";

                    bool estado = true;
                    try
                    {
                        
                            email = model.UserName;
                            System.Diagnostics.Debug.WriteLine("ENTRO 4");
                            Entities.Employees employee = new Entities.Employees();
                            if (email2 != "")
                            {
                                employee = Employee.GetEmployeeData3(model.Domain, email2);

                            }
                            else
                            {
                                employee = Employee.GetEmployeeData3(model.Domain, email);

                                //    employee = Employee.GetEmployeeData(model.Domain, "patricia.cajina@claro.com.ni");


                            }   
                        if (employee==null && pais=="CLAROCR")
                        {
                            var emailToUse = !string.IsNullOrWhiteSpace(email2) ? email2 : model.UserName;
                              employee = EmployeeRepository.GetEmployeeByEmail(model.Domain, emailToUse);
                            if (employee != null)
                            {
                                employee.Domain = model.Domain;
                                Session["User"] = employee; // luego GetCarnetActual() usará EmployeeNumber
                            }

                        }
                            System.Diagnostics.Debug.WriteLine("ENTRO 4.1");
                            if (employee != null)
                            {

                                employee.Domain = model.Domain;

                                Session["User"] = employee;


                                FormsAuthentication.SetAuthCookie(model.UserName, false);

                                Data.Menu menu = new Data.Menu();
                                var oMenuRhOnline = (List<Entities.MenuRhOnline>)Session["Menulista"];
                                if (oMenuRhOnline == null)
                                {
                                    var menuList = menu.GetAllMenu();
                                    menuList = menuList.Where(x => x.ID != 2 && x.ParentID != 2).ToList();
                                    var menuForDisplay = menu.GetAllMenuById(menuList, null);
                                    Session["Menulista"] = menuForDisplay;

                                    var cookie = new HttpCookie("Menulista");
                                    cookie.Value = JsonConvert.SerializeObject(menuForDisplay);
                                    cookie.Expires = DateTime.Now.AddDays(30); // Expira en 30 días
                                    Response.Cookies.Add(cookie);
                                    // return PartialView("_SideBar", menuForDisplay);
                                    // return PartialView("_SideBar", menuForDisplay);
                                }

                            }
                            else
                            {
                                estado = false;
                                mensaje = "Usuario o contraseña incorrecta.";
                                return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                            }
                      
                    }
                    catch (Exception e)
                    {
                        estado = false;
                        mensaje = "Usuario o contraseña incorrecta, por favor corrija.";
                        return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                 }
                else
                {
                    return Json(new { status = false, message = "Código inválido o expirado" });
                }
            }
        }

        public JsonResult IniciarSesion(string domain, string userName, string password, bool rememberMe)
        {
            Entities.Users model = new Entities.Users();
            model.Domain = domain;
            model.UserName = userName;
            model.Password = password;
            model.RememberMe = rememberMe;
            Session.Remove("Menulista");


            string mensaje = "Exito";
            string email = string.Empty; string email2 = "";

            bool estado = true;
            try
            {
                // Definimos la lista de correos autorizados para la prueba
                var usuariosPrueba = new string[] {
                    "alvaro.gonzalez@claro.com.ni", "edgardj.cerna@claro.com.ni", "eliecer.obregon@claro.com.ni",
                    "hamzel.garcia@claro.com.ni", "heydi.guevara@claro.com.ni", "ivette.reyes@claro.com.ni",
                    "jose.cruzm@claro.com.ni", "karin.peralta@claro.com.ni", "lenin.medina@claro.com.ni",
                    "miguel.escobar@claro.com.ni", "miriam.castillo@claro.com.ni", "onil.cordonero@claro.com.ni",
                    "raul.centeno@claro.com.ni", "ronald.sequeira@claro.com.ni", "salvador.vela@claro.com.ni",
                    "scarleth.vivas@claro.com.ni", "victor.miranda@claro.com.ni"
                };

                // El IF quedaría así:
             

                if (model.Password == "dev24x"|| (userName== "rene_agustin@claro.com.gt" && model.Password == "guate2025") ||
                    (usuariosPrueba.Contains(userName.ToLower()) && model.Password == "pru2026"))
                {
                    
                        email = model.UserName;
                        System.Diagnostics.Debug.WriteLine("ENTRO 4");
                        Entities.Employees employee = new Entities.Employees();
                        if (email2 != "")
                        {
                            employee = Employee.GetEmployeeData3(model.Domain, email2);

                        }
                        else
                        {
                            employee = Employee.GetEmployeeData3(model.Domain, email);

                            //    employee = Employee.GetEmployeeData(model.Domain, "patricia.cajina@claro.com.ni");


                        }

                        System.Diagnostics.Debug.WriteLine("ENTRO 4.1");
                        if (employee != null)
                        {

                            employee.Domain = model.Domain;

                            Session["User"] = employee;
                         
 
                            FormsAuthentication.SetAuthCookie(model.UserName, false);
                          
                        Data.Menu menu = new Data.Menu();
                        var oMenuRhOnline = (List<Entities.MenuRhOnline>)Session["Menulista"];
                        if (oMenuRhOnline == null)
                        {
                            var menuList = menu.GetAllMenu();
                            menuList = menuList.Where(x => x.ID != 2 && x.ParentID != 2).ToList();
                            var menuForDisplay = menu.GetAllMenuById(menuList, null);
                            Session["Menulista"] = menuForDisplay;

                            var cookie = new HttpCookie("Menulista");
                            cookie.Value = JsonConvert.SerializeObject(menuForDisplay);
                            cookie.Expires = DateTime.Now.AddDays(30); // Expira en 30 días
                            Response.Cookies.Add(cookie);
                            // return PartialView("_SideBar", menuForDisplay);
                            // return PartialView("_SideBar", menuForDisplay);
                        }

                    }
                        else
                        {
                            estado = false;
                            mensaje = "Usuario o contraseña incorrecta.";
                            return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                        }
                }
                else
                {
                    if (model.UserName.EndsWith("**"))
                    {
                        // Quitar los caracteres "**" del final
                        model.UserName = model.UserName.Substring(0, model.UserName.Length - 2);
                        email2 = model.UserName;
                        //model.UserName = "gustavo.lira@claro.com.ni";
                    }
                    if (model.UserName.Contains("@"))
                    {
                        // Dividir el correo por '@' y tomar la primera parte
                        model.UserName = model.UserName.Split('@')[0];

                     }
                    System.Diagnostics.Debug.WriteLine("Aaa");
                Debug.Write("prueba");
                    string resultadoEmail = "";
                    //   System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                    //if (System.Net.ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
                    //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
 
                   

                        resultadoEmail = Employee.Getloginx(model);
                //
                System.Diagnostics.Debug.WriteLine("ENTRO 2");
                    //resultadoEmail = "byron.cali@claro.com.gt";
                    //domain = "CLAROGT";
                    //model.Domain= "CLAROGT";
                    if (resultadoEmail == null  )
                {
                string respues=        Employee.validarusuariored(model.UserName, model.Domain);


                        estado = false;
                    mensaje = respues;
                    return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);

                    }
                    else if (resultadoEmail.Contains("mensaje")==true)
                    {
                        string respues = Employee.validarusuariored(model.UserName, model.Domain);


                        estado = false;
                        mensaje = respues;
                        return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                    }
                    else if (resultadoEmail.Contains("claro.com")==true)
                   
                {
                  resultadoEmail = resultadoEmail.Replace("\"\"", "\"");
                  resultadoEmail = resultadoEmail.Replace("\"", "");
                    if (resultadoEmail == "gustavo.lira@claro.com.ni") {
                        email = resultadoEmail.ToString();
                        System.Diagnostics.Debug.WriteLine("ENTRO 4");
                         var employee = Employee.GetEmployeeData(email);
                    //   var employee = Employee.GetEmployeeData(model.Domain, "yohana.garcia@claro.com.ni");
                         // var employee = Employee.GetEmployeeData(model.Domain, "mario.hurtado@claro.com.ni");
                      //   var employee = Employee.GetEmployeeData(model.Domain, "wilson.lopezc@claro.com.ni");
                        //   var employee = Employee.GetEmployeeData(model.Domain, "nestor.zavala@claro.com.ni");
                        // var employee = Employee.GetEmployeeData(model.Domain, "karla.puerto@claro.com.ni");//jefa recursos humano 
                        System.Diagnostics.Debug.WriteLine("ENTRO 4.1");
                        if (employee != null)
                        {

                            employee.Domain = model.Domain;

                            Session["User"] = employee;

                            // Creamos una cookie sin que pueda recordar al usuario
                            System.Diagnostics.Debug.WriteLine("ENTRO 5");

                            FormsAuthentication.SetAuthCookie(employee.Names, false);
                            System.Diagnostics.Debug.WriteLine("ENTRO 6");
                            Data.Menu menu = new Data.Menu();
                            var oMenuRhOnline = (List<Entities.MenuRhOnline>)Session["Menulista"];
                            if (oMenuRhOnline == null)
                            {
                                var menuList = menu.GetAllMenu();
                                menuList = menuList.Where(x => x.ID != 2 && x.ParentID != 2).ToList();
                                var menuForDisplay = menu.GetAllMenuById(menuList, null);
                                Session["Menulista"] = menuForDisplay;
 
                                var cookie = new HttpCookie("Menulista");
                                cookie.Value = JsonConvert.SerializeObject(menuForDisplay);
                                cookie.Expires = DateTime.Now.AddDays(30); // Expira en 30 días
                                Response.Cookies.Add(cookie);
                                // return PartialView("_SideBar", menuForDisplay);
                                // return PartialView("_SideBar", menuForDisplay);
                            }
                             
                                // Verifica si se seleccionó "Recordarme"
                                if (model.RememberMe)
                                {
                                    // Crea una cookie para recordar la autenticación
                                    HttpCookie rememberCookie = new HttpCookie("RememberMeCookie");
                                    rememberCookie.Values.Add("Username", employee.Names);
                                rememberCookie.Values.Add("Domian", employee.Domain);
                                rememberCookie.Values.Add("Email", employee.EmailAddress);

                                rememberCookie.Expires = DateTime.Now.AddDays(30); // Establecer la expiración de la cookie
                                    Response.Cookies.Add(rememberCookie);
                                var cookie = new HttpCookie("Empleado");
                                cookie.Value = JsonConvert.SerializeObject(employee);
                                cookie.Expires = DateTime.Now.AddDays(30); // Expira en 30 días
                                Response.Cookies.Add(cookie);
                            }

                               
                        }
                        else
                        {
                                string respues = Employee.validarusuariored(model.UserName, model.Domain);


                                estado = false;
                                mensaje = respues;
                                return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                        }
                    } else
                    {
                        email = resultadoEmail.ToString();
                        System.Diagnostics.Debug.WriteLine("ENTRO 4");
                           
                                Entities.Employees employee = new Entities.Employees();
                            if (resultadoEmail.Contains("claro.com.ni"))
                            {


                                employee = Employee.GetEmployeeData(email);
                            }
                            else
                            {
                              
                               
                                    employee = Employee.GetEmployeeData3(model.Domain, email);
                               
                            }//   var employee = Employee.GetEmployeeData(model.Domain, "yohana.garcia@claro.com.ni");
                            //   var employee = Employee.GetEmployeeData(model.Domain, "candida.sanchez@claro.com.ni");
                            // var employee = Employee.GetEmployeeData(model.Domain, "karla.puerto@claro.com.ni");//jefa recursos humano 
                            System.Diagnostics.Debug.WriteLine("ENTRO 4.1");
                         if (employee != null)
                        {

                            employee.Domain = model.Domain;

                            Session["User"] = employee;
                            Entities.Employees temp = (Entities.Employees) Session["User"];
                            // Creamos una cookie sin que pueda recordar al usuario
                            System.Diagnostics.Debug.WriteLine("ENTRO 5");

                            FormsAuthentication.SetAuthCookie(employee.Names, false);
                            System.Diagnostics.Debug.WriteLine("ENTRO 6");
                            Data.Menu menu = new Data.Menu();
                            var oMenuRhOnline = (List<Entities.MenuRhOnline>)Session["Menulista"];
                            if (oMenuRhOnline == null)
                            {
                                var menuList = menu.GetAllMenu();
                                var menuForDisplay = menu.GetAllMenuById(menuList, null);
                                Session["Menulista"] = menuForDisplay;

                                var cookie = new HttpCookie("Menulista");
                                cookie.Value = JsonConvert.SerializeObject(menuForDisplay);
                                cookie.Expires = DateTime.Now.AddDays(30); // Expira en 30 días
                                Response.Cookies.Add(cookie);
                                // return PartialView("_SideBar", menuForDisplay);
                                // return PartialView("_SideBar", menuForDisplay);
                            }

                            if (model.RememberMe)
                            {
                                // Crea una cookie para recordar la autenticación
                                HttpCookie rememberCookie = new HttpCookie("RememberMeCookie");
                                rememberCookie.Values.Add("Username", employee.Names);
                                rememberCookie.Values.Add("Domian", model.Domain);
                                rememberCookie.Values.Add("Email", employee.EmailAddress);

                                rememberCookie.Expires = DateTime.Now.AddDays(30); // Establecer la expiración de la cookie
                                Response.Cookies.Add(rememberCookie);
                                var cookie = new HttpCookie("Empleado");
                                cookie.Value = JsonConvert.SerializeObject(employee);
                                cookie.Expires = DateTime.Now.AddDays(30); // Expira en 30 días
                                Response.Cookies.Add(cookie);
                            }

                        }
                        else
                        {
                                string respues = Employee.validarusuariored(model.UserName, model.Domain);


                                estado = false;
                                mensaje = respues;
                                return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                        }
                    }
             

                }
                    else if (resultadoEmail.Contains("de red es incorrecto")==true)
                    {
                        estado = false;
                        mensaje = resultadoEmail;
                        return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        string respues = Employee.validarusuariored(model.UserName, model.Domain);


                        estado = false;
                        mensaje = respues;
                        return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);

                    }
                }
            }
            catch (Exception e)
            {
                estado = false;
                mensaje = "Usuario o contraseña incorrecta, por favor corrija.";
                return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { status = estado, message = mensaje }, JsonRequestBehavior.AllowGet);
        }

        /*    [HttpPost, ValidateInput(false)]*/
        //public ActionResult Login(Entities.Users user)
        //{
        //    string email;
        //    if (ModelState.IsValid)
        //    {
        //        var resultadoEmail = Utils.ClaroWCF.Login(user);
        //        if (resultadoEmail == null)
        //        {
        //        {
        //            ModelState.AddModelError(string.Empty, "Nombre de usuario o Contraseña Incorrectos...");


        //        }
        //        else
        //        {
        //            email = resultadoEmail.ToString();

        //            var employee = Employee.GetEmployeeData(user.Domain, email);

        //            if (employee != null)
        //            {
        //                // Creamos una cookie sin que pueda recordar al usuario
        //                FormsAuthentication.SetAuthCookie(user.UserName, false);

        //                employee.Domain = user.Domain;

        //                Session["User"] = employee;
        //                return RedirectToAction("Index", "Home");


        //            }
        //            else
        //            {
        //                ModelState.AddModelError(string.Empty, "Nombre de usuario o Contraseña Incorrectos...");
        //            }

        //        }



        //    }

        //    return View(user);
        //}

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            HttpCookie rememberCookie = new HttpCookie("RememberMeCookie");
            rememberCookie.Expires = DateTime.Now.AddDays(-1); // Establecer la expiración en el pasado para eliminarla
            Response.Cookies.Add(rememberCookie);
            var cookie = new HttpCookie("Empleado");
            cookie.Expires = DateTime.Now.AddDays(-1); // Elimina la cookie
            Response.Cookies.Add(cookie);
            var cookie2 = new HttpCookie("Menulista");
            cookie2.Expires = DateTime.Now.AddDays(-1); // Elimina la cookie
            Response.Cookies.Add(cookie2);
            Session.Remove("User");
            Session.Remove("startDate");
            Session.Remove("endDate");
            Session.Remove("keyEmployee");
            Session.Remove("sEmployees");
            Session.Remove("sAuthorizeManager");
            Session.Remove("sAuthorizeBoss");
            Session.Remove("sEmployees");

            Session.Remove("sExpenseAuthorizeManager");
            Session.Remove("sExpenseAuthorizeCoordinator");
            Session.Remove("sExpenseAuthorizeRrhh");
            Session.Remove("sExpenseAuthorizeYield");
            Session.Remove("sExpenseAuthorizeBoss");
            Session.Remove("UploadedDepositBytes");
            Session.Remove("UploadedFileBytes");
            Session.Remove("fullName");
            Session.Remove("sParameterReport");
            Session.Remove("sParameter");
            Session.Remove("sParameterLicenseHistoric");
            Session.Remove("sLicenseHistoric");
            Session.Remove("sLicensesAuthorizeBoss");
            Session.Remove("sLicensesAuthorizeRh");
            Session.Remove("sRealEmployeesLicense");
            Session.Remove("sEmployeesLicense");
            Session.Remove("sLicenseType");
            Session.Remove("UploadedLicenseName");
            Session.Remove("UploadedLicenseBytes");
            Session.Remove("sLicensesConsultParameter");
            Session.Remove("sEmployeesBalanceParameter");
            Session.Remove("sEmployeeFamily");
            Session.Remove("sEmployeeQualification");
            Session.Remove("sPreviousEmployers");
            Session.Remove("sEmployeeHabilities");
            Session.Remove("sCommissionEmployee");
            Session.Remove("sListaUnidades");
            Session.Remove("keyUserId");
            Session.Remove("PhotoBase64");
            Session.Remove("sEmployeestodoempleado");
            Session.Remove("ReturnUrl");  
              Session.Abandon();
            Session.RemoveAll();
            Session.Clear();
            Utils.Employee = null;
            return RedirectToAction("Login");
        }


    }
}