// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception
{
    public static class HttpClientInterceptorOptionsTests
    {
        [Fact]
        public static void TryGetResponse_Returns_False_If_Request_Not_Registered()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            var request = new HttpRequestMessage();

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeFalse();
            response.ShouldBeNull();
        }

        [Fact]
        public static void TryGetResponse_Returns_False_If_Request_Method_Does_Not_Match()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
                .Register(HttpMethod.Get, new Uri("https://google.com/"), Array.Empty<byte>);

            var request = new HttpRequestMessage(HttpMethod.Delete, "https://google.com/");

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeFalse();
            response.ShouldBeNull();
        }

        [Fact]
        public static void TryGetResponse_Returns_False_If_Request_Uri_Does_Not_Match()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
                .Register(HttpMethod.Get, new Uri("https://google.co.uk/"), Array.Empty<byte>);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://google.com/");

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeFalse();
            response.ShouldBeNull();
        }

        [Fact]
        public static void TryGetResponse_Returns_False_If_Request_Uri_Does_Not_Match_Blank_Request()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
                .Register(HttpMethod.Get, new Uri("https://google.co.uk/"), Array.Empty<byte>);

            var request = new HttpRequestMessage();

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeFalse();
            response.ShouldBeNull();
        }

        [Fact]
        public static void TryGetResponse_Returns_True_If_Request_Matches()
        {
            // Arrange
            var method = HttpMethod.Get;
            var uri = new Uri("https://google.com/");

            var options = new HttpClientInterceptorOptions()
                .Register(method, uri, Array.Empty<byte>);

            var request = new HttpRequestMessage(method, uri);

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeTrue();
            response.ShouldNotBeNull();
        }

        [Fact]
        public static void TryGetResponse_Returns_Empty_Response_If_ContentFactory_Returns_Null()
        {
            // Arrange
            var method = HttpMethod.Get;
            var uri = new Uri("https://google.com/");

            var options = new HttpClientInterceptorOptions()
                .Register(method, uri, () => null as byte[]);

            var request = new HttpRequestMessage(method, uri);

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeTrue();
            response.ShouldNotBeNull();
            response.Content.ShouldNotBeNull();
            response.Content.Headers?.ContentLength.ShouldBe(0);
            response.Content.Headers?.ContentType?.MediaType.ShouldBe("application/json");
        }

        [Fact]
        public static void TryGetResponse_Returns_Empty_Response_If_Custom_Response_Header()
        {
            // Arrange
            var method = HttpMethod.Get;
            var uri = new Uri("https://google.com/");

            var headers = new Dictionary<string, string>()
            {
                { "a", "b" },
                { "c", "d" },
            };

            var options = new HttpClientInterceptorOptions()
                .Register(method, uri, Array.Empty<byte>, headers: headers);

            var request = new HttpRequestMessage(method, uri);

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeTrue();
            response.ShouldNotBeNull();
            response.Headers.GetValues("a").ShouldBe(new[] { "b" });
            response.Headers.GetValues("c").ShouldBe(new[] { "d" });
        }

        [Fact]
        public static void TryGetResponse_Returns_Empty_Response_If_Custom_Response_Header_Is_Null()
        {
            // Arrange
            var method = HttpMethod.Get;
            var uri = new Uri("https://google.com/");

            IDictionary<string, string> headers = null;

            var options = new HttpClientInterceptorOptions()
                .Register(method, uri, Array.Empty<byte>, headers: headers);

            var request = new HttpRequestMessage(method, uri);

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeTrue();
            response.ShouldNotBeNull();
        }

        [Fact]
        public static void TryGetResponse_Returns_Empty_Response_If_Custom_Response_Headers()
        {
            // Arrange
            var method = HttpMethod.Get;
            var uri = new Uri("https://google.com/");

            var headers = new Dictionary<string, IEnumerable<string>>()
            {
                { "a", new[] { "b" } },
                { "c", new[] { "d", "e" } },
            };

            var options = new HttpClientInterceptorOptions()
                .Register(method, uri, Array.Empty<byte>, headers: headers);

            var request = new HttpRequestMessage(method, uri);

            // Act
            bool actual = options.TryGetResponse(request, out HttpResponseMessage response);

            // Assert
            actual.ShouldBeTrue();
            response.ShouldNotBeNull();
            response.Headers.GetValues("a").ShouldBe(new[] { "b" });
            response.Headers.GetValues("c").ShouldBe(new[] { "d", "e" });
        }

        [Fact]
        public static async Task HttpClient_Registrations_Can_Be_Removed()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
                .RegisterGet("https://google.com/", new { message = "Hello world!" })
                .RegisterGet("https://google.co.uk/", new { message = "Hello world!" });

            // Act and Assert
            await HttpAssert.GetAsync(options, "https://google.com/");
            await HttpAssert.GetAsync(options, "https://google.com/");
            await HttpAssert.GetAsync(options, "https://google.co.uk/");
            await HttpAssert.GetAsync(options, "https://google.co.uk/");

            // Arrange
            options.DeregisterGet("https://google.com/")
                   .DeregisterGet("https://google.com/");

            // Act and Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => HttpAssert.GetAsync(options, "https://google.com/"));
            await Assert.ThrowsAsync<HttpRequestException>(() => HttpAssert.GetAsync(options, "https://google.com/"));
            await HttpAssert.GetAsync(options, "https://google.co.uk/");
            await HttpAssert.GetAsync(options, "https://google.co.uk/");
        }

        [Fact]
        public static async Task Options_Can_Be_Scoped()
        {
            // Arrange
            var url = "https://google.com/";
            var payload = new MyObject() { Message = "Hello world!" };

            var options = new HttpClientInterceptorOptions()
                .RegisterGet(url, payload, statusCode: HttpStatusCode.NotFound);

            // Act - begin first scope
            using (options.BeginScope())
            {
                // Assert - original registration
                await HttpAssert.GetAsync(options, url, HttpStatusCode.NotFound);

                // Arrange - first update to registration
                options.RegisterGet(url, payload, statusCode: HttpStatusCode.InternalServerError);

                // Assert - first updated registration
                await HttpAssert.GetAsync(options, url, HttpStatusCode.InternalServerError);

                // Act - begin second scope
                using (options.BeginScope())
                {
                    // Arrange - second update to registration
                    options.RegisterGet(url, payload, statusCode: HttpStatusCode.RequestTimeout);

                    // Assert - second updated registration
                    await HttpAssert.GetAsync(options, url, HttpStatusCode.RequestTimeout);
                }

                // Assert - first updated registration
                await HttpAssert.GetAsync(options, url, HttpStatusCode.InternalServerError);
            }

            // Assert - original registration
            await HttpAssert.GetAsync(options, url, HttpStatusCode.NotFound);
        }

        [Fact]
        public static async Task Options_Can_Be_Cloned()
        {
            // Arrange
            var url = "https://google.com/";
            var payload = new MyObject() { Message = "Hello world!" };

            var options = new HttpClientInterceptorOptions()
                .RegisterGet(url, payload, statusCode: HttpStatusCode.NotFound);

            // Act
            var clone = options.Clone()
                .RegisterGet(url, payload, statusCode: HttpStatusCode.InternalServerError);

            // Assert
            clone.ThrowOnMissingRegistration.ShouldBe(options.ThrowOnMissingRegistration);
            await HttpAssert.GetAsync(options, url, HttpStatusCode.NotFound);
            await HttpAssert.GetAsync(clone, url, HttpStatusCode.InternalServerError);

            // Arrange
            options.ThrowOnMissingRegistration = true;
            options.Clear();

            // Act and Assert
            clone.ThrowOnMissingRegistration.ShouldNotBe(options.ThrowOnMissingRegistration);
            await Assert.ThrowsAsync<InvalidOperationException>(() => HttpAssert.GetAsync(options, url, HttpStatusCode.InternalServerError));
            await HttpAssert.GetAsync(clone, url, HttpStatusCode.InternalServerError);

            // Arrange
            options = new HttpClientInterceptorOptions()
            {
                ThrowOnMissingRegistration = true,
            };

            // Act
            clone = options.Clone();

            // Assert
            clone.ThrowOnMissingRegistration.ShouldBe(options.ThrowOnMissingRegistration);
        }

        [Fact]
        public static void Deregister_Throws_If_Method_Is_Null()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();

            HttpMethod method = null;
            Uri uri = new Uri("https://www.just-eat.co.uk");

            // Act and Assert
            Assert.Throws<ArgumentNullException>("method", () => options.Deregister(method, uri));
        }

        [Fact]
        public static void Deregister_Throws_If_Uri_Is_Null()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();

            HttpMethod method = HttpMethod.Get;
            Uri uri = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("uri", () => options.Deregister(method, uri));
        }

        [Fact]
        public static void Register_Throws_If_Method_Is_Null()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();

            HttpMethod method = null;
            Uri uri = new Uri("https://www.just-eat.co.uk");

            // Act and Assert
            Assert.Throws<ArgumentNullException>("method", () => options.Register(method, uri, Array.Empty<byte>));
        }

        [Fact]
        public static void Register_Throws_If_Uri_Is_Null()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();

            HttpMethod method = HttpMethod.Get;
            Uri uri = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("uri", () => options.Register(method, uri, Array.Empty<byte>));
        }

        [Fact]
        public static void Register_Throws_If_ContentFactory_Is_Null()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();

            HttpMethod method = HttpMethod.Get;
            Uri uri = new Uri("https://www.just-eat.co.uk");
            Func<byte[]> contentFactory = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("contentFactory", () => options.Register(method, uri, contentFactory));
        }

        [Fact]
        public static void TryGetResponse_Throws_If_Request_Is_Null()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            HttpRequestMessage request = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("request", () => options.TryGetResponse(request, out HttpResponseMessage _));
        }

        [Fact]
        public static void TryGetResponse_Throws_If_Content_Cannot_Be_Created()
        {
            // Arrange
            var method = HttpMethod.Get;
            var uri = new Uri("https://google.com/");

            var options = new HttpClientInterceptorOptions()
                .Register(method, uri, () => throw new NotImplementedException());

            var request = new HttpRequestMessage(method, uri);

            // Act and Assert
            Assert.Throws<NotImplementedException>(() => options.TryGetResponse(request, out HttpResponseMessage _));
        }

        private sealed class MyObject
        {
            public string Message { get; set; }
        }
    }
}
