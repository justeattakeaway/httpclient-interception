﻿// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace JustEat.HttpClientInterception;

/// <summary>
/// Represents the exception when an HTTP request is made that has no interception registered. This class cannot be inherited.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#if NETFRAMEWORK
[Serializable]
#endif
public sealed class HttpRequestNotInterceptedException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestNotInterceptedException"/> class.
    /// </summary>
    public HttpRequestNotInterceptedException()
        : base("No HTTP response is configured for this HTTP request.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestNotInterceptedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public HttpRequestNotInterceptedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestNotInterceptedException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="request">The HTTP request message that was not intercepted.</param>
    public HttpRequestNotInterceptedException(string message, HttpRequestMessage request)
        : base(message)
    {
        Request = request;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestNotInterceptedException"/> class with a
    /// specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public HttpRequestNotInterceptedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

#if NETFRAMEWORK
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestNotInterceptedException"/> class with serialized data.
    /// </summary>
    /// <param name="serializationInfo">The object that holds the serialized object data.</param>
    /// <param name="streamingContext">he contextual information about the source or destination.</param>
    private HttpRequestNotInterceptedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#endif

    /// <summary>
    /// Gets the HTTP request message that was not intercepted.
    /// </summary>
    public HttpRequestMessage? Request { get; }
}
