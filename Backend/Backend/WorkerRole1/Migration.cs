using Common.Logging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public static class Migration
    {
        #region Logger

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Migration));

        #endregion

        public static void DoMigration(JobDefinition job)
        {
            Migration.Logger.Debug(string.Format("Migration start:{0} - {1}", job.userId, job.instanceId));

            {
                Migration.RunCommand(job,
                    stepId: "1 - System preparation",
                    substepId: "1",
                    message: "Updating apt-get packages",
                    command: "sudo apt-get update"
                );

                Migration.RunCommand(job,
                    stepId: "1 - System preparation",
                    substepId: "2",
                    message: "Pointing apt-get archive to Azure",
                    command: "sudo sed -i 's,us-west-2.ec2.archive.ubuntu.com,azure.archive.ubuntu.com,g' /etc/apt/sources.list"
                );

                Migration.RunCommand(job,
                      stepId: "1 - System preparation",
                      substepId: "3",
                      message: "Updating apt-get packages",
                      command: "sudo apt-get update"
                );

                Migration.RunCommand(job,
                      stepId: "1 - System preparation",
                      substepId: "4",
                      message: "Installing Windows Azure Linux agent",
                      command: "sudo apt-get install -y walinuxagent"
                );

                Migration.RunCommand(job,
                      stepId: "1 - System preparation",
                      substepId: "5",
                      message: "Deprovision VM",
                      command: "sudo waagent -force -deprovision"
                );

                Migration.RunCommand(job,
                      stepId: "1 - System preparation",
                      substepId: "6",
                      message: "Flush data to disk",
                      command: "sudo sync"
                );

                Thread.Sleep(TimeSpan.FromSeconds(5));

                Migration.RunCommand(job,
                      stepId: "2 - Snapshoting",
                      substepId: "1",
                      message: "Cloning hard drive",
                      command: "sudo dd if=/dev/xvda1 of=/mnt/xvda1.img conv=notrunc bs=4096"
                );

                Migration.RunCommand(job,
                      stepId: "2 - Snapshoting",
                      substepId: "2",
                      message: "Downloading Azure base image",
                      command: "sudo wget https://s3-us-west-2.amazonaws.com/deploymenthub/sda.img.bz2 -O /mnt/sda.img.bz2"
                );

                Migration.RunCommand(job,
                      stepId: "2 - Snapshoting",
                      substepId: "3",
                      message: "Unpacking Azure base image",
                      command: "sudo bunzip2 -k /mnt/sda.img.bz2"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "01",
                      message: "Setting up first loop device",
                      command: "sudo losetup -fv -o 1048576 /mnt/sda.img"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "02",
                      message: "Setting up second loop device",
                      command: "sudo losetup -fv /mnt/xvda1.img"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "03",
                      message: "Creating first mount dir",
                      command: "sudo mkdir -p /mnt/loop/0"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "04",
                      message: "Creating second mount dir",
                      command: "sudo mkdir -p /mnt/loop/1"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "05",
                      message: "Mounting first drive",
                      command: "sudo mount -t ext4 /dev/loop0 /mnt/loop/0"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "06",
                      message: "Mounting second drive",
                      command: "sudo mount -t ext4 /dev/loop1 /mnt/loop/1"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "07",
                      message: "Coping files..",
                      command: "sudo cp -a /mnt/loop/1/* /mnt/loop/0/"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "08",
                      message: "Coping files..",
                      command: "sudo cp -af /mnt/loop/0/azure/boot/* /mnt/loop/0/boot/"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "09",
                      message: "Coping files..",
                      command: "sudo cp -af /mnt/loop/0/azure/etc/cloud/* /mnt/loop/0/etc/cloud/"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "10",
                      message: "Coping files..",
                      command: "sudo cp -af /mnt/loop/0/azure/etc/default/* /mnt/loop/0/etc/default/"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "11",
                      message: "Coping files..",
                      command: "sudo cp -af /mnt/loop/0/azure/etc/dhcp/* /mnt/loop/0/etc/dhcp/"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "12",
                      message: "Coping files..",
                      command: "sudo cp -af /mnt/loop/0/azure/etc/fstab /mnt/loop/0/etc/fstab"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "13",
                      message: "Coping files..",
                      command: "sudo cp -af /mnt/loop/0/azure/etc/mtab /mnt/loop/0/etc/mtab"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "14",
                      message: "Coping files..",
                      command: "sudo rm -f /mnt/loop/0/opt/bitnami/var/init/pre-start/*"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "15",
                      message: "Coping files..",
                      command: "sudo rm -f /mnt/loop/0/opt/bitnami/var/init/post-start/*"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "16",
                      message: "Unmount first drive",
                      command: "sudo umount /mnt/loop/1"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "17",
                      message: "Unmount second drive",
                      command: "sudo umount /mnt/loop/0"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "18",
                      message: "Destroying first loop device",
                      command: "sudo losetup -d /dev/loop1"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "19",
                      message: "Destroying second loop device",
                      command: "sudo losetup -d /dev/loop0"
                );

                Migration.RunCommand(job,
                      stepId: "3 - Data migration",
                      substepId: "20",
                      message: "Flush data to disk",
                      command: "sudo sync"
                );

                Thread.Sleep(TimeSpan.FromSeconds(5));

                Migration.RunCommand(job,
                      stepId: "4 - Creating VHD from raw image",
                      substepId: "1",
                      message: "Adding Virtual box repository",
                      command: "sudo echo \"deb http://download.virtualbox.org/virtualbox/debian precise contrib\" | sudo tee -a /etc/apt/sources.list"
                );

                Migration.RunCommand(job,
                      stepId: "4 - Creating VHD from raw image",
                      substepId: "2",
                      message: "Adding Virtual box repository RSA key",
                      command: "sudo wget -q http://download.virtualbox.org/virtualbox/debian/oracle_vbox.asc -O- | sudo apt-key add -"
                );

                Migration.RunCommand(job,
                      stepId: "4 - Creating VHD from raw image",
                      substepId: "3",
                      message: "Updating apt-get packages",
                      command: "sudo apt-get update"
                );

                Migration.RunCommand(job,
                      stepId: "4 - Creating VHD from raw image",
                      substepId: "4",
                      message: "Installing Virtual box",
                      command: "sudo apt-get install -y virtualbox-4.2"
                );

                Migration.RunCommand(job,
                      stepId: "4 - Creating VHD from raw image",
                      substepId: "5",
                      message: "Creating VHD",
                      command: "sudo VBoxManage convertdd /mnt/sda.img /mnt/sda.vhd --format vhd"
                );

                Migration.RunCommand(job,
                      stepId: "5 - Uploading VHD to Azure",
                      substepId: "1",
                      message: "Installing Python",
                      command: "sudo apt-get install -y python-software-properties python g++ make"
                );

                Migration.RunCommand(job,
                      stepId: "5 - Uploading VHD to Azure",
                      substepId: "2",
                      message: "Adding NodeJS repository",
                      command: "sudo add-apt-repository -y ppa:chris-lea/node.js"
                );

                Migration.RunCommand(job,
                      stepId: "5 - Uploading VHD to Azure",
                      substepId: "3",
                      message: "Updating apt-get packages",
                      command: "sudo apt-get update"
                );

                Migration.RunCommand(job,
                      stepId: "5 - Uploading VHD to Azure",
                      substepId: "4",
                      message: "Installing NodeJS",
                      command: "sudo apt-get install -y nodejs"
                );

                Migration.RunCommand(job,
                      stepId: "5 - Uploading VHD to Azure",
                      substepId: "5",
                      message: "Installing Azure cli tools",
                      command: "sudo npm install -g azure-cli"
                );

                Migration.RunCommand(job,
                      stepId: "5 - Uploading VHD to Azure",
                      substepId: "6",
                      message: "Downlaoding publishsettings",
                      command: string.Format("sudo azure storage blob download -a {0} -k {1} deploymenthub publishing.publishsettings /mnt/account.publishsettings", job.azureStorageAccountName, job.azureStorageAccountKey)
                );

                Migration.RunCommand(job,
                    stepId: "5 - Uploading VHD to Azure",
                    substepId: "7",
                    message: "Importing publishsettings file",
                    command: "sudo azure account import /mnt/account.publishsettings"
                );

                var imagename = "dephub" + (new Random().Next(10000)).ToString();

                Migration.RunCommand(job,
                    stepId: "5 - Uploading VHD to Azure",
                    substepId: "8",
                    message: "Uploading image to Azure",
                    command: string.Format("sudo azure vm image create {0} /mnt/sda.vhd -o linux -u http://{1}.blob.core.windows.net/vm-images/{0}.vhd", imagename, job.azureStorageAccountName)
                );

                Migration.RunCommand(job,
                    stepId: "6 - Creating VM",
                    substepId: "1",
                    message: "Creating VM",
                    command: string.Format("sudo azure vm create {0} {0} {1} Metrics@hub1 --vm-size medium --location \"West US\"", imagename, job.awsUserName)
                );

                Migration.RunCommand(job,
                    stepId: "6 - Creating VM",
                    substepId: "2",
                    message: "Adding port 80",
                    command: string.Format("sudo azure vm endpoint create {0} 80 80", imagename)
                );

                Migration.RunCommand(job,
                      stepId: "6 - Creating VM",
                      substepId: "3",
                      message: "Adding port 443",
                      command: string.Format("sudo azure vm endpoint create {0} 443 443", imagename)
                  );

                Migration.RunCommand(job,
                      stepId: "6 - Creating VM",
                      substepId: "4",
                      message: "Adding port 22",
                      command: string.Format("sudo azure vm endpoint create {0} 22 22", imagename)
                  );

                Migration.Logger.Debug(string.Format("Migration end:{0} - {1}", job.userId, job.instanceId));
            }
        }

        public static void RunCommand(JobDefinition job, string stepId, string substepId, string message, string command)
        {
            try
            {
                var operation = RunWithtimeout(job, stepId, substepId, message, command);
                Task.WaitAny(new Task[] { operation }, TimeSpan.FromMinutes(40));
            }
            catch (Exception)
            {
            }
        }

        public static Task<string> RunWithtimeout(JobDefinition job, string stepId, string substepId, string message, string command)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>(); 

            Task<string>.Factory.StartNew(
                function: () =>
            {
                try
                {
                    var client = new SshClient(
                        host: job.awsHost,
                        username: job.awsUserName,
                        keyFiles: new PrivateKeyFile(new MemoryStream(Encoding.ASCII.GetBytes(job.awsSSHkeys))));

                    using (client)
                    {
                        client.Connect();

                        var start = DateTime.UtcNow;
                        Exception exception = null;
                        var status = "In progress";
                        try
                        {
                            Migration.Logger.Debug(string.Format("Step start:{0} - {1} - {2} - {3}", job.userId, job.instanceId, stepId, substepId));

                            Migration.SaveOperation(job, stepId, substepId, message, command, start, null, status);
                            var cmd = client.RunCommand(command);
                            if (cmd.ExitStatus != 0)
                            {
                                status = string.Format("Failed with error: {0}", cmd.Error ?? "<null>");
                                tcs.TrySetResult(cmd.Error ?? "<null>");
                                return cmd.Error ?? "<null>";
                            }
                            else
                            {
                                status = "Completed";
                                tcs.TrySetResult(cmd.Result);
                                return cmd.Result;
                            }
                        }
                        catch (Exception ex)
                        {
                            status = string.Format("Failed with exception: {0}", ex.Message);
                            exception = ex;
                        }
                        finally
                        {
                            Migration.Logger.Debug(string.Format("Step end:{0} - {1} - {2} - {3}", job.userId, job.instanceId, stepId, substepId));
                            Migration.SaveOperation(job, stepId, substepId, message, command, start, DateTime.UtcNow, status);
                        }
                    }

                    tcs.TrySetResult("Completed");
                }
                catch (Exception exception)
                {
                }

                return "<null>";
            });

            return tcs.Task;
        }

        private static void SaveOperation(JobDefinition job, string stepId, string substepId, string message, string command, DateTime start, DateTime? end, string status)
        {
            var creds = CloudStorageAccount.DevelopmentStorageAccount;
            if (!RoleEnvironment.IsEmulated)
            {
                creds = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=azurathonmh;AccountKey=U/wAoYULNT2DHK5WH595rowUhDfMedYp41HdhJD+HCS8iTHQe8xsSuY3mjAFgWZ7Gd/AgxwE3I9+I4sq6VX9qQ==");
            }

            var key = Guid.NewGuid().ToString().ToLowerInvariant();
            var tableClient = creds.CreateCloudTableClient();
            var table = tableClient.GetTableReference("operations");

            table.CreateIfNotExists();

            var op = new Operation
            {
                userId = job.userId,
                OperationId = job.operationId,
                instanceId = job.instanceId,
                StepId = stepId,
                SubstepId = substepId,
                Message = message,
                Command = command,
                Status = status,
                StartTime = start,
                EndTime = end
            };

            op.PartitionKey = job.userId;
            op.RowKey = string.Format("{0}-{1}-{2}-{3}", job.userId, job.operationId, stepId, substepId);

            var tableOperation = TableOperation.InsertOrReplace(op);
            var res = table.Execute(tableOperation);
        }
    }
}
