using System.Collections.Generic;

namespace slnRhonline.Controllers
{
    public class ContactosHCM
    {
        public List<ContactItem> items { get; set; }

    }
    public class ContactItem
    {
        public string PersonNumber { get; set; } // contacto
        public List<Name> names { get; set; }
        public List<ContactRelationship2> contactRelationships { get; set; }
    }

    public class Name2
    {
        public string DisplayName { get; set; }
    }

    public class ContactRelationship2
    {
        public string RelatedPersonNumber { get; set; }  // empleado origen
        public string ContactType { get; set; }
        public string LegislationCode { get; set; }
        public bool EmergencyContactFlag { get; set; }
        public bool PrimaryContactFlag { get; set; }
        public bool? StatutoryDependent { get; set; }
    }
}