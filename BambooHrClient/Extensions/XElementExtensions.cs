using System.Xml.Linq;

namespace BambooHrClient
{
    public static class XElementExtensions
    {
        public static void AddFieldValueIfNotNull(this XElement xElement, string name, string value)
        {
            if (value == null)
                return;

            var fieldElement = new XElement("field");

            fieldElement.Add(new XAttribute("id", name));
            fieldElement.Value = value;

            xElement.Add(fieldElement);
        }
    }
}
