using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooHrClient.Models
{
    [DeserializeAs(Name = "field")]
    public class BambooHrTabularField
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Alias { get; set; }

        [DeserializeAs(Name="Value")]
        public string Name { get; set; }

        /// <summary>
        /// Parameterless constructor for XML deserialization.
        /// </summary>
        public BambooHrTabularField()
        {

        }
    }
}
