using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooHrClient.Models
{
    public class BambooHrAssignedTimeOffPolicy
    {
        public int TimeOffPolicyId { get; set; }
        public int TimeOffTypeId { get; set; }
        public DateTime AccrualStartDate { get; set; }
    }
}
