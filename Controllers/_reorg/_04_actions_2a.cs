        [HttpPost]
        public async Task<JsonResult> MarcarEnProceso(int id)
        {
            var u = Session["User"] as Entities.Employees;
            if (u == null) return Json(new { success = false, message = "Sesión expirada" });

            try
            {
                using (var cn = new SqlConnection(csHelpdesk))
                {
                    // Validar estado actual
                    var estado = await cn.ExecuteScalarAsync<string>("SELECT Estado FROM dbo.Caso WHERE ID=@id", new { id });
                    if (estado != "Pendiente" && estado != "Asignado")
                        return Json(new { success = false, message = "El caso ya no está Pendiente/Asignado." });

                    // Actualizar a En Proceso
                    await cn.ExecuteAsync(
                        "dbo.usp_Caso_CambiarEstado",
                        new { CasoID = id, NuevoEstado = "En Proceso", UsuarioID = u.EmployeeNumber, Nota = "Iniciada atención" },
                        commandType: CommandType.StoredProcedure
                    );

                    // Notificar usuario
                    GenerarCorreoUsuario(id, "En Proceso");

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
