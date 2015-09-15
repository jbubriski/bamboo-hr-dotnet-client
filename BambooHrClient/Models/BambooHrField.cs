using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooHrClient.Models
{
    public class BambooHrField
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Alias { get; set; }

        public override string ToString()
        {
            var propertyInfos = GetType().GetProperties();

            var sb = new StringBuilder();

            foreach (var info in propertyInfos)
            {
                var value = info.GetValue(this, null) ?? "(null)";
                sb.AppendLine(info.Name + ": " + value);
            }

            return sb.ToString();
        }
    }
}
