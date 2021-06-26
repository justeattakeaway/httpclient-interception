// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing extension methods for the <see cref="HttpRequestInterceptionBuilder"/> class. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        /// Sets builder for an HTTP DELETE request.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder ForDelete(this HttpRequestInterceptionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ForMethod(HttpMethod.Delete);
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
        /// Sets builder for an HTTP PUT request.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder ForPut(this HttpRequestInterceptionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ForMethod(HttpMethod.Put);
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
        /// Sets the parameters to use as the form URL-encoded response content.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="parameters">The parameters to use for the form URL-encoded content.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="parameters"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder WithFormContent(
            this HttpRequestInterceptionBuilder builder,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            async Task<byte[]> ContentFactoryAsync()
            {
                // The cast is to make nullability match for .NET 5.0.
                // See https://github.com/dotnet/runtime/issues/38494.
                using var content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string?, string?>>)parameters);
                return await content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }

            return builder
                .WithMediaType(HttpClientInterceptorOptions.FormMediaType)
                .WithContent(ContentFactoryAsync);
        }

        /// <summary>
        /// Sets the object to use as the response content.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="content">The object to serialize as JSON as the content.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder WithJsonContent(
            this HttpRequestInterceptionBuilder builder,
            object content)
        {
            return builder.WithNewtonsoftJsonContent(content);
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

        /// <summary>
        /// A convenience method that can be used to signify the start of request method calls for fluent registrations.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        public static HttpRequestInterceptionBuilder Requests(this HttpRequestInterceptionBuilder builder) => builder;

        /// <summary>
        /// A convenience method that can be used to signify the start of response method calls for fluent registrations.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        public static HttpRequestInterceptionBuilder Responds(this HttpRequestInterceptionBuilder builder) => builder;

        /// <summary>
        /// Registers the builder with the specified <see cref="HttpClientInterceptorOptions"/> instance.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use to create the registration.</param>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to register the builder with.</param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestInterceptionBuilder RegisterWith(this HttpRequestInterceptionBuilder builder, HttpClientInterceptorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Register(builder);

            return builder;
        }

        /// <summary>
        /// Configures the builder to match any request whose HTTP content meets the criteria defined by the specified predicate.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="predicate">
        /// A delegate to a method which returns <see langword="true"/> if the
        /// request's HTTP content is considered a match; otherwise <see langword="false"/>.
        /// </param>
        /// <returns>
        /// The current <see cref="HttpRequestInterceptionBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Pass a value of <see langword="null"/> to remove a previously-registered custom content matching predicate.
        /// </remarks>
        public static HttpRequestInterceptionBuilder ForContent(
            this HttpRequestInterceptionBuilder builder,
            Predicate<HttpContent>? predicate)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (predicate == null)
            {
                return builder.ForContent(null);
            }
            else
            {
                return builder.ForContent((content) => Task.FromResult(predicate(content)));
            }
        }

        /// <summary>
        /// Sets the parameters to use as the form URL-encoded response content.
        /// </summary>
        /// <param name="builder">The <see cref="HttpRequestInterceptionBuilder"/> to use.</param>
        /// <param name="parameters">The parameters to use for the form URL-encoded content.</param>
        /// <returns>
        /// The value specified by <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="parameters"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Matching is based on a subset rather than an exact match. If all the parameters and values
        /// specified are found in the request's body the request will be considered a match, even if
        /// there are other parameters contained in the request's body. Value matching is case-sensitive.
        /// </para>
        /// <para>
        /// For more specific matching to the request body parameters, use the <see cref="HttpRequestInterceptionBuilder.ForContent(Func{HttpContent, Task{bool}})"/>
        /// method to provide a custom predicate that implements your content matching requirements.
        /// </para>
        /// </remarks>
        public static HttpRequestInterceptionBuilder ForFormContent(
            this HttpRequestInterceptionBuilder builder,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            async Task<bool> IsMatchAsync(HttpContent content)
            {
                // FormUrlEncodedContent derives from ByteArrayContent so use the
                // least specific type for more flexibility (e.g. also StringContent).
                // Other content types are not supported as they may be iterating over
                // a stream which might not support arbitrary seeking if the request
                // is not a match and needs to be sent to the URL originally specified.
                if (!(content is ByteArrayContent))
                {
                    return false;
                }

                string bodyMaybeForm;

                using (var stream = new MemoryStream())
                {
                    await content.CopyToAsync(stream).ConfigureAwait(false);
                    bodyMaybeForm = Encoding.UTF8.GetString(stream.ToArray());
                }

                // This is tolerant to non-well-formed content, so even JSON
                // parses without issues, it then just won't match the input.
                var query = QueryHelpers.ParseQuery(bodyMaybeForm);

                bool matched = true;

                foreach (var pair in parameters)
                {
                    if (!query.TryGetValue(pair.Key, out var value) ||
                        !string.Equals(pair.Value, value.ToString(), StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return matched;
            }

            return builder.ForContent(IsMatchAsync);
        }
    }
}
