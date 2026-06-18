using System;
using System.Linq;
using System.Web.Mvc;
using DevExpress.Web.Mvc;
using slnRhonline.Models;


namespace slnRhonline.Controllers
{
    [SessionExpire]
    public class ConceptsController : Controller
    {
        /// <summary>
        /// Accion que llama al metodo GetAllCategories del modelo Concepts.
        /// </summary>
        /// <param name="clasificationId"></param>
        /// <param name="textField"></param>
        /// <param name="valueField"></param>
        /// <returns></returns>
        public ActionResult GetAllCategories(int clasificationId, string textField, string valueField)
        {
            return GridViewExtension.GetComboBoxCallbackResult(p =>
            {
                p.TextField = textField;
                p.ValueField = valueField;
                p.BindList(Concepts.GetCategories(clasificationId));
            });
        }

        /// <summary>
        /// Accion que llama al metodo GetSubCategories del modelo Concepts.
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="textField"></param>
        /// <param name="valueField"></param>
        /// <returns></returns>
        public ActionResult GetAllSubCategories(int categoryId, string textField, string valueField)
        {
            return GridViewExtension.GetComboBoxCallbackResult(p =>
            {
                p.TextField = textField;
                p.ValueField = valueField;
                p.Columns.Add("SubCategoryName");
                p.Columns.Add("Amount");
                p.BindList(Concepts.GetSubCategories(categoryId));
            });
        }

        /// <summary>
        /// Accion que llama al método GetProductPrice del modelo Concepts.
        /// </summary>
        /// <param name="clasificationId"></param>
        /// <param name="categoryId"></param>
        /// <param name="subCategoryId"></param>
        /// <returns></returns>
        public ActionResult GetProductPrice(int? clasificationId, int? categoryId, int? subCategoryId)
        {
            string productPrice = Concepts.GetProductPrice(clasificationId, categoryId, subCategoryId);
            return Json(productPrice, JsonRequestBehavior.AllowGet);
        }
    }
}