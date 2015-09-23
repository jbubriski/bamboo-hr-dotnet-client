using System;
using System.Collections.Generic;

namespace BambooHrClient.Models
{
    public class BambooHrEmployeeChangedInfos
    {
        public Dictionary<string, BambooHrEmployeeChangedInfo> Employees { get; set; }
    }

    public class BambooHrEmployeeChangedInfo
    {
        public int Id { get; set; }

        /// <summary>
        /// Will be inserted, updated or deleted.
        /// </summary>
        public string Action { get; set; }

        public DateTime LastChanged { get; set; }
    }
}
