using System;
using System.Collections.Generic;
using System.Text;

namespace BambooHrClient.Models
{
    public class DirectoryResponse
    {
        public List<BambooHrEmployee> Employees { get; set; }
    }

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
    }
}
