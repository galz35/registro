using System.Collections;
using System.Linq;
using System.Web.UI;

namespace slnRhonline.Models
{
    public abstract class ItemsData : IHierarchicalEnumerable, IEnumerable
    {
        public ItemsData()
        {
        }

        public IEnumerator GetEnumerator()
        {
            return Data.GetEnumerator();
        }
        public IHierarchyData GetHierarchyData(object enumeratedItem)
        {
            return (IHierarchyData)enumeratedItem;
        }

        public abstract IEnumerable Data { get; }
    }

    public class ItemData : IHierarchyData
    {
        public ItemData(string text, string navigateUrl, string imageUrl, string headerImageUrl)
        {
            Text = text;
            NavigateUrl = navigateUrl;
            ImageUrl = imageUrl;
            HeaderImageUrl = headerImageUrl;
        }

        IHierarchicalEnumerable IHierarchyData.GetChildren()
        {
            return CreateChildren();
        }
        IHierarchyData IHierarchyData.GetParent()
        {
            return null;
        }

        // IHierarchyData
        bool IHierarchyData.HasChildren
        {
            get { return HasChildren(); }
        }

        object IHierarchyData.Item
        {
            get { return this; }
        }

        string IHierarchyData.Path
        {
            get { return NavigateUrl; }
        }


        string IHierarchyData.Type
        {
            get { return GetType().ToString(); }
        }

        protected virtual IHierarchicalEnumerable CreateChildren()
        {
            return null;
        }

        protected virtual bool HasChildren()
        {
            return false;
        }

        public string HeaderImageUrl { get; protected set; }



        public string ImageUrl { get; protected set; }

        public string NavigateUrl { get; protected set; }

        public string Text { get; protected set; }
    }

    public class MenuSystemData : ItemsData
    {
        public override IEnumerable Data
        {
            get { return Models.Menu.GetAllMenu().ToList().Select(c => new MenuData(c)); }
        }
    }

    public class MenuData : ItemData
    {
        //    public CategoryData(Entities.Menu menu)
        //: base(menu.MenuName, "?MenuId=" + menu.MenuId)
        //    {
        //        Menu = menu;
        //    }
        public MenuData(Entities.Menu menu) : base(menu.MenuName, "?MenuId=" + menu.MenuId, "?MenuId=" + menu.MenuId, menu.HeaderImageUrl)
        {
            Menu = menu;
        }

        protected override IHierarchicalEnumerable CreateChildren()
        {
            return new SubMenusData(Menu.MenuId);
        }

        protected override bool HasChildren()
        {
            return true;
        }


        public Entities.Menu Menu { get; protected set; }
    }

    public class SubMenusData : ItemsData
    {

  

        public SubMenusData(int menuID) : base()
        {
            MenuID = menuID;
        }

        public override IEnumerable Data
        {
            get
            {
                return Models.SubMenu
                    .GetAllSubMenu()
                    .Where(p => p.MenuId == MenuID)
                    .ToList()
                    .Select(p => new SubMenuData(p));
            }
        }

        public int MenuID { get; protected set; }

        //Parte nueva para teercer nivel

        


    }

    public class SubMenuData : ItemData
    {
        //public ProductData(Entities.SubMenu subMenu)
        //    : base(subMenu.SubMenuName, "?MenuId=" + subMenu.MenuId + "&SubMenuId=" + subMenu.SubMenuId)
        //{
        //}
        public SubMenuData(Entities.ViewModels.UserOptionsView subMenu) : base(subMenu.SubMenuName, "/" + subMenu.Url, subMenu.ImageUrl, string.Empty)
        {
        }
    }
}