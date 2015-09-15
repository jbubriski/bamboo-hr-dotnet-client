using System;
using System.Linq;
using System.Threading.Tasks;

namespace BambooHrClient.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            ListHolidays();
            Console.WriteLine();

            // Uncomment the following examples

            DisplayEmployeeInfos();
            Console.WriteLine();

            DisplayEmployeeInfo("youremail@yourcompany.com");
            Console.WriteLine();

            // THIS WILL CREATE AN ACTUAL TIME OFF REQUEST IN YOUR SYSTEM
            //CreateTimeOffRequest();

            Console.WriteLine();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        private async static void DisplayEmployeeInfo(string workEmail)
        {
            var bambooHrClient = new BambooHrClient();

            var employee = await bambooHrClient.GetEmployee(workEmail);

            Console.WriteLine(employee);
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
            Console.WriteLine(employees.Last());
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
            var userName = "jsmith";
            var reason = "vacation";
            var startDate = DateTime.Now;
            var endDate = DateTime.Now.AddDays(1);

            var timeOffTypeId = GetTimeOffTypeId(reason);

            var bambooHrClient = new BambooHrClient();

            var employee = bambooHrClient.GetEmployee(userName);

            return await bambooHrClient.CreateTimeOffRequest(employee.Id, timeOffTypeId, startDate, endDate);
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
