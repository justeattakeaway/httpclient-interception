// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace JustEat.HttpClientInterception
{
    internal sealed class HttpInterceptionResponse
    {
        internal HttpMethod Method { get; set; }

        internal Uri RequestUri { get; set; }

        internal HttpStatusCode StatusCode { get; set; }

        internal Func<byte[]> ContentFactory { get; set; }

        internal string ContentMediaType { get; set; }

        internal IEnumerable<KeyValuePair<string, IEnumerable<string>>> ResponseHeaders { get; set; }

        internal Action<HttpRequestMessage> OnIntercepted { get; set; }
    }
}
