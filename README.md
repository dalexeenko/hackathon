DeploymentHub
=============

In less than a minute, see how much your Amazon EC2 instances would cost in Windows Azure. Then, with just three clicks, migrate your running virtual machines to Windows Azure.

## Credentials and local development

This is a legacy .NET Framework 4.5 / Azure Cloud Services project. The checked-in
configuration contains no real credentials. Local storage settings use
`UseDevelopmentStorage=true` and the website database uses LocalDB with Windows
integrated authentication. Run the matching Azure storage/compute emulators and
LocalDB when developing with the legacy Windows toolchain.

Automatic demo-job insertion is disabled by default (`SeedDemoWork=false`).

## Deployment configuration

Create deployment-specific configuration outside source control, for example in
the ignored `.secrets/` directory, and protect it with appropriate file permissions
or your deployment system's secret store. Do not put real credentials back into
the tracked templates.

The roles use the existing `CloudConfigurationManager` configuration path:

| Component | Setting | Purpose |
| --- | --- | --- |
| Worker role | `InputStorageConnectionString` | Storage account containing the `jobs` table and queue |
| Worker role | `OperationStorageConnectionString` | Storage account containing operation history |
| Website role | `StorageConnectionString` | Application storage used by the website |
| Both roles | `Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString` | Diagnostics storage |

Supply these values in the deployment copy of the appropriate
`ServiceConfiguration.Cloud.cscfg`. The checked-in cloud values are intentionally
empty. Both local service configurations contain emulator values. The worker's
`app.config` provides the same local defaults outside the cloud configuration;
the website's `Web.config` provides its local storage default.

For the website, also supply the production SQL `DefaultConnection` connection
string in the deployed `Web.config` through your private deployment configuration
or transform. It is not automatically loaded from an environment variable by this
legacy application. The checked-in value connects only to local LocalDB.

The website no longer includes fixed ASP.NET machine keys. Local development uses
ASP.NET's generated defaults. Before deploying multiple web-role instances,
provision newly generated, shared validation/decryption keys through protected
deployment configuration. Replacing previously exposed keys invalidates existing
authentication cookies and other protected application data as expected.

Legacy Remote Desktop credentials, certificate references, and imports have been
removed, and the publish profiles disable Remote Desktop. Configure any future
remote access separately with fresh credentials.

### Optional demo job

Keep `SeedDemoWork=false` unless you explicitly intend to enqueue the sample
migration job. To enable it, first supply `DemoSshPrivateKeyPath` (a privately
provisioned key file), `DemoAzureStorageAccountName`, and
`DemoAzureStorageAccountKey` in worker configuration. Review the sample instance,
host, and username in the worker code before enabling it. Missing required
settings produce an error without including their values.

### Standalone samples

The two Azure subscription sample programs in `Backend/Backend/` read
`AZURE_SUBSCRIPTION_ID` and `AZURE_MANAGEMENT_CERTIFICATE_PATH` from the process
environment. The certificate must be provisioned outside the repository; set
`AZURE_MANAGEMENT_CERTIFICATE_PASSWORD` if its file is password-protected.

`stash/AWSIntegration/TestApp` reads `AWS_ACCESS_KEY_ID` and
`AWS_SECRET_ACCESS_KEY` from the process environment and fails when either is
missing. No sample credential is provided.

## Previously committed credentials

Removing credentials from the current files does not remove them from Git history
or revoke them. Rotate or revoke previously committed Azure storage keys, Azure
management certificates, AWS credentials, SSH private keys, SQL credentials,
ASP.NET machine keys, and Remote Desktop credentials with their respective
providers. Review access associated with those credentials. History cleanup, if
needed, must be coordinated separately; this change does not rewrite history.
