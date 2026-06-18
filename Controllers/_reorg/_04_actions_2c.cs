        /* ====== Lectura enriquecida ====== */

        public async Task<JsonResult> ObtenerCasos()
        {
            var casos = await ObtenerCasosAsync();
            return Json(casos, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> ObtenerCasosPaginados()
        {
            var casos = await ObtenerCasosPaginadosAsync();
            return Json(new { data = casos }, JsonRequestBehavior.AllowGet);
        }
        public async Task<IEnumerable<CasoView>> ObtenerCasosAsync()
        {
            var eEmployee = Session["User"] as Employees;
            using (var db = new SqlConnection(connectionString))
            {
                var casos = db.Query<CasoView>(
                    "dbo.usp_Casos_Listar_v2",
                    new { Permitido = eEmployee.EmailAddress },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                Session["casos"] = casos;
                return casos;
            }
        }

        public async Task<IEnumerable<CasoView>> ObtenerCasosPaginadosAsync()
        {
            var casos = await ObtenerCasosAsync();
            return casos;
        }

        public JsonResult Details2(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var caso = db.QueryFirstOrDefault<CasoDetalle>(
                                  "dbo.usp_CasosObtenerPorId",
                                  new { Id = id },
                                  commandType: CommandType.StoredProcedure
                              );
                if (caso == null) return Json(null, JsonRequestBehavior.AllowGet);

                var archivos = db.Query<Archivo>("SELECT * FROM dbo.Archivo WHERE CasoID=@CasoID", new { CasoID = id }).ToList();
                if (archivos?.Any() == true)
                {
                    var file = archivos.First();
                    caso.data = Convert.ToBase64String(file.DatosArchivo);
                    caso.DatosArchivo = file.DatosArchivo;
                    caso.TipoArchivo = file.TipoArchivo;
                    caso.NombreArchivo = file.NombreArchivo;
                }
                return Json(caso, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult Details3(int id)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    var caso = cn.QueryFirstOrDefault<CasoDetalleDto>(
                        "dbo.usp_Casos_Detalle_v1",
                        new { Id = id },
                        commandType: CommandType.StoredProcedure);

                    if (caso == null)
                        return Json(null, JsonRequestBehavior.AllowGet);

                    // Traer último adjunto (si tu tabla tiene FechaSubida; sino usa ID DESC)
                    var files = cn.Query<ArchivoDto>(
                        @"SELECT   * 
                          FROM dbo.Archivo 
                          WHERE CasoID=@CasoID 
                          ORDER BY ISNULL(FechaSubida, GETDATE()) DESC, Id DESC",
                        new { CasoID = id });

                    //if (file != null && file.DatosArchivo != null && file.DatosArchivo.Length > 0)
                    //{
                    //    caso.DatosArchivo = file.DatosArchivo;
                    //    caso.TipoArchivo = file.TipoArchivo;
                    //    caso.NombreArchivo = file.NombreArchivo;
                    //    caso.data = Convert.ToBase64String(file.DatosArchivo);
                    //}
                    if (files.Count() > 0)
                    {
                        caso.Adjuntos = new List<ArchivoDto>();
                    }
                    foreach (var f in files)
                    {
                        if (f.DatosArchivo != null && f.DatosArchivo.Length > 0)
                        {
                            f.data = Convert.ToBase64String(f.DatosArchivo);
                            caso.Adjuntos.Add(f);
                        }
                    }
                    return Json(caso, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult Details(int id)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    var caso = cn.QueryFirstOrDefault<CasoDetalleDto>(
                        "dbo.usp_Casos_Detalle_v1",
                        new { Id = id },
                        commandType: CommandType.StoredProcedure);

                    if (caso == null) return Json(null, JsonRequestBehavior.AllowGet);

                    // 1) Traer SOLO metadata primero (sin BLOB) para no inflar payload
                    var filesMeta = cn.Query<ArchivoDto>(@"
                SELECT 
                    Id, CasoID, NombreArchivo, TipoArchivo, 
                    ISNULL(FechaSubida, GETDATE()) AS FechaSubida
                FROM dbo.Archivo
                WHERE CasoID = @CasoID
                ORDER BY ISNULL(FechaSubida, GETDATE()) DESC, Id DESC",
                        new { CasoID = id }).ToList();

                    if (filesMeta != null && filesMeta.Count > 0)
                        caso.Adjuntos = new List<ArchivoDto>();

                    foreach (var meta in filesMeta)
                    {
                        // 2) Si es imagen → incluir base64 inline (data URL). Si es PDF → solo URL descarga.
                        var tipo = (meta.TipoArchivo ?? string.Empty).ToLowerInvariant();
                        var esImagen = tipo.StartsWith("image/");

                        if (esImagen)
                        {
                            // Leer bytes SOLO de esta fila (1 hit por adjunto imagen)
                            var bin = cn.ExecuteScalar<byte[]>(
                                "SELECT DatosArchivo FROM dbo.Archivo WHERE Id=@Id",
                                new { meta.Id });

                            if (bin != null && bin.Length > 0)
                                //f.data = Convert.ToBase64String(meta.DatosArchivo);

                                meta.data = Convert.ToBase64String(bin);
                        }
                        else
                        {
                            // PDF/otros → devolver link de descarga (sin base64)
                            // Crea una acción FileResult: FileArchivo(int id) que haga stream del BLOB
                            meta.UrlDescarga = Url.Action("FileArchivo", "Helpdesk2025", new { id = meta.Id });
                        }

                        caso.Adjuntos.Add(meta);
                    }

                    return Json(caso, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult FileArchivo(int id)
        {
            using (var cn = new SqlConnection(csHelpdesk))
            {
                var row = cn.QueryFirstOrDefault(
                    @"SELECT NombreArchivo, TipoArchivo, DatosArchivo 
              FROM dbo.Archivo WHERE Id=@Id",
                    new { Id = id });

                if (row == null || row.DatosArchivo == null) return HttpNotFound();

                var fileName = (string)row.NombreArchivo ?? "archivo";
                var mime = (string)row.TipoArchivo ?? "application/octet-stream";
                var bytes = (byte[])row.DatosArchivo;

                Response.AppendHeader("Content-Disposition", "inline; filename=\"" + fileName + "\"");
                return File(bytes, mime);
            }
        }

        [HttpGet]
        public JsonResult KPIsCasos(bool equipo = false)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada." });

            var carnet = u.EmployeeNumber;
            if (string.IsNullOrEmpty(carnet))
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            var resumen = new { Pend = 0, Proc = 0, Cerr = 0 };

            try
            {
                // Podemos simplemente reutilizar sp_Helpdesk_CasosPorJefe y contar en memoria:
                var tmp = new List<CasoJefeVM>();

                using (var cn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("dbo.sp_Helpdesk_CasosPorJefe", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CarnetJefe", carnet);
                    cmd.Parameters.AddWithValue("@Equipo", equipo ? 1 : 0);

                    cn.Open();
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            tmp.Add(new CasoJefeVM
                            {
                                Estado = rd["Estado"] as string
                            });
                        }
                    }
                }

                int p = 0, pr = 0, ce = 0;
                foreach (var c in tmp)
                {
                    if (c.Estado == "Pendiente" || c.Estado == "Asignado") p++;
                    else if (c.Estado == "En Proceso") pr++;
                    else if (c.Estado == "Cerrado") ce++;
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Pendiente = p,
                        EnProceso = pr,
                        Cerrado = ce
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public async Task<JsonResult> Admin_ListarSoportes()
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var data = await db.QueryAsync<SoporteAdminItem>(
                    "dbo.usp_Soporte_Listar",
                    commandType: CommandType.StoredProcedure
                );
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult Admin_Visibilidad_Listar(string viewerId)
        {
            try
            {
                using (var db = new SqlConnection(csHelpdesk))
                {
                    var sql = @"
                SELECT 
                    v.Id,
                    v.ViewerSoporteID as  ViewerID,
                    v.TargetSoporteID as CorreoTarget,
                    s.Nombre AS NombreTarget 
                FROM dbo.SoporteVisibilidad v
                LEFT JOIN dbo.Soporte s 
                       ON  s.SoporteID = v.TargetSoporteID   -- si guardas carnet/id
                        OR s.Email     = v.TargetSoporteID   -- si guardas correo
                WHERE v.ViewerSoporteID = @viewer";

                    // Encapsulado en lista fuertemente tipada
                    var lista = db.Query<SoporteVisibilidadDto2>(sql, new { viewer = viewerId }).ToList();

                    return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex) { Response.StatusCode = 500; return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet); }
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Admin_Visibilidad_Agregar(string viewerId, string targetId)
        {
            if (string.IsNullOrWhiteSpace(viewerId) || string.IsNullOrWhiteSpace(targetId))
                return Json(new { success = false, message = "Datos incompletos" });

            if (string.Equals(viewerId, targetId, StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "No puedes agregarte a ti mismo" });

            try
            {
                using (var db = new SqlConnection(csHelpdesk))          // <- csHelpdesk NO debe ser null
                {
                    db.Open();                                          // <- importante antes de BeginTransaction

                    using (var tx = db.BeginTransaction())              // <- aquí ya hay conexión abierta
                    {
                        try
                        {
                            // 1) Evitar duplicado
                            var existe = db.ExecuteScalar<int>(
                                @"SELECT COUNT(1) 
                  FROM dbo.SoporteVisibilidad 
                  WHERE ViewerSoporteID = @v 
                    AND TargetSoporteID = @t",
                                new { v = viewerId, t = targetId },
                                transaction: tx
                            );

                            if (existe > 0)
                            {
                                tx.Rollback();
                                return Json(new { success = true, duplicated = true },
                                            JsonRequestBehavior.AllowGet);
                            }

                            // 2) Insertar visibilidad
                            var sqlIns = @"
                INSERT INTO dbo.SoporteVisibilidad (ViewerSoporteID, TargetSoporteID, CreadoPor)
                VALUES (@v, @t, @u);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            var id = db.ExecuteScalar<int>(
                                sqlIns,
                                new
                                {
                                    v = viewerId,
                                    t = targetId,
                                    u = (User?.Identity?.Name ?? "sistema") // aquí no puede haber NullRef por el ?.
                },
                                transaction: tx
                            );

                            tx.Commit();
                            return Json(new { success = true, duplicated = false, id = id },
                                        JsonRequestBehavior.AllowGet);
                        }
                        catch
                        {
                            if (tx != null) tx.Rollback();
                            throw;
                        }
                    }
                }

            }
            catch (SqlException ex) { Response.StatusCode = 500; return Json(new { success = false, message = "SQL: " + ex.Message }); }
            catch (Exception ex) { Response.StatusCode = 500; return Json(new { success = false, message = ex.Message }); }
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Admin_Visibilidad_Quitar(int id)
        {
            try
            {
                using (var db = new SqlConnection(csHelpdesk))
                {
                    var rows = db.Execute("DELETE FROM dbo.Soporte_VisibilidadExtra WHERE Id=@id", new { id });
                    return Json(new { success = rows > 0 });
                }
            }
            catch (Exception ex) { Response.StatusCode = 500; return Json(new { success = false, message = ex.Message }); }
        }
        [HttpGet]
        public async Task<JsonResult> Admin_ObtenerSoporte(string id)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    using (var multi = await cn.QueryMultipleAsync(
                        "dbo.usp_Soporte_Obtener",
                        new { SoporteID = id },
                        commandType: CommandType.StoredProcedure))
                    {
                        var s = await multi.ReadFirstOrDefaultAsync<SoporteVm>();
                        var p = (await multi.ReadAsync<SoportePermisoDto>()).ToList();
                        if (s == null) return Json(new { success = false, message = "No existe el soporte." }, JsonRequestBehavior.AllowGet);
                        s.Permisos = p;
                        return Json(new { success = true, data = s }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult PersonaInfo(string carnet = null, string correo = null)
        {
            // Ideal mover a web.config
            const string WS_USER = "Claro_RhOnline_WS_SS";
            const string WS_PASS = "HCM-RH0nl1ne@#3";
            string consulta = "";
            if (carnet != null && carnet != "")
            {
                consulta = "usp_Emp2024_Obtener_v2";
            }
            else if (correo != null && correo != "")
            {
                consulta = "usp_Emp2024_Obtener_v3";

            }
            else
            {
                return Json(new { success = false, message = "revise correo" }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    var p = new DynamicParameters();
                    if (carnet != null && carnet != "")
                    {
                        p.Add("@Carnet", (object)carnet ?? DBNull.Value);
                    }
                    else
                    if (correo != null && correo != "")
                    {
                        p.Add("@Correo", (object)correo ?? DBNull.Value);

                    }
                    var persona = cn.QueryFirstOrDefault<PersonaDto>(
                    consulta, p, commandType: CommandType.StoredProcedure);

                    if (persona == null)
                        return Json(new { success = false, message = "No se encontró la persona." }, JsonRequestBehavior.AllowGet);

                    string imageDataURL = null;

                    if (!string.IsNullOrWhiteSpace(persona.Url))
                    {
                        try
                        {
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                            var request = (HttpWebRequest)WebRequest.Create(persona.Url);
                            string authHeader = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                                    .GetBytes($"{WS_USER}:{WS_PASS}"));
                            request.Headers.Add("Authorization", "Basic " + authHeader);
                            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                            using (var response = (HttpWebResponse)request.GetResponse())
                            using (var stream = response.GetResponseStream())
                            {
                                if (response.StatusCode == HttpStatusCode.OK && stream != null)
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        stream.CopyTo(ms);
                                        var bytes = ms.ToArray();
                                        var contentType = response.ContentType ?? "image/jpeg";
                                        var base64 = Convert.ToBase64String(bytes);
                                        imageDataURL = $"data:{contentType};base64,{base64}";
                                    }
                                }
                            }
                        }
                        catch (WebException webEx)
                        {
                            // Si falla la foto, retornamos igual los datos
                            var http = webEx.Response as HttpWebResponse;
                            var msg = (http != null) ? $"Foto HTTP {http.StatusCode}" : webEx.Message;
                            return Json(new { success = true, data = persona, imageDataURL = (string)null, photoError = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    return Json(new { success = true, data = persona, imageDataURL }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public async Task<JsonResult> CancelarCasox(int id, string motivo)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada." });

            using (var db = new SqlConnection(csHelpdesk))
            {
                var filas = await db.ExecuteScalarAsync<long>(
                    "dbo.usp_Caso_Cancelar",
                    new { CasoID = id, Motivo = motivo ?? "", UsuarioID = u.EmployeeNumber },
                    commandType: CommandType.StoredProcedure
                );
                if (filas <= 0) return Json(new { success = false, message = "No se pudo cancelar el caso (¿ya no está Pendiente?)." });
            }
            GenerarCorreoCancelacionCaso(id, "Cancelado", motivo);
            return Json(new { success = true });
        }
        [HttpPost]
        public JsonResult CancelarCaso(int id)
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    var rows = db.Execute(@"
UPDATE dbo.Caso
   SET Estado='Cancelado', FechaActualizacion=SYSDATETIME()
 WHERE ID=@id AND Estado='Pendiente';", new { id });
                    if (rows <= 0) return Json(new { success = false, message = "El caso no está en estado Pendiente o no existe." });
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public CasoView ObtenerCasoPorId(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                return db.QueryFirstOrDefault<CasoView>(
                    "dbo.usp_CasosObtenerPorId",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        /* ====== Dashboard (reportería) ====== */

        public JsonResult DashboardData(string desde, string hasta)
        {
            DateTime d1 = string.IsNullOrWhiteSpace(desde) ? DateTime.Today.AddDays(-30) : DateTime.Parse(desde);
            DateTime d2 = string.IsNullOrWhiteSpace(hasta) ? DateTime.Today : DateTime.Parse(hasta);

            using (var db = new SqlConnection(connectionString))
            {
                var kpi = db.Query("dbo.usp_Dashboard_KPI", new { Desde = d1.Date, Hasta = d2.Date }, commandType: CommandType.StoredProcedure).ToList();
                var dist = db.Query("dbo.usp_Dashboard_TipoSubtipo", new { Desde = d1.Date, Hasta = d2.Date }, commandType: CommandType.StoredProcedure).ToList();
                var aging = db.QueryFirstOrDefault("dbo.usp_Dashboard_Aging", new { Desde = d1.Date, Hasta = d2.Date }, commandType: CommandType.StoredProcedure);

                return Json(new { kpi, dist, aging }, JsonRequestBehavior.AllowGet);
            }
        }
