using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BambooHrClient.Models
{
    public class BambooHrWhosOutInfo
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public bool IsTimeOff
        {
            get
            {
                return Type.ToLower() == "timeoff";
            }
        }

        public bool IsHoliday
        {
            get
            {
                return Type.ToLower() == "holiday";
            }
        }
    }
}
