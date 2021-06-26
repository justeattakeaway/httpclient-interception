// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class representing an <see cref="HttpClientHandler"/> implementation that
    /// supports intercepting HTTP requests instead of performing an HTTP request.
    /// </summary>
    public class InterceptingHttpMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// The <see cref="HttpClientInterceptorOptions"/> to use. This field is read-only.
        /// </summary>
        private readonly HttpClientInterceptorOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterceptingHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to use.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public InterceptingHttpMessageHandler(HttpClientInterceptorOptions options)
            : base()
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterceptingHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to use.</param>
        /// <param name="innerHandler">The inner <see cref="HttpMessageHandler"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public InterceptingHttpMessageHandler(HttpClientInterceptorOptions options, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_options.OnSend != null)
            {
                await _options.OnSend(request).ConfigureAwait(false);
            }

            var response = await _options.GetResponseAsync(request, cancellationToken).ConfigureAwait(false);

            if (response == null && _options.OnMissingRegistration != null)
            {
                response = await _options.OnMissingRegistration(request).ConfigureAwait(false);
            }

            if (response != null)
            {
                return response;
            }

            if (_options.ThrowOnMissingRegistration)
            {
#pragma warning disable CA1062
                throw new HttpRequestNotInterceptedException(
                    $"No HTTP response is configured for {request.Method.Method} {request.RequestUri}.",
                    request);
#pragma warning restore CA1062
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
