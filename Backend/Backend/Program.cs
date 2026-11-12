using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace GetSubscriptionInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            string msVersion = "2012-03-01";
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string certificatePath = Environment.GetEnvironmentVariable("AZURE_MANAGEMENT_CERTIFICATE_PATH");
            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(certificatePath))
            {
                throw new InvalidOperationException("Set AZURE_SUBSCRIPTION_ID and AZURE_MANAGEMENT_CERTIFICATE_PATH before running this sample.");
            }

            try
            {
                // Load a certificate provisioned outside the repository.
                X509Certificate2 certificate = new X509Certificate2(
                    certificatePath, Environment.GetEnvironmentVariable("AZURE_MANAGEMENT_CERTIFICATE_PASSWORD"));
                var storageAccounts = ListStorageAccountsExample(subscriptionId, certificate, msVersion).Result;

                foreach (var account in storageAccounts)
                {
                    Console.WriteLine(
                        string.Format("Name: {0}; Uri: {1}", account.Name, account.URI));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in Main:");
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();
        }

        public class AzureStorageAccountInfo
        {
            public string URI { get; set; }
            public string Name { get; set; }
            public string Key { get; set; }
        }

        public static async Task<IEnumerable<AzureStorageAccountInfo>> ListStorageAccountsExample(
          string subscriptionId,
          X509Certificate2 certificate,
          string version)
        {
            string uriFormat = "https://management.core.windows.net/{0}/services/storageservices";
            Uri uri = new Uri(String.Format(uriFormat, subscriptionId));

            var accounts = new List<AzureStorageAccountInfo>();

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
                response = (HttpWebResponse)(await request.GetResponseAsync());
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
                XElement storageServices = responseBody.Element(wa + "StorageServices");
                foreach (XElement storageService in storageServices.Elements(wa + "StorageService"))
                {
                    string url = storageService.Element(wa + "Url").Value;
                    string serviceName = storageService.Element(wa + "ServiceName").Value;
                    accounts.Add(
                        new AzureStorageAccountInfo { URI = url, Name = serviceName, Key = string.Empty });
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

            foreach (var account in accounts)
            {
                account.Key = await GetStorageAccountKey(subscriptionId, certificate, version, account.Name);
            }

            return accounts;
        }

        private static async Task<string> GetStorageAccountKey(
            string subscriptionId,
            X509Certificate2 certificate,
            string version,
            string serviceName)
        {
            string uriFormat = "https://management.core.windows.net/{0}/services/storageservices/{1}/keys";
            Uri uri = new Uri(String.Format(uriFormat, subscriptionId, serviceName));

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
                response = (HttpWebResponse)(await request.GetResponseAsync());
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
                XElement storageService = responseBody.Element(wa + "StorageService");
                var primaryKey = storageService.Element(wa + "StorageServiceKeys").Element(wa + "Primary");

                return primaryKey.Value.ToString();
            }

            throw new InvalidOperationException(
                string.Format("Call to Get Storage Account Keys returned an error: Status Code: {0} ({1}):{2}{3}",
                (int)statusCode, statusCode, Environment.NewLine,
                responseBody.ToString(SaveOptions.OmitDuplicateNamespaces)));
        }
    }
}
