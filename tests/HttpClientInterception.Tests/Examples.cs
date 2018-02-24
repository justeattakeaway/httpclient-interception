// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JustEat.HttpClientInterception.GitHub;
using Newtonsoft.Json.Linq;
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
        public static async Task Intercept_Http_Get_For_Json_Object()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .ForGet()
                .ForHttps()
                .ForHost("public.je-apis.com")
                .ForPath("terms")
                .WithJsonContent(new { Id = 1, Link = "https://www.just-eat.co.uk/privacy-policy" });

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            string json;

            using (var client = options.CreateHttpClient())
            {
                // Act
                json = await client.GetStringAsync("https://public.je-apis.com/terms");
            }

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

            string html;

            using (var client = options.CreateHttpClient())
            {
                // Act
                html = await client.GetStringAsync("http://www.google.co.uk/search?q=Just+Eat");
            }

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

            byte[] content;

            using (var client = options.CreateHttpClient())
            {
                // Act
                content = await client.GetByteArrayAsync("https://files.domain.com/setup.exe");
            }

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

            HttpStatusCode status;
            string json;

            using (var client = options.CreateHttpClient())
            {
                using (var body = new StringContent(@"{ ""FirstName"": ""John"" }"))
                {
                    // Act
                    using (var response = await client.PostAsync("https://public.je-apis.com/consumer", body))
                    {
                        status = response.StatusCode;
                        json = await response.Content.ReadAsStringAsync();
                    }
                }
            }

            // Assert
            status.ShouldBe(HttpStatusCode.Created);

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
                .WithJsonContent(new { Id = 1, Link = "https://www.just-eat.co.uk/privacy-policy" })
                .WithInterceptionCallback((request) => request.Headers.GetValues("Accept-Tenant").FirstOrDefault() == "uk");

            var options = new HttpClientInterceptorOptions()
                .Register(builder);

            string json;

            using (var client = options.CreateHttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept-Tenant", "uk");

                // Act
                json = await client.GetStringAsync("https://public.je-apis.com/terms");
            }

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
                .ForPost()
                .ForHttps()
                .ForHost("public.je-apis.com")
                .ForPath("consumer")
                .WithStatus(HttpStatusCode.Created)
                .WithContent(@"{ ""id"": 123 }")
                .WithInterceptionCallback(
                    async (request) =>
                    {
                        string requestBody = await request.Content.ReadAsStringAsync();

                        var body = JObject.Parse(requestBody);

                        return body.Value<string>("FirstName") == "John";
                    });

            var options = new HttpClientInterceptorOptions().Register(builder);
            options.ThrowOnMissingRegistration = true;

            HttpStatusCode status;
            string json;

            using (var client = options.CreateHttpClient())
            {
                using (var body = new StringContent(@"{ ""FirstName"": ""John"" }"))
                {
                    // Act
                    using (var response = await client.PostAsync("https://public.je-apis.com/consumer", body))
                    {
                        status = response.StatusCode;
                        json = await response.Content.ReadAsStringAsync();
                    }
                }
            }

            // Assert
            status.ShouldBe(HttpStatusCode.Created);

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

            HttpStatusCode status;
            string json;

            using (var client = options.CreateHttpClient())
            {
                using (var body = new StringContent(@"{ ""User"": { ""DisplayName"": ""John"" } }"))
                {
                    // Act
                    using (var response = await client.PutAsync("https://public.je-apis.com/baskets/123/user", body))
                    {
                        status = response.StatusCode;
                        json = await response.Content.ReadAsStringAsync();
                    }
                }
            }

            // Assert
            status.ShouldBe(HttpStatusCode.Created);

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

            using (var client = options.CreateHttpClient())
            {
                // Act
                using (var response = await client.DeleteAsync("https://public.je-apis.com/baskets/123/orderitems/456"))
                {
                    // Assert
                    response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
                }
            }
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

            byte[] content;

            using (var client = options.CreateHttpClient())
            {
                using (var message = new HttpRequestMessage(new HttpMethod("custom"), "http://custom.domain.com?length=2"))
                {
                    // Act
                    using (var response = await client.SendAsync(message))
                    {
                        content = await response.Content.ReadAsByteArrayAsync();
                    }
                }
            }

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

            using (var client = options.CreateHttpClient())
            {
                // Act and Assert
                await Should.ThrowAsync<HttpRequestException>(() => client.GetStringAsync("http://www.google.co.uk"));
            }
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

            string json;

            using (var client = options.CreateHttpClient())
            {
                // Act
                json = await client.GetStringAsync("http://xunit.github.io/settings.json");
            }

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
            actual.Id.ShouldBe("1516790");
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

            var dotnet = justEat
                .Clone()
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
            justEatOrg.Id.ShouldBe("1516790");
            justEatOrg.Login.ShouldBe("justeat");
            justEatOrg.Url.ShouldBe("https://api.github.com/orgs/justeat");

            // Assert
            dotnetOrg.ShouldNotBeNull();
            dotnetOrg.Id.ShouldBe("9141961");
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
            justEatOrg.Id.ShouldBe("1516790");
            justEatOrg.Login.ShouldBe("justeat");
            justEatOrg.Url.ShouldBe("https://api.github.com/orgs/justeat");

            // Assert
            dotnetOrg.ShouldNotBeNull();
            dotnetOrg.Id.ShouldBe("9141961");
            dotnetOrg.Login.ShouldBe("dotnet");
            dotnetOrg.Url.ShouldBe("https://api.github.com/orgs/dotnet");
        }
    }
}
