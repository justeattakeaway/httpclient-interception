// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing extention methods for the <see cref="HttpClientInterceptorOptions"/> class. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpClientInterceptorOptionsExtensions
    {
        /// <summary>
        /// Creates an <see cref="HttpClient"/> that uses the interceptors registered for the specified options and base address.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="baseAddress">The base address to use for the created HTTP client.</param>
        /// <returns>
        /// The <see cref="HttpClient"/> that uses the specified <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClient CreateHttpClient(this HttpClientInterceptorOptions options, string baseAddress)
        {
            var baseAddressUri = new Uri(baseAddress, UriKind.RelativeOrAbsolute);
            return options.CreateHttpClient(baseAddressUri);
        }

        /// <summary>
        /// Creates an <see cref="HttpClient"/> that uses the interceptors registered for the specified options and base address.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="baseAddress">The base address to use for the created HTTP client.</param>
        /// <param name="innerHandler">The optional inner <see cref="HttpMessageHandler"/>.</param>
        /// <returns>
        /// The <see cref="HttpClient"/> that uses the specified <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClient CreateHttpClient(this HttpClientInterceptorOptions options, Uri baseAddress, HttpMessageHandler? innerHandler = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var client = options.CreateHttpClient(innerHandler);

            client.BaseAddress = baseAddress;

            return client;
        }

        /// <summary>
        /// Registers an HTTP request interception, replacing any existing registration.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="method">The HTTP method to register an interception for.</param>
        /// <param name="uri">The request URI to register an interception for.</param>
        /// <param name="contentFactory">A delegate to a method that returns the raw response content.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="mediaType">The optional media type for the content-type.</param>
        /// <param name="responseHeaders">The optional HTTP response headers.</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/>, <paramref name="method"/>, <paramref name="uri"/> or
        /// <paramref name="contentFactory"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions RegisterByteArray(
            this HttpClientInterceptorOptions options,
            HttpMethod method,
            Uri uri,
            Func<byte[]> contentFactory,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = HttpClientInterceptorOptions.JsonMediaType,
            IEnumerable<KeyValuePair<string, string>>? responseHeaders = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (contentFactory == null)
            {
                throw new ArgumentNullException(nameof(contentFactory));
            }

            IDictionary<string, IEnumerable<string>>? multivalueHeaders = null;

            if (responseHeaders != null)
            {
                multivalueHeaders = new Dictionary<string, IEnumerable<string>>();

                foreach (var pair in responseHeaders)
                {
                    multivalueHeaders[pair.Key] = new[] { pair.Value };
                }
            }

            return options.RegisterByteArray(
                method,
                uri,
                () => Task.FromResult(contentFactory()),
                statusCode,
                mediaType,
                multivalueHeaders);
        }

        /// <summary>
        /// Registers an HTTP request interception, replacing any existing registration.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="method">The HTTP method to register an interception for.</param>
        /// <param name="uri">The request URI to register an interception for.</param>
        /// <param name="contentStream">A delegate to a method that returns the response stream.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="mediaType">The optional media type for the content-type.</param>
        /// <param name="responseHeaders">The optional HTTP response headers.</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/>, <paramref name="method"/>, <paramref name="uri"/> or
        /// <paramref name="contentStream"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions RegisterStream(
            this HttpClientInterceptorOptions options,
            HttpMethod method,
            Uri uri,
            Func<Stream> contentStream,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = HttpClientInterceptorOptions.JsonMediaType,
            IEnumerable<KeyValuePair<string, string>>? responseHeaders = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (contentStream == null)
            {
                throw new ArgumentNullException(nameof(contentStream));
            }

            IDictionary<string, IEnumerable<string>>? multivalueHeaders = null;

            if (responseHeaders != null)
            {
                multivalueHeaders = new Dictionary<string, IEnumerable<string>>();

                foreach (var pair in responseHeaders)
                {
                    multivalueHeaders[pair.Key] = new[] { pair.Value };
                }
            }

            return options.RegisterStream(
                method,
                uri,
                () => Task.FromResult(contentStream()),
                statusCode,
                mediaType,
                multivalueHeaders);
        }

        /// <summary>
        /// Registers an HTTP GET request for the specified JSON content.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uriString">The request URL.</param>
        /// <param name="content">The object to serialize as JSON as the content.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> or <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions RegisterGetJson(
            this HttpClientInterceptorOptions options,
            string uriString,
            object content,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            byte[] ContentFactory()
            {
                string json = JsonConvert.SerializeObject(content);
                return Encoding.UTF8.GetBytes(json);
            }

            return options.RegisterByteArray(HttpMethod.Get, new Uri(uriString), ContentFactory, statusCode);
        }

        /// <summary>
        /// Registers an HTTP GET request for the specified string.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uriString">The request URL.</param>
        /// <param name="content">The UTF-8 encoded string to use as the content.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="mediaType">The optional media type for the content-type.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions RegisterGet(
            this HttpClientInterceptorOptions options,
            string uriString,
            string content,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = HttpClientInterceptorOptions.JsonMediaType)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return options.RegisterByteArray(HttpMethod.Get, new Uri(uriString), () => Encoding.UTF8.GetBytes(content ?? string.Empty), statusCode, mediaType);
        }

        /// <summary>
        /// Deregisters an HTTP GET request.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uriString">The request URL.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions DeregisterGet(this HttpClientInterceptorOptions options, string uriString)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return options.Deregister(HttpMethod.Get, new Uri(uriString));
        }

        /// <summary>
        /// Registers one or more HTTP request interceptions, replacing any existing registrations.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="collection">A collection of <see cref="HttpRequestInterceptionBuilder"/> instances to use to create the registration(s).</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> or <paramref name="collection"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions Register(
            this HttpClientInterceptorOptions options,
            IEnumerable<HttpRequestInterceptionBuilder> collection)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (var builder in collection)
            {
                options.Register(builder);
            }

            return options;
        }

        /// <summary>
        /// Registers one or more HTTP request interceptions, replacing any existing registrations.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="collection">An array of one or more <see cref="HttpRequestInterceptionBuilder"/> instances to use to create the registration(s).</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> or <paramref name="collection"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions Register(
            this HttpClientInterceptorOptions options,
            params HttpRequestInterceptionBuilder[] collection)
        {
            return options.Register((IEnumerable<HttpRequestInterceptionBuilder>)collection);
        }

        /// <summary>
        /// Configures the <see cref="HttpClientInterceptorOptions"/> to throw an exception if a response has not been registered for an HTTP request.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to configure.</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions ThrowsOnMissingRegistration(this HttpClientInterceptorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.ThrowOnMissingRegistration = true;
            return options;
        }
    }
}
