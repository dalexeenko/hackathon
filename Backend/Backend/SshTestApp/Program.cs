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

            var key = Guid.NewGuid().ToString().ToLowerInvariant();

            var job = new JobDefinition
                {
                    userId = "user",
                    operationId = key,
                    instanceId = "i-0356e734",
                    PartitionKey = key,
                    RowKey = key,
                    awsHost = "ec2-54-214-91-252.us-west-2.compute.amazonaws.com",
                    awsUserName = "bitnami",
                    awsSSHkeys = ssh,
                    azureStorageAccountName = "ubuntuaws",
                    azureStorageAccountKey =
                        "HuhJWqcuXyrMzCB4VYsZs2ZfpvpmDVqVwh7uNAxJ4qnmmjbxfnztv5L81fxM6PN/2GmqW8hYvIXxt3Wqqp9qFA=="
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
