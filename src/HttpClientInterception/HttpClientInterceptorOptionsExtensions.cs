// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
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
        /// <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions RegisterGet(
            this HttpClientInterceptorOptions options,
            string uriString,
            object content,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return options.RegisterGet(new Uri(uriString), content, statusCode);
        }

        /// <summary>
        /// Registers an HTTP GET request for the specified JSON object.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uri">The request URI.</param>
        /// <param name="content">The object to serialize as JSON as the content.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="serializerSettings">The optional settings to use to serialize <paramref name="content"/> as JSON.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        public static HttpClientInterceptorOptions RegisterGet(
            this HttpClientInterceptorOptions options,
            Uri uri,
            object content,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            JsonSerializerSettings serializerSettings = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Func<byte[]> contentFactory = () =>
            {
                string json = JsonConvert.SerializeObject(content, serializerSettings ?? new JsonSerializerSettings());
                return Encoding.UTF8.GetBytes(json);
            };

            return options.RegisterGet(uri, contentFactory, statusCode);
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
        public static HttpClientInterceptorOptions RegisterGet(
            this HttpClientInterceptorOptions options,
            string uriString,
            string content,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = HttpClientInterceptorOptions.JsonMediaType)
        {
            return options.RegisterGet(new Uri(uriString), content, statusCode, mediaType);
        }

        /// <summary>
        /// Registers an HTTP GET request for the specified string.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uri">The request URI.</param>
        /// <param name="content">The UTF-8 encoded string to use as the content.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="mediaType">The optional media type for the content-type.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        public static HttpClientInterceptorOptions RegisterGet(
            this HttpClientInterceptorOptions options,
            Uri uri,
            string content,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = HttpClientInterceptorOptions.JsonMediaType)
        {
            return options.RegisterGet(uri, () => Encoding.UTF8.GetBytes(content ?? string.Empty), statusCode, mediaType);
        }

        /// <summary>
        /// Registers an HTTP GET request for the specified byte content.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uriString">The request URL.</param>
        /// <param name="contentFactory">A delegate to a method that returns the raw response content.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="mediaType">The optional media type for the content-type.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        public static HttpClientInterceptorOptions RegisterGet(
            this HttpClientInterceptorOptions options,
            string uriString,
            Func<byte[]> contentFactory,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = HttpClientInterceptorOptions.JsonMediaType)
        {
            return options.RegisterGet(new Uri(uriString), contentFactory, statusCode, mediaType);
        }

        /// <summary>
        /// Registers an HTTP GET request the specified byte content.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uri">The request URI.</param>
        /// <param name="contentFactory">A delegate to a method that returns the raw response content.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="mediaType">The optional media type for the content-type.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        public static HttpClientInterceptorOptions RegisterGet(
            this HttpClientInterceptorOptions options,
            Uri uri,
            Func<byte[]> contentFactory,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = HttpClientInterceptorOptions.JsonMediaType)
        {
            return options.Register(HttpMethod.Get, uri, contentFactory, statusCode, mediaType);
        }

        /// <summary>
        /// Deregisters an HTTP GET request.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uriString">The request URL.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        public static HttpClientInterceptorOptions DeregisterGet(this HttpClientInterceptorOptions options, string uriString)
        {
            return options.DeregisterGet(new Uri(uriString));
        }

        /// <summary>
        /// Deregisters an HTTP GET request.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to set up.</param>
        /// <param name="uri">The request URI.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        public static HttpClientInterceptorOptions DeregisterGet(this HttpClientInterceptorOptions options, Uri uri)
        {
            return options.Deregister(HttpMethod.Get, uri);
        }
    }
}
