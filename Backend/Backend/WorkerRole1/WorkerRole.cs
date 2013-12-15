using System;
using System.Collections.Generic;
using System.Diagnostics;
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

var ssh = @"
-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEAmkezNfQJOcr6XITfspznbcZCpu94zmB9ebKvnP9Rh5b5PfcQ/lSm40HWPQoL
hfJ2V1ucknMDEIrAFPREHLAwpSkJ8e0TJvRnHpEQ5bwVlvZRNVcog40L389fyaM8AovNrUXLlWwB
21gQVWX5YpYjxbpj/qNdU+ZX8q1WjxaSpqc/mz4ajuVGo5UAxEpZUMpYA33LR821+VsfadhX/htB
YDBepsT8/oanQuBz+8V4eDUg1Eh/BtD5HLYVETEMubwQUpRe2Ily25TTUsOWBRDu+FawSVlx3JgC
zucgwPj6SXa5yE8l6z8v2So9E3qUkw6dOHp5gp+PlR1cLJNKans1rQIDAQABAoIBAAice+pFBsbN
B5Bmcj37+AbujAXZU/rg89/5E0hGD/zrdln5El+/xdjlcdnSV7ZQRD64BFNATCl+NR52S972DqEJ
W16/htQjzCWunyzThLj6YqoBhWdiVglV/9i3XcAeoYpMXQKoFqpxjefWW0cfbju5HZ+26pymPL50
4mH2NpVKpcKpZgLcUwDTHcbbt4z/C7g50a/ErIyvbrwgGKDtKCz8BHOY9/yFR49y7YTXvjeeFFA8
3vxh+YAwweIIVBIzwdT3iUcBYf2etjVLMAhgBE4jkJJ4JymbXTHF3VqO0PTtDM+SMXiJ5Y+aGNuD
9UlDKMAw6v5Wg7XlSvjuXkAOqwECgYEA+fnrkFEsjgyFSs9mdo68DSMDLGWMJ+WmBJGmcdQss3t0
pGF2f420db/f/xe17QZikIvQQ+cBYYBocPAWWiFrtpcHeMMUvJDAUlJSVucxjh2pgQK2yipb2Jlz
EV+62n/bsNCBabi4W18gvHieEGPw8WzSA9/uOfPz1SZexaY8kY0CgYEAnf9wciYLK9e3xO9TsOE6
DVefk/7IvbXuQIbQ2VrvdPgs3E9I66Fw6RkDMC7a18WAjDOr7uT1HJFeJvGAKqZSlm1+L2OC60UK
0r4AD2shiQOOV5WGr/iaBtuFMntnlqNBDfadF/bx5d4jOWOEa76NFYQoclQQyenf++GUdBSpXKEC
gYEAxz8SKOTau5p1P+zSQduBPoNSyzdhoIdmbaveXEp/Gsxja4aX2hGL7nLyyrQOeQ+mzonyhb0C
F8Iu/R4Q4uRSo6X7+aCczbQe5z7gjI4YrKst6TvNkJR0ws+ErSt9lx1kcamwDSGEKKhJQpBthKj0
aqqPNzFtA9pT1uiPj8Dx9B0CgYAg3AVI2Dyui5i+rn+bY9ws9jJMF0ssmW/Jn8BD1DPFAfBiiWLD
Drpq4DbXiIcfJQZNIln5v0hy/pC0TLm5JQo8Gt2JgYqy35MWrUlZ64/37PNnp8NxSaTPFEypaRzs
KVvYSZf2afd5NS/iOZ5KwkCZXvkCfhVXyPo6annzgnCOwQKBgQDA1+RheQn1RtIBA+DxMzR/rrGl
xlTW4GEu/cs9M0WAXYvF4XCgV9OhUT7gYP9Lbfl9CYZJklXzf2pB14pvboB2tCyN22/Fu6haolvU
agenLrp7LLa6/6Re+/FubIPCRxp/0qQyv+d0S/htDisMR7tj/zDBOe6eCr01/b8wzxdISA==
-----END RSA PRIVATE KEY-----";

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
                azureStorageAccountName = "ubuntuaws",
                azureStorageAccountKey = "HuhJWqcuXyrMzCB4VYsZs2ZfpvpmDVqVwh7uNAxJ4qnmmjbxfnztv5L81fxM6PN/2GmqW8hYvIXxt3Wqqp9qFA=="
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
