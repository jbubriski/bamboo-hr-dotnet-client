using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BambooHrClient.Models
{
    [DeserializeAs(Name = "row")]
    public class BambooHrRowData
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
    }

    [DeserializeAs(Name="field")]
    public class BambooHrFieldData
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
}
