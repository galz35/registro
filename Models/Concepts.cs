using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace slnRhonline.Models
{
    public static class Concepts
    {
        public static List<Entities.ViewModels.ConceptsSettingView> lcategoria = new List<Entities.ViewModels.ConceptsSettingView>();
        public static List<Entities.ViewModels.ConceptsSettingView> lclasificacion = new List<Entities.ViewModels.ConceptsSettingView>();
        public static List<Entities.ViewModels.ConceptsSettingView> lsubcategoria = new List<Entities.ViewModels.ConceptsSettingView>();
        public static List<Entities.Concepts> GetAllConcepts()
        {
            List<Entities.Concepts> lstConcepts = new List<Entities.Concepts>();
            try
            {
                var oConcepts = Utils.ClaroWCF.GetAllConcepts();
                if (oConcepts != null)
                {
                    lstConcepts = oConcepts.ToList();
                }

            }
            catch (Exception)
            {

                throw;
            }

            return lstConcepts;
        }

        public static List<Entities.ViewModels.ConceptsSettingView> GetAllClasification()
        {
            List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
            try
            {
                if (lclasificacion != null && lclasificacion.Count() > 0)
                {
                    return lclasificacion;
                }
                else
                {
                    var result = Utils.ClaroWCF.GetAllConceptsSetting();
                    if (result != null)
                    {
                        lstConcepts = (from item in result
                                       select item).GroupBy(i => new { i.ClasificationId, i.ClasificationName })
                                         .Select(i => i.FirstOrDefault()).ToList();
                        lclasificacion = lstConcepts;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            return lstConcepts;
        }
        public static List<Entities.ViewModels.ConceptsSettingView> GetAllCategories()
        {
            List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
          
            try
            {

                if (lcategoria!=null&& lcategoria.Count()>0)
                {
                   return lcategoria;

                }
                else { 
                var result = Utils.ClaroWCF.GetAllConceptsSetting();
                if (result != null)
                {
                    lstConcepts = (from item in result
                                   select item).GroupBy(i => new { i.CategoryId, i.CategoryName })
                                     .Select(i => i.FirstOrDefault()).ToList();
                    lcategoria = lstConcepts;
                     }
            }
            }
            catch (Exception)
            {

                throw;
            }

            return lstConcepts;
        }
        public static List<Entities.ViewModels.ConceptsSettingView> GetAllSubCategories()
        {
            List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
            try
            {
                if (lsubcategoria != null && lsubcategoria.Count() > 0)
                {
                    return lsubcategoria;

                }
                else
                {
                    var result = Utils.ClaroWCF.GetAllConceptsSetting();
                    if (result != null)
                    {
                        lstConcepts = (from item in result
                                       select item).GroupBy(i => new { i.SubCategoryId, i.SubCategoryName, i.Amount })
                                         .Select(i => i.FirstOrDefault()).ToList();

                        lsubcategoria = lstConcepts;
                    }


                }
            }
            catch (Exception)
            {

                throw;
            }

            return lstConcepts;
        }
        public static List<Entities.ViewModels.ConceptsSettingView> GetAllSubCategories(int x)
        {
            List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
            try
            {
                if (lsubcategoria != null && lsubcategoria.Count() > 0)
                {
                    lstConcepts = (from item in lsubcategoria
                                   where item.CategoryId == x
                                   select item).GroupBy(i => new { i.SubCategoryId, i.SubCategoryName, i.Amount })
                                         .Select(i => i.FirstOrDefault()).ToList();
                   

                }
                else
                {
                    var result = Utils.ClaroWCF.GetAllConceptsSetting();
                    if (result != null)
                    {
                        lstConcepts = (from item in result
                                       where item.CategoryId == x
                                       select item).GroupBy(i => new { i.SubCategoryId, i.SubCategoryName, i.Amount })
                                         .Select(i => i.FirstOrDefault()).ToList();

                    }
                }


            }
            catch (Exception)
            {

                throw;
            }

            return lstConcepts;
        }

        //public static List<Entities.ViewModels.ConceptsSettingView> GetAllCategories(int clasificationId)
        //{
        //    List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
        //    try
        //    {
        //        var result = Utils.ClaroWCF.GetAllConceptsSetting().Where(c=>c.ClasificationId == clasificationId);
        //        if (result != null)
        //        {
        //            lstConcepts = (from item in result
        //                           select item).GroupBy(i => new { i.CategoryId, i.CategoryName })
        //                             .Select(i => i.FirstOrDefault()).ToList();

        //        }  
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }

        //    return lstConcepts;
        //}

        //public static List<Entities.ViewModels.ConceptsSettingView> GetSubCategoriesByCategoryId(int categoryId)
        //{
        //    List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
        //    try
        //    {
        //        var result = Utils.ClaroWCF.GetAllConceptsSetting().Where(c => c.CategoryId== categoryId);
        //        if (result != null)
        //        {
        //            lstConcepts = (from item in result
        //                           select item).GroupBy(i => new { i.SubCategoryId, i.SubCategoryName,i.Amount})
        //                             .Select(i => i.FirstOrDefault()).ToList();

        //        }


        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }

        //    return lstConcepts;
        //}

        public static List<Entities.ViewModels.ConceptsSettingView> GetAllConceptsSettings()
        {
            List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
            try
            {
                var result = Utils.ClaroWCF.GetAllConceptsSetting();
                if (result != null)
                {

                    lstConcepts = result.ToList();

                    //lstConcepts = (from item in result
                    //               select item).GroupBy(i => new { i.ConcepSettingId, i.ClasificationName,i.CategoryName,i.SubCategoryName, i.Amount })
                    //                 .Select(i => i.FirstOrDefault()).ToList();

                }
            }
            catch (Exception)
            {

                throw;
            }

            return lstConcepts;
        }

        public static List<Entities.ViewModels.ConceptsSettingView> GetCategories()
        {
            return GetCategories(0);
        }
        public static List<Entities.ViewModels.ConceptsSettingView> GetCategories(int clasificationId)
        {
            List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
            try
            {
                var result = Utils.ClaroWCF.GetAllConceptsSetting().Where(g => g.ClasificationId == clasificationId);
                if (result != null)
                {
                    lstConcepts = (from item in result
                                   select item).GroupBy(i => new { i.CategoryId, i.CategoryName })
                                     .Select(i => i.FirstOrDefault()).ToList();

                }
            }
            catch (Exception)
            {

                throw;
            }

            return lstConcepts;
        }

        public static List<Entities.ViewModels.ConceptsSettingView> GetSubCategories()
        {
            return GetSubCategories(0);
        }
        public static List<Entities.ViewModels.ConceptsSettingView> GetSubCategories(int categoryId)
        {
            List<Entities.ViewModels.ConceptsSettingView> lstConcepts = new List<Entities.ViewModels.ConceptsSettingView>();
            try
            {
                if (lsubcategoria!=null &&lsubcategoria.Count()>0)
                {
                    lstConcepts = (from item in lsubcategoria
                                   select item).GroupBy(i => new { i.SubCategoryId, i.SubCategoryName, i.Amount })
                                     .Select(i => i.FirstOrDefault()).ToList();
                }
                else { 
                var result = Utils.ClaroWCF.GetAllConceptsSetting().Where(g => g.CategoryId == categoryId); ;
                if (result != null)
                {
                    lstConcepts = (from item in result
                                   select item).GroupBy(i => new { i.SubCategoryId, i.SubCategoryName, i.Amount })
                                     .Select(i => i.FirstOrDefault()).ToList();

                }
                }


            }
            catch (Exception)
            {

                throw;
            }

            return lstConcepts;
        }


        public static String GetProductPrice(int? clasificationId, int? categoryId, int? subCategoryId)
        {
            List<Entities.ViewModels.ConceptsSettingView> products = GetAllConceptsSettings();
            Entities.ViewModels.ConceptsSettingView currentProduct = products.FirstOrDefault(p => p.ClasificationId == clasificationId && p.CategoryId == categoryId && p.SubCategoryId == subCategoryId);
            if (currentProduct != null)
                return currentProduct.Amount.ToString();
            else
                return "undefined";
        }

    }
}