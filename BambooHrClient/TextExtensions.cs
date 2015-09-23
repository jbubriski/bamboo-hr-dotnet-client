using System.Text;
using Newtonsoft.Json;

namespace BambooHrClient
{
    public static class TextExtensions
    {
        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static T FromJson<T>(this string s)
        {
            return JsonConvert.DeserializeObject<T>(s);
        }

        /// <summary>
        /// Removes control characters and other non-UTF-8 characters
        /// </summary>
        /// <param name="inString">The string to process</param>
        /// <returns>A string with no control characters or entities above 0x00FD</returns>
        /// <remarks>
        /// From http://stackoverflow.com/a/20777/51
        /// </remarks>
        public static string RemoveTroublesomeCharacters(this string inString)
        {
            if (inString == null) return null;

            StringBuilder newString = new StringBuilder();
            char ch;

            for (int i = 0; i < inString.Length; i++)
            {

                ch = inString[i];
                // remove any characters outside the valid UTF-8 range as well as all control characters
                // except tabs and new lines
                if ((ch < 0x00FD && ch > 0x001F) || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    newString.Append(ch);
                }
            }
            newString.Replace(@"\u00a0", " ").Replace(@"\u200e", "").Replace(@"\u00e9", "é");
            return newString.ToString();
        }

        public static string LowerCaseFirstLetter(this string value)
        {
            return value[0].ToString().ToLower() + value.Substring(1, value.Length - 1);
        }
    }
}
