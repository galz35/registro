using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

using slnRhonline.Models.Compensacion;

namespace slnRhonline.Models
{
    public class NotificacionService
    {
        public enum TipoPlantilla { Comisiones = 1, Personal = 2 }

        public string GenerarHtmlNotificacion(string nombreJefe, string nombrePeriodo, string fechaLimite, List<DetallePlantillaViewModel> empleados, TipoPlantilla tipo)
        {
            string colorHeader = (tipo == TipoPlantilla.Comisiones) ? "#fcb913" : "#e31919";
            string tituloHeader = (tipo == TipoPlantilla.Comisiones) ? "Reporte de Comisiones" : "Actualización de Plantilla de Personal";
            string textoAccion = (tipo == TipoPlantilla.Comisiones) ? "confirmar quiénes comisionan" : "validar cargos y ubicaciones";
            
            StringBuilder sb = new StringBuilder();
            sb.Append($@"
<html>
<head>
<meta charset='utf-8'>
<style>
  body{{font-family:'Segoe UI',Arial,sans-serif;color:#333;background:#f8f9fa;margin:0;padding:20px}}
  .container{{max-width:650px;margin:0 auto;background:#fff;border-radius:8px;box-shadow:0 4px 12px rgba(0,0,0,0.1);overflow:hidden;border:1px solid #ddd}}
  .header{{background:{colorHeader};color:{(tipo == TipoPlantilla.Comisiones ? "#000" : "#fff")};padding:25px;text-align:center}}
  .header h1{{margin:0;font-size:20px;text-transform:uppercase;letter-spacing:1px}}
  .content{{padding:25px;line-height:1.6}}
  .greeting{{font-size:16px;font-weight:bold;margin-bottom:15px}}
  .instruction{{margin-bottom:20px;font-size:14px;color:#555}}
  .deadline{{background:#fff9c4;border-left:4px solid #fbc02d;padding:12px;margin:15px 0;font-weight:bold;color:#856404;font-size:14px}}
  table{{width:100%;border-collapse:collapse;margin:20px 0;font-size:13px}}
  th{{background:#f1f1f1;color:#555;padding:10px;text-align:left;border-bottom:2px solid #ddd}}
  td{{padding:10px;border-bottom:1px solid #eee}}
  tr:nth-child(even){{background:#fafafa}}
  .btn-wrap{{text-align:center;margin:30px 0}}
  .btn{{background:{colorHeader};color:{(tipo == TipoPlantilla.Comisiones ? "#000" : "#fff")};padding:14px 28px;text-decoration:none;border-radius:50px;font-weight:bold;display:inline-block;box-shadow:0 4px 8px rgba(0,0,0,0.15)}}
  .footer{{background:#f1f1f1;color:#888;text-align:center;font-size:11px;padding:15px}}
</style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>{tituloHeader}</h1>
    </div>
    <div class='content'>
      <div class='greeting'>Estimado(a) {nombreJefe},</div>
      <div class='instruction'>
        Se ha habilitado el periodo <strong>{nombrePeriodo}</strong> en el portal RH Online. 
        Solicitamos su apoyo para {textoAccion} de su equipo de trabajo que se detalla a continuación:
      </div>

      <div class='deadline'>
        FECHA LÍMITE DE ENTREGA: {fechaLimite}
      </div>

      <table>
        <thead>
          <tr>
            <th>Carnet</th>
            <th>Nombre Completo</th>
            <th>Cargo</th>
            " + (tipo == TipoPlantilla.Comisiones ? "<th>Comisiona</th>" : "<th>Ubicación</th>") + @"
          </tr>
        </thead>
        <tbody>");

            // Mostrar máximo 15 empleados en el correo para no saturar
            var listaResumen = empleados.Take(15).ToList();
            foreach (var emp in listaResumen)
            {
                string datoExtra = (tipo == TipoPlantilla.Comisiones) ? (emp.Comisiona ?? "-") : (emp.Ubicacion_SIGHO ?? "-");
                sb.Append($@"
          <tr>
            <td>{emp.CarnetEmpleado}</td>
            <td>{emp.NombreCompleto}</td>
            <td>{emp.Cargo_SIGHO}</td>
            <td><strong>{datoExtra}</strong></td>
          </tr>");
            }

            if (empleados.Count > 15)
            {
                sb.Append($@"<tr><td colspan='4' style='text-align:center;color:#888;padding:10px'>... y {empleados.Count - 15} colaboradores más</td></tr>");
            }

            sb.Append($@"
        </tbody>
      </table>

      <div class='btn-wrap'>
        <a href='http://rhonline.claro.com.ni/Compensacion/Plantilla' class='btn'>Ingresar al Portal RH Online</a>
      </div>

      <div style='font-size:12px;color:#888;margin-top:20px;border-top:1px solid #eee;padding-top:10px'>
        <strong>Nota:</strong> Si identifica alguna diferencia en los datos mostrados, podrá reportarla directamente en el portal mediante el botón de edición.
      </div>
    </div>
    <div class='footer'>
      Este es un mensaje automático generado por el sistema RH Online.<br>No responda a este correo.
    </div>
  </div>
</body>
</html>");

            return sb.ToString();
        }
    }
}
