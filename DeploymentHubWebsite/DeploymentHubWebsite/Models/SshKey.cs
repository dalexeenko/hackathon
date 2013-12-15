using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DeploymentHubWebsite.Models
{
    public class SshKey : TableEntity
    {
        public SshKey(string instanceId)
        {
            this.InstanceId = instanceId;
            //this.AccessKey = accessKey;
            //this.SecretKey = secretKey;
            //this.ManagementProfile = managementProfile;
        }

        public SshKey() {  }

        public string InstanceId { get; set; }
        public string FileContent { get; set; }
        public string SshUser { get; set; }
        public string StorageAccount { get; set; }
    }
}