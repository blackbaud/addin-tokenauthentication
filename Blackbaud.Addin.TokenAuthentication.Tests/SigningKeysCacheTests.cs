using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blackbaud.Addin.TokenAuthentication.Tests
{
    [TestClass]
    public class SigningKeysCacheTests
    {
        [TestMethod]
        [Description("Ensure that the sington is created.")]
        public void SigningKeysCache_Instance()
        {
            var instance = SigningKeysCache.Instance;
            Assert.IsNotNull(instance, "Instance was null");
        }

        [TestMethod]
        [Description("Happy path test that the refreshing the cache will obtain the issuer and keys from the Open Id configuration endpoint.")]
        public async Task SigningKeysCache_SetsIssuerAndKeys()
        {
            var cache = new SigningKeysCache();
            Assert.IsNull(cache.Issuer);
            Assert.IsNotNull(cache.Certificates);
            Assert.AreEqual(0, cache.Certificates.Count);

            await cache.Refresh();
            Assert.AreEqual("https://oauth2.sky.blackbaud.com/", cache.Issuer);
            Assert.IsTrue(cache.Certificates.Count == 1 || cache.Certificates.Count == 2, "Expected exactly 1 or 2 keys from the config.");
        }

        [TestMethod]
        [Description("Validates that the time of refreshes are tracked and we can know when a refresh is needed.")]
        public async Task SigningKeysCache_RefreshTimeTracking()
        {
            var cache = new SigningKeysCache();
            Assert.IsFalse(cache.LastAttemptedRefresh.HasValue);
            Assert.IsTrue(cache.RefreshNeeded());

            await cache.Refresh();
            Assert.IsFalse(cache.RefreshNeeded());
            Assert.IsTrue(DateTimeOffset.UtcNow.Subtract(cache.LastAttemptedRefresh.GetValueOrDefault()).TotalSeconds < 2);
        }

    }
}