// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Http;

namespace SampleApp.Handlers
{
    public class TimingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            response.Headers.Add("x-request-duration", stopwatch.Elapsed.ToString());

            return response;
        }
    }
}
