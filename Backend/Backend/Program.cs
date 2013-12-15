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
            string subscriptionId = "2b5ffd6b-8a96-4976-abd3-20b7957af61d";
            string certString = "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";
            byte[] data = Convert.FromBase64String(certString);

            try
            {
                // Obtain the certificate with the specified thumbprint
                X509Certificate2 certificate = new X509Certificate2(data);
                var storageAccounts = ListStorageAccountsExample(subscriptionId, certificate, msVersion).Result;

                foreach (var account in storageAccounts)
                {
                    Console.WriteLine(
                        string.Format("Name: {0}; Key: {1}; Uri: {2}", account.Name, account.Key, account.URI));
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
