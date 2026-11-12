using System;
using Microsoft.WindowsAzure;

namespace WorkerRole1
{
    public static class Settings
    {
        public static string InputAccount
        {
            get { return GetRequiredSetting("InputStorageConnectionString"); }
        }

        public static string OperationAccount
        {
            get { return GetRequiredSetting("OperationStorageConnectionString"); }
        }

        public static bool AddWork
        {
            get
            {
                bool enabled;
                return bool.TryParse(CloudConfigurationManager.GetSetting("SeedDemoWork"), out enabled) && enabled;
            }
        }

        public static string GetRequiredSetting(string name)
        {
            var value = CloudConfigurationManager.GetSetting(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Configure the required setting: " + name);
            }
            return value;
        }

        public static string InstanceId = "i-42a01375";
        public static string AwsHost = "ec2-54-202-144-221.us-west-2.compute.amazonaws.com";
    }
}
