// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.HttpClientInterception;

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
    /// A <see cref="Task{TResult}"/> that returns <see langword="false"/>. This field is read-only.
    /// </summary>
    private static readonly Task<bool> FalseTask = Task.FromResult(false);

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
        if (onIntercepted is null)
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
        if (onIntercepted is null)
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
        if (onIntercepted is null)
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
        if (onIntercepted is null)
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
        if (onIntercepted is null)
        {
            return null;
        }

        return async (message, _) => await onIntercepted(message).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a collection of function delegates for a set of predicates to match a request
    /// <see cref="Task{TResult}"/> which returns <see langword="true"/>.
    /// </summary>
    /// <param name="predicates">A collection of delegates to convert.</param>
    /// <returns>
    /// The converted delegate if <paramref name="predicates"/> has a value; otherwise an exception is thrown if <paramref name="predicates"/> is null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="predicates"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// If any <paramref name="predicates"/> value is <see langword="null"/>.
    /// </exception>
    internal static Func<HttpRequestMessage, Task<bool>> ConvertToBooleanTask(Func<HttpRequestMessage, Task<bool>>[] predicates)
    {
        if (predicates == null)
        {
            throw new ArgumentNullException(nameof(predicates));
        }

        foreach (Func<HttpRequestMessage, Task<bool>> predicate in predicates)
        {
            if (predicate == null)
            {
                throw new InvalidOperationException("At least one predicate is null.");
            }
        }

        if (predicates.Length == 1)
        {
            return predicates[0];
        }

        return async (message) =>
        {
            foreach (Func<HttpRequestMessage, Task<bool>> predicate in predicates)
            {
                if (!await predicate(message).ConfigureAwait(false))
                {
                    return false;
                }
            }

            return true;
        };
    }

    /// <summary>
    /// Converts a collection of function delegates for a set of predicates to match a request
    /// <see cref="Task{TResult}"/> which returns <see langword="true"/>.
    /// </summary>
    /// <param name="predicates">A collection of delegates to convert.</param>
    /// <returns>
    /// The converted delegate if <paramref name="predicates"/> has a value; otherwise an exception is thrown if <paramref name="predicates"/> is null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="predicates"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// If any <paramref name="predicates"/> value is <see langword="null"/>.
    /// </exception>
    internal static Func<HttpRequestMessage, Task<bool>> ConvertToBooleanTask(Predicate<HttpRequestMessage>[] predicates)
    {
        if (predicates == null)
        {
            throw new ArgumentNullException(nameof(predicates));
        }

        foreach (Predicate<HttpRequestMessage> predicate in predicates)
        {
            if (predicate == null)
            {
                throw new InvalidOperationException("At least one predicate is null.");
            }
        }

        if (predicates.Length == 1)
        {
            return message => Task.FromResult(predicates[0](message));
        }

        return (message) =>
        {
            foreach (Predicate<HttpRequestMessage> predicate in predicates)
            {
                if (!predicate(message))
                {
                    return FalseTask;
                }
            }

            return TrueTask;
        };
    }
}
