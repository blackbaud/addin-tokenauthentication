using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Blackbaud.Addin.TokenAuthentication
{
    /// <summary>
    /// Handles obtaining and caching the signing certificates used to validate user identity tokens that are issued to addins created by SKY API applications.
    /// </summary>
    internal class SigningKeysCache
    {
        private static readonly SigningKeysCache _singleton = new SigningKeysCache();
        private static Uri _openIdConfigUrl = new Uri("https://oauth2.sky.blackbaud.com/.well-known/openid-configuration");
        
        #region Constructor

        /// <summary>
        /// An internal constructor for testing
        /// </summary>
        internal SigningKeysCache()
        {
        }

        /// <summary>
        /// Returns the singleton instance of the SigningKeysCache class
        /// </summary>
        internal static SigningKeysCache Instance
        {
            get
            {
                return _singleton;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The issuer value that will be signing the tokens.
        /// </summary>
        public virtual string Issuer { get; private set; }

        /// <summary>
        /// The signing keys returned from the OpenID Connect configuration endpoint.
        /// </summary>
        public virtual Dictionary<string, X509Certificate2> Certificates { get; private set; } = new Dictionary<string, X509Certificate2>();

        /// <summary>
        /// Represents the last time a refresh was attempted.
        /// </summary>
        public virtual DateTimeOffset? LastAttemptedRefresh { get; private set; }

        #endregion Properties

        /// <summary>
        /// Returns whether a refresh is needed based on it being too long since the last refresh attempt.
        /// </summary>
        public virtual bool RefreshNeeded()
        {
            return DateTimeOffset.UtcNow.Subtract(LastAttemptedRefresh.GetValueOrDefault()).TotalMinutes > 30;
        }

        /// <summary>
        /// Refresh the signing keys cache
        /// </summary>
        public virtual async Task Refresh()
        {
            this.LastAttemptedRefresh = DateTimeOffset.Now;

            using (var client = new HttpClient())
            {
                // Process OpenIdConfig
                var config = await client.GetStringAsync(_openIdConfigUrl);
                var c = JObject.Parse(config);

                var issuer = c.Value<string>("issuer");
                var jwks_uri = c.Value<string>("jwks_uri");

                // Process JwksUri
                var jwksResponse = await client.GetStringAsync(jwks_uri);
                var jwks = JObject.Parse(jwksResponse);
                var keys = jwks.Value<JArray>("keys");

                // add certificates to the dictionary
                var certDictionary = new Dictionary<string, X509Certificate2>();
                foreach (var key in keys)
                {
                    // currently, user identity tokens issued from the SKY API OAuth 2.0 service use an "x5t" claim in the header - in the future, 
                    // we may decide to transition to using a "kid" claim in the header.  For now, we'll support both x5t and kid versions to future-proof this logic
                    var kid = key.Value<string>("kid");
                    var x5t = key.Value<string>("x5t");
                    var x5c = key.Value<JArray>("x5c")[0].Value<string>();

                    // add the x5t certificate if applicable
                    if (!string.IsNullOrEmpty(x5t) && !certDictionary.ContainsKey(x5t))
                    {
                        certDictionary.Add(x5t, new X509Certificate2(Convert.FromBase64String(x5c)));
                    }

                    // add the kid certificate if applicable
                    if (!string.IsNullOrEmpty(kid) && !certDictionary.ContainsKey(kid))
                    {
                        certDictionary.Add(kid, new X509Certificate2(Convert.FromBase64String(x5c)));
                    }
                }

                // Update shared properties once we were successfully able to parse the metadata document.
                this.Issuer = issuer;
                this.Certificates = certDictionary;
            }
        }

    }
}