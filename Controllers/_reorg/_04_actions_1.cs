        #region Actions

        // GET: Helpdesk2025
        public ActionResult Index()
        {
            // Validar sesión
            var user = Session["User"] as Entities.Employees;
            if (user == null) return RedirectToAction("Index", "Login");

            // ViewBag.EsSoporte = ...
            // ViewBag.EsAdmin = ...
            // (La lógica de roles la manejas tú, aquí simplifico)
            ViewBag.Usuario = user;
            return View();
        }

        public ActionResult Soporte()
        {
            var user = Session["User"] as Entities.Employees;
            if (user == null) return RedirectToAction("Index", "Login");
            return View();
        }

        public ActionResult Admin()
        {
            var user = Session["User"] as Entities.Employees;
            if (user == null) return RedirectToAction("Index", "Login");
            // Validar si es admin real
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerNotas(int casoId)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    // 1. Obtener notas base
                    var notas = (await cn.QueryAsync<NotaDto>(
                        "dbo.usp_Caso_ListarNotas",
                        new { CasoID = casoId },
                        commandType: CommandType.StoredProcedure
                    )).ToList();

                    // 2. Obtener adjuntos para CADA nota (n+1 query, optimizable)
                    //    O traer todos los adjuntos del caso y repartir en memoria.
                    //    Si son pocos, query por nota es pasable.
                    foreach (var n in notas)
                    {
                        var adjuntos = await cn.QueryAsync<ArchivoDto>(
                            @"SELECT Id, NombreArchivo, TipoArchivo 
                      FROM dbo.CasoEventoArchivo 
                      WHERE EventoID = @EventoID", // Tu SP retorna EventoID en NotaDto.NotaID?
                            new { EventoID = n.NotaID }
                        );

                        // Generar URLs de descarga (controlador File)
                        foreach (var a in adjuntos)
                        {
                            a.UrlDescarga = Url.Action("DescargarAdjuntoNota", "Helpdesk2025", new { id = a.Id });
                        }
                        n.Adjuntos = adjuntos.ToList();
                    }

                    return Json(new { success = true, data = notas }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult DescargarAdjuntoNota(int id)
        {
            using (var cn = new SqlConnection(csHelpdesk))
            {
                var row = cn.QueryFirstOrDefault(
                    "SELECT NombreArchivo, TipoArchivo, DatosArchivo FROM dbo.CasoEventoArchivo WHERE Id=@id",
                    new { id });

                if (row == null) return HttpNotFound();

                return File((byte[])row.DatosArchivo, (string)row.TipoArchivo, (string)row.NombreArchivo);
            }
        }
        [HttpGet]
        public ActionResult SubEstado_Listar()
        {
            try
            {
                using (var db = new SqlConnection(csHelpdesk))
                {
                    // Ajusta nombre de tabla/sp
                    var lista = db.Query<SubEstadoDto>("SELECT SubEstadoID, Nombre FROM dbo.SubEstado ORDER BY Nombre").ToList();
                    return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }

        }

        [HttpPost]
        public async Task<JsonResult> CrearCaso(
            string titulo,
            string descripcion,
            string prioridad,
            int? tipoCasoId,
            int? subtipoCasoId,
            string afectadoCarnet,  // <--- Nuevo: carnet del "afectado" (puede ser distinto al login)
            HttpPostedFileBase archivo
        )
        {
            // 1. Validar sesión (quién crea el ticket)
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada" });

            // 2. Validar input
            if (string.IsNullOrWhiteSpace(titulo)) return Json(new { success = false, message = "El título es obligatorio." });
            if (string.IsNullOrWhiteSpace(descripcion)) return Json(new { success = false, message = "La descripción es obligatoria." });

            // 3. Determinar "Solicitante" vs "Afectado"
            //    - Solicitante: u.EmployeeNumber (quien está logueado)
            //    - Afectado: afectadoCarnet. Si viene vacío, asumimos que es el mismo usuario.
            var solicitanteID = u.EmployeeNumber;
            var afectadoID = string.IsNullOrWhiteSpace(afectadoCarnet) ? u.EmployeeNumber : afectadoCarnet;

            // Optional: validar que afectadoID exista en Emp2024
            // var afectadoInfo = ...

            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    await cn.OpenAsync();
                    using (var tx = cn.BeginTransaction())
                    {
                        try
                        {
                            // 4. Insertar Caso (SP v2 que soporte AfectadoID)
                            var p = new DynamicParameters();
                            p.Add("@Titulo", titulo);
                            p.Add("@Descripcion", descripcion);
                            p.Add("@Prioridad", prioridad ?? "Normal");
                            p.Add("@UsuarioID", solicitanteID);   // Quien abre
                            p.Add("@AfectadoID", afectadoID);     // Quien tiene el problema
                            p.Add("@TipoCasoID", tipoCasoId);
                            p.Add("@SubtipoCasoID", subtipoCasoId);
                            p.Add("@Estado", "Pendiente");
                            // p.Add("@Origen", "Web");

                            // Ejecutar y obtener ID
                            var casoId = await cn.ExecuteScalarAsync<int>(
                                "dbo.usp_Caso_Insertar_v2", // Asegúrate de tener este SP o modifícalo
                                p,
                                tx,
                                commandType: CommandType.StoredProcedure
                            );

                            // 5. Guardar Archivo (si existe)
                            if (archivo != null && archivo.ContentLength > 0)
                            {
                                // Validaciones de extensión/peso
                                // ...
                                var nombre = Path.GetFileName(archivo.FileName);
                                var mime = archivo.ContentType; // o mapa manual
                                var bytes = new byte[archivo.ContentLength];
                                archivo.InputStream.Read(bytes, 0, bytes.Length);

                                // Si es imagen muy pesada, podrías redimensionar aquí con Magick.NET
                                // var webpBytes = ConvertToWebP(bytes); 

                                await cn.ExecuteAsync(
                                    "dbo.usp_Archivo_Insertar",
                                    new
                                    {
                                        CasoID = casoId,
                                        NombreArchivo = nombre,
                                        TipoArchivo = mime,
                                        DatosArchivo = bytes,
                                        UsuarioID = solicitanteID
                                    },
                                    tx,
                                    commandType: CommandType.StoredProcedure
                                );
                            }

                            tx.Commit();

                            // 6. Notificaciones (Fire & Forget o await)
                            //    Correo al solicitante + afectado + soporte
                            await GenerarCorreoCreacionCasoSoporteAsync(casoId);

                            return Json(new { success = true, casoId });
                        }
                        catch (Exception inner)
                        {
                            tx.Rollback();
                            throw inner;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }
        [HttpGet]
        public JsonResult EmpleadoInfo(string term)
        {
            // Busca en sigho1.dbo.emp2024 por nombre o carnet
            // Retorna { id: carnet, text: "Nombre - Carnet" } para Select2
            using (var db = new SqlConnection(connectionString))
            {
                var sql = @"
            SELECT TOP 20 
                   EMPLEADO as id, 
                   (NOMBRE + ' - ' + EMPLEADO) as text, 
                   EMAIL as email, 
                   GERENCIA as gerencia
            FROM sigho1.dbo.emp2024 
            WHERE NOMBRE LIKE @q OR EMPLEADO LIKE @q";

                var data = db.Query(sql, new { q = "%" + term + "%" }).ToList();
                return Json(new { results = data }, JsonRequestBehavior.AllowGet);
            }
        }
