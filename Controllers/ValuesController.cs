using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AzureAdAuth.Controllers
{
    public class ValuesController : Controller
    {
        public AzureAdAuthorizationCodeFlow Oauth { get; }

        public ValuesController(AzureAdAuthorizationCodeFlow oauth)
        {
            Oauth = oauth;
        }

        //[HttpGet("adminconsentredirect")]
        //public ContentResult GetAdminConsent(string state)
        //{
        //    Oauth.AdminConsented(state);
        //    return Content("Thanks for providing admin consent, you may now close this window.");
        //}

        [HttpGet("userconsentredirect")]
        public ContentResult GetUserConsent(string code, string state)
        {
            Oauth.UserConsented(code,state);
            return Content("Thanks for providing user consent, you may now close this window.");
        }
    }
}
