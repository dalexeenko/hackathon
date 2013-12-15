using System.Security.Cryptography.X509Certificates;
using System.Web.WebPages;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DeploymentHubWebsite.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using System.IO;
using AWSTool;
using System.Web.Routing;
using System.Threading;
using DataProviders;

namespace DeploymentHubWebsite.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                return RedirectToAction("Creds", "Home");
            }
            return View();
        }

        public ActionResult Creds()
        {
            if (Request.IsAuthenticated)
            {
                UserCreds creds = GetUserCreds();

                if (creds != null)
                {
                    ViewBag.accessKey = creds.AccessKey;
                    ViewBag.secretKey = creds.SecretKey;
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");

            }
            return View();
        }

        public ActionResult VMList()
        {
            if (!Request.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            UserCreds creds = GetUserCreds();
            if (creds == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var instances = ListAWSVirtualMachines(creds.AccessKey, creds.SecretKey);

            var opHistory = this.FindOperationHistory();

            foreach(var instance in instances)
            {
                instance.TotalAWSHourlyCost = instance.HourlyCost;
                if(instance.IsEbsEnabled)
                {
                    instance.TotalAWSHourlyCost += 0.004m;
                }
                if(instance.IsElbEnabled)
                {
                    instance.TotalAWSHourlyCost += 0.025m;
                }

                var instanceHistory = opHistory.Where(i => i.instanceId == instance.InstanceId);
                instance.MigrationStarted = instanceHistory.Any();

                if (instance.MigrationStarted)
                {
                    instance.Step = instanceHistory.OrderBy(i => i.StepId).Last().StepId;
                }

                instance.AzureHourlyCompute = CostDataProvider.InstanceToAzure(instance.Type).Item2;
                instance.TotalAzureHourlyCost = instance.AzureHourlyCompute + 0.0028m;

            }

            ViewBag.totalAWSCost = instances.Select(i => i.TotalAWSHourlyCost).Sum();
            ViewBag.totalAzureCost = instances.Select(i => i.TotalAzureHourlyCost).Sum();

            ViewBag.instances = instances;
            ViewBag.none = !instances.Any();

            var azureinstances = GetAzureVirtualMachines();
            
            //foreach(var instance in azureinstances)
            //{
            //    //CostDataProvider.InstanceCost()

            //}

            ViewBag.azureinstances = azureinstances;
            return View();
        }

        public ActionResult UploadSsh()
        {
            System.Diagnostics.Trace.TraceError("Entered UploadSsh");

            List<AzureStorageAccountInfo> list = this.GetStorageAccounts();

            System.Diagnostics.Trace.TraceError("Exited GetStorageAccounts");

            var a = list.Select(x => new SelectListItem { Value = x.Name, Text = x.Name });

            var model = new StorageAccount
            {
                StorageAccountList = new SelectList(a, "Value", "Text")
            };

            var d = new SelectList(a, "Value", "Text");

            ViewBag.list = d;

            ViewBag.instance_id = (Request.QueryString != null && Request.QueryString["instance_id"] != null) ? Request.QueryString["instance_id"].ToString() : string.Empty;

            System.Diagnostics.Trace.TraceError("Exited UploadSsh");

            return View();
        }

        public ActionResult Status(string instance_id)
        {
            //var waitHandle = new AutoResetEvent(false);
            //ThreadPool.RegisterWaitForSingleObject(
            //    waitHandle,
            //    // Method to execute
            //    (state, timeout) =>
            //    {
            //        // Insert Charles's code to get status here

            //        if (ViewBag.step == "step1")
            //        {
            //            ViewBag.step = "step2";
            //        }
            //        else
            //        {
            //            ViewBag.step = "step1";
            //        }
            //    },
            //    // optional state object to pass to the method
            //    null,
            //    // Execute the method after 5 seconds
            //    TimeSpan.FromSeconds(5),
            //    // Set this to false to execute it repeatedly every 5 seconds
            //    false
            //);

            //ViewBag.step = "step1";

            var ops = this.FindOperationHistory().Where(i => i.instanceId == instance_id)
                .OrderBy(i => i.StartTime)
                .ToArray();

            for (int cv = 0; cv < ops.Length - 1; ++cv)
            {
                ops[cv].Status = "Completed";
            }

            ViewBag.Status = ops.GroupBy(history => history.StepId)
                .OrderBy(group => group.Key)
                .Select(group => group.OrderBy(history => history.SubstepId).ToArray()).ToArray(); ;
            
            return View();
        }

        public EC2Instance[] ListAWSVirtualMachines(string accessKey, string secretKey, bool loadImages = true)
        {
            // Example keys below
            // "AKIAIEFHKSHZIZMNAO5A", "O9IWZnMpfiPSu9hQ4CVilBVg8Mw46HnYH0/x8BGx"
            // or, for stephen's account "AKIAJLTODBLZLH4CTFYQ", "xjmXd9iMkHOnz3fSbERVsvpB0KefYVC061q3UaY5"
            var ec2DataProvider = new ComputeDataProvider();
            var instances = ec2DataProvider
                .FindInstances(keyId: accessKey, key: secretKey, loadImages: loadImages)
                .ToArray();

            return instances;
        }

        public UserCreds GetUserCreds()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = new CloudTableClient(storageAccount.TableEndpoint, storageAccount.Credentials);

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference("usercreds");
            table.CreateIfNotExists();

            //// Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<UserCreds>(User.Identity.GetUserId(), User.Identity.GetUserId());

            //// Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            //// Assign the result to a CustomerEntity object.
            UserCreds updateEntity = (UserCreds)retrievedResult.Result;

            return updateEntity;
        }

        #region Fetching Azure Management Operations

        public List<AzureVmInfo> GetAzureVirtualMachines()
        {
            var userCreds = GetUserCreds();

            var binaryCert = Convert.FromBase64String(userCreds.ManagementCertificate);
            var certificate = new X509Certificate2(binaryCert);

            var azureComputeProvider = new AzureComputeProvider();

            return azureComputeProvider.ListVirtualMachines(
                subscriptionId: userCreds.ManagementSubscription,
                certificate: certificate).Result.ToList();
        }

        public List<AzureStorageAccountInfo> GetStorageAccounts()
        {
            System.Diagnostics.Trace.TraceError("Entered GetStorageAccounts");

            var userCreds = GetUserCreds();

            System.Diagnostics.Trace.TraceError("Calling into FromBase64String");

            System.Diagnostics.Trace.TraceError("cert = " + userCreds.ManagementCertificate);

            var binaryCert = Convert.FromBase64String(userCreds.ManagementCertificate);

            System.Diagnostics.Trace.TraceError("Newing X509Certificate2");

            var certificate = new X509Certificate2(binaryCert);

            System.Diagnostics.Trace.TraceError("Newing AzureStorageProvider");

            var azureStoreProvider = new AzureStorageProvider();

            System.Diagnostics.Trace.TraceError("Calling into ListStorageAccounts");

            return azureStoreProvider.ListStorageAccounts(
                subscriptionId: userCreds.ManagementSubscription,
                certificate: certificate).Result.ToList();

            // Test data
            //List<AzureStorageAccountInfo> list = new List<AzureStorageAccountInfo>();

            //list.Add(new AzureStorageAccountInfo() { Key = "key", Name = "name", URI = "URI" });
            //list.Add(new AzureStorageAccountInfo() { Key = "key2", Name = "name2", URI = "URI2" });

            //return list;
        }

        #endregion

        #region XML serialization

        public XElement XmlGetValue(string xml, string xPathSelectElement, string ns = "")
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());
            if (!String.IsNullOrEmpty(ns))
            {
                manager.AddNamespace("ns", ns);
            }

            return XDocument.Parse(xml).XPathSelectElement(xPathSelectElement, manager);
        }

        public static IEnumerable<XElement> XmlGetValues(string xml, string xPathSelectElement, string ns = "")
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());
            if (!String.IsNullOrEmpty(ns))
            {
                manager.AddNamespace("ns", ns);
            }

            return XDocument.Parse(xml).XPathSelectElements(xPathSelectElement, manager);
        }

        #endregion

        #region Queuing Migration Items

        public void QueueMigrationItem(string instanceId, string sshUserName, string sshKey, string storageAccount, string storageKey)
        {
            var userCreds = GetUserCreds();

            var instances = ListAWSVirtualMachines(userCreds.AccessKey, userCreds.SecretKey, loadImages: false);

            var targetInstance = instances.FirstOrDefault(instance => instance.InstanceId.Equals(instanceId, StringComparison.InvariantCultureIgnoreCase));

            if (storageKey == null)
            {
                var binaryCert = Convert.FromBase64String(userCreds.ManagementCertificate);
                var certificate = new X509Certificate2(binaryCert);
                var azureStoreProvider = new AzureStorageProvider();

                storageKey = azureStoreProvider.GetStorageAccountKey(
                    subscriptionId: userCreds.ManagementSubscription,
                    certificate: certificate,
                    serviceName: storageAccount).Result;
            }

            if (string.IsNullOrWhiteSpace(sshUserName))
            {
                // TODO: what can we default for user name?
                sshUserName = "ec2-user";
            }

            if (targetInstance != null)
            {
                QueueMigrationItem(
                    publishingSetting: userCreds.ManagementProfile,
                    instance: targetInstance,
                    sshUserName: sshUserName,
                    sshKey: sshKey,
                    storageAccountName: storageAccount,
                    storageAccountKey: storageKey);
            }
            else
            {
                // EMIT AN INVLAID INSTANCE ID
            }
        }

        public void QueueMigrationItem(
            string publishingSetting,
            EC2Instance instance,
            string sshUserName,
            string sshKey,
            string storageAccountName,
            string storageAccountKey)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            var customerStorageCreds = new StorageCredentials(storageAccountName, storageAccountKey);
            CloudStorageAccount customerStorageAccount = new CloudStorageAccount(customerStorageCreds, true);

            // Create the clients
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudBlobClient blobClient = customerStorageAccount.CreateCloudBlobClient();

            // Create the table & queue if it doesn't exist.
            CloudTable table = tableClient.GetTableReference("jobs");
            table.CreateIfNotExists();

            // CHANGE THE QUEUE NAME TO jobs for production
            CloudQueue queue = queueClient.GetQueueReference("jobs");
            queue.CreateIfNotExists();

            CloudBlobContainer container = blobClient.GetContainerReference("deploymenthub");
            container.CreateIfNotExists();

            // Upload the blob from a text string.
            CloudBlockBlob blob = container.GetBlockBlobReference("publishing.publishsettings");
            blob.UploadText(publishingSetting);

            // prepare the object
            var operationId = Guid.NewGuid().ToString();
            var userId = User.Identity.GetUserId();

            JobDefinition jobDefinition = new JobDefinition();
            jobDefinition.PartitionKey = operationId;
            jobDefinition.RowKey = operationId;

            jobDefinition.operationId = operationId;
            jobDefinition.userId = userId;
            jobDefinition.instanceId = instance.InstanceId;

            jobDefinition.awsHost = instance.PublicHostName;
            jobDefinition.awsSSHkeys = sshKey;
            jobDefinition.awsUserName = sshUserName;
            jobDefinition.azureStorageAccountName = storageAccountName;
            jobDefinition.azureStorageAccountKey = storageAccountKey;

            // Insert the item into the table
            TableOperation insertOp = TableOperation.InsertOrReplace(jobDefinition);
            var result = table.Execute(insertOp).Result;

            // insert the item into the queue
            CloudQueueMessage message = new CloudQueueMessage(operationId);
            queue.AddMessage(message);

            ViewBag.operationId = operationId;
        }

        #endregion

        public Operation[] FindOperationHistory()
        {
            var userCreds = GetUserCreds();
            var userId = userCreds.UserId;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the clients
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table & queue if it doesn't exist.
            CloudTable table = tableClient.GetTableReference("operations");
            table.CreateIfNotExists();

            var pk = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId);
            var secondPk = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "user");

            var firstOps = table.ExecuteQuery<Operation>(
                new TableQuery<Operation>().Where(pk)).ToArray();

            var secondOps = table.ExecuteQuery<Operation>(
                new TableQuery<Operation>().Where(pk)).ToArray();

            return firstOps.Concat(secondOps).ToArray();
        }

            // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitKeys(string accessKey, string secretKey, HttpPostedFileBase file)
        {

            if (Request.IsAuthenticated)
            {
                // Retrieve the storage account from the connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
             
                if (file != null && file.ContentLength > 0 && !String.IsNullOrWhiteSpace(accessKey) && !String.IsNullOrWhiteSpace(secretKey))
                {
                    var fileName = Path.GetFileName(file.FileName);

                    BinaryReader b = new BinaryReader(file.InputStream);
                    byte[] binData = b.ReadBytes((int)file.InputStream.Length);

                    string content = System.Text.Encoding.UTF8.GetString(binData);
   
                    // Create the table client.
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                    // Create the table if it doesn't exist.
                    CloudTable table = tableClient.GetTableReference("usercreds");
                    table.CreateIfNotExists();

                    // Create a retrieve operation that takes a customer entity.
                    TableOperation retrieveOperation = TableOperation.Retrieve<UserCreds>(User.Identity.GetUserId(), User.Identity.GetUserId());

                    // Execute the operation.
                    TableResult retrievedResult = table.Execute(retrieveOperation);

                    // Assign the result to a CustomerEntity object.
                    UserCreds updateEntity = (UserCreds)retrievedResult.Result;

                    if(updateEntity==null)
                    {
                        string id =  User.Identity.GetUserId();
                        updateEntity = new UserCreds(id);
                        updateEntity.RowKey = id;
                        updateEntity.PartitionKey = id;
                    }

                    updateEntity.AccessKey = accessKey;
                    updateEntity.SecretKey = secretKey;

                    updateEntity.ManagementProfile = content;

                    // parse the profile
                    if (XmlGetValue(content, "/PublishData/PublishProfile")
                        .Attribute("ManagementCertificate") != null)
                    {
                        updateEntity.ManagementCertificate = XmlGetValue(content, "/PublishData/PublishProfile")
                            .Attribute("ManagementCertificate").Value;
                        updateEntity.ManagementSubscription = XmlGetValues(content, "/PublishData/PublishProfile/Subscription")
                            .Select(element => element.Attribute("Id").Value)
                            .FirstOrDefault();
                    }
                    else
                    {
                        var subElement = XmlGetValues(content, "/PublishData/PublishProfile/Subscription")
                            .FirstOrDefault();

                        if (subElement != null)
                        {
                            updateEntity.ManagementSubscription = subElement.Attribute("Id").Value;
                            updateEntity.ManagementCertificate = subElement.Attribute("ManagementCertificate").Value;
                        }
                    }

                    // Create the TableOperation that inserts the customer entity.
                    TableOperation insertOperation = TableOperation.InsertOrReplace(updateEntity);

                    // Execute the insert operation.
                    table.Execute(insertOperation);

                    ViewBag.accessKey = updateEntity.AccessKey;
                    ViewBag.secretKey = updateEntity.SecretKey;
                }
                else
                {
                       return RedirectToAction("Creds", "Home");
                }
            } else
            {
                return RedirectToAction("Index","Home");
            }

            return RedirectToAction("VMList", "Home");
        }

        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitSshKeys(HttpPostedFileBase file, string identity_id, string sshUser, string selector)
        {
            if (Request.IsAuthenticated)
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);

                    BinaryReader b = new BinaryReader(file.InputStream);
                    byte[] binData = b.ReadBytes((int)file.InputStream.Length);

                    string content = System.Text.Encoding.UTF8.GetString(binData);

                    // Create the table client.
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                    // Create the table if it doesn't exist.
                    CloudTable table = tableClient.GetTableReference("key");
                    table.CreateIfNotExists();

                    // Create a retrieve operation that takes a customer entity.
                    TableOperation retrieveOperation = TableOperation.Retrieve<SshKey>(User.Identity.GetUserId(), User.Identity.GetUserId());

                    // Execute the operation.
                    TableResult retrievedResult = table.Execute(retrieveOperation);

                    // Assign the result to a CustomerEntity object.
                    SshKey updateEntity = (SshKey)retrievedResult.Result;

                    if (updateEntity == null)
                    {
                        string id = User.Identity.GetUserId();
                        updateEntity = new SshKey(id);
                        updateEntity.RowKey = identity_id;
                        updateEntity.PartitionKey = identity_id;
                    }

                    updateEntity.FileContent = content;
                    updateEntity.InstanceId = identity_id;
                    updateEntity.SshUser = sshUser;
                    updateEntity.StorageAccount = selector;

                    TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateEntity);
                    // Execute the operation.
                    table.Execute(insertOrReplaceOperation);

                    ViewBag.fileContent = updateEntity.FileContent;

                    this.QueueMigrationItem(
                        updateEntity.InstanceId,
                        updateEntity.SshUser,
                        updateEntity.FileContent,
                        updateEntity.StorageAccount,
                        null /* storageKey */);
                }
            }

            return RedirectToAction("VMList", "Home");
        }
    }
}