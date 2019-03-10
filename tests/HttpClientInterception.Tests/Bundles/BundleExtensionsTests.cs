// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace JustEat.HttpClientInterception.Bundles
{
    public static class BundleExtensionsTests
    {
        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            var headers = new Dictionary<string, string>()
            {
                { "accept", "application/vnd.github.v3+json" },
                { "authorization", "token my-token" },
                { "user-agent", "My-App/1.0.0" },
            };

            // Act
            options.RegisterBundle(Path.Join("Bundles", "http-request-bundle.json"));

            // Assert
            await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/");
            await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/order-history");
            await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat", headers: headers);
        }
    }
}
