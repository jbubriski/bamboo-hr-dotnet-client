using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BambooHrClient.Models
{
    public class DirectoryResponse
    {
        public List<BambooHrEmployee> Employees { get; set; }
    }

    [XmlType(TypeName = "employee")]
    public class BambooHrEmployee
    {
        public int Id { get; set; }

        public DateTime LastChanged { get; set; }

        public string Status { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Nickname { get; set; }
        public string DisplayName { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public int Age { get; set; }

        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }

        public string HomeEmail { get; set; }
        public string HomePhone { get; set; }
        public string MobilePhone { get; set; }

        public string WorkEmail { get; set; }
        public string WorkPhone { get; set; }
        public string WorkPhoneExtension { get; set; }
        public string WorkPhonePlusExtension { get; set; }

        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Location { get; set; }
        public string Division { get; set; }

        public string TerminationDate { get; set; }

        public string SupervisorId { get; set; }
        public string SupervisorEid { get; set; }

        public string LastFirst
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Nickname))
                    return LastName + ", " + Nickname;

                return LastName + ", " + FirstName;
            }
        }

        public string FirstLast
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Nickname))
                    return Nickname + " " + LastName;

                return FirstName + " " + LastName;
            }
        }

        public string ToXml()
        {
            var xElement = new XElement("employee");

            xElement.AddFieldValueIfNotNull("status", Status);

            xElement.AddFieldValueIfNotNull("firstName", FirstName);
            xElement.AddFieldValueIfNotNull("middleName", MiddleName);
            xElement.AddFieldValueIfNotNull("lastName", LastName);
            xElement.AddFieldValueIfNotNull("nickname", Nickname);
            xElement.AddFieldValueIfNotNull("displayName", DisplayName);
            xElement.AddFieldValueIfNotNull("gender", Gender);
            xElement.AddFieldValueIfNotNull("dateOfBirth", DateOfBirth);
            xElement.AddFieldValueIfNotNull("age", Age.ToString());

            xElement.AddFieldValueIfNotNull("address1", Address1);
            xElement.AddFieldValueIfNotNull("address2", Address2);
            xElement.AddFieldValueIfNotNull("city", City);
            xElement.AddFieldValueIfNotNull("state", State);
            xElement.AddFieldValueIfNotNull("country", Country);
            xElement.AddFieldValueIfNotNull("zipCode", ZipCode);

            xElement.AddFieldValueIfNotNull("homeEmail", HomeEmail);
            xElement.AddFieldValueIfNotNull("homePhone", HomePhone);
            xElement.AddFieldValueIfNotNull("mobilePhone", MobilePhone);

            xElement.AddFieldValueIfNotNull("workEmail", WorkEmail);
            xElement.AddFieldValueIfNotNull("workPhone", WorkPhone);
            xElement.AddFieldValueIfNotNull("workPhoneExtension", WorkPhoneExtension);

            xElement.AddFieldValueIfNotNull("jobTitle", JobTitle);
            xElement.AddFieldValueIfNotNull("department", Department);
            xElement.AddFieldValueIfNotNull("location", Location);
            xElement.AddFieldValueIfNotNull("division", Division);

            xElement.AddFieldValueIfNotNull("terminationDate", TerminationDate);

            xElement.AddFieldValueIfNotNull("supervisorId", SupervisorId);
            xElement.AddFieldValueIfNotNull("supervisorEid", SupervisorEid);

            return xElement.ToString();
        }
    }
}
