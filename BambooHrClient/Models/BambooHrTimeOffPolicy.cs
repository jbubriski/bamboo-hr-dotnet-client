using System;

namespace BambooHrClient.Models
{
    public class BambooHrAssignedTimeOffPolicy
    {
        public int TimeOffPolicyId { get; set; }
        public int TimeOffTypeId { get; set; }
        public DateTime AccrualStartDate { get; set; }
    }
}
