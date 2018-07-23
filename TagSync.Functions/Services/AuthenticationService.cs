using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace TagSync.Services
{
    public static class AuthenticationService
    {
        public static string GetAccessTokenAsync()
        {
            string token = null;

            if (Environment.GetEnvironmentVariable("MSI_ENDPOINT") == null)
            {
                // USE SERVICE PRINCIPAL
                string appId = Environment.GetEnvironmentVariable("appId");
                string appSecret = Environment.GetEnvironmentVariable("appSecret");
                string tenantId = Environment.GetEnvironmentVariable("tenantId");

                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(tenantId))
                {
                    throw new Exception("Missing value for service principal. Check App Settings.");
                }

                try
                {
                    token = GetTokenServicePrincipal(appId, appSecret, tenantId);
                }
                catch (Exception ex) { throw ex; }

            }
            else
            {
                // USE MSI
                try
                {
                    return GetTokenMSI();
                }
                catch (Exception ex) { throw ex; }
            }

            return token;
        }

        static string GetTokenServicePrincipal(string appId, string appSecret, string tenantId)
        {
            var authContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", tenantId));
            var credential = new ClientCredential(appId, appSecret);
            AuthenticationResult token = authContext.AcquireTokenAsync("https://management.azure.com/", credential).Result;
            return token.AccessToken;
        }

        static string GetTokenMSI()
        {
            string msiEndpoint = Environment.GetEnvironmentVariable("MSI_ENDPOINT");
            string msiSecret = Environment.GetEnvironmentVariable("MSI_SECRET");
            string resource = "https://management.core.windows.net/";
            string apiVersion = "2017-09-01";
            string requestUri = string.Concat(msiEndpoint, "?api-version=", apiVersion, "&resource=", resource);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
            request.Headers["Secret"] = msiSecret;
            request.Method = "GET";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader streamResponse = new StreamReader(response.GetResponseStream()); 
                string stringResponse = streamResponse.ReadToEnd();
                Dictionary<string, string> list = JsonConvert.DeserializeObject<Dictionary<string,string>>(stringResponse);
                return list["access_token"];
            }
            catch (Exception e)
            {
                string errorText = String.Format("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : "Acquire token failed");
                throw new Exception(errorText);
            }
        }

    }
}