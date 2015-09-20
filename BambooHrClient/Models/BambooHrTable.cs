using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooHrClient.Models
{
    [DeserializeAs(Name = "table")]
    public class BambooHrTable
    {
        public string Alias { get; set; }

        /// <summary>
        /// This is the list of fields for this table.
        /// TODO: Find a better way to handle this.
        /// </summary>
        public List<BambooHrTabularField> Value { get; set; }

        /// <summary>
        /// Read only alias to the Value property.
        /// </summary>
        public List<BambooHrTabularField> Fields
        {
            get
            {
                return Value;
            }
        }

        /// <summary>
        /// Parameterless constructor for XML deserialization.
        /// </summary>
        public BambooHrTable()
        {

        }
    }
}
