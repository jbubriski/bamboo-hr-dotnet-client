using System;

namespace BambooHrClient.Models
{
    public class BambooHrTimeOffTypeInfo
    {
        public BambooHrTimeOffType[] TimeOffTypes { get; set; }
        public BambooHrDefaultHour[] DefaultHours { get; set; }
    }

    public class BambooHrTimeOffType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Units { get; set; }
        public string Color { get; set; }
    }

    public static class BambooHrTimeOffTypeUnit
    {
        public static readonly string Days = "days";
        public static readonly string Hours = "hours";
    }

    public class BambooHrDefaultHour
    {
        public string Name { get; set; }
        public int Amount { get; set; }
    }

    public class BambooHrTimeOffPolicy
    {
        public int Id { get; set; }
        public int TimeOffTypeId { get; set; }
        public string Name { get; set; }
        public DateTime EffectiveDate { get; set; }
    }
}
