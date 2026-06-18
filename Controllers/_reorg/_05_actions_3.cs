        // ====== Endpoints para “ver lo de otro soporte” ======
        [HttpGet]
        public async Task<JsonResult> Admin_GetVisibilidad(string viewerId)
        {
            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    var list = await cn.QueryAsync<SoporteVisibilidadDto>(
                        "dbo.usp_SoporteVisibilidad_Listar",
                        new { ViewerID = viewerId },
                        commandType: CommandType.StoredProcedure);

                    return Json(new RespuestaJson<IEnumerable<SoporteVisibilidadDto>>
                    {
                        Success = true,
                        Data = list
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new RespuestaJson<object>
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Admin_AddVisibilidad(string viewerId, string targetId)
        {
            if (string.IsNullOrWhiteSpace(viewerId) || string.IsNullOrWhiteSpace(targetId))
                return Json(new { Success = false, Message = "viewerId y targetId son obligatorios." });

            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    // SP debe validar duplicados y existencia
                    var id = await cn.ExecuteScalarAsync<int>(
                        "dbo.usp_SoporteVisibilidad_Agregar",
                        new { ViewerID = viewerId, TargetID = targetId },
                        commandType: CommandType.StoredProcedure);

                    return Json(new { Success = true, ID = id });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Admin_DelVisibilidad(int id)
        {
            if (id <= 0) return Json(new { Success = false, Message = "ID inválido." });

            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    await cn.ExecuteAsync(
                        "dbo.usp_SoporteVisibilidad_Eliminar",
                        new { ID = id },
                        commandType: CommandType.StoredProcedure);

                    return Json(new { Success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<JsonResult> GuardarSoporte(SoporteUpsertRequest m)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.SoporteID) || string.IsNullOrWhiteSpace(m.Email) || string.IsNullOrWhiteSpace(m.Nombre))
                return Json(new { success = false, message = "Datos de soporte incompletos." });

            using (var db = new SqlConnection(csHelpdesk))
            {
                await db.OpenAsync();
                using (var tx = db.BeginTransaction())
                {
                    try
                    {
                        // Upsert soporte
                        await db.ExecuteAsync("dbo.usp_Soporte_Upsert", new
                        {
                            SoporteID = m.SoporteID,
                            Email = m.Email,
                            Nombre = m.Nombre,
                            Area = m.Area,
                            Activo = m.Activo
                        }, transaction: tx, commandType: CommandType.StoredProcedure);

                        // TVP permisos
                        var tvp = new DataTable();
                        tvp.Columns.Add("TipoCasoID", typeof(int));
                        tvp.Columns.Add("SubtipoCasoID", typeof(int));
                        if (m.Permisos != null)
                        {
                            foreach (var p in m.Permisos)
                            {
                                var row = tvp.NewRow();
                                row["TipoCasoID"] = p.TipoCasoID;
                                row["SubtipoCasoID"] = (object)(p.SubtipoCasoID ?? (int?)null) ?? DBNull.Value;
                                tvp.Rows.Add(row);
                            }
                        }

                        var dp = new DynamicParameters();
                        dp.Add("@SoporteID", m.SoporteID, DbType.String);
                        dp.Add("@Permisos", tvp.AsTableValuedParameter("dbo.TVP_SoporteTipo"));

                        await db.ExecuteAsync("dbo.usp_SoporteTipo_Reemplazar", dp,
                            transaction: tx, commandType: CommandType.StoredProcedure);

                        tx.Commit();
                        return Json(new { success = true, message = "Soporte guardado correctamente." });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return Json(new { success = false, message = "Error al guardar soporte: " + ex.Message });
                    }
                }
            }
        }

        [HttpPost]
        public async Task<JsonResult> Admin_CambiarEstadoSoporte(string id, bool activo)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "SoporteID requerido." });

            using (var db = new SqlConnection(csHelpdesk))
            {
                var filas = await db.ExecuteScalarAsync<int>(
                    "dbo.usp_Soporte_CambiarEstado",
                    new { SoporteID = id, Activo = activo },
                    commandType: CommandType.StoredProcedure
                );
                return Json(new { success = filas > 0, message = filas > 0 ? "Estado actualizado." : "No se actualizó ningún registro." });
            }
        }
        [HttpGet]
        public async Task<JsonResult> ArbolTipoSubtipo()
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                using (var multi = await db.QueryMultipleAsync(
                    "dbo.usp_TipoSubtipo_Tree",
                    commandType: CommandType.StoredProcedure))
                {
                    var tipos = (await multi.ReadAsync<TipoNode>()).AsList();
                    var subs = (await multi.ReadAsync<SubNode>()).AsList();

                    var map = new Dictionary<int, TipoNode>();
                    foreach (var t in tipos)
                    {
                        t.Subtipos = new List<SubNode>();
                        map[t.TipoCasoID] = t;
                    }
                    foreach (var s in subs)
                    {
                        if (map.TryGetValue(s.TipoCasoID, out var t))
                            t.Subtipos.Add(s);
                    }
                    return Json(new { success = true, data = tipos }, JsonRequestBehavior.AllowGet);
                }
            }
        }
        [HttpGet]
        public JsonResult ObtenerSoportes(int tipoId, int? subtipoId)
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var data = db.Query<SoporteLite>(
                    "dbo.usp_Soportes_PorTipoSubtipo",
                    new { TipoCasoID = tipoId, SubtipoCasoID = subtipoId },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }
        // ===== Catálogos =====
        [HttpGet]
        public async Task<JsonResult> ListarTiposCasos()
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var lista = await db.QueryAsync<TipoItem>("dbo.usp_TipoCaso_Listar", commandType: CommandType.StoredProcedure);
                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<JsonResult> ListarSubtiposPorTipo(int tipoId)
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var lista = await db.QueryAsync<SubtipoItem>("dbo.usp_SubtipoCaso_ListarPorTipo",
                    new { TipoCasoID = tipoId }, commandType: CommandType.StoredProcedure);
                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===== Buscar empleados en sigho1.dbo.emp2024 =====
        [HttpGet]
        public async Task<JsonResult> BuscarEmpleados(string q)
        {
            using (var db = new SqlConnection(csHelpdesk))
            {
                var lista = await db.QueryAsync<EmpleadoItem>("dbo.usp_Empleados_Listar",
                    new { q }, commandType: CommandType.StoredProcedure);
                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===== Guardar/Actualizar Soporte y su matriz de permisos =====
        [HttpGet]
        public ActionResult KPI()
        {
            return View();
        }

        [HttpGet]
        public JsonResult KPIResumen(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPIResumenDto>("dbo.usp_KPI_Casos_Resumen", p, commandType: CommandType.StoredProcedure).FirstOrDefault();
                return Json(new { success = true, data = data ?? new KPIResumenDto() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPIPorDia(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPIPorDiaDto>("dbo.usp_KPI_Casos_PorDia", p, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPIPorTipo(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPIPorTipoDto>("dbo.usp_KPI_Casos_PorTipo", p, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPIPorGerencia(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPIPorGerenciaDto>("dbo.usp_KPI_Casos_PorGerencia", p, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPITiempos(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPITiemposDto>("dbo.usp_KPI_Casos_Tiempos", p, commandType: CommandType.StoredProcedure).FirstOrDefault();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPITopUsuarios(DateTime? desde, DateTime? hasta, int? tipoId, int? subtipoId, string gerencia, int? top)
        {
            var p = new DynamicParameters();
            p.Add("@Desde", desde ?? DateTime.Today.AddDays(-29));
            p.Add("@Hasta", hasta ?? DateTime.Today);
            p.Add("@Gerencia", string.IsNullOrWhiteSpace(gerencia) ? null : gerencia);
            p.Add("@TipoCasoID", tipoId);
            p.Add("@SubtipoCasoID", subtipoId);
            p.Add("@Top", top ?? 10);

            using (var db = new SqlConnection(connectionString))
            {
                var data = db.Query<KPITopUsuarioDto>("dbo.usp_KPI_TopUsuarios", p, commandType: CommandType.StoredProcedure).ToList();
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        // ====== Filtros auxiliares ======
        [HttpGet]
        public ActionResult ExportKpiCsv(DateTime? desde, DateTime? hasta, string gerencia, int? tipoCasoId, int? subtipoCasoId, bool? soloAtendidos)
        {
            DateTime d1 = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime d2 = hasta ?? DateTime.Today;
            bool onlyAttended = soloAtendidos ?? true;

            using (var db = new SqlConnection(connectionString))
            {
                var rows = db.Query<KpiExportRow>(
                    "dbo.usp_KPI_Casos_Export",
                    new { Desde = d1, Hasta = d2, Gerencia = string.IsNullOrWhiteSpace(gerencia) ? null : gerencia, TipoCasoID = tipoCasoId, SubtipoCasoID = subtipoCasoId, SoloAtendidos = onlyAttended ? 1 : 0 },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("ID,FechaCreacion,FechaAtencion,FechaCierre,Tipo,Subtipo,Soporte,SoporteEmail,GerenciaAfectado,Titulo,Descripcion,NotasCierre");
                foreach (var x in rows)
                {
                    Func<string, string> esc = s => string.IsNullOrEmpty(s) ? "" : "\"" + s.Replace("\"", "\"\"") + "\"";
                    sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}\r\n",
                        x.ID,
                        x.FechaCreacion.ToString("yyyy-MM-dd HH:mm"),
                        x.FechaAtencion.HasValue ? x.FechaAtencion.Value.ToString("yyyy-MM-dd HH:mm") : "",
                        x.FechaFinalizacion.HasValue ? x.FechaFinalizacion.Value.ToString("yyyy-MM-dd HH:mm") : "",
                        esc(x.TipoNombre),
                        esc(x.SubtipoNombre),
                        esc(x.SoporteNombre),
                        esc(x.SoporteEmail),
                        esc(x.GerenciaAfectado),
                        esc(x.Titulo),
                        esc(x.Descripcion),
                        esc(x.NotasCierre)
                    );
                }

                var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                var fileName = string.Format("KPI_Casos_{0:yyyyMMdd}_{1:yyyyMMdd}.csv", d1, d2);
                return File(bytes, "text/csv", fileName);
            }
        }
        [HttpGet]
        public JsonResult KPI_ListarGerencias()
        {
            using (var db = new SqlConnection(connectionString))
            {
                var rows = db.Query<string>("SELECT DISTINCT OGERENCIA as GERENCIA FROM sigho1.dbo.emp2024 WHERE ISNULL(OGERENCIA,'')<>'' and  OGERENCIA!='NI COORD. COMERCIAL SF MGA. ESTE-1' ORDER BY OGERENCIA").ToList();
                return Json(new { success = true, data = rows }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult KPI_ListarTipos()
        {
            using (var db = new SqlConnection(connectionString))
            {
                var rows = db.Query<dynamic>("SELECT TipoCasoID, Nombre FROM dbo.TipoCaso ORDER BY Nombre").ToList();
                return Json(new { success = true, data = rows }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult KPI_ListarTiposYSubtipos()
        {
            using (var db = new SqlConnection(connectionString))
            {
                // Traemos Tipo/Subtipo en una sola consulta
                var rows = db.Query<dynamic>(@"
            SELECT 
                tc.TipoCasoID,
                tc.Nombre      AS TipoNombre,
                st.SubtipoCasoID,
                st.Nombre      AS SubtipoNombre
            FROM dbo.TipoCaso tc
            INNER JOIN dbo.SubtipoCaso st
                ON tc.TipoCasoID = st.TipoCasoID
            ORDER BY tc.Nombre, st.Nombre;
        ").ToList();

                // Armamos estructura { TipoCasoID, Nombre, Subtipos:[...] }
                var lookup = new Dictionary<int, TipoDto>();

                foreach (var r in rows)
                {
                    int tipoId = (int)r.TipoCasoID;

                    if (!lookup.ContainsKey(tipoId))
                    {
                        lookup[tipoId] = new TipoDto
                        {
                            TipoCasoID = tipoId,
                            Nombre = (string)r.TipoNombre,
                            Subtipos = new List<SubtipoDto>()
                        };
                    }

                    lookup[tipoId].Subtipos.Add(new SubtipoDto
                    {
                        SubtipoCasoID = (int)r.SubtipoCasoID,
                        Nombre = (string)r.SubtipoNombre
                    });
                }

                var result = lookup.Values
                                   .OrderBy(x => x.Nombre)
                                   .ToList();

                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult KPI_ListarSubtipos(int tipoCasoId)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var rows = db.Query<dynamic>("SELECT SubtipoCasoID, Nombre FROM dbo.SubtipoCaso WHERE TipoCasoID=@t ORDER BY Nombre", new { t = tipoCasoId }).ToList();
                return Json(new { success = true, data = rows }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Recategorizar(RecategorizacionVM vm)
        {
            if (vm == null || vm.CasoID <= 0 || vm.TipoCasoID <= 0 || vm.SubtipoCasoID <= 0)
                return Json(new { success = false, message = "Parámetros inválidos." });

            // usuario que ejecuta (ajusta según tu sesión)
            var usuarioAccion = (User != null && User.Identity != null) ? (User.Identity.Name ?? "") : "";

            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();

                    var p = new DynamicParameters();
                    p.Add("@CasoID", vm.CasoID, DbType.Int32);
                    p.Add("@TipoCasoID", vm.TipoCasoID, DbType.Int32);
                    p.Add("@SubtipoCasoID", vm.SubtipoCasoID, DbType.Int32);
                    p.Add("@Nota", vm.Nota, DbType.String);
                    p.Add("@UsuarioAccion", usuarioAccion, DbType.String);

                    // SP debe devolver una sola fila con los datos consolidados para correo
                    var dto = await cn.QueryFirstOrDefaultAsync<RecategorizacionDTO>(
                        "dbo.usp_Helpdesk_ReCategorizarCaso",
                        p, commandType: CommandType.StoredProcedure
                    );

                    if (dto == null)
                        return Json(new { success = false, message = "No se encontró el caso o no se aplicó el cambio." });

                    // genera HTML como "creación", pero indicando recategorización

                    // asunto claro
                    var asunto = "[Helpdesk] Caso #" + dto.ID + " recategorizado a " + (dto.TipoNuevo ?? "-") + " / " + (dto.SubtipoNuevo ?? "-");

                    // destinos (elige tu estrategia: del SP, o arma to/cc aquí)
                    var to = (dto.DestinatariosCSV ?? "").Trim();
                    if (string.IsNullOrEmpty(to))
                    {
                        // Fallback: autor + afectado + soporte
                        to = JoinCsv(dto.CorreoAutor, dto.CorreoResponsable, dto.CorreoSoporte);
                    }

                    // enviar – usa tu helper actual de envío HTML
                    // IMPORTANTE: reemplaza por tu servicio real (p.ej. CorreoHelper/CorreoService)
                    await GenerarCorreoRecategorizacionCasoAsync(vm.CasoID, dto);

                    return Json(new { success = true, message = "Recategorizado y notificado.", data = dto });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateInput(false)]

        public JsonResult RecategorizarCaso(int casoId, int tipoId, int subtipoId)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    var p = new DynamicParameters();
                    p.Add("@CasoID", casoId, DbType.Int32);
                    p.Add("@TipoID", tipoId, DbType.Int32);
                    p.Add("@SubtipoID", subtipoId, DbType.Int32);

                    // OUTPUTs del SP
                    p.Add("@TipoAnterior", dbType: DbType.String, size: 120, direction: ParameterDirection.Output);
                    p.Add("@SubtipoAnterior", dbType: DbType.String, size: 120, direction: ParameterDirection.Output);
                    p.Add("@TipoNuevo", dbType: DbType.String, size: 120, direction: ParameterDirection.Output);
                    p.Add("@SubtipoNuevo", dbType: DbType.String, size: 120, direction: ParameterDirection.Output);

                    // Ejecuta 1 sola vez (SP hace todo y además retorna un SELECT para depurar si quieres)
                    cn.Execute("dbo.usp_Helpdesk_Caso_Recategorizacion_Detalle", p, commandType: CommandType.StoredProcedure);

                    // Armar DTO desde OUTPUTs
                    var dto = new RecategorizacionDTO
                    {
                        TipoAnterior = p.Get<string>("@TipoAnterior") ?? "-",
                        SubtipoAnterior = p.Get<string>("@SubtipoAnterior") ?? "-",
                        TipoNuevo = p.Get<string>("@TipoNuevo") ?? "-",
                        SubtipoNuevo = p.Get<string>("@SubtipoNuevo") ?? "-"
                    };

                    // Generar/registrar correo (bloqueante, sin async)
                    try { _ = GenerarCorreoRecategorizacionCasoAsync(casoId, dto); } catch { /* log opcional */ }

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }


        // PUT: Marcar En Proceso - se consume el API REST en la ruta: api/helpdesk/proceso?token=021092
        [HttpGet]
        [AllowAnonymous] // ← habilita prueba rápida; si no corresponde, quítalo
        public JsonResult ObtenerTiposCasos()
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    var tipos = cn.Query(
                        "usp_Helpdesk_Tipos_Listar",
                        commandType: CommandType.StoredProcedure
                    );
                    // clave: mismo contrato que KPIs → success + data
                    return Json(new { success = true, data = tipos }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public JsonResult ObtenerSubtiposPorTipo(int tipoId)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    var subtipos = cn.Query(
                        "usp_Helpdesk_Subtipos_PorTipo",
                        new { TipoID = tipoId },
                        commandType: CommandType.StoredProcedure
                    );
                    return Json(new { success = true, data = subtipos }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
