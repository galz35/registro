using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace slnRhonline.Models
{
    public class Nominaexcel
    {
		[NonSerialized]
		private ExtensionDataObject extensionDataField;

		[OptionalField]
		private string AREAField;

		[OptionalField]
		private string EMPLOYEE_NUMBERField;

		[OptionalField]
		private string FULL_NAMEField;

		[OptionalField]
		private string GERENCIAField;

		[OptionalField]
		private decimal HOURSField;

		[OptionalField]
		private string PERIOD_IDField;

		[OptionalField]
		private string PERSON_IDField;

		[OptionalField]
		private string STATUSField;
	}
}