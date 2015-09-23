using RestSharp.Deserializers;

namespace BambooHrClient.Models
{
    [DeserializeAs(Name = "field")]
    public class BambooHrField
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }

        public BambooHrField()
        {

        }
    }
}
