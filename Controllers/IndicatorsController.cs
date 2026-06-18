using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DevExpress.Web.Mvc;
using slnRhonline.Models;

namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class IndicatorsController : Controller
    {
        // GET: Products
        public ActionResult GetIndicatorsByManagement(int? managementId, string textField, string valueField)
        {

            return GridViewExtension.GetComboBoxCallbackResult(p => {
                p.TextField = textField;
                p.ValueField = valueField;
                p.BindList(slnRhonline.Data.Indicator.GetIndicatorsByManagement(managementId.GetValueOrDefault()));
            });
        }

        public ActionResult GetIndicatorsByManagement2(int? managementId)
        {

            List<Entities.Indicators> lstIndicator = new List<Entities.Indicators>();
            lstIndicator = Data.Indicator.GetIndicatorsByManagement(managementId.GetValueOrDefault());
            return View("Edit");
        }

    }
}