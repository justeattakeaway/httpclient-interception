// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.IO;
using System.Net;
using System.Net.Http;

namespace JustEat.HttpClientInterception
{
    internal sealed class HttpInterceptionResponse
    {
        internal Func<HttpContent, Task<bool>>? ContentMatcher { get; set; }

        internal Func<HttpRequestMessage, Task<bool>>? UserMatcher { get; set; }

        internal Matching.RequestMatcher? InternalMatcher { get; set; }

        internal HttpMethod? Method { get; set; }

        internal int? Priority { get; set; }

        internal string? ReasonPhrase { get; set; }

        internal IEnumerable<KeyValuePair<string, IEnumerable<string>>>? RequestHeaders { get; set; }

        internal Uri? RequestUri { get; set; }

        internal HttpStatusCode StatusCode { get; set; }

        internal Func<Task<byte[]>>? ContentFactory { get; set; }

        internal Func<Task<Stream>>? ContentStream { get; set; }

        internal string? ContentMediaType { get; set; }

        internal IEnumerable<KeyValuePair<string, IEnumerable<string>>>? ContentHeaders { get; set; }

        internal bool HasCustomPort { get; set; }

        internal bool IgnoreHost { get; set; }

        internal bool IgnorePath { get; set; }

        internal bool IgnoreQuery { get; set; }

        internal IEnumerable<KeyValuePair<string, IEnumerable<string>>>? ResponseHeaders { get; set; }

        internal Func<HttpRequestMessage, CancellationToken, Task<bool>>? OnIntercepted { get; set; }

        internal Version? Version { get; set; }
    }
}
