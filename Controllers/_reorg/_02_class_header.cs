namespace RHOnline.Controllers
{
    public class Helpdesk2025Controller : Controller
    {
        // ==========================================
        // CONFIG / CONSTANTES / CAMPOS
        // ==========================================
        string connectionString = WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string csHelpdesk = WebConfigurationManager.ConnectionStrings["csHelpdesk"].ConnectionString;
        string csSigho = WebConfigurationManager.ConnectionStrings["csSigho"].ConnectionString;
        string msgDbConn = WebConfigurationManager.ConnectionStrings["msgDbConn"].ConnectionString;


        const string MAIL_FROM = "notificaciones.vo@claro.com.ni";
        const string MAIL_SMTP_HOST = "172.19.74.205";
        const int MAIL_SMTP_PORT = 25;
        const string MAIL_SMTP_USER = "notificaciones.vo";
        const string MAIL_SMTP_PASS = "C1aro.123";

        // URL base para API de correo (ajusta según tu entorno)
        string correoApiBase = "http://172.26.54.66/apihcm/api/values/correo/correohelpdesk2025";
