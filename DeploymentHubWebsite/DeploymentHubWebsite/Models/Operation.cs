using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace DeploymentHubWebsite.Models
{
    public class Operation : TableEntity
    {
        public string userId { get; set; }
        public string OperationId { get; set; }
        public string instanceId { get; set; }
        public string StepId { get; set; }
        public string SubstepId { get; set; }
        public string Message { get; set; }
        public string Command { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}