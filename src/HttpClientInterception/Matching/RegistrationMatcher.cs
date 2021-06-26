// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Http.Headers;

namespace JustEat.HttpClientInterception.Matching
{
    /// <summary>
    /// A class representing an implementation of <see cref="RequestMatcher"/> that matches against
    /// an instance of <see cref="HttpInterceptionResponse"/>. This class cannot be inherited.
    /// </summary>
    internal sealed class RegistrationMatcher : RequestMatcher
    {
        /// <summary>
        /// The string comparer to use to match URIs. This field is read-only.
        /// </summary>
        private readonly IEqualityComparer<string> _comparer;

        /// <summary>
        /// The <see cref="HttpInterceptionResponse"/> to use to generate URIs for comparison. This field is read-only.
        /// </summary>
        private readonly HttpInterceptionResponse _registration;

        /// <summary>
        /// The pre-computed URI that requests are required to match to. This field is read-only.
        /// </summary>
        private readonly string _expected;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationMatcher"/> class.
        /// </summary>
        /// <param name="registration">The <see cref="HttpInterceptionResponse"/> to use for matching.</param>
        /// <param name="comparer">The string comparer to use to compare URIs.</param>
        internal RegistrationMatcher(HttpInterceptionResponse registration, IEqualityComparer<string> comparer)
        {
            _comparer = comparer;
            _registration = registration;
            _expected = GetUriStringForMatch(registration);
        }

        /// <inheritdoc />
        public override async Task<bool> IsMatchAsync(HttpRequestMessage request)
        {
            if (request.RequestUri == null || request.Method != _registration.Method)
            {
                return false;
            }

            string actual = GetUriStringForMatch(_registration, request.RequestUri);

            if (!_comparer.Equals(_expected, actual))
            {
                return false;
            }

            bool isMatch = true;

            if (_registration.RequestHeaders != null)
            {
                isMatch = IsMatchForHeaders(request.Headers);
            }

            if (isMatch && _registration.ContentMatcher != null)
            {
                isMatch = await _registration.ContentMatcher(request.Content!).ConfigureAwait(false);
            }

            return isMatch;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that can be used to compare a URI against another.
        /// </summary>
        /// <param name="registration">The <see cref="HttpInterceptionResponse"/> to use to build the URI.</param>
        /// <param name="uri">The optional <see cref="Uri"/> to use to build the URI if not using the one associated with <paramref name="registration"/>.</param>
        /// <returns>
        /// A <see cref="string"/> containing the URI to use for string comparisons for URIs.
        /// </returns>
        private static string GetUriStringForMatch(HttpInterceptionResponse registration, Uri? uri = null)
        {
            var builder = new UriBuilder(uri ?? registration.RequestUri!);

            if (!registration.HasCustomPort)
            {
                builder.Port = -1;
            }

            if (registration.IgnoreHost)
            {
                builder.Host = "*";
            }

            if (registration.IgnorePath)
            {
                builder.Path = string.Empty;
            }

            if (registration.IgnoreQuery)
            {
                builder.Query = string.Empty;
            }

            return builder.ToString();
        }

        private bool IsMatchForHeaders(HttpHeaders requestHeaders)
        {
            foreach (var headerToMatch in _registration.RequestHeaders!)
            {
                if (!requestHeaders.TryGetValues(headerToMatch.Key, out IEnumerable<string>? values))
                {
                    return false;
                }

                string expectedValues = string.Join(";", headerToMatch.Value);
                string actualValues = string.Join(";", values);

                if (!string.Equals(expectedValues, actualValues, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
