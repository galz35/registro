using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class MenuNode
    {
        public int Id { get; set; }
        public string MenuText { get; set; }
        public int? ParentId { get; set; }
        public decimal OrderMenu { get; set; }
        public List<MenuNode> Children { get; set; } = new List<MenuNode>();
    }
}