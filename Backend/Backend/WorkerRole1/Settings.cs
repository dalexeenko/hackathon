using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public static class Settings
    {
        public const string InputAccount = "DefaultEndpointsProtocol=https;AccountName=dephubinput1;AccountKey=D9Et5HUHTb0pQAIGruKeuN9bbZ2tc4RtXBgyVBV9nmwHpUp38Qey2qNvtvX5bt3XW0uBTATzEWfe77acc7YucA==";
        public const bool AddWork = true;
        public static string InstanceId = "i-42a01375";
        public static string AwsHost = "ec2-54-202-144-221.us-west-2.compute.amazonaws.com";
    }
}
