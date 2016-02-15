using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using BambooHrClient.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Security.Cryptography;

namespace BambooHrClient
{
    public interface IBambooHrClient
    {
        Task<List<BambooHrEmployee>> GetEmployees();

        Task<List<Dictionary<string, string>>> GetTabularData(string employeeId, BambooHrTableType tableType);

        Task<List<BambooHrTimeOffRequest>> GetTimeOffRequests(int employeeId);
        Task<BambooHrTimeOffRequest> GetTimeOffRequest(int timeOffRequestId);
        Task<int> CreateTimeOffRequest(int employeeId, int timeOffTypeId, DateTime startDate, DateTime endDate, bool startHalfDay = false, bool endHalfDay = false, string comment = null, List<DateTime> holidays = null, int? previousTimeOffRequestId = null);
        Task<bool> CancelTimeOffRequest(int timeOffRequestId, string reason = null);
        Task<List<BambooHrAssignedTimeOffPolicy>> GetAssignedTimeOffPolicies(int employeeId);
        Task<List<BambooHrEstimate>> GetFutureTimeOffBalanceEstimates(int employeeId, DateTime? endDate = null);
        Task<List<BambooHrWhosOutInfo>> GetWhosOut(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<BambooHrHoliday>> GetHolidays(DateTime startDate, DateTime endDate);

        Task<string> AddEmployee(BambooHrEmployee bambooHrEmployee);
        Task<BambooHrEmployee> GetEmployee(int employeeId);
        Task<bool> UpdateEmployee(BambooHrEmployee bambooHrEmployee);

        Task<Byte[]> GetEmployeePhoto(int employeeId, string size = "small");
        string GetEmployeePhotoUrl(string employeeEmail);

        Task<BambooHrField[]> GetFields();
        Task<BambooHrTable[]> GetTabularFields();
        Task<List<BambooHrListField>> GetListFieldDetails();
        Task<BambooHrTimeOffTypeInfo> GetTimeOffTypes(string mode = "");
        Task<BambooHrTimeOffPolicy[]> GetTimeOffPolicies();
        Task<BambooHrUser[]> GetUsers();

        Task<BambooHrEmployeeChangedInfo[]> GetLastChangedInfo(DateTime lastChanged, string type = "");
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

        private IRestClient _iRestClient;

        public BambooHrClient()
        {
            _iRestClient = new RestClient(Config.BambooApiUrl)
            {
                Authenticator = new HttpBasicAuthenticator(Config.BambooApiKey, "x")
            };
        }

        public BambooHrClient(IRestClient iRestClient)
        {
            _iRestClient = iRestClient;
        }

        public async Task<List<Dictionary<string, string>>> GetTabularData(string employeeId, BambooHrTableType tableType)
        {
            var url = string.Format("/employees/{0}/tables/{1}/", employeeId, tableType.ToString().LowerCaseFirstLetter());

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse<List<Dictionary<string, string>>> response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync<List<Dictionary<string, string>>>(request);
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing Bamboo request to " + url, ex);
            }

            if (response.ErrorException != null)
                throw new Exception("Error executing Bamboo request to " + url, response.ErrorException);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Data != null)
                {
                    return response.Data;
                }

                throw new Exception("Bamboo Response does not contain data.");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<List<BambooHrEmployee>> GetEmployees()
        {
            const string url = "/reports/custom?format=json";
            var xml = GenerateUserReportRequestXml();

            var request = GetNewRestRequest(url, Method.POST, true);

            request.AddParameter("text/xml", xml, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing Bamboo request to " + url, ex);
            }

            if (response.ErrorException != null)
                throw new Exception("Error executing Bamboo request to " + url, response.ErrorException);

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
        }

        #region Employees

        public async Task<string> AddEmployee(BambooHrEmployee bambooHrEmployee)
        {
            if (string.IsNullOrWhiteSpace(bambooHrEmployee.FirstName))
            {
                throw new Exception("First name is required.");
            }

            if (string.IsNullOrWhiteSpace(bambooHrEmployee.LastName))
            {
                throw new Exception("Lastname is required.");
            }

            var url = "/employees/";

            var request = GetNewRestRequest(url, Method.POST, false);

            var xml = bambooHrEmployee.ToXml();

            request.AddParameter("text/xml", xml, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Bamboo request to {url} to add employee '{bambooHrEmployee.FirstName} {bambooHrEmployee.LastName}'", ex);
            }

            if (response.ErrorException != null)
                throw new Exception($"Error executing Bamboo request to {url} to add employee '{bambooHrEmployee.FirstName} {bambooHrEmployee.LastName}'", response.ErrorException);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new Exception($"There is already an employee with the email address {bambooHrEmployee.WorkEmail}.");
            }

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var location = response.Headers.Single(h => h.Name == "Location").Value.ToString();
                var id = Regex.Match(location, @"\d+$").Value;
                bambooHrEmployee.Id = int.Parse(id);

                if (!string.IsNullOrWhiteSpace(location))
                {
                    return location;
                }

                throw new Exception("Bamboo Response does not contain Employee");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrEmployee> GetEmployee(int employeeId)
        {
            var url = "/employees/" + employeeId;

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), ex);
            }

            if (response.ErrorException != null)
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), response.ErrorException);

            if (string.IsNullOrWhiteSpace(response.Content))
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0} and employee id {1}", response.StatusCode, employeeId));

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

        public async Task<bool> UpdateEmployee(BambooHrEmployee bambooHrEmployee)
        {
            if (bambooHrEmployee.Id <= 0)
            {
                throw new Exception("ID is required.");
            }

            var url = $"/employees/{bambooHrEmployee.Id}";

            var request = GetNewRestRequest(url, Method.POST, false);

            var xml = bambooHrEmployee.ToXml();

            request.AddParameter("text/xml", xml, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Bamboo request to {url} to update employee '{bambooHrEmployee.FirstName} {bambooHrEmployee.LastName}'.", ex);
            }

            if (response.ErrorException != null)
                throw new Exception($"Error executing Bamboo request to {url} to update employee '{bambooHrEmployee.FirstName} {bambooHrEmployee.LastName}'.", response.ErrorException);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception($"Bad XML trying to update employee with ID {bambooHrEmployee.Id}.");
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new Exception($"Either you don't have permissions to update the employee, or none of the requested fields can be updated for employee ID {bambooHrEmployee.Id}.");
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"Employee not found with the ID {bambooHrEmployee.Id}.");
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {errorMessage}");
        }

        #endregion

        #region Photos

        public async Task<Byte[]> GetEmployeePhoto(int employeeId, string size = "small")
        {
            var url = string.Format("/employees/{0}/photo/{1}", employeeId, size);

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
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
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0} and employee ID {1}", response.StatusCode, employeeId));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var fileData = response.RawBytes;

                if (fileData != null)
                {
                    return fileData;
                }

                throw new Exception("Bamboo Response does not contain file data");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public string GetEmployeePhotoUrl(string employeeEmail)
        {
            var hashedEmail = Hash(employeeEmail);
            var url = string.Format(Config.BambooCompanyUrl + "/employees/photos/?h={0}", hashedEmail);

            return url;
        }

        /// <summary>
        /// Mostly from Stack Overflow post: http://stackoverflow.com/a/24031467/57698
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string Hash(string input)
        {
            var asciiBytes = Encoding.ASCII.GetBytes(input.Trim().ToLower());
            var hashedBytes = MD5.Create().ComputeHash(asciiBytes);
            var hashedString = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

            return hashedString;
        }

        #endregion

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
                response = await _iRestClient.ExecuteTaskAsync(request);
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
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"Can't create Time Off Request in {nameof(CreateTimeOffRequest)}, Employee ID {employeeId} not found.");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {errorMessage} in {nameof(CreateTimeOffRequest)}");
        }

        private async Task<bool> AddTimeOffRequestHistoryEntry(int employeeId, int timeOffRequestId, DateTime date)
        {
            var url = string.Format("/employees/{0}/time_off/history/", employeeId);
            var note = "Automatically created by OOO tool because request is in the past.";
            var historyEntryRequestFormat = string.Format(_historyEntryRequestFormat, date.ToString(Constants.BambooHrDateFormat), timeOffRequestId, note);

            var request = GetNewRestRequest(url, Method.PUT, false);

            request.AddParameter("text/xml", historyEntryRequestFormat, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
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

            var request = GetNewRestRequest(url, Method.GET, true);

            request.AddParameter("employeeId", employeeId, ParameterType.QueryString);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
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

                throw new Exception("Bamboo Response does not contain data.");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrTimeOffRequest> GetTimeOffRequest(int timeOffRequestId)
        {
            const string url = "/time_off/requests/";

            var request = GetNewRestRequest(url, Method.GET, true);

            request.AddParameter("id", timeOffRequestId, ParameterType.QueryString);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
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

                throw new Exception("Bamboo Response does not contain data.");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<bool> CancelTimeOffRequest(int timeOffRequestId, string reason = null)
        {
            var url = string.Format("time_off/requests/{0}/status/", timeOffRequestId);

            var request = GetNewRestRequest(url, Method.PUT, true);

            request.AddParameter("text/xml", _cancelTimeOffRequestXml, ParameterType.RequestBody);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
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

        public async Task<List<BambooHrAssignedTimeOffPolicy>> GetAssignedTimeOffPolicies(int employeeId)
        {
            var url = string.Format("/employees/{0}/time_off/policies/", employeeId);

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<List<BambooHrAssignedTimeOffPolicy>>();

                if (package != null && package.Any())
                {
                    return package;
                }

                throw new Exception("Bamboo Response does not contain data.");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<List<BambooHrEstimate>> GetFutureTimeOffBalanceEstimates(int employeeId, DateTime? endDate = null)
        {
            var url = string.Format("/employees/{0}/time_off/calculator/", employeeId);

            var request = GetNewRestRequest(url, Method.GET, true);

            if (endDate.HasValue)
                request.AddParameter("end", endDate.Value.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<List<BambooHrEstimate>>();

                if (package != null && package.Any())
                {
                    return package;
                }

                throw new Exception("Bamboo Response does not contain data.");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<List<BambooHrWhosOutInfo>> GetWhosOut(DateTime? startDate = null, DateTime? endDate = null)
        {
            const string url = "/time_off/whos_out/";

            var request = GetNewRestRequest(url, Method.GET, true);

            if (startDate.HasValue)
                request.AddParameter("start", startDate.Value.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);

            if (endDate.HasValue)
                request.AddParameter("end", endDate.Value.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<List<BambooHrWhosOutInfo>>();

                if (package != null && package.Any())
                {
                    return package;
                }

                throw new Exception("Bamboo Response does not contain data.");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        // See inner todo regarding this pragma
#pragma warning disable 1998
        public async Task<List<BambooHrHoliday>> GetHolidays(DateTime startDate, DateTime endDate)
        {
            const string url = "/time_off/holidays/";

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
                response = _iRestClient.Execute(request);
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
#pragma warning restore 1998

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

        public async Task<BambooHrField[]> GetFields()
        {
            const string url = "/meta/fields/";

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<BambooHrField[]>();

                if (package != null && package.Any())
                {
                    return package;
                }

                throw new Exception("Bamboo Response does not contain file data");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrTable[]> GetTabularFields()
        {
            const string url = "/meta/tables/";

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse<List<BambooHrTable>> response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync<List<BambooHrTable>>(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);

            if (string.IsNullOrWhiteSpace(response.Content))
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Data != null)
                    return response.Data.ToArray();

                throw new Exception("Bamboo Response does not contain data");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<List<BambooHrListField>> GetListFieldDetails()
        {
            const string url = "/meta/lists/";

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse<List<BambooHrListField>> response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync<List<BambooHrListField>>(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);

            if (string.IsNullOrWhiteSpace(response.Content))
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Data != null)
                    return response.Data;

                throw new Exception("Bamboo Response does not contain data");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrTimeOffTypeInfo> GetTimeOffTypes(string mode = "")
        {
            const string url = "/meta/time_off/types/";

            var request = GetNewRestRequest(url, Method.GET, true);

            if (!string.IsNullOrWhiteSpace(mode))
                request.AddParameter("mode", mode, ParameterType.GetOrPost);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);

            if (string.IsNullOrWhiteSpace(response.Content))
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<BambooHrTimeOffTypeInfo>();

                if (package != null)
                {
                    if (package.TimeOffTypes != null)
                        package.TimeOffTypes = package.TimeOffTypes.OrderBy(t => t.Id).ToArray();

                    return package;
                }

                throw new Exception("Bamboo Response does not contain data");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrTimeOffPolicy[]> GetTimeOffPolicies()
        {
            const string url = "/meta/time_off/policies/";

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);

            if (string.IsNullOrWhiteSpace(response.Content))
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<BambooHrTimeOffPolicy[]>();

                if (package != null)
                    return package;

                throw new Exception("Bamboo Response does not contain data");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrUser[]> GetUsers()
        {
            const string url = "/meta/users/";

            var request = GetNewRestRequest(url, Method.GET, true);

            IRestResponse<List<BambooHrUser>> response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync<List<BambooHrUser>>(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);

            if (string.IsNullOrWhiteSpace(response.Content))
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Data != null)
                    return response.Data.ToArray();

                throw new Exception("Bamboo Response does not contain data");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
        }

        public async Task<BambooHrEmployeeChangedInfo[]> GetLastChangedInfo(DateTime lastChanged, string type = "")
        {
            const string url = "/employees/changed/";

            var request = GetNewRestRequest(url, Method.GET, true);

            request.AddParameter("since", lastChanged.ToString("yyyy-MM-ddTHH:mm:sszzz"), ParameterType.GetOrPost);

            if (!string.IsNullOrWhiteSpace(type))
            {
                request.AddParameter("type", type, ParameterType.GetOrPost);
            }

            IRestResponse response;

            try
            {
                response = await _iRestClient.ExecuteTaskAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", url), response.ErrorException);
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0}", response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                var package = raw.FromJson<BambooHrEmployeeChangedInfos>();

                if (package != null && package.Employees.Any())
                {
                    return package.Employees.Values.ToArray();
                }

                throw new Exception("Bamboo Response does not contain file data");
            }

            var error = response.Headers.FirstOrDefault(x => x.Name == "X-BambooHR-Error-Messsage");
            var errorMessage = error != null ? ": " + error.Value : string.Empty;
            throw new Exception(string.Format("Bamboo Response threw error code {0} ({1}) {2}", response.StatusCode, response.StatusDescription, errorMessage));
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

        private RestRequest GetNewRestRequest(string url, Method method, bool sendingJson, bool binary = false)
        {
            var request = new RestRequest(url, method);

            if (!binary)
            {
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Encoding", "utf-8");
            }

            if (sendingJson)
            {
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("format", "JSON", ParameterType.QueryString);
            }
            else
            {
                request.AddHeader("Content-Type", "text/xml");
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
