using AWSTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException("Set AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY before running this sample.");
            }

            var ec2DataProvider = new ComputeDataProvider();

            var instances = ec2DataProvider
                .FindInstances(accessKey, secretKey)
                .ToArray();


        }
    }
}