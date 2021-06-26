// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

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

        internal static Task<string> GetAsync(
            HttpClientInterceptorOptions target,
            string requestUri,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = null,
            IDictionary<string, string> headers = null)
        {
            return SendAsync(target, HttpMethod.Get, requestUri, null, statusCode, mediaType, headers);
        }

        internal static async Task<string> PostAsync(
            HttpClientInterceptorOptions target,
            string requestUri,
            object content,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = null)
        {
            string json = JsonConvert.SerializeObject(content);

            using var httpContent = new StringContent(json, Encoding.UTF8, mediaType ?? "application/json");
            return await SendAsync(target, HttpMethod.Post, requestUri, httpContent, statusCode, mediaType);
        }

        internal static async Task<string> SendAsync(
            HttpClientInterceptorOptions target,
            HttpMethod httpMethod,
            string requestUri,
            HttpContent content = null,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = null,
            IDictionary<string, string> headers = null)
        {
            using var httpClient = target.CreateHttpClient(ErroringHandler.Handler);
            using var request = new HttpRequestMessage(httpMethod, requestUri);

            if (content != null)
            {
                request.Content = content;
            }

            if (headers != null)
            {
                foreach (var pair in headers)
                {
                    request.Headers.Add(pair.Key, pair.Value);
                }
            }

            using var response = await httpClient.SendAsync(request);
            response.StatusCode.ShouldBe(statusCode);

            if (mediaType != null)
            {
                response.Content.Headers.ContentType.MediaType.ShouldBe(mediaType);
            }

            if (response.Content == null)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
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
