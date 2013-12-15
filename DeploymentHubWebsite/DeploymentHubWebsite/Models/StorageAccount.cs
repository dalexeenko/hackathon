using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DeploymentHubWebsite.Models
{
    public class StorageAccount
    {
        public string StorageAccountMember { get; set; } 

        public IEnumerable<SelectListItem> StorageAccountList { get; set; }
    }
}