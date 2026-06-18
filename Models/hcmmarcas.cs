using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace slnRhonline.Models
{
    public class hcmmarcas
    {
 
       

        public class Item
        {

            public string startTime { get; set; }
            public string stopTime { get; set; }

            public string measure { get; set; }

            public string personNumber { get; set; }

            public string comment { get; set; }

            public string earnedDate { get; set; }
        }

        public class Link
        {
            public string rel { get; set; }
            public string href { get; set; }
            public string name { get; set; }
            public string kind { get; set; }
        }

        public class Root
        {
            public List<Item> items { get; set; }
            public int count { get; set; }
            public bool hasMore { get; set; }
            public int limit { get; set; }
            public int offset { get; set; }
            public List<Link> links { get; set; }
        }
    }
}