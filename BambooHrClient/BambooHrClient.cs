using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using RestSharp;
using RestSharp.Authenticators;

namespace BambooHrClient
{
    public interface IBambooHrClient
    {
        Task<List<BambooHrTimeOffRequest>> GetTimeOffRequests(int employeeId);
        Task<BambooHrTimeOffRequest> GetTimeOffRequest(int timeOffRequestId);
        Task<int> CreateTimeOffRequest(int employeeId, int timeOffTypeId, DateTime startDate, DateTime endDate, bool startHalfDay = false, bool endHalfDay = false, string comment = null, List<DateTime> holidays = null, int? previousTimeOffRequestId = null);
        Task<List<BambooHrEmployee>> GetEmployees();
        Task<BambooHrEmployee> GetEmployee(int employeeId);
        Task<BambooHrEmployee> GetEmployee(string email);
        Task<bool> CancelTimeOffRequest(int timeOffRequestId, string reason = null);
        Task<List<BambooHrHoliday>> GetHolidays(DateTime startDate, DateTime endDate);
    }

    public class BambooHrClient : IBambooHrClient
    {
        private readonly string _createRequestFormat = @"<request>
    <timeOffTypeId>{0}</timeOffTypeId>
    <start>{1}</start>
    <end>{2}</end>
    <dates>
        {3}
    </dates>
    <status>approved</status>
    <notes>
        <note from=""employee"">{4}</note>
    </notes>
</request>";

        private readonly string _cancelTimeOffRequestXml = @"<request>
    <status>cancelled</status>
    <note>Request cancelled from OOO.</note>
</request>";

        private readonly string _replaceRequestFormat = @"<request>
    <timeOffTypeId>{0}</timeOffTypeId>
    <start>{1}</start>
    <end>{2}</end>
    <dates>
        {3}
    </dates>
    <status>approved</status>
    <notes>
        <note from=""employee"">{4}</note>
    </notes>
    <previousRequest>{5}</previousRequest>
</request>";

        private readonly string _historyEntryRequestFormat = @"<history>
    <date>{0}</date>
    <eventType>used</eventType>
    <timeOffRequestId>{1}</timeOffRequestId>  
    <note>{2}</note>
</history>";

        public async Task<List<BambooHrEmployee>> GetEmployees()
        {
            const string url = "/reports/custom?format=json";
            var xml = GenerateUserReportRequestXml();

            var restClient = GetNewRestClient();
            var request = GetNewRestRequest(url, Method.POST, true);

            request.AddParameter("text/xml", xml, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = restClient.Execute(request);
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing Bamboo request to " + url, ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception("Error executing Bamboo request to " + url, response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<DirectoryResponse>();

                if (package != null)
                {
                    var employees = package.Employees.Where(e => e.Status == "Active").ToList();

                    return employees;
                }

                throw new Exception("Bamboo Response does not contain Employees collection");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
            //.AddLoggedData("Response", response.Content);
        }

        public async Task<BambooHrEmployee> GetEmployee(int employeeId)
        {
            var url = "/employees/" + employeeId;

            var restClient = GetNewRestClient();
            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse response;

            try
            {
                response = await restClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), response.ErrorException);
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0} and employee id {1}", response.StatusCode, employeeId));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<BambooHrEmployee>();

                if (package != null)
                {
                    return package;
                }

                throw new Exception("Bamboo Response does not contain Employee");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrEmployee> GetEmployee(string workEmail)
        {
            var employees = await GetEmployees();

            var employee = employees.Single(e => e.WorkEmail == workEmail);

            return employee;
        }

        /// <summary>
        /// Creates an approved Time Off Request in BambooHR.  Optionally, you can specify half days which reduces the respective day to 4 hours, comments, a list of holidays to skip, and a previous Time Off Request ID to supersede.
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="timeOffTypeId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="startHalfDay"></param>
        /// <param name="endHalfDay"></param>
        /// <param name="comment"></param>
        /// <param name="holidays">Holidays that apply to the supplied date range.</param>
        /// <param name="previousTimeOffRequestId"></param>
        /// <returns></returns>
        public async Task<int> CreateTimeOffRequest(int employeeId, int timeOffTypeId, DateTime startDate, DateTime endDate, bool startHalfDay = false, bool endHalfDay = false, string comment = null, List<DateTime> holidays = null, int? previousTimeOffRequestId = null)
        {
            var url = string.Format("/employees/{0}/time_off/request", employeeId);

            var restClient = GetNewRestClient();
            var request = GetNewRestRequest(url, Method.PUT, false);

            var datesXml = GetDatesXml(startDate, endDate, startHalfDay, endHalfDay, holidays);

            string requestBody;

            if (previousTimeOffRequestId.HasValue)
            {
                requestBody = string.Format(_replaceRequestFormat, timeOffTypeId, startDate.ToString(Constants.BambooHrDateFormat), endDate.ToString(Constants.BambooHrDateFormat), datesXml, XmlEscape(comment), previousTimeOffRequestId.Value);
            }
            else
            {
                requestBody = string.Format(_createRequestFormat, timeOffTypeId, startDate.ToString(Constants.BambooHrDateFormat), endDate.ToString(Constants.BambooHrDateFormat), datesXml, XmlEscape(comment));
            }

            request.AddParameter("text/xml", requestBody, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = await restClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var location = response.Headers.Single(h => h.Name == "Location").Value.ToString();
                var id = Regex.Match(location, @"\d+$").Value;
                var timeOffRequestId = int.Parse(id);

                // If the first requested day is in the past, then we need to add a history entry for it.
                if (startDate < DateTime.Today)
                {
                    await AddTimeOffRequestHistoryEntry(employeeId, timeOffRequestId, startDate);
                }

                return timeOffRequestId;
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        private async Task<bool> AddTimeOffRequestHistoryEntry(int employeeId, int timeOffRequestId, DateTime date)
        {
            var url = string.Format("/employees/{0}/time_off/history/", employeeId);
            var note = "Automatically created by OOO tool because request is in the past.";
            var historyEntryRequestFormat = string.Format(_historyEntryRequestFormat, date.ToString(Constants.BambooHrDateFormat), timeOffRequestId, note);

            var restClient = GetNewRestClient();
            var request = GetNewRestRequest(url, Method.PUT, false);

            request.AddParameter("text/xml", historyEntryRequestFormat, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = await restClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1} and time off request ID {2}", url, employeeId, timeOffRequestId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1} and time off request ID {2}", url, employeeId, timeOffRequestId), response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.Created)
            {
                return true;
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;

            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<List<BambooHrTimeOffRequest>> GetTimeOffRequests(int employeeId)
        {
            const string url = "/time_off/requests/";

            var restClient = GetNewRestClient();
            var request = GetNewRestRequest(url, Method.GET, true);

            request.AddParameter("employeeId", employeeId, ParameterType.QueryString);

            IRestResponse response;

            try
            {
                response = await restClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), response.ErrorException);
            }

            if (response.Content.IsNullOrWhiteSpace())
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR to {0} for employee ID {1} Code {2}", url, employeeId, response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<List<BambooHrTimeOffRequest>>();

                if (package != null)
                {
                    return package;
                }

                throw new Exception("Bamboo Response does not contain Employees collection");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrTimeOffRequest> GetTimeOffRequest(int timeOffRequestId)
        {
            const string url = "/time_off/requests/";

            var restClient = GetNewRestClient();
            var request = GetNewRestRequest(url, Method.GET, true);

            request.AddParameter("id", timeOffRequestId, ParameterType.QueryString);

            IRestResponse response;

            try
            {
                response = await restClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for time off request ID {1}", url, timeOffRequestId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for time off request ID {1}", url, timeOffRequestId), response.ErrorException);
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0} and time off request id {1}", response.StatusCode, timeOffRequestId));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<List<BambooHrTimeOffRequest>>();

                if (package != null && package.Any())
                {
                    return package.SingleOrDefault();
                }

                throw new Exception("Bamboo Response does not contain Employees collection");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<bool> CancelTimeOffRequest(int timeOffRequestId, string reason = null)
        {
            var url = string.Format("time_off/requests/{0}/status/", timeOffRequestId);

            var restClient = GetNewRestClient();
            var request = GetNewRestRequest(url, Method.PUT, true);

            request.AddParameter("text/xml", _cancelTimeOffRequestXml, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = await restClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for time off request ID {1}", url, timeOffRequestId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for time off request ID {1}", url, timeOffRequestId), response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<List<BambooHrHoliday>> GetHolidays(DateTime startDate, DateTime endDate)
        {
            var url = "/time_off/holidays/";

            var restClient = GetNewRestClient();
            var request = GetNewRestRequest(url, Method.GET, true);

            request.AddParameter("start", startDate.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);
            request.AddParameter("end", endDate.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);

            IRestResponse response;

            try
            {
                // TODO: Revisit in the future
                // !!!
                // Something about this breaks with BambooHR if you use ExecuteTaskAsync and await the response
                // !!!
                response = restClient.Execute(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} with dates {1} - {2} ", url, startDate, endDate), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} with dates {1} - {2}", url, startDate, endDate), response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<List<BambooHrHoliday>>();

                if (package != null)
                {
                    return package;
                }

                throw new Exception("Bamboo Response does not contain Holidays collection");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public string GetDatesXml(DateTime startDate, DateTime endDate, bool startHalfDay, bool endHalfDay, List<DateTime> holidays)
        {
            var dates = new StringBuilder();
            var dateFormat = @"<date ymd=""{0}"" amount=""{1}"" />";
            var dateHours = GetDateHours(startDate, endDate, startHalfDay, endHalfDay, holidays);

            foreach (var kvp in dateHours)
            {
                dates.AppendFormat(dateFormat, kvp.Key.ToString(Constants.BambooHrDateFormat), kvp.Value);
            }

            return dates.ToString();
        }

        private Dictionary<DateTime, int> GetDateHours(DateTime startDate, DateTime endDate, bool startHalfDay, bool endHalfDay, List<DateTime> holidays)
        {
            var dateHours = new Dictionary<DateTime, int>();

            var dateIterator = startDate.Date;

            while (dateIterator <= endDate.Date)
            {
                if (holidays != null && holidays.Any(h => h.Date == dateIterator.Date))
                {
                    dateHours.Add(dateIterator.Date, 0);
                }
                else if (dateIterator.DayOfWeek == DayOfWeek.Saturday || dateIterator.DayOfWeek == DayOfWeek.Sunday)
                {
                    dateHours.Add(dateIterator.Date, 0);
                }
                else if (dateIterator == startDate.Date && startHalfDay)
                {
                    dateHours.Add(dateIterator.Date, 4);
                }
                else if (dateIterator == endDate.Date && endHalfDay)
                {
                    dateHours.Add(dateIterator.Date, 4);
                }
                else
                {
                    dateHours.Add(dateIterator.Date, 8);
                }

                dateIterator = dateIterator.AddDays(1);
            }

            return dateHours;
        }

        private RestClient GetNewRestClient()
        {
            return new RestClient(Constants.BambooApiUrl)
            {
                Authenticator = new HttpBasicAuthenticator(Constants.BambooApiKey, "x")
            };
        }

        private RestRequest GetNewRestRequest(string url, Method method, bool sendingJson)
        {
            var request = new RestRequest(url, method);

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Encoding", "utf-8");

            if (sendingJson)
            {
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("format", "JSON", ParameterType.QueryString);
            }

            return request;
        }

        private string GenerateUserReportRequestXml()
        {
            const string xml = @"<report>
  <title></title>{0}
  <fields>
    <field id=""id"" />

    <field id=""lastChanged"" />

    <field id=""status"" />

    <field id=""firstName"" />
    <field id=""middleName"" />
    <field id=""lastName"" />
    <field id=""nickname"" />
    <field id=""displayName"" />
    <field id=""gender"" />
    <field id=""DateOfBirth"" />
    <field id=""Age"" />

    <field id=""address1"" />
    <field id=""address2"" />
    <field id=""city"" />
    <field id=""state"" />
    <field id=""country"" />
    <field id=""zipCode"" />

    <field id=""homeEmail"" />
    <field id=""homePhone"" />
    <field id=""mobilePhone"" />

    <field id=""workEmail"" />
    <field id=""workPhone"" />
    <field id=""workPhoneExtension"" />
    <field id=""workPhonePlusExtension"" />

    <field id=""jobTitle"" />
    <field id=""department"" />
    <field id=""division"" />
    <field id=""location"" />

    <field id=""terminationDate"" />

    <field id=""supervisorId"" />
    <field id=""supervisorEid"" />
  </fields> 
</report>";

            return xml;
        }

        private static string XmlEscape(string unescaped)
        {
            var doc = new XmlDocument();
            var node = doc.CreateElement("root");

            node.InnerText = unescaped;

            return node.InnerXml;
        }
    }
}
