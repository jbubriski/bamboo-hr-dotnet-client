using System;

namespace BambooHrClient.Models
{
    public class BambooHrTimeOffRequest
    {
        public int Id { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public string EmployeeName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public int TypeId { get; set; }

        public BambooAmount Amount { get; set; }

        public Actions Actions { get; set; }
    }

    public class BambooAmount
    {
        public string Amount { get; set; }
        public string Unit { get; set; }

        public int AmountInt { get { return int.Parse(Amount); } }
    }

    public class Actions
    {
        public bool View { get; set; }
        public bool Edit { get; set; }
        public bool Cancel { get; set; }
        public bool Approve { get; set; }
        public bool Deny { get; set; }
        public bool Bypass { get; set; }

    }
}
