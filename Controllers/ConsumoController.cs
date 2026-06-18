using DevExpress.Web.Mvc;
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Entities;
using Entities.ViewModels;
using slnRhonline.Validations;
using System.Globalization;
using slnRhonline.Reports;
using Tesseract;
using System.Web;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using RestSharp;
using slnRhonline.Models;
using ImageMagick;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Net.Mail;
using System.Net.Mime;
using System.IO;

namespace slnRhonline.Controllers
{
    public class ConsumoController : Controller
    {
        const string keyProveedor = "sProveedorCombustible";
        const string keyUnidadConsumo = "sUnidadConsumo";
        const string keyIdCombustibleConsumo = "sIdCombustibleConsumo";
        const string keyIdCombustibleTraslado = "sIdCombustibleTraslado";
        const string keyIdCombustibleExtraPlan = "sIdCombustibleExtraPlan";
        private readonly string connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog=SIAF;MultipleActiveResultSets=True;User ID=sarh;Password=ktSrW2n_4pR7;";

        static ServiceReference1.ClaroAsemClient ClaroWCF = new ServiceReference1.ClaroAsemClient();

        #region Lista de Unidades
        // <summary>
        /// Metodo que devuelve la lista de consumo por persona a la vista primaria
        /// </summary>
        /// <returns></returns>
        [Authorize]

        public ActionResult CarsList()
        {
           List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();
            Session.Remove("sListaUnidades");
            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
               
                lstCars = Data.Consumo.ObtenerListaUnidades(eEmployee.Idhrms.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return View("CarsList",lstCars);

        }

        // ConsumoController.cs
        // -----------------------------------------------------------------------------
        // GET: /Consumo/DetalleMovimientoJson?idUnidad=ABC123&periodo=5
        // Devuelve los movimientos en JSON ordenados de más antiguo a más reciente.
        public JsonResult DetalleMovimientoJson(string idUnidad, int periodo = 0)
        {
            if (periodo == 0) periodo = DateTime.Today.Month;               // período actual
            var lista = Data.Consumo.ObtenerReporteDetalleMovimiento(idUnidad )
                                   .OrderByDescending(r => r.Fecha)                    // más antiguo primero
                                   .Select(r => new                         // formatea numérico‑texto
                           {
                                       Fecha = r.Fecha.ToString("yyyy-MM-dd"),
                                       r.Movimiento,
                                       Debito = r.Debito.ToString("N2"),
                                       Credito = r.Credito.ToString("N2"),
                                       SaldoCombustible = r.SaldoCombustible.ToString("N2")
                                   })
                                   .ToList();

            return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]

        public ActionResult Actualizacion()
        {
            //List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();
            //Session.Remove("sListaUnidades");
            //try
            //{
            //    Entities.Employees eEmployee = null;
            //    if (Session["User"] != null)
            //    {
            //        eEmployee = (Entities.Employees)Session["User"];
            //    }

            //    lstCars = Data.Consumo.ObtenerListaUnidades(eEmployee.Idhrms.ToString());
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("Se ha producido el siguiente error ", ex);
            //}
            var emp = (Entities.Employees)Session["User"];
            bool esRH = false;
            if (emp != null)
            {
                esRH = (emp.GERENCIA ?? "").ToUpper().Contains("SUBGERENCIA DE RECURSOS HUMANOS") ||
                       (emp.SUBGERENCIA ?? "").ToUpper().Contains("SUBGERENCIA DE RECURSOS HUMANOS");
            }
            ViewBag.EsRH = esRH;
            return View( );
            //return View("Actualizacion", lstCars);

        }
        [Authorize]

        public ActionResult voucherOCR()
        {
            
            return View( );

        }
        /// <summary>
        /// Metodo que devuelve la lista de consumo por persona a la vista parcial
        /// </summary>
        /// <returns></returns>
        /// 


        private byte[] ConvertToWebP(MemoryStream ms)
        {
         
                using (var image = new MagickImage(ms))
                {
                    image.Format = MagickFormat.WebP; // Establecer el formato a WebP
                    image.Quality = 60; // Establecer la calidad (opcional)

                    using (var msWebp = new MemoryStream())
                    {
                        image.Write(msWebp);
                        return msWebp.ToArray();
                    }
                }
            
        }

        public String consumofoto(ReceiptData te, byte[] imagen)
        {
            Entities.ConsumptionClaro consumo = new ConsumptionClaro();
            consumo.CantidadLitros = 0;
            consumo.CantidadLitros = Convert.ToDecimal( te.VolumenLitros);
            consumo.PrecioLitros  = 0;
           
             consumo.PrecioLitros  = Convert.ToDecimal( te.PrecioUnitario);
            consumo.Cedula = te.Cedula;
            consumo.IdUnidad = te.Unidad;
            consumo.IdVoucher = te.Referencia;
            consumo.Estacion = te.Estacion;
            consumo.Cedula = te.Cedula;
            consumo.OdometroInicial =Convert.ToInt32( te.Odometro);
            consumo.FechaRegistro = te.FechaHora;
            consumo.Combustible = "";
            consumo.Departamento = "";
            consumo.IdTipoCombustible = "";
            if (te.PrecioTotal > 0)
            {
                consumo.ValorCordobas = Convert.ToDecimal(te.PrecioTotal);
                consumo.PrecioLitros= consumo.ValorCordobas/ consumo.CantidadLitros;
            }
            else {
                consumo.ValorCordobas=consumo.PrecioLitros*consumo.CantidadLitros;
            }
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string mensajeValidacion = string.Empty;
            //Instancia de la clase de validacion de consumo
            Consumo vConsumo = new Consumo();

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(string.Empty);
            consumo.IpLocal = ipLocal;
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(string.Empty);
            consumo.UsuarioDominio = usuarioDominio;
            //Obtener periodo de transaccion.
            var periodos =
                Data.CombustiblePeriodo.ObtenerCombustiblePeirodosPorFecha(
                    consumo.FechaRegistro.ToString("yyyy/MM/dd")).FirstOrDefault();
            if (periodos != null)
            {
                consumo.IdCombustiblePeriodo = periodos.IdCombustiblePeriodo;
            }
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
                consumo.IdPersona = eEmployee.Idhrms.ToString();
            }


            try
            {


               

                    result = Data.Consumo.InsertarConsumo(consumo);
                    if (result != "EXITO")
                    {

                        return "NO";

                    }
                    else
                    {
                        string connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIAF; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos

                        using (IDbConnection db = new SqlConnection(connectionString))
                        {





                            Archiv nuevoArchivo = new Archiv
                            {
                                Archivo = imagen,
                                Idunidad = te.Unidad,
                                Fecha = te.FechaHora,
                                Referencia = te.Referencia

                            };
                            string insertQuery = "INSERT INTO Archivo_voucher (Archivo,Idunidad, fecha, referencia) " +
                                     "VALUES (@Archivo,@Idunidad, @fecha, @referencia ); ";
                            db.Execute(insertQuery, nuevoArchivo);


                        }


                    }
 
            }
            catch (Exception ex)
            {
                return "NO";

            }

            return "SI";




        }
        public String consumofotoerror(  byte[] imagen)
        {
          
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
             }


            try
            {




             
                
                
                    string connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIAF; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos

                    using (IDbConnection db = new SqlConnection(connectionString))
                    {





                        Archiv nuevoArchivo = new Archiv
                        {
                            Archivo = imagen,
                            Fecha = DateTime.Now

                        };
                        string insertQuery = "INSERT INTO Archivo_voucher_error (Archivo,fecha,idusuario) " +
                                 "VALUES (@Archivo, @referencia@idusuario ); ";
                        db.Execute(insertQuery, nuevoArchivo);


                    


                }

            }
            catch (Exception ex)
            {
                return "NO";

            }

            return "SI";




        }

        public ConsumptionClaro ObtenerRegistroPorIdVoucher(string idVoucher)
        {
            string connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIAF; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos
            ConsumptionClaro temp = new ConsumptionClaro();
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT * FROM [dbo].[CombustibleConsumoClaro] WHERE [IdVoucher] = @IdVoucher";
                var registro = connection.QueryFirstOrDefault<ConsumptionClaro>(query, new { IdVoucher = idVoucher });
                if (registro!=null )
                {
                    return registro;
                }
                else 
                return (temp); // Retorna tupla con indicador y registro si existe
            }
        }
    

        private string ObtenerTextoDesdeAPI(byte[] imageBytes)
        {
            string apiUrl = "http://172.26.54.113:5000/process_image"; // URL de tu API
            var maxRetries = 3; // Número máximo de reintentos
            int attempt = 0;


            var client = new RestClient(apiUrl);

            // Crear la solicitud POST
            var request = new RestRequest(Method.POST);
            request.Timeout = -1;

            // Agregar el parámetro de consulta personId

            // Agregar el archivo de imagen al cuerpo de la solicitud
            // Puedes usar AddFileBytes para agregar un arreglo de bytes directamente

            // Ejecutar la solicitud de manera asíncrona
            request.AddFile("file", imageBytes, "image1.jpeg");
            while (attempt < maxRetries)
            {
                attempt++;
                var response = client.Execute(request);

                if (response != null)
                {
                    return response.Content;

                }
                System.Threading.Thread.Sleep(1000); // Espera 1 segundo antes de reintentar
            }

            // Manejar errores de la API
            throw new Exception("Error al llamar a la API después de varios intentos fallidos.");


        }

        private byte[] ImageToByteArray(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        private Bitmap PreprocesarImagen(Image img)
        {
            // Convertir a escala de grises y aplicar otras mejoras si es necesario
            Bitmap bmp = new Bitmap(img.Width, img.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                {
                new float[] { .3f, .3f, .3f, 0, 0 },
                new float[] { .59f, .59f, .59f, 0, 0 },
                new float[] { .11f, .11f, .11f, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
                });
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }
         public JsonResult ProcesarVoucher(HttpPostedFileBase file)
        {
            byte[] imageBytesx;
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    // Usar el stream del archivo sin guardarlo físicamente
                    using (var memoryStream = new MemoryStream())
                    {
                        // Copiar el archivo subido al MemoryStream
                        file.InputStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        using (var imagex = new MagickImage(memoryStream))
                        {
                            imagex.Format = MagickFormat.WebP; // Establecer el formato a WebP
                            imagex.Quality = 60; // Establecer la calidad (opcional)

                            using (var msWebp = new MemoryStream())
                            {
                                imagex.Write(msWebp);
                                imageBytesx = msWebp.ToArray(); // Retornar como arreglo de bytes
                            }
                        }
                        // Convertir el stream en una imagen para preprocesar
                        using (var img = Image.FromStream(memoryStream))
                        {
                            // Preprocesar la imagen antes de enviarla a la API
                            Bitmap imagenPreprocesada = PreprocesarImagen(img);

                            // Convertir la imagen preprocesada a un array de bytes
                            byte[] imageBytes = ImageToByteArray(imagenPreprocesada);

                            // Enviar la imagen a la API externa y obtener el texto
                            string extractedText = ObtenerTextoDesdeAPI(imageBytes);
                            if (extractedText == "Error al llamar a la API después de varios intentos fallidos.")
                            {
                                return Json(new { success = false, message = "Error al llamar a la API después de varios intentos fallidos." });
                            }
                            if (string.IsNullOrEmpty(extractedText))
                            {
                                return Json(new { success = false, message = "No se pudo extraer texto de la imagen." });
                            }
                            Entities.ConsumptionClaro consumo = new ConsumptionClaro();
                            // Dividir el texto en líneas para procesarlo
                            List<string> textLines = extractedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            List<UnidadControl> listaControl = new List<UnidadControl>();
                            listaControl = (List<UnidadControl>)Session["unidadvoucer"];
                            Entities.Employees eEmployee = null;
                            if (Session["User"] != null)
                            {
                                eEmployee = (Entities.Employees)Session["User"];
                            }
                            if (extractedText.Contains("PUMA") == true)
                            {
                                var processor = new ReceiptProcessor();
                                var receiptData = processor.ProcessBACReceipt(extractedText, listaControl);
                                //         byte[] imagen = ConvertToWebP(memoryStream);
                                if (
                                ValidarCamposReceiptData(receiptData))
                                {
                                    ConsumptionClaro temp = new ConsumptionClaro();
                                    temp = ObtenerRegistroPorIdVoucher(receiptData.Referencia);
                                    if (temp != null && temp.IdUnidad != null && temp.IdUnidad != "")
                                    {
                                        return Json(new { success = false, data = "El voucher(" + temp.IdVoucher + ") de la unidad:" + temp.IdUnidad + " ya esta registrado " + temp.FechaRegistro });

                                    }
                                    string resultado = consumofoto(receiptData, imageBytesx);
                                    if (resultado == "SI")
                                    {
                                        return Json(new { success = true, data = receiptData });
                                    }
                                    else
                                    {
                                        consumofotoerror(imageBytesx);
                                        return Json(new { success = false, data = "La foto del voucher se necesita mejorar la claridad y calidad" });
                                    }
                                    // Devolver los datos extraídos como respuesta JSON

                                }
                                else
                                {
                                    consumofotoerror(imageBytesx);

                                    return Json(new { success = false, data = "La foto del voucher se necesita mejorar la claridad y calidad" });
                                }

                            }
                            else
                            {
                                var processor = new ReceiptProcessor();
                                var receiptData = processor.ProcessText(extractedText, listaControl);
                                //byte[] imagen = ConvertToWebP(memoryStream);
                                if (
                               ValidarCamposReceiptData(receiptData))
                                {
                                    ConsumptionClaro temp = new ConsumptionClaro();

                                    temp = ObtenerRegistroPorIdVoucher(receiptData.Referencia);
                                    if (temp != null && temp.IdUnidad != null && temp.IdUnidad != "")
                                    {
                                        return Json(new { success = false, data = "El voucher(" + temp.IdVoucher + ") de la unidad:" + temp.IdUnidad + " ya esta registrado " + temp.FechaRegistro });

                                    }
                                    string resultado = consumofoto(receiptData, imageBytesx);

                                    // Devolver los datos extraídos como respuesta JSON
                                    if (resultado == "SI")
                                    {
                                        return Json(new { success = true, data = receiptData });
                                    }
                                    else
                                    {
                                        using (var db = new SqlConnection(connectionString))
                                        {
                                            
                                                string query = @"
                        INSERT INTO ImagenVoucher (Imagen, Carnet, Estado)
                        VALUES (@Imagen, @Carnet, 'Erro')";
                                                db.Execute(query, new { Imagen = imageBytes, Carnet = eEmployee.EmployeeNumber });
                                           

                                        }
                                        return Json(new { success = false, data = "La foto del voucher se necesita mejorar la claridad y calidad" });
                                    }
                                }
                                else
                                {
                                    using (var db = new SqlConnection(connectionString))
                                    {

                                        string query = @"
                        INSERT INTO ImagenVoucher (Imagen, Carnet, Estado)
                        VALUES (@Imagen, @Carnet, 'Erro')";
                                        db.Execute(query, new { Imagen = imageBytes, Carnet = eEmployee.EmployeeNumber });


                                    }
                                    return Json(new { success = false, data = "La foto del voucher se necesita mejorar la claridad y calidad" });
                                }
                            }
                             

                        }
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            return Json(new { success = false, message = "No se ha subido ningún archivo." });
        }
        public ActionResult VouchersList()

        {
            List<UnidadControl> listaControl = new List<UnidadControl>();

            using (var db = new SqlConnection(connectionString))
            {
                string sql = "SELECT DISTINCT Id_Unidad, Matricula FROM Vehiculos WHERE Id_Unidad IS NOT NULL";
                var result = db.Query<dynamic>(sql).ToList();

                // Se proyecta cada registro a una instancia de UnidadControl
                var unidades = result.Select(u => new UnidadControl(
                    codigoOriginal: (string)u.Id_Unidad,
                    matriculaOriginal: (string)u.Matricula
                )).ToList();
                Session["unidadvoucer"] = unidades;
            }
            //return View("VouchersList", lstVouchers);
            return View();
        }
        [HttpGet]
        public JsonResult ErrorVouchersListJson()
        {
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            IEnumerable<ImagenVoucher> vouchers;

            if (eEmployee.GERENCIA == "NI GERENCIA DE RECURSOS HUMANOS" && eEmployee.SUBGERENCIA == "NI SUBGERENCIA DE RECURSOS HUMANOS")
            {
                using (var db = new SqlConnection(connectionString))
                {
                    string sql = @"
                    SELECT Id, Carnet, Estado, Descripcion, FechaRegistro, FechaProcesado,  Unidad,Voucher,Fecha,grabado
                    FROM ImagenVoucher  
                     ORDER BY FechaRegistro DESC";
                    vouchers = db.Query<ImagenVoucher>(sql ).ToList();
                }
            }
            else
            {
                using (var db = new SqlConnection(connectionString))
                {
                    string sql = @"
                    SELECT Id, Carnet, Estado, Descripcion, FechaRegistro, FechaProcesado,  Unidad,Voucher,Fecha,grabado
                    FROM ImagenVoucher where carnet=@carnet
                     ORDER BY FechaRegistro DESC";
                    vouchers = db.Query<ImagenVoucher>(sql, new { carnet = eEmployee.EmployeeNumber }).ToList();
                }
            }
            return Json(new { data = vouchers }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetVoucherDetail2(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                // Ajusta la consulta según tus columnas y nombre de tabla
                string sql = "SELECT Id, Descripcion, Voucher, Unidad, Fecha, json FROM ImagenVoucher WHERE Id = @Id";
                var voucher = db.QueryFirstOrDefault<ImagenVoucher>(sql, new { Id = id });
                if (voucher != null)
                {
                    ReceiptData receipt = null;
                    try
                    {
                        // Deserializamos el campo Descripcion que contiene el ReceiptData serializado en JSON
                        receipt = JsonConvert.DeserializeObject<ReceiptData>(voucher.json);
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "Error al deserializar el ReceiptData: " + ex.Message }, JsonRequestBehavior.AllowGet);
                    }

                    // Preparamos el objeto con los datos que queremos mostrar
                    var detail = new
                    {
                        Unidad = receipt?.Unidad,
                        Fecha = receipt?.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                        Referencia = receipt?.Referencia, // Consideramos este campo como voucher
                        Litro = receipt?.VolumenLitros,
                        Odometro=receipt?.Odometro
                    };

                    return Json(new { success = true, data = detail }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "No se encontró el voucher." }, JsonRequestBehavior.AllowGet);
                }
            
        }
        }
        [HttpGet]
        public JsonResult GetVoucherDetail(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                // Consulta para obtener los datos extra
                string sql = @"SELECT 
                            Id, 
                            Descripcion, 
                            Voucher, 
                            Unidad, 
                            Fecha, 
                            json, 
                            Estado, 
                            Carnet, 
                            FechaRegistro, 
                            FechaProcesado
                       FROM ImagenVoucher
                       WHERE Id = @Id";
                var voucher = db.QueryFirstOrDefault<ImagenVoucher>(sql, new { Id = id });
                if (voucher != null)
                {
                    return Json(new { success = true, data = voucher }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "No se encontró el voucher." }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        /// <summary>
        /// Dado el id de un voucher, retorna la imagen en formato base64 para visualizarla.
        /// </summary>
        [HttpGet]
        public JsonResult GetVoucherImage(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                string sql = "SELECT Imagen FROM ImagenVoucher WHERE Id = @Id";
                byte[] imageData = db.QueryFirstOrDefault<byte[]>(sql, new { Id = id });
                if (imageData != null)
                {
                    string base64Image = "data:image/webp;base64," + Convert.ToBase64String(imageData);
                    return Json(new { success = true, image = base64Image }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { success = false, message = "No se encontró la imagen." }, JsonRequestBehavior.AllowGet);
        }
        public bool ValidarCamposReceiptData(ReceiptData data)
        {
            if (data == null)
                return false;

            return
                   !string.IsNullOrWhiteSpace(data.Unidad) &&

                   !string.IsNullOrWhiteSpace(data.Referencia) &&

                   data.FechaHora != default(DateTime) &&
                   data.VolumenLitros > 0 &&

                   !string.IsNullOrWhiteSpace(data.Odometro);
        }
        [HttpPost]
        public JsonResult ProcesarVoucher2025(HttpPostedFileBase file)
        {
             byte[] imageBytesx;
            if (file != null && file.ContentLength > 0)
            {
               
                    try
                    {
                        // Usar el stream del archivo sin guardarlo físicamente
                        using (var memoryStream = new MemoryStream())
                        {
                            // Copiar el archivo subido al MemoryStream
                            file.InputStream.CopyTo(memoryStream);
                            memoryStream.Position = 0;
                            using (var imagex = new MagickImage(memoryStream))
                            {
                                imagex.Format = MagickFormat.WebP; // Establecer el formato a WebP
                                imagex.Quality = 60; // Establecer la calidad (opcional)

                                using (var msWebp = new MemoryStream())
                                {
                                    imagex.Write(msWebp);
                                    imageBytesx = msWebp.ToArray(); // Retornar como arreglo de bytes
                                }
                          
                            string connectionString = "Data Source=192.168.8.234;Connection Timeout=60; Initial Catalog = SIAF; MultipleActiveResultSets=True; User ID=sarh; Password=ktSrW2n_4pR7;"; // Cadena de conexión a la base de datos
                            Entities.Employees eEmployee = null;
                            if (Session["User"] != null)
                            {
                                eEmployee = (Entities.Employees)Session["User"];
                            }

                            using (var db = new SqlConnection(connectionString))
                            {
                                //int count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM ImagenVoucher WHERE Estado IN ('Pendiente', 'Procesando')");
                                //if (count > 0)
                                //{
                                    string query = @"
                        INSERT INTO ImagenVoucher (Imagen, Carnet, Estado,Peso)
                        VALUES (@Imagen, @Carnet, 'Pendiente',@Peso)";
                                    db.Execute(query, new { Imagen = imageBytesx, Carnet = eEmployee.EmployeeNumber, Peso= imageBytesx.Length });
                                //}
                                //else { }

                            }
                            return Json(new { success = true, message = "Imagen registrada. El procesamiento puede demorar entre 1 a 10 minutos según la cola actual." });

                        }
                    }
                    }
                    catch (Exception e) { }

              }
            return Json(new { success = false, message = "No se subió ningún archivo." });
        }

        [HttpPost]
        public async Task<JsonResult> ProcesarVoucherx(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    // Usar el stream del archivo sin guardarlo físicamente
                    using (var memoryStream = new MemoryStream())
                    {
                        // Copiar el archivo subido al MemoryStream
                        file.InputStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;

                        // Convertir el stream en una imagen para preprocesar
                        using (var img = Image.FromStream(memoryStream))
                        {
                            // Preprocesar la imagen antes de pasársela a Tesseract
                            Bitmap imagenPreprocesada = PreprocesarImagen(img);

                            // Procesar la imagen preprocesada con Tesseract
                            string extractedText = ExtraerTextoDesdeBitmap(imagenPreprocesada);
                      //      var groqResult = ProcesarConGroq(memoryStream);
                            
                            return Json(new { success = true, text = extractedText });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            return Json(new { success = false, message = "No se ha subido ningún archivo." });


        }
 
        private string ExtraerTextoDesdeBitmap(Bitmap bitmap)
        {
            string textoExtraido = "";
            using (var engine = new TesseractEngine(Server.MapPath("~/tessdata"), "eng", EngineMode.Default))
            {
 
                using (var img = PixConverter.ToPix(bitmap))
                {
                    using (var page = engine.Process(img ))
                    {
                        textoExtraido = page.GetText();
                    }
                }
            }

            // Aplicar correcciones post-OCR
            string textCorregido = textoExtraido
                .Replace("§", "S")
                .Replace("C4", "C$")
                .Replace("Lts", "Lts")
                .Replace("Volunen", "Volumen"); // Ajuste de errores comunes
            var regex = new Regex(@"Referencia:\s*([A-Za-z0-9]+)");
            var match = regex.Match(textoExtraido);

            if (match.Success)
            {
                // Extraer la referencia y aplicar correcciones
                string referenciaOriginal = match.Groups[1].Value; // El valor alfanumérico después de "Referencia:"
                string referenciaCorregida = referenciaOriginal
                    .Replace("PI", "F9")    // Confusión PI -> F9
                    .Replace("Y", "1")      // Otras correcciones
                    .Replace("L", "1")
                    .Replace("I", "1")
                    .Replace("O", "0")
                    .Replace("Z", "2")
                    .Replace("S", "5");
                    

                // Reemplazar la referencia corregida en el texto completo
                textCorregido = regex.Replace(textoExtraido, $"Referencia: {referenciaCorregida}");
            }


            return textCorregido;
         }

        // Método que usa Tesseract para extraer el texto
        // Método para procesar la imagen desde un stream
        private string ExtraerTextoDesdeStream(Stream imageStream)
        {
            string textoExtraido = "";
             using (var engine = new TesseractEngine(Server.MapPath("~/tessdata"), "eng", EngineMode.Default))
            //            using (var engine = new TesseractEngine(Server.MapPath("~/tessdata"), "eng", EngineMode.Default))
            {
                using (var img = Pix.LoadFromMemory(ReadFully(imageStream)))
                {
                    //using (var page = engine.Process(img))
                    using (var page = engine.Process(img, PageSegMode.SingleBlock))
                    {
                        textoExtraido = page.GetText();
                    }
                }
            }
            string textCorregido = textoExtraido
   .Replace("§", "S")
   .Replace("C4", "C$")
   .Replace("Lts", "Lts")
   .Replace("Volunen", "Volumen"); // Ajuste de errores comunes

            return textCorregido;
        }
        private Bitmap PreprocesarImagenx(Image img)
        {
            // Convertir a escala de grises
            Bitmap bmp = new Bitmap(img.Width, img.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                {
            new float[] { .3f, .3f, .3f, 0, 0 },
            new float[] { .59f, .59f, .59f, 0, 0 },
            new float[] { .11f, .11f, .11f, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { 0, 0, 0, 0, 1 }
                });
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        // Método auxiliar para leer un stream completamente
        private byte[] ReadFully(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
        public ActionResult CarsListPartial()

        {
                

           List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();

            Session.Remove("sListaUnidades");
            try
            {

                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                
               
                lstCars = Data.Consumo.ObtenerListaUnidades(eEmployee.Idhrms.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("CarsListPartial",lstCars);
        }
    
        public JsonResult CarsListPartialjson()

        {


            List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();

            Session.Remove("sListaUnidades");
            try
            {

                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }


                lstCars = Data.Consumo.ObtenerListaUnidades(eEmployee.Idhrms.ToString());
                //lstCars = Data.Consumo.ObtenerListaUnidades(68606+"");
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return Json(new { data = lstCars }, JsonRequestBehavior.AllowGet);
        }
        /// Accion que retorna el resultado de la accion RegisterDetail
        /// </summary>
        /// <returns></returns>
        public ActionResult BackToList()
        {
            //string sIdUnidad;
            //sIdUnidad = (string)Session[keyUnidadConsumo];


            return CarsList();
        }
        #endregion
        #region Lista de registros de consumo
        [Authorize]

       

        public JsonResult VouchersListjson()

        {
            List<Entities.ConsumptionClaro> lstVouchers = new List<Entities.ConsumptionClaro>();
            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                lstVouchers = Data.Consumo.ObtenerConsumoPorUnidad(eEmployee.Idhrms.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            if (lstVouchers.Count()>0)
            {
                lstVouchers = lstVouchers.OrderByDescending(x => x.FechaRegistro).ToList();
            }
            return Json(new { data = lstVouchers }, JsonRequestBehavior.AllowGet);
        }
        //[HttpGet]
        public ActionResult VouchersListPartial()
        {
            List<Entities.ConsumptionClaro > lstVouchers = new List<Entities.ConsumptionClaro>();
            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                //lstVouchers = Data.Consumo.ObtenerConsumoPorUnidad(idUnidad);
                lstVouchers = Data.Consumo.ObtenerConsumoPorUnidad(eEmployee.Idhrms.ToString());
               
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView(lstVouchers);
        }



        #endregion
        #region Lista de registros de consumo por unidad
        [Authorize]

        public ActionResult VouchersListByCar(string idUnidad)
        {
            Session["Vunidad"] = idUnidad;
            //Claves.ClaveUnidad = idUnidad;
            Entities.ViewModels.VistaUnidadesConsumo unidad = new Entities.ViewModels.VistaUnidadesConsumo();
            List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();
            try
            {
                if (Session["sListaUnidades"] != null)
                {
                    lstCars = (List<Entities.ViewModels.VistaUnidadesConsumo>) Session["sListaUnidades"];

                    unidad = lstCars.FirstOrDefault(x => x.IdUnidad == idUnidad);
                    Session["sListaUnidades1"] = unidad;

                }


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View("VouchersListByCar", unidad);
        }
        public ActionResult MostrarPopup()
        {
            return PartialView("_PopupConsumo");
        }
        public JsonResult VouchersListByCarjson(string idUnidad)
        {
            //Claves.ClaveUnidad = idUnidad;
            Entities.ViewModels.VistaUnidadesConsumo unidad = new Entities.ViewModels.VistaUnidadesConsumo();
            List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();
            try
            {
                if (Session["sListaUnidades"] != null)
                {
                    lstCars = (List<Entities.ViewModels.VistaUnidadesConsumo>)Session["sListaUnidades"];

                    unidad = lstCars.FirstOrDefault(x => x.IdUnidad == idUnidad);
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return Json(new { data = unidad }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult VouchersListByCarPartial(string idUnidad)
        {
            
             string sIdUnidad= string.Empty;
            List<Entities.ConsumptionClaro> lstVouchers = new List<Entities.ConsumptionClaro>();
            //if (idUnidad == null)
            //{
            //    idUnidad = Claves.ClaveUnidad;
            //}
            if (idUnidad != null)
            {
                Session[keyUnidadConsumo] = idUnidad;


            }


            sIdUnidad = (string) Session[keyUnidadConsumo];
            

            try
            {
                //lstVouchers = Data.Consumo.ObtenerConsumoPorUnidad(idUnidad);
                 lstVouchers = Data.Consumo.ObtenerConsumoPorUnidad(sIdUnidad); 
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView("VouchersListByCarPartial", lstVouchers.ToList().AsEnumerable());
        }
        public JsonResult VouchersListByCarPartialjson(string idUnidad)
        {

            string sIdUnidad = string.Empty;
            List<Entities.ConsumptionClaro> lstVouchers = new List<Entities.ConsumptionClaro>();
            //if (idUnidad == null)
            //{
            //    idUnidad = Claves.ClaveUnidad;
            //}
            if (idUnidad != null)
            {
                Session[keyUnidadConsumo] = idUnidad;


            }
         

            sIdUnidad = (string)Session["Vunidad"];


            try
            {
                //lstVouchers = Data.Consumo.ObtenerConsumoPorUnidad(idUnidad);
                lstVouchers = Data.Consumo.ObtenerConsumoPorUnidad(sIdUnidad);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return Json(new { data = lstVouchers }, JsonRequestBehavior.AllowGet);
        }


        #endregion
        #region Lista de registros de traslados pot unidad

        public ActionResult TransfersList(string idUnidad)
        {
            Entities.ViewModels.VistaUnidadesConsumo unidad = new Entities.ViewModels.VistaUnidadesConsumo();
            List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();
            try
            {
                if (Session["sListaUnidades"] != null)
                {
                    lstCars = (List<Entities.ViewModels.VistaUnidadesConsumo>)Session["sListaUnidades"];

                    unidad = lstCars.FirstOrDefault(x => x.IdUnidad == idUnidad);
                }
                Session["Vunidad"]=idUnidad;


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(unidad);
        }
        public JsonResult TransfersListjson( )
        {
            List<VistaTraslados> lstTraslados = new List<VistaTraslados>();

            try
            {
                

                string  sIdUnidad = (string)Session["Vunidad"];

                  lstTraslados = Data.Traslado.ObtenerTraslados(sIdUnidad);


            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return Json(new { data = lstTraslados }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult TransfersListPartial(string idUnidad)

        {
            string sIdUnidad = string.Empty;
            List<VistaTraslados> lstTraslados = new List<VistaTraslados>();

            if (idUnidad != null)
            {
                Session[keyUnidadConsumo] = idUnidad;
            }


            sIdUnidad = (string)Session[keyUnidadConsumo];


            try
            {

                ////Obtener persona que se loguea en el sistema
                //Entities.Employees eEmployee = null;
                //if (Session["User"] != null)
                //{
                //    eEmployee = (Entities.Employees)Session["User"];
                //}
                lstTraslados = Data.Traslado.ObtenerTraslados(sIdUnidad);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView(lstTraslados);
        }

        #endregion
        #region Lista de extra planes
        public ActionResult ExtraPlanList(string idUnidad)
        {
            Entities.ViewModels.VistaUnidadesConsumo unidad = new Entities.ViewModels.VistaUnidadesConsumo();
             List<Entities.ViewModels.VistaUnidadesConsumo> lstCars = new List<Entities.ViewModels.VistaUnidadesConsumo>();
            //try
            //{
               Session["Vunidad"] = idUnidad   ;
             //    {
                   lstCars = (List<Entities.ViewModels.VistaUnidadesConsumo>)Session["sListaUnidades"];

                   unidad = lstCars.FirstOrDefault(x => x.IdUnidad == idUnidad);
            Session["sListaUnidades1"] = unidad;
            //    }


            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("Se ha producido el siguiente error ", ex);
            //}
            return View( );

        }
        public JsonResult ExtraPlanListJson()
        {
            string sIdUnidad = (string)Session["Vunidad"];

            List<Entities.ViewModels.VistaExtraPlan> lstTraslados = new List<Entities.ViewModels.VistaExtraPlan>();
             try
            {

                ////Obtener persona que se loguea en el sistema
                //Entities.Employees eEmployee = null;
                //if (Session["User"] != null)
                //{
                //    eEmployee = (Entities.Employees)Session["User"];
                //}

                lstTraslados = Data.ExtraPlan.ListarExtraPlanes(sIdUnidad);
            
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return Json(new { data = lstTraslados }, JsonRequestBehavior.AllowGet);

        
        }
        public ActionResult ExtraPlanListPartial(string idUnidad)

        {
            List<Entities.ViewModels.VistaExtraPlan> lstExtraPlan = new List<Entities.ViewModels.VistaExtraPlan>();


            try
            {
                string sIdUnidad = string.Empty;
                List<VistaTraslados> lstTraslados = new List<VistaTraslados>();

                if (idUnidad != null)
                {
                    Session[keyUnidadConsumo] = idUnidad;
                }


                sIdUnidad = (string)Session[keyUnidadConsumo];
                ////Obtener persona que se loguea en el sistema
                //Entities.Employees eEmployee = null;
                //if (Session["User"] != null)
                //{
                //    eEmployee = (Entities.Employees)Session["User"];
                //}

                lstExtraPlan = Data.ExtraPlan.ListarExtraPlanes(sIdUnidad);
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }

            return PartialView(lstExtraPlan);
        }



        #endregion
        #region Acciones de crud de consumo por empleado
       

        public ActionResult EditarConsumo(int idCombustibleConsumo)
        {
            string mensajeValidacion = string.Empty;
            ConsumptionClaro consumo = new ConsumptionClaro();
            consumo.IdCombustibleConsumoClaro = idCombustibleConsumo;

  
              //Instancia de la clase de validacion de consumo
                Consumo vConsumo = new Consumo();
            mensajeValidacion = vConsumo.ValidarEstadoConsumo(consumo);
            try
            {

                if (mensajeValidacion == "ok")
                {
                    var resultado = Data.Consumo.ObtenerConsumoPorId(idCombustibleConsumo);

                    if (resultado != null)
                    {
                        consumo.IdCombustibleConsumoClaro = resultado.IdCombustibleConsumoClaro;
                        consumo.IdVoucher = resultado.IdVoucher;
                        consumo.FechaRegistro = DateTime.Parse(resultado.FechaRegistro.ToShortDateString());
                        consumo.IdUnidad = resultado.IdUnidad;
                        consumo.CantidadLitros = resultado.CantidadLitros;
                        consumo.IdCombustibleProveedor = resultado.IdCombustibleProveedor;
                        consumo.IdTipoCombustible = resultado.IdTipoCombustible;
                        consumo.ValorCordobas = resultado.ValorCordobas;
                        consumo.IdDepartamento = resultado.IdDepartamento;
                        consumo.PrecioLitros = resultado.PrecioLitros;
                        consumo.Estacion = resultado.Estacion;
                        consumo.OdometroInicial = resultado.OdometroInicial;
                        consumo.Municipio = resultado.Municipio;
                      

                    }

                    return Json(new { status = "Exito", data = consumo }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { status = "Error", data = mensajeValidacion }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                return Json(new { status = "Error", data = "Ha ocurrido un error en la transaccion." }, JsonRequestBehavior.AllowGet);
            }


        }

       
        /// <summary>
        /// Accion que guarda el consumo por empleado
        /// </summary>
        /// <param name="consumo"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult GuardarConsumo(Entities.ConsumptionClaro consumo)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string mensajeValidacion = string.Empty;
            //Instancia de la clase de validacion de consumo
            Consumo vConsumo = new Consumo();
         
            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(string.Empty);
            consumo.IpLocal = ipLocal;
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(string.Empty);
            consumo.UsuarioDominio = usuarioDominio;
            //Obtener periodo de transaccion.
            var periodos =
                Data.CombustiblePeriodo.ObtenerCombustiblePeirodosPorFecha(
                    consumo.FechaRegistro.ToString("yyyy/MM/dd")).FirstOrDefault();
            if (periodos != null)
            {
                consumo.IdCombustiblePeriodo = periodos.IdCombustiblePeriodo;
            }
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
                consumo.IdPersona = eEmployee.Idhrms.ToString();
            }
          

            try
            {
                mensajeValidacion = vConsumo.ValidarConsumo(consumo);

                if (mensajeValidacion == "ok")
                {

                    if (consumo.IdCombustibleConsumoClaro == -1)
                    {


                        result = Data.Consumo.InsertarConsumo(consumo);
                        if (result != "EXITO")
                        {

                            return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });


                        }
                    }

                    else
                    {
                        result = Data.Consumo.ActualizarConsumo(consumo);
                        if (result != "EXITO")
                        {

                            return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });


                        }

                    }


                }
                else
                {
                    return Json(new { status = "Error", message = mensajeValidacion });
                }


            }
            catch (Exception ex)
            {

                return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });
            }



            return Json(new { status = "Exito", message = "El registro ha sido actualizado con éxito." });

           
        }
        #endregion
        #region Acciones CRUD de consumo por unidad

        public ActionResult EditarConsumoPorUnidad(int idCombustibleConsumo)
        {
            string mensajeValidacion = string.Empty;
            ConsumptionClaro consumo = new ConsumptionClaro();
            consumo.IdCombustibleConsumoClaro = idCombustibleConsumo;


            //Instancia de la clase de validacion de consumo
            Consumo vConsumo = new Consumo();
            mensajeValidacion = vConsumo.ValidarEstadoConsumo(consumo);
            try
            {

                if (mensajeValidacion == "ok")
                {
                    var resultado = Data.Consumo.ObtenerConsumoPorId(idCombustibleConsumo);

                    if (resultado != null)
                    {
                        consumo.IdCombustibleConsumoClaro = resultado.IdCombustibleConsumoClaro;
                        consumo.IdVoucher = resultado.IdVoucher;
                        consumo.FechaRegistro = DateTime.Parse(resultado.FechaRegistro.ToShortDateString());
                        consumo.IdUnidad = resultado.IdUnidad;
                        consumo.CantidadLitros = resultado.CantidadLitros;
                        consumo.IdCombustibleProveedor = resultado.IdCombustibleProveedor;
                        consumo.IdTipoCombustible = resultado.IdTipoCombustible;
                        consumo.ValorCordobas = resultado.ValorCordobas;
                        consumo.IdDepartamento = resultado.IdDepartamento;
                        consumo.PrecioLitros = resultado.PrecioLitros;
                        consumo.Estacion = resultado.Estacion;
                        consumo.OdometroInicial = resultado.OdometroInicial;
                        consumo.Municipio = resultado.Municipio;


                    }

                    return Json(new { status = "Exito", data = consumo }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { status = "Error", data = mensajeValidacion }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                return Json(new { status = "Error", data = "Ha ocurrido un error en la transaccion." }, JsonRequestBehavior.AllowGet);
            }


        }

        /// <summary>
        /// Accion que carga la vista de edicion de consumo por unidad
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        //public ActionResult EditarConsumoPorUnidad(int IdCombustibleConsumoClaro = -1)
        //{

        //    //Session[keyIdCombustibleConsumo] = IdCombustibleConsumoClaro;
        //    //string idUnidad = (string)Session[keyUnidadConsumo];

        //    Entities.ConsumptionClaro consumo =
        //        Data.Consumo.ObtenerConsumoPorId(IdCombustibleConsumoClaro);
        //    if (consumo.IdCombustibleConsumoClaro == 0)
        //    {
        //        DateTime fechaActual = DateTime.Today;
        //        consumo = new Entities.ConsumptionClaro();
        //        consumo.IdCombustibleConsumoClaro = -1;
        //        consumo.FechaRegistro = fechaActual;


        //    }

        //    return View("EditCar", consumo);
        //}
        /// <summary>
        /// Accion que guarda el consumo por empleado
        /// </summary>
        /// <param name="consumo"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult GuardarConsumoPorUnidad(Entities.ConsumptionClaro consumo)
        {


            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string mensajeValidacion = string.Empty;
            //Instancia de la clase de validacion de consumo
            Consumo vConsumo = new Consumo();

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(string.Empty);
            consumo.IpLocal = ipLocal;
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(string.Empty);
            consumo.UsuarioDominio = usuarioDominio;
            //Obtener periodo de transaccion.
            var periodos =
                Data.CombustiblePeriodo.ObtenerCombustiblePeirodosPorFecha(
                    consumo.FechaRegistro.ToString("yyyy/MM/dd")).FirstOrDefault();
            if (periodos != null)
            {
                consumo.IdCombustiblePeriodo = periodos.IdCombustiblePeriodo;
            }
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
                consumo.IdPersona = eEmployee.Idhrms.ToString();
            }


            try
            {
                mensajeValidacion = vConsumo.ValidarConsumo(consumo);

                if (mensajeValidacion == "ok")
                {

                    if (consumo.IdCombustibleConsumoClaro <1  )
                    {


                        result = Data.Consumo.InsertarConsumo(consumo);
                        if (result != "EXITO")
                        {

                            return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });


                        }
                    }

                    else
                    {
                        result = Data.Consumo.ActualizarConsumo(consumo);
                        if (result != "EXITO")
                        {

                            return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });


                        }

                    }


                }
                else
                {
                    return Json(new { status = "Error", message = mensajeValidacion });
                }


            }
            catch (Exception ex)
            {

                return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });
            }



            return Json(new { status = "Exito", message = "El registro ha sido actualizado con éxito." });

          
        }
        public JsonResult GuardarConsumoPorUnidadprueba(Entities.ConsumptionClaro consumo)
        {

            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string mensajeValidacion = string.Empty;
            //Instancia de la clase de validacion de consumo
            Consumo vConsumo = new Consumo();

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(string.Empty);
            consumo.IpLocal = ipLocal;
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(string.Empty);
            consumo.UsuarioDominio = usuarioDominio;
            //Obtener periodo de transaccion.
            var periodos =
                Data.CombustiblePeriodo.ObtenerCombustiblePeirodosPorFecha(
                    consumo.FechaRegistro.ToString("yyyy/MM/dd")).FirstOrDefault();
            if (periodos != null)
            {
                consumo.IdCombustiblePeriodo = periodos.IdCombustiblePeriodo;
            }
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
                consumo.IdPersona = eEmployee.Idhrms.ToString();
            }


            try
            {
                mensajeValidacion = vConsumo.ValidarConsumo(consumo);

                if (mensajeValidacion == "ok")
                {

                    if (consumo.IdCombustibleConsumoClaro < 1)
                    {


                        result = Data.Consumo.InsertarConsumo(consumo);
                        if (result != "EXITO")
                        {

                            return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });


                        }
                    }

                    else
                    {
                        result = Data.Consumo.ActualizarConsumo(consumo);
                        if (result != "EXITO")
                        {

                            return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });


                        }

                    }


                }
                else
                {
                    return Json(new { status = "Error", message = mensajeValidacion });
                }


            }
            catch (Exception ex)
            {

                return Json(new { status = "Error", message = "Ocurrió un error al actualizar la información. Por favor verifique los datos y vuelva a intentarlo." });
            }



            return Json(new { status = "Exito", message = "El registro ha sido actualizado con éxito." });



 

        }



        /// <summary>
        /// Metodo para eliminar un extraplan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AnularConsumo(int idCombustibleConsumoClaro)
        {

            string result = String.Empty;
            string ip = string.Empty;
            string usuario = String.Empty;

            var estado = Data.Consumo.ObtenerEstadoConsumoPorId(idCombustibleConsumoClaro); //Data.Consumo.ObtenerConsumoPorId(idCombustibleConsumoClaro);

            if (estado!= null)
            {
             
                if (estado.IdEstado != "1501")
                {
                    return Json(new { status = "Error", message = "Solo se pueden anular consumos en estado REGISTRADO" });
                }
            }
            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }


            result = Data.Consumo.AnularConsumo(idCombustibleConsumoClaro, ipLocal, eEmployee.Idhrms.ToString(),
                usuarioDominio);

            if (result != "EXITO")
            {


                return Json(new { status = "Error", message = "Error en la anulación del consumo" });


            }
            return Json(new { status = "Exito", message = "El consumo ha sido anulado" });
        }


        #endregion
        #region Acciones del CRUD de Traslados

       
        //public ActionResult ObtenerUnidadesPorProveedor(int? idProveedor) //string textField, string valueField)
        //{
        //    try
        //    {
        //        return GridViewExtension.GetComboBoxCallbackResult(p =>
        //        {
        //            p.TextField = "DescripcionUnidad";
        //            p.ValueField = "IdUnidad";
        //            p.ValueType = typeof(string);
        //            if (string.IsNullOrEmpty(idProveedor.ToString()))
        //                p.BindList(Data.Consumo.ObtenerDetalleUnidades());
        //            else
        //            {
        //                p.BindList(Data.Consumo.ObtenerDetalleUnidadesPorProveedor(idProveedor.ToString()));
        //            }
        //            //p.BindList(Data.Consumo.ObtenerListaUnidadesPorProveedor(null, proveedorId.GetValueOrDefault()));
        //        });
        //    }
        //    catch (Exception e)
        //    {
                
        //        throw new Exception(e.Message);
        //    }
          
        //}
        //[HttpGet]
        public ActionResult ObtenerSaldoUnidad(string idUnidad)
        {
            List<Entities.ViewModels.AssignmentCarsView> lstCars = new List<Entities.ViewModels.AssignmentCarsView>();
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            lstCars = Data.ExtraPlan.GetCarsByManagement();
            var unidad = lstCars.Where(x => x.IdUnidad == idUnidad).FirstOrDefault();
            if (unidad != null)
            {
                string saldoUnidad = unidad.SaldoCombustible.ToString();  // lstCars.Where(x => x.IdUnidad == idUnidad).FirstOrDefault().SaldoCombustible;
                return Json(saldoUnidad, JsonRequestBehavior.AllowGet);
            }
            else
            {
                decimal saldoUnidad = 0;
                return Json(saldoUnidad, JsonRequestBehavior.AllowGet);
            }



        }

        /// <summary>
        /// Metodo para validar el traslado
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        [HttpPost]

        public JsonResult ValidarTraslado(string idTraslado,string tipoEdicion)
        {

            string result = String.Empty;
            if (tipoEdicion != "Nuevo")
            {
                var estado = Data.Traslado.ObtenerTrasladoPorId(idTraslado);

                if (estado.Count > 0)
                {
                    string estadoTraslado = estado.FirstOrDefault().Estado;
                    if (estadoTraslado != "REGISTRADO")
                    {
                        EditarTraslado(idTraslado);
                        return Json(new { status = "Error", message = "Solo se pueden editar traslados en estado REGISTRADO" });
                    }
                }
            }

            return Json(new { status = "Exito", message = "El extra plan ha sido eliminado" });
        }
        public JsonResult ValidarTrasladojson(string idTraslado, string tipoEdicion)
        {

            string result = String.Empty;
            if (tipoEdicion != "Nuevo")
            {
                var estado = Data.Traslado.ObtenerTrasladoPorId(idTraslado);

                if (estado.Count > 0)
                {
                    string estadoTraslado = estado.FirstOrDefault().Estado;
                    if (estadoTraslado != "REGISTRADO")
                    {
                        EditarTraslado(idTraslado);
                        return Json(new { status = "Error", message = "Solo se pueden editar traslados en estado REGISTRADO" });
                    }
                }
            }

            return Json(new { status = "Exito", message = "El extra plan ha sido eliminado" });
        }
        /// <summary>
        /// Accion que carga la informacion a la vista de encabezado del extra plan
        /// </summary>
        /// <param name="idTraslado"></param>
        /// <returns></returns>
        public ActionResult EditarTraslado(string idTraslado = "-1")
        {
            if (idTraslado!="-1" && idTraslado.Contains("TRS") == false)
            {
                if (idTraslado.Length < 6)
                {
                    // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                    idTraslado = idTraslado.PadLeft(6, '0');
                }
                idTraslado = "TRS-" + idTraslado;
            }
            Session[keyIdCombustibleTraslado] = idTraslado;

            Entities.ViewModels.VistaTraslados editarTraslado = Data.Traslado.ObtenerTrasladoPorId(idTraslado).FirstOrDefault();
            if (editarTraslado == null)
            {
                DateTime fechaActual = DateTime.Today;
                editarTraslado = new Entities.ViewModels.VistaTraslados();
                editarTraslado.IdCombustibleTraslado = "-1";
                editarTraslado.FechaTraslado = fechaActual;
                Session.Remove("sDetalleTraslado");
            }
          
            return View("EditTransfer", editarTraslado);
        }
        /// <summary>
        /// Accion que carga  el detalle del traslado
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        public ActionResult EditDetailTransfer()
        {
            string idTraslado = (string)Session[keyIdCombustibleTraslado];

            List<Entities.ViewModels.VistaTrasladosDetalle> lstDetail = new List<Entities.ViewModels.VistaTrasladosDetalle>();

            try
            {

                lstDetail = Data.Traslado.ObtenerDetalleTrasladosPorId(idTraslado);

            }



            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


            return PartialView("EditDetailTransfer",lstDetail);
        }

        /// <summary>
        /// Metodo para guardar un traslado
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>
        public JsonResult SaveTraslado(Entities.ViewModels.VistaTraslados vTraslado)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string resultadoEstado = string.Empty;
            string ip = string.Empty;
            string usuario = String.Empty;
            Validations.Traslado valTraslado = new Validations.Traslado();
            bool esUnidadIgual, esNegativoSaldoOrigen;


            if (Data.Traslado.ObtenerDetalleTrasladosPorId("-1").Count == 0)
            {
                return Json(new { status = "Error", message = "Debe guardar primero el detalle del traslado" });
            }
            //Validar unidades de origen y destino iguales.
            esUnidadIgual = valTraslado.ValidarUnidadesIguales();
            if (esUnidadIgual == false)
            {
                return Json(new { status = "Error", message = "La unidad Origen y la unidad Destino no pueden ser iguales." });
            }
            //Validar unidades de origen con saldo negativo.
            esNegativoSaldoOrigen = valTraslado.ValidarSaldoNegativaOrigen();
            if (esNegativoSaldoOrigen == false)
            {
                return Json(new { status = "Error", message = "La unidad de origen no puede tener saldo negativo o cero." });
            }
            //if (vTraslado.IdCombustibleProveedor == 0)
            //{
            //    return Json(new { status = "Error", message = "Debe seleccionar el proveedor de combustible" });
            //}
            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

            //Obtener periodo de transaccion.
            var periodos =
                Data.CombustiblePeriodo.ObtenerCombustiblePeirodosPorFecha(
                    vTraslado.FechaTraslado.ToString("yyyy/MM/dd")).FirstOrDefault();
            if (periodos != null)
            {
                vTraslado.IdCombustiblePeriodo = periodos.IdCombustiblePeriodo;
            }



            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }



            //Asignar los datos del modelo a la entidad de dominio.
            Entities.Traslados traslado = new Traslados();

            traslado.IdCombustibleTraslado = vTraslado.IdCombustibleTraslado;
            traslado.IdPersona = eEmployee.Idhrms.ToString();
            traslado.FechaTraslado = vTraslado.FechaTraslado;
            traslado.FechaAnulacion = default(DateTime);
            traslado.IdCombustiblePeriodo = vTraslado.IdCombustiblePeriodo;
            //traslado.IdCombustibleProveedor = vTraslado.IdCombustibleProveedor;

            if (vTraslado.IdCombustibleTraslado == "-1")
            {

                result = Data.Traslado.InsertarTraslado(traslado);
                if (result.Length == 10)
                {
                    resultadoDetalle = Data.Traslado.GuardarDetalle(result);
                    if (resultadoDetalle.Length == 10)
                    {
                        Session.Remove("sDetalleTraslado");
                        Entities.TrasladosEstados trasladoEstado = new TrasladosEstados();
                        trasladoEstado.IdCombustibleTraslado = result;
                        trasladoEstado.IdEstado = "1801";
                        trasladoEstado.EsActivo = "Y";
                        trasladoEstado.IdPersona = eEmployee.Idhrms.ToString();
                        trasladoEstado.carnet = eEmployee.EmployeeNumber.ToString();
                        trasladoEstado.UsuarioDominioInserto = usuarioDominio;
                        trasladoEstado.IpLocal = ipLocal;
                        resultadoEstado = Data.TrasladoEstado.CambiarEstado(trasladoEstado);
                        if (resultadoEstado.Length == 10)
                        {
                            string resultadoCorreo = EnviarCorreo("TrasladoNuevo", result, traslado.FechaTraslado);
                            return Json(new { status = "Exito", message = "Exito al guardar el traslado" });
                        }


                    }

                }
            }

            else
            {
                result = Data.Traslado.ActualizarTraslado(traslado);
                if (result.Length == 10)
                {
                    resultadoDetalle = Data.Traslado.GuardarDetalle(result);
                    if (resultadoDetalle.Length == 10)
                    {
                        Session.Remove("sDetalleTraslado");
                        return Json(new { status = "Exito", message = "Exito al actualizar el traslado" });

                    }

                }

            }

            return Json(new { status = "Error", message = "Error en la transaccion" });

        }


        /// <summary>
        /// Accion para guardar el detalle del traslado en la sesion.
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>

        public ActionResult EditarDetalleTraslado(MVCxGridViewBatchUpdateValues<Entities.ViewModels.VistaTrasladosDetalle> updateValues)
        {

            //Actualizacion dedel detalle en la sesion
            foreach (var item in updateValues.Update)
            {
                if (updateValues.IsValid(item))
                {
                    try
                    {


                        Data.Traslado.EditSessionDetail(item);




                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                }

            }
            //Actualizacion dedel detalle en la sesion
            foreach (var item in updateValues.Insert)
            {
                if (updateValues.IsValid(item))
                {
                    try
                    {


                        Data.Traslado.AddSessionDetail(item);



                        ////Llamar al metodo EditGoal
                        //SafeExecute(() => Data.EmployeeGoal.EditGoal(item));

                    }
                    catch (Exception e)
                    {
                        ViewData["EditError"] = e.Message;
                    }
                }

            }

            //Elimiar linea de detalle de l sesion
            foreach (var id in updateValues.DeleteKeys)
            {
                string result;
                try
                {
                    var editableItem = Data.Traslado.ObtenerDetalleTrasladosPorId("-1")
                        .Where(x => x.IdCombustibleDetalleTraslado == int.Parse(id))
                        .FirstOrDefault();

                    if (editableItem.IsBdRecord == 1)
                    {
                        result = Data.Traslado.DeleteBdDetail(int.Parse(id));
                        if (result != "EXITO")
                        {
                            return Content("Error al eliminar el detalle");
                        }
                    }
                    else
                    {

                        Data.Traslado.DeleteBdDetail(int.Parse(id));
                    }




                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }



            }



            return EditDetailTransfer();
        }

        /// <summary>
        /// Metodo para eliminar un extraplan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AnularTraslado(string idTraslado)
        {
            if (idTraslado.Contains("TRS") == false)
            {
                if (idTraslado.Length < 6)
                {
                    // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                    idTraslado = idTraslado.PadLeft(6, '0');
                }
                idTraslado = "TRS-" + idTraslado;
            }
            string result = String.Empty;
            result = idTraslado;
            var estado = Data.Traslado.ObtenerTrasladoPorId(idTraslado);

            if (estado.Count > 0)
            {
                string estadoTraslado = estado.FirstOrDefault().Estado;
                if (estadoTraslado != "REGISTRADO")
                {
                    return Json(new { status = "Error", message = "Solo se pueden anular traslados en estado REGISTRADO" });
                }
            }
            string ip = string.Empty;
            string usuario = String.Empty;

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Entities.TrasladosEstados trasladoEstado = new TrasladosEstados();
            trasladoEstado.IdCombustibleTraslado = result;
            trasladoEstado.IdEstado = "1803";
            trasladoEstado.EsActivo = "Y";
            trasladoEstado.IdPersona = eEmployee.Idhrms.ToString();
            trasladoEstado.UsuarioDominioInserto = usuarioDominio;
            trasladoEstado.IpLocal = ipLocal;
            result = Data.TrasladoEstado.CambiarEstado(trasladoEstado);

            if (result.Length != 10)
            {


                return Json(new { status = "Error", message = "Error en la anular del extraplan" });


            }
            string resultadoCorreo = EnviarCorreo("TrasladoAnular", idTraslado, estado.FirstOrDefault().FechaTraslado);
            return Json(new { status = "Exito", message = "El extra plan ha sido anulado" });
        }



        #endregion
        #region Acciones CRUD ExtraPlan
        /// <summary>
        /// Metodo para validar un extra plan
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>

        [HttpPost]
        public JsonResult ValidarExtraPlan(string idExtraPlan,string tipoEdicion)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string resultadoEstado = string.Empty;
            string ip = string.Empty;
            string usuario = String.Empty;

            if (tipoEdicion != "Nuevo")
            {
                var estado = Data.ExtraPlan.ObtenerExtraPlanById(idExtraPlan);

                if (estado.Count > 0)
                {
                    string estadoExtraPlan = estado.FirstOrDefault().Estado;
                    if (estadoExtraPlan != "1901")
                    {

                        return Json(new { status = "Error", message = "Solo se pueden editar extraplanes en estado de REGISTRADO" });
                    }
                }
            }

            return Json(new { status = "Exito", message = "" });
 
        }

        /// <summary>
        /// Accion que carga la informacion a la vista de encabezado del extra plan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        public ActionResult EditExtraPlan(string idExtraPlan = "-1")
        {
            if (idExtraPlan!=null && idExtraPlan!="-1" && idExtraPlan!="")
            {

                if (idExtraPlan.Contains("EXP") == false)
                {
                    if (idExtraPlan.Length < 6)
                    {
                        // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                        idExtraPlan = idExtraPlan.PadLeft(6, '0');
                    }
                    idExtraPlan = "EXP-" + idExtraPlan;
                }
            }

            Session[keyIdCombustibleExtraPlan] = idExtraPlan;

            Entities.ViewModels.VistaExtraPlan editExtraPlan = Data.ExtraPlan.ObtenerExtraPlanById(idExtraPlan).FirstOrDefault();
            if (editExtraPlan == null)
            {
                DateTime fechaActual = DateTime.Today;
                editExtraPlan = new Entities.ViewModels.VistaExtraPlan();
                editExtraPlan.IdCombustibleExtraPlan = "-1";
                editExtraPlan.FechaSolicitud = fechaActual;
                Session.Remove("sDetailExtraPlan");
            }
         
            
            return View("EditExtraPlan", editExtraPlan);
        }
        /// <summary>
        /// Accion que carga  el detalle del extra plan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        public ActionResult EditDetailExtraPlan(string idExtraPlan = "-1")
        {
            idExtraPlan = (string)Session[keyIdCombustibleExtraPlan];
            //Session.Remove("sDetailExtraPlan");
            List<Entities.ViewModels.ExtraPlanDetailView> lstDetailExtraPlan = new List<Entities.ViewModels.ExtraPlanDetailView>();

            try
            {

                lstDetailExtraPlan = Data.ExtraPlan.GetDetailExtraPlan(idExtraPlan);

            }



            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }


            return PartialView("EditDetailExtraPlan", lstDetailExtraPlan);
        }
        /// <summary>
        /// Metodo para guardar un extra plan
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>
        public JsonResult GuardarExtraPlan(Entities.ViewModels.VistaExtraPlan extraPlan)
        {
            string result = string.Empty;
            string resultadoDetalle = string.Empty;
            string resultadoEstado = string.Empty;
            string ip = string.Empty;
            string usuario = String.Empty;
            string esFechaDetalleDuplicada = string.Empty;
            //Asignar el idUnidad de la sesion en el objeto entidad extraPlan
            string sIdUnidad = (string)Session["Vunidad"];

            extraPlan.IdUnidad = sIdUnidad;
            //extraPlan.IdUnidad = (string)Session[keyUnidadConsumo];
            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

            //Obtener periodo de transaccion.
            var periodos =
                Data.CombustiblePeriodo.ObtenerCombustiblePeirodosPorFecha(
                    extraPlan.FechaSolicitud.ToString("yyyy/MM/dd")).FirstOrDefault();
            if (periodos != null)
            {
                extraPlan.IdCombustiblePeriodo = periodos.IdCombustiblePeriodo;
            }


            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            extraPlan.IdPersona = eEmployee.Idhrms.ToString();
            extraPlan.carnet = eEmployee.EmployeeNumber;
            extraPlan.NumeroEmpleado     = eEmployee.EmployeeNumber;
            //Validar total de filas
            int totalFilas = Data.ExtraPlan.GetDetailExtraPlan("-1").Count;

            if (totalFilas > 7)
            {

                return Json(new { status = "Error", message = "No se pueden guardar mas de una semana para un extra plan, por favor corrija." });
            }

            //Validar que las fechas no esten duplicadas.
            Validations.ExtraPlan vExtraPlan = new ExtraPlan();
            bool esFechaValida = false;
            esFechaValida = vExtraPlan.ValidarFechaDuplicada();
            if (esFechaValida == false)
            {
                return Json(new { status = "Error", message = "No se pueden grabar fechas duplicadas." });
            }

            //Validar que la unidad tiene el porcentaje de consumo maximo para solicitar un extra plan.
            string respuestaPorcentajeMaximo = Data.ExtraPlan.ObtenerRespuestaPorcentajeMaximo(extraPlan.IdUnidad,
                extraPlan.IdCombustiblePeriodo);
            if (!string.IsNullOrEmpty(respuestaPorcentajeMaximo))
            {
                return Json(new { status = "Error", message = respuestaPorcentajeMaximo });
            }

            //Validar que la fecha de solicitud no se repita
            bool esFechaSolicitudValida = false;
            esFechaSolicitudValida = vExtraPlan.ValidarFechaSolicitud(extraPlan.IdUnidad, extraPlan.FechaSolicitud, extraPlan.IdCombustibleExtraPlan);
            if (esFechaSolicitudValida == false)
            {
                return Json(new { status = "Error", message = "No se pueden grabar fechas de solicitud duplicadas." });
            }
            //Validar que la fecha de actividad actual para la unidad no se repita en las fechas de actividades de la base de datos.
            esFechaDetalleDuplicada = vExtraPlan.ValidarFechaDuplicadaDetalle(extraPlan.IdUnidad);
            if (!string.IsNullOrEmpty(esFechaDetalleDuplicada))
            {
                return Json(new { status = "Error", message = esFechaDetalleDuplicada });
            }
            //if (extraPlan.IdCombustibleProveedor == 0)
            //{

            //    return Json(new { status = "Error", message = "Debe seleccionar el proveedor de combustible." });
            //}

            if (extraPlan.IdCombustibleExtraPlan == "-1")
            {
                result = Data.ExtraPlan.InsertExtraPlan(extraPlan);
                if (result.Length == 10)
                {
                    resultadoDetalle = Data.ExtraPlan.SaveBdDetailExtraPlan(result);
                    if (resultadoDetalle.Length == 10)
                    {
                        Session.Remove("sDetailExtraPlan");
                        Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                        extraPlanEstado.IdCombustibleExtraPlan = result;
                        extraPlanEstado.IdEstado = "1901";
                        extraPlanEstado.EsActivo = "Y";
                        extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                        extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                        extraPlanEstado.IpLocal = ipLocal;
                        resultadoEstado = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);
                        if (resultadoEstado.Length == 10)
                        {
                            string resultadoCorreo = EnviarCorreo("ExtraPlanNuevo", result, extraPlan.FechaSolicitud);
                            return Json(new { status = "Exito", message = "Exito al guardar el extra plan" });
                        }

                    }

                }
            }

            else
            {

                result = Data.ExtraPlan.UpdateExtraPlan(extraPlan);
                if (result.Length == 10)
                {
                    resultadoDetalle = Data.ExtraPlan.SaveBdDetailExtraPlan(result);
                    if (resultadoDetalle.Length == 10)
                    {
                        Session.Remove("sDetailExtraPlan");
                        return Json(new { status = "Exito", message = "Exito al actualizar el registro" });

                    }

                }

            }

            return Json(new { status = "Error", message = "Ocurrió un error en la transaccion, por favor verifique." });

        }


        /// <summary>
        /// Metodo para guardar un extraplan
        /// </summary>
        /// <param name="extraPlan"></param>
        /// <returns></returns>

        public ActionResult BatchUpdateDetailExtraPlan(MVCxGridViewBatchUpdateValues<Entities.ViewModels.ExtraPlanDetailView> updateValues)
        {

            try
            {
                //Insert en la sesion 
                foreach (var item in updateValues.Insert)
                {
                    if (updateValues.IsValid(item))


                        Data.ExtraPlan.AddSessionDetail(item);
                }
                //Update en la sesión
                foreach (var item in updateValues.Update)
                {
                    if (updateValues.IsValid(item))
                        Data.ExtraPlan.EditSessionDetail(item);
                }

                // Delete en la sesion o en la Base de datos.
                foreach (var itemKey in updateValues.DeleteKeys)
                {
                    string result;
                    var editableItem = Data.ExtraPlan.GetDetailExtraPlan("-1")
                                .Where(x => x.IdCombustibleDetalleExtraPlan == int.Parse(itemKey))
                                .FirstOrDefault();

                    if (editableItem.IsBdRecord == 1)
                    {
                        result = Data.ExtraPlan.DeleteBdDetail(int.Parse(itemKey));
                        if (result != "EXITO")
                        {
                            return Content("Error al eliminar el detalle");
                        }
                    }
                    else
                    {

                        Data.ExtraPlan.DeleteSessionDetail(int.Parse(itemKey));
                    }
                }
            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

          



            return EditDetailExtraPlan("-1");
        }


        /// <summary>
        /// Accion para que llama a metodo para anular un extraplan
        /// </summary>
        /// <param name="idExtraPlan"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AnularExtraPlan(string idExtraPlan)
        {
            if (idExtraPlan.Contains("EXP") == false)
            {
                if (idExtraPlan.Length < 6)
                {
                    // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                    idExtraPlan = idExtraPlan.PadLeft(6, '0');
                }
                idExtraPlan = "EXP-" + idExtraPlan;
            }
            string result = String.Empty;
            
            string ip = string.Empty;
            string usuario = string.Empty;
            var estado = Data.ExtraPlan.ListarExtraPlanes(idExtraPlan);

            if (estado.Count > 0)
            {
                string estadoEx = estado.FirstOrDefault().Estado;
                if (estadoEx != "REGISTRADO")
                {
                    return Json(new { status = "Error", message = "Solo se pueden eliminar extra planes en estado REGISTRADO" });
                }
            }

            //Obtener IPlocal
            string ipLocal = Utils.ObtenerIpLocal(ip);
            //Obtener usuario de dominio
            string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }

            Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
            extraPlanEstado.IdCombustibleExtraPlan = idExtraPlan;
            extraPlanEstado.IdEstado = "1906";
            extraPlanEstado.EsActivo = "Y";
            extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
            extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
            extraPlanEstado.IpLocal = ipLocal;
            result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);



            if (result.Length != 10)
            {


                return Json(new { status = "Error", message = "Error en la eliminación del extraplan" });


            }
            string resultadoCorreo = EnviarCorreo("ExtraPlanAnular", idExtraPlan, estado.FirstOrDefault().FechaSolicitud);
            return Json(new { status = "Exito", message = "El extra plan ha sido eliminado" });
        }

        #endregion
        #region Autorizaciones de jefe ExtraPlan

        [Authorize]
        [HttpGet]

        public ActionResult AuthorizeBossExtraPlan()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                //Entities.Employees eEmployee = null;
                //if (Session["User"] != null)
                //{
                //    eEmployee = (Entities.Employees)Session["User"];
                //}
                //var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                //if (managementResult != null)
                //{
                //    string gerencia = managementResult.FirstOrDefault().Gerencia;
                //    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1901",
                //        eEmployee.Idhrms.ToString());

                //}

                //else
                //{
                //    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                //}



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstDetail);
        }
        public JsonResult autorizarExtraPlanListJson()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {

               
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1901",
                        eEmployee.Idhrms.ToString());

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return Json(new { data = lstDetail }, JsonRequestBehavior.AllowGet);



        }

        public JsonResult MautorizarExtraPlanListJson()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema

                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());    //eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    if (managementResult.Count() ==0)
                    {
                        lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(eEmployee.GERENCIAIDHRMS, "1902", eEmployee.Idhrms.ToString());

                    }
                    else
                    {
                        string gerencia = managementResult.FirstOrDefault().Gerencia;
                        lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902", eEmployee.Idhrms.ToString());
                    }

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return Json(new { data = lstDetail }, JsonRequestBehavior.AllowGet);



        }
        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeBossExtraPlanPartial()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1901",
                        eEmployee.Idhrms.ToString());

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeBossExtraPlanPartial", lstDetail);

        }
        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes pero destruyendo la sesion.
        /// </summary>
        /// <returns></returns>
        public ActionResult RefreshPartial()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1901",
                        eEmployee.Idhrms.ToString());

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeBossExtraPlanPartial", lstDetail);

        }

        /// <summary>
        /// Metodo para autorizar un extraplan por el jefe inmediato
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeBoss(string ids)
        {
            string result = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    //Obtener IPlocal
                    string ipLocal = Utils.ObtenerIpLocal(ip);
                    //Obtener usuario de dominio
                    string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

                    Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                    extraPlanEstado.IdCombustibleExtraPlan = keyAuthorize;
                    extraPlanEstado.IdEstado = "1902";
                    extraPlanEstado.EsActivo = "Y";
                    extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                    extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                    extraPlanEstado.IpLocal = ipLocal;
                    result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

            
                    if (result.Length != 10)
                    {


                        return Json(new { status = "Error", message = "Error en la autorizacion" });


                    }


                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }


        /// <summary>
        /// Metodo para denegar un extra plan por el jefe inmediato
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedBoss(string ids)
        {
            string result = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    //Obtener IPlocal
                    string ipLocal = Utils.ObtenerIpLocal(ip);
                    //Obtener usuario de dominio
                    string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

                    Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                    extraPlanEstado.IdCombustibleExtraPlan = keyAuthorize;
                    extraPlanEstado.IdEstado = "1907";
                    extraPlanEstado.EsActivo = "Y";
                    extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                    extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                    extraPlanEstado.IpLocal = ipLocal;
                    result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

                    if (result.Length != 10)
                    {


                        return Json(new { status = "Error", message = "Error en denegar el extra plan" });


                    }

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }


        #endregion
        #region Autorizaciones de gerente Extra Plan

        [Authorize]
        [HttpGet]

        public ActionResult AuthorizeManagementExtraPlan()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee("49430");
               // var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    //lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902",
                    //    eEmployee.Idhrms.ToString());
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902",
                    "49430");

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return View(lstDetail);
        }

        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes
        /// </summary>
        /// <returns></returns>
        public ActionResult AuthorizeManagementExtraPlanPartial()
        {
            Session.Remove("sExtraPlanAuthorize");
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee("49430");
                //var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    //lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902",
                    //    eEmployee.Idhrms.ToString());
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902",
                        "49430");

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeManagementExtraPlanPartial", lstDetail);

        }
        /// <summary>
        /// Accion que llama a metodo para mostrar lista extra planes pero destruyendo la sesion.
        /// </summary>
        /// <returns></returns>
        public ActionResult RefreshManagementPartial()
        {
            List<Entities.ViewModels.VistaExtraPlan> lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
            try
            {
                Session.Remove("sExtraPlanAuthorize");
                //Obtener persona que se loguea en el sistema
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }
                var managementResult = Data.ConsumptionClaro.GetManagementByEmployee(eEmployee.Idhrms.ToString());
                if (managementResult != null)
                {
                    string gerencia = managementResult.FirstOrDefault().Gerencia;
                    lstDetail = Data.ExtraPlan.GetExtraPlanForAuthorize(gerencia, "1902",
                        eEmployee.Idhrms.ToString());

                }

                else
                {
                    lstDetail = new List<Entities.ViewModels.VistaExtraPlan>();
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Se ha producido el siguiente error ", ex);
            }
            return PartialView("AuthorizeManagementExtraPlanPartial", lstDetail);

        }

        /// <summary>
        /// Metodo para autorizar un extraplan por el gerente
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AuthorizeManagement(string ids)
        {
            string result = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    //Obtener IPlocal
                    string ipLocal = Utils.ObtenerIpLocal(ip);
                    //Obtener usuario de dominio
                    string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

                    Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                    extraPlanEstado.IdCombustibleExtraPlan = keyAuthorize;
                    extraPlanEstado.IdEstado = "1903";
                    extraPlanEstado.EsActivo = "Y";
                    extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                    extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                    extraPlanEstado.IpLocal = ipLocal;
                    result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

                    if (result.Length != 10)
                    {


                        return Json(new { status = "Error", message = "Error en la autorizacion" });


                    }


                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en la autorización" });
        }


        /// <summary>
        /// Metodo para denegar un extra plan por el jefe inmediato
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult DeniedMangagement(string ids)
        {
            string result = string.Empty;
            string ip = string.Empty;
            string usuario = string.Empty;

            string keysET = ids + ",";

            //Validar que no venga vacia Keyset
            if (keysET != ",")
            {
                while (keysET.Trim().Length > 0)
                {
                    string keyAuthorize = keysET.Substring(0, keysET.IndexOf(","));
                    Entities.Employees eEmployee = null;
                    if (Session["User"] != null)
                    {
                        eEmployee = (Entities.Employees)Session["User"];
                    }
                    //Obtener IPlocal
                    string ipLocal = Utils.ObtenerIpLocal(ip);
                    //Obtener usuario de dominio
                    string usuarioDominio = Utils.ObtenerUsuarioDominio(usuario);

                    Entities.ExtraPlanEstados extraPlanEstado = new Entities.ExtraPlanEstados();
                    extraPlanEstado.IdCombustibleExtraPlan = keyAuthorize;
                    extraPlanEstado.IdEstado = "1908";
                    extraPlanEstado.EsActivo = "Y";
                    extraPlanEstado.IdPersona = eEmployee.Idhrms.ToString();
                    extraPlanEstado.UsuarioDominioInserto = usuarioDominio;
                    extraPlanEstado.IpLocal = ipLocal;
                    result = Data.ExtraPlanEstado.CambiarEstado(extraPlanEstado);

                    if (result.Length != 10)
                    {



                        return Json(new { status = "Error", message = "Error en denegar el extraplan" });


                    }

                    keysET = keysET.Substring(keysET.IndexOf(",") + 1);
                }

            }

            return Json(new { status = "Exito", message = "Exito en denegar registros" });
        }


        #endregion
        #region Consulta de Detalle Extra Plan de las autorizaciones

        public ActionResult AuthorizeConsult(string idExtraPlan)
        {

            List<Entities.ViewModels.ExtraPlanDetailView> lstExpenseDetail =
                new List<Entities.ViewModels.ExtraPlanDetailView>();

            lstExpenseDetail = Data.ExtraPlan.GetDetailExtraPlan(idExtraPlan);



            return View("AuthorizeConsult", lstExpenseDetail);
        }

        /// <summary>
        /// Accion que retorna la vista parcial AuthorizeConsultPartial
        /// </summary>
        /// <param name="_expenseId"></param>
        /// <returns></returns>
        public ActionResult AuthorizeConsultPartial(string idExtraPlan)
        {
            List<Entities.ViewModels.ExtraPlanDetailView> lstExpenseDetail =
                new List<Entities.ViewModels.ExtraPlanDetailView>();

            lstExpenseDetail = Data.ExtraPlan.GetDetailExtraPlan(idExtraPlan);


            return PartialView("AuthorizeConsultPartial", lstExpenseDetail);
        }

        #endregion
        #region Consulta de Estados Traslados

        public ActionResult TrasladosStateConsult(string idTraslado)
        {

            List<Entities.ViewModels.VistaTrasladosEstados> lstTraslados =
                new List<Entities.ViewModels.VistaTrasladosEstados>();

            lstTraslados = Data.Consumo.ObtenerEstadosTraslados(idTraslado);



            return View("TrasladosStateConsult", lstTraslados);
        }
        public JsonResult TrasladosStateConsultjson(string idTraslado)
        {
            if (idTraslado.Contains("TRS")==false)
            {
                if (idTraslado.Length < 6)
                {
                    // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                    idTraslado = idTraslado.PadLeft(6, '0');
                }
                idTraslado = "TRS-" + idTraslado;
            }

            List<Entities.ViewModels.VistaTrasladosEstados> lstTraslados =
                new List<Entities.ViewModels.VistaTrasladosEstados>();

            lstTraslados = Data.Consumo.ObtenerEstadosTraslados(idTraslado);



            return Json(new { data = lstTraslados }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Accion que retorna la vista parcial AuthorizeConsultPartial
        /// </summary>
        /// <param name="_expenseId"></param>
        /// <returns></returns>
        public ActionResult TrasladosStateConsultPartial(string idTraslado)
        {
            List<Entities.ViewModels.VistaTrasladosEstados> lstTraslados =
                new List<Entities.ViewModels.VistaTrasladosEstados>();

            lstTraslados = Data.Consumo.ObtenerEstadosTraslados(idTraslado);


            return PartialView("TrasladosStateConsultPartial", lstTraslados);
        }

        #endregion
        #region Consulta de Estados de Extra Plan

        public ActionResult ExtraPlanStateConsult(string idExtraPlan)
        {

            List<Entities.ViewModels.VistaExtraPlanEstados> lstExtraPlan =
                new List<Entities.ViewModels.VistaExtraPlanEstados>();

            lstExtraPlan = Data.Consumo.ObtenerEstadosExtraPlan(idExtraPlan);



            return View(lstExtraPlan);
        }

        /// <summary>
        /// Accion que retorna la vista parcial AuthorizeConsultPartial
        /// </summary>
        /// <param name="_expenseId"></param>
        /// <returns></returns>
        public ActionResult ExtraPlanStateConsultPartial(string idExtraPlan)
        {
            List<Entities.ViewModels.VistaExtraPlanEstados> lstExtraPlan =
                new List<Entities.ViewModels.VistaExtraPlanEstados>();

            lstExtraPlan = Data.Consumo.ObtenerEstadosExtraPlan(idExtraPlan);


            return PartialView("ExtraPlanStateConsultPartial", lstExtraPlan);
        }
        public JsonResult ExtraPlanStateConsultPartialjson(string idExtraPlan)
        {
            List<Entities.ViewModels.VistaExtraPlanEstados> lstExtraPlan =
                   new List<Entities.ViewModels.VistaExtraPlanEstados>();

            if (idExtraPlan.Contains("EXP") == false)
            {
                if (idExtraPlan.Length < 6)
                {
                    // Agrega ceros a la izquierda para que tenga al menos 6 dígitos
                    idExtraPlan = idExtraPlan.PadLeft(6, '0');
                }
                idExtraPlan = "EXP-" + idExtraPlan;
            }


            lstExtraPlan = Data.Consumo.ObtenerEstadosExtraPlan(idExtraPlan);



            return Json(new { data = lstExtraPlan }, JsonRequestBehavior.AllowGet);
        }
 
  
    #endregion
        #region Consulta de Estados de Consumo

    public ActionResult ConsumoStateConsult(int idConsumo)
        {

            List<Entities.ViewModels.VistaConsumoEstados> lstConsumo =
                new List<Entities.ViewModels.VistaConsumoEstados>();

            lstConsumo = Data.Consumo.ObtenerEstadosConsumo(idConsumo);



            return View(lstConsumo);
        }
        public JsonResult ConsumoStateConsultjson(int idConsumo)
        {

            List<Entities.ViewModels.VistaConsumoEstados> lstConsumo =
                new List<Entities.ViewModels.VistaConsumoEstados>();

            lstConsumo = Data.Consumo.ObtenerEstadosConsumo(idConsumo);



            return Json(new { data = lstConsumo }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Accion que retorna la vista parcial AuthorizeConsultPartial
        /// </summary>
        /// <param name="_expenseId"></param>
        /// <returns></returns>
        public ActionResult ConsumoStateConsultPartial(int idConsumo)
        {
            List<Entities.ViewModels.VistaConsumoEstados> lstConsumo =
                new List<Entities.ViewModels.VistaConsumoEstados>();

            lstConsumo = Data.Consumo.ObtenerEstadosConsumo(idConsumo);


            return PartialView("ConsumoStateConsultPartial", lstConsumo);
        }

        #endregion
        #region Envio de Correo
        public string EnviarCorreo(string tipoCorreo,string id, DateTime fecha)
        {
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
           // Envio del mensaje de correo al usuario
            //string destinatario = eEmployee.FullName;

            //string nombreDestinatario = eEmployee.FirstName;
          
            //string nombreDestinatarioMinuscula = nombreDestinatario.ToLower();
            //string nombreDestinatarioPrimeraMayuscula = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nombreDestinatarioMinuscula);
            
            
    
           
                if (tipoCorreo == "TrasladoNuevo")
                {
                   titulo = "Nuevo Traslado";
                   mensaje = "Estimada Transporte:" + 
                                     "<br/>" +
                                     "<br/>" + "Le informamos que el traslado " + id + ", solicitado por el usuario "+ eEmployee.FullName + " para la fecha" +
                                     " " + fecha.ToString("dd/MM/yyyy") + ", esta pendiente de autorizar. " +
                                     "<br/>" +
                                     "<br/>" +
                                     "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                                     "<br/>" +
                                     "<br/>" + "Saludos.";
                }
            if (tipoCorreo == "TrasladoAnular")
            {
                titulo = "Anulación Traslado";
                mensaje = "Estimada Transporte:" +
                          "<br/>" +
                          "<br/>" + "Le informamos que el traslado " + id + ", solicitado para la fecha" +
                          " " + fecha.ToString("dd/MM/yyyy") + ", ha sido anulado por el usuario " + eEmployee.FullName+
                          "<br/>" +
                          "<br/>" +
                          "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                          "<br/>" +
                          "<br/>" + "Saludos.";
            }
            if (tipoCorreo == "ExtraPlanNuevo")
                {
                    titulo = "Nuevo Extra Plan";
                     mensaje = "Estimada Transporte:" +
                                     "<br/>" +
                                     "<br/>" + "Le informamos que el extra plan " + id + ", solicitado +"+eEmployee.FullName+"+ para la fecha" +
                                     " " + fecha.ToString("dd/MM/yyyy") + ", esta pendiente por autorizar. " +
                                     "<br/>" +
                                     "<br/>" +
                                     "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                                     "<br/>" +
                                     "<br/>" + "Saludos.";
                }
            if (tipoCorreo == "ExtraPlanAnular")
            {
                titulo = "Anulación Extra Plan";
                mensaje = "Estimada Transporte:" +
                          "<br/>" +
                          "<br/>" + "Le informamos que el extra plan " + id + ", solicitado +" + eEmployee.FullName + "+ para la fecha" +
                          " " + fecha.ToString("dd/MM/yyyy") + ", ha sido anulado por el usuario. " +
                          "<br/>" +
                          "<br/>" +
                          "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                          "<br/>" +
                          "<br/>" + "Saludos.";
            }

            ClaroWCF = new ServiceReference1.ClaroAsemClient();
            resultadoCorreo = ClaroWCF.getcorreoenviar("transporte@claro.com.ni", titulo,  mensaje);
                if (resultadoCorreo != "EXITO")
                {
                    resultadoCorreo = "La transaccion se genero exitosamente, pero ocurrió un error al enviar el correo";
                    
                }
            
          
            return resultadoCorreo;
        }
     
   
        public string EnviarCorreoausenciamal(string tipoCorreo, string id, DateTime fecha)
        {
            string titulo = string.Empty;
            string mensaje = string.Empty;
            string resultadoCorreo = string.Empty;
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
            {
                eEmployee = (Entities.Employees)Session["User"];
            }
            // Envio del mensaje de correo al usuario
            //string destinatario = eEmployee.FullName;

            //string nombreDestinatario = eEmployee.FirstName;

            //string nombreDestinatarioMinuscula = nombreDestinatario.ToLower();
            //string nombreDestinatarioPrimeraMayuscula = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nombreDestinatarioMinuscula);




            if (tipoCorreo == "TrasladoNuevo")
            {
                titulo = "Nuevo Traslado";
                mensaje = "Estimada Transporte:" +
                                  "<br/>" +
                                  "<br/>" + "Le informamos que el traslado " + id + ", solicitado por el usuario " + eEmployee.FullName + " para la fecha" +
                                  " " + fecha.ToString("dd/MM/yyyy") + ", esta pendiente de autorizar. " +
                                  "<br/>" +
                                  "<br/>" +
                                  "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                                  "<br/>" +
                                  "<br/>" + "Saludos.";
            }
            if (tipoCorreo == "TrasladoAnular")
            {
                titulo = "Anulación Traslado";
                mensaje = "Estimada Transporte:" +
                          "<br/>" +
                          "<br/>" + "Le informamos que el traslado " + id + ", solicitado para la fecha" +
                          " " + fecha.ToString("dd/MM/yyyy") + ", ha sido anulado por el usuario " + eEmployee.FullName +
                          "<br/>" +
                          "<br/>" +
                          "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                          "<br/>" +
                          "<br/>" + "Saludos.";
            }
            if (tipoCorreo == "ExtraPlanNuevo")
            {
                titulo = "Nuevo Extra Plan";
                mensaje = "Estimada Transporte:" +
                                "<br/>" +
                                "<br/>" + "Le informamos que el extra plan " + id + ", solicitado +" + eEmployee.FullName + "+ para la fecha" +
                                " " + fecha.ToString("dd/MM/yyyy") + ", esta pendiente por autorizar. " +
                                "<br/>" +
                                "<br/>" +
                                "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                                "<br/>" +
                                "<br/>" + "Saludos.";
            }
            if (tipoCorreo == "ExtraPlanAnular")
            {
                titulo = "Anulación Extra Plan";
                mensaje = "Estimada Transporte:" +
                          "<br/>" +
                          "<br/>" + "Le informamos que el extra plan " + id + ", solicitado +" + eEmployee.FullName + "+ para la fecha" +
                          " " + fecha.ToString("dd/MM/yyyy") + ", ha sido anulado por el usuario. " +
                          "<br/>" +
                          "<br/>" +
                          "Gracias por utilizar RHOnline, esperamos que su experiencia con nosotros sea satisfactoria. " +
                          "<br/>" +
                          "<br/>" + "Saludos.";
            }

            ClaroWCF = new ServiceReference1.ClaroAsemClient();
            resultadoCorreo = ClaroWCF.getcorreoenviar("transporte@claro.com.ni", titulo, mensaje);
            if (resultadoCorreo != "EXITO")
            {
                resultadoCorreo = "La transaccion se genero exitosamente, pero ocurrió un error al enviar el correo";

            }


            return resultadoCorreo;
        }
        #endregion
        #region Reporte de Consumo
        public ViewResult ReportParameters()
        {
            Session.Remove("sConsumptionParameter");

            //ViewData["startPeriod"] = "01/01/2017";
            return View();
        }

        public ActionResult ConsumptionReport(Entities.MyEntities.ConsumptionParameters eParameter)
        {
            Session["sConsumptionParameters"] = eParameter;

            if (ModelState.IsValid)
            {
                try
                {
                    return View("ConsumptionByDate");
                }
                catch (Exception e)
                {
                    ViewData["EditError"] = e.Message;
                }
            }
            else
            {
                ViewData["EditError"] = "Error en la operacion";
            }

            return View("ReportParameters");
        }

        public ActionResult ConsumptionByDatePartial()
        {
            List<Entities.ViewModels.ConsumptionView> model = new List<Entities.ViewModels.ConsumptionView>();
            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                Entities.MyEntities.ConsumptionParameters consumptionParameter =
                            new Entities.MyEntities.ConsumptionParameters();
                consumptionParameter =
                    (Entities.MyEntities.ConsumptionParameters)Session["sConsumptionParameters"];


                var result = Data.Consumo.ObtenerReporteConsumoPorFechaYPersona(eEmployee.Idhrms.ToString(),
                    consumptionParameter.StatusId, consumptionParameter.StartDate, consumptionParameter.EndDate);


                if (result != null)
                {
                    foreach (var item in result)
                    {
                        Entities.ViewModels.ConsumptionView consumo = new Entities.ViewModels.ConsumptionView();
                        consumo.IdCombustibleConsumoClaro = item.IdCombustibleConsumoClaro;
                        consumo.IdVoucher = item.IdVoucher;
                        consumo.IdPersona = item.IdPersona;
                        consumo.IdUnidad = item.IdUnidad;
                        consumo.FechaRegistro = item.FechaRegistro;
                        consumo.CantidadLitros = item.CantidadLitros;
                        consumo.PrecioLitros = item.PrecioLitros;
                        consumo.ValorCordobas = item.ValorCordobas;
                        consumo.Estacion = item.Estacion;
                        consumo.IdTipoCombustible = item.IdTarjeta;
                        consumo.Municipio = item.Municipio;
                        consumo.IdDepartamento = item.IdDepartamento;
                        consumo.OdometroInicial = item.OdometroInicial;
                        consumo.NombreEmpleado = item.NombreEmpleado;
                        consumo.Cedula = item.Cedula;
                        consumo.Gerencia = item.Gerencia;
                        consumo.SubGerencia = item.SubGerencia;
                        consumo.FechaFin = consumptionParameter.EndDate;
                        consumo.FechaInicio = consumptionParameter.StartDate;

                        model.Add(consumo);
                    }

                }
                else
                {
                    ViewData["EditError"] = "Sesión nula";
                }

            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }

            return PartialView("ConsumptionByDatePartial", model);
        }

        public ActionResult ConsumptionByDateExport()
        {
            ConsumptionByDate report = new ConsumptionByDate();
            List<Entities.ViewModels.ConsumptionView> model = new List<Entities.ViewModels.ConsumptionView>();
            try
            {
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                {
                    eEmployee = (Entities.Employees)Session["User"];
                }

                Entities.MyEntities.ConsumptionParameters consumptionParameter =
                            new Entities.MyEntities.ConsumptionParameters();
                consumptionParameter =
                    (Entities.MyEntities.ConsumptionParameters)Session["sConsumptionParameters"];


                var result = Data.Consumo.ObtenerReporteConsumoPorFechaYPersona(eEmployee.Idhrms.ToString(),
                    consumptionParameter.StatusId, consumptionParameter.StartDate, consumptionParameter.EndDate);


                if (result != null)
                {
                    foreach (var item in result)
                    {
                        Entities.ViewModels.ConsumptionView consumo = new Entities.ViewModels.ConsumptionView();
                        consumo.IdCombustibleConsumoClaro = item.IdCombustibleConsumoClaro;
                        consumo.IdVoucher = item.IdVoucher;
                        consumo.IdPersona = item.IdPersona;
                        consumo.IdUnidad = item.IdUnidad;
                        consumo.FechaRegistro = item.FechaRegistro;
                        consumo.CantidadLitros = item.CantidadLitros;
                        consumo.PrecioLitros = item.PrecioLitros;
                        consumo.ValorCordobas = item.ValorCordobas;
                        consumo.Estacion = item.Estacion;
                        consumo.IdTipoCombustible = item.IdTarjeta;
                        consumo.Municipio = item.Municipio;
                        consumo.IdDepartamento = item.IdDepartamento;
                        consumo.OdometroInicial = item.OdometroInicial;
                        consumo.NombreEmpleado = item.NombreEmpleado;
                        consumo.Cedula = item.Cedula;
                        consumo.Gerencia = item.Gerencia;
                        consumo.SubGerencia = item.SubGerencia;
                        consumo.FechaFin = consumptionParameter.EndDate;
                        consumo.FechaInicio = consumptionParameter.StartDate;

                        model.Add(consumo);
                    }

                }


                report.DataSource = model;
    

            }
            catch (Exception e)
            {
                ViewData["EditError"] = e.Message;
            }



            return ReportViewerExtension.ExportTo(report);


        }
        public class Archiv
        {
            public int ID { get; set; }  // Identificador único del archivo
             public string Idunidad { get; set; }  // Nombre del archivo
            public string Referencia { get; set; }  // Tipo de archivo (e.g., imagen/webp)
            public string idusuario { get; set; }  // Tipo de archivo (e.g., imagen/webp)
            public byte[] Archivo { get; set; }  // Datos binarios del archivo
            public DateTime Fecha { get; set; }  // Fecha en la que se subió el archivo

        }


        #endregion
        #region inspeccion
        [HttpGet]
        public JsonResult getunidaddatos()
        {
            Entities.Employees eEmployee = null;
            if (Session["User"] != null)
                eEmployee = (Entities.Employees)Session["User"];

            if (eEmployee == null)
                return Json(null, JsonRequestBehavior.AllowGet);
            string idhrms = eEmployee.Idhrms.ToString();
            // Construcción del endpoint
            string apiUrl = $"http://172.26.54.66/apihcm/api/vehiculo/datos?token=021092&id={idhrms}";

            // Consumir la API con RestSharp
            var client = new RestClient(apiUrl);
            var request = new RestRequest(Method.GET);
            request.Timeout = -1;  // Sin límite de tiempo, según tu ejemplo
            List<UnidadInspeccionDto> expenses = new List<UnidadInspeccionDto>();
            try
            {
                var response = client.Execute(request);
                if (response == null)
                {
                    // Manejo de error
                    return Json(null, JsonRequestBehavior.AllowGet);
                }

                // Deserializar
                expenses = JsonConvert.DeserializeObject<List<UnidadInspeccionDto>>(response.Content);
                Session["actualizaciondatosvehicular"] = expenses;
            }
            catch (Exception e)
            {
                // Error de parseo
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            return Json(new { data = expenses }, JsonRequestBehavior.AllowGet);
        }
      
        [HttpGet]
        public ActionResult ObtenerInspeccion(string idVehiculo)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Obtener el registro de inspección más reciente para el vehículo, incluyendo IdInspeccion
                    string sql = @"
                SELECT TOP 1 
                    i.IdInspeccion,
                    i.IdVehiculo,
                    i.Kilometraje,
                    i.AsignadaA,
                    i.Domicilio,
                    i.Telefono,
                    i.PlantelAsignado,
                    i.FotoLicenciaFrontal,
                    i.FotoLicenciaPosterior,
                    i.PruebaManejo,
                    i.Departamento,
                    i.Edificio,
                    i.FechaRegistro,
                    i.Estado,i.Correo
                FROM Inspeccion i
                WHERE i.IdVehiculo = @idVehiculo
                ORDER BY i.FechaRegistro DESC;
            ";

                    var inspeccion = connection.QuerySingleOrDefault(sql, new { idVehiculo });
                    if (inspeccion != null)
                    {
                        // Convertir imagen frontal a Base64 (si existe)
                        string fotoFrontalBase64 = null;
                        if (inspeccion.FotoLicenciaFrontal != null)
                        {
                            byte[] fotoBytes = (byte[])inspeccion.FotoLicenciaFrontal;
                            fotoFrontalBase64 = "data:image/webp;base64," + Convert.ToBase64String(fotoBytes);
                        }
                        // Convertir imagen posterior a Base64 (si existe)
                        string fotoPosteriorBase64 = null;
                        if (inspeccion.FotoLicenciaPosterior != null)
                        {
                            byte[] fotoBytes = (byte[])inspeccion.FotoLicenciaPosterior;
                            fotoPosteriorBase64 = "data:image/webp;base64," + Convert.ToBase64String(fotoBytes);
                        }

                        // Obtener conductores registrados (cedulas)
                        string sqlConductores = @"
                    SELECT Conductor 
                    FROM InspeccionConductor 
                    WHERE IdInspeccion = @idInspeccion";
                        var conductoresList = connection.Query<string>(sqlConductores, new { idInspeccion = inspeccion.IdInspeccion }).ToList();

                        // Obtener el listado de empleados de sesión para enriquecer la información de conductores
                        List<EmpleadoInspeccion> lstEmpleado = new List<EmpleadoInspeccion>();
                        if (Session["sEmployeestodoempleado2"] != null)
                        {
                            lstEmpleado = (List<EmpleadoInspeccion>)Session["sEmployeestodoempleado2"];
                        }
                        var conductoresDetallados = conductoresList.Select(cedula =>
                        {
                            var emp = lstEmpleado.FirstOrDefault(e => e.CEDULA == cedula);
                            return new
                            {
                                CEDULA = cedula,
                                NOMBRE_COMPLETO = emp != null ? emp.NOMBRE_COMPLETO : "",
                                CARNET = emp != null ? emp.CARNET : ""
                            };
                        }).ToList();

                        // Obtener fotos de inspección adicionales (Extra)
                        string sqlFotos = @"
                    SELECT Parte, Foto 
                    FROM InspeccionFoto 
                    WHERE IdInspeccion = @idInspeccion";
                        var fotosRaw = connection.Query(sqlFotos, new { idInspeccion = inspeccion.IdInspeccion }).ToList();
                        var fotosDict = new Dictionary<string, string>();
                        foreach (var row in fotosRaw)
                        {
                            if (row.Foto != null)
                            {
                                byte[] fotoBytes = (byte[])row.Foto;
                                string base64Foto = "data:image/webp;base64," + Convert.ToBase64String(fotoBytes);
                                // Si hay varias fotos para la misma parte, se puede ajustar (por ahora se usa la última)
                                fotosDict[row.Parte] = base64Foto;
                            }
                        }

                        var inspeccionDto = new
                        {
                            IdInspeccion = inspeccion.IdInspeccion,
                            IdVehiculo = inspeccion.IdVehiculo,
                            Kilometraje = inspeccion.Kilometraje,
                            AsignadaA = inspeccion.AsignadaA,
                            Domicilio = inspeccion.Domicilio,
                            Telefono = inspeccion.Telefono,
                             FotoLicenciaFrontalBase64 = fotoFrontalBase64,
                            FotoLicenciaPosteriorBase64 = fotoPosteriorBase64,
                            PruebaManejo = inspeccion.PruebaManejo,
                            Departamento = inspeccion.Departamento, 
                            Edificio = inspeccion.Edificio,
                            FechaRegistro = inspeccion.FechaRegistro,
                            Estado = inspeccion.Estado,Correo= inspeccion.Correo,
                            ConductoresDetalle = conductoresDetallados,
                            ExtraFotos = fotosDict ,


                        };

                        return Json(new { success = true, data = inspeccionDto }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { success = false, message = "No se encontró inspección." }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult ObtenerInspeccionDatos2025(string idVehiculo)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = @"
                SELECT TOP 1 IdInspeccion, IdVehiculo, Kilometraje, AsignadaA, 
                    Domicilio, Telefono, PlantelAsignado, PruebaManejo, 
                    Departamento, Edificio, FechaRegistro, Estado, Correo
                FROM Inspeccion
                WHERE IdVehiculo = @idVehiculo
                ORDER BY FechaRegistro DESC";

                    var inspeccion = connection.QuerySingleOrDefault(sql, new { idVehiculo });

                    if (inspeccion != null)
                    {
                        // Conductores
                        var conductores = connection.Query<string>(
                            "SELECT Conductor FROM InspeccionConductor WHERE IdInspeccion = @id",
                            new { id = inspeccion.IdInspeccion }).ToList();
                        List<EmpleadoInspeccion> empleados = new List<EmpleadoInspeccion>(); 
                        if (Session["sEmployeestodoempleado2"] != null)
                            empleados = (List<EmpleadoInspeccion>)Session["sEmployeestodoempleado2"]   ;

                        var detalle = conductores.Select(c =>
                        {
                            var emp = empleados.FirstOrDefault(e => e.CEDULA == c);
                            return new
                            {
                                CEDULA = c,
                                NOMBRE_COMPLETO = emp?.NOMBRE_COMPLETO ?? "",
                                CARNET = emp?.CARNET ?? ""
                            };
                        }).ToList();

                        return Json(new
                        {
                            success = true,
                            data = new
                            {
                                inspeccion.IdInspeccion,
                                inspeccion.IdVehiculo,
                                inspeccion.Kilometraje,
                                inspeccion.AsignadaA,
                                inspeccion.Domicilio,
                                inspeccion.Telefono,
                                inspeccion.PlantelAsignado,
                                inspeccion.PruebaManejo,
                                inspeccion.Departamento,
                                inspeccion.Edificio,
                                inspeccion.Estado,
                                inspeccion.Correo,
                                ConductoresDetalle = detalle
                            }
                        }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new { success = false, message = "No se encontró inspección." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult ObtenerLicenciaImagenes2025(int idInspeccion)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var sql = "SELECT FotoLicenciaFrontal, FotoLicenciaPosterior FROM Inspeccion WHERE IdInspeccion = @id";
                    var data = connection.QuerySingleOrDefault(sql, new { id = idInspeccion });

                    string frontal = data.FotoLicenciaFrontal != null ? "data:image/webp;base64," + Convert.ToBase64String((byte[])data.FotoLicenciaFrontal) : null;
                    string posterior = data.FotoLicenciaPosterior != null ? "data:image/webp;base64," + Convert.ToBase64String((byte[])data.FotoLicenciaPosterior) : null;

                    return Json(new { success = true, frontal, posterior }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
  
        public ActionResult ObtenerFotoPorParte2025(int idInspeccion, string parte)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var sql = "SELECT TOP 1 Foto FROM InspeccionFoto WHERE IdInspeccion = @id AND Parte = @parte";
                    var foto = connection.QueryFirstOrDefault<byte[]>(sql, new { id = idInspeccion, parte });

                    string base64 = foto != null ? "data:image/webp;base64," + Convert.ToBase64String(foto) : null;

                    return Json(new { success = true, base64 }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public ActionResult ObtenerInspeccionPorId(int idInspeccion)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = @"
                SELECT TOP 1 
                    IdInspeccion,
                    IdVehiculo,
                    Kilometraje,
                    AsignadaA,
                    Domicilio,
                    Telefono,
                    PlantelAsignado,
                    FotoLicenciaFrontal,
                    FotoLicenciaPosterior,
                    PruebaManejo,
                    Departamento,
                    Edificio
                FROM Inspeccion
                WHERE IdInspeccion = @idInspeccion";
                    var inspeccion = connection.QuerySingleOrDefault(sql, new { idInspeccion });
                    if (inspeccion != null)
                    {
                        // Convertir imágenes a Base64 (si existen)
                        string fotoFrontalBase64 = inspeccion.FotoLicenciaFrontal != null
                            ? "data:image/webp;base64," + Convert.ToBase64String((byte[])inspeccion.FotoLicenciaFrontal)
                            : "";
                        string fotoPosteriorBase64 = inspeccion.FotoLicenciaPosterior != null
                            ? "data:image/webp;base64," + Convert.ToBase64String((byte[])inspeccion.FotoLicenciaPosterior)
                            : "";
                        var data = new
                        {
                            inspeccion.IdInspeccion,
                            inspeccion.IdVehiculo,
                            inspeccion.Kilometraje,
                            inspeccion.AsignadaA,
                            inspeccion.Domicilio,
                            inspeccion.Telefono,
                            inspeccion.PlantelAsignado,
                            FotoLicenciaFrontalBase64 = fotoFrontalBase64,
                            FotoLicenciaPosteriorBase64 = fotoPosteriorBase64,
                            inspeccion.PruebaManejo,
                            inspeccion.Departamento,
                            inspeccion.Edificio
                        };
                        return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { success = false, message = "Registro no encontrado." }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult ActualizarDatos(InspeccionViewModel model)
        {
            string lineaActual = "Inicio";
          
            

            //if (model!=null && model.IdInspeccion>0)
            //{
            //    return ActualizarDatosEdicion(model);
            //}
            //else { 
            try
            {
                // Remover prefijo innecesario en AsignadaA (si se requiere)
                model.FotoLicenciaFrontalBase64 = Session["ImagenLicenciaFrontal"] as string;
                model.FotoLicenciaPosteriorBase64 = Session["ImagenLicenciaPosterior"] as string; 
                model.Imagenes = new Dictionary<string, string>
        {
            { "Frontal", Session["ImagenFrontal"] as string },
            { "Tablero", Session["ImagenTablero"] as string },
            { "LateralIzquierdo", Session["ImagenLateralIzquierdo"] as string },
            { "LateralDerecho", Session["ImagenLateralDerecho"] as string },
            { "Trasero", Session["ImagenTrasero"] as string }
        };
                lineaActual = "Remover prefijo de AsignadaA";
                model.AsignadaA = model.AsignadaA.Replace("->slnRhonline.Models.EmpleadoInspeccion.", "");

                lineaActual = "Asignar nombre completo";
                string nombrecompleto = model.AsignadaA;

                lineaActual = "Obtener lista de empleados 2";
                List<EmpleadoInspeccion> lstEmpleado2 = (List<EmpleadoInspeccion>)Session["sEmployeestodoempleado2"];

                lineaActual = "Buscar empleado por nombre";
                var emp2 = lstEmpleado2.Where(x => x.NOMBRE_COMPLETO == model.AsignadaA).FirstOrDefault();

                lineaActual = "Obtener usuario de sesión";
                Entities.Employees eEmployee = null;
                if (Session["User"] != null)
                    eEmployee = (Entities.Employees)Session["User"];

                lineaActual = "Obtener lista de empleados";
                List<EmpleadoInspeccion> lstEmpleado = new List<EmpleadoInspeccion>();
                if (Session["sEmployeestodoempleado2"] != null)
                    lstEmpleado = (List<EmpleadoInspeccion>)Session["sEmployeestodoempleado2"];

                lineaActual = "Asignar correo";
                model.Usuario = eEmployee.EmailAddress;
                model.Correo = emp2.CORREO;

                lineaActual = "Inicializar NombreConductores";
                model.NombreConductores = new List<string>();

                lineaActual = "Procesar lista de Conductores";
                if (model.Conductores != null && model.Conductores.Any())
                {
                    foreach (var cond in model.Conductores)
                    {
                        var emp = lstEmpleado.FirstOrDefault(x => x.CEDULA == cond);
                        if (emp != null)
                            model.NombreConductores.Add($"{emp.NOMBRE_COMPLETO} - {emp.CARNET}");
                        else
                            model.NombreConductores.Add(cond);
                    }
                }

                lineaActual = "Definir endpoint";
                string endpoint = "http://172.26.54.66/apihcm/api/vehiculo/datos/actualizar?token=021092";

                lineaActual = "Inicializar RestClient y RestRequest";
                var client = new RestClient(endpoint);
                var request = new RestRequest(endpoint, Method.POST);

                lineaActual = "Serializar modelo";
                string jsonBody = JsonConvert.SerializeObject(model);

                lineaActual = "Agregar headers y body";
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

                lineaActual = "Ejecutar request";
                IRestResponse response = client.Execute(request);

                lineaActual = "Verificar respuesta";
                if (response.Content.Contains("actualización de datos guardada") ||
                    response.Content.Contains("Inspección actualizada correctamente") ||
                    response.Content.Contains("Inspección guardada"))
                {
                    Session.Remove("LicenciaFrontal");
                    Session.Remove("LicenciaPosterior");
                    Session.Remove("ImagenFrontal");
                    Session.Remove("ImagenTablero");
                    Session.Remove("ImagenLateralIzquierdo");
                    Session.Remove("ImagenLateralDerecho");
                    Session.Remove("ImagenTrasero");
                    return Json(new { success = true, message = "Registrado.-" + response.Content });
                }
                else
                {
                    Session.Remove("LicenciaFrontal");
                    Session.Remove("LicenciaPosterior");
                    Session.Remove("ImagenFrontal");
                    Session.Remove("ImagenTablero");
                    Session.Remove("ImagenLateralIzquierdo");
                    Session.Remove("ImagenLateralDerecho");
                    Session.Remove("ImagenTrasero");
                    return Json(new { success = false, message = "Error-" + response.Content+  lineaActual });
                }
            }
            catch (Exception ex)
            {
                Session.Remove("LicenciaFrontal");
                Session.Remove("LicenciaPosterior");
                Session.Remove("ImagenFrontal");
                Session.Remove("ImagenTablero");
                Session.Remove("ImagenLateralIzquierdo");
                Session.Remove("ImagenLateralDerecho");
                Session.Remove("ImagenTrasero");
                return Json(new { success = false, message =  lineaActual });
            }
            //}
        }
        [HttpPost]
        public ActionResult ActualizarDatos2025(InspeccionViewModel model)
        {
            string lineaActual = "Inicio";

            try
            {
                // 1. Recuperar imágenes
                string base64Frontal = Session["ImagenLicenciaFrontal"] as string;
                string base64Posterior = Session["ImagenLicenciaPosterior"] as string;

                var imagenes = new Dictionary<string, string>
        {
            { "Frontal", Session["ImagenFrontal"] as string },
            { "Tablero", Session["ImagenTablero"] as string },
            { "LateralIzquierdo", Session["ImagenLateralIzquierdo"] as string },
            { "LateralDerecho", Session["ImagenLateralDerecho"] as string },
            { "Trasero", Session["ImagenTrasero"] as string }
        };

                // 2. Asignar datos
                model.AsignadaA = model.AsignadaA?.Replace("->slnRhonline.Models.EmpleadoInspeccion.", "");

                var empleados = (List<EmpleadoInspeccion>)Session["sEmployeestodoempleado2"];
                var emp2 = empleados.FirstOrDefault(x => x.NOMBRE_COMPLETO == model.AsignadaA);

                var eEmployee = (Entities.Employees)Session["User"];
                model.Usuario = eEmployee?.EmailAddress;
                model.Correo = emp2?.CORREO;
                model.NombreConductores = new List<string>();

                if (model.Conductores != null)
                {
                    foreach (var c in model.Conductores)
                    {
                        var emp = empleados.FirstOrDefault(x => x.CEDULA == c);
                        model.NombreConductores.Add(emp != null ? $"{emp.NOMBRE_COMPLETO} - {emp.CARNET}" : c);
                    }
                }

                // 3. Enviar datos generales (API 1)
                lineaActual = "API 1 - Datos generales";
                string jsonBody = JsonConvert.SerializeObject(model);
                var client1 = new RestClient("http://172.26.54.66/apihcm/api/vehiculo/datos/registrar?token=021092");
                var request1 = new RestRequest(Method.POST);
                request1.AddHeader("Content-Type", "application/json");
                request1.AddParameter("application/json", jsonBody, ParameterType.RequestBody);
                IRestResponse response1 = client1.Execute(request1);

                var resultado = JsonConvert.DeserializeObject<dynamic>(response1.Content);
                int idInspeccion = 0;
                if (resultado == null || resultado.idInspeccion == null || !int.TryParse(resultado.idInspeccion.ToString(), out   idInspeccion) || idInspeccion <= 0)
                {
                    return Json(new { success = false, message = "No se logró registrar la actualizacion de datos" });
                }


                // 4. Enviar licencia (API 2)
                lineaActual = "API 2 - Fotos de licencia";
                var licData = new
                {
                    IdInspeccion = idInspeccion,
                    FotoFrontal = base64Frontal,
                    FotoPosterior = base64Posterior
                };
                var client2 = new RestClient("http://172.26.54.66/apihcm/api/vehiculo/licencia?token=021092");
                var request2 = new RestRequest(Method.POST);
                request2.AddHeader("Content-Type", "application/json");
                request2.AddParameter("application/json", JsonConvert.SerializeObject(licData), ParameterType.RequestBody);
                client2.Execute(request2);

                // 5. Enviar fotos por parte (API 3)
                foreach (var item in imagenes)
                {
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        string base64Reducido = ComprimirBase64(item.Value);

                        lineaActual = $"API 3 - Foto parte: {item.Key}";
                        var fotoData = new
                        {
                            IdInspeccion = idInspeccion,
                            Parte = item.Key,
                            ImagenBase64 = base64Reducido
                        };
                        var client3 = new RestClient("http://172.26.54.66/apihcm/api/vehiculo/foto?token=021092");
                        var request3 = new RestRequest(Method.POST);
                        request3.AddHeader("Content-Type", "application/json");
                        request3.AddParameter("application/json", JsonConvert.SerializeObject(fotoData), ParameterType.RequestBody);
                        IRestResponse response2 = client3.Execute(request3);
                        var resultado2= JsonConvert.DeserializeObject<dynamic>(response2.Content); 
                    }
                }

                // 6. Limpiar sesión
                Session.Remove("ImagenLicenciaFrontal");
                Session.Remove("ImagenLicenciaPosterior");
                Session.Remove("ImagenFrontal");
                Session.Remove("ImagenTablero");
                Session.Remove("ImagenLateralIzquierdo");
                Session.Remove("ImagenLateralDerecho");
                Session.Remove("ImagenTrasero");

                return Json(new { success = true, message = "Registrado exitoso: " });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error en: " + lineaActual });
            }
        }


        public static string ComprimirBase64(string base64Original, long calidad = 40L, long maxBytes = 3_000_000)
        {
            var limpio = base64Original
                   .Replace("data:image/jpeg;base64,", "")
                   .Replace("data:image/png;base64,", "")
                   .Replace("data:image/webp;base64,", "")
                   .Replace("\r", "")
                   .Replace("\n", "")
                   .Trim();

            byte[] bytesOriginal = Convert.FromBase64String(limpio);
            if (bytesOriginal.Length <= maxBytes)
                return base64Original; // no necesita compresión

            using (var msOriginal = new MemoryStream(bytesOriginal))
            using (var img = Image.FromStream(msOriginal))
            using (var msComprimido = new MemoryStream())
            {
                var jpeg = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                var param = new EncoderParameters(1);
                param.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, calidad);

                img.Save(msComprimido, jpeg, param);
                return Convert.ToBase64String(msComprimido.ToArray());
            }
        }

        public JsonResult ActualizarDatosEdicion(InspeccionViewModel model)
        {
            try
            {
                // Remover prefijo innecesario en AsignadaA (si se requiere)
                model.AsignadaA = model.AsignadaA.Replace("->slnRhonline.Models.EmpleadoInspeccion.", "");
                string nombrecompleto = model.AsignadaA;
                List<EmpleadoInspeccion> lstEmpleado2 = (List<EmpleadoInspeccion>)Session["sEmployeestodoempleado2"];
                var emp2 = lstEmpleado2.FirstOrDefault(x => x.NOMBRE_COMPLETO == model.AsignadaA);
                Entities.Employees eEmployee = Session["User"] as Entities.Employees;

                // Convertir imágenes de licencia
                byte[] fotoLicenciaFrontalBytes = !string.IsNullOrEmpty(model.FotoLicenciaFrontalBase64)
                    ? ConvertirImagenWebP(model.FotoLicenciaFrontalBase64)
                    : null;
                byte[] fotoLicenciaPosteriorBytes = !string.IsNullOrEmpty(model.FotoLicenciaPosteriorBase64)
                    ? ConvertirImagenWebP(model.FotoLicenciaPosteriorBase64)
                    : null;

                using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    connection.Open();

                    // Actualizar registro existente en Inspeccion
                    string sqlUpdateInspeccion = @"
UPDATE Inspeccion
SET
    Kilometraje = @Kilometraje,
    AsignadaA = @AsignadaA,
    Domicilio = @Domicilio,
    Telefono = @Telefono,
    PlantelAsignado = @PlantelAsignado,
    FotoLicenciaFrontal = @FotoLicenciaFrontal,
    FotoLicenciaPosterior = @FotoLicenciaPosterior,
    PruebaManejo = @PruebaManejo,
    Departamento = @Departamento,
    Edificio = @Edificio,
    Usuario = @Usuario,
    Correo = @Correo
WHERE IdInspeccion = @IdInspeccion";
                    int affected = connection.Execute(sqlUpdateInspeccion, new
                    {
                        model.Kilometraje,
                        model.AsignadaA,
                        model.Domicilio,
                        model.Telefono,
                        PlantelAsignado = model.PlantelAsignado,
                        FotoLicenciaFrontal = fotoLicenciaFrontalBytes,
                        FotoLicenciaPosterior = fotoLicenciaPosteriorBytes,
                        model.PruebaManejo,
                        Departamento = model.Departamento,
                        Edificio = model.Edificio,
                        Usuario = eEmployee.EmailAddress,
                        Correo = emp2.CORREO,
                        model.IdInspeccion
                    });

                    // Actualizar conductores:
                    // 1. Eliminar los conductores existentes para este IdInspeccion
                    string sqlDeleteConductores = "DELETE FROM InspeccionConductor WHERE IdInspeccion = @IdInspeccion";
                    connection.Execute(sqlDeleteConductores, new { IdInspeccion = model.IdInspeccion });
                    // 2. Insertar los nuevos conductores (si existen)
                    if (model.Conductores != null && model.Conductores.Any())
                    {
                        string sqlInsertConductor = @"
INSERT INTO InspeccionConductor (IdInspeccion, Conductor, FechaRegistro)
VALUES (@IdInspeccion, @Conductor, GETDATE());";
                        foreach (var conductor in model.Conductores)
                        {
                            connection.Execute(sqlInsertConductor, new
                            {
                                IdInspeccion = model.IdInspeccion,
                                Conductor = conductor
                            });
                        }
                    }

                    // Actualizar fotos extras:
                    // 1. Eliminar las fotos existentes para este IdInspeccion
                    string sqlDeleteFotos = "DELETE FROM InspeccionFoto WHERE IdInspeccion = @IdInspeccion";
                    connection.Execute(sqlDeleteFotos, new { IdInspeccion = model.IdInspeccion });
                    // 2. Insertar las nuevas fotos (si existen)
                    if (model.Imagenes != null)
                    {
                        string sqlInsertFoto = @"
INSERT INTO InspeccionFoto (IdInspeccion, Parte, Foto, FechaRegistro)
VALUES (@IdInspeccion, @Parte, @Foto, GETDATE());";
                        foreach (var item in model.Imagenes)
                        {
                            byte[] fotoBytes = ConvertirImagenWebP(item.Value);
                            connection.Execute(sqlInsertFoto, new
                            {
                                IdInspeccion = model.IdInspeccion,
                                Parte = item.Key,
                                Foto = fotoBytes
                            });
                        }
                    }
                }

                // Preparar la lista de conductores detallados para enriquecer el correo
                List<EmpleadoInspeccion> lstEmpleado = new List<EmpleadoInspeccion>();
                if (Session["sEmployeestodoempleado2"] != null)
                    lstEmpleado = (List<EmpleadoInspeccion>)Session["sEmployeestodoempleado2"];
                string conductoresHtml = "";
                if (model.Conductores != null && model.Conductores.Any())
                {
                    conductoresHtml = "<ul>";
                    foreach (var cond in model.Conductores)
                    {
                        var emp = lstEmpleado.FirstOrDefault(x => x.CEDULA == cond);
                        if (emp != null)
                            conductoresHtml += $"<li>{emp.NOMBRE_COMPLETO} - {emp.CARNET}</li>";
                        else
                            conductoresHtml += $"<li>{cond}</li>";
                    }
                    conductoresHtml += "</ul>";
                }

                // --- Generar el PDF usando la clase InspeccionPdfCreator ---
                string pdfPath = InspeccionPdfCreator.CreatePdf(model, lstEmpleado);

                // --- Construir el contenido del correo (en HTML) ---
                string emailBody = $@"
<html>
<head>
  <style>
    body {{ font-family: Arial, sans-serif; font-size: 14px; color: #333; background-color: #f5f5f5; margin:0; padding:0; }}
    .header {{ background-color: #d32f2f; color: #fff; padding: 15px; text-align: center; }}
    .content {{ background-color: #fff; padding: 20px; margin: 20px; border: 1px solid #ccc; }}
    .section-title {{ color: #d32f2f; border-bottom: 2px solid #d32f2f; padding-bottom: 5px; margin-bottom: 10px; }}
  </style>
</head>
<body>
  <div class='header'>
    <h2>Edición de Datos de la unidad - {model.IdVehiculo}</h2>
  </div>
  <div class='content'>
    <h3 class='section-title'>Datos de Inspección</h3>
    <p><strong>Asignada a:</strong> {model.AsignadaA}</p>
    <p><strong>Departamento:</strong> {model.Departamento}</p>
    <p><strong>Edificio:</strong> {model.Edificio}</p>
    <p><strong>Lugar de estacionamiento:</strong> {model.Domicilio}</p>
    <p><strong>Teléfono:</strong> {model.Telefono}</p>
    <p><strong>Kilometraje:</strong> {model.Kilometraje}</p>
    <p><strong>Realizó la Prueba de Manejo:</strong> {(model.PruebaManejo ? "Sí" : "No")}</p>
    <h3 class='section-title'>Conductores</h3>
    {conductoresHtml}
  </div>
</body>
</html>
";

                // --- Preparar y enviar el correo ---
                MailMessage email = new MailMessage();
                email.To.Add("transporte@claro.com.ni"); // Cambiar según corresponda
                email.CC.Add("ali.rodriguez@claro.com.ni");
                 email.CC.Add("josue.garcia@claro.com.ni");
                email.CC.Add("pedro.castillo@claro.com.ni");
                email.CC.Add("pablo.cruz@claro.com.ni");
                email.CC.Add("candida.sanchez@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
                email.CC.Add("edgardo.saballos@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
                email.CC.Add("taniaa.aguirre@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
                email.CC.Add("candida.sanchez@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
                email.CC.Add("gustavo.lira@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
                email.From = new MailAddress("transporte@claro.com.ni");
                email.Subject = $"Edición de datos de la unidad - {model.IdVehiculo}";
                email.SubjectEncoding = System.Text.Encoding.UTF8;
                email.Body = emailBody;
                email.BodyEncoding = System.Text.Encoding.UTF8;
                email.IsBodyHtml = true;
                email.Priority = MailPriority.Normal;
                email.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.Delay;

                // Adjuntar el PDF generado
                if (System.IO.File.Exists(pdfPath))
                {
                    Attachment attPdf = new Attachment(pdfPath);
                    attPdf.ContentId = "pdfInspeccion";
                    attPdf.ContentDisposition.Inline = false; // Se adjunta como archivo
                    email.Attachments.Add(attPdf);
                }

                // Enviar correo
                List<string> tempFiles = new List<string>();
                ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
                SmtpClient cliente = new SmtpClient("10.200.5.23", 587);
                // Credenciales (ajusta o elimina antes de producción)
                cliente.Credentials = new NetworkCredential("transporte@claro.com.ni", "Enero&r546");
                cliente.EnableSsl = true;

                string emailOutput;
                try
                {
                    cliente.Send(email);
                    emailOutput = "Correo enviado con éxito.";
                    email.Dispose();
                }
                catch (Exception ex)
                {
                    emailOutput = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                }
                finally
                {
                    if (System.IO.File.Exists(pdfPath))
                        System.IO.File.Delete(pdfPath);
                    foreach (string tempFile in tempFiles)
                    {
                        try
                        {
                            if (System.IO.File.Exists(tempFile))
                                System.IO.File.Delete(tempFile);
                        }
                        catch { }
                    }
                }

                return Json(new { success = true, message = "actualización datos-" + emailOutput });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ValidarInspeccion(int idInspeccion)
        {
            try
            {
                string endpoint = $"http://172.26.54.66/apihcm/api/vehiculo/datos/validarinspeccion?idInspeccion={idInspeccion}";


                 var client = new RestClient(endpoint);
                var request = new RestRequest(  Method.POST);

                 

                

                // Agregar el header Content-Type y el body
                //request.AddHeader("Content-Type", "application/json");
                //request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                //using (var connection = new SqlConnection(connectionString))
                //{
                //    connection.Open();
                //    // Actualiza la columna Validar a 'validado' para la inspección dada
                //    string sql = "UPDATE Inspeccion SET Validar = 'validado' WHERE IdInspeccion = @idInspeccion";
                //    int affected = connection.Execute(sql, new { idInspeccion });
                //    if (affected > 0)
                //        return Json(new { success = true, message = "Inspección validada." });
                //    else
                if (response.Content.Contains("Inspección validada") ==true)
                {
                    return Json(new { success = true, message = "Actualizacion de validada." });

                }
                else
                {
                    return Json(new { success = false, message = "No se pudo validar la actualizacion de datos." });
                }
                //}
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public ActionResult DenegarInspeccion(DenegarInspeccionModel model)
        {
            try
            {
                List<UnidadInspeccionDto> lista = new List<UnidadInspeccionDto>();
                lista = (List<UnidadInspeccionDto>)Session["actualizaciondatosvehicular"];
                var qr = lista.FirstOrDefault(x => x.IdInspeccion == model.idInspeccion);
                string correo = "";
                string usuario = "";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT Correo, Usuario FROM Inspeccion";
                     var result = connection.Query<dynamic>(sql).ToList(); 
                    var primerRegistro = result[0];
                    correo = primerRegistro.Correo;
                    usuario = primerRegistro.Usuario;
                    model.Usuario = usuario;
                    model.Correo = correo;
                    model.IdUnidad = qr.IdUnidad;
                }
              
                string endpoint = "http://172.26.54.66/apihcm/api/vehiculo/datos/denegarinspeccion";


                // Crear el request con RestSharp (versión 104)
                var client = new RestClient(endpoint);
                var request = new RestRequest( Method.POST);

                // Serializar el modelo a JSON
                string jsonBody = JsonConvert.SerializeObject(model);

                // Agregar encabezados y cuerpo JSON
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

                // Ejecutar la solicitud
                IRestResponse response = client.Execute(request);
                if (response.Content.Contains("Inspección denegada y correo enviado") ==true)
                {
                    return Json(new { success = true, message = "actualización de datos guardada y " + response.Content });
                }
                else { return Json(new { success = false, message = "Error-" + response.Content }); }
               
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            //using (var connection = new SqlConnection(connectionString))
            //{
            //    connection.Open();
            //    string sql = "SELECT Correo, Usuario FROM Inspeccion";
            //    // Eliminar el registro de inspección
            //    var result = connection.Query<dynamic>(sql).ToList();
            //    string correo = "";
            //    string usuario = "";
            //    if (result.Any())
            //    {
            //        var primerRegistro = result[0];
            //        correo = primerRegistro.Correo;
            //        usuario = primerRegistro.Usuario;
            //        model.Usuario = usuario;
            //        model.Correo = correo;
            //        model.IdUnidad = qr.IdUnidad;
            //    }
            //                    string sqlDelete = @"
            //    DELETE FROM InspeccionConductor WHERE IdInspeccion = @idInspeccion;
            //    DELETE FROM InspeccionFoto       WHERE IdInspeccion = @idInspeccion;
            //    DELETE FROM Inspeccion          WHERE IdInspeccion = @idInspeccion;
            //"; int affected = connection.Execute(sqlDelete, new { idInspeccion = model.idInspeccion });

            //                    if (affected > 0)
            //                    {

            //                        // Preparar el cuerpo del correo
            //                        string emailBody = $@"
            //<html>
            //<head><meta charset='utf-8' /></head>
            //<body>
            //    <p>La actualizacion de datos de la Unidad <strong>{qr.IdUnidad}</strong> ha sido <span style='color:red;'>denegada</span>.</p>
            //    <p><strong>Motivo de denegación:</strong></p>
            //    <p>{model.comentario}</p>
            //    <p>La subgerencia de recursos humanos ha validado su registro.</p>
            //</body>
            //</html>";
            //                        // Enviar correo (ajusta destinatarios y credenciales según tu entorno)
            //                        MailMessage email = new MailMessage();
            //                        email.To.Add("transporte@claro.com.ni"); // Cambiar según corresponda
            //                        email.CC.Add("ali.rodriguez@claro.com.ni");
             //                        email.CC.Add("josue.garcia@claro.com.ni");
            //                        email.CC.Add("pedro.castillo@claro.com.ni");
            //                        email.CC.Add("pablo.cruz@claro.com.ni");
            //                        email.CC.Add("candida.sanchez@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
            //                        email.CC.Add("edgardo.saballos@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
            //                        email.CC.Add("taniaa.aguirre@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
            //                        email.CC.Add("candida.sanchez@claro.com.ni");                         // Puedes agregar más destinatarios si es necesario
            //                        email.CC.Add("gustavo.lira@claro.com.ni");
            //                        //email.To.Add("candida.sanchez@claro.com.ni");
            //                        // Destinatario principal o variable\n
            //                        email.From = new MailAddress("transporte@claro.com.ni");
            //                        //email.CC.Add(usuario); 
            //                        email.Subject = $"Actualizacion de Dato rechazada - {model.idInspeccion}";
            //                        email.SubjectEncoding = System.Text.Encoding.UTF8;
            //                        email.Body = emailBody;
            //                        email.IsBodyHtml = true;

            //                        ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
            //                        System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

            //                        SmtpClient cliente = new SmtpClient("10.200.5.23", 587); // IP y puerto de FortiMail
            //                        cliente.Credentials = new NetworkCredential("recursoshumanos@claro.com.ni", "Enero&272025"); // Eliminar antes de producción
            //                        cliente.Credentials = new NetworkCredential("transporte@claro.com.ni", "Enero&r546"); // Eliminar antes de producción
            //                        cliente.EnableSsl = true;
            //                        cliente.EnableSsl = true;
            //                        cliente.Send(email);
            //                        email.Dispose();

            //                        return Json(new { success = true, message = "Inspección denegada y correo enviado." });
            //                    }
            //                    else
            //                    {
            //                        return Json(new { success = false, message = "No se encontró la inspección." });
            //                    }
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                return Json(new { success = false, message = ex.Message });
            //            }
        }
        [HttpPost]
        public JsonResult GuardarFotoLicenciaFrontal(string  req)
        {
            Session["ImagenLicenciaFrontal"] = req;
            return Json(new { success = true });
        }

        // 2. Licencia Posterior
        [HttpPost]
        public JsonResult GuardarFotoLicenciaPosterior(string  req)
        {
            Session["ImagenLicenciaPosterior"] = req;
            return Json(new { success = true });
        }

        // 3. Frontal
        [HttpPost]
        public JsonResult GuardarFotoFrontal(string  req)
        {
            Session["ImagenFrontal"] = req;
            return Json(new { success = true });
        }

        // 4. Tablero
        [HttpPost]
        public JsonResult GuardarFotoTablero(string  req)
        {
            Session["ImagenTablero"] = req;
            return Json(new { success = true });
        }

        // 5. Lateral Izquierdo
        [HttpPost]
        public JsonResult GuardarFotoLateralIzquierdo(string  req)
        {
            Session["ImagenLateralIzquierdo"] = req;
            return Json(new { success = true });
        }

        // 6. Lateral Derecho
        [HttpPost]
        public JsonResult GuardarFotoLateralDerecho(string  req)
        {
            Session["ImagenLateralDerecho"] = req;
            return Json(new { success = true });
        }

        // 7. Trasero
        [HttpPost]
        public JsonResult GuardarImagenTrasero(string  req)
        {
            Session["ImagenTrasero"] = req;
            return Json(new { success = true });
        }

        /// <summary>
        /// Convierte una imagen codificada en base64 a formato WebP.
        /// Si la imagen no está en WebP, la convierte usando Magick.NET.
        /// </summary>
        /// <param name="base64Image">Cadena base64 de la imagen (con o sin prefijo)</param>
        /// <returns>Arreglo de bytes en formato WebP</returns>
        private byte[] ConvertirImagenWebP(string base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
                throw new ArgumentException("La imagen no puede ser nula o vacía.");

            byte[] imageBytes;

            if (!base64Image.StartsWith("data:image/webp"))
            {
                // Extraer la parte base64 después de la coma (en caso de tener prefijo, ej: data:image/jpeg;base64,...)
                int commaIndex = base64Image.IndexOf(",");
                var base64Data = commaIndex >= 0 ? base64Image.Substring(commaIndex + 1) : base64Image;
                byte[] originalBytes = Convert.FromBase64String(base64Data);

                using (var msInput = new MemoryStream(originalBytes))
                {
                    using (var image = new MagickImage(msInput))
                    {
                        image.Format = MagickFormat.WebP;
                        image.Quality = 70;
                        using (var msOutput = new MemoryStream())
                        {
                            image.Write(msOutput);
                            imageBytes = msOutput.ToArray();
                        }
                    }
                }
            }
            else
            {
                // Ya viene en WebP; quitar el prefijo y decodificar
                var base64Webp = base64Image.Replace("data:image/webp;base64,", "");
                imageBytes = Convert.FromBase64String(base64Webp);
            }
            return imageBytes;
        }
        #endregion

    }
}