﻿// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace JustEat.HttpClientInterception;

/// <summary>
/// A class that can be used to build HTTP requests for interception.
/// </summary>
public class HttpRequestInterceptionBuilder
{
    private static readonly Task<byte[]> EmptyContent = Task.FromResult(Array.Empty<byte>());

    private static readonly Func<Task<byte[]>> EmptyContentFactory = () => EmptyContent;

    private Func<Task<byte[]>>? _contentFactory;

    private Func<HttpContent, Task<bool>>? _contentMatcher;

    private Func<Task<Stream>>? _contentStream;

    private IDictionary<string, ICollection<string>>? _contentHeaders;

    private IDictionary<string, ICollection<string>>? _requestHeaders;

    private IDictionary<string, ICollection<string>>? _responseHeaders;

    private string _mediaType = HttpClientInterceptorOptions.JsonMediaType;

    private HttpMethod _method = HttpMethod.Get;

    private Func<HttpRequestMessage, CancellationToken, Task<bool>>? _onIntercepted;

    private Func<HttpRequestMessage, Task<bool>>? _requestMatcher;

    private string? _reasonPhrase;

    private HttpStatusCode _statusCode = HttpStatusCode.OK;

    private UriBuilder _uriBuilder = new();

    private Version? _version;

    private bool _hasCustomPort;

    private bool _ignoreHost;

    private bool _ignorePath;

    private bool _ignoreQuery;

    private int? _priority;

    private int _revision;

    private string? _matchKey;

    private int? _matchKeyRevision;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestInterceptionBuilder"/> class.
    /// </summary>
    public HttpRequestInterceptionBuilder()
    {
    }

    /// <summary>
    /// Gets the internal match key for the builder, if any.
    /// </summary>
    /// <remarks>
    /// This is effectively treated as a version of the builder setup itself
    /// and is used to track whether or not the registrations have changed.
    /// <see href="https://github.com/justeattakeaway/httpclient-interception/issues/361"/>.
    /// </remarks>
    internal string? Key => _revision == _matchKeyRevision ? _matchKey : null;

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
    public HttpRequestInterceptionBuilder For(Predicate<HttpRequestMessage>? predicate)
    {
        _requestMatcher = predicate == null ? null : new Func<HttpRequestMessage, Task<bool>>((message) => Task.FromResult(predicate(message)));
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Configures the builder to match any request that meets all the criteria defined by the specified predicates.
    /// </summary>
    /// <param name="predicates">
    /// Delegates to methods which must all return <see langword="true"/> for the
    /// request to be considered a match.
    /// </param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Pass a value of <see langword="null"/> to remove a previously-registered custom request matching predicate.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForAll(ICollection<Predicate<HttpRequestMessage>> predicates)
    {
        _requestMatcher = DelegateHelpers.ConvertToBooleanTask(predicates);
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Configures the builder to match any request that meets all the criteria defined by the specified predicates.
    /// </summary>
    /// <param name="predicates">
    /// Two or more delegates to a method which returns <see langword="true"/> if the
    /// request is considered a match; otherwise <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Pass a value of <see langword="null"/> to remove a previously-registered custom request matching predicate.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForAll(ICollection<Func<HttpRequestMessage, Task<bool>>> predicates)
    {
        _requestMatcher = DelegateHelpers.ConvertToBooleanTask(predicates);
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Configures the builder to match any request that meets the criteria defined by the specified asynchronous predicate.
    /// </summary>
    /// <param name="predicate">
    /// A delegate to an asynchronous method which returns <see langword="true"/> if
    /// the request is considered a match; otherwise <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Pass a value of <see langword="null"/> to remove a previously-registered custom request matching predicate.
    /// </remarks>
    public HttpRequestInterceptionBuilder For(Func<HttpRequestMessage, Task<bool>> predicate)
    {
        _requestMatcher = predicate;
        IncrementRevision();
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
        IncrementRevision();
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
        IncrementRevision();
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
        IncrementRevision();
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
        IncrementRevision();
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
        _hasCustomPort = port != -1;
        IncrementRevision();
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
        IncrementRevision();
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
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// If true URI paths will be ignored when testing request URIs.
    /// </summary>
    /// <param name="ignorePath">Whether to ignore paths or not; defaults to true.</param>
    /// <returns>The current <see cref="HttpRequestInterceptionBuilder"/>.</returns>
    public HttpRequestInterceptionBuilder IgnoringPath(bool ignorePath = true)
    {
        _ignorePath = ignorePath;
        IncrementRevision();
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
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets the request URI to intercept a request for.
    /// </summary>
    /// <param name="uri">The request URI to intercept.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="uri"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder ForUri(Uri uri)
    {
        _uriBuilder = new UriBuilder(uri);
        _ignoreHost = false;
        IncrementRevision();
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
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets the function to use to build the response content.
    /// </summary>
    /// <param name="contentFactory">A delegate to a method that returns the raw response content.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Pass a value of <see langword="null"/> to reset to no content factory.
    /// </remarks>
    public HttpRequestInterceptionBuilder WithContent(Func<byte[]>? contentFactory)
    {
        if (contentFactory is null)
        {
            _contentFactory = null;
        }
        else
        {
            _contentFactory = () => Task.FromResult(contentFactory());
            _contentStream = null;
        }

        IncrementRevision();

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
        if (_contentFactory is not null)
        {
            _contentStream = null;
        }

        IncrementRevision();

        return this;
    }

    /// <summary>
    /// Sets the function to use to build the response stream.
    /// </summary>
    /// <param name="contentStream">A delegate to a method that returns the response stream for the content.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Pass a value of <see langword="null"/> to reset to no content stream.
    /// </remarks>
    public HttpRequestInterceptionBuilder WithContentStream(Func<Stream>? contentStream)
    {
        if (contentStream is null)
        {
            _contentStream = null;
        }
        else
        {
            _contentStream = () => Task.FromResult(contentStream());
            _contentFactory = null;
        }

        IncrementRevision();

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
        if (_contentStream is not null)
        {
            _contentFactory = null;
        }

        IncrementRevision();

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
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithContentHeader(string name, string value)
        => WithContentHeader(name, [value]);

    /// <summary>
    /// Sets a custom HTTP content header to use with multiple values.
    /// </summary>
    /// <param name="name">The name of the custom HTTP content header.</param>
    /// <param name="values">The values for the content header.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> or <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithContentHeader(string name, params string[] values)
        => WithContentHeader(name, (IEnumerable<string>)values);

    /// <summary>
    /// Sets a custom HTTP content header to use with multiple values.
    /// </summary>
    /// <param name="name">The name of the custom HTTP content header.</param>
    /// <param name="values">The values for the content header.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> or <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithContentHeader(string name, IEnumerable<string> values)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        _contentHeaders ??= new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);

        if (!_contentHeaders.TryGetValue(name, out var current))
        {
            _contentHeaders[name] = current = [];
        }

        current.Clear();

        foreach (string value in values)
        {
            current.Add(value);
        }

        IncrementRevision();

        return this;
    }

    /// <summary>
    /// Sets any custom HTTP content headers to use.
    /// </summary>
    /// <param name="headers">Any custom HTTP content headers to use.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headers"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithContentHeaders(IDictionary<string, ICollection<string>> headers)
    {
        if (headers is null)
        {
            throw new ArgumentNullException(nameof(headers));
        }

        _contentHeaders = new Dictionary<string, ICollection<string>>(headers, StringComparer.OrdinalIgnoreCase);
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets any custom HTTP content headers to use.
    /// </summary>
    /// <param name="headers">Any custom HTTP content headers to use.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headers"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithContentHeaders(IDictionary<string, string> headers)
    {
        if (headers is null)
        {
            throw new ArgumentNullException(nameof(headers));
        }

        var copy = new Dictionary<string, ICollection<string>>(headers.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in headers)
        {
            copy[pair.Key] = [pair.Value];
        }

        _contentHeaders = copy;
        IncrementRevision();

        return this;
    }

    /// <summary>
    /// Sets a delegate to a method that generates any custom HTTP content headers to use.
    /// </summary>
    /// <param name="headerFactory">Any delegate that creates any custom HTTP content headers to use.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headerFactory"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithContentHeaders(
        Func<IEnumerable<KeyValuePair<string, ICollection<string>>>> headerFactory)
    {
        if (headerFactory is null)
        {
            throw new ArgumentNullException(nameof(headerFactory));
        }

        _contentHeaders = new DynamicDictionary(headerFactory);
        IncrementRevision();
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
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithResponseHeader(string name, string value)
        => WithResponseHeader(name, [value]);

    /// <summary>
    /// Sets a custom HTTP response header to use with multiple values.
    /// </summary>
    /// <param name="name">The name of the custom HTTP response header.</param>
    /// <param name="values">The values for the response header.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> or <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithResponseHeader(string name, params string[] values)
        => WithResponseHeader(name, (IEnumerable<string>)values);

    /// <summary>
    /// Sets a custom HTTP response header to use with multiple values.
    /// </summary>
    /// <param name="name">The name of the custom HTTP response header.</param>
    /// <param name="values">The values for the response header.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> or <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithResponseHeader(string name, IEnumerable<string> values)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        _responseHeaders ??= new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);

        if (!_responseHeaders.TryGetValue(name, out ICollection<string>? current))
        {
            _responseHeaders[name] = current = [];
        }

        current.Clear();

        foreach (string value in values)
        {
            current.Add(value);
        }

        IncrementRevision();

        return this;
    }

    /// <summary>
    /// Sets any custom HTTP response headers to use.
    /// </summary>
    /// <param name="headers">Any custom HTTP response headers to use.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headers"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithResponseHeaders(IDictionary<string, string> headers)
    {
        if (headers is null)
        {
            throw new ArgumentNullException(nameof(headers));
        }

        var copy = new Dictionary<string, ICollection<string>>(headers.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in headers)
        {
            copy[pair.Key] = [pair.Value];
        }

        _responseHeaders = copy;
        IncrementRevision();

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
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets a delegate to a method that generates any custom HTTP response headers to use.
    /// </summary>
    /// <param name="headerFactory">Any delegate that creates any custom HTTP response headers to use.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headerFactory"/> is <see langword="null"/>.
    /// </exception>
    public HttpRequestInterceptionBuilder WithResponseHeaders(
        Func<IEnumerable<KeyValuePair<string, ICollection<string>>>> headerFactory)
    {
        if (headerFactory is null)
        {
            throw new ArgumentNullException(nameof(headerFactory));
        }

        _responseHeaders = new DynamicDictionary(headerFactory);
        IncrementRevision();
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
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets HTTP status code for the response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    public HttpRequestInterceptionBuilder WithStatus(int statusCode)
        => WithStatus((HttpStatusCode)statusCode);

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
        IncrementRevision();
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
        IncrementRevision();
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
        IncrementRevision();
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
        IncrementRevision();
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
    public HttpRequestInterceptionBuilder WithInterceptionCallback(Predicate<HttpRequestMessage>? onIntercepted)
    {
        _onIntercepted = DelegateHelpers.ConvertToBooleanTask(onIntercepted);
        IncrementRevision();
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
        IncrementRevision();
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
        _onIntercepted = DelegateHelpers.ConvertToBooleanTask(onIntercepted);
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets an asynchronous callback to use to use when a request is intercepted that returns
    /// <see langword="true"/> if the request should be intercepted or <see langword="false"/>
    /// if the request should not be intercepted.
    /// </summary>
    /// <param name="onIntercepted">A delegate to a method to await when a request is intercepted.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    public HttpRequestInterceptionBuilder WithInterceptionCallback(Func<HttpRequestMessage, CancellationToken, Task> onIntercepted)
    {
        _onIntercepted = DelegateHelpers.ConvertToBooleanTask(onIntercepted);
        IncrementRevision();
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
    public HttpRequestInterceptionBuilder WithInterceptionCallback(Func<HttpRequestMessage, CancellationToken, Task<bool>> onIntercepted)
    {
        _onIntercepted = onIntercepted;
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets the priority for matching against HTTP requests.
    /// </summary>
    /// <param name="priority">The priority of the HTTP interception.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <remarks>
    /// The priority is used to establish a hierarchy for matching of intercepted
    /// HTTP requests, particularly when <see cref="For(Predicate{HttpRequestMessage})"/> is used.
    /// This allows registered requests to establish an order of precedence when an HTTP request
    /// could match against multiple predicates, where the matching predicate with
    /// the lowest value for <paramref name="priority"/> dictates the HTTP response
    /// that is used for the intercepted request.
    /// <para />
    /// By default an interception has no priority, so the first arbitrary registration
    /// that matches which does not have a priority will be used to provide the response.
    /// </remarks>
    public HttpRequestInterceptionBuilder HavingPriority(int? priority)
    {
        _priority = priority;
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets an HTTP request header to intercept a request for.
    /// </summary>
    /// <param name="name">The name of the HTTP request header.</param>
    /// <param name="value">The value of the request header.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// HTTP request headers are only tested for interception if the URI requested was registered for interception.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForRequestHeader(string name, string value)
        => ForRequestHeader(name, [value]);

    /// <summary>
    /// Sets an HTTP request header to intercept with multiple values.
    /// </summary>
    /// <param name="name">The name of the HTTP request header.</param>
    /// <param name="values">The values for the request header.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> or <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// HTTP request headers are only tested for interception if the URI requested was registered for interception.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForRequestHeader(string name, params string[] values)
        => ForRequestHeader(name, (IEnumerable<string>)values);

    /// <summary>
    /// Sets an HTTP request header to intercept with multiple values.
    /// </summary>
    /// <param name="name">The name of the HTTP request header.</param>
    /// <param name="values">The values for the request header.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> or <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// HTTP request headers are only tested for interception if the URI requested was registered for interception.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForRequestHeader(string name, IEnumerable<string> values)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        _requestHeaders ??= new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);

        if (!_requestHeaders.TryGetValue(name, out ICollection<string>? current))
        {
            _requestHeaders[name] = current = [];
        }

        current.Clear();

        foreach (string value in values)
        {
            current.Add(value);
        }

        IncrementRevision();

        return this;
    }

    /// <summary>
    /// Sets any custom HTTP request headers to intercept.
    /// </summary>
    /// <param name="headers">Any HTTP request headers to intercept.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headers"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// HTTP request headers are only tested for interception if the URI requested was registered for interception.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForRequestHeaders(IDictionary<string, string> headers)
    {
        if (headers is null)
        {
            throw new ArgumentNullException(nameof(headers));
        }

        var copy = new Dictionary<string, ICollection<string>>(headers.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in headers)
        {
            copy[pair.Key] = [pair.Value];
        }

        _requestHeaders = copy;
        IncrementRevision();

        return this;
    }

    /// <summary>
    /// Sets the HTTP request headers to intercept.
    /// </summary>
    /// <param name="headers">Any HTTP request headers to intercept.</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headers"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// HTTP request headers are only tested for interception if the URI requested was registered for interception.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForRequestHeaders(IDictionary<string, ICollection<string>> headers)
    {
        if (headers is null)
        {
            throw new ArgumentNullException(nameof(headers));
        }

        _requestHeaders = new Dictionary<string, ICollection<string>>(headers, StringComparer.OrdinalIgnoreCase);
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Sets a delegate to a method that generates the HTTP request headers to intercept.
    /// </summary>
    /// <param name="headerFactory">Any delegate that returns the HTTP request headers to intercept..</param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headerFactory"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// HTTP request headers are only tested for interception if the URI requested was registered for interception.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForRequestHeaders(
        Func<IEnumerable<KeyValuePair<string, ICollection<string>>>> headerFactory)
    {
        if (headerFactory is null)
        {
            throw new ArgumentNullException(nameof(headerFactory));
        }

        _requestHeaders = new DynamicDictionary(headerFactory);
        IncrementRevision();
        return this;
    }

    /// <summary>
    /// Configures the builder to match any request whose HTTP content meets the criteria defined by the specified predicate.
    /// </summary>
    /// <param name="predicate">
    /// A delegate to a method which returns <see langword="true"/> if the
    /// request's HTTP content is considered a match; otherwise <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The current <see cref="HttpRequestInterceptionBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Pass a value of <see langword="null"/> to remove a previously-registered custom content matching predicate.
    /// </remarks>
    public HttpRequestInterceptionBuilder ForContent(Func<HttpContent, Task<bool>>? predicate)
    {
        _contentMatcher = predicate;
        IncrementRevision();
        return this;
    }

    internal HttpInterceptionResponse Build()
    {
        var response = new HttpInterceptionResponse()
        {
            ContentFactory = _contentFactory ?? EmptyContentFactory,
            ContentMatcher = _contentMatcher,
            ContentStream = _contentStream,
            ContentMediaType = _mediaType,
            HasCustomPort = _hasCustomPort,
            IgnoreHost = _ignoreHost,
            IgnorePath = _ignorePath,
            IgnoreQuery = _ignoreQuery,
            Method = _method,
            OnIntercepted = _onIntercepted,
            Priority = _priority,
            ReasonPhrase = _reasonPhrase,
            RequestUri = _uriBuilder.Uri,
            StatusCode = _statusCode,
            UserMatcher = _requestMatcher,
            Version = _version,
        };

        if (_requestHeaders is not null)
        {
            if (_requestHeaders is DynamicDictionary factory)
            {
                response.RequestHeaders = factory;
            }
            else if (_requestHeaders.Count > 0)
            {
                var headers = new Dictionary<string, IEnumerable<string>>(_requestHeaders.Count);

                foreach (var pair in _requestHeaders)
                {
                    headers[pair.Key] = [.. pair.Value];
                }

                response.RequestHeaders = headers;
            }
        }

        if (_responseHeaders is not null)
        {
            if (_responseHeaders is DynamicDictionary factory)
            {
                response.ResponseHeaders = factory;
            }
            else if (_responseHeaders.Count > 0)
            {
                var headers = new Dictionary<string, IEnumerable<string>>(_responseHeaders.Count);

                foreach (var pair in _responseHeaders)
                {
                    headers[pair.Key] = [.. pair.Value];
                }

                response.ResponseHeaders = headers;
            }
        }

        if (_contentHeaders is not null)
        {
            if (_contentHeaders is DynamicDictionary factory)
            {
                response.ContentHeaders = factory;
            }
            else if (_contentHeaders.Count > 0)
            {
                var headers = new Dictionary<string, IEnumerable<string>>(_contentHeaders.Count);

                foreach (var pair in _contentHeaders)
                {
                    headers[pair.Key] = pair.Value;
                }

                response.ContentHeaders = headers;
            }
        }

        return response;
    }

    internal void SetMatchKey(string key)
    {
        _matchKey = key;
        _matchKeyRevision = _revision;
    }

    private void IncrementRevision()
    {
        // It doesn't matter if somehow this Wraps around
        unchecked
        {
            _revision++;
        }
    }

    internal sealed class DynamicDictionary :
        IDictionary<string, ICollection<string>>,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>
    {
        private readonly Func<IEnumerable<KeyValuePair<string, ICollection<string>>>> _generator;

        internal DynamicDictionary(Func<IEnumerable<KeyValuePair<string, ICollection<string>>>> generator)
        {
            _generator = generator;
        }

        [ExcludeFromCodeCoverage]
        public ICollection<string> Keys => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public ICollection<ICollection<string>> Values => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public int Count => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public bool IsReadOnly => true;

        [ExcludeFromCodeCoverage]
        public ICollection<string> this[string key]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public IEnumerator<KeyValuePair<string, ICollection<string>>> GetEnumerator()
            => _generator().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        IEnumerator<KeyValuePair<string, IEnumerable<string>>> IEnumerable<KeyValuePair<string, IEnumerable<string>>>.GetEnumerator()
            => new EnumeratorAdapter(GetEnumerator());

        [ExcludeFromCodeCoverage]
        public void Add(string key, ICollection<string> value)
            => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public void Add(KeyValuePair<string, ICollection<string>> item)
            => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public void Clear()
            => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public bool Contains(KeyValuePair<string, ICollection<string>> item)
            => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public bool ContainsKey(string key)
            => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public void CopyTo(KeyValuePair<string, ICollection<string>>[] array, int arrayIndex)
            => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public bool Remove(string key)
             => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public bool Remove(KeyValuePair<string, ICollection<string>> item)
             => throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public bool TryGetValue(string key, out ICollection<string> value)
             => throw new NotSupportedException();

        private sealed class EnumeratorAdapter : IEnumerator<KeyValuePair<string, IEnumerable<string>>>
        {
            private readonly IEnumerator<KeyValuePair<string, ICollection<string>>> _enumerator;

            internal EnumeratorAdapter(IEnumerator<KeyValuePair<string, ICollection<string>>> enumerator)
            {
                _enumerator = enumerator;
            }

            public KeyValuePair<string, IEnumerable<string>> Current
                => new(_enumerator.Current.Key, _enumerator.Current.Value);

            object IEnumerator.Current
                => _enumerator.Current;

            public void Dispose()
                => _enumerator.Dispose();

            public bool MoveNext()
                => _enumerator.MoveNext();

            public void Reset()
                => _enumerator.Reset();
        }
    }
}
