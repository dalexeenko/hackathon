using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using WorkerRole1;

namespace SshTestApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var ssh = @"";

            var key = Guid.NewGuid().ToString().ToLowerInvariant();

            var job = new JobDefinition
                {
                    userId = "user",
                    operationId = key,
                    instanceId = "i-0356e734",
                    PartitionKey = key,
                    RowKey = key,
                    awsHost = "us-west-2.compute.amazonaws.com",
                    awsUserName = "bitnami",
                    awsSSHkeys = ssh,
                    azureStorageAccountName = "ubuntuaws",
                    azureStorageAccountKey =
                        ""
                };

            RunAndWaitCommand(
                job,
                "fo",
                "sub",
                "message",
                "sleep 20",
                TimeSpan.FromMinutes(20));
        }

        public static void RunAndWaitCommand(JobDefinition job, string stepId, string substepId, string message, string command, TimeSpan timeout)
        {
            var sessionId = Guid.NewGuid().ToString().ToLowerInvariant();
            try
            {
                var updatedCommand = string.Format(
                    "sh -c ( (nohup {0};nohup mkdir -p ~/deploymenthub;nohup touch ~/deploymenthub/{1} > /dev/null 2>&1) &)",
                    command,
                    sessionId);
                var checkStatusCommand = string.Format(
                    "[ -f ~/deploymenthub/{0} ] && echo \"File exists\" || echo \"File does not exists\"",
                    sessionId);

                bool isCompleted = false;
                Stopwatch stopwatch = new Stopwatch();

                // Begin timing
                stopwatch.Start();

                do
                {
                    var result = RunCommandWithTimeout(
                        awsHost: job.awsHost,
                        awsUser: job.awsUserName,
                        awsSshKeys: job.awsSSHkeys,
                        command: updatedCommand);

                    result.Wait(TimeSpan.FromMinutes(5));

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    var checkStatusRemote = RunCommandWithTimeout(
                        awsHost: job.awsHost,
                        awsUser: job.awsUserName,
                        awsSshKeys: job.awsSSHkeys,
                        command: checkStatusCommand);

                    checkStatusRemote.Wait(TimeSpan.FromMinutes(5));

                    if (!checkStatusRemote.IsFaulted &&
                        !checkStatusRemote.IsCanceled)
                    {
                        var checkResult = checkStatusRemote.Result;
                        isCompleted = checkResult.Contains("File exists");
                    }

                } while (!isCompleted && stopwatch.Elapsed < timeout);
            }
            catch (Exception)
            {
                //[ -f /etc/passwd ] && echo "File exists" || echo "File does not exists"
            }
        }

        public static Task<string> RunCommandWithTimeout(string awsHost, string awsUser, string awsSshKeys, string command)
        {
            try
            {
                return RunCommand(
                        awsHost: awsHost,
                        awsUser: awsUser,
                        awsSshKeys: awsSshKeys,
                        command: command);
            }
            catch (Exception)
            {
                return Task.FromResult(string.Empty);
            }
        }

        public static Task<string> RunCommand(string awsHost, string awsUser, string awsSshKeys, string command)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            Task<string>.Factory.StartNew(
                function: () =>
                {
                    try
                    {
                        var client = new SshClient(
                            host: awsHost,
                            username: awsUser,
                            keyFiles: new PrivateKeyFile(new MemoryStream(Encoding.ASCII.GetBytes(awsSshKeys))));

                        using (client)
                        {
                            client.Connect();

                            var start = DateTime.UtcNow;
                            Exception exception = null;
                            var status = "In progress";
                            try
                            {
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
    }
}
