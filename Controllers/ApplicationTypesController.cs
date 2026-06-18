using System.Web.Mvc;
using DevExpress.Web.Mvc;
using slnRhonline.Models;

namespace slnRhonline.Controllers
{
    public class ApplicationTypesController : Controller
    {
        /// <summary>
        /// Action que llama al metodo GetApplicationTypesById del modelo ApplicationTypes.
        /// </summary>
        /// <param name="habilityTypeId"></param>
        /// <param name="textField"></param>
        /// <param name="valueField"></param>
        /// <returns></returns>
        public ActionResult GetApplicationTypeById(int? habilityTypeId, string textField, string valueField)
        {
            return GridViewExtension.GetComboBoxCallbackResult(p =>
            {
                p.TextField = textField;
                p.ValueField = valueField;
                p.BindList(ApplicationTypes.GetApplicationTypesById(habilityTypeId.GetValueOrDefault()));
            });
        }
    }
}