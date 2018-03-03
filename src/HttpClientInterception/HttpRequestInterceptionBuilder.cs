// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class that can be used to build HTTP requests for interception.
    /// </summary>
    public class HttpRequestInterceptionBuilder
    {
        private static readonly Task<byte[]> EmptyContent = Task.FromResult(Array.Empty<byte>());

        private static readonly Func<Task<byte[]>> EmptyContentFactory = () => EmptyContent;

        private Func<Task<byte[]>> _contentFactory;

        private Func<Task<Stream>> _contentStream;

        private IDictionary<string, ICollection<string>> _contentHeaders;

        private IDictionary<string, ICollection<string>> _responseHeaders;

        private string _mediaType = HttpClientInterceptorOptions.JsonMediaType;

        private HttpMethod _method = HttpMethod.Get;

        private Func<HttpRequestMessage, Task<bool>> _onIntercepted;

        private Predicate<HttpRequestMessage> _requestMatcher;

        private string _reasonPhrase;

        private HttpStatusCode _statusCode = HttpStatusCode.OK;

        private UriBuilder _uriBuilder = new UriBuilder();

        private Version _version;

        private bool _ignoreHost;

        private bool _ignoreQuery;

        /// <summary>
        /// Configures the builder to match any request that meets the criteria defined by the specified predicate.
        /// </summary>
        /// <param name="predicate">
        /// A delegate to a method which returns <see langword="true"/> if the
        /// request is considered a match; otherwise <see langword="false"/>.
        /// </param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        /// <remarks>
        /// Pass a value of <see langword="null"/> to remove a previously-registered custom request matching predicate.
        /// </remarks>
        public HttpRequestInterceptionBuilder For(Predicate<HttpRequestMessage> predicate)
        {
            _requestMatcher = predicate;
            return this;
        }

        /// <summary>
        /// Configures the builder to match any host name.
        /// </summary>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder ForAnyHost()
        {
            _ignoreHost = true;
            return this;
        }

        /// <summary>
        /// Sets the HTTP method to intercept a request for.
        /// </summary>
        /// <param name="method">The HTTP method to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="method"/> is <see langword="null"/>.
        /// </exception>
        public HttpRequestInterceptionBuilder ForMethod(HttpMethod method)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
            return this;
        }

        /// <summary>
        /// Sets the scheme of the request URI to intercept a request for.
        /// </summary>
        /// <param name="scheme">The request URI scheme to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder ForScheme(string scheme)
        {
            _uriBuilder.Scheme = scheme;
            return this;
        }

        /// <summary>
        /// Sets the host name of the request URI to intercept a request for.
        /// </summary>
        /// <param name="host">The request URI host name to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder ForHost(string host)
        {
            _uriBuilder.Host = host;
            _ignoreHost = false;
            return this;
        }

        /// <summary>
        /// Sets the port number of the request URI to intercept a request for.
        /// </summary>
        /// <param name="port">The request URI port number to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder ForPort(int port)
        {
            _uriBuilder.Port = port;
            return this;
        }

        /// <summary>
        /// Sets the path of the request URI to intercept a request for.
        /// </summary>
        /// <param name="path">The request URI path to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder ForPath(string path)
        {
            _uriBuilder.Path = path;
            return this;
        }

        /// <summary>
        /// Sets the query of the request URI to intercept a request for.
        /// </summary>
        /// <param name="query">The request URI query to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder ForQuery(string query)
        {
            _uriBuilder.Query = query;
            return this;
        }

        /// <summary>
        /// If true query strings will be ignored when testing request URIs.
        /// </summary>
        /// <param name="ignoreQuery">Whether to ignore query strings or not; defaults to true.</param>
        /// <returns>The current <see cref="HttpRequestInterceptionBuilder"/>.</returns>
        public HttpRequestInterceptionBuilder IgnoringQuery(bool ignoreQuery = true)
        {
            _ignoreQuery = ignoreQuery;
            return this;
        }

        /// <summary>
        /// Sets the request URI to intercept a request for.
        /// </summary>
        /// <param name="uri">The request URI to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder ForUri(Uri uri)
        {
            _uriBuilder = new UriBuilder(uri);
            _ignoreHost = false;
            return this;
        }

        /// <summary>
        /// Sets a builder for the request URI to intercept a request for.
        /// </summary>
        /// <param name="uriBuilder">The build for the request URI to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="uriBuilder"/> is <see langword="null"/>.
        /// </exception>
        public HttpRequestInterceptionBuilder ForUri(UriBuilder uriBuilder)
        {
            _uriBuilder = uriBuilder ?? throw new ArgumentNullException(nameof(uriBuilder));
            _ignoreHost = false;
            return this;
        }

        /// <summary>
        /// Sets the function to use to build the response content.
        /// </summary>
        /// <param name="contentFactory">A delegate to a method that returns the raw response content.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContent(Func<byte[]> contentFactory)
        {
            if (contentFactory == null)
            {
                _contentFactory = null;
            }
            else
            {
                _contentFactory = () => Task.FromResult(contentFactory());
                _contentStream = null;
            }

            return this;
        }

        /// <summary>
        /// Sets the function to use to asynchronously build the response content.
        /// </summary>
        /// <param name="contentFactory">A delegate to a method that returns the raw response content asynchronously.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContent(Func<Task<byte[]>> contentFactory)
        {
            _contentFactory = contentFactory;

            // Remove the stream if setting the factory
            if (_contentFactory != null)
            {
                _contentStream = null;
            }

            return this;
        }

        /// <summary>
        /// Sets the function to use to build the response stream.
        /// </summary>
        /// <param name="contentStream">A delegate to a method that returns the response stream for the content.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContentStream(Func<Stream> contentStream)
        {
            if (contentStream == null)
            {
                _contentStream = null;
            }
            else
            {
                _contentStream = () => Task.FromResult(contentStream());
                _contentFactory = null;
            }

            return this;
        }

        /// <summary>
        /// Sets the function to use to asynchronously build the response stream.
        /// </summary>
        /// <param name="contentStream">A delegate to a method that returns the response stream for the content asynchronously.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContentStream(Func<Task<Stream>> contentStream)
        {
            _contentStream = contentStream;

            // Remove the factory if setting the stream
            if (_contentStream != null)
            {
                _contentFactory = null;
            }

            return this;
        }

        /// <summary>
        /// Sets a custom HTTP content header to use with a single value.
        /// </summary>
        /// <param name="name">The name of the custom HTTP content header.</param>
        /// <param name="value">The value for the content header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContentHeader(string name, string value) => WithContentHeader(name, new[] { value });

        /// <summary>
        /// Sets a custom HTTP content header to use with multiple values.
        /// </summary>
        /// <param name="name">The name of the custom HTTP content header.</param>
        /// <param name="values">The values for the content header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContentHeader(string name, params string[] values)
        {
            return WithContentHeader(name, values as IEnumerable<string>);
        }

        /// <summary>
        /// Sets a custom HTTP content header to use with multiple values.
        /// </summary>
        /// <param name="name">The name of the custom HTTP content header.</param>
        /// <param name="values">The values for the content header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContentHeader(string name, IEnumerable<string> values)
        {
            if (_contentHeaders == null)
            {
                _contentHeaders = new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);
            }

            if (!_contentHeaders.TryGetValue(name, out ICollection<string> current))
            {
                _contentHeaders[name] = current = new List<string>();
            }

            current.Clear();

            foreach (string value in values)
            {
                current.Add(value);
            }

            return this;
        }

        /// <summary>
        /// Sets any custom HTTP content headers to use.
        /// </summary>
        /// <param name="headers">Any custom HTTP content headers to use.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContentHeaders(IDictionary<string, ICollection<string>> headers)
        {
            _contentHeaders = new Dictionary<string, ICollection<string>>(headers, StringComparer.OrdinalIgnoreCase);
            return this;
        }

        /// <summary>
        /// Sets any custom HTTP content headers to use.
        /// </summary>
        /// <param name="headers">Any custom HTTP content headers to use.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithContentHeaders(IDictionary<string, string> headers)
        {
            var copy = new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in headers)
            {
                copy[pair.Key] = new[] { pair.Value };
            }

            _contentHeaders = copy;

            return this;
        }

        /// <summary>
        /// Sets a custom HTTP response header to use with a single value.
        /// </summary>
        /// <param name="name">The name of the custom HTTP response header.</param>
        /// <param name="value">The value for the response header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithResponseHeader(string name, string value) => WithResponseHeader(name, new[] { value });

        /// <summary>
        /// Sets a custom HTTP response header to use with multiple values.
        /// </summary>
        /// <param name="name">The name of the custom HTTP response header.</param>
        /// <param name="values">The values for the response header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithResponseHeader(string name, params string[] values)
        {
            return WithResponseHeader(name, values as IEnumerable<string>);
        }

        /// <summary>
        /// Sets a custom HTTP response header to use with multiple values.
        /// </summary>
        /// <param name="name">The name of the custom HTTP response header.</param>
        /// <param name="values">The values for the response header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithResponseHeader(string name, IEnumerable<string> values)
        {
            if (_responseHeaders == null)
            {
                _responseHeaders = new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);
            }

            if (!_responseHeaders.TryGetValue(name, out ICollection<string> current))
            {
                _responseHeaders[name] = current = new List<string>();
            }

            current.Clear();

            foreach (string value in values)
            {
                current.Add(value);
            }

            return this;
        }

        /// <summary>
        /// Sets any custom HTTP response headers to use.
        /// </summary>
        /// <param name="headers">Any custom HTTP response headers to use.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithResponseHeaders(IDictionary<string, string> headers)
        {
            var copy = new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in headers)
            {
                copy[pair.Key] = new[] { pair.Value };
            }

            _responseHeaders = copy;

            return this;
        }

        /// <summary>
        /// Sets any custom HTTP response headers to use.
        /// </summary>
        /// <param name="headers">Any custom HTTP response headers to use.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithResponseHeaders(IDictionary<string, ICollection<string>> headers)
        {
            _responseHeaders = new Dictionary<string, ICollection<string>>(headers, StringComparer.OrdinalIgnoreCase);
            return this;
        }

        /// <summary>
        /// Sets media type for the response body content.
        /// </summary>
        /// <param name="mediaType">The media type for the content-type.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithMediaType(string mediaType)
        {
            _mediaType = mediaType;
            return this;
        }

        /// <summary>
        /// Sets HTTP status code for the response.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithStatus(int statusCode) => WithStatus((HttpStatusCode)statusCode);

        /// <summary>
        /// Sets HTTP status code for the response.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithStatus(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            return this;
        }

        /// <summary>
        /// Sets the reason phrase for the response.
        /// </summary>
        /// <param name="reasonPhrase">The reason phrase.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithReason(string reasonPhrase)
        {
            _reasonPhrase = reasonPhrase;
            return this;
        }

        /// <summary>
        /// Sets the HTTP version for the response.
        /// </summary>
        /// <param name="version">The HTTP version.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithVersion(Version version)
        {
            _version = version;
            return this;
        }

        /// <summary>
        /// Sets a callback to use to use when a request is intercepted.
        /// </summary>
        /// <param name="onIntercepted">A delegate to a method to call when a request is intercepted.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithInterceptionCallback(Action<HttpRequestMessage> onIntercepted)
        {
            _onIntercepted = DelegateHelpers.ConvertToBooleanTask(onIntercepted);
            return this;
        }

        /// <summary>
        /// Sets an callback to use to use when a request is intercepted that returns
        /// <see langword="true"/> if the request should be intercepted or <see langword="false"/>
        /// if the request should not be intercepted.
        /// </summary>
        /// <param name="onIntercepted">
        /// A delegate to a method to call when a request is intercepted which returns a <see langword="bool"/>
        /// indicating whether the request should be intercepted or not.
        /// </param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithInterceptionCallback(Predicate<HttpRequestMessage> onIntercepted)
        {
            _onIntercepted = DelegateHelpers.ConvertToBooleanTask(onIntercepted);
            return this;
        }

        /// <summary>
        /// Sets an asynchronous callback to use to use when a request is intercepted.
        /// </summary>
        /// <param name="onIntercepted">A delegate to a method to await when a request is intercepted.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithInterceptionCallback(Func<HttpRequestMessage, Task> onIntercepted)
        {
            _onIntercepted = DelegateHelpers.ConvertToBooleanTask(onIntercepted);
            return this;
        }

        /// <summary>
        /// Sets an asynchronous callback to use to use when a request is intercepted that returns
        /// <see langword="true"/> if the request should be intercepted or <see langword="false"/>
        /// if the request should not be intercepted.
        /// </summary>
        /// <param name="onIntercepted">
        /// A delegate to a method to await when a request is intercepted which returns a <see langword="bool"/>
        /// indicating whether the request should be intercepted or not.
        /// </param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithInterceptionCallback(Func<HttpRequestMessage, Task<bool>> onIntercepted)
        {
            _onIntercepted = onIntercepted;
            return this;
        }

        internal HttpInterceptionResponse Build()
        {
            var response = new HttpInterceptionResponse()
            {
                ContentFactory = _contentFactory ?? EmptyContentFactory,
                ContentStream = _contentStream,
                ContentMediaType = _mediaType,
                IgnoreHost = _ignoreHost,
                IgnoreQuery = _ignoreQuery,
                Method = _method,
                OnIntercepted = _onIntercepted,
                ReasonPhrase = _reasonPhrase,
                RequestUri = _uriBuilder.Uri,
                StatusCode = _statusCode,
                UserMatcher = _requestMatcher,
                Version = _version,
            };

            if (_responseHeaders?.Count > 0)
            {
                var headers = new Dictionary<string, IEnumerable<string>>();

                foreach (var pair in _responseHeaders)
                {
                    headers[pair.Key] = pair.Value;
                }

                response.ResponseHeaders = headers;
            }

            if (_contentHeaders?.Count > 0)
            {
                var headers = new Dictionary<string, IEnumerable<string>>();

                foreach (var pair in _contentHeaders)
                {
                    headers[pair.Key] = pair.Value;
                }

                response.ContentHeaders = headers;
            }

            return response;
        }
    }
}
