// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using JustEat.HttpClientInterception;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace SampleApp.Tests
{
    [Collection(HttpServerCollection.Name)] // Use the shared HTTP server fixture 
    public class ReposTests
    {
        private readonly HttpServerFixture _fixture;

        public ReposTests(HttpServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Can_Get_Organization_Repositories()
        {
            // Arrange - use a scope to clean-up registrations
            using (_fixture.Interceptor.BeginScope())
            {
                // Setup an expected response from the GitHub API
                var builder = new HttpRequestInterceptionBuilder()
                    .ForHttps()
                    .ForHost("api.github.com")
                    .ForPath("orgs/weyland-yutani/repos")
                    .ForQuery("per_page=2")
                    .WithJsonContent(
                        new[]
                        {
                            new { id = 1, name = "foo" },
                            new { id = 2, name = "bar" },
                        });

                _fixture.Interceptor.Register(builder);

                string[] actual;

                using (var httpClient = new HttpClient())
                {
                    // Set up the HTTP client to use the self-hosted HTTP server for the application
                    httpClient.BaseAddress = new Uri(_fixture.ServerUrl);

                    // Act - Perform the HTTP request against our application and deserialize the response
                    string json = await httpClient.GetStringAsync("api/repos?count=2");
                    actual = JsonConvert.DeserializeObject<string[]>(json);
                }

                // Assert - Our application should have parsed the stub-names
                actual.ShouldBe(new[] { "bar", "foo" });
            }
        }
    }
}
