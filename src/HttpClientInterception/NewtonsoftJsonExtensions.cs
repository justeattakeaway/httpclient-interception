// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing extension methods for the <see cref="HttpRequestInterceptionBuilder"/>
    /// class for <c>Newtonsoft.Json</c>. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class NewtonsoftJsonExtensions
    {
        /// <summary>
        /// Sets the object to use as the response content.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="content">The object to serialize as JSON as the content.</param>
        /// <param name="settings">The optional <see cref="JsonSerializerSettings"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder WithNewtonsoftJsonContent(
            this HttpRequestInterceptionBuilder builder,
            object content,
            JsonSerializerSettings? settings = null)
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
                string json = JsonConvert.SerializeObject(content, settings!);
                return Encoding.UTF8.GetBytes(json);
            }

            return builder
                .WithMediaType(HttpClientInterceptorOptions.JsonMediaType)
                .WithContent(ContentFactory);
        }
    }
}
