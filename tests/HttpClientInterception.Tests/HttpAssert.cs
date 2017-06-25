// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shouldly;

namespace JustEat.HttpClientInterception
{
    internal static class HttpAssert
    {
        internal static async Task<T> GetAsync<T>(
            HttpClientInterceptorOptions target,
            string requestUri,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            string json = await GetAsync(target, requestUri, statusCode);
            return JsonConvert.DeserializeObject<T>(json);
        }

        internal static async Task<string> GetAsync(
            HttpClientInterceptorOptions target,
            string requestUri,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = null)
        {
            using (var httpClient = target.CreateHttpClient(ErroringHandler.Handler))
            {
                using (var response = await httpClient.GetAsync(requestUri))
                {
                    response.StatusCode.ShouldBe(statusCode);

                    if (mediaType != null)
                    {
                        response.Content.Headers.ContentType.MediaType.ShouldBe(mediaType);
                    }

                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        private sealed class ErroringHandler : HttpMessageHandler
        {
            internal static readonly ErroringHandler Handler = new ErroringHandler();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException("HTTP request was not intercepted.");
            }
        }
    }
}
