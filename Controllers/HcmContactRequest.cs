using System.Collections.Generic;

namespace slnRhonline.Controllers
{
    // MODELOS (C# 7.3) --------------------------------------------
    public class ContactoExcelVm
    {
        public string Carnet { get; set; }
        public string PrimerNombre { get; set; }
        public string SegundoNombre { get; set; }      // opcional
        public string PrimerApellido { get; set; }
        public string SegundoApellido { get; set; }    // opcional
        public string FechaNacimiento { get; set; }    // "yyyy-MM-dd" o vacío
        public string Genero { get; set; }             // opcional
        public string TipoRelacion { get; set; }
    }

    // Payload HCM (si ya lo tienes, reutiliza el tuyo)
    public class HcmContactRequest
    {
        public List<Name> names { get; set; }
        public List<LegislativeInfo> legislativeInfo { get; set; }
        public string DateOfBirth { get; set; }
        public List<ContactRelationship> contactRelationships { get; set; }
    }
    public class Name
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleNames { get; set; }      // opcional
        public string LegislationCode { get; set; }
        public string NameInformation1 { get; set; } // segundo apellido (opcional)
    }
    public class LegislativeInfo { public string Gender { get; set; } public string LegislationCode { get; set; } } // opcional
    public class ContactRelationship
    {
        public string RelatedPersonNumber { get; set; }
        public string ContactType { get; set; }
        public string LegislationCode { get; set; }
        public bool EmergencyContactFlag { get; set; }
        public bool PrimaryContactFlag { get; set; }
    }

}