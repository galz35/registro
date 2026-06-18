        #region Helpers
        public void NotificarNotaCaso(int casoId, string nombreSoporte, string correoSoporte)
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    // Obtener info del caso
                    var caso = db.QueryFirstOrDefault<NotificacionCasoDto>(
                        "SELECT ID AS CasoID, Titulo, Estado, UsuarioID AS CorreoSolicitante FROM dbo.Caso WHERE ID=@id",
                        new { id = casoId });

                    if (caso != null)
                    {
                        string asunto = $"[Helpdesk] Nueva nota en caso #{casoId} - {caso.Titulo}";
                        string cuerpo = $"Estimado {nombreSoporte}, se ha agregado una nota al caso #{casoId}.";

                        // Enviar correo (puedes usar tu helper getcorreohelpapi)
                        getcorreohelpapi(correoSoporte, "", asunto, cuerpo, casoId);
                    }
                }
            }
            catch { }
        }
        public void NotificarNotaCasousuario(int casoId, string nombreSoporte, string correoSoporte)
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    // Obtener info del caso
                    //var caso = db.QueryFirstOrDefault<NotificacionCasoDto>(
                    //    "SELECT ID AS CasoID, Titulo, Estado, UsuarioID AS CorreoSolicitante FROM dbo.Caso WHERE ID=@id",
                    //    new { id = casoId });
                    var caso = db.QueryFirstOrDefault<NotificacionCasoDto>(
                        "SELECT ID AS CasoID, Titulo, Estado, Correo AS CorreoSolicitante FROM dbo.Caso WHERE ID=@id",
                        new { id = casoId });

                    if (caso != null)
                    {
                        string asunto = $"[Helpdesk] Nueva nota en caso #{casoId} - {caso.Titulo}";
                        string cuerpo = $"Estimado usuario, se ha agregado una nota al caso #{casoId}.";

                        // Enviar correo (puedes usar tu helper getcorreohelpapi)
                        getcorreohelpapi(caso.CorreoSolicitante, "", asunto, cuerpo, casoId);
                    }
                }
            }
            catch { }
        }
        private string CurrentUserEmail
        {
            get
            {
                var u = Session["User"] as Entities.Employees;
                return u?.EmailAddress ?? "";
            }
        }
        public static byte[] ReadFully(Stream input, int length)
        {
            using (var ms = new MemoryStream(length))
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
        private static string MapMimeByExtension(string ext)
        {
            switch (ext)
            {
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".webp": return "image/webp";
                case ".pdf": return "application/pdf";
                case ".doc": case ".docx": return "application/msword";
                case ".xls": case ".xlsx": return "application/vnd.ms-excel";
                default: return "application/octet-stream";
            }
        }
        private static byte[] ToWebP(HttpPostedFileBase file)
        {
            // Magick.NET (Convertir a WebP)
            using (var ms = new MemoryStream())
            {
                file.InputStream.CopyTo(ms);
                ms.Position = 0;
                using (var image = new MagickImage(ms))
                {
                    image.Format = MagickFormat.WebP;
                    image.Quality = 75; // compresión
                    return image.ToByteArray();
                }
            }
        }
        private static int SafeInt(string val)
        {
            if (int.TryParse(val, out int x)) return x;
            return 0;
        }
        private bool IsImagenMime(string mime, string ext)
        {
            // Lógica simple O estricta
            if (mime.StartsWith("image/")) return true;
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp") return true;
            return false;
        }

        private byte[] ReadAllBytes(Stream stream)
        {
            if (stream is MemoryStream ms) return ms.ToArray();
            using (var m = new MemoryStream())
            {
                stream.CopyTo(m);
                return m.ToArray();
            }
        }
        private double? CalcularTiempoAtencionHabilMin(DateTime inicio, DateTime fin)
        {
            // Lógica simplificada: fin - inicio (TimeSpan.TotalMinutes)
            // Si quieres descontar fines de semana / horarios, implementa aquí.
            if (fin < inicio) return 0;
            return (fin - inicio).TotalMinutes;
        }

        private string CalcularTiempoAtencionHabilStr(DateTime inicio, DateTime fin)
        {
            if (fin < inicio) return "0min";
            var span = fin - inicio;
            // E.g. "2d 5h 30m"
            return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m";
        }

        public string JsonNet(object data)
        {
            return JsonConvert.SerializeObject(data);
        }
        // Helper estático para convertir a WebP (reutilizable)
        private static byte[] ConvertToWebP(HttpPostedFileBase f)
        {
            using (var ms = new MemoryStream())
            {
                f.InputStream.CopyTo(ms);
                ms.Position = 0;
                using (var image = new MagickImage(ms))
                {
                    image.Format = MagickFormat.WebP;
                    image.Quality = 70;
                    return image.ToByteArray();
                }
            }
        }
        public async Task<CasoView> ObtenerCasoByIdAsync(int id)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var caso = await db.QueryFirstOrDefaultAsync<CasoView>(
                    "dbo.usp_CasosObtenerPorId",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );
                return caso;
            }
        }

        public void UpsertCasoEnSesion(CasoView c)
        {
            var list = Session["casos"] as List<CasoView>;
            if (list == null) return;
            var idx = list.FindIndex(x => x.ID == c.ID);
            if (idx >= 0) list[idx] = c;
            else list.Insert(0, c);
            Session["casos"] = list;
        }
