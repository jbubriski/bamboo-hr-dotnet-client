using RestSharp;
using System.Linq;

namespace BambooHrClient
{
    public static class IRestResponseExtensions
    {
        private static readonly string _bambooHrErrorMessageHeaderName = "X-BambooHR-Error-Message";

        public static string GetBambooHrErrorMessage(this IRestResponse response)
        {
            var error = response?.Headers.FirstOrDefault(x => x.Name == _bambooHrErrorMessageHeaderName);

            return error?.Value.ToString();
        }
    }
}
