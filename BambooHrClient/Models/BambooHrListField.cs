using RestSharp.Deserializers;
using System;
using System.Collections.Generic;

namespace BambooHrClient.Models
{
    [DeserializeAs(Name="list")]
    public class BambooHrListField
    {
        public int FieldId { get; set; }
        public string Alias { get; set; }
        public string Manageable { get; set; }
        public string Multiple { get; set; }
        public string Name { get; set; }

        public List<BambooHrListFieldOption> Options { get; set; }

        public BambooHrListField()
        {

        }
    }

    [DeserializeAs(Name = "option")]
    public class BambooHrListFieldOption
    {
        public int Id { get; set; }
        public string Archived { get; set; }
        public string Value { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? ArchivedDate { get; set; }

        public BambooHrListFieldOption()
        {

        }
    }
}
