// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text.Json;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing extension methods for the <see cref="HttpRequestInterceptionBuilder"/>
    /// class for <c>System.Text.Json</c>. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SystemTextJsonExtensions
    {
        /// <summary>
        /// Sets the object to use as the response content.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="content">The object to serialize as JSON as the content.</param>
        /// <param name="options">The optional <see cref="JsonSerializerOptions"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder WithSystemTextJsonContent(
            this HttpRequestInterceptionBuilder builder,
            object content,
            JsonSerializerOptions? options = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            byte[] ContentFactory()
            {
                return JsonSerializer.SerializeToUtf8Bytes(content, options);
            }

            return builder
                .WithMediaType(HttpClientInterceptorOptions.JsonMediaType)
                .WithContent(ContentFactory);
        }
    }
}
