using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureAdAuth.Controllers
{
    public class AzureAdAuthorizationCodeFlow
    {
        string azureAdTenant = "jemann.onmicrosoft.com";
        string clientId = "7acef7ed-25a7-4a41-9156-be2b701c789c";
        string adminConsentRedirectUri = "https://localhost:44355/adminconsentredirect";
        string userConsentRedirectUri = "https://localhost:44355/userconsentredirect";
        string permissionsRequested = "User.Read.All";
        string clientSecret = "";

        public AzureAdAuthorizationCodeFlow()
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine("------------------------------------");
            Console.WriteLine("(LOG IN WITH AN TENANT ADMIN) Provide admin consent at:");
            //Console.WriteLine($"https://login.microsoftonline.com/{azureAdTenant}/adminconsent?client_id={clientId}&state=12345&redirect_uri={adminConsentRedirectUri}");
            Console.WriteLine($"https://login.microsoftonline.com/{azureAdTenant}/oauth2/v2.0/authorize?client_id={clientId}&scope={permissionsRequested}&response_type=code&response_mode=query&redirect_uri={userConsentRedirectUri}&state=12345");
            Console.WriteLine("------------------------------------");
            Console.WriteLine("------------------------------------");
        }

        //public void AdminConsented(string state)
        //{
        //    Console.WriteLine("------------------------------------");
        //    Console.WriteLine("------------------------------------");
        //    Console.WriteLine("*** ADMIN CONSENTED ***");
        //    Console.WriteLine("(LOG IN WITH ANY USER IN THE TENANT) Provide user consent at:");
        //    Console.WriteLine($"https://login.microsoftonline.com/{azureAdTenant}/oauth2/v2.0/authorize?client_id={clientId}&scope={permissionsRequested}&response_type=code&response_mode=query&redirect_uri={userConsentRedirectUri}&state={state}");
        //    Console.WriteLine("------------------------------------");
        //    Console.WriteLine("------------------------------------");
        //}

        public async Task UserConsented(string code, string state)
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine("------------------------------------");
            Console.WriteLine("*** USER CONSENTED ***");

            // execute a query that requires admin consent (my manager)
            var httpClient = new HttpClient();
            var accessToken = await httpClient.GetAzureAdToken(azureAdTenant, code, clientId, userConsentRedirectUri, clientSecret, permissionsRequested);

            (var name, var phone, var email) = await httpClient.GetMicrosoftGraphMyManager(accessToken);

            Console.WriteLine(code);
            Console.WriteLine("------------------------------------");
            Console.WriteLine("------------------------------------");
        }
    }

    public static class AzureAdExtensions
    {
        public static async Task<string> GetAzureAdToken(this HttpClient client, string tenant, string code, string clientId, string redirectUri, string clientSecret, string permissionsRequested)
        {
            var formFields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", "User.Read.All"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            };
            var aadAccessTokenRequest = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token") { Content = new FormUrlEncodedContent(formFields) };
            var aadAccessTokenResponse = await client.SendAsync(aadAccessTokenRequest);
            dynamic result = JsonConvert.DeserializeObject<ExpandoObject>(await aadAccessTokenResponse.Content.ReadAsStringAsync());
            return result.access_token;
        }
    }

    public static class MicrosoftGraphExtensions
    {
        public static async Task<(string name, string phone, string email)> GetMicrosoftGraphMyManager(this HttpClient client, string accessToken)
        {
            dynamic result = await ExecuteGraphQuery(client, accessToken, "https://graph.microsoft.com/v1.0/me/manager");
            return (result.displayName, result.mobilePhone, result.mail);
        }
        private static async Task<dynamic> ExecuteGraphQuery(HttpClient client, string accessToken, string graphUrl)
        {
            var graphreq = new HttpRequestMessage(HttpMethod.Get, graphUrl);
            graphreq.Headers.Add("Authorization", $"Bearer {accessToken}");
            var graphres = await client.SendAsync(graphreq);
            dynamic result = JsonConvert.DeserializeObject<ExpandoObject>(await graphres.Content.ReadAsStringAsync());
            return result;
        }
    }
}
