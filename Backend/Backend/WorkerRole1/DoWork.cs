using Common.Logging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class DoWork
    {
        #region Logger

        private static readonly ILog Logger = LogManager.GetLogger(typeof(DoWork));

        #endregion

        public async static Task Go(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!await ReadAndDoWork(token))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }
                catch (Exception ex) 
                {
                    DoWork.Logger.Debug("Go;" + ex.ToString());
                }
            }
        }

        public static Task<bool> ReadAndDoWork(CancellationToken token)
        {
            var creds = CloudStorageAccount.DevelopmentStorageAccount;
            if (!RoleEnvironment.IsEmulated)
            {
                creds = CloudStorageAccount.Parse(Settings.InputAccount);
            }

            var queueClient = creds.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("jobs");

            queue.CreateIfNotExists();

            var message = queue.GetMessage(visibilityTimeout: TimeSpan.FromHours(8));
            if (message == null)
            {
                return Task.FromResult(false);
            }

            var key = message.AsString.ToLowerInvariant();
            var tableClient = creds.CreateCloudTableClient();
            var table = tableClient.GetTableReference("jobs");

            table.CreateIfNotExists();

            var tableOperation = TableOperation.Retrieve<JobDefinition>(key, key);
            var job = table.Execute(tableOperation).Result as JobDefinition;
            if (message == null)
            {
                return Task.FromResult(false);
            }

            Migration.DoMigration(job);

            queue.DeleteMessage(message);

            return Task.FromResult(true);
        }
    }
}
