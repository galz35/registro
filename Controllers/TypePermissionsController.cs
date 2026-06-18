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
    public class TypePermissionsController : Controller
    {
        public ActionResult GetTypePermissionsById(int? typeId, string textField, string valueField)
        {
          
            return GridViewExtension.GetComboBoxCallbackResult(p => {
                p.TextField = textField;
                p.ValueField = valueField;
                p.BindList(TypePermissions.GetTypePermisionById(typeId.GetValueOrDefault()));
            });
        }
    }
}