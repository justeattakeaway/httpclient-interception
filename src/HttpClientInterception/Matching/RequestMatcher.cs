// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace JustEat.HttpClientInterception.Matching
{
    /// <summary>
    /// Represents a class that tests for matches for HTTP requests for a registered HTTP interception.
    /// </summary>
    internal abstract class RequestMatcher
    {
        /// <summary>
        /// Returns a value indicating whether the specified <see cref="HttpRequestMessage"/>
        /// is considered a match as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request to consider a match against.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that returns <see langword="true"/> if <paramref name="request"/>
        /// is considered a match; otherwise <see langword="false"/>.
        /// </returns>
        public abstract Task<bool> IsMatchAsync(HttpRequestMessage request);
    }
}
