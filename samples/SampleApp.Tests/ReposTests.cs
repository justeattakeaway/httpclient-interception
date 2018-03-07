// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using JustEat.HttpClientInterception;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace SampleApp.Tests
{
    [Collection(HttpServerCollection.Name)] // Use the shared HTTP server fixture
    public class ReposTests
    {
        private readonly HttpServerFixture _fixture;
        private readonly ITestOutputHelper _outputHelper;

        public ReposTests(HttpServerFixture fixture, ITestOutputHelper outputHelper)
        {
            _fixture = fixture;
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task Can_Get_Organization_Repositories()
        {
            // Add a callback to log any HTTP requests made
            _fixture.Interceptor.OnSend = (request) =>
                {
                    _outputHelper.WriteLine($"HTTP {request.Method} {request.RequestUri}");
                    return Task.CompletedTask;
                };

            // Arrange - use a scope to clean-up registrations
            using (_fixture.Interceptor.BeginScope())
            {
                // Setup an expected response from the GitHub API
                var builder = new HttpRequestInterceptionBuilder()
                    .Requests()
                    .ForHttps()
                    .ForHost("api.github.com")
                    .ForPath("orgs/weyland-yutani/repos")
                    .ForQuery("per_page=2")
                    .Responds()
                    .WithJsonContent(
                        new[]
                        {
                            new { id = 1, name = "foo" },
                            new { id = 2, name = "bar" },
                        })
                    .RegisterWith(_fixture.Interceptor);

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
