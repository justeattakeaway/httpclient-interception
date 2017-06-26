// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing extenion methods for the <see cref="HttpRequestInterceptionBuilder"/> class. This class cannot be inherited.
    /// </summary>
    public static class HttpRequestInterceptionBuilderExtensions
    {
        /// <summary>
        /// Sets builder for an HTTP request.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder ForHttp(this HttpRequestInterceptionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ForScheme("http");
        }

        /// <summary>
        /// Sets builder for an HTTPS request.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder ForHttps(this HttpRequestInterceptionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ForScheme("https");
        }

        /// <summary>
        /// Sets builder for an HTTP GET request.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder ForGet(this HttpRequestInterceptionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ForMethod(HttpMethod.Get);
        }

        /// <summary>
        /// Sets builder for an HTTP POST request.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder ForPost(this HttpRequestInterceptionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ForMethod(HttpMethod.Post);
        }

        /// <summary>
        /// Sets the string to use as the response content.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="content">The UTF-8 encoded string to use as the content.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder WithContent(this HttpRequestInterceptionBuilder builder, string content)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.WithContent(() => Encoding.UTF8.GetBytes(content ?? string.Empty));
        }

        /// <summary>
        /// Sets the object to use as the response content.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="content">The object to serialize as JSON as the content.</param>
        /// <param name="settings">The optional settings to use to serialize <paramref name="content"/> as JSON.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder WithJsonContent(
            this HttpRequestInterceptionBuilder builder,
            object content,
            JsonSerializerSettings settings = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Func<byte[]> contentFactory = () =>
            {
                string json = JsonConvert.SerializeObject(content, settings ?? new JsonSerializerSettings());
                return Encoding.UTF8.GetBytes(json);
            };

            return builder
                .WithMediaType(HttpClientInterceptorOptions.JsonMediaType)
                .WithContent(contentFactory);
        }

        /// <summary>
        /// Sets the request URL to intercept a request for.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="uriString">The request URL.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder ForUrl(this HttpRequestInterceptionBuilder builder, string uriString)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ForUri(new Uri(uriString));
        }
    }
}
