// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing options for configuring an intercepting <see cref="HttpMessageHandler"/>.
    /// </summary>
    public class HttpClientInterceptorOptions
    {
        /// <summary>
        /// The media type to use for JSON.
        /// </summary>
        internal const string JsonMediaType = "application/json";

        /// <summary>
        /// The mapped HTTP request interceptors. This field is read-only.
        /// </summary>
        private readonly IDictionary<string, HttpInterceptionResponse> _mappings = new ConcurrentDictionary<string, HttpInterceptionResponse>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception if a response has not been registered for an HTTP request.
        /// </summary>
        public bool ThrowOnMissingRegistration { get; set; }

        /// <summary>
        /// Clears any and all existing registrations.
        /// </summary>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        public HttpClientInterceptorOptions Clear()
        {
            _mappings.Clear();
            return this;
        }

        /// <summary>
        /// Deregisters an existing HTTP request interception, if it exists.
        /// </summary>
        /// <param name="method">The HTTP method to deregister.</param>
        /// <param name="uri">The request URI to deregister.</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="method"/> or <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public HttpClientInterceptorOptions Deregister(HttpMethod method, Uri uri)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string key = BuildKey(method, uri);
            _mappings.Remove(key);

            return this;
        }

        /// <summary>
        /// Registers an HTTP request interception, replacing any existing registration.
        /// </summary>
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
        /// <paramref name="method"/>, <paramref name="uri"/> or <paramref name="contentFactory"/> is <see langword="null"/>.
        /// </exception>
        public HttpClientInterceptorOptions Register(
            HttpMethod method,
            Uri uri,
            Func<byte[]> contentFactory,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = JsonMediaType,
            IReadOnlyDictionary<string, string> headers = null)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (contentFactory == null)
            {
                throw new ArgumentNullException(nameof(contentFactory));
            }

            var interceptor = new HttpInterceptionResponse()
            {
                ContentFactory = contentFactory,
                ContentMediaType = mediaType,
                Headers = headers,
                Method = method,
                RequestUri = uri,
                StatusCode = statusCode
            };

            string key = BuildKey(method, uri);
            _mappings[key] = interceptor;

            return this;
        }

        /// <summary>
        /// Tries to fetch the HTTP response, if any, set up for the specified HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request to try and get the intercepted response for.</param>
        /// <param name="response">
        /// When the method returns, contains the HTTP response to use, if any; otherwise <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an HTTP response was registered for <paramref name="request"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public bool TryGetResponse(HttpRequestMessage request, out HttpResponseMessage response)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string key = BuildKey(request.Method, request.RequestUri);

            if (!_mappings.TryGetValue(key, out HttpInterceptionResponse options))
            {
                response = null;
                return false;
            }

            var result = new HttpResponseMessage(options.StatusCode);

            try
            {
                byte[] content = options.ContentFactory() ?? Array.Empty<byte>();

                result.Content = new ByteArrayContent(content);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(options.ContentMediaType);

                if (options.Headers?.Count > 0)
                {
                    foreach (var pair in options.Headers)
                    {
                        result.Headers.Add(pair.Key, pair.Value);
                    }
                }
            }
            catch (Exception)
            {
                result.Dispose();
                throw;
            }

            response = result;
            return true;
        }

        /// <summary>
        /// Creates a <see cref="DelegatingHandler"/> that uses the interceptors registered for the current instance.
        /// </summary>
        /// <returns>
        /// The <see cref="DelegatingHandler"/> that uses the current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        public DelegatingHandler CreateHttpMessageHandler() => new InterceptingHttpMessageHandler(this);

        /// <summary>
        /// Creates an <see cref="HttpClient"/> that uses the interceptors registered for the current instance.
        /// </summary>
        /// <param name="innerHandler">The optional inner <see cref="HttpMessageHandler"/>.</param>
        /// <returns>
        /// The <see cref="HttpClient"/> that uses the current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        public HttpClient CreateHttpClient(HttpMessageHandler innerHandler = null)
        {
            var handler = CreateHttpMessageHandler();

            try
            {
                handler.InnerHandler = innerHandler ?? new HttpClientHandler();
            }
            catch (Exception)
            {
                handler.Dispose();
            }

            return new HttpClient(handler, true);
        }

        /// <summary>
        /// Builds the mapping key to use for the specified HTTP request.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="uri">The HTTP request URI.</param>
        /// <returns>
        /// A <see cref="string"/> to use as the key for the interceptor registration.
        /// </returns>
        private static string BuildKey(HttpMethod method, Uri uri) => $"{method?.Method}:{uri}";

        private sealed class HttpInterceptionResponse
        {
            internal HttpMethod Method { get; set; }

            internal Uri RequestUri { get; set; }

            internal HttpStatusCode StatusCode { get; set; }

            internal Func<byte[]> ContentFactory { get; set; }

            internal string ContentMediaType { get; set; }

            internal IReadOnlyDictionary<string, string> Headers { get; set; }
        }
    }
}
