using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSTool
{
    public class EC2Instance
    {
        public string InstanceId { get; set; }
        public string State { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public string LaunchTime { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public decimal HourlyCost { get; set; }

        public string KeyName { get; set; }
        public string PublicHostName { get; set; }
        public string IpAddress { get; set; }
        public string PrivateIpAddress { get; set; }

        public string ImageId { get; set; }
        public string ImageDescription { get; set; }
        public string ImageName { get; set; }
        public bool IsWindows { get; set; }

        public bool IsRunning { get; set; }
        public bool IsEbsEnabled { get; set; }
        public bool IsElbEnabled { get; set; }

        public decimal TotalAWSHourlyCost { get; set; }

        public decimal AzureHourlyCompute { get; set; }

        public decimal TotalAzureHourlyCost { get; set; }

        public bool MigrationStarted { get; set; }

        public string Step { get; set; }
    }
}