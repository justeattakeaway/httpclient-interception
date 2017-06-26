// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class that can be used to build HTTP requests for interception.
    /// </summary>
    public class HttpRequestInterceptionBuilder
    {
        private Func<byte[]> _contentFactory;

        private IDictionary<string, ICollection<string>> _headers;

        private string _mediaType = HttpClientInterceptorOptions.JsonMediaType;

        private HttpMethod _method = HttpMethod.Get;

        private HttpStatusCode _statusCode = HttpStatusCode.OK;

        private UriBuilder _uriBuilder = new UriBuilder();

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
        /// Sets the request URI to intercept a request for.
        /// </summary>
        /// <param name="uri">The request URI to intercept.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder ForUri(Uri uri)
        {
            _uriBuilder = new UriBuilder(uri);
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
            _contentFactory = contentFactory;
            return this;
        }

        /// <summary>
        /// Sets a custom HTTP response header to use.
        /// </summary>
        /// <param name="name">The name of the custom HTTP response header.</param>
        /// <param name="value">The value for the header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithHeader(string name, string value) => WithHeader(name, new[] { value });

        /// <summary>
        /// Sets a custom HTTP response header to use.
        /// </summary>
        /// <param name="name">The name of the custom HTTP response header.</param>
        /// <param name="values">The values for the header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithHeader(string name, params string[] values)
        {
            return WithHeader(name, values as IEnumerable<string>);
        }

        /// <summary>
        /// Sets a custom HTTP response header to use.
        /// </summary>
        /// <param name="name">The name of the custom HTTP response header.</param>
        /// <param name="values">The values for the header.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithHeader(string name, IEnumerable<string> values)
        {
            if (_headers == null)
            {
                _headers = new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);
            }

            if (!_headers.TryGetValue(name, out ICollection<string> current))
            {
                _headers[name] = current = new List<string>();
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
        public HttpRequestInterceptionBuilder WithHeaders(IDictionary<string, string> headers)
        {
            var copy = new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in headers)
            {
                copy[pair.Key] = new[] { pair.Value };
            }

            _headers = copy;

            return this;
        }

        /// <summary>
        /// Sets any custom HTTP response headers to use.
        /// </summary>
        /// <param name="headers">Any custom HTTP response headers to use.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        public HttpRequestInterceptionBuilder WithHeaders(IDictionary<string, ICollection<string>> headers)
        {
            _headers = new Dictionary<string, ICollection<string>>(headers, StringComparer.OrdinalIgnoreCase);
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

        internal HttpInterceptionResponse Build()
        {
            var response = new HttpInterceptionResponse()
            {
                ContentFactory = _contentFactory ?? Array.Empty<byte>,
                ContentMediaType = _mediaType,
                Method = _method,
                RequestUri = _uriBuilder.Uri,
                StatusCode = _statusCode,
            };

            if (_headers?.Count > 0)
            {
                var headers = new Dictionary<string, IEnumerable<string>>();

                foreach (var pair in _headers)
                {
                    headers[pair.Key] = pair.Value;
                }

                response.Headers = headers;
            }

            return response;
        }
    }
}
