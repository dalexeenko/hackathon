using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class JobDefinition : TableEntity
    {
        public string operationId { get; set; }
        public string userId { get; set; }
        public string instanceId { get; set; }
        public string awsHost { get; set; }
        public string awsUserName { get; set; }
        public string awsSSHkeys { get; set; }
        public string azureStorageAccountName { get; set; }
        public string azureStorageAccountKey { get; set; }
    }
}
