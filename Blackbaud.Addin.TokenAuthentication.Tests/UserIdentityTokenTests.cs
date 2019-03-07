using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Blackbaud.Addin.TokenAuthentication.Exceptions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Blackbaud.Addin.TokenAuthentication.Tests
{
    [TestClass]
    public class UserIdentityTokenTests
    {
        private const string TEST_X5T = "my_test_x5t";
        private const string TEST_JWT = "my test token value";
        private const string TEST_AUD = "f4414c4d-7a29-4249-82b4-4b10a8b4719f";
        private const string TEST_USERID = "9d874c83-cd07-4690-ab6d-ad9551dbacf4";
        private const string TEST_ENVID = "p-Q2caBopqjEisbiLbtjEoMh";

        [TestMethod]
        [Description("Tests that the constructor is successful.")]
        public void UserIdentityToken_Constructor_Success()
        {
            var token = new UserIdentityToken(new SigningKeysCache());
        }

        [TestMethod]
        [Description("Tests that an exception is thrown when a null token is provided.")]
        [ExpectedException(typeof(ArgumentNullException), "Expected an ArgumentNullException when a null token was supplied.")]
        public async Task UserIdentityToken_NullToken()
        {
            var token = await UserIdentityToken.ParseAsync(null, Guid.NewGuid());
        }

        [TestMethod]
        [Description("Tests that an exception is thrown when an empty token is provided.")]
        [ExpectedException(typeof(ArgumentNullException), "Expected an ArgumentNullException when an empty token was supplied.")]
        public async Task UserIdentityToken_EmptyToken()
        {
            var token = await UserIdentityToken.ParseAsync(string.Empty, Guid.NewGuid());
        }

        [TestMethod]
        [Description("Tests that an exception is thrown when a whitespace token is provided.")]
        [ExpectedException(typeof(ArgumentNullException), "Expected an ArgumentNullException when a whitespace token was supplied.")]
        public async Task UserIdentityToken_WhitespaceToken()
        {
            var token = await UserIdentityToken.ParseAsync(string.Empty, Guid.NewGuid());
        }

        [TestMethod]
        [Description("Tests that an exception is thrown when an empty guid is provided.")]
        [ExpectedException(typeof(ArgumentNullException), "Expected an ArgumentNullException when an empty guid was supplied.")]
        public async Task UserIdentityToken_EmptyGuidApplication()
        {
            var token = await UserIdentityToken.ParseAsync("valid token", Guid.Empty);
        }

        [TestMethod]
        [Description("Tests that an exception is thrown  when a malformed token is provided.")]
        [ExpectedException(typeof(InvalidTokenFormatException), "Expected an InvalidTokenFormatException when a malforrmed token was supplied.")]
        public async Task UserIdentityToken_MalformedToken()
        {
            var token = await UserIdentityToken.ParseAsync("malformed_token", Guid.NewGuid());
        }

        [TestMethod]
        [Description("Ensure that if the JWT x5t isn't in the key cache, then Refresh is called.")]
        public async Task UserIdentityToken_MissingKeyCausesRefresh()
        {
            var mockCache = new Mock<SigningKeysCache>();
            mockCache.Setup(o => o.Certificates).Returns(new Dictionary<string, X509Certificate2>());

            var mockToken = GetMockUserIdentityToken(mockCache.Object);
            SetupUserIdentityTokenReadJwtToken(mockToken);

            try
            {
                await mockToken.Object.ValidateTokenAsync(TEST_JWT, new Guid(TEST_AUD));
            }
            catch
            {
                // eat the exception, we just want to verify that refresh was called
            }

            mockCache.Verify(o => o.Refresh());
        }

        [TestMethod]
        [Description("Ensure that if RefreshNeeded, then Refresh is called, even though the X5T is already found.")]
        public async Task UserIdentityToken_RefreshesIfRefreshNeeded()
        {
            var keys = new Dictionary<string, X509Certificate2>();
            keys.Add(TEST_X5T, GetTestCert());

            var mockCache = new Mock<SigningKeysCache>();
            mockCache.Setup(o => o.Certificates).Returns(keys);
            
            // Flag the cache to indicate that a Refresh is needed.
            mockCache.Setup(o => o.RefreshNeeded()).Returns(true);

            var mockToken = GetMockUserIdentityToken(mockCache.Object);
            SetupUserIdentityTokenReadJwtToken(mockToken);
            SetupUserIdentityTokenInvalidToken(mockToken);

            try
            {
                await mockToken.Object.ValidateTokenAsync(TEST_JWT, new Guid(TEST_AUD));
            }
            catch
            {
                // eat the exception, we just want to verify that refresh was called
            }

            mockCache.Verify(o => o.Refresh());
        }

        [TestMethod]
        [Description("If ValidateTokenAsync throws a SecurityTokenInvalidAudienceException, then ensure that an InvalidTokenApplicationException is thrown.")]
        [ExpectedException(typeof(InvalidTokenApplicationException), "Expected an InvalidTokenApplicationException when a SecurityTokenInvalidAudienceException is thrown.")]
        public async Task UserIdentityToken_Treat_SecurityTokenInvalidAudienceException_As_Unauthenticated()
        {
            var keys = new Dictionary<string, X509Certificate2>();
            keys.Add(TEST_X5T, GetTestCert());

            var mockCache = new Mock<SigningKeysCache>();
            mockCache.Setup(o => o.Certificates).Returns(keys);

            var mockToken = GetMockUserIdentityToken(mockCache.Object);
            SetupUserIdentityTokenReadJwtToken(mockToken);
            SetupUserIdentityTokenInvalidAudience(mockToken);

            await mockToken.Object.ValidateTokenAsync(TEST_JWT, new Guid(TEST_AUD));
        }

        [TestMethod]
        [Description("If ValidateTokenAsync throws a SecurityTokenExpiredException, then ensure that a TokenExpiredException is thrown.")]
        [ExpectedException(typeof(TokenExpiredException), "Expected an TokenExpiredException when a SecurityTokenExpiredException is thrown.")]
        public async Task UserIdentityToken_Treat_SecurityTokenExpiredException_As_Unauthenticated()
        {
            var keys = new Dictionary<string, X509Certificate2>();
            keys.Add(TEST_X5T, GetTestCert());

            var mockCache = new Mock<SigningKeysCache>();
            mockCache.Setup(o => o.Certificates).Returns(keys);

            var mockToken = GetMockUserIdentityToken(mockCache.Object);
            SetupUserIdentityTokenReadJwtToken(mockToken);
            SetupUserIdentityTokenExpiredToken(mockToken);

            await mockToken.Object.ValidateTokenAsync(TEST_JWT, new Guid(TEST_AUD));
        }

        [TestMethod]
        [Description("If ValidateTokenAsync throws a SecurityTokenValidationException, then ensure that an InvalidTokenSignatureException is thrown.")]
        [ExpectedException(typeof(InvalidTokenSignatureException), "Expected an InvalidTokenSignatureException when a SecurityTokenValidationException is thrown.")]
        public async Task UserIdentityToken_Treat_SecurityTokenValidationException_As_Unauthenticated()
        {
            var keys = new Dictionary<string, X509Certificate2>();
            keys.Add(TEST_X5T, GetTestCert());

            var mockCache = new Mock<SigningKeysCache>();
            mockCache.Setup(o => o.Certificates).Returns(keys);

            var mockToken = GetMockUserIdentityToken(mockCache.Object);
            SetupUserIdentityTokenReadJwtToken(mockToken);
            SetupUserIdentityTokenInvalidToken(mockToken);

            await mockToken.Object.ValidateTokenAsync(TEST_JWT, new Guid(TEST_AUD));
        }

        [TestMethod]
        [Description("If ValidateTokenAsync throws any other exception, then ensure that an InvalidTokenFormatException is thrown.")]
        [ExpectedException(typeof(InvalidTokenFormatException), "Expected an InvalidTokenFormatException when any other exception is thrown.")]
        public async Task UserIdentityToken_Treat_AnyOtherException_As_Unauthenticated()
        {
            var keys = new Dictionary<string, X509Certificate2>();
            keys.Add(TEST_X5T, GetTestCert());

            var mockCache = new Mock<SigningKeysCache>();
            mockCache.Setup(o => o.Certificates).Returns(keys);

            var mockToken = GetMockUserIdentityToken(mockCache.Object);
            SetupUserIdentityTokenReadJwtToken(mockToken);
            SetupUserIdentityTokenOtherException(mockToken);

            await mockToken.Object.ValidateTokenAsync(TEST_JWT, new Guid(TEST_AUD));
        }

        [TestMethod]
        [Description("If the token is valid, the userId and environmentId are returned.")]
        public async Task UserIdentityToken_ValidToken()
        {
            var mockCache = GetMockCache();

            var mockToken = GetMockUserIdentityToken(mockCache.Object);
            SetupUserIdentityTokenReadJwtToken(mockToken);

            mockToken.Setup(o => o.ValidateToken(It.IsAny<string>(), It.IsAny<TokenValidationParameters>())).Returns(() =>
            {
                var result = new ClaimsPrincipal();

                var identity = new ClaimsIdentity("RandomValue");
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, TEST_USERID));
                identity.AddClaim(new Claim("environment_id", TEST_ENVID));

                result.AddIdentity(identity);
                return result;
             });

            await mockToken.Object.ValidateTokenAsync(TEST_JWT, new Guid(TEST_AUD));

            Assert.AreEqual(mockToken.Object.UserId, TEST_USERID, true);
            Assert.AreEqual(mockToken.Object.EnvironmentId, TEST_ENVID, true);

            mockCache.Verify(o => o.Refresh(), Times.Never);
        }

        #region Helper

        private X509Certificate2 GetTestCert()
        {
            return new X509Certificate2(Convert.FromBase64String("MIIDFzCCAgOgAwIBAgIQYyFH4hyHqIZJFSvhaTufDDAJBgUrDgMCHQUAMCAxHjAcBgNVBAMTFVRlc3RCQkF1dGhTaWduaW5nQ2VydDAeFw0xMzExMDUxNjUzNTlaFw0zOTEyMzEyMzU5NTlaMCAxHjAcBgNVBAMTFVRlc3RCQkF1dGhTaWduaW5nQ2VydDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMyjnT+N3cypXtoTfr8HTNXcThqPKSQyioqIt0A7ZEQjByErcFdx5LyPSxyUWNg9r9Ay3T/qUqmCD4roK/LruT8W+02SLgZLGJ4cecuQqOryABVn3SeTCuViLVRFkAbGVWXqEI2woFq226xCyKEbYMIr0Lxisuo8nEQ0XSAYh2tiT6XXMf2aHomBxxUk9tkfyjBqim+OBZxglKQ+jQgA35CDi6NezJaSE13oNuo7iMlOyYV3+jPh/tohYKxhAod2biMa5oKDmVc3C6zvKZyoSVMQE0jRs2SzZW/TzBXkHfBAtoSRCarWtTv+ode+XYcQw0Hi5p5FKYmKx/sdx4RvLG0CAwEAAaNVMFMwUQYDVR0BBEowSIAQZY/UuhZiXYqsNv1VH1ec3aEiMCAxHjAcBgNVBAMTFVRlc3RCQkF1dGhTaWduaW5nQ2VydIIQYyFH4hyHqIZJFSvhaTufDDAJBgUrDgMCHQUAA4IBAQBokHC1xp2I4+K7SzGQiXehlLcjDX4+9wWX+8ZzByOKyTIfc+3DthoU1aWiuG1ioFyL8ttRLm10n3PSXR7hJtXY4JnyxfolZy+c6+n3AsYnstaZipZgnCxJ2+P1e+MzbOoMFuBceg+vpW0dJex2MrJ5h/khwFNVvhoPnGT8W7j6Q+Lw6VeexbbLBNPtpmHlrK5/7RjjZdZTvFbEMqBz1hl4Ny1Gz+mLq4fsNxC4eoW5kq/MyVbigX8kwOonr4dh68OSOLoYJ9ml62wE0uhiamM89zaFeui6e/R/xAqsTlnl10qDBDVKcGHyIrmgBUkHYknQxCnHoFW/N+w8KmhryAko"));
        }

        private JwtSecurityToken GetTestJwtSecurityToken()
        {
            var result = new JwtSecurityToken(audience: TEST_AUD);
            result.Header.Add("x5t", TEST_X5T);
            return result;
        }

        private void SetupUserIdentityTokenReadJwtToken(Mock<UserIdentityToken> mockToken)
        {
            mockToken.Setup(o => o.ReadJwtToken(It.Is<string>(t => t == TEST_JWT))).Returns((string token) =>
            {
                return GetTestJwtSecurityToken();
            });
        }

        private void SetupUserIdentityTokenInvalidAudience(Mock<UserIdentityToken> mockToken)
        {
            mockToken.Setup(o => o.ValidateToken(It.IsAny<string>(), It.IsAny<TokenValidationParameters>())).Returns(() =>
            {
                throw new SecurityTokenInvalidAudienceException();
            });
        }

        private void SetupUserIdentityTokenExpiredToken(Mock<UserIdentityToken> mockToken)
        {
            mockToken.Setup(o => o.ValidateToken(It.IsAny<string>(), It.IsAny<TokenValidationParameters>())).Returns(() =>
            {
                throw new SecurityTokenExpiredException();
            });
        }

        private void SetupUserIdentityTokenOtherException(Mock<UserIdentityToken> mockToken)
        {
            mockToken.Setup(o => o.ValidateToken(It.IsAny<string>(), It.IsAny<TokenValidationParameters>())).Returns(() =>
            {
                throw new InvalidOperationException();
            });
        }

        private void SetupUserIdentityTokenInvalidToken(Mock<UserIdentityToken> mockToken)
        {
            mockToken.Setup(o => o.ValidateToken(It.IsAny<string>(), It.IsAny<TokenValidationParameters>())).Returns(() =>
            {
                throw new SecurityTokenValidationException();
            });
        }

        private Mock<SigningKeysCache> GetMockCache()
        {
            var keys = new Dictionary<string, X509Certificate2>();
            keys.Add(TEST_X5T, GetTestCert());

            var mockCache = new Mock<SigningKeysCache>();
            mockCache.Setup(o => o.Certificates).Returns(keys);
            mockCache.Setup(o => o.Issuer).Returns("some_issuer");

            return mockCache;
        }

        private Mock<UserIdentityToken> GetMockUserIdentityToken(SigningKeysCache signingKeysCache)
        {
            return new Mock<UserIdentityToken>(signingKeysCache) { CallBase = true };
        }

        #endregion Helper
    }
}