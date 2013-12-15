using AWSTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DataProviders
{
    public class AzureVmInfo
    {
        public string Name { get; set; }
        public string OS { get; set; }
        public string Size { get; set; }
        public string Region { get; set; }
        public string ServiceName { get; set; }
        public decimal HourlyCost { get; set; }
    }

    public class AzureCloudService
    {
        public string Name { get; set; }
        public string Region { get; set; }
    }

    public class AzureComputeProvider
    {
        public async Task<IEnumerable<AzureVmInfo>> ListVirtualMachines(
            string subscriptionId,
            X509Certificate2 certificate,
            string version = "2012-08-01")
        {
            var machines = new List<AzureVmInfo>();

            var cloudServices = await ListCloudServices(subscriptionId, certificate, version).ConfigureAwait(false);

            foreach (var service in cloudServices)
            {
                var vms = await ListVms(subscriptionId, service.Name, certificate, version).ConfigureAwait(false);

                foreach (var vm in vms)
                {
                    vm.Region = service.Region;
                    vm.ServiceName = service.Name;
                    vm.HourlyCost = CostDataProvider.AzureVmToCost(vm.Size);
                    machines.Add(vm);
                }
            }

            return machines;
        }

        private async Task<IEnumerable<AzureCloudService>> ListCloudServices(
            string subscriptionId,
            X509Certificate2 certificate,
            string version)
        {
            string uriFormat = "https://management.core.windows.net/{0}/services/hostedservices";

            Uri uri = new Uri(String.Format(uriFormat, subscriptionId));

            var services = new List<AzureCloudService>();

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.Method = "GET";
            request.Headers.Add("x-ms-version", version);
            request.ClientCertificates.Add(certificate);
            request.ContentType = "application/xml";

            XDocument responseBody = null;
            HttpStatusCode statusCode;
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)(await request.GetResponseAsync().ConfigureAwait(false));
            }
            catch (WebException ex)
            {
                // GetResponse throws a WebException for 400 and 500 status codes
                response = (HttpWebResponse)ex.Response;
            }

            statusCode = response.StatusCode;
            if (response.ContentLength > 0)
            {
                using (XmlReader reader = XmlReader.Create(response.GetResponseStream()))
                {
                    responseBody = XDocument.Load(reader);
                }
            }
            response.Close();
            if (statusCode.Equals(HttpStatusCode.OK))
            {
                XNamespace wa = "http://schemas.microsoft.com/windowsazure";
                XElement storageServices = responseBody.Element(wa + "HostedServices");
                foreach (XElement hostedService in storageServices.Elements(wa + "HostedService"))
                {
                    var serviceName = hostedService.Element(wa + "ServiceName");
                    var properties = hostedService.Element(wa + "HostedServiceProperties");
                    var region = properties.Element(wa + "Location");

                    services.Add(new AzureCloudService
                    {
                        Name = serviceName.Value.ToString(),
                        Region = region == null ? string.Empty : region.Value.ToString(),
                    });
                }
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("Call to List Cloud Services returned an error: Status Code: {0} ({1}):{2}{3}",
                      (int)statusCode,
                      statusCode,
                      Environment.NewLine,
                      responseBody.ToString(SaveOptions.OmitDuplicateNamespaces)));
            }

            return services;
        }

        private async Task<IEnumerable<AzureVmInfo>> ListVms(
            string subscriptionId,
            string cloudServiceName,
            X509Certificate2 certificate,
            string version)
        {
            string uriFormat = "https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/production";

            Uri uri = new Uri(String.Format(uriFormat, subscriptionId, cloudServiceName));

            var vms = new List<AzureVmInfo>();

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.Method = "GET";
            request.Headers.Add("x-ms-version", version);
            request.ClientCertificates.Add(certificate);
            request.ContentType = "application/xml";

            XDocument responseBody = null;
            HttpStatusCode statusCode;
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)(await request.GetResponseAsync().ConfigureAwait(false));
            }
            catch (WebException ex)
            {
                // GetResponse throws a WebException for 400 and 500 status codes
                response = (HttpWebResponse)ex.Response;

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new AzureVmInfo[0];
                }
            }
            statusCode = response.StatusCode;
            if (response.ContentLength > 0)
            {
                using (XmlReader reader = XmlReader.Create(response.GetResponseStream()))
                {
                    responseBody = XDocument.Load(reader);
                }
            }
            response.Close();
            if (statusCode.Equals(HttpStatusCode.OK))
            {
                XNamespace wa = "http://schemas.microsoft.com/windowsazure";
                XElement storageServices = responseBody.Element(wa + "Deployment");
                XElement roleList = storageServices.Element(wa + "RoleList");

                foreach (XElement role in roleList.Elements(wa + "Role"))
                {
                    //var role = item.Element(wa + "Role");
                    var roleTypeElement = role.Element(wa + "RoleType");
                    if (roleTypeElement != null && roleTypeElement.Value.Equals("PersistentVMRole", StringComparison.OrdinalIgnoreCase))
                    {
                        var roleName = role.Element(wa + "RoleName");
                        var roleSize = role.Element(wa + "RoleSize");

                        var diskOs = role.Element(wa + "OSVirtualHardDisk").Element(wa + "OS");

                        vms.Add(
                            new AzureVmInfo
                            {
                                Name = roleName.Value.ToString(),
                                OS = diskOs.Value.ToString(),
                                Size = roleSize.Value.ToString(),
                            });
                    }
                }
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("Call to List Storage Accounts returned an error: Status Code: {0} ({1}):{2}{3}",
                      (int)statusCode,
                      statusCode,
                      Environment.NewLine,
                      responseBody.ToString(SaveOptions.OmitDuplicateNamespaces)));
            }

            return vms;
        }
    }
}
