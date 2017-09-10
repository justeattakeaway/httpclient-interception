// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception
{
    public static class HttpClientInterceptorOptionsExtensionsTests
    {
        [Fact]
        public static async Task RegisterGet_For_With_Defaults_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string[] expected = new[] { "foo", "bar" };

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected);

            // Act
            string[] actual = await HttpAssert.GetAsync<string[]>(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_For_Plaintext_String_With_Custom_Status_Code_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string expected = "foo";

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_For_Html_With_Custom_Media_Type_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string mediaType = "text/html";
            string expected = "<html>foo></html>";

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected, mediaType: mediaType);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri, mediaType: mediaType);

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
            string actual = await HttpAssert.GetAsync(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static void RegisterGet_Validates_Parameters_For_Null()
        {
            // Arrange
            HttpClientInterceptorOptions options = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.RegisterGet("https://google.com", new { }));
            Assert.Throws<ArgumentNullException>("options", () => options.RegisterGet("https://google.com", string.Empty));

            // Arrange
            options = new HttpClientInterceptorOptions();

            // Act and Assert
            Assert.Throws<ArgumentNullException>("content", () => options.RegisterGet("https://google.com", null as object));
        }

        [Fact]
        public static void DeregisterGet_Throws_If_Options_Is_Null()
        {
            // Arrange
            HttpClientInterceptorOptions options = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.DeregisterGet("https://google.com"));
        }

        [Fact]
        public static void Register_Throws_If_Options_Is_Null()
        {
            // Arrange
            var method = HttpMethod.Get;
            var uri = new Uri("https://google.com");

            HttpClientInterceptorOptions options = null;
            IEnumerable<KeyValuePair<string, string>> headers = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.Register(method, uri, Array.Empty<byte>, responseHeaders: headers));
        }

        [Fact]
        public static void CreateHttpClient_Throws_If_Options_Is_Null()
        {
            // Arrange
            var baseAddress = new Uri("https://google.com");

            HttpClientInterceptorOptions options = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.CreateHttpClient(baseAddress));
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
