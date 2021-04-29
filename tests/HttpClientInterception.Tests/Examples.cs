// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JustEat.HttpClientInterception.GitHub;
using Newtonsoft.Json.Linq;
using Polly;
using Refit;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// This class contains tests which provide example scenarios for using the library.
    /// </summary>
    public static class Examples
    {
        [Fact]
        public static async Task Fault_Injection()
        {
            #region fault-injection

            var options = new HttpClientInterceptorOptions();

            var builder = new HttpRequestInterceptionBuilder()
                .Requests()
                .ForHost("public.je-apis.com")
                .WithStatus(HttpStatusCode.InternalServerError)
                .RegisterWith(options);

            var client = options.CreateHttpClient();

            // Throws an HttpRequestException
            await Assert.ThrowsAsync<HttpRequestException>(
                () => client.GetStringAsync("http://public.je-apis.com"));

            #endregion
        }

        [Fact]
        public static async Task Intercept_Http_Get_For_Json_Object()
        {
            #region minimal-example

            // Arrange
            var options = new HttpClientInterceptorOptions();
            var builder = new HttpRequestInterceptionBuilder();

            builder
                .Requests()
                .ForGet()
                .ForHttps()
                .ForHost("public.je-apis.com")
                .ForPath("terms")
                .Responds()
                .WithJsonContent(new { Id = 1, Link = "https://www.just-eat.co.uk/privacy-policy" })
                .RegisterWith(options);

            using var client = options.CreateHttpClient();

            // Act
            string json = await client.GetStringAsync("https://public.je-apis.com/terms");

            #endregion

            // Assert
            var content = JObject.Parse(json);
            content.Value<int>("Id").ShouldBe(1);
            content.Value<string>("Link").ShouldBe("https://www.just-eat.co.uk/privacy-policy");
        }

        [Fact]
        public static async Task Intercept_Http_Get_For_Html_String()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForHost("www.google.co.uk")
                .ForPath("search")
                .ForQuery("q=Just+Eat")
                .WithMediaType("text/html")
                .WithContent(@"<!DOCTYPE html><html dir=""ltr"" lang=""en""><head><title>Just Eat</title></head></html>");

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();

            // Act
            string html = await client.GetStringAsync("http://www.google.co.uk/search?q=Just+Eat");

            // Assert
            html.ShouldContain("Just Eat");
        }

        [Fact]
        public static async Task Intercept_Http_Get_For_Raw_Bytes()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("files.domain.com")
                .ForPath("setup.exe")
                .WithMediaType("application/octet-stream")
                .WithContent(() => new byte[] { 0, 1, 2, 3, 4 });

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();

            // Act
            byte[] content = await client.GetByteArrayAsync("https://files.domain.com/setup.exe");

            // Assert
            content.ShouldBe(new byte[] { 0, 1, 2, 3, 4 });
        }

        [Fact]
        public static async Task Intercept_Http_Post_For_Json_String()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForPost()
                .ForHttps()
                .ForHost("public.je-apis.com")
                .ForPath("consumer")
                .WithStatus(HttpStatusCode.Created)
                .WithContent(@"{ ""id"": 123 }");

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();
            using var body = new StringContent(@"{ ""FirstName"": ""John"" }");

            // Act
            using var response = await client.PostAsync("https://public.je-apis.com/consumer", body);
            string json = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            var content = JObject.Parse(json);
            content.Value<int>("id").ShouldBe(123);
        }

        [Fact]
        public static async Task Intercept_Http_Get_For_Json_Object_Based_On_Request_Headers()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForGet()
                .ForHttps()
                .ForHost("public.je-apis.com")
                .ForPath("terms")
                .ForRequestHeader("Accept-Tenant", "uk")
                .WithJsonContent(new { Id = 1, Link = "https://www.just-eat.co.uk/privacy-policy" });

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();
            client.DefaultRequestHeaders.Add("Accept-Tenant", "uk");

            // Act
            string json = await client.GetStringAsync("https://public.je-apis.com/terms");

            // Assert
            var content = JObject.Parse(json);
            content.Value<int>("Id").ShouldBe(1);
            content.Value<string>("Link").ShouldBe("https://www.just-eat.co.uk/privacy-policy");
        }

        [Fact]
        public static async Task Conditionally_Intercept_Http_Post_For_Json_Object()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .Requests()
                .ForPost()
                .ForHttps()
                .ForHost("public.je-apis.com")
                .ForPath("consumer")
                .ForContent(
                    async (requestContent) =>
                    {
                        string requestBody = await requestContent.ReadAsStringAsync();

                        var body = JObject.Parse(requestBody);

                        return body.Value<string>("FirstName") == "John";
                    })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .WithContent(@"{ ""id"": 123 }");

            var options = new HttpClientInterceptorOptions()
                .ThrowsOnMissingRegistration()
                .Register(builder);

            using var client = options.CreateHttpClient();
            using var body = new StringContent(@"{ ""FirstName"": ""John"" }");

            // Act
            using var response = await client.PostAsync("https://public.je-apis.com/consumer", body);
            string json = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            var content = JObject.Parse(json);
            content.Value<int>("id").ShouldBe(123);
        }

        [Fact]
        public static async Task Intercept_Http_Put_For_Json_String()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForPut()
                .ForHttps()
                .ForHost("public.je-apis.com")
                .ForPath("baskets/123/user")
                .WithStatus(HttpStatusCode.Created)
                .WithContent(@"{ ""id"": 123 }");

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();
            using var body = new StringContent(@"{ ""User"": { ""DisplayName"": ""John"" } }");

            // Act
            using var response = await client.PutAsync("https://public.je-apis.com/baskets/123/user", body);
            string json = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            var content = JObject.Parse(json);
            content.Value<int>("id").ShouldBe(123);
        }

        [Fact]
        public static async Task Intercept_Http_Delete()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForDelete()
                .ForHttps()
                .ForHost("public.je-apis.com")
                .ForPath("baskets/123/orderitems/456")
                .WithStatus(HttpStatusCode.NoContent);

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();

            // Act
            using var response = await client.DeleteAsync("https://public.je-apis.com/baskets/123/orderitems/456");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public static async Task Intercept_Custom_Http_Method()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForMethod(new HttpMethod("custom"))
                .ForHost("custom.domain.com")
                .ForQuery("length=2")
                .WithContent(() => new byte[] { 0, 1 });

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();
            using var message = new HttpRequestMessage(new HttpMethod("custom"), "http://custom.domain.com?length=2");

            // Act
            using var response = await client.SendAsync(message);
            byte[] content = await response.Content.ReadAsByteArrayAsync();

            // Assert
            content.ShouldBe(new byte[] { 0, 1 });
        }

        [Fact]
        public static async Task Inject_Fault_For_Http_Get()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForHost("www.google.co.uk")
                .WithStatus(HttpStatusCode.InternalServerError);

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();

            // Act and Assert
            await Should.ThrowAsync<HttpRequestException>(() => client.GetStringAsync("http://www.google.co.uk"));
        }

        [Fact]
        public static async Task Inject_Latency_For_Http_Get()
        {
            // Arrange
            var latency = TimeSpan.FromMilliseconds(50);

            var builder = new HttpRequestInterceptionBuilder()
                .ForHost("www.google.co.uk")
                .WithInterceptionCallback((_) => Task.Delay(latency));

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            var stopwatch = new Stopwatch();

            using (var client = options.CreateHttpClient())
            {
                stopwatch.Start();

                // Act
                await client.GetStringAsync("http://www.google.co.uk");

                stopwatch.Stop();
            }

            // Assert
            stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(latency);
        }

        [Fact]
        public static async Task Intercept_Http_Get_To_Stream_Content_From_Disk()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForHost("xunit.github.io")
                .ForPath("settings.json")
                .WithContentStream(() => File.OpenRead("xunit.runner.json"));

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var client = options.CreateHttpClient();

            // Act
            string json = await client.GetStringAsync("http://xunit.github.io/settings.json");

            // Assert
            json.ShouldNotBeNullOrWhiteSpace();

            var config = JObject.Parse(json);
            config.Value<string>("methodDisplay").ShouldBe("method");
        }

        [Fact]
        public static async Task Use_Scopes_To_Change_And_Then_Restore_Behavior()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForHost("public.je-apis.com")
                .WithJsonContent(new { value = 1 });

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            string json1;
            string json2;
            string json3;

            // Act
            using (var client = options.CreateHttpClient())
            {
                json1 = await client.GetStringAsync("http://public.je-apis.com");

                using (options.BeginScope())
                {
                    options.Register(builder.WithJsonContent(new { value = 2 }));

                    json2 = await client.GetStringAsync("http://public.je-apis.com");
                }

                json3 = await client.GetStringAsync("http://public.je-apis.com");
            }

            // Assert
            json1.ShouldNotBe(json2);
            json1.ShouldBe(json3);
        }

        [Fact]
        public static async Task Use_With_Refit()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .WithJsonContent(new { id = 1516790, login = "justeat", url = "https://api.github.com/orgs/justeat" });

            var options = new HttpClientInterceptorOptions().Register(builder);
            var service = RestService.For<IGitHub>(options.CreateHttpClient("https://api.github.com"));

            // Act
            Organization actual = await service.GetOrganizationAsync("justeat");

            // Assert
            actual.ShouldNotBeNull();
            actual.Id.ShouldBe(1516790);
            actual.Login.ShouldBe("justeat");
            actual.Url.ShouldBe("https://api.github.com/orgs/justeat");
        }

        [Fact]
        public static async Task Use_Multiple_Registrations()
        {
            // Arrange
            var justEat = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .WithJsonContent(new { id = 1516790, login = "justeat", url = "https://api.github.com/orgs/justeat" });

            var dotnet = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/dotnet")
                .WithJsonContent(new { id = 9141961, login = "dotnet", url = "https://api.github.com/orgs/dotnet" });

            var options = new HttpClientInterceptorOptions()
                .Register(justEat, dotnet);

            var service = RestService.For<IGitHub>(options.CreateHttpClient("https://api.github.com"));

            // Act
            Organization justEatOrg = await service.GetOrganizationAsync("justeat");
            Organization dotnetOrg = await service.GetOrganizationAsync("dotnet");

            // Assert
            justEatOrg.ShouldNotBeNull();
            justEatOrg.Id.ShouldBe(1516790);
            justEatOrg.Login.ShouldBe("justeat");
            justEatOrg.Url.ShouldBe("https://api.github.com/orgs/justeat");

            // Assert
            dotnetOrg.ShouldNotBeNull();
            dotnetOrg.Id.ShouldBe(9141961);
            dotnetOrg.Login.ShouldBe("dotnet");
            dotnetOrg.Url.ShouldBe("https://api.github.com/orgs/dotnet");
        }

        [Fact]
        public static async Task Use_The_Same_Builder_For_Multiple_Registrations_On_The_Same_Host()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();

            // Configure a response for https://api.github.com/orgs/justeat
            var builder = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .WithJsonContent(new { id = 1516790, login = "justeat", url = "https://api.github.com/orgs/justeat" });

            options.Register(builder);

            // Update the same builder to configure a response for https://api.github.com/orgs/dotnet
            builder.ForPath("orgs/dotnet")
                   .WithJsonContent(new { id = 9141961, login = "dotnet", url = "https://api.github.com/orgs/dotnet" });

            options.Register(builder);

            var service = RestService.For<IGitHub>(options.CreateHttpClient("https://api.github.com"));

            // Act
            Organization justEatOrg = await service.GetOrganizationAsync("justeat");
            Organization dotnetOrg = await service.GetOrganizationAsync("dotnet");

            // Assert
            justEatOrg.ShouldNotBeNull();
            justEatOrg.Id.ShouldBe(1516790);
            justEatOrg.Login.ShouldBe("justeat");
            justEatOrg.Url.ShouldBe("https://api.github.com/orgs/justeat");

            // Assert
            dotnetOrg.ShouldNotBeNull();
            dotnetOrg.Id.ShouldBe(9141961);
            dotnetOrg.Login.ShouldBe("dotnet");
            dotnetOrg.Url.ShouldBe("https://api.github.com/orgs/dotnet");
        }

        [Fact]
        public static async Task Match_Any_Host_Name()
        {
            // Arrange
            string expected = @"{""id"":12}";
            string actual;

            var builder = new HttpRequestInterceptionBuilder()
                .ForHttp()
                .ForAnyHost()
                .ForPath("orders")
                .ForQuery("id=12")
                .WithStatus(HttpStatusCode.OK)
                .WithContent(expected);

            var options = new HttpClientInterceptorOptions().Register(builder);

            using (var client = options.CreateHttpClient())
            {
                // Act
                actual = await client.GetStringAsync("http://myhost.net/orders?id=12");
            }

            // Assert
            actual.ShouldBe(expected);

            using (var client = options.CreateHttpClient())
            {
                // Act
                actual = await client.GetStringAsync("http://myotherhost.net/orders?id=12");
            }

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task Use_Default_Response_For_Unmatched_Requests()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
            {
                OnMissingRegistration = (request) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)),
            };

            using var client = options.CreateHttpClient();

            // Act
            using var response = await client.GetAsync("https://google.com/");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public static async Task Use_Custom_Request_Matching()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .Requests().For((request) => request.RequestUri.Host == "google.com")
                .Responds().WithContent(@"<!DOCTYPE html><html dir=""ltr"" lang=""en""><head><title>Google Search</title></head></html>");

            var options = new HttpClientInterceptorOptions().Register(builder);

            using var client = options.CreateHttpClient();

            // Act and Assert
            (await client.GetStringAsync("https://google.com/")).ShouldContain("Google Search");
            (await client.GetStringAsync("https://google.com/search")).ShouldContain("Google Search");
            (await client.GetStringAsync("https://google.com/search?q=foo")).ShouldContain("Google Search");
        }

        [Fact]
        public static async Task Use_Custom_Request_Matching_With_Priorities()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
                .ThrowsOnMissingRegistration();

            var builder = new HttpRequestInterceptionBuilder()
                .Requests().For((request) => request.RequestUri.Host == "google.com").HavingPriority(1)
                .Responds().WithContent(@"First")
                .RegisterWith(options)
                .Requests().For((request) => request.RequestUri.Host.Contains("google", StringComparison.OrdinalIgnoreCase)).HavingPriority(2)
                .Responds().WithContent(@"Second")
                .RegisterWith(options)
                .Requests().For((request) => request.RequestUri.PathAndQuery.Contains("html", StringComparison.OrdinalIgnoreCase)).HavingPriority(3)
                .Responds().WithContent(@"Third")
                .RegisterWith(options)
                .Requests().For((request) => true).HavingPriority(null)
                .Responds().WithContent(@"Fourth")
                .RegisterWith(options);

            using var client = options.CreateHttpClient();

            // Act and Assert
            (await client.GetStringAsync("https://google.com/")).ShouldBe("First");
            (await client.GetStringAsync("https://google.co.uk")).ShouldContain("Second");
            (await client.GetStringAsync("https://example.org/index.html")).ShouldContain("Third");
            (await client.GetStringAsync("https://www.just-eat.co.uk/")).ShouldContain("Fourth");
        }

        [Fact]
        public static async Task Intercept_Http_Requests_Registered_Using_A_Bundle_File()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
                .RegisterBundle("example-bundle.json")
                .ThrowsOnMissingRegistration();

            // Act
            string content;

            using (var client = options.CreateHttpClient())
            {
                content = await client.GetStringAsync("https://www.just-eat.co.uk/");
            }

            // Assert
            content.ShouldBe("<html><head><title>Just Eat</title></head></html>");

            // Act
            using (var client = options.CreateHttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                client.DefaultRequestHeaders.Add("Authorization", "bearer my-token");
                client.DefaultRequestHeaders.Add("User-Agent", "My-App/1.0.0");

                content = await client.GetStringAsync("https://api.github.com/orgs/justeat");
            }

            // Assert
            var organization = JObject.Parse(content);
            organization.Value<int>("id").ShouldBe(1516790);
            organization.Value<string>("login").ShouldBe("justeat");
            organization.Value<string>("url").ShouldBe("https://api.github.com/orgs/justeat");
        }

        [Fact]
        public static async Task Intercept_Http_Get_For_Json_Object_Using_System_Text_Json()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            var builder = new HttpRequestInterceptionBuilder();

            builder.Requests().ForGet().ForHttps().ForHost("public.je-apis.com").ForPath("terms")
                   .Responds().WithJsonContent(new { Id = 1, Link = "https://www.just-eat.co.uk/privacy-policy" })
                   .RegisterWith(options);

            using var client = options.CreateHttpClient();

            // Act
            using var utf8Json = await client.GetStreamAsync("https://public.je-apis.com/terms");

            // Assert
            using var content = await JsonDocument.ParseAsync(utf8Json);
            content.RootElement.GetProperty("Id").GetInt32().ShouldBe(1);
            content.RootElement.GetProperty("Link").GetString().ShouldBe("https://www.just-eat.co.uk/privacy-policy");
        }

        [Fact]
        public static async Task Inject_Latency_For_Http_Get_With_Cancellation()
        {
            // Arrange
            var latency = TimeSpan.FromMilliseconds(50);

            var builder = new HttpRequestInterceptionBuilder()
                .ForHost("www.google.co.uk")
                .WithInterceptionCallback(async (_, token) =>
                {
                    try
                    {
                        await Task.Delay(latency, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignored
                    }
                    finally
                    {
                        // Assert
                        token.IsCancellationRequested.ShouldBeTrue();
                    }
                });

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            using var cts = new CancellationTokenSource(TimeSpan.Zero);

            using var client = options.CreateHttpClient();

            // Act
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => client.GetAsync("http://www.google.co.uk", cts.Token));

            // Assert
            cts.IsCancellationRequested.ShouldBeTrue();
        }

        [Fact]
        public static async Task Simulate_Http_Rate_Limiting()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
                .ThrowsOnMissingRegistration();

            // Keep track of how many HTTP requests to GitHub have been made
            int count = 0;

            static bool IsHttpGetForJustEatGitHubOrg(HttpRequestMessage request)
            {
                return
                    request.Method.Equals(HttpMethod.Get) &&
                    request.RequestUri == new Uri("https://api.github.com/orgs/justeat");
            }

            // Register an HTTP 429 error with a specified priority. The For() delegate
            // is used to match invocation counts that are not divisible by three so that
            // the first, second, fourth, fifth, seventh etc. request returns an error.
            var builder1 = new HttpRequestInterceptionBuilder()
                .For((request) => IsHttpGetForJustEatGitHubOrg(request) && ++count % 3 != 0)
                .HavingPriority(0)
                .Responds()
                .WithStatus(HttpStatusCode.TooManyRequests)
                .WithSystemTextJsonContent(new { message = "Too many requests" })
                .RegisterWith(options);

            // Register another request for an HTTP 200 with no priority that will match
            // if the higher-priority for the HTTP 429 response does not match the request.
            // In practice this will match for the third, sixth, ninth etc. request.
            var builder2 = new HttpRequestInterceptionBuilder()
                .For(IsHttpGetForJustEatGitHubOrg)
                .Responds()
                .WithStatus(HttpStatusCode.OK)
                .WithSystemTextJsonContent(new { id = 1516790, login = "justeat", url = "https://api.github.com/orgs/justeat" })
                .RegisterWith(options);

            var service = RestService.For<IGitHub>(options.CreateHttpClient("https://api.github.com"));

            // Configure a Polly retry policy that will retry an HTTP request up to three times
            // if the HTTP request fails due to an HTTP 429 response from the server.
            int retryCount = 3;

            var policy = Policy
                .Handle<ApiException>((ex) => ex.StatusCode == HttpStatusCode.TooManyRequests)
                .RetryAsync(retryCount);

            // Act
            Organization actual = await policy.ExecuteAsync(() => service.GetOrganizationAsync("justeat"));

            // Assert
            actual.ShouldNotBeNull();
            actual.Id.ShouldBe(1516790);
            actual.Login.ShouldBe("justeat");
            actual.Url.ShouldBe("https://api.github.com/orgs/justeat");

            // Verify that the expected number of attempts were made
            count.ShouldBe(retryCount);
        }
    }
}
