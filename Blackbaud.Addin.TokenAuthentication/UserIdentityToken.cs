using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Blackbaud.Addin.TokenAuthentication.Exceptions;
using Microsoft.IdentityModel.Tokens;

namespace Blackbaud.Addin.TokenAuthentication
{
    /// <summary>
    /// Represents a user identity token issued to an addin that was created by a SKY API application
    /// </summary>
    public class UserIdentityToken
    {
        private SigningKeysCache _signingKeysCache;
        private JwtSecurityTokenHandler _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        #region Constructor

        /// <summary>
        /// Creates a new instance of a <see cref="UserIdentityToken" /> class using the specified signing keys cache
        /// </summary>
        internal UserIdentityToken(SigningKeysCache signingKeysCache)
        {
            _signingKeysCache = signingKeysCache;
        }

        /// <summary>
        /// Parse and validate the specified user identity token string
        /// </summary>
        /// <param name="token">The user identity token string</param>
        /// <param name="applicationId">The expected identifier of the application to which the token was issued</param>
        /// <returns>A new instance of a UserIdentityToken</returns>
        /// <exception cref="ArgumentNullException">
        /// When the <paramref name="token"/> is <c>null</c> or <c>whitespace</c> or when <paramref name="applicationId"/> is an empty Guid
        /// </exception>
        /// <exception cref="InvalidTokenFormatException">
        /// When the <paramref name="token"/> is malformed or otherwise unreadable
        /// </exception>
        /// <exception cref="InvalidTokenApplicationException">
        /// When the application to which the <paramref name="token"/> was issued does not match the expected <paramref name="applicationId"/> 
        /// </exception>
        /// <exception cref="TokenExpiredException">
        /// When the <paramref name="token"/> is expired
        /// </exception>
        /// <exception cref="InvalidTokenSignatureException">
        /// When the <paramref name="token"/> has an invalid signature
        /// </exception>
        public static async Task<UserIdentityToken> ParseAsync(string token, Guid applicationId)
        {
            var uit = new UserIdentityToken(SigningKeysCache.Instance);
            await uit.ValidateTokenAsync(token, applicationId);
            return uit;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The user identifier
        /// </summary>
        public string UserId { get; private set; }

        /// <summary>
        /// The user email address
        /// </summary>
        public string Email { get; private set; }

        /// <summary>
        /// The user last name
        /// </summary>
        public string FamilyName { get; private set; }

        /// <summary>
        /// The user first name
        /// </summary>
        public string GivenName { get; private set; }

        /// <summary>
        /// The environment identifier
        /// </summary>
        public string EnvironmentId { get; private set; }

        #endregion

        /// <summary>
        /// Validate the token against the OIDC and ensure it was issued to the specified application
        /// </summary>
        /// <param name="token">The user identity token string</param>
        /// <param name="applicationId">The application identifier to whom the token should be issued</param>
        internal async Task ValidateTokenAsync(string token, Guid applicationId)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (applicationId.Equals(Guid.Empty))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }

            // attempt to read the token
            JwtSecurityToken t = null;
            try
            {
                t = ReadJwtToken(token);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidTokenFormatException(ex.Message);
            }

            // if the token could not be read, then throw an exception
            if (t == null)
            {
                throw new InvalidTokenFormatException();
            }

            // currently, user identity tokens issued from the SKY API OAuth 2.0 service use an "x5t" claim in the header - in the future, 
            // we may decide to transition to using a "kid" claim in the header.  For now, we'll support both x5t and kid versions to future-proof this logic
            var cacheKey = t.Header.Kid ?? t.Header.X5t ?? string.Empty;
            if (!_signingKeysCache.Certificates.ContainsKey(cacheKey) || _signingKeysCache.RefreshNeeded())
            {
                await _signingKeysCache.Refresh();

                // If it still doesn't contain the key, then enter a null value to avoid re-checking each time.
                if (!_signingKeysCache.Certificates.ContainsKey(cacheKey))
                {
                    var keys = _signingKeysCache.Certificates;
                    lock (keys)
                    {
                        keys.Add(cacheKey, null);
                    }
                }
            }

            if (_signingKeysCache.Certificates.TryGetValue(cacheKey, out X509Certificate2 cert) && cert != null)
            {
                var validationparams = new TokenValidationParameters() {
                    ValidateAudience = true,
                    ValidAudience = applicationId.ToString(),
                    ValidateIssuer = true,
                    ValidIssuer = _signingKeysCache.Issuer,
                    IssuerSigningKey = new X509SecurityKey(cert),
                    //AudienceValidator = (audiences, securityToken, validationParameters) =>
                    //{
                    //    return audiences.Any(a => a.Equals(applicationId.ToString(), StringComparison.OrdinalIgnoreCase));
                    //}
                };

                try
                {
                    var cp = ValidateToken(token, validationparams);
                    var userId = GetClaimValue(ClaimTypes.NameIdentifier, cp.Claims);
                    var envId = GetClaimValue("environment_id", cp.Claims);
                    var email = GetClaimValue(ClaimTypes.Email, cp.Claims);
                    var familyName = GetClaimValue(ClaimTypes.Surname, cp.Claims);
                    var givenName = GetClaimValue(ClaimTypes.GivenName, cp.Claims);

                    // if no userId was found, throw an exception
                    if (string.IsNullOrEmpty(userId))
                    {
                        throw new InvalidTokenFormatException();
                    }

                    // if no environmentId was found, throw an exception
                    if (string.IsNullOrEmpty(envId))
                    {
                        throw new InvalidTokenFormatException();
                    }

                    // if no email was found, throw an exception
                    if (string.IsNullOrEmpty(email))
                    {
                        throw new InvalidTokenFormatException();
                    }

                    // if no familyName was found, throw an exception
                    if (string.IsNullOrEmpty(familyName))
                    {
                        throw new InvalidTokenFormatException();
                    }

                    // if no givenName was found, throw an exception
                    if (string.IsNullOrEmpty(givenName))
                    {
                        throw new InvalidTokenFormatException();
                    }


                    this.UserId = userId;
                    this.EnvironmentId = envId;
                    this.Email = email;
                    this.FamilyName = familyName;
                    this.GivenName = givenName;
                    return;
                }
                catch (SecurityTokenInvalidAudienceException e1)
                {
                    throw new InvalidTokenApplicationException(e1.Message);
                }
                catch (SecurityTokenExpiredException e2)
                {
                    throw new TokenExpiredException(e2.Message);
                }
                catch (SecurityTokenValidationException e3)
                {
                    throw new InvalidTokenSignatureException(e3.Message);
                }
                catch (Exception e4)
                {
                    throw new InvalidTokenFormatException(e4.Message);
                }
            }
        }

        /// <summary>
        /// Read the contents of the JWT.
        /// </summary>
        internal virtual JwtSecurityToken ReadJwtToken(string token)
        {
            return (JwtSecurityToken)_jwtSecurityTokenHandler.ReadToken(token);
        }

        /// <summary>
        /// Validate the JWT.
        /// </summary>
        internal virtual ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationparams)
        {
            return _jwtSecurityTokenHandler.ValidateToken(token, validationparams, out SecurityToken outToken);
        }

        /// <summary>
        /// Returns the specified claim value from the claims list.
        /// If no claims exist by this time, then null is returned.
        /// If there are are multiple claims by this type then the first value is returned.
        /// </summary>
        private static string GetClaimValue(string claimType, IEnumerable<Claim> claims)
        {
            var claim = claims.FirstOrDefault(c => c.Type == claimType);
            return claim?.Value;
        }
    }
}