        /* ====== Correo ====== */

        private async Task GenerarCorreoCreacionCasoSoporteAsync(int casoId)
        {
            // 1. Obtener datos del caso (con SP enriquecido)
            var c = await ObtenerCasoByIdAsync(casoId);
            if (c == null) return;

            // 2. Determinar destinatarios
            //    - Soporte asignado (si hubiera lógica de auto-asignación, aquí iría)
            //    - O a la lista de distribución de la categoría
            //    En este ejemplo: mandamos a una cuenta fija de Helpdesk o recuperamos correos según categoría
            string emailSoporte = "soporte@claro.com.ni"; // Default

            // Si hay lógica de categoría → obtener correo responsable
            /*
            if (c.CategoriaID > 0) { ... emailSoporte = ... }
            */

            // 3. Armar HTML
            var asunto = $"[Helpdesk] Nuevo Caso #{c.ID} - {c.Titulo}";

            // Usamos plantilla HTML limpia
            var mensaje = $@"
<html>
<head>
<style>
  body {{ font-family: Arial, sans-serif; color: #333; }}
  .box {{ max-width: 600px; margin: 0 auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px; }}
  .header {{ background-color: #d32f2f; color: #fff; padding: 10px; border-radius: 4px; text-align: center; }}
  .row {{ margin-bottom: 10px; }}
  .label {{ font-weight: bold; width: 120px; display: inline-block; }}
  .footer {{ font-size: 12px; color: #777; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
</style>
</head>
<body>
<div class='box'>
  <div class='header'>
    <h2>Nuevo Caso #{c.ID}</h2>
  </div>
  <p>Se ha registrado un nuevo caso en el sistema.</p>
  
  <div class='row'><span class='label'>Solicitante:</span> {c.NombreAutor} ({c.CorreoAutor})</div>
  <div class='row'><span class='label'>Afectado:</span> {c.NombreResponsable} ({c.Correo})</div>
  <div class='row'><span class='label'>Gerencia:</span> {c.GerenciaResponsable}</div>
  <div class='row'><span class='label'>Tipo:</span> {c.TipoNombre} / {c.SubtipoNombre}</div>
  <div class='row'><span class='label'>Prioridad:</span> {c.Prioridad}</div>
  
  <hr/>
  <div class='row'>
    <span class='label'>Asunto:</span> <strong>{c.Titulo}</strong>
  </div>
  <div>
    <span class='label'>Descripción:</span>
    <p>{c.Descripcion}</p>
  </div>

  <div class='footer'>
    Helpdesk RhOnline - Generado automáticamente
  </div>
</div>
</body>
</html>";

            // 4. Registrar en cola de correos (MensajeBD / API)
            //    Tu sistema usa usp_CorreoCaso_Insert o similar
            //    Aquí simulo la llamada a tu API o DB de correos
            int idCorreo = 0;
            using (var db = new SqlConnection(connectionString))
            {
                // Convertir HTML a "numerico" si es requisito de tu legacy
                var bodySafe = EncodeHtmlToNumeric(mensaje);

                idCorreo = await db.ExecuteScalarAsync<int>("dbo.usp_CorreoCaso_Insert", new
                {
                    CasoID = c.ID,
                    TipoCaso = "Creacion",
                    Estado = "Pendiente", // estado del envío
                    Asunto = asunto,
                    ContenidoHtml = bodySafe
                }, commandType: CommandType.StoredProcedure);
            }

            // 5. Invocar API de envío (fire & forget o await)
            getcorreohelpapi(emailSoporte, c.CorreoAutor, asunto, idCorreo.ToString(), c.ID);
        }

        private void GenerarCorreoCancelacionCaso(int id, string nuevoEstado, string motivo)
        {
            // Lógica similar: armar HTML avisando cancelación
        }
        private void GenerarCorreoUsuario(int casoId, string estado)
        {
            // Avisar al usuario "Tu caso cambió a estado X"
            // Recuperar caso
            using (var db = new SqlConnection(connectionString))
            {
                // ...
            }
        }
        private string GenerarCorreoCierreCaso(int casoId, string estado)
        {
            var c = ObtenerCasoPorId(casoId);
            if (c == null) return "Caso no encontrado";

            // Paleta de colores Claro (aproximada)
            var colorHeader = "#e11d48"; // Rojo corporativo (ejemplo)
            var bgBody = "#f4f4f9";
            var bgCard = "#ffffff";

            // Formatear fechas
            var fCreacion = c.FechaCreacion.HasValue ? c.FechaCreacion.Value.ToString("dd/MM/yyyy HH:mm") : "-";
            var fCierre = c.FechaFinalizacion.HasValue ? c.FechaFinalizacion.Value.ToString("dd/MM/yyyy HH:mm") : DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // Encode básico
            Func<string, string> esc = (s) => HttpUtility.HtmlEncode(s ?? "");

            var asunto = $"Encuesta de Satisfacción - Caso #{c.ID}: {c.Titulo}";

            // URL encuesta (ajusta tu dominio real)
            var linkEncuesta = $"http://192.168.8.234/rhonline/Helpdesk2025/Encuesta?t={c.ID}"; // ejemplo

            var html = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: {bgBody}; margin: 0; padding: 20px; color: #333; }}
  .container {{ max-width: 650px; margin: 0 auto; background-color: {bgCard}; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
  .header {{ background-color: {colorHeader}; color: white; padding: 20px; text-align: center; }}
  .header h1 {{ margin: 0; font-size: 24px; font-weight: normal; }}
  .content {{ padding: 30px; }}
  .info-grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 20px; font-size: 14px; }}
  .info-item label {{ display: block; color: #777; font-size: 12px; margin-bottom: 4px; }}
  .info-item strong {{ font-size: 15px; color: #000; }}
  .status-badge {{ background: #22c55e; color: white; padding: 4px 10px; border-radius: 12px; font-size: 12px; font-weight: bold; display: inline-block; }}
  .desc-box {{ background: #f9f9f9; border-left: 4px solid #ddd; padding: 15px; font-style: italic; margin: 20px 0; }}
  .btn {{ display: block; width: 200px; margin: 30px auto; text-align: center; background-color: {colorHeader}; color: white; text-decoration: none; padding: 12px 0; border-radius: 25px; font-weight: bold; font-size: 16px; transition: opacity 0.3s; }}
  .btn:hover {{ opacity: 0.9; }}
  .footer {{ background: #eee; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
</style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>Caso Cerrado</h1>
      <div style='font-size:14px; opacity:0.9; margin-top:5px'>Ticket #{c.ID}</div>
    </div>
    <div class='content'>
      <p>Hola <strong>{esc(c.NombreAutor)}</strong>,</p>
      <p>Te informamos que tu caso ha sido resuelto y cerrado por nuestro equipo de soporte.</p>
      
      <div class='info-grid'>
         <div class='info-item'><label>Título</label><strong>{esc(c.Titulo)}</strong></div>
         <div class='info-item'><label>Estado</label><span class='status-badge'>Finalizado</span></div>
         <div class='info-item'><label>Fecha Creación</label><strong>{fCreacion}</strong></div>
         <div class='info-item'><label>Fecha Cierre</label><strong>{fCierre}</strong></div>
         <div class='info-item'><label>Atendido por</label><strong>{esc(c.Nombresoport)}</strong></div>
         <div class='info-item'><label>Categoría</label><strong>{esc(c.TipoNombre)}</strong></div>
      </div>

      <div class='desc-box'>
        ""{esc(c.NotasCierre)}""
      </div>

      <p style='text-align:center; margin-top:30px'>
        Tu opinión es muy importante para mejorar nuestro servicio.
        <br>Por favor califica la atención recibida:
      </p>

      <a href='{linkEncuesta}' class='btn'>Calificar Servicio</a>
    </div>
    <div class='footer'>
      Helpdesk System • RhOnline
      <br><small>No respondas a este correo.</small>
    </div>
  </div>
</body>
</html>";

            // Guardar en DB de correos y enviar
            // ... (tu lógica de db.Execute usp_CorreoCaso_Insert ...)
            // getcorreohelpapi(c.CorreoAutor, "", asunto, idCorreo);
            var payload = EncodeHtmlToNumeric(html);
            int idcorreo = 0;
            using (var db = new SqlConnection(connectionString))
            {
                // Convertir HTML a "numerico" si es requisito de tu legacy


                idcorreo = db.ExecuteScalar<int>("dbo.usp_CorreoCaso_Insert", new
                {
                    CasoID = c.ID,
                    TipoCaso = "Cierre",
                    Estado = "Pendiente", // estado del envío
                    Asunto = asunto,
                    ContenidoHtml = payload
                }, commandType: CommandType.StoredProcedure);
            }
            return getcorreohelpapi(c.CorreoAutor, "", asunto, idcorreo.ToString(), c.ID);
            // return html; // Para debug
        }
        private string GenerarCorreoAsignacionCaso(int casoId, string soporteId, string mensajeAsignacion)
        {
            var c = ObtenerCasoPorId(casoId);
            if (c == null) return "Caso no encontrado";

            // Paleta
            var colorHeader = "#0ea5e9"; // Azul claro para asignación
            var bgBody = "#f0f9ff";

            Func<string, string> esc = (s) => HttpUtility.HtmlEncode(s ?? "");
            var fCreacion = c.FechaCreacion.HasValue ? c.FechaCreacion.Value.ToString("dd/MM/yyyy HH:mm") : "-";

            var asunto = $"[Asignado] Caso #{c.ID}: {c.Titulo}";

            var html = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: {bgBody}; margin: 0; padding: 20px; color: #333; }}
  .container {{ max-width: 600px; margin: 0 auto; background-color: #fff; border: 1px solid #bae6fd; border-radius: 8px; overflow: hidden; }}
  .header {{ background-color: {colorHeader}; color: white; padding: 16px; text-align: center; }}
  .content {{ padding: 24px; }}
  .label {{ color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; margin-bottom: 4px; }}
  .value {{ font-size: 15px; color: #0f172a; margin-bottom: 16px; display: block; }}
  .note {{ background: #e0f2fe; border-left: 4px solid #0ea5e9; padding: 12px; margin: 20px 0; color: #0c4a6e; }}
  .btn {{ display: inline-block; background: {colorHeader}; color: white; padding: 10px 20px; text-decoration: none; border-radius: 6px; font-weight: bold; margin-top: 10px; }}
</style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h2 style='margin:0'>Caso Asignado</h2>
    </div>
    <div class='content'>
      <p>Hola <strong>{esc(soporteId)}</strong>,</p>
      <p>Se te ha asignado el siguiente caso:</p>

      <span class='label'>ID TICKET</span>
      <span class='value'>#{c.ID}</span>

      <span class='label'>SOLICITANTE</span>
      <span class='value'>{esc(c.NombreAutor)} <small>({c.CorreoAutor})</small></span>

      <span class='label'>TÍTULO</span>
      <span class='value'>{esc(c.Titulo)}</span>

      <span class='label'>CATEGORÍA</span>
      <span class='value'>{esc(c.TipoNombre)} / {esc(c.SubtipoNombre)}</span>

      <div class='note'>
        <strong>Nota de asignación:</strong><br>
        {esc(mensajeAsignacion)}
      </div>

      <a href='http://192.168.8.234/rhonline/Helpdesk2025/Soporte' class='btn'>Ver en Panel</a>
    </div>
  </div>
</body>
</html>";
            var payload = EncodeHtmlToNumeric(html);
            int idcorreo = 0;
            using (var db = new SqlConnection(connectionString))
            {
                // Convertir HTML a "numerico" si es requisito de tu legacy


                idcorreo = db.ExecuteScalar<int>("dbo.usp_CorreoCaso_Insert", new
                {
                    CasoID = c.ID,
                    TipoCaso = "Asignacion",
                    Estado = "Pendiente", // estado del envío
                    Asunto = asunto,
                    ContenidoHtml = payload
                }, commandType: CommandType.StoredProcedure);
            }
            return getcorreohelpapi(soporteId, "", asunto, idcorreo.ToString(), c.ID);
            // return GuardarYEnviar(c.CorreoSoport ?? soporteId, asunto, html, "Asignacion");
        }
        private string GenerarCorreoNotaSeguimiento(int casoId, string autor, string mensaje)
        {
            var c = ObtenerCasoPorId(casoId);
            if (c == null) return "Caso no encontrado";

            // Paleta
            var colorHeader = "#6366f1"; // Indigo
            var bgBody = "#eef2ff";

            Func<string, string> esc = (s) => HttpUtility.HtmlEncode(s ?? "");

            // Determinar si es nota de usuario o de soporte para dirigir el correo
            // (Lógica simplificada: se envía al "otro" actor)
            var asunto = $"[Nota] Caso #{c.ID}: {c.Titulo}";

            var html = $@"
<html>
<body>
  <div style='background:{bgBody}; padding:20px; font-family:sans-serif;'>
    <div style='background:#fff; max-width:600px; margin:0 auto; padding:20px; border-top: 5px solid {colorHeader};'>
      <h3 style='color:{colorHeader}; margin-top:0;'>Nuevo comentario</h3>
      <p><strong>{esc(autor)}</strong> ha agregado una nota al caso <strong>#{c.ID}</strong>.</p>
      
      <div style='background:#f8fafc; padding:15px; border:1px solid #e2e8f0; border-radius:6px;'>
        {esc(mensaje)}
      </div>

      <br>
      <small style='color:#999'>Puedes responder agregando otra nota en el sistema.</small>
    </div>
  </div>
</body>
</html>";
            var payload = EncodeHtmlToNumeric(html);
            int idcorreo = 0;
            using (var db = new SqlConnection(connectionString))
            {
                // Convertir HTML a "numerico" si es requisito de tu legacy


                idcorreo = db.ExecuteScalar<int>("dbo.usp_CorreoCaso_Insert", new
                {
                    CasoID = c.ID,
                    TipoCaso = "Nota",
                    Estado = "Pendiente", // estado del envío
                    Asunto = asunto,
                    ContenidoHtml = payload
                }, commandType: CommandType.StoredProcedure);
            }
            return getcorreohelpapi(c.CorreoAutor, c.CorreoSoport, asunto, idcorreo.ToString(), c.ID);
            // return GuardarYEnviar(destinatario, asunto, html, "Nota");
        }
        private string EnviarCorreoAsignacion(int CasoID, string SoporteID, string Nota)
        {

            return GenerarCorreoAsignacionCaso(CasoID, SoporteID, Nota);
        }
        // ================= Métodos legacy envío =================
        public string getcorreohelpapi(string para, string cc, string asunto, string idcorreo, int casoid = 0)
        {
            string url = "";
            try
            {
                // var client = new RestClient("http://172.26.54.66/apihcm/api/values/correo/correohelpdesk2025");
                var client = new RestClient(correoApiBase);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                // request.AddBody(new { para = "gustavo.lira@claro.com.ni", copias = "gustavo.lira@claro.com.ni", asunto = asunto, idcorreo = idcorreo});
                request.AddBody(new { para = para, copias = cc, asunto = asunto, idcorreo = idcorreo });
                IRestResponse response = client.Execute(request);

                // Log opcional response.Content
                Console.WriteLine(response.Content);
                url = response.ResponseUri.ToString();
                return response.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine(url);
                return ex.Message;
            }
        }
        public string getcorreohelp(string para, string cc, string asunto, string body)
        {
            try
            {
                MailMessage correo = new MailMessage();
                correo.From = new MailAddress(MAIL_FROM, "Helpdesk RhOnline", System.Text.Encoding.UTF8);
                correo.To.Add(para);
                if (!string.IsNullOrEmpty(cc)) correo.CC.Add(cc);
                correo.Subject = asunto;
                correo.Body = body;
                correo.IsBodyHtml = true;
                correo.Priority = MailPriority.Normal;

                SmtpClient smtp = new SmtpClient();
                smtp.UseDefaultCredentials = false;
                smtp.Host = MAIL_SMTP_HOST;
                smtp.Port = MAIL_SMTP_PORT;
                smtp.Credentials = new System.Net.NetworkCredential(MAIL_SMTP_USER, MAIL_SMTP_PASS);
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                smtp.EnableSsl = true; // El servidor 587 suele pedir SSL, revisa tu infra

                smtp.Send(correo);
                return "1";
            }
            catch (Exception ex)
            {
                return "0" + ex.Message;
            }
        }

        public static string EncodeHtmlToNumeric(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in html)
            {
                if (c > 127)
                {
                    sb.Append("&#");
                    sb.Append((int)c);
                    sb.Append(";");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        // === HTML correo (similar a "creación") pero enfatiza el cambio de categoría ===
        // C# 7.3 — Recategorización con mismo “look & flow” del correo de creación.
        // Usa misma paleta/estructura, inserta en usp_CorreoCaso_Insert y envía con getcorreohelpapi.
        private async Task<string> GenerarCorreoRecategorizacionCasoAsync(int id, RecategorizacionDTO dto)
        {
            var c = ObtenerCasoPorId(id);

            // Encode básico anti-inyección
            Func<string, string> E = s => HttpUtility.HtmlEncode(s ?? "-");
            Func<DateTime?, string> EDate = d => d.HasValue ? d.Value.ToString("dd/MM/yyyy HH:mm") : "-";

            // Campos (según DTO)
            var idTxt = c.ID;                                  // ID del caso
            var titulo = E(c.Titulo);
            var tipoAnt = E(dto.TipoAnterior);
            var subAnt = E(dto.SubtipoAnterior);
            var tipoNew = E(dto.TipoNuevo);
            var subNew = E(dto.SubtipoNuevo);
            var prioridad = E(c.Prioridad);
            var fCreTxt = EDate(c.FechaCreacion);
            var fRecTxt = DateTime.Now.ToString();         // si no existe en tu DTO, usa DateTime.Now
            if (fRecTxt == "-") fRecTxt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            var nomAutor = E(c.NombreAutor);
            var nomAfect = E(c.NombreResponsable);
            var nomSop = E(c.Nombresoport);

            var asunto = $"Helpdesk Tick-{idTxt}: Caso Recategorizado - {titulo}";

            // HTML unificado (mismo estilo que “Creación”)
            var mensaje = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:Arial,Helvetica,sans-serif;color:#111;background:#f4f4f9;margin:0;padding:24px}}
  .wrap{{max-width:720px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 10px 24px rgba(0,0,0,.10);overflow:hidden}}
  .hdr{{background:#e11d48;color:#fff;padding:22px 26px}}
  .hdr h1{{margin:0;line-height:1.25;font-size:22px;display:flex;flex-wrap:wrap;gap:8px;align-items:center}}
  .ticket,.titulo,.act{{white-space:nowrap}} .dash{{opacity:.8}}
  .badge-new{{display:inline-block;background:#fee2e2;color:#7f1d1d;border:1px solid #fecaca;padding:4px 10px;border-radius:999px;font-size:12px}}
  .cnt{{padding:22px 26px}}
  .kpis{{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}}
  .pill{{border:1px solid #e5e7eb;border-radius:999px;padding:6px 10px;font-size:12px;background:#f3f4f6;color:#111}}
  .chg{{border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;margin:10px 0;background:#f9fafb}}
  .chg .row{{display:flex;flex-wrap:wrap;gap:10px;align-items:center}}
  .arrow{{opacity:.7;padding:0 6px}}
  table.sum{{width:100%;border-collapse:collapse;margin:12px 0 6px 0}}
  table.sum th,table.sum td{{border:1px solid #e5e7eb;padding:10px;font-size:13px;text-align:left;vertical-align:top}}
  table.sum th{{background:#f9fafb;color:#374151;width:28%}}
  .grid{{display:flex;flex-wrap:wrap;gap:12px}}
  .card{{flex:1 1 240px;border:1px solid #e5e7eb;border-radius:10px;padding:14px 16px;min-width:240px}}
  .card h3{{margin:0 0 8px 0;font-size:14px;color:#111}}
  .pair{{font-size:13px;margin:6px 0}} .pair span{{color:#6b7280}}
  .ftr{{background:#f9fafb;color:#6b7280;text-align:center;font-size:12px;padding:16px}}
  @media (max-width:520px){{ .hdr h1{{font-size:18px}} .titulo{{flex:1 1 100%}} }}
</style>
</head>
<body>
  <div class='wrap'>
    <div class='hdr'>
      <h1>
        <span class='ticket'>Ticket #<strong>{idTxt}</strong></span>
        <span class='dash'>—</span>
        <span class='titulo'><strong>{titulo}</strong></span>
        <span class='badge-new'>Recategorizado</span>
      </h1>
    </div>

    <div class='cnt'>
      <div class='kpis'>
        <div class='pill'>Tipo (antes): <strong>{tipoAnt} / {subAnt}</strong></div>
        <div class='pill'>Tipo (ahora): <strong>{tipoNew} / {subNew}</strong></div>
        <div class='pill'>Prioridad: <strong>{prioridad}</strong></div>
        <div class='pill'>Creación: <strong>{fCreTxt}</strong></div>
        <div class='pill'>Recategorización: <strong>{fRecTxt}</strong></div>
      </div>

      <div class='chg'>
        <div class='row'>
          <div><strong>Antes:</strong> {tipoAnt} / {subAnt}</div>
          <div class='arrow'>⟶</div>
          <div><strong>Ahora:</strong> {tipoNew} / {subNew}</div>
        </div>
      </div>

      <table class='sum' role='presentation' aria-hidden='true'>
        <tr><th>Solicitante</th><td>{nomAutor}</td></tr>
        <tr><th>Colaborador afectado</th><td>{nomAfect}</td></tr>
        <tr><th>Soporte</th><td>{nomSop}</td></tr>
      </table>

    
    </div>

    <div class='ftr'>Mensaje automático de Helpdesk. No responder a este correo.</div>
  </div>
</body>
</html>";

            // Persistencia + envío (mismo flujo que “Creación”)
            var payload = EncodeHtmlToNumeric(mensaje);           // ↳ convierte a entidades numéricas
            var idcorreo = 0;

            using (var db = new SqlConnection(connectionString))
            {
                idcorreo = db.ExecuteScalar<int>(
                    "dbo.usp_CorreoCaso_Insert",
                    new
                    {
                        CasoID = c.ID,
                        TipoCaso = tipoNew,                       // guarda tipo “nuevo” como referencia
                        Estado = "Pendiente",
                        Asunto = asunto,
                        ContenidoHtml = payload
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            // Destinatarios: soporte + autor (ajusta si tu DTO trae correo de autor/soporte)
            var apiResp = getcorreohelpapi("soporte@claro.com.ni", c.CorreoAutor ?? "", asunto, idcorreo.ToString(), c.ID);

            await Task.Yield(); // mantiene firma async sin bloqueos
            return apiResp;
        }

        // util: une correos en CSV evitando vacíos/duplicados
        private static string JoinCsv(params string[] correos)
        {
            var hs = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < correos.Length; i++)
            {
                var c = (correos[i] ?? "").Trim();
                if (c.Length > 0) hs.Add(c);
            }
            return string.Join(",", hs);
        }
        #endregion
