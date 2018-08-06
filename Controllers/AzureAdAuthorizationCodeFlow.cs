using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureAdAuth.Controllers
{
    public class AzureAdAuthorizationCodeFlow
    {
        string azureAdTenant = "jemann.onmicrosoft.com";
        string clientId = "7acef7ed-25a7-4a41-9156-be2b701c789c";
        string userConsentRedirectUri = "https://localhost:44355/userconsentredirect";
        string permissionsRequested = "Calendars.ReadWrite.Shared";
        string clientSecret = "";

        public AzureAdAuthorizationCodeFlow()
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine("------------------------------------");
            Console.WriteLine(" Provide consent at:");
            Console.WriteLine($"https://login.microsoftonline.com/{azureAdTenant}/oauth2/v2.0/authorize?client_id={clientId}&scope={permissionsRequested}&response_type=code&response_mode=query&redirect_uri={userConsentRedirectUri}&state=12345");
            Console.WriteLine("------------------------------------");
            Console.WriteLine("------------------------------------");
        }

        public async Task UserConsented(string code, string state)
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine("------------------------------------");
            Console.WriteLine("*** USER CONSENTED ***");

            var httpClient = new HttpClient();
            // get an access token from azure ad
            var accessToken = await httpClient.GetAzureAdToken(azureAdTenant, code, clientId, userConsentRedirectUri, clientSecret, permissionsRequested);

            // find 1 hour meeting slots in the next 24 hours in room 
            var meetingTimes = await httpClient.GetMicrosoftGraphFindMeeting(accessToken, DateTime.Now, DateTime.Now.AddDays(1), "PT1H", "boardroom@jemann.onmicrosoft.com");

            Console.WriteLine(code);
            Console.WriteLine("------------------------------------");
            Console.WriteLine("------------------------------------");
        }
    }

    public static class AzureAdExtensions
    {
        // This method retrieves an azure ad access token after the user has consented and a authorization code has been provided
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
        // Find meeting times in graph
        public static async Task<MeetingTimeSuggestionsResult> GetMicrosoftGraphFindMeeting(this HttpClient client, string accessToken, DateTime from, DateTime to, string meetingDuration, string meetingRoomEmailAddress)
        {
            var graphClient = new GraphServiceClient(new PreAuthorizedBearerTokenAuthenticationProvider(accessToken));
            return await graphClient.Me.FindMeetingTimes(
                LocationConstraint: new LocationConstraint()
                {
                    IsRequired = true,
                    Locations = new LocationConstraintItem[] 
                    {
                        new LocationConstraintItem() { LocationEmailAddress = meetingRoomEmailAddress }
                    }
                }, 
                TimeConstraint: new TimeConstraint()
                {
                    ActivityDomain = ActivityDomain.Unrestricted,
                    Timeslots = new TimeSlot[] {
                        new TimeSlot()
                        {
                            Start = new DateTimeTimeZone() { DateTime = from.ToString("yyyy-MM-ddThh:mm:ss"), TimeZone = "UTC" },
                            End = new DateTimeTimeZone() { DateTime = to.ToString("yyyy-MM-ddThh:mm:ss"), TimeZone = "UTC" }
                        }
                    }
                },
                MeetingDuration: new Duration(meetingDuration)).Request().PostAsync();
        }

        // Authenticate using our access token
        class PreAuthorizedBearerTokenAuthenticationProvider : IAuthenticationProvider
        {
            public PreAuthorizedBearerTokenAuthenticationProvider(string accessToken)
            {
                AccessToken = accessToken;
            }

            public string AccessToken { get; }

            public async Task AuthenticateRequestAsync(HttpRequestMessage request)
            {
                request.Headers.Add("Authorization", $"Bearer {AccessToken}");
            }
        }
    }
}
