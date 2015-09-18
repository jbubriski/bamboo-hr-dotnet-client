using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BambooHrClient.Models
{
    [DeserializeAs(Name = "User")]
    public class BambooHrUser
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Email { get; set; }
        
        public DateTime LastLogin { get; set; }

        public string Status { get; set; }

        public string LastFirst
        {
            get
            {
                return LastName + ", " + FirstName;
            }
        }

        public string FirstLast
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

        public BambooHrUser()
        {

        }
    }
}
