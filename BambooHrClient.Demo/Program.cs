using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BambooHrClient.Extensions;
using BambooHrClient.Models;

namespace BambooHrClient.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Task.WaitAll(MainAsync(args));
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        static async Task MainAsync(string[] args)
        {
            // Uncomment the following examples to try them out

            //ListHolidays();
            //Console.WriteLine();


            //DisplayEmployeeInfos();
            //Console.WriteLine();

            // THIS WILL CREATE AN ACTUAL EMPLOYEE IN YOUR SYSTEM
            //AddEmployee("John", "Doe");
            //Console.WriteLine();

            // THIS WILL UPDATE AN ACTUAL EMPLOYEE IN YOUR SYSTEM
            // Update the ID to whatever yours is
            //var employee = await GetEmployee(40525);
            //employee.FirstName = "John-updated2";
            //await UpdateEmployee(employee);
            //Console.WriteLine("Updated employee first name");
            //Console.WriteLine();


            //await DisplayTabluarData(1, BambooHrTableType.Compensation);
            //Console.WriteLine();
            //await DisplayTabluarData(1, BambooHrTableType.Dependents);
            //Console.WriteLine();
            //await DisplayTabluarData(1, BambooHrTableType.EmergencyContacts);
            //Console.WriteLine();
            //await DisplayTabluarData(1, BambooHrTableType.EmploymentStatus);
            //Console.WriteLine();
            //await DisplayTabluarData(1, BambooHrTableType.JobInfo);
            //Console.WriteLine();


            // THIS WILL CREATE AN ACTUAL TIME OFF REQUEST IN YOUR SYSTEM
            //await CreateTimeOffRequest();
            //Console.WriteLine();

            //await DisplayAssignedTimeOffPolicies(1);
            //Console.WriteLine();

            //await DisplayFutureTimeOffBalanceEstimates();
            //Console.WriteLine();

            await DisplayWhosOut();
            Console.WriteLine();


            //GetEmployeePhoto(123456789);
            //Console.WriteLine();

            //GetEmployeePhotoUrl("test@example.com");
            //Console.WriteLine();


            //await DisplayFields();
            //Console.WriteLine();

            //await DisplayTabularFields();
            //Console.WriteLine();

            //await DisplayListFieldDetails();
            //Console.WriteLine();

            //await DisplayTimeOffTypes();
            //Console.WriteLine();

            //await DisplayTimeOffTypesByPermissions();
            //Console.WriteLine();

            //await DisplayTimeOffPolicies();
            //Console.WriteLine();

            //await DisplayUsers();
            //Console.WriteLine();


            //await DisplayLastChangedInfo();
            //Console.WriteLine();


            Console.WriteLine();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        private async static void DisplayEmployeeInfos()
        {
            var bambooHrClient = new BambooHrClient();

            var employees = await bambooHrClient.GetEmployees();

            foreach (var employee in employees)
            {
                Console.WriteLine(employee.LastFirst);
            }

            // Display the details of the last employee in the list to compare to the regular GetEmployee call
            Console.WriteLine();
            Console.WriteLine(employees.Last().PropsToString());
        }

        private async static void AddEmployee(string firstName, string lastName)
        {
            var bambooHrClient = new BambooHrClient();

            var bambooHrEmployee = new BambooHrEmployee
            {
                FirstName = firstName,
                LastName = lastName,
                WorkEmail = "jd@example.com"
            };

            var url = await bambooHrClient.AddEmployee(bambooHrEmployee);

            Console.WriteLine($"Employee created at {url}");
            Console.WriteLine(bambooHrEmployee.PropsToString());
        }

        private async static Task<BambooHrEmployee> GetEmployee(int employeeId)
        {
            var bambooHrClient = new BambooHrClient();
            var employee = await bambooHrClient.GetEmployee(employeeId);

            Console.WriteLine($"Got employee with ID {employee.Id}");

            return employee;
        }

        private async static Task<bool> UpdateEmployee(BambooHrEmployee bambooHrEmployee)
        {
            var bambooHrClient = new BambooHrClient();
            await bambooHrClient.UpdateEmployee(bambooHrEmployee);

            Console.WriteLine($"Updated employee with ID {bambooHrEmployee.Id}");

            return true;
        }

        private async static Task DisplayTabluarData(int employeeId, BambooHrTableType tableType)
        {
            var bambooHrClient = new BambooHrClient();

            var data = await bambooHrClient.GetTabularData(employeeId.ToString(), tableType);

            foreach (var row in data)
            {
                foreach (var key in row.Keys)
                {
                    Console.WriteLine(row[key]);
                }
            }
        }

        private async static Task DisplayAssignedTimeOffPolicies(int employeeId)
        {
            var bambooHrClient = new BambooHrClient();

            var timeOffPolicies = await bambooHrClient.GetAssignedTimeOffPolicies(employeeId);

            foreach (var timeOffPolicy in timeOffPolicies)
            {
                Console.WriteLine(timeOffPolicy.PropsToString());
            }
        }

        private async static Task DisplayFutureTimeOffBalanceEstimates(int employeeId)
        {
            var bambooHrClient = new BambooHrClient();

            var estimates = await bambooHrClient.GetFutureTimeOffBalanceEstimates(employeeId);

            foreach (var estimate in estimates)
            {
                Console.WriteLine(estimate.PropsToString());
            }
        }

        private async static Task DisplayWhosOut()
        {
            var bambooHrClient = new BambooHrClient();

            var whosOut = await bambooHrClient.GetWhosOut();

            foreach (var whosOutInfo in whosOut)
            {
                Console.WriteLine(whosOutInfo.PropsToString());
            }
        }

        private async static Task DownloadEmployeePhoto(int employeeId)
        {
            var bambooHrClient = new BambooHrClient();

            var fileData = await bambooHrClient.GetEmployeePhoto(employeeId);

            File.WriteAllBytes(@"C:\test.jpeg", fileData);
        }

        private static void DisplayEmployeePhotoUrl(string employeeEmail)
        {
            var bambooHrClient = new BambooHrClient();

            var url = bambooHrClient.GetEmployeePhotoUrl(employeeEmail);

            Console.WriteLine(url);
        }

        private async static Task DisplayFields()
        {
            var bambooHrClient = new BambooHrClient();

            var fields = await bambooHrClient.GetFields();

            foreach (var field in fields)
            {
                Console.WriteLine(field.PropsToString());
            }
        }

        private async static Task DisplayTabularFields()
        {
            var bambooHrClient = new BambooHrClient();

            var tables = await bambooHrClient.GetTabularFields();

            foreach (var table in tables)
            {
                Console.WriteLine();
                Console.WriteLine(table.Alias);

                foreach (var field in table.Fields)
                {
                    Console.WriteLine(field.PropsToString());
                }
            }
        }

        private async static Task DisplayListFieldDetails()
        {
            var bambooHrClient = new BambooHrClient();

            var fields = await bambooHrClient.GetListFieldDetails();

            foreach (var field in fields)
            {
                Console.WriteLine();
                Console.WriteLine(field.PropsToString());

                foreach (var option in field.Options)
                {
                    Console.WriteLine(option.PropsToString());
                }
            }
        }

        public async static Task DisplayTimeOffTypes()
        {
            var bambooHrClient = new BambooHrClient();

            var timeOffInfo = await bambooHrClient.GetTimeOffTypes();

            Console.WriteLine("Time Off Types:");
            foreach (var timeOffType in timeOffInfo.TimeOffTypes)
            {
                Console.WriteLine(timeOffType.PropsToString());
            }

            Console.WriteLine("Default Hours:");
            foreach (var defaultHour in timeOffInfo.DefaultHours)
            {
                Console.WriteLine(defaultHour.PropsToString());
            }
        }

        public async static Task DisplayTimeOffTypesByPermissions()
        {
            var bambooHrClient = new BambooHrClient();

            var timeOffInfo = await bambooHrClient.GetTimeOffTypes("request");

            Console.WriteLine("Time Off Types:");
            foreach (var timeOffType in timeOffInfo.TimeOffTypes)
            {
                Console.WriteLine(timeOffType.PropsToString());
            }

            Console.WriteLine("Default Hours:");
            foreach (var defaultHour in timeOffInfo.DefaultHours)
            {
                Console.WriteLine(defaultHour.PropsToString());
            }
        }

        public async static Task DisplayTimeOffPolicies()
        {
            var bambooHrClient = new BambooHrClient();

            var timeOffPolicies = await bambooHrClient.GetTimeOffPolicies();

            foreach (var timeOffPolicy in timeOffPolicies)
            {
                Console.WriteLine(timeOffPolicy.PropsToString());
            }
        }

        public async static Task DisplayUsers()
        {
            var bambooHrClient = new BambooHrClient();

            var users = await bambooHrClient.GetUsers();

            foreach (var user in users)
            {
                Console.WriteLine(user.PropsToString());
            }
        }

        public async static Task DisplayLastChangedInfo()
        {
            var bambooHrClient = new BambooHrClient();

            var users = await bambooHrClient.GetLastChangedInfo(DateTime.Now.AddDays(-7));

            foreach (var user in users)
            {
                Console.WriteLine(user.PropsToString());
            }
        }

        public async static void ListHolidays()
        {
            var bambooHrClient = new BambooHrClient();

            var holidays = await bambooHrClient.GetHolidays(DateTime.Now, DateTime.Now.AddYears(1));

            foreach (var holiday in holidays)
            {
                Console.WriteLine("{0} {1} {2}", holiday.Id, holiday.Start.ToString(Constants.BambooHrDateFormat), holiday.Name);
            }
        }

        public async static Task<int> CreateTimeOffRequest()
        {
            var userId = 5;
            var reason = "vacation";
            var startDate = DateTime.Now;
            var endDate = DateTime.Now.AddDays(1);

            var timeOffTypeId = GetTimeOffTypeId(reason);

            var bambooHrClient = new BambooHrClient();

            var employee = bambooHrClient.GetEmployee(userId);

            var result = await bambooHrClient.CreateTimeOffRequest(employee.Id, timeOffTypeId, startDate, endDate);

            Console.WriteLine($"Result: {result}");

            return result;
        }

        /// <summary>
        /// You probably need some method or something to map your business logic to the Bamboo HR Time Off Type ID's
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        private static int GetTimeOffTypeId(string reason)
        {
            if (reason == "vacation")
            {
                return 1;
            }
            else if (reason == "sick")
            {
                return 2;
            }

            throw new Exception(string.Format("Your reason does not map to a Bamboor HR Time Off Type: {0}.", reason));
        }
    }
}
