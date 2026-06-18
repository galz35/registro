        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<ActionResult> CerrarCaso(
    int CasoID,
    string NotasCierre,
    HttpPostedFileBase AdjuntoEvidencia
)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null)
                return Json(new { success = false, message = "Sesión expirada." });

            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    await cn.OpenAsync();

                    // Traemos la FechaCreacion para calcular duración
                    var casoInfo = await cn.QueryFirstOrDefaultAsync<dynamic>(
                        @"SELECT ID, FechaCreacion
                  FROM dbo.Caso
                  WHERE ID = @ID",
                        new { ID = CasoID }
                    );

                    if (casoInfo == null)
                    {
                        return Json(new { success = false, message = "Caso no encontrado." });
                    }

                    DateTime fechaCreacion = (DateTime)casoInfo.FechaCreacion;
                    DateTime fechaFin = DateTime.Now;

                    // calculamos duración hábil
                    var tiempoMin = CalcularTiempoAtencionHabilMin(fechaCreacion, fechaFin);
                    var tiempoTexto = CalcularTiempoAtencionHabilStr(fechaCreacion, fechaFin);

                    using (var tx = cn.BeginTransaction())
                    {
                        // 1) Llamar SP actualizado
                        var p = new DynamicParameters();
                        p.Add("@Correo", u.EmailAddress);
                        p.Add("@CasoID", CasoID);
                        p.Add("@NotasCierre", (object)NotasCierre ?? DBNull.Value);
                        p.Add("@UsuarioAccion", u.FullName);
                        p.Add("@FechaFinalizacion", fechaFin);
                        p.Add("@TiempoAtencion", (object)tiempoTexto ?? DBNull.Value);
                        p.Add("@TiempoAtencionMinutos", tiempoMin);

                        var ok = await cn.ExecuteScalarAsync<int>(
                            "dbo.usp_Caso_Cerrar_v1",
                            p,
                            tx,
                            commandType: CommandType.StoredProcedure
                        );

                        if (ok != 1)
                        {
                            tx.Rollback();
                            return Json(new { success = false, message = "No fue posible cerrar el caso." });
                        }

                        // 2) Guardar evidencia (opcional)
                        if (AdjuntoEvidencia != null && AdjuntoEvidencia.ContentLength > 0)
                        {
                            var allowed = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
                            var contentType = (AdjuntoEvidencia.ContentType ?? "").ToLowerInvariant();

                            if (!allowed.Contains(contentType))
                            {
                                tx.Rollback();
                                return Json(new { success = false, message = "Tipo de archivo no permitido." });
                            }
                            if (AdjuntoEvidencia.ContentLength > 10 * 1024 * 1024) // 10MB
                            {
                                tx.Rollback();
                                return Json(new { success = false, message = "El archivo excede 10MB." });
                            }

                            var bytes = ToWebP(AdjuntoEvidencia); // tu helper actual

                            var p2 = new DynamicParameters();
                            p2.Add("@CasoID", CasoID);
                            p2.Add("@NombreArchivo", Path.GetFileName(AdjuntoEvidencia.FileName));
                            p2.Add("@TipoArchivo", "image/webp");
                            p2.Add("@DatosArchivo", bytes, DbType.Binary);
                            p2.Add("@UsuarioAccion", u.EmailAddress);
                            p2.Add("@Tipo", "Soporte");

                            await cn.ExecuteAsync(
                                "dbo.usp_Archivo_InsertarEvidencia",
                                p2,
                                tx,
                                commandType: CommandType.StoredProcedure
                            );
                        }

                        // 3) Commit
                        tx.Commit();

                        // 4) Correo de notificación
                        GenerarCorreoCierreCaso(CasoID, "Cerrado");

                        return Json(new { success = true });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // Cerrar caso directo (con soporte actual)
        [HttpPost]
        public async Task<JsonResult> AtenderCasocerrado(int CasoID, string NotasCierre)
        {
            try
            {
                var me = Session["User"] as Employees;
                if (me == null) return Json(new { success = false, message = "Sesión expirada." });

                long filas;
                using (var db = new SqlConnection(connectionString))
                {
                    filas = await db.ExecuteScalarAsync<long>(
                        "dbo.usp_Caso_Cerrar",
                        new { CasoID, NotasCierre, SoporteID = me.EmployeeNumber },
                        commandType: CommandType.StoredProcedure
                    );
                }
                if (filas <= 0) return Json(new { success = false, message = "No se encontró el caso." });

                try { GenerarCorreoCierreCaso(CasoID, "Cerrado"); } catch { }

                return Json(new { success = true, message = "Caso cerrado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }



        // Agregar evento (nota / cambio estado) + evidencia
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult AgregarEvento(int casoID, string tipoEvento, string nota, bool notaVisible, IEnumerable<HttpPostedFileBase> evidencias)
        {
            try
            {
                var me = Session["User"] as Employees;
                if (me == null) return Json(new { success = false, message = "Sesión expirada." });

                long eventoId;
                using (var db = new SqlConnection(connectionString))
                {
                    var r = db.QueryFirstOrDefault(
                        "dbo.usp_Caso_AgregarEvento",
                        new { CasoID = casoID, TipoEvento = tipoEvento, Nota = nota, NotaVisible = notaVisible, UsuarioAccion = me.EmployeeNumber },
                        commandType: CommandType.StoredProcedure);
                    eventoId = Convert.ToInt64(r.EventoID);
                }

                if (evidencias != null)
                {
                    using (var db = new SqlConnection(connectionString))
                    {
                        foreach (var f in evidencias)
                        {
                            if (f == null || f.ContentLength <= 0) continue;

                            byte[] data = ConvertToWebP(f); // compacta
                            db.Execute(
@"INSERT INTO dbo.CasoEventoArchivo (EventoID,NombreArchivo,TipoArchivo,DatosArchivo)
  VALUES (@EventoID,@NombreArchivo,@TipoArchivo,@DatosArchivo)",
                                new
                                {
                                    EventoID = eventoId,
                                    NombreArchivo = Path.GetFileName(f.FileName),
                                    TipoArchivo = "image/webp",
                                    DatosArchivo = data
                                });
                        }
                    }
                }

                return Json(new { success = true, message = "Evento agregado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Cambiar estado simple (wrapper si lo deseas)
        [HttpPost]
        public JsonResult CambiarEstado(int casoID, string nuevoEstado, string nota)
        {
            try
            {
                return AgregarEvento(casoID, nuevoEstado, nota, true, null);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
