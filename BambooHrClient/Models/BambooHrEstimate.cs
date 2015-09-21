using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooHrClient.Models
{
    public class BambooHrEstimate
    {
        public int TimeOffType { get; set; }
        public string Name { get; set; }
        public string Units { get; set; }
        public decimal Balance { get; set; }
        public DateTime End { get; set; }
    }
}
