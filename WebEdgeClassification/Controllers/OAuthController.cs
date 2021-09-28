using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Autodesk.Forge;
using WebEdgeClassification.Models;

namespace WebEdgeClassification.Controllers
{
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private static dynamic InternalToken { get; set; }
        private static dynamic PublicToken { get; set; }
        //depo for Forge_Client_ID and secret
        internal static string FORGE_CLIENT_ID { get; set; }
        internal static string FORGE_CLIENT_SECRET { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userModel"></param>
        [HttpPost]
        [Route("api/forge/oauth/cred")]
        public void SetCredentials([FromBody] UserModel userModel)
        {
            FORGE_CLIENT_ID = userModel.ForgeClient;
            FORGE_CLIENT_SECRET = userModel.ForgeSecret;
        }

        /// <summary>
        /// Get access token with public (viewables:read) scope
        /// </summary>
        [HttpGet]
        [Route("api/forge/oauth/token")]
        public async Task<dynamic> GetPublicAsync()
        {
            if (PublicToken == null || PublicToken.ExpiresAt < DateTime.UtcNow)
            {
                PublicToken = await Get2LeggedTokenAsync(new Scope[] { Scope.ViewablesRead, Scope.DataRead });
                PublicToken.ExpiresAt = DateTime.UtcNow.AddSeconds(PublicToken.expires_in);
            }
            return PublicToken;
        }

        /// <summary>
        /// Get access token with internal (write) scope
        /// </summary>
        public static async Task<dynamic> GetInternalAsync()
        {
            if (InternalToken == null || InternalToken.ExpiresAt < DateTime.UtcNow)
            {
                Scope[] internalKeyScope = new Scope[]
                {
                    Scope.BucketCreate,
                    Scope.BucketRead,
                    Scope.BucketDelete,
                    Scope.DataRead,
                    Scope.DataWrite,
                    Scope.DataCreate,
                    Scope.CodeAll
                };
                InternalToken = await Get2LeggedTokenAsync(internalKeyScope);
                InternalToken.ExpiresAt = DateTime.UtcNow.AddSeconds(InternalToken.expires_in);
            }

            return InternalToken;
        }

        /// <summary>
        /// Get the access token from Autodesk
        /// </summary>
        private static async Task<dynamic> Get2LeggedTokenAsync(Scope[] scopes)
        {
            TwoLeggedApi oauth = new TwoLeggedApi();
            string grantType = "client_credentials";
            string clientId = GetAppSetting("FORGE_CLIENT_ID");
            string clientSecret = GetAppSetting("FORGE_CLIENT_SECRET");
            dynamic bearer = await oauth.AuthenticateAsync(clientId, clientSecret, grantType, scopes);
            return bearer;
        }

        /// <summary>
        /// Reads appsettings from web.config
        /// </summary>
        public static string GetAppSetting(string settingKey)
        {
            return Environment.GetEnvironmentVariable(settingKey).Trim();
        }
    }
}