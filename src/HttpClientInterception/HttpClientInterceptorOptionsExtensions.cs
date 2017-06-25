// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing extenion methods for the <see cref="HttpClientInterceptorOptions"/> class. This class cannot be inherited.
    /// </summary>
    public static class HttpClientInterceptorOptionsExtensions
    {
        /// <summary>
        /// Registers an HTTP request interception, replacing any existing registration.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="method">The HTTP method to register an interception for.</param>
        /// <param name="uri">The request URI to register an interception for.</param>
        /// <param name="contentFactory">A delegate to a method that returns the raw response content.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="mediaType">The optional media type for the content-type.</param>
        /// <param name="headers">The optional HTTP response headers.</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/>, <paramref name="method"/>, <paramref name="uri"/> or
        /// <paramref name="contentFactory"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions Register(
            this HttpClientInterceptorOptions options,
            HttpMethod method,
            Uri uri,
            Func<byte[]> contentFactory,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = HttpClientInterceptorOptions.JsonMediaType,
            IEnumerable<KeyValuePair<string, string>> headers = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            IDictionary<string, IEnumerable<string>> multivalueHeaders = null;

            if (headers != null)
            {
                multivalueHeaders = new Dictionary<string, IEnumerable<string>>();

                foreach (var pair in headers)
                {
                    multivalueHeaders[pair.Key] = new[] { pair.Value };
                }
            }

            return options.Register(
                method,
                uri,
                contentFactory,
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
        public static HttpClientInterceptorOptions RegisterGet(
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

            Func<byte[]> contentFactory = () =>
            {
                string json = JsonConvert.SerializeObject(content);
                return Encoding.UTF8.GetBytes(json);
            };

            return options.Register(HttpMethod.Get, new Uri(uriString), contentFactory, statusCode);
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

            return options.Register(HttpMethod.Get, new Uri(uriString), () => Encoding.UTF8.GetBytes(content ?? string.Empty), statusCode, mediaType);
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
    }
}
