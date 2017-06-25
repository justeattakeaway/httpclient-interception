// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception
{
    public static class HttpClientInterceptorOptionsTests
    {
        [Fact]
        public static async Task HttpClient_Requests_Can_Be_Intercepted()
        {
            string url = "https://google.com/";

            var payload = new MyObject()
            {
                Message = "Hello world!"
            };

            var options = new HttpClientInterceptorOptions();

            options.RegisterGet(url, payload, statusCode: HttpStatusCode.NotFound);

            using (var httpClient = options.CreateHttpClient())
            {
                using (var response = await httpClient.GetAsync(url))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
                    response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");

                    string json = await response.Content.ReadAsStringAsync();
                    json.ShouldBe(@"{""Message"":""Hello world!""}");
                }

                payload.Message = "Bonjour tout le monde!";

                using (var response = await httpClient.GetAsync(url))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
                    response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");

                    string json = await response.Content.ReadAsStringAsync();
                    json.ShouldBe(@"{""Message"":""Bonjour tout le monde!""}");
                }
            }

            options.Clear();

            using (var httpClient = options.CreateHttpClient())
            {
                using (var response = await httpClient.GetAsync(url))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    response.Content.Headers.ContentType.MediaType.ShouldBe("text/html");
                }
            }

            options.RegisterGet(
                url,
                "<html>Hello world!</html>",
                mediaType: "text/html");

            using (var httpClient = options.CreateHttpClient())
            {
                using (var response = await httpClient.GetAsync(url))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    response.Content.Headers.ContentType.MediaType.ShouldBe("text/html");

                    string html = await response.Content.ReadAsStringAsync();
                    html.ShouldBe(@"<html>Hello world!</html>");
                }
            }

            options.DeregisterGet(url);

            using (var httpClient = options.CreateHttpClient())
            {
                using (var response = await httpClient.GetAsync(url))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    response.Content.Headers.ContentType.MediaType.ShouldBe("text/html");

                    string html = await response.Content.ReadAsStringAsync();
                    html.ShouldNotBe(@"<html>Hello world!</html>");
                }
            }
        }

        private sealed class MyObject
        {
            public string Message { get; set; }
        }
    }
}
