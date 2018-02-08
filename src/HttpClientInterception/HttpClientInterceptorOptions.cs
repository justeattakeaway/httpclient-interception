// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

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
        /// The <see cref="StringComparer"/> to use to key registrations. This field is read-only.
        /// </summary>
        private readonly StringComparer _comparer;

        /// <summary>
        /// The mapped HTTP request interceptors.
        /// </summary>
        private IDictionary<string, HttpInterceptionResponse> _mappings;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientInterceptorOptions"/> class.
        /// </summary>
        /// <remarks>
        /// The path and query string of URIs which are registered are case-sensitive.
        /// </remarks>
        public HttpClientInterceptorOptions()
            : this(caseSensitive: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientInterceptorOptions"/> class.
        /// </summary>
        /// <param name="caseSensitive">Whether registered URIs paths and queries are case-sensitive.</param>
        public HttpClientInterceptorOptions(bool caseSensitive)
        {
            _comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            _mappings = new ConcurrentDictionary<string, HttpInterceptionResponse>(_comparer);
        }

        /// <summary>
        /// Gets or sets an optional delegate to invoke when an HTTP request is sent.
        /// </summary>
        public Func<HttpRequestMessage, Task> OnSend { get; set; }

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
        /// <param name="responseHeaders">The optional HTTP response headers for the response.</param>
        /// <param name="contentHeaders">The optional HTTP response headers for the content.</param>
        /// <param name="onIntercepted">An optional delegate to invoke when the HTTP message is intercepted.</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="method"/>, <paramref name="uri"/> or <paramref name="contentFactory"/> is <see langword="null"/>.
        /// </exception>
        public HttpClientInterceptorOptions Register(
            HttpMethod method,
            Uri uri,
            Func<Task<byte[]>> contentFactory,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = JsonMediaType,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> responseHeaders = null,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> contentHeaders = null,
            Func<HttpRequestMessage, Task> onIntercepted = null)
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
                ContentHeaders = contentHeaders,
                ContentMediaType = mediaType,
                Method = method,
                OnIntercepted = DelegateHelpers.ConvertToBooleanTask(onIntercepted),
                RequestUri = uri,
                ResponseHeaders = responseHeaders,
                StatusCode = statusCode
            };

            string key = BuildKey(method, uri);
            _mappings[key] = interceptor;

            return this;
        }

        /// <summary>
        /// Registers an HTTP request interception, replacing any existing registration.
        /// </summary>
        /// <param name="method">The HTTP method to register an interception for.</param>
        /// <param name="uri">The request URI to register an interception for.</param>
        /// <param name="contentStream">A delegate to a method that returns the response stream.</param>
        /// <param name="statusCode">The optional HTTP status code to return.</param>
        /// <param name="mediaType">The optional media type for the content-type.</param>
        /// <param name="responseHeaders">The optional HTTP response headers for the response.</param>
        /// <param name="contentHeaders">The optional HTTP response headers for the content.</param>
        /// <param name="onIntercepted">An optional delegate to invoke when the HTTP message is intercepted.</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="method"/>, <paramref name="uri"/> or <paramref name="contentStream"/> is <see langword="null"/>.
        /// </exception>
        public HttpClientInterceptorOptions Register(
            HttpMethod method,
            Uri uri,
            Func<Task<Stream>> contentStream,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = JsonMediaType,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> responseHeaders = null,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> contentHeaders = null,
            Func<HttpRequestMessage, Task> onIntercepted = null)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (contentStream == null)
            {
                throw new ArgumentNullException(nameof(contentStream));
            }

            var interceptor = new HttpInterceptionResponse()
            {
                ContentStream = contentStream,
                ContentHeaders = contentHeaders,
                ContentMediaType = mediaType,
                Method = method,
                OnIntercepted = DelegateHelpers.ConvertToBooleanTask(onIntercepted),
                RequestUri = uri,
                ResponseHeaders = responseHeaders,
                StatusCode = statusCode
            };

            string key = BuildKey(method, uri);
            _mappings[key] = interceptor;

            return this;
        }

        /// <summary>
        /// Registers an HTTP request interception, replacing any existing registration.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use to create the registration.</param>
        /// <returns>
        /// The current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public HttpClientInterceptorOptions Register(HttpRequestInterceptionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            HttpInterceptionResponse interceptor = builder.Build();

            string key = BuildKey(interceptor.Method, interceptor.RequestUri, interceptor.IgnoreQuery);
            _mappings[key] = interceptor;

            return this;
        }

        /// <summary>
        /// Gets the HTTP response, if any, set up for the specified HTTP request as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request to try and get the intercepted response for.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that returns the HTTP response to use, if any,
        /// for <paramref name="request"/>; otherwise <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public async virtual Task<HttpResponseMessage> GetResponseAsync(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string key = BuildKey(request.Method, request.RequestUri, ignoreQueryString: false);

            if (!_mappings.TryGetValue(key, out HttpInterceptionResponse options))
            {
                string keyWithoutQueryString = BuildKey(request.Method, request.RequestUri, ignoreQueryString: true);

                if (!_mappings.TryGetValue(keyWithoutQueryString, out options))
                {
                    return null;
                }
            }

            if (options.OnIntercepted != null && !await options.OnIntercepted(request))
            {
                return null;
            }

            var result = new HttpResponseMessage(options.StatusCode);

            try
            {
                result.RequestMessage = request;

                if (options.ReasonPhrase != null)
                {
                    result.ReasonPhrase = options.ReasonPhrase;
                }

                if (options.Version != null)
                {
                    result.Version = options.Version;
                }

                if (options.ContentStream != null)
                {
                    result.Content = new StreamContent(await options.ContentStream() ?? Stream.Null);
                }
                else
                {
                    byte[] content = await options.ContentFactory() ?? Array.Empty<byte>();
                    result.Content = new ByteArrayContent(content);
                }

                if (options.ContentHeaders != null)
                {
                    foreach (var pair in options.ContentHeaders)
                    {
                        result.Content.Headers.Add(pair.Key, pair.Value);
                    }
                }

                result.Content.Headers.ContentType = new MediaTypeHeaderValue(options.ContentMediaType);

                if (options.ResponseHeaders != null)
                {
                    foreach (var pair in options.ResponseHeaders)
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

            return result;
        }

        /// <summary>
        /// Creates a <see cref="DelegatingHandler"/> that uses the interceptors registered for the current instance.
        /// </summary>
        /// <returns>
        /// The <see cref="DelegatingHandler"/> that uses the current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        public virtual DelegatingHandler CreateHttpMessageHandler() => new InterceptingHttpMessageHandler(this);

        /// <summary>
        /// Creates an <see cref="HttpClient"/> that uses the interceptors registered for the current instance.
        /// </summary>
        /// <param name="innerHandler">The optional inner <see cref="HttpMessageHandler"/>.</param>
        /// <returns>
        /// The <see cref="HttpClient"/> that uses the current <see cref="HttpClientInterceptorOptions"/>.
        /// </returns>
        public virtual HttpClient CreateHttpClient(HttpMessageHandler innerHandler = null)
        {
            var handler = new InterceptingHttpMessageHandler(this, innerHandler ?? new HttpClientHandler());
            return new HttpClient(handler, true);
        }

        /// <summary>
        /// Builds the mapping key to use for the specified HTTP request.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="uri">The HTTP request URI.</param>
        /// <param name="ignoreQueryString">If true create a key without any query string but with an extra string to disambiguate.</param>
        /// <returns>
        /// A <see cref="string"/> to use as the key for the interceptor registration.
        /// </returns>
        private static string BuildKey(HttpMethod method, Uri uri, bool ignoreQueryString = false)
        {
            if (ignoreQueryString)
            {
                var uriWithoutQueryString = uri == null ? null : new UriBuilder(uri) { Query = string.Empty }.Uri;

                return $"{method.Method}:IGNOREQUERY:{uriWithoutQueryString}";
            }
            else
            {
                return $"{method.Method}:{uri}";
            }
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
                _parent._mappings = _new = new ConcurrentDictionary<string, HttpInterceptionResponse>(_old, parent._comparer);
            }

            void IDisposable.Dispose()
            {
                Interlocked.CompareExchange(ref _parent._mappings, _old, _new);
                _new.Clear();
            }
        }
    }
}
