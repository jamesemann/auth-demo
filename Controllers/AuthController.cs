using Microsoft.AspNetCore.Mvc;

namespace AzureAdAuth.Controllers
{
    public class AuthController : Controller
    {
        public AzureAdAuthorizationCodeFlow Oauth { get; }

        public AuthController(AzureAdAuthorizationCodeFlow oauth)
        {
            Oauth = oauth;
        }

        [HttpGet("userconsentredirect")]
        public async ContentResult GetUserConsent(string code, string state)
        {
            await Oauth.UserConsented(code,state);
            return Content("Thanks for providing user consent, you may now close this window.");
        }
    }
}
