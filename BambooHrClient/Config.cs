using System.Configuration;

namespace BambooHrClient
{
    public class Config
    {
        public static string BambooApiUser { get { return ConfigurationManager.AppSettings["BambooApiUser"]; } }
        public static string BambooApiKey { get { return ConfigurationManager.AppSettings["BambooApiKey"]; } }
        public static string BambooApiUrl { get { return ConfigurationManager.AppSettings["BambooApiUrl"]; } }
        public static string BambooCompanyUrl { get { return ConfigurationManager.AppSettings["BambooCompanyUrl"]; } }
    }
}
