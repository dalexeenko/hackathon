using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Queue;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        #region Logger

        private static readonly ILog Logger = LogManager.GetLogger(typeof(WorkerRole));

        #endregion

        private CancellationTokenSource tcs = null;
        private Task[] threads = null;

        public override void Run()
        {
            try
            {
                WorkerRole.Logger.Debug("Run_Begin;");
                this.threads = this.Go(tcs.Token);
                base.Run();

                WorkerRole.Logger.Debug("Run_End;");
            }
            catch (Exception ex)
            {
                WorkerRole.Logger.Debug("Run_End;" + ex.ToString());
                throw;
            }
        }

        private Task[] Go(CancellationToken cancellationToken)
        {
            var list = new List<Task>();
            for (int i = 0; i < 24; i++)
            {
                list.Add(DoWork.Go(cancellationToken));
            }

            return list.ToArray();
        }

        public override bool OnStart()
        {
            try
            {
                if (Settings.AddWork)
                {
                   this.InsertWork();
                }

                WorkerRole.Logger.Debug("OnStart_Beign;");
                tcs = new CancellationTokenSource();

                ServicePointManager.SetTcpKeepAlive(true, 30000, 1000);
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                WorkerRole.Logger.Debug("OnStart_End;");

                return base.OnStart();
            }
            catch (Exception ex)
            {
                WorkerRole.Logger.Debug("OnStart;" + ex.ToString());
                throw;
            }
        }

        private void InsertWork()
        {
            var creds = CloudStorageAccount.Parse(Settings.InputAccount);

            //var creds = CloudStorageAccount.DevelopmentStorageAccount;


            var key = Guid.NewGuid().ToString().ToLowerInvariant();
            var tableClient = creds.CreateCloudTableClient();
            var table = tableClient.GetTableReference("jobs");

            table.CreateIfNotExists();

            var ssh = File.ReadAllText(Settings.GetRequiredSetting("DemoSshPrivateKeyPath"));

            var job = new JobDefinition
            {
                userId = "user",
                operationId = key,
                instanceId = Settings.InstanceId,
                PartitionKey = key,
                RowKey = key,
                awsHost = Settings.AwsHost,
                awsUserName = "bitnami",
                awsSSHkeys = ssh,
                azureStorageAccountName = Settings.GetRequiredSetting("DemoAzureStorageAccountName"),
                azureStorageAccountKey = Settings.GetRequiredSetting("DemoAzureStorageAccountKey")
            };

            var tableOperation = TableOperation.InsertOrReplace(job);
            var res = table.Execute(tableOperation);

            var queueClient = creds.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("jobs");

            queue.CreateIfNotExists();

            var message = new CloudQueueMessage(key);
            queue.AddMessage(message);
        }

        public override void OnStop()
        {
            try
            {
                WorkerRole.Logger.Debug("OnStop_Being;");
                this.tcs.Cancel();
                this.OnStop();
                WorkerRole.Logger.Debug("OnStop_End;");
            }
            catch (Exception ex)
            {
                WorkerRole.Logger.Debug("OnStop;" + ex.ToString());
                throw;
            }
        }
    }
}
