// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net.Http;

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
        internal static Func<HttpRequestMessage, CancellationToken, Task<bool>>? ConvertToBooleanTask(Action<HttpRequestMessage>? onIntercepted)
        {
            if (onIntercepted == null)
            {
                return null;
            }

            return (message, _) =>
            {
                onIntercepted(message);
                return TrueTask;
            };
        }

        /// <summary>
        /// Converts an action delegate for an intercepted message to return a
        /// <see cref="Task{TResult}"/> which returns <see langword="true"/>.
        /// </summary>
        /// <param name="onIntercepted">An optional delegate to convert.</param>
        /// <returns>
        /// The converted delegate if <paramref name="onIntercepted"/> has a value; otherwise <see langword="null"/>.
        /// </returns>
        internal static Func<HttpRequestMessage, CancellationToken, Task<bool>>? ConvertToBooleanTask(Predicate<HttpRequestMessage>? onIntercepted)
        {
            if (onIntercepted == null)
            {
                return null;
            }

            return (message, _) => Task.FromResult(onIntercepted(message));
        }

        /// <summary>
        /// Converts a function delegate for an intercepted message to return a
        /// <see cref="Task{TResult}"/> which returns <see langword="true"/>.
        /// </summary>
        /// <param name="onIntercepted">An optional delegate to convert.</param>
        /// <returns>
        /// The converted delegate if <paramref name="onIntercepted"/> has a value; otherwise <see langword="null"/>.
        /// </returns>
        internal static Func<HttpRequestMessage, CancellationToken, Task<bool>>? ConvertToBooleanTask(Func<HttpRequestMessage, Task>? onIntercepted)
        {
            if (onIntercepted == null)
            {
                return null;
            }

            return ConvertToBooleanTask((message, _) => onIntercepted(message));
        }

        /// <summary>
        /// Converts a function delegate for an intercepted message to return a
        /// <see cref="Task{TResult}"/> which returns <see langword="true"/>.
        /// </summary>
        /// <param name="onIntercepted">An optional delegate to convert.</param>
        /// <returns>
        /// The converted delegate if <paramref name="onIntercepted"/> has a value; otherwise <see langword="null"/>.
        /// </returns>
        internal static Func<HttpRequestMessage, CancellationToken, Task<bool>>? ConvertToBooleanTask(Func<HttpRequestMessage, CancellationToken, Task>? onIntercepted)
        {
            if (onIntercepted == null)
            {
                return null;
            }

            return async (message, token) =>
            {
                await onIntercepted(message, token).ConfigureAwait(false);
                return true;
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
        internal static Func<HttpRequestMessage, CancellationToken, Task<bool>>? ConvertToBooleanTask(Func<HttpRequestMessage, Task<bool>>? onIntercepted)
        {
            if (onIntercepted == null)
            {
                return null;
            }

            return async (message, _) => await onIntercepted(message).ConfigureAwait(false);
        }
    }
}
