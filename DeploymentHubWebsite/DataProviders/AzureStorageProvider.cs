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
    public class AzureStorageProvider
    {
        public async Task<IEnumerable<AzureStorageAccountInfo>> ListStorageAccounts(
              string subscriptionId,
              X509Certificate2 certificate,
              bool getKeys = false,
              string version = "2012-08-01")
        {
            System.Diagnostics.Trace.TraceError("Entered ListStorageAccounts");

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
                System.Diagnostics.Trace.TraceError("ListStorageAccounts: Sending the request");

                response = (HttpWebResponse) (await request.GetResponseAsync().ConfigureAwait(false));

                System.Diagnostics.Trace.TraceError("ListStorageAccounts: Request succeeded");
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError("Exception has been raised");

                if (exception is WebException)
                {
                    System.Diagnostics.Trace.TraceError("WebException:" + exception.ToString());

                    // GetResponse throws a WebException for 400 and 500 status codes
                    var ex = exception as WebException;
                    response = (HttpWebResponse)ex.Response;
                }
                else
                {
                    System.Diagnostics.Trace.TraceError("Exception:" + exception.ToString());

                    throw;
                }
            }

            System.Diagnostics.Trace.TraceError("Response code:" + response.StatusCode);

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

            if (getKeys)
            {
                foreach (var account in accounts)
                {
                    account.Key = await GetStorageAccountKey(subscriptionId, certificate, version, account.Name);
                }
            }

            return accounts;
        }

        public async Task<string> GetStorageAccountKey(
            string subscriptionId,
            X509Certificate2 certificate,
            string serviceName,
            string version = "2012-08-01")
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