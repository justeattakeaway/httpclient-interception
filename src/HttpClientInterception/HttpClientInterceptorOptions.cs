// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

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
        /// The <see cref="StringComparer"/> to use to key registrations.
        /// </summary>
        private static readonly StringComparer _comparer = StringComparer.Ordinal;

        /// <summary>
        /// The mapped HTTP request interceptors.
        /// </summary>
        private IDictionary<string, HttpInterceptionResponse> _mappings;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientInterceptorOptions"/> class.
        /// </summary>
        public HttpClientInterceptorOptions()
        {
            _mappings = new ConcurrentDictionary<string, HttpInterceptionResponse>(_comparer);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception if a response has not been registered for an HTTP request.
        /// </summary>
        public bool ThrowOnMissingRegistration { get; set; }

        /// <summary>
        /// Begins a new options scope where any changes to the
        /// currently registered interceptions are only persisted
        /// until the returned <see cref="IDisposable"/> is disposed of.
        /// </summary>
        /// <returns>
        /// An <see cref="IDisposable"/> that is used to end the registration scope.
        /// </returns>
        public IDisposable BeginScope() => new OptionsScope(this);

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
        /// Clones the existing <see cref="HttpClientInterceptorOptions"/>.
        /// </summary>
        /// <returns>
        /// A new <see cref="HttpClientInterceptorOptions"/> cloned from the current instance.
        /// </returns>
        public HttpClientInterceptorOptions Clone()
        {
            var clone = new HttpClientInterceptorOptions()
            {
                ThrowOnMissingRegistration = ThrowOnMissingRegistration
            };

            clone._mappings = new ConcurrentDictionary<string, HttpInterceptionResponse>(_mappings, _comparer);

            return clone;
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
            var handler = new InterceptingHttpMessageHandler(this, innerHandler ?? new HttpClientHandler());
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

        private sealed class OptionsScope : IDisposable
        {
            private readonly HttpClientInterceptorOptions _parent;
            private readonly IDictionary<string, HttpInterceptionResponse> _old;
            private readonly IDictionary<string, HttpInterceptionResponse> _new;

            internal OptionsScope(HttpClientInterceptorOptions parent)
            {
                _parent = parent;

                // TODO There might be a nicer way to do this so it's more atomic
                _old = _parent._mappings;
                _parent._mappings = _new = new ConcurrentDictionary<string, HttpInterceptionResponse>(_old, _comparer);
            }

            void IDisposable.Dispose()
            {
                Interlocked.CompareExchange(ref _parent._mappings, _old, _new);
                _new.Clear();
            }
        }
    }
}
