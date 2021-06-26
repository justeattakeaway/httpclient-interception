// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace SampleApp.Handlers
{
    public class AddRequestIdHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("x-request-id", Guid.NewGuid().ToString());

            return base.SendAsync(request, cancellationToken);
        }
    }
}
