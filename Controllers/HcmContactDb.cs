using System;

namespace slnRhonline.Controllers
{
    internal class HcmContactDb
    {
        public string PersonNumber { get; set; }
        public string FirstName { get; set; }
        public string MiddleNames { get; set; }
        public string LastName { get; set; }
        public string SecondLastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ContactType { get; set; }
        public string RelatedPersonNumber { get; set; }
        public string UploadedByCarnet { get; set; }   // carnet quien sube

    }
}