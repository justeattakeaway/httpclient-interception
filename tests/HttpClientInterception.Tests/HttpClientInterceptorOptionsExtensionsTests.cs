// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception
{
    public static class HttpClientInterceptorOptionsExtensionsTests
    {
        [Fact]
        public static async Task RegisterGet_For_UriString_With_Defaults_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string[] expected = new[] { "foo", "bar" };

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected);

            // Act
            string[] actual = await AssertGet<string[]>(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_For_Uri_With_Defaults_Registers_Interception()
        {
            // Arrange
            Uri uri = new Uri("https://google.com/");
            string[] expected = new[] { "foo", "bar" };

            var options = new HttpClientInterceptorOptions().RegisterGet(uri, expected);

            // Act
            string[] actual = await AssertGet<string[]>(options, uri.AbsoluteUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_Consumes_Content_By_Reference()
        {
            // Arrange
            string requestUri = "https://google.com/";

            var expected = new CustomObject()
            {
                FavoriteColor = CustomObject.Color.Blue,
                Number = 2,
                Text = "The elephant",
            };

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected);

            // Act
            var actual1 = await AssertGet<CustomObject>(options, requestUri);

            // Assert
            actual1.ShouldNotBeNull();
            actual1.ShouldNotBeSameAs(expected);
            actual1.FavoriteColor.ShouldBe(CustomObject.Color.Blue);
            actual1.Number.ShouldBe(2);
            actual1.Text.ShouldBe("The elephant");

            // Arrange
            expected.FavoriteColor = CustomObject.Color.Red;
            expected.Number = 42;
            expected.Text = "L'éléphant";

            // Act
            var actual2 = await AssertGet<CustomObject>(options, requestUri);

            // Assert
            actual2.ShouldNotBeNull();
            actual2.ShouldNotBeSameAs(actual1);
            actual2.FavoriteColor.ShouldBe(CustomObject.Color.Red);
            actual2.Number.ShouldBe(42);
            actual2.Text.ShouldBe("L'éléphant");
        }

        [Fact]
        public static async Task RegisterGet_Uses_Default_Json_Serializer()
        {
            // Arrange
            string requestUri = "https://google.com/";
            var expected = new { mode = EventResetMode.ManualReset };

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected);

            // Act
            string actual = await AssertGet(options, requestUri);

            // Assert
            actual.Equals(@"{""mode"":1}");
        }

        [Fact]
        public static async Task RegisterGet_Uses_Specified_Json_Serializer()
        {
            // Arrange
            var uri = new Uri("https://google.com/");
            var expected = new { mode = EventResetMode.ManualReset };

            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.Converters.Add(new StringEnumConverter());

            var options = new HttpClientInterceptorOptions()
                .RegisterGet(uri, expected, serializerSettings: serializerSettings);

            // Act
            string actual = await AssertGet(options, uri.AbsoluteUri);

            // Assert
            actual.Equals(@"{""mode"":""ManualReset""}");
        }

        [Fact]
        public static async Task RegisterGet_For_Plaintext_String_With_Custom_Status_Code_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            var statusCode = HttpStatusCode.Accepted;

            string expected = "foo";

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected, statusCode);

            // Act
            string actual = await AssertGet(options, requestUri, statusCode);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_For_Html_With_Custom_Media_Type_Registers_Interception()
        {
            // Arrange
            var uri = new Uri("https://google.com/");
            string mediaType = "text/html";
            string expected = "<html>foo></html>";

            var options = new HttpClientInterceptorOptions().RegisterGet(uri, expected, mediaType: mediaType);

            // Act
            string actual = await AssertGet(options, uri.AbsoluteUri, mediaType: mediaType);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_If_Content_String_Is_Null_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string content = null;
            string expected = string.Empty;

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, content);

            // Act
            string actual = await AssertGet(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_For_Raw_Bytes_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            Func<byte[]> contentFactory = () => new byte[] { 46, 78, 69, 84 };

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, contentFactory);

            // Act
            string actual = await AssertGet(options, requestUri);

            // Assert
            actual.ShouldBe(".NET");
        }

        [Fact]
        public static void RegisterGet_Throws_If_Content_Is_Null()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            object content = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("content", () => options.RegisterGet("https://google.com", content));
            Assert.Throws<ArgumentNullException>("content", () => options.RegisterGet(new Uri("https://google.com"), content));
        }

        private static async Task<T> AssertGet<T>(
            HttpClientInterceptorOptions target,
            string requestUri,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            string json = await AssertGet(target, requestUri, statusCode);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private static async Task<string> AssertGet(
            HttpClientInterceptorOptions target,
            string requestUri,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = null)
        {
            using (var httpClient = target.CreateHttpClient(ErroringHandler.Handler))
            {
                using (var response = await httpClient.GetAsync(requestUri))
                {
                    response.StatusCode.ShouldBe(statusCode);

                    if (mediaType != null)
                    {
                        response.Content.Headers.ContentType.MediaType.ShouldBe(mediaType);
                    }

                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        private sealed class ErroringHandler : HttpMessageHandler
        {
            internal static readonly ErroringHandler Handler = new ErroringHandler();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException("HTTP request was not intercepted.");
            }
        }

        private sealed class CustomObject
        {
            internal enum Color
            {
                Red = 1,
                Blue = 2,
            }

            public Color FavoriteColor { get; set; }

            public int Number { get; set; }

            public string Text { get; set; }
        }
    }
}
