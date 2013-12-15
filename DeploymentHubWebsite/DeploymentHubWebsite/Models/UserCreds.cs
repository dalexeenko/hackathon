using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DeploymentHubWebsite.Models
{
    public class UserCreds : TableEntity
    {
        public UserCreds(string userId)
        {
            this.UserId = userId;
            //this.AccessKey = accessKey;
            //this.SecretKey = secretKey;
            //this.ManagementProfile = managementProfile;
        }

        public UserCreds() {  }

        public string UserId { get; set; }

        public string AccessKey { get; set; }
        public string SecretKey { get; set; }

        public string ManagementProfile { get; set; }
        public string ManagementCertificate { get; set; }
        public string ManagementSubscription { get; set; }
    }
}