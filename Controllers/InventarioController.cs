using Dapper;
using slnRhonline.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace slnRhonline.Controllers
{
    public class InventarioController : Controller
    {
        private static SqlConnection Conn()
        {
            return Logica.Abrir(); // Tu string de conexión a RRHH_Inventario
        }

        // =========================================================================
        // 0. CLASES DTO (Data Transfer Objects) PARA RESPUESTAS TIPADAS
        // =========================================================================
        public class ResponseJson<T>
        {
            public bool ok { get; set; }
            public T data { get; set; }
            public string msg { get; set; }
            public int? bajoMinimo { get; set; }
        }

        public class AlmacenDto { public int IdAlmacen { get; set; } public string Nombre { get; set; } }
        public class ArticuloDto { public int IdArticulo { get; set; } public string Codigo { get; set; } public string Nombre { get; set; } public string Tipo { get; set; } }
        public class ComboDto { public string Id { get; set; } public string Txt { get; set; } }
        public class VarianteDto { public string Talla { get; set; } public string Sexo { get; set; } }

        public class InventarioStockDto
        {
            public int IdArticulo { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Tipo { get; set; }
            public string Talla { get; set; }
            public string Sexo { get; set; }
            public int StockActual { get; set; }
            public int StockMinimo { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public class MovHistDto
        {
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; }
            public int Cantidad { get; set; }
            public string Talla { get; set; }
            public string Sexo { get; set; }
            public string Lote { get; set; }
            public string Comentario { get; set; }
        }

        public class SolicitudListDto
        {
            public long IdSolicitud { get; set; }
            public DateTime FechaCreacion { get; set; }
            public string Gerencia { get; set; }
            public string Empleado { get; set; }
            public string Sexo { get; set; }
            public string Estado { get; set; }
            public string Motivo { get; set; }
        }

        public class SolicitudDetalleDto
        {
            public long IdDetalle { get; set; }
            public int IdArticulo { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Tipo { get; set; }
            public string Talla { get; set; }
            public string Sexo { get; set; }
            public int Cantidad { get; set; }
            public int Entregado { get; set; }
            public int Stock { get; set; }
        }

        public class MovimientoListDto
        {
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; }
            public string Origen { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Talla { get; set; }
            public string Sexo { get; set; }
            public int Cantidad { get; set; }
            public string Lote { get; set; }
            public string EmpleadoCarnet { get; set; }
        }

        public class SalidaDiaDto
        {
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Talla { get; set; }
            public string Sexo { get; set; }
            public int Cantidad { get; set; }
        }

        // =========================================================================
        // 1. LA VISTA PRINCIPAL
        // =========================================================================
        public ActionResult Index()
        {
            return View();
        }

        // =========================================================================
        // 2. CATÁLOGOS (Dropdowns, Autocompletes)
        // =========================================================================
        [HttpGet]
        public ActionResult Almacenes()
        {
            using (var cn = Conn())
            {
                var data = cn.Query<AlmacenDto>("SELECT IdAlmacen, Nombre FROM dbo.Almacenes WHERE Activo = 1 ORDER BY Nombre").ToList();
                return Json(new ResponseJson<List<AlmacenDto>> { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Articulos()
        {
            using (var cn = Conn())
            {
                var data = cn.Query<ArticuloDto>("SELECT IdArticulo, Codigo, Nombre, Tipo FROM dbo.Articulos ORDER BY Nombre").ToList();
                return Json(new ResponseJson<List<ArticuloDto>> { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Empleados()
        {
            using (var cn = Conn())
            {
                // Apuntando directo al esquema original y filtrando activos (fechabaja IS NULL)
                string sql = "SELECT carnet as Id, nombre_completo as Txt FROM sigho1.dbo.emp2024 WHERE fechabaja IS NULL ORDER BY nombre_completo";
                var data = cn.Query<ComboDto>(sql).ToList();
                return Json(new ResponseJson<List<ComboDto>> { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ArticuloVariantes(int idArticulo, int idAlmacen)
        {
            using (var cn = Conn())
            {
                var data = cn.Query<VarianteDto>("SELECT Talla, Sexo FROM dbo.ArticulosStockVar WHERE IdArticulo = @art AND IdAlmacen = @alm",
                                                 new { art = idArticulo, alm = idAlmacen }).ToList();
                return Json(new ResponseJson<List<VarianteDto>> { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult ArticuloCrear(string cod, string nom, string tipo, string uni)
        {
            using (var cn = Conn())
            {
                cn.Execute("INSERT INTO dbo.Articulos (Codigo, Nombre, Tipo, Unidad) VALUES (@c, @n, @t, @u)",
                           new { c = cod, n = nom, t = tipo, u = uni });
                return Json(new { ok = true });
            }
        }

        // =========================================================================
        // 3. INVENTARIO FÍSICO Y KARDEX
        // =========================================================================
        [HttpGet]
        public ActionResult Inventario(int idAlmacen)
        {
            using (var cn = Conn())
            {
                var sql = @"SELECT a.IdArticulo, a.Codigo, a.Nombre, a.Tipo, v.Talla, v.Sexo, 
                                   v.StockActual, v.StockMinimo, v.PrecioUnitario
                            FROM dbo.Articulos a 
                            JOIN dbo.ArticulosStockVar v ON a.IdArticulo = v.IdArticulo
                            WHERE v.IdAlmacen = @alm";

                var data = cn.Query<InventarioStockDto>(sql, new { alm = idAlmacen }).ToList();
                int bajoMin = data.Count(x => x.StockActual <= x.StockMinimo);

                return Json(new ResponseJson<List<InventarioStockDto>> { ok = true, data = data, bajoMinimo = bajoMin }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult InventarioMovimiento(int idAlmacen, string tipo, int idArticulo, string talla, string sexo, int cantidad, string comentario, string loteCodigo, DateTime? venc)
        {
            try
            {
                using (var cn = Conn())
                {
                    cn.Execute("dbo.Inv_Mov_EntradaMerma", new
                    {
                        IdAlmacen = idAlmacen,
                        Tipo = tipo,
                        IdArticulo = idArticulo,
                        Talla = talla,
                        Sexo = sexo,
                        Cantidad = cantidad,
                        Comentario = comentario,
                        LoteCodigo = loteCodigo,
                        Vence = venc,
                        Usuario = User.Identity.Name
                    }, commandType: CommandType.StoredProcedure);
                    return Json(new { ok = true });
                }
            }
            catch (Exception ex) { return Json(new { ok = false, msg = ex.Message }); }
        }

        [HttpGet]
        public ActionResult MovHist(int idAlmacen, int idArticulo, string talla, string sexo, int top)
        {
            using (var cn = Conn())
            {
                var sql = @"SELECT TOP (@t) m.Fecha, m.Tipo, m.Cantidad, m.Talla, m.Sexo, m.Comentario, l.LoteCodigo as Lote
                            FROM dbo.MovimientosInventario m
                            LEFT JOIN dbo.InvLotes l ON m.IdLote = l.IdLote
                            WHERE m.IdAlmacen=@alm AND m.IdArticulo=@art AND m.Talla=@tal AND m.Sexo=@sex 
                            ORDER BY m.Fecha DESC";

                var data = cn.Query<MovHistDto>(sql, new { t = top, alm = idAlmacen, art = idArticulo, tal = talla, sex = sexo }).ToList();
                return Json(new ResponseJson<List<MovHistDto>> { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Transferir(int idAlmacenOrigen, int idAlmacenDestino, int idArticulo, string talla, string sexo, int cantidad, string comentario, string usuario)
        {
            try
            {
                using (var cn = Conn())
                {
                    // Restamos del Origen (Merma)
                    cn.Execute("dbo.Inv_Mov_EntradaMerma", new
                    {
                        IdAlmacen = idAlmacenOrigen,
                        Tipo = "MERMA",
                        IdArticulo = idArticulo,
                        Talla = talla,
                        Sexo = sexo,
                        Cantidad = cantidad,
                        Comentario = "Traslado hacia almacén " + idAlmacenDestino + ". " + comentario,
                        Usuario = usuario
                    }, commandType: CommandType.StoredProcedure);

                    // Sumamos al Destino (Entrada)
                    cn.Execute("dbo.Inv_Mov_EntradaMerma", new
                    {
                        IdAlmacen = idAlmacenDestino,
                        Tipo = "ENTRADA",
                        IdArticulo = idArticulo,
                        Talla = talla,
                        Sexo = sexo,
                        Cantidad = cantidad,
                        Comentario = "Traslado desde almacén " + idAlmacenOrigen + ". " + comentario,
                        Usuario = usuario
                    }, commandType: CommandType.StoredProcedure);

                    return Json(new { ok = true });
                }
            }
            catch (Exception) { return Json(new { ok = false, msg = "Error de stock en origen." }); }
        }

        // =========================================================================
        // 4. SOLICITUDES Y APROBACIONES
        // =========================================================================
        [HttpGet]
        public ActionResult Solicitudes(string estado = "*")
        {
            using (var cn = Conn())
            {
                // Usamos LEFT JOIN para que si el carnet no está en emp2024, al menos veamos el ID
                // Usamos ISNULL para evitar que los nombres lleguen nulos al Datatable
                var sql = @"SELECT s.IdSolicitud, 
                           s.FechaCreacion, 
                           ISNULL(e.OGERENCIA, 'N/A') as Gerencia, 
                           ISNULL(e.nombre_completo, s.EmpleadoCarnet) as Empleado, 
                           ISNULL(e.Gender, 'N') as Sexo, 
                           s.Estado, 
                           ISNULL(s.MotivoUsuario, '') as Motivo
                    FROM dbo.Solicitudes s 
                    LEFT JOIN sigho1.dbo.emp2024 e ON s.EmpleadoCarnet = e.carnet
                    WHERE (@est = '*' OR s.Estado = @est)
                    ORDER BY s.FechaCreacion DESC";

                var data = cn.Query(sql, new { est = estado }).ToList();
                return Json(new { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult BodegaPendientes()
        {
            using (var cn = Conn())
            {
                var sql = @"SELECT s.IdSolicitud, 
                           s.FechaCreacion, 
                           ISNULL(e.OGERENCIA, 'N/A') as Gerencia, 
                           ISNULL(e.nombre_completo, s.EmpleadoCarnet) as Empleado, 
                           ISNULL(e.Gender, 'N') as Sexo
                    FROM dbo.Solicitudes s 
                    LEFT JOIN sigho1.dbo.emp2024 e ON s.EmpleadoCarnet = e.carnet
                    WHERE s.Estado IN ('Aprobada', 'Parcial') 
                    ORDER BY s.FechaCreacion ASC";

                var data = cn.Query(sql).ToList();
                return Json(new { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult SolicitudesCrear(string carnetEmpleado, string motivo, string detallesJson)
        {
            try
            {
                using (var cn = Conn())
                {
                    cn.Execute("dbo.Sol_CrearSolicitud", new { EmpleadoCarnet = carnetEmpleado, Motivo = motivo, DetallesJson = detallesJson }, commandType: CommandType.StoredProcedure);
                    return Json(new { ok = true });
                }
            }
            catch (Exception ex) { return Json(new { ok = false, msg = ex.Message }); }
        }

        [HttpPost]
        public ActionResult SolicitudesAprobar(long id)
        {
            using (var cn = Conn())
            {
                cn.Execute("UPDATE dbo.Solicitudes SET Estado = 'Aprobada' WHERE IdSolicitud = @id", new { id });
                cn.Execute("UPDATE dbo.SolicitudesDetalle SET CantidadAprobada = CantidadSolicitada WHERE IdSolicitud = @id", new { id });
                return Json(new { ok = true });
            }
        }

        [HttpPost]
        public ActionResult SolicitudesRechazar(long id, string observacion)
        {
            using (var cn = Conn())
            {
                cn.Execute("UPDATE dbo.Solicitudes SET Estado = 'Rechazada', RespuestaRRHH = @obs WHERE IdSolicitud = @id", new { id, obs = observacion });
                return Json(new { ok = true });
            }
        }

        [HttpGet]
        public ActionResult SolicitudDetalle(long id, int idAlmacen)
        {
            using (var cn = Conn())
            {
                var sql = @"SELECT d.IdDetalle, d.IdArticulo, a.Codigo, a.Nombre, a.Tipo, d.Talla, d.Sexo, 
                                   d.CantidadAprobada as Cantidad, d.CantidadEntregada as Entregado, ISNULL(v.StockActual, 0) as Stock
                            FROM dbo.SolicitudesDetalle d 
                            JOIN dbo.Articulos a ON d.IdArticulo = a.IdArticulo
                            LEFT JOIN dbo.ArticulosStockVar v ON v.IdArticulo = d.IdArticulo AND v.Talla = d.Talla AND v.Sexo = d.Sexo AND v.IdAlmacen = @alm
                            WHERE d.IdSolicitud = @id";

                var data = cn.Query<SolicitudDetalleDto>(sql, new { id = id, alm = idAlmacen }).ToList();
                return Json(new ResponseJson<List<SolicitudDetalleDto>> { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        // =========================================================================
        // 5. BODEGA Y DESPACHOS
        // =========================================================================
    

        [HttpPost, ValidateInput(false)]
        public ActionResult BodegaDespachar(int idAlmacen, long id, string despachoJson, string comentario)
        {
            try
            {
                using (var cn = Conn())
                {
                    cn.Execute("dbo.Bod_Despachar", new { IdAlmacen = idAlmacen, IdSolicitud = id, CarnetBodeguero = User.Identity.Name, DespachoJson = despachoJson }, commandType: CommandType.StoredProcedure);
                    if (!string.IsNullOrEmpty(comentario))
                        cn.Execute("UPDATE dbo.Solicitudes SET RespuestaRRHH = @c WHERE IdSolicitud = @id", new { c = comentario, id });

                    return Json(new { ok = true });
                }
            }
            catch (Exception ex) { return Json(new { ok = false, msg = ex.Message }); }
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult BodegaSalidaDirecta(int idAlmacen, string carnetEmpleado, string comentario, string salidaJson)
        {
            try
            {
                using (var cn = Conn())
                {
                    var idSol = cn.ExecuteScalar<long>("dbo.Sol_CrearSolicitud", new { EmpleadoCarnet = carnetEmpleado, Motivo = "SALIDA DIRECTA: " + comentario, DetallesJson = salidaJson }, commandType: CommandType.StoredProcedure);
                    cn.Execute("UPDATE dbo.Solicitudes SET Estado = 'Aprobada' WHERE IdSolicitud = @id", new { id = idSol });
                    cn.Execute("UPDATE dbo.SolicitudesDetalle SET CantidadAprobada = CantidadSolicitada WHERE IdSolicitud = @id", new { id = idSol });

                    string despacho = salidaJson.Replace("Cantidad", "Entregar");
                    cn.Execute("dbo.Bod_Despachar", new { IdAlmacen = idAlmacen, IdSolicitud = idSol, CarnetBodeguero = User.Identity.Name, DespachoJson = despacho }, commandType: CommandType.StoredProcedure);

                    return Json(new { ok = true });
                }
            }
            catch (Exception ex) { return Json(new { ok = false, msg = ex.Message }); }
        }

        // =========================================================================
        // 6. REPORTES Y REGISTROS
        // =========================================================================
        [HttpGet]
        public ActionResult MovimientosListado(DateTime desde, DateTime hasta, string tipo, string origen, string empleado, int idAlmacen)
        {
            using (var cn = Conn())
            {
                var sql = @"SELECT m.Fecha, m.Tipo, 'Manual' as Origen, a.Codigo, a.Nombre, m.Talla, m.Sexo, 
                                   m.Cantidad, l.LoteCodigo as Lote, m.CarnetDestino as EmpleadoCarnet
                            FROM dbo.MovimientosInventario m 
                            JOIN dbo.Articulos a ON m.IdArticulo = a.IdArticulo
                            LEFT JOIN dbo.InvLotes l ON m.IdLote = l.IdLote
                            WHERE m.IdAlmacen = @alm AND CAST(m.Fecha AS DATE) BETWEEN @f1 AND @f2";

                if (!string.IsNullOrEmpty(tipo)) sql += " AND m.Tipo = @tipo";
                if (!string.IsNullOrEmpty(empleado)) sql += " AND m.CarnetDestino = @emp";

                var data = cn.Query<MovimientoListDto>(sql, new { alm = idAlmacen, f1 = desde.Date, f2 = hasta.Date, tipo = tipo, emp = empleado }).ToList();
                return Json(new ResponseJson<List<MovimientoListDto>> { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult SalidasDelDia(DateTime fecha, int idAlmacen)
        {
            using (var cn = Conn())
            {
                var sql = @"SELECT a.Codigo, a.Nombre, m.Talla, m.Sexo, SUM(ABS(m.Cantidad)) as Cantidad
                            FROM dbo.MovimientosInventario m 
                            JOIN dbo.Articulos a ON m.IdArticulo = a.IdArticulo
                            WHERE m.IdAlmacen = @alm AND m.Tipo = 'SALIDA' AND CAST(m.Fecha AS DATE) = @f
                            GROUP BY a.Codigo, a.Nombre, m.Talla, m.Sexo";

                var data = cn.Query<SalidaDiaDto>(sql, new { alm = idAlmacen, f = fecha.Date }).ToList();
                return Json(new ResponseJson<List<SalidaDiaDto>> { ok = true, data = data }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}