using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class CampanasAVController : Controller
    {
        private const string RutaRedImagenes = @"\\172.26.54.66\Publicaciones\notificacion\";

        //[HttpGet]
        //public ActionResult ListJson()
        //{
        //    var list = new List<object>();
        //    using (var con = DbNotificameAV.Abrir())
        //    using (var cmd = new SqlCommand(@"
        //        SELECT Id,NombreCampana,RutaImagen,Activa,CreatedAt
        //        FROM dbo.Campanas
        //        ORDER BY Id DESC;", con))
        //    using (var rd = cmd.ExecuteReader())
        //    {
        //        while (rd.Read())
        //        {
        //            list.Add(new
        //            {
        //                Id = (int)rd["Id"],
        //                NombreCampana = rd["NombreCampana"] as string,
        //                RutaImagen = rd["RutaImagen"] as string,
        //                Activa = (bool)rd["Activa"],
        //                CreatedAt = (DateTime)rd["CreatedAt"]
        //            });
        //        }
        //    }
        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        //[HttpGet]
        //public ActionResult GetJson(int id)
        //{
        //    object item = null;

        //    using (var con = DbNotificameAV.Abrir())
        //    using (var cmd = new SqlCommand(@"
        //        SELECT TOP 1 Id,NombreCampana,MensajeTexto,RutaImagen,Activa
        //        FROM dbo.Campanas
        //        WHERE Id=@Id;", con))
        //    {
        //        cmd.Parameters.AddWithValue("@Id", id);

        //        using (var rd = cmd.ExecuteReader())
        //        {
        //            if (rd.Read())
        //            {
        //                item = new
        //                {
        //                    Id = (int)rd["Id"],
        //                    NombreCampana = rd["NombreCampana"] as string,
        //                    MensajeTexto = rd["MensajeTexto"] as string,
        //                    RutaImagen = rd["RutaImagen"] as string,
        //                    Activa = (bool)rd["Activa"]
        //                };
        //            }
        //        }
        //    }

        //    if (item == null) return HttpNotFound();
        //    return Json(item, JsonRequestBehavior.AllowGet);
        //}

        //[HttpPost]
        //public ActionResult SaveJson(int Id, string NombreCampana, string MensajeTexto, bool Activa, HttpPostedFileBase imagen)
        //{
        //    if (string.IsNullOrWhiteSpace(NombreCampana)) return new HttpStatusCodeResult(400, "Nombre requerido");
        //    if (string.IsNullOrWhiteSpace(MensajeTexto)) return new HttpStatusCodeResult(400, "Mensaje requerido");

        //    string rutaImagen = null;
        //    if (imagen != null && imagen.ContentLength > 0)
        //        rutaImagen = GuardarImagenUNC(imagen);

        //    var user = (User != null && User.Identity != null) ? User.Identity.Name : null;

        //    using (var con = DbNotificameAV.Abrir())
        //    {
        //        if (Id <= 0)
        //        {
        //            using (var cmd = new SqlCommand(@"
        //                INSERT INTO dbo.Campanas(NombreCampana,MensajeTexto,RutaImagen,Activa,CreatedBy)
        //                VALUES(@N,@M,@R,@A,@U);", con))
        //            {
        //                cmd.Parameters.AddWithValue("@N", NombreCampana.Trim());
        //                cmd.Parameters.AddWithValue("@M", MensajeTexto);
        //                cmd.Parameters.AddWithValue("@R", (object)rutaImagen ?? DBNull.Value);
        //                cmd.Parameters.AddWithValue("@A", Activa);
        //                cmd.Parameters.AddWithValue("@U", (object)user ?? DBNull.Value);
        //                cmd.ExecuteNonQuery();
        //            }
        //        }
        //        else
        //        {
        //            using (var cmd = new SqlCommand(@"
        //                UPDATE dbo.Campanas
        //                SET NombreCampana=@N,
        //                    MensajeTexto=@M,
        //                    RutaImagen=COALESCE(@R, RutaImagen),
        //                    Activa=@A,
        //                    UpdatedAt=SYSDATETIME(),
        //                    UpdatedBy=@U
        //                WHERE Id=@Id;", con))
        //            {
        //                cmd.Parameters.AddWithValue("@Id", Id);
        //                cmd.Parameters.AddWithValue("@N", NombreCampana.Trim());
        //                cmd.Parameters.AddWithValue("@M", MensajeTexto);
        //                cmd.Parameters.AddWithValue("@R", (object)rutaImagen ?? DBNull.Value);
        //                cmd.Parameters.AddWithValue("@A", Activa);
        //                cmd.Parameters.AddWithValue("@U", (object)user ?? DBNull.Value);
        //                cmd.ExecuteNonQuery();
        //            }
        //        }
        //    }

        //    return Json(new { ok = true });
        //}

        //[HttpPost]
        //public ActionResult DeleteJson(int id)
        //{
        //    using (var con = DbNotificameAV.Abrir())
        //    using (var cmd = new SqlCommand(@"DELETE FROM dbo.Campanas WHERE Id=@Id;", con))
        //    {
        //        cmd.Parameters.AddWithValue("@Id", id);
        //        cmd.ExecuteNonQuery();
        //    }
        //    return Json(new { ok = true });
        //}

        //private static string GuardarImagenUNC(HttpPostedFileBase archivo)
        //{
        //    var ext = (Path.GetExtension(archivo.FileName) ?? "").ToLowerInvariant();
        //    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
        //        throw new Exception("Extensión de imagen no permitida.");

        //    var nombre = Guid.NewGuid().ToString("N") + ext;
        //    var destino = Path.Combine(RutaRedImagenes, nombre);

        //    archivo.SaveAs(destino);
        //    return destino;
        //}
    
    
    }
}