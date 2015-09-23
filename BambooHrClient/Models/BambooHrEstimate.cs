using System;

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
