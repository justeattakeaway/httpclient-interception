// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing methods to help work with delegates. This class cannot be inherited.
    /// </summary>
    internal static class DelegateHelpers
    {
        /// <summary>
        /// A <see cref="Task{TResult}"/> that returns <see langword="true"/>. This field is read-only.
        /// </summary>
        private static readonly Task<bool> TrueTask = Task.FromResult(true);

        /// <summary>
        /// Converts an action delegate for an intercepted message to return a
        /// <see cref="Task{TResult}"/> which returns <see langword="true"/>.
        /// </summary>
        /// <param name="onIntercepted">An optional delegate to convert.</param>
        /// <returns>
        /// The converted delegate if <paramref name="onIntercepted"/> has a value; otherwise <see langword="null"/>.
        /// </returns>
        internal static Func<HttpRequestMessage, Task<bool>> ConvertToBooleanTask(Action<HttpRequestMessage> onIntercepted)
        {
            if (onIntercepted == null)
            {
                return null;
            }

            return (message) =>
            {
                onIntercepted(message);
                return TrueTask;
            };
        }

        /// <summary>
        /// Converts a function delegate for an intercepted message to return a
        /// <see cref="Task{TResult}"/> which returns <see langword="true"/>.
        /// </summary>
        /// <param name="onIntercepted">An optional delegate to convert.</param>
        /// <returns>
        /// The converted delegate if <paramref name="onIntercepted"/> has a value; otherwise <see langword="null"/>.
        /// </returns>
        internal static Func<HttpRequestMessage, Task<bool>> ConvertToBooleanTask(Func<HttpRequestMessage, Task> onIntercepted)
        {
            if (onIntercepted == null)
            {
                return null;
            }

            return async (message) =>
            {
                await onIntercepted(message);
                return true;
            };
        }
    }
}
